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
    /// изделия на складе
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2025-09-03</released>
    /// <changed>2025-09-03</changed>
    public partial class StockConditionProduct:ControlBase
    {
        public StockConditionProduct()
        {
            InitializeComponent();

            ControlSection = "stock_condition";
            RoleName = "[erp]warehouse_condition";
            ControlTitle ="Изделия на складе";
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
                                if(
                                    (
                                        row.CheckGet("POSITION_BIND_QUANTITY").ToInt() > 0
                                        && (
                                            row.CheckGet("POSITION_DATE").IsNullOrEmpty()
                                            || row.CheckGet("POSITION_SHIPPED").ToInt() == 1
                                        )
                                    )
                                    || row.CheckGet("POSITION_BIND_QUANTITY").ToInt() > 1
                                )
                                {
                                    result=true;
                                }
                                return result;
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "unbind",
                            Title = "Отвязать",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "UnbindButton",
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var row=Grid.SelectedItem;
                                Unbind(row);
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var row=Grid.SelectedItem;
                                if(!row.CheckGet("APPLICATION_SHIPMENT_TITLE").IsNullOrEmpty())
                                {
                                    result=true;
                                }
                                return result;
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "export_excel",
                            Title = "В Excel",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "ExportExcelButton",
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                Grid.ItemsExportExcel();
                            },                           
                        });
                        
                    }
                }

                Commander.Init(this);
            }
        }

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
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="ИД поддона",
                    Path="PALLET_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="PRODUCT_CODE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="PRODUCT_TITLE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=32,
                },
                new DataGridHelperColumn
                {
                    Header="Количество",
                    Doc="Количество, шт",
                    Path="QUANTITY",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Поддон",
                    Doc="Номер поддона",
                    Path="PALLET_NUMBER",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Место",
                    Doc="Место размещения поддона",
                    Path="PLACE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Group="Заявка",
                    Header="Отгрузка",
                    Path="APPLICATION_SHIPMENT_TITLE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=24,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if(
                                    row.CheckGet("PRODUCTION_TASK_APPLICATION_ID").ToInt() != 0
                                    && row.CheckGet("APPLICATION_POSITION_ID").ToInt() != 0
                                    && row.CheckGet("PRODUCTION_TASK_APPLICATION_ID").ToInt() !=
                                        row.CheckGet("APPLICATION_POSITION_ID").ToInt()
                                )
                                {
                                    color = HColor.Yellow;
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        },
                    },
                },
                new DataGridHelperColumn
                {
                    Group="Заявка",
                    Header="ИД позиции заявки",
                    Path="APPLICATION_POSITION_ID",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                    
                },
                new DataGridHelperColumn
                {
                    Group="Заявка",
                    Header="ИД отгрузки",
                    Path="APPLICATION_SHIPMENT_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                    Options="zeroempty",
                },
                new DataGridHelperColumn
                {
                    Group="ПЗ",
                    Header="Отгрузка",
                    Path="PRODUCTION_TASK_SHIPMENT_TITLE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=24,
                },
                new DataGridHelperColumn
                {
                    Group="ПЗ",
                    Header="ИД позиции заявки",
                    Path="PRODUCTION_TASK_APPLICATION_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                    Options="zeroempty",
                },
                new DataGridHelperColumn
                {
                    Header="ИД товара",
                    Path="PRODUCT_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
            };
            Grid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        if(
                            (
                                !row.CheckGet("POSITION_DATE").IsNullOrEmpty()
                                && (
                                    row.CheckGet("POSITION_SHIPPED").ToInt() == 0
                                    || row.CheckGet("POSITION_BIND_QUANTITY").ToInt() == 0
                                )
                                && row.CheckGet("POSITION_DATE").ToDateTime().ConvertToUnixTimestamp() < DateStart.ToDateTime().ConvertToUnixTimestamp()
                            )
                        )
                        {
                            color = HColor.Yellow;
                        }
                        
                        else if(
                            !row.CheckGet("POSITION_DATE").IsNullOrEmpty()
                            && row.CheckGet("POSITION_SHIPPED").ToInt() == 0
                            && row.CheckGet("POSITION_DATE").ToDateTime().ConvertToUnixTimestamp() >= DateStart.ToDateTime().ConvertToUnixTimestamp()
                        )
                        {
                             color = HColor.Green;
                        }
                        
                        else if(
                            (
                                row.CheckGet("POSITION_DATE").IsNullOrEmpty()
                                || row.CheckGet("POSITION_SHIPPED").ToInt() == 1
                            )
                            && row.CheckGet("POSITION_BIND_QUANTITY").ToInt() == 0
                        )
                        {
                            color = HColor.Blue;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
            };
            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("_ROWNUMBER");
            Grid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            Grid.SearchText = GridSearch;
            Grid.Toolbar = GridToolbar; 
            Grid.UseProgressBar=true;                
            Grid.QueryLoadItems = new RequestData()
            {
                Module = "Stock",
                Object = "Product",
                Action = "ListCondition",
                AnswerSectionKey = "ITEMS",     
                BeforeRequest = (RequestData rd) =>
                {
                    var p=Form.GetValues();
                    rd.Params = new Dictionary<string, string>()
                    {
                        {"FACTORY_ID", p.CheckGet("FACTORY_ID")},
                    };
                },                
            };        
            Grid.OnFilterItems = () =>
            {
                var v=Form.GetValues();
                var items = new List<Dictionary<string, string>>();
                foreach(var row in Grid.Items)
                {
                    var include=false;
                    switch(v.CheckGet("PALLET_TYPE"))
                    {
                        case "ALL":
                            {
                                include=true;
                            }
                            break;

                        case "BINDED":
                            {
                                if(
                                    !row.CheckGet("POSITION_DATE").IsNullOrEmpty()
                                    && row.CheckGet("POSITION_SHIPPED").ToInt() == 0
                                    && row.CheckGet("POSITION_DATE").ToDateTime().ConvertToUnixTimestamp() >= DateStart.ToDateTime().ConvertToUnixTimestamp()
                                )
                                {
                                    include=true;
                                }
                            }
                            break;

                        case "UNBINDED":
                            {
                                if(
                                    (   
                                        row.CheckGet("POSITION_DATE").IsNullOrEmpty()
                                        || row.CheckGet("POSITION_SHIPPED").ToInt() == 1
                                    )
                                    && row.CheckGet("POSITION_BIND_QUANTITY").ToInt() == 0                                    
                                )
                                {
                                    include=true;
                                }
                            }
                            break;

                        case "FOR_BINDING":
                            {
                                if(
                                    (   
                                        row.CheckGet("POSITION_DATE").IsNullOrEmpty()
                                        || row.CheckGet("POSITION_SHIPPED").ToInt() == 1
                                    )
                                    && row.CheckGet("POSITION_BIND_QUANTITY").ToInt() > 0                                    
                                )
                                {
                                    include=true;
                                }
                            }
                            break;

                        case "FOR_UNBINDING":
                            {
                                if(
                                    !row.CheckGet("POSITION_DATE").IsNullOrEmpty()
                                    && (   
                                        row.CheckGet("POSITION_BIND_QUANTITY").ToInt() == 0 
                                        || row.CheckGet("POSITION_SHIPPED").ToInt() == 0
                                    )
                                    && row.CheckGet("POSITION_DATE").ToDateTime().ConvertToUnixTimestamp() < DateStart.ToDateTime().ConvertToUnixTimestamp()                                
                                )
                                {
                                    include=true;
                                }
                            }
                            break;
                    }

                    if(include)
                    {
                        items.Add(row);
                    }
                }
                Grid.Items=items;
            };
            Grid.Commands = Commander;
            Grid.DebugName="product";
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
                new FormHelperField()
                {
                    Path="FACTORY_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Default="1",
                    Control=FactoryId,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange=(FormHelperField f, string v)=>
                    {
                        Grid.UpdateItems();
                    },
                    QueryLoadItems = new RequestData()
                    {
                        Module = "Service",
                        Object = "Factory",
                        Action = "List",
                        AnswerSectionKey="ITEMS",
                        OnComplete = (FormHelperField f,ListDataSet ds) =>
                        {
                            var list=ds.GetItemsList("ID","NAME");
                            var c=(SelectBox)f.Control;
                            if(c != null)
                            {
                                c.Items=list;
                            }
                        },
                    },
                },
                new FormHelperField()
                {
                    Path="PALLET_TYPE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Default="ALL",
                    Control=PalletType,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange=(FormHelperField f, string v)=>
                    {
                        Grid.UpdateItems();
                    },
                    OnUpdateItems=(FormHelperField f)=>
                    { 
                        var list=new Dictionary<string,string>()
                        {
                            {"ALL", "Все"},
                            {"BINDED", "Привязанные"},
                            {"UNBINDED", "Непривязанные"},
                            {"FOR_BINDING", "Для привязки"},
                            {"FOR_UNBINDING", "Для отвязки"},
                        };
                        return list;
                    },                   
                },
            };
            Form.SetFields(fields);
        }
    
        /// <summary>
        /// привязать отгрузку к поддону
        /// </summary>
        private void Bind(Dictionary<string,string> row)
        {
            var v=Form.GetValues();
            var h=new ShipmentBindPallet();
            h.Item=row;
            h.Item.CheckAdd("FACTORY_ID",v.CheckGet("FACTORY_ID"));
            h.Show();
        }

        private void Unbind(Dictionary<string,string> row)
        {
            var resume=true;

            if(resume)
            {
                var s="";
                s=s.Append($"Отвязать отгрузку от поддона?",true);
                s=s.Append($"Отгрузка: {row.CheckGet("APPLICATION_SHIPMENT_TITLE")}",true);
                s=s.Append($"Поддон: {row.CheckGet("PALLET_NUMBER")}",true);

                var r=DialogWindow.ShowDialog(
                    s, 
                    $"Отвязка отгрузки", 
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
                    p.CheckAdd("APPLICATION_POSITION_ID", 0.ToString());
                    p.CheckAdd("PRODUCT_ID", row.CheckGet("PRODUCT_ID"));
                    p.CheckAdd("INCOME_PRODUCTION_TASK_ID", row.CheckGet("INCOME_PRODUCTION_TASK_ID"));
                    p.CheckAdd("INCOME_NUMBER", row.CheckGet("INCOME_NUMBER"));
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Stock");
                q.Request.SetParam("Object", "Product");
                q.Request.SetParam("Action", "UpdatePositionId");
                q.Request.SetParams(p);

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    Grid.LoadItems();
                }
                else
                {
                    q.ProcessError();
                }
            }
        }
    }
}
