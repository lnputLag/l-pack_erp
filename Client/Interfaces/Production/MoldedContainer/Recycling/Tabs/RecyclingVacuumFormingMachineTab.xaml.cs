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
    public partial class RecyclingVacuumFormingMachineTab : ControlBase
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
        public RecyclingVacuumFormingMachineTab()
        {
            ControlTitle = "ВФМ ЛТ";
            DocumentationUrl = "/doc/l-pack-erp-new/lt/recycling_lt";
            RoleName = "[erp]molded_contnr_operator";

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

                SetDefaults();
                FormInit();
                ProductionTaskGridInit();

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
                            
                ArrivalPalletGrid.ItemsAutoUpdate = true;
                ArrivalPalletGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                ProductionTaskGrid.ItemsAutoUpdate = false;
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
                    }
                    break;

                case Role.AccessMode.FullAccess:
                    {
                        ReadOnlyFlag = false;
                    }
                    break;

                case Role.AccessMode.ReadOnly:
                    {
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

            var list = new Dictionary<string, string>();
            list.Add("306", "ВФМ BST-1");
            list.Add("305", "ВФМ BST-2");
            Machines.Items = list;
            Machines.SetSelectedItemByKey("306"); 
        }

        /// <summary>
        /// обновляем все гриды
        /// </summary>
        public void Refresh()
        {
            // buttons[0] = false;
            // RefreshButton.IsEnabled = false;
            // ButtonTimer.Run();

            ProductionTaskGrid.LoadItems();
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
            MachineGetCount();

            if (Machines != null && Machines.SelectedItem.Key != null)
            {
                var p = new Dictionary<string, string>();
                if (Machines.SelectedItem.Key == "306")
                {
                    p.Add("ST", "1");
                }
                else
                if (Machines.SelectedItem.Key == "305")
                {
                    p.Add("ST", "2");
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "MoldedContainer");
                q.Request.SetParam("Object", "Recycling");
                q.Request.SetParam("Action", "GetInfoMachineVfm");
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
                     //   CounterQty.Text = counter_qty;
                        TaskQuantity.Text = task_quantity;
                        PrihodQty.Text = prihod_qty;
                        RashodQty.Text = rashod_qty;
                    }
                }
            }
        }

        /// <summary>
        /// получаем значение счетчиков для ВФМ1 и ВФМ2
        /// </summary>
        private async void MachineGetCount()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Machine");
            q.Request.SetParam("Action", "GetCountByIds");

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
                    if (ds.Items.Count > 0)
                    {
                        var counter_qty_305 = ds.Items.FirstOrDefault().CheckGet("COUNTER_QTY").ToInt().ToString();
                        var counter_qty_306 = ds.Items.Last().CheckGet("COUNTER_QTY").ToInt().ToString();
                        if (Machines != null && Machines.SelectedItem.Key != null)
                        {
                            if (Machines.SelectedItem.Key.ToInt() == 306)
                            {
                                CounterQty.Text = $"{counter_qty_306}";
                            }
                            else if (Machines.SelectedItem.Key.ToInt() == 305)
                            {
                                CounterQty.Text = $"{counter_qty_305}";
                            }
                        }
                    }
                }
            }
            else
            {
                q.ProcessError();
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
                        Header="Станок",
                        Path="MACHINE_NAME",
                        Description="",
                        ColumnType=ColumnTypeRef.String,
                        Width2=12,
                        Visible = true,
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
                        Header="Прим. для производства",
                        Path="PRODUCTION_NOTE",
                        Description="",
                        ColumnType=ColumnTypeRef.String,
                        Width2=18,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Примечание",
                        Path="ORDER_NOTE_GENERAL",
                        Description="примечание ОПП и складу",
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
                        Header="ИД изделия",
                        Path="GOODS_ID",
                        Description="(id2)",
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
                           // if (ProductionTaskGrid.Items.FirstOrDefault(x => x.CheckGet("TASK_ID").ToInt() == selectedItem.CheckGet("TASK_ID").ToInt()) == null)
                            {
                             //   ProductionTaskGrid.SelectRowFirst();
                                ArrivalPalletGridLoadItems();
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
                if (Machines.SelectedItem.Key == "306")
                {
                    p.Add("ST", "1");
                }
                else
                if (Machines.SelectedItem.Key == "305")
                {
                    p.Add("ST", "2");
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "MoldedContainer");
                q.Request.SetParam("Object", "Recycling");
                q.Request.SetParam("Action", "ListVfm");
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
                        ProductionTaskGrid.UpdateItems(ProductionTaskGridDataSet);
                        ProductionTaskGrid.SelectRowFirst();
                        ArrivalPalletGrid.LoadItems();
                    }
                }
                else
                {
                    // q.ProcessError();
                }
            }
        }


        public void ProductionTaskGridFilterItems()
        {
            if (ProductionTaskGrid != null && ProductionTaskGrid.SelectedItem != null && ProductionTaskGrid.SelectedItem.Count > 0)
            {
                ProductionTaskGrid.Commands.Message = new ItemMessage() { Action = "refresh", Message = $"{ProductionTaskGrid.SelectedItem.CheckGet("TASK_ID")}" };
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
                        Width2=27,
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
                p.Add("PROT_ID", ProductionTaskGrid.SelectedItem.CheckGet("TASK_ID").ToInt().ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "MoldedContainer");
                q.Request.SetParam("Object", "Recycling");
                q.Request.SetParam("Action", "PalletListVfm");
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
        ////отображаем список ранее выполненых ПЗ для текущего станка
        /// </summary>
        private void ShowAllProdictionTask()
        {
            buttons[3] = false;

            ButtonTimer.Run();

            if (Machines != null && Machines.SelectedItem.Key != null)
            {
                var h = new RecyclingPtoductionTaskAll();
                h.IdSt = Machines.SelectedItem.Key.ToString();
                h.Edit();

            }
            ProductionTaskGrid.LoadItems();
        }

        /// <summary>
        ////отображаем список ранее оприходованных паллет с заготовками для текущего ВФМ
        /// </summary>
        private void ShowAllArrivialPallet()
        {
            if (Machines != null && Machines.SelectedItem.Key != null)
            {
                var h = new RecyclingArrivalPalletAllVfm();
                h.ReceiverName = ControlName;
                h.IdSt = Machines.SelectedItem.Key.ToString();
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
                h.IdSt = Machines.SelectedItem.Key.ToString();
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

                            }
                            break;

                        //Закончить ПЗ
                        case 2:
                            {

                            }
                            break;

                        // Все ПЗ
                        case 3:
                            {

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

                            }
                            break;

                        //Создать паллету
                        case 6:
                            {

                            }
                            break;

                        // Удалить паллету
                        case 7:
                            {

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


      





        ///

    }
}
