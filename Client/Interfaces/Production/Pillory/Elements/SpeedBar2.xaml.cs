using Client.Assets.HighLighters;
using Client.Common;
using NPOI.XSSF.Streaming.Values;
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
    /// Логика взаимодействия для SpeedBar2.xaml
    /// </summary>
    public partial class SpeedBar2 : UserControl
    {
        public SpeedBar2()
        {
            InitializeComponent();
            SetDefault();
        }

        public const int MaxSpeed = 500;

        private double AnglePerSpeed {  get; set; }

        private RotateTransform RotateTransformCurrentValue {  get; set; }

        private RotateTransform RotateTransformPlanValue { get; set; }

        public void SetDefault()
        {
            AnglePerSpeed = (double)180 / MaxSpeed;

            RotateTransformCurrentValue = new RotateTransform(0, CurrentValue.Width - 1, CurrentValue.Height / 2);
            CurrentValue.RenderTransform = RotateTransformCurrentValue;

            RotateTransformPlanValue = new RotateTransform(0, PlanValue.Width - 1, PlanValue.Height / 2);
            PlanValue.RenderTransform = RotateTransformPlanValue;

            DivisionInnerBar.Background = HColor.White.ToBrush();
        }

        public void SetCurrentValue(double currentValue, double planValue)
        {
            double currentAngle = currentValue * AnglePerSpeed;
            RotateTransformCurrentValue.Angle = currentAngle;
            CurrentValue.RenderTransform = RotateTransformCurrentValue;

            double planAngle = planValue * AnglePerSpeed;
            RotateTransformPlanValue.Angle = planAngle;
            PlanValue.RenderTransform = RotateTransformPlanValue;

            // Определяем цвет заполненной шкалы
            {
                if (currentValue >= planValue)
                {
                    DivisionInnerBar.Background = HColor.Green.ToBrush();
                }
                else if (currentValue >= (planValue / 2))
                {
                    DivisionInnerBar.Background = HColor.YellowOrange.ToBrush();
                }
                else
                {
                    DivisionInnerBar.Background = HColor.Red.ToBrush();
                }
            }
        }
    }
}
