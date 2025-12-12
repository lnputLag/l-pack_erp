using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Production.Corrugator.CorrugatorMachineOperator.Frames;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Client.Interfaces.Production.Corrugator.CorrugatorMachineOperatorKsh
{
    /// <summary>
    /// Интерфейс оператора гофроагрегата
    /// </summary>
    /// <author>volkov_as</author>   
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

        public static string Role { get => "[erp]corrugator_operator_ksh"; }

        /// <summary>
        /// ИД текущего станка
        /// 23
        /// </summary>
        public static int CurrentMachineId { get; set; }
       

        /// <summary>
        /// ИД выбранного станка, для которого нужно показать данные
        /// 23
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
        /// Статус синхронизации 0/1
        /// </summary>
        public static int StatusSync { get; set; } = 0;

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

            SetCurrentTime();
            GetPlaceName();
            FireStatus.Visibility = Visibility.Collapsed;
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
            
            SpeedChart.OnAfterLoad += SpeedChart_OnAfterLoad;
            TaskQueue.OnItemChange += TaskQueue_OnItemChange;
            TaskQueue.OnSelectedTasksChange += TaskQueue_OnSelectedTasksChange;
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
                LoadStatusSync();
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
                Central.Stat.TimerAdd("CorrugatorMachineOperator_SetSlowTimer", row);
            }

            SlowTimer.Tick += (s, e) =>
            {
                UpdateData();
                SetCurrentTime();
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
        
        public bool IsUpdateInsteadOfTapeCounterBusy { get; set; }
        
        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            // Остановка таймеров
            FastTimer?.Stop();
            SlowTimer?.Stop();
            TimerFire?.Stop();

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
                                ButtonApply.Style = (Style)IsJSButton.TryFindResource("Button");
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
                ButtonDefectReasons.Style = (Style)ButtonDefectReasons.TryFindResource("FButtonPrimary");
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
            TimerFire.Tick += CheckTimeStamp;
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
            if (FireInPlace == string.Empty)
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
                FireInPlace = string.Empty;
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
                p.Add("FIRE_NAME", "FIRE_KSH");

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
        /// TODO: Позже тут будет оповещение о пожаре
        private async void CheckTimeStamp(object sender, EventArgs e)
        {
            try
            {
                var q = await LPackClientQuery.DoQueryAsync("Production", "IndustrialWaste", "ListFire", "ITEMS", new Dictionary<string, string>
                {
                    { "FIRE_NAME", "FIRE_KSH" }
                });

                if (q.Answer.Status == 0)
                {
                    if (q.Answer.QueryResult != null)
                    {
                        var ds = q.Answer.QueryResult;

                        if (ds.Items != null && ds.Items.Count > 0)
                        {
                            FireInPlace = ds.Items.First().CheckGet("PARAM_VALUE");

                            if (FireInPlace != null && FireInPlace != "null" && !FireInPlace.IsNullOrEmpty())
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
            catch (Exception ex)
            {
                CorrugatorErrors.LogError(ex);
            }


            try
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Control");
                q.Request.SetParam("Object", "ConfigurationOption");
                q.Request.SetParam("Action", "Get");
                q.Request.SetParam("PARAM_NAME", "REPORT_WORK_JOB_GA_KSH");

                await Task.Run(() => q.DoQuery());

                if (q.Answer.Status == 0)
                {
                    var result = q.Answer.Data;

                    if (result != null)
                    {
                        if ((DateTime.Now - result.ToDateTime()).TotalSeconds > 30)
                        {
                            StatusReadTimeStamp.Text = $"ПРОБЛЕМЫ В РАБОТЕ ЧТЕНИЯ ДАННЫХ. ПОСЛЕДНИЕ ЧТЕНИЕ {result}";
                            StatusReadTimeStamp.Foreground = Brushes.Red;
                        }
                        else
                        {
                            StatusReadTimeStamp.Text = $"СЕРВИСЫ ЧТЕНИЯ ДАННЫХ РАБОТАЮТ - {result}";
                            StatusReadTimeStamp.Foreground = Brushes.Green;
                        }
                    }
                } 
                else
                {
                    StatusReadTimeStamp.Text = "НЕ УДАЛОСЬ ПОЛУЧИТЬ ДАННЫЕ С СЕРВЕРА";
                    StatusReadTimeStamp.Foreground = Brushes.Red;
                }

            }
            catch(Exception ex)
            {
                CorrugatorErrors.LogError(ex);
                StatusReadTimeStamp.Text = "ОШИБКА ПРИ ВЫПОЛНЕНИИ ЗАПРОСА";
                StatusReadTimeStamp.Foreground = Brushes.Red;
            }
        }

        /// <summary>
        /// Название станка для записи в место пожара
        /// </summary>
        public async void GetPlaceName()
        {
            try
            {
                var q = await LPackClientQuery.DoQueryAsync("Production", "CorrugatorMachineOperator", "GetPlaceName", "PLACE",
                    new Dictionary<string, string>
                    {
                    { "ID_ST", SelectedMachineId.ToString() }
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
            SelectedMachineId = machineId;

            IsJSButton.Style = (Style)IsJSButton.TryFindResource("Button");
            
            //кнопку активного раската в активное состояние
            switch (machineId)
            {
                case 23:
                    IsJSButton.Style = (Style)IsJSButton.TryFindResource("FButtonPrimary");
                    break;
            }

            bool isEnabled = !!IsCurrentMachineSelected;


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

        public void SetCurrentTime()
        {
            CurrentTime.Text = $"{(DateTime.Now.Hour):00}:{DateTime.Now.Minute:00}";
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
            Central.ShowHelp("/doc/l-pack-erp/production/CorrugatorMachineOperatorKsh");
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
                ButtonApply.Style = (Style)ButtonApply.TryFindResource("FButtonPrimary");
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
            if ((sender as Button).Name == "IsJSButton")
            {
                CheckWhatCMButton(23);
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

        private void ShowOldIdle_Checked(object sender, RoutedEventArgs e)
        {
            IdleList.ShowOld = true;
        }

        private void ShowOldIdle_Unchecked(object sender, RoutedEventArgs e)
        {
            IdleList.ShowOld = false;
        }

        private async Task LoadStatusSync()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "CorrugatorMachineOperatorKsh");
            q.Request.SetParam("Action", "GetStatusSync");

            await Task.Run(() => q.DoQuery());

            if (q.Answer.Status == 0)
            {
                var answer = JsonConvert.DeserializeObject<Dictionary<string, string>>(q.Answer.Data);

                StatusSync = answer.CheckGet("PARAM_VALUE").ToInt();

                switch (StatusSync)
                {
                    case 1:
                        AutoSyncControlBtn.Style = (Style)AutoSyncControlBtn.TryFindResource("FButtonPrimary");
                        break;
                    case 0:
                        AutoSyncControlBtn.Style = (Style)AutoSyncControlBtn.TryFindResource("FButtonError");
                        break;
                }
            }
        }

        /// <summary>
        /// Для управления автосинхронизацией
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private async void AutoSyncControlBtn_OnClick(object sender, RoutedEventArgs e)
        {
            StatusSync = StatusSync switch
            {
                1 => 0,
                0 => 1,
                _ => StatusSync
            };

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "CorrugatorMachineOperatorKsh");
            q.Request.SetParam("Action", "ChangeStatusSync");
            q.Request.SetParam("STATUS", StatusSync.ToString());

            await Task.Run(() => q.DoQuery());

            if (q.Answer.Status == 0)
            {
                switch (StatusSync)
                {
                    case 1:
                        AutoSyncControlBtn.Style = (Style)AutoSyncControlBtn.TryFindResource("FButtonPrimary");
                        break;
                    case 0:
                        AutoSyncControlBtn.Style = (Style)AutoSyncControlBtn.TryFindResource("FButtonError");
                        break;
                }
            }
        }
    }
}
