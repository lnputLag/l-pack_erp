using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Accounts;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction.Rig.RigMonitorKsh.Elements;
using DevExpress.Xpf.Core;
using DevExpress.XtraPrinting.Native;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using static Client.Common.Role;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.MoldedContainer
{
    /// <summary>
    /// Отчёт по весам заготовок на ВФМ литой тары
    /// </summary>
    /// <author>greshnyh_ni</author>   
    public partial class RecyclingVacuumFormingMachineWeight : UserControl
    {
        public RecyclingVacuumFormingMachineWeight()
        {
            InitializeComponent();

            FormInit();
            SetDefaults();
            GridInit();

            Grid.SelectItemMode = 2;

            ProcessPermissions();
            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            //  WeightIs = false;

            ComPortInit();

        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Выбранная запись в гриде
        /// </summary>
        public Dictionary<string, string> WeightSelectedItem { get; set; }

        private bool ReadOnly { get; set; }

        /// <summary>
        /// Порт для считывания данных с весов
        /// </summary>
        SerialPort ComPortWeight { get; set; }

        /// <summary>
        /// Буфер выходного потока весов
        /// </summary>
        string WeightInputBuffer { get; set; }
        /// <summary>
        /// признак для получения веса заготовки
        /// </summary>
        bool WeightIs { get; set; }

        /// <summary>
        /// Инициализация грида
        /// </summary>
        public void GridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width=40,
                },
                new DataGridHelperColumn
                {
                    Header="Станок",
                    Path="MACHINE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width=160,
                },
                new DataGridHelperColumn
                {
                    Header = "Дата замера",
                    Path = "DTTM",
                    ColumnType = ColumnTypeRef.String,
                    Width = 130,
                },
                new DataGridHelperColumn
                {
                    Header = "Смена",
                    Path = "WOTE_ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Width = 65,
                },
                new DataGridHelperColumn
                {
                    Header = "Вес",
                    Path = "WEIGHT",
                    ColumnType = ColumnTypeRef.Integer,
                    Width = 110,
                },
                new DataGridHelperColumn
                {
                    Header = "",
                    Name="_",
                    Path = "_",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 5,
                    MaxWidth = 1200,
                },
            };

            Grid.PrimaryKey = "_ROWNUMBER";
            Grid.SetColumns(columns);

            Grid.SearchText = SearchText;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem = item =>
            {
                WeightSelectedItem = item;
            };


            //данные грида
            Grid.OnLoadItems = GridLoadItems;

            Grid.AutoUpdateInterval = 5 * 60;
            Grid.SetSorting("DTTM");

            Grid.Init();
            Grid.Run();
            //фокус ввода       
            Grid.Focus();
        }

        /// <summary>
        /// Получение данных для заполнения грида
        /// </summary>
        public async void GridLoadItems()
        {
            DisableControls();

            var resume = true;

            var f = Form.GetValueByPath("FROM_DATE").ToDateTime();
            var t = Form.GetValueByPath("TO_DATE").ToDateTime();

            if (DateTime.Compare(f, t) > 0)
            {
                const string msg = "Дата начала должна быть меньше даты окончания.";
                var d = new DialogWindow($"{msg}", "Проверка данных");
                d.ShowDialog();
                resume = false;
            }

            if (resume)
            {
                var st = Machines.SelectedItem.Key.ToInt().ToString();
                if (Machines.SelectedItem.Key.ToInt() == 0)
                    st = "";

                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID_ST", st);
                    p.CheckAdd("FROM_DATE", Form.GetValueByPath("FROM_DATE"));
                    p.CheckAdd("TO_DATE", Form.GetValueByPath("TO_DATE"));
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "MoldedContainer");
                q.Request.SetParam("Object", "Scales");
                q.Request.SetParam("Action", "WeightList");

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
                        Grid.UpdateItems(ds);
                    }
                }
            }

            EnableControls();
        }

        /// <summary>
        /// Деактивация контролов
        /// </summary>
        public void DisableControls()
        {
            Grid.ShowSplash();
            GridToolbar.IsEnabled = false;
        }

        /// <summary>
        /// Активация контролов
        /// </summary>
        public void EnableControls()
        {
            Grid.HideSplash();
            GridToolbar.IsEnabled = true;
        }

        /// <summary>
        /// Управление доступом
        /// </summary>
        /// <param name="roleCode"></param>
        private void ProcessPermissions(string roleCode = "")
        {
            string role = "";
            // Проверяем уровень доступа
            role = "[erp]molded_contnr_scales";

            var mode = Central.Navigator.GetRoleLevel(role);
            var userAccessMode = mode;

            switch (mode)
            {
                case Common.Role.AccessMode.Special:
                    {
                        AddButton.IsEnabled = false;
                        ReadOnly = false;
                    }
                    break;

                case Common.Role.AccessMode.FullAccess:
                    {
                        AddButton.IsEnabled = true;
                        ReadOnly = false;
                    }
                    break;

                case Common.Role.AccessMode.ReadOnly:
                    {
                        AddButton.IsEnabled = false;
                        ReadOnly = true;
                    }
                    break;
            }

            if (Grid != null && Grid.Menu != null && Grid.Menu.Count > 0)
            {
                foreach (var manuItem in Grid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            ShowButton.Style = (Style)ShowButton.TryFindResource("Button");

            var start = DateTime.Now;

            int hour = start.Hour;

            if (hour > 20)
            {
                start = new DateTime(start.Year, start.Month, start.Day, 20, 0, 0);
            }
            else
            {
                start = new DateTime(start.Year, start.Month, start.Day, 8, 0, 0);
            }

            FromDate.EditValue = start;
            ToDate.EditValue = start.AddHours(12);

            CurrentWeight.Content = "0";  // вес
            FormStatus.Content = "";      // сообщение по весам
        }

        /// <summary>
        /// Закрытие окна
        /// </summary>
        public void Destroy()
        {
            ////отправляем сообщение о закрытии окна
            //Messenger.Default.Send(new ItemMessage()
            //{
            //    ReceiverGroup = "PaperProduction",
            //    ReceiverName = "",
            //    SenderName = "RecyclingVacuumFormingMachineWeight",
            //    Action = "Closed",
            //});

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// Инициализация формы
        /// </summary>
        public void FormInit()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path = "FROM_DATE",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = FromDate,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path = "TO_DATE",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = ToDate,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path="MACHINE_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Default="0",
                    Control=Machines,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange=(FormHelperField f, string v)=>
                    {
                    },
                    QueryLoadItems = new RequestData()
                    {
                        Module = "MoldedContainer",
                        Object = "Scales",
                        Action = "ListByIdSt",
                        AnswerSectionKey="ITEMS",
                        OnComplete = (FormHelperField f,ListDataSet ds) =>
                        {
                            var list=ds.GetItemsList("MACHINE_ID","MACHINE_NAME");
                            var c=(SelectBox)f.Control;
                            if(c != null)
                            {
                                c.Items=list;
                                Machines.SetSelectedItemByKey("0.0");
                            }
                        },
                    },
                },
            };

            Form.SetFields(fields);
            Form.ToolbarControl = GridToolbar;
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.IndexOf("MoldedContainer") > -1)
            {
                if (m.ReceiverName.IndexOf("RecyclingVacuumFormingMachineWeight") > -1)
                {
                    switch (m.Action)
                    {
                        case "RefreshRecyclingWeightList":
                            {
                                GridLoadItems();
                            }
                            break;
                    }
                }
            }
        }


        /// <summary>
        /// Обработчик выбора станка
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void Types_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
            ProcessPermissions();
            GridLoadItems();
            
            if (CurrentWeight.Content.ToInt() > 0)
            {
                if (Machines.SelectedItem.Key.ToInt() == 0)
                    WeightButton.IsEnabled = false;
                else
                    WeightButton.IsEnabled = true;
            }
                
        }

        /// <summary>
        /// Документация
        /// </summary>
        private void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/production/machine_report/report_write_off");
        }

        private void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            ShowButton.Style = (Style)ShowButton.TryFindResource("Button");
            GridLoadItems();
        }

        private void ExcelButton_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcel();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }
        private void DateTextChanged(object sender, RoutedEventArgs e)
        {
            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
        }

        private void LeftShiftButton_Click(object sender, RoutedEventArgs e)
        {
            if (FromDate.EditValue is DateTime start)
            {
                start = start.AddHours(-12);
                int hour = start.Hour;

                if (hour > 12)
                {
                    start = new DateTime(start.Year, start.Month, start.Day, 20, 0, 0);
                }
                else
                {
                    start = new DateTime(start.Year, start.Month, start.Day, 8, 0, 0);
                }

                FromDate.EditValue = start;
                ToDate.EditValue = start.AddHours(12);

                GridLoadItems();
            }
        }

        private void RightShiftButton_Click(object sender, RoutedEventArgs e)
        {
            if (FromDate.EditValue is DateTime start)
            {
                start = start.AddHours(12);
                int hour = start.Hour;

                if (hour > 12)
                {
                    start = new DateTime(start.Year, start.Month, start.Day, 20, 0, 0);
                }
                else
                {
                    start = new DateTime(start.Year, start.Month, start.Day, 8, 0, 0);
                }

                FromDate.EditValue = start;
                ToDate.EditValue = start.AddHours(12);

                GridLoadItems();
            }

        }

        /// <summary>
        /// экспорт записей грида в Excel
        /// </summary>
        private async void ExportToExcel()
        {
            DisableControls();

            if (Grid.Items != null)
            {
                if (Grid.Items.Count > 0)
                {
                    var eg = new ExcelGrid();
                    var cols = Grid.Columns;
                    eg.SetColumnsFromGrid(cols);
                    eg.Items = Grid.Items;
                    await Task.Run(() =>
                    {
                        eg.Make();
                    });
                }
            }

            EnableControls();
        }


        /// <summary>
        ///  нажали кнопку "добавить вес"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("", "");
            }

            WeihgtEdit(p);
        }

        /// <summary>
        ///  добавляем вес заготовки вручную
        /// </summary>
        /// <param name="idle"></param>
        public void WeihgtEdit(Dictionary<string, string> idle)
        {
            if (idle != null)
            {
                var weightRecord = new RecyclingWeightRecord(idle);
                weightRecord.ReceiverName = "RecyclingVacuumFormingMachineWeight";
                weightRecord.Edit();
            }
        }


        /// <summary>
        /// сохраняем вес заготовки автоматически для выбранного ВФМ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WeightButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentWeight.Content.ToInt() > 0)
            {
                ShowButton.Style = (Style)ShowButton.TryFindResource("Button");
                WeightButton.IsEnabled = false;
                SaveData();
            }
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void ComPortInit()
        {
            FormStatus.Content = "";
            CurrentWeight.Foreground = HColor.BlackFG.ToBrush();

            if (Central.Config.Ports != null && Central.Config.Ports.Count != 0)
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
                    CurrentWeight.Foreground = HColor.GreenFG.ToBrush();

                    if (Machines.SelectedItem.Key.ToInt() == 0)
                        WeightButton.IsEnabled = false;
                    else
                        WeightButton.IsEnabled = true;
                }
                catch
                {
                    CurrentWeight.Foreground = HColor.RedFG.ToBrush();
                    FormStatus.Content = "Нет связи с весами!";
                    WeightButton.IsEnabled = false;
                }
            }
            else
            {
                FormStatus.Content = "Не указан ComPort для связи с весами!";
                WeightButton.IsEnabled = false;
            }
        }

        /// <summary>
        /// Функция вызывается каждый раз, когда через порт поступают новые данные 
        /// </summary>
        private void ComPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var port = (SerialPort)sender;
            System.Threading.Thread.Sleep(1000);

            try
            {
                WeightInputBuffer += ComPortWeight.ReadExisting();

                // ждём, пока в буфере накопится как минимум одна полная строка вывода весов,
                // отделенная от других строк с обеих сторон символами S 
                if (WeightInputBuffer.Count(c => c == 'S') > 1)
                {
                    string[] lines = WeightInputBuffer.Split('S');
                    // гарантированно полная строка вывода весов
                    string line = lines[1];

                    // вырезаем только цифры
                    line = line.Substring(0, 5);

                    // оставляем только цифры и точку - десятичный разделитель
                    string weightStr = new String(line.Where(c => Char.IsDigit(c)).ToArray());

                    double weightDouble;
                    // убираем ведущие ноли
                    // и проверяем еще раз, что это точно число
                    if (double.TryParse(weightStr, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out weightDouble))
                    {
                        int weight = (int)Math.Truncate(weightDouble);

                        this.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate ()
                        {
                            CurrentWeight.Content = $"{weight}";
                        }));
                    }
                }

                // на всякий случай, чтобы функция не перегрузила поток
                System.Threading.Thread.Sleep(10);

                // очищаем буфер
                WeightInputBuffer = "";

            }
            catch
            {
                if (ComPortWeight.IsOpen)
                {
                    ComPortWeight.Close();
                }
            }
        }

        /// <summary>
        /// Добавляем данные по замеру
        /// </summary>
        /// <param name="p"></param>
        private async void SaveData()
        {
            var q = new LPackClientQuery();

            var p = new Dictionary<string, string>();
            {
                p.Add("ID_ST", Machines.SelectedItem.Key.ToInt().ToString());
                p.Add("WEIGHT", CurrentWeight.Content.ToString());
            }

            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Scales");
            q.Request.SetParam("Action", "WeightAdd");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            WeightButton.IsEnabled = true;

            if (q.Answer.Status == 0)
            {
                GridLoadItems();
            }
            else
            {
                q.ProcessError();
            }
        }


    }
}
