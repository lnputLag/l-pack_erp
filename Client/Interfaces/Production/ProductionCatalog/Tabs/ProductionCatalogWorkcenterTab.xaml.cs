using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Debug;
using Client.Interfaces.Delivery;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
using Client.Interfaces.Production;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using GalaSoft.MvvmLight.Messaging;
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
    /// Доставка покупателю
    /// </summary>
    /// <author>motenko_ek</author>
    public partial class ProductionCatalogWorkcenterTab : ControlBase
    {
        public ProductionCatalogWorkcenterTab()
        {
            InitializeComponent();

            ControlSection = "production_catalog_workcenter";
            RoleName = "[erp]production_catalog";
            ControlTitle = "Рабочие центры";
            DocumentationUrl = "/doc/l-pack-erp/production/production_catalog/production_workcenter";
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
                ProductionWorkcenterGridInit();
                ProductionMachineGridInit();
            };
            OnUnload = () =>
            {
                ProductionWorkcenterGrid.Destruct();
                ProductionMachineGrid.Destruct();
            };
            OnNavigate = () =>
            {
                var id = Parameters.CheckGet("PRWO_ID");
                if (!id.IsNullOrEmpty())
                {
                    ProductionWorkcenterSearchText.Text = id;
                    ProductionWorkcenterGrid.UpdateItems();
                }

                id = Parameters.CheckGet("PRMA_ID");
                if (!id.IsNullOrEmpty())
                {
                    ProductionMachineSearchText.Text = id;
                    ProductionMachineGrid.UpdateItems();
                }
            };

            Commander.SetCurrentGroup("main");
            //Commander.Add(new CommandItem()
            //{
            //    Name = "help",
            //    Enabled = true,
            //    Title = "Справка",
            //    Description = "Показать справочную информацию",
            //    ButtonUse = true,
            //    ButtonName = "HelpButton",
            //    HotKey = "F1",
            //    Action = () =>
            //    {
            //        Central.ShowHelp(DocumentationUrl);
            //    },
            //});
            Commander.SetCurrentGridName("ProductionWorkcenterGrid");
            Commander.Add(new CommandItem()
            {
                Name = "production_catalog_workcenter_refresh",
                Group = "grid_base",
                Enabled = true,
                Title = "Обновить",
                Description = "Обновить",
                ButtonUse = true,
                ButtonName = "ProductionWorkcenterRefreshButton",
                MenuUse = true,
                Action = () =>
                {
                    ProductionWorkcenterGrid.LoadItems();
                },
            });
            Commander.SetCurrentGroup("item");
            Commander.SetCurrentGridName("ProductionMachineGrid");
            Commander.Add(new CommandItem()
            {
                Name = "production_catalog_machine_refresh",
                Group = "grid_base",
                Enabled = true,
                Title = "Обновить",
                Description = "Обновить",
                ButtonUse = false,
                MenuUse = true,
                ActionMessage = (ItemMessage message) =>
                {
                    ProductionMachineGrid.LoadItems();
                    ProductionMachineGrid.SelectRowByKey(message.Message);
                },
            });
            Commander.Add(new CommandItem()
            {
                Name = "production_catalog_machine_add",
                Title = "Добавить",
                MenuUse = true,
                HotKey = "Insert",
                ButtonUse = true,
                ButtonName = "ProductionMachineAddButton",
                AccessLevel = Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    new ProductionWorkcenterMachineForm(ProductionWorkcenterGrid.SelectedItem);
                },
                CheckEnabled = () =>
                {
                    return ProductionWorkcenterGrid.SelectedItem != null
                        && ProductionWorkcenterGrid.SelectedItem.Count > 0;
                },
            });
            Commander.SetCurrentGroup("item");
            Commander.Add(new CommandItem()
            {
                Name = "production_catalog_machine_delete",
                Title = "Исключить",
                MenuUse = true,
                ButtonUse = true,
                ButtonName = "ProductionMachineDeleteButton",
                AccessLevel = Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    DeleteProductionMachine(ProductionMachineGrid.SelectedItem);
                },
                CheckEnabled = () =>
                {
                    return ProductionMachineGrid.SelectedItem != null
                        && ProductionMachineGrid.SelectedItem.Count > 0;
                },
            });
            Commander.Init(this);
        }

        private string Id;

        public void ProductionWorkcenterGridInit()
        {
            var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="PRWO_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=30,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Короткое наименование",
                        Path="SHORT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=30,
                    },
                };
            ProductionWorkcenterGrid.SetColumns(columns);
            ProductionWorkcenterGrid.SetPrimaryKey("PRWO_ID");
            ProductionWorkcenterGrid.SearchText = ProductionWorkcenterSearchText;
            ProductionWorkcenterGrid.Toolbar = ProductionWorkcenterGridToolbar;
            ProductionWorkcenterGrid.Commands = Commander;
            ProductionWorkcenterGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            ProductionWorkcenterGrid.AutoUpdateInterval = 0;
            ProductionWorkcenterGrid.ItemsAutoUpdate = false;
            ProductionWorkcenterGrid.QueryLoadItems = new RequestData()
            {
                Module = "ProductionCatalog",
                Object = "ProductionWorkcenter",
                Action = "List",
                AnswerSectionKey = "ITEMS",
            };
            ProductionWorkcenterGrid.OnSelectItem = (row) =>
            {
                ProductionMachineGrid.LoadItems();
            };
            ProductionWorkcenterGrid.Init();
        }

        public void ProductionMachineGridInit()
        {
            var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="PRWM_ID",
                        Visible=false,
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="PRMA_ID",
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
                        Header="Краткое наименование",
                        Path="SHORT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Группа станков",
                        Path="PRMG_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=15,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Производство",
                        Path="PROD_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=15,
                    },
                };
            ProductionMachineGrid.SetColumns(columns);
            ProductionMachineGrid.SetPrimaryKey("PRWM_ID");
            ProductionMachineGrid.SearchText = ProductionMachineSearchText;
            ProductionMachineGrid.Toolbar = ProductionMachineGridToolbar;
            ProductionMachineGrid.Commands = Commander;
            ProductionMachineGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            ProductionMachineGrid.AutoUpdateInterval = 0;
            ProductionMachineGrid.ItemsAutoUpdate = false;
            ProductionMachineGrid.QueryLoadItems = new RequestData()
            {
                Module = "ProductionCatalog",
                Object = "ProductionWorkcenterMachine",
                Action = "List",
                AnswerSectionKey = "ITEMS",
                BeforeRequest = (RequestData rd) =>
                    {
                        rd.Params = new Dictionary<string, string>()
                            {
                                { "PRWO_ID", ProductionWorkcenterGrid.SelectedItem.CheckGet("PRWO_ID") },
                            };
                    },
            };
            ProductionMachineGrid.Init();
        }

        private async void DeleteProductionMachine(Dictionary<string, string> row)
        {
            var message = $"Исключить \"{row.CheckGet("NAME")}\"?";
            if (DialogWindow.ShowDialog(message, "Станок", "", DialogWindowButtons.NoYes) != true)return;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionCatalog");
            q.Request.SetParam("Object", "ProductionWorkcenterMachine");
            q.Request.SetParam("Action", "Delete");
            q.Request.SetParam("PRWM_ID", row.CheckGet("PRWM_ID"));
            q.DoQuery();
            if (q.Answer.Status == 0)
            {
                ProductionMachineGrid.SelectRowPrev();
                ProductionMachineGrid.LoadItems();
            }
            else
            {
                q.ProcessError();
            }
        }
    }
}
