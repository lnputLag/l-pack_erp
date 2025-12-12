using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Accounts;
using Client.Interfaces.Main;
using DevExpress.DirectX.StandardInterop.Direct2D;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;
using ControlBase = Client.Interfaces.Main.ControlBase;

namespace Client.Interfaces.Sources
{
    /// <summary>
    /// Страница перестилов и их производственных заданий
    /// </summary>
    /// /// <author>lavrenteva_ma</author>
    public partial class InterlayerTab : ControlBase
    {
        public InterlayerTab()
        {
            InitializeComponent();

            ControlSection = "interlayer";
            RoleName = "[erp]interlayer";
            ControlTitle = "Перестил";
            DocumentationUrl = "/doc/l-pack-erp-new/products_materials/interlayer";

            Id2 = 0;
            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    switch (m.Action)
                    {
                        case "RefreshGrid":
                            InterlayerGrid.LoadItems();
                            break;
                        default:
                            break;
                    }

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
                InterlayerGridInit();
                ProductionTaskGridInit();
            };
            InterlayerGrid.OnLoadItems = InterlayerGridLoadItems;

            Commander.SetCurrentGridName("InterlayerGrid");
            {
                Commander.SetCurrentGroup("item");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "update",
                        Group = "crud",
                        Enabled = true,
                        Title = "Изменить",
                        Description = "Изменить перестил",
                        ButtonUse = true,
                        ButtonName = "InterlayerChangeButton",
                        MenuUse = true,
                        HotKey = "DoubleCLick",
                        AccessLevel = Common.Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                        
                            int id2 = InterlayerGrid.SelectedItem.CheckGet("ID2").ToInt();
                            var i = new InterlayerForm();
                            i.Values = InterlayerGrid.SelectedItem;
                            i.Init();
                            i.Id2= id2;
                            i.Show();
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
                        Action = () =>
                        {
                            Central.ShowHelp(DocumentationUrl);
                        },
                    });
                }
            }
            Commander.Init(this);
        }

        private int Id2 { get; set; }

        private void InterlayerGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="ИД товара",
                    Path="ID2",
                    Width2=9,
                    ColumnType=ColumnTypeRef.Integer,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="ARTIKUL",
                    ColumnType=ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="NAME",
                    Doc="Наименование товара",
                    Description="Наименование товара",
                    ColumnType=ColumnTypeRef.String,
                    Width2=35,
                },
                new DataGridHelperColumn
                {
                    Header = "Мин. ост.",
                    Path="MIN_REMAINDER",
                    Doc="Минимальный остаток на складе",
                    Description="Минимальный остаток на складе",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header = "Заказ на станки",
                    Path="RECYCLING_FLAG",
                    Description="Возможность заказывать перестил в ручном режиме на станки переработки",
                    Doc="Возможность заказывать перестил в ручном режиме на станки переработки",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header = "Заказ на сигнод",
                    Path="SIGNODE_FLAG",
                    Description="Возможность заказывать перестил в ручном режиме на сигнод",
                    Doc="Возможность заказывать перестил в ручном режиме на сигнод",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=13,
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во для ПЗ",
                    Path="TASK_QTY",
                    Description="Количество для производственного задания",
                    Doc="Возможность заказывать перестил в ручном режиме на сигнод",
                    ColumnType=ColumnTypeRef.String,
                    Width2=13,
                },

            };
            InterlayerGrid.SetColumns(columns);
            InterlayerGrid.SetPrimaryKey("ID2");
            InterlayerGrid.SetSorting("NAME", ListSortDirection.Ascending);
            InterlayerGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

            InterlayerGrid.Toolbar = InterlayerGridToolbar;
            InterlayerGrid.ItemsAutoUpdate = false;
            InterlayerGrid.Commands = Commander;
            InterlayerGrid.SearchText = InterlayerSearchText;
            InterlayerGrid.OnSelectItem = (row) =>
            {
                ProductionTaskGridLoadItems();
            };
            InterlayerGrid.Init();
        }
        private void ProductionTaskGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="ИД ПЗ",
                    Path="ID_PZ",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Номер ПЗ",
                    Description="Внешний номер ПЗ",
                    Path="NUM",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Дата",
                    Path="DATA",
                    ColumnType=ColumnTypeRef.DateTime,
                    Format="dd.MM.yyyy",
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Количество",
                    Description="Количество по данному ПЗ в шт.",
                    Path="KOL",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Выполнено",
                    Path="POSTING",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=13,
                },
            };
            ProductionTaskGrid.SetColumns(columns);
            ProductionTaskGrid.SetPrimaryKey("ID_PZ");
            ProductionTaskGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            ProductionTaskGrid.Toolbar = ProductionTaskGridToolbar;
            ProductionTaskGrid.ItemsAutoUpdate = false;
            ProductionTaskGrid.Commands = Commander;
            ProductionTaskGrid.SearchText = ProductionTaskSearchText;
            ProductionTaskGrid.Init();
        }

        private async void ProductionTaskGridLoadItems()
        {
            
            Id2 = InterlayerGrid.SelectedItem.CheckGet("ID2").ToInt();
            if (Id2 > 0)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID2", Id2.ToString());
                }
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sources/Interlayer");
                q.Request.SetParam("Object", "ProductionTask");
                q.Request.SetParam("Action", "ListById2");
                q.Request.SetParams(p);

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
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");

                            var modeList = Acl.GetAccessModeList();
                            ds.Items = ListDataSet.AddColumnToList(ds.Items, "DATA_ACCESS_MODE", "_MODE_NAME", modeList);
                            ProductionTaskGrid.UpdateItems(ds);

                        }
                    }
                }
            }
        }
        private async void InterlayerGridLoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sources/Interlayer");
            q.Request.SetParam("Object", "Interlayer");
            q.Request.SetParam("Action", "List");
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        InterlayerGrid.UpdateItems(ds);

                    }
                }
            }
            else
            {
                q.ProcessError();
            }
            
        }

        private void HelpButtonClick(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp-new/products_materials/interlayer");
        }
    }
}
