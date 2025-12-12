using Client.Common;
using Client.Interfaces.Main;
using DevExpress.Xpf.Editors;
using GalaSoft.MvvmLight.Messaging;
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

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Интерфейс управления областями хранения для WMS системы
    /// </summary>
    public partial class CatalogArea : ControlBase
    {
        public CatalogArea()
        {
            ControlTitle = "Область хранения";
            DocumentationUrl = "/doc/l-pack-erp/";
            RoleName = "[erp]warehouse_directory";
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
                AreaGridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                AreaGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                AreaGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
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
                    ButtonControl = RefreshStorageButton,
                    ButtonName = "RefreshStorageButton",
                    Action = () =>
                    {
                        Refresh();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "create_storage",
                    Group = "main",
                    Enabled = true,
                    Title = "Создать",
                    Description = "Создать область хранения",
                    ButtonUse = true,
                    ButtonControl = CreateStorageButton,
                    ButtonName = "CreateStorageButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        CreateStorageArea();
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

            Commander.SetCurrentGridName("AreaGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "edit_storage",
                    Title = "Изменить",
                    Group = "area_grid_default",
                    Enabled = true,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = EditStorageButton,
                    ButtonName = "EditStorageButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        EditStorageArea();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (AreaGrid != null && AreaGrid.Items != null && AreaGrid.Items.Count > 0)
                        {
                            if (AreaGrid.SelectedItem != null && AreaGrid.SelectedItem.Count > 0)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
            }

            Commander.Init(this);
        }

        private ListDataSet AreaDataSet { get; set; }

        public void SetDefaults()
        {
            AreaDataSet = new ListDataSet();

            StorageFilter.Items.Add("-1", "Все");
            FormHelper.ComboBoxInitHelper(StorageFilter, "Warehouse", "Warehouse", "List", "WMWA_ID", "WAREHOUSE", null, true);
            StorageFilter.SelectedItem = new KeyValuePair<string, string>("-1", "Все");
        }

        /// <summary>
        /// Инициализация грида
        /// </summary>
        private void AreaGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="WMSA_ID",
                        Doc="ИД типа ячейки",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Область хранения",
                        Path="AREA",
                        Doc="Область хранения",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 27,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Склад",
                        Path="WAREHOUSE",
                        Doc="Склад",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 16,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ограничение по количеству",
                        Path="ITEM_LIMIT_CNT",
                        Doc="Ограничение по количеству",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 22,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид склада",
                        Path="WMWA_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },
                };
                AreaGrid.SetColumns(columns);
                AreaGrid.SetPrimaryKey(WarehouseAreaItem.ID);
                AreaGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                AreaGrid.SearchText = StorageSearchBox;
                AreaGrid.Toolbar = StorageGridToolbar;

                //двойной клик на строке откроет форму редактирования
                AreaGrid.OnDblClick = selectedItem =>
                {
                    EditStorageArea();
                };

                //данные грида
                AreaGrid.OnLoadItems = AreaGridLoadItems;

                AreaGrid.OnFilterItems = () =>
                {
                    if (AreaGrid.Items != null && AreaGrid.Items.Count > 0)
                    {
                        // Фильтрация по складу
                        if (StorageFilter.SelectedItem.Key != null)
                        {
                            var key = StorageFilter.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            switch (key)
                            {
                                // Все склады
                                case -1:
                                    items = AreaGrid.Items;
                                    break;

                                default:
                                    items.AddRange(AreaGrid.Items.Where(x => x.CheckGet("WMWA_ID").ToInt() == key));
                                    break;
                            }

                            AreaGrid.Items = items;
                        }
                    }
                };

                AreaGrid.Commands = Commander;

                AreaGrid.Init();
            }
        }

        private async void AreaGridLoadItems()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "StorageArea");
            q.Request.SetParam("Action", "List");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            AreaDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    AreaDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            AreaGrid.UpdateItems(AreaDataSet);
        }

        private void StorageFilter_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (AreaGrid != null && AreaGrid.Items != null)
            {
                AreaGrid.SelectedItem = new Dictionary<string, string>();
                AreaGrid.UpdateItems();
            }
        }

        public void Refresh()
        {
            AreaGridLoadItems();
        }

        public void CreateStorageArea()
        {
            new WarehouseAreaItem().Create();
        }

        private void EditStorageArea()
        {
            if (Central.Navigator.GetRoleLevel("[erp]warehouse_directory") >= Role.AccessMode.FullAccess)
            {
                if (AreaGrid != null && AreaGrid.SelectedItem != null && AreaGrid.SelectedItem.Count > 0)
                {
                    new WarehouseAreaItem().Edit(AreaGrid.SelectedItem.CheckGet(WarehouseAreaItem.ID).ToInt());
                }
            }
        }
    }
}
