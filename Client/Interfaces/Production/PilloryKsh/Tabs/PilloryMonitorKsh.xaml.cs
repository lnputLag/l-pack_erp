using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Production.Pillory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Монитор позорного столба Кашира
    /// </summary>
    public partial class PilloryMonitorKsh : ControlBase
    {
        public PilloryMonitorKsh()
        {
            ControlTitle = "Монитор мастера КШ";
            RoleName = "[erp]pillory_ksh";
            DocumentationUrl = "/doc/l-pack-erp-new/recycling/pillory";
            InitializeComponent();

            if (Central.DebugMode)
            {
                RePlaceBlocks.Visibility = Visibility.Visible;
            }
            else
            {
                RePlaceBlocks.Visibility = Visibility.Collapsed;
            }

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }

                if (m.ReceiverGroup == "All")
                {
                    if (m.SenderName == "Central")
                    {
                        if (m.Action == "StartUp")
                        {
                            // При открытии интерфейса через AutoloadInterfaces в момент обработки параметров навигации Central.MainWindow ещё не существует,
                            // чтобы обработать выбор монитора через параметры навигации вызовем установку монитора после получения сообщения о том, что Central.MainWindow создан

                            SetMonitorByMonitorSelectBoxSelectedItem();
                        }
                    }
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
                SetDefaultMonitor();
                InitMachineList();
                SetDefaultPage();
                LoadItems();
                RunAutoUpdateTimer();
                RunFireStatusTimer();
                PalletByPlaceCheck();
                RunPalletByPlaceTimer();
                RunGlueStatusTimer();
                RunPlanCountingStatusTimer();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                // Важные таймеры
                {
                    if (FireStatusTimer != null)
                    {
                        FireStatusTimer.Stop();
                    }
                }

                // Обычные таймеры
                {
                    if (AutoUpdateTimer != null)
                    {
                        AutoUpdateTimer.Stop();
                    }

                    if (PalletByPlaceTimer != null)
                    {
                        PalletByPlaceTimer.Stop();
                    }

                    if (GlueStatusTimer != null)
                    {
                        GlueStatusTimer.Stop();
                    }

                    if (Clock != null)
                    {
                        Clock.Stop();
                    }

                    if (PlanCountingStatusTimer != null)
                    {
                        PlanCountingStatusTimer.Stop();
                    }
                }

                // Ситуативные таймеры
                {
                    if (GlueVisibleTimer != null)
                    {
                        GlueVisibleTimer.Stop();
                    }

                    if (FireVisibleTimer != null)
                    {
                        FireVisibleTimer.Stop();
                    }
                }
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                // Обычные таймеры
                {
                    AutoUpdateTimer.Start();

                    PalletByPlaceTimer.Start();

                    GlueStatusTimer.Start();

                    Clock.Start();

                    PlanCountingStatusTimer.Start();
                }
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                //// Обычные таймеры
                //{
                //    if (AutoUpdateTimer != null)
                //    {
                //        AutoUpdateTimer.Stop();
                //    }

                //    if (PalletByPlaceTimer != null)
                //    {
                //        PalletByPlaceTimer.Stop();
                //    }

                //    if (GlueStatusTimer != null)
                //    {
                //        GlueStatusTimer.Stop();
                //    }

                //    if (Clock != null)
                //    {
                //        Clock.Stop();
                //    }

                //    if (PlanCountingStatusTimer != null)
                //    {
                //        PlanCountingStatusTimer.Stop();
                //    }
                //}

                //// Ситуативные таймеры
                //{
                //    if (GlueVisibleTimer != null)
                //    {
                //        GlueVisibleTimer.Stop();
                //    }
                //}
            };

            Commander.Add(new CommandItem()
            {
                Name = "change_machine_refresh",
                Group = "main",
                Enabled = true,
                ButtonUse = false,
                MenuUse = false,
                ActionMessage = (ItemMessage message) =>
                {
                    if (message.ContextObject is ChangeMachineDataStruct)
                    {
                        SetPlanCountingFlag(true);

                        try
                        {
                            ChangeMachineDataStruct changeMachineDataStruct = (ChangeMachineDataStruct)message.ContextObject;
                            if (changeMachineDataStruct.MachineIdOldList != null && changeMachineDataStruct.MachineIdOldList.Count > 0)
                            {
                                if (changeMachineDataStruct.MachineIdNewList != null && changeMachineDataStruct.MachineIdNewList.Count > 0)
                                {
                                    for (int i = 0; i < changeMachineDataStruct.MachineIdOldList.Count; i++)
                                    {
                                        int machineOldId = changeMachineDataStruct.MachineIdOldList[i].ToInt();
                                        if (machineOldId > 0)
                                        {
                                            if (changeMachineDataStruct.MachineIdNewList.Count >= i + 1)
                                            {
                                                int machineNewId = changeMachineDataStruct.MachineIdNewList[i].ToInt();
                                                if (machineOldId != machineNewId)
                                                {
                                                    var oldMachineItemList = MachineList[machineOldId].GridDataSet.Items;
                                                    var newMachineItemList = MachineList[machineNewId].GridDataSet.Items;

                                                    int maxNewMachineItemRownumber = newMachineItemList.Max(x => x.CheckGet("_ROWNUMBER").ToInt());

                                                    var changedItemList = oldMachineItemList.Where(x =>
                                                        x.CheckGet("ORDER_POSITION_ID").ToInt() == changeMachineDataStruct.OrderPositionId
                                                        ||
                                                        (x.CheckGet("BLANK_PRODUCTION_TASK_ID").ToInt() == changeMachineDataStruct.BlankProductionTaskId
                                                        && x.CheckGet("BLANK_PRODUCT_ID").ToInt() == changeMachineDataStruct.BlankProductId)
                                                    ).ToList();

                                                    for (int j = 0; j < changedItemList.Count; j++)
                                                    {
                                                        oldMachineItemList.Remove(changedItemList[i]);
                                                        changedItemList[i].CheckAdd("_ROWNUMBER", $"{maxNewMachineItemRownumber + j + 1}");
                                                        newMachineItemList.Add(changedItemList[i]);
                                                    }
                                                    MachineList[machineOldId].GridDataSet.Items = oldMachineItemList;
                                                    MachineList[machineNewId].GridDataSet.Items = newMachineItemList;

                                                    MachineList[machineOldId].LoadItems();
                                                    MachineList[machineNewId].LoadItems();
                                                }
                                            }
                                            else
                                            {
                                                MachineList[machineOldId].GridDataSet.Items = MachineList[machineOldId].GridDataSet.Items.Where(x =>
                                                    !
                                                    (
                                                        x.CheckGet("ORDER_POSITION_ID").ToInt() == changeMachineDataStruct.OrderPositionId
                                                        ||
                                                        (x.CheckGet("BLANK_PRODUCTION_TASK_ID").ToInt() == changeMachineDataStruct.BlankProductionTaskId
                                                        && x.CheckGet("BLANK_PRODUCT_ID").ToInt() == changeMachineDataStruct.BlankProductId)
                                                    )
                                                ).ToList();
                                                MachineList[machineOldId].LoadItems();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                },
            });
            Commander.Init(this);
        }

        /// <summary>
        /// Идентификатор площадки.
        /// </summary>
        public int FactoryId = 2;

        /// <summary>
        /// Номер монитора по умолчанию, на котором должен открываться интерфейс.
        /// Заполняем из вне.
        /// </summary>
        public int DefaultMonitorId = 0;

        /// <summary>
        /// Номер страницы по умолчанию, на окторой отображается список станков.
        /// Заполняем из вне.
        /// </summary>
        public int DefaultPageId = 0;

        /// <summary>
        /// Словарь Ид станка - Контрол
        /// </summary>
        private Dictionary<int, PilloryGrid> MachineList { get; set; }

        /// <summary>
        /// Датасет с данными по ПЗ на станках
        /// </summary>
        private ListDataSet ProductionTaskDataSet { get; set; }

        /// <summary>
        /// Датасет с данными по сводке работы станков
        /// </summary>
        private ListDataSet MachineDataSet { get; set; }

        /// <summary>
        /// Датасет с данными для столбчатых диаграмм станков
        /// </summary>
        private ListDataSet BarChartDataSet { get; set; }

        /// <summary>
        /// Датасет с данными для общих столбчатых диаграмм
        /// </summary>
        private ListDataSet MainBarChartDataSet { get; set; }

        /// <summary>
        /// Датасет с данными для общей столбчатой диаграммы
        /// </summary>
        private ListDataSet MainPieChartDataSet { get; set; }

        /// <summary>
        /// Данные по списку производственных заданий для станков (для грида).
        /// Ид станка - Лист словарей с данными
        /// </summary>
        private Dictionary<int, List<Dictionary<string, string>>> MachineIdProductionTaskData { get; set; }

        /// <summary>
        /// Данные по сводке работы станка для станков (для верхней сводки).
        /// Ид станка - Словарь с данными
        /// </summary>
        private Dictionary<int, Dictionary<string, string>> MachineIdMachineData { get; set; }

        /// <summary>
        /// Данные по загруженности для станков (для столбчатой диаграммы).
        /// Ид станка - Словарь с данными
        /// </summary>
        private Dictionary<int, Dictionary<string, string>> MachineIdBarChartData { get; set; }

        /// <summary>
        /// Данные по загруженности для общих столбчатых диаграмм.
        /// Виртуальный Ид станка - Словарь с данными.
        /// -1 -- ГА;
        /// -2 -- Лист;
        /// -3 -- СГП;
        /// -4 -- Заг-ки;
        /// </summary>
        private Dictionary<int, Dictionary<string, string>> VirtualMachineIdMainBarChartData { get; set; }

        /// <summary>
        /// Отступ между блоками в колонках грида
        /// </summary>
        private int OffsetBlocks { get; set; }

        /// <summary>
        /// Таймер получения данных
        /// </summary>
        private DispatcherTimer AutoUpdateTimer { get; set; }

        /// <summary>
        /// Флаг того, что план переработки сейчас пересчитывается. 
        /// Блокирует автообновление данных, пока не будет закончен пересчёт плана.
        /// </summary>
        private bool PlanCountingFlag { get; set; }

        private bool GlueAlarmFlag { get; set; }

        private bool FireAlarmFlag { get; set; }

        /// <summary>
        /// интервал обновления таймера получения данных (сек)
        /// </summary>
        private int AutoUpdateTimerInterval { get; set; }

        private int GridCountByWidth { get; set; }

        private int GridCountByHeight { get; set; }

        private BarChart CorrugatorBarChart { get; set; }

        private BarChart CorrugatorGoodsBarChart { get; set; }

        private BarChart StockBarChart { get; set; }

        private BarChart Blank1BarChart { get; set; }

        /// <summary>
        /// Флаг того, что пожар на этом рабочем месте
        /// </summary>
        private bool FireInCurrentPlace { get; set; }

        /// <summary>
        /// Таймер получения статуса пожара
        /// </summary>
        private DispatcherTimer FireStatusTimer { get; set; }

        /// <summary>
        /// интервал обновления таймера получения статуса пожара (сек)
        /// </summary>
        private int FireStatusTimerInterval { get; set; }

        /// <summary>
        /// Таймер отображения статуса пожара
        /// </summary>
        private DispatcherTimer FireVisibleTimer { get; set; }

        /// <summary>
        /// интервал обновления таймера отображения статуса пожара (сек)
        /// </summary>
        private int FireVisibleTimerInterval { get; set; }

        /// <summary>
        /// Таймер проверки поддонов в ячейках
        /// </summary>
        private DispatcherTimer PalletByPlaceTimer { get; set; }

        /// <summary>
        /// интервал обновления таймера проверки поддонов в ячейках (сек)
        /// </summary>
        private int PalletByPlaceTimerInterval { get; set; }

        /// <summary>
        /// Таймер получения статуса уровня клея
        /// </summary>
        private DispatcherTimer GlueStatusTimer { get; set; }

        /// <summary>
        /// интервал обновления таймера получения статуса уровня клея (сек)
        /// </summary>
        private int GlueStatusTimerInterval { get; set; }

        /// <summary>
        /// Таймер отображения статуса уровня клея
        /// </summary>
        private DispatcherTimer GlueVisibleTimer { get; set; }

        /// <summary>
        /// интервал обновления таймера отображения статуса уровня клея (сек)
        /// </summary>
        private int GlueVisibleTimerInterval { get; set; }

        /// <summary>
        /// Таймер проверки статуса пересчёта плана переработки
        /// </summary>
        private DispatcherTimer PlanCountingStatusTimer { get; set; }

        /// <summary>
        /// интервал обновления таймера проверки статуса пересчёта плана переработки
        /// </summary>
        private int PlanCountingStatusTimerInterval { get; set; }

        /// <summary>
        /// Список ячеек, поддоны в которых анализируются
        /// </summary>
        private List<Cell> CellList { get; set; }

        /// <summary>
        /// Список ячеек, поддоны в которых анализируются, в формате (Имя ячейки + Номер ячейки;)
        /// </summary>
        private string CellListString { get; set; }

        private void SetDefaults()
        {
            MachineList = new Dictionary<int, PilloryGrid>();

            ProductionTaskDataSet = new ListDataSet();
            MachineIdProductionTaskData = new Dictionary<int, List<Dictionary<string, string>>>();

            MachineDataSet = new ListDataSet();
            MachineIdMachineData = new Dictionary<int, Dictionary<string, string>>();

            BarChartDataSet = new ListDataSet();
            MachineIdBarChartData = new Dictionary<int, Dictionary<string, string>>();

            MainBarChartDataSet = new ListDataSet();
            VirtualMachineIdMainBarChartData = new Dictionary<int, Dictionary<string, string>>();

            MainPieChartDataSet = new ListDataSet();

            CorrugatorBarChart = new BarChart(-1, "ГА");
            CorrugatorGoodsBarChart = new BarChart(-2, "Лист");
            StockBarChart = new BarChart(-3, "СГП");
            Blank1BarChart = new BarChart(-4, "Заг-ки");

            OffsetBlocks = 5;

            // Важные таймеры
            {
                FireStatusTimerInterval = 10;
            }

            // Обычные таймеры
            {
                AutoUpdateTimerInterval = 60;
                PalletByPlaceTimerInterval = 60;
                GlueStatusTimerInterval = 10;
                PlanCountingStatusTimerInterval = 10;
            }

            // Ситуативные таймеры
            {
                GlueVisibleTimerInterval = 1;
                FireVisibleTimerInterval = 1;
            }

            GridCountByWidth = 3;
            if (Central.Config.PilloryGridCountByWidth > 0)
            {
                GridCountByWidth = Central.Config.PilloryGridCountByWidth;
            }
            GridCountByHeight = 4;
            if (Central.Config.PilloryGridCountByHeight > 0)
            {
                GridCountByHeight = Central.Config.PilloryGridCountByHeight;
            }

            var p = new PilloryGrid();
            MainGrid.Width = p.Width * GridCountByWidth + GridCountByWidth * OffsetBlocks + OffsetBlocks;
            MainGrid.Height = p.Height * GridCountByHeight + GridCountByHeight * OffsetBlocks + OffsetBlocks;
            p = null;

            InitCellList();

            MonitorSelectBoxLoadItems();
        }

        /// <summary>
        /// обработчик системы навигации по URL
        /// </summary>
        public void ProcessNavigation()
        {
            //параметры запуска
            var p = Central.Navigator.Address.Params;

            if (!string.IsNullOrEmpty(p.CheckGet("monitor_id")))
            {
                DefaultMonitorId = p.CheckGet("monitor_id").ToInt();
            }

            if (!string.IsNullOrEmpty(p.CheckGet("page_id")))
            {
                DefaultPageId = p.CheckGet("page_id").ToInt();
            }
        }

        #region Timer

        #region Важные таймеры
        private void RunFireStatusTimer()
        {
            if (FireStatusTimerInterval != 0)
            {
                if (FireStatusTimer == null)
                {
                    FireStatusTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0, 0, FireStatusTimerInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", FireStatusTimerInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("PilloryMonitorKsh_RunFireStatusTimer", row);
                    }

                    FireStatusTimer.Tick += (s, e) =>
                    {
                        FireAlarmCheck();
                    };
                }

                if (FireStatusTimer.IsEnabled)
                {
                    FireStatusTimer.Stop();
                }

                FireStatusTimer.Start();
            }
        }

        private async void FireAlarmCheck()
        {
            var p = new Dictionary<string, string>();
            p.Add("FIRE_NAME", "FIRE_KSH");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "IndustrialWaste");
            q.Request.SetParam("Action", "ListFire");

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
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        string firePlace = ds.Items[0]["PARAM_VALUE"];
                        if (firePlace != null && firePlace != "null")
                        {
                            FireStatus.Text = $"Пожар! {firePlace}";

                            if (!FireAlarmFlag)
                            {
                                FireStatus.Visibility = Visibility.Visible;
                                RunFireVisibleTimer();
                                FireAlarmFlag = true;
                            }

                            if (firePlace == ControlTitle)
                            {
                                FireInCurrentPlace = true;

                                FireAlarmButton.Style = (System.Windows.Style)FireAlarmButton.TryFindResource("ButtonFireActive");
                                FireAlarmImage.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                FireInCurrentPlace = false;

                                FireAlarmButton.Style = (System.Windows.Style)FireAlarmButton.TryFindResource("ButtonFire");
                                FireAlarmImage.Visibility = Visibility.Collapsed;
                            }
                        }
                        else
                        {
                            if (FireAlarmFlag)
                            {
                                if (FireVisibleTimer != null)
                                {
                                    FireVisibleTimer.Stop();
                                }
                                FireStatus.Visibility = Visibility.Collapsed;
                                FireStatus.Text = $"";
                                FireAlarmFlag = false;
                            }

                            FireInCurrentPlace = false;
                        }
                    }
                }
            }
        }

        #endregion

        #region Обычные таймеры

        private void RunAutoUpdateTimer()
        {
            if (AutoUpdateTimerInterval != 0)
            {
                if (AutoUpdateTimer == null)
                {
                    AutoUpdateTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0, 0, AutoUpdateTimerInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", AutoUpdateTimerInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("PilloryMonitorKsh_RunAutoUpdateTimer", row);
                    }

                    AutoUpdateTimer.Tick += (s, e) =>
                    {
                        if (!PlanCountingFlag)
                        {
                            LoadItems();
                        }
                    };
                }

                if (AutoUpdateTimer.IsEnabled)
                {
                    AutoUpdateTimer.Stop();
                }

                AutoUpdateTimer.Start();
            }
        }

        private void RunPalletByPlaceTimer()
        {
            if (PalletByPlaceTimerInterval != 0)
            {
                if (PalletByPlaceTimer == null)
                {
                    PalletByPlaceTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0, 0, PalletByPlaceTimerInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", PalletByPlaceTimerInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("PilloryMonitorKsh_RunPalletByPlaceTimer", row);
                    }

                    PalletByPlaceTimer.Tick += (s, e) =>
                    {
                        PalletByPlaceCheck();
                    };
                }

                if (PalletByPlaceTimer.IsEnabled)
                {
                    PalletByPlaceTimer.Stop();
                }

                PalletByPlaceTimer.Start();
            }
        }

        private async void PalletByPlaceCheck()
        {
            var p = new Dictionary<string, string>();
            p.Add("FACTORY_ID", $"{this.FactoryId}");
            p.Add("CELL_LIST", CellListString);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production/Pillory");
            q.Request.SetParam("Object", "Pallet");
            q.Request.SetParam("Action", "ListCellWithShipmentFault");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            PalletByPlaceStackPanel.Children.Clear();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        foreach (var item in ds.Items)
                        {
                            Label label = new Label();
                            label.Content = item.CheckGet("PLACE");
                            label.ToolTip = $"Дата плановой готовности продукции под отгрузку: {item.CheckGet("PRODUCTION_TASK_END_DTTM")}";
                            label.Foreground = item.CheckGet("COLOR").ToBrush();
                            label.FontWeight = FontWeights.Bold;
                            label.Margin = new Thickness(0, 0, 5, 0);
                            label.FontSize = 18;
                            label.VerticalAlignment = VerticalAlignment.Center;
                            label.Padding = new Thickness(0);

                            PalletByPlaceStackPanel.Children.Add(label);
                        }
                    }
                }
            }
        }

        private void RunGlueStatusTimer()
        {
            if (GlueStatusTimerInterval != 0)
            {
                if (GlueStatusTimer == null)
                {
                    GlueStatusTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0, 0, GlueStatusTimerInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", GlueStatusTimerInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("PilloryMonitorKsh_RunGlueStatusTimer", row);
                    }

                    GlueStatusTimer.Tick += (s, e) =>
                    {
                        GlueStatusCheck();
                    };
                }

                if (GlueStatusTimer.IsEnabled)
                {
                    GlueStatusTimer.Stop();
                }

                GlueStatusTimer.Start();
            }
        }

        private async void GlueStatusCheck()
        {
            var p = new Dictionary<string, string>();
            p.Add("FACTORY_ID", $"{FactoryId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production/Pillory");
            q.Request.SetParam("Object", "Glue");
            q.Request.SetParam("Action", "GetLowLevelFlag");

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
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        if (ds.Items[0].CheckGet("LOW_LEVEL_FLAG").ToInt() > 0)
                        {
                            if (!GlueAlarmFlag)
                            {
                                GlueStatus.Visibility = Visibility.Visible;
                                RunGlueVisibleTimer();
                                GlueAlarmFlag = true;
                            }
                        }
                        else
                        {
                            if (GlueAlarmFlag)
                            {
                                if (GlueVisibleTimer != null)
                                {
                                    GlueVisibleTimer.Stop();
                                }
                                GlueStatus.Visibility = Visibility.Collapsed;
                                GlueAlarmFlag = false;
                            }
                        }
                    }
                }
            }
        }

        private void RunPlanCountingStatusTimer()
        {
            if (PlanCountingStatusTimerInterval != 0)
            {
                if (PlanCountingStatusTimer == null)
                {
                    PlanCountingStatusTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0, 0, PlanCountingStatusTimerInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", PlanCountingStatusTimerInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("PilloryMonitor_PlanCountingStatusTimer", row);
                    }

                    PlanCountingStatusTimer.Tick += (s, e) =>
                    {
                        GetPlanCountingStatus();
                    };
                }

                if (PlanCountingStatusTimer.IsEnabled)
                {
                    PlanCountingStatusTimer.Stop();
                }

                PlanCountingStatusTimer.Start();
            }
        }

        private async void GetPlanCountingStatus()
        {
            int willBeCountPlan = 0;
            int planCounting = 0;

            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production/Pillory");
            q.Request.SetParam("Object", "ProductionTask");
            q.Request.SetParam("Action", "GetPlanStatus");

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
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        willBeCountPlan = ds.Items[0].CheckGet("WILL_BE_COUNT_PLAN").ToInt();
                        planCounting = ds.Items[0].CheckGet("PLAN_COUNTING").ToInt();
                    }
                }
            }

            if (willBeCountPlan > 0 || planCounting > 0)
            {
                SetPlanCountingFlag(true);
            }
            else
            {
                SetPlanCountingFlag(false);
            }
        }

        #endregion

        #region Ситуативные таймеры

        private void RunGlueVisibleTimer()
        {
            if (GlueVisibleTimerInterval != 0)
            {
                if (GlueVisibleTimer == null)
                {
                    GlueVisibleTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0, 0, GlueVisibleTimerInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", GlueVisibleTimerInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("PilloryMonitorKsh_RunGlueVisibleTimer", row);
                    }

                    GlueVisibleTimer.Tick += (s, e) =>
                    {
                        SetGlueVisible();
                    };
                }

                if (GlueVisibleTimer.IsEnabled)
                {
                    GlueVisibleTimer.Stop();
                }

                GlueVisibleTimer.Start();
            }
        }

        private void SetGlueVisible()
        {
            if (GlueStatus.Visibility == Visibility.Visible)
            {
                GlueStatus.Visibility = Visibility.Hidden;
            }
            else
            {
                GlueStatus.Visibility = Visibility.Visible;
            }
        }

        private void RunFireVisibleTimer()
        {
            if (FireVisibleTimerInterval != 0)
            {
                if (FireVisibleTimer == null)
                {
                    FireVisibleTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0, 0, FireVisibleTimerInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", FireVisibleTimerInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("PilloryMonitorKsh_RunFireVisibleTimer", row);
                    }

                    FireVisibleTimer.Tick += (s, e) =>
                    {
                        SetFireVisible();
                    };
                }

                if (FireVisibleTimer.IsEnabled)
                {
                    FireVisibleTimer.Stop();
                }

                FireVisibleTimer.Start();
            }
        }

        private void SetFireVisible()
        {
            if (FireStatus.Visibility == Visibility.Visible)
            {
                FireStatus.Visibility = Visibility.Hidden;
            }
            else
            {
                FireStatus.Visibility = Visibility.Visible;
            }
        }

        #endregion

        #endregion

        #region Monitor

        /// <summary>
        /// Загружаем список мониторово в выпадающий список мониторов MonitorSelectBox
        /// </summary>
        public void MonitorSelectBoxLoadItems()
        {
            Dictionary<string, string> monitorSelectBoxItems = new Dictionary<string, string>();
            var allMonitors = System.Windows.Forms.Screen.AllScreens;
            if (allMonitors.Length > 0)
            {
                for (int i = 0; i < allMonitors.Length; i++)
                {
                    monitorSelectBoxItems.Add($"{i}", $"Монитор {i + 1}");
                }
            }

            MonitorSelectBox.SetItems(monitorSelectBoxItems);
        }

        /// <summary>
        /// Устанавливаем положение окна программы и выбранный монитор в списке мониторов MonitorSelectBox по DefaultMonitorId
        /// </summary>
        public void SetDefaultMonitor()
        {
            if (MonitorSelectBox.Items != null && MonitorSelectBox.Items.Count > 0)
            {
                if (MonitorSelectBox.Items.ContainsKey($"{DefaultMonitorId}"))
                {
                    MonitorSelectBox.SetSelectedItemByKey($"{DefaultMonitorId}");
                }
                else
                {
                    SetMonitor(DefaultMonitorId);
                }
            }
            else
            {
                SetMonitor(DefaultMonitorId);
            }
        }

        /// <summary>
        /// Переносим окно программы на монитор, который выбран в выпадающем списке мониторов MonitorSelectBox
        /// </summary>
        public void SetMonitorByMonitorSelectBoxSelectedItem()
        {
            int monitorId = MonitorSelectBox.SelectedItem.Key.ToInt();
            SetMonitor(monitorId);
        }

        /// <summary>
        /// Устанавливаем положение окна программы на указанном мониторе
        /// </summary>
        /// <param name="monitorNumber"></param>
        public void SetMonitor(int monitorNumber = 0)
        {
            if (Central.MainWindow != null)
            {
                Central.MainWindow.WindowState = WindowState.Minimized;

                var allMonitors = System.Windows.Forms.Screen.AllScreens;
                if (allMonitors.Length > 0)
                {
                    if (allMonitors.Length - 1 >= monitorNumber)
                    {
                        var selectedMonitor = allMonitors[monitorNumber];
                        var area = selectedMonitor.WorkingArea;
                        Central.MainWindow.Left = area.Left;
                        Central.MainWindow.Top = area.Top;
                        Central.MainWindow.Width = area.Width;
                        Central.MainWindow.Height = area.Height;
                    }
                    else
                    {
                        var selectedMonitor = allMonitors[allMonitors.Length - 1];
                        var area = selectedMonitor.WorkingArea;
                        Central.MainWindow.Left = area.Left;
                        Central.MainWindow.Top = area.Top;
                        Central.MainWindow.Width = area.Width;
                        Central.MainWindow.Height = area.Height;
                    }
                }

                Central.MainWindow.WindowState = WindowState.Maximized;
            }
        }

        #endregion

        /// <summary>
        /// Первичное получение списка станков в MachineList;
        /// Разделение станков по страницам.
        /// </summary>
        private void InitMachineList()
        {
            // Получаем список станков переработки для указанной площадки
            List<KeyValuePair<int, string>> machineList = LoadMachineList();

            // Создаём список гридов для полученных станков
            if (machineList != null && machineList.Count > 0)
            {
                foreach (var machineListItem in machineList)
                {
                    MachineList.Add(machineListItem.Key, new PilloryGrid(machineListItem.Key, machineListItem.Value)
                        {
                            RoleName = this.RoleName,
                            ParentFrame = this.FrameName,
                            FactoryId = this.FactoryId
                        }
                    );
                }

                PrepareMachineList();
            }
        }

        private void PrepareMachineList()
        {
            Dictionary<string, string> pageSelectBoxItems = new Dictionary<string, string>();
            if (MachineList != null && MachineList.Count > 0)
            {
                var machineList = MachineList.ToList();
                int gridCountByPage = GridCountByHeight * GridCountByWidth;
                int pageCount = Math.Ceiling((double)MachineList.Count / (double)gridCountByPage).ToInt();
                for (int i = 0; i < pageCount; i++)
                {
                    pageSelectBoxItems.Add($"{i}", $"Страница {i + 1}");

                    for (int j = 0; j < gridCountByPage; j++)
                    {
                        if (machineList.Count >= j + (i * gridCountByPage) + 1)
                        {
                            machineList[j + (i * gridCountByPage)].Value.PageNumber = i;
                        }
                    }
                }
            }

            PageSelectBox.SetItems(pageSelectBoxItems);
        }

        /// <summary>
        /// Размещаем гриды из MachineList на форме
        /// Размещаем столбчатые диаграммы
        /// </summary>
        public void PlaceMachineList()
        {
            MainGrid.Children.Clear();
            BarChartGrid.Children.Clear();

            if (MachineList != null && MachineList.Count > 0)
            {
                int selectedPage = DefaultPageId;
                if (PageSelectBox.SelectedItem.Key != null)
                {
                    selectedPage = PageSelectBox.SelectedItem.Key.ToInt();
                }

                // Список станков на выбранной странице
                var machineList = MachineList.Where(x => x.Value.PageNumber == selectedPage).ToList();

                bool errorFlagPlacePilloryGrid = false;
                int xOffsetPilloryGrid = OffsetBlocks;
                int yOffsetPilloryGrid = OffsetBlocks;

                bool errorFlagPlaceBarChart = false;
                int xOffsetBarChart = OffsetBlocks;

                // Размещаем общие столбчатые диаграммы
                {
                    if (!errorFlagPlaceBarChart)
                    {
                        // если вмещается горизонтально
                        if (CorrugatorBarChart.Width < MainGrid.ActualWidth - xOffsetBarChart)
                        {
                            CorrugatorBarChart.VerticalAlignment = VerticalAlignment.Center;
                            CorrugatorBarChart.HorizontalAlignment = HorizontalAlignment.Left;

                            CorrugatorBarChart.Margin = new Thickness(xOffsetBarChart, 0, 0, 0);

                            BarChartGrid.Children.Add(CorrugatorBarChart);

                            xOffsetBarChart = xOffsetBarChart + (int)CorrugatorBarChart.Width + OffsetBlocks;
                        }
                        else
                        {
                            errorFlagPlaceBarChart = true;
                        }
                    }

                    if (!errorFlagPlaceBarChart)
                    {
                        // если вмещается горизонтально
                        if (CorrugatorGoodsBarChart.Width < MainGrid.ActualWidth - xOffsetBarChart)
                        {
                            CorrugatorGoodsBarChart.VerticalAlignment = VerticalAlignment.Center;
                            CorrugatorGoodsBarChart.HorizontalAlignment = HorizontalAlignment.Left;

                            CorrugatorGoodsBarChart.Margin = new Thickness(xOffsetBarChart, 0, 0, 0);

                            BarChartGrid.Children.Add(CorrugatorGoodsBarChart);

                            xOffsetBarChart = xOffsetBarChart + (int)CorrugatorGoodsBarChart.Width + OffsetBlocks;
                        }
                        else
                        {
                            errorFlagPlaceBarChart = true;
                        }
                    }
                }

                foreach (var machineItem in machineList)
                {
                    // Если не смогли один раз вместить грид, то не пытаемся разместить другие
                    if (!errorFlagPlacePilloryGrid)
                    {
                        // если вмещается горизонтально
                        if (machineItem.Value.Width < MainGrid.Width - xOffsetPilloryGrid)
                        {
                            machineItem.Value.VerticalAlignment = VerticalAlignment.Top;
                            machineItem.Value.HorizontalAlignment = HorizontalAlignment.Left;

                            machineItem.Value.Margin = new Thickness(xOffsetPilloryGrid, yOffsetPilloryGrid, 0, 0);

                            MainGrid.Children.Add(machineItem.Value);

                            xOffsetPilloryGrid = xOffsetPilloryGrid + (int)machineItem.Value.Width + OffsetBlocks;
                        }
                        else
                        {
                            xOffsetPilloryGrid = OffsetBlocks;
                            yOffsetPilloryGrid = yOffsetPilloryGrid + (int)machineItem.Value.Height + OffsetBlocks;

                            // если вмещается вертикально
                            if (machineItem.Value.Height < MainGrid.Height - yOffsetPilloryGrid)
                            {
                                machineItem.Value.VerticalAlignment = VerticalAlignment.Top;
                                machineItem.Value.HorizontalAlignment = HorizontalAlignment.Left;

                                machineItem.Value.Margin = new Thickness(xOffsetPilloryGrid, yOffsetPilloryGrid, 0, 0);

                                MainGrid.Children.Add(machineItem.Value);

                                xOffsetPilloryGrid = xOffsetPilloryGrid + (int)machineItem.Value.Width + OffsetBlocks;
                            }
                            else
                            {
                                errorFlagPlacePilloryGrid = true;
                            }
                        }

                        // Если смогли разместить грид, то пытаемся разместить столбчатую диаграмму
                        if (!errorFlagPlacePilloryGrid && !errorFlagPlaceBarChart)
                        {
                            // если вмещается горизонтально
                            if (machineItem.Value.BarChart.Width < MainGrid.ActualWidth - xOffsetBarChart)
                            {
                                machineItem.Value.BarChart.VerticalAlignment = VerticalAlignment.Center;
                                machineItem.Value.BarChart.HorizontalAlignment = HorizontalAlignment.Left;

                                machineItem.Value.BarChart.Margin = new Thickness(xOffsetBarChart, 0, 0, 0);

                                BarChartGrid.Children.Add(machineItem.Value.BarChart);

                                xOffsetBarChart = xOffsetBarChart + (int)machineItem.Value.BarChart.Width + OffsetBlocks;
                            }
                            else
                            {
                                errorFlagPlaceBarChart = true;
                            }
                        }
                    }
                }

                // Размещаем общие столбчатые диаграммы
                {
                    if (!errorFlagPlaceBarChart)
                    {
                        // если вмещается горизонтально
                        if (Blank1BarChart.Width < MainGrid.ActualWidth - xOffsetBarChart)
                        {
                            Blank1BarChart.VerticalAlignment = VerticalAlignment.Center;
                            Blank1BarChart.HorizontalAlignment = HorizontalAlignment.Left;

                            Blank1BarChart.Margin = new Thickness(xOffsetBarChart, 0, 0, 0);

                            BarChartGrid.Children.Add(Blank1BarChart);

                            xOffsetBarChart = xOffsetBarChart + (int)Blank1BarChart.Width + OffsetBlocks;
                        }
                        else
                        {
                            errorFlagPlaceBarChart = true;
                        }
                    }

                    if (!errorFlagPlaceBarChart)
                    {
                        // если вмещается горизонтально
                        if (StockBarChart.Width < MainGrid.ActualWidth - xOffsetBarChart)
                        {
                            StockBarChart.VerticalAlignment = VerticalAlignment.Center;
                            StockBarChart.HorizontalAlignment = HorizontalAlignment.Left;

                            StockBarChart.Margin = new Thickness(xOffsetBarChart, 0, 0, 0);

                            BarChartGrid.Children.Add(StockBarChart);

                            xOffsetBarChart = xOffsetBarChart + (int)StockBarChart.Width + OffsetBlocks;
                        }
                        else
                        {
                            errorFlagPlaceBarChart = true;
                        }
                    }
                }

                if (errorFlagPlacePilloryGrid)
                {
                    string msg = $"Не удалось вместить грид";
                    var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
                else if (errorFlagPlaceBarChart)
                {
                    string msg = $"Не удалось вместить столбчатую диаграмму";
                    var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
        }

        public void SetDefaultPage()
        {
            if (PageSelectBox.Items != null && PageSelectBox.Items.Count > 0)
            {
                if (PageSelectBox.Items.ContainsKey($"{DefaultPageId}"))
                {
                    PageSelectBox.SetSelectedItemByKey($"{DefaultPageId}");
                }
                else
                {
                    PlaceMachineList();
                }
            }
            else
            {
                PlaceMachineList();
            }
        }

        /// <summary>
        /// Получаем список станков выбранной площадки
        /// </summary>
        /// <returns>KeyValuePair(MACHINE_ID, MACHINE_NAME2)</returns>
        private List<KeyValuePair<int, string>> LoadMachineList()
        {
            List<KeyValuePair<int, string>> machineList = new List<KeyValuePair<int, string>>();

            var p = new Dictionary<string, string>();
            p.Add("FACTORY_ID", $"{FactoryId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production/Pillory");
            q.Request.SetParam("Object", "Machine");
            q.Request.SetParam("Action", "List");
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
                        foreach (var item in ds.Items)
                        {
                            machineList.Add(new KeyValuePair<int, string>(item.CheckGet("MACHINE_ID").ToInt(), item.CheckGet("MACHINE_NAME2")));
                        }
                    }
                }
            }

            return machineList;
        }

        /// <summary>
        /// Получаем, подготавливаем и заполняем данные по станкам.
        /// Запускает Task.
        /// </summary>
        public void LoadItems()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                RefreshButton.IsEnabled = false;
                LoadingStatusLabel.Content = "Загрузка...";
            });

            Task.Run(() =>
            {
                GetData();
                PrepareData();
                SetData();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    RefreshButton.IsEnabled = true;
                    LoadingStatusLabel.Content = "";
                });
            });
        }

        /// <summary>
        /// Получаем данные по станкам
        /// </summary>
        private void GetData()
        {
            GetProductionTaskData();
            GetMachineData();
        }

        /// <summary>
        /// Получаем данные по ПЗ на станках для ProductionTaskDataSet
        /// </summary>
        private void GetProductionTaskData()
        {
            ProductionTaskDataSet = new ListDataSet();

            var p = new Dictionary<string, string>();
            p.Add("FACTORY_ID", $"{FactoryId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production/Pillory");
            q.Request.SetParam("Object", "ProductionTask");
            q.Request.SetParam("Action", "List");

            q.Request.SetParams(p);

            q.Request.Timeout = 40000;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    ProductionTaskDataSet = ListDataSet.Create(result, "ITEMS");
                    BarChartDataSet = ListDataSet.Create(result, "BAR_ITEMS");
                    MainBarChartDataSet = ListDataSet.Create(result, "BAR_MAIN_ITEMS");
                    MainPieChartDataSet = ListDataSet.Create(result, "PIE_CHART_MAIN_ITEMS");
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Получаем данные по сводке работы станков для MachineDataSet
        /// </summary>
        private void GetMachineData()
        {
            MachineDataSet = new ListDataSet();

            var p = new Dictionary<string, string>();
            p.Add("FACTORY_ID", $"{FactoryId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production/Pillory");
            q.Request.SetParam("Object", "Machine");
            q.Request.SetParam("Action", "ListData");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    MachineDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Делим данные из датасетов по станкам
        /// </summary>
        private void PrepareData()
        {
            PrepareProductionTaskData();
            PrepareMachineData();
        }

        /// <summary>
        /// Делим данные из датасета ПЗ на станках по станкам в MachineIdProductionTaskData; 
        /// Делим данные из датасета загруженности по станкам в MachineIdBarChartData.
        /// </summary>
        private void PrepareProductionTaskData()
        {
            MachineIdProductionTaskData = new Dictionary<int, List<Dictionary<string, string>>>();
            if (ProductionTaskDataSet != null && ProductionTaskDataSet.Items != null && ProductionTaskDataSet.Items.Count > 0)
            {
                foreach (var item in ProductionTaskDataSet.Items)
                {
                    if (!MachineIdProductionTaskData.ContainsKey(item["MACHINE_ID"].ToInt()))
                    {
                        MachineIdProductionTaskData.Add(item["MACHINE_ID"].ToInt(), new List<Dictionary<string, string>>());
                    }

                    MachineIdProductionTaskData[item["MACHINE_ID"].ToInt()].Add(item);
                }
            }

            MachineIdBarChartData = new Dictionary<int, Dictionary<string, string>>();
            if (BarChartDataSet != null && BarChartDataSet.Items != null && BarChartDataSet.Items.Count > 0)
            {
                foreach (var item in BarChartDataSet.Items)
                {
                    if (!MachineIdBarChartData.ContainsKey(item["MACHINE_ID"].ToInt()))
                    {
                        MachineIdBarChartData.Add(item["MACHINE_ID"].ToInt(), new Dictionary<string, string>());
                    }

                    MachineIdBarChartData[item["MACHINE_ID"].ToInt()] = item;
                }
            }

            VirtualMachineIdMainBarChartData = new Dictionary<int, Dictionary<string, string>>();
            if (MainBarChartDataSet != null && MainBarChartDataSet.Items != null && MainBarChartDataSet.Items.Count > 0)
            {
                foreach (var item in MainBarChartDataSet.Items)
                {
                    if (!VirtualMachineIdMainBarChartData.ContainsKey(item["MACHINE_ID"].ToInt()))
                    {
                        VirtualMachineIdMainBarChartData.Add(item["MACHINE_ID"].ToInt(), new Dictionary<string, string>());
                    }

                    VirtualMachineIdMainBarChartData[item["MACHINE_ID"].ToInt()] = item;
                }
            }
        }

        /// <summary>
        /// Делим данные из датасета по сводке работы станков по станкам в MachineIdMachineData.
        /// </summary>
        private void PrepareMachineData()
        {
            MachineIdMachineData = new Dictionary<int, Dictionary<string, string>>();
            if (MachineDataSet != null && MachineDataSet.Items != null && MachineDataSet.Items.Count > 0)
            {
                foreach (var item in MachineDataSet.Items)
                {
                    if (!MachineIdMachineData.ContainsKey(item["MACHINE_ID"].ToInt()))
                    {
                        MachineIdMachineData.Add(item["MACHINE_ID"].ToInt(), new Dictionary<string, string>());
                    }

                    MachineIdMachineData[item["MACHINE_ID"].ToInt()] = item;
                }
            }
        }

        /// <summary>
        /// Заполняем данные по станкам MachineList из MachineIdProductionTaskData и MachineIdMachineData
        /// </summary>
        private void SetData()
        {
            if (MachineList != null && MachineList.Count > 0)
            {
                bool doLoadItems = false;

                foreach (var machineListItem in MachineList)
                {
                    doLoadItems = false;

                    if (MachineIdProductionTaskData.ContainsKey(machineListItem.Key))
                    {
                        machineListItem.Value.GridDataSet = ListDataSet.Create(MachineIdProductionTaskData[machineListItem.Key]);
                        doLoadItems = true;
                    }

                    if (MachineIdMachineData.ContainsKey(machineListItem.Key))
                    {
                        machineListItem.Value.FormData = MachineIdMachineData[machineListItem.Key];
                        doLoadItems = true;
                    }

                    if (MachineIdBarChartData.ContainsKey(machineListItem.Key))
                    {
                        machineListItem.Value.BarChartData = MachineIdBarChartData[machineListItem.Key];
                        doLoadItems = true;
                    }

                    if (doLoadItems)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            machineListItem.Value.LoadItems();
                        });
                    }
                }

                // Заполняем данными общие столбчатые диаграммы
                {
                    foreach (var item in VirtualMachineIdMainBarChartData)
                    {
                        switch (item.Value.CheckGet("MACHINE_ID").ToInt())
                        {
                            case -1:
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    CorrugatorBarChart.SetValues(
                                        item.Value.CheckGet("HEIGHT").ToDouble(),
                                        item.Value.CheckGet("WORKLOAD"),
                                        $"{item.Value.CheckGet("HOUR").ToDouble().ToString("#0.0")} ч.",
                                        item.Value.CheckGet("WORKLOAD2"),
                                        item.Value.CheckGet("COLOR")
                                        );
                                });
                                break;

                            case -2:
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    CorrugatorGoodsBarChart.SetValues(
                                        item.Value.CheckGet("HEIGHT").ToDouble(),
                                        item.Value.CheckGet("WORKLOAD"),
                                        item.Value.CheckGet("HOUR"),
                                        item.Value.CheckGet("WORKLOAD2"),
                                        item.Value.CheckGet("COLOR")
                                        );
                                });
                                break;

                            case -3:
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    StockBarChart.SetValues(
                                        item.Value.CheckGet("HEIGHT").ToDouble(),
                                        $"{item.Value.CheckGet("WORKLOAD").ToDouble().ToString("#0")} %",
                                        item.Value.CheckGet("HOUR"),
                                        item.Value.CheckGet("WORKLOAD2"),
                                        item.Value.CheckGet("COLOR")
                                        );
                                });
                                break;

                            case -4:
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    Blank1BarChart.SetValues(
                                        item.Value.CheckGet("HEIGHT").ToDouble(),
                                        $"{item.Value.CheckGet("WORKLOAD").ToDouble().ToString("#0")} %",
                                        item.Value.CheckGet("HOUR"),
                                        item.Value.CheckGet("WORKLOAD2"),
                                        item.Value.CheckGet("COLOR")
                                        );
                                    Blank1BarChart.SecondValueTextBlock.ToolTip = "Откондиционированных листов в буфере / Всего листов в буфере";
                                });
                                break;
                        }
                    }
                }

                // Заполняем данными круговую диаграмму
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        PieChart.SetValues(MainPieChartDataSet.Items);
                    });
                }
            }
        }

        private void FireAlarm()
        {
            if (!FireInCurrentPlace)
            {
                var d = new DialogWindow($"Объявить пожарную тревогу на объекте {ControlTitle}?", "Пожарная тревога", "", DialogWindowButtons.YesNo);
                if (d.ShowDialog() == true)
                {
                    FireStatusTimer.Stop();

                    FireStatus.Visibility = Visibility.Visible;
                    FireStatus.Text = $"Пожар! {ControlTitle}";
                    RunFireVisibleTimer();
                    FireAlarmFlag = true;

                    UpdateFireStatus(ControlTitle);

                    FireAlarmButton.Style = (System.Windows.Style)FireAlarmButton.TryFindResource("ButtonFireActive");
                    FireAlarmImage.Visibility = Visibility.Visible;

                    FireInCurrentPlace = true;
                }
            }
            else
            {
                FireInCurrentPlace = false;

                FireAlarmButton.Style = (System.Windows.Style)FireAlarmButton.TryFindResource("ButtonFire");
                FireAlarmImage.Visibility = Visibility.Collapsed;

                UpdateFireStatus();

                if (FireVisibleTimer != null)
                {
                    FireVisibleTimer.Stop();
                }
                FireStatus.Visibility = Visibility.Collapsed;
                FireStatus.Text = $"";
                FireAlarmFlag = false;

                FireStatusTimer.Start();
            }
        }

        private void UpdateFireStatus(string place = "")
        {
            var p = new Dictionary<string, string>();
            p.Add("FIRE_NAME", "FIRE_KSH");
            p.Add("PLACE", place);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "IndustrialWaste");
            q.Request.SetParam("Action", "UpdateFire");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status != 0)
            {
                q.ProcessError();
            }
        }

        private void SetPlanCountingFlag(bool planCountingFlag)
        {
            if (planCountingFlag)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    LoadingStatusLabel.Content = "Пересчёт плана";
                });

                PlanCountingFlag = true;
            }
            else
            {
                if (PlanCountingFlag)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        LoadingStatusLabel.Content = "";
                        LoadItems();
                    });
                }

                PlanCountingFlag = false;
            }
        }

        private void InitCellList()
        {
            CellList = new List<Cell>();
            CellListString = "";

            CreateCellItem("КПР", 0);
            CreateCellItem("КГА", 0);
        }

        private void CreateCellItem(string cellName, int cellNumber)
        {
            Cell cell = new Cell(cellName, cellNumber);
            CellListString = $"{CellListString}{cell.Name};";
            CellList.Add(cell);

            MenuItem menuItem = new MenuItem();
            menuItem.Header = cell.Name;
            menuItem.Background = "#ffffff".ToBrush();
            menuItem.Click += (o, e) => ShowPalletByPlace(cell.CellName, $"{cell.CellNumber}");
            PalletByPlaceButtonMenu.Items.Add(menuItem);
        }

        private void ShowPalletByPlace(string placeName, string placeNumber)
        {
            var i = new PalletListByPlace();
            i.RoleName = this.RoleName;
            i.ParentFrame = this.FrameName;
            i.PlaceName = placeName;
            i.PlaceNumber = placeNumber;
            i.Show();
        }

        private void RePlaceBlocks_Click(object sender, RoutedEventArgs e)
        {
            // Размещаем гриды на форме
            PlaceMachineList();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadItems();
        }

        private void MonitorSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SetMonitorByMonitorSelectBoxSelectedItem();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            Central.ShowHelp(DocumentationUrl);
        }

        private void PageSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Размещаем гриды на форме
            PlaceMachineList();
        }

        private void FireAlarmButton_Click(object sender, RoutedEventArgs e)
        {
            FireAlarm();
        }

        private void PalletByPlaceButton_Click(object sender, RoutedEventArgs e)
        {
            PalletByPlaceButtonMenu.IsOpen = true;
        }

        private void DowntimeButton_Click(object sender, RoutedEventArgs e)
        {
            var i = new DowntimeList();
            i.RoleName = this.RoleName;
            i.ParentFrame = this.FrameName;
            i.FactoryId = this.FactoryId;
            i.Show();
        }

        private void SubProductButton_Click(object sender, RoutedEventArgs e)
        {
            var i = new Client.Interfaces.Production.Pillory.SubProductList();
            i.RoleName = this.RoleName;
            i.ParentFrame = this.FrameName;
            i.FactoryId = this.FactoryId;
            i.Show();
        }

        private void ConditioningButton_Click(object sender, RoutedEventArgs e)
        {
            var i = new Client.Interfaces.Production.Pillory.PalletListConditioning();
            i.RoleName = this.RoleName;
            i.ParentFrame = this.FrameName;
            i.FactoryId = this.FactoryId;
            i.Show();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            var informationMessage = Central.MakeInfoString();
            var d = new DialogWindow($"{informationMessage}", this.ControlTitle, "", DialogWindowButtons.OK);
            d.ShowDialog();
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Central.MainWindow).Restart();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)Central.MainWindow).Exit();
        }
    }
}
