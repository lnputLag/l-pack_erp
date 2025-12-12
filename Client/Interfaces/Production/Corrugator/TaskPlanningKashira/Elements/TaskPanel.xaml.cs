using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Client.Interfaces.Main;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;

namespace Client.Interfaces.Production.Corrugator.TaskPlanningKashira
{
    /// <summary>
    /// Панель для гофроагрегатов
    /// <author>volkov_as</author>
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

        private bool AutoPlanFlagUpdateProcessed { get; set; } = false;


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
            PlanLengthText.Text = totalLenght.ToString();
            PlanTotalTimeText.Text = $"{(int)duration.TotalHours:00}:{duration.Minutes:00}";

        }
        public void SetMachineData(string CurrentProdTask, double CurrentProgress, string CurrentProgressTask, string TotalTaskLength, string AvgSpeed)
        {
            CurrentTaskText.Text = CurrentProdTask.ToInt().ToString();
            CurrentTaskProgressText.Text = CurrentProgressTask.ToInt().ToString();
            CurrentTaskLengthText.Text = TotalTaskLength;
            CurrentTaskPAvgSpeedText.Text = AvgSpeed.ToString();

            PlanTimeText.IsReadOnly = false;

            UpdateAutoPlanSettings();

        }

        private async void UpdateAutoPlanSettings()
        {
            // Проверим текущее значение флага автопланирования
            if(!AutoPlanFlagUpdateProcessed)
            {
                int code = TaskPlaningDataSet.MachineCode[Stanok];

                var q = await LPackClientQuery.DoQueryAsync("Production", "TaskPlanningKashira", "AutoPlanGet", "ITEMS", 
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
                                    AutoPlan.ToolTip = description.Substring(2).Replace("True", "включено").Replace("False", "выключено");
                                    PlanTimeText.Text = time;
                                }


                                if (res != AutoPlan.IsChecked)
                                {
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


                var q = await LPackClientQuery.DoQueryAsync("Production", "TaskPlanningKashira", "AutoplanSet", "",
                    new Dictionary<string, string>()
                    {
                    { "ID_ST", ((int)Stanok).ToString() },
                    { "CODE", code.ToString() },
                    { "ENABLE", Checked ? "1" : "0" },
                    { "TIME", PlanTimeText.Text }
                    });

                AutoPlan.ToolTip = Checked ? "Автопланирование включено" : "Автопалнирование выключено";
            }

            AutoPlanFlagUpdateProcessed = false;
        }

        private void PlanTimeText_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
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


        /// <summary>
        /// Установка нового времени для автопланирования
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void InstallNewTime_Click(object sender, RoutedEventArgs e)
        {
            var time = PlanTimeText.Text;

            var prepayParams = new Dictionary<string, string>
            {
                { "CODE", $"{TaskPlaningDataSet.MachineCode[Stanok]}" },
                { "TIME", time }
            };

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "TaskPlanningKashira");
            q.Request.SetParam("Action", "AutoplanSaveNewTime");
            q.Request.SetParams(prepayParams);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var dialog = new DialogWindow("Время успешно установлено", "Автопланирование");
                
                dialog.ShowDialog();
            }
        }
    }
}
