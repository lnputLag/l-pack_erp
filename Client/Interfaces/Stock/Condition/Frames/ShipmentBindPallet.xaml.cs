using Client.Assets.HighLighters;
using Client.Common;
using Client.Common.Extensions;
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

namespace Client.Interfaces.Stock.Condition
{
    /// <summary>
    /// привязка погрузки к поддону
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2025-09-05</released>
    /// <changed>2025-09-05</changed>
    public partial class ShipmentBindPallet:ControlBase
    {
        public ShipmentBindPallet()
        {
            InitializeComponent();

            ControlSection = "stock_condition";
            RoleName = "[erp]warehouse_condition";
            ControlTitle ="Привязка отгрузки к поддону";
            FrameTitle=ControlTitle;
            DocumentationUrl = "/doc/l-pack-erp/warehouse/condition/product";

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
                GridInit();                
            };

            OnUnload=()=>
            {
                Grid.Destruct();
            };

            OnFocusGot=()=>
            {
                Grid.ItemsAutoUpdate=true;
                Grid.Run();
            };

            OnFocusLost=()=>
            {
                Grid.ItemsAutoUpdate=false;
            };

            OnNavigate = () =>
            {
            };

            {
                Commander.SetCurrentGroup("main");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "refresh",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить данные",
                        ButtonUse = true,
                        ButtonName = "RefreshButton",                        
                        MenuUse = true,
                        AccessLevel = Role.AccessMode.ReadOnly,
                        Action = () =>
                        {
                            Grid.LoadItems();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "help",
                        Enabled = true,
                        Title = "Справка",
                        Description = "Показать справочную информацию",
                        ButtonUse = true,
                        ButtonName = "HelpButton",                        
                        HotKey = "F1",
                        AccessLevel = Role.AccessMode.ReadOnly,
                        Action = () =>
                        {
                            Central.ShowHelp(DocumentationUrl);
                        },
                    });
                }

                Commander.SetCurrentGridName("Grid");
                {
                    Commander.SetCurrentGroup("item");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "bind",
                            Title = "Привязать",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "BindButton",
                            HotKey = "Return|DoubleCLick",
                            AccessLevel = Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var row=Grid.SelectedItem;
                                Bind(row);
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var row=Grid.SelectedItem;
                                if(row.CheckGet("APPLICATION_POSITION_ID").ToInt() !=0)
                                {
                                    result=true;
                                }
                                return result;
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "bind",
                            Title = "Отмена",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "CancelButton",
                            HotKey = "Escape",
                            AccessLevel = Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                Close();
                            },
                        });
                    }
                }

                Commander.Init(this);
            }

            Item=new Dictionary<string, string>();
        }

        public Dictionary<string,string> Item {get;set;}
        
        private FormHelper Form { get; set; }
        private string DateStart { get; set; }=DateTime.Now.ToString("dd.MM.yyyy 00:00:00");

        private void SetDefaults()
        {         
            Form.SetDefaults();
        }

        private void GridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=2,
                },
                new DataGridHelperColumn
                {
                    Header="Дата отгрузки",
                    Path="SHIPMENT_DATE",
                    ColumnType=ColumnTypeRef.DateTime,
                    Format="dd.MM.yyyy HH:mm",
                    Width2=18,
                },
                new DataGridHelperColumn
                {
                    Header="Покупатель",
                    Doc="Наименование покупателя",
                    Path="BUYER_TITLE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="Количество",
                    Doc="Количество в заявке, шт",
                    Path="APPLICATION_QUANTITY",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="На складе",
                    Doc="Количество, доступное на складе, шт",
                    Path="STOCK_QUANTITY",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="ИД заявки",
                    Path="APPLICATION_POSITION_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
            };            
            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("_ROWNUMBER");
            Grid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.SearchText = GridSearch;
            Grid.Toolbar = GridToolbar; 
            Grid.UseProgressBar=true;                
            Grid.QueryLoadItems = new RequestData()
            {
                Module = "Stock",
                Object = "Product",
                Action = "ListBinding",
                AnswerSectionKey = "ITEMS",     
                BeforeRequest = (RequestData rd) =>
                {
                    rd.Params = new Dictionary<string, string>()
                    {
                        {"FACTORY_ID", Item.CheckGet("FACTORY_ID")},
                        {"PRODUCT_ID", Item.CheckGet("PRODUCT_ID")},
                    };
                },                
            };        
            Grid.Commands = Commander;
            Grid.Init();            
        }

        private void FormInit()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="SEARCH",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=GridSearch,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
            };
            Form.SetFields(fields);
        }
    
        /// <summary>
        /// привязать отгрузку к поддону
        /// </summary>
        private void Bind(Dictionary<string,string> row)
        {
            var resume=true;
            if(resume)
            {
                var s="";
                s=s.Append($"Привязать отгрузку к поддону?",true);
                s=s.Append($"ИД заявки: {row.CheckGet("APPLICATION_POSITION_ID").ToInt()}",true);                

                var r=DialogWindow.ShowDialog(
                    s, 
                    $"Привязка отгрузки", 
                    "", 
                    DialogWindowButtons.NoYes
                );

                if(!(bool)r)
                {
                    resume=false;
                }
            }

            if(resume)
            {
                var p=new Dictionary<string,string>();
                {
                    p.CheckAdd("APPLICATION_POSITION_ID", row.CheckGet("APPLICATION_POSITION_ID"));
                    p.CheckAdd("PRODUCT_ID", Item.CheckGet("PRODUCT_ID"));
                    p.CheckAdd("INCOME_PRODUCTION_TASK_ID", Item.CheckGet("INCOME_PRODUCTION_TASK_ID"));
                    p.CheckAdd("INCOME_NUMBER", Item.CheckGet("INCOME_NUMBER"));
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Stock");
                q.Request.SetParam("Object", "Product");
                q.Request.SetParam("Action", "UpdatePositionId");
                q.Request.SetParams(p);

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverName = "StockConditionProduct",
                        Action = "Refresh",
                        Message= $"{row.CheckGet("APPLICATION_POSITION_ID")}",
                    });
                    Close();
                }
                else
                {
                    q.ProcessError();
                }
            }
        }        
    }
}
