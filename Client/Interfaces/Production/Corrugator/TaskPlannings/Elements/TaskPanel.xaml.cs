using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
using Client.Interfaces.Main;
using System.Text.RegularExpressions;

namespace Client.Interfaces.Production.Corrugator.TaskPlannings
{
    /// <summary>
    /// Панель для гофроагрегатов
    /// <author>eletskikh_ya</author>
    /// </summary>
    public partial class TaskPanel : UserControl, ITaskPanel
    {
        private TaskPlaningDataSet.TypeStanok Stanok { get;set; }

        public event Action OnFullScreen;
        public TaskPanel(TaskPlaningDataSet.TypeStanok stanok = TaskPlaningDataSet.TypeStanok.Unknow)
        {
            InitializeComponent();

            Stanok = stanok;

            //AutoPlan.Properties.OnText = "Enabled";
            //AutoPlan.Properties.OffText = "Disabled";
        }

        event ITaskPanel.FindFormat ITaskPanel.OnHoursFilter
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        event ITaskPanel.FindFormat ITaskPanel.OnFindFormat
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        private string _Kpd = string.Empty;

        public string Kpd
        {
            get
            {
                return _Kpd;
            }
            set
            {
                //LabelKpd.Content = value;
                _Kpd = value;
                PerformanceText.Text = _Kpd;
            }
        }

        public TextBox SearchBox => SearchText;

        public string NameStanok 
        {
            get => MachineNameLabel.Content.ToString();
            set
            {
                MachineNameLabel.Content = value;
            }
        }

        public void SetPlanData(int totalLenght, TimeSpan duration)
        {
            //txt += " Всего в плане " + totalLenght.ToString() + " метров. Продолжительность " + String.Format("{0:00}:{1:00}", (int)time.TotalHours, time.Minutes);

            PlanLengthText.Text = totalLenght.ToString();
            PlanTotalTimeText.Text = String.Format("{0:00}:{1:00}", (int)duration.TotalHours, duration.Minutes);

        }
        public void SetMachineData(string CurrentProdTask, double CurrentProgress, string CurrentProgressTask, string TotalTaskLength, string AvgSpeed)
        {
            //var text = "Задание: " + CurrentProdTask.ToInt().ToString() + " выполнено " + CurrentProgressTask + " из " + ds.Items[0].CheckGet("TSK_LENGTH") + ". Средняя скорость " + ds.Items[0].CheckGet("CRT_SPEED");

            CurrentTaskText.Text = CurrentProdTask.ToInt().ToString();
            CurrentTaskProgressText.Text = CurrentProgressTask.ToInt().ToString();
            CurrentTaskLengthText.Text = TotalTaskLength;
            CurrentTaskPAvgSpeedText.Text = AvgSpeed.ToString();

            PlanTimeText.IsReadOnly = false;

            UpdateAutoPlanSettings();

        }

        bool AutoPlanFlagUpdateProcessed = false;

        private async void UpdateAutoPlanSettings()
        {
            // Проверим текущее значение флага автопланирования
            if(!AutoPlanFlagUpdateProcessed)
            {
                int code = TaskPlaningDataSet.MachineCode[Stanok];

                var q = await LPackClientQuery.DoQueryAsync("Production", "TaskPlanning", "AutoPlanGet", "ITEMS", 
                    new Dictionary<string, string>()
                    {
                        { "CODE", code.ToString() },
                    });

                if (q != null)
                {
                    if(q.Answer.Status==0)
                    {
                        if(q.Answer.QueryResult!=null)
                        {
                            if(q.Answer.QueryResult.Items.Count>0)
                            {
                                var first = q.Answer.QueryResult.Items.First();
                                var res = first.CheckGet("ID").ToInt() != 0;
                                var description = first.CheckGet("CODE");
                                var time = first.CheckGet("TIME");

                                if (description != null)
                                {
                                    if(description.Length > 2)
                                    {
                                        AutoPlan.ToolTip = description.Replace("True", "включено").Replace("False", "выключено");
                                        PlanTimeText.Text = time;
                                    }
                                }


                                if (res != AutoPlan.IsChecked)
                                {
                                    // не будем отсылать значение в базу, так как мы только что его от туда получили
                                    AutoPlanFlagUpdateProcessed = true;
                                    AutoPlan.IsChecked = res;
                                }
                            }
                        }
                    }
                }
            }
        }

