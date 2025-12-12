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
    /// Форма изменения листов заготовок для образцов
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleCardboardEditQty : UserControl
    {
        public SampleCardboardEditQty()
        {
            InitializeComponent();

            InitForm();
            SetDefaults();
        }

        public string ReceiverName;
        public string TabName;
        public int PreformIdValue;

        /// <summary>
        /// форма
        /// </summary>
        public FormHelper Form { get; set; }
        /// <summary>
        /// Окно
        /// </summary>
        public Window Window { get; set; }

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
                    Path="QTY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PreformQty,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                        { FormHelperField.FieldFilterRef.MinValue, 0 },
                        { FormHelperField.FieldFilterRef.MaxValue, 100 },
                    },
                },
                new FormHelperField()
                {
                    Path="RACK_NUM",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=RackNum,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="PLACE_NUM",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PlaceNum,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="NOTE_CARTON",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Note,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NUM",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                }
            };

            Form.SetFields(fields);
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();
            TabName = "EditPreforms";
            PreformIdValue = 0;
            Status.Text = "";
        }

        /// <summary>
        /// Остановка вспомогательных процессов при закрытии вкладки
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Preproduction",
                ReceiverName = ReceiverName,
                SenderName = TabName,
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            if (!ReceiverName.IsNullOrEmpty())
            {
                Central.WM.SetActive(ReceiverName, true);
                ReceiverName = "";
            }
        }

        public void Edit(int preformId)
        {
            PreformIdValue = preformId;
            GetData();
            Show();
        }

        private async void GetData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "SampleCardboards");
            q.Request.SetParam("Action", "Get");
            q.Request.SetParam("ID", PreformIdValue.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "SampleCardboard");
                    Form.SetValues(ds);
                }
            }

        }

        /// <summary>
        /// Отображение окна
        /// </summary>
        public void Show()
        {
            Central.WM.AddTab($"{TabName}_{PreformIdValue}", $"Заготовка {PreformIdValue}", true, "add", this);
        }

        public void Close()
        {
            Central.WM.RemoveTab($"{TabName}_{PreformIdValue}");
            Destroy();
        }

        public void Save()
        {
            var validationResult = Form.Validate();
            string error = "";

            if (validationResult)
            {
                var v = Form.GetValues();
                SaveData(v);
            }
            else
            {
                error = "Не все обязательные поля заполнены верно";
                Form.SetStatus(error, 1);
            }


        }

        private async void SaveData(Dictionary<string,string> p)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "SampleCardboards");
            q.Request.SetParam("Action", "Save");
            q.Request.SetParam("ID", PreformIdValue.ToString());
            q.Request.SetParams(p);

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
                        //отправляем сообщение о необходимости обновить грид
                        Messenger.Default.Send(new ItemMessage()
                        {
                            ReceiverGroup = "Preproduction",
                            ReceiverName = ReceiverName,
                            SenderName = TabName,
                            Action = "Refresh",
                        });
                        // Отправляем сообщение гриду в новую шину
                        Central.Msg.SendMessage(new ItemMessage()
                        {
                            ReceiverGroup = "Preproduction",
                            ReceiverName = ReceiverName,
                            SenderName = TabName,
                            Action = "Refresh",
                        });
                        Close();
                    }
                }
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
