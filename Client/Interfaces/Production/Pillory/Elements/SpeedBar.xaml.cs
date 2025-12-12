using Client.Assets.HighLighters;
using Client.Common;
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
    /// Логика взаимодействия для SpeedBar.xaml
    /// </summary>
    public partial class SpeedBar : UserControl
    {
        public SpeedBar()
        {
            InitializeComponent();
            SetDefault();
        }

        public void SetDefault()
        {
            FirstDivisionTextBlock.Text = "";
            SecondDivisionTextBlock.Text = "";
            ThirdDivisionTextBlock.Text = "";
            FourthDivisionTextBlock.Text = "";
            FifthDivisionTextBlock.Text = "";

            CurrentValueBorder.Background = "#00FFFFFF".ToBrush();
            PlanValueBorder.Margin = new Thickness(0,0,0, -PlanValueBorder.Height);
        }

        public void SetCurrentValue(double currentValue, double planValue, double maxValue)
        {
            double maxHeight = MainContainerBorder.Height; // мб ActualHeight

            // Выставляем высоту заполненной шкалы
            {
                double currentHeight = (currentValue / maxValue) * maxHeight;
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

            // Определяем цвет заполненной шкалы
            {
                if (currentValue >= planValue)
                {
                    CurrentValueBorder.Background = HColor.Green.ToBrush();
                }
                else if (currentValue >= (planValue / 2))
                {
                    CurrentValueBorder.Background = HColor.YellowOrange.ToBrush();
                }
                else
                {
                    CurrentValueBorder.Background = HColor.Red.ToBrush();
                }
            }

            // Заполняем деления на шкале делений
            {
                int devisionValue = Math.Ceiling(maxValue / 5).ToInt();

                FirstDivisionTextBlock.Text = $"{1 * devisionValue}";
                SecondDivisionTextBlock.Text = $"{2 * devisionValue}";
                ThirdDivisionTextBlock.Text = $"{3 * devisionValue}";
                FourthDivisionTextBlock.Text = $"{4 * devisionValue}";
                FifthDivisionTextBlock.Text = $"{maxValue.ToInt()}";
            }

            // Показываем значение плановой скорости
            {
                double planHeight = (planValue / maxValue) * maxHeight;
                if (planHeight > maxHeight)
                {
                    planHeight = maxHeight;
                }

                if (planHeight.IsNaN())
                {
                    planHeight = 0;
                }

                planHeight = planHeight - PlanValueBorder.Height;
                PlanValueBorder.Margin = new Thickness(0, 0, 0, planHeight);
            }
        }
    }
}
