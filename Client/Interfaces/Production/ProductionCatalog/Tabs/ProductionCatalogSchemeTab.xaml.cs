using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Debug;
using Client.Interfaces.DeliveryAddresses;
using Client.Interfaces.Main;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using GalaSoft.MvvmLight.Messaging;
using iTextSharp.text.log;
using Newtonsoft.Json;
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

namespace Client.Interfaces.ProductionCatalog
{
    /// <summary>
    /// Справочник схем
    /// </summary>
    /// <author>motenko_ek</author>
    public partial class ProductionCatalogSchemeTab : ControlBase
    {
        public ProductionCatalogSchemeTab()
        {
            InitializeComponent();

            ControlSection = "production_catalog_scheme";
            RoleName = "[erp]production_catalog";
            ControlTitle = "Схемы";
            DocumentationUrl = "/doc/l-pack-erp/production/production_catalog/production_scheme";
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
                ProductionSchemeGridInit();
                ProductionStageGridInit();
            };
            OnUnload = () =>
            {
                ProductionSchemeGrid.Destruct();
                ProductionStageGrid.Destruct();
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
            Commander.SetCurrentGridName("ProductionSchemeGrid");
            Commander.Add(new CommandItem()
            {
                Name = "production_catalog_scheme_refresh",
                Group = "grid_base",
                Enabled = true,
                Title = "Обновить",
                Description = "Обновить данные",
                ButtonUse = true,
                ButtonName = "ProductionSchemeRefreshButton",
                MenuUse = true,
                Action = () =>
                {
                    ProductionSchemeGrid.LoadItems();
                },
            });
            Commander.Add(new CommandItem()
            {
                Name = "production_catalog_scheme_add",
                Group = "grid_base",
                Enabled = true,
                Title = "Создать",
                MenuUse = true,
                HotKey = "Insert",
                ButtonUse = true,
                ButtonName = "ProductionSchemeAddButton",
                AccessLevel = Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    new ProductionSchemeForm();
                },
            });
            Commander.SetCurrentGroup("item");
            Commander.Add(new CommandItem()
            {
                Name = "production_catalog_scheme_edit",
                Title = "Изменить",
                MenuUse = true,
                HotKey = "Return|DoubleCLick",
                ButtonUse = true,
                ButtonName = "ProductionSchemeEditButton",
                AccessLevel = Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    var k = ProductionSchemeGrid.GetPrimaryKey();
                    var id = ProductionSchemeGrid.SelectedItem.CheckGet(k).ToInt();
                    if (id != 0)
                    {
                        new ProductionSchemeForm(id);
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
                Name = "production_catalog_scheme_delete",
                Title = "Удалить",
                MenuUse = true,
                ButtonUse = true,
                ButtonName = "ProductionSchemeDeleteButton",
                AccessLevel = Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    DeleteProductionScheme(ProductionSchemeGrid.SelectedItem);
                },
                CheckEnabled = () =>
                {
                    return ProductionSchemeGrid.SelectedItem != null
                        && ProductionSchemeGrid.SelectedItem.Count > 0;
                },
            });
            Commander.SetCurrentGridName("ProductionStageGrid");
            Commander.Add(new CommandItem()
            {
                Name = "production_catalog_stage_refresh",
                Group = "grid_base",
                Enabled = true,
                Title = "Обновить",
                Description = "Обновить данные",
                ButtonUse = false,
                MenuUse = true,
                ActionMessage = (ItemMessage message) =>
                {
                    ProductionStageGrid.LoadItems();
                    ProductionStageGrid.SelectRowByKey(message.Message);
                },
            });
            Commander.Add(new CommandItem()
            {
                Name = "production_catalog_stage_add",
                Group = "grid_base",
                Enabled = true,
                Title = "Создать",
                MenuUse = true,
                HotKey = "Insert",
                ButtonUse = true,
                ButtonName = "ProductionStageAddButton",
                AccessLevel = Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    new ProductionStageForm(ProductionSchemeGrid.SelectedItem);
                },
                CheckEnabled = () =>
                {
                    return ProductionSchemeGrid.SelectedItem != null
                        && ProductionSchemeGrid.SelectedItem.Count > 0;
                },
            });
            Commander.SetCurrentGroup("item");
            Commander.Add(new CommandItem()
            {
                Name = "production_catalog_stage_edit",
                Title = "Изменить",
                MenuUse = true,
                HotKey = "Return|DoubleCLick",
                ButtonUse = true,
                ButtonName = "ProductionStageEditButton",
                AccessLevel = Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    var k = ProductionStageGrid.GetPrimaryKey();
                    var id = ProductionStageGrid.SelectedItem.CheckGet(k).ToInt();
                    if (id != 0)
                    {
                        new ProductionStageForm(ProductionSchemeGrid.SelectedItem, id);
                    }
                },
                CheckEnabled = () =>
                {
                    return ProductionStageGrid.SelectedItem != null
                        && ProductionStageGrid.SelectedItem.Count > 0;
                },
            });
            Commander.Add(new CommandItem()
            {
                Name = "production_catalog_stage_delete",
                Title = "Удалить",
                MenuUse = true,
                ButtonUse = true,
                ButtonName = "ProductionStageDeleteButton",
                AccessLevel = Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    DeleteProductionStage(ProductionStageGrid.SelectedItem);
                },
                CheckEnabled = () =>
                {
                    return ProductionStageGrid.SelectedItem != null
                        && ProductionStageGrid.SelectedItem.Count > 0;
                },
            });
            Commander.Init(this);
        }
        public void ProductionSchemeGridInit()
        {
            var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="PRSC_ID",
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
                        Header="Архивная",
                        Path="ARCHIVE_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=4,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Литая тара",
                        Path="CONTAINER_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=4,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Гильзовый картон",
                        Path="COREBOARD_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=4,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Гофропроизводство",
                        Path="CORRUGATED_CARDBOARD_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=4,
                    },
                };
            ProductionSchemeGrid.SetColumns(columns);
            ProductionSchemeGrid.SetPrimaryKey("PRSC_ID");
            ProductionSchemeGrid.SearchText = ProductionSchemeSearchText;
            ProductionSchemeGrid.Toolbar = ProductionSchemeGridToolbar;
            ProductionSchemeGrid.Commands = Commander;
            ProductionSchemeGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            ProductionSchemeGrid.AutoUpdateInterval = 0;
            ProductionSchemeGrid.ItemsAutoUpdate = false;
            ProductionSchemeGrid.QueryLoadItems = new RequestData()
            {
                Module = "ProductionCatalog",
                Object = "ProductionScheme",
                Action = "List",
                AnswerSectionKey = "ITEMS",
            };
            ProductionSchemeGrid.OnSelectItem = (row) =>
            {
                ProductionStageGrid.LoadItems();
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
                    //new DataGridHelperColumn
                    //{
                    //    Header="Схема производства",
                    //    Path="PRSC_NAME",
                    //    ColumnType=ColumnTypeRef.String,
                    //    Width2=15,
                    //},
                    new DataGridHelperColumn
                    {
                        Header="Рабочий центр",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=80,
                        DxEnableColumnSorting=false,
                    },
                    //new DataGridHelperColumn
                    //{
                    //    Header="ИД следующего этапа",
                    //    Path="NEXT_PRST_ID",
                    //    ColumnType=ColumnTypeRef.Integer,
                    //    Width2=6,
                    //},
                    //new DataGridHelperColumn
                    //{
                    //    Header="Схема производства следующего этапа",
                    //    Path="NEXT_PRSC_NAME",
                    //    ColumnType=ColumnTypeRef.String,
                    //    Width2=15,
                    //},
                    //new DataGridHelperColumn
                    //{
                    //    Header="Рабочий центр следующего этапа",
                    //    Path="NEXT_PRWO_NAME",
                    //    ColumnType=ColumnTypeRef.String,
                    //    Width2=15,
                    //},
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
            ProductionStageGrid.Init();
        }

        private async void DeleteProductionScheme(Dictionary<string, string> row)
        {
            if (row == null
                || DialogWindow.ShowDialog($"Удалить схему \"{row.CheckGet("NAME")}\"?", "Схема", "", DialogWindowButtons.NoYes) != true) return;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionCatalog");
            q.Request.SetParam("Object", "ProductionScheme");
            q.Request.SetParam("Action", "Delete");
            q.Request.SetParam("PRSC_ID", row.CheckGet(ProductionSchemeGrid.GetPrimaryKey()));
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
        private async void DeleteProductionStage(Dictionary<string, string> row)
        {
            if (row == null
                || DialogWindow.ShowDialog($"Удалить этап \"{row.CheckGet("NAME")}\"?", "Этап", "", DialogWindowButtons.NoYes) != true) return;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionCatalog");
            q.Request.SetParam("Object", "ProductionStage");
            q.Request.SetParam("Action", "Delete");
            q.Request.SetParam("PRST_ID", row.CheckGet(ProductionStageGrid.GetPrimaryKey()));
            q.DoQuery();
            if (q.Answer.Status == 0)
            {
                ProductionStageGrid.SelectRowPrev();
                ProductionStageGrid.LoadItems();
            }
            else
            {
                q.ProcessError();
            }
        }
    }
}
