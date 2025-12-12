using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Client.Common;
using Client.Interfaces.Main;
using Gu.Wpf.DataGrid2D;
using Newtonsoft.Json;

namespace Client.Interfaces.Сounterparty.Customers.Frames
{
    public partial class LinkCustomersProductFrame : ControlBase
    {
        public LinkCustomersProductFrame()
        {
            InitializeComponent();

            OnLoad = () =>
            {
                FormInit();
            };

            OnGetFrameTitle = () =>
            {
                var result = "Привязывание изделий";
                
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
                        Action = Save
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
        
        private FormHelper Form { get; set; }
        private string SelectCustomerId { get; set; }
        
        private void FormInit()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path = "BUYER",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = BuyerSelectBox,
                    ControlType = "SelectBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>
                    {},
                    OnChange = (FormHelperField f, string v) =>
                    {
                        Form.Fields.ForEach(fs =>
                        {
                            if (fs.Path == "PRODUCT_CUSTOMER")
                            {
                                fs.UpdateItems();
                            }
                        });
                    },
                    QueryLoadItems = new RequestData()
                    {
                        Module = "Сounterparty",
                        Object = "Customers",
                        Action = "ListBuyer",
                        AnswerSectionKey = "ITEMS",
                        OnComplete = (FormHelperField f, ListDataSet ds) =>
                        {
                            var row = new Dictionary<string, string>()
                            {};
                            ds.ItemsPrepend(row);
                            var list = ds.GetItemsList("ID_POK", "NAME");
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
                    Path = "PRODUCT_CUSTOMER",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = ProductSelectBox,
                    ControlType = "SelectBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>
                        {},
                    OnChange = (FormHelperField f, string v) => { },
                    QueryLoadItems = new RequestData()
                    {
                        Module = "Сounterparty",
                        Object = "Customers",
                        Action = "ListForSelect",
                        AnswerSectionKey = "ITEMS",
                        BeforeRequest = rd =>
                        {
                            var f = Form.GetValues();
                            
                            rd.Params = new Dictionary<string, string>()
                            {
                                { "ID_POK", f.CheckGet("BUYER").ToInt().ToString() }
                            };
                        },
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
                                c.SetSelectedItemByKey(SelectCustomerId);
                                c.IsReadOnly = true;
                            }
                        }
                    }
                },
            };
            
            Form.SetFields(fields);
        }

        private async void Save()
        {
            var f = Form.GetValues();
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Сounterparty");
            q.Request.SetParam("Object", "Customers");
            q.Request.SetParam("Action", "SaveLinkWithBuyer");
            q.Request.SetParam("CUSTOMER", ProductSelectBox.SelectedItem.Value);
            q.Request.SetParam("CUST_ID", SelectCustomerId.ToInt().ToString());
            q.Request.SetParam("ID_POK", f.CheckGet("BUYER").ToInt().ToString());
            
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
                    Action = "refresh_buyer_grid",
                    Message = "ID", 
                });
                
                Close();
            }
        }

        public void Create(string selectCustomerId)
        {
            SelectCustomerId = selectCustomerId;
            Show();
        }
    }
}