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

namespace Client.Interfaces.Shipments
{
    //FIXME: refactor me 
    //      check: http://192.168.3.237/developer/std/cheklist-21-11


    /// <summary>
    /// Управление отгрузкой клише
    /// </summary>
    /// <author>Рясной П.В.</author>
    public partial class ShipmentCliche:UserControl
    {
        /// <summary>
        /// инициализация элемента интерфейса Управление отгрузкой клише
        /// </summary>
        public ShipmentCliche()
        {
            InitializeComponent();
            FactoryId = 1;

            GridInit();
            ProcessPermissions();

            Messenger.Default.Register<ItemMessage>(this,_ProcessMessages);
            Central.Msg.Register(ProcessMessages);
        }

        /// <summary>
        /// данные для таблицы Управление отгрузкой клише
        /// </summary>
        public ListDataSet ShipmentClicheDS { get; set; }

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        public Dictionary<string,string> SelectedItem { get; set; }

        /// <summary>
        /// Идентификатор площадки отгрузки: 1 - Липецк, 2 - Кашира
        /// </summary>
        public int FactoryId { get; set; }

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
        /// Остановка вспомогательных процессов при закрытии интерфейса.
        /// </summary>
        public void Destroy()
        {
            Messenger.Default.Unregister<ItemMessage>(this);
            Central.Msg.UnRegister(ProcessMessages);

            Grid.Destruct();
        }

