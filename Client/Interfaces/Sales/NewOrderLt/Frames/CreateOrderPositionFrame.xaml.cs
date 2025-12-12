using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
namespace Client.Interfaces.Sales.NewOrderLt.Frames
{
    public partial class CreateOrderPositionFrame : ControlBase
    {
        public CreateOrderPositionFrame()
        {
            InitializeComponent();
            
             InitForm();
            SetDefaults();

            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverGroup == "SelectOrders")
                {
                    if (msg.ReceiverName == ControlName)
                    {
                        ProcessMessage(msg);
                    }
                }
            };

            Commander.SetCurrentGroup("main");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "save",
                    Group = "main_form",
                    Enabled = true,
                    Title = "Сохранить",
                    Description = "Сохранение данных",
                    ButtonUse = true,
                    ButtonName = "SaveButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        Save();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "close",
                    Group = "main_form",
                    Enabled = true,
                    Title = "Отмена",
                    Description = "Закрыть форму без сохранения",
                    ButtonUse = true,
                    ButtonName = "CancelButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        Close();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "help",
                    Group = "main_form",
                    Enabled = true,
                    Description = "Справка",
                    ButtonUse = true,
                    ButtonName = "HelpButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        Central.ShowHelp(DocumentationUrl);
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "product_select",
                    Group = "main_form",
                    Enabled = true,
                    Description = "Выбрать изделие",
                    ButtonUse = true,
                    ButtonName = "ProductSelectButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        var productFrame = new ProductSelectFrame();
                        productFrame.OrderId = OrderId;
                        productFrame.ReceiverName = ControlName;
                        productFrame.Show();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "refresh_list_adrress",
                    Group = "main_form",
                    Enabled = true,
                    Title = "Обновить",
                    Description = "Обновить список с адресами",
                    ButtonUse = true,
                    ButtonName = "RefreshListAddress",
                    MenuUse = false,
                    Action = () =>
                    {
                        GetData();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "add_new_address",
                    Group = "main_form",
                    Enabled = true,
                    Title = "Добавить новый адрес",
                    Description = "Добавить новый адрес",
                    ButtonUse = true,
                    ButtonName = "AddNewAddress",
                    MenuUse = false,
                    Action = () =>
                    {
                        new DeliveryAddresses.ShippingAddressForm(new ItemMessage()
                        {
                            ReceiverName = ControlName,
                            Action = "refresh",
                        }, idBuyer, false);
                    },
                });
            }
            Commander.Init(this);
        }
        
        /// <summary>
        /// Имя вкладки, окуда вызвана форма редактирования
        /// </summary>
        public string ReceiverName;
        /// <summary>
        /// Форма редактирования спецификации
        /// </summary>
        FormHelper Form { get; set; }
        /// <summary>
        /// Идентификатор заявки
        /// </summary>
        public int OrderId { get; set; }
        /// <summary>
        /// Идентификатор позиции заявки
        /// </summary>
        public int OrderPositionId { get; set; }
        /// <summary>
        /// Данные по адресам доставки грузополучателя заявки
        /// </summary>
        private ListDataSet ShipmentAddressDS { get; set; }
        /// <summary>
        /// Значение НДС
        /// </summary>
        private double Vat;
        /// <summary>
        /// Признак, что спецификация
        /// </summary>
        private bool SpecificationLoaded;
        /// <summary>
        /// ИД Покупателя
        /// </summary>
        public int idBuyer { get; set; }

        public bool hasPz { get; set; }
        public bool tenderIs { get; set; }
        public bool editMode { get; set; }
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
                    Path="PRODUCT_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
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
                    Path="CATEGORY_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CategoryId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ADDRESS_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Address,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="DIRECTION",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Direction,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SHIP_ORDER",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ShipmentSequence,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                        { FormHelperField.FieldFilterRef.IsNotZero, null },
                    },
                },
                new FormHelperField()
                {
                    Path="QUANTITY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Quantity,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="SPECFCTN_POSITION_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=SpecificationPosition,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SPECFCTN_POSITION_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=SpecificationPositionId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRICE_SPEC_VAT_EXCLUDED",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=PriceSpecVatExcluded,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    Format="N2",
                },
                new FormHelperField()
                {
                    Path="PRICE_VAT_EXCLUDED",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=PriceVatExcluded,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    Format="N2",
                },
                new FormHelperField()
                {
                    Path="PRICE_WITH_VAT",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=PriceWithVat,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    Format="N2",
                },
                new FormHelperField()
                {
                    Path="FIXED_PRICE_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=FixedPriceFlag,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NOTE_GENERAL",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Note,
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
            Form.SetDefaults();
            ShipmentAddressDS = new ListDataSet();
            ShipmentAddressDS.Init();
            OrderId = 0;
            OrderPositionId = 0;
            ShipmentSequence.Text = "1";
            SpecificationLoaded = false;
        }

        /// <summary>
        /// Обработка сообщений
        /// </summary>
        /// <param name="obj"></param>
        private void ProcessMessage(ItemMessage obj)
        {
            string action = obj.Action;
            if (!action.IsNullOrEmpty())
            {
                switch (action)
                {
                    case "ProductSelect":
                        var v = (Dictionary<string, string>)obj.ContextObject;
                        if (v.ContainsKey("PRODUCT_ID") && v.ContainsKey("CATEGORY_ID"))
                        {
                            v["PRODUCT_NAME"] = $"{v["SKU_CODE"]} {v["PRODUCTS_NAME"]}";
                            Form.SetValues(v);
                            GetCurrentSpecification();
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Получение данных для формы из БД
        /// </summary>
        private async void GetData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "NewOrderLt");
            q.Request.SetParam("Object", "CreateOrderPosition");
            q.Request.SetParam("Action", "GetData");
            q.Request.SetParam("ORDER_ID", OrderId.ToString());
            q.Request.SetParam("POSITION_ID", OrderPositionId.ToString());

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestGridAttempts;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    ShipmentAddressDS = ListDataSet.Create(result, "SHIPMENT_ADDRESS");
                    Address.Items = ShipmentAddressDS.GetItemsList("ID", "ADDRESS");
                    // находим значение НДС из первой строки
                    if (ShipmentAddressDS.Items.Count > 0)
                    {
                        Vat = 1 + ShipmentAddressDS.Items[0].CheckGet("VAT").ToDouble() / 100;
                    }

                    if (OrderPositionId > 0)
                    {
                        var positionDS = ListDataSet.Create(result, "POSITION");
                        // Если спецификация загружена, то отмечаем флаг, чтобы потом не искать
                        if (positionDS.Items.Count > 0)
                        {
                            var item = positionDS.Items[0];
                            var specificationPositionId = item.CheckGet("SPECIFICATION_POSITION_ID").ToInt();
                            if (specificationPositionId > 0)
                            {
                                SpecificationLoaded = true;
                            }
                        }
                        Form.SetValues(positionDS);

                        //Если позиция уже отгружена, блокируем все контролы
                        bool shipped = positionDS.Items[0].CheckGet("SHIPPED").ToBool();
                        if (shipped)
                        {
                            ProductSelectButton.IsEnabled = false;
                            Address.IsReadOnly = true;
                            ShipmentSequence.IsReadOnly = true;
                            ShipmentSequenceInc.IsEnabled = false;
                            ShipmentSequenceDec.IsEnabled = false;
                            Quantity.IsReadOnly = true;
                            PriceVatExcluded.IsReadOnly = true;
                            SaveButton.IsEnabled = false;
                            CancelButton.Content = "Закрыть";
                        }
                    }

                    Show();
                    // Сбрасываем флаг, чтобы загружать спецификации при изменении адреса
                    SpecificationLoaded = false;
                }
            }
        }

        /// <summary>
        /// Вызов формы редактирования позиции заявки
        /// </summary>
        /// <param name="id"></param>
        public void Edit(int id = 0)
        {
            OrderPositionId = id;
            ControlName = $"OrderPosition_{id}";
            // Если включен режим редактирования у заявки то пропускаем этот if и можем редактировать если уже стоит ПЗ
            if (!editMode)
            {
                Quantity.IsReadOnly = hasPz;
                Address.IsReadOnly = tenderIs;
            }
            GetData();
        }

        /// <summary>
        /// Загрузка данных из активной специфкации ценв на выбранное изделие
        /// </summary>
        private async void GetCurrentSpecification()
        {
            int directionId = 0;
            int addressId = Address.SelectedItem.Key.ToInt();
            foreach (var item in ShipmentAddressDS.Items)
            {
                if (item["ID"].ToInt() == addressId)
                {
                    directionId = item["ID_DIR"].ToInt();
                    break;
                }
            }

            if (ProductId.Text.ToInt() > 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "NewOrderLt");
                q.Request.SetParam("Object", "CreateOrderPosition");
                q.Request.SetParam("Action", "GetCurrentSpecification");
                q.Request.SetParam("ORDER_ID", OrderId.ToString());
                q.Request.SetParam("PRODUCT_ID", ProductId.Text);
                q.Request.SetParam("CATEGORY_ID", CategoryId.Text);
                q.Request.SetParam("DIRECTION_ID", directionId.ToString());

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestGridAttempts;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "PRICE");
                        Form.SetValues(ds);

                        // Если цена не заполнена, заполняем ценой из спецификации
                        if (PriceVatExcluded.Text.IsNullOrEmpty())
                        {
                            PriceVatExcluded.Text = PriceSpecVatExcluded.Text;
                        }
                    }
                }
                else if (q.Answer.Error.Code == 145)
                {
                    if (CategoryId.ToInt() == 12)
                    {
                        Form.SetStatus(q.Answer.Error.Message);
                    }

                    if (!SpecificationPosition.Text.IsNullOrEmpty())
                    {
                        SpecificationPosition.Text = "";
                        SpecificationPositionId.Text = "";
                        PriceSpecVatExcluded.Text = "";
                    }
                }
            }
        }

        /// <summary>
        /// Вычисление цены с НДС
        /// </summary>
        private void CalculatePriceWithVat()
        {
            if (!PriceVatExcluded.Text.IsNullOrEmpty())
            {
                var price = PriceVatExcluded.Text.ToDouble();
                if (price > 0)
                {
                    var priceVat = Math.Round(price * Vat, 2);
                    PriceWithVat.Text = priceVat.ToString();
                }
            }
            else
            {
                PriceWithVat.Text = "";
            }
        }

        public void Show()
        {
            if (OrderPositionId > 0)
            {
                ControlTitle = $"Позиция к заявке {OrderPositionId}";
            }
            else
            {
                ControlTitle = "Новая позиция к заявке";
            }

            Central.WM.Show(ControlName, ControlTitle, true, "add", this);
        }

        public void Close()
        {
            Central.WM.Close(ControlName);
            Central.WM.SetActive(ReceiverName);
        }

        /// <summary>
        /// Проверки при сохранении позиции заявки
        /// </summary>
        public void Save()
        {
            if (Form.Validate())
            {
                var v = Form.GetValues();

                v.CheckAdd("POSITION_ID", OrderPositionId.ToString());
                v.CheckAdd("ORDER_ID", OrderId.ToString());

                SaveData(v);
            }
        }

        /// <summary>
        /// Запись данных в БД
        /// </summary>
        /// <param name="p"></param>
        private async void SaveData(Dictionary<string, string> p)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "NewOrderLt");
            q.Request.SetParam("Object", "CreateOrderPosition");
            q.Request.SetParam("Action", "Save");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestGridAttempts;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result.ContainsKey("ITEMS"))
                {
                    // Отправляем сообщение гриду на обновление
                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverName = "OrderPositionGrid",
                        Action = "refresh_order_position_grid",
                        Message = $"{OrderId}",
                    });

                    Close();
                }
            }
        }

        /// <summary>
        ///  Заполняет название направления
        /// </summary>
        private void SetDirection()
        {
            int addressId = Address.SelectedItem.Key.ToInt();
            if (addressId > 0)
            {
                foreach (var item in ShipmentAddressDS.Items)
                {
                    if (addressId == item.CheckGet("ID").ToInt())
                    {
                        Direction.Text = item.CheckGet("DIRECTION");
                    }
                }
            }
        }

        private void ShipmentSequenceInc_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            int num = ShipmentSequence.Text.ToInt();
            num++;
            ShipmentSequence.Text = (num).ToString();
        }

        private void ShipmentSequenceDec_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            int num = ShipmentSequence.Text.ToInt();
            if (num > 1)
            {
                num--;
                ShipmentSequence.Text = (num).ToString();
            }
        }

        private void PriceVatExcluded_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculatePriceWithVat();
        }

        private void Address_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SetDirection();

            if (!SpecificationLoaded)
            {
                // Если заполнено изделие, ищем активную спецификацию для этого изделия
                if (ProductId.Text.ToInt() > 0)
                {
                    GetCurrentSpecification();
                }
            }
        }
    }
}