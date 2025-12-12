using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;

namespace Client.Interfaces.Сounterparty.Customers
{
    public partial class CustomersEditFrame : ControlBase
    {
        public CustomersEditFrame()
        {
            _customerId = 0;
            
            InitializeComponent();
            FormInit();

            OnGetFrameTitle = () =>
            {
                var result = "";

                if (_customerId != 0)
                {
                    return $"Потребитель {_customerId}";
                }
                
                return result;
            };
                
            {
                Commander.SetCurrentGroup("main");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "save",
                        Enabled = true,
                        Title = "Сохранить",
                        ButtonUse = true,
                        ButtonName = "SaveButton",
                        Action = SaveCustomer
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "cancel",
                        Enabled = true,
                        Title = "Отмена",
                        ButtonUse = true,
                        ButtonName = "CancelButton",
                        Action = Close
                    });
                }
                Commander.Init(this);
            }

        }
        
        private int _customerId;
        private ListDataSet CustomerInfo { get; set; }
        private FormHelper Form { get; set; }

        private void FormInit()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path = "CUSTOMER",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = Customer,
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> { },
                },
                new FormHelperField()
                {
                    Path = "DEALER",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = Dealer,
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> { },
                },
                new FormHelperField()
                {
                    Path = "SHELF_LIFE",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = ShelfLife,
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> { },
                },
                new FormHelperField
                {
                    Path = "TOVAR_DETAILS",
                    FieldType = FormHelperField.FieldTypeRef.Boolean,
                    Control = TovarDetail,
                    ControlType = "CheckBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> { },
                },
                new FormHelperField
                {
                    Path = "TOVAR_DETAILS_ALL",
                    FieldType = FormHelperField.FieldTypeRef.Boolean,
                    Control = TovarDetailAll,
                    ControlType = "CheckBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> { },
                },
                new FormHelperField()
                {
                    Path = "UNIT_MEASURE",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = UnitMeasure,
                    ControlType = "SelectBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>
                    {},
                    OnChange = (FormHelperField f, string v) => { },
                    QueryLoadItems = new RequestData()
                    {
                        Module = "Сounterparty",
                        Object = "Customers",
                        Action = "UnitMeasureList",
                        AnswerSectionKey = "ITEMS",
                        OnComplete = (FormHelperField f, ListDataSet ds) =>
                        {
                            var row = new Dictionary<string, string>()
                            {};
                            ds.ItemsPrepend(row);
                            var list = ds.GetItemsList("ID", "NAME");
                            var c = (SelectBox)f.Control;
                            if (c != null)
                            {
                                c.Items = list;
                            }
                        }
                    }
                },
                new FormHelperField()
                {
                    Path = "CUST_ID_GROUP",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = SelectGroupCustomer,
                    ControlType = "SelectBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>
                        {},
                    OnChange = (FormHelperField f, string v) => { },
                    QueryLoadItems = new RequestData()
                    {
                        Module = "Сounterparty",
                        Object = "Customers",
                        Action = "ListGroup",
                        AnswerSectionKey = "ITEMS",
                        OnComplete = (FormHelperField f, ListDataSet ds) =>
                        {
                            var row = new Dictionary<string, string>()
                                {};
                            ds.ItemsPrepend(row);
                            var list = ds.GetItemsList("CUST_ID", "CUSTOMER");
                            var c = (SelectBox)f.Control;
                            if (c != null)
                            {
                                c.Items = list;
                            }
                        }
                    }
                },
                
            };
            
            Form.SetFields(fields);
            Form.StatusControl = FormStatus;
        }

        private async void LoadItem()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Сounterparty");
            q.Request.SetParam("Object", "Customers");
            q.Request.SetParam("Action", "Get");
            q.Request.SetParam("ID", _customerId.ToString());
            
            q.Request.Timeout = Central.Parameters.RequestTimeoutDefault;
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
                    CustomerInfo = ListDataSet.Create(result, "ITEM");
                }
                
                Form.SetValues(CustomerInfo);
                
                UnitMeasure.SetSelectedItemByKey(CustomerInfo.GetFirstItemValueByKey("IDIZM2"));

                var custGroupId = CustomerInfo.Items.First().CheckGet("CUST_ID_GROUP");
                
                if (!string.IsNullOrEmpty(custGroupId))
                {
                    SelectGroupCustomer.SetSelectedItemByKey(custGroupId);
                }
                
                Customer.IsReadOnly = true;
                
                Show();
            }
        }
        
        private async void SaveCustomer()
        {
            var f = Form.GetValues();
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Сounterparty");
            q.Request.SetParam("Object", "Customers");
            q.Request.SetParam("Action", "Save");
            q.Request.SetParam("DEALER", f.CheckGet("DEALER"));
            q.Request.SetParam("SHELF_LIFE", f.CheckGet("SHELF_LIFE"));
            q.Request.SetParam("IDIZM2", UnitMeasure.SelectedItem.Key);
            q.Request.SetParam("ID", _customerId.ToString());
            q.Request.SetParam("TOVAR_DETAILS", f.CheckGet("TOVAR_DETAILS"));
            q.Request.SetParam("TOVAR_DETAILS_ALL", f.CheckGet("TOVAR_DETAILS_ALL"));
            q.Request.SetParam("CUST_ID_GROUP", SelectGroupCustomer.SelectedItem.Key);
            q.Request.SetParam("CUSTGROUP", SelectGroupCustomer.SelectedItem.Value);
            
            q.Request.Timeout = Central.Parameters.RequestTimeoutDefault;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;
            
            await Task.Run(() =>
            {
                q.DoQuery();
            });
            
            if (q.Answer.Status == 0)
            {
                
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverName = "CustomersGrid",
                    Action = "refresh_customers_grid",
                    Message = $"{_customerId}", 
                });
                
                Close();
            }
        }
        

        public void Edit(int id)
        {
            _customerId = id;
            LoadItem();
        }
    }
}