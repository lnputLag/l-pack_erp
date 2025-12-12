using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Debug;
using Client.Interfaces.DeliveryAddresses;
using Client.Interfaces.Main;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using GalaSoft.MvvmLight.Messaging;
using iTextSharp.text.log;
using Newtonsoft.Json;
using Org.BouncyCastle.Ocsp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using Xceed.Wpf.Toolkit.Primitives;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static Client.Interfaces.Main.FormDialog;

namespace Client.Interfaces.Sources.ProductionScheme2
{
    /// <summary>
    /// Справочник схем
    /// </summary>
    /// <author>motenko_ek</author>
    public partial class ProductionScheme2Tab : ControlBase
    {
        public ProductionScheme2Tab()
        {
            InitializeComponent();

            GoodsGridSearch.TextChanged += GoodsGridSearchTextChanged;
            AllCheckBox.Click += AllCheckBox_Click;

            ControlSection = "production_scheme2";
            RoleName = "[erp]production_scheme";
            ControlTitle = "Схемы производства 2";
            DocumentationUrl = "/doc/l-pack-erp/production/sources/production_scheme";
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
            OnLoad = () =>
            {
                GoodsGridInit();
                ProductionSchemeGridInit();
                ProductionStageGridInit();
                ProductionStageMachineGridInit();
                ProductionStageInputGridInit();
                ProductionStageOutputGridInit();
            };
            OnUnload = () =>
            {
                GoodsGrid.Destruct();
                ProductionSchemeGrid.Destruct();
                ProductionStageGrid.Destruct();
                ProductionStageInputGrid.Destruct();
                ProductionStageOutputGrid.Destruct();
            };
            OnNavigate = () =>
            {
                var id = Parameters.CheckGet("PRSC_ID");
                if (!id.IsNullOrEmpty())
                {
                    ProductionSchemeSearchText.Text = id;
                    ProductionSchemeGrid.UpdateItems();
                }

                id = Parameters.CheckGet("PRST_ID");
                if (!id.IsNullOrEmpty())
                {
                    ProductionStageSearchText.Text = id;
                    ProductionStageGrid.UpdateItems();
                }
            };

            Commander.SetCurrentGroup("main");
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

            Commander.SetCurrentGridName("GoodsGrid");
            Commander.Add(new CommandItem()
            {
                Name = "goods_refresh",
                Group = "grid_base",
                Enabled = true,
                Title = "Показать",
                Description = "Загрузить данные",
                ButtonUse = true,
                ButtonName = "GoodsGridSearchButton",
                Action = () =>
                {
                    GoodsGrid.LoadItems();
                },
            });

            Commander.SetCurrentGridName("ProductionSchemeGrid");
            Commander.Add(new CommandItem()
            {
                Name = "production_scheme_refresh",
                Group = "grid_base",
                Enabled = true,
                Title = "Обновить",
                Description = "Обновить",
                ButtonUse = false,
                MenuUse = true,
                ActionMessage = (ItemMessage message) =>
                {
                    ProductionSchemeGrid.LoadItems();
                    ProductionSchemeGrid.SelectRowByKey(message.Message);
                },
            });
            Commander.Add(new CommandItem()
            {
                Name = "production_scheme_attach",
                Group = "grid_base",
                Enabled = true,
                Title = "Добавить",
                MenuUse = true,
                HotKey = "Insert",
                ButtonUse = true,
                ButtonName = "ProductionSchemeAttachButton",
                AccessLevel = Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    new ProductionSchemeProductForm(new ItemMessage()
                    {
                        ReceiverName = ControlName,
                        Action = "production_scheme_refresh",
                    }, GoodsGrid.SelectedItem);
                },
                CheckEnabled = () =>
                {
                    return GoodsGrid.SelectedItem != null
                        && GoodsGrid.SelectedItem.Count > 0;
                },
            });
            Commander.SetCurrentGroup("item");
            Commander.Add(new CommandItem()
            {
                Name = "production_scheme_edit",
                Title = "Изменить",
                MenuUse = true,
                HotKey = "Return|DoubleCLick",
                ButtonUse = true,
                ButtonName = "ProductionSchemeEditButton",
                AccessLevel = Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    var id = ProductionSchemeGrid.SelectedItem.CheckGet("PRSP_ID").ToInt();
                    if (id != 0)
                    {
                        new ProductionSchemeProductForm(new ItemMessage()
                        {
                            ReceiverName = ControlName,
                            Action = "production_scheme_refresh"
                        },
                        GoodsGrid.SelectedItem,
                        id);
                    }
                },
                CheckEnabled = () =>
                {
                    return ProductionSchemeGrid.SelectedItem != null
                        && ProductionSchemeGrid.SelectedItem.Count > 0;
                },
            });
            Commander.Add(new CommandItem()
            {
                Name = "production_scheme_detach",
                Title = "Исключить",
                MenuUse = true,
                ButtonUse = true,
                ButtonName = "ProductionSchemeDetachButton",
                AccessLevel = Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    DetachProductionScheme(ProductionSchemeGrid.SelectedItem);
                },
                CheckEnabled = () =>
                {
                    return ProductionSchemeGrid.SelectedItem != null
                        && ProductionSchemeGrid.SelectedItem.Count > 0;
                },
            });

            Commander.SetCurrentGridName("ProductionStageInputGrid");
            Commander.Add(new CommandItem()
            {
                Name = "production_stage_input_refresh",
                Group = "grid_base",
                Enabled = true,
                Title = "Обновить",
                Description = "Обновить",
                ButtonUse = false,
                MenuUse = true,
                ActionMessage = (ItemMessage message) =>
                {
                    ProductionStageInputGrid.LoadItems();
                    ProductionStageInputGrid.SelectRowByKey(message.Message);
                },
            });
            Commander.Add(new CommandItem()
            {
                Name = "production_stage_input_create",
                Group = "grid_base",
                Enabled = true,
                Title = "Создать",
                MenuUse = true,
                HotKey = "Insert",
                ButtonUse = true,
                ButtonName = "ProductionStageInputCreateButton",
                AccessLevel = Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    new ProductionStageInputOutputForm(new ItemMessage() 
                    { 
                        ReceiverName = ControlName, 
                        Action = "production_stage_input_refresh" 
                    }, 
                    false,
                    GoodsGrid.SelectedItem,
                    ProductionSchemeGrid.SelectedItem["PRSP_ID"].ToInt(),
                    ProductionStageGrid.SelectedItem);
                },
                CheckEnabled = () =>
                {
                    if (ProductionStageGrid.SelectedItem == null
                        || ProductionStageGrid.SelectedItem.Count == 0)
                        return false;

                    if (ProductionStageInputGrid.Items.Count > 0
                        && GoodsGrid.SelectedItem.CheckGet("IDK1").ToInt() == 16)
                        return false;

                    return true;
                },
            });
            Commander.SetCurrentGroup("item");
            Commander.Add(new CommandItem()
            {
                Name = "production_stage_input_edit",
                Title = "Изменить",
                MenuUse = true,
                HotKey = "Return|DoubleCLick",
                ButtonUse = true,
                ButtonName = "ProductionStageInputEditButton",
                AccessLevel = Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    var k = ProductionStageInputGrid.GetPrimaryKey();
                    var id = ProductionStageInputGrid.SelectedItem.CheckGet(k).ToInt();
                    if (id != 0)
                    {
                        new ProductionStageInputOutputForm(new ItemMessage() 
                        { 
                            ReceiverName = ControlName, 
                            Action = "production_stage_input_refresh" 
                        },
                        false,
                        GoodsGrid.SelectedItem,
                        ProductionSchemeGrid.SelectedItem["PRSP_ID"].ToInt(),
                        ProductionStageGrid.SelectedItem,
                        id);
                    }
                },
                CheckEnabled = () =>
                {
                    return ProductionStageInputGrid.SelectedItem != null
                        && ProductionStageInputGrid.SelectedItem.Count > 0;
                },
            });
            Commander.Add(new CommandItem()
            {
                Name = "production_stage_input_delete",
                Title = "Удалить",
                MenuUse = true,
                ButtonUse = true,
                ButtonName = "ProductionStageInputDeleteButton",
                AccessLevel = Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    DeleteProductionStageInput(ProductionStageInputGrid.SelectedItem);
                },
                CheckEnabled = () =>
                {
                    return ProductionStageInputGrid.SelectedItem != null
                        && ProductionStageInputGrid.SelectedItem.Count > 0;
                },
            });

            Commander.SetCurrentGridName("ProductionStageOutputGrid");
            Commander.Add(new CommandItem()
            {
                Name = "production_stage_output_refresh",
                Group = "grid_base",
                Enabled = true,
                Title = "Обновить",
                Description = "Обновить",
                ButtonUse = false,
                MenuUse = true,
                ActionMessage = (ItemMessage message) =>
                {
                    ProductionStageOutputGrid.LoadItems();
                    ProductionStageOutputGrid.SelectRowByKey(message.Message);
                },
            });
            Commander.Add(new CommandItem()
            {
                Name = "production_stage_output_create",
                Group = "grid_base",
                Enabled = true,
                Title = "Создать",
                MenuUse = true,
                HotKey = "Insert",
                ButtonUse = true,
                ButtonName = "ProductionStageOutputCreateButton",
                AccessLevel = Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    new ProductionStageInputOutputForm(new ItemMessage()
                    {
                        ReceiverName = ControlName,
                        Action = "production_stage_output_refresh"
                    },
                    true,
                    GoodsGrid.SelectedItem,
                    ProductionSchemeGrid.SelectedItem["PRSP_ID"].ToInt(),
                    ProductionStageGrid.SelectedItem);
                },
                CheckEnabled = () =>
                {
                    if (ProductionStageGrid.SelectedItem == null
                        || ProductionStageGrid.SelectedItem.Count == 0)
                        return false;

                    if (ProductionStageOutputGrid.Items.Count > 0
                        && ProductionStageGrid.SelectedItem["NEXT_PRST_ID"].ToInt() == 0)
                        return false;

                    return true;
                },
            });
            Commander.SetCurrentGroup("item");
            Commander.Add(new CommandItem()
            {
                Name = "production_stage_output_edit",
                Title = "Изменить",
                MenuUse = true,
                HotKey = "Return|DoubleCLick",
                ButtonUse = true,
                ButtonName = "ProductionStageOutputEditButton",
                AccessLevel = Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    var k = ProductionStageOutputGrid.GetPrimaryKey();
                    var id = ProductionStageOutputGrid.SelectedItem.CheckGet(k).ToInt();
                    if (id != 0)
                    {
                        new ProductionStageInputOutputForm(new ItemMessage()
                        {
                            ReceiverName = ControlName,
                            Action = "production_stage_output_refresh"
                        },
                        true,
                        GoodsGrid.SelectedItem,
                        ProductionSchemeGrid.SelectedItem["PRSP_ID"].ToInt(),
                        ProductionStageGrid.SelectedItem,
                        id);
                    }
                },
                CheckEnabled = () =>
                {
                    return ProductionStageOutputGrid.SelectedItem != null
                        && ProductionStageOutputGrid.SelectedItem.Count > 0;
                },
            });
            Commander.Add(new CommandItem()
            {
                Name = "production_stage_output_delete",
                Title = "Удалить",
                MenuUse = true,
                ButtonUse = true,
                ButtonName = "ProductionStageOutputDeleteButton",
                AccessLevel = Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    DeleteProductionStageOutput(ProductionStageOutputGrid.SelectedItem);
                },
                CheckEnabled = () =>
                {
                    if( ProductionStageOutputGrid.SelectedItem == null
                        || ProductionStageOutputGrid.SelectedItem.Count == 0)
                        return false;

                    if (ProductionStageOutputGrid.SelectedItem.CheckGet("ID2").ToInt() == GoodsGrid.SelectedItem.CheckGet("ID2").ToInt())
                        return false;

                    return true;
                },
            });
            Commander.Init(this);
        }

        private void GoodsGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="ИД изделия",
                    Path="ID2",
                    Width2=10,
                    ColumnType=ColumnTypeRef.Integer,

                },
                new DataGridHelperColumn
                {
                    Header="idk1",
                    Path="IDK1",
                    Width2=6,
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="ARTIKUL",
                    ColumnType=ColumnTypeRef.String,
                    Width2=18,
                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="NAME",
                    Doc="Наименование товара",
                    ColumnType=ColumnTypeRef.String,
                    Width2=48,
                },
            };
            GoodsGrid.SetColumns(columns);
            GoodsGrid.SetPrimaryKey("ID2");
            GoodsGrid.Toolbar = GoodsGridToolbar;
            GoodsGrid.Commands = Commander;
            GoodsGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            GoodsGrid.AutoUpdateInterval = 0;
            GoodsGrid.ItemsAutoUpdate = false;
            GoodsGrid.Commands = Commander;
            GoodsGrid.QueryLoadItems = new RequestData()
            {
                Module = "Sources/ProductionScheme2",
                Object = "Goods",
                Action = "List",
                AnswerSectionKey = "ITEMS",
                Timeout = 2000,
                BeforeRequest = (RequestData rd) =>
                {
                    rd.Params = new Dictionary<string, string>()
                            {
                                { "ALL", (AllCheckBox.IsChecked == true).ToInt().ToString() },
                                { "TEXT", "%" + GoodsGridSearch.Text + "%" },
                            };
                },
                AfterRequest = (RequestData rd, ListDataSet ds) =>
                {
                    GoodsGridSearchButton.Style = (Style)GoodsGridSearch.TryFindResource("Button");

                    return ds;
                },
            };
            GoodsGrid.OnSelectItem = (row) =>
            {
                ProductionStageOutputGrid.ClearItems();
                ProductionStageInputGrid.ClearItems();
                ProductionStageGrid.ClearItems();
                ProductionSchemeGrid.ClearItems();
                if (GoodsGrid.SelectedItem.Count > 0) ProductionSchemeGrid.LoadItems();
            };
            GoodsGrid.Init();
        }
        public void ProductionSchemeGridInit()
        {
            var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД схемы",
                        Path="PRSC_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД связи с изделием",
                        Path="PRSP_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=25,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Основная",
                        Path="PRIMARY_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=4,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Архивная",
                        Path="ARCHIVE_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=4,
                    },
                };
            ProductionSchemeGrid.SetColumns(columns);
            ProductionSchemeGrid.SetPrimaryKey("PRSP_ID");
            ProductionSchemeGrid.SearchText = ProductionSchemeSearchText;
            ProductionSchemeGrid.Toolbar = ProductionSchemeGridToolbar;
            ProductionSchemeGrid.Commands = Commander;
            ProductionSchemeGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            ProductionSchemeGrid.AutoUpdateInterval = 0;
            ProductionSchemeGrid.ItemsAutoUpdate = false;
            ProductionSchemeGrid.QueryLoadItems = new RequestData()
            {
                Module = "Sources/ProductionScheme2",
                Object = "ProductionScheme",
                Action = "List",
                AnswerSectionKey = "ITEMS",
                BeforeRequest = (RequestData rd) =>
                {
                    rd.Params = new Dictionary<string, string>()
                            {
                                { "ID2", GoodsGrid.SelectedItem.CheckGet("ID2") },
                            };
                },
            };
            ProductionSchemeGrid.OnSelectItem = (row) =>
            {
                ProductionStageOutputGrid.ClearItems();
                ProductionStageInputGrid.ClearItems();
                ProductionStageGrid.ClearItems();
                if(ProductionSchemeGrid.SelectedItem.Count > 0) ProductionStageGrid.LoadItems();
            };
            ProductionSchemeGrid.Init();
        }
        public void ProductionStageGridInit()
        {
            var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="PRST_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                        DxEnableColumnSorting=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Рабочий центр",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=60,
                        DxEnableColumnSorting=false,
                    },
                };
            ProductionStageGrid.SetColumns(columns);
            ProductionStageGrid.SetPrimaryKey("PRST_ID");
            ProductionStageGrid.SearchText = ProductionStageSearchText;
            ProductionStageGrid.Toolbar = ProductionStageGridToolbar;
            ProductionStageGrid.Commands = Commander;
            ProductionStageGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            ProductionStageGrid.AutoUpdateInterval = 0;
            ProductionStageGrid.ItemsAutoUpdate = false;
            ProductionStageGrid.QueryLoadItems = new RequestData()
            {
                Module = "ProductionCatalog",
                Object = "ProductionStage",
                Action = "List",
                AnswerSectionKey = "ITEMS",
                BeforeRequest = (RequestData rd) =>
                {
                    rd.Params = new Dictionary<string, string>()
                            {
                                { "PRSC_ID", ProductionSchemeGrid.SelectedItem.CheckGet("PRSC_ID") },
                            };
                },
                AfterRequest = (RequestData rd, ListDataSet ds) =>
                {
                    ds.Items.Reverse();

                    var maxLevel = 0;
                    foreach ( var i in ds.Items)
                    {
                        maxLevel = Math.Max(maxLevel, i["LVL"].ToInt());
                    }

                    var prevLevel = new List<int>() { -1 };
                    foreach (var i in ds.Items)
                    {
                        var level = maxLevel - i["LVL"].ToInt();
                        var name = new String('\u2002', level) + "●";

                        if (i["NEXT_PRST_ID"].ToInt() > 0)
                        {
                            if(level < prevLevel.Last())
                            {
                                name += "┐";
                                prevLevel.Add(level);
                            }
                            else if(level > prevLevel.Last())
                            {
                                prevLevel[prevLevel.Count - 1] = level;

                                if (prevLevel.Count > 1
                                && prevLevel[prevLevel.Count - 1] == prevLevel[prevLevel.Count - 2])
                                {
                                    name += "┤";
                                    prevLevel.RemoveAt(prevLevel.Count - 1);
                                }
                                else
                                {
                                    name += "┐";
                                }
                            }
                            else
                            {
                                name += "┤";
                            }

                            for (var v = prevLevel.Count - 2; v >= 0; v--)
                            {
                                if (prevLevel[v] <= level) break;

                                name += new String('\u2002', prevLevel[v] - level - 1) + "│";
                            }
                        }
                        else
                            prevLevel = new List<int>() { -1 };

                        i["NAME"] = name + new String('\u2002', maxLevel - name.Length + 2) + i["NAME"];
                    }


                    return ds;
                }
            };
            ProductionStageGrid.OnSelectItem = (row) =>
            {
                ProductionStageMachineGrid.ClearItems();
                ProductionStageInputGrid.ClearItems();
                ProductionStageOutputGrid.ClearItems();
                if (ProductionStageGrid.SelectedItem.Count > 0)
                {
                    ProductionStageMachineGrid.LoadItems();
                    Commander.Process("production_stage_input_refresh");
                    ProductionStageOutputGrid.LoadItems();
                }
            };
            ProductionStageGrid.Init();
        }

        public void ProductionStageMachineGridInit()
        {
            var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="PRSM_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Краткое наименование",
                        Path="SHORT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Скорость плановая",
                        Path="SPEED_PLAN",
                        ColumnType=ColumnTypeRef.String,
                        Width2=15,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Первичная",
                        Path="PRIMARY_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=4,
                    },
                };
            ProductionStageMachineGrid.SetColumns(columns);
            ProductionStageMachineGrid.SetPrimaryKey("PRSM_ID");
            ProductionStageMachineGrid.SearchText = ProductionStageMachineSearchText;
            ProductionStageMachineGrid.Toolbar = ProductionStageMachineGridToolbar;
            ProductionStageMachineGrid.Commands = Commander;
            ProductionStageMachineGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            ProductionStageMachineGrid.AutoUpdateInterval = 0;
            ProductionStageMachineGrid.ItemsAutoUpdate = false;
            ProductionStageMachineGrid.QueryLoadItems = new RequestData()
            {
                Module = "Sources/ProductionScheme2",
                Object = "ProductionStageMachine",
                Action = "List",
                AnswerSectionKey = "ITEMS",
                BeforeRequest = (RequestData rd) =>
                {
                    rd.Params = new Dictionary<string, string>()
                            {
                                { "PRSP_ID", ProductionSchemeGrid.SelectedItem.CheckGet("PRSP_ID") },
                                { "PRST_ID", ProductionStageGrid.SelectedItem.CheckGet("PRST_ID") },
                            };
                },
            };
            ProductionStageMachineGrid.Init();
        }
        public void ProductionStageInputGridInit()
        {
            var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД заготовки",
                        Path="ID2",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование заготовки",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=40,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Path="QTY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                };
            ProductionStageInputGrid.SetColumns(columns);
            ProductionStageInputGrid.SetPrimaryKey("ID");
            ProductionStageInputGrid.SearchText = ProductionStageInputSearchText;
            ProductionStageInputGrid.Toolbar = ProductionStageInputGridToolbar;
            ProductionStageInputGrid.Commands = Commander;
            ProductionStageInputGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            ProductionStageInputGrid.AutoUpdateInterval = 0;
            ProductionStageInputGrid.ItemsAutoUpdate = false;
            ProductionStageInputGrid.QueryLoadItems = new RequestData()
            {
                Module = "Sources/ProductionScheme2",
                Object = "ProductionStageInput",
                Action = "List",
                AnswerSectionKey = "ITEMS",
                BeforeRequest = (RequestData rd) =>
                {
                    rd.Params = new Dictionary<string, string>()
                            {
                                { "PRSP_ID", ProductionSchemeGrid.SelectedItem.CheckGet("PRSP_ID") },
                                { "PRST_ID", ProductionStageGrid.SelectedItem.CheckGet("PRST_ID") },
                            };
                },
            };
            ProductionStageInputGrid.Init();
        }
        public void ProductionStageOutputGridInit()
        {
            var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД продукта",
                        Path="ID2",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование продукта",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=40,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Path="QTY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                };
            ProductionStageOutputGrid.SetColumns(columns);
            ProductionStageOutputGrid.SetPrimaryKey("ID");
            ProductionStageOutputGrid.SearchText = ProductionStageOutputSearchText;
            ProductionStageOutputGrid.Toolbar = ProductionStageOutputGridToolbar;
            ProductionStageOutputGrid.Commands = Commander;
            ProductionStageOutputGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            ProductionStageOutputGrid.AutoUpdateInterval = 0;
            ProductionStageOutputGrid.ItemsAutoUpdate = false;
            ProductionStageOutputGrid.QueryLoadItems = new RequestData()
            {
                Module = "Sources/ProductionScheme2",
                Object = "ProductionStageOutput",
                Action = "List",
                AnswerSectionKey = "ITEMS",
                BeforeRequest = (RequestData rd) =>
                {
                    rd.Params = new Dictionary<string, string>()
                            {
                                { "PRSP_ID", ProductionSchemeGrid.SelectedItem.CheckGet("PRSP_ID") },
                                { "PRST_ID", ProductionStageGrid.SelectedItem.CheckGet("PRST_ID") },
                            };
                },
            };
            ProductionStageOutputGrid.Init();
        }

        private void DetachProductionScheme(Dictionary<string, string> row)
        {
            if (row == null
                || DialogWindow.ShowDialog($"Исключить \"{row.CheckGet("NAME")}\"?", "Схема", "", DialogWindowButtons.NoYes) != true) return;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sources/ProductionScheme2");
            q.Request.SetParam("Object", "ProductionSchemeProduct");
            q.Request.SetParam("Action", "Delete");
            q.Request.SetParam("PRSP_ID", row.CheckGet("PRSP_ID"));
            q.DoQuery();
            if (q.Answer.Status == 0)
            {
                ProductionSchemeGrid.SelectRowPrev();
                ProductionSchemeGrid.LoadItems();
            }
            else
            {
                q.ProcessError();
            }
        }
        private void DeleteProductionStageInput(Dictionary<string, string> row)
        {
            if (row == null
                || DialogWindow.ShowDialog($"Удалить \"{row.CheckGet("NAME")}\"?", "Заготовки", "", DialogWindowButtons.NoYes) != true) return;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sources/ProductionScheme2");
            q.Request.SetParam("Object", "ProductionStageInput");
            q.Request.SetParam("Action", "Delete");
            q.Request.SetParam("ID", row.CheckGet(ProductionStageInputGrid.GetPrimaryKey()));
            q.DoQuery();
            if (q.Answer.Status == 0)
            {
                ProductionStageInputGrid.SelectRowPrev();
                Commander.Process("production_stage_input_refresh");
            }
            else
            {
                q.ProcessError();
            }
        }
        private void DeleteProductionStageOutput(Dictionary<string, string> row)
        {
            if (row == null
                || DialogWindow.ShowDialog($"Удалить \"{row.CheckGet("NAME")}\"?", "Заготовки", "", DialogWindowButtons.NoYes) != true) return;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sources/ProductionScheme2");
            q.Request.SetParam("Object", "ProductionStageOutput");
            q.Request.SetParam("Action", "Delete");
            q.Request.SetParam("ID", row.CheckGet(ProductionStageOutputGrid.GetPrimaryKey()));
            q.DoQuery();
            if (q.Answer.Status == 0)
            {
                ProductionStageOutputGrid.SelectRowPrev();
                ProductionStageOutputGrid.LoadItems();
            }
            else
            {
                q.ProcessError();
            }
        }

        private void GoodsGridSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            GoodsGridSearchButton.Style = (Style)GoodsGridSearch.TryFindResource("FButtonPrimary");
        }
        private void AllCheckBox_Click(object sender, RoutedEventArgs e)
        {
            GoodsGridSearchButton.Style = (Style)GoodsGridSearch.TryFindResource("FButtonPrimary");
        }
    }
}
