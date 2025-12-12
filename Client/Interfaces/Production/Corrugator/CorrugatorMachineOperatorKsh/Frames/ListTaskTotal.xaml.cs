using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.Corrugator.CorrugatorMachineOperatorKsh
{
    /// <summary>
    /// Список всех актуальных пз, которых еще нет на ГА
    /// </summary>
    /// <author>vlasov_ea</author>
    /// <refactor>volkov_as</refactor>
    public partial class ListTaskTotal : ControlBase
    {
        public ListTaskTotal()
        {
            Id = 0;
            FrameName = "PositionKsh";

            InitializeComponent();

            OnLoad = () =>
            {
                Init();
                TaskOnMachineGridInit();
            };

            OnUnload = () =>
            {
                TaskOnMachine.Destruct();
                TaskGrid.Destruct();
            };

            {
                Commander.SetCurrentGroup("main");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "update_task_list",
                        Enabled = true,
                        Title = "Обновить",
                        ButtonUse = true,
                        ButtonName = "UpdateTaskList",
                        MenuUse = true,
                        Action = TaskOnMachineLoadItems
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "select_js",
                        Enabled = true,
                        Title = "J.S",
                        ButtonUse = true,
                        ButtonName = "JS",
                        MenuUse = true,
                        Action = () => 
                        {
                            IdMachine = "23";
                            TaskOnMachineLoadItems();
                        }
                    });
                }
                Commander.Init(this);
            }
        }

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// идентификатор записи, с которой работает форма
        /// (primary key записи таблицы)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// ИД Машины (2, 21, 22)
        /// </summary>
        private string IdMachine { get; set; } = "23";

        /// <summary>
        /// Делегат после обновления грида очереди станка
        /// </summary>
        public Action OnAfterAddTaskIntoQueue;

        public Dictionary<string, string> SelectedTaskItem { get; set; }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void Init()
        {
            TaskGridInit();
            ListProfile();

            var AccessMode = Central.Navigator.GetRoleLevel(CorrugatorMachineOperator.Role);

            List<Button> buttons = UIUtil.GetVisualChilds<Button>(this.Content as DependencyObject);
            if (buttons != null && buttons.Count > 0)
            {
                foreach (var button in buttons)
                {
                    var buttonTagList = UIUtil.GetTagList(button);
                    var accessMode = Acl.FindTagAccessMode(buttonTagList);
                    if (accessMode > AccessMode)
                    {
                        button.IsEnabled = false;
                    }
                }
            }

        }

        /// <summary>
        /// инициализация грида
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
                        Header="#",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=27,
                        MaxWidth=50,
                        Width2 = 2,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Начать до",
                        Path="START_BEFORE",
                        ColumnType=ColumnTypeRef.String,
                        Width=110,
                        Width2 = 6,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskHelper.GetColor("START_BEFORE", row)
                            },
                        },
                    },
                   new DataGridHelperColumn
                    {
                        Header="ИД ПЗ",
                        Path="ID_PZ",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=100,
                        Width2 = 5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ПЗ",
                        Path="NUM",
                        ColumnType=ColumnTypeRef.String,
                        Width=110,
                        Width2 = 4,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskHelper.GetColor("NUM", row)
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Длина, м",
                        Path="LEN",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=100,
                        Width2 = 4,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Профиль",
                        Path="PROFIL_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width=50,
                        Width2 = 2,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskHelper.GetColor("PROFIL_NAME", row)
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вал 1",
                        Path="NAME_ROLL_1",
                        ColumnType=ColumnTypeRef.String,
                        Width=70,
                        Width2 = 2,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskHelper.GetColor("NAME_ROLL1", row)
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вал 2",
                        Path="NAME_ROLL_2",
                        ColumnType=ColumnTypeRef.String,
                        Width=70,
                        Width2 = 2,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskHelper.GetColor("NAME_ROLL2", row)
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Товар",
                        Path="TOVAR_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width=70,
                        Width2 = 15,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskHelper.GetColor("TOVAR_NAME", row)
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Формат",
                        Path="WEB_WIDTH",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=100,
                        Width2 = 3,
                        OnRender=(row,el)=>TaskHelper.GetIcons("WEB_WIDTH", row),
                    },
                    new DataGridHelperColumn
                    {
                        Header="Толщина, мм",
                        Path="TOTAL_THICKNESS",
                        ColumnType=ColumnTypeRef.Double,
                        Width2 = 4,
                        Format = "N3",
                        OnRender=(row,el)=>TaskHelper.GetIcons("", row),
                    },
                    new DataGridHelperColumn
                    {
                        Header="Станок",
                        Path="MACHINE",
                        ColumnType=ColumnTypeRef.String,
                        Width=150,
                        Width2 = 10,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskHelper.GetColor("SNAME", row)
                            },
                        },
                        OnRender=(row,el)=>TaskHelper.GetIcons("MACHINE", row),
                    },
                    new DataGridHelperColumn
                    {
                        Header="1",
                        Path="LAYER_1",
                        ColumnType=ColumnTypeRef.String,
                        Width=100,
                        Width2 = 4,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskHelper.GetColor("LAYER_1", row)
                            },
                        },
                        OnRender=(row,el)=>TaskHelper.GetIcons("LAYER_1", row),
                    },
                    new DataGridHelperColumn
                    {
                        Header="2",
                        Path="LAYER_2",
                        ColumnType=ColumnTypeRef.String,
                        Width=100,
                        Width2 = 4,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskHelper.GetColor("LAYER_2", row)
                            },
                        },
                        OnRender=(row,el)=>TaskHelper.GetIcons("LAYER_2", row),
                    },
                    new DataGridHelperColumn
                    {
                        Header="3",
                        Path="LAYER_3",
                        ColumnType=ColumnTypeRef.String,
                        Width=100,
                        Width2 = 4,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskHelper.GetColor("LAYER_3", row)
                            },
                        },
                        OnRender=(row,el)=>TaskHelper.GetIcons("LAYER_3", row),
                    },
                    new DataGridHelperColumn
                    {
                        Header="4",
                        Path="LAYER_4",
                        ColumnType=ColumnTypeRef.String,
                        Width=100,
                        Width2 = 4,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskHelper.GetColor("LAYER_4", row)
                            },
                        },
                        OnRender=(row,el)=>TaskHelper.GetIcons("LAYER_4", row),
                    },
                    new DataGridHelperColumn
                    {
                        Header="5",
                        Path="LAYER_5",
                        ColumnType=ColumnTypeRef.String,
                        Width=100,
                        Width2 = 4,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskHelper.GetColor("LAYER_5", row)
                            },
                        },
                        OnRender=(row,el)=>TaskHelper.GetIcons("LAYER_5", row),
                    },
                    new DataGridHelperColumn
                    {
                        Header="Скорость, м/мин",
                        Path="SPEED",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=80,
                        Width2 = 3,
                        OnRender=(row,el)=>TaskHelper.GetIcons("", row),
                    },
                    new DataGridHelperColumn
                    {
                        Header="Обрезь, мм",
                        Path="OBREZ",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=70,
                        Width2 = 4,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskHelper.GetColor("OBREZ", row)
                            },
                        },
                        OnRender=(row,el)=>TaskHelper.GetIcons("OBREZ", row),
                    },
                    new DataGridHelperColumn
                    {
                        Header="Композиция",
                        Path="QID",
                        ColumnType=ColumnTypeRef.String,
                        Width=100,
                        Width2 = 6,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => TaskHelper.GetColor("CHECK_QID", row)
                            },
                        },
                        OnRender=(row,el)=>TaskHelper.GetIcons("QID", row),
                    },
                    new DataGridHelperColumn
                    {
                        Header="Смещение клише",
                        Path="FANFOLD_PRINT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=100,
                        Width2 = 2,
                        OnRender=(row,el)=>TaskHelper.GetIcons("", row),
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер клише",
                        Path="NKLISHE",
                        ColumnType=ColumnTypeRef.String,
                        Width=100,
                        Width2 = 2,
                        OnRender=(row,el)=>TaskHelper.GetIcons("", row),
                    },
                    new DataGridHelperColumn
                    {
                        Header="Цвет",
                        Path="COLOR",
                        ColumnType=ColumnTypeRef.String,
                        Width=100,
                        Width2 = 2,
                        OnRender=(row,el)=>TaskHelper.GetIcons("", row),
                    },
                    new DataGridHelperColumn
                    {
                        Header="Стопа на фенфолд",
                        Path="KOL_PAK",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=70,
                        Width2 = 2,
                        OnRender=(row,el)=>TaskHelper.GetIcons("", row),
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество, шт",
                        Path="QTY",
                        ColumnType=ColumnTypeRef.String,
                        Width=100,
                        Width2 = 4,
                        OnRender=(row,el)=>TaskHelper.GetIcons("", row),
                    },
                };
                TaskGrid.SetColumns(columns);

                TaskGrid.PrimaryKey = "_ROWNUMBER";
                TaskGrid.SearchText = SearchText;
                TaskGrid.OnDblClick = (selectedItem) => AddTaskToGA(CorrugatorMachineOperator.CurrentMachineId);
                TaskGrid.UseRowHeader = false;

                TaskGrid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    { "AddToGA1", new DataGridContextMenuItem(){
                        Header="На J.S",
                        Action=()=>
                        {
                            AddTaskToGA(23);
                        }
                    }}
                };
                TaskGrid.SetColumns(columns);

                TaskGrid.PrimaryKey = "_ROWNUMBER";

                //при выборе строки в гриде, обновляются актуальные действия для записи
                TaskGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem.Count > 0)
                    {
                        SelectedTaskItem = selectedItem;
                    }
                };

                //данные грида
                TaskGrid.OnLoadItems = TaskGridLoadItems;
                TaskGrid.OnFilterItems = TaskGridFilter;


                TaskGrid.Init();
                TaskGrid.LoadItems();
                //фокус ввода           
                TaskGrid.Focus();
            }
        }

        private void TaskOnMachineGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "#",
                    Path = "_ROWNUMBER",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 2,
                },
                new DataGridHelperColumn
                {
                    Header = "Начать до",
                    Path = "START_BEFORE",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 8,
                },
                new DataGridHelperColumn
                {
                    Header = "ИД ПЗ",
                    Path = "ID_PZ",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                },
                new DataGridHelperColumn
                {
                    Header = "ПЗ",
                    Path = "NUM",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 9,
                },
                new DataGridHelperColumn
                {
                    Header = "Длина, м",
                    Path = "LEN",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 6,
                },
                new DataGridHelperColumn
                {
                    Header = "Профиль",
                    Path = "PROFIL_NAME",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 2,
                },
                new DataGridHelperColumn
                {
                    Header = "Вал 1",
                    Path = "NAME_ROLL_1",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 2,
                },
                new DataGridHelperColumn
                {
                    Header = "Вал 2",
                    Path = "NAME_ROLL_2",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 2,
                },
                new DataGridHelperColumn
                {
                    Header = "Формат",
                    Path = "WEB_WIDTH",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 6,
                },
                new DataGridHelperColumn
                {
                    Header = "Толщина, мм",
                    Path = "TOTAL_THICKNESS",
                    ColumnType = ColumnTypeRef.Double,
                    Width2 = 5,
                    Format = "N3",
                },
                new DataGridHelperColumn
                {
                    Header = "Станок",
                    Path = "MACHINE",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 18,
                },
                new DataGridHelperColumn
                {
                    Header = "1",
                    Path = "LAYER_1",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 9,
                },
                new DataGridHelperColumn
                {
                    Header = "2",
                    Path = "LAYER_2",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 9,
                },
                new DataGridHelperColumn
                {
                    Header = "3",
                    Path = "LAYER_3",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 9,
                },
                new DataGridHelperColumn
                {
                    Header = "4",
                    Path = "LAYER_4",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 9,
                },
                new DataGridHelperColumn
                {
                    Header = "5",
                    Path = "LAYER_5",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 9,
                },
                new DataGridHelperColumn
                {
                    Header = "Скорость, м/мин",
                    Path = "SPEED",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 3,
                },
                new DataGridHelperColumn
                {
                    Header = "Обрезь, мм",
                    Path = "OBREZ",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 3,
                },
                new DataGridHelperColumn
                {
                    Header = "Композиция",
                    Path = "QID",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 9,
                },
                new DataGridHelperColumn
                {
                    Header = "Смещение клише",
                    Path = "FANFOLD_PRINT",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 2,
                },
                new DataGridHelperColumn
                {
                    Header = "Номер клише",
                    Path = "NKLISHE",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 2,
                },
                new DataGridHelperColumn
                {
                    Header = "Цвет",
                    Path = "COLOR",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 2,
                },
                new DataGridHelperColumn
                {
                    Header = "Стопа на фенфолд",
                    Path = "KOL_PAK",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 2,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество, шт",
                    Path = "QTY",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 4,
                },
                new DataGridHelperColumn
                {
                    Header = "Лаборатория",
                    Path = "TESTING_FLAG",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 4,
                    Visible = false,
                },
                new DataGridHelperColumn
                {
                    Header = "NOTE",
                    Path = "NOTE",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 4,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "TWICE_WHITE_RAW",
                    Path = "TWICE_WHITE_RAW",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 4,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "FILL_PRINTING_FLAG",
                    Path = "FILL_PRINTING_FLAG",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 4,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "RAWS_TO_START_SPLICER",
                    Path = "RAWS_TO_START_SPLICER",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 4,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "RILEVKI",
                    Path = "RILEVKI",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 4,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "STYPE",
                    Path = "STYPE",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 4,
                    Visible = false
                },
            };
            TaskOnMachine.SetColumns(columns);
            TaskOnMachine.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            TaskOnMachine.AutoUpdateInterval = 0;
            TaskOnMachine.SetPrimaryKey("_ROWNUMBER");
            TaskOnMachine.EnableSortingGrid = false;
            TaskOnMachine.SearchText = SearchTask;
            TaskOnMachine.OnLoadItems = TaskOnMachineLoadItems;
            TaskOnMachine.Commands = Commander;
            TaskOnMachine.Init();
        }

        /// <summary>
        /// Фильтрация
        /// </summary>
        public void TaskGridFilter()
        {
            if (TaskGrid.GridItems != null)
            {
                foreach (var item in TaskGrid.GridItems)
                {
                    // Объединяем примечания в одно поле NOTE
                    if (!item.CheckGet("PRIM").IsNullOrEmpty())
                    {
                        item["NOTE"] += $" {item.CheckGet("PRIM")}";
                    }
                    item["NOTE"] = item.CheckGet("NOTE").Trim();
                }
            }
        }

        /// <summary>
        /// Запрос для получения очереди на станке
        /// </summary>
        private async void TaskOnMachineLoadItems()
        {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "TaskQueue");
                q.Request.SetParam("Action", "List");
                q.Request.SetParam("ID_ST", IdMachine);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var list = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                    if (list != null)
                    {
                        TaskOnMachine.UpdateItems(ListDataSet.Create(list, "TASKS"));

                        if (IdMachine == "23")
                        {
                            Bhs1.Style = (Style)Bhs1.TryFindResource("FButtonPrimary");
                        } 
                    }
                }
        }

        /// <summary>
        /// получение записей
        /// </summary>
        public async void TaskGridLoadItems()
        {
            DisableControls();

            string profilName = Profil.SelectedItem?.ToString() ?? "";
            if(profilName == "Все")
            {
                profilName = "";
            }

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_ST", CorrugatorMachineOperator.SelectedMachineId.ToString());
                p.CheckAdd("PROFIL_NAME", profilName);
                // Текст (номер) для поиска среди выполненных заданий, если пустой - значит ищем среди НЕвыполненных заданий
                p.CheckAdd("SEARCH_TEXT_FOR_COMPLETED_TASKS", SearchTextForCompletedTasks.Text);
                p.CheckAdd("FORMAT_WIDTH", FormatFilter.SelectedItem.Key == "0" ? "" : FormatFilter.SelectedItem.Value);
            }
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "CorrugatorMachineOperatorKsh");
            q.Request.SetParam("Action", "ListTaskTotal");
            q.Request.SetParams(p);

            q.Request.Timeout = 30000;
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
                    var ds = ListDataSet.Create(result, "TASKS");
                    TaskGrid.UpdateItems(ds);
                    TaskGrid.SelectedItem = null;
                    
                    if (ds.Items.Count > 0)
                    {
                        TaskGrid.SelectRowFirst();
                    }
                }
            }
            EnableControls();
        }

        /// <summary>
        /// Добавление пз в очередь на станок
        /// </summary>
        public async void AddTaskToGA(int machineId)
        {
            if (CorrugatorMachineOperator.IsCurrentMachineSelected)
            {
                DisableControls();

                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID_ST", machineId.ToString());
                    p.CheckAdd("ID_PZ", SelectedTaskItem.CheckGet("ID_PZ").ToString());
                    p.CheckAdd("WEB_WIDTH_NOETIC", SelectedTaskItem.CheckGet("WEB_WIDTH").ToString());
                }
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "TaskQueue");
                q.Request.SetParam("Action", "AddTaskToMachineAction");
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
                        if (result.ContainsKey("ITEMS"))
                        {
                            TaskGrid.LoadItems();
                            TaskOnMachine.LoadItems();
                            OnAfterAddTaskIntoQueue?.Invoke();
                        }
                    }
                }
                EnableControls();
            }
            else
            {
                var dlg = new DialogWindow("Вы не можете добавлять заказы на данные гофроагрегаты");
                dlg.ShowDialog();
            }
        }

        public async void ListProfile()
        {
            DisableControls();

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("TYPE", "1");
            }
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "TechnologicalMapForSite");
            q.Request.SetParam("Action", "ListProfile");
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
                        var profilNames = new List<string>();
                        profilNames.Add("Все");
                        foreach (var item in ds.Items)
                        {
                            profilNames.Add(item.CheckGet("NAME"));
                        }
                        Profil.ItemsSource = profilNames;
                        Profil.SelectedIndex = 0;

                        ListPaperWidth();
                    }
                }
            }
            EnableControls();
        }

        private async void ListPaperWidth()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "CorrugatorMachineOperator");
            q.Request.SetParam("Action", "ListPaperRaw");

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
                    var list = new Dictionary<string, string>
                    {
                        { "0", "Все" },
                    };

                    foreach (var width in ds.Items)
                    {
                        list.Add(width.CheckGet("PAWI_ID"), width.CheckGet("WIDTH"));
                    }
                    
                    if (list.Count > 0)
                    {
                        FormatFilter.Items = list;
                        FormatFilter.SetSelectedItemByKey("0");
                    }
                }
            }
        }


        public void Destroy()
        {
            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            OnAfterAddTaskIntoQueue = null;

            TaskGrid.Destruct();
        }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;
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
            var frameName = GetFrameName();
            Central.WM.Show(frameName, "ПЗ", true, "add", this);
        }

        public void Close()
        {
            var frameName = GetFrameName();
            Central.WM.Close(frameName);
            Destroy();
        }

        /// <summary>
        /// формирует уникальный идентификатор фрейма
        /// </summary>
        /// <returns></returns>
        public string GetFrameName()
        {
            string result = "";
            result = $"{FrameName}_{Id}";
            return result;
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
            TaskGrid.ShowSplash();
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
            TaskGrid.HideSplash();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AddGA1Button_Click(object sender, RoutedEventArgs e)
        {
            AddTaskToGA(23);
        }
        
        private void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            TaskGrid.LoadItems();
            ShowButton.Style = (Style)ShowButton.TryFindResource("Button");
        }
        private void Profil_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string profil = ((sender as ComboBox).SelectedItem as string);
            TaskGrid.LoadItems();
        }
        private void TextChanged(object sender, RoutedEventArgs e)
        {
            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
        }

        private void FormatFilter_OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TaskGrid.LoadItems();
        }
    }
}
