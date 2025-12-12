using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Окно редактирования оценки времени изготовления образца
    /// </summary>
    public partial class SampleTaskEditTime : UserControl
    {
        public SampleTaskEditTime()
        {
            InitializeComponent();

            InitForm();
        }

        /// <summary>
        /// форма
        /// </summary>
        public FormHelper Form { get; set; }
        /// <summary>
        /// Окно
        /// </summary>
        public Window Window { get; set; }
        /// <summary>
        /// Название получателя сообщения
        /// </summary>
        public string ReceiverName;

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
                    Path="ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="ESTIMATE_TIME",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=EstimateTime,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                        { FormHelperField.FieldFilterRef.MinValue, 1 },
                        { FormHelperField.FieldFilterRef.MaxValue, 720 },
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
                    FormStatus.Text = "Неверное значение";
                }
            };
        }

        /// <summary>
        /// Отображение окна
        /// </summary>
        public void Show()
        {
            string title = $"Оценка времени изготовления";

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

            EstimateTime.Focus();
        }

        /// <summary>
        /// редактирование
        /// </summary>
        public void Edit(Dictionary<string, string> values)
        {
            Form.SetValues(values);
            Show();
        }

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
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "SampleTask");
                q.Request.SetParam("Action", "UpdateEstimateTime");
                q.Request.SetParams(v);

                q.Request.Timeout = Central.Parameters.RequestTimeoutMax;
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
                        if (result.ContainsKey("ITEMS"))
                        {
                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup = "PreproductionSample",
                                ReceiverName = ReceiverName,
                                SenderName = "EditTime",
                                Action = "Refresh",
                            });
                        }

                        Close();
                    }
                }

            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

