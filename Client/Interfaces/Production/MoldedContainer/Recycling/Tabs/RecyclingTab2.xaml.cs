using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Stock;
using Microsoft.Office.Interop.Excel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.MoldedContainer
{
    /// <summary>
    /// Логика взаимодействия для RecyclingTab2.xaml
    /// <author>Greshnyh_ni</author>
    /// <version>1</version>
    /// <released>2024-07-16</released>
    /// <changed>2025-04-08</changed>
    /// </summary>
    public partial class RecyclingTab2 : ControlBase
    {
        public FormHelper Form { get; set; }

        private ListDataSet ProductionTaskGridDataSet { get; set; }
        private ListDataSet ConsumptionPalletGridDataSet { get; set; }
        private ListDataSet ArrivalPalletGridDataSet { get; set; }
        private ListDataSet ProductionTaskDataSetColor { get; set; }

        /// <summary>
        /// текущий в работе Id_pz
        /// </summary>
        public int RecyclingGridIdPz { get; set; }
        /// <summary>
        /// текущий в работе Prot_Id
        /// </summary>
        public int RecyclingGridProtId { get; set; }

        /// <summary>
        /// текущий в работе Id2 изделие
        /// </summary>
        public int RecyclingGridId2 { get; set; }
        /// <summary>
        /// текущий в работе Blank_Id2 заготовка цвет
        /// </summary>
        public int RecyclingGridBlankId2 { get; set; }

        /// <summary>
        /// true - у текущего задания есть не оприходованные паллеты 
        /// </summary>
        public bool PalletNotArrivialFlag { get; set; }
        /// <summary>
        /// true - паллета уже проведена 
        /// </summary>
        public bool PalletArrivialFlag { get; set; }

        /// <summary>
        /// Таймер задержки повторного нажатия кнопки
        /// </summary>
        public Timeout ButtonTimer { get; set; }
        /// <summary>
        /// Массив кнопок для их сброса в первоначальное состояние
        /// </summary>
        private bool[] buttons = new bool[20];

        public bool Initialized { get; set; }

        /// <summary>
        /// Таймер периодического обновления каждые 30 секунд
        /// </summary>
        private DispatcherTimer FastTimer { get; set; }

        /// <summary>
        /// Таймер периодического обновления каждые 60 секунд
        /// </summary>
        private DispatcherTimer SlowTimer { get; set; }

        public bool ReadOnlyFlag { get; set; }
        private bool VisibleFlag { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public RecyclingTab2()
        {
            ControlTitle = "Переработка ЛТ";
            DocumentationUrl = "/doc/l-pack-erp-new/lt/recycling_lt";
            RoleName = "[erp]molded_contnr_converting";

            InitializeComponent();

            Form = null;

            //регистрация обработчика сообщений
            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnKeyPressed = (KeyEventArgs e) =>
            {
                if (ReadOnlyFlag == false)
                {
                    if (!Input.WordScanned.IsNullOrEmpty())
                    {
                        var id = Input.WordScanned.ToInt();
                        if (id != 0)
                        {
                            InfoPallet(id.ToString());
                            StrihKodText.Text = "";
                        }
                    }
                }

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
                Initialized = false;

                ProcessPermissions();
                
                LoadListMachine();
                SetDefaults();
                FormInit();
                ProductionTaskGridInit();
                ConsumptionPalletGridInit();
                ArrivalPalletGridInit();
                CounterLoad();

                Initialized = true;
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                ProductionTaskGrid.Destruct();
                ConsumptionPalletGrid.Destruct();
                ArrivalPalletGrid.Destruct();
                // Остановка таймеров
                FastTimer?.Stop();
                SlowTimer?.Stop();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                ProductionTaskGrid.ItemsAutoUpdate = true;
                ProductionTaskGrid.Run();

                ConsumptionPalletGrid.ItemsAutoUpdate = true;
                ConsumptionPalletGrid.Run();

                ArrivalPalletGrid.ItemsAutoUpdate = true;
                ArrivalPalletGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                ProductionTaskGrid.ItemsAutoUpdate = false;
                ConsumptionPalletGrid.ItemsAutoUpdate = false;
                ArrivalPalletGrid.ItemsAutoUpdate = false;
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

            Commander.SetCurrentGridName("ProductionTaskGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "start_production_task",
                    Title = "Начать ПЗ",
                    Description = "Начать выбранное производственное задание",
                    Group = "production_task_grid_default",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = ProductionTaskGridStartButton,
                    ButtonName = "ProductionTaskGridStartButton",
                    Enabled = false,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        EditStatusProductionTask();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ReadOnlyFlag == false)
                        {
                            if (ProductionTaskGrid != null
                            && ProductionTaskGrid.SelectedItem != null
                            && ProductionTaskGrid.SelectedItem.Count > 0)
                            {
                                if ((ProductionTaskGrid.SelectedItem.CheckGet("TASK_STATUS_ID").ToInt() <= 3)
                                || (ProductionTaskGrid.SelectedItem.CheckGet("TASK_STATUS_ID").ToInt() == 8))
                                {
                                    // получаем текущие Id_pz и на каком станке производят изделия
                                    GurrentTask();
                                    if (RecyclingGridIdPz == 0)
                                        result = true;
                                }
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "suspend_production_task",
                    Title = "Приостановить ПЗ",
                    Description = "Приостановить выполнение выбранного производственного задания",
                    Group = "production_task_grid_default",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = ProductionTaskGridSuspendButton,
                    ButtonName = "ProductionTaskGridSuspendButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Enabled = false,
                    Action = () =>
                    {
                        SuspendProductionTask();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ReadOnlyFlag == false)
                        {
                            if (ProductionTaskGrid != null
                            && ProductionTaskGrid.SelectedItem != null
                            && ProductionTaskGrid.SelectedItem.Count > 0)
                            {
                                if (ProductionTaskGrid.SelectedItem.CheckGet("TASK_STATUS_ID").ToInt() == 4)
                                {
                                    result = true;
                                }
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "end_production_task",
                    Title = "Закончить ПЗ",
                    Description = "Закончить выполнение выбранного производственного задания",
                    Group = "production_task_grid_default",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = ProductionTaskGridEndButton,
                    ButtonName = "ProductionTaskGridEndButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Enabled = false,
                    Action = () =>
                    {
                        EditStatusProductionTask();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;
                        if (ReadOnlyFlag == false)
                        {
                            if (ProductionTaskGrid != null && ProductionTaskGrid.SelectedItem != null && ProductionTaskGrid.SelectedItem.Count > 0)
                            {
                                if (ProductionTaskGrid.SelectedItem.CheckGet("TASK_STATUS_ID").ToInt() == 4)
                                {
                                    result = true;
                                }
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "return_production_task",
                    Title = "Вернуть в очередь",
                    Description = "Вернуть в очередь выбранное производственного задания",
                    Group = "production_task_grid_default",
                    MenuUse = true,
                    ButtonUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Enabled = false,
                    Action = () =>
                    {
                        var row = ProductionTaskGrid.SelectedItem;
                        var taskId = row.CheckGet("TASK_ID").ToInt();
                        var note = row.CheckGet("SUSPEND_NOTE");
                        var taskNum = row.CheckGet("TASK_NUMBER");

                        ReturnProductionTask(taskNum, taskId, note);
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;
                        if (ReadOnlyFlag == false)
                        {
                            if (ProductionTaskGrid != null && ProductionTaskGrid.SelectedItem != null && ProductionTaskGrid.SelectedItem.Count > 0)
                            {
                                if (ProductionTaskGrid.SelectedItem.CheckGet("TASK_STATUS_ID").ToInt() == 5)
                                {
                                    result = true;
                                }
                            }
                        }

                        return result;
                    },
                });

                Commander.Add(new CommandItem()
                {
                    Name = "show_technologhical_map",
                    Title = "Открыть ТК",
                    Description = "Открыть Excel файл тех карты для продукции по этому заданию",
                    Group = "production_task_grid_default",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = ShowExcelButton,
                    ButtonName = "ShowExcelButton",
                    Enabled = false,
                    Action = () =>
                    {
                        var filePath = ProductionTaskGrid.SelectedItem.CheckGet("TK_FILE_PATH");
                        OpenTechnologicalMap(filePath);
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;

                        if (ProductionTaskGrid != null && ProductionTaskGrid.SelectedItem != null && ProductionTaskGrid.SelectedItem.Count > 0)
                        {
                            var filePath = ProductionTaskGrid.SelectedItem.CheckGet("TK_FILE_PATH");
                            if ((ProductionTaskGrid.SelectedItem.CheckGet("TASK_ID").ToInt() != 0)
                                && (!filePath.IsNullOrEmpty()))
                            {
                                if (File.Exists(filePath))
                                {
                                    result = true;
                                }
                            }
                        }

                        return result;
                    },
                });

                Commander.Add(new CommandItem()
                {
                    Name = "show_all_task",
                    Title = "Все ПЗ",
                    Description = "Показать все ранее выполненые задания",
                    Group = "production_task_grid_default",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonName = "ShowAllPz",
                    Enabled = true,
                    HotKey = "",
                    Action = () =>
                    {
                        ShowAllProdictionTask();
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;

                        if (ReadOnlyFlag == false)
                        {
                            result = true;
                        }
                        return result;
                    },

                });
            }

            Commander.SetCurrentGridName("ArrivalPalletGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "add_pallet",
                    Title = "Создать",
                    Description = "Создать новую паллету",
                    Group = "arrival_pallet_grid_default",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = PalletCreateButton,
                    ButtonName = "PalletCreateButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Enabled = false,
                    Action = () =>
                    {
                        CreatePallet();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ReadOnlyFlag == false)
                        {
                            // это не упаковщик
                            // if (Machines.SelectedItem.Key.ToInt() != 331)
                            {
                                if (ProductionTaskGrid != null && ProductionTaskGrid.SelectedItem != null && ProductionTaskGrid.SelectedItem.Count > 0)
                                {
                                    // Задание в работе 
                                    if (ProductionTaskGrid.SelectedItem.CheckGet("TASK_STATUS_ID").ToInt() == 4)
                                    {
                                        result = true;
                                    }
                                }
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "delete_pallet",
                    Title = "Удалить",
                    Description = "Удалить выбранную не оприходованную паллету",
                    Group = "arrival_pallet_grid_default",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = PalletDeleteButton,
                    ButtonName = "PalletDeleteButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Enabled = false,
                    Action = () =>
                    {
                        DeletePallet();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;
                        if (ReadOnlyFlag == false)
                        {
                            // это не упаковщик
                            if (Machines.SelectedItem.Key.ToInt() != 331)
                            {
                                if (ArrivalPalletGrid != null && ArrivalPalletGrid.SelectedItem != null && ArrivalPalletGrid.SelectedItem.Count > 0)
                                {
                                    if (ArrivalPalletGrid.SelectedItem.CheckGet("PALLET_POST").ToInt() == 0)
                                    {
                                        result = true;
                                    }
                                }
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "print_pallet",
                    Title = "Печать ярлыка",
                    Description = "Распечатать ярлык для выбранной паллеты",
                    Group = "arrival_pallet_grid_default",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = PalletPrintButton,
                    ButtonName = "PalletPrintButton",
                    Enabled = false,
                    Action = () =>
                    {
                        PrintPallet();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ArrivalPalletGrid != null && ArrivalPalletGrid.SelectedItem != null && ArrivalPalletGrid.SelectedItem.Count > 0)
                        {
                            result = true;
                        }

                        return result;
                    },
                });

                Commander.Add(new CommandItem()
                {
                    Name = "show_all_arrivial",
                    Title = "Все паллеты",
                    Description = "Показать все оприходованные паллеты с готовой продукцией",
                    Group = "arrival_pallet_grid_default",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonName = "ShowAllArrivalPallet",
                    Enabled = true,
                    HotKey = "",
                    Action = () =>
                    {
                        ShowAllArrivialPallet();
                    },
                    CheckEnabled = () =>
                    {
                        var result = true;
                        return result;
                    },
                });
            }

            Commander.SetCurrentGridName("ConsumptionPalletGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "show_all_consumption",
                    Title = "Все паллеты",
                    Description = "Показать все оприходованные паллеты с заготовками",
                    Group = "consumption_pallet_grid_default",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonName = "ShowAllConsumptionPallet",
                    Enabled = true,
                    HotKey = "",
                    Action = () =>
                    {
                        ShowConsumptionPalletAll();
                    },
                    CheckEnabled = () =>
                    {
                        var result = true;
                        return result;
                    },
                });

            }

            Commander.Init(this);

            ButtonTimer = new Timeout(
            3,
            () =>
            {
                ButtonPush();
            },
            true,
            false
            );
            ButtonTimer.Finish();

        }

        /// <summary>
        /// проверка доступа
        /// </summary>
        public void ProcessPermissions()
        {
            var mode = Central.Navigator.GetRoleLevel(this.RoleName);
            var userAccessMode = mode;
            ReadOnlyFlag = true;

            VisibleFlag = false;
            switch (mode)
            {
                case Role.AccessMode.Special:
                    {
                        ReadOnlyFlag = false;
                        VisibleFlag = true;
                        ProductionTaskGridStartButton.IsEnabled = false;
                        ProductionTaskGridSuspendButton.IsEnabled = false;
                        ProductionTaskGridEndButton.IsEnabled = false;
                        ShowAllPz.IsEnabled = true;
                        PalletCreateButton.IsEnabled = false;
                    }
                    break;

                case Role.AccessMode.FullAccess:
                    {
                        ReadOnlyFlag = false;
                    }
                    break;

                case Role.AccessMode.ReadOnly:
                    {
                        ProductionTaskGridStartButton.IsEnabled = false;
                        ProductionTaskGridSuspendButton.IsEnabled = false;
                        ProductionTaskGridEndButton.IsEnabled = false;
                        ShowAllPz.IsEnabled = false;
                        PalletCreateButton.IsEnabled = false;
                    }
                    break;
            }

        }


        /// <summary>
        // инициализация компонентов формы
        /// </summary>
        public void FormInit()
        {
            // Информация по простоям
            if (Machines != null && Machines.SelectedItem.Key != null)
            {
                IdlesData.MachineId = Machines.SelectedItem.Key.ToInt();
                IdlesData.Init();
            }

            // график намотки тамбура
            if (Machines != null && Machines.SelectedItem.Key != null)
            {
                SpeedChart.MachineId = Machines.SelectedItem.Key.ToInt();
                SpeedChart.Init();
            }

            SetFastTimer(30);
            SetSlowTimer(60);
        }

        /// <summary>
        /// настройки по умолчанию 
        /// </summary>
        public void SetDefaults()
        {
            ProductionTaskGridDataSet = new ListDataSet();
            ConsumptionPalletGridDataSet = new ListDataSet();
            ArrivalPalletGridDataSet = new ListDataSet();
            ProductionTaskDataSetColor = new ListDataSet();
            Machines.SetSelectedItemByKey("311.0");
        }

        /// <summary>
        /// обновляем все гриды
        /// </summary>
        public void Refresh()
        {
            // buttons[0] = false;
            // RefreshButton.IsEnabled = false;
            // ButtonTimer.Run();

            LoadListMachine();
            
            ProductionTaskGrid.LoadItems();
            ConsumptionPalletGrid.LoadItems();
            ArrivalPalletGrid.LoadItems();

            // График скорости
            if (Initialized)
            {
                // График скорости
                SpeedChart.MachineId = Machines.SelectedItem.Key.ToInt();
                SpeedChart.LoadData();
            }

            // Информация по простоям
            IdlesData.MachineId = Machines.SelectedItem.Key.ToInt();
            IdlesData.LoadItems();

            // Информация по заданию
            CounterLoad();
        }

        /// <summary>
        /// Таймер частого обновления (30 секунд)
        /// </summary>
        public void SetFastTimer(int autoUpdateInterval)
        {
            FastTimer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, autoUpdateInterval)
            };

            {
                var row = new Dictionary<string, string>();
                row.CheckAdd("TIMEOUT", autoUpdateInterval.ToString());
                row.CheckAdd("DESCRIPTION", "");
                Central.Stat.TimerAdd("RecyclingTab2_SetFastTimer", row);
            }

            FastTimer.Tick += (s, e) =>
            {
                CounterLoad();
            };

            FastTimer.Start();
        }

        /// <summary>
        /// Таймер медленного обновления (60 секунд)
        /// </summary>
        public void SetSlowTimer(int autoUpdateInterval)
        {
            SlowTimer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, autoUpdateInterval)
            };

            {
                var row = new Dictionary<string, string>();
                row.CheckAdd("TIMEOUT", autoUpdateInterval.ToString());
                row.CheckAdd("DESCRIPTION", "");
                Central.Stat.TimerAdd("RecyclingTab2_SetSlowTimer", row);
            }

            SlowTimer.Tick += (s, e) =>
            {
                if (Initialized)
                {
                    // График скорости
                    SpeedChart.MachineId = Machines.SelectedItem.Key.ToInt();
                    SpeedChart.LoadData();
                    // Информация по простоям
                    IdlesData.MachineId = Machines.SelectedItem.Key.ToInt();
                    IdlesData.LoadItems();
                }
            };

            SlowTimer.Start();
        }

        /// <summary>
        /// получение данных счетчика для выбранного станка и текущего в работе задания
        /// </summary>
        public async void CounterLoad()
        {
            if (Machines != null && Machines.SelectedItem.Key != null)
            {
                var p = new Dictionary<string, string>();
                p.Add("ID_ST", Machines.SelectedItem.Key.ToInt().ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "MoldedContainer");
                q.Request.SetParam("Object", "Recycling");
                q.Request.SetParam("Action", "GetInfoMachine");
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
                        var ds = ListDataSet.Create(result, "ITEMS");
                        var counter_qty = ds.Items.FirstOrDefault().CheckGet("COUNTER_QTY").ToInt().ToString();
                        var task_quantity = ds.Items.FirstOrDefault().CheckGet("TASK_QUANTITY").ToInt().ToString();
                        var prihod_qty = ds.Items.FirstOrDefault().CheckGet("PRIHOD_QTY").ToInt().ToString();
                        var rashod_qty = ds.Items.FirstOrDefault().CheckGet("RASHOD_QTY").ToInt().ToString();
                        CounterQty.Text = counter_qty;
                        TaskQuantity.Text = task_quantity;
                        PrihodQty.Text = prihod_qty;
                        RashodQty.Text = rashod_qty;
                    }
                }
            }
        }

        /// <summary>
        /// настройка грида ПЗ
        /// </summary>
        public void ProductionTaskGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=3,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="TASK_ID",
                        Description="(prot_id)",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер",
                        Path="TASK_NUMBER",
                        Description="",
                        ColumnType=ColumnTypeRef.String,
                        Width2=12,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Дата отгрузки",
                        Path = "SHIP_DTTM",
                        Description = "",
                        ColumnType = ColumnTypeRef.DateTime,
                        Width2 = 15,
                        Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor, row =>
                                {
                                    var result = DependencyProperty.UnsetValue;
                                    var color = "";

                                    if (!string.IsNullOrEmpty(row.CheckGet("SHIP_DTTM")))
                                    {
                                        DateTime startDttm = DateTime
                                            .ParseExact(row.CheckGet("FINISH_DTTM"), "dd.MM.yyyy HH:mm:ss",
                                                CultureInfo.InvariantCulture).ToUniversalTime();
                                        DateTime shipDttm = DateTime.ParseExact(row.CheckGet("SHIP_DTTM"),
                                            "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture).ToUniversalTime();

                                        double timeDiff = (startDttm - shipDttm).TotalMinutes;

                                        if (timeDiff > 0)
                                        {
                                            if (timeDiff <= 120)
                                            {
                                                color = HColor.Yellow;
                                            }
                                            else if (timeDiff > 120 && timeDiff <= 240)
                                            {
                                                color = HColor.Orange;
                                            }
                                            else
                                            {
                                                color = HColor.Red;
                                            }
                                        }
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result = color.ToBrush();
                                    }

                                    return result;
                                }
                            }
                        }
                    },
                    new DataGridHelperColumn
                    {
                        Header="Станок",
                        Path="MACHINE_ID",
                        Description="",
                        ColumnType=ColumnTypeRef.String,
                        Width2=12,
                        Visible = false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Изделие",
                        Path="GOODS_NAME",
                        Description="",
                        ColumnType=ColumnTypeRef.String,
                        Width2=42,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул",
                        Path="GOODS_CODE",
                        Description="",
                        ColumnType=ColumnTypeRef.String,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Цвет  лотка",
                        Path="COLOR_BLANK",
                        Description="",
                        ColumnType=ColumnTypeRef.String,
                        Width2=8,
                        Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor, row =>
                                {
                                    var result = DependencyProperty.UnsetValue;
                                    var color = "";

                                    if (row.CheckGet("COLOR_BLANK") == "белый")
                                    {
                                        color = HColor.YellowOrange;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result = color.ToBrush();
                                    }

                                    return result;
                                }
                            }
                        }



                    },
                    new DataGridHelperColumn
                    {
                        Header="Схема производства",
                        Path="PRODUCTION_SCHEME_NAME",
                        Description="",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус",
                        Path="TASK_STATUS_TITLE",
                        Description="",
                        ColumnType=ColumnTypeRef.String,
                        Width2=13,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус",
                        Path="TASK_STATUS_ID",
                        Description="",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=2,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество, шт",
                        Path="TASK_QUANTITY",
                        Description="",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="По счетчику, шт",
                        Path="COUNTER_QTY",
                        Description="Произведено продукции по данным счетчика",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Произведено, шт",
                        Path="LABEL_QTY",
                        Description="",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Оприходовано, шт",
                        Path="PRIHOD_QTY",
                        Description="",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Списано, шт",
                        Path="RASHOD_QTY",
                        Description="",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Заявка",
                        Path="ORDER_TITLE",
                        Description="",
                        ColumnType=ColumnTypeRef.String,
                        Width2=15,
                        Visible = VisibleFlag,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Прим. для производства",
                        Path="ORDER_NOTE_GENERAL",
                        Description="примечание ОПП и складу",
                        ColumnType=ColumnTypeRef.String,
                        Width2=18,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Прим. приостановки ПЗ",
                        Path="SUSPEND_NOTE",
                        Description="",
                        ColumnType=ColumnTypeRef.String,
                        Width2=18,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Примечание",
                        Path="PRODUCTION_NOTE",
                        Description="",
                        ColumnType=ColumnTypeRef.String,
                        Width2=18,
                    },
                     new DataGridHelperColumn
                    {
                        Header="ИДПЗ",
                        Path="TASK_ID2",
                        Description="(proiz_zad)",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД позиции заявки",
                        Path="ORDER_POSITION_ID",
                        Description="(idorderdates)",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД изделия",
                        Path="GOODS_ID",
                        Description="(id2)",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД заготовки",
                        Path="BLANK_ID2",
                        Description="(blank_id2)",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=10,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Количество на паллете",
                        Path="PER_PALLET_QTY",
                        Description="(tc.per_pallet_qty)",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=10,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="FINISH_DTTM",
                        Path="FINISH_DTTM",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="PRODUCTION_TIME",
                        Path="PRODUCTION_TIME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="CHANGEOVER_TIME",
                        Path="CHANGEOVER_TIME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                        Visible=false,
                    }
                };
                ProductionTaskGrid.SetColumns(columns);
                ProductionTaskGrid.SetPrimaryKey("TASK_ID");
                ProductionTaskGrid.SearchText = ProductionTaskSearchBox;
                //данные грида
                ProductionTaskGrid.OnLoadItems = ProductionTaskGridLoadItems;
                ProductionTaskGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                ProductionTaskGrid.AutoUpdateInterval = 60 * 5;
                ProductionTaskGrid.Toolbar = ProductionTaskGridToolbar;
                ProductionTaskGrid.EnableSortingGrid = false;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                ProductionTaskGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem != null)
                    {
                        if (ProductionTaskGrid != null && ProductionTaskGrid.Items != null && ProductionTaskGrid.Items.Count > 0)
                        {
                            if (ProductionTaskGrid.Items.FirstOrDefault(x => x.CheckGet("TASK_ID").ToInt() == selectedItem.CheckGet("TASK_ID").ToInt()) == null)
                            {
                                ProductionTaskGrid.SelectRowFirst();
                            }
                        }
                    }
                };

                ProductionTaskGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    {
                        StylerTypeRef.BackgroundColor,
                        (Dictionary<string, string> row) =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";
                            
                            // Зеленый -- в работе
                            if(row.ContainsKey("TASK_STATUS_ID"))
                            {
                                if (row.CheckGet("TASK_STATUS_ID").ToInt() == 4)
                                {
                                    color = HColor.Green;
                                }
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },
                };

                ProductionTaskGrid.OnFilterItems = ProductionTaskGridFilterItems;

                ProductionTaskGrid.Commands = Commander;

                ProductionTaskGrid.Init();
            }
        }

        public async void ProductionTaskGridLoadItems()
        {
            if (Machines != null && Machines.SelectedItem.Key != null)
            {
                var p = new Dictionary<string, string>();
                p.Add("MACHINE_ID", Machines.SelectedItem.Key.ToInt().ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "MoldedContainer");
                q.Request.SetParam("Object", "Recycling");
                q.Request.SetParam("Action", "List");
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
                        var ProductDataSet = RefactorTimeStart(ProductionTaskGridDataSet);
                        ProductionTaskGrid.UpdateItems(ProductDataSet);
                        //список цветов для заданий
                        ColorTask();
                    }
                }
                else
                {
                    // q.ProcessError();
                }
            }
        }


        /// <summary>
        /// Расчет времени окончания и начала задания
        /// </summary>
        /// <param name="list">Список заявок</param>
        /// <returns></returns>
        private ListDataSet RefactorTimeStart(ListDataSet list)
        {
            var num = 1;
            var timeCount = 0;
            DateTime now = DateTime.Now;
            now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);

            foreach (var item in list.Items)
            {
                var prodTime = item.CheckGet("PRODUCTION_TIME").ToInt() +
                               item.CheckGet("CHANGEOVER_TIME").ToInt();

                var status = item.CheckGet("PRTS_ID").ToInt();
                if (status == 2)
                {
                    DateTime dttmStart = now.AddMinutes(timeCount);
                    item.CheckAdd("START_DTTM", dttmStart.ToString("dd.MM.yyyy HH:mm:ss"));
                }

                timeCount += prodTime;
                DateTime dttmEnd = now.AddMinutes(timeCount);


                item.CheckAdd("FINISH_DTTM", dttmEnd.ToString("dd.MM.yyyy HH:mm:ss"));
                item.CheckAdd("NUM", num.ToString());

                num++;
            }

            return list;
        }

        public void ProductionTaskGridFilterItems()
        {
            if (ProductionTaskGrid != null && ProductionTaskGrid.SelectedItem != null && ProductionTaskGrid.SelectedItem.Count > 0)
            {
                ProductionTaskGrid.Commands.Message = new ItemMessage() { Action = "refresh", Message = $"{ProductionTaskGrid.SelectedItem.CheckGet("TASK_ID")}" };
            }
        }

        /// <summary>
        ////описание грида со списком паллет с заготовками для ЛТ
        /// </summary>
        public void ConsumptionPalletGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Дата списания",
                        Path="PALLET_RASHOD",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Description="",
                        Width2=14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ паллеты",
                        Path="PALLET_NUMBER_CUSTOM",
                        Description="",
                        ColumnType=ColumnTypeRef.String,
                        Width2=9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество, шт",
                        Path="GOODS_QUANTITY",
                        Description="",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                    },
                    //new DataGridHelperColumn
                    //{
                    //    Header="Станок",
                    //    Path="NAME",
                    //    Description="",
                    //    ColumnType=ColumnTypeRef.String,
                    //    Width2=28,
                    //},
                    new DataGridHelperColumn
                    {
                        Header="ИД расхода",
                        Path="IDR",
                        Description="idr",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД ПЗ",
                        Path="PRODUCTION_TASK_ID",
                        Description="prot_id",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД паллета",
                        Path="PALLET_ID",
                        Description="(id_poddon)",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=9,
                    },
                };
                ConsumptionPalletGrid.SetColumns(columns);
                ConsumptionPalletGrid.SetPrimaryKey("PALLET_ID");
                ConsumptionPalletGrid.SearchText = ProductionTaskSearchBox;
                //данные грида
                ConsumptionPalletGrid.OnLoadItems = ConsumptionPalletGridLoadItems;
                ConsumptionPalletGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                ConsumptionPalletGrid.AutoUpdateInterval = 60 * 5;
                ConsumptionPalletGrid.Toolbar = ConsumptionPalletGridToolbar;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                ConsumptionPalletGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem != null)
                    {
                        if (ConsumptionPalletGrid != null && ConsumptionPalletGrid.Items != null && ConsumptionPalletGrid.Items.Count > 0)
                        {
                            if (ConsumptionPalletGrid.Items.FirstOrDefault(x => x.CheckGet("PALLET_ID").ToInt() == selectedItem.CheckGet("PALLET_ID").ToInt()) == null)
                            {
                                ConsumptionPalletGrid.SelectRowFirst();
                            }
                        }
                    }
                };

                ConsumptionPalletGrid.OnFilterItems = ConsumptionPalletGridFilterItems;

                ConsumptionPalletGrid.Commands = Commander;

                ConsumptionPalletGrid.Init();
            }
        }

        /// <summary>
        /// загрузка данных для оприходованных паллет с заготовками
        /// </summary>
        public async void ConsumptionPalletGridLoadItems()
        {
            if (Machines != null && Machines.SelectedItem.Key != null)
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("ID_ST", Machines.SelectedItem.Key.ToInt().ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "MoldedContainer");
                q.Request.SetParam("Object", "Recycling");
                q.Request.SetParam("Action", "PalletСonsumptionList");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                ConsumptionPalletGridDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        ConsumptionPalletGridDataSet = ListDataSet.Create(result, "ITEMS");
                    }
                }
                else
                {
                    //  q.ProcessError();
                }

                ConsumptionPalletGrid.UpdateItems(ConsumptionPalletGridDataSet);
            }
        }


        /// <summary>
        /// загрузка данных для оприходованных паллет с заготовками (все)
        /// </summary>
        public async void ConsumptionPalletGridLoadAllItems()
        {
            if (Machines != null && Machines.SelectedItem.Key != null)
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("ID_ST", Machines.SelectedItem.Key.ToInt().ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "MoldedContainer");
                q.Request.SetParam("Object", "Recycling");
                q.Request.SetParam("Action", "PalletСonsumptionAllList");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                ConsumptionPalletGridDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        ConsumptionPalletGridDataSet = ListDataSet.Create(result, "ITEMS");
                    }
                }
                else
                {
                    //   q.ProcessError();
                }

                ConsumptionPalletGrid.UpdateItems(ConsumptionPalletGridDataSet);
            }
        }


        public void ConsumptionPalletGridFilterItems()
        {
            if (ConsumptionPalletGrid != null && ConsumptionPalletGrid.SelectedItem != null && ConsumptionPalletGrid.SelectedItem.Count > 0)
            {
                ConsumptionPalletGrid.Commands.Message = new ItemMessage() { Action = "refresh", Message = $"{ConsumptionPalletGrid.SelectedItem.CheckGet("PALLET_ID")}" };
            }
        }

        public void ArrivalPalletGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Дата создания",
                        Path="PALLET_CREATED",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Description="",
                        Width2=14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ паллета",
                        Path="PALLET_NUMBER_CUSTOM",
                        Description="",
                        ColumnType=ColumnTypeRef.String,
                        Width2=9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество, шт",
                        Path="GOODS_QUANTITY",
                        Description="",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="GOODS_NAME",
                        Description="",
                        ColumnType=ColumnTypeRef.String,
                        Width2=39,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД ПЗ",
                        Path="PRODUCTION_TASK_ID",
                        Description="prot_id",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД паллета",
                        Path="PALLET_ID",
                        Description="(id_poddon)",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Оприходован",
                        Path="PALLET_POST",
                        Description="",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ПЗ",
                        Path="PRODUCTION_TASK2_ID",
                        Description="id_pz",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                        Hidden = true
                    },
                };
                ArrivalPalletGrid.SetColumns(columns);
                ArrivalPalletGrid.SetPrimaryKey("PALLET_ID");
                ArrivalPalletGrid.SearchText = ProductionTaskSearchBox;
                //данные грида
                ArrivalPalletGrid.OnLoadItems = ArrivalPalletGridLoadItems;
                ArrivalPalletGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                ArrivalPalletGrid.AutoUpdateInterval = 60 * 5;
                ArrivalPalletGrid.Toolbar = ArrivalPalletGridToolbar;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                ArrivalPalletGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem != null)
                    {
                        if (ArrivalPalletGrid != null && ArrivalPalletGrid.Items != null && ArrivalPalletGrid.Items.Count > 0)
                        {
                            if (ArrivalPalletGrid.Items.FirstOrDefault(x => x.CheckGet("PALLET_ID").ToInt() == selectedItem.CheckGet("PALLET_ID").ToInt()) == null)
                            {
                                ArrivalPalletGrid.SelectRowFirst();
                            }
                        }
                    }
                };

                // раскраска грида
                ArrivalPalletGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    {
                        StylerTypeRef.BackgroundColor,
                        (Dictionary<string, string> row) =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            // Зеленый -- оприходован
                            if(row.ContainsKey("PALLET_POST"))
                            {
                                if (row.CheckGet("PALLET_POST").ToInt() == 1)
                                {
                                    color = HColor.Green;
                                }
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },
            };


                ArrivalPalletGrid.OnFilterItems = ArrivalPalletGridFilterItems;

                ArrivalPalletGrid.Commands = Commander;

                ArrivalPalletGrid.Init();
            }
        }

        public async void ArrivalPalletGridLoadItems()
        {
            if (Machines != null && Machines.SelectedItem.Key != null)
            {
                var p = new Dictionary<string, string>();
                p.Add("ID_ST", Machines.SelectedItem.Key.ToInt().ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "MoldedContainer");
                q.Request.SetParam("Object", "Recycling");
                q.Request.SetParam("Action", "PalletList");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                ArrivalPalletGridDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        ArrivalPalletGridDataSet = ListDataSet.Create(result, "ITEMS");
                    }
                }
                else
                {
                    // q.ProcessError();
                }

                ArrivalPalletGrid.UpdateItems(ArrivalPalletGridDataSet);
            }
        }

        public void ArrivalPalletGridFilterItems()
        {
            if (ArrivalPalletGrid != null && ArrivalPalletGrid.SelectedItem != null && ArrivalPalletGrid.SelectedItem.Count > 0)
            {
                ArrivalPalletGrid.Commands.Message = new ItemMessage() { Action = "refresh", Message = $"{ArrivalPalletGrid.SelectedItem.CheckGet("PALLET_ID")}" };
            }
        }

        /// <summary>
        /// Возврат производственного задания в очередь
        /// </summary>
        /// <param name="taskName"></param>
        private async void ReturnProductionTask(string taskName, int taskId, string note)
        {
            var dw = new DialogWindow($"Вы действительно хотите вернуть производственное задание №{taskName}?", "Работа с ПЗ", "", DialogWindowButtons.NoYes);
            if (dw.ShowDialog() == true)
            {
                ReturnTask(taskId, note);
            }
        }

        private async void ReturnTask(int taskId, string note)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Recycling");
            q.Request.SetParam("Action", "TaskStatusSave");
            q.Request.SetParam("TASK_ID", taskId.ToString());
            q.Request.SetParam("SUSPEND_NOTE", note);
            q.Request.SetParam("PRTS_ID", "3");

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                ProductionTaskGridLoadItems();
            }
        }

        /// <summary>
        /// Открыть/ закрыть ПЗ
        /// </summary>
        public async void EditStatusProductionTask()
        {
            bool resume = true;

            ProductionTaskGrid.UpdateItems();

            var id = ProductionTaskGrid.SelectedItem.CheckGet("TASK_ID").ToInt();
            var status = ProductionTaskGrid.SelectedItem.CheckGet("TASK_STATUS_ID").ToInt();
            var num = ProductionTaskGrid.SelectedItem.CheckGet("TASK_NUMBER").ToString();
            var task_col = ProductionTaskGrid.SelectedItem.CheckGet("TASK_QUANTITY").ToInt();
            var task_prih = ProductionTaskGrid.SelectedItem.CheckGet("PRIHOD_QTY").ToInt();

            if (resume)
            {
                if (status == 4)
                {
                    if (CheckReceivedPalletByTask())
                    {
                        resume = false;
                    }
                }
            }

            if (resume)
            {
                //// проверяем выполнение задания
                if ((status == 4) && (task_col > task_prih))
                {
                    var dw = new DialogWindow($"Производственное задание №{num} выполнено не полностью.", "Работа с ПЗ", "Закрыть?", DialogWindowButtons.NoYes);
                    if (dw.ShowDialog() != true)
                    {
                        resume = false;
                    }
                    else
                    {
                        // вносим комментарий по текущему ПЗ
                        CloseProductionTask();
                        resume = false;
                    }
                }

            }

            if (resume)
            {
                if (id > 0)
                {
                    if (resume)
                    {
                        var mes = status == 4 ? "закончить" : "начать";
                        var dw = new DialogWindow($"Вы действительно хотите {mes} производственное задание №{num}?", "Работа с ПЗ", "", DialogWindowButtons.NoYes);
                        if (dw.ShowDialog() == true)
                        {
                            var q = new LPackClientQuery();
                            q.Request.SetParam("Module", "MoldedContainer");
                            q.Request.SetParam("Object", "Recycling");
                            q.Request.SetParam("Action", "Save");

                            if ((status < 4) || (status == 8))
                            {
                                q.Request.SetParam("TASK_ID", id.ToString());
                            }
                            else
                            {
                                q.Request.SetParam("TASK_ID", "");
                            }

                            q.Request.SetParam("PRODUCTION_MACHINE_ID", ProductionTaskGrid.SelectedItem.CheckGet("MACHINE_ID").ToInt().ToString());

                            await Task.Run(() =>
                            {
                                q.DoQuery();
                            });

                            if (q.Answer.Status == 0)
                            {
                                // устанавливаем статус 4- в работе
                                if ((status < 4) || (status == 8))
                                {
                                    q = new LPackClientQuery();
                                    q.Request.SetParam("Module", "MoldedContainer");
                                    q.Request.SetParam("Object", "Recycling");
                                    q.Request.SetParam("Action", "TaskStatusSave");

                                    var p = new Dictionary<string, string>();
                                    {
                                        p.Add("TASK_ID", id.ToString());
                                        p.Add("PRTS_ID", "4");
                                        p.Add("SUSPEND_NOTE", ProductionTaskGrid.SelectedItem.CheckGet("SUSPEND_NOTE").ToString());
                                    }

                                    q.Request.SetParams(p);

                                    await Task.Run(() =>
                                    {
                                        q.DoQuery();
                                    });

                                    if (q.Answer.Status != 0)
                                    {
                                        //q.ProcessError();
                                        var error = q.GetError();
                                        LogMsg($"Ошибка при смене статуса задания {error}");
                                    }
                                }
                            }
                            else
                            {
                                q.ProcessError();
                            }
                        }
                        Refresh();
                    }
                }
            }
        }

        /// <summary>
        /// Приостановить текущее активное ПЗ
        /// </summary>
        public async void SuspendProductionTask()
        {
            var h = new RecyclingPtoductionTaskSuspend();
            h.Values.CheckAdd("TASK_ID", ProductionTaskGrid.SelectedItem.CheckGet("TASK_ID"));
            h.Values.CheckAdd("SUSPEND_NOTE", ProductionTaskGrid.SelectedItem.CheckGet("SUSPEND_NOTE"));
            h.ReceiverName = ControlName;
            h.Create();
        }

        /// <summary>
        /// Проверка наличия не оприходованных поддонов по выбранному производственному заданию.
        /// Если есть не отсканированные поддоны, вернёт true.
        /// </summary>
        /// <returns></returns>
        public bool CheckReceivedPalletByTask()
        {
            bool existNotReceivedPallet = false;

            var p = new Dictionary<string, string>();
            {
                p.Add("ID_PZ", ProductionTaskGrid.SelectedItem.CheckGet("TASK_ID2").ToInt().ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Recycling");
            q.Request.SetParam("Action", "TaskInfo");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        var t = "Внимание информация!";
                        var m = $"Для текущего задания №{ProductionTaskGrid.SelectedItem.CheckGet("TASK_NUMBER").ToString()}";
                        m += $"\nесть не оприходованные паллеты.";
                        m += $"\n Необходимо их провести или удалить.";
                        var i = new ErrorTouch();
                        i.Show(t, m);
                        existNotReceivedPallet = true;
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            return existNotReceivedPallet;
        }

        public void OpenTechnologicalMap(string filePath)
        {
            try
            {
                Central.OpenFile(filePath);
            }
            catch (Exception ex)
            {
            }
        }

        /// <summary>
        /// удаляем созданную, но еще не оприходованную паллету с готовой продукцией
        /// </summary>
        public async void DeletePallet()
        {
            var id = ArrivalPalletGrid.SelectedItem.CheckGet("PALLET_ID").ToInt();
            if (id > 0)
            {
                var dw = new DialogWindow("Вы действительно хотите удалить паллету?", "Удаление паллеты", "Подтверждение удаления.", DialogWindowButtons.NoYes);
                if (dw.ShowDialog() == true)
                {
                    var p = new Dictionary<string, string>();
                    {
                        p.Add("PALLET_ID", id.ToString());
                    }

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "MoldedContainer");
                    q.Request.SetParam("Object", "Recycling");
                    q.Request.SetParam("Action", "DeletePallet");
                    q.Request.SetParams(p);

                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    if (q.Answer.Status == 0)
                    {
                        Refresh();
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
            }
        }

        /// <summary>
        /// вызов формы для создания паллеты по текущему ПЗ
        /// </summary>
        public void CreatePallet()
        {
            var h = new RecyclingPalletCreate();
            h.Values.CheckAdd("TASK_ID", ProductionTaskGrid.SelectedItem.CheckGet("TASK_ID"));
            h.Values.CheckAdd("GOODS_NAME", ProductionTaskGrid.SelectedItem.CheckGet("GOODS_NAME"));
            h.Values.CheckAdd("MACHINE_ID", Machines.SelectedItem.Key.ToInt().ToString());
            h.ReceiverName = ControlName;
            h.Edit();
        }

        /// <summary>
        /// Установка настроек для принтера
        /// </summary>
        public void SetPrintSettings()
        {
            LabelReport2.SetPrintingProfile();
        }

        public void PrintPallet()
        {
            LabelReport2 report = new LabelReport2(true);
            report.PrintLabel(ArrivalPalletGrid.SelectedItem.CheckGet("PALLET_ID").ToInt().ToString());
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            SetPrintSettings();
        }

        private void Machines_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Refresh();
        }


        /// <summary>
        /// списание заготовок на станке 331 (упаковщик)
        /// </summary>
        /// <param name="str"></param>
        private void PalletConsumptionHand(int q, int id_poddon)
        {

            // вызываем ввод количества заготовок для списания
            var h = new RecyclingPalletConsumption();
            h.Values.CheckAdd("TASK_ID", ProductionTaskGrid.SelectedItem.CheckGet("TASK_ID"));
            h.Values.CheckAdd("QUANTITY", q.ToString());
            h.Values.CheckAdd("ID_PODDON", id_poddon.ToString());
            h.ReceiverName = ControlName;
            h.Edit();

        }

        /// <summary>
        /// информация о паллете (списывать как заготовку или оприходовать как готовую продукцию)
        /// </summary>
        private void InfoPallet(string str)
        {
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("ID_PODDON", str);
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "MoldedContainer");
                q.Request.SetParam("Object", "Recycling");
                q.Request.SetParam("Action", "PalletInfo");
                q.Request.SetParams(p);

                q.Request.Timeout = 5000;
                q.Request.Attempts = 1;

                //await Task.Run(() =>
                //{
                q.DoQuery();
                //});

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var dataSet = ListDataSet.Create(result, "ITEMS");
                        if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                        {
                            // на каком станке была создана паллета
                            var idSt = dataSet.Items.First().CheckGet("ID_ST").ToInt();
                            // название паллеты
                            var palletName = dataSet.Items.First().CheckGet("NAME_PALLET").ToString();

                            // это станки для заготовок
                            if ((idSt == 301) || (idSt == 302) || (idSt == 303) || (idSt == 304) || (idSt == 305) || (idSt == 306) || (idSt == 714))
                            {
                                // получаем текущие Id_pz и на каком станке производят изделия
                                GurrentTask();
                                if ((RecyclingGridIdPz > 0) && (Machines.SelectedItem.Key.ToInt() > 0))
                                {
                                    // это Упаковщик ЛТ
                                    //if (Machines.SelectedItem.Key.ToString() == "331")
                                    //{
                                    //    // сколько штук заготовок на паллете
                                    //    var qty = dataSet.Items.First().CheckGet("KOL").ToInt();
                                    //    PalletConsumptionHand(qty, str.ToInt());
                                    //}
                                    //else
                                    {
                                        // проверяем, эта заготовка для этого задания
                                        if (dataSet.Items.First().CheckGet("ID2").ToInt() == RecyclingGridBlankId2)
                                        {
                                            var p2 = new Dictionary<string, string>();
                                            {
                                                p2.Add("PALLET_ID", str);
                                                p2.Add("ID_PZ", RecyclingGridIdPz.ToString());
                                                p2.Add("ID_ST", Machines.SelectedItem.Key.ToInt().ToString());
                                                p2.Add("PALLET_NUMBER_CUSTOM", palletName);
                                                p2.Add("I_QTY", dataSet.Items.First().CheckGet("KOL").ToInt().ToString());
                                            }
                                            // списываем паллету с заготовкой
                                            СonsumptionPallet(p2);
                                        }
                                        else
                                        {
                                            string msg = $"Для текущего задания должен быть другой цвет полуфабриката.";
                                            var d = new DialogTouch($"{msg}", "Информация", "", DialogWindowButtons.OKAutohide);
                                        }
                                    }
                                }
                                else
                                {
                                    string st = Machines.SelectedItem.ToString();
                                    string msg = $"Для выбранного станка {st} нет активного задания";
                                    var d = new DialogTouch($"{msg}", "Информация", "", DialogWindowButtons.OKAutohide);
                                }
                            }
                            else if ((idSt == 311) || (idSt == 321) || (idSt == 312) || (idSt == 322) || (idSt == 331)) // принтеры,этикетеры и упаковщик
                            {
                                // получаем текущие Id_pz и на каком станке производят изделия
                                GurrentTask();

                                if (Machines.SelectedItem.Key.ToInt() != idSt)
                                {
                                    string msg = $"Данный ярлык распечатан для другого станка.";
                                    var d = new DialogTouch($"{msg}", "Информация", "", DialogWindowButtons.OKAutohide);
                                }
                                else
                                if ((RecyclingGridIdPz > 0) && (Machines.SelectedItem.Key.ToInt() > 0))
                                {
                                    var p2 = new Dictionary<string, string>();
                                    {
                                        p2.Add("PALLET_ID", str);
                                        p2.Add("PALLET_NUMBER_CUSTOM", palletName);
                                    }

                                    // оприходуем паллету
                                    ArrivialPallet(p2);
                                }
                                else
                                {
                                    string st = Machines.SelectedItem.ToString();
                                    string msg = $"Для выбранного станка {st} нет активного задания";
                                    var d = new DialogTouch($"{msg}", "Информация", "", DialogWindowButtons.OKAutohide);
                                }
                            }
                        }
                        else
                        {
                            string msg = $"Нет данных о паллете ШК {str}";
                            msg += $"\nВозможно паллета была удалена, а ярлык остался.";
                            var d = new DialogTouch($"{msg}", "Информация", "", DialogWindowButtons.OKAutohide);
                        }
                    }
                    else
                    {
                        string msg = q.Answer.Error.Message;
                        var d = new DialogTouch($"{msg}", "Информация", "", DialogWindowButtons.OKAutohide);
                    }
                    Refresh();
                }

            }
        }

        /// <summary>
        /// получаем наличие начатого задания для выбранного станка
        /// </summary>
        private bool GurrentTask()
        {
            var res = false;
            if (Machines != null && Machines.SelectedItem.Key != null)
            {
                RecyclingGridIdPz = 0;
                RecyclingGridProtId = 0;
                RecyclingGridId2 = 0;
                RecyclingGridBlankId2 = 0;

                var p = new Dictionary<string, string>();
                {
                    p.Add("MACHINE_ID", Machines.SelectedItem.Key.ToInt().ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "MoldedContainer");
                q.Request.SetParam("Object", "Recycling");
                q.Request.SetParam("Action", "GetCurrentTask");

                q.Request.SetParams(p);

                q.Request.Timeout = 5000;
                q.Request.Attempts = 1;

                //            await Task.Run(() =>
                //            {
                q.DoQuery();
                //            });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var dataSet = ListDataSet.Create(result, "ITEMS");
                        if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                        {
                            if (dataSet.Items.First().CheckGet("PRODUCTION_TASK_ID").ToInt() != 0)
                            {
                                RecyclingGridProtId = dataSet.Items.First().CheckGet("PRODUCTION_TASK_ID").ToInt();
                                RecyclingGridIdPz = dataSet.Items.First().CheckGet("PRODUCTION_TASK2_ID").ToInt();
                                RecyclingGridId2 = dataSet.Items.First().CheckGet("GOODS_ID").ToInt();
                                RecyclingGridBlankId2 = dataSet.Items.First().CheckGet("BLANK_ID2").ToInt();

                                res = true;
                            }
                        }
                    }
                }

            }

            return res;
        }

        /// <summary>
        /// списание паллеты с заготовками для Принтера и Этикетера
        /// </summary>
        private void СonsumptionPallet(Dictionary<string, string> p)
        {
            var palleta = p.CheckGet("PALLET_ID").ToString();
            var palletaName = p.CheckGet("PALLET_NUMBER_CUSTOM").ToString();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Recycling");
            q.Request.SetParam("Action", "PalletСonsumption");
            q.Request.SetParams(p);

            q.Request.Timeout = 5000;
            q.Request.Attempts = 1;

            //await Task.Run(() =>
            //{
            q.DoQuery();
            //});

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var dataSet = ListDataSet.Create(result, "ITEMS");
                    if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                    {
                        if (dataSet.Items.First().CheckGet("ID").ToInt() == 0)
                        {
                            // паллета списана успешно
                            string msg = $"Паллета [{palletaName}] списана успешно!{Environment.NewLine}.";
                            int status = 2;
                            var d = new StackerScanedLableInfo($"{msg}", status);
                            d.WindowMaxSizeFlag = true;
                            d.ShowAndAutoClose(2);
                        }
                        else
                        {
                            // ошибка списания паллеты
                            string msg = $"Ошибка списания паллеты [{palletaName}] !{Environment.NewLine}.";
                            int status = 2;
                            var d = new StackerScanedLableInfo($"{msg}", status);
                            d.WindowMaxSizeFlag = true;
                            d.ShowAndAutoClose(2);
                        }
                    }
                }
            }
            else if (q.Answer.Status == 145)
            {
                string msg = q.Answer.Error.Message;
                int status = 1;
                var d = new StackerScanedLableInfo(msg, status);
                d.WindowMaxSizeFlag = true;
                d.ShowAndAutoClose(2);
            }
        }

        /// <summary>
        /// оприходование паллеты для текущего ПЗ
        /// </summary>
        private void ArrivialPallet(Dictionary<string, string> p)
        {
            var palleta = p.CheckGet("PALLET_ID").ToString();
            var palletaName = p.CheckGet("PALLET_NUMBER_CUSTOM").ToString();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Recycling");
            q.Request.SetParam("Action", "PalletArrivial");
            q.Request.SetParams(p);

            q.Request.Timeout = 5000;
            q.Request.Attempts = 1;

            //await Task.Run(() =>
            //{
            q.DoQuery();
            //});

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var dataSet = ListDataSet.Create(result, "ITEMS");
                    if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                    {
                        if (dataSet.Items.First().CheckGet("ID").ToInt() == 0)
                        {
                            // паллет оприходован успешно
                            string msg = $"Паллета [{palletaName}] оприходована успешно!{Environment.NewLine}.";
                            int status = 2;
                            var d = new StackerScanedLableInfo($"{msg}", status);
                            d.WindowMaxSizeFlag = true;
                            d.ShowAndAutoClose(2);
                        }
                    }
                }
            }
            else if (q.Answer.Status == 145)
            {
                string msg = q.Answer.Error.Message;
                int status = 1;
                var d = new StackerScanedLableInfo(msg, status);
                d.WindowMaxSizeFlag = true;
                d.ShowAndAutoClose(2);
            }
            else if (q.Answer.Status == 7)
            {
                q.ProcessError();
                var error = q.GetError();
                LogMsg($"Ошибка при оприходовании паллеты. {error}");
            }
        }

        /// <summary>
        ////отображаем список ранее выполненых ПЗ для текущего станка
        /// </summary>
        private void ShowAllProdictionTask()
        {
            buttons[3] = false;
            ShowAllPz.IsEnabled = false;
            ButtonTimer.Run();

            if (Machines != null && Machines.SelectedItem.Key != null)
            {
                var h = new RecyclingPtoductionTaskAll();
                h.IdSt = Machines.SelectedItem.Key.ToInt().ToString();
                h.Edit();

            }
            ProductionTaskGrid.LoadItems();
        }

        /// <summary>
        ////отображаем список ранее оприходованных паллет с готовой продукцией для текущего станка
        /// </summary>
        private void ShowAllArrivialPallet()
        {
            if (Machines != null && Machines.SelectedItem.Key != null)
            {
                var h = new RecyclingArrivalPalletAll();
                h.ReceiverName = ControlName;
                h.IdSt = Machines.SelectedItem.Key.ToInt().ToString();
                h.Edit();
            }

        }

        /// <summary>
        ////отображаем список ранее оприходованных паллет с готовой продукцией для текущего станка
        /// </summary>
        private void ShowConsumptionPalletAll()
        {
            if (Machines != null && Machines.SelectedItem.Key != null)
            {
                var h = new RecyclingConsumptionPalletAll();
                h.ReceiverName = ControlName;
                h.IdSt = Machines.SelectedItem.Key.ToInt().ToString();
                h.Edit();
            }

        }


        private void ButtonPush()
        {

            for (int i = 0; i < 20; i++)
            {
                if (!buttons[i])
                {
                    buttons[i] = true;
                    switch (i)
                    {
                        //Обновить    
                        case 0:
                            {
                                RefreshButton.IsEnabled = true;
                            }
                            break;

                        //Начать ПЗ
                        case 1:
                            {
                                ProductionTaskGridStartButton.IsEnabled = true;
                            }
                            break;

                        //Закончить ПЗ
                        case 2:
                            {
                                ProductionTaskGridEndButton.IsEnabled = true;
                            }
                            break;

                        // Все ПЗ
                        case 3:
                            {
                                ShowAllPz.IsEnabled = true;
                            }
                            break;

                        //Открыть ТК    
                        case 4:
                            {
                                ShowExcelButton.IsEnabled = true;
                            }
                            break;

                        //Все паллеты на списание
                        case 5:
                            {
                                ShowAllConsumptionPallet.IsEnabled = true;
                            }
                            break;

                        //Создать паллету
                        case 6:
                            {
                                PalletCreateButton.IsEnabled = true;
                            }
                            break;

                        // Удалить паллету
                        case 7:
                            {
                                PalletDeleteButton.IsEnabled = true;
                            }
                            break;
                        // печать ярлыка
                        case 8:
                            {
                                PalletPrintButton.IsEnabled = true;
                            }
                            break;
                        // Все паллеты с готовой продукцией
                        case 9:
                            {
                                ShowAllArrivalPallet.IsEnabled = true;
                            }
                            break;
                        // 
                        case 10:
                            break;

                    }
                    ButtonTimer.Finish();

                }
            }
        }

        /// <summary>
        ///  сканировать введенный ШК (для проверки/отладки)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            if (!StrihKodText.Text.IsNullOrEmpty())
                InfoPallet(StrihKodText.Text);
            StrihKodText.Text = "";
        }

        /// <summary>
        ////включаем отладку
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Debug_Click(object sender, RoutedEventArgs e)
        {
            StrihKodLabel.Visibility = Visibility;
            StrihKodText.Visibility = Visibility;
            ScanStrihKodButton.Visibility = Visibility;
        }

        /// <summary>
        ///  получаем список красок для указанного задания
        /// </summary>
        public void ProductionTaskGridColorLoad(int prot_Id)
        {
            {
                var p = new Dictionary<string, string>();
                p.Add("PROT_ID", prot_Id.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "MoldedContainer");
                q.Request.SetParam("Object", "Recycling");
                q.Request.SetParam("Action", "TaskColor");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                //                await Task.Run(() =>
                {
                    q.DoQuery();
                }
                //                );

                ProductionTaskDataSetColor = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        ProductionTaskDataSetColor = ListDataSet.Create(result, "ITEMS");
                    }
                }
                else
                {
                    // q.ProcessError();
                }
            }
        }

        /// <summary>
        /// Функция перевода строки содержащей hex код цвета краски в цвет Brush
        /// <param name="hex_code">строка с hex числом</param>
        /// <return>Brush.цвет</return>
        /// </summary>
        private Brush HexToBrush(string hex_code)
        {
            SolidColorBrush result = null;
            var hexString = (hex_code as string).Replace("#", "");

            if (hexString.Length == 6)
            {
                var r = hexString.Substring(0, 2);
                var g = hexString.Substring(2, 2);
                var b = hexString.Substring(4, 2);

                result = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0xff,
                   byte.Parse(r, System.Globalization.NumberStyles.HexNumber),
                   byte.Parse(g, System.Globalization.NumberStyles.HexNumber),
                   byte.Parse(b, System.Globalization.NumberStyles.HexNumber)));
            }

            return result;
        }

        private void ColorTask()
        {
            PROT_ID1.Text = "";
            FRONT_BOARD_1.Text = "";
            FRONT_BOARD_1.Background = null;
            FRONT_BOARD_2.Text = "";
            FRONT_BOARD_2.Background = null;
            FRONT_BOARD_3.Text = "";
            FRONT_BOARD_3.Background = null;
            FRONT_BOARD_4.Text = "";
            FRONT_BOARD_4.Background = null;
            CAP_1.Text = "";
            CAP_1.Background = null;
            CAP_2.Text = "";
            CAP_2.Background = null;
            CAP_3.Text = "";
            CAP_3.Background = null;
            CAP_4.Text = "";
            CAP_4.Background = null;
            TAILGATE_1.Text = "";
            TAILGATE_1.Background = null;
            TAILGATE_2.Text = "";
            TAILGATE_2.Background = null;
            TAILGATE_3.Text = "";
            TAILGATE_3.Background = null;
            TAILGATE_4.Text = "";
            TAILGATE_4.Background = null;
            INSIDE_1.Text = "";
            INSIDE_1.Background = null;
            INSIDE_2.Text = "";
            INSIDE_2.Background = null;

            PROT_ID2.Text = "";
            FRONT_BOARD2_1.Text = "";
            FRONT_BOARD2_1.Background = null;
            FRONT_BOARD2_2.Text = "";
            FRONT_BOARD2_2.Background = null;
            FRONT_BOARD2_3.Text = "";
            FRONT_BOARD2_3.Background = null;
            FRONT_BOARD2_4.Text = "";
            FRONT_BOARD2_4.Background = null;
            CAP2_1.Text = "";
            CAP2_1.Background = null;
            CAP2_2.Text = "";
            CAP2_2.Background = null;
            CAP2_3.Text = "";
            CAP2_3.Background = null;
            CAP2_4.Text = "";
            CAP2_4.Background = null;
            TAILGATE2_1.Text = "";
            TAILGATE2_1.Background = null;
            TAILGATE2_2.Text = "";
            TAILGATE2_2.Background = null;
            TAILGATE2_3.Text = "";
            TAILGATE2_3.Background = null;
            TAILGATE2_4.Text = "";
            TAILGATE2_4.Background = null;
            INSIDE2_1.Text = "";
            INSIDE2_1.Background = null;
            INSIDE2_2.Text = "";
            INSIDE2_2.Background = null;

            PROT_ID3.Text = "";
            FRONT_BOARD3_1.Text = "";
            FRONT_BOARD3_1.Background = null;
            FRONT_BOARD3_2.Text = "";
            FRONT_BOARD3_2.Background = null;
            FRONT_BOARD3_3.Text = "";
            FRONT_BOARD3_3.Background = null;
            FRONT_BOARD3_4.Text = "";
            FRONT_BOARD3_4.Background = null;
            CAP3_1.Text = "";
            CAP3_1.Background = null;
            CAP3_2.Text = "";
            CAP3_2.Background = null;
            CAP3_3.Text = "";
            CAP3_3.Background = null;
            CAP3_4.Text = "";
            CAP3_4.Background = null;
            TAILGATE3_1.Text = "";
            TAILGATE3_1.Background = null;
            TAILGATE3_2.Text = "";
            TAILGATE3_2.Background = null;
            TAILGATE3_3.Text = "";
            TAILGATE3_3.Background = null;
            TAILGATE3_4.Text = "";
            TAILGATE3_4.Background = null;
            INSIDE3_1.Text = "";
            INSIDE3_1.Background = null;
            INSIDE3_2.Text = "";
            INSIDE3_2.Background = null;

            // идем сверху вниз от первой записи
            foreach (var item in ProductionTaskGrid.Items)
            {
                if (item != null)
                {
                    var row = item.CheckGet("_ROWNUMBER").ToInt();
                    var protId = item.CheckGet("TASK_ID").ToInt();
                    if (row == 1)
                    {
                        ProductionTaskGridColorLoad(protId);

                        var first = ProductionTaskDataSetColor.Items.FirstOrDefault();

                        PROT_ID1.Text = first.CheckGet("TASK_ID").ToInt().ToString();

                        FRONT_BOARD_1.Text = first.CheckGet("FRONT_BOARD_1");
                        FRONT_BOARD_1.Background = HexToBrush(first.CheckGet("FRONT_BOARD_1_HEX").ToString());
                        FRONT_BOARD_2.Text = first.CheckGet("FRONT_BOARD_2");
                        FRONT_BOARD_2.Background = HexToBrush(first.CheckGet("FRONT_BOARD_2_HEX").ToString());
                        FRONT_BOARD_3.Text = first.CheckGet("FRONT_BOARD_3");
                        FRONT_BOARD_3.Background = HexToBrush(first.CheckGet("FRONT_BOARD_3_HEX").ToString());
                        FRONT_BOARD_4.Text = first.CheckGet("FRONT_BOARD_4");
                        FRONT_BOARD_4.Background = HexToBrush(first.CheckGet("FRONT_BOARD_4_HEX").ToString());

                        CAP_1.Text = first.CheckGet("CAP_1");
                        CAP_1.Background = HexToBrush(first.CheckGet("CAP_1_HEX").ToString());
                        CAP_2.Text = first.CheckGet("CAP_2");
                        CAP_2.Background = HexToBrush(first.CheckGet("CAP_2_HEX").ToString());
                        CAP_3.Text = first.CheckGet("CAP_3");
                        CAP_3.Background = HexToBrush(first.CheckGet("CAP_3_HEX").ToString());
                        CAP_4.Text = first.CheckGet("CAP_4");
                        CAP_4.Background = HexToBrush(first.CheckGet("CAP_4_HEX").ToString());

                        TAILGATE_1.Text = first.CheckGet("TAILGATE_1");
                        TAILGATE_1.Background = HexToBrush(first.CheckGet("TAILGATE_1_HEX").ToString());
                        TAILGATE_2.Text = first.CheckGet("TAILGATE_2");
                        TAILGATE_2.Background = HexToBrush(first.CheckGet("TAILGATE_2_HEX").ToString());
                        TAILGATE_3.Text = first.CheckGet("TAILGATE_3");
                        TAILGATE_3.Background = HexToBrush(first.CheckGet("TAILGATE_3_HEX").ToString());
                        TAILGATE_4.Text = first.CheckGet("TAILGATE_4");
                        TAILGATE_4.Background = HexToBrush(first.CheckGet("TAILGATE_4_HEX").ToString());

                        INSIDE_1.Text = first.CheckGet("INSIDE_1");
                        INSIDE_1.Background = HexToBrush(first.CheckGet("INSIDE_1_HEX").ToString());

                        INSIDE_2.Text = first.CheckGet("INSIDE_2");
                        INSIDE_2.Background = HexToBrush(first.CheckGet("INSIDE_2_HEX").ToString());
                    }
                    else if (row == 2)
                    {
                        ProductionTaskGridColorLoad(protId);

                        var first = ProductionTaskDataSetColor.Items.FirstOrDefault();

                        PROT_ID2.Text = first.CheckGet("TASK_ID").ToInt().ToString();

                        FRONT_BOARD2_1.Text = first.CheckGet("FRONT_BOARD_1");
                        FRONT_BOARD2_1.Background = HexToBrush(first.CheckGet("FRONT_BOARD_1_HEX").ToString());
                        FRONT_BOARD2_2.Text = first.CheckGet("FRONT_BOARD_2");
                        FRONT_BOARD2_2.Background = HexToBrush(first.CheckGet("FRONT_BOARD_2_HEX").ToString());
                        FRONT_BOARD2_3.Text = first.CheckGet("FRONT_BOARD_3");
                        FRONT_BOARD2_3.Background = HexToBrush(first.CheckGet("FRONT_BOARD_3_HEX").ToString());
                        FRONT_BOARD2_4.Text = first.CheckGet("FRONT_BOARD_4");
                        FRONT_BOARD2_4.Background = HexToBrush(first.CheckGet("FRONT_BOARD_4_HEX").ToString());

                        CAP2_1.Text = first.CheckGet("CAP_1");
                        CAP2_1.Background = HexToBrush(first.CheckGet("CAP_1_HEX").ToString());
                        CAP2_2.Text = first.CheckGet("CAP_2");
                        CAP2_2.Background = HexToBrush(first.CheckGet("CAP_2_HEX").ToString());
                        CAP2_3.Text = first.CheckGet("CAP_3");
                        CAP2_3.Background = HexToBrush(first.CheckGet("CAP_3_HEX").ToString());
                        CAP2_4.Text = first.CheckGet("CAP_4");
                        CAP2_4.Background = HexToBrush(first.CheckGet("CAP_4_HEX").ToString());

                        TAILGATE2_1.Text = first.CheckGet("TAILGATE_1");
                        TAILGATE2_1.Background = HexToBrush(first.CheckGet("TAILGATE_1_HEX").ToString());
                        TAILGATE2_2.Text = first.CheckGet("TAILGATE_2");
                        TAILGATE2_2.Background = HexToBrush(first.CheckGet("TAILGATE_2_HEX").ToString());
                        TAILGATE2_3.Text = first.CheckGet("TAILGATE_3");
                        TAILGATE2_3.Background = HexToBrush(first.CheckGet("TAILGATE_3_HEX").ToString());
                        TAILGATE2_4.Text = first.CheckGet("TAILGATE_4");
                        TAILGATE2_4.Background = HexToBrush(first.CheckGet("TAILGATE_4_HEX").ToString());

                        INSIDE2_1.Text = first.CheckGet("INSIDE_1");
                        INSIDE2_1.Background = HexToBrush(first.CheckGet("INSIDE_1_HEX").ToString());

                        INSIDE2_2.Text = first.CheckGet("INSIDE_2");
                        INSIDE2_2.Background = HexToBrush(first.CheckGet("INSIDE_2_HEX").ToString());
                    }
                    else if (row == 3)
                    {
                        ProductionTaskGridColorLoad(protId);

                        var first = ProductionTaskDataSetColor.Items.FirstOrDefault();

                        PROT_ID3.Text = first.CheckGet("TASK_ID").ToInt().ToString();

                        FRONT_BOARD3_1.Text = first.CheckGet("FRONT_BOARD_1");
                        FRONT_BOARD3_1.Background = HexToBrush(first.CheckGet("FRONT_BOARD_1_HEX").ToString());
                        FRONT_BOARD3_2.Text = first.CheckGet("FRONT_BOARD_2");
                        FRONT_BOARD3_2.Background = HexToBrush(first.CheckGet("FRONT_BOARD_2_HEX").ToString());
                        FRONT_BOARD3_3.Text = first.CheckGet("FRONT_BOARD_3");
                        FRONT_BOARD3_3.Background = HexToBrush(first.CheckGet("FRONT_BOARD_3_HEX").ToString());
                        FRONT_BOARD3_4.Text = first.CheckGet("FRONT_BOARD_4");
                        FRONT_BOARD3_4.Background = HexToBrush(first.CheckGet("FRONT_BOARD_4_HEX").ToString());

                        CAP3_1.Text = first.CheckGet("CAP_1");
                        CAP3_1.Background = HexToBrush(first.CheckGet("CAP_1_HEX").ToString());
                        CAP3_2.Text = first.CheckGet("CAP_2");
                        CAP3_2.Background = HexToBrush(first.CheckGet("CAP_2_HEX").ToString());
                        CAP3_3.Text = first.CheckGet("CAP_3");
                        CAP3_3.Background = HexToBrush(first.CheckGet("CAP_3_HEX").ToString());
                        CAP3_4.Text = first.CheckGet("CAP_4");
                        CAP3_4.Background = HexToBrush(first.CheckGet("CAP_4_HEX").ToString());

                        TAILGATE3_1.Text = first.CheckGet("TAILGATE_1");
                        TAILGATE3_1.Background = HexToBrush(first.CheckGet("TAILGATE_1_HEX").ToString());
                        TAILGATE3_2.Text = first.CheckGet("TAILGATE_2");
                        TAILGATE3_2.Background = HexToBrush(first.CheckGet("TAILGATE_2_HEX").ToString());
                        TAILGATE3_3.Text = first.CheckGet("TAILGATE_3");
                        TAILGATE3_3.Background = HexToBrush(first.CheckGet("TAILGATE_3_HEX").ToString());
                        TAILGATE3_4.Text = first.CheckGet("TAILGATE_4");
                        TAILGATE3_4.Background = HexToBrush(first.CheckGet("TAILGATE_4_HEX").ToString());

                        INSIDE3_1.Text = first.CheckGet("INSIDE_1");
                        INSIDE3_1.Background = HexToBrush(first.CheckGet("INSIDE_1_HEX").ToString());

                        INSIDE3_2.Text = first.CheckGet("INSIDE_2");
                        INSIDE3_2.Background = HexToBrush(first.CheckGet("INSIDE_2_HEX").ToString());
                    }
                    else
                    {
                        break;
                    }
                }
            }

        }

        /// <summary>
        /// Закрытие текущего активного ПЗ (выполнено не полностью)
        /// </summary>
        public async void CloseProductionTask()
        {
            var h = new RecyclingPtoductionTaskClose();
            h.Values.CheckAdd("TASK_ID", ProductionTaskGrid.SelectedItem.CheckGet("TASK_ID"));
            h.Values.CheckAdd("ORDER_NOTE_GENERAL", ProductionTaskGrid.SelectedItem.CheckGet("ORDER_NOTE_GENERAL"));
            h.Values.CheckAdd("SUSPEND_NOTE", ProductionTaskGrid.SelectedItem.CheckGet("SUSPEND_NOTE"));
            h.Values.CheckAdd("TASK_STATUS_ID", ProductionTaskGrid.SelectedItem.CheckGet("TASK_STATUS_ID"));
            h.Values.CheckAdd("TASK_NUMBER", ProductionTaskGrid.SelectedItem.CheckGet("TASK_NUMBER"));
            h.Values.CheckAdd("TASK_QUANTITY", ProductionTaskGrid.SelectedItem.CheckGet("TASK_QUANTITY"));
            h.Values.CheckAdd("PRIHOD_QTY", ProductionTaskGrid.SelectedItem.CheckGet("PRIHOD_QTY"));
            h.Values.CheckAdd("PRODUCTION_MACHINE_ID", ProductionTaskGrid.SelectedItem.CheckGet("MACHINE_ID"));

            h.ReceiverName = ControlName;
            h.Create();
        }


        /// <summary>
        /// Загрузить список станков и количество заданий по ним
        /// </summary>
        private void LoadListMachine()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Recycling");
            q.Request.SetParam("Action", "ListTaskStanok");
            
            q.Request.Timeout = 5000;
            q.Request.Attempts = 1;

            //await Task.Run(() =>
            //{
            q.DoQuery();
            //});

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var key_cur =  Machines.SelectedItem.Key;
                    var dataSet = ListDataSet.Create(result, "ITEMS");
                    if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                    {
                        var list = dataSet.GetItemsList("MACHINE_ID", "MACHINE_NAME");
                        Machines.Items = list;
                        Machines.SetSelectedItemByKey(key_cur);
                    }
                }
            }
        }


        ///

    }
}
