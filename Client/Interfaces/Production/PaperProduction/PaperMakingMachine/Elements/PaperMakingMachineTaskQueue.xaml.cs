using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Production.Corrugator;
using DevExpress.DXBinding.Native;
using DevExpress.Xpf.Grid;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Office.Interop.Excel;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using Org.BouncyCastle.Asn1.Utilities;
using Org.BouncyCastle.Bcpg.Sig;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using System.Windows.Media;
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Список заданий на конкретном станке БДМ
    /// </summary>
    /// <author>greshnyh_ni</author>   
    public partial class PaperMakingMachineTaskQueue : ControlBase
    {

        public delegate void ItemChange(Dictionary<string, string> item);
        public event ItemChange OnItemChange;

        /// <summary>
        /// Очередь в процессе загрузки
        /// </summary>
        public bool IsQueueLoading { get; set; }

        /// <summary>
        /// Ставить курсор на первую запись
        /// </summary>
        public bool FlagFirst { get; set; }

        private ListDataSet DataSet { get; set; }

        public Dictionary<string, string> SelectedTaskItem { get; set; }

        /// <summary>
        /// Событие на обновление грида очереди станка
        /// </summary>
        public event System.Action OnAfterLoadQueue;

        private ListDataSet TaskGridDataSet { get; set; }
        public int MachineId { get; internal set; }

        /// <summary>
        /// Таймер задержки установки курсора на первую запись
        /// </summary>
        public Timeout FirstTimer { get; set; }

        /// <summary>
        /// Элементы грида
        /// </summary>
        //public List<Dictionary<string, string>> Items
        //{
        //    get => TaskGrid.GetItems();
        //}



        public PaperMakingMachineTaskQueue()
        {
            InitializeComponent();

            //конструктор, будет вызван, когда объект создается
            //здесь создаются все внутренние структуры
            //впервые этот коллбэк будет вызван, когда данный таб станет активным
            //впервые (до этих пор, никакая работа внутри не происходит, что экономит ресурсы)
            OnLoad = () =>
            {
                SetDefaults();
                Init();
                TaskGridInit();

                //регистрация обработчика сообщений
                Messenger.Default.Register<ItemMessage>(this, ProcessMessage);
            };


            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                Messenger.Default.Unregister<ItemMessage>(this);
                TaskGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
               TaskGrid.ItemsAutoUpdate = true;
               TaskGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                TaskGrid.ItemsAutoUpdate = false;
            };
            Commander.Init(this);
        }

        /// <summary>
        /// Обработка общих сообщений
        /// </summary>
        /// <param name="action"></param>
        /// <param name="obj"></param>
        //public void ProcessMessage(string action, ItemMessage obj = null)
        public void ProcessMessage(ItemMessage message)
        {

            if (message != null)
            {
                switch (message.Action)
                {
                    case "UP":
                        UpMoveTask();
                        break;

                    case "DOWN":
                        DownMoveTask();
                        break;

                    case "CLOSE_TASK":
                        TaskClose();
                        break;

                    case "GRID_UP":
                        FlagFirst = true;
                        break;

                    case "GRID_DOWN":
                        FlagFirst = false;
                        break;

                    case "EXCEL":

                        ExportToExcel();
                        break;

                    case "CLOSED":
                        OnUnload();
                        break;
                }
            }
        }

        /// <summary>
        /// настройки по умолчанию 
        /// </summary>
        public void SetDefaults()
        {
            TaskGridDataSet = new ListDataSet();
        }

        /// <summary>
        /// Инициализация грида
        /// </summary>
        public void Init()
        {
            IsQueueLoading = false;
            FlagFirst = true;

            double nScale = 2.0;
            TaskGrid.LayoutTransform = new ScaleTransform(nScale, nScale);
/*
            FirstTimer = new Timeout(
              5,
              () =>
              {
                  FlagFirst = true;
                  FirstTimer.Finish();
              },
              true,
              false
            );
            FirstTimer.Finish();
*/
        }


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
                        Header="№",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 1,
                        Visible = false,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "№ задания",
                        Path = "NUM",
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 6,

                    },
                    new DataGridHelperColumn
                    {
                        Header="Марка",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 6,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => GetColor("NAME", row)
                            },
                        },
                        /*
                        Labels=new List<DataGridHelperColumnLabel>()
                        {
                            new DataGridHelperColumnLabel()
                            {
                                Construct=()=>
                                {
                                    var block=DataGridHelperColumnLabel.MakeElement("!","#FFFFC182","#ff000000", 16, 16);
                                    return block;
                                },
                                Update = (Dictionary<string, string> row) =>
                                {
                                    var result = DependencyProperty.UnsetValue;
                                    {
                                        result=Visibility.Hidden;
                                        if (!row.CheckGet("NOTE").IsNullOrEmpty())
                                        {
                                            result=Visibility.Visible;
                                        }
                                    }
                                    return result;
                                },
                            },
                        },
                        */
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вес",
                        Path="RO",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 4,

                    },
                    new DataGridHelperColumn
                    {
                        Header="Диаметр",
                        Path="DIAMETER",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 8,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => GetColor("DIAMETER", row)
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Формат 1",
                        Path="B1",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 8,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => GetColor("B1", row)
                            },
                        },

                    },
                    new DataGridHelperColumn
                    {
                        Header = "Формат 2",
                        Path = "B2",
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 8,
                        Visible = MachineId.ToInt() != 716,
                        Exportable =  MachineId.ToInt() != 716,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => GetColor("B2", row)
                            },
                        },

                    },
                    new DataGridHelperColumn
                    {
                        Header = "Формат 3",
                        Path = "B3",
                        //ColumnType = ColumnTypeRef.Integer,
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 8,
                        Visible = MachineId.ToInt() != 716,
                        Exportable =  MachineId.ToInt() != 716,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => GetColor("B3", row)
                            },
                        },

                    },
                    new DataGridHelperColumn
                    {
                        Header = "Задание 1/Выполнено 1/Осталось 1",
                        Path = "BALANCE1",
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 10,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => GetColor("BALANCE1", row)
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Задание 2/Выполнено 2/Осталось 2",
                        Path = "BALANCE2",
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 10,
                        Visible = MachineId.ToInt() != 716,
                        Exportable =  MachineId.ToInt() != 716,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => GetColor("BALANCE2", row)
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Задание 3/Выполнено 3/Осталось 3",
                        Path = "BALANCE3",
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 10,
                        Visible = MachineId.ToInt() != 716,
                        Exportable =  MachineId.ToInt() != 716,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => GetColor("BALANCE3", row)
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Задание (т.)/Выполнено (т.)/Осталось (т.)",
                        Path = "BALANCE_QTY_1",
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 26,
                        Visible = MachineId.ToInt() == 716,
                        Exportable =  MachineId.ToInt() == 716,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Сумма",
                        Path = "SUM_KOL",
                        ColumnType = ColumnTypeRef.Double,
                        Width2 = 6,
                    },

                    new DataGridHelperColumn
                    {
                        Header = "Скорость",
                        Path = "BDM_SPEED",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2 = 6,
                    },

                    new DataGridHelperColumn
                    {
                        Header = "Закончить",
                        Path = "CDTTM",
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 8,

                    },
                    new DataGridHelperColumn
                    {
                        Header = "Примечание",
                        Path = "NOTE",
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 18,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="ID_PZ",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 7,
                        Visible = false,
                        Exportable= false,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "ORDERBY",
                        Path = "ORDERBY",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2 = 4,
                        Visible = false,
                        Exportable= false,
                    },
                    /// не видимые поля
                    new DataGridHelperColumn
                    {
                        Header = "DTBEGIN",
                        Path = "DTBEGIN",
                        ColumnType = ColumnTypeRef.DateTime,
                        Width2 = 12,
                        Visible = false,
                        Exportable= false,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "DATA",
                        Path = "DATA",
                        ColumnType = ColumnTypeRef.DateTime,
                        Width2 = 12,
                        Visible = false,
                        Exportable= false,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "SUB_B",
                        Path = "SUB_B",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2 = 6,
                        Visible = false,
                        Exportable= false,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "GLUED_FLAG",
                        Path = "GLUED_FLAG",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2 = 2,
                        Visible = false,
                        Exportable= false,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "B_ALL",
                        Path = "B_ALL",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2 = 4,
                        Visible = false,
                        Exportable= false,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "ZAD1",
                        Path = "ZAD1",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2 = 4,
                        Visible = false,
                        Exportable= false,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "ZAD2",
                        Path = "ZAD2",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2 = 4,
                        Visible = false,
                        Exportable= false,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "ZAD3",
                        Path = "ZAD3",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2 = 4,
                        Visible = false,
                        Exportable= false,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "PRIH1",
                        Path = "PRIH1",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2 = 4,
                        Visible = false,
                        Exportable= false,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "PRIH2",
                        Path = "PRIH2",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2 = 4,
                        Visible = false,
                        Exportable= false,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "PRIH3",
                        Path = "PRIH3",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2 = 4,
                        Visible = false,
                        Exportable= false,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "SALE_FLAG1",
                        Path = "SALE_FLAG1",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2 = 4,
                        Visible = false,
                        Exportable= false,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "SALE_FLAG2",
                        Path = "SALE_FLAG2",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2 = 4,
                        Visible = false,
                        Exportable= false,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "SALE_FLAG3",
                        Path = "SALE_FLAG3",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2 = 4,
                        Visible = false,
                        Exportable= false,
                    },
                };
  
                TaskGrid.SetColumns(columns);
                TaskGrid.SetPrimaryKey("ID_PZ");
                //  TaskGrid.SetPrimaryKey("_ROWNUMBER");

                TaskGrid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
                TaskGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                TaskGrid.AutoUpdateInterval = 0;

                // Раскраска строк
                TaskGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                };

                //при выборе строки в гриде, обновляются актуальные действия для записи
                TaskGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem.Count > 0)
                    {
                        SelectedTaskItem = selectedItem;
                        UpdateTaskGridActions(selectedItem);

                        OnItemChange?.Invoke(SelectedTaskItem);
                    }
                };

                //данные грида
                TaskGrid.OnLoadItems = TaskGridLoadItems;
                TaskGrid.OnFilterItems = TaskGridFilterItems;

                TaskGrid.Commands = Commander;

                TaskGrid.Init();

            }
        }

        public void TaskGridFilterItems()
        {
            if (TaskGrid != null && TaskGrid.SelectedItem != null && TaskGrid.SelectedItem.Count > 0)
            {
                TaskGrid.Commands.Message = new ItemMessage() { Action = "refresh", Message = $"{TaskGrid.SelectedItem.CheckGet("ID_PZ")}" };
            }
        }

        /// <summary>
        /// Загрузка очереди заданий выбранного станка 
        /// </summary>
        public async void TaskGridLoadItems()
        {
            //проверка, если уже идёт загрузка очереди
            if (!IsQueueLoading)
            {
                IsQueueLoading = true;
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID_ST", MachineId.ToString());
                }

                try
                {
                    var q = await LPackClientQuery.DoQueryAsync("ProductionPm", "Monitoring", "TaskMachineList", "ITEMS", p);

                    if (q.Answer.Status == 0)
                    {
                        if (q.Answer.QueryResult != null)
                        {
                            DataSet = q.Answer.QueryResult;

                            {
                                LoadDataSet(DataSet);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {

                }

                IsQueueLoading = false;

            }
        }

        /// <summary>
        /// Загрузка датасета в грид 
        /// </summary>
        /// <param name="dataSet"></param>
        private void LoadDataSet(ListDataSet dataSet)
        {
            TaskGrid.UpdateItems(DataSet);

            if (FlagFirst)
                TaskGrid.SelectRowFirst();

            OnAfterLoadQueue?.Invoke();
          //  TaskGridApplyStyles();
        }

        /// <summary>
        /// Фильтрация
        /// </summary>
        public void TaskGridFilter()
        {
            /*
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

                        // Проставляем сохранённые чекбоксы
                        int idTask = item.CheckGet("ID_PZ").ToInt();
                        if (SelectedTaskIndexes.CheckGet(idTask.ToString()).ToBool())
                        {
                            item["_SELECTED"] = "1";
                        }
                        else
                        {
                            item["_SELECTED"] = "0";
                        }
                    }


                }
    */
        }

        /// <summary>
        /// Применение своих стилей к загруженному гриду с очередью заданий на га
        /// </summary>
        public void TaskGridApplyStyles()
        {
            //foreach (var textBlock in FormHelper.FindLogicalChildren<TextBlock>(TaskGrid))
            //{
            //    textBlock.FontWeight = FontWeights.SemiBold;
            //}
        }

        public void LoadItems()
        {
            TaskGrid.LoadItems();
        }

        /// <summary>
        /// Возвращает цвет для ячейки списка заданий
        /// </summary>
        public static object GetColor(string fieldName, Dictionary<string, string> row)
        {
            var result = DependencyProperty.UnsetValue;
            var color = "";

            if (fieldName == "NAME")
            {
                if (row.CheckGet("GLUED_FLAG").ToInt() == 1)
                {
                    //   R  G   B
                    // 221-215-172   Оливковый
                    color = $"#ddd7ac";
                }
                else if (row.CheckGet("NAME").IndexOf('0') == 1) // Б0 или К0
                {
                    //   R  G   B
                    // 127-255-127   Зеленый
                    color = $"#7fff7f";
                }
            }
            else if ((fieldName == "BALANCE1") || (fieldName == "B1"))
            {
                if (row.CheckGet("SALE_FLAG1").ToInt() == 1)
                {
                    //   R  G   B
                    // 172-237-247   морская волна
                    color = $"#acedf7";
                }
            }
            else if ((fieldName == "BALANCE2") || (fieldName == "B2"))
            {
                if (row.CheckGet("SALE_FLAG2").ToInt() == 1)
                {
                    //   R  G   B
                    // 172-237-247   морская волна
                    color = $"#acedf7";
                }
            }
            else if ((fieldName == "BALANCE3") || (fieldName == "B3"))
            {
                if (row.CheckGet("SALE_FLAG3").ToInt() == 1)
                {
                    //   R  G   B
                    // 172-237-247   морская волна
                    color = $"#acedf7";
                }
            }
            else if ((fieldName == "DIAMETER"))
            {
                if ((row.CheckGet("SALE_FLAG1").ToInt() == 1) && (row.CheckGet("DIAMETER").ToInt() == 1260))
                {
                    //   R  G   B
                    // 172-237-247   красный
                    color = $"#df2050";
                }
            }

            if (!color.IsNullOrEmpty())
            {
                result = color.ToBrush();
            }

            return result;
        }


        /// <summary>
        /// обновление методов работы с выбранной в гриде строкой
        /// </summary>
        public void UpdateTaskGridActions(Dictionary<string, string> selectedItem)
        {
        }

        /// <summary>
        /// закрываем ПЗ
        /// </summary>
        private void TaskClose()
        {
            var num = SelectedTaskItem.CheckGet("NUM").ToString();

            var dw = new DialogWindow($"Вы действительно хотите закрыть производственное задание №{num}?.", "Работа с ПЗ", "Закрыть?", DialogWindowButtons.NoYes);
            if (dw.ShowDialog() == true)
            {
                CloseTask();
            }
        }

        /// <summary>
        /// двигаем выбранное задание вверх
        /// </summary>
        private void UpMoveTask()
        {
            var cnt = SelectedTaskItem.CheckGet("_ROWNUMBER").ToInt();
            if (cnt == 1)
            {
                var dw = new DialogWindow($"Это самое верхнее задание.", "Работа с ПЗ", "", DialogWindowButtons.OK);
                dw.ShowDialog();
                return;
            }

            // запоминаем старые данные
            var numOld = SelectedTaskItem.CheckGet("NUM").ToString();
            var idPzOld = SelectedTaskItem.CheckGet("ID_PZ").ToInt().ToString();
            var dtOld = SelectedTaskItem.CheckGet("DATA_STR").ToDateTime();
            var orderByOld = SelectedTaskItem.CheckGet("ORDERBY").ToInt().ToString();

            // новые данные
            var numNew = "";
            var idPzNew = "";
            var dtNew = DateTime.Now;
            var orderByNew = "";

            // идем сверху вниз от первой записи
            foreach (var item in TaskGrid.Items)
            {
                // запомним промежуточные данные
                var numNew1 = item.CheckGet("NUM").ToString();
                var idPzNew1 = item.CheckGet("ID_PZ").ToInt().ToString();
                var dtNew1 = item.CheckGet("DATA_STR").ToDateTime();
                var orderByNew1 = item.CheckGet("ORDERBY").ToInt().ToString();

                if (numNew1 == numOld)    // дошли до текущей выбранной строки
                    break;
                else
                {
                    // запомним предыдущие данные
                    numNew = numNew1;
                    idPzNew = idPzNew1;
                    dtNew = dtNew1;
                    orderByNew = orderByNew1;
                }
            }

            // перемещаем
            var p1 = new Dictionary<string, string>();
            {
                p1.Add("ID_PZ", idPzOld);
                p1.Add("ORDERBY", orderByNew);
                p1.Add("DATA", dtNew.ToString());
            }

            MoveTask(p1);

            var p2 = new Dictionary<string, string>();
            {
                p2.Add("ID_PZ", idPzNew);
                p2.Add("ORDERBY", orderByOld);
                p2.Add("DATA", dtOld.ToString());
            }

            MoveTask(p2);
            // запускаем таймер на обновление грида с фиксацией строки
//            FlagFirst = false;

//            FirstTimer.Run();
            //обновить производственное задание
            LoadItems();

        }

        /// <summary>
        ////двигаем выбранное задание вниз
        /// </summary>
        private void DownMoveTask()
        {
            var cnt = SelectedTaskItem.CheckGet("_ROWNUMBER").ToInt();
            if (cnt == TaskGrid.Items.Count)
            {
                var dw = new DialogWindow($"Это самое нижнее задание.", "Работа с ПЗ", "", DialogWindowButtons.OK);
                dw.ShowDialog();
                return;
            }

            // запоминаем старые данные
            var numOld = SelectedTaskItem.CheckGet("NUM").ToString();
            var idPzOld = SelectedTaskItem.CheckGet("ID_PZ").ToInt().ToString();
            var dtOld = SelectedTaskItem.CheckGet("DATA_STR").ToDateTime();
            var orderByOld = SelectedTaskItem.CheckGet("ORDERBY").ToInt().ToString();

            // новые данные
            var numNew = "";
            var idPzNew = "";
            var dtNew = DateTime.Now;
            var orderByNew = "";

            var i = 0;

            // идем сверху вниз от первой записи
            foreach (var item in TaskGrid.Items)
            {
                // запомним промежуточные данные
                var numNew1 = item.CheckGet("NUM").ToString();
                var idPzNew1 = item.CheckGet("ID_PZ").ToInt().ToString();
                var dtNew1 = item.CheckGet("DATA_STR").ToDateTime();
                var orderByNew1 = item.CheckGet("ORDERBY").ToInt().ToString();

                if (numNew1 == numOld)    // дошли до текущей выбранной строки
                {
                    i = 1;
                }

                else
                {
                    if (i == 1)
                    {
                        // запомним следующие данные
                        numNew = numNew1;
                        idPzNew = idPzNew1;
                        dtNew = dtNew1;
                        orderByNew = orderByNew1;
                        break;
                    }
                }
            }

            // перемещаем
            var p1 = new Dictionary<string, string>();
            {
                p1.Add("ID_PZ", idPzOld);
                p1.Add("ORDERBY", orderByNew);
                p1.Add("DATA", dtNew.ToString());
            }

            MoveTask(p1);

            var p2 = new Dictionary<string, string>();
            {
                p2.Add("ID_PZ", idPzNew);
                p2.Add("ORDERBY", orderByOld);
                p2.Add("DATA", dtOld.ToString());
            }

            MoveTask(p2);
            // запускаем таймер на обновление грида с фиксацией строки
        //    FlagFirst = false;

        //    FirstTimer.Run();

            //обновить производственное задание
            LoadItems();
        }

        /// <summary>
        /// устанавливаем статус (1 - выполнена) для текущего задания (id_pz)
        /// </summary>
        /// <param name="p"></param>
        public async void CloseTask()
        {
            string error = "";
            var idPz = SelectedTaskItem.CheckGet("ID_PZ").ToInt();
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "ProductionTask");
            q.Request.SetParam("Action", "Finish");

            var p = new Dictionary<string, string>();
            {
                p.Add("ID", idPz.ToString());
            }

            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                //производственное задание
                LoadItems();
            }
            else
            {
                //q.ProcessError();
                error = q.GetError();
                LogMsg($"Ошибка при смене статуса задания {error}");
            }
        }

        /// <summary>
        /// перемещаем текущего задание (id_pz) вверх/вниз на одну позицию
        /// </summary>
        /// <param name="p"></param>
        public void MoveTask(Dictionary<string, string> p)
        {
            string error = "";

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "Monitoring");
            q.Request.SetParam("Action", "TaskMove");

            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
            }
            else
            {
                //q.ProcessError();
                error = q.GetError();
                LogMsg($"Ошибка при перемещение задания {error}");
            }
        }

        /// <summary>
        //// Экспорт заданий в Excel
        /// </summary>
        public void ExportToExcel()
        {
            TaskGrid.ItemsExportExcel();
        }

        ///////
    }
}