        /// <summary>
        /// инициализация таблицы Управление отгрузкой клише
        /// </summary>
        public void GridInit()
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
                    Doc="Станок, номер ячейки и место, где хранится клише",
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
                    Header="Дата смены статуса",
                    Path="STATUS_DTTM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    MinWidth=100,
                    MaxWidth=125,
                    Format="dd.MM.yyyy HH:mm"
                },
                new DataGridHelperColumn()
                {
                    Header="Артикулы",
                    Path="ARTIKUL",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=80,
                    MaxWidth=150,
                },
                new DataGridHelperColumn()
                {
                    Header="Код статуса",
                    Path="STATUS_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn()
                {
                    Header="Акт",
                    Path="ACT_FILE_NAME_FLAG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth=40,
                    MaxWidth=40,
                    Doc="Наличие файла акта приема-передачи",
                },
                new DataGridHelperColumn()
                {
                    Header="Плательщик",
                    Path="POK_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=150,
                    MaxWidth=300,
                },
                new DataGridHelperColumn()
                {
                    Header="Дата",
                    Group="Отгрузка",
                    Path="DT_OTGR",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    MinWidth=100,
                    MaxWidth=100,
                    Format="dd.MM.yyyy HH:mm"
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
                    MaxWidth=300,
                },
                new DataGridHelperColumn()
                {
                    Header="Кладовщик",
                    Path="STOREKEEPER_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=100,
                    MaxWidth=250,
                },
                new DataGridHelperColumn()
                {
                    Header="Примечание",
                    Path="STOREKEEPER_NOTE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=100,
                    MaxWidth=300,
                    Doc="Примечание кладовщика",
                },
                new DataGridHelperColumn()
                {
                    Header=" ",
                    Path="_",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=10,
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
                        int currentStatus = row.CheckGet("STATUS_ID").ToInt();

                        // status = 2 - готово к передаче
                        if (currentStatus.ContainsIn(2, 18))
                        {
                            color=HColor.GreenFG;
                        }

                        // status = 6 - получено на СГП
                        if (currentStatus.ContainsIn(6, 17))
                        {
                            // transport_status = 2 - машина для отгрузки на терминале
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
            Grid.ItemsAutoUpdate=false;
            Grid.Init();

            // контекстное меню
            Grid.Menu = new Dictionary<string,DataGridContextMenuItem>()
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
                                "StatusPrepareTransfer",
                                new DataGridContextMenuItem()
                                {
                                    Header ="Получено",
                                    Action=() =>
                                    {
                                        UpdateStatus(6);
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
            Grid.OnFilterItems = FilterItems;
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
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Cliche");
            //FIXME: rename action: ShipmentList -> List*
            q.Request.SetParam("Action", "ShipmentList");
            q.Request.SetParam("FACTORY_ID", FactoryId.ToString());

            // Состояние чекбокса преобразуем в строку и передаём как параметр запроса
            var allCliche = (bool)AllClicheCheckBox.IsChecked;
            q.Request.SetParam("AllRec", allCliche ? "1" : "0");

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
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

            GridToolbar.IsEnabled = true;
            Grid.HideSplash();
        }

        /// <summary>
        /// Фильтрация строк грида
        /// </summary>
        public void FilterItems()
        {
            if (Grid.GridItems != null)
            {
                if (Grid.GridItems.Count > 0)
                {
                    var items = new List<Dictionary<string, string>>();
                    foreach (var row in Grid.GridItems)
                    {
                        if ((bool)ToShipCheckBox.IsChecked)
                        {
                            if (((row.CheckGet("STATUS_ID").ToInt() == 2) || (row.CheckGet("STATUS_ID").ToInt() == 6))
                                && (row.CheckGet("TRANSPORT_STATUS").ToInt() == 2))
                            {
                                items.Add(row);
                            }
                        }
                        else
                        {
                            items.Add(row);
                        }
                    }

                    Grid.GridItems = items;
                }
            }
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
            Grid.Menu["DeattachShipment"].Enabled = false;

            if(SelectedItem != null)
            {
                int currentStatus = SelectedItem["STATUS_ID"].ToInt();

                // настройка доступности пунктов контекстного меню
                Grid.Menu["ChangeStatus"].Items["StatusPrepareTransfer"].Enabled = (currentStatus == 3) || (currentStatus == 2);
                Grid.Menu["ChangeStatus"].Items["StatusTransferred"].Enabled = (currentStatus == 6);
                Grid.Menu["ActFileNameShow"].Enabled = SelectedItem["ACT_FILE_NAME_FLAG"].ToBool();

                //отвязать можно только позиции с отгрузкой
                //и статусом 2 (Готово к передаче)
                if(
                    !string.IsNullOrEmpty(SelectedItem.CheckGet("POK_OTGR"))
                    && SelectedItem.CheckGet("STATUS_ID").ToInt()==2
                )
                {
                    Grid.Menu["DeattachShipment"].Enabled = true;
                }
                
                //в отладочном режиме разрешено все
                if(Central.DebugMode)
                {
                    Grid.Menu["ChangeStatus"].Items["StatusPrepareTransfer"].Enabled = true;
                    Grid.Menu["ChangeStatus"].Items["StatusTransferred"].Enabled = true;
                    Grid.Menu["DeattachShipment"].Enabled = true;
                }
            }

            ProcessPermissions();
        }

        /// <summary>
        /// обновление статуса клише при отгрузке
        /// </summary>
        /// <param name="newStatus">Значение нового статуса.</param>
        private async void UpdateStatus(int newStatus)
        {
            if(SelectedItem != null)
            {
                var clic_id = SelectedItem["ID"].ToInt();
                //Для передачи на другую площадку другие статусы
                bool transferToOtherFactory = Grid.SelectedItem.CheckGet("STATUS_ID").ToInt().ContainsIn(16, 17, 18);

                if (clic_id != 0)
                {
                    if (newStatus == 6 && transferToOtherFactory)
                    {
                        newStatus = 17;
                    }
                    else if (newStatus == 3 && transferToOtherFactory)
                    {
                        newStatus = 16;
                    }

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Preproduction");
                    q.Request.SetParam("Object", "Cliche");
                    q.Request.SetParam("Action", "UpdateStatus");
                    q.Request.SetParam("IdClic", clic_id.ToString());
                    q.Request.SetParam("Status", newStatus.ToString());

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    }
                    );

                    if(q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                        if(result != null)
                        {
                            if(result.ContainsKey("ITEMS"))
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
        /// Вызов окна редактирования примечания кладовщика
        /// </summary>
        private void AddStorekeeperNote()
        {
            if (SelectedItem != null)
            {
                var StorekeeperNote = new ShipmentNote();
                StorekeeperNote.ObjectId = SelectedItem["ID"].ToInt();
                StorekeeperNote.Object = "Cliche";
                StorekeeperNote.Edit();
            }
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
                    && message.ReceiverName == "ShipmentsControl_Equipment_Cliche"
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
        /// обработчик сообщений.
        /// </summary>
        /// <param name="m">сообщение.</param>
        private void _ProcessMessages(ItemMessage m)
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
        /// обработка ввода с клавиатуры (роли)
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
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
            Central.ShowHelp("/doc/l-pack-erp/shipments/control/equipments/shipmentcliche");
        }

        /// <summary>
        /// обработчик системы навигации по URL.
        /// </summary>
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
        /// Обработчик загрузки данных
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="RoutedEventArgs"/>.</param>
        private void OnLoad(object sender,RoutedEventArgs e)
        {
            Grid.UpdateGrid();
        }

        /// <summary>
        /// Открытие акта приема-передачи
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
                        q.Request.SetParam("Module", "Preproduction");
                        q.Request.SetParam("Object", "Cliche");
                        q.Request.SetParam("Action", "GetActFile");
                        q.Request.SetParam("actFileName", actFileName);

                        await Task.Run(() =>
                        {
                            q.DoQuery();
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
                msg = $"{msg}Отвязать клише №{itemId} от отгрузки?\n";

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
                q.Request.SetParam("Object", "Cliche");
                q.Request.SetParam("Action", "UpdateTS");
                q.Request.SetParam("IdClic", itemId.ToString());
                q.Request.SetParam("IdTs", "0");

                await Task.Run(() =>
                {
                    q.DoQuery();
                }
                );

                if (q.Answer.Status==0)
                {
                    Grid.LoadItems();
                }
                else
                {
                    q.ProcessError();
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
                list = Grid.GridItems;
            }

            var eg = new ExcelGrid();
            eg.SetColumnsFromGrid(Grid.Columns);
            // уберем колонку с номерами строк
            eg.Columns.RemoveAt(0);
            eg.Items = list;
            await Task.Run(() =>
            {
                eg.Make();
            });
        }

        /// <summary>
        /// Обработчик нажатия на кнопку обновления
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="RoutedEventArgs"/>.</param>
        private void RefreshButton_Click(object sender,RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        /// <summary>
        /// Обработчик изменения состояния чекбокса 
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="RoutedEventArgs"/>.</param>
        private void AllClicheCheckBox_Click(object sender,RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку справки
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="RoutedEventArgs"/>.</param>
        private void HelpButton_Click(object sender,RoutedEventArgs e)
        {
            ShowHelp();
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
