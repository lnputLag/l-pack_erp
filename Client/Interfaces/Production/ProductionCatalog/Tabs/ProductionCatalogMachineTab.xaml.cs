using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Debug;
using Client.Interfaces.DeliveryAddresses;
using Client.Interfaces.Main;
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
    /// Справочник станков
    /// </summary>
    /// <author>motenko_ek</author>
    public partial class ProductionCatalogMachineTab : ControlBase
    {
        public ProductionCatalogMachineTab()
        {
            InitializeComponent();

            ControlSection = "production_catalog_machine";
            RoleName = "[erp]production_catalog";
            ControlTitle = "Станки";
            DocumentationUrl = "/doc/l-pack-erp/production/production_catalog/production_machine";
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
                ProductionMachineGridInit();
            };
            OnUnload = () =>
            {
                ProductionMachineGrid.Destruct();
            };
            OnNavigate = () =>
            {
                var id = Parameters.CheckGet("PRMA_ID");
                if (!id.IsNullOrEmpty())
                {
                    ProductionMachineSearchText.Text = id;
                    ProductionMachineGrid.UpdateItems();
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
            Commander.SetCurrentGridName("ProductionMachineGrid");
            Commander.Add(new CommandItem()
            {
                Name = "production_catalog_machine_refresh",
                Group = "grid_base",
                Enabled = true,
                Title = "Обновить",
                Description = "Обновить данные станков",
                ButtonUse = true,
                ButtonName = "ProductionMachineRefreshButton",
                MenuUse = true,
                Action = () =>
                {
                    ProductionMachineGrid.LoadItems();
                },
            });
            Commander.Add(new CommandItem()
            {
                Name = "production_catalog_machine_add",
                Group = "grid_base",
                Enabled = true,
                Title = "Создать станок",
                MenuUse = true,
                HotKey = "Insert",
                ButtonUse = true,
                ButtonName = "ProductionMachineAddButton",
                AccessLevel = Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    new ProductionMachineForm();
                },
            });
            Commander.SetCurrentGroup("item");
            Commander.Add(new CommandItem()
            {
                Name = "production_catalog_machine_edit",
                Title = "Изменить станок",
                MenuUse = true,
                HotKey = "Return|DoubleCLick",
                ButtonUse = true,
                ButtonName = "ProductionMachineEditButton",
                AccessLevel = Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    var k = ProductionMachineGrid.GetPrimaryKey();
                    var id = ProductionMachineGrid.SelectedItem.CheckGet(k).ToInt();
                    if (id != 0)
                    {
                        new ProductionMachineForm(id);
                    }
                },
                CheckEnabled = () =>
                {
                    return ProductionMachineGrid.SelectedItem != null
                        && ProductionMachineGrid.SelectedItem.Count > 0;
                },
            });
            Commander.Add(new CommandItem()
            {
                Name = "production_catalog_machine_delete",
                Title = "Удалить станок",
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
        public void ProductionMachineGridInit()
        {
            var columns = new List<DataGridHelperColumn>
                {
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
            ProductionMachineGrid.SetPrimaryKey("PRMA_ID");
            ProductionMachineGrid.SearchText = ProductionMachineSearchText;
            ProductionMachineGrid.Toolbar = ProductionMachineGridToolbar;
            ProductionMachineGrid.Commands = Commander;
            ProductionMachineGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            ProductionMachineGrid.AutoUpdateInterval = 0;
            ProductionMachineGrid.ItemsAutoUpdate = false;
            ProductionMachineGrid.QueryLoadItems = new RequestData()
            {
                Module = "ProductionCatalog",
                Object = "ProductionMachine",
                Action = "List",
                AnswerSectionKey = "ITEMS",
            };
            ProductionMachineGrid.OnSelectItem = (Dictionary<string, string> selectedItem) =>
            {
                selectedItem = selectedItem;
            };
            ProductionMachineGrid.Init();
        }

        private async void DeleteProductionMachine(Dictionary<string, string> row)
        {
            if (row == null
                || DialogWindow.ShowDialog($"Удалить станок \"{row.CheckGet("NAME")}\"?", "Станок", "", DialogWindowButtons.NoYes) != true) return;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionCatalog");
            q.Request.SetParam("Object", "ProductionMachine");
            q.Request.SetParam("Action", "Delete");
            q.Request.SetParam("PRMA_ID", row.CheckGet(ProductionMachineGrid.GetPrimaryKey()));
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
