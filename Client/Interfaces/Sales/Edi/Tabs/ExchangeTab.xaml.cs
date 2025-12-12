using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Sales.Edi
{
    /// <summary>
    /// отлпдочная консоль процесса обмена
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2024-09-27</released>
    /// <changed>2024-09-27</changed>
    public partial class ExchangeTab : ControlBase
    {
        public ExchangeTab()
        {
            InitializeComponent();

            ControlSection = "exchange";
            RoleName = "[erp]edi";
            ControlTitle ="Обмен";
            DocumentationUrl = "/doc/l-pack-erp";

            OnMessage = (ItemMessage m) =>
            {
                if(m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnKeyPressed = (KeyEventArgs e) =>
            {
                if(!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };

            OnLoad =()=>
            {
                FormInit();
                SetDefaults();
            };

            OnUnload=()=>
            {
            };

            OnFocusGot=()=>
            {
            };

            OnFocusLost=()=>
            {                
            };

            OnNavigate = () =>
            {               
            };

            {
                Commander.SetCurrentGroup("main");
                {                   
                    Commander.Add(new CommandItem()
                    {
                        Name = "help",
                        Enabled = true,
                        Title = "Справка",
                        Description = "Показать справочную информацию",
                        ButtonUse = true,
                        ButtonName = "HelpButton",
                        HotKey = "F1",
                        Action = () =>
                        {
                            Central.ShowHelp(DocumentationUrl);
                        },
                    });
                }

                {
                    
                    Commander.Add(new CommandItem()
                    {
                        Name = "man",
                        Enabled = true,
                        ButtonUse = true,
                        ButtonName = "ManButton",
                        Action = () =>
                        {
                            var s = "";
                            s = s.Append("COMMANDS",true);
                            s = s.Append("    GET_INFO", true);
                            s = s.Append("    EVENTS_GET", true);
                            s = s.Append("    EVENTS_PROCESS", true);

                            var v = new Dictionary<string, string>();
                            v.CheckAdd("LOG", s);
                            Form.SetValues(v);
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "cls",
                        Enabled = true,
                        ButtonUse = true,
                        ButtonName = "ClsButton",
                        Action = () =>
                        {
                            var v = new Dictionary<string, string>();
                            v.CheckAdd("LOG", "");
                            Form.SetValues(v);
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "list_params",
                        Enabled = true,
                        ButtonUse = true,
                        ButtonName = "ListParamsButton",
                        Action = () =>
                        {
                            RequestListParams();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "auth",
                        Enabled = true,
                        ButtonUse = true,
                        ButtonName = "AuthButton",
                        Action = () =>
                        {
                            RequestAuth();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "get_status",
                        Enabled = true,
                        ButtonUse = true,
                        ButtonName = "GetStatusButton",
                        Action = () =>
                        {
                            GetStatus();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "test_command",
                        Enabled = true,
                        ButtonUse = true,
                        ButtonName = "TestCommandButton",
                        Action = () =>
                        {
                            TestCommand();
                        },
                    });




                }

                Commander.Init(this);
            }

        }

        public FormHelper Form { get; set; }

        public void FormInit()
        {
            {
                Form = new FormHelper();
                var fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="SEARCH",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=OrderGridSearch,                        
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path="LOG",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=Log,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path="COMMAND",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=Command,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    
                };
                Form.SetFields(fields);
            }
        }

        public void SetDefaults()
        {
            Form.SetDefaults();
        }

        public async void Request(string action, string command="")
        {
            bool resume = true;
            var s = "";

            if(resume)
            {
                var p = new Dictionary<string, string>();
                {
                    if(!command.IsNullOrEmpty())
                    {
                        p.CheckAdd("COMMAND", command);
                    }
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "EdiExchange");
                q.Request.SetParam("Action", action);
                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if(q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if(result != null)
                    {
                        {
                            var ds = ListDataSet.Create(result, "REPORT");
                            if(ds.Items.Count > 0)
                            {
                                var row = ds.GetFirstItem();
                                var complete = row.CheckGet("RESULT").ToString();
                                var log = row.CheckGet("LOG").ToString();

                                s = s.Append($"ACTION=[{q.Request.Params.CheckGet("Action")}] STATUS=[{q.Answer.Status}] TIME=[{q.Answer.Time}] COMPLETE=[{complete}]", true);
                                s = s.Append($"{log}", true);
                            }
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            var v = new Dictionary<string, string>();
            v.CheckAdd("LOG", s);
            Form.SetValues(v);
        }

        public void RequestListParams()
        {
            Request("ListParams");
        }

        public void RequestAuth()
        {
            Request("Auth");
        }

        public void GetStatus()
        {
            Request("GetStatus");
        }

        public void TestCommand()
        {
            var v = Form.GetValues();
            var command = v.CheckGet("COMMAND");
            Request("TestCommand", command);
        }
        
    }
}
