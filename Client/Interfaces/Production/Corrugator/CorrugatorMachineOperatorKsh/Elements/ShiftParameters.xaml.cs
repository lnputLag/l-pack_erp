using Client.Common;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls; 
using System.Windows.Input;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace Client.Interfaces.Production.Corrugator.CorrugatorMachineOperatorKsh
{
    /// <summary>
    /// Показатели смены
    /// </summary>
    /// <author>zelenskiy_sv</author>   
    public partial class ShiftParameters : UserControl
    {
        public ShiftParameters()
        {
            InitializeComponent();
        }

        /// <summary>
        /// инициализация блока
        /// </summary>
        public void Init()
        {
            LoadData();
        }

        /// <summary>
        /// Показатели смены
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
                var q = await LPackClientQuery.DoQueryAsync("Production", "CorrugatorMachineOperatorKsh", "GetShiftData", string.Empty, p);

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var dsShiftNumber = ListDataSet.Create(result, "SHIFT_NUMBER");
                        var firstShiftNumber = dsShiftNumber.Items.FirstOrDefault();
                        var shiftNumber = firstShiftNumber.CheckGet("SHT_NUM").ToInt();

                        var dsTaskCount = ListDataSet.Create(result, "TASK_COUNT");
                        var firstTaskCount = dsTaskCount.Items.FirstOrDefault();
                        var completedTaskCount = firstTaskCount.CheckGet("TASK_COUNT").ToInt();

                        var dsLengthTotal = ListDataSet.Create(result, "LENGTH_TOTAL");
                        var firstLengthTotal = dsLengthTotal.Items.FirstOrDefault();
                        var lengthTotal = firstLengthTotal.CheckGet("SUM_LENGTH").ToDouble();

                        var dsSumDefect = ListDataSet.Create(result, "SUM_DEFECT");
                        var firstSumDefect = dsSumDefect.Items.FirstOrDefault();
                        var sumDefect = firstSumDefect.CheckGet("SUM_QTY").ToDouble();

                        var dsCurrentCountAndLength = ListDataSet.Create(result, "CURRENT_COUNT_AND_LENGTH");
                        var firstCurrentCountAndLength = dsCurrentCountAndLength.Items.FirstOrDefault();
                        var currentTaskCount = firstCurrentCountAndLength.CheckGet("COUNT_TASK").ToInt();
                        var currentLengthSum = firstCurrentCountAndLength.CheckGet("SUM_LENGTH").ToInt();

                        var dsIdle = ListDataSet.Create(result, "IDLE");
                        var firstIdle = dsIdle.Items.FirstOrDefault();
                        var idleSeconds = firstIdle.CheckGet("TIME_IDLES").ToInt();
                        var sumIdles = TimeSpan.FromSeconds(idleSeconds);

                        var dsCalculatedTime = ListDataSet.Create(result, "CALCULATED_TIME");
                        var calculatedTime = dsCalculatedTime.Items.FirstOrDefault().CheckGet("TIME").ToInt();

                        var dsTimeIdlesWithoutTechnological = ListDataSet.Create(result, "TIME_IDLES_WITHOUT_TECHNOLOGICAL");
                        var timeIdlesWithoutTechnologicalSeconds = dsTimeIdlesWithoutTechnological?.Items?.FirstOrDefault()?.CheckGet("SUM_TIME")?.ToDouble();
                        var timeIdlesWithoutTechnologicalTime = TimeSpan.FromSeconds(timeIdlesWithoutTechnologicalSeconds ?? 0);

                        // время работы текущей смены за исключением простоев
                        var realTimeTotal = (DateTime.Now - CorrugatorMachineOperator.DateShiftStart - timeIdlesWithoutTechnologicalTime).TotalSeconds;
                        double kpd = 0;
                        if (realTimeTotal > 0)
                        {
                            kpd = (calculatedTime / realTimeTotal) * 100;
                            if (kpd > 100)
                            {
                                kpd = 100;
                            }
                        }
                        // м/с
                        var averageSpeed = lengthTotal / (realTimeTotal / 60);

                        double defectPercentage = 0;
                        if (lengthTotal > 0)
                        {
                            defectPercentage = (sumDefect / lengthTotal) * 100;
                        }

                        // Смена №
                        TextShiftNumber.Text = shiftNumber.ToString();
                        // КПД
                        TextShiftKPD.Text = Math.Round(kpd, 1).ToString();
                        // Выполненных заданий
                        TextShiftTaskCountDone.Text = completedTaskCount.ToString();
                        // Проехали за смену
                        TextShiftDone.Text = lengthTotal.ToString();
                        // Средняя скорость
                        TextShiftAvgSpeed.Text = Math.Round(averageSpeed).ToString();
                        // Заданий в очереди ГА
                        TextQueueTaskCount.Text = currentTaskCount.ToString();
                        // Метров в очереди ГА
                        TextCurrentLengthSum.Text = currentLengthSum.ToString();
                        // Процент брака
                        TextDefectPercentage.Text = Math.Round(defectPercentage, 1).ToString();
                        // Суммарное время простоев
                        TextShiftSumIdle.Text = $"{Math.Truncate(sumIdles.TotalMinutes):00}:{sumIdles.Seconds:00}";
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
