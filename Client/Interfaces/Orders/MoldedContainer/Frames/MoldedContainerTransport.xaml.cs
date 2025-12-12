using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Client.Interfaces.Orders.MoldedContainer
{
    /// <summary>
    /// Создание отгрузки для заявки по литой таре
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class MoldedContainerTransport : ControlBase
    {
        public MoldedContainerTransport()
        {
            DocumentationUrl = "/doc/l-pack-erp/orders/molded_container_order";
            InitializeComponent();

            InitForm();

            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverName == ControlName)
                {
                    ProcessMessage(msg);
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
                    Name = "transport_select",
                    Group = "main_form",
                    Enabled = true,
                    Description = "Выбрать водителя",
                    ButtonUse = true,
                    ButtonName = "TransportSelectButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        var transportFrame = new MoldedContainerTransportSelect();
                        transportFrame.ReceiverName = ControlName;
                        transportFrame.Show(DriverId.Text.ToInt());
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
        /// Форма редактирования техкарты
        /// </summary>
        FormHelper Form { get; set; }
        /// <summary>
        /// ID заявки для создания
        /// </summary>
        public int OrderId = 0;
        /// <summary>
        /// ID отгрузки при редактировании
        /// </summary>
        public int ShipmentId = 0;

        public bool SelfShipmentFlag = false;
        /// <summary>
        /// ID плательщика за перевозку. Передаем ID продавца из заявки
        /// </summary>
        public int PayerId;

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
                    case "TransportSelect":
                        var v = (Dictionary<string, string>)obj.ContextObject;
                        if (v.ContainsKey("ID") && v.ContainsKey("DRIVERNAME"))
                        {
                            var f = new Dictionary<string, string>()
                            {
                                { "DRIVER_NAME", v["DRIVERNAME"] },
                                { "ID_DR", v["ID"] },
                            };
                            Form.SetValues(f);
                        }
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
                    Path="DATETS",
                    FieldType=FormHelperField.FieldTypeRef.DateTime,
                    Control=ShipmentDate,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="TMSHIP",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ShipmentTime,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="ID_DR",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=DriverId,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        //{ FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="DRIVER_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=DriverName,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="ID_TR",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Transporter,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="VEHICLE_TYPE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=VehicleType,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="COMMENTS",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Comment,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            Form.SetFields(fields);
            Form.StatusControl = FormStatus;
        }

        private async void GetData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Orders");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "GetShipment");
            q.Request.SetParam("SHIPMENT_ID", ShipmentId.ToString());

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
                    // Перевозчики
                    var transporterDS = ListDataSet.Create(result, "TRANSPORTERS");
                    Transporter.Items = transporterDS.GetItemsList("TRANSPORTER_ID", "TRANSPORTER_NAME");

                    // Типы ТС
                    var vehicleTypes = new Dictionary<string, string>
                    {
                        { "1", "Грузовое ТС" },
                        { "2", "Вагон" },
                        { "3", "Газель" },
                    };
                    VehicleType.Items = vehicleTypes;

                    if (result.ContainsKey("SHIPMENT"))
                    {
                        var ds = ListDataSet.Create(result, "SHIPMENT");
                        if (ds.Items.Count > 0)
                        {
                            var item = ds.Items[0];
                            var dt = item.CheckGet("DATETS");
                            if (dt.Length > 10)
                            {
                                item["DATETS"] = dt.Substring(0, 10);
                            }
                            var tm = item.CheckGet("TMSHIP");
                            if (tm.Length > 10)
                            {
                                item["TMSHIP"] = tm.Substring(11);
                            }
                        }
                        Form.SetValues(ds);
                    }
                    else
                    {
                        VehicleType.SetSelectedItemByKey("1");
                        if (SelfShipmentFlag)
                        {
                            DriverName.Text = "самовывоз";
                            DriverId.Text = "1095";
                        }
                        else
                        {
                            DriverName.Text = "неизвестно";
                            DriverId.Text = "2659";

                        }
                    }
                }


                Show();
            }
        }

        /// <summary>
        /// Открываем форму редактирования для отгрузки
        /// </summary>
        /// <param name="id"></param>
        public void ShowTab(Dictionary<string, string> values)
        {
            ShipmentId = values.CheckGet("SHIPMENT_ID").ToInt();
            OrderId = values.CheckGet("ORDER_ID").ToInt();
            PayerId = values.CheckGet("PAYER_ID").ToInt();
            ShipmentDate.Text = values.CheckGet("DEFAULT_DATE");

            GetData();
        }

        public void Save()
        {
            if (Form.Validate())
            {
                bool resume = true;
                var v = Form.GetValues();

                if (resume)
                {
                    v.CheckAdd("SHIPMENT_ID", ShipmentId.ToString());
                    v.CheckAdd("ORDER_ID", OrderId.ToString());
                    v.CheckAdd("PAYER_ID", PayerId.ToString());
                    SaveData(v);
                }
                else
                {
                    Form.SetStatus("Не все поля заполнены верно", 1);
                }
            }
        }

        public void Show()
        {
            ControlTitle = "Новая отгрузка";
            Central.WM.Show(ControlName, ControlTitle, true, "add", this);
        }

        private async void SaveData(Dictionary<string, string> v)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Orders");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "SaveShipment");
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
                    if (result.ContainsKey("ITEMS"))
                    {
                        Central.Msg.SendMessage(new ItemMessage()
                        {
                            ReceiverGroup = "Orders",
                            ReceiverName = ReceiverName,
                            SenderName = ControlName,
                            Action = "RefreshShipments",
                        });

                        Close();
                    }
                }
            }
        }

        public void Close()
        {
            Central.WM.Close(ControlName);
            Central.WM.SetActive(ReceiverName);
        }
    }
}