        private async void AutoPlan_Checked(object sender, RoutedEventArgs e)
        {
            if (!AutoPlanFlagUpdateProcessed)
            {
                AutoPlanFlagUpdateProcessed = true;

                var Checked = AutoPlan.IsChecked == true;
                int code = TaskPlaningDataSet.MachineCode[Stanok];


                var q = await LPackClientQuery.DoQueryAsync("Production", "TaskPlanning", "AutoplanSet", "",
                    new Dictionary<string, string>()
                    {
                    { "ID_ST", ((int)Stanok).ToString() },
                    { "CODE", code.ToString() },
                    { "ENABLE", Checked ? "1" : "0" },
                    { "TIME", string.IsNullOrEmpty(PlanTimeText.Text) ? "0" : PlanTimeText.Text }
                    });

                AutoPlan.ToolTip = Checked ? "Автопланирование включено" : "Автопалнирование выключено";
            }

            AutoPlanFlagUpdateProcessed = false;
        }

        private string oldPlanTimeText = string.Empty;
        
        private void SetNewTime_Click(object sender, RoutedEventArgs e)
        {
            var text = PlanTimeText.Text;
            if (text != null && text != oldPlanTimeText)
            {
                var d = new DialogWindow(
                    $"Вы хотите изменить время расчета плана?",
                    $"Изменение время расчета на {text}ч",
                    "",
                    DialogWindowButtons.YesNoCancel);

                if (d.ShowDialog() == true)
                {
                    oldPlanTimeText = text;
                    SaveNewAutoPlanTime(text.ToInt());
                }
                else
                {
                    PlanTimeText.Text = oldPlanTimeText;
                }
            }
        }
        
        /// <summary>
        /// Сохранение нового времени для станка
        /// </summary>
        /// <param name="time">Время которое ввели в TextBox Name="PlanTimeText"</param>
        private async void SaveNewAutoPlanTime(int time)
        {
            try
            {
                int code = TaskPlaningDataSet.MachineCode[Stanok];
            
                var q = await LPackClientQuery.DoQueryAsync("Production", "TaskPlanning", "AutoplanSaveNewTime", "",
                    new Dictionary<string, string>()
                    {
                        { "CODE", code.ToString() },
                        { "TIME", time.ToString() }
                    });
                
                SetNewTime.Style = (Style)TryFindResource("Button");
            }
            catch (Exception e)
            {
                var d = new DialogWindow(
                    $"При изменении времени расчета произошла ошибка",
                    $"Ошибка",
                    "");

                d.ShowDialog();
            }
        }

        private void PlanTimeText_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            SetNewTime.Style = (Style)TryFindResource("FButtonPrimary");
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        internal void ProcessPermissions(string roleCode)
        {
            var userAccessMode = Central.Navigator.GetRoleLevel(roleCode);

            List<string> accessList = new List<string>()
            {
                AutoPlan.Tag.ToString()
            };

            var accessMode = Acl.FindTagAccessMode(accessList);

            if (accessMode > userAccessMode)
            {
                AutoPlan.IsEnabled = false;
            }
        }

        private void Toolbar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.ClickCount>1)
            {
                OnFullScreen?.Invoke();
            }
        }

        public void Update(Dictionary<string, string> item)
        {
            if (item != null)
            {
                ArticulText.Content = item.CheckGet(TaskPlaningDataSet.Dictionary.Artikul);
            }
            else
            {
                ArticulText.Content = "";
            }
        }
    }
}
