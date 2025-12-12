using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
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
    /// Отчет по расположению ТМЦ
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class ReportInventoryItemPlace : ControlBase
    {
        public ReportInventoryItemPlace()
        {
            InitializeComponent();
            ControlTitle = "Расположение ТМЦ";
            RoleName = "[erp]warehouse_report";
            DocumentationUrl = "/doc/l-pack-erp/";
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
                FormInit();
                SetDefaults();
                GridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                Grid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                Grid.ItemsAutoUpdate = true;
                Grid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                Grid.ItemsAutoUpdate = false;
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

            Commander.SetCurrentGridName("Grid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "export_to_excel",
                    Title = "В Excel",
                    Description = "Экспортировать в Excel",
                    Group = "grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = ExportToExcelButton,
                    ButtonName = "ExportToExcelButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        Grid.ItemsExportExcel();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (Grid != null && Grid.Items != null && Grid.Items.Count > 0)
                        {
                            result = true;
                        }

                        return result;
                    },
                });
            }

            Commander.Init(this);
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Датасет с данными грида
        /// </summary>
        private ListDataSet GridDataSet { get; set; }

        public int FactoryId = 1;

        private void FormInit()
        {
            //инициализация формы
            {
                Form = new FormHelper();

                //колонки формы
                var fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="SEARCH",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=SearchText,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path="ZONE",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=ZoneSelectBox,
                        ControlType="SelectBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                };

                Form.SetFields(fields);
            }
        }


        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            GridDataSet = new ListDataSet();

            ItemTypeSelectBox.Items.Add("0", "Все типы");
            FormHelper.ComboBoxInitHelper(ItemTypeSelectBox, "Warehouse", "ItemGroup", "List", "ID", "NAME", null, true);
            ItemTypeSelectBox.SelectedItem = new KeyValuePair<string, string>("0", "Все типы");

            FormHelper.ComboBoxInitHelper(ZoneSelectBox, "Warehouse", "Zone", "ListByFactory", "WMZO_ID", "ZONE_FULL_NAME", new Dictionary<string, string>() { { "FACTORY_ID", $"{FactoryId}" } }, true);
            ZoneSelectBox.SetSelectedItemFirst();
        }

        private void GridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="#",
                        Description = "Порядковый номер записи",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                        TotalsType=TotalsTypeRef.Count,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид хранилища",
                        Description = "Идентификатор хранилища",
                        Path="STOGARE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Хранилище",
                        Description = "Наименование хранилища",
                        Path="STORAGE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Description = "Наименование ТМЦ",
                        Path="ITEM_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=62,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Позиций, шт.",
                        Description = "Количество единиц ТМЦ, штук",
                        Path="ITEM_COUNT",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=11,
                        Format="N0",
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Description = "Суммарное количество продукции по ТМЦ",
                        Path="ITEM_QUANTITY",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=11,
                        Format="N0",
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ед. изм.",
                        Description = "Единица измерения продукции по ТМЦ",
                        Path="UNIT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Зона",
                        Description = "Наименование зоны",
                        Path="ZONE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=19,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Склад",
                        Description = "Наименование склада",
                        Path="WAREHOUSE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Тип",
                        Description = "Тип ТМЦ",
                        Path="ITEM_GROUP_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=14,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Ид типа",
                        Description = "Идентификатор типа ТМЦ",
                        Path="ITEM_GROUP_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=4,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид склада",
                        Description = "Идентификатор склада",
                        Path="WAREHOUSE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=4,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид зоны",
                        Description = "Идентификатор зоны",
                        Path="ZONE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=4,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ряд",
                        Description = "Наименование ряда",
                        Path="ROW_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=4,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ячейка",
                        Description = "Наименование ячейки",
                        Path="CELL_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=4,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Уровень",
                        Description = "Наименование уровня",
                        Path="LEVEL_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=4,
                        Hidden=true,
                    },
                };
                Grid.SetColumns(columns);
                Grid.SearchText = SearchText;
                Grid.OnLoadItems = GridLoadItems;
                Grid.SetPrimaryKey("_ROWNUMBER");
                Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                Grid.AutoUpdateInterval = 5 * 60;
                Grid.Toolbar = GridToolbar;
                Grid.OnFilterItems = () =>
                {
                    if (Grid.Items != null && Grid.Items.Count > 0)
                    {
                        // Фильтрация по типу ТМЦ
                        // 0 -- Все типы
                        if (ItemTypeSelectBox.SelectedItem.Key != null)
                        {
                            var key = ItemTypeSelectBox.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            switch (key)
                            {
                                // Все типы
                                case 0:
                                    items = Grid.Items;
                                    break;

                                default:
                                    items.AddRange(Grid.Items.Where(x => x.CheckGet("ITEM_GROUP_ID").ToInt() == key));
                                    break;
                            }

                            Grid.Items = items;
                        }
                    }
                };
                Grid.Commands = Commander;
                Grid.UseProgressSplashAuto = false;
                Grid.Init();
            }
        }

        private async void GridLoadItems()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                EnableSplash();
            });

            bool resume = false;

            int zoneid = Form.GetValueByPath("ZONE").ToInt();

            if (zoneid > 0)
            {
                resume = true;
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();
                p.Add("FACTORY_ID", $"{FactoryId}");
                p.Add("ZONE_ID", $"{zoneid}");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Stock");
                q.Request.SetParam("Object", "Report");
                q.Request.SetParam("Action", "InventoryItemPlace");
                q.Request.SetParams(p);
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                GridDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        GridDataSet = ListDataSet.Create(result, "ITEMS");
                    }
                }
                else
                {
                    q.ProcessError();
                }
                Grid.UpdateItems(GridDataSet);
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                DisableSplash();
            });
        }

        private void EnableSplash()
        {
            SplashControl.Message = $"Пожалуйста, подождите.{Environment.NewLine}Идёт загрузка данных.";
            SplashControl.Visible = true;
        }

        private void DisableSplash()
        {
            SplashControl.Message = "";
            SplashControl.Visible = false;
        }

        public void Refresh()
        {
            Grid.LoadItems();
        }

        private void ZoneSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Refresh();
        }

        private void ItemTypeSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }
    }
}
