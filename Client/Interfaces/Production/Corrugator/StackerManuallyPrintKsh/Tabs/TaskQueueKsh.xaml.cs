using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
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
using System.Windows.Threading;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Список заданий на гофроагрегатах.
    /// (В плане и уже выполненых)
    /// </summary>
    /// <author>sviridov_ae</author>
    public partial class TaskQueueKsh : UserControl
    {
        public TaskQueueKsh()
        {
            InitializeComponent();

            LogTableName = "corrugator_label";
            Loaded += (object sender, RoutedEventArgs e) =>
            {
                FrameName = Central.WM.TabItems.FirstOrDefault(x => x.Value.Content == this).Key;
                Central.WM.SetActive(FrameName);
                Central.WM.SelectedTab = FrameName;
            };

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            ProcessPermissions();
            FormInit();
            SetDefaults();
            GridInit();
            PositionGridInit();
        }

        public static int FactoryId = 2;

        public static string ParentContainer = "manually_print_ksh";

        /// <summary>
        /// Имя папки верхнего уровня, в которой хранятся лог файлы по работе стекера
        /// </summary>
        public string LogTableName { get; set; }

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Основной датасет с данными по производственным заданиям
        /// </summary>
        public ListDataSet GridDataSet { get; set; }

        /// <summary>
        /// Основной датасет с данными по позициям производственного задания
        /// </summary>
        public ListDataSet PositionGridDataSet { get; set; }

        /// <summary>
        /// Выбранная запись в гриде
        /// </summary>
        public Dictionary<string, string> GridSelectedItem { get; set; }

        /// <summary>
        /// Выбранная запись в гриде позиций задания
        /// </summary>
        public Dictionary<string, string> PositionGridSelectedItem { get; set; }

        /// <summary>
        /// Идентификатор станка.
        /// 2 -- "ГА1";
        /// 21 -- "ГА2";
        /// 22 -- "ГА3";
        /// 23 -- "ГА4".
        /// </summary>
        public int MachineId { get; set; }

        /// <summary>
        /// Таймер получения данных по текущему производственному заданию
        /// </summary>
        public DispatcherTimer CurrentProductionTaskTimer { get; set; }

        /// <summary>
        /// интервал обновления таймера (сек)
        /// </summary>
        public int CurrentProductionTaskTimerAutoUpdateInterval { get; set; }

        public string RoleName = "[erp]corrugator_stacker_ksh";

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel(this.RoleName);
            var userAccessMode = mode;
            switch (mode)
            {
                case Role.AccessMode.Special:
                    break;

                case Role.AccessMode.FullAccess:
                    break;

                case Role.AccessMode.ReadOnly:
                default:
                    break;
            }

            List<Button> buttons = UIUtil.GetVisualChilds<Button>(this.Content as DependencyObject);
            if (buttons != null && buttons.Count > 0)
            {
                foreach (var button in buttons)
                {
                    var buttonTagList = UIUtil.GetTagList(button);
                    var accessMode = Acl.FindTagAccessMode(buttonTagList);
                    if (accessMode > userAccessMode)
                    {
                        button.IsEnabled = false;
                    }
                }
            }

            if (Grid != null && Grid.Menu != null && Grid.Menu.Count > 0)
            {
                foreach (var manuItem in Grid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }

            if (PositionGrid != null && PositionGrid.Menu != null && PositionGrid.Menu.Count > 0)
            {
                foreach (var manuItem in PositionGrid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }
        }

        public void FormInit()
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
                            Path = "FROM_DATE",
                            FieldType = FormHelperField.FieldTypeRef.String,
                            Control = FromDate,
                            ControlType = "TextBox",
                            Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                            },
                        },
                        new FormHelperField()
                        {
                            Path = "CORRUGATOR_MACHINE",
                            FieldType = FormHelperField.FieldTypeRef.String,
                            Control = CorrugatorMachineSelectBox,
                            ControlType = "SelectBox",
                            Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                            },
                        },
                    };

                Form.SetFields(fields);
            }
        }

        /// <summary>
        /// инициализация грида производственных заданий
        /// </summary>
        public void GridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Номер задания",
                        Path="PRODUCTION_TASK_NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=50,
                        MaxWidth=55,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // Текущее производственное задание
                                    if( row.CheckGet("CURRENT_PRODUCTION_TASK").ToInt() == 1 )
                                    {
                                        color = HColor.Gray;
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
                        Header="Дата создания",
                        Path="CREATED_DTTM",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=63,
                        MaxWidth=110,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Дата начала{Environment.NewLine}{Environment.NewLine}Дата и время начала выполнения производственного задания",
                        Path="START_DTTM",                        
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=63,
                        MaxWidth=110,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Дата окончания{Environment.NewLine}{Environment.NewLine}Дата и время окончания выполнения производственного задания",
                        Path="END_DTTM",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=63,
                        MaxWidth=110,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Станок{Environment.NewLine}{Environment.NewLine}Гофроагрерат, на котором будет выполняться производственное задание",
                        Path="CORRUGATOR_MACHINE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=55,
                        MaxWidth=55,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Стекеры{Environment.NewLine}{Environment.NewLine}1 -- Нижний стекер{Environment.NewLine}2 -- Верхний стекер",
                        Path="CUTOFF_ALLOCATION",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=55,
                        MaxWidth=55,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // Если продукция по одной из заявок этого ПЗ это бесконечный картон
                                    if(row.CheckGet("FANFOLD_FLAG").ToInt() == 1)
                                    {
                                        color = HColor.Violet;
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
                        Header=$"Хранение{Environment.NewLine}{Environment.NewLine}Станок, куда отправится заготовка после выхода",
                        Path="NEXT_MACHINE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=55,
                        MaxWidth=115,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // Если по выбранному ПЗ есть заявка, в которой Готовая продукция
                                    if(row.CheckGet("PRODUCT_CATEGORY").ToInt() > 4)
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
                        Header=$"Длина{Environment.NewLine}{Environment.NewLine}Длина всего производственного задания",
                        Path="LENGTH",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=55,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Количество{Environment.NewLine}{Environment.NewLine}Общее количество картона по производственному заданию",
                        Path="QUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=52,
                        MaxWidth=82,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Формат",
                        Path="FORMAT",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=48,
                        MaxWidth=60,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Профиль{Environment.NewLine}{Environment.NewLine}Профиль картона",
                        Path="PROFILE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=35,
                        MaxWidth=68,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Сырьё{Environment.NewLine}{Environment.NewLine}Описание используемого картона",
                        Path="CARDBOARD_DESCRIPTION",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=45,
                        MaxWidth=168,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Вал 1{Environment.NewLine}{Environment.NewLine}Вал для производства гофрослоя на прессе 1",
                        Path="NAME_ROLL_1",
                        Doc="Вал для производства гофрослоя на прессе 1",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=45,
                        MaxWidth=45,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Вал 2{Environment.NewLine}{Environment.NewLine}Вал для производства гофрослоя на прессе 2",
                        Path="NAME_ROLL_2",
                        Doc="Вал для производства гофрослоя на прессе 2",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=45,
                        MaxWidth=46,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Проверка{Environment.NewLine}{Environment.NewLine}Первое значение -- сколько выпустил ГА{Environment.NewLine}Второе значение -- сколько создано на ярлыках",
                        Path="CHECK_SCANED_QUANTITY",
                        Description="Первое значение-сколько выпустил ГА. Второе значение-сколько создано на ярлыках.",
                        Doc="Первое значение-сколько выпустил ГА. Второе значение-сколько создано на ярлыках.",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=35,
                        MaxWidth=80,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // разница между количеством по заданию и созданным количеством
                                    if(!string.IsNullOrEmpty(row.CheckGet("CHECK_SCANED_QUANTITY")))
                                    {
                                        color = HColor.Red;
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
                        Header=$"Спецподдон{Environment.NewLine}{Environment.NewLine}По одной из заявок производственного задания будет использоваться спецподдон",
                        Path="NON_STANDART_PALLET_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=60,
                        MaxWidth=60,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Заливная печать{Environment.NewLine}{Environment.NewLine}По одной из заявок производственного задания указана заливная печать",
                        Path="FILL_PRINTING_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=60,
                        MaxWidth=60,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Меняли укладку{Environment.NewLine}{Environment.NewLine}По одной из заявок производственного задания вручную меняли укладку на поддон",
                        Path="EDIT_STACKING_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=60,
                        MaxWidth=60,
                        Editable=false,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // Если по выбранному ПЗ есть заявка, в которой вручную меняли укладку
                                    if(row.CheckGet("EDIT_STACKING_FLAG").ToInt() == 1)
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
                        Header=$"В очереди{Environment.NewLine}{Environment.NewLine}Задание находится в очереди гофроагрегата",
                        Path="IN_QUEUE",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=60,
                        MaxWidth=60,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Создатель{Environment.NewLine}{Environment.NewLine}Создатель производственного задания",
                        Path="CREATOR",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=75,
                        MaxWidth=100,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Ид{Environment.NewLine}{Environment.NewLine}Идентификатор производственного задания",
                        Path="PRODUCTION_TASK_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид станка",
                        Path="CORRUGATOR_MACHINE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=35,
                        MaxWidth=75,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид профиля",
                        Path="PROFILE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=35,
                        MaxWidth=75,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Текущее ПЗ",
                        Path="CURRENT_PRODUCTION_TASK",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=35,
                        MaxWidth=75,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Размеры продукции",
                        Path="PRODUCT_SIZE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=0,
                        MaxWidth=0,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид площадки",
                        Path="FACTORY_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=0,
                        MaxWidth=0,
                        Hidden=true,
                    },

                    new DataGridHelperColumn
                    {
                        Header=" ",
                        Path="_",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=2,
                        MaxWidth=2000,
                    },
                };
                Grid.SetColumns(columns);
                Grid.PrimaryKey = "PRODUCTION_TASK_ID";
                Grid.UseSorting = false;
                Grid.SearchText = SearchText;
                Grid.OnLoadItems = GridLoadItems;

                Grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    {
                        StylerTypeRef.BackgroundColor,
                        (Dictionary<string, string> row) =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            // зелёный -- задание в очереди на ГА
                            if (row.CheckGet("IN_QUEUE").ToInt() == 1)
                            {
                                color = HColor.Green;
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },
                };
                                
                //при выборе строки в гриде, обновляются актуальные действия для записи
                Grid.OnSelectItem = selectedItem =>
                {
                    GridSelectedItem = selectedItem;
                    PositionGridLoadItems();
                };

                Grid.OnDblClick = selectedItem =>
                {
                    ManuallyPrint();
                };

                Grid.OnFilterItems = () =>
                {
                    if (Grid.GridItems != null)
                    {
                        if (Grid.GridItems.Count > 0)
                        {
                            if (CorrugatorMachineSelectBox.SelectedItem.Key != null)
                            {
                                var corrugatorMachineId = CorrugatorMachineSelectBox.SelectedItem.Key.ToInt();
                                var items = new List<Dictionary<string, string>>();

                                if (corrugatorMachineId == -1)
                                {
                                    items = Grid.GridItems;
                                }
                                else
                                {
                                    items.AddRange(Grid.GridItems.Where(row => row.CheckGet("CORRUGATOR_MACHINE_ID").ToInt() == corrugatorMachineId));
                                }

                                Grid.GridItems = items;
                            }
                        }
                    }
                };

                // контекстное меню
                Grid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    {
                        "ManuallyPrint",
                        new DataGridContextMenuItem()
                        {
                            Header="Ручная печать",
                            Action=()=>
                            {
                                ManuallyPrint();
                            }
                        }
                    },
                    {
                        "OrderPallet",
                        new DataGridContextMenuItem()
                        {
                            Header="Заказать пустые поддоны",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                OrderPallet();
                            }
                        }
                    },
                    {
                        "OrderSubProduct",
                        new DataGridContextMenuItem()
                        {
                            Header="Заказать перестил",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                OrderSubProduct();
                            }
                        }
                    },
                    {
                        "ManuallyReject",
                        new DataGridContextMenuItem()
                        {
                            Header="Ручная отбраковка",
                            Action=()=>
                            {
                                ManuallyReject();
                            }
                        }
                    },
                };

                Grid.Init();
                Grid.Run();
            }
        }

        public void ClearAllValues()
        {
            if (Grid != null && Grid.Items != null)
            {
                Grid.Items = new List<Dictionary<string, string>>();
            }

            if (PositionGrid != null && PositionGrid.Items != null)
            {
                PositionGrid.Items = new List<Dictionary<string, string>>();
            }

            GridDataSet = new ListDataSet();
            GridSelectedItem = new Dictionary<string, string>();
            PositionGridDataSet = new ListDataSet();
            PositionGridSelectedItem = new Dictionary<string, string>();
        }

        /// <summary>
        /// Получение данных по производственным заданиям
        /// </summary>
        public async void GridLoadItems()
        {
            DisableControls();
            SplashControl.Visible = true;

            ClearAllValues();

            var p = new Dictionary<string, string>();
            p.Add("DATE_FROM", Form.GetValueByPath("FROM_DATE"));
            p.Add("FACTORY_ID", $"{FactoryId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "ManuallyPrint");
            q.Request.SetParam("Action", "ListProductionTask");
            q.Request.SetParams(p);

            q.Request.Timeout = 20000;
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
                    GridDataSet = ListDataSet.Create(result, "ITEMS");
                    Grid.UpdateItems(GridDataSet);
                }
            }

            SplashControl.Visible = false;
            EnableControls();

            GetCurrentProductionTask();
        }

        /// <summary>
        /// Инициализация грида позиций производственного задания
        /// </summary>
        public void PositionGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header=$"Продукция{Environment.NewLine}{Environment.NewLine}Наименование продукции по заявке",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=80,
                        MaxWidth=240,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Артикул{Environment.NewLine}{Environment.NewLine}Артикул продукции по заявке",
                        Path="PRODUCT_CODE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=120,
                        MaxWidth=120,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Категория{Environment.NewLine}{Environment.NewLine}Заготовка/Продукция",
                        Path="PRODUCT_CATEGORY_NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=35,
                        MaxWidth=72,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                        // Готовая продукция (Листы)
                                        if( row.CheckGet("PRODUCT_CATEGORY_ID").ToInt() == 5 )
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
                        Header=$"По заявке{Environment.NewLine}{Environment.NewLine}Заказанное по заявке количество",
                        Path="QUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=42,
                        MaxWidth=75,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Стандартная укладка{Environment.NewLine}{Environment.NewLine}Сколько изначально было в стопе",
                        Path="STACKING",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=60,
                        MaxWidth=80,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Укладка по факту{Environment.NewLine}{Environment.NewLine}Укладка, которая будет отражена на ярлыках",
                        Path="STACKING_ON_PALLET",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=60,
                        MaxWidth=80,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                        // Если укладка на поддон менялась вручную
                                        if(row.CheckGet("EDIT_STACKING_FLAG").ToInt() == 1)
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
                        Header=$"На поддоне{Environment.NewLine}{Environment.NewLine}Сколько штук укладывать на поддон",
                        Path="QUANTITY_ON_PALLET",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=42,
                        MaxWidth=82,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Поддон{Environment.NewLine}{Environment.NewLine}Габариты поддона",
                        Path="PALLET_NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=60,
                        MaxWidth=80,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                        // Если не стандартный поддон
                                        if( row.CheckGet("PALLET_STANDART_FLAG").ToInt() == 0 )
                                        {
                                            color = HColor.Green;
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
                        Header="Потоков",
                        Path="NUMBER_OF_OUTS",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=30,
                        MaxWidth=63,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Стекер{Environment.NewLine}{Environment.NewLine}1 -- Нижний стекер{Environment.NewLine}2 -- Верхний стекер",
                        Path="STACKER",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=30,
                        MaxWidth=55,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // Если продукция по заявке это бесконечный картон
                                    if(row.CheckGet("FANFOLD_FLAG").ToInt() == 1)
                                    {
                                        color = HColor.Violet;
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
                        Header=$"Сканер{Environment.NewLine}{Environment.NewLine}Сколько штук на отсканированных ярлыках",
                        Path="QUANTITY_SCANNED",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=30,
                        MaxWidth=60,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // Если заблокирована печать последнего ярлыка
                                    if(row.CheckGet("BLOCKED_LAST_LABEL_PRINT_FLAG").ToInt() > 0)
                                    {
                                        color = HColor.Red;
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
                        Header=$"Блокировка{Environment.NewLine}{Environment.NewLine}Заблокирована печать последнего ярлыка по заданию",
                        Path="BLOCKED_LAST_LABEL_PRINT_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=40,
                        MaxWidth=60,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Станок{Environment.NewLine}{Environment.NewLine}Станок, куда отправится заготовка после выхода",
                        Path="NEXT_MACHINE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=60,
                        MaxWidth=60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Примечание",
                        Path="NOTE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=45,
                        MaxWidth=168,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Мастер{Environment.NewLine}{Environment.NewLine}Для новых изделий -- без мастера не делать",
                        Path="MASTER_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=40,
                        MaxWidth=60,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Образец{Environment.NewLine}{Environment.NewLine}Признак опытной партии",
                        Path="SAMPLE_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=40,
                        MaxWidth=60,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Лаборатория{Environment.NewLine}{Environment.NewLine}Отправить образцы на тестирование",
                        Path="TESTING_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=40,
                        MaxWidth=60,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Сырьё{Environment.NewLine}{Environment.NewLine}Описание картона",
                        Path="CARDBOARD_DESCRIPTION",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=45,
                        MaxWidth=168,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Качество",
                        Path="QUALITY",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=50,
                        MaxWidth=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Рилёвки{Environment.NewLine}{Environment.NewLine}Нет/Симметр#/#,#",
                        Path="SCORING",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=32,
                        MaxWidth=83,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Ид продукции{Environment.NewLine}{Environment.NewLine}Идентификатор продукции",
                        Path="PRODUCT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=32,
                        MaxWidth=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид категории продукции",
                        Path="PRODUCT_CATEGORY_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=32,
                        MaxWidth=60,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид ПЗ",
                        Path="PRODUCTION_TASK_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=32,
                        MaxWidth=55,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер ПЗ",
                        Path="PRODUCTION_TASK_NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=32,
                        MaxWidth=75,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Заявка",
                        Path="ORDER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=32,
                        MaxWidth=55,
                    },

                    new DataGridHelperColumn
                    {
                        Header=" ",
                        Path="_",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=2,
                        MaxWidth=2000,
                    },
                };
                PositionGrid.SetColumns(columns);
                PositionGrid.UseSorting = false;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                PositionGrid.OnSelectItem = selectedItem =>
                {
                    PositionGridSelectedItem = selectedItem;
                };

                // контекстное меню
                PositionGrid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    {
                        "EditStackingOnPallet",
                        new DataGridContextMenuItem()
                        {
                            Header="Изменить укладку на поддоне",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                EditStackingOnPallet();
                            }
                        }
                    },
                    {
                        "BlockLastLabelPrint",
                        new DataGridContextMenuItem()
                        {
                            Header="Заблокировать печать последнего ярлыка",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                BlockLastLabelPrint();
                            }
                        }
                    },
                    { "s0", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    {
                        "ShowLog",
                        new DataGridContextMenuItem()
                        {
                            Header="История автоматической печати ярлыков",
                            Action=()=>
                            {
                                ShowLog();
                            }
                        }
                    },
                    {
                        "ShowTechnologicalMap",
                        new DataGridContextMenuItem()
                        {
                            Header="Открыть техкарту",
                            Action=()=>
                            {
                                ShowTechnologicalMap();
                            }
                        }
                    },
                    {
                        "ShowTaskDetails",
                        new DataGridContextMenuItem()
                        {
                            Header="Детализация задания",
                            Action=()=>
                            {
                                ShowTaskDetails();
                            }
                        }
                    },
                };

                PositionGrid.Init();
                PositionGrid.Run();
            }
        }

        /// <summary>
        /// Получение данных по позициям производственного задания
        /// </summary>
        public async void PositionGridLoadItems()
        {
            if (PositionGrid != null)
            {
                PositionGrid.ClearItems();
            }

            if (GridSelectedItem != null && GridSelectedItem.Count > 0)
            {
                DisableControls();

                var p = new Dictionary<string, string>();
                p.Add("PRODUCTION_TASK_ID", GridSelectedItem.CheckGet("PRODUCTION_TASK_ID"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "ManuallyPrint");
                q.Request.SetParam("Action", "ManuallyPrintListPosition");
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
                        PositionGridDataSet = ListDataSet.Create(result, "ITEMS");
                        PositionGrid.UpdateItems(PositionGridDataSet);
                    }
                }

                EnableControls();
            }
        }

        public void SetDefaults()
        {
            GridDataSet = new ListDataSet();
            GridSelectedItem = new Dictionary<string, string>();
            PositionGridDataSet = new ListDataSet();
            PositionGridSelectedItem = new Dictionary<string, string>();

            Dictionary<string, string> corrugatorMachines = new Dictionary<string, string>();
            corrugatorMachines.Add("-1", "Все ГА");
            corrugatorMachines.Add("23", "ГА1 КШ");
            CorrugatorMachineSelectBox.SetItems(corrugatorMachines);
            CorrugatorMachineSelectBox.SetSelectedItemByKey("-1");
            ChoiceCorrugatorMachineForSelectBox();

            if (Form != null)
            {
                Form.SetDefaults();
            }

            Form.SetValueByPath("FROM_DATE", DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy"));

            CurrentProductionTaskTimerAutoUpdateInterval = 65;
            RunTimer();
        }

        public void ChoiceCorrugatorMachineForSelectBox()
        {
            if (MachineId > 0)
            {
                CorrugatorMachineSelectBox.SetSelectedItemByKey($"{MachineId}");
            }
        }

        /// <summary>
        /// Получает производственные задания, которые в данный момент едут.
        /// Выделяет желтым цветом номера заданий, которые сейчас едут на линиях.
        /// 
        /// Работает по своему таймеру, обновляется быстрее, чем остальные гриды.
        /// Логика такая: 
        /// грид с данными по ПЗ большой и тяжёлый, нет смысла часто его обновлять, он не несёт большой пользы при обновлении; 
        /// информация по текущему ПЗ довольно важная. Запрос легковесный и обновлять его нужно часто (есть задания, которые проходят быстрее, чем за 5 минут). Поэтому этот запрос будет вызывать по таймеру
        /// </summary>
        public async void GetCurrentProductionTask()
        {
            var p = new Dictionary<string, string>();
            p.Add("FACTORY_ID", $"{FactoryId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "ManuallyPrint");
            q.Request.SetParam("Action", "GetCurrentProductionTask");
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
                    var dataSet = ListDataSet.Create(result, "ITEMS");
                    if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                    {
                        var listCurrentProductionTask = dataSet.Items.Select(x => x.CheckGet("PRODUCTION_TASK_ID")).ToList();

                        if (Grid != null && Grid.Items != null && Grid.Items.Count > 0)
                        {
                            foreach (var item in Grid.Items)
                            {
                                if (listCurrentProductionTask.Contains(item.CheckGet("PRODUCTION_TASK_ID")))
                                {
                                    item.CheckAdd("CURRENT_PRODUCTION_TASK", "1");
                                }
                                else
                                {
                                    item.CheckAdd("CURRENT_PRODUCTION_TASK", "0");
                                }
                            }

                            Grid.UpdateItems();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Запуск таймера для получения данных по текущим производственным заданиям
        /// </summary>
        public void RunTimer()
        {
            if (CurrentProductionTaskTimer == null)
            {
                CurrentProductionTaskTimer = new DispatcherTimer
                {
                    Interval = new TimeSpan(0, 0, CurrentProductionTaskTimerAutoUpdateInterval)
                };

                {
                    var row = new Dictionary<string, string>();
                    row.CheckAdd("TIMEOUT", CurrentProductionTaskTimerAutoUpdateInterval.ToString());
                    row.CheckAdd("DESCRIPTION", "");
                    Central.Stat.TimerAdd("TaskQueueKsh_RunTimer", row);
                }

                CurrentProductionTaskTimer.Tick += (s, e) =>
                {
                    GetCurrentProductionTask();
                };
            }
            else
            {
                CurrentProductionTaskTimer.Stop();
            }
            CurrentProductionTaskTimer.Start();
            
        }

        public void ManuallyPrint()
        {
            if (GridSelectedItem != null && GridSelectedItem.Count > 0)
            {
                var i = new StackerManuallyPrint();
                i.RoleName = this.RoleName;
                i.MachineId = MachineId;
                i.Form.SetValueByPath("PRODUCTION_TASK_ID", GridSelectedItem.CheckGet("PRODUCTION_TASK_ID"));
                i.Form.SetValueByPath("PRODUCTION_TASK_NUMBER", GridSelectedItem.CheckGet("PRODUCTION_TASK_NUMBER"));
                i.ParentFrame = FrameName;
                i.ParentContainer = ParentContainer;
                i.Show();
            }
            else
            {
                var msg = "Не выбранно производственное задание, для которого нужно распечатать ярлык.";
                var d = new DialogWindow($"{msg}", "Ручная печать ярлыков", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Заказать пустые поддоны
        /// </summary>
        public void OrderPallet()
        {
            if (GridSelectedItem != null && GridSelectedItem.Count > 0)
            {
                List<int> palletTypeIdList = new List<int>();

                var p = new Dictionary<string, string>();
                p.Add("PRODUCTION_TASK_ID", GridSelectedItem.CheckGet("PRODUCTION_TASK_ID"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "ManuallyPrint");
                q.Request.SetParam("Action", "GetPalletTypeByProductionTaskId");
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
                            palletTypeIdList = dataSet.Items.Select(x => x.CheckGet("PALLET_ID").ToInt()).ToList();
                        }
                    }
                }

                var i = new PalletList();
                i.ParentFrame = FrameName;
                i.ParentContainer = ParentContainer;
                i.MachineId = MachineId;
                if (palletTypeIdList != null)
                {
                    i.PalletTypeIdList = palletTypeIdList;
                }
                i.Show();
            }
            else
            {
                var i = new PalletList();
                i.ParentFrame = FrameName;
                i.ParentContainer = ParentContainer;
                i.MachineId = MachineId;
                i.Show();
            }
        }

        /// <summary>
        /// Заказать перестил
        /// </summary>
        public void OrderSubProduct()
        {
            var i = new SubProductList();
            i.ParentFrame = FrameName;
            i.ParentContainer = ParentContainer;
            i.MachineId = MachineId;
            i.Show();
        }

        /// <summary>
        /// Отбраковать вручную продукцию по выбранному производственному заданию
        /// </summary>
        public void ManuallyReject()
        {
            var i = new ManuallyReject();
            i.RoleName = this.RoleName;
            i.ProductionTaskId = GridSelectedItem.CheckGet("PRODUCTION_TASK_ID").ToInt();
            i.ParentFrame = FrameName;
            i.ParentContainer = ParentContainer;
            i.ProductionTaskNumber = GridSelectedItem.CheckGet("PRODUCTION_TASK_NUMBER").ToInt().ToString();
            i.Show();
        }

        /// <summary>
        /// Изменить укладку для выбранной заявки выбранного производственного задания
        /// </summary>
        public void EditStackingOnPallet()
        {
            if (GridSelectedItem != null && GridSelectedItem.Count > 0)
            {
                // Если задание ещё не началось, то можем менять
                if (string.IsNullOrEmpty(GridSelectedItem.CheckGet("START_DTTM")))
                {
                    if (PositionGridSelectedItem != null && PositionGridSelectedItem.Count > 0)
                    {
                        if (PositionGridSelectedItem.CheckGet("PRODUCT_CATEGORY_ID").ToInt() == 4)
                        {
                            DisableControls();

                            var i = new StackerManuallyPrintStackingEditor(
                                PositionGridSelectedItem.CheckGet("PRODUCTION_TASK_NUMBER"),
                                PositionGridSelectedItem.CheckGet("PRODUCT_NAME"),
                                PositionGridSelectedItem.CheckGet("QUANTITY_STACK_ON_PALLET").ToInt(),
                                PositionGridSelectedItem.CheckGet("QUANTITY_ON_STACK").ToInt(),
                                PositionGridSelectedItem.CheckGet("QUANTITY_ON_PALLET").ToInt(),
                                PositionGridSelectedItem.CheckGet("PRODUCTION_TASK_ID").ToInt(),
                                PositionGridSelectedItem.CheckGet("ORDER_ID").ToInt(),
                                PositionGridSelectedItem.CheckGet("PRODUCT_ID").ToInt()
                                );
                            i.ParemtFrameName = this.FrameName;
                            i.Show();

                            EnableControls();
                        }
                        else
                        {
                            var msg = "Нельзя изменить укладку для поддона с готовой продукцией.";
                            var d = new DialogWindow($"{msg}", "Ручная печать ярлыков", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                    else
                    {
                        var msg = "Не выбрана позиция производственного задания.";
                        var d = new DialogWindow($"{msg}", "Ручная печать ярлыков", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    var msg = "Нельзя изменить укладку на поддоне для задания, которое уже началось.";
                    var d = new DialogWindow($"{msg}", "Ручная печать ярлыков", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                var msg = "Не выбрано задание.";
                var d = new DialogWindow($"{msg}", "Ручная печать ярлыков", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Блокировка печати последнего ярлыка
        /// </summary>
        public async void BlockLastLabelPrint()
        {
            if (PositionGridSelectedItem != null && PositionGridSelectedItem.Count > 0)
            {
                var message = $"Заблокировать печать последнего ярлыка с продукцией {PositionGridSelectedItem.CheckGet("PRODUCT_NAME")} " +
                    $"по заданию {PositionGridSelectedItem.CheckGet("PRODUCTION_TASK_NUMBER")}?";
                if (DialogWindow.ShowDialog(message, "Ручная печать ярлыков", "", DialogWindowButtons.YesNo) == true)
                {
                    var p = new Dictionary<string, string>();
                    p.Add("PRODUCT_ID", PositionGridSelectedItem.CheckGet("PRODUCT_ID"));
                    p.Add("PRODUCTION_TASK_ID", PositionGridSelectedItem.CheckGet("PRODUCTION_TASK_ID"));
                    p.Add("LAST_PALLET_BLOCKED_PRINT_FLAG", "1");

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Production");
                    q.Request.SetParam("Object", "ManuallyPrint");
                    q.Request.SetParam("Action", "UpdateLastPalletBlockedPrintFlag");
                    q.Request.SetParams(p);
                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    if (q.Answer.Status != 0)
                    {
                        q.ProcessError();
                    }

                    PositionGridLoadItems();
                }
            }
        }

        /// <summary>
        /// Открытие эксель файла техкарты для продукции выбранной заявки производсвтенного задания
        /// </summary>
        public void ShowTechnologicalMap()
        {
            if (PositionGridSelectedItem != null)
            {
                if (PositionGridSelectedItem.CheckGet("PRODUCT_CATEGORY_ID").ToInt() == 4)
                {
                    var msg = "У заготовок нет техкарт.";
                    var d = new DialogWindow($"{msg}", "Ручная печать ярлыков", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
                else
                {
                    string pathTk = PositionGridSelectedItem.CheckGet("PATHTK");

                    if (!string.IsNullOrEmpty(pathTk))
                    {
                        if (System.IO.File.Exists(pathTk))
                        {
                            Central.OpenFile(pathTk);
                        }
                        else
                        {
                            var msg = $"Файл {pathTk} не найден по указанному пути";
                            var d = new DialogWindow($"{msg}", "Ручная печать ярлыков", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                    else
                    {
                        var msg = "Не найден путь к Excel файлу тех карты";
                        var d = new DialogWindow($"{msg}", "Ручная печать ярлыков", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
            }
            else
            {
                var msg = "Не выбрана заявка, для которой нужно найти тех карту";
                var d = new DialogWindow($"{msg}", "Ручная печать ярлыков", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Открытие формы с подробной информацией по заявке производственного задания
        /// </summary>
        public void ShowTaskDetails()
        {
            int productionTaskId = GridSelectedItem.CheckGet("PRODUCTION_TASK_ID").ToInt();
            int productId = PositionGridSelectedItem.CheckGet("PRODUCT_ID").ToInt();

            if (productionTaskId > 0 && productId > 0)
            {
                var i = new TaskDetails();
                i.ProductionTaskId = productionTaskId;
                i.ProductId = productId;
                i.ParentFrame = FrameName;
                i.Show();
            }
        }

        /// <summary>
        /// Отображение логов по выбранному производственному заданию по автоматической печати ярлыков
        /// </summary>
        public async void ShowLog()
        {
            string productionTaskNumber = GridSelectedItem.CheckGet("PRODUCTION_TASK_NUMBER");
            string productName = PositionGridSelectedItem.CheckGet("PRODUCT_NAME");
            int productionTaskId = GridSelectedItem.CheckGet("PRODUCTION_TASK_ID").ToInt();
            int productId = PositionGridSelectedItem.CheckGet("PRODUCT_ID").ToInt();
            string tableDirectory = $"{productionTaskId}_{productId}";

            var p = new Dictionary<string, string>();
            // 1=global,2=local,3=net
            p.Add("STORAGE_TYPE", "3");
            p.Add("TABLE_NAME", LogTableName);
            p.Add("TABLE_DIRECTORY", tableDirectory);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Service");
            q.Request.SetParam("Object", "LiteBase");
            q.Request.SetParam("Action", "List");
            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                string logMsg = "";

                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, LogTableName);
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        List<Dictionary<string, string>> logList = new List<Dictionary<string, string>>();
                        logList = ds.Items.OrderBy(x => x.CheckGet("ON_DATE").ToDateTime()).ToList();

                        logMsg = $"Задание: {productionTaskNumber}. Продукция: {productName}.";
                        foreach (var logItem in logList)
                        {
                            logMsg = $"{logMsg}" +
                                $"{Environment.NewLine}-----{logItem.CheckGet("ON_DATE")}-----" +
                                $"{Environment.NewLine}{logItem.CheckGet("MESSAGE")}";
                        }

                        var d = new DialogWindow($"{logMsg}", "История автоматической печати ярлыков", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }

                if (string.IsNullOrEmpty(logMsg))
                {
                    var d = new DialogWindow($"Не найдена история по выбранной заявке.", "История автоматической печати ярлыков", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// обработчик системы навигации по URL
        /// </summary>
        public void ProcessNavigation()
        {
            //параметры запуска
            var p = Central.Navigator.Address.Params;

            var machineId = p.CheckGet("machine_id").ToInt();
            if (machineId > 0)
            {
                if (machineId.ContainsIn(23))
                {
                    MachineId = machineId;
                    ChoiceCorrugatorMachineForSelectBox();

                    switch (MachineId)
                    {
                        case 23:
                            CorrugatorMachineNumberLabel.Content = "ГА1 КШ";
                            break;

                        default:
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// отображение фрейма
        /// </summary>
        public void Show()
        {
            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 1;

            var dt = DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss");
            FrameName = $"{FrameName}_{dt}";
            Central.WM.Show(FrameName, $"Список ПЗ КШ", true, "add", this);
        }

        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
            Grid.ShowSplash();
            PositionGrid.ShowSplash();
        }

        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
            Grid.HideSplash();
            PositionGrid.HideSplash();
        }

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Production",
                ReceiverName = "",
                SenderName = this.FrameName,
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// отображение справочной статьи
        /// (относительный путь)
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp-new/gofroproduction/label_printing/task_list");
            //Central.ShowHelp("/doc/l-pack-erp/production/stacker_cm/stacker_manually_print/list_production_task");
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.IndexOf("Production") > -1)
            {
                if (m.ReceiverName.IndexOf(this.FrameName) > -1)
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            GridLoadItems();
                            break;
                    }
                }
            }
        }

        private void ResreshButton_Click(object sender, RoutedEventArgs e)
        {
            GridLoadItems();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void CorrugatorMachineSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void ManuallyPrintButton_Click(object sender, RoutedEventArgs e)
        {
            ManuallyPrint();
        }
    }
}
