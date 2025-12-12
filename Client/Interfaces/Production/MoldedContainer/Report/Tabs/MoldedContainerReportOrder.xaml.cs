using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.MoldedContainer.Report.Tabs
{
    /// <summary>
    /// Интерфейс отчета по заказам
    /// </summary>
    /// <author>volkov_as</author>
    public partial class MoldedContainerReportOrder : ControlBase
    {
        public MoldedContainerReportOrder()
        {
            InitializeComponent();

            ControlSection = "report_order";
            ControlTitle = "Отчет по заявкам";
            DocumentationUrl = "";

            OnLoad = () =>
            {
                FormInit();
                GridInit();
            };

            OnUnload = () =>
            {
                Grid.Destruct();
            };

            OnFocusGot = () =>
            {
                Grid.ItemsAutoUpdate = true;
            };

            OnFocusLost = () =>
            {
                Grid.ItemsAutoUpdate = false;
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
                        Action = () => { Central.ShowHelp(DocumentationUrl); },
                    });

                    Commander.Add(new CommandItem()
                    {
                        Name = "show",
                        Enabled = true,
                        Title = "Показать",
                        Description = "Показать отчет",
                        ButtonUse = true,
                        MenuEnabled = true,
                        ButtonName = "ShowButton",
                        Action = () => Grid.LoadItems()
                    });

                    Commander.Add(new CommandItem()
                    {
                        Name = "export_to_excel",
                        Enabled = true,
                        Title = "Экспорт в Excel",
                        Description = "Экспортировать отчет в Excel",
                        ButtonUse = true,
                        MenuEnabled = true,
                        ButtonName = "ExcelButton",
                        Action = () =>
                        {
                            Grid.ItemsExportExcel();
                        }
                    });
                }
            }

            Commander.Init(this);
        }

        private FormHelper Form { get; set; }
        private ListDataSet GridItems { get; set; }

        public void FormInit()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="FROM_DT",
                    FieldType=FormHelperField.FieldTypeRef.DateTime,
                    Default = DateTime.Now.AddMonths(-1).ToString("dd.MM.yyyy"),
                    Control= FromDate,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {
                        ShowButton.Style = (System.Windows.Style)ShowButton.TryFindResource("FButtonPrimary");
                    }
                },
                new FormHelperField()
                {
                    Path="TO_DT",
                    FieldType=FormHelperField.FieldTypeRef.DateTime,
                    Default = DateTime.Now.ToString("dd.MM.yyyy"),
                    Control= ToDate,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {
                        ShowButton.Style = (System.Windows.Style)ShowButton.TryFindResource("FButtonPrimary");
                    }
                },
                new FormHelperField()
                {
                    Path = "BUYER_SELECT_LIST",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = BuyerSelectList,
                    ControlType = "SelectBox",
                    OnChange = (f, v) =>
                    {
                        Grid.LoadItems();
                    },
                    QueryLoadItems = new RequestData()
                    {
                        Module = "Orders",
                        Object = "MoldedContainer",
                        Action = "ListBuyer",
                        AnswerSectionKey = "BUYERS",
                        OnComplete = (f, v) =>
                        {
                            var row = new Dictionary<string, string>
                            {
                                { "ID", "0" },
                                { "NAME", "Все"}
                            };
                            v.ItemsPrepend(row);
                            var list = v.GetItemsList("ID", "NAME");
                            var c = (SelectBox)f.Control;

                            if (c != null)
                            {
                                c.Items = list;
                            }

                            BuyerSelectList.SetSelectedItemByKey("0");
                        }
                    }
                },
                new FormHelperField()
                {
                    Path = "TYPE_PRODUCTION",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = TypeProductionSelectList,
                    ControlType = "SelectBox",
                    OnChange = (f, v) =>
                    {
                        Grid.LoadItems();
                    },
                    QueryLoadItems = new RequestData()
                    {
                        Module = "MoldedContainer",
                        Object = "Report",
                        Action = "ListTechcard",
                        AnswerSectionKey = "ITEMS",
                        OnComplete = (f, v) =>
                        {
                            var row = new Dictionary<string, string>
                            {
                                { "ID", "0" },
                                { "FULL_TYPE_NAME", "Все" }
                            };
                            v.ItemsPrepend(row);
                            var list = v.GetItemsList("ID", "FULL_TYPE_NAME");
                            var c = (SelectBox)f.Control;

                            if (c != null)
                            {
                                c.Items = list;
                            }

                            TypeProductionSelectList.SetSelectedItemByKey("0");
                        }
                    }
                }
            };
            Form.SetFields(fields);
            Form.SetDefaults();
        }

        private void GridInit()
        {
            var column = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "Дата отгрузки",
                    Path = "DTTM",
                    ColumnType = ColumnTypeRef.DateTime,
                    Width2 = 15,
                },
                new DataGridHelperColumn
                {
                    Header = "Дата доставки",
                    Path = "DT_SUPPLY",
                    ColumnType = ColumnTypeRef.DateTime,
                    Width2 = 15,
                },
                new DataGridHelperColumn
                {
                    Header = "Дата принятия",
                    Path = "ACCEPTED_DTTM",
                    ColumnType = ColumnTypeRef.DateTime,
                    Width2 = 15,
                },
                new DataGridHelperColumn
                {
                    Header = "Покупатель",
                    Path = "NAME_POK",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 25,
                },
                new DataGridHelperColumn
                {
                    Header = "Артикул",
                    Path = "ARTIKUL",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 10,
                },
                new DataGridHelperColumn
                {
                    Header = "Наименование",
                    Path = "NAME",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 45,
                },
                new DataGridHelperColumn
                {
                  Header = "Схема производства",
                  Path = "NAME_SCHEMA",
                  ColumnType = ColumnTypeRef.String,
                  Width2 = 16,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество",
                    Description = "Количество продукции в заявке",
                    Path = "QTY",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 10,
                    Totals =(List<Dictionary<string,string>> rows) =>
                    {
                            int result=0;
                            if(rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    result += row.CheckGet("QTY").ToInt();
                                }
                            }
                            return string.Format("{0:### ### ###}", result);
                    },
                },
                new DataGridHelperColumn
                {
                    Header = "По заявке на складе",
                    Path = "QTY_STOCK",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 18,
                    Totals =(List<Dictionary<string,string>> rows) =>
                    {
                            int result=0;
                            if(rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    result += row.CheckGet("QTY_STOCK").ToInt();
                                }
                            }
                            return string.Format("{0:### ### ###}", result);
                    },
                },
                new DataGridHelperColumn
                {
                    Header = "Всего на складе",
                    Path = "QTY_STOCK_ALL",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 13,
                    Totals =(List<Dictionary<string,string>> rows) =>
                    {
                            int result=0;
                            if(rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    result += row.CheckGet("QTY_STOCK_ALL").ToInt();
                                }
                            }
                            return string.Format("{0:### ### ###}", result);
                    },

                },
                new DataGridHelperColumn
                {
                    Header = "В ПЗ",
                    Path = "QTY_TASK",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Totals =(List<Dictionary<string,string>> rows) =>
                    {
                            int result=0;
                            if(rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    result += row.CheckGet("QTY_TASK").ToInt();
                                }
                            }
                            return string.Format("{0:### ### ###}", result);
                    },
                },
                new DataGridHelperColumn
                {
                    Header = "Отгружено",
                    Path = "QTY_R",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 10,
                    Totals =(List<Dictionary<string,string>> rows) =>
                    {
                            int result=0;
                            if(rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    result += row.CheckGet("QTY_R").ToInt();
                                }
                            }
                            return string.Format("{0:### ### ###}", result);
                    },
                },
                new DataGridHelperColumn
                {
                    Header = "ID_POK",
                    Path = "ID_POK",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 10,
                    Exportable = false,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "TECT_ID",
                    Path = "TECT_ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 10,
                    Exportable = false,
                    Visible = false
                }
            };
            Grid.SetColumns(column);
            Grid.SetPrimaryKey("ID");
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.SearchText = Search;
            Grid.OnLoadItems = LoadItems;

            Grid.OnFilterItems = () =>
            {
                if (Grid.Items.Count > 0)
                {
                    bool showAll = false;
                    var v = Form.GetValues();

                    if (v.CheckGet("BUYER_SELECT_LIST").ToInt() == 0 &&
                            v.CheckGet("TYPE_PRODUCTION").ToInt() == 0)
                    {
                        showAll = true;
                    }

                    var items = new List<Dictionary<string, string>>();

                    foreach (var row in Grid.Items)
                    {
                        if (showAll)
                        {
                            items.Add(row);
                        } 
                        else
                        {
                            var resume = true;

                            if (resume)
                            {
                                if (v.CheckGet("BUYER_SELECT_LIST").ToInt() == 0)
                                {
                                    resume = true;
                                }
                                else if (row.CheckGet("ID_POK").ToInt() == v.CheckGet("BUYER_SELECT_LIST").ToInt())
                                {
                                    resume = true;
                                } 
                                else
                                {
                                    resume = false;
                                }
                            }

                            if (resume)
                            {
                                if (v.CheckGet("TYPE_PRODUCTION").ToInt() == 0)
                                {
                                    resume = true;
                                }
                                else if (row.CheckGet("TECT_ID").ToInt() == v.CheckGet("TYPE_PRODUCTION").ToInt())
                                {
                                    resume = true;
                                }
                                else
                                {
                                    resume = false;
                                }
                            }

                            if (resume)
                            {
                                items.Add(row);
                            }
                        }
                    }

                    Grid.Items = items;
                }
            };


            Grid.Commands = Commander;
            Grid.Init();
        }


        private async void LoadItems()
        {
            var v = Form.GetValues();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Report");
            q.Request.SetParam("Action", "OrderList");
            q.Request.SetParam("FROM_DT", v.CheckGet("FROM_DT"));
            q.Request.SetParam("TO_DT", v.CheckGet("TO_DT"));

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
                    GridItems = ListDataSet.Create(result, "ITEMS");
                }
            }

            Grid.UpdateItems(GridItems);
        }
    }
}