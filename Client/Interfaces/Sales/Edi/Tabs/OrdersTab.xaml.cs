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
    /// заявки 
    /// статусы процессов EDI по ним
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2024-09-19</released>
    /// <changed>2024-09-19</changed>
    public partial class OrdersTab : ControlBase
    {
        public OrdersTab()
        {
            InitializeComponent();

            ControlSection = "orders";
            RoleName = "[erp]edi";
            ControlTitle ="Заявки";
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
                OrderGridInit();
            };

            OnUnload=()=>
            {
                OrderGrid.Destruct();
            };

            OnFocusGot=()=>
            {
                OrderGrid.ItemsAutoUpdate=true;
                OrderGrid.Run();
            };

            OnFocusLost=()=>
            {
                OrderGrid.ItemsAutoUpdate=false;
            };

            OnNavigate = () =>
            {
                //var departmentId = Parameters.CheckGet("department_id");
                //if(!departmentId.IsNullOrEmpty())
                //{
                //    OrderGridSearch.Text = departmentId;
                //    OrderGrid.UpdateItems();
                //}
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

                Commander.SetCurrentGridName("OrderGrid");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "order_refresh",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить данные",
                        ButtonUse = true,
                        ButtonName = "OrderGridRefreshButton",
                        MenuUse = true,
                        Action = () =>
                        {
                            OrderGrid.LoadItems();
                        },
                    });

                    Commander.SetCurrentGroup("item");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "order_edit",
                            Title = "Открыть",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "OrderGridEditButton",
                            HotKey = "Return|DoubleCLick",
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var k = OrderGrid.GetPrimaryKey();
                                var row = OrderGrid.SelectedItem;
                                var id = row.CheckGet(k).ToInt();
                                if(id != 0)
                                {
                                    var h = new OrderForm();
                                    h.FormInit();
                                    h.Edit(id);
                                }
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = OrderGrid.GetPrimaryKey();
                                var row = OrderGrid.SelectedItem;
                                if(row.CheckGet(k).ToInt() != 0)
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });
                    }
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
                        Path = "INVOICE_DATE_FROM",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = DateFrom,
                        ControlType = "TextBox",
                        Default=DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy"),
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "INVOICE_DATE_TO",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = DateTo,
                        ControlType = "TextBox",
                        Default=DateTime.Now.ToString("dd.MM.yyyy"),
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                };

                Form.SetFields(fields);
            }
        }

        public void SetDefaults()
        {
            Form.SetDefaults();
        }

        public void OrderGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Ид",
                    Path="NSTHET",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Дата создания",
                    Path="DATA",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Продавец",
                    Path="SELLER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="Покупатель",
                    Path="BUYER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="Сумма",
                    Path="SUM_PRICE",
                    ColumnType=ColumnTypeRef.Double,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Позиции",
                    Path="COUNT_CONSUMPTION",
                    Doc="Количество позиций по артикулу и цене продажи",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Дата отгрузки",
                    Path="SHIPMENT_DATA",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Грузополучатель",
                    Path="CONSIGNEE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="Покупатель по договору",
                    Path="CONTRACT_BUYER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="Договор",
                    Path="CONTRACT_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание",
                    Path="COMMENTS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="Ид покупателя",
                    Path="BUYER_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Ид продавца",
                    Path="SELLER_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Ид договора",
                    Path="CONTRACT_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
            };
            OrderGrid.SetColumns(columns);
            OrderGrid.SetPrimaryKey("NSTHET");
            OrderGrid.SetSorting("NSTHET", ListSortDirection.Ascending);
            OrderGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            OrderGrid.SearchText = OrderGridSearch;
            OrderGrid.Toolbar = OrderGridToolbar;
            OrderGrid.QueryLoadItems = new RequestData()
            {
                Module = "Sales",
                Object = "Sale",
                Action = "List",
                AnswerSectionKey = "ITEMS",
                BeforeRequest = (RequestData rd) =>
                {
                    var v = Form.GetValues();
                    {
                        rd.Params = new Dictionary<string, string>()
                        {
                            { "INVOICE_DATE_FROM", v.CheckGet("INVOICE_DATE_FROM").ToString() },
                            { "INVOICE_DATE_TO", v.CheckGet("INVOICE_DATE_TO").ToString()},
                        };
                    }
                },
            };
            OrderGrid.Commands = Commander;
            OrderGrid.Init();            
        }
    }
}
