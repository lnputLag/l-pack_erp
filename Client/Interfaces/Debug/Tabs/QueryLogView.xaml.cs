using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Debug
{
    /// <summary>
    /// Логика взаимодействия для ShipmentClicheView.xaml.
    /// </summary>
    public partial class QueryLogView:UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShipmentClicheView"/> class.
        /// </summary>
        public QueryLogView()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this,ProcessMessages);

            //регистрация обработчика клавиатуры
            PreviewKeyDown += OnKeyDown;


            InitGrid();
        }

        // данные для таблицы
        /// <summary>
        /// Gets or sets the ShipmentClicheDS.
        /// </summary>
        public ListDataSet ShipmentClicheDS { get; set; }

        // выбранная в гриде запись
        /// <summary>
        /// Gets or sets the SelectedItem.
        /// </summary>
        public Dictionary<string,string> SelectedItem { get; set; }

        /// <summary>
        /// The InitGrid.
        /// </summary>
        public void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn()
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=45,
                },
                new DataGridHelperColumn()
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=45,
                    MaxWidth=55,
                },
                new DataGridHelperColumn()
                {
                    Header="Номер клише",
                    Path="CLICHE_NUM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=80,
                    MaxWidth=100,
                },
                new DataGridHelperColumn()
                {
                    Header="Статус",
                    Path="STATUS_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=110,
                    MaxWidth=125,
                },
                new DataGridHelperColumn()
                {
                    Header="Акт",
                    Path="ACT_FILE_NAME_FLAG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth=40,
                    MaxWidth=40,
                },
                new DataGridHelperColumn()
                {
                    Header="Дата",
                    Group="Отгрузка",
                    Path="DT_OTGR",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    MinWidth=100,
                    MaxWidth=120,
                },
                new DataGridHelperColumn()
                {
                    Header="Покупатель",
                    Group="Отгрузка",
                    Path="POK_OTGR",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=150,
                    MaxWidth=470,
                },

                new DataGridHelperColumn()
                {
                    Header="Водитель",
                    Group="Отгрузка",
                    Path="DRIVER_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=150,
                    MaxWidth=900,
                },
                new DataGridHelperColumn()
                {
                    Header="Статус транспорта",
                    Path="TRANSPORT_STATUS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true
                },
                new DataGridHelperColumn()
                {
                    Header="Код статуса",
                    Path="STATUS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true
                },
                new DataGridHelperColumn()
                {
                    Header="Имя файла",
                    Path="ACT_FILE_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Hidden=true
                },
            };
            Grid.SetColumns(columns);
            // Раскраска строк
            Grid.RowStylers = new Dictionary<StylerTypeRef,StylerDelegate>()
            {
                {
                    StylerTypeRef.ForegroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";
                        if (row["STATUS"].ToInt() == 2)
                        {
                            if (row["TRANSPORT_STATUS"].ToInt() == 2)
                            {
                                color=HColor.MagentaFG;
                            }
                            else
                            {
                                color=HColor.BlueFG;
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
            // используем ту сортировку, которая определена в запросе.
            // добавили колонку с номером строки результата запроса, по ней выполним сортировку
            Grid.SetSorting("_ROWNNMBER",ListSortDirection.Ascending);

            Grid.SearchText = SearchText;
            Grid.Init();

            // контекстное меню
            Grid.Menu = new Dictionary<string,DataGridContextMenuItem>()
            {
                {
                    "ChangeStatus",
                    new DataGridContextMenuItem()
                    {
                        Header="Отметить статус",
                        Action=() =>
                        {
                        },
                        Items=new Dictionary<string, DataGridContextMenuItem>()
                        {
                            {
                                "StatusPrepareTransfer",
                                new DataGridContextMenuItem()
                                {
                                    Header ="Готово к передаче",
                                    Action=() =>
                                    {
                                        UpdateStatus(2);
                                    }
                                }
                            },
                            {
                                "StatusTransferred",
                                new DataGridContextMenuItem()
                                {
                                    Header ="Передано клиенту",
                                    Action=() =>
                                    {
                                        UpdateStatus(3);
                                    }
                                }
                            },
                        }
                    }
                },
                {
                    "ActFileNameShow",
                    new DataGridContextMenuItem()
                    {
                        Header="Открыть акт приема-передачи",
                        Action=() =>
                        {
                            OpenActFile();
                        }
                    }
                },
            };

            //данные грида
            Grid.OnLoadItems = LoadItems;
            //Grid.OnFilterItems = FilterItems;
            Grid.Run();

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem = selectedItem =>
            {
                if(selectedItem.Count > 0)
                {
                    UpdateActions(selectedItem);
                }
            };

            //фокус ввода
            Grid.Focus();
        }

        /// <summary>
        /// Загрузка данных из БД.
        /// </summary>
        public async void LoadItems()
        {
            GridToolbar.IsEnabled = false;
            Grid.ShowSplash();

            var q = new LPackClientQuery();
            var p = new Dictionary<string,string>();
            // Состояние чекбокса преобразуем в строку и передаём как параметр запроса
            var allCliche = (bool)AllClicheCheckBox.IsChecked;
            p.Add("AllRec",allCliche ? "1" : "0");

            await Task.Run(() =>
            {
                q = _LPackClientDataProvider.DoQueryGetResult("Shipments","ShipmentCliche","List","",p);
            }
            );
            if(q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                if(result != null)
                {
                    if(result.Count > 0)
                    {
                        if(result.ContainsKey("ShipmentCliche"))
                        {
                            ShipmentClicheDS = result["ShipmentCliche"];
                            ShipmentClicheDS?.Init();
                            Grid.UpdateItems(ShipmentClicheDS);
                        }
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            GridToolbar.IsEnabled = true;
            Grid.HideSplash();
        }

        /// <summary>
        /// обновление методов работы с выбранной записью.
        /// </summary>
        /// <param name="selectedItem">.</param>
        public void UpdateActions(Dictionary<string,string> selectedItem)
        {
            SelectedItem = selectedItem;

            Grid.Menu["ChangeStatus"].Items["StatusPrepareTransfer"].Enabled = false;
            Grid.Menu["ChangeStatus"].Items["StatusTransferred"].Enabled = false;

            if(SelectedItem != null)
            {
                int currentStatus = SelectedItem["STATUS"].ToInt();

                // настройка доступности пунктов контекстного меню
                //Grid.Menu["ChangeStatus"].Items["StatusPrepareTransfer"].Enabled = (currentStatus == 3);
                Grid.Menu["ChangeStatus"].Items["StatusTransferred"].Enabled = (currentStatus == 2);
                Grid.Menu["ActFileNameShow"].Enabled = SelectedItem["ACT_FILE_NAME_FLAG"].ToBool();

                //в отладочном режиме разрешено все
                if(Central.DebugMode)
                {
                    Grid.Menu["ChangeStatus"].Items["StatusPrepareTransfer"].Enabled = true;
                    Grid.Menu["ChangeStatus"].Items["StatusTransferred"].Enabled = true;
                }
            }
        }

        /// <summary>
        /// Остановка вспомогательных процессов при закрытии интерфейса.
        /// </summary>
        public void Destroy()
        {
            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            Grid.Destruct();
        }

        /// <summary>
        /// обновление статуса отгрузки.
        /// </summary>
        /// <param name="newStatus">Значение нового статуса.</param>
        private async void UpdateStatus(int newStatus)
        {
            if(SelectedItem != null)
            {
                var clic_id = SelectedItem["ID"].ToInt();
                if(clic_id != 0)
                {
                    var q = new LPackClientQuery();
                    var p = new Dictionary<string,string>()
                    {
                        { "IdClic", clic_id.ToString() },
                        { "Status", newStatus.ToString() },
                        { "ClientStatus", "0" }
                    };

                    await Task.Run(() =>
                    {
                        q = _LPackClientDataProvider.DoQueryGetResult("Shipments","ShipmentCliche","UpdateStatus","",p);
                    }
                    );

                    if(q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string,string>>(q.Answer.Data);
                        if(result != null)
                        {
                            if(result.Count > 0)
                            {
                                Grid.LoadItems();
                            }
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
            }
        }

        /// <summary>
        /// обработчик сообщений.
        /// </summary>
        /// <param name="m">сообщение.</param>
        private void ProcessMessages(ItemMessage m)
        {
            //Group ShipmentControl
            if(m.ReceiverGroup.IndexOf("ShipmentControl") > -1)
            {
                if(m.ReceiverName.IndexOf("ShipmentCliche") > -1)
                {
                    switch(m.Action)
                    {
                        case "Refresh":
                            Grid.LoadItems();
                            break;
                    }

                }
            }
        }

        /// <summary>
        /// The OnKeyDown.
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="KeyEventArgs"/>.</param>
        private void OnKeyDown(object sender,KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.F5:
                    Grid.LoadItems();
                    e.Handled = true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

                case Key.Home:
                    Grid.SetSelectToFirstRow();
                    e.Handled = true;
                    break;

                case Key.End:
                    Grid.SetSelectToLastRow();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// The ShowHelp.
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/shipments/control/equipments/shipmentcliche");
        }

        /// <summary>
        /// обработчик системы навигации по URL.
        /// </summary>
        public void ProcessNavigation()
        {
        }

        /// <summary>
        /// The OnLoad.
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="RoutedEventArgs"/>.</param>
        private void OnLoad(object sender,RoutedEventArgs e)
        {
            Grid.UpdateGrid();
        }

        /// <summary>
        /// The OpenActFile.
        /// </summary>
        private async void OpenActFile()
        {
            if(SelectedItem != null)
            {
                if(SelectedItem.ContainsKey("ACT_FILE_NAME"))
                {
                    var actFileName = SelectedItem["ACT_FILE_NAME"].ToString();
                    if(!string.IsNullOrEmpty(actFileName))
                    {
                        var q = new LPackClientQuery();
                        var p = new Dictionary<string,string>()
                        {
                            { "actFileName", actFileName }
                        };

                        await Task.Run(() =>
                        {
                            q = _LPackClientDataProvider.DoQueryGetResult("Shipments","ShipmentCliche","GetActFile","",p);
                        }
                        );

                        if(q.Answer.Status == 0)
                        {
                            var result = JsonConvert.DeserializeObject<Dictionary<string,string>>(q.Answer.Data);
                            if(result != null)
                            {
                                if(result.Count > 0)
                                {
                                    if(result.ContainsKey("documentFile"))
                                    {
                                        Central.OpenFile(result["documentFile"]);
                                    }
                                }
                            }
                        }
                        else
                        {
                            q.ProcessError();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The RefreshButton_Click.
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="RoutedEventArgs"/>.</param>
        private void RefreshButton_Click(object sender,RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        /// <summary>
        /// The AllClicheCheckBox_Click.
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="RoutedEventArgs"/>.</param>
        private void AllClicheCheckBox_Click(object sender,RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        /// <summary>
        /// The HelpButton_Click.
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="RoutedEventArgs"/>.</param>
        private void HelpButton_Click(object sender,RoutedEventArgs e)
        {
            ShowHelp();
        }
    }
}
