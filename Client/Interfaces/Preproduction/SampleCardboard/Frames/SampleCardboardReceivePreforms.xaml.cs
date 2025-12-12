using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Окно ввода количества листов и места хранения изготовленных на ГА заготовок для образцов
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleCardboardReceivePreforms : UserControl
    {
        public SampleCardboardReceivePreforms()
        {
            InitializeComponent();
            InitForm();
            SetDefaults();
        }

        public string ReceiverName;
        public string TabName;
        /// <summary>
        /// ID изделия
        /// </summary>
        int ProductIdValue;

        public int CardboardNumber;

        /// <summary>
        /// форма
        /// </summary>
        public FormHelper Form { get; set; }
        /// <summary>
        /// Окно
        /// </summary>
        public Window Window { get; set; }

        public void InitForm()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="ID2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ProductId,
                },
                new FormHelperField()
                {
                    Path="QTY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PreformQty,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                        { FormHelperField.FieldFilterRef.MinValue, 1 },
                        { FormHelperField.FieldFilterRef.MaxValue, 100 },
                    },
                },
                new FormHelperField()
                {
                    Path="RACK_NUM",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=RackNum,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PLACE_NUM",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PlaceNum,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
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
            };

            Form.SetFields(fields);

        }

        public void SetDefaults()
        {
            Form.SetDefaults();
            TabName = "ReceivePreforms";
            ProductIdValue = 0;
            Status.Text = "";
            CardboardNumber = 0;
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
                SenderName = "CreateTask",
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

        public void Edit(int productId)
        {
            ProductIdValue = productId;
            var v = new Dictionary<string, string>();
            v.CheckAdd("ID2", productId.ToString());
            Form.SetValues(v);
            Show();
        }

        /// <summary>
        /// Отображение окна
        /// </summary>
        public void Show()
        {
            Central.WM.AddTab($"{TabName}_{ProductIdValue}", "Добавить заготовки", true, "add", this);
        }

        public void Close()
        {
            Central.WM.RemoveTab($"{TabName}_{ProductIdValue}");
            Destroy();
        }

        public async void Save()
        {
            string error = "";
            var validationResult = Form.Validate();
            if (validationResult)
            {
                var p = Form.GetValues();
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "SampleCardboards");
                q.Request.SetParam("Action", "PreformAppend");
                q.Request.SetParam("MANUAL", "0");
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
                            // Выводим ярлык для печати в графический файл
                            var imageGen = new TextImageGenerator(240, "Arial", 40);
                            var bitmap = imageGen.CreateBitmap(CardboardNumber.ToString());
                            bitmap.RotateFlip(System.Drawing.RotateFlipType.Rotate90FlipNone);

                            var path = Path.GetTempPath();
                            var fileName = Path.Combine(path, "cardboard_label.jpg");
                            bitmap.Save(fileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                            Central.OpenFile(fileName);

                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup = "Preproduction",
                                ReceiverName = ReceiverName,
                                SenderName = TabName,
                                Action = "Refresh",
                            });
                        }

                        Close();
                    }
                }
            }
            else
            {
                error = "Не все обязательные поля заполнены верно";
                Form.SetStatus(error, 1);

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
