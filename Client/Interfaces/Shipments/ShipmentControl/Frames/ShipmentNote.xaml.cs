using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Управление отгрузками, форма "Примечание СГП"
    /// Используется для добавления примечаний в отгрузках образцов и отгрузках штанцформ
    /// </summary>
    /// <author>balchugov_dv</author>       
    public partial class ShipmentNote : UserControl
    {
        public ShipmentNote()
        {
            InitializeComponent();

            ObjectId = 0;

            InitForm();
            SetDefaults();

        }

        public FormHelper Form { get; set; }
        public int ObjectId { get; set; }
        public string Object { get; set; }
        public Window Window { get; set; }

        private void SetDefaults()
        {
            StoreKeeperNote.Text="";
        }

        private void InitForm()
        {
            Form = new FormHelper();
            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="STOREKEEPER_NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=StoreKeeperNote,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };
            Form.SetFields(fields);
        }

        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="ShipmentControl",
                ReceiverName = "",
                SenderName = "StorekeeperNoteView",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// основной метод вызова окна с примечанием
        /// </summary>
        public void Edit()
        {
            GetData();
        }

        /// <summary>
        /// Получение данных для отображения в полях формы
        /// </summary>
        public async void GetData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", Object);
            q.Request.SetParam("Action", "GetStorekeeperNote");
            q.Request.SetParam("Id", ObjectId.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    if (result.ContainsKey("StorekeeperNote"))
                    {
                        var FormDS = result["StorekeeperNote"];
                        FormDS?.Init();
                        Form.SetValues(FormDS);
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            Show();
        }

        /// <summary>
        /// Показывает окно
        /// </summary>
        private void Show()
        {
            string title = $"Примечание кладовщика";
            Window = new Window
            {
                Title = title,
                Width = 440,
                Height = 180,
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
            StoreKeeperNote.Focus();

        }

        /// <summary>
        /// Сохранение примечания кладовщика
        /// </summary>
        private async void Save()
        {
            if (Form.Validate())
            {
                var p = Form.GetValues();
                p.Add("ID", ObjectId.ToString());
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", Object);
                q.Request.SetParam("Action", "SaveStorekeeperNote");
                q.Request.SetParams(p);

                await Task.Run(() => {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    //отправляем сообщение Гриду о необходимости обновить данные
                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup = "ShipmentControl",
                        ReceiverName = "Shipment" + Object,
                        SenderName = "StorekeeperNote",
                        Action = "Refresh",
                    });
                }
                else
                {
                    q.ProcessError();
                }

                Close();
            }
        }

        /// <summary>
        /// закрытие окна
        /// </summary>
        public void Close()
        {
            var window = this.Window;
            if (window != null)
            {
                window.Close();
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
