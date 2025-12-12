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
    /// Окно редактирования примечания к ПЗ
    /// </summary>
    /// <author>Рясной П.В.</author>
    public partial class ProductionTaskNote : UserControl
    {
        public ProductionTaskNote()
        {
            InitializeComponent();
            InitForm();
            SetDefaults();
        }

        /// <summary>
        /// форма
        /// </summary>
        public FormHelper Form { get; set; }
        /// <summary>
        /// Окно редактирования примечания
        /// </summary>
        public Window Window { get; set; }
        /// <summary>
        /// Название окна получателя сообщения
        /// </summary>
        public string ReceiverName;

        /// <summary>
        /// инициализация формы
        /// </summary>
        private void InitForm()
        {
            Form = new FormHelper();
            //список полей формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Note,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };
            Form.SetFields(fields);
            Form.StatusControl = FormStatus;
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            Form.SetDefaults();
            ReceiverName = "";
        }

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "ProductionTask",
                ReceiverName = "",
                SenderName = "ProductionTaskNote",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
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

        /// <summary>
        /// редактирование
        /// </summary>
        public void Edit(Dictionary<string, string> values)
        {
            Form.SetValues(values);
            Show();
        }

        /// <summary>
        /// Показывает окно
        /// </summary>
        private void Show()
        {
            string title = $"Примечание к ПЗ";

            Window = new Window
            {
                Title = title,
                Width = this.Width + 24,
                Height = this.Height + 40,
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
            Note.Focus();

        }

        public void Save()
        {

            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "ProductionTask",
                ReceiverName = ReceiverName,
                SenderName = "ProductionTaskNote",
                Action = "SaveNote",
                ContextObject = Form.GetValues(),
            });
            Close();
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

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

    }
}
