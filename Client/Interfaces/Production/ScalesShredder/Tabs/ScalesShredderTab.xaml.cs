using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.ScalesShredder
{
    /// <summary>
    /// Весы шредера
    /// </summary>
    /// <author>vlasov_ea</author>
    /// <author>greshnyh_ni</author>
    /// <changed>2024-12-06</changed>
    public partial class ScalesShredderTab : UserControl
    {
        public ScalesShredderTab()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            GridInit();
            GridResultInit();

            // таймер проверки пожара
            SetTimerFire();
            // таймер пересменки
            SetTimerShift();

            SetDefaults();
            ComPortInit();
            SetShift();

            ProcessPermissions();
        }

        public string RoleName = "[erp]scales_shredder";

        /// <summary>
        /// данные из выбранной в гриде строки
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// статус пожара на объекте
        /// </summary>
        public string FireInPlace { get; set; }

        /// <summary>
        /// место работы программы
        /// </summary>
        private const string PLACE = "Весы шредера";

        private int CurrentMachineId = 716;

        /// <summary>
        /// место работы программы
        /// </summary>
        private const string PositionsFileName = "Places.txt";

        /// <summary>
        /// Имя вкладки
        /// </summary>
        public string TabName { get; set; }

        /// <summary>
        /// Количество попыток выполнения запроса оприходования и списания
        /// </summary>
        public int AttemptCount { get; set; }

        /// <summary>
        /// таймер проверки пожара
        /// </summary>
        System.Windows.Threading.DispatcherTimer TimerFire;

        /// <summary>
        /// таймер обновления таблиц с обновлением смены
        /// </summary>
        System.Windows.Threading.DispatcherTimer TimerShift;

        /// <summary>
        /// Порт для считывания данных с весов
        /// </summary>
        SerialPort ComPortWeight { get; set; }

        /// <summary>
        /// Буфер выходного потока весов
        /// </summary>
        string WeightInputBuffer { get; set; }

        /// <summary>
        /// регулярное выражение для проверки ввода
        /// </summary>
        public static Regex onlyNumbers = new Regex("[^0-9,]+");

        public string CurrentSklad { get; set; }
        public string CurrentPlace { get; set; }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel(this.RoleName);
            var userAccessMode = mode;
            switch (mode)
            {
                case Role.AccessMode.Special:
                    break;

                case Role.AccessMode.FullAccess:
                    break;

                case Role.AccessMode.ReadOnly:
                default:
                    break;
            }

            //UIUtil.SetFrameworkElementEnabledByTagAccessMode(this.Content as DependencyObject, Acl.AccessMode.ReadOnly);

            List<Button> buttons = UIUtil.GetVisualChilds<Button>(this.Content as DependencyObject);
            if (buttons != null && buttons.Count > 0)
            {
                foreach (var button in buttons)
                {
                    var buttonTagList = UIUtil.GetTagList(button);
                    var accessMode = Acl.FindTagAccessMode(buttonTagList);
                    if (accessMode > userAccessMode)
                    {
                        button.IsEnabled = false;
                    }
                }
            }
        }
                

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            AttemptCount = 1;

            FireStatus.Visibility = Visibility.Hidden;
            FireAlarmImage.Visibility = Visibility.Hidden;

            area1.Background = "#f0f0f0".ToBrush();
            area2.Background = HColor.YellowOrange.ToBrush();
            area3.Background = HColor.Violet.ToBrush();

            ConnectionCaption.Text = "Нет связи с весами!";
            ConnectionCaption.Foreground = HColor.RedFG.ToBrush();

            string[] places = new string[10];
            if (File.Exists(PositionsFileName))
            {
                using (StreamReader streamReader = new StreamReader(PositionsFileName))
                {
                    while (!streamReader.EndOfStream)
                    {
                        string line = streamReader.ReadLine();
                        if (line.Length >= 5)
                        {
                            string[] ss = line.Split('=');
                            if (ss.Length == 2)
                            {
                                int ind = ss[0].ToInt();
                                places[ind] = ss[1];
                            }
                        }
                    }
                }
            }

            Position1.Init(SavePositions, places[1]);
            Position2.Init(SavePositions, places[2]);
            Position3.Init(SavePositions, places[3]);
            Position4.Init(SavePositions, places[4]);
            
            Position5.Init(SavePositions, places[5]);
            Position5.Sklad.IsEnabled = false;
            Position5.Place.IsEnabled = false;

            Position6.Init(SavePositions, places[6]);
            Position6.Sklad.IsEnabled = false;
            Position6.Place.IsEnabled = false;

            Position7.Init(SavePositions, places[7]);
            Position7.Sklad.IsEnabled = false;
            Position7.Place.IsEnabled = false;

            Position8.Init(SavePositions, places[8]);
            Position8.Sklad.IsEnabled = false;
            Position8.Place.IsEnabled = false;
        }

        public void SavePositions()
        {
            using (StreamWriter streamWriter = new StreamWriter(PositionsFileName))
            {
                streamWriter.WriteLine($"Сохранённые ряды и места для программы l-pack_erp:");
                streamWriter.WriteLine($"1={Position1.Sklad.SelectedItem},{Position1.Place.SelectedItem}");
                streamWriter.WriteLine($"2={Position2.Sklad.SelectedItem},{Position2.Place.SelectedItem}");
                streamWriter.WriteLine($"3={Position3.Sklad.SelectedItem},{Position3.Place.SelectedItem}");
                streamWriter.WriteLine($"4={Position4.Sklad.SelectedItem},{Position4.Place.SelectedItem}");
                streamWriter.WriteLine($"5={Position5.Sklad.SelectedItem},{Position5.Place.SelectedItem}");
                streamWriter.WriteLine($"6={Position6.Sklad.SelectedItem},{Position6.Place.SelectedItem}");
                streamWriter.WriteLine($"7={Position7.Sklad.SelectedItem},{Position7.Place.SelectedItem}");
                streamWriter.WriteLine($"8={Position8.Sklad.SelectedItem},{Position8.Place.SelectedItem}");
            }
        }

        #region ComPort
        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void ComPortInit()
        {
            ConnectionCaption.Text = "Подключение к весам...";
            ConnectionCaption.Foreground = HColor.BlackFG.ToBrush();

            if (Central.Config.Ports != null
                && Central.Config.Ports.Count != 0)
            {
                var port = Central.Config.Ports[0];
                try
                {
                    ComPortWeight = new SerialPort();
                    ComPortWeight.DataReceived += ComPort_DataReceived;

                    ComPortWeight.PortName = port.PortName;
                    ComPortWeight.BaudRate = port.BaudRate.ToInt();
                    ComPortWeight.DataBits = port.DataBits.ToInt();
                    ComPortWeight.ReadTimeout = port.ReadTimeout.ToInt();

                    if (port.Parity.ToUpper() == "NONE")
                    {
                        ComPortWeight.Parity = Parity.None;
                    }
                    else if (port.Parity.ToUpper() == "ODD")
                    {
                        ComPortWeight.Parity = Parity.Odd;
                    }
                    else if (port.Parity.ToUpper() == "EVEN")
                    {
                        ComPortWeight.Parity = Parity.Even;
                    }
                    else if (port.Parity.ToUpper() == "MARK")
                    {
                        ComPortWeight.Parity = Parity.Mark;
                    }
                    else if (port.Parity.ToUpper() == "SPACE")
                    {
                        ComPortWeight.Parity = Parity.Space;
                    }

                    if (port.StopBits == "0")
                    {
                        ComPortWeight.StopBits = StopBits.None;
                    }
                    else if (port.Parity == "1")
                    {
                        ComPortWeight.StopBits = StopBits.One;
                    }
                    else if (port.Parity == "1.5" || port.Parity == "1,5")
                    {
                        ComPortWeight.StopBits = StopBits.OnePointFive;
                    }
                    else if (port.Parity == "2")
                    {
                        ComPortWeight.StopBits = StopBits.Two;
                    }


                    if (ComPortWeight.IsOpen)
                    {
                        ComPortWeight.Close();
                    }
                    ComPortWeight.Open();

                    ConnectionCaption.Text = "";
                    ConnectionCaption.Foreground = HColor.Gray.ToBrush();
                }
                catch
                {
                    ConnectionCaption.Text = "Нет связи с весами!";
                    ConnectionCaption.Foreground = HColor.RedFG.ToBrush();
                }
            }
            else
            {
                ConnectionCaption.Text = "Нет связи с весами!";
                ConnectionCaption.Foreground = HColor.RedFG.ToBrush();
            }
        }

        /// <summary>
        /// Функция вызывается каждый раз, когда через порт поступают новые данные 
        /// <para>(На практике не совсем каждый раз. Здесь не критично, но нужно быть осторожным)</para>
        /// </summary>
        private void ComPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var port = (SerialPort)sender;
            try
            {
                WeightInputBuffer += ComPortWeight.ReadExisting();

                // ждём, пока в буфере накопится как минимум одна полная строка вывода весов,
                // отделенная от других строк с обеих сторон символами \r (13 в числовом коде)
                if (WeightInputBuffer.Count(c => c == '\r') > 1)
                {
                    string[] lines = WeightInputBuffer.Split('\r');
                    // гарантированно полная строка вывода весов
                    string line = lines[1];
                    // оставляем только цифры и точку - десятичный разделитель
                    string weightStr = new String(line.Where(c => Char.IsDigit(c) || c == '.').ToArray());
                    double weightDouble;
                    // убераем ведущие ноли
                    // и проверяем еще раз, что это точно число
                    if (double.TryParse(weightStr, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out weightDouble))
                    {
                        int weight = (int)Math.Truncate(weightDouble);

                        this.Dispatcher.Invoke(() =>
                        {
                            Weight.Text = $"{weight}";
                        });
                    }

                    // на всякий случай, чтобы функция не перегрузила поток
                    Thread.Sleep(10);

                    // очищаем буфер
                    WeightInputBuffer = "";
                }
            }
            // данные поступают постоянно, поэтому если пару раз что-то будет не так, то ничего страшного
            catch { }
        }
        #endregion

        #region Common
        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            //Group ProductionTask
            if (m.ReceiverGroup.IndexOf("Production") > -1
                && m.ReceiverName.IndexOf("ProcessMessages") > -1)
            {
                switch (m.Action)
                {
                    case "Refresh":
                        Grid.LoadItems();
                        GridResult.LoadItems();
                        break;
                }
            }
        }

        /// <summary>
        /// обработка ввода с клавиатуры (роли)
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

                case Key.F5:
                    Grid.LoadItems();
                    e.Handled = true;
                    break;

                case Key.Home:
                    Grid.SetSelectToFirstRow();
                    e.Handled = true;
                    break;

                case Key.End:
                    Grid.SetSelectToLastRow();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// Закрытие окна
        /// </summary>
        public void Destroy()
        {

            if (ComPortWeight.IsOpen)
            {
                ComPortWeight.Close();
            }

            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Production",
                ReceiverName = "",
                SenderName = "ScalesShredder",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры гридов
            Grid.Destruct();
            GridResult.Destruct();
        }

        #endregion

        /// <summary>
        /// инициализация компонентов таблицы
        /// </summary>
        public void GridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=175,
                        MaxWidth=200,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ рулона",
                        Path="NUM",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=110,
                        MaxWidth=150,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вес, кг",
                        Path="KOL",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=50,
                        MaxWidth=70,
                    },
                    new DataGridHelperColumn
                    {
                       Header = " ",
                       Path = "_",
                       ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                       MinWidth = 5,
                       MaxWidth = 2000,
                    },
                };
                Grid.SetColumns(columns);

                Grid.SetMode(1);

                Grid.UseRowHeader = true;
                Grid.Init();

                Grid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    { "writeOff", new DataGridContextMenuItem(){
                        Header="Списать",
                        Action=()=>
                        {
                            WriteOff();
                        }
                    }},
                    { "updateTable", new DataGridContextMenuItem(){
                        Header="Обновить таблицу",
                        Action=()=>
                        {
                            GridLoadItems();
                        }
                    }},
                };
                Grid.Menu.Add("Debug", new DataGridContextMenuItem()
                {
                    Visible = false,
                });

                Grid.PrimaryKey = "INWS_ID";

                //при выборе строки в гриде, обновляются актуальные действия для записи
                Grid.OnSelectItem = selectedItem =>
                {
                    SelectedItem = selectedItem;
                    var outVal = new DataGridContextMenuItem();
                    if (Grid.Menu.TryGetValue("Item1", out outVal))
                        outVal.Enabled = !SelectedItem.CheckGet("LOCKED").ToBool();
                };

                //двойной клик на строке предлагает списать рулон
                Grid.OnDblClick = selectedItem =>
                {
                    WriteOff();
                };

                //данные грида
                Grid.OnLoadItems = GridLoadItems;
                Grid.Run();
            }
        }

        /// <summary>
        /// инициализация компонентов результирующей таблицы
        /// </summary>
        public void GridResultInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Источник",
                        Path="SOURCE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=200,
                        MaxWidth=250,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество, шт",
                        Path="C",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=40,
                        MaxWidth=40,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вес, кг",
                        Path="S",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=50,
                        MaxWidth=70,
                    },
                    new DataGridHelperColumn
                    {
                       Header = " ",
                       Path = "_",
                       ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                       MinWidth = 5,
                       MaxWidth = 2000,
                    },
                };
                GridResult.SetColumns(columns);

                GridResult.SetMode(1);

                GridResult.UseRowHeader = true;
                GridResult.Init();

                GridResult.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    { "updateTable", new DataGridContextMenuItem(){
                        Header="Обновить таблицу",
                        Action=()=>
                        {
                            GridResultLoadItems();
                        }
                    }},
                };

                GridResult.Menu.Add("Debug", new DataGridContextMenuItem()
                {
                    Visible = false,
                });

                GridResult.PrimaryKey = "SOURCE";

                //данные грида
                GridResult.OnLoadItems = GridResultLoadItems;
                GridResult.Run();
            }
        }

        /// <summary>
        /// получение данных весов
        /// </summary>
        public async void GridLoadItems()
        {
            Grid.ShowSplash();
            bool resume = true;

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "ScalesShredder");
                q.Request.SetParam("Action", "ListRolls");

                q.Request.SetParam("NUM_PLACE", "-1");
                q.Request.SetParam("SKLAD", "N");

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        Grid.UpdateItems(ds);
                    }
                }
            }
            Grid.HideSplash();
        }

        /// <summary>
        /// получение результирующих данных
        /// </summary>
        public async void GridResultLoadItems()
        {
            GridResult.ShowSplash();
            bool resume = true;

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "ScalesShredder");
                q.Request.SetParam("Action", "ListResult");

                q.Request.SetParam("IDK1", "121");

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        GridResult.UpdateItems(ds);
                    }
                }
            }
            GridResult.HideSplash();
        }

        /// <summary>
        /// Опроиходование/списание тюков в макулатурный пресс
        /// </summary>
        public async void PostBales(int tag, int isNeedSpend)
        {
            DisableControls();
            TakePosition(tag, isNeedSpend);

            bool resume = false;
            int weight = Weight.Text.ToInt();
            int count = Count.Text.ToInt();
            if (weight == 0)
            {
                var d = new DialogWindow($"Укажите корректный вес", "Неверный формат ввода данных");
                d.ShowDialog();
            }
            else if (CurrentSklad.IsNullOrEmpty() || CurrentPlace.IsNullOrEmpty())
            {
                var d = new DialogWindow($"Укажите ряд и место", "Неверный формат ввода данных");
                d.ShowDialog();
            }
            else
            {
                for (int i = 0; i < AttemptCount; i++)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Production");
                    q.Request.SetParam("Object", "ScalesShredder");
                    q.Request.SetParam("Action", "PostBale");

                    q.Request.SetParam("SKLAD", CurrentSklad);
                    q.Request.SetParam("NUM_PLACE", CurrentPlace);
                    q.Request.SetParam("WASTEPAPER_SOURCE", tag.ToString());
                    q.Request.SetParam("QTY", weight.ToString());
                    q.Request.SetParam("CNT", count.ToString());
                    q.Request.SetParam("IS_NEED_SPEND", isNeedSpend.ToString());

                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            if (ds.Items != null)
                            {
                                if (ds.Items.Count != 0)
                                {
                                    string strResume = ds.Items[0].CheckGet("RESULT");
                                    resume = strResume.ToBool();
                                }
                            }
                        }
                    }

                    // Если запрос отработал успешно - больше попытки отправить запрос не делаются
                    if (resume)
                    {
                        SavePositions();
                        PostSuccess();
                        break;
                    }
                    // иначе ждём секунду и пробуем ещё раз
                    Thread.Sleep(1000);
                }

                if (!resume)
                {
                    PostFailure();
                    var d = new DialogWindow($"Не удалось отправить тюки", "Оприходование/списание тюков", "Повторите попытку", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            EnableControls();
        }

        /// <summary>
        /// Определение позиции
        /// </summary>
        private void TakePosition(int tag, int isNeedSpend)
        {
            if (tag == 11 || tag == 2)
            {
                CurrentSklad = Position1.Sklad.Text;
                CurrentPlace = Position1.Place.Text;
            }
            if (tag == 12 || tag == 4)
            {
                CurrentSklad = Position2.Sklad.Text;
                CurrentPlace = Position2.Place.Text;
            }
            if (tag == 9 || tag == 1)
            {
                CurrentSklad = Position3.Sklad.Text;
                CurrentPlace = Position3.Place.Text;
            }
            if (tag == 10 || tag == 6)
            {
                CurrentSklad = Position4.Sklad.Text;
                CurrentPlace = Position4.Place.Text;
            }
            if (tag == 3)
            {
                CurrentSklad = Position5.Sklad.Text;
                CurrentPlace = Position5.Place.Text;
            }
            if (tag == 8)
            {
                CurrentSklad = Position6.Sklad.Text;
                CurrentPlace = Position6.Place.Text;
            }
            if (tag == 13)
            {
                CurrentSklad = Position7.Sklad.Text;
                CurrentPlace = Position7.Place.Text;
            }
            if (tag == 14)
            {
                CurrentSklad = Position8.Sklad.Text;
                CurrentPlace = Position8.Place.Text;
            }

            if (isNeedSpend == 1)
            {
                if (tag != 2
                    && tag != 4
                    && tag != 1
                    && tag != 6
                    )
                {
                    //оприходование в виртуальную ячейку и затем сразу списание
                    CurrentSklad = "N";
                    CurrentPlace = "-6"; //"-2"; //14-11-2024
                }
            }
        }

        /// <summary>
        /// Списание
        /// </summary>
        public async void WriteOff()
        {
            DisableControls();
            bool resume = false;
            var d = new DialogWindow($"Вы дейтвительно хотите списать данный рулон?\n Наименование: {SelectedItem.CheckGet("NAME")}\n № рулона: {SelectedItem.CheckGet("NUM")}\n Вес: {SelectedItem.CheckGet("KOL").ToInt()} кг", "Всё верно?", "", DialogWindowButtons.YesNo);
            d.ShowDialog();
            if (d.DialogResult == true)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "ScalesShredder");
                q.Request.SetParam("Action", "WriteOff");

                q.Request.SetParam("IDP", SelectedItem.CheckGet("IDP"));
                q.Request.SetParam("ID_ST", CurrentMachineId.ToString());

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds.Items != null)
                        {
                            if (ds.Items.Count != 0)
                            {
                                string strResult = ds.Items[0].CheckGet("RESULT");
                                if (strResult == "1")
                                {
                                    resume = true;
                                }
                            }
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }

                if (!resume)
                {
                    PostFailure();
                    var dresult = new DialogWindow($"Не удалось списать рулон", "Ошибка", "Повторите попытку", DialogWindowButtons.OK);
                    dresult.ShowDialog();
                }
                else
                {
                    PostSuccess();
                }
            }
            EnableControls();
        }

        #region Fire Alarm
        /// <summary>
        /// установка таймера проверки пожара
        /// </summary>
        public void SetTimerFire()
        {
            TimerFire = new System.Windows.Threading.DispatcherTimer();
            TimerFire.Tick += new EventHandler(FireAlarmCheck);
            TimerFire.Interval = new TimeSpan(0, 0, 10);
            TimerFire.Start();

            {
                var row = new Dictionary<string, string>();
                row.CheckAdd("TIMEOUT", "10");
                row.CheckAdd("DESCRIPTION", "");
                Central.Stat.TimerAdd("ScalesShredderTab_SetTimerFire", row);
            }
        }
        /// <summary>
        /// Обработчик нажатия на кнопку пожара
        /// </summary>
        private void FireAlarmButton_Click(object sender, RoutedEventArgs e)
        {
            FireAlarm();
        }
        /// <summary>
        /// Сообщение о пожаре
        /// </summary>
        private void FireAlarm()
        {
            if (FireInPlace == null)
            {
                var d = new DialogWindow($"Объявить пожарную тревогу на объекте?", "Пожарная тревога", "", DialogWindowButtons.YesNo);
                d.ShowDialog();
                if (d.DialogResult == true)
                {
                    TimerFire.Stop();


                    DisableControls();
                    FireStatus.Visibility = Visibility.Visible;
                    FireStatus.Text = $"Пожар! {PLACE}";
                    FireAlarmButton.Style = (Style)FireAlarmButton.TryFindResource("RollReelButtonActive");
                    FireInPlace = PLACE;

                    FireAlarmImage.Visibility = Visibility.Visible;

                    UpdateFireStatus(PLACE);

                    EnableControls();
                }
            }
            else if (FireInPlace == PLACE)
            {
                DisableControls();

                FireInPlace = null;
                FireStatus.Visibility = Visibility.Hidden;
                FireAlarmButton.Style = (Style)FireAlarmButton.TryFindResource("RollReelButton");

                FireAlarmImage.Visibility = Visibility.Hidden;

                UpdateFireStatus();

                EnableControls();

                TimerFire.Start();
            }
        }

        /// <summary>
        /// запрос на изменение места пожара
        /// </summary>
        /// <param name="place"> место пожара, если 'null' значит пожара нет</param>
        public void UpdateFireStatus(string place = "")
        {
            bool resume = true;

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "IndustrialWaste");
                q.Request.SetParam("Action", "UpdateFire");

                var p = new Dictionary<string, string>();

                p.Add("PLACE", place);
                p.Add("FIRE_NAME", "FIRE_BDM1");

                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();
            }
        }

        /// <summary>
        /// проверка пожара
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void FireAlarmCheck(object sender, EventArgs e)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "IndustrialWaste");
            q.Request.SetParam("Action", "ListFire");

            var p = new Dictionary<string, string>();
            p.Add("FIRE_NAME", "FIRE_BDM1");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    FireInPlace = ds.Items[0]["PARAM_VALUE"];

                    if (FireInPlace != null && FireInPlace != "null")
                    {
                        FireStatus.Text = "Пожар! " + FireInPlace;
                        FireStatus.Visibility = Visibility.Visible;
                        FireAlarmImage.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        FireStatus.Visibility = Visibility.Hidden;
                        FireAlarmImage.Visibility = Visibility.Hidden;
                    }
                }
            }
        }
        #endregion


        #region Shift (пересменка)
        /// <summary>
        /// установка таймера пересменки
        /// </summary>
        public async void SetTimerShift()
        {

            TimerShift = new System.Windows.Threading.DispatcherTimer();
            TimerShift.Tick += new EventHandler(ShiftUpdate);

            

            DateTime now = DateTime.Now;
            DateTime eightAM = DateTime.Today.AddHours(8).AddMinutes(1).AddSeconds(10);
            DateTime eightPM = DateTime.Today.AddHours(20).AddMinutes(1).AddSeconds(10);

            // если время пересменки сегодня уже прошло, берем время пересменки следующего дня
            if (now > eightAM)
            {
                eightAM = eightAM.AddDays(1.0);
            }
            if (now > eightPM)
            {
                eightPM = eightPM.AddDays(1.0);
            }

            int secondsUntilEightAM = (int)((eightAM - now).TotalSeconds);
            int secondsUntilEightPM = (int)((eightPM - now).TotalSeconds);
            int secondsUntilNextShift = Math.Min(secondsUntilEightAM, secondsUntilEightPM);

            SetShift();

            TimerShift.Interval = new TimeSpan(0, 0, secondsUntilNextShift);
            TimerShift.Start();

            {
                var row = new Dictionary<string, string>();
                row.CheckAdd("TIMEOUT", secondsUntilNextShift.ToString());
                row.CheckAdd("DESCRIPTION", "");
                Central.Stat.TimerAdd("ScalesShredderTab_SetTimerShift", row);
            }
        }

        /// <summary>
        /// пересменка
        /// </summary>
        private async void ShiftUpdate(object sender, EventArgs e)
        {
            TimerShift.Interval = new TimeSpan(12, 0, 0);
            GridLoadItems();
            GridResultLoadItems();
            SetShift();
        }
        /// <summary>
        /// Получение записи текущей смены из БД
        /// </summary>
        private async void SetShift()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "ScalesShredder");
            q.Request.SetParam("Action", "RecordShift");

            q.Request.SetParam("ID_ST", CurrentMachineId.ToString());

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "SHIFT");
                    if (ds.Items != null)
                    {
                        if (ds.Items.Count != 0)
                        {
                            string strDataFrom = ds.Items[0].CheckGet("DATA_FROM");
                            string strDataTo = ds.Items[0].CheckGet("DATA_TO");

                            DateTime dataFrom = strDataFrom.ToDateTime();
                            DateTime dataTo = strDataTo.ToDateTime();

                            if (dataFrom != DateTime.MinValue
                                && dataTo != DateTime.MinValue
                                )
                            {
                                ShiftTextBlock.Text = $"За смену {dataFrom.ToString("HH:mm")}-{dataTo.ToString("HH:mm")} оприходовано";
                            }
                        }
                    }
                }
            }
        }
        #endregion


        public async void PostSuccess()
        {
            areaBig.Background = HColor.Green.ToBrush();
            areaBig.Visibility = Visibility.Visible;
            textBlockBig.Text = "Операция прошла успешно";
            textBlockBig.Visibility = Visibility.Visible;

            await Task.Run(() =>
            {
                Thread.Sleep(5000);
            });

            areaBig.Visibility = Visibility.Collapsed;
            textBlockBig.Visibility = Visibility.Collapsed;
        }
        public async void PostFailure()
        {
            areaBig.Background = HColor.Red.ToBrush();
            areaBig.Visibility = Visibility.Visible;
            textBlockBig.Text = "В ходе операции произошла ошибка, повторите попытку";
            textBlockBig.Visibility = Visibility.Visible;

            await Task.Run(() =>
            {
                Thread.Sleep(5000);
            });

            areaBig.Visibility = Visibility.Collapsed;
            textBlockBig.Visibility = Visibility.Collapsed;
        }

        public void DisableControls()
        {
            Grid.ShowSplash();
            GridResult.ShowSplash();
            foreach (Button button in FormHelper.FindLogicalChildren<Button>(this))
            {
                button.IsEnabled = false;
            }
            BurgerButton.IsEnabled = true;
        }
        public void EnableControls()
        {
            Grid.HideSplash();
            GridResult.HideSplash();
            foreach (Button button in FormHelper.FindLogicalChildren<Button>(this))
            {
                button.IsEnabled = true;
            }
            Grid.LoadItems();
            GridResult.LoadItems();

            ProcessPermissions();
        }

        /// <summary>
        /// должны быть только цифры
        /// </summary>
        private void PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = onlyNumbers.IsMatch(e.Text);
        }

        /// <summary>
        /// обработчик нажатия на кнопку документации
        /// </summary>
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        /// <summary>
        /// Вызов справки
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/production/scales_shredder");
        }

        /// <summary>
        /// Отладочная информация
        /// </summary>
        private void ShowInfo()
        {
            var t = "Отладочная информация";
            var m = Central.MakeInfoString();
            var i = new ErrorTouch();
            i.Show(t, m);
        }

        /// <summary>
        /// Обработчик нажатия на кнопку Обновить
        /// </summary>
        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            Restart();
        }

        /// <summary>
        /// рестарт программы
        /// </summary>
        private void Restart()
        {
            TimerFire.Stop();
            TimerShift.Stop();

            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Main",
                ReceiverName = "MainWindow",
                SenderName = "Navigator",
                Action = "Restart",
                Message = "",
            });
        }

        /// <summary>
        /// Обработчик нажатия на кнопку Выход
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BurgerExit_Click(object sender, RoutedEventArgs e)
        {
            Exit();
        }
        /// <summary>
        /// закрытие программы
        /// </summary>
        private void Exit()
        {
            TimerFire.Stop();
            TimerShift.Stop();

            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Main",
                ReceiverName = "MainWindow",
                SenderName = "Navigator",
                Action = "Exit",
                Message = "",
            });
        }

        /// <summary>
        /// Отображает меню бургера по нажатию
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BurgerButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }
        /// <summary>
        /// обработчик нажатия на кнопку Информация
        /// </summary>
        private void InfoButto_Click(object sender, RoutedEventArgs e)
        {
            ShowInfo();
        }

        private void RecieveAndSpend_Click(object sender, RoutedEventArgs e)
        {
            int tag = 0;

            var buttonTagList = UIUtil.GetTagList(sender as Button);
            foreach (var buttonTag in buttonTagList) 
            {
                if (buttonTag.ToInt() > 0)
                {
                    tag = buttonTag.ToInt();
                }
            }

            if (tag > 0)
            {
                string bales = "";
                if (tag >= 9 && tag <= 12)
                {
                    bales = "рваные тюки из ";
                }
                if (tag == 2
                     || tag == 4
                     || tag == 1
                     || tag == 6)
                {
                    bales = "целые тюки из ";
                }

                string name = ((sender as Button).Content as TextBlock).Text;
                string nameLowFirstChar = char.ToLower(name[0]) + name.Substring(1);

                var d = new DialogWindow($"Вы дейтвительно хотите оприходовать и списать {bales}{nameLowFirstChar}?", "Всё верно?", "", DialogWindowButtons.YesNo);
                d.ShowDialog();
                if (d.DialogResult == true)
                {
                    PostBales(tag, 1);

                    foreach (Button button in FormHelper.FindLogicalChildren<Button>(this))
                    {
                        button.Background = HColor.White.ToBrush();
                    }
                    (sender as Button).Background = "#eaeaea".ToBrush();
                }
            }
        }

        private void Recieve_Click(object sender, RoutedEventArgs e)
        {
            int tag = 0;

            var buttonTagList = UIUtil.GetTagList(sender as Button);
            foreach (var buttonTag in buttonTagList)
            {
                if (buttonTag.ToInt() > 0)
                {
                    tag = buttonTag.ToInt();
                }
            }

            if (tag > 0)
            {
                string bales = "";
                if (tag == 2
                     || tag == 4
                     || tag == 1
                     || tag == 6)
                {
                    bales = "тюки из ";
                }

                string name = ((sender as Button).Content as TextBlock).Text;
                string nameLowFirstChar = char.ToLower(name[0]) + name.Substring(1);

                var d = new DialogWindow($"Вы дейтвительно хотите оприходовать {bales}{nameLowFirstChar}?", "Всё верно?", "", DialogWindowButtons.YesNo);
                d.ShowDialog();
                if (d.DialogResult == true)
                {
                    PostBales(tag, 0);

                    foreach (Button button in FormHelper.FindLogicalChildren<Button>(this))
                    {
                        button.Background = HColor.White.ToBrush();
                    }
                    (sender as Button).Background = "#eaeaea".ToBrush();
                }
            }
        }

        private void WriteOff_Click(object sender, RoutedEventArgs e)
        {
            WriteOff();
        }

        private void Count_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            int count = (sender as TextBox).Text.ToInt();
            if (e.ChangedButton == MouseButton.Right)
            {
                count--;
            }
            if (e.ChangedButton == MouseButton.Left)
            {
                count++;
            }
            if (count < 1)
            {
                count = 1;
            }
            if (count > 4)
            {
                count = 4;
            }
            (sender as TextBox).Text = count.ToString();
        }

        private void areaBig_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            areaBig.Visibility = Visibility.Collapsed;
            textBlockBig.Visibility = Visibility.Collapsed;
        }
    }
}
