using Client.Annotations;
using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Production.Corrugator;
using Client.Interfaces.Production.Corrugator.CorrugatorMachineOperator.Frames;
using DevExpress.Xpf.Core.Native;
using GalaSoft.MvvmLight.Messaging;
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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.Corrugator.CorrugatorMachineOperator
{
    /// <summary>
    /// Интерфейс оператора гофроагрегата
    /// </summary>
    /// <author>zelenskiy_sv</author>   
    /// <refactor>eletskikh_ya</refactor>
    public partial class CorrugatorMachineOperator : UserControl
    {
        public CorrugatorMachineOperator()
        {
            InitializeComponent();

            Initialized = false;
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            SetDefaults();
            Init();
            Initialized = true;
        }

        /// <summary>
        /// уровень доступа пользователя к интерфейсу
        /// </summary>
        Role.AccessMode AccessMode { get; set; }

        public static string Role { get => "[erp]corrugator_operator"; }

        /// <summary>
        /// ИД текущего станка
        /// 2 21 22
        /// </summary>
        public static int CurrentMachineId { get; set; }


        /// <summary>
        /// ИД выбранного станка, для которого нужно показать данные
        /// 2 21 22
        /// </summary>
        public static int SelectedMachineId { get; set; }
        /// <summary>
        /// Выбран текущий станок
        /// </summary>
        public static bool IsCurrentMachineSelected 
        { 
            get 
            {
                // Проверяем разрешение на управление в текущей группе
                return MachineGroups.IsMachineAllowedInGroup(
                    Central.Navigator.Address.GetLastBit(), 
                    SelectedMachineId);
            }
        }

        /// <summary>
        /// Количество первых заданий в очереди станка, которые нельзя трогать
        /// </summary>
        public static int NumberOfUntouchableTasks { get; set; }
        
        /// <summary>
        /// Текущий режим работы (группа машин)
        /// </summary>
        private string CurrentGroupMode => Central.Navigator.Address.GetLastBit();

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
        /// Таймер записи вместо тейпкаунтера
        /// </summary>
        private DispatcherTimer UpdateInsteadOfTapeCounterTimer { get; set; }
        private DispatcherTimer FrequentUpdateInsteadOfTapeCounterTimer { get; set; }

        /// <summary>
        /// Статус пожара на объекте
        /// </summary>
        public string FireInPlace { get; set; }
        /// <summary>
        /// Таймер проверки пожара
        /// </summary>
        private DispatcherTimer TimerFire;

        /// <summary>
        /// Разрешен вызов UpdateInsteadOfTapeCounter
        /// </summary>
        public bool UpdateEnable { get; private set; } = false;

        /// <summary>
        /// Механизм первого обновления после старта, если разрешена работа то будет вызван UpdateInsteadOfTapeCounter
        /// </summary>
        public bool JustStarted { get; private set; } = true;

        /// <summary>
        /// Время начала смены
        /// </summary>
        public static DateTime DateShiftStart { 
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

        public bool Initialized { get; set; }


        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            AccessMode = Central.Navigator.GetRoleLevel(CorrugatorMachineOperator.Role);
            InitializeMachineParameters();
            
            CheckWhatCMButton(CurrentMachineId);

            // Информация по количеству времени выполнения заданий разной степени просроченности
            TaskQueue.OnAfterLoadQueue += UpdateTaskCounter;
            // Раскраска кнопки Применить в обычный стиль, не синий
            TaskQueue.OnAfterLoadQueue += ButtonApplyToNormal;

            TaskData.OnStatusValueChanged += ShiftData.HandleStatusChange;
            TaskData.OnIsHaveK2 += SetVisibleK2;

            //SetCurrentTime();
            GetPlaceName();
            FireStatus.Visibility = Visibility.Collapsed;
        }

        private void TaskData_OnChangeData(int avgSpeed)
        {
            if(UpdateEnable)
            {
                if (JustStarted)
                {
                    JustStarted = false;
                    UpdateInsteadOfTapeCounter();
                }
            }
        }
        
        /// <summary>
        /// Инициализация параметров машины на основе группы
        /// </summary>
        private void InitializeMachineParameters()
        {
            var (defaultMachine, _, untouchableTasks) = MachineGroups.GetGroupConfig(CurrentGroupMode);
            CurrentMachineId = defaultMachine;
            NumberOfUntouchableTasks = untouchableTasks;
        }

        /// <summary>
        /// Инициализация компонентов
        /// </summary>
        public void Init()
        {
            IsUpdateInsteadOfTapeCounterBusy = false;
            bool resume = false;
            string host = Dns.GetHostName();
            IPAddress[] addresses = Dns.GetHostAddresses(host);
            foreach (IPAddress address in addresses)
            {
                // подозреваю что это машина на которой запускается тейпкаунтер обычно "192.168.15.88"
                if (address.ToString() == "192.168.15.88")
                {
                    resume = true;
                }
            }

            // если это не отладка и запускаем на станке, включается lpack erp тейпкаунтер 
            if (!Central.DebugMode && resume)
            {
                // включился таймер, но он отработает лишь через минуту, необходимо восстановить очередь сразу же после запуска
                ButtonInsteadOfTapeCounterAction();
            }
            

            SetTimerFire(10);
            SetFastTimer(5);
            SetSlowTimer(60);
            SetCheckBlockTimer(600);

            TaskQueue.Init();
            IdleGrid.Init();
            ShiftData.Init();
            TaskData.Init();
            ProfileSpeedGrid.Init();

            StorageStateChart.Init();
            SpeedChart.Init();
            RawGrid1.Init(1);
            RawGrid2.Init(2);
            RawGrid3.Init(3);
            RawGrid4.Init(4);
            RawGrid5.Init(5);

            List<Button> buttons = UIUtil.GetVisualChilds<Button>(this.Content as DependencyObject);
            if (buttons != null && buttons.Count > 0)
            {
                foreach (var button in buttons)
                {
                    var buttonTagList = UIUtil.GetTagList(button);
                    var accessMode = Acl.FindTagAccessMode(buttonTagList);
                    if (accessMode > AccessMode)
                    {
                        button.IsEnabled = false;
                    }
                }
            }

            TaskData.OnChangeData += TaskData_OnChangeData;
            SpeedChart.OnAfterLoad += SpeedChart_OnAfterLoad;
            TaskQueue.OnItemChange += TaskQueue_OnItemChange;
            TaskQueue.OnSelectedTasksChange += TaskQueue_OnSelectedTasksChange;

            // Увеличение грида
            IdleGrid.LayoutTransform = new ScaleTransform(1.3, 1.3);
        }

        private void TaskQueue_OnItemChange(Dictionary<string, string> item)
        {
            bool result = false;

            if (AccessMode >= Common.Role.AccessMode.FullAccess)
            {
                if (item != null)
                {
                    if (item.CheckGet("_ROWNUMBER").ToInt() > CorrugatorMachineOperator.NumberOfUntouchableTasks)
                    {
                        result = true;
                    }
                }

                ButtonDelete.IsEnabled = result;
                
                UpdateDeleteSelectedButtonState();
            }
        }

        private void TaskQueue_OnSelectedTasksChange()
        {
            UpdateDeleteSelectedButtonState();
        }

        private void UpdateDeleteSelectedButtonState()
        {
            if (AccessMode >= Common.Role.AccessMode.FullAccess)
            {
                bool hasSelectedTasks = false;
                if (TaskQueue.SelectedTaskIndexes != null)
                {
                    hasSelectedTasks = TaskQueue.SelectedTaskIndexes.Values.Any(x => x == "true" || x == "1");
                }
                ButtonDeleteSelected.IsEnabled = hasSelectedTasks && IsCurrentMachineSelected;
            }
        }

        private void SpeedChart_OnAfterLoad()
        {
            ButtonUpdate.IsEnabled = true;
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
                Central.Stat.TimerAdd("CorrugatorMachineOperator_SetFastTimer", row);
            }

            FastTimer.Tick += (s, e) =>
            {
                TaskData.LoadData();
                TaskQueue.LoadItems();
                ProfileSpeedGrid.LoadItems();
                CheckDefects();
                CheckFirstTask();

                if ((TaskParameters.statusSendTask == 5 && SelectedMachineId == 2) ||
                    (TaskParameters.statusSendTask == 5 && SelectedMachineId == 21))
                {
                    SaveEditOutBhs.Visibility = Visibility.Visible;
                    CancelEditOutBhs.Visibility = Visibility.Visible;
                    EditMode.Visibility = Visibility.Collapsed;
                }
                else if (SelectedMachineId == 2 || SelectedMachineId == 21)
                {
                    SaveEditOutBhs.Visibility = Visibility.Collapsed;
                    CancelEditOutBhs.Visibility = Visibility.Collapsed;
                    EditMode.Visibility = Visibility.Visible;
                }
                else
                {
                    SaveEditOutBhs.Visibility = Visibility.Collapsed;
                    CancelEditOutBhs.Visibility = Visibility.Collapsed;
                    EditMode.Visibility = Visibility.Collapsed;
                }
            };

            FastTimer.Start();
        }
        /// <summary>
        /// Таймер медленного обновления
        /// </summary>
        public async void SetSlowTimer(int autoUpdateInterval)
        {
            // Обновляться должно в начале каждой минуты
            int secondsBeforeFirstUpdate = 65 - DateTime.Now.Second;
            await Task.Delay(secondsBeforeFirstUpdate * 1000);

            SlowTimer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, autoUpdateInterval)
            };

            {
                var row = new Dictionary<string, string>();
                row.CheckAdd("TIMEOUT", autoUpdateInterval.ToString());
                row.CheckAdd("DESCRIPTION", "");
                Central.Stat.TimerAdd("CorrugatorMachineOperator_SetSlowTimer", row);
            }

            SlowTimer.Tick += (s, e) =>
            {
                UpdateData();
                //SetCurrentTime();
            };

            SlowTimer.Start();
        }
        /// <summary>
        /// Таймер проверки блокировок 
        /// </summary>
        public async void SetCheckBlockTimer(int autoUpdateInterval)
        {
            CheckBlockTimer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, autoUpdateInterval)
            };

            {
                var row = new Dictionary<string, string>();
                row.CheckAdd("TIMEOUT", autoUpdateInterval.ToString());
                row.CheckAdd("DESCRIPTION", "");
                Central.Stat.TimerAdd("CorrugatorMachineOperator_SetCheckBlockTimer", row);
            }

            CheckBlockTimer.Tick += (s, e) =>
            {
                IsUpdateInsteadOfTapeCounterBusy = false;
                TaskQueue.IsQueueLoading = false;
            };

            CheckBlockTimer.Start();
        }

        public void SetUpdateInsteadOfTapeCounterTimer(int autoUpdateInterval)
        {
            if (UpdateInsteadOfTapeCounterTimer != null)
            {
                UpdateInsteadOfTapeCounterTimer.Stop();
            }

            UpdateInsteadOfTapeCounterTimer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, autoUpdateInterval)
            };

            {
                var row = new Dictionary<string, string>();
                row.CheckAdd("TIMEOUT", autoUpdateInterval.ToString());
                row.CheckAdd("DESCRIPTION", "");
                Central.Stat.TimerAdd("CorrugatorMachineOperator_SetUpdateInsteadOfTapeCounterTimer", row);
            }

            UpdateInsteadOfTapeCounterTimer.Tick += (s, e) =>
            {
                UpdateInsteadOfTapeCounter();
            };

            UpdateInsteadOfTapeCounterTimer.Start();
        }

        public void SetFrequentUpdateInsteadOfTapeCounterTimer(int autoFrequentUpdateInterval)
        {
            if (FrequentUpdateInsteadOfTapeCounterTimer != null)
            {
                FrequentUpdateInsteadOfTapeCounterTimer.Stop();
            }

            FrequentUpdateInsteadOfTapeCounterTimer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, autoFrequentUpdateInterval)
            };

            {
                var row = new Dictionary<string, string>();
                row.CheckAdd("TIMEOUT", autoFrequentUpdateInterval.ToString());
                row.CheckAdd("DESCRIPTION", "");
                Central.Stat.TimerAdd("CorrugatorMachineOperator_SetFrequentUpdateInsteadOfTapeCounterTimer", row);
            }

            FrequentUpdateInsteadOfTapeCounterTimer.Tick += (s, e) =>
            {
                FrequentUpdateInsteadOfTapeCounter();
            };

            FrequentUpdateInsteadOfTapeCounterTimer.Start();
        }

        public bool IsUpdateInsteadOfTapeCounterBusy { get; set; }
        
        

        public int MinutesWithZeroSpeedCount { get; set; } = 0;

        /// <summary>
        /// запускается каждую минуту при включенном аналоге тейпкаунтера
        /// TODO: Удаление данного метода и все что с ним связанно (после перехода на новый тейпкаунтер)
        /// </summary>
        public async void UpdateInsteadOfTapeCounter()
        {
            // проверка на повторный запуск, не уверен что она нужна
            if (!IsUpdateInsteadOfTapeCounterBusy)
            {
                IsUpdateInsteadOfTapeCounterBusy = true;


                int countMinuteToWriteIdle = 0;
                if (TaskData.TextTaskSpeed.Text.ToInt() == 0)
                {
                    MinutesWithZeroSpeedCount++;
                }
                else
                {
                    countMinuteToWriteIdle = MinutesWithZeroSpeedCount;
                    MinutesWithZeroSpeedCount = 0;
                }

                int speedMax = TaskQueue.TaskGrid.Items.FirstOrDefault().CheckGet("SPEED").ToInt();

                /*var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID_ST", CurrentMachineId.ToString());
                    p.CheckAdd("MAX_SPEED", speedMax.ToString());
                    p.CheckAdd("MINUTES_WITH_ZERO_SPEED_COUNT", countMinuteToWriteIdle.ToString());
                }
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "CorrugatorMachineOperator");
                q.Request.SetParam("Action", "UpdateInsteadOfTapeCounter");
                q.Request.SetParams(p);

                q.Request.Timeout = 10000;
                q.Request.Attempts = 1;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });*/

                try
                {
                    var result = await LPackClientQuery.DoQueryAsync("Production", "CorrugatorMachineOperator", "UpdateInsteadOfTapeCounter", string.Empty, new Dictionary<string, string>
                    {
                        { "ID_ST", CurrentMachineId.ToString() },
                        { "MAX_SPEED", speedMax.ToString() },
                        { "MINUTES_WITH_ZERO_SPEED_COUNT", countMinuteToWriteIdle.ToString() }
                    }, 10000);
                }
                catch(Exception ex)
                {
                    CorrugatorErrors.LogError(ex);
                }

                IsUpdateInsteadOfTapeCounterBusy = false;
            }
        }

        /// <summary>
        /// вызывветмя каждые 10 секунд
        /// </summary>
        public async void FrequentUpdateInsteadOfTapeCounter()
        {
            int speedMax = TaskQueue.TaskGrid.Items.FirstOrDefault().CheckGet("SPEED").ToInt();

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_ST", CurrentMachineId.ToString());
                p.CheckAdd("MAX_SPEED", speedMax.ToString());
            }

            /// Временный акшн, записывает данные вместо тейпкаунтера, работает как джоб, но включается и выключается из клиента

            /*var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "CorrugatorMachineOperator");
            q.Request.SetParam("Action", "FrequentUpdateInsteadOfTapeCounter");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestGridAttempts;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var dsTask = ListDataSet.Create(result, "ITEMS");
                    if (dsTask.Items.Count > 0)
                    {

                    }
                }
            }*/

            try
            {
                /// FIXME весь клж перенесен в EndTask вызываемого из FosberTaskJob, после проверки необходимо полностью удалить этот файл за ненадобностью
                var result = await LPackClientQuery.DoQueryAsync("Production", "CorrugatorMachineOperator", "FrequentUpdateInsteadOfTapeCounter", string.Empty, p, 10000);
            }
            catch (Exception ex)
            {
                CorrugatorErrors.LogError(ex);
            }
        }

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            // Остановка таймеров
            FastTimer?.Stop();
            SlowTimer?.Stop();
            CheckBlockTimer?.Stop();
            TimerFire?.Stop();
            FrequentUpdateInsteadOfTapeCounterTimer?.Stop();
            UpdateInsteadOfTapeCounterTimer?.Stop();

            TaskQueue.OnAfterLoadQueue -= UpdateTaskCounter;
            TaskQueue.OnAfterLoadQueue -= ButtonApplyToNormal;
            TaskData.OnIsHaveK2 -= SetVisibleK2;
            TaskData.OnChangeData -= TaskData_OnChangeData;
            TaskData.OnStatusValueChanged -= ShiftData.HandleStatusChange;
            SpeedChart.OnAfterLoad -= SpeedChart_OnAfterLoad;
            TaskQueue.OnItemChange -= TaskQueue_OnItemChange;
            TaskQueue.OnSelectedTasksChange -= TaskQueue_OnSelectedTasksChange;

            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Production",
                ReceiverName = "",
                SenderName = "CorrugatorMachineOperator",
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
                            IdleGrid.IdleGrid.LoadItems();

                            var id = m.Message.ToInt();
                            IdleGrid.IdleGrid.SetSelectedItemId(id);

                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Обновление счетчиков длины заданий
        /// </summary>
        private void UpdateTaskCounter()
        {
            var taskLength1 = 0;
            var taskLength2 = 0;
            var taskLength3 = 0;
            var taskLength4 = 0;
            var taskLength5 = 0;

            if (TaskQueue.TaskGrid.Items != null)
            {
                foreach (var item in TaskQueue.TaskGrid.Items)
                {
                    var taskLength = item.CheckGet("LEN").ToInt();
                    var beginWork = item.CheckGet("START_BEFORE").ToDateTime();
                    double hours = (beginWork - DateTime.Now).TotalHours;

                    if (hours < 0)
                    {
                        taskLength1 += taskLength;
                    }
                    else if (hours < 2)
                    {
                        taskLength2 += taskLength;
                    }
                    else if (hours < 4)
                    {
                        taskLength3 += taskLength;
                    }
                    else{
                        taskLength4 += taskLength;
                    }

                    taskLength5 += taskLength;
                }
            }

            TaskLength1.Text = $"{taskLength1}";
            TaskLength2.Text = $"{taskLength2}";
            TaskLength3.Text = $"{taskLength3}";
            TaskLength4.Text = $"{taskLength4}";
            TaskLength5.Text = $"{taskLength5}";
        }

        /// <summary>
        /// Раскраска кнопки Применить в обычный стиль, не синий
        /// </summary>
        public void ButtonApplyToNormal()
        {
            ButtonApply.Style = (Style)ButtonApply.TryFindResource("Button");
        }

        /// <summary>
        /// Обновление всей информации, кроме графика
        /// </summary>
        private async void UpdateData()
        {
            // заблокировать кнопку обновления, разблокировка происходит в событии после загрузки графика
            ButtonUpdate.IsEnabled = false;

            // Очередь ПЗ
            TaskQueue.LoadItems();
            // Простоям
            IdleGrid.LoadItems();
            // Сырьё
            RawGrid1.LoadItems();
            RawGrid2.LoadItems();
            RawGrid3.LoadItems();
            RawGrid4.LoadItems();
            RawGrid5.LoadItems();
            // Состояние СГП и буферов
            StorageStateChart.LoadData();
            // Показатели задания
            TaskData.LoadData();
            // Показатели смены
            ShiftData.LoadData();
            // График скорости
            if (Initialized)
            {
                SpeedChart.LoadData();
            }
        }

        public void CheckFirstTask()
        {
            if (IsCurrentMachineSelected)
            {
                if (TaskData.CurentTaskId != 0)
                {
                    if (TaskQueue.TaskGrid?.Items?.Count > 0)
                    {
                        var firstTask = TaskQueue.TaskGrid.Items.FirstOrDefault();
                        var secondTask = TaskQueue.TaskGrid.Items.Find(task => task.CheckGet("_ROWNUMBER").ToInt() == 2) ?? new Dictionary<string, string>();
                        int firstTaskId = firstTask.CheckGet("ID_PZ").ToInt();
                        int secondTaskId = secondTask.CheckGet("ID_PZ").ToInt();
                        // если задание сменилось - обновляем таблицу из бд
                        if (TaskData.CurentTaskId != firstTaskId)
                        {
                            if (TaskData.CurentTaskId == secondTaskId)
                            {
                                TaskQueue.LoadItems();
                                ButtonApply.Style = (Style)IsCM1Button.TryFindResource("Button");
                                //jmljnl;k
                            }
                        }
                    }
                }
            }
        }

        public async void CheckDefects()
        {
            var ds = await Defects.ListDefects();
            if (ds.Items.Count > 0)
            {
                ButtonDefectReasons.Style = (Style)ButtonDefectReasons.TryFindResource("FButtonError");
            }
            else
            {
                ButtonDefectReasons.Style = (Style)ButtonDefectReasons.TryFindResource("Button");
            }
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
                Central.Stat.TimerAdd("CorrugatorMachineOperator_SetTimerFire", row);
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
                    FireAlarmButton.Style = (Style)FireAlarmButton.TryFindResource("ButtonFireActive");
                    FireInPlace = CurrentPlaceName;

                    FireAlarmImage.Visibility = Visibility.Visible;

                    UpdateFireStatus(CurrentPlaceName);

                }
            }
            else if (FireInPlace == CurrentPlaceName)
            {
                FireInPlace = null;
                FireStatus.Visibility = Visibility.Hidden;
                FireAlarmButton.Style = (Style)FireAlarmButton.TryFindResource("ButtonFire");

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
                p.Add("FIRE_NAME", "FIRE");

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
            try
            {
                var q = await LPackClientQuery.DoQueryAsync("Production", "IndustrialWaste", "ListFire", "ITEMS", new Dictionary<string, string>
                {
                    { "FIRE_NAME", "FIRE_BDM1" }
                });

                if (q.Answer.Status == 0)
                {
                    
                    if (q.Answer.QueryResult != null)
                    {
                        var ds = q.Answer.QueryResult;

                        if (ds.Items!= null)
                        {
                            if (ds.Items.Count > 0)
                            {
                                FireInPlace = ds.Items[0]["PARAM_VALUE"];

                                if (FireInPlace != null && FireInPlace != "null")
                                {

                                    FireStatus.Text = "Пожар! " + FireInPlace;
                                    FireStatus.Visibility = Visibility.Visible;

                                    foreach (Window window in Application.Current.Windows)
                                    {
                                        if (window.Content is FireInformatio)
                                        {
                                            window.Close();
                                        }
                                    }

                                    var fire = new FireInformatio();
                                    fire.ShowWindow(FireInPlace);
                                }
                                else
                                {
                                    FireStatus.Visibility = Visibility.Collapsed;
                                }
                            }
                        }
                    }
                }
                
                var qb = await LPackClientQuery.DoQueryAsync("Production", "CorrugatorMachineOperator", "GetInfoAboutStatus", "MACHINE_INFO");

                if (qb.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(qb.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "MACHINE_INFO");

                        foreach (var item in ds.Items)
                        {
                            if (item.CheckGet("MACHINE_ID").ToInt() == SelectedMachineId)
                            {
                                if (item.CheckGet("SOURCE5_READ").ToInt() != 1 ||
                                    ((DateTime.Now - item.CheckGet("SOURCE5_ONDATE").ToDateTime()).TotalSeconds > 30) || 
                                    item.CheckGet("DATA_ACTUAL").ToInt() != 1)
                                {
                                    StatusReadFile.Text = $"НЕТ ДАННЫХ АСУТП ID:{SelectedMachineId}";
                                    StatusReadFile.Foreground = Brushes.Red;
                                }
                                else
                                {
                                    StatusReadFile.Text = $"Данные АСУТП получены. Последнее обновление {item.CheckGet("SOURCE5_ONDATE")}; ID:{SelectedMachineId}";
                                    StatusReadFile.Foreground = Brushes.Green;
                                }

                                if (SelectedMachineId == 22)
                                {
                                    StatusReadFile.Text = "";
                                }
                            }
                        }
                    }
                }
                else
                {
                    StatusReadFile.Text = "НЕ УДАЛОСЬ ПОЛУЧИТЬ ДАННЫЕ С СЕРВЕРА";
                    StatusReadFile.Foreground = Brushes.Red;
                }
                
                // Добавим сюда еще обновление самого грида со списком заданий
                // TaskQueue.LoadItems();
            }
            catch(Exception ex)
            {
                CorrugatorErrors.LogError(ex);
            }
        }

        /// <summary>
        /// Название станка для записи в место пожара
        /// </summary>
        public async void GetPlaceName()
        {
            /*var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_ST", CurrentMachineId.ToString());
            }
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "CorrugatorMachineOperator");
            q.Request.SetParam("Action", "GetPlaceName");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestGridAttempts;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "PLACE");
                    if (ds.Items.Count > 0)
                    {
                        CurrentPlaceName = ds.Items[0].CheckGet("NAME");
                    }
                }
            }*/

            try
            {
                var q = await LPackClientQuery.DoQueryAsync("Production", "CorrugatorMachineOperator", "GetPlaceName", "PLACE",
                    new Dictionary<string, string>
                    {
                    { "ID_ST", CurrentMachineId.ToString() }
                    }

                    );

                if (q.Answer.Status == 0)
                {
                    
                    if (q.Answer.QueryResult != null)
                    {
                        var ds = q.Answer.QueryResult;
                        if (ds.Items.Count > 0)
                        {
                            CurrentPlaceName = ds.Items[0].CheckGet("NAME");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CorrugatorErrors.LogError(ex);
            }
        }
        #endregion

        /// <summary>
        /// Выбор гофроагрегата, на котором показывать очередь заданий
        /// </summary>
        private async void CheckWhatCMButton(int machineId)
        {
            // Проверка на то включен ли у них режим редактирования
            if ((SelectedMachineId == 2 && TaskParameters.statusSendTask == 5) ||
                (SelectedMachineId == 21 && TaskParameters.statusSendTask == 5))
            {
                var dialog = new DialogWindow(
                    $"У вас включен режим редактирования для станка {(SelectedMachineId == 2 ? "БХС-1" : "БХС-2")}" +
                    $"\nВы действительно хотите переключиться на другой станок? Это приведет к потере не сохраненных данных!",
                    $"Предупреждение", "", DialogWindowButtons.YesNo);
                
                if (dialog.ShowDialog() != true)
                {
                    return;
                }
                else
                {
                    // Произвести отмену состояния редактирования
                    SaveConfigurationOptionsUpdate(SelectedMachineId, 6);
                    TaskParameters.statusSendTask = 6;
                }
            }
            
            SelectedMachineId = machineId;

            IsCM1Button.Style = (Style)IsCM1Button.TryFindResource("Button");
            IsCM2Button.Style = (Style)IsCM2Button.TryFindResource("Button");
            IsCM3Button.Style = (Style)IsCM3Button.TryFindResource("Button");
            
            //ButtonSendData.Visibility = (machineId == 22) ? Visibility.Collapsed : Visibility.Visible;

            //кнопку активного раската в активное состояние
            switch (machineId)
            {
                case 2:
                    IsCM1Button.Style = (Style)IsCM1Button.TryFindResource("FButtonPrimary");
                    ButtonGetFosberData.Visibility = Visibility.Collapsed;
                    
                    
                    SaveEditOutBhs.Visibility = Visibility.Collapsed;
                    CancelEditOutBhs.Visibility = Visibility.Collapsed;
                    EditMode.Visibility = Visibility.Visible;
                    break;

                case 21:
                    IsCM2Button.Style = (Style)IsCM2Button.TryFindResource("FButtonPrimary");
                    ButtonGetFosberData.Visibility = Visibility.Collapsed;
                    
                    
                    SaveEditOutBhs.Visibility = Visibility.Collapsed;
                    CancelEditOutBhs.Visibility = Visibility.Collapsed;
                    EditMode.Visibility = Visibility.Visible;
                    
                    break;
                
                case 22:
                    IsCM3Button.Style = (Style)IsCM3Button.TryFindResource("FButtonPrimary");
                    ButtonGetFosberData.Visibility = Visibility.Visible;
                    
                    SaveEditOutBhs.Visibility = Visibility.Collapsed;
                    CancelEditOutBhs.Visibility = Visibility.Collapsed;
                    EditMode.Visibility = Visibility.Collapsed;
                    break;
            }

            bool isEnabled = true;
            if (!IsCurrentMachineSelected)
            {
                isEnabled = false;
            }


            if (AccessMode >= Common.Role.AccessMode.FullAccess)
            {
                ButtonDeleteTask.IsEnabled = isEnabled;
                ButtonDeleteSelected.IsEnabled = isEnabled;
                // ButtonUP.IsEnabled = isEnabled; TODO: Временное отключение до фикса 
                ButtonUp.IsEnabled = isEnabled;
                ButtonDown.IsEnabled = isEnabled;
                ButtonChangeRolls.IsEnabled = isEnabled;
                ButtonApply.IsEnabled = isEnabled;
            }


            if (Initialized)
            {
                UpdateData();
            }
            
            ProfileSpeed.CurrentMachineId = machineId;
        }

        //public void SetCurrentTime()
        //{
        //    CurrentTime.Text = $"{(DateTime.Now.Hour):00}:{DateTime.Now.Minute:00}";
        //}

        private void LabelK2_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            new ProductionComplectationCorrugatorInStockInterface();
        }

        /// <summary>
        /// Снятие галочек со всех чекбоксов грида очереди заданий
        /// </summary>
        public void ClearAllCheckBoxes()
        {
            TaskQueue.SelectedTaskIndexes = new Dictionary<string, string>();
            TaskQueue.NotifySelectedTasksChanged();
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

                case Key.Home:
                    TaskQueue.TaskGrid.SelectRowFirst();
                    e.Handled = true;
                    break;

                case Key.End:
                    TaskQueue.TaskGrid.SelectRowLast();
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
            Central.ShowHelp("/doc/l-pack-erp/production/CorrugatorMachineOperator");
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void ButtonUpdate_Click(object sender, RoutedEventArgs e)
        {
            UpdateGrid();
        }

        private void UpdateGrid()
        {
            TaskQueue.ClearDeletetTask();

            ClearAllCheckBoxes();
            UpdateData();
        }

        private void ButtonListTaskTotal_Click(object sender, RoutedEventArgs e)
        {
            var listTaskTotal = new ListTaskTotal();
            listTaskTotal.OnAfterAddTaskIntoQueue += TaskQueue.TaskGrid.LoadItems;
            listTaskTotal.Show();
        }
        private void ButtonDeleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (TaskQueue.SelectedTaskItem!=null)
            {
                if (CanMakeAction(true))
                {
                    //List<int> listOrders = new List<int>();
                    //TaskQueue.SelectedTaskIndexes.ForEach(x => listOrders.Add(x.Key.ToInt()));
                    //var list = string.Join(",", listOrders);

                    //var d = new DialogWindow($"Удалить вбыранное задание {TaskQueue.SelectedTaskItem.CheckGet("FOSBER_NUM")}?", "Удаление задания из очереди", "", DialogWindowButtons.YesNo);
                    //d.ShowDialog();
                    //if (d.DialogResult == true)
                    {
                        TaskQueue.DeleteTask(0);
                        ButtonApply.Style = (Style)ButtonApply.TryFindResource("FButtonPrimary");
                    }
                }
            }
        }

        private void ButtonDeleteSelectedTasks_Click(object sender, RoutedEventArgs e)
        {
            if (CanMakeAction(true))
            {
                TaskQueue.DeleteSelectedTasks(0);
                // Не устанавливаем синий стиль, так как DeleteSelectedTasks уже обновляет грид
                // и вызывает ButtonApplyToNormal через OnAfterLoadQueue
            }
        }
        private void ButtonChangeRolls_Click(object sender, RoutedEventArgs e)
        {
            ClearAllCheckBoxes();
            TaskQueue.ChangeSelectedRolls();
        }

        private bool CanMakeAction(bool message)
        {
            if(!IsCurrentMachineSelected)
            {
                return false;
            }

            return true;
        }

        private void ButtonTaskUP_Click(object sender, RoutedEventArgs e)
        {
            if (CanMakeAction(true))
            {
                TaskQueue.MoveTask("UP");
                ButtonApply.Style = (Style)ButtonApply.TryFindResource("FButtonPrimary");
            }
        }
        private void ButtonTaskUp_Click(object sender, RoutedEventArgs e)
        {
            if (CanMakeAction(true))
            {
                TaskQueue.MoveTask("up");
                ButtonApply.Style = (Style)ButtonApply.TryFindResource("FButtonPrimary");
            }
        }
        private void ButtonTaskDown_Click(object sender, RoutedEventArgs e)
        {
            if (CanMakeAction(true))
            {
                TaskQueue.MoveTask("down");
                ButtonApply.Style = (Style)ButtonApply.TryFindResource("FButtonPrimary");
            }
        }

        private void ButtonApply_Click(object sender, RoutedEventArgs e)
        {
            if (CanMakeAction(true))
            {
                bool resume = true;
                if (TaskData.ChangeTaskSeconds < 6)
                {
                    var d = new DialogWindow($"До смены задания осталось менее 5 секунд, необходимо дождаться смены задания. Возможно, после сохранения нужно будет проверить первый заказ на выполнение. Продолжить сохранение?", "Сохранение очереди", "", DialogWindowButtons.YesNo);
                    d.ShowDialog();
                    if (d.DialogResult == true)
                    {

                    }
                    else
                    {
                        resume = false;
                    }
                }

                if (resume)
                {
                    ClearAllCheckBoxes();
                    TaskQueue.SaveQueue();
                    ButtonApply.Style = (Style)ButtonApply.TryFindResource("Button");
                    TaskQueue.LoadItems(); // Обновление состояние чекбоксов после применения
                }
            }
        }

        /// <summary>
        /// обновление методов работы с выбранной в гриде строкой
        /// </summary>
        public void UpdateTaskGridActions(Dictionary<string, string> selectedItem)
        {

        }

        private void ScheduledRepairsShow()
        {
            var scheduledRepairs = new ScheduledRepairs();
            scheduledRepairs.Show();
        }
        private void ProblemPalletsShow()
        {
            var problemPallets = new ProblemPallets();
            problemPallets.Edit();
        }
        private void DefectsShow()
        {
            var defects = new Defects();
            defects.Edit();
        }
        private void LogbookShow()
        {
            var logbook = new Logbook();
            logbook.Show();
        }

        private void ButtonProblemPallets_Click(object sender, RoutedEventArgs e)
        {
            ProblemPalletsShow();
        }
        private void ButtonScheduledRepairs_Click(object sender, RoutedEventArgs e)
        {
            ScheduledRepairsShow();
        }
        private void ButtonDefectReasons_Click(object sender, RoutedEventArgs e)
        {
            DefectsShow();
        }
        private void ButtonLogbook_Click(object sender, RoutedEventArgs e)
        {
            LogbookShow();
        }

        private void ButtonCheckWhatCM_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button).Name == "IsCM1Button")
            {
                CheckWhatCMButton(2);
            }
            else if ((sender as Button).Name == "IsCM2Button")
            {
                CheckWhatCMButton(21);
            }
            else if ((sender as Button).Name == "IsCM3Button")
            {
                CheckWhatCMButton(22);
            }
            else if ((sender as Button).Name == "IsCM4Button")
            {
                CheckWhatCMButton(23);
            }
        }
        private void ButtonInsteadOfTapeCounter_Click(object sender, RoutedEventArgs e)
        {
            ButtonInsteadOfTapeCounterAction();
        }

        /// <summary>
        /// нажатие включения выключения аналога тейпкаунтера
        /// </summary>
        private void ButtonInsteadOfTapeCounterAction()
        {
            // FIXME: сделано для отладки, включение разрешено запретим отключение

            if (ButtonInsteadOfTapeCounter.Style == (Style)ButtonInsteadOfTapeCounter.TryFindResource("Button"))
            {
                ButtonInsteadOfTapeCounter.Style = (Style)ButtonInsteadOfTapeCounter.TryFindResource("FButtonPrimary");
                SetUpdateInsteadOfTapeCounterTimer(60);
                SetFrequentUpdateInsteadOfTapeCounterTimer(10);

                UpdateEnable = true;
            }
            else
            {
                // запретим выключение

                //ButtonInsteadOfTapeCounter.Style = (Style)ButtonInsteadOfTapeCounter.TryFindResource("Button");
                //UpdateInsteadOfTapeCounterTimer.Stop();
                //FrequentUpdateInsteadOfTapeCounterTimer.Stop();

                //UpdateEnable = false;
            }
        }

        private async void ButtonGetFosberData_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedMachineId == CurrentMachineId)
            {
                // Fosber 22
                if (SelectedMachineId == 22)
                {
                    var fc = new FosberConnector();
                    var sequence = fc.GetSequence();
                    string msg = string.Join("\n", sequence);
                    var sequenceWindow = new DialogWindow(msg, "Загрузить очередь из фосбера в базу?", "", DialogWindowButtons.YesNo);
                    if (sequenceWindow.ShowDialog() == true)
                    {
                        SaveDBQueue(sequence, 22);
                    }
                }
                else
                {
                    var sequenceWindow = new DialogWindow("Вы хотите записать данные станка в базу?", $"Синхронизация БХС с кодом {SelectedMachineId}", "", DialogWindowButtons.YesNo);
                    if (sequenceWindow.ShowDialog() == true)
                    {
                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Production");
                        q.Request.SetParam("Object", "CorrugatorMachineOperator");
                        q.Request.SetParam("Action", "SetSynhronizationState");

                        q.Request.SetParam("ID_ST", SelectedMachineId.ToString());

                        q.DoQuery();

                        string log = "";

                        if (q.Answer.Status == 0)
                        {
                            var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                            if (result != null)
                            {
                                var ds = ListDataSet.Create(result, "ITEMS");
                                if (ds!= null)
                                {
                                    if (ds.Items != null)
                                    {
                                        var first = ds.Items.FirstOrDefault();

                                        if (first != null)
                                        {
                                            var res = first.CheckGet("RESULT").ToInt();

                                            if(res!=2)
                                            {
                                                sequenceWindow = new DialogWindow("Синхронизация не удалась", $"Код ответа {res}", "", DialogWindowButtons.YesNo);
                                                if (sequenceWindow.ShowDialog() == true)
                                                {

                                                }
                                            }
                                            else
                                            {
                                                // обновим 
                                                UpdateGrid();
                                            }
                                        }
                                    }

                                }
                            }
                        }


                    }
                }
            }
            else
            {
                var sequenceWindow = new DialogWindow("Вы не можете синхронизировать данные на выбранном гофроагрегате", $"Для работы задан гофроагрегат {CurrentMachineId}", "", DialogWindowButtons.YesNo);
                if (sequenceWindow.ShowDialog() == true)
                {

                }
            }
        }

        /// <summary>
        /// Функция для взятия данных с бхсов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SynchronizationBhsButton_Click(object sender, RoutedEventArgs e)
        {
            var btn= (Button)sender;
            var nameBtn = btn.Name;

            OperationBhsSync(nameBtn);
        }

        private async void OperationBhsSync(string nameBtn)
        {
            switch (nameBtn)
            {
                case "EditMode":
                    SaveEditOutBhs.Visibility = Visibility.Visible;
                    CancelEditOutBhs.Visibility = Visibility.Visible;
                    EditMode.Visibility = Visibility.Collapsed;
                    
                    SaveConfigurationOptionsUpdate(SelectedMachineId, 5);
                    
                    break;
                case "SaveEditOutBhs":
                    SaveEditOutBhs.Visibility = Visibility.Collapsed;
                    CancelEditOutBhs.Visibility = Visibility.Collapsed;
                    EditMode.Visibility = Visibility.Visible;
                    
                    SaveConfigurationOptionsUpdate(SelectedMachineId, 1);
                    
                    break;
                case "CancelEditOutBhs":
                    SaveEditOutBhs.Visibility = Visibility.Collapsed;
                    CancelEditOutBhs.Visibility = Visibility.Collapsed;
                    EditMode.Visibility = Visibility.Visible;
                    
                    SaveConfigurationOptionsUpdate(SelectedMachineId, 6);
                    
                    break;
            }
        }


        /// <summary>
        /// Запрос на обновление ключа в бд
        /// </summary>
        private async void SaveConfigurationOptionsUpdate(int machineId, int value)
        {
            TaskParameters.statusSendTask = value;
            
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "CorrugatorMachineOperator");
            q.Request.SetParam("Action", "SaveConfOptions");
            q.Request.SetParam("ID_ST", machineId.ToString());
            q.Request.SetParam("VALUE", value.ToString());
            
            q.DoQuery();
            
            if (q.Answer.Status == 0)
            {
                
            } 
            else
            {
                var dialog = new DialogWindow("Не удалось сохранить настройки", $"Код ошибки {q.Answer.Error.Code}", "", DialogWindowButtons.YesNo);
                dialog.ShowDialog();
            }
        }


        /// <summary>
        /// Сохранение очереди заданий в БД
        /// </summary>
        private void SaveDBQueue(List<int> queue, int machineId)
        {
            var taskQueue = new List<Dictionary<string, string>>();
            bool resume = false;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "TaskQueue");
            q.Request.SetParam("Action", "SaveQueueFromCM");

            q.Request.SetParam("ID_ST", machineId.ToString());
            q.Request.SetParam("TASK_QUEUE", JsonConvert.SerializeObject(queue));
            q.Request.SetParam("IS_ID_FOSBER", (machineId == 22 ? 1 : 0).ToString());


            q.DoQuery();

            string log = "";

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds.Items.Count > 0)
                    {
                        resume = ds.Items[0].CheckGet("RESUME").ToBool();
                        if (resume)
                        {
                            log = $"Очередь заданий сохранена в БД успешно";
                        }
                        else
                        {
                            log = $"При сохранении очереди заданий БД произошла ошибка";
                        }
                    }
                }
            }

            if (!resume)
            {
                var s = $"Error: List. Code=[{q.Answer.Error.Code}] Message=[{q.Answer.Error.Message}] Description=[{q.Answer.Error.Description}]\n{log}";

                var errorWindow = new DialogWindow(s, "Загрузка не удалась", "", DialogWindowButtons.OK);
                errorWindow.ShowDialog();
            }
        }
        
        /// <summary>
        /// Для отображения скорости по профилю
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void SpeedAnyProfile_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;

            if (ProfileSpeed.StateButton == 0)
            {
                IdleBorderGrid.Visibility = Visibility.Collapsed;
                SpeedProfileBorderGrid.Visibility = Visibility.Visible;
                btn.Content = "Скрыть скорость по профилю";
                
                btn.Style = (Style)btn.TryFindResource("FButtonPrimary");
                
                ProfileSpeed.StateButton = 1;
            } 
            else if (ProfileSpeed.StateButton == 1)
            {
                IdleBorderGrid.Visibility = Visibility.Visible;
                SpeedProfileBorderGrid.Visibility = Visibility.Collapsed;
                btn.Content = "Скорость по профилю";
                
                btn.Style = (Style)btn.TryFindResource("Button");
                
                ProfileSpeed.StateButton = 0;
            }
        }
        
        public void SetVisibleK2(bool isHaveK2)
        {
            if (isHaveK2)
            {
                LabelK2.Visibility = Visibility.Visible;
            }
            else
            {
                LabelK2.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowOldIdle_Checked(object sender, RoutedEventArgs e)
        {
            IdleList.ShowOld = true;
        }

        private void ShowOldIdle_Unchecked(object sender, RoutedEventArgs e)
        {
            IdleList.ShowOld = false;
        }
    }

    /// <summary>
    /// ДЛЯ ОТЛАДКИ
    /// </summary>
    class FosberConnector
    {
        public FosberConnector()
        {
            //FosberIP = "192.168.21.154";
            FosberIP = "192.168.15.86";
            FosberPort = 60000;
            FosberClientTimeout = 5000;
        }
        /// <summary>
        /// ИД текущего станка
        /// </summary>
        public int CurrentMachineId { get; set; }

        public string FosberIP { get; set; }
        public int FosberPort { get; set; }
        public int FosberClientTimeout { get; set; }

        public List<int> GetSequence()
        {
            var sequence = new List<int>();
            string fosberRespond = SendRequestToFosber("LD0000001");

            if (fosberRespond?.Length >= 4)
            {
                string answerCode = fosberRespond.Substring(1, 2);
                if (answerCode == "OK")
                {
                    for (int i = 10; i < fosberRespond.Length; i += 6)
                    {
                        string numStr = fosberRespond.Substring(i, 4);
                        int num = numStr.ToInt();
                        if (num != 0)
                        {
                            sequence.Add(num);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            return sequence;
        }

        /// <summary>
        /// Конвертация строки в формат для фосбера
        /// </summary>
        public string FormatToFosber(string s)
        {
            // вычисление контрольной суммы
            int checkSum = 2;
            for (int i = 0; i < s.Length; i++)
            {
                int num = (int)s[i];
                checkSum ^= num;
            }
            // конвертация целого числа в шестнадцатеричный формат, 2 символа
            string hexCheckSum = checkSum.ToString("X2");

            // сборка итоговой строки для обращения к фосберу
            string result = (char)2 + s + hexCheckSum + (char)3;
            return result;
        }

        /// <summary>
        /// Отправка запроса на фосбер
        /// </summary>
        /// <param name="requestMessage"> Строка запроса </param>
        private string SendRequestToFosber(string requestMessage)
        {
            string respondMessage = "";
            if (!requestMessage.IsNullOrEmpty())
            {
                using (var FosberUdpClient = new UdpClient(FosberIP, FosberPort))
                {
                    FosberUdpClient.Client.ReceiveTimeout = FosberClientTimeout;
                    FosberUdpClient.Client.SendTimeout = FosberClientTimeout;

                    string formattedRequestMessage = FormatToFosber(requestMessage);
                    // преобразуем отправляемые данные в массив байтов
                    byte[] data = Encoding.UTF8.GetBytes(formattedRequestMessage);
                    // определяем конечную точку для отправки данных
                    IPEndPoint remotePoint = new IPEndPoint(IPAddress.Parse(FosberIP), FosberPort);
                    // отправляем данные
                    int bytes = FosberUdpClient.Send(data, data.Length);

                    // получаем данные
                    var result = FosberUdpClient.Receive(ref remotePoint);
                    // преобразуем полученные байты в строку
                    respondMessage = Encoding.UTF8.GetString(result);
                }
            }
            // возвращаем полученную строку
            return respondMessage;
        }
    }
}
