using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Preproduction;
using DevExpress.Xpf.Core.Native;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Столбчатая диаграмма для интерфейса Позорнй столб
    /// </summary>
    public partial class BarChart : UserControl
    {
        public BarChart(int machineId = 0, string machineName = "")
        {
            InitializeComponent();

            MachineId = machineId;
            MachineName = machineName;

            SetDefault();
        }

        public int MachineId { get; set; }

        public string MachineName { get; set; }

        private const string DefaultColor = HColor.Gray;

        public void SetDefault()
        {
            FirstValueTextBlock.Text = "";
            SecondValueTextBlock.Text = "";
            InnerValueTextBlock.Text = "";

            FooterValueTextBlock.Text = MachineName;

            CurrentValueBorder.Background = DefaultColor.ToBrush();
        }

        public void SetValues(double currentValue, string innerValue, string firstValue, string secondValue, string color = DefaultColor)
        {
            FirstValueTextBlock.Text = firstValue;
            SecondValueTextBlock.Text = secondValue;
            InnerValueTextBlock.Text = innerValue;

            double maxHeight = BarContainerBorder.Height; // мб ActualHeight

            {
                double currentHeight = currentValue * maxHeight;
                if (currentHeight > maxHeight)
                {
                    currentHeight = maxHeight;
                }

                if (currentHeight.IsNaN())
                {
                    currentHeight = 0;
                }

                CurrentValueBorder.Height = currentHeight;
            }

            try
            {
                CurrentValueBorder.Background = color.ToBrush();
            }
            catch (Exception ex)
            {
                CurrentValueBorder.Background = DefaultColor.ToBrush();
            }
        }
    }
}
