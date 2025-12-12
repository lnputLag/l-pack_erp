using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Client.Common;
using GalaSoft.MvvmLight.Messaging;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Форма ввода количества изделий на поддоне
    /// </summary>
    /// <author>михеев</author>   
    public partial class ComplectationNumberEdit : UserControl
    {
        public bool OkFlag { get; set; }

        public int Value
        {
            get
            {
                int.TryParse(NumberTextBox.Text, out var v);
                return v;
            }

            set => NumberTextBox.Text = value.ToString();
        }

        /// <summary>
        /// рекомендуемое количесто товар на поддоне
        /// </summary>
        public int DefaultProductCountOnPallet { get; set; }


        public FormHelper FormHelper { get; set; }
        public Window Window { get; set; }

        public ComplectationNumberEdit()
        {
            InitializeComponent();

            InitForm();
            SetDefaults();
        }

        private void InitForm()
        {
            FormHelper = new FormHelper();
            //список колонок формы
            var fields = new List<FormHelperField>
            {
                new FormHelperField
                {
                    Path = "NUMBER",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = NumberTextBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
//                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                        { FormHelperField.FieldFilterRef.IsNotZero, null }


                    },
                },
            };
            FormHelper.SetFields(fields);

            NumberTextBox.Focus();
        }

        private void SetDefaults()
        {
            OkFlag = false;
            //Value = 0;
        }

        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "StockControl",
                ReceiverName = "",
                SenderName = "StockNumberEdit",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }


        /// <summary>
        /// Показывает окно
        /// </summary>
        public void Show()
        {
            var title = $"Ввод количества";

            Window = new Window
            {
                Title = title,
                Width = 420,
                Height = 102,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow,
                Content = new Frame
                {
                    Content = this,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                },
            };
            
            CaptionLabel.Content = "Количество изделий на поддоне" + (DefaultProductCountOnPallet != 0 ? $" (рекомендуемое {DefaultProductCountOnPallet})" : "") + ":";


            if (Window != null)
            {
                Window.Topmost = true;
                Window.ShowDialog();
            }
            NumberTextBox.Focus();

        }


        /// <summary>
        /// закрытие окна
        /// </summary>
        public void Close()
        {
            Window?.Close();
        }


        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (FormHelper.Validate())
            {
                if (NumberTextBox.Text.Trim() != string.Empty)
                {
                    OkFlag = true;
                    Close();
                }
                else
                {
                    NumberTextBox.BorderBrush = (Brush)new BrushConverter().ConvertFrom("#ffee0000");
                    NumberTextBox.ToolTip = "Введите значение";
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
