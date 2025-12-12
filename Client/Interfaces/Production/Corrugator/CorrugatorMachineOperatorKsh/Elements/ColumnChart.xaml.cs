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

namespace Client.Interfaces.Production.Corrugator.CorrugatorMachineOperatorKsh
{
    /// <summary>
    /// Блок "Загрузка складов"
    /// </summary>
    /// <author>volkov_as</author>
    /// TODO: Поменять ключ 
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
        /// Обновление информации о заполнении буфера сырья
        /// </summary>
        /// <param name="percentBuffer"></param>
        public void SetBuffer(int bufferNumber, int percentBuffer, int percentBufferDry = 0)
        {
            string color = HColor.RedFG;
            if (percentBuffer <= 50)
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

                if (percentBuffer > 100)
                {
                    percentBuffer = 100;
                }

                Buffer1Percent = percentBuffer;
                Buf1GrayRow.Height = new GridLength(Convert.ToDouble(100 - percentBuffer), GridUnitType.Star);
                Buf1ColorRow.Height = new GridLength(Convert.ToDouble(percentBuffer - percentBufferDry), GridUnitType.Star);
                Buf1GrayRow2.Height = new GridLength(Convert.ToDouble(percentBufferDry), GridUnitType.Star);
                Buf1ColorRectangle.Fill = color.ToBrush();
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
                q.Request.SetParam("Module", "Control");
                q.Request.SetParam("Object", "ConfigurationOption");
                q.Request.SetParam("Action", "Get");
                q.Request.SetParam("PARAM_NAME", "BLANK_BUFFER_PCT_STORAGE_WARN_KSH");

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    int result = JsonConvert.DeserializeObject<int>(q.Answer.Data);
                    SetBuffer(1, result, 0);
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
