using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using DevExpress.Utils.About;
using DevExpress.Utils.Filtering.Internal;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.MoldedContainer
{
    /// <summary>
    /// Переоприходование макулатуры литой тары
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class RearrivalScrapPaper : ControlBase
    {
        public RearrivalScrapPaper()
        {
            ControlTitle = "Переоприходование макулатуры";
            DocumentationUrl = "/doc/l-pack-erp/";
            RoleName = "[erp]rearrival_scrap_paper";
            InitializeComponent();

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnKeyPressed = (KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };

            //конструктор, будет вызван, когда объект создается
            //здесь создаются все внутренние структуры
            //впервые этот коллбэк будет вызван, когда данный таб станет активным
            //впервые (до этих пор, никакая работа внутри не происходит, что экономит ресурсы)
            OnLoad = () =>
            {
                SetDefaults();
                InvoiceGridInit();
                PositionGridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                InvoiceGrid.Destruct();
                PositionGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                InvoiceGrid.ItemsAutoUpdate = true;
                InvoiceGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                InvoiceGrid.ItemsAutoUpdate = false;
            };

            {
                Commander.Add(new CommandItem()
                {
                    Name = "refresh",
                    Group = "main",
                    Enabled = true,
                    Title = "Обновить",
                    Description = "Обновить данные",
                    ButtonUse = true,
                    ButtonControl = RefreshButton,
                    ButtonName = "RefreshButton",
                    Action = () =>
                    {
                        Refresh();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "help",
                    Group = "main",
                    Enabled = true,
                    Title = "Справка",
                    Description = "Показать справочную информацию",
                    ButtonUse = true,
                    ButtonControl = HelpButton,
                    ButtonName = "HelpButton",
                    HotKey = "F1",
                    Action = () =>
                    {
                        Central.ShowHelp(DocumentationUrl);
                    },
                });
            }

            Commander.SetCurrentGridName("InvoiceGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "rearrival",
                    Title = "Переоприходовать",
                    Description = "Переоприходовать всю макулатуру по накладной в другом цвете",
                    Group = "invoice_grid_default",
                    MenuUse = true,
                    Enabled = false,
                    ButtonUse = true,
                    ButtonControl = RearrivalButton,
                    ButtonName = "RearrivalButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        Rearrival();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (InvoiceGrid != null && InvoiceGrid.Items != null && InvoiceGrid.Items.Count > 0)
                        {
                            if (InvoiceGrid.SelectedItem != null && InvoiceGrid.SelectedItem.Count > 0
                                && !string.IsNullOrEmpty(InvoiceGrid.SelectedItem.CheckGet("INVOICE_ID")))
                            {
                                if (PositionGrid != null && PositionGrid.Items != null && PositionGrid.Items.Count > 0)
                                {
                                    result = true;
                                }
                            }
                        }

                        return result;
                    },
                });
            }

            Commander.Init(this);
        }

        private ListDataSet InvoiceDataSet { get; set; }

        private ListDataSet PositionDataSet { get; set; }

        public static Dictionary<int, int> ProductAlternativeDictionary = new Dictionary<int, int>() 
        {
            {35007, 555871},
            {555871, 35007},
            {335156, 555872},
            {555872, 335156},
        };

        public static Dictionary<int, string> ProductIdNameDictionary = new Dictionary<int, string>()
        {
            {35007, "Макулатура МС-11В"},
            {555871, "Макулатура МС-11В Белая"},
            {335156, "Макулатура МС-6Б1"},
            {555872, "Макулатура МС-6Б1 Белая"},
        };

        public void SetDefaults()
        {
            InvoiceDataSet = new ListDataSet();
            PositionDataSet = new ListDataSet();
        }

        public void Refresh()
        {
            InvoiceGrid.LoadItems();
        }

        public void InvoiceGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Description = "Идентификатор накладной прихода",
                        Path="INVOICE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата",
                        Description = "Дата накладной прихода",
                        Path="INVOICE_DATE",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy",
                        Width2 = 9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер с/ф",
                        Description = "Номер счет - фактуры с внешнего документа",
                        Path="NAMESF",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 11,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер накладной",
                        Description = "Внешний номер накладной (с внешнего документа)",
                        Path="NAME_NAKL",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Поставщик",
                        Description = "Имя поставщика",
                        Path="SELLER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 24,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Description = "Номенклатурное наименование",
                        Path="INVENTORY_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 47,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вес, кг.",
                        Description = "Суммарный вес продукции на остатке",
                        Path="WEIGHT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество, шт.",
                        Description = "Суммарное количество продукции на остатке",
                        Path="QUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 10,
                    },
                };
                InvoiceGrid.SetColumns(columns);
                InvoiceGrid.SetPrimaryKey("INVOICE_ID");
                InvoiceGrid.SearchText = InvoiceSearchBox;
                //данные грида
                InvoiceGrid.OnLoadItems = InvoiceGridLoadItems;
                InvoiceGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                InvoiceGrid.AutoUpdateInterval = 60 * 5;
                InvoiceGrid.Toolbar = InvoiceGridToolbar;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                InvoiceGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem != null && selectedItem.Count > 0)
                    {
                        if (InvoiceGrid != null && InvoiceGrid.Items != null && InvoiceGrid.Items.Count > 0)
                        {
                            if (InvoiceGrid.Items.FirstOrDefault(x => x.CheckGet("INVOICE_ID").ToInt() == selectedItem.CheckGet("INVOICE_ID").ToInt()) == null)
                            {
                                InvoiceGrid.SelectRowFirst();
                            }
                        }

                        PositionGridLoadItems();
                    }
                };

                InvoiceGrid.OnFilterItems = InvoiceGridFilterItems;

                InvoiceGrid.Commands = Commander;

                InvoiceGrid.Init();
            }
        }

        public async void InvoiceGridLoadItems()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "RearrivalScrapPaper");
            q.Request.SetParam("Action", "ListInvoice");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            InvoiceDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    InvoiceDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            InvoiceGrid.UpdateItems(InvoiceDataSet);
        }

        public void InvoiceGridFilterItems()
        {
            PositionGrid.ClearItems();

            if (InvoiceGrid != null && InvoiceGrid.SelectedItem != null && InvoiceGrid.SelectedItem.Count > 0)
            {
                InvoiceGrid.Commands.Message = new ItemMessage() { Action = "refresh", Message = $"{InvoiceGrid.SelectedItem.CheckGet("INVOICE_ID")}" };
            }
        }

        public void PositionGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="ITEM_ID",
                        Description="Идентификатор складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="ITEM_NAME",
                        Description="Наименование складской единицы",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 20,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество, шт.",
                        Path="INCOMING_QUANTITY",
                        Description="Количество в складской единице",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2 = 10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вес, кг.",
                        Path="INCOMING_WEIGHT",
                        Description="Вес складской единицы",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2 = 10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ячейка",
                        Path="STORAGE_NUM",
                        Description="Место хранения складской единицы",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Внешний ИД",
                        Path="OUTER_ID",
                        Description="Внешний идентификатор складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД накладной",
                        Path="INVOICE_ID",
                        Description="Идентификатор накладной прихода",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номенклатура",
                        Path="INVENTORY_NAME",
                        Description="Номенклатурное наименование",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД продукции",
                        Path="PRODUCT_ID",
                        Description="Идентификатор продукции",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },
                };
                PositionGrid.SetColumns(columns);
                PositionGrid.SetPrimaryKey("ITEM_ID");
                PositionGrid.SearchText = PositionSearchBox;
                //данные грида
                PositionGrid.OnLoadItems = PositionGridLoadItems;
                PositionGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                PositionGrid.AutoUpdateInterval = 0;
                PositionGrid.ItemsAutoUpdate = false;
                PositionGrid.Toolbar = PositionGridToolbar;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                PositionGrid.OnSelectItem = selectedItem =>
                {
                    if (PositionGrid != null && PositionGrid.Items != null && PositionGrid.Items.Count > 0)
                    {
                        if (PositionGrid.Items.FirstOrDefault(x => x.CheckGet("ITEM_ID").ToInt() == selectedItem.CheckGet("ITEM_ID").ToInt()) == null)
                        {
                            PositionGrid.SelectRowFirst();
                        }
                    }
                };

                PositionGrid.Commands = Commander;

                PositionGrid.Init();
            }
        }

        public async void PositionGridLoadItems()
        {
            if (InvoiceGrid != null && InvoiceGrid.SelectedItem != null && InvoiceGrid.SelectedItem.Count > 0)
            {
                var p = new Dictionary<string, string>();
                p.Add("INVOICE_ID", InvoiceGrid.SelectedItem["INVOICE_ID"]);

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "MoldedContainer");
                q.Request.SetParam("Object", "RearrivalScrapPaper");
                q.Request.SetParam("Action", "ListPosition");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                PositionDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        PositionDataSet = ListDataSet.Create(result, "ITEMS");
                    }
                }
                PositionGrid.UpdateItems(PositionDataSet);
            }
        }

        private void Rearrival()
        {
            int currentId2 = PositionGrid.Items[0].CheckGet("PRODUCT_ID").ToInt();
            int alternativeId2 = GetAlternativeProductId(currentId2);

            if (alternativeId2 > 0)
            {
                var d = new DialogWindow($"" +
                    $"Переоприходовать {InvoiceGrid.SelectedItem["QUANTITY"].ToInt()} штук {InvoiceGrid.SelectedItem["INVENTORY_NAME"]} в {ProductIdNameDictionary[alternativeId2]}?",
                    this.ControlTitle, "", DialogWindowButtons.YesNo);
                if (d.ShowDialog() != true)
                {
                    return;
                }

                var p = new Dictionary<string, string>();
                p.Add("INVOICE_ID", InvoiceGrid.SelectedItem["INVOICE_ID"]);
                p.Add("PRODUCT_ID", $"{alternativeId2}");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "MoldedContainer");
                q.Request.SetParam("Object", "RearrivalScrapPaper");
                q.Request.SetParam("Action", "Rearrival");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    bool succesfullFlag = false;

                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                        {
                            if (ds.Items[0].CheckGet("INVOICE_ID").ToInt() > 0)
                            {
                                succesfullFlag = true;
                            }
                        }
                    }

                    if (succesfullFlag)
                    {
                        Refresh();
                        DialogWindow.ShowDialog($"Успешное выполнение переоприходования макулатуры литой тары.");
                    }
                    else
                    {
                        DialogWindow.ShowDialog($"При выполнении переоприходования макулатуры литой тары произошла ошибка. Пожалуйста, сообщите о проблеме.");
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
            else
            {
                DialogWindow.ShowDialog($"Не найдена макулатура, в которую можно переоприходовать эту макулатуру.");
            }
        }

        public int GetAlternativeProductId(int curentId)
        {
            int alternativeId = 0;

            try
            {
                alternativeId = ProductAlternativeDictionary[curentId];
            }
            catch (Exception ex)
            {

            }

            return alternativeId;
        }
    }
}
