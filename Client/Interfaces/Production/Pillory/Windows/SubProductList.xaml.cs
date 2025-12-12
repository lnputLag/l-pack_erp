using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
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
using static DevExpress.Data.Filtering.Helpers.SubExprHelper.ThreadHoppingFiltering;

namespace Client.Interfaces.Production.Pillory
{
    /// <summary>
    /// Интерфейс заказа перестила на станки
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class SubProductList : ControlBase
    {
        public SubProductList()
        {
            ControlTitle = "Заказ перестила";
            RoleName = "[erp]pillory";
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
                Commander.Init(this);

                FormInit();
                SetDefaults();
                SubProductGridInit();
                PalletGridInit();
                ProductionTaskGridInit();
                OrderGridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                SubProductGrid?.Destruct();
                OrderGrid?.Destruct();
                PalletGrid?.Destruct();
                ProductionTaskGrid.Destruct();
            };

            OnFocusGot = () =>
            {
            };

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
                    ButtonControl = RefreshButton,
                    ButtonName = "RefreshButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
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
                    ButtonName = "HelpButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    HotKey = "F1",
                    Action = () =>
                    {
                        Central.ShowHelp(DocumentationUrl);
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "cancel",
                    Group = "main",
                    Enabled = true,
                    Title = "",
                    Description = "Отмена",
                    ButtonUse = true,
                    ButtonControl = CancelButton,
                    ButtonName = "CancelButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        Close();
                    },
                });
            }

            Commander.SetCurrentGridName("SubProductGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "create_task",
                    Title = "Создать задание",
                    Description = "Создать задание на изготовление перестила",
                    Group = "subproduct_grid_default",
                    Enabled = true,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = CreateTaskButton,
                    ButtonName = "CreateTaskButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        CreateTask();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (SubProductGrid != null && SubProductGrid.SelectedItem != null && SubProductGrid.SelectedItem.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(SubProductGrid.SelectedItem.CheckGet("PRODUCT_ID")))
                            {
                                if (SubProductGrid.SelectedItem.CheckGet("MAX_BALANCE").ToInt() == 0
                                    || SubProductGrid.SelectedItem.CheckGet("CURRENT_BALANCE").ToInt() < SubProductGrid.SelectedItem.CheckGet("MAX_BALANCE").ToInt())
                                {
                                    if (!string.IsNullOrEmpty(SubProductGrid.SelectedItem.CheckGet("LAST_PRODUCTION_TASK_ID")))
                                    {
                                        result = true;
                                    }
                                }
                            }
                        }

                        return result;
                    },
                });
            }

            Commander.SetCurrentGridName("OrderGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "create_order",
                    Title = "Заказать",
                    Description = "Заказать выбранный перестил на выбранный станок",
                    Group = "order_grid_default",
                    Enabled = true,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = CreateOrderButton,
                    ButtonName = "CreateOrderButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        CreateOrder();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (SubProductGrid != null && SubProductGrid.SelectedItem != null && SubProductGrid.SelectedItem.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(SubProductGrid.SelectedItem.CheckGet("PRODUCT_ID")))
                            {
                                if (!string.IsNullOrEmpty(Form.GetValueByPath("MACHINE")))
                                {
                                    result = true;
                                }
                            }
                        }

                        return result;
                    },
                });
            }

            Commander.SetCurrentGridName("PalletGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "consumption_pallet",
                    Title = "Списать",
                    Description = "Списать поддон",
                    Group = "pallet_grid_default",
                    Enabled = true,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = ConsumptionPalletButton,
                    ButtonName = "ConsumptionPalletButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        ConsumptionPallet();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (PalletGrid != null && PalletGrid.Items != null && PalletGrid.Items.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(Form.GetValueByPath("MACHINE_PALLET")))
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
            }
        }

        /// <summary>
        /// Наименование таба, который вызвал этот таб
        /// </summary>
        public string ParentFrame { get; set; }

        /// <summary>
        /// Идентификатор станка
        /// stanok.id_st
        /// </summary>
        public int MachineId { get; set; }

        public int FactoryId = 1;

        /// <summary>
        /// процессор форм
        /// </summary>
        private FormHelper Form { get; set; }

        private ListDataSet SubProductGridDataSet { get; set; }

        private ListDataSet OrderGridDataSet { get; set; }

        private ListDataSet PalletGridDataSet { get; set; }

        private ListDataSet ProductionTaskGridDataSet { get; set; }

        /// <summary>
        /// отображение фрейма
        /// </summary>
        public void Show()
        {
            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 2;

            FrameName = $"{FrameName}";
            Dictionary<string, string> windowParametrs = new Dictionary<string, string>();
            windowParametrs.Add("no_resize", "1");
            windowParametrs.Add("center_screen", "1");
            this.MinHeight = 650;
            this.MinWidth = 1550;
            Central.WM.Show(FrameName, this.ControlTitle, true, "main", this, "top", windowParametrs);
        }

        /// <summary>
        /// инициализация компонентов формы
        /// </summary>
        private void FormInit()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="SUBPRODUCT_SEARCH",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=SubProductSearchText,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
                new FormHelperField()
                {
                    Path="PRODUCTION_TASK_SEARCH",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductionTaskSearchText,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
                new FormHelperField()
                {
                    Path="ORDER_SEARCH",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=OrderSearchText,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
                new FormHelperField()
                {
                    Path="MACHINE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=MachineForOrderSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
                new FormHelperField()
                {
                    Path="PALLET_SEARCH",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=PalletSearchText,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
                new FormHelperField()
                {
                    Path="MACHINE_PALLET",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=MachineForPalletSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
            };
            Form.SetFields(fields);
        }

        private void SetDefaults()
        {
            SubProductGridDataSet = new ListDataSet();
            PalletGridDataSet = new ListDataSet();
            ProductionTaskGridDataSet = new ListDataSet();
            OrderGridDataSet = new ListDataSet();

            FormHelper.ComboBoxInitHelper(MachineForOrderSelectBox, "Production/Pillory", "Machine", "ListSimple", "MACHINE_ID", "MACHINE_NAME2", new Dictionary<string, string>() { { "FACTORY_ID", $"{FactoryId}"} }, true);
            FormHelper.ComboBoxInitHelper(MachineForPalletSelectBox, "Production/Pillory", "Machine", "ListSimple", "MACHINE_ID", "MACHINE_NAME2", new Dictionary<string, string>() { { "FACTORY_ID", $"{FactoryId}" } }, true);
            if (MachineId > 0)
            {
                MachineForOrderSelectBox.SetSelectedItemByKey($"{MachineId}");
                MachineForOrderSelectBox.IsReadOnly = true;

                MachineForPalletSelectBox.SetSelectedItemByKey($"{MachineId}");
                MachineForPalletSelectBox.IsReadOnly = true;
            }
        }

        private void SubProductGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид продукции",
                        Description = "Идентификатор продукции",
                        Path="PRODUCT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Description = "Наименование продукции",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=35,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // Если эту продукцию ещё не производили - нет последнего ПЗ
                                    if (string.IsNullOrEmpty(row.CheckGet("LAST_PRODUCTION_TASK_ID")))
                                    {
                                        color = HColor.Blue;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="По умолчанию",
                        Description = "Количество по умолчанию, шт.",
                        Path="DEFAULT_QUANTITY",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=6,
                        Format="N0",
                    },
                    new DataGridHelperColumn
                    {
                        Header="В буфере",
                        Description = "Количество продукции на остатке, шт.",
                        Path="QUANTITY_IN_BUFFER",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=6,
                        Format="N0",
                    },
                    new DataGridHelperColumn
                    {
                        Header="В заданиях",
                        Description = "Количество продукции по невыполненным заданиям, шт.",
                        Path="QUANTITY_IN_TASK",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=6,
                        Format="N0",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Остаток",
                        Description = "Суммарное количество продукции в буфере и невыполненных заданиях, шт.",
                        Path="CURRENT_BALANCE",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=6,
                        Format="N0",
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // Если продукции достаточно - количсетво на остатке больше или равно максимальному количеству
                                    if (row.CheckGet("MAX_BALANCE").ToInt() > 0
                                        && row.CheckGet("CURRENT_BALANCE").ToInt() >= row.CheckGet("MAX_BALANCE").ToInt())
                                    {
                                        color = HColor.Yellow;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Максимальный остаток",
                        Description = "Максимальный допустимый остаток, шт.",
                        Path="MAX_BALANCE",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=6,
                        Format="N0"
                    },
                    new DataGridHelperColumn
                    {
                        Header="Заказов",
                        Description = "Заказов на перемещение на станки",
                        Path="COUNT_TASK_TO_MOVE",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=5,
                        Format="N0"
                    },
                    new DataGridHelperColumn
                    {
                        Header="Заказы",
                        Description = "Список станков, на которых присутствуют невыполненные заказы",
                        Path="MACHINE_NAME_LIST",
                        ColumnType=ColumnTypeRef.String,
                        Width2=35,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Ид категории продукции",
                        Description = "Идентификатор категории продукции",
                        Path="PRODUCT_CATEGORY_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=4,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид последнего ПЗ",
                        Description = "Идентификатор последнего производственного задания на эту продукцию",
                        Path="LAST_PRODUCTION_TASK_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=4,
                        Hidden=true,
                    },
                };
                SubProductGrid.SetColumns(columns);
                SubProductGrid.SetPrimaryKey("PRODUCT_ID");
                SubProductGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                SubProductGrid.SearchText = SubProductSearchText;
                SubProductGrid.Toolbar = SubProductStackPanel;
                SubProductGrid.AutoUpdateInterval = 0;
                SubProductGrid.OnLoadItems = SubProductGridLoadItems;
                SubProductGrid.OnSelectItem += (Dictionary<string, string> selectedItem) =>
                {
                    PalletGrid.LoadItems();
                    ProductionTaskGrid.LoadItems();
                };
                SubProductGrid.UseProgressSplashAuto = false;
                SubProductGrid.Commands = this.Commander;
                SubProductGrid.Init();
            }
        }

        private async void SubProductGridLoadItems()
        {
            var p = new Dictionary<string, string>();
            p.Add("FACTORY_ID", $"{FactoryId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production/Pillory");
            q.Request.SetParam("Object", "SubProduct");
            q.Request.SetParam("Action", "List");
            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            SubProductGridDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    SubProductGridDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            else
            {
                q.ProcessError();
            }
            SubProductGrid.UpdateItems(SubProductGridDataSet);
        }

        private void PalletGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header = "*",
                        Path = "_SELECTED",
                        ColumnType = ColumnTypeRef.Boolean,
                        Width2=3,
                        Editable = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид поддона",
                        Description = "Идентификатор поддона",
                        Path="PALLET_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Поддон",
                        Description = "Полный номер поддона",
                        Path="PALLET_NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество, шт.",
                        Description = "Количество продукции на поддоне, штук",
                        Path="QUANTITY",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=6,
                        Format="N0",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ячейка",
                        Description = "Местоположение поддона",
                        Path="PLACE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Продукция",
                        Description = "Наименование продукции",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=35,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид прихода",
                        Description = "Идентификатор прихода",
                        Path="INCOMING_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид продукции",
                        Description = "Идентификатор продукции",
                        Path="PRODUCT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид категории продукции",
                        Description = "Идентификатор категории продукции",
                        Path="PRODUCT_CATEGORY_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                        Hidden=true,
                    },
                };
                PalletGrid.SetColumns(columns);
                PalletGrid.SetPrimaryKey("PALLET_ID");
                PalletGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                PalletGrid.SearchText = PalletSearchText;
                PalletGrid.Toolbar = PalletStackPanel;
                PalletGrid.AutoUpdateInterval = 0;
                PalletGrid.OnLoadItems = PalletGridLoadItems;
                PalletGrid.UseProgressSplashAuto = false;
                PalletGrid.Commands = this.Commander;
                PalletGrid.Init();
            }
        }

        private async void PalletGridLoadItems()
        {
            string productId = SubProductGrid.SelectedItem?.CheckGet("PRODUCT_ID");
            if (!string.IsNullOrEmpty(productId))
            {
                var p = new Dictionary<string, string>();
                p.Add("FACTORY_ID", $"{FactoryId}");
                p.Add("PRODUCT_ID", productId);

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production/Pillory");
                q.Request.SetParam("Object", "SubProduct");
                q.Request.SetParam("Action", "GetBalance");
                q.Request.SetParams(p);
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                PalletGridDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        PalletGridDataSet = ListDataSet.Create(result, "ITEMS");
                    }
                }
                else
                {
                    q.ProcessError();
                }
                PalletGrid.UpdateItems(PalletGridDataSet);
            }
        }

        private void ProductionTaskGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид ПЗ",
                        Description = "Идентификатор производственного задания",
                        Path="PRODUCTION_TASK_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Задание",
                        Description = "Номер производственного задания",
                        Path="PRODUCTION_TASK_NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        Width2=9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Description = "Количество продукции в производственном задании, шт.",
                        Path="QUANTITY",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=6,
                        Format="N0",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата начала",
                        Description = "Дата начала производственного задания",
                        Path="START_DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2=14,
                        Format="dd.MM.yyyy HH:mm:ss",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата окончания",
                        Description = "Дата окончания производственного задания",
                        Path="END_DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2=14,
                        Format="dd.MM.yyyy HH:mm:ss",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Длительность",
                        Description = "Длительность производственного задания, мин.",
                        Path="DURATION_IN_MINUTE",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=6,
                        Format="N2",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата создания",
                        Description = "Дата создания производственного задания",
                        Path="CREATED_DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2=14,
                        Format="dd.MM.yyyy HH:mm:ss",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Description = "Наименование продукци",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=35,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид продукции",
                        Description = "Идентификатор продукции",
                        Path="PRODUCT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                };
                ProductionTaskGrid.SetColumns(columns);
                ProductionTaskGrid.SetPrimaryKey("PRODUCTION_TASK_ID");
                ProductionTaskGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                ProductionTaskGrid.SearchText = ProductionTaskSearchText;
                ProductionTaskGrid.Toolbar = ProductionTaskStackPanel;
                ProductionTaskGrid.AutoUpdateInterval = 0;
                ProductionTaskGrid.OnLoadItems = ProductionTaskGridLoadItems;
                ProductionTaskGrid.UseProgressSplashAuto = false;
                ProductionTaskGrid.Commands = this.Commander;
                ProductionTaskGrid.Init();
            }
        }

        private async void ProductionTaskGridLoadItems()
        {
            string productId = SubProductGrid.SelectedItem?.CheckGet("PRODUCT_ID");
            if (!string.IsNullOrEmpty(productId))
            {
                var p = new Dictionary<string, string>();
                p.Add("FACTORY_ID", $"{FactoryId}");
                p.Add("PRODUCT_ID", productId);

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production/Pillory");
                q.Request.SetParam("Object", "SubProduct");
                q.Request.SetParam("Action", "GetProductionTask");
                q.Request.SetParams(p);
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                ProductionTaskGridDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        ProductionTaskGridDataSet = ListDataSet.Create(result, "ITEMS");
                    }
                }
                else
                {
                    q.ProcessError();
                }
                ProductionTaskGrid.UpdateItems(ProductionTaskGridDataSet);
            }
        }

        private void OrderGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид заказа",
                        Description = "Идентификатор заказа",
                        Path="ORDER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата заказа",
                        Description = "Дата заказа",
                        Path="ORDER_DT",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2=8,
                        Format="dd.MM.yyyy",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Description = "Наименование продукции",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=35,
                    },
                    new DataGridHelperColumn
                    {
                        Header="По умолчанию",
                        Description = "Количество по умолчанию, шт.",
                        Path="DEFAULT_QUANTITY",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=6,
                        Format="N0",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Станок",
                        Description = "Наименование станка",
                        Path="MACHINE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=8,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Ид станка",
                        Description = "Идентификатор станка",
                        Path="MACHINE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид продукции",
                        Description = "Идентификатор продукции",
                        Path="PRODUCT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид категории продукции",
                        Description = "Идентификатор категории продукции",
                        Path="PRODUCT_CATEGORY_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                        Hidden=true,
                    },
                };
                OrderGrid.SetColumns(columns);
                OrderGrid.SetPrimaryKey("ORDER_ID");
                OrderGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                OrderGrid.SearchText = OrderSearchText;
                OrderGrid.Toolbar = OrderStackPanel;
                OrderGrid.AutoUpdateInterval = 0;
                OrderGrid.OnLoadItems = OrderGridLoadItems;
                OrderGrid.UseProgressSplashAuto = false;
                OrderGrid.Commands = this.Commander;
                OrderGrid.Init();
            }
        }

        private async void OrderGridLoadItems()
        {
            string machineId = Form.GetValueByPath("MACHINE");
            if (!string.IsNullOrEmpty(machineId))
            {
                var p = new Dictionary<string, string>();
                p.Add("MACHINE_ID", machineId);

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production/Pillory");
                q.Request.SetParam("Object", "SubProduct");
                q.Request.SetParam("Action", "SubProductListOrderByMachine");
                q.Request.SetParams(p);
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                OrderGridDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        OrderGridDataSet = ListDataSet.Create(result, "ITEMS");
                    }
                }
                else
                {
                    q.ProcessError();
                }
                OrderGrid.UpdateItems(OrderGridDataSet);
            }
        }

        private async void CreateOrder()
        {
            if (SubProductGrid != null && SubProductGrid.SelectedItem != null && SubProductGrid.SelectedItem.Count > 0)
            {
                string productId = SubProductGrid.SelectedItem.CheckGet("PRODUCT_ID");
                string productCategoryId = SubProductGrid.SelectedItem.CheckGet("PRODUCT_CATEGORY_ID");
                string machineId = Form.GetValueByPath("MACHINE");
                
                if (!string.IsNullOrEmpty(productId)
                    && !string.IsNullOrEmpty(productCategoryId)
                    && !string.IsNullOrEmpty(machineId))
                {
                    var d0 = new DialogWindow($"Создать заказ {SubProductGrid.SelectedItem.CheckGet("PRODUCT_NAME")} для {MachineForOrderSelectBox.Items[machineId]}?", this.ControlTitle, "", DialogWindowButtons.YesNo);
                    if (d0.ShowDialog() == true)
                    {
                        var p = new Dictionary<string, string>();
                        p.Add("CORRUGATOR_MACHINE_ID", machineId);
                        p.Add("PRODUCT_ID", productId);
                        p.Add("PRODUCT_CATEGORY_ID", productCategoryId);

                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Production");
                        q.Request.SetParam("Object", "ManuallyPrint");
                        q.Request.SetParam("Action", "SaveTovarSubOrder");
                        q.Request.SetParams(p);
                        q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                        q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                        await Task.Run(() =>
                        {
                            q.DoQuery();
                        });

                        if (q.Answer.Status == 0)
                        {
                            bool succesfullFlag = false;

                            var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                            if (result != null)
                            {
                                var ds = ListDataSet.Create(result, "ITEMS");
                                if (ds != null && ds.Items != null && ds.Items.Count > 0)
                                {
                                    if (!string.IsNullOrEmpty(ds.Items[0].CheckGet("PRODUCT_ID")))
                                    {
                                        succesfullFlag = true;
                                    }
                                }
                            }

                            if (succesfullFlag)
                            {
                                OrderGrid.LoadItems();

                                var d = new DialogWindow("Успешное создание заказа.", this.ControlTitle, "", DialogWindowButtons.OK);
                                d.ShowDialog();
                            }
                            else
                            {
                                var d = new DialogWindow("При заказе произошла ошибка. Пожалуйста, сообщите о проблеме.", this.ControlTitle, "", DialogWindowButtons.OK);
                                d.ShowDialog();
                            }
                        }
                        else
                        {
                            q.ProcessError();
                        }
                    }
                }
                else
                {
                    var d = new DialogWindow("Не найдены данные выбранной продукции. Пожалуйста, сообщите о проблеме.", this.ControlTitle, "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                var d = new DialogWindow("Не выбрана продукция для заказа", this.ControlTitle, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        private async void CreateTask()
        {
            if (SubProductGrid != null && SubProductGrid.SelectedItem != null && SubProductGrid.SelectedItem.Count > 0)
            {
                if (SubProductGrid.SelectedItem.CheckGet("MAX_BALANCE").ToInt() == 0
                    || SubProductGrid.SelectedItem.CheckGet("CURRENT_BALANCE").ToInt() < SubProductGrid.SelectedItem.CheckGet("MAX_BALANCE").ToInt())
                {
                    string productionTaskId = SubProductGrid.SelectedItem.CheckGet("LAST_PRODUCTION_TASK_ID");
                    if (!string.IsNullOrEmpty(productionTaskId))
                    {
                        var d0 = new DialogWindow($"Создать производственное задание для {SubProductGrid.SelectedItem.CheckGet("PRODUCT_NAME")}?", this.ControlTitle, "", DialogWindowButtons.YesNo);
                        if (d0.ShowDialog() == true)
                        {
                            var p = new Dictionary<string, string>();
                            p.Add("PRODUCTION_TASK_ID", productionTaskId);

                            var q = new LPackClientQuery();
                            q.Request.SetParam("Module", "Production/Pillory");
                            q.Request.SetParam("Object", "SubProduct");
                            q.Request.SetParam("Action", "CreateProductionTask");
                            q.Request.SetParams(p);
                            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                            await Task.Run(() =>
                            {
                                q.DoQuery();
                            });

                            if (q.Answer.Status == 0)
                            {
                                bool succesfullFlag = false;

                                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                                if (result != null)
                                {
                                    var ds = ListDataSet.Create(result, "ITEMS");
                                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                                    {
                                        if (!string.IsNullOrEmpty(ds.Items[0].CheckGet("OUTPUT_PRODUCTION_TASK_ID")))
                                        {
                                            succesfullFlag = true;
                                        }
                                    }
                                }

                                if (succesfullFlag)
                                {
                                    Refresh();

                                    var d = new DialogWindow("Успешное создание производственного задания.", this.ControlTitle, "", DialogWindowButtons.OK);
                                    d.ShowDialog();
                                }
                                else
                                {
                                    var d = new DialogWindow("При создании производственного задания произошла ошибка. Пожалуйста, сообщите о проблеме.", this.ControlTitle, "", DialogWindowButtons.OK);
                                    d.ShowDialog();
                                }
                            }
                            else
                            {
                                q.ProcessError();
                            }
                        }
                    }
                    else
                    {
                        var d = new DialogWindow("Не найдены данные по предыдущему производственному заданию. Пожалуйста, сообщите о проблеме.", this.ControlTitle, "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    var d = new DialogWindow("Достаточно продукции. Запрещено производить больше продукции.", this.ControlTitle, "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                var d = new DialogWindow("Не выбрана продукция для созания задания", this.ControlTitle, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        private void ConsumptionPallet()
        {
            string machineId = Form.GetValueByPath("MACHINE_PALLET");
            if (!string.IsNullOrEmpty(machineId))
            {
                var checkedRowList = PalletGrid.GetItemsSelected();
                if (checkedRowList != null && checkedRowList.Count > 1)
                {
                    if (new DialogWindow($"Хотите списать {checkedRowList.Count} поддонов на станке {MachineForPalletSelectBox.Items[machineId]}?", this.ControlTitle, "", DialogWindowButtons.YesNo).ShowDialog() == true)
                    {
                        bool errorFlag = false;

                        foreach (var checkedRow in checkedRowList)
                        {
                            if (!ConsumptionPalletOne(checkedRow.CheckGet("INCOMING_ID"), machineId))
                            {
                                errorFlag = true;
                            }
                        }

                        if (errorFlag)
                        {
                            DialogWindow.ShowDialog($"При выполнении списания произошла ошибка. Пожалуйста, сообщите о проблеме.");
                        }
                        else
                        {
                            DialogWindow.ShowDialog($"Успешное списание перестила.");
                        }

                        Refresh();
                    }
                }
                else
                {
                    if (checkedRowList != null && checkedRowList.Count == 1)
                    {
                        PalletGrid.SelectRowByKey(checkedRowList[0].CheckGet("PALLET_ID"));
                    }

                    if (new DialogWindow($"Хотите списать поддон {PalletGrid.SelectedItem.CheckGet("PALLET_NUMBER")} на станке {MachineForPalletSelectBox.Items[machineId]}?", this.ControlTitle, "", DialogWindowButtons.YesNo).ShowDialog() == true)
                    {
                        if (!ConsumptionPalletOne(PalletGrid.SelectedItem.CheckGet("INCOMING_ID"), machineId))
                        {
                            DialogWindow.ShowDialog($"При выполнении списания произошла ошибка. Пожалуйста, сообщите о проблеме.");
                        }
                        else
                        {
                            DialogWindow.ShowDialog($"Успешное списание перестила.");
                        }

                        Refresh();
                    }
                }
            }
            else
            {
                var d = new DialogWindow("Не выбран станок списания", this.ControlTitle, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        private bool ConsumptionPalletOne(string incomingId, string machineId)
        {
            bool consumptionResult = false;

            var p = new Dictionary<string, string>();
            p.Add("INCOMING_ID", incomingId);
            p.Add("MACHINE_ID", machineId);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production/Pillory");
            q.Request.SetParam("Object", "SubProduct");
            q.Request.SetParam("Action", "ConsumptionCreate");
            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var dataSet = ListDataSet.Create(result, "ITEMS");
                    if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                    {
                        if (dataSet.Items[0].CheckGet("INCOMING_ID").ToInt() > 0)
                        {
                            consumptionResult = true;
                        }
                    }
                }
            }
            else
            {
                q.SilentErrorProcess = true;
                q.ProcessError();
            }

            return consumptionResult;
        }

        private void Refresh()
        {
            SubProductGrid.LoadItems();
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        private void Close()
        {
            if (!string.IsNullOrEmpty(ParentFrame))
            {
                Central.WM.SetActive(ParentFrame, true);
            }

            Central.WM.Close(FrameName);

            //вся работа по утилизации ресурсов происходит в Destroy
            //он будет вызван при закрытии фрейма
        }

        private void MachineForOrderSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            OrderGrid.LoadItems();
        }

        private void MachineForPalletSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Commander.UpdateActions();
        }
    }
}
