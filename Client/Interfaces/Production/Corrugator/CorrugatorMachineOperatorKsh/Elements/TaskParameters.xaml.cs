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

namespace Client.Interfaces.Production.Corrugator.CorrugatorMachineOperatorKsh
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


        public delegate void ChangeTaskId(int taskId);
        public event ChangeTaskId OnChangeTask;

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
                var q = await LPackClientQuery.DoQueryAsync("Production", "CorrugatorMachineOperator", "GetTaskData", "TASK", p);

                if (q.Answer.Status == 0)
                {
                    //var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (q.Answer.QueryResult != null)
                    {
                        var dsTask = q.Answer.QueryResult;// ListDataSet.Create(result, "TASK");
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
                            
                            // if (firstTask.CheckGet("COUNT").ToInt() > 0)
                            // {
                            //     LabelK2.Visibility = Visibility.Visible;
                            // }
                            // else
                            // {
                            //     LabelK2.Visibility = Visibility.Hidden;
                            // }

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
                            
                            // №
                            TextTaskNum.Text = firstTask.CheckGet("CRT_NUM").ToString();
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

                            OnChangeData?.Invoke(avgSpeed);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                CorrugatorErrors.LogError(ex);
            }
        }

        public void ShowSplash()
        {
            Splash.Visibility = Visibility.Visible;
            this.Cursor = Cursors.Wait;
            Splash.Cursor = Cursors.Wait;
        }

        public void HideSplash()
        {
            Splash.Visibility = Visibility.Collapsed;
            this.Cursor = null;
            Splash.Cursor = null;
        }
    }
}
