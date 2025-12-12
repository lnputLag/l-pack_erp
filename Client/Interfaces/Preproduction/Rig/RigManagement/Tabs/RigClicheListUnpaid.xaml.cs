using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Список клише на оплату и разрешения на заказ клише.
    /// Менеджеры ОРК разрешают заказывать клише инжененрам ОПП
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class RigClicheListUnpaid : UserControl
    {
        public RigClicheListUnpaid()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);
            Central.Msg.Register(ProcessNewMessages);

            ProcessPermition();
            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/osn_management/cliche/unresolved";
            LoadRef();
            InitGrid();
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
            Grid.Destruct();
        }

        /// <summary>
        /// Обработка ввода с клавиатуры
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
                        Grid.ItemsAutoUpdate = true;
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

                        Grid.UpdateItems();
                        break;

                    case "FocusLost":
                        Grid.ItemsAutoUpdate = false;
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
                            Grid.LoadItems();
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
                            Grid.LoadItems();
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
                MarkPaymentButton.IsEnabled = false;
                AllowOrderButton.IsEnabled = false;
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

        public void InitGrid()
        {
            var editableCheck = RoleLevel == Role.AccessMode.FullAccess || RoleLevel == Role.AccessMode.Special;
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header=" ",
                    Path="CHECKING",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth=40,
                    MaxWidth=40,
                    Editable=true,
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
                    Header="Покупатель",
                    Path="CUSTOMER_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=220,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="ART",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=140,
                },
                new DataGridHelperColumn
                {
                    Header="Наименование изделия",
                    Path="PRODUCT_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=550,
                },
                new DataGridHelperColumn
                {
                    Header="Счет выставлен",
                    Path="INVOICE_FLAG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth=40,
                    MaxWidth=40,
                    Editable=editableCheck,
                    OnClickAction = (row, el) =>
                    {
                        UpdateInvoiceFlag();

                        return null;
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Оплата",
                    Path="PAYMENT_FLAG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth=40,
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header="Разрешение на заказ",
                    Path="ORDER_RIG_FLAG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth=40,
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header="Менеджер",
                    Path="MANAGER_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=150,
                },
                new DataGridHelperColumn
                {
                    Header=" ",
                    Path="_",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=20,
                    MaxWidth=1500,
                },
                new DataGridHelperColumn
                {
                    Header="Путь к ТК",
                    Path="PATHTK",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ИД менеджера",
                    Path="MANAGER_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ИД ТК",
                    Path="ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            Grid.SetColumns(columns);
            Grid.SearchText = SearchText;
            Grid.Init();

            // меню
            Grid.Menu = new Dictionary<string, DataGridContextMenuItem>()
            {
                { "MarkPayment",
                    new DataGridContextMenuItem()
                    {
                        Header ="Отметить оплату",
                        Action=() =>
                        {
                            MarkPayment();
                        },
                        Enabled=editableCheck,
                    }
                },
                { "AllowOrder",
                    new DataGridContextMenuItem()
                    {
                        Header ="Разрешить заказ",
                        Action=() =>
                        {
                            AllowOrder();
                        },
                        Enabled=editableCheck,
                    }
                },
                { "ShowMap",
                    new DataGridContextMenuItem()
                    {
                        Header ="Показать ТК",
                        Action=() =>
                        {
                            OpenTechMap();
                        }
                    }
                }
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
        }

        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        private async void LoadItems()
        {
            GridToolbar.IsEnabled = false;
            Grid.ShowSplash();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Cliche");
            q.Request.SetParam("Action", "ListUnpaid");

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
                    var ds = ListDataSet.Create(result, "UNPAID_CLICHE");

                    // Добавим поле для отметки строк
                    foreach (var item in ds.Items)
                    {
                        item.CheckAdd("CHECKING", "0");
                    }

                    Grid.UpdateItems(ds);
                }
            }

            Grid.HideSplash();
            GridToolbar.IsEnabled = true;
        }

        /// <summary>
        /// Фильтрация строк
        /// </summary>
        private void FilterItems()
        {
            if (Grid.GridItems != null)
            {
                if (Grid.GridItems.Count > 0)
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
                        foreach (Dictionary<string, string> row in Grid.GridItems)
                        {
                            bool includeByManager = true;

                            if (doFilteringByManager)
                            {
                                includeByManager = false;
                                var l = managerIds.ToArray();
                                if (row.CheckGet("MANAGER_ID").ToInt().ContainsIn(l))
                                {
                                    includeByManager = true;
                                }
                            }

                            if (includeByManager)
                            {
                                items.Add(row);
                            }

                        }
                        Grid.GridItems = items;
                    }
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

            if (RoleLevel == Role.AccessMode.FullAccess || RoleLevel == Role.AccessMode.Special)
            {
                MarkPaymentButton.IsEnabled = SelectedItem.CheckGet("PAYMENT_FLAG").ToInt() == 0;
                Grid.Menu["MarkPayment"].Enabled = SelectedItem.CheckGet("PAYMENT_FLAG").ToInt() == 0;

                AllowOrderButton.IsEnabled = SelectedItem.CheckGet("ORDER_RIG_FLAG").ToInt() == 0;
                Grid.Menu["AllowOrder"].Enabled = SelectedItem.CheckGet("ORDER_RIG_FLAG").ToInt() == 0;
            }

            ShowTechMapButton.IsEnabled = !SelectedItem.CheckGet("PATHTK").IsNullOrEmpty();
        }

        /// <summary>
        /// Ставит отметку об оплате
        /// </summary>
        private async void MarkPayment()
        {
            var dw = new DialogWindow("Вы действительно хотите отметить оплату клише?", "Оплата клише", "", DialogWindowButtons.YesNo);
            if ((bool)dw.ShowDialog())
            {
                if (dw.ResultButton == DialogResultButton.Yes)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Preproduction");
                    q.Request.SetParam("Object", "Cliche");
                    q.Request.SetParam("Action", "SetPayment");
                    q.Request.SetParam("TECH_MAP_ID", SelectedItem.CheckGet("ID"));

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
                            Grid.LoadItems();
                        }
                    }
                    else if (q.Answer.Error.Code == 145)
                    {
                        var de = new DialogWindow(q.Answer.Error.Message, "Разрешение на клише");
                        de.ShowDialog();
                    }
                }
            }
        }

        /// <summary>
        /// Ставит отметку о разрешении заказа
        /// </summary>
        private async void AllowOrder()
        {
            var dw = new DialogWindow("Вы действительно хотите разрешить заказ клише?", "Разрешение на клише", "", DialogWindowButtons.YesNo);
            if ((bool)dw.ShowDialog())
            {
                if (dw.ResultButton == DialogResultButton.Yes)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Preproduction");
                    q.Request.SetParam("Object", "Cliche");
                    q.Request.SetParam("Action", "SetAllowOrder");
                    q.Request.SetParam("TECH_MAP_ID", SelectedItem.CheckGet("ID"));

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
                            Grid.LoadItems();
                        }
                    }
                    else if (q.Answer.Error.Code == 145)
                    {
                        var de = new DialogWindow(q.Answer.Error.Message, "Разрешение на клише");
                        de.ShowDialog();
                    }
                }
            }
        }

        /// <summary>
        /// Записывает в БД поставленный признак выставления счета
        /// </summary>
        /// <param name="mapId">ID техкарты</param>
        private async void UpdateInvoiceFlag()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Cliche");
            q.Request.SetParam("Action", "SetInvoice");
            q.Request.SetParam("TECH_MAP_ID", SelectedItem.CheckGet("ID"));
            q.Request.SetParam("INVOICE_FLAG", SelectedItem.CheckGet("INVOICE_FLAG"));

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
                        Grid.LoadItems();
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Открывает файл техкарты
        /// </summary>
        private void OpenTechMap()
        {
            bool success = true;
            var path = SelectedItem.CheckGet("PATHTK");
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

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        private void MarkPaymentButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem != null)
            {
                MarkPayment();
            }
        }

        private void AllowOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem != null)
            {
                AllowOrder();
            }
        }

        private void ShowTechMapButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem != null)
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
            Grid.UpdateItems();
        }

        private void SelectManagerButton_Click(object sender, RoutedEventArgs e)
        {
            SingleManager = false;
            var selectManager = new SampleSelectManager();
            selectManager.ReceiverName = TabName;
            selectManager.Show();
        }

        private void CopyToClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            var text = "";
            if (Grid.GridItems.Count > 0)
            {
                foreach(var row in Grid.GridItems)
                {
                    if (row["CHECKING"].ToInt() == 1)
                    {
                        text = $"{text}\n{row.CheckGet("ART")} {row.CheckGet("PRODUCT_NAME")}";
                    }
                }
            }

            Clipboard.SetText(text);
        }
    }
}
