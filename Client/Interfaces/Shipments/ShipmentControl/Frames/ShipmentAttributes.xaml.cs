using Client.Common;
using GalaSoft.MvvmLight.Messaging;
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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client.Interfaces.Shipments
{

    /// <summary>
    /// Установка параметров сцены ShipmentShemeTwo
    /// </summary>
    public partial class ShipmentAttributes : UserControl
    {
        public ShipmentAttributes()
        {

            FrameName = "ShipmentAtribytes";

            Angle1Text = new TextBox();
            Angle2Text = new TextBox();
            DistanceText = new TextBox();
            L1IntensityText = new TextBox();
            L2IntensityText = new TextBox();

            InitializeComponent();

            Init();


        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// имя фрейма,
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// Угол обзора по горизонтали
        /// </summary>
        public string Angle1 { get; set; }

        /// <summary>
        /// Угол обзора по вертикали
        /// </summary>
        public string Angle2 { get; set; }

        /// <summary>
        /// Расстояние до камеры
        /// </summary>
        public string Distance { get; set; }

        /// <summary>
        /// Оствещение направленное
        /// </summary>
        public string L1 { get; set; }

        /// <summary>
        /// Освещение рассеенное
        /// </summary>
        public string L2 { get; set; }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void Init()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="ANGLE1",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Angle1Text,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="ANGLE2",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Angle2Text,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="DISTANCE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=DistanceText,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="L1",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=L1IntensityText,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="L2",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=L2IntensityText,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
            };

            Form.SetFields(fields);
        }

        public void Show()
        {
            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 2;
            
            var frameName = FrameName;
            Central.WM.Show(frameName, "Параметры сцены", true, "add", this);
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            GetValues();

            Destroy();
            Close();
        }

        /// <summary>
        /// Получение начальных значений сцены
        /// </summary>
        /// <param name="Angle1"></param>
        /// <param name="Angle2"></param>
        /// <param name="Distance"></param>
        /// <param name="L1"></param>
        /// <param name="L2"></param>
        public void SetValues(string Angle1, string Angle2, string Distance, string L1, string L2)
        {
            this.Angle1 = Angle1;
            this.Angle2 = Angle2;

            this.Distance = Distance;

            this.L1 = L1;
            this.L2 = L2;

            SetTextBoxParametr();

            Angle1Slider.Value = Angle1Text.Text.ToDouble();
            Angle2Slider.Value = Angle2Text.Text.ToDouble();
            DistanceSlider.Value = DistanceText.Text.ToDouble();
            L1IntensitySlider.Value = L1IntensityText.Text.ToDouble();
            L2IntensitySlider.Value = L2IntensityText.Text.ToDouble();
        }


        /// <summary>
        /// Передача в textbox значений переменных
        /// </summary>
        public void SetTextBoxParametr()
        {
            Angle1Text.Text = this.Angle1;
            Angle2Text.Text = this.Angle2;
            DistanceText.Text = this.Distance;
            L1IntensityText.Text = this.L1;
            L2IntensityText.Text = this.L2;
        }


        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            var frameName = FrameName;
            Central.WM.Close(frameName);
        }

        public void Destroy()
        {
            Messenger.Default.Unregister<ItemMessage>(this);
        }




        /// <summary>
        /// Передача параметров сцены через шину сообщений
        /// </summary>
        public void GetValues()
        {
            if (Form != null)
            {
                var RowValue = Form.GetValues();

                Messenger.Default.Send(new ItemMessage()
                {
                    ReceiverGroup = "ShipmentControl",
                    ReceiverName = "ShipmentShemeTwo",
                    SenderName = "ShipmentAtribytes",
                    Action = "Save",
                    Message = "",
                    ContextObject = RowValue,
                }
                );
            }

        }

        private void L2IntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            L2 = L2IntensitySlider.Value.ToString();
            SetTextBoxParametr();
        }

        private void L1IntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            L1 = L1IntensitySlider.Value.ToString();
            SetTextBoxParametr();
        }

        private void DistanceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Distance = DistanceSlider.Value.ToString();
            SetTextBoxParametr();
        }

        private void Angle2Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Angle2 = Angle2Slider.Value.ToString();
            SetTextBoxParametr();
        }

        private void Angle1Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Angle1 = Angle1Slider.Value.ToString();
            SetTextBoxParametr();
        }

        private void Angle1Text_TextChanged(object sender, TextChangedEventArgs e)
        {
            GetValues();
        }

        private void Angle2Text_TextChanged(object sender, TextChangedEventArgs e)
        {
            GetValues();
        }

        private void DistanceText_TextChanged(object sender, TextChangedEventArgs e)
        {
            GetValues();
        }

        private void L2IntensityText_TextChanged(object sender, TextChangedEventArgs e)
        {
            GetValues();
        }

        private void L1IntensityText_TextChanged(object sender, TextChangedEventArgs e)
        {
            GetValues();
        }
    }

}
