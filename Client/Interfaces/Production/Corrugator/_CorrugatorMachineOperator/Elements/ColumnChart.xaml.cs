using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Client.Assets.HighLighters;
using System;
using System.Windows.Input;
using Newtonsoft.Json;
using System.Linq;

namespace Client.Interfaces.Production.Corrugator.CorrugatorMachineOperator
{
    /// <summary>
    /// Блок "Загрузка складов"
    /// </summary>
    /// <author>zelenskiy_sv</author>   
    public partial class ColumnChart : UserControl
    {
        public ColumnChart()
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
        /// Процент заполнения буфера 1
        /// </summary>
        public int Buffer1Percent { get; set; }
        /// <summary>
        /// Процент заполнения буфера 2
        /// </summary>
        public int Buffer2Percent { get; set; }

        /// <summary>
        /// Процент заполнения СГП
        /// </summary>
        public int SGPPercent { get; set; }

        /// <summary>
        /// Обновление информации о заполнении буфера сырья
        /// </summary>
        /// <param name="percentBuffer"></param>
        public void SetBuffer(int bufferNumber, int percentBuffer, int percentBufferDry = 0)
        {
            string color = HColor.RedFG;
            if (percentBuffer <= 50 )
            {
                color = HColor.Blue;
            }
            else if (percentBuffer <= 70 && percentBuffer > 50)
            {
                color = HColor.Orange;
            }
            else if (percentBuffer <= 90 && percentBuffer > 70)
            {
                color = HColor.Red;
            }
            else if (percentBuffer > 90)
            {
                color = HColor.RedFG; 
            }

            if (bufferNumber == 1)
            {
                Buf1Label.Text = $"{percentBuffer}%";
                Buf1GrayRectangle.ToolTip = $"{100 - percentBuffer}% поддонов свободно в буфере 1";
                Buf1ColorRectangle.ToolTip = $"{percentBuffer}% поддонов занято в буфере 1";
                Buf1DarkGrayRectangle.ToolTip = $"{percentBufferDry}% поддонов откондиционировано в буфере 1";

                if(percentBuffer > 100)
                {
                    percentBuffer = 100;
                }
                Buffer1Percent = percentBuffer;
                Buf1GrayRow.Height  = new GridLength(Convert.ToDouble(100 - percentBuffer), GridUnitType.Star);
                Buf1ColorRow.Height = new GridLength(Convert.ToDouble(percentBuffer - percentBufferDry), GridUnitType.Star);
                Buf1GrayRow2.Height = new GridLength(Convert.ToDouble(percentBufferDry), GridUnitType.Star);
                Buf1ColorRectangle.Fill = color.ToBrush();
            }
            else if (bufferNumber == 2)
            {
                Buf2Label.Text = $"{percentBuffer}%";
                Buf2GrayRectangle.ToolTip = $"{100 - percentBuffer}% поддонов свободно в буфере 2";
                Buf2ColorRectangle.ToolTip = $"{percentBuffer}% поддонов занято в буфере 2";

                if (percentBuffer > 100)
                {
                    percentBuffer = 100;
                }
                Buffer2Percent = percentBuffer;
                Buf2GrayRow.Height = new GridLength(Convert.ToDouble(100 - percentBuffer), GridUnitType.Star);
                Buf2ColorRow.Height = new GridLength(Convert.ToDouble(percentBuffer), GridUnitType.Star);
                Buf2ColorRectangle.Fill = color.ToBrush();
            }
        }

        /// <summary>
        /// Обновление информации о заполнении СГП
        /// </summary>
        /// <param name="percentSGP"></param>
        public void SetSGP(int percentSGP)
        {
            // СГП
            SGPPercent = percentSGP;

            // Вывод значения
            SGPLabel.Text = $"{percentSGP}%";
            SGPGrayRectangle.ToolTip = $"{100 - percentSGP}% поддонов свободно на складе готовой продукции";
            SGPColorRectangle.ToolTip = $"{percentSGP}% поддонов занято на складе готовой продукции";

            // Отрисовка псевдогистограммы
            if(percentSGP > 100)
            {
                percentSGP = 100;
            }
            SGPColorRow.Height = new GridLength(Convert.ToDouble(percentSGP), GridUnitType.Star);
            SGPGrayRow.Height = new GridLength(Convert.ToDouble(100 - percentSGP), GridUnitType.Star);

            // Установка цветов
            if (percentSGP > 80)
            {
                SGPColorRectangle.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(HColor.RedFG)); 
            }
            else if (percentSGP <= 80 && percentSGP > 65)
            {
                SGPColorRectangle.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(HColor.Red)); 
            }
            else if (percentSGP <= 65 && percentSGP > 45)
            {
                SGPColorRectangle.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(HColor.Orange));
            }
            else if (percentSGP <= 45 )
            {
                SGPColorRectangle.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(HColor.Blue));
            }
            else
            {
                SGPColorRectangle.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(HColor.RedFG)); 
            }
        }

        public async void LoadData()
        {
            ShowSplash();

            SGPAndBuffersLoadData();

            HideSplash();
        }

        /// <summary>
        /// получение информации о заполнении буфера сырья
        /// </summary>
        public async void SGPAndBuffersLoadData()
        {
            bool resume = true;

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "CorrugatorMachineOperator");
                q.Request.SetParam("Action", "GetSGPAndBufferState");

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
                        {
                            var ds = ListDataSet.Create(result, "SGP");

                            if (ds.Items.Count > 0)
                            {
                                var first = ds.Items.FirstOrDefault();
                                SetSGP(first.CheckGet("PARAM_VALUE").ToInt());
                            }
                        }

                        {
                            var ds = ListDataSet.Create(result, "BUFFER1");
                            if (ds.Items.Count > 0)
                            {
                                var ds2 = ListDataSet.Create(result, "BUFFER1_DRY");
                                if (ds2.Items.Count > 0)
                                {
                                    var bufferFullness = ds.Items.FirstOrDefault().CheckGet("PARAM_VALUE").ToInt();
                                    var bufferDryFullness = ds2.Items.FirstOrDefault().CheckGet("PARAM_VALUE").ToInt();
                                    SetBuffer(1, bufferFullness, bufferDryFullness);
                                }
                            }
                        }

                        {
                            var ds = ListDataSet.Create(result, "BUFFER2");
                            if (ds.Items.Count > 0)
                            {
                                var bufferFullness = ds.Items.FirstOrDefault().CheckGet("PARAM_VALUE").ToInt();
                                SetBuffer(2, bufferFullness);
                            }
                        }
                    }
                }
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
