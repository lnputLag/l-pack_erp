using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
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
    /// Изменение способа доставки уже изготовленного образца
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleUpdateDelivery : UserControl
    {
        public SampleUpdateDelivery()
        {
            InitializeComponent();

            InitForm();
            DeliveryType.Items = DeliveryTypes.Items;
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
        /// Старый тип доставки. Если при нажатии на кнопку Save тип не изменился, то закрываем окно
        /// </summary>
        private int OldDelivery;

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "PreproductionSample",
                ReceiverName = "",
                SenderName = "SampleUpdateDelivery",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        private void InitForm()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path = "ID",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = null,
                    ControlType = "void",
                },
                new FormHelperField()
                {
                    Path = "STATUS_ID",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = null,
                    ControlType = "void",
                },
                new FormHelperField()
                {
                    Path = "DELIVERY_ID",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = DeliveryType,
                    ControlType = "SelectBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>
                    {
                    },
                },
            };
            Form.SetFields(fields);
        }

        public void Edit(Dictionary<string, string> p)
        {
            Form.SetValues(p);
            OldDelivery = p.CheckGet("DELIVERY_ID").ToInt();
            Show();
        }

        /// <summary>
        /// Показывает окно
        /// </summary>
        public void Show()
        {
            string title = "Изменение типа доставки";

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

        /// <summary>
        /// Проверки перед сохранением
        /// </summary>
        public void Save()
        {
            var v = Form.GetValues();
            int newDelivery = v.CheckGet("DELIVERY_ID").ToInt();
            if (newDelivery != OldDelivery)
            {
                SaveData(v);
            }

            Close();
        }

        private async void SaveData(Dictionary<string, string> v)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "UpdateDelivery");
            q.Request.SetParams(v);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            }
            );

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    if (result.ContainsKey("ITEMS"))
                    {
                        //отправляем сообщение о закрытии окна
                        Messenger.Default.Send(new ItemMessage()
                        {
                            ReceiverGroup = "PreproductionSample",
                            ReceiverName = ReceiverName,
                            SenderName = "SampleUpdateDelivery",
                            Action = "Refresh",
                        });
                    }
                }
            }

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
