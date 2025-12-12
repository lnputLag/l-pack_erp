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
    /// Управление отгрузками штанцформ
    /// </summary>
    /// <author>Рясной П.В.</author>
    public partial class ShipmentShtanz : UserControl
    {
        /// <summary>
        /// Инициализация элемента интерфейса Управление отгрузками штанцформ
        /// </summary>
        public ShipmentShtanz()
        {
            InitializeComponent();

            FactoryId = 1;

            GridInit();
            ItemsGridInit();
            ProcessPermissions();

            Messenger.Default.Register<ItemMessage>(this, _ProcessMessages);
            Central.Msg.Register(ProcessMessages);
        }

        /// <summary>
        /// Данные из БД для таблицы Управление отгрузками штанцформ
        /// </summary>
        public ListDataSet ShipmentShtanzDS { get; set; }

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

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
        /// Остановка вспомогательных процессов при закрытии интерфейса
        /// </summary>
        public void Destroy()
        {
            Messenger.Default.Unregister<ItemMessage>(this);
            Central.Msg.UnRegister(ProcessMessages);

            Grid.Destruct();
        }

        /// <summary>
        /// Инициализация таблицы Управление отгрузками штанцформ
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
                    Header="Статус",
                    Path="STATUS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=130,
                    MaxWidth=150,
                },
                new DataGridHelperColumn()
                {
                    Header="Дата смены статуса",
                    Path="STATUS_DTTM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    MinWidth=90,
                    MaxWidth=125,
                    Format="dd.MM.yyyy HH:mm"
                },
                new DataGridHelperColumn()
                {
                    Header="Плательщик",
                    Path="CUSTOMER_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=150,
                    MaxWidth=300,
                },
                new DataGridHelperColumn()
                {
                    Header="Дата",
                    Group="Отгрузка",
                    Path="SHIPMENT_DTTM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Format="dd.MM.yyyy HH:mm",
                    MinWidth=63,
                    MaxWidth=115,
                },
                
                new DataGridHelperColumn()
                {
                    Header="Покупатель",
                    Group="Отгрузка",
                    Path="SHIPMENT_OWNER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=150,
                    MaxWidth=180,
                },
                new DataGridHelperColumn()
                {
                    Header="Водитель",
                    Group="Отгрузка",
                    Path="DRIVER_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=150,
                    MaxWidth=250,
                },
                new DataGridHelperColumn()
                {
                    Header="Ячейка",
                    Path="STORAGE_NUM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Doc="Место хранения штанцформы на складе",
                    MinWidth=50,
                    MaxWidth=80,
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
                    Path="NOTE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=100,
                    MaxWidth=900,
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
                    Path="STATUS_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true
                },
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
                        int statusId = row.CheckGet("STATUS_ID").ToInt();

                        if (statusId.ContainsIn(12, 18))
                        {
                            color=HColor.GreenFG;
                        }
                        else if (statusId.ContainsIn(13, 17))
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
                                "StatusPrepareTransfer",
                                new DataGridContextMenuItem()
                                {
                                    Header ="Получена",
                                    Action=() =>
                                    {
                                        UpdateStatus(13);
                                    }
                                }
                            },
                            
                            {
                                "StatusTransferred",
                                new DataGridContextMenuItem()
                                {
                                    Header ="Передана клиенту",
                                    Action=() =>
                                    {
                                        UpdateStatus(14);
                                    }
                                }
                            },
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
                    "ChangeStorageNum",
                    new DataGridContextMenuItem()
                    {
                        Header="Изменить ячейку",
                        Tag = "access_mode_full_access",
                        Action=() =>
                        {
                            ChangeStorageNum();
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
        /// Инициализация таблицы с содержимым пакета
        /// </summary>
        private void ItemsGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn()
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=80,
                },
                new DataGridHelperColumn()
                {
                    Header="Штанцформа",
                    Path="NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=150,
                    MaxWidth=350,
                    Doc="Имя элемента штанцформы на отгрузку",
                },
                new DataGridHelperColumn()
                {
                    Header="Акт",
                    Path="ACT_FILE_FLAG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth=40,
                    MaxWidth=40,
                    Doc="Наличие акта приема-передачи штанцформы",
                },
                new DataGridHelperColumn()
                {
                    Header="Имя файла",
                    Path="ACT_FILE_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Hidden=true
                },
            };
            ItemsGrid.SetColumns(columns);
            ItemsGrid.SetSorting("_ROWNNMBER", ListSortDirection.Ascending);

            //при выборе строки в гриде, обновляются актуальные действия для записи
            ItemsGrid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    if (selectedItem.CheckGet("ACT_FILE_FLAG").ToInt() == 1)
                    {
                        ItemsGrid.Menu["ActFileShow"].Enabled = true;
                    }
                    else
                    {
                        ItemsGrid.Menu["ActFileShow"].Enabled = false;
                    }
                }
            };

            // контекстное меню
            ItemsGrid.Menu = new Dictionary<string, DataGridContextMenuItem>()
            {
                {
                    "ActFileShow",
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
            ItemsGrid.Init();
        }

        /// <summary>
        /// Загрузка данных из БД
        /// </summary>
        public async void LoadItems()
        {
            Grid.ShowSplash();
            GridToolbar.IsEnabled = false;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "RigTransfer");
            q.Request.SetParam("Action", "List");
            q.Request.SetParam("RIG_TYPE", "1");
            q.Request.SetParam("FACTORY_ID", FactoryId.ToString());

            // Состояние чекбокса преобразуем в строку и передаём как параметр запроса
            var allShtanz = (bool)AllShtanzCheckBox.IsChecked;
            q.Request.SetParam("ALL_RECORDS", allShtanz ? "1" : "0");

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                if(result != null)
                {
                    var ds = ListDataSet.Create(result, "TRANSFER");
                    Grid.UpdateItems(ds);
                }
            }

            Grid.HideSplash();
            GridToolbar.IsEnabled = true;
        }

        private async void LoadStampItems()
        {
            if (SelectedItem != null)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Rig");
                q.Request.SetParam("Object", "RigTransfer");
                q.Request.SetParam("Action", "ListStampItems");
                q.Request.SetParam("ID", SelectedItem.CheckGet("ID"));

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
                        var ds = ListDataSet.Create(result, "STAMP_LIST");
                        ItemsGrid.UpdateItems(ds);
                    }
                }

            }
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
                            if (((row.CheckGet("STATUS_ID").ToInt() == 7) || (row.CheckGet("STATUS_ID").ToInt() == 9))
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
        /// обновление методов работы с выбранной записью
        /// </summary>
        /// <param name="selectedItem"></param>
        public void UpdateActions(Dictionary<string, string> selectedItem)
        {
            SelectedItem = selectedItem;

            LoadStampItems();

            Grid.Menu["ChangeStatus"].Items["StatusPrepareTransfer"].Enabled = false;
            Grid.Menu["ChangeStatus"].Items["StatusTransferred"].Enabled=false;

            if (SelectedItem != null)
            {
                int currentStatus = SelectedItem["STATUS_ID"].ToInt();

                // настройка доступности пунктов контекстного меню
                Grid.Menu["ChangeStatus"].Items["StatusPrepareTransfer"].Enabled = currentStatus.ContainsIn(12, 14, 18);
                Grid.Menu["ChangeStatus"].Items["StatusTransferred"].Enabled = currentStatus.ContainsIn(13, 17);

                //в отладочном режиме разрешено все
                if(Central.DebugMode)
                {
                    Grid.Menu["ChangeStatus"].Items["StatusPrepareTransfer"].Enabled = true;
                    Grid.Menu["ChangeStatus"].Items["StatusTransferred"].Enabled = true;
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
                    && message.ReceiverName == "ShipmentsControl_Equipment_Shtanzforms"
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
                else if (message.ReceiverName == "ShipmentsControl_Equipment_Shtanzforms")
                {
                    switch (message.Action)
                    {
                        case "Refresh":
                            Grid.LoadItems();
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
                if (m.ReceiverName.IndexOf("ShipmentShtanzForms") > -1)
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
            Central.ShowHelp("/doc/l-pack-erp/shipments/control/equipments/shipmentshtanz");
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
        /// обновление статуса отгрузки
        /// </summary>
        /// <param name="newStatus">Значение нового статуса</param>
        private async void UpdateStatus(int newStatus)
        {
            if (SelectedItem != null)
            {
                //Для передачи на другую площадку другие статусы
                bool transferToOtherFactory = SelectedItem.CheckGet("STATUS_ID").ToInt().ContainsIn(16, 17, 18);
                if (newStatus == 13 && transferToOtherFactory)
                {
                    newStatus = 17;
                }
                else if (newStatus == 14 && transferToOtherFactory)
                {
                    newStatus = 16;
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Rig");
                q.Request.SetParam("Object", "RigTransfer");
                q.Request.SetParam("Action", "SetStatus");
                q.Request.SetParam("ID", SelectedItem.CheckGet("ID"));
                q.Request.SetParam("STATUS", newStatus.ToString());
                q.Request.SetParam("RIG_TYPE", "1");

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    Grid.LoadItems();
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
                StorekeeperNote.Object = "ShtanzForms";
                StorekeeperNote.Edit();
            }
        }

        /// <summary>
        /// Открытие файла акта приема-передачи
        /// </summary>
        private async void OpenActFile()
        {
            if (ItemsGrid.SelectedItem != null)
            {
                if (ItemsGrid.SelectedItem.ContainsKey("ACT_FILE_NAME"))
                {
                    var actFileName = ItemsGrid.SelectedItem["ACT_FILE_NAME"].ToString();
                    if (!string.IsNullOrEmpty(actFileName))
                    {
                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Rig");
                        q.Request.SetParam("Object", "CuttingStamp");
                        q.Request.SetParam("Action", "GetActFile");
                        q.Request.SetParam("ACT_FILE_NAME", actFileName);

                        await Task.Run(() =>
                        {
                            q.DoQuery();
                        });

                        if (q.Answer.Status == 0)
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
        /// Отрытие окна редактирования ячейки хранения штанцформы
        /// </summary>
        private void ChangeStorageNum()
        {
            if (SelectedItem != null)
            {
                var storageCellWindow = new ShipmentRigStorageCell();
                storageCellWindow.ReceiverName = "ShipmentsControl_Equipment_Shtanzforms";
                storageCellWindow.Edit(SelectedItem);
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
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshButton_Click(object sender,RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        /// <summary>
        /// Обработчик смены состояния чекбокса
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AllShtanzCheckBox_Click(object sender, RoutedEventArgs e)
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
