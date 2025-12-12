using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Форма редактирования образца с линии
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleProduction : UserControl
    {
        public SampleProduction()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            InitForm();
            SetDefaults();
        }

        /// <summary>
        /// ID образца
        /// </summary>
        int SampleId;

        /// <summary>
        /// Статус редактируемого образца
        /// </summary>
        int Status;

        /// <summary>
        /// Признак, что образец получен из ЛК клиента. Информация о клиенте недоступна для редактирования
        /// </summary>
        bool FromWeb;

        /// <summary>
        /// Форма редактирования образца
        /// </summary>
        FormHelper Form { get; set; }

        /// <summary>
        /// Имя вкладки, куда происходит возврат при закрытии этой вкладки
        /// </summary>
        public string ReceiverName;

        /// <summary>
        /// Имя этой вкладки
        /// </summary>
        public string TabName;

        /// <summary>
        /// Обработка сообщений
        /// </summary>
        /// <param name="obj"></param>
        private void ProcessMessages(ItemMessage obj)
        {
            if (obj.ReceiverGroup.IndexOf("PreproductionSample") > -1)
            {
                if (obj.ReceiverName.IndexOf(TabName) > -1)
                {
                    if (obj.Action == "SelectProduct")
                    {
                        var v = (Dictionary<string, string>)obj.ContextObject;
                        SetProduct(v);
                    }
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
                    Path="ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="CUSTOMER_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CustomerName,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DT_CREATED",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CreatedDate,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    Format="dd.MM.yyyy HH:mm",
                },
                new FormHelperField()
                {
                    Path="NUM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Num,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ProductId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Product,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_CATEGORY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ProductCategory,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="QTY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=EditQty,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DELIVERY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=DeliveryType,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="MANAGER_EMPL_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ManagerName,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=EditNote,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PACKING_TYPE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PackingType,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };
            Form.SetFields(fields);

            Form.StatusControl = FormStatus;
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            TabName = "";
            Status = 0;
            FromWeb = false;
            Form.SetDefaults();

            // типы доставки. по умолчанию - из липецкого офиса
            DeliveryType.Items = DeliveryTypes.Items;
            DeliveryType.SelectedItem = DeliveryTypes.Items.GetEntry("1");
            // типы упаковки образца
            PackingType.Items = PackingTypes.Items;
            PackingType.SelectedItem = PackingTypes.Items.GetEntry("1");
            CreatedDate.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
        }

        public void Edit(int id)
        {
            SampleId = id;
            GetData();
        }

        /// <summary>
        /// Настройка доступности полей
        /// </summary>
        private void SetReadOnly()
        {
            // Изменять изделие можно только в момент создания заявки на образец
            if (SampleId > 0)
            {
                Product.IsReadOnly = true;
                ProductButton.IsEnabled = false;
            }

            // Образец создан в ЛК клиента
            if (FromWeb)
            {
                CustomerName.IsReadOnly = true;
                Num.IsReadOnly = true;
            }

            // Изготовлен и далее
            if (Status >= SampleStates.Produced)
            {
                EditQty.IsReadOnly = true;
                PackingType.IsEnabled = false;
            }

            if ((Status == SampleStates.Received) || (Status == SampleStates.Shipped))
            {
                SaveButton.IsEnabled = false;
                DeliveryType.IsReadOnly = true;
                ManagerName.IsReadOnly = true;
                EditNote.IsReadOnly = true;
            }

        }

        /// <summary>
        /// Получение данных для образца
        /// </summary>
        private async void GetData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "GetProduction");
            q.Request.SetParam("ID", SampleId.ToString());

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
                    // Покупатель
                    var CustomerDS = ListDataSet.Create(result, "Customers");
                    CustomerName.Items = CustomerDS.GetItemsList("ID", "NAME");

                    // менеджер
                    // менеджер
                    string managerKey = "MANAGERS";
                    if (result.ContainsKey("USER_GROUP"))
                    {
                        managerKey = "USER_GROUP";
                    }
                    var managersDS = ListDataSet.Create(result, managerKey);
                    var list = new Dictionary<string, string>()
                    {
                        { "0", " " },
                    };
                    foreach (var item in managersDS.Items)
                    {
                        list.CheckAdd(item["ID"].ToInt().ToString(), item["FIO"]);
                    }
                    ManagerName.Items = list;

                    var values = new Dictionary<string, string>();
                    if (SampleId > 0)
                    {
                        var ds = ListDataSet.Create(result, "SampleRec");
                        if (ds.Items.Count > 0)
                        {
                            values = ds.Items.First();
                        }
                    }
                    else
                    {
                        // Если активный пользователь есть в списке менеджеров, выберем его при создании образца
                        string emplId = Central.User.EmployeeId.ToString();
                        if (list.ContainsKey(emplId))
                        {
                            values.CheckAdd("MANAGER_EMPL_ID", emplId);
                        }
                    }
                    values.Add("ID", SampleId.ToString());

                    Status = values.CheckGet("STATUS").ToInt();
                    FromWeb = values.CheckGet("WEB").ToBool();

                    Form.SetValues(values);
                    Show();
                }
            }
        }

        /// <summary>
        /// Отображение вкладки редактирования образца
        /// </summary>
        private void Show()
        {
            string title = $"Образец {SampleId}";
            if (SampleId == 0)
            {
                title = "Новый с линии";
            }
            TabName = $"SampleProduction_{SampleId}";
            Central.WM.AddTab(TabName, title, true, "add", this);
            SetReadOnly();
        }

        private void SetProduct(Dictionary<string, string> product)
        {
            if (product.Count > 0)
            {
                int productId = product.CheckGet("ID").ToInt();
                int productCategory = product.CheckGet("CATEGORY").ToInt();

                if (productId > 0 && productCategory > 0)
                {
                    var v = new Dictionary<string, string>()
                    {
                        { "PRODUCT_ID", productId.ToString() },
                        { "PRODUCT_CATEGORY", productCategory.ToString() },
                        { "PRODUCT_NAME", product.CheckGet("PRODUCT_NAME") }
                    };
                    Form.SetValues(v);
                }
                else
                {
                    Form.SetStatus("Ошибка получения изделия", 1);
                }
            }
            else
            {
                Form.SetStatus("Ошибка получения изделия", 1);
            }
        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
        public void Close()
        {
            Central.WM.RemoveTab(TabName);

            //отправляем сообщение о закрытии вкладки
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "PreproductionSample",
                ReceiverName = "",
                SenderName = TabName,
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            Central.WM.SetActive(ReceiverName, true);
            ReceiverName = "";
        }

        /// <summary>
        /// Проверки перед сохранением
        /// </summary>
        private void Save()
        {
            bool resume = true;
            var v = Form.GetValues();
            string msg = "";
            var qty = v.CheckGet("QTY").ToInt();

            if (resume)
            {
                var customerId = v.CheckGet("CUSTOMER_ID").ToInt();
                if (customerId == 0)
                {
                    resume = false;
                    msg = "Не выбран покупатель";
                }
            }

            if (resume)
            {
                var managerId = v.CheckGet("MANAGER_EMPL_ID").ToInt();
                if (managerId == 0)
                {
                    resume = false;
                    msg = "Не выбран менеджер";
                }
            }

            if (resume)
            {
                
                if (qty == 0)
                {
                    resume = false;
                    msg = "Не задано количество";
                }
            }

            if (resume)
            {
                var product = v.CheckGet("PRODUCT_ID").ToInt();
                if (product == 0)
                {
                    resume = false;
                    msg = "Не выбрано изделие";
                }
            }

            if (resume)
            {
                // Если количество больше 10, отправляем на подтверждение, если нет - сразу в работу
                if (Status == 0)
                {
                    if (qty > 10)
                    {
                        v.CheckAdd("STATUS", "0");
                        v.CheckAdd("CONFIRMATION", "1");
                    }
                    else
                    {
                        v.CheckAdd("STATUS", "1");
                        v.CheckAdd("CONFIRMATION", "0");
                    }
                }

                SaveData(v);
            }
            else
            {
                Form.SetStatus(msg, 1);
            }
        }

        /// <summary>
        /// Сохранение данных в БД
        /// </summary>
        /// <param name="p"></param>
        private async void SaveData(Dictionary<string, string> p)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "SaveProduction");
            q.Request.SetParams(p);

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
                        //отправляем гриду сообщение о необходимости обновления
                        Messenger.Default.Send(new ItemMessage()
                        {
                            ReceiverGroup = "PreproductionSample",
                            ReceiverName = ReceiverName,
                            SenderName = "SampleProduction",
                            Action = "Refresh",
                        });
                    }
                }
                Close();
            }
        }

        private void ProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (CustomerName.SelectedItem.Key.ToInt() != 0)
            {
                var selectProduct = new SampleSelectProduct();
                selectProduct.CustomerId = CustomerName.SelectedItem.Key.ToInt();
                selectProduct.ReceiverName = TabName;
                selectProduct.Show();
            }
            else
            {
                var dw = new DialogWindow("Выберите покупателя", "Образец с линии");
                dw.ShowDialog();
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
