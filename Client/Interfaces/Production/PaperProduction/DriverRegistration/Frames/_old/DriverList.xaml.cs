using Client.Assets.HighLiters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;
using System.Linq;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Регистрация водителей на проходных БДМ1 и БДМ2
    /// </summary>
    /// <author>Грешных Н.</author>
    /// <version>1</version>
    /// <released>2023-04-26</released>
    /// <changed>2023-04-26</changed>
    public partial class DriverList : UserControl
    {
        public DriverList()
        {

            InitializeComponent();

            Loaded += OnLoad;
            Loaded += DriverControl_Loaded;
            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);
            //  DataStateList = new List<Dictionary<string, string>>();
            Init();
        }

        //private List<Dictionary<string, string>> DataStateList { get; set; }


        /// <summary>
        /// интервал автообновления времени, сек
        /// </summary>
        public int AutoUpdateTimeInterval { get; set; }

        /// <summary>
        /// таймер анимации времени
        /// </summary>
        private DispatcherTimer TimeBlinkTimer { get; set; }

        /// <summary>
        /// форма с полями для фильтрации данных
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// текущая плащадка 716- БДМ1, 1716-БДМ2
        /// </summary>
        public int IdSt { get; set; }

        /// <summary>
        /// Имя этой вкладки
        /// </summary>
        public string TabName;

        private void DriverControl_Loaded(object sender, RoutedEventArgs e)
        {
            Central.WM.SelectedTab = "DriverList";
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        private void ProcessMessages(ItemMessage obj)
        {
            //Group 
            if (obj.ReceiverGroup.IndexOf("DriverRegistration") > -1)
            {
                if (obj.ReceiverName.IndexOf("DriverList") > -1)
                {
                    switch (obj.Action)
                    {
                        case "Closed":
                            ListCarGridLoadItems();
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// обработчик системы навигации по URL
        /// </summary>
        public void ProcessNavigation()
        {
        }

        /// <summary>
        /// инициализация 
        /// </summary>
        public void Init()
        {
            TabName = "DriverList";

            IdSt = 716;
            // Если в конфиге указан станок, выбираем его
            IdSt = Central.Config.CurrentMachineId;

            var m = "2";
            if (IdSt == 716)
                m = "1";

            //выводим название площадки
            LabelLisCar.Content = LabelLisCar.Content + m;
            //DateTime now = DateTime.Now;
            LabelTime.Content = ($"{DateTime.Now:f}");

            AutoUpdateTimeInterval = 60;
            DriverGridInit();
            SetDefaults();
            TimerBlinkRun();
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            //отправляем сообщение о загрузке интерфейса
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "DriverRegistration",
                ReceiverName = "",
                SenderName = "DriverList",
                Action = "Loaded",
            });
        }

        /// <summary>
        /// деструктор интерфейса
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "DriverRegistration",
                ReceiverName = "",
                SenderName = "DriverList",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            ListCarGrid.Destruct();
            //останавливаем таймеры времени
            TimerBlinkStop();
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {

        }

        /// <summary>
        /// инициализация списка машин
        /// </summary>
        public void DriverGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Машина",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width=200,
                        MinWidth=150,
                        MaxWidth=300,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ телефона",
                        Path="PHONE_NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        Width=170,
                        MinWidth=100,
                        MaxWidth=200,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Время",
                        Path="TM",
                        ColumnType=ColumnTypeRef.String,
                        Width=80,
                        MinWidth=60,
                        MaxWidth=90,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус",
                        Path="STATUS",
                        ColumnType=ColumnTypeRef.String,
                        Width=700,
                     //   MinWidth=200,
                     //   MaxWidth=1000,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="ID_SCRAP",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=60,
                        MaxWidth=60,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                       Header = " ",
                       Path = "_",
                       ColumnType = ColumnTypeRef.String,
                       MinWidth = 5,
                       MaxWidth = 1000,
                    },
                };
                ListCarGrid.SetColumns(columns);
            };

            // Запрет на изменение сортировки в таблице
            ListCarGrid.UseSorting = false;
            // автообновление каждые 30 сек
            ListCarGrid.AutoUpdateInterval = 30;
            ListCarGrid.UseRowHeader = false;

            ListCarGrid.Init();
            // предотвращение двойного вызова OnSelectItem
            ListCarGrid.SelectItemMode = 0;

            //двойной клик на строке напечатает ярлык
            ListCarGrid.OnDblClick = selectedItem =>
            {
                TestPrint();
            };

            //данные грида
            ListCarGrid.OnLoadItems = ListCarGridLoadItems;
            ListCarGrid.Run();

            ScaleTransform scale = new ScaleTransform(2, 2);
            ListCarGrid.LayoutTransform = scale;

            //фокус ввода           
            ListCarGrid.Focus();
        }

        /// <summary>
        /// получение записей списка зарегистрированных машин
        /// </summary>
        private void ListCarGridLoadItems()
        {
            DisableControls();

            bool resume = true;

            if (resume)
            {

                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID_ST", IdSt.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "ProductionPm");
                q.Request.SetParam("Object", "DriverRegistration");
                q.Request.SetParam("Action", "DriverRegistrationList");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestTimeoutGrid;
                q.Request.Attempts = Central.Parameters.RequestAttemptsGrid;

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            ListCarGrid.UpdateItems(ds);
                        }

                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            EnableControls();
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            ListCarGrid.ShowSplash();
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            ListCarGrid.HideSplash();
        }

        /// <summary>
        /// запус таймера анимации времени
        /// </summary>
        private void TimerBlinkRun()
        {

            if (AutoUpdateTimeInterval != 0)
            {
                if (TimeBlinkTimer == null)
                {
                    TimeBlinkTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0, 0, AutoUpdateTimeInterval)
                    };

                    TimeBlinkTimer.Tick += (s, e) =>
                        {

                            //Central.Dbg($"TimeBlinkTimer");
                            LabelTime.Content = ($"{DateTime.Now:f}");
                        };
                }

                if (TimeBlinkTimer.IsEnabled)
                {
                    TimeBlinkTimer.Stop();
                }
                TimeBlinkTimer.Start();
            }

        }

        //останов таймера анимации времени
        private void TimerBlinkStop()
        {

            if (TimeBlinkTimer != null)
            {
                if (TimeBlinkTimer.IsEnabled)
                {
                    TimeBlinkTimer.Stop();
                }
            }

        }

        /// <summary>
        /// нажали кнопку регистрации водителя
        /// </summary>
        private void DriverRegistrationButton_Click(object sender, RoutedEventArgs e)
        {

            Helper.ButtonClickAnimation(sender, RegistrationDrive);
        }

        /// <summary>
        /// нажали кнопку выхода из программы
        /// </summary>
        private void BurgerExit_Click(object sender, RoutedEventArgs e)
        {
            Exit();
        }

        private void BurgerButton_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender, ShowMenu);
        }

        private void ShowMenu()
        {
            BurgerMenu.IsOpen = true;
        }

        private void Exit()
        {
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Main",
                ReceiverName = "MainWindow",
                SenderName = "DriverList",
                Action = "Exit",
                Message = "",
            });
        }

        /// <summary>
        /// регистрируем нового водителя
        /// </summary>
        private void RegistrationDrive()
        {

            var p = new Dictionary<string, string>();
            p.CheckAdd("IdSt", IdSt.ToString());
            p.CheckAdd("ReceiverName", TabName.ToString());
            p.CheckAdd("Mode", "-1");
            p.CheckAdd("PhoneDriver", "");
            p.CheckAdd("IdPost", "0");
            p.CheckAdd("PostavshicName", "");
            p.CheckAdd("MarkaCar", "");
            p.CheckAdd("NumberCar", "");

            var i = new SettingsMode(p);
            i.Show();
        }


        /// <summary>
        /// печатаем ярлык 
        /// </summary>
        /// <param name="mode">1=просмотр,2=печать</param>
        /// <param name="idScrap"> id машины</param>
        /// <returns></returns>

        public bool PrintLabel(int mode, int idScrap)
        {
            bool result = false;

            var receiptViewer = new DriverLabelViewer();
            receiptViewer.IdScrap = idScrap;
            result = receiptViewer.Init();

            switch (mode)
            {
                //просмотр
                default:
                case 1:
                    receiptViewer.Show();
                    break;

                //печать
                case 2:
                    receiptViewer.Print(true);
                    break;
            }

            return result;
        }

        //печать ярлыка из списка машин
        private void TestPrint()
        {
            PrintLabel(1, ListCarGrid.SelectedItem.CheckGet("ID_SCRAP").ToInt());
        }


    }
}
