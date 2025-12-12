using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Управление отгрузкой образцов
    /// </summary>
    /// <author>Рясной П.В.</author>
    public partial class ShipmentSamples : UserControl
    {
        /// <summary>
        /// Инициализация элемента интерфейса Управление отгрузкой образцов
        /// </summary>
        public ShipmentSamples()
        {
            InitializeComponent();

            FactoryId = 1;

            ProcessPermissions();
            GridInit();
            SetDefaults();

            Messenger.Default.Register<ItemMessage>(this, _ProcessMessages);
            Central.Msg.Register(ProcessMessages);

            PreviewKeyDown += OnKeyDown;
        }

        /// <summary>
        /// данные для таблицы Управление отгрузкой образцов
        /// </summary>
        public ListDataSet ShipmentSamplesdDS { get; set; }

        /// <summary>
        /// Выбранная в таблице запись
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        public int FactoryId;

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel("[erp]shipment_control");
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

            if (Grid != null && Grid.Menu != null && Grid.Menu.Count > 0)
            {
                foreach (var manuItem in Grid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// Остановка вспомогательных процессов при закрытии интерфейса
        /// </summary>
        public void Destroy()
        {
            Messenger.Default.Unregister<ItemMessage>(this);
            Central.Msg.UnRegister(ProcessMessages);

            Grid.Destruct();
        }

        /// <summary>
        /// Инициализация таблицы Управление отгрузкой образцов
        /// </summary>
        public void GridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                // Номер строки результата запроса. Колонка нужна для первичной сортировки
                new DataGridHelperColumn()
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=40,
                    Exportable=false,
                },
                new DataGridHelperColumn()
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=55,
                },
                new DataGridHelperColumn()
                {
                    Header="Дата изготовления",
                    Path="DT_COMPLITED",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    MinWidth=63,
                    MaxWidth=63,
                    Doc="Дата изготовления образца",
                },
                new DataGridHelperColumn()
                {
                    Header="Тип изготовления",
                    Path="PRODUCTION_TYPE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=55,
                    MaxWidth=65,
                    Doc="Где произведен образец: на плоттере или на линии",
                },
                new DataGridHelperColumn()
                {
                    Header="Покупатель",
                    Path="NAME_POK",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=100,
                    MaxWidth=165,
                },
                new DataGridHelperColumn()
                {
                    Header="Образец",
                    Path="SAMPLE_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=100,
                    MaxWidth=300,
                },
                new DataGridHelperColumn()
                {
                    Header="Количество",
                    Path="QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=40,
                },
                new DataGridHelperColumn()
                {
                    Header="Статус",
                    Path="STATUS_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=60,
                },
                new DataGridHelperColumn()
                {
                    Header="Номер",
                    Path="NUM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=80,
                    MaxWidth=110,
                    Doc="Номер образца, присвоенный клиентом",
                },
                new DataGridHelperColumn()
                {
                    Header="Дата",
                    Group="Отгрузка",
                    Path="DT_SHIPMENT",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    MinWidth=63,
                    MaxWidth=63,
                },
                new DataGridHelperColumn()
                {
                    Header="Покупатель",
                    Group="Отгрузка",
                    Path="NAME_POK_SHIP",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=176,
                },
                new DataGridHelperColumn()
                {
                    Header="Водитель",
                    Group="Отгрузка",
                    Path="DRIVER_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=210,
                },
                new DataGridHelperColumn()
                {
                    Header="Менеджер",
                    Path="EMPLOYEE_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=115,
                },
                new DataGridHelperColumn()
                {
                    Header="Номер ячейки",
                    Path="CELL_NUM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=40,
                },
                new DataGridHelperColumn()
                {
                    Header="Кладовщик",
                    Path="STOREKEEPER_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=115,
                },
                new DataGridHelperColumn()
                {
                    Header="Примечание",
                    Path="STOREKEEPER_NOTE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=350,
                    Doc="Примечание кладовщика",
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
                    Header="Тип доставки",
                    Path="DELIVERY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true
                }
            };

            Grid.SetColumns(columns);
            // Раскраска строк
            Grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                {
                    StylerTypeRef.ForegroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";


                        // Изготовлен или передан
                        if (row["STATUS"].ToInt() == 3 || row["STATUS"].ToInt() == 7)
                        {
                            color=HColor.GreenFG;
                        }
                        // Получен
                        else if (row["STATUS"].ToInt() == 4)
                        {
                            color=HColor.BlueFG;
                        }

                        // 
                        if ((row["TRANSPORT_STATUS"].ToInt() > 0) && (row["STATUS"].ToInt() == 3 || row["STATUS"].ToInt() == 7 || row["STATUS"].ToInt() == 4))
                        {
                            color=HColor.MagentaFG;
                        }

                        if (row["STATUS"].ToInt() == 6)
                        {
                             color=HColor.RedFG;
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
            Grid.SetSorting("_ROWNNMBER", ListSortDirection.Ascending);

            Grid.SearchText = SearchText;
            Grid.ItemsAutoUpdate=false;
            Grid.Init();

            // контекстное меню
            Grid.Menu = new Dictionary<string, DataGridContextMenuItem>()
            {
                {
                    "ChangeStatus",
                    new DataGridContextMenuItem()
                    {
                        Header="Отметить статус",
                        Tag = "access_mode_full_access",
                        Action=() =>
                        {
                        },
                        Items=new Dictionary<string, DataGridContextMenuItem>()
                        {
                            {
                                "StatusReceived",
                                new DataGridContextMenuItem()
                                {
                                    Header ="Получен",
                                    Action=() =>
                                    {
                                        UpdateStatus(4);
                                    }
                                }
                            },
                            {
                                "StatusTransferred",
                                new DataGridContextMenuItem()
                                {
                                    Header ="Передан",
                                    Action=() =>
                                    {
                                        UpdateStatus(7);
                                    }
                                }
                            },
                            {
                                "StatusShipped",
                                new DataGridContextMenuItem()
                                {
                                    Header ="Отгружен",
                                    Action=() =>
                                    {
                                        UpdateStatus(5);
                                    }
                                }
                            },
                        }
                    }
                },
                {
                    "ChangeCellNum",
                    new DataGridContextMenuItem()
                    {
                        Header="Изменить номер ячейки",
                        Tag = "access_mode_full_access",
                        Action=() =>
                        {
                            ChangeCellNum();
                        }
                    }
                },
                {
                    "DeattachShipment",
                    new DataGridContextMenuItem()
                    {
                        Header="Отвязать от отгрузки",
                        Tag = "access_mode_full_access",
                        Action=() =>
                        {
                            DeattachShipment();
                        }
                    }
                },
                {
                    "AddComment",
                    new DataGridContextMenuItem()
                    {
                        Header="Добавить комментарий",
                        Tag = "access_mode_full_access",
                        Action=() =>
                        {
                            AddStorekeeperNote();
                        }
                    }
                },
            };

            //данные грида
            Grid.OnLoadItems = LoadItems;
            Grid.OnFilterItems = FilterItems;
            Grid.Run();

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdateActions(selectedItem);
                }
            };

            //фокус ввода           
            Grid.Focus();
        }

        /// <summary>
        /// Установка начальных значений и параметров
        /// </summary>
        public void SetDefaults()
        {
            // Фильтр статуса отгрузки
            var statusSelectList = new Dictionary<string, string>();
            statusSelectList.Add("-1", "Все");
            statusSelectList.Add("3", "Изготовлен");
            statusSelectList.Add("7", "Передан");
            statusSelectList.Add("4", "Получен");
            statusSelectList.Add("5", "Отгружен");
            statusSelectList.Add("6", "Утилизирован");
            StatusComboBox.Items = statusSelectList;
            StatusComboBox.SelectedItem = statusSelectList.FirstOrDefault((x) => x.Key == "-1");

        }

        /// <summary>
        /// Загрузка данных из БД
        /// </summary>
        public async void LoadItems()
        {
            Grid.ShowSplash();
            GridToolbar.IsEnabled = false;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "ListShipment");
            q.Request.SetParam("FACTORY_ID", FactoryId.ToString());

            var allSamples = (bool)AllSamplesCheckBox.IsChecked;
            // Состояние чекбокса преобразуем в строку и передаём как параметр запроса
            q.Request.SetParam("AllRec", allSamples ? "1" : "0");

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            }
            );

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                if(result != null)
                {
                    if(result.Count > 0)
                    {
                        if(result.ContainsKey("ShipmentSamples"))
                        {
                            ShipmentSamplesdDS = result["ShipmentSamples"];
                            ShipmentSamplesdDS?.Init();
                            Grid.UpdateItems(ShipmentSamplesdDS);
                        }
                    }
                }
            }

            Grid.HideSplash();
            GridToolbar.IsEnabled = true;
        }

        /// <summary>
        /// Фильтрация записей
        /// </summary>
        public  void FilterItems()
        {
            if (Grid.GridItems != null && Grid.GridItems.Count > 0)
            {
                bool doFilteringByStatus = false;
                int selStatus = -1;
                if (StatusComboBox.SelectedItem.Key != null)
                {
                    selStatus = StatusComboBox.SelectedItem.Key.ToInt();
                    if (selStatus > 0)
                    {
                        doFilteringByStatus = true;
                    }
                }

                bool doFiltegingByTransport = (bool)ToShipCheckBox.IsChecked;

                var items = new List<Dictionary<string, string>>();
                foreach (var row in Grid.GridItems)
                {
                    var rowStatus = row["STATUS"].ToInt();

                    bool includeRow = true;
                    // Если отмечен чекбокс На отгрузку, показываем строки на отгрузку, не учитывая фильтр статуса
                    if (doFiltegingByTransport)
                    {
                        includeRow = (row["TRANSPORT_STATUS"].ToInt() > 0) && rowStatus.ContainsIn(3, 4, 7);
                    }
                    else if (doFilteringByStatus)
                    {
                        includeRow = false;
                        if (rowStatus == selStatus)
                        {
                            includeRow = true;
                        }
                    }

                    if (includeRow)
                    {
                        items.Add(row);
                    }


                }
                Grid.GridItems = items;
            }
        }

        /// <summary>
        /// обновление методов работы с выбранной записью
        /// </summary>
        /// <param name="selectedItem"></param>
        public void UpdateActions(Dictionary<string, string> selectedItem)
        {
            SelectedItem = selectedItem;

            Grid.Menu["ChangeStatus"].Items["StatusReceived"].Enabled = false;
            Grid.Menu["ChangeStatus"].Items["StatusShipped"].Enabled = false;
            Grid.Menu["ChangeStatus"].Items["StatusTransferred"].Enabled = false;
            Grid.Menu["DeattachShipment"].Enabled = false;
            Grid.Menu["AddComment"].Enabled = false;

            if (SelectedItem != null)
            {
                int currentStatus = SelectedItem["STATUS"].ToInt();

                // настройка доступности пунктов контекстного меню
                Grid.Menu["ChangeStatus"].Items["StatusReceived"].Enabled = (currentStatus == 3 || currentStatus == 5 || currentStatus == 7);
                Grid.Menu["ChangeStatus"].Items["StatusShipped"].Enabled = (currentStatus == 4);
                Grid.Menu["ChangeStatus"].Items["StatusTransferred"].Enabled = (currentStatus == 4);

                //отвязать можно только позиции с отгрузкой
                if (!string.IsNullOrEmpty(SelectedItem.CheckGet("NAME_POK_SHIP")) && currentStatus != 6)
                {
                    Grid.Menu["DeattachShipment"].Enabled = true;
                }

                // У отгруженных позиций нельзя менять ячейку
                Grid.Menu["ChangeCellNum"].Enabled = (currentStatus != 5 && currentStatus != 6);
                CellNumButton.IsEnabled = (currentStatus != 5 && currentStatus != 6);

                if (currentStatus != 6)
                {
                    Grid.Menu["AddComment"].Enabled = true;
                }

                //в отладочном режиме разрешено все
                if(Central.DebugMode)
                {
                    Grid.Menu["ChangeStatus"].Items["StatusReceived"].Enabled = true;
                    Grid.Menu["ChangeStatus"].Items["StatusShipped"].Enabled = true;
                    Grid.Menu["ChangeStatus"].Items["StatusTransferred"].Enabled = true;
                    Grid.Menu["DeattachShipment"].Enabled = true;
                    Grid.Menu["ChangeCellNum"].Enabled = true;
                    Grid.Menu["AddComment"].Enabled = true;
                }
            }

            ProcessPermissions();
        }

        /// <summary>
        /// обработка сообщений
        /// </summary>
        /// <param name="message"></param>
        public void ProcessMessages(ItemMessage message)
        {
            if(message!=null)
            {
                if(
                    message.SenderName == "WindowManager"
                    && message.ReceiverName == "ShipmentsControl_Equipment_Samples"
                )
                {
                    switch (message.Action)
                    {
                        case "FocusGot":
                            Grid.ItemsAutoUpdate=true;
                            break;

                        case "FocusLost":
                            Grid.ItemsAutoUpdate=false;
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m">сообщение</param>
        private void _ProcessMessages(ItemMessage m)
        {
            //Group ShipmentControl
            if (m.ReceiverGroup.IndexOf("ShipmentControl") > -1)
            {
                if (m.ReceiverName.IndexOf("ShipmentSamples") > -1)
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            Grid.LoadItems();
                            break;
                    }

                }
            }
        }

        /// <summary>
        /// Обработчики нажатий клавиш
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
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
        /// Вызов справки
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/shipments/control/equipments/shipmentsamples");
        }


        public void ProcessNavigation()
        {            
            var filteringMode=Central.Navigator.Address.GetLastBit();
            if(!filteringMode.IsNullOrEmpty())
            {
                switch(filteringMode)
                {
                    case "for_loading":
                        ToShipCheckBox.IsChecked=true;
                        break;
                }
            }
            Grid.LoadItems();
        }

        /// <summary>
        /// обновление статуса отгрузки
        /// </summary>
        /// <param name="newStatus">Значение нового статуса</param>
        private async void UpdateStatus(int newStatus)
        {
            if (SelectedItem != null)
            {
                var sampleId = SelectedItem.CheckGet("ID").ToInt();
                if (sampleId != 0)
                {
                    int delivery = SelectedItem.CheckGet("DELIVERY").ToInt();
                    if (newStatus == 5 && delivery == 5)
                    {
                        //Передаем образец в Каширу для отгрузки клиенту там
                        //Если статус Отгружен, то отправляем фиктивный статус: у образца будет статус Передан с фамилией отгрузившего кладовщика
                        newStatus = 20;
                    }

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Preproduction");
                    q.Request.SetParam("Object", "Samples");
                    q.Request.SetParam("Action", "UpdateStatus");

                    q.Request.SetParam("SAMPLE_ID", sampleId.ToString());
                    q.Request.SetParam("STATUS", newStatus.ToString());

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    }
                    );

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                        if(result != null)
                        {
                            if(result.Count > 0)
                            {
                                // пришел непустой ответ, обновляем грид
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
        /// Вызов окна редактирования примечания кладовщика
        /// </summary>
        private void AddStorekeeperNote()
        {
            if (SelectedItem != null)
            {
                var StorekeeperNote = new ShipmentNote();
                StorekeeperNote.ObjectId = SelectedItem["ID"].ToInt();
                StorekeeperNote.Object = "Samples";
                StorekeeperNote.Edit();
            }
        }

        /// <summary>
        /// Отвязать от отгрузки        
        /// </summary>
        private async void DeattachShipment()
        {
            var resume=true;

            var itemId=0;
            if(resume)
            {
                if(SelectedItem!=null)
                {
                    itemId=SelectedItem.CheckGet("ID").ToInt();
                    if(itemId==0)
                    {
                        resume=false;
                    }
                }
                else
                {
                    resume=false;
                }
            }

            if (resume)
            {
                var msg = "";
                msg = $"{msg}Отвязать образец №{itemId} от отгрузки?\n";

                var d = new DialogWindow($"{msg}", "Отвязка от отгрузки", "", DialogWindowButtons.NoYes);
                if (d.ShowDialog() != true)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "Samples");
                q.Request.SetParam("Action", "UpdateTS");

                q.Request.SetParam("IdSmpl", itemId.ToString());
                q.Request.SetParam("IdTs", "0");

                await Task.Run(() =>
                {
                    q.DoQuery();
                }
                );

                if (q.Answer.Status==0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        // если ответ не пустой, обновим таблицу
                        Messenger.Default.Send(new ItemMessage()
                        {
                            ReceiverGroup = "ShipmentControl",
                            ReceiverName = "ShipmentSamples",
                            SenderName = "ShipmentSamplesView",
                            Action = "Refresh",
                        });
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// Изменение номера ячейки хранения образца
        /// </summary>
        private void ChangeCellNum()
        {
            if (SelectedItem != null)
            {
                var itemId = SelectedItem.CheckGet("ID").ToInt();
                if (itemId > 0)
                {
                    var cellNumWindow = new ShipmentSampleCellNum();
                    cellNumWindow.Edit(itemId);
                }
            }
        }

        /// <summary>
        /// Экспорт в Excel отмеченных записей или всей таблицы
        /// </summary>
        private async void ExportToExcel()
        {
            var list = Grid.GetSelectedItems("CHECKING");

            if (list.Count == 0)
            {
                list = Grid.Items;
            }

            var eg = new ExcelGrid();
            eg.SetColumnsFromGrid(Grid.Columns);
            eg.Items = list;
            await Task.Run(() =>
            {
                eg.Make();
            });
        }

        /// <summary>
        /// Обработчик нажатия на чекбокс
        /// </summary>
        private void AllSamplesCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        /// <summary>
        /// Обработчик выбора значения статуса в комбобоксе 
        /// </summary>
        private void StatusComboBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку обновления
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshButton_Click(object sender,RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку справки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void CellNumButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeCellNum();
        }

        /// <summary>
        /// Обработчик чекбокса На отгрузку
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToShipCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void ToExcelButton_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcel();
        }
    }
}
