using Client.Common;
using Client.Interfaces.Main;
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

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Окно редактирования несимметричной рилевки
    /// </summary>
    public partial class AssortmentCreaseEdit : ControlBase
    {
        public AssortmentCreaseEdit()
        {
            InitializeComponent();

            //регистрация обработчика клавиатуры
            PreviewKeyDown += ProcessKeyboard;

            InitForm();
        }

        /// <summary>
        /// Номер рилевки
        /// </summary>
        public int Num;

        /// <summary>
        /// Имя вкладки, куда происходит возврат при закрытии этой вкладки
        /// </summary>
        public string ReceiverName;

        /// <summary>
        /// форма
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Central.Msg.SendMessage(new ItemMessage()
            {
                ReceiverGroup = "Preproduction",
                ReceiverName = "",
                SenderName = ControlName,
                Action = "Close",
            });
        }

        /// <summary>
        /// инициализация формы
        /// </summary>
        public void InitForm()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="NUM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="CREASE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Crease,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                        { FormHelperField.FieldFilterRef.IsNotZero, null },
                    },
                },
            };

            Form.SetFields(fields);
            Form.OnValidate = (bool valid, string message) =>
            {
                if (valid)
                {
                    FormStatus.Text = "";
                }
                else
                {
                    FormStatus.Text = "Не все поля заполнены верно";
                }
            };
        }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        private void ProcessKeyboard(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;
                case Key.Enter:
                    Save();
                    e.Handled = true;
                    break;
            }
        }

        public void Edit(Dictionary<string, string> values)
        {
            Form.SetValues(values);
            Num = values.CheckGet("NUM").ToInt();
            Show();
        }

        public Window Window { get; set; }
        public void Show()
        {
            string title = $"Несимметричная рилевка {Num}";

            int w = (int)Width;
            int h = (int)Height;

            Window = new Window
            {
                Title = title,
                Width = w + 17,
                Height = h + 40,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow,

            };

            Window.Content = new Frame
            {
                Content = this,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };


            if (Window != null)
            {
                Window.Topmost = true;
                Window.ShowDialog();
            }

            Window.Closed += Window_Closed;

            Crease.Focus();
        }

        public void Close()
        {
            var window = this.Window;
            if (window != null)
            {
                window.Close();
            }
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            Destroy();
        }

        public void Save()
        {
            if (Form.Validate())
            {
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = "Preproduction",
                    ReceiverName = ReceiverName,
                    SenderName = ControlName,
                    Action = "SaveCrease",
                    ContextObject = Form.GetValues(),
                });

                Close();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
