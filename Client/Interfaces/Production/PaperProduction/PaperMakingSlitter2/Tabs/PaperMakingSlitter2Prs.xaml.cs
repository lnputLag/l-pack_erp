using AutoUpdaterDotNET;
using Client.Annotations;
using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Stock;
using DevExpress.ClipboardSource.SpreadsheetML;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using DevExpress.Xpf.Core.DragDrop.Native;
using DevExpress.Xpf.Core.Native;
using DevExpress.Xpf.Grid;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Office.Interop.Excel;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Интерфейс оператора ПРС на БДМ2
    /// </summary>
    /// <author>Greshnyh_NI</author>   
    /// 
    public partial class PmSlitter2List : UserControl
    {
        public PmSlitter2List()
        {
            TabName = "PaperMakingSlitter2";
            DocumentationUrl = "/doc/l-pack-erp/production/molded_container/machine_control";
            InitializeComponent();

            Form = null;
            MachineId = 1716;
            ControlPlcConnectionClient = null;

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            Initialized = false;
            FormInit();
            SetDefaults();
            Init();
            TaskLoadItems();
            RollsGridInit();

            // получение прав пользователя
            ProcessPermissions();
            Initialized = true;

            ButtonTimer = new Timeout(
      3,
      () =>
      {
          ButtonPush();
      },
      true,
      false
  );
            ButtonTimer.Finish();
        }

        public FormHelper Form { get; set; }
        /// <summary>
        /// Имя вкладки
        /// </summary>
        public string TabName;

        public string DocumentationUrl { get; set; }
        public bool Initialized { get; set; }

        /// <summary>
        /// ИД  станка = 1716
        /// </summary>
        private int MachineId { get; set; }

        /// <summary>
        /// Место работы программы
        /// </summary>
        private string CurrentPlaceName;
        /// <summary>
        /// Таймер проверки пожара
        /// </summary>
        private DispatcherTimer TimerFire;
        /// <summary>
        /// Статус пожара на объекте
        /// </summary>
        private string FireInPlace { get; set; }

        /// <summary>
        /// Таймер периодического обновления каждую минуту
        /// </summary>
        private DispatcherTimer SlowTimer { get; set; }

        /// Таймер периодического обновления каждую  секунду
        /// </summary>
        private DispatcherTimer FastTimer { get; set; }

        private ListDataSet DataSet { get; set; }
        /// <summary>
        /// данные для печати ярлыка
        /// </summary>
        private ListDataSet DataRollForLabel { get; set; }

        private ListDataSet RollsGridDataSet { get; set; }
        private Dictionary<string, string> SelectedRollsItem { get; set; }

        /// <summary>
        /// Очередь текущего задания в процессе загрузки
        /// </summary>
        private bool IsQueueLoading { get; set; }

        /// <summary>
        /// Очередь списка тамбуров и рулонов в процессе загрузки
        /// </summary>
        private bool IsRollsQueueLoading { get; set; }

        /// <summary>
        ///  видимость журнала работы по
        /// </summary>
        private bool LogViewFlag { get; set; }

        /// <summary>
        /// флаг доступности кнопок
        /// </summary>
        private bool ReadOnlyFlag { get; set; }

        /// <summary>
        ///  время обновления гридов (сек)
        /// </summary>
        private int RefreshTime { get; set; }

        /// <summary>
        ///  последний номер рулона с тамбура
        /// </summary>
        private int NumRollTmbr { get; set; }

        /// <summary>
        ///  url адреса весов
        /// </summary>
        string urlWeight = "http://192.168.132.72/weight.html";

        private WebClient ControlPlcConnectionClient { get; set; }

        /// <summary>
        ///  IP платы Laurent для подачи головки торцевого принтера
        /// </summary>
        string IpLaurent = "192.168.132.224";

        /// <summary>
        ///  IP торцевого принтера 
        /// </summary>
        string TorquePrinterIp = "192.168.132.75";

        /// <summary>
        ///  Port торцевого принтера 
        /// </summary>
        int TorquePrinterPort = 10001;

        /// данные для вывода на торцевой принтер
        /// <summary>
        ///  вес рулона
        /// </summary>
        private string Kol { get; set; }
        /// <summary>
        ///  марка рулона
        /// </summary>        
        private string Name { get; set; }
        /// <summary>
        /// внутренний номер рулона
        /// </summary>
        private string Num { get; set; }

        /// <summary>
        ///  текущее ПЗ для станка
        /// </summary>
        private int CurIdPz { get; set; }

        /// <summary>
        ///  Idp_roll
        /// </summary>
        private int IdpRoll { get; set; }

        private Timeout RefreshButtonTimeout { get; set; }

        /// <summary>
        /// Таймер задержки повторного нажатия кнопки
        /// </summary>
        public Timeout ButtonTimer { get; set; }

        /// <summary>
        ///  признак работы таймера обновления кнопок
        /// </summary>
        public bool ButtonTimerRun { get; set; }

        /// <summary>
        /// Массив кнопок для их сброса в первоначальное состояние
        /// </summary>
        private bool[] buttons = new bool[10];

        /// <summary>
        ////количество секунд до обновления информации
        /// </summary>
        private int CurSecund { get; set; }

        /// <summary>
        /// Флаг того, что идет оприходование рулона
        /// </summary>
        public bool QueryInProgress = false;

        ///////////////////////////////////////////////////


        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            string role = "";
            // Проверяем уровень доступа
            role = "[erp]slitter_bdm2";

            var mode = Central.Navigator.GetRoleLevel(role);
            var userAccessMode = mode;
            ReadOnlyFlag = true;
            switch (mode)
            {
                case Role.AccessMode.Special:
                    {

                    }

                    break;

                case Role.AccessMode.FullAccess:
                    {
                        ReadOnlyFlag = false;
                    }
                    break;

                case Role.AccessMode.ReadOnly:
                    {
                    }
                    break;
            }
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            RefreshTime = 40;
            SetCurrentTime();
            CurIdPz = 0;
            FireStatus.Text = "";
            FireStatus.Visibility = Visibility.Collapsed;
            V0.Text = V1.Text = V2.Text = V3.Text = V4.Text = V5.Text = V6.Text = V7.Text = V8.Text = V9.Text = V10.Text = V11.Text = V12.Text = V13.Text = "";
            CurrentPlaceName = "Локация БДМ2 (станок ПРС)";
            LogViewFlag = false;
            LogPanel.Visibility = Visibility.Hidden;
            ButtonTimerRun = false;

            // если это одладка, то не печатаем на торцевой принтер
            if (Central.DebugMode)
            {
                AutoPrint.IsChecked = false;
                LogEnable.IsChecked = true;
            }

            var pathInfo = new FileInfo(Assembly.GetExecutingAssembly().Location);
            string printFile = $"{pathInfo.Directory}\\printLabel.exe";
            if (!File.Exists(printFile))
            {
                PrintAlternative.Visibility = Visibility.Hidden;
                PrintAlternative.IsChecked = false;
            }
            else
            {
                PrintAlternative.Visibility = Visibility.Visible;
                PrintAlternative.IsChecked = true;
            }


        }

        /// <summary>
        /// инициализация формы, элементы тулбара 
        /// </summary>
        public void FormInit()
        {
            //инициализация формы, элементы тулбара
            {
                Form = new FormHelper();

                //колонки формы
                var fields = new List<FormHelperField>()
                {
                };
                Form.SetFields(fields);

                //после установки значений
                Form.AfterSet = (Dictionary<string, string> v) =>
                {
                    //фокус на кнопку обновления
                    RefreshButton.Focus();
                };

                double nScale = 1.5;
                RollsGrid.LayoutTransform = new ScaleTransform(nScale, nScale);
                SettingPanel.LayoutTransform = new ScaleTransform(nScale, nScale);
                Toolbar.LayoutTransform = new ScaleTransform(nScale, nScale);
                GroupBoxOut.LayoutTransform = new ScaleTransform(nScale, nScale);
                GroupBoxLog.LayoutTransform = new ScaleTransform(nScale, nScale);
                GroupBoxSetup.LayoutTransform = new ScaleTransform(nScale, nScale);
            }

        }

        /// <summary>
        /// Инициализация компонентов
        /// </summary>
        public void Init()
        {
            bool resume = false;
            IsQueueLoading = false;
            IsRollsQueueLoading = false;
            SetFastTimer(1);
            SetTimerFire(10);
            SetSlowTimer(RefreshTime);
            CurSecund = 0;

        }

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            // Остановка таймеров
            SlowTimer?.Stop();
            TimerFire?.Stop();
            FastTimer?.Stop();
            RollsGrid.Destruct();

            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Production",
                ReceiverName = "",
                SenderName = "PaperMakingSlitter2Prs",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
        }


        public void SetCurrentTime()
        {
            CurrentTime.Text = $"{(DateTime.Now.Hour):00}:{DateTime.Now.Minute:00}";
        }

        #region Fire Alarm
        /// <summary>
        /// установка таймера проверки пожара
        /// </summary>
        public void SetTimerFire(int autoUpdateFireStatusInterval)
        {
            TimerFire = new DispatcherTimer();
            TimerFire.Tick += new EventHandler(FireAlarmCheck);
            TimerFire.Interval = new TimeSpan(0, 0, autoUpdateFireStatusInterval);
            TimerFire.Start();

            {
                var row = new Dictionary<string, string>();
                row.CheckAdd("TIMEOUT", autoUpdateFireStatusInterval.ToString());
                row.CheckAdd("DESCRIPTION", "");
                Central.Stat.TimerAdd("PaperMakingMachineOperator_SetTimerFire", row);
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

                    FireStatus.Visibility = Visibility.Visible;
                    FireStatus.Text = $"Пожар! {CurrentPlaceName}";
                    FireAlarmButton.Style = (System.Windows.Style)FireAlarmButton.TryFindResource("ButtonFireActive");
                    FireInPlace = CurrentPlaceName;

                    FireAlarmImage.Visibility = Visibility.Visible;

                    UpdateFireStatus(CurrentPlaceName);

                }
            }
            else if (FireInPlace == CurrentPlaceName)
            {
                FireInPlace = null;
                FireStatus.Visibility = Visibility.Hidden;
                FireAlarmButton.Style = (System.Windows.Style)FireAlarmButton.TryFindResource("ButtonFire");

                FireAlarmImage.Visibility = Visibility.Hidden;

                UpdateFireStatus();

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
                if (MachineId == 716)
                    p.Add("FIRE_NAME", "FIRE_BDM1");
                else
                    p.Add("FIRE_NAME", "FIRE_BDM2");

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
            if (MachineId == 716)
                p.Add("FIRE_NAME", "FIRE_BDM1");
            else
                p.Add("FIRE_NAME", "FIRE_BDM2");

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
                        //FireAlarmImage.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        FireStatus.Visibility = Visibility.Hidden;
                        //FireAlarmImage.Visibility = Visibility.Hidden;
                    }
                }
            }
        }

        #endregion

        #region Menu_Items

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            SetPrintSettings();
        }

        /// <summary>
        /// Установка настроек для принтера
        /// </summary>
        public void SetPrintSettings()
        {
            LabelReport2.SetPrintingProfile();
        }

        /// <summary>
        ///  информация
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InfoMenu_Click(object sender, RoutedEventArgs e)
        {
            var t = "Отладочная информация";
            var m = Central.MakeInfoString();
            var i = new ErrorTouch();
            i.Show(t, m);
        }

        private void BurgerMenuButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }

        private void RestartMenu_Click(object sender, RoutedEventArgs e)
        {
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Main",
                ReceiverName = "MainWindow",
                SenderName = "Navigator",
                Action = "Restart",
                Message = "",
            });

        }

        private void ExitMenu_Click(object sender, RoutedEventArgs e)
        {
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Main",
                ReceiverName = "MainWindow",
                SenderName = "Navigator",
                Action = "Exit",
                Message = "",
            });
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            Central.ShowHelp(DocumentationUrl);
        }
        #endregion


        /// <summary>
        /// Таймер частого обновления (1 секунда)
        /// </summary>
        public void SetFastTimer(int autoUpdateInterval)
        {
            FastTimer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, autoUpdateInterval)
            };

            FastTimer.Tick += (s, e) =>
            {
                RefreshButtonUpdate();
            };

            FastTimer.Start();
        }

        /// <summary>
        /// Таймер медленного обновления
        /// </summary>
        public async void SetSlowTimer(int autoUpdateInterval)
        {
            // Обновляться должно в начале каждой минуты
            int secondsBeforeFirstUpdate = 60 - DateTime.Now.Second + 5;
            Task.Delay(secondsBeforeFirstUpdate * 1000);

            SlowTimer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, autoUpdateInterval)
            };

            SlowTimer.Tick += (s, e) =>
            {
                SetCurrentTime();
            };

            SlowTimer.Start();
        }

        /// <summary>
        /// обновляем время на кнопке до обновления информации
        /// </summary>
        private void RefreshButtonUpdate()
        {
            if (CurSecund >= RefreshTime)
            {
                CurSecund = 0;
                UpdateData();
                LogMsg($"Обновил данные по таймеру {RefreshTime} сек., текущее ПЗ [{CurIdPz}]");
            }
            CurSecund = CurSecund + 1;
            int secondsBeforeFirstUpdate = RefreshTime - CurSecund;
            RefreshButton.Content = $"Обновить {secondsBeforeFirstUpdate}";
        }

        /// <summary>
        ///  нажали кнопку обновить
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            CurSecund = 0;
            UpdateData();
            FastTimer.Start();
            LogMsg($"Обновил данные и запустил таймер {RefreshTime} сек. вручную.");
        }

        /// <summary>
        /// Обновление всей информации
        /// </summary>
        private async void UpdateData()
        {
            RefreshButton.IsEnabled = false;
            TaskLoadItems();
            RollsGrid.LoadItems();
            RefreshButton.IsEnabled = true;
        }

        private void FireAlarmImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            FireInPlace = null;
            FireStatus.Visibility = Visibility.Hidden;
            FireAlarmButton.Style = (System.Windows.Style)FireAlarmButton.TryFindResource("ButtonFire");
            FireAlarmImage.Visibility = Visibility.Hidden;
            UpdateFireStatus();
            TimerFire.Start();
        }

        /// <summary>
        /// Загрузка очереди заданий выбранного станка 
        /// </summary>
        private async void TaskLoadItems()
        {
            //проверка, если уже идёт загрузка очереди
            // if (!IsQueueLoading)
            {
                IsQueueLoading = false;
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID_ST", MachineId.ToString());
                }

                try
                {
                    var q = await LPackClientQuery.DoQueryAsync("ProductionPm", "Monitoring", "TaskMachineList", "ITEMS", p);

                    if (q.Answer.Status == 0)
                    {
                        if (q.Answer.QueryResult != null)
                        {
                            DataSet = q.Answer.QueryResult;
                            var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                            {
                                //                                V0.Text = V1.Text = V2.Text = V3.Text = V4.Text = V5.Text = V6.Text = V7.Text = V8.Text = V9.Text = V10.Text = V11.Text = V12.Text = V13.Text = "";

                                var ds = ListDataSet.Create(result, "ITEMS");
                                var first = ds.Items.FirstOrDefault();

                                IsQueueLoading = true;

                                // заполняем данными  текущего ПЗ
                                CurIdPz = first.CheckGet("ID_PZ").ToInt();

                                V0.Text = first.CheckGet("NUM").ToString();
                                V1.Text = first.CheckGet("NAME").ToString();
                                V2.Text = first.CheckGet("RO").ToInt().ToString();
                                V3.Text = first.CheckGet("DIAMETER").ToInt().ToString();
                                V4.Text = first.CheckGet("B1").ToInt().ToString();
                                V5.Text = first.CheckGet("B2").ToInt().ToString();
                                V6.Text = first.CheckGet("B3").ToInt().ToString();
                                V7.Text = first.CheckGet("BALANCE1").ToString();
                                V8.Text = first.CheckGet("BALANCE2").ToString();
                                V9.Text = first.CheckGet("BALANCE3").ToString();
                                V10.Text = first.CheckGet("SUM_KOL").ToDouble().ToString();
                                V11.Text = first.CheckGet("BDM_SPEED").ToInt().ToString();
                                V12.Text = first.CheckGet("CDTTM").ToString();
                                V13.Text = first.CheckGet("NOTE").ToString();

                                // раскрашиваем
                                {
                                    if (first.CheckGet("GLUED_FLAG").ToInt() == 1)
                                    {
                                        V1.Foreground = "#ddd7ac".ToBrush();    //HColor.RedFG.ToBrush();
                                    }
                                    else if (first.CheckGet("NAME").IndexOf('0') == 1) // Б0 или К0
                                    {
                                        V1.Background = "#7fff7f".ToBrush();
                                    }

                                    if (first.CheckGet("SALE_FLAG1").ToInt() == 1)
                                    {
                                        V4.Background = "#7fff7f".ToBrush();
                                        V7.Background = "#7fff7f".ToBrush();
                                    }

                                    if (first.CheckGet("SALE_FLAG2").ToInt() == 1)
                                    {
                                        V5.Background = "#7fff7f".ToBrush();
                                        V8.Background = "#7fff7f".ToBrush();
                                    }

                                    if (first.CheckGet("SALE_FLAG3").ToInt() == 1)
                                    {
                                        V6.Background = "#7fff7f".ToBrush();
                                        V9.Background = "#7fff7f".ToBrush();
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }
        }

        /// <summary>
        /// инициализация грида RollsGrid
        /// </summary>
        public void RollsGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="№",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 1,
                        Visible = false,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Рулон",
                        Path = "ROLL",
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 8,

                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус",
                        Path="ID_STATUS_STR",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 24,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => GetColorRolls("ID_STATUS_STR", row)
                            },
                        },

                    },
                    new DataGridHelperColumn
                    {
                        Header="Длина",
                        Path="ROLL_LENGTH",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вес",
                        Path="ROLL_WEIGHT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,

                    },
                    new DataGridHelperColumn
                    {
                        Header="Диаметр",
                        Path="ROLL_DIAMETER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,

                    },
                    new DataGridHelperColumn
                    {
                        Header="Формат",
                        Path="ROLL_FORMAT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Марка",
                        Path = "NAME",
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 10,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Создан",
                        Path = "DT_TM_CREATED_SHOW",
                        ColumnType = ColumnTypeRef.DateTime,
                        Width2 = 9,
                        Visible = false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="вн. № рулона",
                        Path="ROLL_NUM",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД тамбура",
                        Path="BDRC_ID",
                        Description="bdrc_id",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД рулона",
                        Path="IDP_ROLL",
                        Description="idp_roll",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ тамбура",
                        Path="NUM_TAMBOUR",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ рулона",
                        Path="NUM_ROLL_TMBR",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД изделия",
                        Path="ID2",
                        Description="id2",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 10,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Дата/Время",
                        Path = "DT_TM_CREATED",
                        ColumnType = ColumnTypeRef.DateTime,
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ съема",
                        Path="SEGMENT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 6,
                        Visible=true,
                    },
                        new DataGridHelperColumn
                    {
                        Header="Ид статуса",
                        Path="ID_STATUS",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 6,
                        Visible=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="IDR",
                        Path="IDR",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 2,
                        Visible=false,
                    },
                };

                RollsGrid.SetColumns(columns);
                RollsGrid.SetPrimaryKey("_ROWNUMBER");
                RollsGrid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
                RollsGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                RollsGrid.AutoUpdateInterval = 0;
                RollsGrid.EnableSortingGrid = false;

                // Раскраска строк
                RollsGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                };

                // при выборе строки
                RollsGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem != null)
                    {
                        SelectedRollsItem = selectedItem;
                        RollsGridOnChange();
                    }
                };

                //данные грида
                RollsGrid.OnLoadItems = RollsGridLoadItems;
                RollsGrid.Init();

                RollsGrid.Run();
            }
        }

        /// <summary>
        /// Загрузка списка тамбуров и рулонов
        /// </summary>
        private async void RollsGridLoadItems()
        {
            //проверка, если уже идёт загрузка очереди
            if (!IsRollsQueueLoading)
            {
                IsRollsQueueLoading = true;
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID_ST", MachineId.ToString());
                }

                if (HideComplete.IsChecked == false)
                {
                    p.CheckAdd("FILTR", "0");
                }
                else
                {
                    p.CheckAdd("FILTR", "1");
                }

                if (NumberDay.Text.IsNullOrEmpty())
                {
                    p.Add("PERIOD", "1");
                }
                else
                {
                    p.Add("PERIOD", NumberDay.Text.ToString());
                }

                try
                {
                    var q = await LPackClientQuery.DoQueryAsync("ProductionPm", "PmSlitter", "RollsPzSlitterList", "ITEMS", p);

                    if (q.Answer.Status == 0)
                    {
                        if (q.Answer.QueryResult != null)
                        {
                            RollsGridDataSet = q.Answer.QueryResult;
                            var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                            {
                                /// Загрузка датасета в грид 
                                RollsGrid.UpdateItems(RollsGridDataSet);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {

                }
                IsRollsQueueLoading = false;
            }
        }

        /// <summary>
        /// Возвращает цвет  ячейки для списка тамбуров и рулонов
        /// </summary>
        public static object GetColorRolls(string fieldName, Dictionary<string, string> row)
        {
            var result = DependencyProperty.UnsetValue;
            var color = "";

            if (fieldName == "ID_STATUS_STR")
            {
                // съем снят с БДМ
                if ((row.CheckGet("ID_STATUS").ToInt() == 0) && (row.CheckGet("IDP_ROLL").ToInt() == 0))
                {
                    color = $"#A6CAF0";
                } // образец съема в лаборатории
                else if ((row.CheckGet("ID_STATUS").ToInt() == 2) && (row.CheckGet("IDP_ROLL").ToInt() == 0))
                {
                    color = $"#FFFF00";
                } // съем отправлен в брак
                else if ((row.CheckGet("ID_STATUS").ToInt() == 3) && (row.CheckGet("IDP_ROLL").ToInt() == 0))
                {
                    color = $"#808000";
                } // съем установлен на ПРС
                else if ((row.CheckGet("ID_STATUS").ToInt() == 4) && (row.CheckGet("IDP_ROLL").ToInt() == 0))
                {
                    color = $"#00FF00";
                } // съем смотан на ПРС
                else if (row.CheckGet("ID_STATUS").ToInt() == 8)
                {
                    color = $"";
                } // съем отправлен в брак'
                else if (row.CheckGet("ID_STATUS").ToInt() == 9)
                {
                    color = $"";
                } // рулон снят с ПРС, марки/веса нет
                else if ((row.CheckGet("ID_STATUS").ToInt() == 14)
                             && ((row.CheckGet("ROLL_WEIGHT").ToInt() == 0) || (row.CheckGet("ID2").ToInt() == 0))
                            && ((row.CheckGet("IDP_ROLL").ToInt() != 0)))
                {
                    color = $"#FF0000";
                } // рулон отложен лабораторией
                else if ((row.CheckGet("ID_STATUS").ToInt() == 15) && ((row.CheckGet("IDP_ROLL").ToInt() != 0)))
                {
                    color = $"#800080";
                } // рулон оприходован на склад
                else if (row.CheckGet("ID_STATUS").ToInt() == 16)
                {
                    color = $"";
                } // рулон отправлен в брак
                else if ((row.CheckGet("ID_STATUS").ToInt() == 18) && ((row.CheckGet("IDP_ROLL").ToInt() != 0)))
                {
                    color = $"#808000";
                } // рулон перемаркирован
                
                if ((row.CheckGet("ID_STATUS").ToInt() == 16) && (row.CheckGet("IDR").ToInt() > 0)) 
                {
                    color = $"#C0C0C0";
                }

            }

            if (!color.IsNullOrEmpty())
            {
                result = color.ToBrush();
            }

            return result;
        }

        /// <summary>
        /// установка количества дней для выборки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NumberDay_IsKeyboardFocusedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            RollsGrid.LoadItems();
        }

        /// <summary>
        ///  фильтр показать все рулоны
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowAllComplete_Click(object sender, RoutedEventArgs e)
        {
            RollsGrid.LoadItems();
        }


        /// <summary>
        ///  поставить на ПРС
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPrsButton_Click(object sender, RoutedEventArgs e)
        {
            var dw = new DialogWindow($"Вы действительно хотите установить тамбур на ПРС?", "Установка тамбура", $"Подтверждение установки тамбура [{SelectedRollsItem.CheckGet("NUM_TAMBOUR").ToInt()}].", DialogWindowButtons.NoYes);
            if (dw.ShowDialog() == true)
            {
                OnTamburToPrs(1);
                RollsGrid.LoadItems();
            }
        }

        /// <summary>
        ///  снять с ПРС
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OffPrsButton_Click(object sender, RoutedEventArgs e)
        {
            var dw = new DialogWindow($"Вы действительно хотите снять тамбур с ПРС?", "Снятие тамбура", $"Подтверждение снятия тамбура [{SelectedRollsItem.CheckGet("NUM_TAMBOUR").ToInt()}].", DialogWindowButtons.NoYes);
            if (dw.ShowDialog() == true)
            {
                OnTamburToPrs(0);
                RollsGrid.LoadItems();
            }
        }

        /// <summary>
        ///  создать рулон
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewRollButton_Click(object sender, RoutedEventArgs e)
        {
            var dw = new DialogWindow($"Вы действительно хотите создать рулоны?", "Создание рулонов", $"Подтверждение создания рулонов для тамбура [{SelectedRollsItem.CheckGet("NUM_TAMBOUR").ToInt()}].", DialogWindowButtons.NoYes);
            if (dw.ShowDialog() == true)
            {
                if (CountRoll.Text.ToInt() <= 0)
                {
                    LogMsg($"Ошибка! Количество рулонов должно быть больше 0 ");
                    const string msg = "Ошибка! Количество рулонов должно быть больше 0.";
                    var d = new DialogWindow($"{msg}", "Проверка данных");
                    d.ShowDialog();
                    return;
                }

                // получаем текущее ПЗ для станка 1716
                GetCurrentPz();

                // создаем указанное количество рулонов для текущего тамбура
                for (int i = 0; i < CountRoll.Text.ToInt(); i++)
                {
                    AddRollToTambour();
                    NumberRoll.Text = (NumberRoll.Text.ToInt() + 1).ToString();
                }

                RollsGrid.LoadItems();
            }
        }

        /// <summary>
        ///  получить вес рулона
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WeightButton_Click(object sender, RoutedEventArgs e)
        {
            GetWeigh();
        }

        /// <summary>
        /// удалить рулон
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RollDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var dw = new DialogWindow($"Вы действительно хотите удалить рулон?", "Удаление рулона", $"Подтверждение удаления рулона [{SelectedRollsItem.CheckGet("ROLL")}].", DialogWindowButtons.NoYes);
            if (dw.ShowDialog() == true)
            {
                RollDeletePrs();
                RollsGrid.LoadItems();
                FastTimer.Start();
            }

        }

        /// <summary>
        ///  показать журнал работы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LogButton_Click(object sender, RoutedEventArgs e)
        {
            if (LogViewFlag == true)
            {
                LogPanel.Visibility = Visibility.Hidden;
                LogViewFlag = false;
                LogButton.Content = "Настройки";
            }
            else
            {
                LogPanel.Visibility = Visibility.Visible;
                LogViewFlag = true;
                LogButton.Content = "Скрыть настройки";
            }

        }


        /// <summary>
        ///  подготавливаем данные для печати на торцевом принтере
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrepareDataInsidePrinterButton_Click(object sender, RoutedEventArgs e)
        {
            if (!SelectedRollsItem.CheckGet("IDP_ROLL").IsNullOrEmpty())
            {
                // проверяем был ли рулон перемаркирован
                GetRemarkRoll();
                // получаем данные для печати на торцевом принтере
                PrepareDataForInsidePrinter();
            }
        }

        /// <summary>
        ///  отправляем данные для печати на торцевом принтере
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartToInsidePrinterButton_Click(object sender, RoutedEventArgs e)
        {
            StartToInsidePrinter();
        }

        /// <summary>
        /// пишем логи 
        /// </summary>
        /// <param name="text"></param>
        private void LogMsg(string text)
        {
            if (LogEnable.IsChecked == true)
            {
                var t = DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss");
                var s = "";
                s = $"{t} {text}";
                Log.Text = Log.Text.Append(s, true);
                Log.ScrollToEnd();
            }
        }

        /// <summary>
        ///  получаем последний номер рулона для текущего тамбура
        /// </summary>
        /// <param name="bdrcId"></param>
        private void GetNumRoll(int bdrcId)
        {
            NumRollTmbr = -1;
            var p = new Dictionary<string, string>();
            p.CheckAdd("BDRC_ID", bdrcId.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "PmSlitter");
            q.Request.SetParam("Action", "GetRollLast");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            //await Task.Run(() =>
            //{
            q.DoQuery();
            //            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds.Items.Count > 0)
                        {
                            NumRollTmbr = ds.Items.First().CheckGet("NUM_ROLL_TMBR").ToInt();
                            // NumRollTmbr = ds.Items[0].CheckGet("NUM_ROLL_TMBR").ToInt();
                        }
                    }
                }
            }
        }

        /// <summary>
        ///  получаем все данные по номеру рулона
        /// </summary>
        /// <param name="bdrcId"></param>
        private void GetDataRoll(int idpRoll)
        {
            NumRollTmbr = -1;
            var p = new Dictionary<string, string>();
            p.CheckAdd("IDP_ROLL", idpRoll.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "PmSlitter");
            q.Request.SetParam("Action", "GetRollAllData");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds.Items.Count > 0)
                        {
                            var data = ds.Items.First();
                            LenRoll.Text = data.CheckGet("LENGTH_PRS").ToInt().ToString();
                            FormatRoll.Text = data.CheckGet("ROLL_FORMAT").ToInt().ToString();
                            DiameterRoll.Text = data.CheckGet("ROLL_DIAMETER").ToInt().ToString();
                            SegmentRoll.Text = data.CheckGet("SEGMENT").ToInt().ToString();
                            WeightNettoRoll.Text = data.CheckGet("ROLL_WEIGHT").ToInt().ToString();
                            if ((!FormatRoll.Text.IsNullOrEmpty() && (WeightNettoRoll.Text != "0")))
                            {
                                WeightRoll.Text = (WeightNettoRoll.Text.ToInt() + Math.Round((FormatRoll.Text.ToInt() * 2.64 / 1000)).ToInt()).ToString();
                            }
                            else
                            {
                                WeightRoll.Text = "";
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// очистка лог файла
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            Log.Text = "";
        }

        /// <summary>
        /// сохранить лог в файл
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveLogButton_Click(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        ///  получаем вес рулона
        /// </summary>
        /// <returns></returns>
        private bool GetWeigh()
        {
            var result = true;
            WeightRoll.Text = "";
            WeightNettoRoll.Text = "";

            var content = SendHttpRequest(urlWeight);
            if (!content.IsNullOrEmpty())
            {
                WeightRoll.Text = content;
                WeightNettoRoll.Text = (WeightRoll.Text.ToInt() - Math.Round((FormatRoll.Text.ToInt() * 2.64 / 1000)).ToInt()).ToString();
            }
            LogMsg($"Получен вес брутто =[{WeightRoll.Text}], расчетный вес нетто =[{WeightNettoRoll.Text}] \nдля рулона IDP_ROLL=[{SelectedRollsItem.CheckGet("IDP_ROLL").ToInt()}], рулон №=[{SelectedRollsItem.CheckGet("NUM_TAMBOUR").ToInt()}.{SelectedRollsItem.CheckGet("NUM_ROLL_TMBR").ToInt()}]");
            return result;
        }

        /// </summary>
        /// <param name="machine"></param>
        /// <param name="reelNumber"></param>
        /// <param name="state"></param>
        private void DoSendCommandHttp(string ipLaurent, int reelNumber, int state)
        {
            LogMsg($"DoSendCommandHTTP. Laurent IP=[{ipLaurent}], ReleNumber=[{reelNumber}], State=[{state}]");
            // http://192.168.132.224/cmd.cgi?cmd=REL,4,1

            var url = $"http://{ipLaurent}/cmd.cgi?psw=Laurent&cmd=REL,{reelNumber},{state}";

            bool repeat = true;
            int step = 1;
            int maxAttempts = 3;
            while (repeat)
            {
                var content = SendHttpRequest(url);
                var checkResult = false;

                if (!content.IsNullOrEmpty())
                {
                    checkResult = CheckRelayState(ipLaurent, reelNumber);
                    if (checkResult)
                    {
                        repeat = false;
                    }
                }

                step++;
                if (step >= maxAttempts)
                {
                    repeat = false;
                }
            }

        }

        /// <summary>
        ///  возвращает статус реле (включено/выключено)
        /// </summary>
        /// <param name="machine"></param>
        /// <param name="reelNumber"></param>
        /// <returns></returns>
        private bool CheckRelayState(string ipLaurent, int reelNumber)
        {

            bool result = true;

            return result;
        }

        /// <summary>
        /// запрос к плате по HTTP
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private string SendHttpRequest(string url)
        {
            var result = "";
            var report = "";
            var profiler = new Profiler("SendHttpRequest");

            bool repeat = true;
            int step = 1;
            int maxAttempts = 2;
            while (repeat)
            {
                {
                    try
                    {
                        {
                            if (ControlPlcConnectionClient == null)
                            {
                                ControlPlcConnectionClient = new WebClient();
                                NetworkCredential myCreds = new NetworkCredential("admin", "Laurent");
                                ControlPlcConnectionClient.Credentials = myCreds;
                            }
                        }

                        {
                            if (ControlPlcConnectionClient != null)
                            {
                                result = ControlPlcConnectionClient.DownloadString(url);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (ControlPlcConnectionClient != null)
                        {
                            ControlPlcConnectionClient.Dispose();
                        }
                        ControlPlcConnectionClient = null;
                    }
                }

                if (!result.IsNullOrEmpty())
                {
                    repeat = false;
                }
                else
                {
                    report = $"{report} ({step})result_empty";
                }

                step++;
                if (step > maxAttempts)
                {
                    repeat = false;
                }
            }

            if (ControlPlcConnectionClient != null)
            {
                ControlPlcConnectionClient.Dispose();
            }
            ControlPlcConnectionClient = null;
            return result;
        }

        // вручную меняем вес брутто
        private void WeightRoll_TextChanged(object sender, TextChangedEventArgs e)
        {
            if ((!FormatRoll.Text.IsNullOrEmpty() && (!WeightRoll.Text.IsNullOrEmpty())))
            {
                WeightNettoRoll.Text = (WeightRoll.Text.ToInt() - Math.Round((FormatRoll.Text.ToInt() * 2.64 / 1000)).ToInt()).ToString();
            }
            else
            {
                WeightNettoRoll.Text = "";
            }
        }

        /// <summary>
        ///  получаем данные для печати на торцевом принтере
        /// </summary>
        private async void PrepareDataForInsidePrinter()
        {
            Kol = "";
            Name = "";
            Num = "";

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("IDP_ROLL", IdpRoll.ToString());
            }

            try
            {
                var q = await LPackClientQuery.DoQueryAsync("ProductionPm", "PmSlitter", "GetRollDataForInsidePrint", "ITEMS", p);

                if (q.Answer.Status == 0)
                {
                    if (q.Answer.QueryResult != null)
                    {
                        DataSet = q.Answer.QueryResult;
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        {
                            // получаем данные для вывода на торцевой принтер
                            var ds = ListDataSet.Create(result, "ITEMS");
                            Kol = ds.Items.FirstOrDefault().CheckGet("KOL").ToInt().ToString();
                            Name = ds.Items.FirstOrDefault().CheckGet("NAME").ToString();
                            Num = ds.Items.FirstOrDefault().CheckGet("NUM").ToString();
                            LogMsg($"Получены данные для печати на торцевом принтере, для рулона idp_roll=[{IdpRoll.ToString()}]\n Kol=[{Kol}], Namel=[{Name}], Num=[{Num}]");
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }

            if (!Kol.IsNullOrEmpty() && !Name.IsNullOrEmpty() && !Num.IsNullOrEmpty())
            {
                // формируем пакет данных для печати на торцевом принтере
                SendDataToInsidePrinter();
            }

        }

        /// <summary>
        ///  формируем пакет данных для печати на торцевом принтере
        /// </summary>
        private void SendDataToInsidePrinter()
        {
            string xml = @$"<?xml version=""1.0"" encoding=""UTF-8""?>
            <request>
            <setVariable name=""VES"">{Kol}кг</setVariable>
            <setVariable name=""SORT"">{Name}</setVariable>
            <setVariable name=""RULON"">{Num}</setVariable>
            </request>
";
            if (!Central.DebugMode)
            {
                //// создаем TCP подключение к торцевому принтеру
                IPAddress ipAddr = IPAddress.Parse(TorquePrinterIp);
                IPEndPoint endPoint = new IPEndPoint(ipAddr, TorquePrinterPort);
                TcpClient newClient = new TcpClient();

                try
                {
                    // Для создания соединения с сервером надо вызвать connect()
                    newClient.Connect(ipAddr, TorquePrinterPort);
                    LogMsg("Cоздал соединение с сервером для подключения к торцевому принтеру");
                    byte[] sendBytes = Encoding.UTF8.GetBytes(xml);
                    NetworkStream tcpStream = newClient.GetStream();
                    tcpStream.Write(sendBytes, 0, sendBytes.Length);
                    System.Threading.Thread.Sleep(1000); // Сон на 1 секунды
                }
                catch (SocketException ex)
                {
                    LogMsg("Exception: " + ex.ToString());
                }

                // закрываем подключение
                newClient.Close();
            }
            LogMsg($"{xml}\n");
            //            LogMsg($"{xml}\nдля рулона IDP_ROLL=[{IdpRoll}], рулон №=[{SelectedRollsItem.CheckGet("NUM_TAMBOUR").ToInt()}.{SelectedRollsItem.CheckGet("NUM_ROLL_TMBR").ToInt()}]");
        }

        /// <summary>
        ///  отправляем данные для печати на торцевом принтере
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartToInsidePrinter()
        {
            if (!Central.DebugMode)
            {
                // включаем реле №1 на 2 сек
                DoSendCommandHttp(IpLaurent, 1, 1);
                LogMsg("Включил реле привода торцевого принтера");
                System.Threading.Thread.Sleep(2000); // Сон на 2 секунды
                // выключаем реле №1 на 2 сек
                DoSendCommandHttp(IpLaurent, 1, 0);
                LogMsg("Выключил реле привода торцевого принтера");
            }
        }

        /// <summary>
        ///  поставить/снять тамбур на ПРС
        /// </summary>
        private void OnTamburToPrs(int vid)
        {
            var p = new Dictionary<string, string>();
            p.CheckAdd("ID_ST", MachineId.ToString());
            p.CheckAdd("BDRC_ID", SelectedRollsItem.CheckGet("BDRC_ID").ToInt().ToString());
            p.CheckAdd("VID", vid.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "PmSlitter");
            q.Request.SetParam("Action", "SetTambourToPrs");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status != 0)
            {
                q.ProcessError();
                LogMsg("Ошибка при постановки/снятия тамбура: " + q.Answer.Data);
            }

        }

        /// <summary>
        ///  удалить рулон на ПРС
        /// </summary>
        private void RollDeletePrs()
        {
            var p = new Dictionary<string, string>();
            p.CheckAdd("IDP_ROLL", SelectedRollsItem.CheckGet("IDP_ROLL").ToInt().ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "PmSlitter");
            q.Request.SetParam("Action", "RollDelete");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status != 0)
            {
                q.ProcessError();
                LogMsg("Ошибка при удалении рулона: " + q.Answer.Data);
            }

        }

        /// <summary>
        ///  получение текущего ПЗ для БДМ2
        /// </summary>
        private void GetCurrentPz()
        {
            CurIdPz = 0;
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "Slitter");
            q.Request.SetParam("Action", "ListProductionTaskCurrent");

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            //await Task.Run(() =>
            {
                q.DoQuery();
            }
            //);

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                {
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds.Items.Count > 0)
                        {
                            CurIdPz = ds.Items.FirstOrDefault().CheckGet("ID_PZ").ToInt();
                            LogMsg($"Получено текущее ПЗ [{CurIdPz}]");
                        }
                    }
                    else
                    {
                        q.ProcessError();
                        LogMsg("Нет данных при получении текущего ПЗ: " + q.Answer.Data);
                    }
                }
            }
            else
            {
                q.ProcessError();
                LogMsg("Ошибка при получении текущего ПЗ: " + q.Answer.Data);
            }
        }

        /// <summary>
        ///  получение последнего выполненого ПЗ для БДМ2
        /// </summary>
        private void GetLastClosedPz()
        {
            CurIdPz = 0;
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "Slitter");
            q.Request.SetParam("Action", "ListProductionTaskClosed");

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                {
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds.Items.Count > 0)
                        {
                            CurIdPz = ds.Items.FirstOrDefault().CheckGet("ID_PZ").ToInt();
                            LogMsg($"Получено предыдущее выполненое ПЗ [{CurIdPz}]");
                        }
                    }
                    else
                    {
                        q.ProcessError();
                        LogMsg("Нет данных при получении последнего выполненого ПЗ: " + q.Answer.Data);
                    }
                }
            }
            else
            {
                q.ProcessError();
                LogMsg("Ошибка при получении последнего выполненого ПЗ: " + q.Answer.Data);
            }
        }

        /// <summary>
        ///  сохранение ПЗ для БДМ2
        /// </summary>
        private void SaveCurrentPz()
        {
            var p = new Dictionary<string, string>();
            p.CheckAdd("ID_PZ", CurIdPz.ToInt().ToString());
            p.CheckAdd("IDP_ROLL", SelectedRollsItem.CheckGet("IDP_ROLL").ToInt().ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "PmSlitter");
            q.Request.SetParam("Action", "SaveBdmPrsIdPz");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = 1;

            q.DoQuery();

            if (q.Answer.Status != 0)
            {
                q.ProcessError();
                LogMsg("Ошибка при обновлении текущего ПЗ в bdm_prs: " + q.Answer.Data);
            }
        }

        /// <summary>
        ///  обновляем данные рулона (вес, длина, диаметр и т.д.)
        /// </summary>
        private void SetRollData()
        {
            var p = new Dictionary<string, string>();
            p.CheckAdd("IDP_ROLL", SelectedRollsItem.CheckGet("IDP_ROLL").ToInt().ToString());
            p.CheckAdd("ROLL_WEIGHT", WeightNettoRoll.Text);
            p.CheckAdd("ROLL_DIAMETER", DiameterRoll.Text);
            p.CheckAdd("ROLL_FORMAT", FormatRoll.Text);
            p.CheckAdd("LENGTH_PRS", LenRoll.Text);
            p.CheckAdd("SEGMENT_ROLL", SegmentRoll.Text);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "PmSlitter");
            q.Request.SetParam("Action", "SaveBdmPrsAll");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = 1;

            q.DoQuery();

            if (q.Answer.Status != 0)
            {
                q.ProcessError();
                LogMsg("Ошибка при обновлении данных рулона [{SelectedRollsItem.CheckGet(\"IDP_ROLL\").ToInt()}], " + q.Answer.Data);
            }
        }

        /// <summary>
        ///  добавляем новый рулон для текущего тамбура
        /// </summary>
        private void AddRollToTambour()
        {
            if (CurIdPz != 0)
            {
                var p = new Dictionary<string, string>();
                p.CheckAdd("ID_PZ", CurIdPz.ToString());
                p.CheckAdd("BDRC_ID", SelectedRollsItem.CheckGet("BDRC_ID").ToInt().ToString());
                p.CheckAdd("NUM_ROLL_TMBR", NumberRoll.Text);

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "ProductionPm");
                q.Request.SetParam("Object", "PmSlitter");
                q.Request.SetParam("Action", "CreateRoll");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = 1;

                q.DoQuery();

                if (q.Answer.Status != 0)
                {
                    q.ProcessError();
                    LogMsg("Ошибка при создании нового рулона: " + q.Answer.Data);
                }
                else
                {
                    LogMsg($"Создан новый рулон [{NumberRoll.Text}], для ПЗ [{CurIdPz}],  bdrc_id [{SelectedRollsItem.CheckGet("BDRC_ID").ToInt()}]");
                }
            }
            else
            {
                LogMsg("Не получено текущее ПЗ.");
            }

        }

        /// <summary>
        ///  установить данные (вес, диаметр, длина и т.д.) для текущего рулона
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RollSetButton_Click(object sender, RoutedEventArgs e)
        {
            // блок проверок
            if (DiameterRoll.Text.IsNullOrEmpty())
            {
                LogMsg($"Ошибка! Диаметр рулона должен быть заполнен, IDP_ROLL={SelectedRollsItem.CheckGet("IDP_ROLL").ToInt()} ");
                const string msg = "Ошибка! Диаметр рулона должен быть заполнен.";
                var d = new DialogWindow($"{msg}", "Проверка данных");
                d.ShowDialog();
                return;
            }
            else
            {
                if (DiameterRoll.Text.ToInt() > 1450)
                {
                    LogMsg($"Ошибка! Диаметр рулона не может быть больше 1450 мм., IDP_ROLL={SelectedRollsItem.CheckGet("IDP_ROLL").ToInt()} ");
                    DiameterRoll.Text = "1450";
                    const string msg = "Ошибка! Диаметр рулона не может быть больше 1450 мм.";
                    var d = new DialogWindow($"{msg}", "Проверка данных");
                    d.ShowDialog();
                    return;
                }
            }

            FastTimer.Stop();
            RollSetButton.IsEnabled = false;

            if (!WeightNettoRoll.Text.IsNullOrEmpty())
            {
                // если нет активных заданий, тогда выбираем последнее из закрытых
                if (CurIdPz == 0)
                {
                    GetLastClosedPz();
                }

                if (CurIdPz > 0)
                {
                    // обновляем bdm_prs.id_pz
                    SaveCurrentPz();
                }
            }

            // обновляем данные по рулону
            SetRollData();
            RollsGrid.LoadItems();
            FastTimer.Start();
            LogMsg($"Запустил таймер {RefreshTime} сек.");
        }

        /// <summary>
        ///  оприходовать рулон
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RollArrivialButton_Click(object sender, RoutedEventArgs e)
        {
            var resume = true;

            if (ArrivialConfirmation.IsChecked == true)
            {
                var dw = new DialogWindow($"Вы действительно хотите оприходовать рулон?", "Оприходование рулона", $"Подтверждение оприходования рулона [{SelectedRollsItem.CheckGet("ROLL")}].", DialogWindowButtons.NoYes);
                if (dw.ShowDialog() != true)
                {
                    resume = false;
                }
            }

            if (resume)
            {

                buttons[1] = false;
                RollArrivialButton.IsEnabled = false;
                SetSplash(true, "Идет оприходование рулона");
                ButtonTimer.Run();
                ButtonTimerRun = true;

                FastTimer.Stop();
                LogMsg($"остановил таймер {RefreshTime} сек. перед оприходованием.");

                // списываем съем в брак 
                if (SelectedRollsItem.CheckGet("ID_STATUS").ToInt() == 3)
                {
                    OnTamburToPrs(9);
                    return;
                }

                // проверяем, заполнен ли диаметр рулона
                var idpRoll = SelectedRollsItem.CheckGet("IDP_ROLL").ToInt();
                GetDataRoll(idpRoll);

                if (DiameterRoll.Text.IsNullOrEmpty())
                {
                    LogMsg($"Ошибка! Диаметр рулона должен быть заполнен, IDP_ROLL={SelectedRollsItem.CheckGet("IDP_ROLL").ToInt()} ");
                    const string msg = "Ошибка! Диаметр рулона должен быть заполнен.";
                    var d = new DialogWindow($"{msg}", "Проверка данных");
                    d.ShowDialog();
                    return;
                }

                if (SelectedRollsItem.CheckGet("ID2").ToInt() > 0)
                {

                    // оприходуем рулон
                    if (PrintAlternative.IsChecked == true)
                    {
                        ArrivialRollNew();
                    }
                    else
                    {
                        ArrivialRoll();
                    }
                }
            }
        }

        /// <summary>
        ///  оприходуем рулон
        /// </summary>
        private async void ArrivialRoll()
        {
            var idp = SelectedRollsItem.CheckGet("IDP_ROLL").ToInt().ToString();
            var p = new Dictionary<string, string>();
            p.CheckAdd("IDP_ROLL", idp);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "PmSlitter");
            q.Request.SetParam("Action", "ArrivialRoll");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = 1;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var dataSet = ListDataSet.Create(result, "ITEMS");
                    if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                    {
                        if (dataSet.Items.First().CheckGet("ID").ToInt() == 0)
                        {
                            // рулон оприходован успешно
                            string msg = $"Рулон [{idp}] оприходован успешно!{Environment.NewLine}";
                            LogMsg($"{msg}  ПЗ [{CurIdPz}],  bdrc_id [{SelectedRollsItem.CheckGet("BDRC_ID").ToInt()}]");

                            // проверяем, если задание на продажу и марка отличается, перемаркировываем на нужную марку
                            RemarkRoll();
                        }
                    }
                }
            }
            else
            {
                string msg = q.Answer.Error.Message;
                LogMsg($"Ошибка при оприходовании рулона [{SelectedRollsItem.CheckGet("IDP_ROLL").ToInt()}], \nmsg");
            }
        }

        /// <summary>
        ///  проверка на необходимость перемаркировки рулона
        /// </summary>
        private async void RemarkRoll()
        {
            IdpRoll = SelectedRollsItem.CheckGet("IDP_ROLL").ToInt();
            var p = new Dictionary<string, string>();
            p.CheckAdd("IDP_ROLL", IdpRoll.ToString());

            LogMsg($"Идет проверка необходимости перемаркировки рулона, idp_roll=[{IdpRoll}]");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "PmSlitter");
            q.Request.SetParam("Action", "RemarkRoll");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = 1;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var dataSet = ListDataSet.Create(result, "ITEMS");
                    if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                    {
                        var idp_roll_remark = dataSet.Items.First().CheckGet("ID").ToInt();
                        if (idp_roll_remark != 0)
                        {
                            // новый idp_roll рулона после перемаркировки
                            IdpRoll = idp_roll_remark;
                            LogMsg($"Рулон перемаркирован. Новый idp_roll=[{IdpRoll.ToString()}]");
                        }
                    }
                }
                SetRollOrderdates();
            }
            else
            {
                string msg = q.Answer.Error.Message;
                LogMsg($"Ошибка при проверке перемаркировки рулона [{IdpRoll}], \nmsg");
            }
        }

        /// <summary>
        /// SetRollOrderdates
        /// </summary>
        private async void SetRollOrderdates()
        {
            var p = new Dictionary<string, string>();
            p.CheckAdd("IDP_ROLL", IdpRoll.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "PmSlitter");
            q.Request.SetParam("Action", "SetRollOrderdates");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = 1;

            await Task.Run(() => { q.DoQuery(); });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var dataSet = ListDataSet.Create(result, "ITEMS");
                    if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                    {
                        var rez = dataSet.Items.First().CheckGet("ID").ToInt();
                        if (rez == 0)
                        {
                            LogMsg($"Успешное выполнение SetRollOrderdates для рулона [{IdpRoll.ToString()}], \nготовим данные для печати ярлыка.");

                            if (PrintPreviewEnable.IsChecked == false)
                            {
                                // получаем данные для печати ярлыка на лазерном принтере 
                                // и сразу печатаем  2 копии
                                GetDataRollForLabel(2);
                            }
                            else
                            {
                                // просмотр перед печатью
                                var rawMaterialLabelReport = new RawMaterialLabelReport();
                                // это внешний ярлык
                                rawMaterialLabelReport.CurrentLabelType = RawMaterialLabelReport.LabelType.OuterLabel;
                                rawMaterialLabelReport.ShowLabel(IdpRoll.ToString());
                            }

                            // сразу печатаем на торцевом принтере
                            if (AutoPrint.IsChecked == true)
                            {
                                // получаем данные для печати на торцевом принтере
                                PrepareDataForInsidePrinter();
                                // печатаем на торцевом принтере (включаем реле 1 на 2 сек.)
                                StartToInsidePrinter();
                            }

                            CurSecund = 0;
                            UpdateData();
                            FastTimer.Start();
                            LogMsg($"Запустил таймер {RefreshTime} сек. после оприходования");
                        }
                    }
                }
            }
            else   //if (q.Answer.Status == 145)
            {
                string msg = q.Answer.Error.Message;
                LogMsg($"Ошибка SetRollOrderdates  рулон [{IdpRoll.ToString()}], \nmsg");
            }
        }

        /// <summary>
        ///  проверка перед печатью ярлыка, был ли перемаркирован рулон
        /// </summary>
        private void GetRemarkRoll()
        {
            IdpRoll = SelectedRollsItem.CheckGet("IDP_ROLL").ToInt();
            var p = new Dictionary<string, string>();
            p.CheckAdd("IDP_ROLL", IdpRoll.ToString());

            ///  LogMsg($"Проверяем, был ли перемаркирован  рулон, старый idp_roll=[{IdpRoll}]");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "PmSlitter");
            q.Request.SetParam("Action", "GetRemarkRoll");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = 1;

            //await Task.Run(() => { q.DoQuery(); });
            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var dataSet = ListDataSet.Create(result, "ITEMS");
                    if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                    {
                        var idp_roll_remark = dataSet.Items.First().CheckGet("ID").ToInt();
                        if (idp_roll_remark != 0)
                        {
                            // новый idp_roll рулона после перемаркировки
                            IdpRoll = idp_roll_remark;
                            ///    LogMsg($"Рулон был перемаркирован. Новый idp_roll=[{IdpRoll.ToString()}]");
                        }
                    }
                }
            }
            else
            {
                string msg = q.Answer.Error.Message;
                ///      LogMsg($"Ошибка при проверке перемаркировки рулона [{IdpRoll}], \nmsg");
            }
        }

        /// <summary>
        ///  получили фокус
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LenRoll_GotFocus(object sender, RoutedEventArgs e)
        {
            //  RefreshButtonTimeout.Finish();
            FastTimer.Stop();
            LogMsg($"Остановил таймер {RefreshTime} сек.");
        }

        /// <summary>
        ///  получаем все данные рулона для печати ярлыка
        /// </summary>
        private async void GetDataRollForLabel(int cnt)
        {
            var rawMaterialLabelReport = new RawMaterialLabelReport();
            // это внешний ярлык
            rawMaterialLabelReport.CurrentLabelType = RawMaterialLabelReport.LabelType.OuterLabel;
            // копий
            rawMaterialLabelReport.PrintingCopies = cnt;
            rawMaterialLabelReport.PrintLabel(IdpRoll.ToString());
        }

        /// <summary>
        ///  получаем все данные рулона для печати ярлыка
        /// </summary>
        private async void _GetDataRollForLabel()
        {
            var p = new Dictionary<string, string>();
            p.CheckAdd("IDP_ROLL", IdpRoll.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "PmSlitter");
            q.Request.SetParam("Action", "GetDataRollForLabel");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status != 0)
            {
                q.ProcessError();
                LogMsg($"Ошибка при получении данных по рулону [{IdpRoll.ToInt()}] для печати ярлыка, " + q.Answer.Data);
            }
            else
            {

                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds.Items.Count > 0)
                        {
                            var first = ds.Items.First();

                            var name_proizv = first.CheckGet("NAME_PROIZV");
                            var name = first.CheckGet("NAME");
                            var full_name = first.CheckGet("FULL_NAME");
                            var num = first.CheckGet("NUM");
                            var kol_brutto = first.CheckGet("KOL_BRUTTO").ToInt();
                            var kol_netto = first.CheckGet("KOL_NETTO").ToInt();
                            var length_roll = first.CheckGet("LENGTH_ROLL").ToInt();
                            var barcode1 = first.CheckGet("BARCODE1");
                            var barcode2 = first.CheckGet("BARCODE2");
                            var wet_list = first.CheckGet("WET_LIST");
                            var dt_tm = first.CheckGet("DT_TM");
                            var tu = first.CheckGet("TU");
                            var id_pz = first.CheckGet("ID_PZ").ToInt();

                            LogMsg($"Получены данные для печати ярлыка по рулону [{IdpRoll.ToInt()}]:" +
                               $" \n Производитель [{name_proizv}]" +
                               $" \n Бумага [{name}]" +
                               $" \n Название [{full_name}]" +
                               $" \n № рулона [{num}]" +
                               $" \n Вес брутто [{kol_brutto}]" +
                               $" \n Вес нетто [{kol_netto}]" +
                               $" \n Длина [{length_roll}]" +
                               $" \n ШК1 [{barcode1}]" +
                               $" \n ШК2 {barcode2}]" +
                               $" \n Плотность [{wet_list}]" +
                               $" \n Дата изготовления [{dt_tm}]" +
                               $" \n ТУ [{tu}]" +
                               $"\n Партия [{id_pz}] \n");

                            // вызов формирования ярлыка



                        }
                    }
                }
            }
        }

        public void SetSplash(bool inProgressFlag, string msg = "")
        {
            QueryInProgress = inProgressFlag;
            SplashControl.Visible = inProgressFlag;
            SplashControl.Message = msg;
        }

        /// <summary>
        /// возврат кнопок в исходное состояние
        /// </summary>
        private void ButtonPush()
        {
            for (int i = 0; i < 10; i++)
            {
                if (!buttons[i])
                {
                    buttons[i] = true;
                    switch (i)
                    {
                        // Печать ярлыка
                        case 0:
                            {
                                PrintLabelButton.IsEnabled = true;
                                RollSetButton.IsEnabled = false;
                                RollArrivialButton.IsEnabled = false;
                                LogMsg($"Сработал таймер печати ярлыка.");
                            }
                            break;

                        // Оприходовать
                        case 1:
                            {
                                RollArrivialButton.IsEnabled = true;
                            }
                            break;

                        //
                        case 2:
                            {
                                //.IsEnabled = true;
                            }
                            break;

                        // 
                        case 3:
                            {
                                //.IsEnabled = true;
                            }
                            break;

                        //
                        case 4:
                            {
                                //.IsEnabled = true;
                            }
                            break;

                        //
                        case 5:
                            {
                                //.IsEnabled = true;
                            }
                            break;

                        // установить данные
                        case 6:
                            {
                                RollSetButton.IsEnabled = true;

                            }
                            break;

                        // данные на торцевой принтер
                        case 7:
                            {
                                PrepareDataInsidePrinterButton.IsEnabled = true;
                            }
                            break;
                        // включить реле на торцевом принтере
                        case 8:
                            {
                                StartToInsidePrinterButton.IsEnabled = true;

                            }
                            break;
                    }
                    SetSplash(false);
                    ButtonTimer.Finish();
                    ButtonTimerRun = false;
                }
            }
        }

        /// <summary>
        ///  логика работы с кнопками
        /// </summary>
        private void RollsGridOnChange()
        {
            if (ButtonTimerRun == true)
            {
                return;
            }

            NewRollsPanel.IsEnabled = false;
            RollSettingPanel.IsEnabled = false;
            RollSettingPanel2.IsEnabled = false;

            PrintLabelButton.IsEnabled = false;
            OnPrsButton.IsEnabled = false;
            OffPrsButton.IsEnabled = false;
            RollSetButton.IsEnabled = false;
            RollArrivialButton.IsEnabled = false;
            RollDeleteButton.IsEnabled = false;

            LenRoll.Text = "";
            DiameterRoll.Text = "";
            FormatRoll.Text = "";
            SegmentRoll.Text = "";
            NumberRoll.Text = "";
            WeightRoll.Text = "";
            WeightNettoRoll.Text = "";
            RollArrivialButton.Content = "Оприходовать";

            if (ReadOnlyFlag == false)
            {
                // можно  поставить на ПРС
                if ((SelectedRollsItem.CheckGet("ID_STATUS").ToInt() == 2)
                    && (SelectedRollsItem.CheckGet("NUM_ROLL_TMBR").IsNullOrEmpty())
                    && (!SelectedRollsItem.CheckGet("BDRC_ID").IsNullOrEmpty()))
                {
                    NewRollsPanel.IsEnabled = true;
                    OnPrsButton.IsEnabled = true;
                    NewRollButton.IsEnabled = false;
                }
                // можно  снять с ПРС
                else if ((SelectedRollsItem.CheckGet("ID_STATUS").ToInt() == 4)
                    && (SelectedRollsItem.CheckGet("NUM_ROLL_TMBR").IsNullOrEmpty())
                    && (!SelectedRollsItem.CheckGet("BDRC_ID").IsNullOrEmpty()))
                {
                    NewRollsPanel.IsEnabled = true;
                    OffPrsButton.IsEnabled = true;
                }

                // вычисляем номер рулона для этого съема
                if ((SelectedRollsItem.CheckGet("ID_STATUS").ToInt() == 4)
                    && (SelectedRollsItem.CheckGet("NUM_ROLL_TMBR").IsNullOrEmpty()))
                {
                    var bdrcId = SelectedRollsItem.CheckGet("BDRC_ID").ToInt();
                    GetNumRoll(bdrcId);
                    if (NumRollTmbr != -1)
                    {
                        NumberRoll.Text = (NumRollTmbr + 1).ToString();
                        NewRollsPanel.IsEnabled = true;
                        NewRollButton.IsEnabled = true;
                    }
                    else
                    {
                        NewRollButton.IsEnabled = false;
                    }
                    LogMsg($"Получаю следующий номер={NumRollTmbr + 1} рулона для съема BDRC_ID={bdrcId}");
                }

                // если есть idp_roll у рулона
                if (!SelectedRollsItem.CheckGet("IDP_ROLL").IsNullOrEmpty())
                {
                    // получаем и заполняем все параметры рулона
                    var idpRoll = SelectedRollsItem.CheckGet("IDP_ROLL").ToInt();
                    GetDataRoll(idpRoll);
                    RollSettingPanel.IsEnabled = true;
                    RollSettingPanel2.IsEnabled = true;
                    RollSetButton.IsEnabled = true;
                }

                // решаем можно ли удалить рулон
                if ((SelectedRollsItem.CheckGet("ID_STATUS").ToInt() == 14 || SelectedRollsItem.CheckGet("ID_STATUS").ToInt() == 15))
                {
                    RollDeleteButton.IsEnabled = true;
                }

                // решаем оприходовать/списать в брак рулон
                if ((SelectedRollsItem.CheckGet("ID_STATUS").ToInt() == 14)
                    && (SelectedRollsItem.CheckGet("ID2").ToInt() > 0)
                    && (SelectedRollsItem.CheckGet("ROLL_WEIGHT").ToInt() > 0))
                {
                    RollArrivialButton.Content = "Оприходовать";
                    RollArrivialButton.IsEnabled = true;
                    QueryInProgress = false;
                }

                if ((SelectedRollsItem.CheckGet("ID_STATUS").ToInt() == 3)
                    && (SelectedRollsItem.CheckGet("NUM_ROLL_TMBR").IsNullOrEmpty()))
                {
                    RollArrivialButton.Content = "Списать в брак";
                    RollArrivialButton.IsEnabled = true;
                    QueryInProgress = false;
                }
            }

            // если рулон оприходован, то можно повторно напечатать ярлык
            if (SelectedRollsItem.CheckGet("ID_STATUS").ToInt() == 16)
            {
                RollSetButton.IsEnabled = false;
                NewRollsPanel.IsEnabled = false;
                RollSettingPanel.IsEnabled = false;
                RollSettingPanel2.IsEnabled = false;
                PrintLabelButton.IsEnabled = true;
            }

        }

        /// <summary>
        /// напечатать ярлык для выбранного рулона 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrintLabelButton_Click(object sender, RoutedEventArgs e)
        {
            // LogMsg($"Нажали кнопку печать ярлыка, idp_roll=[{IdpRoll.ToString()}]");
            buttons[0] = false;
            PrintLabelButton.IsEnabled = false;
            SetSplash(true, "Ждите");
            ButtonTimer.Run();

            PrintLabel();

            // LogMsg($"Закончена печать ярлыка, idp_roll=[{IdpRoll.ToString()}]");
        }

        /// <summary>
        /// печать или просмотр ярлыка на рулон
        /// </summary>
        private async void PrintLabel()
        {
            IdpRoll = SelectedRollsItem.CheckGet("IDP_ROLL").ToInt();
            // проверяем был ли рулон перемаркирован
            //  GetRemarkRoll();

            await Task.Run(() => { GetRemarkRoll(); });

            if (PrintPreviewEnable.IsChecked == false)
            {
                // сразу печатаем на лазерном принтере ярлык
                // получаем данные для печати ярлыка
                ///    LogMsg($"Получаем данные для печати одного ярлыка, idp_roll=[{IdpRoll.ToString()}]");
                GetDataRollForLabel(1);
                //await Task.Run(() => { GetDataRollForLabel(1); });
            }
            else
            {
                ///     LogMsg($"Получаем данные для просмотра ярлыка, idp_roll=[{IdpRoll.ToString()}]");
                var rawMaterialLabelReport = new RawMaterialLabelReport();
                // это внешний ярлык
                rawMaterialLabelReport.CurrentLabelType = RawMaterialLabelReport.LabelType.OuterLabel;
                rawMaterialLabelReport.ShowLabel(IdpRoll.ToString());
                //await Task.Run(() => { rawMaterialLabelReport.ShowLabel(IdpRoll.ToString()); });
            }
        }

        /// <summary>
        ///  оприходуем рулон (5 в 1)
        /// </summary>
        private async void ArrivialRollNew()
        {
            var idp = SelectedRollsItem.CheckGet("IDP_ROLL").ToInt().ToString();
            Kol = "";
            Name = "";
            Num = "";

            LogMsg($"Начато оприходование рулона idp_roll=[{idp}]");
            var p = new Dictionary<string, string>();
            p.CheckAdd("IDP_ROLL", idp);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "PmSlitter");
            q.Request.SetParam("Action", "ArrivialRollFull");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = 1;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var dataSet = ListDataSet.Create(result, "ITEMS");
                    if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                    {
                        // результат 
                        var idp_old = dataSet.Items.First().CheckGet("IDP").ToInt();
                        var idp_new = dataSet.Items.First().CheckGet("IDP_NEW").ToInt();

                        var rez1 = dataSet.Items.First().CheckGet("CODE1").ToInt();
                        var rez2 = dataSet.Items.First().CheckGet("CODE2").ToInt();
                        var rez3 = dataSet.Items.First().CheckGet("CODE3").ToInt();

                        if (rez1 == 0)
                        {
                            //0 - рулон оприходован успешно. 1- ошибка при оприходовании рулона
                            string msg = $"Рулон [{idp_old}] оприходован успешно!{Environment.NewLine}";
                            LogMsg($"{msg}  ПЗ [{CurIdPz}],  bdrc_id [{SelectedRollsItem.CheckGet("BDRC_ID").ToInt()}]");

                            // проверяем, была ли проведена перемаркировка рулона
                            if (rez2 == 0)
                            {
                                LogMsg($"Проверка перемаркировки проведена успешно.");

                                if (idp_old != idp_new)
                                {
                                    LogMsg($"Рулон был перемаркирован. Новый idp_roll=[{idp_new.ToString()}]");
                                }
                            }

                            if (rez3 == 0)
                            {
                                LogMsg($"Успешное выполнение SetRollOrderdates для рулона [{idp_new.ToString()}].");
                            }

                            // получили данные для печати на лазерном принтере ярлыка
                            var dsLabel = ListDataSet.Create(result, "LABEL");
                            if (dsLabel != null && dsLabel.Items != null && dsLabel.Items.Count > 0)
                            {
                                var first = dsLabel.Items.First();

                                var name_proizv = first.CheckGet("NAME_PROIZV");
                                var name = first.CheckGet("NAME");
                                var full_name = first.CheckGet("FULL_NAME");
                                var num = first.CheckGet("NUM");
                                var kol_brutto = first.CheckGet("KOL_BRUTTO").ToInt();
                                var kol_netto = first.CheckGet("KOL_NETTO").ToInt();
                                var length_roll = first.CheckGet("LENGTH_ROLL").ToInt();
                                var barcode1 = first.CheckGet("BARCODE1");
                                var barcode2 = first.CheckGet("BARCODE2");
                                var wet_list = first.CheckGet("WET_LIST");
                                var dt_tm = first.CheckGet("DT_TM");
                                var tu = first.CheckGet("TU");
                                var id_pz = first.CheckGet("ID_PZ").ToInt();

                                LogMsg($"Получены данные для печати ярлыка по рулону [{idp_new}]:" +
                                   $" \n Производитель [{name_proizv}]" +
                                   $" \n Бумага [{name}]" +
                                   $" \n Название [{full_name}]" +
                                   $" \n № рулона [{num}]" +
                                   $" \n Вес брутто [{kol_brutto}]" +
                                   $" \n Вес нетто [{kol_netto}]" +
                                   $" \n Длина [{length_roll}]" +
                                   $" \n ШК1 [{barcode1}]" +
                                   $" \n ШК2 {barcode2}]" +
                                   $" \n Плотность [{wet_list}]" +
                                   $" \n Дата изготовления [{dt_tm}]" +
                                   $" \n ТУ [{tu}]" +
                                   $"\n Партия [{id_pz}] \n");

                                var countCopy = 2;
                                var action = "P"; // сразу печать

                                if (PrintPreviewEnable.IsChecked == true)
                                {
                                    countCopy = 1;
                                    action = "S"; // просмотр
                                }

                                if (PrintAlternative.IsChecked == true)
                                {
                                    // сохраняем данные в файл (перезаписываем)
                                    var pathInfo = new FileInfo(Assembly.GetExecutingAssembly().Location);
                                    string printFile = $"{pathInfo.Directory}\\@@";
                                    try
                                    {
                                        string text = $"1;{name_proizv}" +
                                                              $"\n2;{name}" +
                                                              $"\n3;{full_name}" +
                                                              $"\n4;{num}" +
                                                              $"\n5;{kol_brutto}" +
                                                              $"\n6;{kol_netto}" +
                                                              $"\n7;{length_roll}" +
                                                              $"\n8;{barcode1}" +
                                                              $"\n9;{barcode2}" +
                                                              $"\n10;{wet_list}" +
                                                              $"\n11;{dt_tm}" +
                                                              $"\n12;{tu}" +
                                                              $"\n13;{id_pz}" +
                                                              $"\n14;{countCopy}" +
                                                              $"\n15;{action}";

                                        File.WriteAllText(printFile, text);

                                        // вызываем программу внешней печати
                                        Process ExternalProcess = new Process();
                                        ExternalProcess.StartInfo.FileName = $"{pathInfo.Directory}\\printLabel.exe";
                                        ExternalProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                        ExternalProcess.Start();
                                        ExternalProcess.WaitForExit(1000);
                                    }
                                    catch (Exception e)
                                    {

                                    }
                                }
                            }

                            // получили данные для печати на торцевом принтере
                            var ds = ListDataSet.Create(result, "INSIDE");
                            if (ds != null && ds.Items != null && ds.Items.Count > 0)
                            {
                                // результат 
                                Kol = ds.Items.FirstOrDefault().CheckGet("KOL").ToInt().ToString();
                                Name = ds.Items.FirstOrDefault().CheckGet("NAME").ToString();
                                Num = ds.Items.FirstOrDefault().CheckGet("NUM").ToString();
                                LogMsg($"Получены данные для печати на торцевом принтере, для рулона idp_roll=[{idp_new.ToString()}]\n Kol=[{Kol}], Namel=[{Name}], Num=[{Num}]");

                                // сразу печатаем на торцевом принтере
                                if (AutoPrint.IsChecked == true)
                                {
                                    if (!Kol.IsNullOrEmpty() && !Name.IsNullOrEmpty() && !Num.IsNullOrEmpty())
                                    {
                                        // формируем пакет данных для передачи на торцевой принтер
                                        SendDataToInsidePrinter();
                                        // печатаем на торцевом принтере (включаем реле 1 на 2 сек.)
                                        StartToInsidePrinter();
                                    }
                                }
                            }

                            CurSecund = 0;
                            UpdateData();
                            FastTimer.Start();
                            LogMsg($"Запустил таймер {RefreshTime} сек. после оприходования.");
                        }
                        else
                        {
                            var d = new DialogWindow($"Ошибка. Код ответа [{rez1}]", "Оприходование рулона");
                            d.ShowDialog();
                        }
                    }
                }
            }
            else
            {
                string msg = q.Answer.Error.Message;
                LogMsg($"Ошибка при оприходовании рулона [{SelectedRollsItem.CheckGet("IDP_ROLL").ToInt()}], \nmsg");

                var i = new ErrorTouch();
                i.Show("Ошибка при оприходовании рулона", msg, 5);

                //var d = new DialogTouch($"{msg}", "Оприходование рулона", "", DialogWindowButtons.OKAutohide);
                // var d = new DialogWindow($"{msg}", "Оприходование рулона");
                // d.ShowDialog();
            }
        }

        ///////////////////////
    }
}
