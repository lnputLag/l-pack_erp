using Client.Common;
using Client.Interfaces.Main;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Xpf;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using NPOI.HSSF.Record.Chart;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Список заданий (в работе/выполненные) для ПРС на БДМ1
    /// </summary>
    /// <author>Greshnyh_NI</author>   
    public partial class PaperMakingSlitter1Task : ControlBase
    {
        public PaperMakingSlitter1Task()
        {
            ControlTitle = "Отчет задания/рулоны";
            InitializeComponent();

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    ProcessMessages(m);
                    ProcessCommand(m.Action, m);
                }
            };


            OnKeyPressed = (KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    switch (e.Key)
                    {
                        case Key.F1:
                            ProcessCommand("help");
                            e.Handled = true;
                            break;
                    }
                }

                if (!e.Handled)
                {
                    //TaskGrid.ProcessKeyboard(e);
                }
            };


            //конструктор, будет вызван, когда объект создается
            //здесь создаются все внутренние структуры
            //впервые этот коллбэк будет вызван, когда данный таб станет активным
            //впервые (до этих пор, никакая работа внутри не происходит, что экономит ресурсы)
            OnLoad = () =>
            {
                //регистрация обработчика сообщений
               // Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

                // получение прав пользователя
                ProcessPermissions();
                FormInit();
                SetDefaults();
                TaskGridInit();
                RollsGridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                Messenger.Default.Unregister<ItemMessage>(this);
                TaskGrid.Destruct();
                RollsGrid.Destruct();
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


        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// ИД  станка = 716
        /// </summary>
        public int MachineId { get; set; }

        public ListDataSet TaskDataSet { get; set; }

        public bool ReadOnlyFlag { get; set; }

        /// <summary>
        /// данные из выбранной в гриде рулонов строки
        /// </summary>
        Dictionary<string, string> SelectedItemRolls { get; set; }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel("[erp]slitter_bdm2");
            var userAccessMode = mode;
            ReadOnlyFlag = true;

            switch (mode)
            {
                case Role.AccessMode.Special:
                    {
                        ReadOnlyFlag = false;
                    }

                    break;

                case Role.AccessMode.FullAccess:
                    ReadOnlyFlag = false;
                    break;

                case Role.AccessMode.ReadOnly:
                    {
                        UpdatePzButton.IsEnabled = false;
                    }
                    break;
            }
        }

        /// <summary>
        /// инициализация формы, элементы тулбара 
        /// </summary>
        public void FormInit()
        {
            {
                Form = new FormHelper();

                //колонки формы
                var fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path = "FROM_DATE",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = FromDate,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "FROM_TIME",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = TimeStart,
                        ControlType = "SelectBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },

                    new FormHelperField()
                    {
                        Path = "TO_DATE",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = ToDate,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "TO_TIME",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = TimeEnd,
                        ControlType = "SelectBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                };

                Form.SetFields(fields);
            }
        }


        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            MachineId = 716;
            TaskDataSet = new ListDataSet();

            Form.SetValueByPath("FROM_DATE", DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy"));
            Form.SetValueByPath("TO_DATE", DateTime.Now.AddDays(1).ToString("dd.MM.yyyy"));

            var list = new Dictionary<string, string>();
            list.Add("0", "00:00");
            list.Add("1", "01:00");
            list.Add("2", "02:00");
            list.Add("3", "03:00");
            list.Add("4", "04:00");
            list.Add("5", "05:00");
            list.Add("6", "06:00");
            list.Add("7", "07:00");
            list.Add("8", "08:00");
            list.Add("9", "09:00");
            list.Add("10", "10:00");
            list.Add("11", "11:00");
            list.Add("12", "12:00");
            list.Add("13", "13:00");
            list.Add("14", "14:00");
            list.Add("15", "15:00");
            list.Add("16", "16:00");
            list.Add("17", "17:00");
            list.Add("18", "18:00");
            list.Add("19", "19:00");
            list.Add("20", "20:00");
            list.Add("21", "21:00");
            list.Add("22", "22:00");
            list.Add("23", "23:00");
            TimeStart.SetItems(list);
            TimeEnd.SetItems(list);

            Form.SetValueByPath("FROM_TIME", "8");
            Form.SetValueByPath("TO_TIME", "8");
        }

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {

            TaskGrid.Destruct();
            RollsGrid.Destruct();

            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Production",
                ReceiverName = "",
                SenderName = "PaperMakingSlitter1Task",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            string action = m.Action;
            switch (action)
            {
                case "RefreshRolls":
                    RollsGrid.LoadItems();
                    break;
            }

        }

        /// <summary>
        ///  Обработчик команд
        /// </summary>
        /// <param name="command"></param>
        /// <param name="m"></param>
        public void ProcessCommand(string command, ItemMessage m = null)
        {
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "refresh":
                        TaskGrid.LoadItems();
                        break;

                    case "help":
                        Central.ShowHelp("/doc/l-pack-erp/");
                        break;

                }
            }
        }

        /// <summary>
        /// список заданий на ПРС
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
                        Width2=4,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Задание",
                        Path="NUM_PZ",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Path="ID_PZ",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ID2",
                        Path="ID2",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=10,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=34,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                         {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => GetColor("NAME", row)
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header=" Количество (по заданию/выполнено)",
                        Path="KOL",
                        ColumnType=ColumnTypeRef.String,
                        Width2=9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Выполнено",
                        Path="POSTING",
                        ColumnType=ColumnTypeRef.String,
                        Width2=7,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="На продажу",
                        Path="SALE_FLAG",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Начать",
                        Path="PZ_DATA_PLAN",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2=15,
                        Visible=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дт/вр окончания",
                        Path="PZ_DATA_END",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2=15,
                        Visible=true,
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
                TaskGrid.SetColumns(columns);
                TaskGrid.SetPrimaryKey("_ROWNUMBER");
                TaskGrid.SetSorting("NUM_PZ", ListSortDirection.Ascending);
                TaskGrid.AutoUpdateInterval = 0;
                TaskGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                TaskGrid.Toolbar = GridToolbar;
                TaskGrid.SearchText = SearchText;
                // при выборе строки
                TaskGrid.OnSelectItem = selectedItem =>
                {
                    RollsGrid.LoadItems();
                };

                //данные грида
                TaskGrid.OnLoadItems = TaskGridLoadItems;
                //TaskGrid.Commands = Commander;
                TaskGrid.Init();
            }
        }

        /// <summary>
        /// список намотаных рулонов по заданию 
        /// </summary>
        public void RollsGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.String,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="Рулон",
                    Path="ROLL",
                    ColumnType=ColumnTypeRef.String,
                    Doc="№Тамбура.№рулона",
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="NAME",
                    Doc="Наименование продукции",
                    ColumnType=ColumnTypeRef.String,
                    Width2=36,
                },
                new DataGridHelperColumn
                {
                    Header="Номер рулона",
                    Path="ROLL_NUM",
                    Doc="Номер рулона",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Вес рулона",
                    Path="ROLL_WEIGHT",
                    Doc="Вес рулона",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                    TotalsType=TotalsTypeRef.Summ,
                },
                new DataGridHelperColumn
                {
                    Header="Длина рулона",
                    Path="LENGTH_PRS",
                    Doc="Длина рулона",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Диаметр рулона",
                    Path="ROLL_DIAMETER",
                    Doc="Диаметр рулона",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Номер ПЗ",
                    Path="NUM",
                    Doc="Номер рулона",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="ИД рулона",
                    Path="IDP_ROLL",
                    Doc="IDP рулона",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                    //Visible = false,
                },
            };

            RollsGrid.SetColumns(columns);
            RollsGrid.SetPrimaryKey("_ROWNUMBER");
            //  RollsGrid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
            RollsGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            RollsGrid.ItemsAutoUpdate = false;
            //RollsGrid.Commands = Commander;

            RollsGrid.OnLoadItems = RollsGridLoadItems;

            RollsGrid.OnFilterItems = () =>
            {
                var id2 = RollsGroup.SelectedItem.Key.ToInt();

                if (RollsGrid.Items.Count > 0)
                {
                    if (id2 != 0)
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in RollsGrid.Items)
                        {
                            if (row.CheckGet("ID2").ToInt() == id2)
                            {
                                items.Add(row);
                            }
                            RollsGrid.Items = items;
                        }
                    }
                }
            };

            {
                //при выборе строки в гриде, обновляются актуальные действия для записи
                RollsGrid.OnSelectItem = selectedItem =>
                {
                    SelectedItemRolls = selectedItem;
                };

            }

            RollsGrid.Init();
        }

        /// <summary>
        /// данные для списка заданий 
        /// </summary>
        public async void TaskGridLoadItems()
        {
            if (Form.Validate())
            {
                string fromDateTime = $"{Form.GetValueByPath("FROM_DATE")} {TimeStart.SelectedItem.Value}:00";
                string toDateTime = $"{Form.GetValueByPath("TO_DATE")} {TimeEnd.SelectedItem.Value}:00";

                var dtFrom = fromDateTime.ToDateTime("dd.MM.yyyy HH:mm:ss");
                var dtTo = toDateTime.ToDateTime("dd.MM.yyyy HH:mm:ss");

                if (DateTime.Compare(dtFrom, dtTo) > 0)
                {
                    const string msg = "Дата начала должна быть меньше даты окончания.";
                    var d = new DialogWindow($"{msg}", "Проверка данных");
                    d.ShowDialog();
                }
                else
                {
                    var p = new Dictionary<string, string>();
                    p.Add("ID_ST", MachineId.ToString());
                    p.Add("FROM_DT", fromDateTime);
                    p.Add("TO_DT", toDateTime);

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "ProductionPm");
                    q.Request.SetParam("Object", "PmSlitter");
                    q.Request.SetParam("Action", "TaskSlitterList");

                    q.Request.SetParams(p);

                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    TaskDataSet = new ListDataSet();
                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            TaskDataSet = ListDataSet.Create(result, "ITEMS");
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                    TaskGrid.UpdateItems(TaskDataSet);

                    // Группировка по номеру ПЗ
                    {
                        var column = TaskGrid.GridControl.Columns["NUM_PZ"];
                        if (column != null)
                        {
                            column.GroupIndex = 1;
                        }
                    }

                    TaskGrid.GridControl.ExpandAllGroups();
                }
            }
        }

        /// <summary>
        /// данные для списка намотанных рулонов
        /// </summary>
        public async void RollsGridLoadItems()
        {
            bool resume = true;

            UpdatePzButton.IsEnabled = false;

            int IdPz = TaskGrid.SelectedItem.CheckGet("ID_PZ").ToInt();
            int id21 = 0;
            int id22 = 0;
            int id23 = 0;

            ///// отладка
            //if (IdPz != 0)
            //{
            //    var s = $"( _ROWNUMBER =  {TaskGrid.SelectedItem.CheckGet("_ROWNUMBER")}  IdPz= {IdPz} Id2= {TaskGrid.SelectedItem.CheckGet("ID2")} )";  
            //    //var e = new DialogWindow($"{s}", "IdPzParent");
            //    var e = new LogWindow(s, "Отладка");
            //    e.ShowDialog();
            //}
            //resume = false;
            /////

            if ((resume) && (IdPz != 0))
            {
                // получаем ID2 для указанного Id_pz
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID_PZ", IdPz.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "ProductionPm");
                q.Request.SetParam("Object", "PmSlitter");
                q.Request.SetParam("Action", "Id2List");
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
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            if (ds.Items.Count > 0)
                            {

                                var list = new Dictionary<string, string>();
                                list.Add("0", "Все");

                                foreach (Dictionary<string, string> row in ds.Items)
                                {
                                    var id = row.CheckGet("ID").ToInt();
                                    list.Add(row.CheckGet("ID"), row.CheckGet("NAME"));

                                    switch (row.CheckGet("_ROWNUMBER"))
                                    {
                                        case "1":
                                            id21 = id;
                                            break;
                                        case "2":
                                            id22 = id;
                                            break;
                                        case "3":
                                            id23 = id;
                                            break;
                                        default:
                                            break;
                                    }
                                }
                                RollsGroup.Items = list;
                                RollsGroup.SetSelectedItemByKey("0");
                            }
                            else
                            {
                                resume = false;
                            }
                        }
                    }
                    else
                    {
                        resume = false;
                    }
                }
                else
                {
                    resume = false;
                }

                if (resume)
                {
                    // получаем данные для списка без фильтрации
                    var p2 = new Dictionary<string, string>();
                    p2.CheckAdd("ID_ST", MachineId.ToString());
                    p2.CheckAdd("ID_PZ", IdPz.ToString());
                    p2.CheckAdd("ID2_1", id21.ToString());
                    p2.CheckAdd("ID2_2", id22.ToString());
                    p2.CheckAdd("ID2_3", id23.ToString());

                    var q2 = new LPackClientQuery();
                    q2.Request.SetParam("Module", "ProductionPm");
                    q2.Request.SetParam("Object", "PmSlitter");
                    q2.Request.SetParam("Action", "RollsSlitterList");
                    q2.Request.SetParams(p2);

                    q2.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q2.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    await Task.Run(() =>
                    {
                        q2.DoQuery();
                    });

                    if (q2.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q2.Answer.Data);
                        if (result != null)
                        {
                            {
                                var ds = ListDataSet.Create(result, "ITEMS");
                                RollsGrid.UpdateItems(ds);

                                if (!ReadOnlyFlag && ds.Items.Count > 0)
                                {
                                    UpdatePzButton.IsEnabled = true;
                                }

                            }
                        }
                    }
                }
            }
        }

        //////////////////////////////////////////////////
        /// Вспомогательные функции
        ////////////////////////////////////////////////// 

        /// <summary>
        /// Возвращает цвет для ячейки списка заданий
        /// </summary>
        public static object GetColor(string fieldName, Dictionary<string, string> row)
        {
            var result = DependencyProperty.UnsetValue;
            var color = "";

            if ((fieldName == "NAME"))
            {
                if (row.CheckGet("SALE_FLAG").ToInt() == 1)
                {
                    //   R  G   B
                    // 172-237-247   морская волна
                    color = $"#acedf7";
                }
            }
            if (!color.IsNullOrEmpty())
            {
                result = color.ToBrush();
            }

            return result;
        }

        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
            TaskGrid.ShowSplash();
        }

        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
            TaskGrid.HideSplash();
        }

        /// <summary>
        /// Текущая смена
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCurrentShift(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now;
            if (date.Hour >= 20 || date.Hour < 8)
            {
                TimeStart.SetSelectedItemByKey("20");
                TimeEnd.SetSelectedItemByKey("8");

                if (date.Hour >= 20)
                {
                    FromDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
                    ToDate.Text = DateTime.Now.AddDays(1).ToString("dd.MM.yyyy");
                }
                else
                {
                    FromDate.Text = DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy");
                    ToDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
                }
            }
            else
            {
                FromDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
                ToDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
                TimeStart.SetSelectedItemByKey("8");
                TimeEnd.SetSelectedItemByKey("20");
            }

            ProcessCommand("refresh");
        }

        /// <summary>
        /// Предыдущая смена
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPrevShift(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now.AddHours(-12);
            if (date.Hour >= 20 || date.Hour < 8)
            {
                TimeStart.SetSelectedItemByKey("20");
                TimeEnd.SetSelectedItemByKey("8");

                if (date.Hour >= 20)
                {
                    FromDate.Text = date.ToString("dd.MM.yyyy");
                    ToDate.Text = date.AddDays(1).ToString("dd.MM.yyyy");
                }
                else
                {
                    FromDate.Text = date.AddDays(-1).ToString("dd.MM.yyyy");
                    ToDate.Text = date.ToString("dd.MM.yyyy");
                }
            }
            else
            {
                FromDate.Text = date.ToString("dd.MM.yyyy");
                ToDate.Text = date.ToString("dd.MM.yyyy");
                TimeStart.SetSelectedItemByKey("8");
                TimeEnd.SetSelectedItemByKey("20");
            }

            ProcessCommand("refresh");
        }

        /// <summary>
        /// Текущие сутки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCurrentDay(object sender, RoutedEventArgs e)
        {
            FromDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
            ToDate.Text = DateTime.Now.AddDays(1).ToString("dd.MM.yyyy");
            TimeStart.SetSelectedItemByKey("0");
            TimeEnd.SetSelectedItemByKey("0");

            ProcessCommand("refresh");
        }

        /// <summary>
        /// Предыдущие сутки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPrevDay(object sender, RoutedEventArgs e)
        {
            FromDate.Text = DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy");
            ToDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
            TimeStart.SetSelectedItemByKey("0");
            TimeEnd.SetSelectedItemByKey("0");

            ProcessCommand("refresh");
        }

        /// <summary>
        /// Текущая неделя
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCurrentWeek(object sender, RoutedEventArgs e)
        {
            DayOfWeek day = DateTime.Now.DayOfWeek;
            int days = day - DayOfWeek.Monday;
            DateTime date = DateTime.Now.AddDays(-days);
            FromDate.Text = date.Date.ToString("dd.MM.yyyy");
            ToDate.Text = date.Date.AddDays(7).ToString("dd.MM.yyyy");
            TimeStart.SetSelectedItemByKey("0");
            TimeEnd.SetSelectedItemByKey("0");

            ProcessCommand("refresh");
        }

        /// <summary>
        /// Предыдущая неделя
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPrevWeek(object sender, RoutedEventArgs e)
        {
            DayOfWeek day = DateTime.Now.DayOfWeek;
            int days = day - DayOfWeek.Monday;
            DateTime date = DateTime.Now.AddDays(-days).AddDays(-7);
            FromDate.Text = date.Date.ToString("dd.MM.yyyy");
            ToDate.Text = date.Date.AddDays(7).ToString("dd.MM.yyyy");
            TimeStart.SetSelectedItemByKey("0");
            TimeEnd.SetSelectedItemByKey("0");

            ProcessCommand("refresh");
        }

        /// <summary>
        /// Текущий месяц
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCurrentMonth(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now;
            FromDate.Text = new DateTime(date.Year, date.Month, 1).ToString("dd.MM.yyyy");
            ToDate.Text = new DateTime(date.Year, date.Month, 1).AddMonths(1).ToString("dd.MM.yyyy");
            TimeStart.SetSelectedItemByKey("0");
            TimeEnd.SetSelectedItemByKey("0");

            ProcessCommand("refresh");
        }

        /// <summary>
        /// Предыдущий месяц
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPrevMonth(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now.AddMonths(-1);
            FromDate.Text = new DateTime(date.Year, date.Month, 1).ToString("dd.MM.yyyy");
            ToDate.Text = new DateTime(date.Year, date.Month, 1).AddMonths(1).ToString("dd.MM.yyyy");
            TimeStart.SetSelectedItemByKey("0");
            TimeEnd.SetSelectedItemByKey("0");

            ProcessCommand("refresh");
        }

        /// <summary>
        /// Показать
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResreshButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessCommand("refresh");
        }

        /// <summary>
        ///  Свернуть
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CollapseButton_Click(object sender, RoutedEventArgs e)
        {
            TaskGrid.GridControl.CollapseAllGroups();
        }


        /// <summary>
        /// Развернуть
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExpandButton_Click(object sender, RoutedEventArgs e)
        {
            TaskGrid.GridControl.ExpandAllGroups();
        }


        /// <summary>
        ///  Помощь (документация)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        ///  фильтруем список рулонов
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void RollsGroup_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RollsGrid.UpdateItems();
        }

        private void FromDate_TextChanged(object sender, TextChangedEventArgs e)
        {
            ResreshButton.Style = (Style)ResreshButton.TryFindResource("FButtonPrimary");
        }

        private void ToDate_TextChanged(object sender, TextChangedEventArgs e)
        {
            ResreshButton.Style = (Style)ResreshButton.TryFindResource("FButtonPrimary");
        }

        /// <summary>
        /// вызываем форму присвоения ПЗ для выбранного рулона
        /// </summary>
        private void ShowPz()
        {
            //var idp = RollsGrid.SelectedItem.CheckGet("IDP_ROLL").ToInt();
            //var e = new DialogWindow($"idp = {idp}", "Внимание").ShowDialog();

            var pzRecord = new EditPz1Form(RollsGrid.SelectedItem as Dictionary<string, string>);
            pzRecord.ReceiverName = ControlName;
            pzRecord.Edit();

        }

        /// <summary>
        ///  Изменить задания для выбранного рулона
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdatePzButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPz();
        }


        ////----
    }
}
