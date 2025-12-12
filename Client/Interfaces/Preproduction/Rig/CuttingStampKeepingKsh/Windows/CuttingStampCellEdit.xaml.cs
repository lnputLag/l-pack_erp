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
    /// Переименование ячейки хранения штанцформы
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class CuttingStampCellEdit : ControlBase
    {
        public CuttingStampCellEdit()
        {
            InitializeComponent();
            InitForm();
        }

        /// <summary>
        /// Форма редактирования поддонов
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Имя вкладки, окуда вызвана форма редактирования
        /// </summary>
        public string ReceiverName;

        /// <summary>
        /// Структура окна
        /// </summary>
        private Window Window { get; set; }

        /// <summary>
        /// Обработка команд
        /// </summary>
        /// <param name="command"></param>
        private void ProcessCommand(string command)
        {
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "save":
                        Save();
                        break;
                    case "close":
                        Close();
                        break;
                }
            }
        }

        /// <summary>
        /// Инициализация формы
        /// </summary>
        public void InitForm()
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
                    Path="CELL_NUM",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=RackNum,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="CELL_PLACE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CellNum,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="OLD_NUM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=OldCellNum,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STAMP_ITEM_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                },
            };
            Form.SetFields(fields);

        }

        /// <summary>
        /// /Редактирование ячейки
        /// </summary>
        /// <param name="values"></param>
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
            string title = $"Изменение ячейки";

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
        /// Сохранение данных
        /// </summary>
        public async void Save()
        {
            if (Form.Validate())
            {
                var v = Form.GetValues();

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Rig");
                q.Request.SetParam("Object", "CuttingStamp");
                q.Request.SetParam("Action", "UpdateCell");
                q.Request.SetParams(v);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        if (result.ContainsKey("ITEM"))
                        {
                            //отправляем сообщение 
                            Central.Msg.SendMessage(new ItemMessage()
                            {
                                ReceiverGroup = "Preproduction/Rig",
                                ReceiverName = ReceiverName,
                                SenderName = "SaveCell",
                                Action = "Refresh",
                            });
                            Close();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Обработка нажатия на кнопку
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
