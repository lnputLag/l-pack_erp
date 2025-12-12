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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Окно редактирования виртуальной массы бумаги для автораскроя
    /// </summary>
    public partial class ProductionTaskVirtualPaperEdit : UserControl
    {
        public ProductionTaskVirtualPaperEdit()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            //регистрация обработчика клавиатуры
            PreviewKeyDown += ProcessKeyboard;

            Weight = 0;

            InitForm();
        }

        /// <summary>
        /// Идентификатор бумаги
        /// </summary>
        public int Id;

        /// <summary>
        /// Вес виртуального сырья
        /// </summary>
        public double Weight;

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
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "ProductionTaskCutted",
                ReceiverName = "PaperList",
                SenderName = "VirtualPaperEdit",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
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
                    Path="WEIGHT_VIRTUAL",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=WeightVirtual,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitCommaOnly, null },
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
        /// обработчик сообщений
        /// </summary>
        private void ProcessMessages(ItemMessage m)
        {
            //Group 
            if (m.ReceiverGroup.IndexOf("ShipmentControl") > -1)
            {
                switch (m.Action)
                {
                    case "Refresh":
                        break;
                }
            }
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

        public void Edit()
        {
            if (Id > 0)
            {
                WeightVirtual.Text = Weight.ToString();
                Show();
            }
        }

        public Window Window { get; set; }
        public void Show()
        {
            string title = $"Виртуальная масса бумаги";

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

            WeightVirtual.Focus();
        }

        public void Close()
        {
            var window = this.Window;
            if (window != null)
            {
                window.Close();
            }

            Destroy();
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            Destroy();
        }

        public void Save()
        {
            if (Form.Validate())
            {
                var p = new Dictionary<string, string>();
                p.Add("ID", Id.ToString());
                p.Add("WEIGHT_VIRTUAL", WeightVirtual.Text);

                Messenger.Default.Send(new ItemMessage()
                {
                    ReceiverGroup = "ProductionTaskCutted",
                    ReceiverName = "PaperList",
                    SenderName = "VirtualPaperEdit",
                    Action = "Save",
                    ContextObject = p,
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
