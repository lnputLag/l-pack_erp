using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Отчёт по движению рулонов 
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class ReportRollMovement : ControlBase
    {
        public ReportRollMovement()
        {
            ControlTitle = "Движение рулонов";
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

        /// <summary>
        /// Идентификатор площадки
        /// </summary>
        public int FactoryId = 1;

        public enum OperationType 
        {
            /// <summary>
            /// Приход
            /// </summary>
            Arrival = 1,
            /// <summary>
            /// Приход с БДМ
            /// </summary>
            ArrivalFromPaperMachine = 11,
            /// <summary>
            /// Приход от поставщика
            /// </summary>
            ArrivalFromSupplier = 12,
            /// <summary>
            /// Возврат
            /// </summary>
            ArrivalFromReturn = 13,

            /// <summary>
            /// Расход
            /// </summary>
            Consumption = 2,
            /// <summary>
            /// Списание в брак
            /// </summary>
            ConsumptionToFault = 21,
            /// <summary>
            /// Отгрузка
            /// </summary>
            ConsumptionToShipment = 22,
            /// <summary>
            /// Списание в производство
            /// </summary>
            ConsumptionToProduction = 23,

            /// <summary>
            /// Перемещение
            /// </summary>
            Movement = 3,
            /// <summary>
            /// Перемещение внутри склада
            /// </summary>
            MovementInner = 31,
            /// <summary>
            /// Перемещение на ГА
            /// </summary>
            MovementToCorrugatorMachine = 32,
            /// <summary>
            /// Перемещение с БДМ
            /// </summary>
            MovementFromPaperMachine = 33,
            /// <summary>
            /// Перемещение на БДМ
            /// </summary>
            MovementToPaperMachine= 34,
            /// <summary>
            /// Перемещение с ГА
            /// </summary>
            MovementFromCorrugatorMachine = 35,            
        };

        public static Dictionary<int, string> OperationTypeName = new Dictionary<int, string>() 
        {
            {(int)OperationType.Arrival, "Приход"},
            {(int)OperationType.ArrivalFromPaperMachine, "Приход с БДМ"},
            {(int)OperationType.ArrivalFromSupplier, "Приход от поставщика"},
            {(int)OperationType.ArrivalFromReturn, "Возврат"},

            {(int)OperationType.Consumption, "Расход"},
            {(int)OperationType.ConsumptionToFault, "Списание в брак"},
            {(int)OperationType.ConsumptionToShipment, "Отгрузка"},
            {(int)OperationType.ConsumptionToProduction, "Списание в производство"},

            {(int)OperationType.Movement, "Перемещение"},
            {(int)OperationType.MovementInner, "Перемещение внутри склада"},
            {(int)OperationType.MovementToCorrugatorMachine, "Перемещение на ГА"},
            {(int)OperationType.MovementFromPaperMachine, "Перемещение с БДМ"},
            {(int)OperationType.MovementToPaperMachine, "Перемещение на БДМ"},
            {(int)OperationType.MovementFromCorrugatorMachine, "Перемещение с ГА"},
        };

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
                        Path="FROM_DATE_TIME",
                        FieldType=FormHelperField.FieldTypeRef.DateTime,
                        Control=FromDateTime,
                        ControlType="dateedit",
                        Format="dd.MM.yyyy HH:mm:ss",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        }
                    },
                    new FormHelperField()
                    {
                        Path="TO_DATE_TIME",
                        FieldType=FormHelperField.FieldTypeRef.DateTime,
                        Control=ToDateTime,
                        ControlType="dateedit",
                        Format="dd.MM.yyyy HH:mm:ss",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
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

            Dictionary<string, string> operationTypeSelectBoxItems = new Dictionary<string, string>();
            operationTypeSelectBoxItems.Add("0", "Все операции");
            foreach (var operationTypeItem in OperationTypeName)
            {
                operationTypeSelectBoxItems.Add($"{operationTypeItem.Key}", operationTypeItem.Value);
            }
            OperationTypeSelectBox.Items = operationTypeSelectBoxItems;
            OperationTypeSelectBox.SetSelectedItemByKey("0");

            var date = DateTime.Now;
            if (date.Hour >= 20 || date.Hour < 8)
            {
                if (date.Hour >= 20)
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.AddDays(1).ToString("dd.MM.yyyy")} 08:00:00");
                }
                else
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 08:00:00");
                }
            }
            else
            {
                Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 08:00:00");
                Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 20:00:00");
            }
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
                        Header = "#",
                        Path = "_ROWNUMBER",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Дата",
                        Path = "DTTM",
                        ColumnType = ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2=14,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Ид прихода",
                        Path = "INCOMING_ID",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Рулон",
                        Path = "ROLL_NUMBER",
                        ColumnType = ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Операция",
                        Path = "COMMETN",
                        ColumnType = ColumnTypeRef.String,
                        Width2=22,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Из ячейки",
                        Path = "CELL_OUT",
                        ColumnType = ColumnTypeRef.String,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "В ячейку",
                        Path = "CELL_IN",
                        ColumnType = ColumnTypeRef.String,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Вес, кг.",
                        Path = "WEIGHT",
                        ColumnType = ColumnTypeRef.Double,
                        Format="N0",
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Пользователь",
                        Path = "NAME",
                        ColumnType = ColumnTypeRef.String,
                        Width2=12,
                    },

                    new DataGridHelperColumn
                    {
                        Header = "Тип операции",
                        Path = "TYPE",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2=4,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Подтип операции",
                        Path = "SUB_TYPE",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2=4,
                        Hidden=true,
                    },
                };
                Grid.SetColumns(columns);
                Grid.SearchText = SearchText;
                Grid.Toolbar = GridToolbar;
                Grid.OnLoadItems = GridLoadItems;
                Grid.SetPrimaryKey("_ROWNUMBER");
                Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                Grid.AutoUpdateInterval = 5 * 60;
                Grid.Toolbar = GridToolbar;
                Grid.OnFilterItems = () =>
                {
                    OperatorProgressClearItems();

                    if (Grid.Items != null && Grid.Items.Count > 0)
                    {
                        // Фильтрация по типу операции
                        // 0 -- Все виды операции
                        if (OperationTypeSelectBox.SelectedItem.Key != null)
                        {
                            var key = OperationTypeSelectBox.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            switch (key)
                            {
                                // Все виды операции
                                case 0:
                                    items = Grid.Items;
                                    break;

                                case (int)OperationType.Arrival:
                                case (int)OperationType.Consumption:
                                case (int)OperationType.Movement:
                                    items.AddRange(Grid.Items.Where(x => x.CheckGet("TYPE").ToInt() == key));
                                    break;

                                default:
                                    items.AddRange(Grid.Items.Where(x => x.CheckGet("SUB_TYPE").ToInt() == key));
                                    break;
                            }

                            Grid.Items = items;
                        }
                    }

                    OperatorProgressLoadItems();
                };
                Grid.Commands = Commander;
                Grid.UseProgressSplashAuto = false;
                Grid.Init();
            }
        }

        private async void GridLoadItems()
        {
            bool resume = true;

            var f = Form.GetValueByPath("FROM_DATE_TIME").ToDateTime();
            var t = Form.GetValueByPath("TO_DATE_TIME").ToDateTime();

            if (DateTime.Compare(f, t) > 0)
            {
                const string msg = "Дата начала должна быть меньше даты окончания.";
                var d = new DialogWindow($"{msg}", this.ControlTitle);
                d.ShowDialog();
                resume = false;
            }

            if (resume)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    EnableSplash($"Пожалуйста, подождите.{Environment.NewLine}Идёт загрузка данных.");
                });

                var p = new Dictionary<string, string>();
                p.Add("FROM_DTTM", Form.GetValueByPath("FROM_DATE_TIME"));
                p.Add("TO_DTTM", Form.GetValueByPath("TO_DATE_TIME"));
                p.Add("FACTORY_ID", $"{FactoryId}");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Stock");
                q.Request.SetParam("Object", "Report");
                q.Request.SetParam("Action", "RollMovement");
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

                Application.Current.Dispatcher.Invoke(() =>
                {
                    DisableSplash();
                });
            }
        }

        private void OperatorProgressClearItems()
        {
            PanelScore.Children.Clear();
            PanelScoreFooter.Children.Clear();
        }

        private void OperatorProgressLoadItems()
        {
            int defaultPanelHeight = 30;

            int maxOperationCount = 0;

            int summaryArrivalCount = 0;
            int summaryArrivalWeight = 0;
            int summaryArrivalFromPaperMachineCount = 0;
            int summaryArrivalFromSupplierCount = 0;
            int summaryArrivalFromReturnCount = 0;
            int summaryArrivalFromPaperMachineWeight = 0;
            int summaryArrivalFromSupplierWeight = 0;
            int summaryArrivalFromReturnWeight = 0;

            int summaryConsumptionCount = 0;
            int summaryConsumptionWeight = 0;
            int summaryConsumptionToFaultCount = 0;
            int summaryConsumptionToShipmentCount = 0;
            int summaryConsumptionToProductionCount = 0;
            int summaryConsumptionToFaultWeight = 0;
            int summaryConsumptionToShipmentWeight = 0;
            int summaryConsumptionToProductionWeight = 0;

            int summaryMovementCount = 0;
            int summaryMovementWeight = 0;
            int summaryMovementInnerCount = 0;
            int summaryMovementToCorrugatorMachineCount = 0;
            int summaryMovementFromPaperMachineCount = 0;
            int summaryMovementToPaperMachineCount = 0;
            int summaryMovementFromCorrugatorMachineCount = 0;
            int summaryMovementInnerWeight = 0;
            int summaryMovementToCorrugatorMachineWeight = 0;
            int summaryMovementFromPaperMachineWeight = 0;
            int summaryMovementToPaperMachineWeight = 0;
            int summaryMovementFromCorrugatorMachineWeight = 0;

            if (Grid != null && Grid.Items != null && Grid.Items.Count > 0)
            {
                // Данные для прогресбаров по сторудникам
                {
                    var groupedItems = Grid.Items.GroupBy(x => x.CheckGet("NAME")).OrderBy(x => x.Key);
                    maxOperationCount = groupedItems.Max(x => x.Count());
                    foreach (var groupedItem in groupedItems)
                    {
                        int arrivalCount = 0;
                        int movementCount = 0;
                        int consumptionCount = 0;
                        foreach (var item in groupedItem)
                        {
                            switch ((OperationType)(item.CheckGet("TYPE").ToInt()))
                            {
                                case OperationType.Arrival:
                                    arrivalCount++;
                                    summaryArrivalWeight += item.CheckGet("WEIGHT").ToInt();
                                    break;

                                case OperationType.Movement:
                                    movementCount++;
                                    summaryMovementWeight += item.CheckGet("WEIGHT").ToInt();
                                    break;

                                case OperationType.Consumption:
                                    consumptionCount++;
                                    summaryConsumptionWeight += item.CheckGet("WEIGHT").ToInt();
                                    break;
                            }

                            switch ((OperationType)(item.CheckGet("SUB_TYPE").ToInt()))
                            {
                                case OperationType.ArrivalFromPaperMachine:
                                    summaryArrivalFromPaperMachineCount++;
                                    summaryArrivalFromPaperMachineWeight += item.CheckGet("WEIGHT").ToInt();
                                    break;
                                case OperationType.ArrivalFromSupplier:
                                    summaryArrivalFromSupplierCount++;
                                    summaryArrivalFromSupplierWeight += item.CheckGet("WEIGHT").ToInt();
                                    break;
                                case OperationType.ArrivalFromReturn:
                                    summaryArrivalFromReturnCount++;
                                    summaryArrivalFromReturnWeight += item.CheckGet("WEIGHT").ToInt();
                                    break;

                                case OperationType.ConsumptionToFault:
                                    summaryConsumptionToFaultCount++;
                                    summaryConsumptionToFaultWeight += item.CheckGet("WEIGHT").ToInt();
                                    break;
                                case OperationType.ConsumptionToShipment:
                                    summaryConsumptionToShipmentCount++;
                                    summaryConsumptionToShipmentWeight += item.CheckGet("WEIGHT").ToInt();
                                    break;
                                case OperationType.ConsumptionToProduction:
                                    summaryConsumptionToProductionCount++;
                                    summaryConsumptionToProductionWeight += item.CheckGet("WEIGHT").ToInt();
                                    break;

                                case OperationType.MovementInner:
                                    summaryMovementInnerCount++;
                                    summaryMovementInnerWeight += item.CheckGet("WEIGHT").ToInt();
                                    break;
                                case OperationType.MovementToCorrugatorMachine:
                                    summaryMovementToCorrugatorMachineCount++;
                                    summaryMovementToCorrugatorMachineWeight += item.CheckGet("WEIGHT").ToInt();
                                    break;
                                case OperationType.MovementFromPaperMachine:
                                    summaryMovementFromPaperMachineCount++;
                                    summaryMovementFromPaperMachineWeight += item.CheckGet("WEIGHT").ToInt();
                                    break;
                                case OperationType.MovementToPaperMachine:
                                    summaryMovementToPaperMachineCount++;
                                    summaryMovementToPaperMachineWeight += item.CheckGet("WEIGHT").ToInt();
                                    break;
                                case OperationType.MovementFromCorrugatorMachine:
                                    summaryMovementFromCorrugatorMachineCount++;
                                    summaryMovementFromCorrugatorMachineWeight += item.CheckGet("WEIGHT").ToInt();
                                    break;
                                default:
                                    break;
                            }
                        }

                        var operatorProgress = CreateOperatorProgressForEmployee(defaultPanelHeight, groupedItem.Key, "0");
                        operatorProgress.SetProgress(OperatorProgress.CalculateProgressPercent(groupedItem.Count(), maxOperationCount), movementCount, arrivalCount, consumptionCount, $"{arrivalCount}/{movementCount}/{consumptionCount}");
                        PanelScore.Children.Add(operatorProgress);

                        summaryArrivalCount += arrivalCount;
                        summaryMovementCount += movementCount;
                        summaryConsumptionCount += consumptionCount;
                    }
                }

                maxOperationCount = Grid.Items.Count;
            }

            // Данные для прогресбаров по операциям
            {
                {
                    var operatorProgress = CreateOperatorProgressForOperation(
                        defaultPanelHeight, 
                        OperationTypeName[(int)OperationType.Arrival], 
                        $"{(int)OperationType.Arrival}",
                        summaryArrivalCount,
                        maxOperationCount,
                        0, 
                        summaryArrivalCount, 
                        0, 
                        summaryArrivalCount, 
                        summaryArrivalWeight
                        );
                    PanelScoreFooter.Children.Add(operatorProgress);
                }

                {
                    {
                        var operatorProgress = CreateOperatorProgressForOperation(
                            defaultPanelHeight, 
                            OperationTypeName[(int)OperationType.ArrivalFromPaperMachine],
                            $"{(int)OperationType.ArrivalFromPaperMachine}",
                            summaryArrivalFromPaperMachineCount, 
                            summaryArrivalCount, 
                            0, 
                            summaryArrivalFromPaperMachineCount, 
                            0, 
                            summaryArrivalFromPaperMachineCount, 
                            summaryArrivalFromPaperMachineWeight
                            );
                        PanelScoreFooter.Children.Add(operatorProgress);
                    }

                    {
                        var operatorProgress = CreateOperatorProgressForOperation(
                            defaultPanelHeight, 
                            OperationTypeName[(int)OperationType.ArrivalFromSupplier], 
                            $"{(int)OperationType.ArrivalFromSupplier}",
                            summaryArrivalFromSupplierCount, 
                            summaryArrivalCount, 
                            0, 
                            summaryArrivalFromSupplierCount, 
                            0, 
                            summaryArrivalFromSupplierCount, 
                            summaryArrivalFromSupplierWeight
                            );
                        PanelScoreFooter.Children.Add(operatorProgress);
                    }

                    {
                        var operatorProgress = CreateOperatorProgressForOperation(
                            defaultPanelHeight, 
                            OperationTypeName[(int)OperationType.ArrivalFromReturn],
                            $"{(int)OperationType.ArrivalFromReturn}",
                            summaryArrivalFromReturnCount, 
                            summaryArrivalCount, 
                            0, 
                            summaryArrivalFromReturnCount, 
                            0, 
                            summaryArrivalFromReturnCount, 
                            summaryArrivalFromReturnWeight
                            );
                        PanelScoreFooter.Children.Add(operatorProgress);
                    }
                }

                {
                    var operatorProgress = CreateOperatorProgressForOperation(
                        defaultPanelHeight, 
                        OperationTypeName[(int)OperationType.Consumption],
                        $"{(int)OperationType.Consumption}",
                        summaryConsumptionCount, 
                        maxOperationCount, 
                        0,
                        0, 
                        summaryConsumptionCount, 
                        summaryConsumptionCount, 
                        summaryConsumptionWeight
                        );
                    PanelScoreFooter.Children.Add(operatorProgress);
                }

                {
                    {
                        var operatorProgress = CreateOperatorProgressForOperation(
                            defaultPanelHeight, 
                            OperationTypeName[(int)OperationType.ConsumptionToFault], 
                            $"{(int)OperationType.ConsumptionToFault}",
                            summaryConsumptionToFaultCount, 
                            summaryConsumptionCount, 
                            0, 
                            0, 
                            summaryConsumptionToFaultCount, 
                            summaryConsumptionToFaultCount, 
                            summaryConsumptionToFaultWeight
                            );
                        PanelScoreFooter.Children.Add(operatorProgress);
                    }

                    {
                        var operatorProgress = CreateOperatorProgressForOperation(
                            defaultPanelHeight,
                            OperationTypeName[(int)OperationType.ConsumptionToShipment],
                            $"{(int)OperationType.ConsumptionToShipment}",
                            summaryConsumptionToShipmentCount, 
                            summaryConsumptionCount, 
                            0, 
                            0, 
                            summaryConsumptionToShipmentCount, 
                            summaryConsumptionToShipmentCount, 
                            summaryConsumptionToShipmentWeight
                            );
                        PanelScoreFooter.Children.Add(operatorProgress);
                    }

                    {
                        var operatorProgress = CreateOperatorProgressForOperation(
                            defaultPanelHeight,
                            OperationTypeName[(int)OperationType.ConsumptionToProduction], 
                            $"{(int)OperationType.ConsumptionToProduction}",
                            summaryConsumptionToProductionCount, 
                            summaryConsumptionCount, 
                            0, 
                            0, 
                            summaryConsumptionToProductionCount, 
                            summaryConsumptionToProductionCount, 
                            summaryConsumptionToProductionWeight
                            );
                        PanelScoreFooter.Children.Add(operatorProgress);
                    }
                }

                {
                    var operatorProgress = CreateOperatorProgressForOperation(
                        defaultPanelHeight, 
                        OperationTypeName[(int)OperationType.Movement],
                        $"{(int)OperationType.Movement}",
                        summaryMovementCount, 
                        maxOperationCount, 
                        summaryMovementCount, 
                        0, 
                        0, 
                        summaryMovementCount, 
                        summaryMovementWeight
                        );
                    PanelScoreFooter.Children.Add(operatorProgress);
                }

                {
                    {
                        var operatorProgress = CreateOperatorProgressForOperation(
                            defaultPanelHeight, 
                            OperationTypeName[(int)OperationType.MovementInner], 
                            $"{(int)OperationType.MovementInner}",
                            summaryMovementInnerCount, 
                            summaryMovementCount, 
                            summaryMovementInnerCount, 
                            0, 
                            0, 
                            summaryMovementInnerCount, 
                            summaryMovementInnerWeight
                            );
                        PanelScoreFooter.Children.Add(operatorProgress);
                    }

                    {
                        var operatorProgress = CreateOperatorProgressForOperation(
                            defaultPanelHeight, 
                            OperationTypeName[(int)OperationType.MovementToCorrugatorMachine], 
                            $"{(int)OperationType.MovementToCorrugatorMachine}",
                            summaryMovementToCorrugatorMachineCount, 
                            summaryMovementCount, 
                            summaryMovementToCorrugatorMachineCount, 
                            0, 
                            0, 
                            summaryMovementToCorrugatorMachineCount, 
                            summaryMovementToCorrugatorMachineWeight
                            );
                        PanelScoreFooter.Children.Add(operatorProgress);
                    }

                    {
                        var operatorProgress = CreateOperatorProgressForOperation(
                            defaultPanelHeight, 
                            OperationTypeName[(int)OperationType.MovementFromPaperMachine], 
                            $"{(int)OperationType.MovementFromPaperMachine}",
                            summaryMovementFromPaperMachineCount, 
                            summaryMovementCount, 
                            summaryMovementFromPaperMachineCount, 
                            0, 
                            0, 
                            summaryMovementFromPaperMachineCount, 
                            summaryMovementFromPaperMachineWeight
                            );
                        PanelScoreFooter.Children.Add(operatorProgress);
                    }

                    {
                        var operatorProgress = CreateOperatorProgressForOperation(
                            defaultPanelHeight, 
                            OperationTypeName[(int)OperationType.MovementToPaperMachine], 
                            $"{(int)OperationType.MovementToPaperMachine}",
                            summaryMovementToPaperMachineCount, 
                            summaryMovementCount, 
                            summaryMovementToPaperMachineCount, 
                            0, 
                            0,
                            summaryMovementToPaperMachineCount,
                            summaryMovementToPaperMachineWeight
                            );
                        PanelScoreFooter.Children.Add(operatorProgress);
                    }

                    {
                        var operatorProgress = CreateOperatorProgressForOperation(
                            defaultPanelHeight, 
                            OperationTypeName[(int)OperationType.MovementFromCorrugatorMachine], 
                            $"{(int)OperationType.MovementFromCorrugatorMachine}",
                            summaryMovementFromCorrugatorMachineCount,
                            summaryMovementCount,
                            summaryMovementFromCorrugatorMachineCount,
                            0,
                            0,
                            summaryMovementFromCorrugatorMachineCount,
                            summaryMovementFromCorrugatorMachineWeight
                            );
                        PanelScoreFooter.Children.Add(operatorProgress);
                    }
                }
            }
        }

        private OperatorProgress CreateOperatorProgressForOperation(int height, string description, object tag, int currentValue, int maxValue, int move, int arrival, int writeOff, int count, int weight)
        {
            OperatorProgress operatorProgress = new OperatorProgress();
            operatorProgress.Height = height;
            operatorProgress.Description.Text = description;
            operatorProgress.Tag = tag;
            operatorProgress.OnMouseDown += OperatorProgressMouseDown;
            operatorProgress.Buf1PercentRow.Width = new GridLength(3, GridUnitType.Star);
            operatorProgress.Buf1DataRow.Width = new GridLength(2, GridUnitType.Star);
            operatorProgress.SetProgress2(
                OperatorProgress.CalculateProgressPercent(currentValue, maxValue), 
                move, 
                arrival, 
                writeOff,
                $"{count.ToString("#,###,###,##0")}шт.",
                $"{weight.ToString("#,###,###,##0")}кг."
                );
            return operatorProgress;
        }

        private OperatorProgress CreateOperatorProgressForEmployee(int height, string description, object tag)
        {
            OperatorProgress operatorProgress = new OperatorProgress();
            operatorProgress.Height = height;
            operatorProgress.Description.Text = description;
            operatorProgress.Tag = tag;
            operatorProgress.OnMouseDown += OperatorProgressMouseDown;
            operatorProgress.Buf1PercentRow.Width = new GridLength(3, GridUnitType.Star);
            operatorProgress.Buf1DataRow.Width = new GridLength(2, GridUnitType.Star);
            return operatorProgress;
        }

        private void OperatorProgressMouseDown(object sender, MouseEventArgs e)
        {
            if (sender is OperatorProgress)
            {
                OperatorProgress operatorProgress = sender as OperatorProgress;

                if (e.RightButton == MouseButtonState.Pressed)
                {
                    System.Windows.Clipboard.SetText(operatorProgress.Description2.Text);
                }
                else
                {
                    string key = operatorProgress.Tag.ToString();
                    if (key.ToInt() == 0)
                    {
                        OperationTypeSelectBox.SetSelectedItemByKey("0");
                        string description = operatorProgress.Description.Text;
                        if (SearchText.Text == description)
                        {
                            SearchText.Text = "";
                        }
                        else
                        {
                            SearchText.Text = description;
                        }

                        Grid.UpdateItems();
                    }
                    else
                    {
                        if (OperationTypeSelectBox.SelectedItem.Key == key)
                        {
                            OperationTypeSelectBox.SetSelectedItemByKey("0");
                        }
                        else
                        {
                            OperationTypeSelectBox.SetSelectedItemByKey(key);
                        }
                    }
                }
            }
        }

        private void EnableSplash(string message)
        {
            SplashControl.Message = message;
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

        private void OnCurrentShift(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now;
            if (date.Hour >= 20 || date.Hour < 8)
            {
                if (date.Hour >= 20)
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.AddDays(1).ToString("dd.MM.yyyy")} 08:00:00");
                }
                else
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 08:00:00");
                }
            }
            else
            {
                Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 08:00:00");
                Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 20:00:00");
            }

            Refresh();
        }

        private void OnPrevShift(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now.AddHours(-12);
            if (date.Hour >= 20 || date.Hour < 8)
            {
                if (date.Hour >= 20)
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{date.ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{date.AddDays(1).ToString("dd.MM.yyyy")} 08:00:00");
                }
                else
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{date.AddDays(-1).ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{date.ToString("dd.MM.yyyy")} 08:00:00");
                }
            }
            else
            {
                Form.SetValueByPath("FROM_DATE_TIME", $"{date.ToString("dd.MM.yyyy")} 08:00:00");
                Form.SetValueByPath("TO_DATE_TIME", $"{date.ToString("dd.MM.yyyy")} 20:00:00");
            }

            Refresh();
        }

        private void OnCurrentDay(object sender, RoutedEventArgs e)
        {
            Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.AddDays(1).ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void OnPrevDay(object sender, RoutedEventArgs e)
        {
            Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void OnCurrentWeek(object sender, RoutedEventArgs e)
        {
            DayOfWeek day = DateTime.Now.DayOfWeek;
            int days = day - DayOfWeek.Monday;
            DateTime date = DateTime.Now.AddDays(-days);

            Form.SetValueByPath("FROM_DATE_TIME", $"{date.Date.ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{date.Date.AddDays(7).ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void OnPrevWeek(object sender, RoutedEventArgs e)
        {
            DayOfWeek day = DateTime.Now.DayOfWeek;
            int days = day - DayOfWeek.Monday;
            DateTime date = DateTime.Now.AddDays(-days).AddDays(-7);

            Form.SetValueByPath("FROM_DATE_TIME", $"{date.Date.ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{date.Date.AddDays(7).ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void OnCurrentMonth(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now;

            Form.SetValueByPath("FROM_DATE_TIME", $"{new DateTime(date.Year, date.Month, 1).ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{new DateTime(date.Year, date.Month, 1).AddMonths(1).ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void OnPrevMonth(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now.AddMonths(-1);

            Form.SetValueByPath("FROM_DATE_TIME", $"{new DateTime(date.Year, date.Month, 1).ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{new DateTime(date.Year, date.Month, 1).AddMonths(1).ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void OperationTypeSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }
    }
}
