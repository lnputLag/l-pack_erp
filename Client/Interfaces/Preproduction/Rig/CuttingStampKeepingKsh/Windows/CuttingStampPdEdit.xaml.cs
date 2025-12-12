using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Окно редактирования фазировки штанцформы
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class CuttingStampPdEdit : ControlBase
    {
        public CuttingStampPdEdit()
        {
            InitializeComponent();
            InitForm();
        }

        /// <summary>
        /// Форма редактирования поддонов
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Структура окна
        /// </summary>
        private Window Window { get; set; }

        /// <summary>
        /// Имя вкладки, окуда вызвана форма редактирования
        /// </summary>
        public string ReceiverName;

        /// <summary>
        /// Инициализация формы редактирования
        /// </summary>
        private void InitForm()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="PD",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PdEdit,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
            };
            Form.SetFields(fields);
        }

        /// <summary>
        /// Запуск редактирования номера ячейки
        /// </summary>
        public void Edit(Dictionary<string, string> values)
        {
            Form.SetValues(values);
            Show();
        }

        /// <summary>
        /// Показ окна
        /// </summary>
        public void Show()
        {
            int w = (int)Width;
            int h = (int)Height;
            string title = $"Изменение PD";

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
        }

        /// <summary>
        /// Закрытие окна
        /// </summary>
        public void Close()
        {
            var window = this.Window;
            if (window != null)
            {
                window.Close();
            }
        }

        /// <summary>
        /// Сохранение
        /// </summary>
        private void Save()
        {
            if (Form.Validate())
            {
                //отправляем сообщение с данными полей окна
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = "Preproduction/Rig",
                    ReceiverName = ReceiverName,
                    SenderName = "PdEdit",
                    Action = "SavePd",
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
