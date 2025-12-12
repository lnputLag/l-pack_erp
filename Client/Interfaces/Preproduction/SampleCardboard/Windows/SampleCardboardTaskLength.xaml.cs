using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Окно редактирования длины задания на картон для образцов
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleCardboardTaskLength : ControlBase
    {
        public SampleCardboardTaskLength()
        {
            InitializeComponent();

            InitForm();
        }

        /// <summary>
        /// форма
        /// </summary>
        public FormHelper Form { get; set; }
        /// <summary>
        /// Окно редактирования
        /// </summary>
        public Window Window { get; set; }
        /// <summary>
        /// Имя вкладки, куда происходит возврат при закрытии этой вкладки
        /// </summary>
        public string ReceiverName;
        /// <summary>
        /// Идентификатор изделия
        /// </summary>
        public int TaskId { get; set; }

        /// <summary>
        /// Обработка и выполнение команд
        /// </summary>
        /// <param name="command"></param>
        public void ProcessCommand(string command, ItemMessage m = null)
        {
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "close":
                        Close();
                        break;
                    case "save":
                        Save();
                        break;
                }
            }
        }

        /// <summary>
        /// Инициализация формы
        /// </summary>
        private void InitForm()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="TASK_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="SHEET_QUANTITY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=SheetQuantity,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                        { FormHelperField.FieldFilterRef.IsNotZero, null },
                    },
                },
            };
            Form.SetFields(fields);
            Form.StatusControl = FormStatus;
        }

        /// <summary>
        /// Получение данных для редактирования задания
        /// </summary>
        /// <param name="values"></param>
        public void Edit(Dictionary<string, string> values)
        {
            Form.SetValues(values);
            Show();
        }

        /// <summary>
        /// Отображение окна
        /// </summary>
        public void Show()
        {
            string title = $"Количество листов";

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
        /// Сохранение количества листов
        /// </summary>
        public void Save()
        {
            if (Form.Validate())
            {
                bool resume = true;

                if (resume)
                {
                    int newSheets = SheetQuantity.Text.ToInt();
                    if (newSheets < 28)
                    {
                        resume = false;
                        Form.SetStatus("Минимум 28 листов");
                    }
                }

                if (resume)
                {
                    var v = Form.GetValues();

                    //Central.Msg.SendMessage(new ItemMessage()
                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup = "Preproduction",
                        ReceiverName = ReceiverName,
                        SenderName = ControlName,
                        Action = "EditTaskLength",
                        ContextObject = v,
                    });

                    Close();
                }
            }
        }

        private void ButtonOnClick(object sender, RoutedEventArgs e)
        {
            var b = (Button)sender;
            if (b != null)
            {
                var t = b.Tag.ToString();
                ProcessCommand(t);
            }
        }
    }
}
