using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Client.Interfaces.Production.Corrugator._CorrugatorMachineOperator.Frames;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.Corrugator.CorrugatorMachineOperator
{
    /// <summary>
    /// Список заданий на конкретном станке с крупным примечанием, если таковое есть
    /// </summary>
    /// <author>vlasov_ea</author>   
    public partial class MachineTaskQueue : ControlBase
    {
        public MachineTaskQueue()
        {
            InitializeComponent();

            AccessMode = Central.Navigator.GetRoleLevel(CorrugatorMachineOperator.Role);
        }
        
        /// <summary>
        /// уровень доступа пользователя к интерфейсу
        /// </summary>
        Role.AccessMode AccessMode { get; set; }

        /// <summary>
        /// Инициализация грида
        /// </summary>
        public void Init()
        {
            IsQueueLoading = false;
            SelectedTaskIndexes = new Dictionary<string, string>();
            TaskGridInit();

            TaskGridInfoInit();

            TaskGridLoadItems();

            double nScale = 1.5;
            TaskGrid.LayoutTransform = new ScaleTransform(nScale, nScale);
        }

        public delegate void ItemChange(Dictionary<string, string> item);
        public event ItemChange OnItemChange;

        public delegate void SelectedTasksChange();
        public event SelectedTasksChange OnSelectedTasksChange;

        /// <summary>
        /// Публичный метод для уведомления об изменении выбранных заданий
        /// </summary>
        public void NotifySelectedTasksChanged()
        {
            OnSelectedTasksChange?.Invoke();
        }


        // список зпдпний которые не должны показываться в гриде
        private List<int> deletedTask { get; set; } = new List<int>();

        /// <summary>
        /// Очередь в процессе загрузки
        /// </summary>
        public bool IsQueueLoading { get; set; }

        private ListDataSet DataSet { get; set; }

        public Dictionary<string, string> SelectedTaskItem { get; set; }

        /// <summary>
        /// Кэш для чекбоксов слева
        /// </summary>
        public Dictionary<string, string> SelectedTaskIndexes { get; set; }

        /// <summary>
        /// Сохраненная позиция скролла для восстановления после обновления
        /// </summary>
        private double SavedScrollPosition { get; set; } = 0;

        ///// <summary>
        ///// Элементы грида
        ///// </summary>
        //public List<Dictionary<string, string>> Items
        //{
        //    get => TaskGrid.Items;
        //}

        /// <summary>
        /// Событие на обновление грида очереди станка
        /// </summary>
        public event Action OnAfterLoadQueue;

        /// <summary>
        /// инициализация грида TaskGrid
        /// </summary>
        public void TaskGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header = "*",
                        Path="_SELECTED",
                        ColumnType=ColumnTypeRef.Boolean,
                        Editable = true,
                        Width2 = 1,
                        Labels = TaskHelper.CreateLabel("_SELECTED"),
                        
                        OnClickAction = (row, el) =>
                        {
                            if(CorrugatorMachineOperator.IsCurrentMachineSelected)
                            {
                                // если выбран текущий станок
                                // и выбирается задание, которое можно перемещать
                                if(CorrugatorMachineOperator.IsCurrentMachineSelected
                                    && row.CheckGet("_ROWNUMBER").ToInt() > CorrugatorMachineOperator.NumberOfUntouchableTasks
                                    )
                                {
                                    // тогда ставим/снимаем галочку
                                    var idTask = row.CheckGet("ID_PZ").ToInt();
                                    if (row["_SELECTED"].ToInt() == 0)
                                    {
                                        SelectedTaskIndexes[idTask.ToString()] = "true";
                                    }
                                    else
                                    {
                                        SelectedTaskIndexes[idTask.ToString()] = "false";
                                    }

                                    // Уведомляем об изменении выбранных заданий
                                    NotifySelectedTasksChanged();

                                    return true;
                                }
                                else
                                {
                                    var idTask = row.CheckGet("ID_PZ").ToInt();
                                    SelectedTaskIndexes[idTask.ToString()] = "false";

                                    return false;
                                }
                            }
                            else
                            {
                                return false;
                            }
                        }
                    },
                    new DataGridHelperColumn
                    {
                        Header="Начать до",
                        Path="START_BEFORE",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 8,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskHelper.GetColor("START_BEFORE", row)
                            },
                        },
                        Labels = TaskHelper.CreateLabel("START_BEFORE"),
                        
                    },
                    new DataGridHelperColumn
                    {
                        Header="ПЗ",
                        Path="NUM",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 9,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskHelper.GetColor("NUM", row)
                            },
                        },
                        Labels = TaskHelper.CreateLabel("NUM"),
                        
                    },
                    new DataGridHelperColumn
                    {
                        Header="Длина, м",
                        Path="LEN",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 6,
                        Labels = TaskHelper.CreateLabel("LEN"),
                        
                    },
                    new DataGridHelperColumn
                    {
                        Header="Профиль",
                        Path="PROFIL_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 2,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskHelper.GetColor("PROFIL_NAME", row)
                            },
                        },
                        Labels = TaskHelper.CreateLabel("PROFIL_NAME"),
                        
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вал 1",
                        Path="NAME_ROLL_1",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 2,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskHelper.GetColor("NAME_ROLL1", row)
                            },
                        },
                        Labels = TaskHelper.CreateLabel("NAME_ROLL1"),
                        
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вал 2",
                        Path="NAME_ROLL_2",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 2,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskHelper.GetColor("NAME_ROLL2", row)
                            },
                        },
                        Labels = TaskHelper.CreateLabel("NAME_ROLL2"),
                        
                    },
                    new DataGridHelperColumn
                    {
                        Header="Станок",
                        Path="MACHINE",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 18,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskHelper.GetColor("SNAME", row)
                            },
                        },
                        Labels = TaskHelper.CreateLabel("MACHINE")
                    },
                    new DataGridHelperColumn
                    {
                        Header="Формат",
                        Path="WEB_WIDTH",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 6,
                        Labels = TaskHelper.CreateLabel("WEB_WIDTH")
                    },
                    new DataGridHelperColumn
                    {
                        Header="1",
                        Path="LAYER_1",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 9,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskHelper.GetColor("LAYER_1", row)
                            },
                        },
                        Labels = TaskHelper.CreateLabel("LAYER_1"),
                    },
                    new DataGridHelperColumn
                    {
                        Header="2",
                        Path="LAYER_2",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 9,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskHelper.GetColor("LAYER_2", row)
                            },
                        },
                        Labels = TaskHelper.CreateLabel("LAYER_2"),
                    },
                    new DataGridHelperColumn
                    {
                        Header="3",
                        Path="LAYER_3",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 9,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskHelper.GetColor("LAYER_3", row)
                            },
                        },
                        Labels = TaskHelper.CreateLabel("LAYER_3"),
                    },
                    new DataGridHelperColumn
                    {
                        Header="4",
                        Path="LAYER_4",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 9,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskHelper.GetColor("LAYER_4", row)
                            },
                        },
                        Labels = TaskHelper.CreateLabel("LAYER_4"),
                    },
                    new DataGridHelperColumn
                    {
                        Header="5",
                        Path="LAYER_5",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 9,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskHelper.GetColor("LAYER_5", row)
                            },
                        },
                        Labels = TaskHelper.CreateLabel("LAYER_5"),
                    },
                    new DataGridHelperColumn
                    {
                        Header="Скорость, м/мин",
                        Path="SPEED",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 3,
                        Labels = TaskHelper.CreateLabel("SPEED"),
                    },
                    new DataGridHelperColumn
                    {
                        Header="Обрезь, мм",
                        Path="OBREZ",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 3,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskHelper.GetColor("OBREZ", row)
                            },
                        },
                        Labels = TaskHelper.CreateLabel("OBREZ")
                    },
                    new DataGridHelperColumn
                    {
                        Header="Композиция",
                        Path="QID",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 9,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskHelper.GetColor("CHECK_QID", row)
                            },
                        },
                        Labels = TaskHelper.CreateLabel("QID"),
                    },
                    new DataGridHelperColumn
                    {
                        Header="Смещение клише",
                        Path="FANFOLD_PRINT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 2,
                        Labels = TaskHelper.CreateLabel("FANFOLD_PRINT"),                        
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер клише",
                        Path="NKLISHE",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 2,
                        Labels = TaskHelper.CreateLabel("NKLISHE"),                        
                    },
                    new DataGridHelperColumn
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 2,
                        Labels = TaskHelper.CreateLabel("_ROWNUMBER"),                        
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД ПЗ",
                        Path="ID_PZ",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 7,
                        Labels = TaskHelper.CreateLabel("ID_PZ"),                        
                    },
                    new DataGridHelperColumn
                    {
                        Header="Толщина, мм",
                        Path="TOTAL_THICKNESS",
                        ColumnType=ColumnTypeRef.Double,
                        Width2 = 5,
                        Format = "N3",
                        Labels = TaskHelper.CreateLabel("TOTAL_THICKNESS"),                        
                    },
                    new DataGridHelperColumn
                    {
                        Header="Цвет",
                        Path="COLOR",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 2,
                        Labels = TaskHelper.CreateLabel("COLOR"),                        
                    },
                    new DataGridHelperColumn
                    {
                        Header="Стопа на фенфолд",
                        Path="KOL_PAK",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 2,
                        Labels = TaskHelper.CreateLabel("KOL_PAK"),
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество, шт",
                        Path="QTY",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 4,
                        Labels = TaskHelper.CreateLabel("QTY"),
                        
                    },
                    new DataGridHelperColumn
                    {
                        Header="Лаборатория",
                        Path="TESTING_FLAG",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 4,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "NOTE",
                        Path = "NOTE",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 4,
                        Visible = false
                    },
                    new DataGridHelperColumn
                    {
                        Header = "TWICE_WHITE_RAW",
                        Path = "TWICE_WHITE_RAW",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 4,
                        Visible = false
                    },
                    new DataGridHelperColumn
                    {
                        Header = "FILL_PRINTING_FLAG",
                        Path = "FILL_PRINTING_FLAG",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 4,
                        Visible = false
                    },
                    new DataGridHelperColumn
                    {
                        Header = "RAWS_TO_START_SPLICER",
                        Path = "RAWS_TO_START_SPLICER",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 4,
                        Visible = false
                    },
                    new DataGridHelperColumn
                    {
                        Header = "RILEVKI",
                        Path = "RILEVKI",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 4,
                        Visible = false
                    },
                    new DataGridHelperColumn
                    {
                        Header = "STYPE",
                        Path = "STYPE",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 4,
                        Visible = false
                    },
                    
                };

                if (Central.DebugMode)
                {
                    columns.Add(new DataGridHelperColumn
                    {
                        Header = "FOSBER_NUM",
                        Path = "FOSBER_NUM",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2 = 5,
                        Labels = TaskHelper.CreateLabel("FOSBER_NUM"),
                        // OnRender = (row, el) => TaskHelper.GetIcons("", row),
                    });
                }

                TaskGrid.SetColumns(columns);
                TaskGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                TaskGrid.AutoUpdateInterval = 0;
                TaskGrid.SetPrimaryKey("_ROWNUMBER");
                TaskGrid.EnableSortingGrid = false;

                TaskGrid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    { "showmap", new DataGridContextMenuItem(){
                        Header="Карта ПЗГА",
                        Action=()=>
                        {
                            ShowProductionTaskMap();
                        }
                    }},
                    { "s1", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    { "ChangeMfRolls", new DataGridContextMenuItem(){
                        Header="Сменить валы гофрирования",
                        Action=()=>
                        {
                            ChangeMfRolls(new List<Dictionary<string, string>>() { SelectedTaskItem });
                        }
                    }},
                    { "s2", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    { "Delete", new DataGridContextMenuItem(){
                        Header="Удалить задание из очереди",
                        Action=()=>
                        {
                            DeleteTask(0);
                        }
                    }},
                    { "DeleteSelected", new DataGridContextMenuItem(){
                        Header="Удалить выбранные задания",
                        Action=()=>
                        {
                            DeleteSelectedTasks(0);
                        }
                    }},
                    
                    { "s3", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    { "UP", new DataGridContextMenuItem(){
                        Header="Перемеcтить выделенные в начало",
                        Action=()=>
                        {
                            MoveTask("UP");
                        }
                    }},
                    { "up", new DataGridContextMenuItem(){
                        Header="Перемеcтить выделенные выше",
                        Action=()=>
                        {
                            MoveTask("up");
                        }
                    }},
                    { "down", new DataGridContextMenuItem(){
                        Header="Перемеcтить выделенные ниже",
                        Action=()=>
                        {
                            MoveTask("down");
                        }
                    }},

                    { "s4", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    

                    { "End", new DataGridContextMenuItem(){
                        Header="Закрыть задание",
                        Action=()=>
                        {
                            DeleteTask(1);
                        }
                    }},

                    { "s5", new DataGridContextMenuItem(){
                        Header="-",
                    }},

                    {
                        "techk", new DataGridContextMenuItem()
                        {
                            Header="Техкарта",
                            Items = new Dictionary<string, DataGridContextMenuItem>()
                            {
                                {
                                    "stack1", new DataGridContextMenuItem()
                                    {
                                        Header = "Стекер 1",
                                        Action=()=>{ ShowTKStacker(1); }
                                    }
                                },
                                {
                                    "stack2", new DataGridContextMenuItem()
                                    {
                                        Header = "Стекер 2",
                                        Action=()=>{ ShowTKStacker(2); }
                                    }
                                },
                                {
                                    "stack3", new DataGridContextMenuItem()
                                    {
                                        Header = "Стекер 3",
                                        Action=()=>{ ShowTKStacker(3); }
                                    }
                                }
                            }
                        }
                    },
                };

                // Раскраска строк
                TaskGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    {
                        StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            // задание не добавилось на фосбер
                            if (CorrugatorMachineOperator.SelectedMachineId == 22)
                            {
                                string fosber_answer = row.CheckGet("FOSBER_ANSWER");

                                if(!string.IsNullOrEmpty(fosber_answer))
                                {
                                    if (fosber_answer.Length < 4
                                     || fosber_answer.Substring(1,2) != "OK")
                                    {
                                        //ошибка уникальности RunId1, это задание уже есть в очереди
                                        //такую ошибку подсвечивать не нужно
                                        if(fosber_answer.Substring(1,2) != "DU")
                                        {
                                            color=HColor.Red;
                                        }
                                    }
                                }
                            }

                            if(color=="")
                            {
                                int crt_id_pz = row.CheckGet("CRT_ID_PZ").ToInt();
                                int id_pz = row.CheckGet("ID_PZ").ToInt();

                                if(crt_id_pz!=0)
                                {
                                    if(id_pz==crt_id_pz)
                                    {
                                        color = HColor.Blue;
                                    }
                                }
                            }

                            if (!color.IsNullOrEmpty())
                            {
                                result=color.ToBrush();
                            }
                            return result;
                        }
                    },
                    {
                        StylerTypeRef.FontWeight, 
                        row =>
                        {
                            var result = FontWeights.Bold;

                            return result;
                        }
                    }
                };

                //при выборе строки в гриде, обновляются актуальные действия для записи
                TaskGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem.Count > 0)
                    {
                        SelectedTaskItem = selectedItem;
                        UpdateTaskGridActions(selectedItem);
                        UpdateNote(selectedItem);

                        TaskGridInfoLoadItems();

                        OnItemChange?.Invoke(SelectedTaskItem);
                    }
                };

                //данные грида
                // TaskGrid.OnLoadItems = TaskGridLoadItems;
                TaskGrid.OnFilterItems = TaskGridFilter;

                TaskGrid.Init();
            }
        }

        private async void ShowTKStacker(int stacker)
        {
            if(SelectedTaskItem!=null)
            {
                int ProductionOrderId = SelectedTaskItem.CheckGet("ID_PZ").ToInt();

                if (ProductionOrderId > 0)
                {
                    var p = new Dictionary<string, string>();
                    {
                        p.CheckAdd("ID_PZ", ProductionOrderId.ToString());
                        p.CheckAdd("CUTOFF_ALLOCATION", stacker.ToString());
                    }
                    
                    try
                    {
                        var q = await LPackClientQuery.DoQueryAsync("Production", "TaskQueue", "TaskQueueSelectTechnikalMap", "ITEMS", p);

                        if (q.Answer.Status == 0)
                        {
                            if (q.Answer.QueryResult!=null)
                            {
                                var ds = q.Answer.QueryResult;

                                if (ds.Items.Count > 0)
                                {
                                    string fileName = ds.Items[0].CheckGet("PATHTK");
                                    Central.OpenFile(fileName);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        CorrugatorErrors.LogError(ex);
                    }
                }
            }
        }

        public void TaskGridInfoInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="IDORDERDATES",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 1,
                        Labels = TaskHelper.CreateLabel("IDORDERDATES"),
                    },
                    new DataGridHelperColumn
                    {
                        Header="Стекер",
                        Path="CUTOFF_ALLOCATION",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 2,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул",
                        Path="ARTIKUL",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Лаборатория",
                        Path="TESTING_FLAG",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 2,
                    },
                    new DataGridHelperColumn
                    {
                        Header="По ПЗ, шт",
                        Path="KOL",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 3,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Всего сделано по заявке, шт",
                        Path="DONE_QTY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 3,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Всего по заявке в выполненых ПЗ, шт",
                        Path="PZ_DONE_QTY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 3,
                    },
                    new DataGridHelperColumn
                    {
                        Header="% брака",
                        Path="DEFECT_PCT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 3,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Всего по заявке, шт",
                        Path="PZ_QTY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 3,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ПЗ выполнено",
                        Path="DONE_CNT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 3,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Всего ПЗ",
                        Path="CNT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 3,
                    },
                };


                TaskGridInfo.SetColumns(columns);
                TaskGridInfo.AutoUpdateInterval = 0;
                TaskGridInfo.SetPrimaryKey("IDORDERDATES");
                TaskGridInfo.OnLoadItems = TaskGridInfoLoadItems;

                TaskGridInfo.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    { "search_task_production", new DataGridContextMenuItem(){
                        Header="Поиск по артиклу",
                        Action=()=>
                        {
                            ShowWindowWithTask(new List<Dictionary<string, string>>() { TaskGridInfo.SelectedItem });
                        }
                    }},
                };
                
                TaskGridInfo.Init();
            }
        }

        /// <summary>
        /// Поиск кол-во заказов
        /// </summary>
        /// <param name="selectedTasks">Выбранный элемент в гриде</param>
        private void ShowWindowWithTask(List<Dictionary<string, string>> selectedTasks)
        {
            var searchWindow = new SearchProductionTask();
            searchWindow.ShowWindow(selectedTasks[0].CheckGet("IDORDERDATES").ToInt(), selectedTasks[0].CheckGet("ARTIKUL"));
        }

        /// <summary>
        /// для предотвращения повторяющихся запросов
        /// </summary>
        private int PrevIdPz { get; set; } = 0;

        private async void TaskGridInfoLoadItems()
        {
            if(SelectedTaskItem!=null)
            {
                int ProductionOrderId = SelectedTaskItem.CheckGet("ID_PZ").ToInt();
                if (ProductionOrderId > 0)
                {
                    if (PrevIdPz != ProductionOrderId)
                    {
                        PrevIdPz = ProductionOrderId;

                        var p = new Dictionary<string, string>();
                        {
                            p.CheckAdd("ID_PZ", ProductionOrderId.ToString());
                        }

                        try
                        {
                            var q = await LPackClientQuery.DoQueryAsync("Production", "TaskQueue", "ListAdditionalInfoByIdPz", "ITEMS", p);

                            if (q.Answer.Status == 0)
                            {

                                if (q.Answer.QueryResult != null)
                                {
                                    var ds = q.Answer.QueryResult;
                                    TaskGridInfo.UpdateItems(ds);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            CorrugatorErrors.LogError(ex);
                        }
                    }
                }
            }
            
        }



        /// <summary>
        /// Загрузка очереди заданий выбранного станка 
        /// </summary>
        public async void TaskGridLoadItems()
        {
            // Подсчет есть ли выделенные записи
            var countSelectedStatusTrue = SelectedTaskIndexes.Values.Count(x => x == "true" || x == "1");
            
            //проверка, если уже идёт загрузка очереди
            if (!IsQueueLoading && countSelectedStatusTrue==0)
            {
                IsQueueLoading = true;
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID_ST", CorrugatorMachineOperator.SelectedMachineId.ToString());
                }

                try
                {

                    var q = await LPackClientQuery.DoQueryAsync("Production", "TaskQueue", "List", "TASKS", p);

                    if (q.Answer.Status == 0)
                    {
                        if(q.Answer.QueryResult!=null)
                        { 
                            DataSet = q.Answer.QueryResult;

                            if(DataSet !=null)
                            {
                                if (TaskGrid.Items.Count > 0)
                                {
                                    if (DataSet.Items[0].CheckGet("POSTING").ToInt() == 1 &&
                                        TaskParameters.textTaskNum != DataSet.Items[0].CheckGet("NUM"))
                                    {
                                        DeleteCompleteTask(DataSet.Items[0].CheckGet("ID_PZ"));
                                    }
                                    if (TaskGrid.Items.Count != DataSet.Items.Count)
                                    {
                                        LoadDataSet(DataSet);
                                        TaskGrid.SelectRowFirst();
                                    }
                                    else
                                    {
                                        for (int i = 0; i < DataSet.Items.Count; i++)
                                        {
                                            if (DataSet.Items[i].CheckGet("ID_PZ").ToInt() ==
                                                TaskGrid.Items[i].CheckGet("ID_PZ").ToInt() &&
                                                DataSet.Items[i].CheckGet("NAME_ROLL_1") ==
                                                TaskGrid.Items[i].CheckGet("NAME_ROLL_1") &&
                                                DataSet.Items[i].CheckGet("NAME_ROLL_2") ==
                                                TaskGrid.Items[i].CheckGet("NAME_ROLL_2"))
                                            {
                                            }
                                            else
                                            {
                                                LoadDataSet(DataSet);
                                                break;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    LoadDataSet(DataSet);
                                }
                            }
                        }
                        
                        SelectedTaskIndexes.Clear();
                        NotifySelectedTasksChanged();
                    }
                }
                catch(Exception ex)
                {
                    CorrugatorErrors.LogError(ex);
                }

                IsQueueLoading = false;

            }
        }

        /// <summary>
        /// Функция которая корректирует ошибки джоба (попатка воздействовать на первые 3 задания)
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private async Task DeleteCompleteTask(string idPz)
        {
            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_ST", CorrugatorMachineOperator.SelectedMachineId.ToString());
                p.CheckAdd("ID_PZ", idPz);
                p.CheckAdd("IS_COMPLETED", "1");
            }
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "TaskQueue");
            q.Request.SetParam("Action", "DeleteTaskAction");
            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                TaskGridLoadItems();
            }
        }

        /// <summary>
        /// Загрузка датасета в грид
        /// </summary>
        /// <param name="dataSet"></param>
        private void LoadDataSet(ListDataSet dataSet)
        {
            int i, n = CorrugatorMachineOperator.NumberOfUntouchableTasks + 1;
            if (n > DataSet.Items.Count) n = DataSet.Items.Count;

            for(i=0;i<n;i++)
            {
                int taskId = DataSet.Items[i].CheckGet("ID_PZ").ToInt();

                if (SelectedTaskIndexes.ContainsKey(taskId.ToString()))
                {
                    SelectedTaskIndexes.Remove(taskId.ToString());
                }
            }

            TaskGrid.UpdateItems(DataSet);
            OnAfterLoadQueue?.Invoke();
        }

        /// <summary>
        /// Фильтрация
        /// </summary>
        public void TaskGridFilter()
        {
            if (TaskGrid.Items != null)
            {
                foreach (var item in TaskGrid.Items)
                {
                    string originalNote = item.CheckGet("NOTE");
                    string primNote = item.CheckGet("PRIM");

                    if (!primNote.IsNullOrEmpty())
                    {
                        string cleanNote = originalNote.Replace($" {primNote}", "").Replace(primNote, "").Trim();
                        item["NOTE"] = $"{cleanNote} {primNote}".Trim();
                    }
                    else
                    {
                        item["NOTE"] = originalNote.Trim();
                    }

                    // Проставляем сохранённые чекбоксы
                    int idTask = item.CheckGet("ID_PZ").ToInt();
                    if (SelectedTaskIndexes.ContainsKey(idTask.ToString()) &&
                        SelectedTaskIndexes[idTask.ToString()].ToBool())
                    {
                        item["_SELECTED"] = "1";
                    }
                    else
                    {
                        item["_SELECTED"] = "0";
                    }
                }

                CheckWebWidthNoetic();
            }
        }

        /// <summary>
        /// Сохранение состояния чекбоксов перед каким либо действием со списком
        /// </summary>
        private void SaveSelectedTasks()
        {
            if (TaskGrid.Items != null)
            {
                foreach (var item in TaskGrid.Items)
                {
                    int idTask = item.CheckGet("ID_PZ").ToInt();

                    if (item.CheckGet("_SELECTED").ToInt() == 1)
                    {
                        SelectedTaskIndexes[idTask.ToString()] = "1";
                    }
                }
            }
        }

        
        
        /// <summary>
        /// Обновление примечания
        /// </summary>
        public void UpdateNote(Dictionary<string, string> selectedItem)
        {
            string note = selectedItem.CheckGet("NOTE");

            note = note.Replace(TaskHelper.DecodeIconTexts.CheckGet("НРП"), "");
            note = note.Replace(TaskHelper.DecodeIconTexts.CheckGet("ДНК"), "");
            note = note.Replace(TaskHelper.DecodeIconTexts.CheckGet("ПП"), "");

            note = note.Trim(new char[] { '.', ',', ' ' });

            if (!note.IsNullOrEmpty())
            {
                note = $"Примечание: {note}";
                //NoteBorder.Height = double.NaN;
                NoteBorder.Visibility = Visibility.Visible;
            }
            else
            {
                //NoteBorder.Height = 0;
                NoteBorder.Visibility = Visibility.Collapsed;
            }

            NoteTextBlock.Text = note;
        }

        /// <summary>
        /// Если идут подряд задания с одинаковой композицией и одинаковым форматом, но с разным сырьём, 
        /// то их реальные форматы должны отличаться на 1мм, 
        /// чтобы на БХС сработал слайсер
        /// </summary>
        public void CheckWebWidthNoetic()
        {
            if (TaskGrid.Items != null)
            {
                if (TaskGrid.Items.Count > 1)
                {
                    for (int i = 1; i < TaskGrid.Items.Count; i++)
                    {
                        var task0 = TaskGrid.Items[i - 1];
                        var task = TaskGrid.Items[i];

                        int webWidthNoetic0 = task0.CheckGet("WEB_WIDTH_NOETIC").ToInt();
                        int webWidth = task.CheckGet("WEB_WIDTH").ToInt();

                        // для каких слоёв сырья надо включить сплайсер вручную
                        string rawsToStartSplicerStr = "";

                        if (task0.CheckGet("QID") == task.CheckGet("QID")
                            && task0.CheckGet("WEB_WIDTH") == task.CheckGet("WEB_WIDTH")
                            )
                        {
                            if (task0.CheckGet("LAYER_1") != task.CheckGet("LAYER_1")
                                || task0.CheckGet("LAYER_2") != task.CheckGet("LAYER_2")
                                || task0.CheckGet("LAYER_3") != task.CheckGet("LAYER_3")
                                || task0.CheckGet("LAYER_4") != task.CheckGet("LAYER_4")
                                || task0.CheckGet("LAYER_5") != task.CheckGet("LAYER_5")
                                )
                            {
                                // если предыдущий реальный формат не увеличенный, тогда текущий увеличиваем на 1мм, чтобы на БХС сработал сплайсер
                                if (webWidthNoetic0 % 2 == 0)
                                {
                                    webWidth++;
                                }
                                // иначе если предыдущий реальный формат уже увеличенный, тогда текущий оставляем неувеличенным, чтобы форматы отличались и на БХС сработал сплайсер

                                task["WEB_WIDTH_NOETIC"] = webWidth.ToString();

                                if (task0.CheckGet("LAYER_1") != task.CheckGet("LAYER_1"))
                                {
                                    rawsToStartSplicerStr += "1";
                                }
                                if (task0.CheckGet("LAYER_2") != task.CheckGet("LAYER_2"))
                                {
                                    rawsToStartSplicerStr += "2";
                                }
                                if (task0.CheckGet("LAYER_3") != task.CheckGet("LAYER_3"))
                                {
                                    rawsToStartSplicerStr += "3";
                                }
                                if (task0.CheckGet("LAYER_4") != task.CheckGet("LAYER_4"))
                                {
                                    rawsToStartSplicerStr += "4";
                                }
                                if (task0.CheckGet("LAYER_5") != task.CheckGet("LAYER_5"))
                                {
                                    rawsToStartSplicerStr += "5";
                                }
                            }
                        }
                        task["RAWS_TO_START_SPLICER"] = rawsToStartSplicerStr;
                    }
                }
            }
        }

        /// <summary>
        /// Удаление задания из очереди станка
        /// </summary>
        public void DeleteTask(int isCompleted)
        {
            DeleteTask(SelectedTaskItem, isCompleted);
        }

        /// <summary>
        /// Массовое удаление выбранных заданий из очереди станка
        /// </summary>
        public async void DeleteSelectedTasks(int isCompleted)
        {
            if (CorrugatorMachineOperator.IsCurrentMachineSelected)
            {
                // Получаем список выбранных заданий
                var selectedTasks = TaskGrid.Items?.Where(task => task.CheckGet("_SELECTED").ToInt() == 1).ToList();

                if (selectedTasks == null || selectedTasks.Count == 0)
                {
                    var noSelectionDialog = new DialogWindow("Не выбрано ни одного задания для удаления", "Удаление заданий", "", DialogWindowButtons.OK);
                    noSelectionDialog.ShowDialog();
                    return;
                }

                // Подтверждение удаления
                string taskNumbers = string.Join(", ", selectedTasks.Select(task => task.CheckGet("NUM")));
                var confirmDialog = new DialogWindow(
                    $"Вы действительно хотите {(isCompleted == 1 ? "закрыть" : "удалить")} {selectedTasks.Count} заданий?\n\nЗадания: {taskNumbers}",
                    isCompleted == 1 ? "Закрытие заданий!" : "Удаление заданий",
                    "",
                    DialogWindowButtons.YesNo);

                confirmDialog.ShowDialog();
                if (confirmDialog.DialogResult == true)
                {
                    int successCount = 0;
                    int errorCount = 0;
                    var errorMessages = new List<string>();

                    // Удаляем задания по одному
                    foreach (var taskItem in selectedTasks)
                    {
                        try
                        {
                            var p = new Dictionary<string, string>();
                            {
                                p.CheckAdd("ID_ST", CorrugatorMachineOperator.SelectedMachineId.ToString());
                                p.CheckAdd("ID_PZ", taskItem.CheckGet("ID_PZ"));
                                p.CheckAdd("IS_COMPLETED", isCompleted.ToString());
                            }

                            var q = new LPackClientQuery();
                            q.Request.SetParam("Module", "Production");
                            q.Request.SetParam("Object", "TaskQueue");
                            q.Request.SetParam("Action", "DeleteTaskAction");
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
                                    if (ds.Items.Count > 0)
                                    {
                                        string strResume = ds.Items[0].CheckGet("RESUME");
                                        if (strResume.ToBool())
                                        {
                                            successCount++;
                                        }
                                        else
                                        {
                                            errorCount++;
                                            errorMessages.Add($"Задание {taskItem.CheckGet("NUM")}: ошибка удаления");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                errorCount++;
                                errorMessages.Add($"Задание {taskItem.CheckGet("NUM")}: ошибка запроса");
                            }
                        }
                        catch (Exception ex)
                        {
                            errorCount++;
                            errorMessages.Add($"Задание {taskItem.CheckGet("NUM")}: {ex.Message}");
                            CorrugatorErrors.LogError(ex);
                        }
                    }

                    // Показываем результат операции
                    string resultMessage = $"Обработано заданий: {selectedTasks.Count}\n";
                    resultMessage += $"Успешно удалено: {successCount}\n";
                    if (errorCount > 0)
                    {
                        resultMessage += $"Ошибок: {errorCount}\n\n";
                        resultMessage += string.Join("\n", errorMessages);
                    }

                    var resultDialog = new DialogWindow(resultMessage, "Результат удаления заданий", "", DialogWindowButtons.OK);
                    resultDialog.ShowDialog();

                    // Очищаем выбранные задания и обновляем грид
                    SelectedTaskIndexes.Clear();
                    NotifySelectedTasksChanged();
                    TaskGrid.LoadItems();
                }
            }
        }
        /// <summary>
        /// Удаление задания из очереди станка
        /// </summary>
        public async void DeleteTask(Dictionary<string, string> taskItem, int isCompleted)
        {
            if (CorrugatorMachineOperator.IsCurrentMachineSelected)
            {
                if (taskItem != null)
                {
                    //if (taskItem.CheckGet("_ROWNUMBER").ToInt() != 1
                    //    && taskItem.CheckGet("_ROWNUMBER").ToInt() != 2)
                    {
                        bool resume = false;
                        //TaskGrid.Items.Remove(taskItem);
                        //TaskGrid.UpdateItems();

                        var d = new DialogWindow($"Вы дейтвительно хотите {(isCompleted == 1 ? "закрыть":"удалить")} задание \"{taskItem.CheckGet("NUM")}\"?", isCompleted==1 ? "Закрытие задания!" : "Удаление задания", "", DialogWindowButtons.YesNo);
                        d.ShowDialog();
                        if (d.DialogResult == true)
                        {
                            var p = new Dictionary<string, string>();
                            {
                                p.CheckAdd("ID_ST", CorrugatorMachineOperator.SelectedMachineId.ToString());
                                p.CheckAdd("ID_PZ", taskItem.CheckGet("ID_PZ"));
                                p.CheckAdd("IS_COMPLETED", isCompleted.ToString());
                            }
                            var q = new LPackClientQuery();
                            q.Request.SetParam("Module", "Production");
                            q.Request.SetParam("Object", "TaskQueue");
                            q.Request.SetParam("Action", "DeleteTaskAction");
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
                                    if (ds.Items.Count > 0)
                                    {
                                        string strResume = ds.Items[0].CheckGet("RESUME");
                                        resume = strResume.ToBool();
                                    }
                                }
                            }

                            if (!resume)
                            {
                                var dresult = new DialogWindow($"При попытке удалить задание произошла ошибка", "Удаление задания", "Повторите попытку", DialogWindowButtons.OK);
                                dresult.ShowDialog();
                            }


                            if(resume)
                            {
                                SelectedTaskIndexes.Clear();
                                NotifySelectedTasksChanged();
                            }

                            TaskGrid.LoadItems();
                        }
                    }

                }
            }
        }

        /// <summary>
        /// Перемещение выделенных заданий по очереди станка
        /// </summary>
        /// <param name="direction"> Направление сдвига заданий (UP, up, down) </param>
        public void MoveTask(string direction)
        {
            if (CorrugatorMachineOperator.IsCurrentMachineSelected)
            {
                // Сохраняем выбранные задания
                SaveSelectedTasks();

                if (TaskGrid.Items != null && TaskGrid.Items.Count > 0)
                {
                    // в самый верх, за исключением первых заданий, которые трогать нельзя
                    if (direction == "UP")
                    { 
                        //первые n заданий трогать нельзя, начинаем с n
                        int iNew = CorrugatorMachineOperator.NumberOfUntouchableTasks;
                        // сначала по порядку нумеруем выделенные задания
                        for (int i = CorrugatorMachineOperator.NumberOfUntouchableTasks; i < TaskGrid.Items.Count; i++)
                        {
                            if (TaskGrid?.Items[i]["_SELECTED"].ToInt() == 1)
                            {
                                TaskGrid.Items[i]["_ROWNUMBER"] = iNew.ToString();
                                iNew++;
                            }
                        }
                        // потом нумеруем все остальные задания в очереди
                        for (int i = CorrugatorMachineOperator.NumberOfUntouchableTasks; i < TaskGrid.Items.Count; i++)
                        {
                            if (TaskGrid?.Items[i]["_SELECTED"].ToInt() == 0)
                            {
                                TaskGrid.Items[i]["_ROWNUMBER"] = iNew.ToString();
                                iNew++;
                            }
                        }
                    }
                    // на 1 вверх
                    else if (direction == "up")
                    {
                        //первые n заданий трогать нельзя,
                        //начинаем с n+1, чтобы при перемещении наверх(-1) первые n заданий не изменились
                        for (int i = CorrugatorMachineOperator.NumberOfUntouchableTasks + 1; i < TaskGrid.Items.Count; i++)
                        {
                            if (TaskGrid?.Items[i]["_SELECTED"].ToInt() == 1
                                && i > CorrugatorMachineOperator.NumberOfUntouchableTasks
                                && (i - 1) > CorrugatorMachineOperator.NumberOfUntouchableTasks)
                            {
                                // меняем задания местами в гриде
                                (TaskGrid.Items[i], TaskGrid.Items[i - 1]) = (TaskGrid?.Items[i - 1], TaskGrid?.Items[i]);
                            }
                        }
                        
                        // Перенумеровываем все строки после перемещения, начиная с 0
                        for (int i = 0; i < TaskGrid.Items.Count; i++)
                        {
                            TaskGrid.Items[i]["_ROWNUMBER"] = i.ToString();
                        }
                    }
                    // на 1 вниз
                    else if (direction == "down")
                    {
                        //первые n заданий трогать нельзя,
                        //начинаем с N-1, чтобы при перемещении вниз задания не ушли за границу и идём наверх до n+1 включительно
                        for (int i = TaskGrid.Items.Count - 2; i >= CorrugatorMachineOperator.NumberOfUntouchableTasks; i--)
                        {
                            if (TaskGrid?.Items[i]["_SELECTED"].ToInt() == 1)
                            {
                                // меняем задания местами
                                (TaskGrid.Items[i], TaskGrid.Items[i + 1]) = (TaskGrid?.Items[i + 1], TaskGrid?.Items[i]);
                            }
                        }
                        
                        // Перенумеровываем все строки после перемещения, начиная с 0
                        for (int i = 0; i < TaskGrid.Items.Count; i++)
                        {
                            TaskGrid.Items[i]["_ROWNUMBER"] = i.ToString();
                        }
                    }
                    
                    // внутри UpdateItems задания отсортируются по _ROWNUMBER
                    TaskGrid.UpdateItems();
                }
            }
        }

        /// <summary>
        /// Сохранение очереди заданий на станке
        /// </summary>
        public async void SaveQueue()
        {
            try
            {
                var virtualList = new List<Dictionary<string, string>>();

                if (TaskGrid.Items != null)
                {
                    foreach (var taskItem in TaskGrid.Items)
                    {
                        var taskInfoForAdding = new Dictionary<string, string>();
                        taskInfoForAdding.CheckAdd("ID_PZ", taskItem.CheckGet("ID_PZ"));
                        taskInfoForAdding.CheckAdd("WEB_WIDTH_NOETIC", taskItem.CheckGet("WEB_WIDTH_NOETIC"));
                        taskInfoForAdding.CheckAdd("FOSBER_NUM", taskItem.CheckGet("FOSBER_NUM"));

                        virtualList.Add(taskInfoForAdding);
                    }
                }

                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID_ST", CorrugatorMachineOperator.SelectedMachineId.ToString());
                    p.CheckAdd("VIRTUAL", JsonConvert.SerializeObject(virtualList));
                }
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "TaskQueue");
                q.Request.SetParam("Action", "SaveQueue");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                _ = await Task.Run(() =>
                {
                    q.DoQuery();
                    return q;
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result?.Count != null)
                    {
                        TaskGridLoadItems();
                    }
                }
            }
            catch (Exception ex)
            {
                CorrugatorErrors.LogError(ex);
            }
        }

        public async void ChangeSelectedRolls()
        {
            var i = TaskGrid.Items.Where(task => task.CheckGet("_SELECTED").ToInt() == 1).ToList();
            
            if (i.Count == 0)
            {
                if (TaskGrid.SelectedItem.CheckGet("_ROWNUMBER").ToInt() <= 2)
                {
                    i.Add(TaskGrid.SelectedItem);
                }
            }

            ChangeMfRolls(i);
        }

        /// <summary>
        /// Смена валов гофрирования
        /// </summary>
        public async void ChangeMfRolls(List<Dictionary<string, string>> tasks)
        {
            try
            {
                var virtualList = tasks
                    .Where(task => (task.CheckGet("MF1_ROLL_FACT") == "" || task.CheckGet("MF2_ROLL_FACT") == ""))
                    .Select(task =>
                    new Dictionary<string, string>()
                    {
                    { "ID_PZ", task.CheckGet("ID_PZ") },
                    { "MF1_ROLL_FACT", task.CheckGet("MF1_ROLL_FACT") },
                    { "MF2_ROLL_FACT", task.CheckGet("MF2_ROLL_FACT") },
                    }
                    ).ToList();

                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("VIRTUAL", JsonConvert.SerializeObject(virtualList));
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "CorrugatorMachineOperator");
                q.Request.SetParam("Action", "ChangeMfRolls");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                _ = await Task.Run(() =>
                {
                    q.DoQuery();
                    return q;
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        TaskGrid.LoadItems();
                    }
                }
            }
            catch(Exception ee)
            {
                CorrugatorErrors.LogError(ee);
            }
        }

        /// <summary>
        /// показать карту пз
        /// (печатная форма для ГА)
        /// </summary>
        public async void ShowProductionTaskMap()
        {
            try
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("ID", SelectedTaskItem.CheckGet("ID_PZ"));
                    p.Add("TEMP_FILE", "1");
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "ProductionTask");
                q.Request.SetParam("Action", "TaskGetMap");

                q.Request.SetParams(p);

                q.Request.Timeout = 10000;
                q.Request.Attempts = 1;

                q.Request.Timeout = Central.Parameters.RequestTimeoutMin;

                _ = await Task.Run(() =>
                {
                    q.DoQuery();
                    return q;
                });

                if (q.Answer.Status == 0)
                {
                    Central.OpenFile(q.Answer.DownloadFilePath);
                }
                else
                {
                    q.ProcessError();
                }
            }
            catch (Exception ee)
            {
                CorrugatorErrors.LogError(ee);
            }
        }
        
        /// <summary>
        /// обновление методов работы с выбранной в гриде строкой
        /// </summary>
        public void UpdateTaskGridActions(Dictionary<string, string> selectedItem)
        {

            
            bool isEnabled = AccessMode== Role.AccessMode.FullAccess;

            if (!CorrugatorMachineOperator.IsCurrentMachineSelected)
            {
                isEnabled = false;
            }
            
            TaskGrid.Menu["Delete"].Enabled = isEnabled;
            TaskGrid.Menu["DeleteSelected"].Enabled = isEnabled;
            TaskGrid.Menu["UP"].Enabled = isEnabled;
            TaskGrid.Menu["up"].Enabled = isEnabled;
            TaskGrid.Menu["down"].Enabled = isEnabled;
            TaskGrid.Menu["ChangeMfRolls"].Enabled = isEnabled;
            TaskGrid.Menu["End"].Enabled = isEnabled;
        }

        public void LoadItems()
        {
            // TaskGrid.LoadItems();
            TaskGridLoadItems();
        }

        /// <summary>
        /// Кдаление завершенных заказов из датасета
        /// </summary>
        /// <returns></returns>
        private bool RemoveOldOrders()
        {
            bool resume = false;
            bool removeCurrent = false;
            int removeCount = 0;

            if (DataSet != null)
            {
                if (DataSet.Items.Count > 0)
                {
                    var list = new List<Dictionary<string, string>>();

                    foreach (var item in DataSet.Items)
                    {
                        if (!deletedTask.Contains(item.CheckGet("ID_PZ").ToInt()))
                        {
                            list.Add(item);
                        }
                        else
                        {
                            removeCount++;
                            resume = true;
                            if(SelectedTaskItem != null)
                            {
                                if(SelectedTaskItem.CheckGet("ID_PZ").ToInt()==item.CheckGet("ID_PZ").ToInt())
                                {
                                    removeCurrent = true;
                                }
                            }

                            Console.WriteLine("Удаление номера");
                        }
                    }

                    if (resume)
                    {
                        if(removeCurrent)
                        {
                            TaskGrid.SelectRowByKey(removeCount.ToString());
                            TaskGrid.SelectRowFirst();
                        }

                        DataSet.Items = list;
                    }
                }
            }

            return resume;
        }

        /// <summary>
        /// Ткстирование удаления первого задания
        /// </summary>
        public void TestRemoveFirstPosition()
        {
            if (TaskGrid.Items.Count > 0)
            {
                var taskId = TaskGrid.Items[0].CheckGet("ID_PZ").ToInt();
                ChangeTask(taskId);

                ClearDeletetTask();
            }
        }


        /// <summary>
        /// Очистка массива с удаленными заданиями
        /// </summary>
        public void ClearDeletetTask()
        {
            deletedTask.Clear();
        }

        /// <summary>
        /// задача сменилась, необходимо удалить данный таск из грида
        /// </summary>
        /// <param name="taskId"></param>
        /// <exception cref="NotImplementedException"></exception>
        internal void ChangeTask(int taskId)
        {
            // обновить грид, удалить taskId запись

            //TaskGridLoadItems();

            //LoadItems();

            if(!deletedTask.Contains(taskId))
            {
                deletedTask.Add(taskId);
            }
                

            if (!IsQueueLoading)
            {

                if (SelectedTaskIndexes != null)
                {
                    if (SelectedTaskIndexes.ContainsKey(taskId.ToString()))
                    {
                        SelectedTaskIndexes.Remove(taskId.ToString());
                    }
                }

                if(RemoveOldOrders())
                {
                    LoadDataSet(DataSet);
                }
            }

            // если мы не в процессе загрузки
            /* Ни один способ фильтрации в GrisBox3 у меня не сработал
             * if (!IsQueueLoading)
            {
                if (TaskGrid.Items.Count > 0)
                {
                    var list = new List<Dictionary<string, string>>();

                    foreach (var item in TaskGrid.Items)
                    {
                        if (item.CheckGet("ID_PZ").ToInt()!=taskId)
                        {
                            list.Add(item);
                        }
                        else
                        {
                            Console.WriteLine("Удаление номера");
                        }
                    }

                    TaskGrid.Items = list;
                    TaskGrid.SelectRowFirst();
                }
            }*/

        }
    }
}
