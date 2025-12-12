using Client.Common;
using Client.Interfaces.Main;
using DevExpress.Xpf.Editors.Internal;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Sales.Edi
{
    /// <summary>
    /// карточка заявки
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2024-09-19</released>
    /// <changed>2024-09-19</changed>
    public partial class OrderForm : ControlBase
    {
        public OrderForm()
        {
            Id = 0;

            InitializeComponent();

            RoleName = "[erp]edi";
            FrameMode = 0;
            OnGetFrameTitle = () =>
            {
                var result = "";

                var id = Id.ToInt();
                if(id == 0)
                {
                    result = $"Новая заявка";
                }
                else
                {
                    result = $"Заявка #{id}";
                }

                return result;
            };

            OnMessage = (ItemMessage m) =>
            {
                if(m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            {
                Commander.Add(new CommandItem()
                {
                    Name = "cancel",
                    Enabled = true,
                    Title = "Закрыть",
                    Description = "",
                    ButtonUse = true,
                    ButtonName = "CancelButton",
                    HotKey = "Escape",
                    Action = () =>
                    {
                        Hide();
                    },
                });

                Commander.SetCurrentGridName("OrderDocumentGrid");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "orderdocument_refresh",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить данные",
                        ButtonUse = true,
                        ButtonName = "OrderDocumentGridRefreshButton",
                        MenuUse = true,
                        AccessLevel = Common.Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            OrderDocumentGrid.LoadItems();
                        },
                    });

                    Commander.SetCurrentGroup("test");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "orderdocumentgrid_create2",
                            Title = "+2 Подтверждение заказа (ORDRSP)",
                            MenuUse = true,
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                TestDocumentCreate(2);
                            },
                            CheckEnabled = () =>
                            {
                                var result = true;
                                return result;
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "orderdocumentgrid_create3",
                            Title = "+3 Уведомление об отгрузке (DESADV)",
                            MenuUse = true,
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                TestDocumentCreate(3);
                            },
                            CheckEnabled = () =>
                            {
                                var result = true;
                                return result;
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "orderdocumentgrid_create5",
                            Title = "+5 Счет-фактура (INVOIC)",
                            MenuUse = true,
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                TestDocumentCreate(5);
                            },
                            CheckEnabled = () =>
                            {
                                var result = true;
                                return result;
                            },
                        });
                    }

                    Commander.SetCurrentGroup("item");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "orderdocumentgrid_view",
                            Title = "Открыть",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "OrderDocumentGridViewButton",
                            HotKey= "Return|DoubleCLick",
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var k = OrderDocumentGrid.GetPrimaryKey();
                                var row = OrderDocumentGrid.SelectedItem;
                                var uid = row.CheckGet(k).ToString();                                
                                if(!uid.IsNullOrEmpty())
                                {
                                    DocumentView(Id,uid);
                                }
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = OrderDocumentGrid.GetPrimaryKey();
                                var row = OrderDocumentGrid.SelectedItem;
                                var uid = row.CheckGet(k).ToString();
                                if(!uid.IsNullOrEmpty())
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

            FormInit();
            SetDefaults();
            OrderPositionGridInit(); 
            OrderDocumentGridInit();
        }

        public FormHelper Form { get; set; }
        //id заказа (naklrashod.hsthet)
        public int Id { get; set; }

        public void OrderPositionGridInit()
        {
            {
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Path="CONSUMPTION_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул",
                        Path="PRODUCT_CODE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=16,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование продукции",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=30,
                    },                    
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Path="QUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Единица измерения",
                        Path="IZM_FIRST_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=4,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Цена",
                        Path="PRICE",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Сумма",
                        Path="SUMMARY_PRICE",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=8,
                        Format="N2",
                    },
                    new DataGridHelperColumn
                    {
                        Header="НДС",
                        Path="CUSTOMER_VAT",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=6,
                        Format="N2",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Площадь на поддоне",
                        Path="SQUARE",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Площадь изделия",
                        Path="PRODUCT_SQUARE",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Проведен",
                        Path="COMPLETED_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                    },
                };

                OrderPositionGrid.SetColumns(columns);
                OrderPositionGrid.SetPrimaryKey("CONSUMPTION_ID");
                OrderPositionGrid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
                OrderPositionGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                OrderPositionGrid.AutoUpdateInterval = 0;
                OrderPositionGrid.SearchText = OrderPositionGridSearch;
                OrderPositionGrid.Toolbar = OrderPositionGridToolbar;
                OrderPositionGrid.QueryLoadItems = new RequestData()
                {
                    Module = "Sales",
                    Object = "Sale",
                    Action = "ListConsumption",
                    AnswerSectionKey = "ITEMS",
                    BeforeRequest = (RequestData rd) =>
                    {
                        rd.Params = new Dictionary<string, string>()
                        {
                            { "INVOICE_ID", Id.ToString() },
                        };
                    },
                };
                OrderPositionGrid.Commands = Commander;
                OrderPositionGrid.Init();
            }
        }

        public void OrderDocumentGridInit()
        {
            {
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ID",
                        Path="ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="DOCUMENT_UID",
                        Path="DOCUMENT_UID",
                        ColumnType=ColumnTypeRef.String,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ON_DATE",
                        Path="ON_DATE",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2=16,
                    },
                    new DataGridHelperColumn
                    {
                        Header="BUYER_ID",
                        Path="BUYER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="SELLER_ID",
                        Path="SELLER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="DESCRIPTION",
                        Path="DESCRIPTION",
                        ColumnType=ColumnTypeRef.String,
                        Width2=20,
                    },
                    new DataGridHelperColumn
                    {
                        Header="DOCUMENT_TYPE",
                        Path="DOCUMENT_TYPE",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="FILE_NAME",
                        Path="FILE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=20,
                    },
                    new DataGridHelperColumn
                    {
                        Header="MODE",
                        Path="MODE",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                    },
                };

                OrderDocumentGrid.SetColumns(columns);
                OrderDocumentGrid.SetPrimaryKey("DOCUMENT_UID");
                OrderDocumentGrid.SetSorting("ON_DATE", ListSortDirection.Ascending);
                OrderDocumentGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                OrderDocumentGrid.AutoUpdateInterval = 0;
                OrderDocumentGrid.SearchText = OrderDocumentGridSearch;
                OrderDocumentGrid.Toolbar = OrderDocumentGridToolbar;
                OrderDocumentGrid.QueryLoadItems = new RequestData()
                {
                    Module = "Sales",
                    Object = "EdiDocument",
                    Action = "List",
                    AnswerSectionKey = "ITEMS",
                    BeforeRequest = (RequestData rd) =>
                    {
                        rd.Params = new Dictionary<string, string>()
                        {
                            { "ORDER_ID", Id.ToString() },
                        };
                    },
                };
                OrderDocumentGrid.Commands = Commander;
                OrderDocumentGrid.Init();
            }
        }

        public void FormInit()
        {
            Form = new FormHelper();

            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="INVOICE_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=OrderId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
            Form.StatusControl = StatusBar;
        }

        public void SetDefaults()
        {
            Form.SetDefaults();
        }

        public void Edit(int id)
        {
            Id = id;
            GetData();
        }

        public async void GetData()
        {
            Form.SetBusy(true);

            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("INVOICE_ID", Id.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "Sale");
                q.Request.SetParam("Action", "Get");
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
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            Form.SetValues(ds);
                        }

                        {
                            OrderPositionGrid.LoadItems();
                            OrderDocumentGrid.LoadItems();
                        }

                        Show();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            Form.SetBusy(false);
        }

        public void Save()
        {
            bool resume = true;
            string error = "";

            //стандартная валидация данных средствами формы
            if (resume)
            {
                var validationResult = Form.Validate();
                if (!validationResult)
                {
                    resume = false;
                }
            }

            var v = Form.GetValues();

            //отправка данных
            if (resume)
            {
                SaveData(v);
            }
            else
            {
                Form.SetStatus(error, 1);
            }
        }

        public async void SaveData(Dictionary<string, string> p)
        {
            Form.SetBusy(true);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Accounts");
            q.Request.SetParam("Object", "Role");
            q.Request.SetParam("Action", "Save");

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
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        var id = ds.GetFirstItemValueByKey("ID").ToInt();

                        if (id != 0)
                        {
                            Central.Msg.SendMessage(new ItemMessage()
                            {
                                ReceiverName = "RoleTab",
                                SenderName = "RoleView",
                                Action = "Refresh",
                                Message = $"{id}",
                            });

                            Central.Msg.SendMessage(new ItemMessage()
                            {
                                ReceiverName = "AccountTab",
                                SenderName = "RoleView",
                                Action = "Refresh",
                            });

                            Close();
                        }
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            Form.SetBusy(false);
        }


        private async void TestDocumentCreate(int documentType)
        {
            var resume = true;
            var p = new Dictionary<string, string>();

            /*
                    MNAME     TITLE                      EXCHANGE DIRECTION
                    ------    -----------------------    ------------------
                (1) ORDERS    Заказ                      IN
                (2) ORDRSP    Подтверждение заказа       OUT
                (3) DESADV    Уведомление об отгрузке    OUT
                (4) RECADV    Уведомление о приемке      IN
                (5) INVOIC    Счет-фактура               OUT
             */

            switch(documentType)
            {
                case 2:
                    break;

                case 3:
                    break;

                case 5:
                    break;
            }

            p.CheckAdd("DOCUMENT_TYPE",documentType.ToString());
            p.CheckAdd("ORDER_ID", Id.ToString());

            if(resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "EdiDocument");
                q.Request.SetParam("Action", "CreateTest");

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
                            var ds = ListDataSet.Create(result, "ITEMS");
                            var id = ds.GetFirstItemValueByKey("ID").ToInt();

                            if(id != 0)
                            {
                                Central.Msg.SendMessage(new ItemMessage()
                                {
                                    ReceiverName = ControlName,
                                    SenderName = ControlName,
                                    Action = "orderdocument_refresh",
                                    Message = $"{id}",
                                });
                            }
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        private async void DocumentView(int orderId, string uid)
        {
            var resume = true;
            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("DOCUMENT_UID", uid.ToString());
                p.CheckAdd("ORDER_ID", orderId.ToString());
            }

            if(resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "EdiDocument");
                q.Request.SetParam("Action", "Download");

                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if(q.Answer.Status == 0)
                {
                    if(q.Answer.Type == LPackClientAnswer.AnswerTypeRef.File)
                    {
                        Central.OpenFile(q.Answer.DownloadFilePath);
                    }                    
                }
                else
                {
                    q.ProcessError();
                }
            }

        }
    }
}
