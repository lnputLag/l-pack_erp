using Client.Assets.HighLighters;
using Client.Common;
using DevExpress.XtraPrinting;
using NPOI.SS.Formula.Functions;
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

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Контрол для отображения выполнения различных операций
    /// В виде гистограммы
    /// <author>eletskikh_ya</author>
    /// </summary>
    public partial class OperatorProgress : UserControl
    {
        public OperatorProgress()
        {
            InitializeComponent();
        }

        public delegate void MouseDown(object sender, MouseEventArgs e);
        public MouseDown OnMouseDown;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="percent">общий процент от максимального размера</param>
        /// <param name="move">кол-во перемещений</param>
        /// <param name="arrival">кол-во оприходований</param>
        /// <param name="writeOff">кол-во списаний</param>
        /// <param name="description">описаник</param>
        public void SetProgress(int percent, int move, int arrival, int writeOff, string description)
        {
            if (percent == Int32.MinValue)
            {
                percent = 0;
            }

            string color = HColor.RedFG;
            if (percent <= 50)
            {
                color = HColor.Blue;
            }
            else if (percent <= 70 && percent > 50)
            {
                color = HColor.Orange;
            }
            else if (percent <= 90 && percent > 70)
            {
                color = HColor.LightSelection;
            }
            else if (percent > 90)
            {
                color = HColor.Green;
            }

            Buf1ColorRow.Width = new GridLength(Convert.ToDouble(percent), GridUnitType.Star);
            Buf1GrayRow.Width = new GridLength(Convert.ToDouble(100-percent), GridUnitType.Star);

            ColumnArrivale.Width = new GridLength(Convert.ToDouble(arrival), GridUnitType.Star);
            ColumnMove.Width = new GridLength(Convert.ToDouble(move), GridUnitType.Star);
            ColumnWriteOff.Width = new GridLength(Convert.ToDouble(writeOff), GridUnitType.Star);

            if(description==string.Empty)
            {
                Buf1DataRow.Width = new GridLength(0, GridUnitType.Pixel);
            }

            Description2.Text = description;
        }

        public void SetProgress2(int percent, int move, int arrival, int writeOff, string description1, string description2, string tooltip1 = null, string tooltip2 = null)
        {
            if (percent == Int32.MinValue)
            {
                percent = 0;
            }

            string color = HColor.RedFG;
            if (percent <= 50)
            {
                color = HColor.Blue;
            }
            else if (percent <= 70 && percent > 50)
            {
                color = HColor.Orange;
            }
            else if (percent <= 90 && percent > 70)
            {
                color = HColor.LightSelection;
            }
            else if (percent > 90)
            {
                color = HColor.Green;
            }

            Buf1ColorRow.Width = new GridLength(Convert.ToDouble(percent), GridUnitType.Star);
            Buf1GrayRow.Width = new GridLength(Convert.ToDouble(100 - percent), GridUnitType.Star);

            ColumnArrivale.Width = new GridLength(Convert.ToDouble(arrival), GridUnitType.Star);
            ColumnMove.Width = new GridLength(Convert.ToDouble(move), GridUnitType.Star);
            ColumnWriteOff.Width = new GridLength(Convert.ToDouble(writeOff), GridUnitType.Star);

            if (description1 == string.Empty
                && description2 == string.Empty)
            {
                Buf1DataRow.Width = new GridLength(0, GridUnitType.Pixel);
            }
            else
            {
                Description2.Text = $"{description1} {description2}";
                Description2.Visibility = Visibility.Collapsed;

                Grid grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) });
                grid.HorizontalAlignment = HorizontalAlignment.Stretch;
                grid.VerticalAlignment = VerticalAlignment.Stretch;

                {
                    TextBlock textBlock = new TextBlock();
                    textBlock.HorizontalAlignment = HorizontalAlignment.Right;
                    textBlock.VerticalAlignment = VerticalAlignment.Center;
                    textBlock.FontSize = 12;
                    textBlock.Text = description1;
                    if (!string.IsNullOrEmpty(tooltip1))
                    {
                        textBlock.ToolTip = tooltip1;
                    }
                    else
                    {
                        textBlock.ToolTip = description1;
                    }
                    System.Windows.Controls.Grid.SetColumn(textBlock, 0);
                    grid.Children.Add(textBlock);
                }

                {
                    TextBlock textBlock = new TextBlock();
                    textBlock.HorizontalAlignment = HorizontalAlignment.Right;
                    textBlock.VerticalAlignment = VerticalAlignment.Center;
                    textBlock.FontSize = 12;
                    textBlock.Text = description2;
                    if (!string.IsNullOrEmpty(tooltip2))
                    {
                        textBlock.ToolTip = tooltip2;
                    }
                    else
                    {
                        textBlock.ToolTip = description2;
                    }
                    System.Windows.Controls.Grid.SetColumn(textBlock, 2);
                    grid.Children.Add(textBlock);
                }

                Buf1DataGrid.Children.Add(grid);
            }
        }

        /// <summary>
        /// Расчёт процента от максимального значения
        /// </summary>
        /// <param name="currentValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public static int CalculateProgressPercent(int currentValue, int maxValue)
        {
            int result = 0;
            
            if (maxValue > 0)
            {
                result = (int)((double)currentValue / maxValue * 100);
            }

            return result;
        }

        /// <summary>
        /// отправляет событие нажатия мыши для обработки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            OnMouseDown?.Invoke(this, e);
        }
    }
}
