using Client.Annotations;
using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using DevExpress.Xpf.Core.Native;
using DevExpress.Xpf.Grid;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Office.Interop.Excel;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Интерфейс оператора БДМ
    /// </summary>
    /// <author>Greshnyh_NI</author>   
    /// <refactor>Greshnyh_NI</refactor>
    /// 
    public partial class PaperMakingMachineOperator : ControlBase
    {
        public PaperMakingMachineOperator()
        {
            InitializeComponent();

            Form = null;

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            OnLoad = () =>
            {
                Initialized = false;
                //                var i = new ErrorTouch();
                //                i.Show("Информация", CurrentMachineId.ToString(), 5);
                if (MachineId == 716)
                {
                    CurrentPlaceName = "Локация БДМ1";
                    CurrentMacine.Text = "БДМ1";
                }
                else
                {
                    MachineId = 1716;
                    CurrentPlaceName = "Локация БДМ2";
                    CurrentMacine.Text = "БДМ2";
                }

                InitForm();
                SetDefaults();
                Init();

                // получение прав пользователя
                ProcessPermissions();
                Initialized = true;
            };
        }

        public FormHelper Form { get; set; }
        /// <summary>
        /// Имя вкладки
        /// </summary>
        public string TabName;

        /// <summary>
        /// ИД переданного станка
        /// 716 или 1716
        /// </summary>
        public int MachineId { get; set; }

        /// <summary>
        /// Место работы программы
        /// </summary>
        public string CurrentPlaceName;

        /// <summary>
        /// Таймер периодического обновления каждые 5 секунд
        /// </summary>
        private DispatcherTimer FastTimer { get; set; }
        /// <summary>
        /// Таймер периодического обновления каждую минуту
        /// </summary>
        private DispatcherTimer SlowTimer { get; set; }
        /// <summary>
        /// Таймер проверки блокировки слишком частых обновлений
        /// </summary>
        private DispatcherTimer CheckBlockTimer { get; set; }
        /// <summary>
        /// Таймер проверки пожара
        /// </summary>
        private DispatcherTimer TimerFire;
        /// <summary>
        /// Статус пожара на объекте
        /// </summary>
        public string FireInPlace { get; set; }

        /// <summary>
        /// признак свертки грида ПЗ
        /// </summary>
        public int ColapseFlag { get; set; }

        /// <summary>
        /// признак свертки грида простоев
        /// </summary>
        public int ColapseIdlesFlag { get; set; }

        public bool Initialized { get; set; }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {

            string role = "";
            // Проверяем уровень доступа
            if (MachineId == 716)
            {
                role = "[erp]bdm_1_control";
            }
            else
            {
                role = "[erp]bdm_2_control";
            }

            var mode = Central.Navigator.GetRoleLevel(role);
            var userAccessMode = mode;

            switch (mode)
            {
                case Role.AccessMode.Special:
                    {
                        ButtonCloseTask.IsEnabled = false;
                        ButtonUpTask.IsEnabled = true;
                        ButtonDownTask.IsEnabled = true;
                    }

                    break;

                case Role.AccessMode.FullAccess:
                    {
                        ButtonCloseTask.IsEnabled = true;
                        ButtonUpTask.IsEnabled = true;
                        ButtonDownTask.IsEnabled = true;
                    }
                    break;

                case Role.AccessMode.ReadOnly:
                    {
                        ButtonCloseTask.IsEnabled = false;
                        ButtonUpTask.IsEnabled = false;
                        ButtonDownTask.IsEnabled = false;
                    }
                    break;
            }
        }

        /// <summary>
        /// Время начала смены
        /// </summary>
        public static DateTime DateShiftStart
        {
            get
            {
                DateTime dateNow = DateTime.Now.Date;
                int hourNow = DateTime.Now.Hour;
                DateTime timeShiftStart = dateNow.AddHours(hourNow);
                while (timeShiftStart.Hour != 8
                    && timeShiftStart.Hour != 20)
                {
                    timeShiftStart = timeShiftStart.AddHours(-1);
                }
                return timeShiftStart;
            }
        }

        /// <summary>
        /// базовая дата, начало интервала
        /// BASE-12 часов
        /// </summary>
        /// <returns></returns>
        public DateTime GetBaseDate()
        {
            var todayDateString = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            var today = todayDateString.ToDateTime().AddHours(-12);

            return today;
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {

            // Информация по количеству времени выполнения заданий разной степени просроченности
            //            TaskQueue.OnAfterLoadQueue += UpdateTaskCounter;
            // Раскраска кнопки Применить в обычный стиль, не синий
            TaskQueue.OnAfterLoadQueue += ButtonApplyToNormal;

            SetCurrentTime();
            FireStatus.Visibility = Visibility.Collapsed;
            ColapseFlag = 0;
            ColapseIdlesFlag = 0;
        }

        public void InitForm()
        {
            //инициализация формы, элементы тулбара
            {
                Form = new FormHelper();

                //колонки формы
                var fields = new List<FormHelperField>()
                {
                    //new FormHelperField()
                    //{
                    //    Path="TODAY",
                    //    FieldType=FormHelperField.FieldTypeRef.String,
                    //    Control=Today,
                    //    ControlType="TextBox",
                    //  //  Default=MomentStart.ToString("dd.MM.yyyy"),
                    //    Default=DateTime.Now.ToString("dd.MM.yyyy"),
                    //    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    //    }
                    //},
                };
                Form.SetFields(fields);

                //после установки значений
                Form.AfterSet = (Dictionary<string, string> v) =>
                {
                    //фокус на кнопку обновления
                    RefreshButton.Focus();
                };
            }

        }


        /// <summary>
        /// Инициализация компонентов
        /// </summary>
        public void Init()
        {
            bool resume = false;

            //   SetFastTimer(5);
            SetTimerFire(10);
            SetSlowTimer(60);

            ButtonCloseTask.Visibility = Visibility.Visible;

            // произведено всего и остаток на складе  
            LoadRemains();

            // производственное задание
            TaskQueue.MachineId = MachineId;
            TaskQueue.Init();

            // Информация по простоям
            IdlesData.MachineId = MachineId;
            IdlesData.Init();

            // Показатели задания
            ProductionData.MachineId = MachineId;
            ProductionData.Init();

            // график намотки тамбура
            SpeedChart.MachineId = MachineId;
            SpeedChart.Init();

            TaskQueue.OnAfterLoadQueue += TaskQueue_OnAfterLoad;
        }

        /// <summary>
        /// Обновление всей информации
        /// </summary>
        private async void UpdateData()
        {
            // заблокировать кнопку обновления, разблокировка происходит в событии после загрузки графика
            RefreshButton.IsEnabled = false;

            // произведено всего и остаток на складе  
            LoadRemains();

            //производственное задание
            TaskQueue.LoadItems();

            // Информация по простоям
            IdlesData.LoadItems();

            // Показатели задания
            ProductionData.LoadItems();

            // График скорости
            SpeedChart.LoadData();
        }

        private void TaskQueue_OnItemChange(Dictionary<string, string> item)
        {
        }

        private void SpeedChart_OnAfterLoad()
        {
            RefreshButton.IsEnabled = true;
        }

        private void TaskQueue_OnAfterLoad()
        {
            RefreshButton.IsEnabled = true;
        }


        /// <summary>
        /// Таймер частого обновления (5 секунд)
        /// </summary>
        public void SetFastTimer(int autoUpdateInterval)
        {
            FastTimer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, autoUpdateInterval)
            };

            {
                var row = new Dictionary<string, string>();
                row.CheckAdd("TIMEOUT", autoUpdateInterval.ToString());
                row.CheckAdd("DESCRIPTION", "");
                Central.Stat.TimerAdd("PaperMakingMachineOperator_SetFastTimer", row);
            }

            FastTimer.Tick += (s, e) =>
            {
                //  TaskData.LoadData();
                //                CheckDefects();
                //                CheckFirstTask();
                //  TaskQueue.TaskGridApplyStyles();
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

            {
                var row = new Dictionary<string, string>();
                row.CheckAdd("TIMEOUT", autoUpdateInterval.ToString());
                row.CheckAdd("DESCRIPTION", "");
                Central.Stat.TimerAdd("PaperMakingMachineOperator_SetSlowTimer", row);
            }

            SlowTimer.Tick += (s, e) =>
            {
                UpdateData();
                SetCurrentTime();
            };

            SlowTimer.Start();
        }


        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            // Остановка таймеров
            FastTimer?.Stop();
            SlowTimer?.Stop();
            TimerFire?.Stop();

            //Central.Msg.SendMessage(new ItemMessage()
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverName = "PaperMakingMachineTaskQueue",
                SenderName = ControlName,
                Action = "CLOSED",
            });

            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Production",
                ReceiverName = "",
                SenderName = "PaperMakingMachineOperator",
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
            if (m.ReceiverGroup.IndexOf("Production") > -1)
            {
                // обновление данных
                if (m.ReceiverName.IndexOf("Idles") > -1)
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            //                            IdleGrid.IdleGrid.LoadItems();

                            var id = m.Message.ToInt();
                            //                            IdleGrid.IdleGrid.SetSelectedItemId(id);
                            break;

                        case "RefreshIdles":
                            // Информация по простоям
                            IdlesData.LoadItems();
                            break;
                        case "RollUpIdles":
                            // Свернуть/развернуть грид по простоям
                            RollUpIdles();
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Раскраска кнопки Применить в обычный стиль, не синий
        /// </summary>
        public void ButtonApplyToNormal()
        {
            // ButtonApply.Style = (Style)ButtonApply.TryFindResource("Button");
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

        public void SetCurrentTime()
        {
            CurrentTime.Text = $"{(DateTime.Now.Hour):00}:{DateTime.Now.Minute:00}";
        }

        /// <summary>
        /// обработка ввода с клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.F5:
                    TaskQueue.LoadItems();
                    e.Handled = true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// отображение справочной статьи
        /// (относительный путь)
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/production/PaperMakingMachineOperator");
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        /// <summary>
        /// Кнопка "Обновить"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // заблокировать кнопку обновления, разблокировка происходит в событии после загрузки графика
            RefreshButton.IsEnabled = false;

            // произведено всего и остаток на складе  
            LoadRemains();

            // Очередь ПЗ
            TaskQueue.LoadItems();

            // Произведено за смену, недел., месяц
            ProductionData.LoadItems();

            // График скорости
            if (Initialized)
            {
                SpeedChart.LoadData();
            }

            RefreshButton.IsEnabled = true;
        }

        /// <summary>
        /// Кнопка "Обрывы БДМ"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonDefectReasons_Click(object sender, RoutedEventArgs e)
        {
            //            DefectsShow();
        }

        /// <summary>
        /// Кнопка "Раскрыть список заданий"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonChangeTask_Click(object sender, RoutedEventArgs e)
        {
            RollUpPz();
        }

        /// <summary>
        /// Раскрыть//свернуть список заданий
        /// </summary>
        private void RollUpPz()
        {
            if (ColapseFlag == 0)
            {
                // развернуть
                PanelPz.Height = new GridLength(2, GridUnitType.Star);
                PanelBottom.Height = new GridLength(5);
                ButtonChangeTask.Content = "Свернуть список заданий";
                //     ButtonUpTask.Visibility = Visibility.Visible;
                //     ButtonDownTask.Visibility = Visibility.Visible;
                //     ButtonCloseTask.Visibility = Visibility.Collapsed;

                ProcessPermissions();
                // по указанию от Лупача только для избранных
                //  ButtonUpTask.IsEnabled = true;
                //  ButtonDownTask.IsEnabled = true;

                  ColapseFlag = 1;

                //Central.Msg.SendMessage(new ItemMessage()
                Messenger.Default.Send(new ItemMessage()
                {
                    ReceiverName = "PaperMakingMachineTaskQueue",
                    SenderName = ControlName,
                    Action = "GRID_DOWN",
                });

            }
            else
            {
                // свернуть
                PanelPz.Height = new GridLength(200);
                PanelBottom.Height = new GridLength(1, GridUnitType.Star);
                ButtonChangeTask.Content = "Раскрыть список заданий";
                //   ButtonUpTask.Visibility = Visibility.Collapsed;
                //   ButtonDownTask.Visibility = Visibility.Collapsed;
                //   ButtonCloseTask.Visibility = Visibility.Visible;

                ButtonUpTask.IsEnabled = false;
                ButtonDownTask.IsEnabled = false;

                ColapseFlag = 0;

                //Central.Msg.SendMessage(new ItemMessage()
                 Messenger.Default.Send(new ItemMessage()
                  {
                    ReceiverName = "PaperMakingMachineTaskQueue",
                    SenderName = ControlName,
                    Action = "GRID_UP",
                });

            }
        }

        /// <summary>
        /// настройка программы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void BurgerMenuButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }

        /// <summary>
        /// Обновить программу
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// закрыть программу
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Информация
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InfoMenu_Click(object sender, RoutedEventArgs e)
        {
            ShowInfo();
        }

        private void ShowInfo()
        {
            var t = "Отладочная информация";
            var m = Central.MakeInfoString();
            var i = new ErrorTouch();
            i.Show(t, m);
        }

        /// <summary>

        /// остаток на складе продукции для указанного станка
        /// произведено продукции для указанного станка (всего по ПЗ)
        /// </summary>
        private async void LoadRemains()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "Monitoring");
            q.Request.SetParam("Action", "RemainsList");

            var p = new Dictionary<string, string>();
            p.Add("ID_ST", MachineId.ToString());

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
                    Ostatok.Text = ds.Items.FirstOrDefault().CheckGet("CNT").ToString(); //ds.Items[0]["CNT"].ToString();

                    var ds2 = ListDataSet.Create(result, "ITEMS2");
                    Prihod.Text = ds2.Items.FirstOrDefault().CheckGet("KOL").ToString(); //ds2.Items[0]["KOL"].ToString();

                    var ds3 = ListDataSet.Create(result, "SA_ON_DUTY");
                    var name = ds3.Items.FirstOrDefault().CheckGet("NAME").ToString();
                  
                    InfoDuty.Text = $"Дежурный СА: {name}";
                }
            }
        }

        /// <summary>
        /// Закрыть текущее задание
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonCloseTask_Click(object sender, RoutedEventArgs e)
        {
            //Central.Msg.SendMessage(new ItemMessage()
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverName = "PaperMakingMachineTaskQueue",
                SenderName = ControlName,
                Action = "CLOSE_TASK",
            });
        }


        /// <summary>
        /// переместить задание выше
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonUpTask_Click(object sender, RoutedEventArgs e)
        {
            //  Central.Msg.SendMessage(new ItemMessage()
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverName = "PaperMakingMachineTaskQueue",
                SenderName = ControlName,
                Action = "UP",
            });
        }

        /// <summary>
        /// переместить задание ниже
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonDownTask_Click(object sender, RoutedEventArgs e)
        {
            //   Central.Msg.SendMessage(new ItemMessage()
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverName = "PaperMakingMachineTaskQueue",
                SenderName = ControlName,
                Action = "DOWN",
            });
        }

        /// <summary>
        /// Свернуть/развернуть грид простоев
        /// </summary>
        private void RollUpIdles()
        {
            if (ColapseIdlesFlag == 0)
            {
                // развернуть
                Col0.Width = new GridLength(0, GridUnitType.Star);
                Col1.Width = new GridLength(0, GridUnitType.Star);
                Col2.Width = new GridLength(0, GridUnitType.Star);
                Col3.Width = new GridLength(0, GridUnitType.Star);
                ColIdles.Width = new GridLength(1, GridUnitType.Star);
                ColapseIdlesFlag = 1;
            }
            else
            {
                // свернуть
                Col0.Width = new GridLength(0.5, GridUnitType.Star);
                Col1.Width = new GridLength(5, GridUnitType.Pixel);
                Col2.Width = new GridLength(0.4, GridUnitType.Star);
                Col3.Width = new GridLength(1, GridUnitType.Pixel);
                ColIdles.Width = new GridLength(0.4, GridUnitType.Star);
                ColapseIdlesFlag = 0;
            }
        }

        /// <summary>
        ///  экспорт в  Excel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExcelButton_Click(object sender, RoutedEventArgs e)
        {
            //  Central.Msg.SendMessage(new ItemMessage()
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverName = "PaperMakingMachineTaskQueue",
                SenderName = ControlName,
                Action = "EXCEL",
            });
        }


        ///////////////
    }
}
