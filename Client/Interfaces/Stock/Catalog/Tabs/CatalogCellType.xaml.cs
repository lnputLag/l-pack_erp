using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using NPOI.SS.UserModel;
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
    /// Интерфейс редактирования типов ячеек WMS
    /// </summary>
    public partial class CatalogCellType : ControlBase
    {
        public CatalogCellType()
        {
            ControlTitle = "Тип ячейки";
            DocumentationUrl = "/doc/l-pack-erp/warehouse/warehouseDirectory/warehouseCellType";
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
                CellTypeGridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                CellTypeGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                CellTypeGrid.Run();
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
                    ButtonControl = RefreshCellTypeButton,
                    ButtonName = "RefreshCellTypeButton",
                    Action = () =>
                    {
                        Refresh();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "create_cell_type",
                    Group = "main",
                    Enabled = true,
                    Title = "Создать",
                    Description = "Создать тип ячейки",
                    ButtonUse = true,
                    ButtonControl = CreateCellTypeButton,
                    ButtonName = "CreateCellTypeButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        CreateCellType();
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

            Commander.SetCurrentGridName("CellTypeGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "edit_cell_type",
                    Title = "Изменить",
                    Group = "cell_type_grid_default",
                    Enabled = true,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = EditCellTypeButton,
                    ButtonName = "EditCellTypeButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        EditCellType();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (CellTypeGrid != null && CellTypeGrid.Items != null && CellTypeGrid.Items.Count > 0)
                        {
                            if (CellTypeGrid.SelectedItem != null && CellTypeGrid.SelectedItem.Count > 0)
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

        private ListDataSet CellTypeDataSet { get; set; }

        public void SetDefaults()
        {
            CellTypeDataSet = new ListDataSet();

            StorageFilter.Items.Add("-1", "Все");
            FormHelper.ComboBoxInitHelper(StorageFilter, "Warehouse", "Warehouse", "List", "WMWA_ID", "WAREHOUSE", null, true);
            StorageFilter.SelectedItem = new KeyValuePair<string, string>("-1", "Все");
        }

        /// <summary>
        /// Инициализация грида
        /// </summary>
        private void CellTypeGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="WMSY_ID",
                        Doc="ИД типа ячейки",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Тип ячейки",
                        Path="STORAGE_TYPE",
                        Doc="Тип ячейки",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 38,
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
                        Header="Стеллажная ячейка",
                        Path="RACK_FLAG",
                        Doc="Стелажное хранение",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2 = 15,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ширина",
                        Path="WIDTH",
                        Doc="Ширина",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Глубина",
                        Path="DEPTH",
                        Doc="Глубина",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Высота",
                        Path="HEIGHT",
                        Doc="Высота",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вес",
                        Path="WEIGHT",
                        Doc="Вес",
                        ColumnType=ColumnTypeRef.Double,
                        Format = "N2",
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Использование штрихкодов",
                        Path="BARCODE_FLAG",
                        Doc="Использование штрихкодов",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2 = 21,
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
                CellTypeGrid.SetColumns(columns);
                CellTypeGrid.SetPrimaryKey("WMSY_ID");
                CellTypeGrid.SearchText = StorageSearchBox;
                CellTypeGrid.Toolbar = CellTypeGridToolbar;
                CellTypeGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

                //двойной клик на строке откроет форму редактирования
                CellTypeGrid.OnDblClick = selectedItem =>
                {
                    EditCellType();
                };

                //данные грида
                CellTypeGrid.OnLoadItems = CellTypeGridLoadItems;

                CellTypeGrid.OnFilterItems = () =>
                {
                    if (CellTypeGrid.Items != null && CellTypeGrid.Items.Count > 0)
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
                                    items = CellTypeGrid.Items;
                                    break;

                                default:
                                    items.AddRange(CellTypeGrid.Items.Where(x => x.CheckGet("WMWA_ID").ToInt() == key));
                                    break;
                            }

                            CellTypeGrid.Items = items;
                        }
                    }
                };

                CellTypeGrid.Commands = Commander;

                CellTypeGrid.Init();
            }
        }

        /// <summary>
        /// Загрузка данными грида
        /// </summary>
        private async void CellTypeGridLoadItems()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "StorageType");
            q.Request.SetParam("Action", "List");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            CellTypeDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    CellTypeDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            CellTypeGrid.UpdateItems(CellTypeDataSet);
        }

        public void CreateCellType()
        {
            new WarehouseCellTypeItem().Create();
        }

        public void Refresh()
        {
            CellTypeGridLoadItems();
        }

        private void StorageFilter_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (CellTypeGrid != null && CellTypeGrid.Items != null)
            {
                CellTypeGrid.SelectedItem = new Dictionary<string, string>();
                CellTypeGrid.UpdateItems();
            }
        }

        /// <summary>
        /// редактирование текущей позицмм
        /// </summary>
        private void EditCellType()
        {
            if (Central.Navigator.GetRoleLevel("[erp]warehouse_directory") >= Role.AccessMode.FullAccess)
            {
                if (CellTypeGrid != null && CellTypeGrid.SelectedItem != null && CellTypeGrid.SelectedItem.Count > 0)
                {
                    new WarehouseCellTypeItem().Edit(CellTypeGrid.SelectedItem.CheckGet("WMSY_ID").ToInt());
                }
            }
        }
    }
}
