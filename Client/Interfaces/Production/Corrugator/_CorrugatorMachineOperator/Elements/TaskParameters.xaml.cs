using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Client.Assets.HighLighters;
using System.Windows.Input;
using Newtonsoft.Json;
using System.Linq;
using System;

namespace Client.Interfaces.Production.Corrugator.CorrugatorMachineOperator
{
    /// <summary>
    /// блок "Показатели текущего ПЗ"
    /// </summary>
    /// <author>zelenskiy_sv</author>   
    public partial class TaskParameters : UserControl
    {
        public TaskParameters()
        {
            InitializeComponent();

            ChangeTaskSeconds = double.MaxValue;
        }

        public double ChangeTaskSeconds {  get; set; }

        public delegate void ChangeData(int avgSpeed);
        public event ChangeData OnChangeData;

        /// <summary>
        /// ИД текущего задания
        /// </summary>
        public int CurentTaskId { get; set; }

        public int PrevTaskId { get; set; } = -1;

        public static int statusSendTask = 0;
        
        public static string textTaskNum { get; set; }

        public delegate void IsHaveK2(bool isHaveK2);
        public event IsHaveK2 OnIsHaveK2;


        public delegate void ChangeTaskId(int taskId);
        public event ChangeTaskId OnChangeTask;

        //Статус работы джоба
        public delegate void StatusValueChanged(int statusValue);
        public event StatusValueChanged OnStatusValueChanged;

        /// <summary>
        /// инициализация блока
        /// </summary>
        public void Init()
        {
            LoadData();
        }

        /// <summary>
        /// Показатели задания
        /// </summary>
        public async void LoadData()
        {
            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_ST", CorrugatorMachineOperator.SelectedMachineId.ToString());
                p.CheckAdd("DTTM_SHIFT_START", CorrugatorMachineOperator.DateShiftStart.ToString("dd.MM.yyyy HH:mm:ss"));
            }

            try
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "CorrugatorMachineOperator");
                q.Request.SetParam("Action", "GetTaskData");
                q.Request.SetParams(p);

                await Task.Run(() => q.DoQuery());


                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                    if (result != null)
                    {
                        var dsTask = ListDataSet.Create(result, "TASK");

                        if (dsTask.Items.Count > 0)
                        {
                            var firstTask = dsTask.Items.FirstOrDefault();
                            var crtLength = firstTask.CheckGet("CRT_LENGTH").ToInt();
                            var taskLength = firstTask.CheckGet("TSK_LENGTH").ToInt();
                            var taskLeft = taskLength - crtLength;
                            var avgSpeed = firstTask.CheckGet("AVG_SPEED").ToInt();
                            var crtSpeed = firstTask.CheckGet("CRT_SPEED").ToInt();
                            CurentTaskId = firstTask.CheckGet("CRT_ID_PZ").ToInt();
                            statusSendTask = firstTask.CheckGet("SEND_TASK").ToInt();
                            
                            if (firstTask.CheckGet("COUNT").ToInt() > 0)
                            {
                                OnIsHaveK2?.Invoke(true);
                            }
                            else
                            {
                                OnIsHaveK2?.Invoke(false);
                            }

                            if (PrevTaskId == -1)
                            {
                                PrevTaskId = CurentTaskId;
                            }
                            else
                            {
                                if (PrevTaskId != CurentTaskId)
                                {
                                    OnChangeTask?.Invoke(PrevTaskId);
                                    PrevTaskId = CurentTaskId;
                                }
                            }


                            double minutesLeft = 0;
                            if (taskLeft > 0 && crtSpeed > 0)
                            {
                                minutesLeft = (double)taskLeft / (double)crtSpeed;
                            }
                            var timeLeft = TimeSpan.FromMinutes(minutesLeft);

                            ChangeTaskSeconds = minutesLeft * 60;

                            // TODO: Вызов делегата если задание сменилось


                            // №
                            TextTaskNum.Text = firstTask.CheckGet("CRT_NUM").ToString();
                            textTaskNum = firstTask.CheckGet("CRT_NUM");
                            // Проехали
                            TextTaskDone.Text = crtLength.ToString();
                            // Осталось
                            TextTaskLeft.Text = taskLeft.ToString();
                            // Длина задания
                            TextTaskLenght.Text = taskLength.ToString();
                            // Средняя скорость
                            TextTaskAvgSpeed.Text = avgSpeed.ToString();
                            // Текущая скорость
                            TextTaskSpeed.Text = crtSpeed.ToString();
                            // До смены задания
                            TextTaskLastTime.Text = $"{Math.Truncate(timeLeft.TotalMinutes):00}:{timeLeft.Seconds:00}";

                            if (CorrugatorMachineOperator.SelectedMachineId != 22)
                            {
                                TextTaskAvgSpeed.Visibility = Visibility.Collapsed;
                                TitleTaskAvgSpeed.Visibility = Visibility.Collapsed;
                                MeasuringTaskAvgSpeed.Visibility = Visibility.Collapsed;
                            }
                            else
                            {
                                TextTaskAvgSpeed.Visibility = Visibility.Visible;
                                TitleTaskAvgSpeed.Visibility = Visibility.Visible;
                                MeasuringTaskAvgSpeed.Visibility = Visibility.Visible;
                            }
                            
                            SetCurrentTime();

                            OnChangeData?.Invoke(avgSpeed);
                        }

                        if (CorrugatorMachineOperator.SelectedMachineId.ContainsIn(2, 21))
                        {
                            var statusValue = ListDataSet.Create(result, "STATUS");

                            if (statusValue.Items.Count > 0)
                            {
                                var value = statusValue.Items.FirstOrDefault().CheckGet("PARAM_VALUE").ToInt();

                                OnStatusValueChanged?.Invoke(value);
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                CorrugatorErrors.LogError(ex);
            }
        }

        public void SetCurrentTime()
        {
            CurrentTime.Text = $"{(DateTime.Now.Hour):00}:{DateTime.Now.Minute:00}";
        }
    }
}
