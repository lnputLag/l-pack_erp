using Client.Common;
using Client.Interfaces.Main;
using DevExpress.Xpf.Core;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Список клише, которые подготовлены к передаче клиенту, и которые надо привязать к отгрузке
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class RigClicheTransferList : UserControl
    {
        public RigClicheTransferList()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);
            Central.Msg.Register(ProcessNewMessages);

            ProcessPermition();
            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/osn_management/cliche/to_transfer";
            LoadRef();
            InitClicheGrid();
            InitTechMapGrid();
        }

        /// <summary>
        /// Признак фильтрации по менеджеру: false - по списку из сессии, true - по выбранному в выпадающем списке
        /// </summary>
        private bool SingleManager;

        /// <summary>
        /// Уровень доступа пользователя к функциям интерфейса
        /// </summary>
        Role.AccessMode RoleLevel { get; set; }

        #region Common

        /// <summary>
        /// Имя вкладки
        /// </summary>
        public string TabName;
        /// <summary>
        /// Ссылка на страницу документации
        /// </summary>
        private string DocumentationUrl;

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// Деструктор. Остановка вспомогательных процессов при закрытии вкладки
        /// </summary>
        public void Destroy()
        {
            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры гридов
            ClicheGrid.Destruct();
            TechMapGrid.Destruct();
        }

        /// <summary>
        /// Вызов справки
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp(DocumentationUrl);
        }

        public void ProcessNavigation()
        {

        }

        #endregion

        /// <summary>
        /// Обработка сообщений в новой шине
        /// </summary>
        /// <param name="obj"></param>
        private void ProcessNewMessages(ItemMessage obj)
        {
            // Управление автообновлением
            if (obj.SenderName == "WindowManager" && obj.ReceiverName == TabName)
            {
                switch (obj.Action)
                {
                    case "FocusGot":
                        ClicheGrid.ItemsAutoUpdate = true;
                        // Проверим состояние фильтра менеджеров. Если выбран 1, покажем селектбокс, если несколько, покажем текст
                        var ids = Central.SessionValues["ManagersConfig"]["ListActive"];
                        if (!string.IsNullOrEmpty(ids))
                        {
                            var arr = ids.Split(',');
                            int qty = arr.Length;
                            if (qty == 1)
                            {
                                ManagerName.Visibility = Visibility.Visible;
                                ManagerName.SetSelectedItemByKey(arr[0]);
                                SelectedManagerCount.Visibility = Visibility.Collapsed;
                            }
                            else
                            {
                                ManagerName.Visibility = Visibility.Collapsed;
                                SelectedManagerCount.Visibility = Visibility.Visible;
                                SelectedManagerCount.Text = $"Выбрано {qty}";
                            }
                        }
                        else
                        {
                            ManagerName.Visibility = Visibility.Visible;
                            ManagerName.SetSelectedItemByKey("-1");
                            SelectedManagerCount.Visibility = Visibility.Collapsed;
                        }

                        ClicheGrid.UpdateItems();
                        break;

                    case "FocusLost":
                        ClicheGrid.ItemsAutoUpdate = false;
                        break;
                }
            }
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="obj">сообщение</param>
        private void ProcessMessages(ItemMessage obj)
        {
            if (obj.ReceiverGroup.IndexOf("PreproductionSample") > -1)
            {
                if (obj.ReceiverName.IndexOf(TabName) > -1)
                {
                    switch (obj.Action)
                    {
                        case "Refresh":
                            ClicheGrid.LoadItems();
                            break;
                    }
                }
            }
            if (obj.ReceiverGroup.IndexOf("PreproductionCliche") > -1)
            {
                if (obj.ReceiverName.IndexOf(TabName) > -1)
                {
                    switch (obj.Action)
                    {
                        case "Refresh":
                            ClicheGrid.LoadItems();
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Обработка прав доступа пользователя
        /// </summary>
        private void ProcessPermition()
        {
            RoleLevel = Central.Navigator.GetRoleLevel("[erp]rig_management");

            if (RoleLevel == Role.AccessMode.ReadOnly)
            {
                BindShipmentButton.IsEnabled = false;
                UnbindShipmentButton.IsEnabled = false;
            }
        }

        /// <summary>
        /// Загрузка общей информации
        /// </summary>
        public async void LoadRef()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Cliche");
            q.Request.SetParam("Action", "ListRef");

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    // менеджеры по работе с клиентами
                    var managersDS = ListDataSet.Create(result, "MANAGERS");
                    var list = new Dictionary<string, string>()
                    {
                        { "-1", "Все" },
                    };

                    var managers = new Dictionary<string, string>();
                    foreach (var item in managersDS.Items)
                    {
                        list.CheckAdd(item["ID"].ToInt().ToString(), item["FIO"]);
                    }

                    ManagerName.Items = list;
                    // Если активный пользователь есть в списке, установим его в выбранном значении
                    string emplId = Central.User.EmployeeId.ToString();
                    if (list.ContainsKey(emplId))
                    {
                        ManagerName.SetSelectedItemByKey(emplId);
                    }
                    else
                    {
                        ManagerName.SetSelectedItemByKey("-1");
                    }
                }
            }
        }

        private void InitClicheGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header=" ",
                    Path="CHECKING",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    Editable=true,
                    MinWidth=40,
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header="№",
                    Path="_ROWNUMBER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=60,
                },
                new DataGridHelperColumn
                {
                    Header="Клише",
                    Path="CLICHE_NUM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="Плательщик",
                    Path="CUSTOMER_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=100,
                    MaxWidth=250,
                },
                new DataGridHelperColumn
                {
                    Header="Статус",
                    Path="STATUS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=100,
                    MaxWidth=140,
                },
                new DataGridHelperColumn
                {
                    Header="Дата статуса",
                    Path="STATUS_DT",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    MinWidth=100,
                    MaxWidth=100,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header="Площадка",
                    Path="FACTORY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=14,
                },
                new DataGridHelperColumn
                {
                    Header="Отгрузка",
                    Path="SHIPMENT_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=100,
                    MaxWidth=250,
                },
                new DataGridHelperColumn
                {
                    Header="Ширина упаковки, мм",
                    Path="CELL_LENGTH",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Exportable=false,
                    MinWidth=40,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание кладовщика",
                    Path="STOREKEEPER_NOTE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Exportable=false,
                    MinWidth=60,
                    MaxWidth=150,
                },
                new DataGridHelperColumn
                {
                    Header="Менеджер",
                    Path="MANAGER_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=100,
                    MaxWidth=250,
                },
                new DataGridHelperColumn
                {
                    Header="Артикулы изделий",
                    Path="ARTIKUL",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=150,
                },
                new DataGridHelperColumn
                {
                    Header=" ",
                    Path="_",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=5,
                    MaxWidth=1500,
                },
                new DataGridHelperColumn
                {
                    Header="Код статуса",
                    Path="STATUS_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Код плательщика",
                    Path="CUSTOMER_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Код менеджера",
                    Path="MANAGER_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ИД отгрузки",
                    Path="SHIPMENT_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ИД отгрузки",
                    Path="FACTORY_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            ClicheGrid.SetColumns(columns);
            ClicheGrid.SearchText = SearchText;
            ClicheGrid.Init();
            //данные грида
            ClicheGrid.OnLoadItems = LoadClicheItems;
            ClicheGrid.OnFilterItems = FilterClicheItems;
            ClicheGrid.Run();

            //при выборе строки в гриде, обновляются актуальные действия для записи
            ClicheGrid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdateActions(selectedItem);
                }
            };
        }

        /// <summary>
        /// Инициализация таблицы с техкартами
        /// </summary>
        private void InitTechMapGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Потребитель",
                    Path="CUSTOMER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=100,
                    MaxWidth=250,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул изделия",
                    Path="ARTIKUL",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=250,
                },
                new DataGridHelperColumn
                {
                    Header="Дата рассылки",
                    Path="NOTIFICATION_DT",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    MinWidth=60,
                    MaxWidth=100,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="ID_TK",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Путь к ТК",
                    Path="PATHTK",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Hidden=true,
                },
            };
            TechMapGrid.SetColumns(columns);
            TechMapGrid.Init();
        }

        /// <summary>
        /// Загрузка данных в таблицу клише
        /// </summary>
        private async void LoadClicheItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Cliche");
            q.Request.SetParam("Action", "ListTransfer");

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
                    var ds = ListDataSet.Create(result, "CLICHE");
                    ClicheGrid.UpdateItems(ds);
                }
            }
        }

        /// <summary>
        /// Фильтрация данных в таблице клише
        /// </summary>
        private void FilterClicheItems()
        {
            if (ClicheGrid.GridItems != null)
            {
                if (ClicheGrid.GridItems.Count > 0)
                {

                    bool doFilteringByManager = false;
                    var managerIds = new List<int>();
                    var ids = Central.SessionValues["ManagersConfig"]["ListActive"];
                    if (!string.IsNullOrEmpty(ids))
                    {
                        var arr = ids.Split(',');
                        if (arr.Length == 1)
                        {
                            if (ManagerName.SelectedItem.Key != null)
                            {
                                var managerId = ManagerName.SelectedItem.Key.ToInt();
                                if (managerId > 0)
                                {
                                    managerIds.Add(managerId);
                                    doFilteringByManager = true;
                                }
                            }
                        }
                        else
                        {
                            foreach (var item in arr)
                            {
                                managerIds.Add(item.ToInt());
                            }
                            doFilteringByManager = true;
                        }
                    }

                    if (doFilteringByManager)
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in ClicheGrid.GridItems)
                        {
                            bool includeByManager = true;

                            if (doFilteringByManager)
                            {
                                includeByManager = false;
                                if (row.CheckGet("MANAGER_ID").ToInt() == 0)
                                {
                                    includeByManager = true;
                                }
                                else
                                {
                                    var l = managerIds.ToArray();
                                    if (row.CheckGet("MANAGER_ID").ToInt().ContainsIn(l))
                                    {
                                        includeByManager = true;
                                    }
                                }
                            }

                            if (includeByManager)
                            {
                                items.Add(row);
                            }

                        }
                        ClicheGrid.GridItems = items;
                    }
                }
            }
        }

        /// <summary>
        /// Загрузка данных в таблицу тахкарт
        /// </summary>
        private async void LoadTechMapItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Cliche");
            q.Request.SetParam("Action", "ListTransferTechMap");
            q.Request.SetParam("ID", SelectedItem["ID"]);

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
                    var ds = ListDataSet.Create(result, "CLICHE_TRANSFER_MAPS");
                    TechMapGrid.UpdateItems(ds);
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
            if (SelectedItem.ContainsKey("ID"))
            {
                LoadTechMapItems();
            }
            else
            {
                TechMapGrid.ClearItems();
            }

            if (RoleLevel == Role.AccessMode.FullAccess || RoleLevel == Role.AccessMode.Special)
            {
                BindShipmentButton.IsEnabled = SelectedItem.CheckGet("SHIPMENT_ID").ToInt() == 0;
                UnbindShipmentButton.IsEnabled = SelectedItem.CheckGet("SHIPMENT_ID").ToInt() != 0;
            }
        }

        /// <summary>
        /// Открывает файл техкарты
        /// </summary>
        private void OpenTechMap()
        {
            bool success = true;
            var path = TechMapGrid.SelectedItem.CheckGet("PATHTK");
            if (!string.IsNullOrEmpty(path))
            {
                if (File.Exists(path))
                {
                    Central.OpenFile(path);
                }
                else
                {
                    success = false;
                }
            }
            else
            {
                success = false;
            }

            if (!success)
            {
                var dw = new DialogWindow("Файл техкарты не найден", "Файл не найден");
                dw.ShowDialog();
            }
        }

        /// <summary>
        /// Определение выбранных клише и привязка их к отгрузке
        /// </summary>
        private void BindToShipment()
        {
            if (ClicheGrid.Items.Count > 0)
            {
                int customerId = -1;
                int factoryId = 0;
                bool resume = true;
                int typeOrder = 0;

                var list = new List<int>();

                foreach (var row in ClicheGrid.Items)
                {
                    if (row.CheckGet("CHECKING").ToBool())
                    {
                        var id = row.CheckGet("ID").ToInt();
                        if (id > 0)
                        {
                            list.Add(id);
                            var currCustomerId = row.CheckGet("CUSTOMER_ID").ToInt();
                            if (customerId == -1)
                            {
                                customerId = currCustomerId;
                            }
                            else if ((customerId > 0) && (customerId != currCustomerId))
                            {
                                customerId = 0;
                            }

                            var currentStatus = row.CheckGet("STATUS_ID").ToInt();
                            var currentTypeOrder = 1;
                            if (row.CheckGet("STATUS_ID").ToInt().ContainsIn(17, 18))
                            {
                                currentTypeOrder = 2;
                            }
                            if (typeOrder == 0)
                            {
                                typeOrder = currentTypeOrder;
                            }
                            else if (typeOrder != currentTypeOrder)
                            {
                                resume = false;
                                var dw = new DialogWindow("Все выбранные клише должны отгружаться или клиентам, или на другую площадку", "Привязка к отгрузке");
                                dw.ShowDialog();
                                break;
                            }

                            //Все выбранные клише должны отгружаться с одной площадки
                            int currentFactoryId = row.CheckGet("FACTORY_ID").ToInt();
                            if (factoryId == 0)
                            {
                                factoryId = currentFactoryId;
                            }
                            else if (factoryId != currentFactoryId)
                            {
                                resume = false;
                                var dw = new DialogWindow("Все выбранные клише должны отгружаться с одной площадки", "Привязка к отгрузке");
                                dw.ShowDialog();
                                break;
                            }
                        }
                    }
                }

                if (resume)
                {
                    // Если не отмечено ни одной строки, привяжем выбранную строку
                    if (list.Count == 0)
                    {
                        var id = SelectedItem.CheckGet("ID").ToInt();
                        if (id > 0)
                        {
                            list.Add(id);
                            customerId = SelectedItem.CheckGet("CUSTOMER_ID").ToInt();
                            factoryId = SelectedItem.CheckGet("FACTORY_ID").ToInt();
                            typeOrder = SelectedItem.CheckGet("STATUS_ID").ToInt().ContainsIn(2, 6) ? 1 : 2;
                        }
                    }
                }

                if (resume)
                {
                    if (list.Count > 0)
                    {
                        var bindToShipment = new SampleBindToShipment();
                        bindToShipment.ReceiverName = TabName;
                        bindToShipment.ObjectName = "Cliche";
                        bindToShipment.CustomerId = customerId;
                        bindToShipment.FactoryId = factoryId;
                        bindToShipment.TypeOrder = typeOrder;
                        bindToShipment.Bind(string.Join(",", list));
                    }
                    else
                    {
                        var dw = new DialogWindow("Нет подходящих клише для привязки", "Привязка к отгрузке");
                        dw.ShowDialog();
                    }
                }
            }
        }

        /// <summary>
        /// Отвязка выбранного клише от отгрузки
        /// </summary>
        private async void UnbindCliche()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Cliche");
            q.Request.SetParam("Action", "UpdateTS");
            q.Request.SetParam("IdClic", SelectedItem.CheckGet("ID"));
            q.Request.SetParam("IdTs", "0");

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
                    ClicheGrid.LoadItems();
                }
            }
        }

        /// <summary>
        /// Экспорт в Excel отмеченных записей или всей таблицы
        /// </summary>
        private async void ExportToExcel()
        {
            var list = ClicheGrid.GetSelectedItems("CHECKING");

            if (list.Count == 0)
            {
                list = ClicheGrid.GridItems;
            }

            var eg = new ExcelGrid();
            eg.SetColumnsFromGrid(ClicheGrid.Columns);
            // уберем колонку с чекбоксами и номерами строк
            eg.Columns.RemoveAt(0);
            eg.Columns.RemoveAt(0);
            eg.Items = list;
            await Task.Run(() =>
            {
                eg.Make();
            });
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            ClicheGrid.LoadItems();
        }

        private void BindShipment_Click(object sender, RoutedEventArgs e)
        {
            if (ClicheGrid.GridItems != null)
            {
                BindToShipment();
            }
        }

        private void UnbindShipment_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem != null)
            {
                UnbindCliche();
            }
        }

        private void ShowTkButton_Click(object sender, RoutedEventArgs e)
        {
            if (TechMapGrid.SelectedItem != null)
            {
                OpenTechMap();
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void ManagerName_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SingleManager = true;
            Central.SessionValues["ManagersConfig"]["ListActive"] = ManagerName.SelectedItem.Key;
            ClicheGrid.UpdateItems();
        }

        private void SelectManagerButton_Click(object sender, RoutedEventArgs e)
        {
            SingleManager = false;
            var selectManager = new SampleSelectManager();
            selectManager.ReceiverName = TabName;
            selectManager.Show();
        }

        private void ToExcelButton_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcel();
        }
    }
}
