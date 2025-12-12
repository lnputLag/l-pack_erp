using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Работа транспортной системы Fosber
    /// </summary>
    /// <author>zelenskiy_sv</author>   
    public partial class FosberTransportMonitor : UserControl
    {
        public FosberTransportMonitor()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            InitForm();

            RollsGridInit();
            DB900GridInit();

            SetDefaults();

            ProcessPermissions();
        }

        public string RoleName = "[erp]fosber_transport_system";

        /// <summary>
        /// данные из выбранной в гриде строки
        /// </summary>
        private Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// форма с полями для фильтрации данных
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// таймер периодического обновления
        /// </summary>
        private DispatcherTimer RefreshTimer { get; set; }

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
        }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void InitForm()
        {
            //инициализация формы
            {
                Form = new FormHelper();

                //после установки значений
                Form.AfterSet = (Dictionary<string, string> v) =>
                {
                };
            }

            RefreshTimer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 10)
            };

            {
                var row = new Dictionary<string, string>();
                row.CheckAdd("TIMEOUT", "10");
                row.CheckAdd("DESCRIPTION", "");
                Central.Stat.TimerAdd("FosberTransportMonitor_InitForm", row);
            }

            RefreshTimer.Tick += (s, e) =>
            {
                RollsGrid.LoadItems();
                DB900Grid.LoadItems();
                RefreshDiagram();
            };

        }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void RollsGridInit()
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
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД рулона",
                        Path="IDP_ROLL",
                        Doc="ИД рулона",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=55,
                        MaxWidth=150,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Бумага",
                        Path="NAME",
                        Doc="Бумага",
                        ColumnType=ColumnTypeRef.String,
                        Width=200,
                    },
                    //prihod.num
                    new DataGridHelperColumn
                    {
                        Header="№ внутренний",
                        Path="NUM",
                        Doc="№ внутренний (prihod.num)",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=80,
                        MaxWidth=200,
                    },
                    //prihod.name_roll
                    new DataGridHelperColumn
                    {
                        Header="№ внешний",
                        Path="NAME_ROLL",
                        Doc="№ внешний (prihod.name_roll)",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=80,
                        MaxWidth=200,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Раскат",
                        Path="REEL",
                        Doc="Раскат",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=35,
                        MaxWidth=80,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Сторона",
                        Path="SIDE",
                        Doc="Сторона раската",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=35,
                        MaxWidth=100,
                        FormatterRaw=(v) =>
                        {
                            var result = "";
                            if (v.CheckGet("POSITION").ToInt() > 0)
                            {
                                switch (v.CheckGet("SIDE").ToInt())
                                {
                                    case 0:
                                        result = "Авто";
                                        break;
                                    case 1:
                                        result = "Левая";
                                        break;
                                    case 2:
                                        result = "Правая";
                                        break;
                                    default:
                                        result = "";
                                        break;
                                }
                            }
                            return result;
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Позиция",
                        Path="POSITION",
                        Doc="Позиция рулона в трвнспортной системе",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=35,
                        MaxWidth=100,
                    },
                    new DataGridHelperColumn
                    {
                        Header="MOVE_FLAG",
                        Path="MOVE_FLAG",
                        Doc="",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header=" ",
                        Path="_",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=5,
                        MaxWidth=500,
                    },
                };
                RollsGrid.SetColumns(columns);

                // Раскраска строк
                RollsGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()

                {
                // Цвета фона строк
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";
                        var currentStatus = row.CheckGet("MOVE_FLAG").ToString();

                        if (currentStatus == "0")
                        {
                            color = HColor.Green;
                        }

                        if (currentStatus == "1")
                        {
                            color = HColor.Blue;
                        }

                        if (currentStatus == "2")
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
            };

                RollsGrid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
                RollsGrid.UseRowHeader = false;
                RollsGrid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                RollsGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem.Count > 0)
                    {
                        UpdateActions(selectedItem);
                    }
                };

                //данные грида
                RollsGrid.OnLoadItems = RollsGridLoadItems;

                RollsGrid.Run();

                //фокус ввода           
                RollsGrid.Focus();
            }
        }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void DB900GridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Позиция",
                        Path="POSITION",
                        Name="col2",
                        Doc="Позиция рулона в транспортной системе",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=37,
                        Width=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД рулона",
                        Path="IDP",
                        Name="col1",
                        Doc="ИД рулона в заводской системе (idp_roll)",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=37,
                        Width=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Смотан",
                        Path="REMOVED_FLAG",
                        Name="col8",
                        Doc="Рулон полностью смотан на ГА",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=37,
                        Width=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Формат",
                        Path="ROLL_WIDTH",
                        Name="col10",
                        Doc="Ширина, мм",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=50,
                        Width=100,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Направление",
                        Path="MOVE_FLAG",
                        Name="col13",
                        Doc="Куда в данный момент направляется рулон",
                        ColumnType=ColumnTypeRef.String,
                        FormatterRaw=(v) =>
                        {
                            var result="";
                            switch (v.CheckGet("MOVE_FLAG").ToInt())
                            {
                                case 0:
                                    result=$"На месте";
                                    break;
                                case 1:
                                    result=$"На раскат";
                                    break;
                                case 2:
                                    result=$"На выход";
                                    break;
                                default:
                                    break;
                            }
                            return result;
                        },
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                (Dictionary<string, string> row) =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    switch (row.CheckGet("MOVE_FLAG").ToInt())
                                    {
                                        case 0:
                                            // На месте
                                            color = HColor.Green;
                                            break;
                                        case 1:
                                            // На раскат
                                            color = HColor.Blue;
                                            break;
                                        case 2:
                                            // На выход
                                            color = HColor.Yellow;
                                            break;
                                        default:
                                            break;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },

                        MinWidth=50,
                        Width=100,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вес рулона",
                        Path="ROLL_WEIGHT",
                        Name="col15",
                        Description="Вес рулона на выходе, кг",
                        Doc="Вес рулона на выходе, кг",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=50,
                        Width=100,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Раскат",
                        Path="REEL",
                        Name="col11",
                        Description="Раскат, на который требуется привести рулон",
                        Doc="Раскат, на который требуется привести рулон",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=37,
                        Width=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Сторона раската",
                        Path="SIDE",
                        Name="col12",
                        Description="Сторона раската",
                        Doc="Сторона раската",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=37,
                        Width=50,
                        FormatterRaw=(v) =>
                        {
                            var result = "";

                            switch (v.CheckGet("SIDE").ToInt())
                            {
                                case 0:
                                    result = "Авто";
                                    break;
                                case 1:
                                    result = "Левая";
                                    break;
                                case 2:
                                    result = "Правая";
                                    break;
                                default:
                                    result = "";
                                    break;
                            }

                            return result;
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Время установки",
                        Path="PLACED_DTTM",
                        Name="col14",
                        Description="Время нахождения рулона на месте назначения",
                        Doc="Время нахождения рулона на месте назначения",
                        ColumnType=ColumnTypeRef.DateTime,
                        MinWidth=50,
                        Format="HH:mm:ss",
                        Width=100,
                    },
                };
                DB900Grid.SetColumns(columns);

                DB900Grid.SetSorting("POSITION", ListSortDirection.Ascending);
                DB900Grid.UseRowHeader = false;
                DB900Grid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                DB900Grid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem.Count > 0)
                    {
                        UpdateActions(selectedItem);
                    }
                };

                //данные грида
                DB900Grid.OnLoadItems = DB900GridLoadItems;

                DB900Grid.Run();

                //фокус ввода           
                DB900Grid.Focus();
            }

            RefreshDiagram();
        }

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            // Остановка таймера
            RefreshTimer.Stop();

            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Production",
                ReceiverName = "",
                SenderName = "FosberTransportMonitor",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            RollsGrid.Destruct();
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.IndexOf("Production") > -1)
            {
                switch (m.Action)
                {
                    case "Refresh":
                        RollsGrid.LoadItems();

                        // выделение на новую строку
                        var id = m.Message.ToInt();
                        RollsGrid.SetSelectedItemId(id);
                        break;
                }
            }
        }

        /// <summary>
        /// получение записей
        /// </summary>
        public async void RollsGridLoadItems()
        {
            DisableControls();

            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID", "22");
                }
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "FosberTransport");
                q.Request.SetParam("Action", "ListRolls");
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
                            RollsGrid.UpdateItems(ds);
                        }
                    }
                }
            }

            EnableControls();
        }

        /// <summary>
        /// получение записей
        /// </summary>
        public async void DB900GridLoadItems()
        {
            DisableControls();

            bool resume = true;

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "FosberTransport");
                q.Request.SetParam("Action", "ListVariables");
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
                            DB900Grid.UpdateItems(ds);
                        }
                    }
                }
            }

            RefreshDiagram();

            EnableControls();
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            DB900Grid.ShowSplash();
            RollsGrid.ShowSplash();
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            DB900Grid.HideSplash();
            RollsGrid.HideSplash();
        }

        /// <summary>
        /// обновление методов работы с выбранной в гриде строкой
        /// </summary>
        /// <param name="selectedItem"></param>
        public void UpdateActions(Dictionary<string, string> selectedItem)
        {
            SelectedItem = selectedItem;
        }

        /// <summary>
        /// обработка ввода с клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.F5:
                    RollsGrid.LoadItems();
                    DB900Grid.LoadItems();
                    RefreshDiagram();
                    e.Handled = true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

                case Key.Home:
                    RollsGrid.SetSelectToFirstRow();
                    DB900Grid.SetSelectToFirstRow();
                    e.Handled = true;
                    break;

                case Key.End:
                    RollsGrid.SetSelectToLastRow();
                    DB900Grid.SetSelectToLastRow();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// отображение справочной статьи
        /// (относительный путь)
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/production/fosber_transport");
        }

        /// <summary>
        /// Обновление обновление информации по рулонам в ТС
        /// </summary>
        private void RefreshDiagram()
        {
            TransportDiagram.ClearPositionColors();

            if (DB900Grid.Items?.Count > 0)
            {
                foreach (var item in DB900Grid.Items)
                {
                    switch (item.CheckGet("MOVE_FLAG").ToInt())
                    {
                        case 0:
                            TransportDiagram.SetPositionColor(item.CheckGet("POSITION").ToInt(), HColor.Green, item.CheckGet("REEL").ToInt(), item.CheckGet("SIDE").ToInt());
                            break;
                        case 1:
                            TransportDiagram.SetPositionColor(item.CheckGet("POSITION").ToInt(), HColor.Blue, item.CheckGet("REEL").ToInt(), item.CheckGet("SIDE").ToInt());
                            break;
                        case 2:
                            TransportDiagram.SetPositionColor(item.CheckGet("POSITION").ToInt(), HColor.Yellow, item.CheckGet("REEL").ToInt(), item.CheckGet("SIDE").ToInt());
                            break;
                        default:
                            break;
                    }

                    if (item.CheckGet("IDP").ToInt() <= 0
                        && item.CheckGet("POSITION").ToInt() > 2)
                    {
                        TransportDiagram.SetPositionColor(item.CheckGet("POSITION").ToInt(), HColor.Red, item.CheckGet("REEL").ToInt(), item.CheckGet("SIDE").ToInt());
                    }
                }
            }
        }

        /// <summary>
        /// Включение, отключение автообновления
        /// </summary>
        private void RefreshTimerUpdate()
        {
            var selected = (bool)DiagramAutoUpdate.IsChecked;

            if (selected)
                RefreshTimer.Start();
            else
                RefreshTimer.Stop();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void RefreshButtonDiagram_Click(object sender, RoutedEventArgs e)
        {
            RollsGrid.LoadItems();
            DB900Grid.LoadItems();
            RefreshDiagram();
        }

        private void DiagramAutoUpdate_Click(object sender, RoutedEventArgs e)
        {
            RefreshTimerUpdate();
        }
    }
}
