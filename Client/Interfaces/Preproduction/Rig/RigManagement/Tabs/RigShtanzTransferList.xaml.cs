using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static Client.Interfaces.Preproduction.Rig.CuttingStampTab;

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Список штанцформ, которые подготовлены к передаче клиенту, и которые надо привязать к отгрузке
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class RigShtanzTransferList : ControlBase
    {
        public RigShtanzTransferList()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            //Messenger.Default.Register<ItemMessage>(this, ProcessMessages);
            //Central.Msg.Register(ProcessNewMessages);

            ControlTitle = "На передачу";
            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/osn_management/cliche/to_transfer";
            RoleName = "[erp]rig_management";

            OnLoad = () =>
            {
                LoadRef();
                InitShtanzGrid();
                InitTechMapGrid();
            };

            OnUnload = () =>
            {
                ShtanzGrid.Destruct();
                TechMapGrid.Destruct();
            };

            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverName == ControlName)
                {
                    ProcessMessages(msg);
                }
            };

            OnFocusGot = () =>
            {
                ShtanzGrid.ItemsAutoUpdate = true;
                ShtanzGrid.Run();
            };

            OnFocusLost = () =>
            {
                ShtanzGrid.ItemsAutoUpdate = false;
            };

            Commander.SetCurrentGroup("main");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "refresh",
                    Group = "main",
                    Enabled = true,
                    Title = "Обновить",
                    Description = "Обновить данные",
                    ButtonUse = true,
                    ButtonName = "RefreshButton",
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        ShtanzGrid.LoadItems();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "help",
                    Enabled = true,
                    Title = "Справка",
                    Description = "Показать справочную информацию",
                    ButtonUse = true,
                    ButtonName = "HelpButton",
                    MenuUse = false,
                    HotKey = "F1",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        Central.ShowHelp(DocumentationUrl);
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "exporttoexcel",
                    Group = "main",
                    Enabled = true,
                    Title = "В Excel",
                    Description = "Выгрузить данные в Excel файл",
                    ButtonUse = true,
                    ButtonName = "ToExcelButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        ShtanzGrid.ItemsExportExcel();
                    },
                });
            }
            Commander.SetCurrentGridName("ShtanzGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "bindshipment",
                    Enabled = true,
                    Title = "Привязать",
                    Group = "operations",
                    Description = "Привязать штанцформу к отгрузке",
                    ButtonUse = true,
                    ButtonName = "BindShipmentButton",
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = ShtanzGrid.GetPrimaryKey();
                        var id = ShtanzGrid.SelectedItem.CheckGet(k).ToInt();

                        if (id != 0)
                        {
                            BindToShipment();
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = ShtanzGrid.SelectedItem;
                        if (row != null)
                        {
                            if (row.CheckGet("SHIPMENT_ID").ToInt() == 0)
                            {
                                result = true;
                            }
                        }
                        return result;
                    }
                });
                Commander.Add(new CommandItem()
                {
                    Name = "unbindshipment",
                    Enabled = true,
                    Title = "Отвязать",
                    Group = "operations",
                    Description = "Отвязать штанцформу от отгрузки",
                    ButtonUse = true,
                    ButtonName = "UnbindShipmentButton",
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = ShtanzGrid.GetPrimaryKey();
                        var id = ShtanzGrid.SelectedItem.CheckGet(k).ToInt();

                        if (id != 0)
                        {
                            UnbindShtanz();
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = ShtanzGrid.SelectedItem;
                        if (row != null)
                        {
                            if (row.CheckGet("SHIPMENT_ID").ToInt() > 0)
                            {
                                result = true;
                            }
                        }
                        return result;
                    }
                });
                Commander.Add(new CommandItem()
                {
                    Name = "selectmanager",
                    Enabled = true,
                    Title = "",
                    Group = "operations",
                    Description = "Выбрать менеджеров",
                    ButtonUse = true,
                    ButtonName = "SelectManagerButton",
                    MenuUse = false,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        SingleManager = false;
                        var selectManager = new SampleSelectManager();
                        selectManager.ReceiverName = ControlName;
                        selectManager.Show();
                    },
                    CheckEnabled = () =>
                    {
                        var result = true;
                        return result;
                    }
                });
            }
            Commander.SetCurrentGridName("TechMapGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "showtk",
                    Enabled = true,
                    Title = "Техкарта",
                    Description = "Показать техкарту изделия",
                    ButtonUse = true,
                    ButtonName = "ShowTkButton",
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        var path = TechMapGrid.SelectedItem.CheckGet("TECHCARD_PATH");
                        Central.OpenFile(path);
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var path = TechMapGrid.SelectedItem.CheckGet("TECHCARD_PATH");
                        if (!path.IsNullOrEmpty())
                        {
                            if (File.Exists(path))
                            {
                                result = true;
                            }
                        }
                        return result;
                    }
                });
            }
            Commander.Init(this);
        }

        /// <summary>
        /// Признак фильтрации по менеджеру: false - по списку из сессии, true - по выбранному в выпадающем списке
        /// </summary>
        private bool SingleManager;

        /// <summary>
        /// Уровень доступа пользователя к функциям интерфейса
        /// </summary>
        Role.AccessMode RoleLevel { get; set; }

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        public void ProcessNavigation()
        {

        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="obj">сообщение</param>
        private void ProcessMessages(ItemMessage obj)
        {
            if (obj.ReceiverName.IndexOf(ControlName) > -1)
            {
                switch (obj.Action)
                {
                    case "Refresh":
                        ShtanzGrid.LoadItems();
                        break;
                }
            }
        }

        /// <summary>
        /// Инициализация таблицы штанцформ
        /// </summary>
        private void InitShtanzGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header=" ",
                    Path="_SELECTED",
                    ColumnType=ColumnTypeRef.Boolean,
                    Editable=true,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="№",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="Плательщик",
                    Path="CUSTOMER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="Статус",
                    Path="STATUS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20
                },
                new DataGridHelperColumn
                {
                    Header="Дата статуса",
                    Path="STATUS_CHANGED_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=10,
                    Format="dd.MM.yyyy",
                    Stylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("SHIPMENT_NAME").IsNullOrEmpty())
                                {
                                    var statusDttm = row.CheckGet("STATUS_CHANGED_DTTM").ToDateTime("dd.MM.yyyy");
                                    if (DateTime.Compare(statusDttm, DateTime.Now.AddDays(-30)) < 0)
                                    {
                                        color = HColor.PinkOrange;
                                    }
                                }

                                if (!color.IsNullOrEmpty())
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        },
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Площадка",
                    Path="FACTORY",
                    ColumnType=ColumnTypeRef.String,
                    Width2=14,
                },
                new DataGridHelperColumn
                {
                    Header="Отгрузка",
                    Path="SHIPMENT_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="Размер упаковки, мм",
                    Path="PACK_SIZE",
                    ColumnType=ColumnTypeRef.String,
                    Exportable=false,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание кладовщика",
                    Path="NOTE",
                    ColumnType=ColumnTypeRef.String,
                    Exportable=false,
                    Width2=16,
                },
                new DataGridHelperColumn
                {
                    Header="Менеджер",
                    Path="MANAGER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="Артикулы изделий",
                    Path="ARTIKUL_LIST",
                    ColumnType=ColumnTypeRef.String,
                    //idth2=20,
                    Hidden=true,
                    Searchable=true,
                },
                new DataGridHelperColumn
                {
                    Header="Код статуса",
                    Path="STATUS_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Код менеджера",
                    Path="MANAGER_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ИД отгрузки",
                    Path="SHIPMENT_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ИД плательщика",
                    Path="CUSTOMER_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ИД ШФ",
                    Path="STAMP_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };

            ShtanzGrid.SetColumns(columns);
            ShtanzGrid.SetPrimaryKey("ID");
            ShtanzGrid.SetSorting("ID", ListSortDirection.Descending);
            ShtanzGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            ShtanzGrid.SearchText = SearchText;
            ShtanzGrid.Toolbar = GridToolbar;
            ShtanzGrid.Commands = Commander;
            //данные грида
            ShtanzGrid.OnLoadItems = LoadShtanzItems;
            ShtanzGrid.OnFilterItems = FilterShtanzItems;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            ShtanzGrid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdateActions(selectedItem);
                }
            };
            ShtanzGrid.Init();
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
                    Width2=100,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул изделия",
                    Path="ARTICLE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=60,
                },
                new DataGridHelperColumn
                {
                    Header="Дата рассылки",
                    Path="NOTIFICATION_DT",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Width2=60,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=40,
                },
                new DataGridHelperColumn
                {
                    Header="Путь к ТК",
                    Path="TECHCARD_PATH",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Hidden=true,
                },
            };
            TechMapGrid.SetColumns(columns);
            TechMapGrid.SetPrimaryKey("ID");
            TechMapGrid.SetSorting("ID", ListSortDirection.Descending);
            TechMapGrid.Commands = Commander;
            TechMapGrid.Init();
        }

        /// <summary>
        /// Загрузка справочников
        /// </summary>
        private async void LoadRef()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "ShtanzForms");
            q.Request.SetParam("Action", "LoadRef");

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    //Производственная площадка
                    var factoryDS = ListDataSet.Create(result, "FACTORY");
                    var factoryItems = new Dictionary<string, string>()
                    {
                        { "0", "Все" },
                    };
                    foreach (var f in factoryDS.Items)
                    {
                        factoryItems.Add(f.CheckGet("ID"), f.CheckGet("NAME"));
                    }
                    Factory.Items = factoryItems;
                    Factory.SetSelectedItemByKey("0");

                    // менеджеры по работе с клиентами
                    var managersDS = ListDataSet.Create(result, "MANAGERS");
                    var list = new Dictionary<string, string>()
                    {
                        { "-1", "Все" },
                    };

                    var managers = new Dictionary<string, string>();
                    foreach (var item in managersDS.Items)
                    {
                        list.CheckAdd(item.CheckGet("ID").ToInt().ToString(), item.CheckGet("FIO"));
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

        /// <summary>
        /// Загрузка данных в таблицу штанцформ
        /// </summary>
        private async void LoadShtanzItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "CuttingStamp");
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
                    var ds = ListDataSet.Create(result, "STAMP_TRANSFER");
                    ShtanzGrid.UpdateItems(ds);
                }
            }
        }

        /// <summary>
        /// Фильтрация данных в таблице клише
        /// </summary>
        private void FilterShtanzItems()
        {
            if (ShtanzGrid.Items != null)
            {
                if (ShtanzGrid.Items.Count > 0)
                {

                    bool doFilteringByManager = false;
                    bool doFilteringByFactory = false;

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

                    var factoryId = Factory.SelectedItem.Key.ToInt();
                    if (factoryId > 0)
                    {
                        doFilteringByFactory= true;
                    }

                    if (doFilteringByManager || doFilteringByFactory)
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in ShtanzGrid.Items)
                        {
                            bool includeByManager = true;
                            bool includeByFactory = true;

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

                            if (doFilteringByFactory)
                            {
                                includeByFactory = false;
                                if (row.CheckGet("FACTORY_ID").ToInt() == factoryId)
                                {
                                    includeByFactory = true;
                                }
                            }

                            if (includeByManager && includeByFactory)
                            {
                                items.Add(row);
                            }

                        }
                        ShtanzGrid.Items = items;
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
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "CuttingStamp");
            q.Request.SetParam("Action", "TkList");
            q.Request.SetParam("ID", SelectedItem.CheckGet("STAMP_ID"));

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
                    var ds = ListDataSet.Create(result, "TECHCARD_LIST");
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
        /// Определение выбранных штанцформ и привязка их к отгрузке
        /// </summary>
        private void BindToShipment()
        {
            if (ShtanzGrid.Items.Count > 0)
            {
                int customerId = -1;
                int factoryId = 0;
                bool resume = true;
                int typeOrder = 0;

                var list = new List<int>();
                foreach (var row in ShtanzGrid.Items)
                {
                    if (row.CheckGet("_SELECTED").ToBool())
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
                                var dw = new DialogWindow("Все выбранные штанцформы должны отгружаться или клиентам, или на другую площадку", "Привязка к отгрузке");
                                dw.ShowDialog();
                                break;
                            }

                            //Все выбранные штанцформы должны отгружаться с одной площадки
                            int currentFactoryId = row.CheckGet("FACTORY_ID").ToInt();
                            if (factoryId == 0)
                            {
                                factoryId = currentFactoryId;
                            }
                            else if (factoryId != currentFactoryId)
                            {
                                resume= false;
                                var dw = new DialogWindow("Все выбранные штанцформы должны отгружаться с одной площадки", "Привязка к отгрузке");
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
                            typeOrder = SelectedItem.CheckGet("STATUS_ID").ToInt().ContainsIn(12, 13) ? 1 : 2;
                        }
                    }
                }

                if (resume)
                {
                    if (list.Count > 0)
                    {
                        var bindToShipment = new RigBindToShipment();
                        bindToShipment.ReceiverName = ControlName;
                        bindToShipment.ObjectName = "CuttingStamp";
                        bindToShipment.CustomerId = customerId;
                        bindToShipment.FactoryId = factoryId;
                        bindToShipment.TypeOrder = typeOrder;
                        bindToShipment.Bind(string.Join(",", list));
                    }
                    else
                    {
                        var dw = new DialogWindow("Нет подходящих штанцформ для привязки", "Привязка к отгрузке");
                        dw.ShowDialog();
                    }
                }
            }
        }

        /// <summary>
        /// Отвязка выбранной штанцформы от отгрузки
        /// </summary>
        private async void UnbindShtanz()
        {
            var idList = "";
            foreach (var item in ShtanzGrid.Items)
            {
                if (item.CheckGet("_SELECTED").ToBool())
                {
                    var currentId = item.CheckGet("ID");
                    if (idList.Length == 0)
                    {
                        idList = currentId;
                    }
                    else
                    {
                        idList = $"{idList},{currentId}";
                    }
                }
            }

            if (!idList.IsNullOrEmpty())
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Rig");
                q.Request.SetParam("Object", "CuttingStamp");
                q.Request.SetParam("Action", "BindShipment");
                q.Request.SetParam("SHIPMENT_ID", "0");
                q.Request.SetParam("ID_LIST", idList);

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
                        ShtanzGrid.LoadItems();
                    }
                }
            }
        }

        private void ManagerName_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SingleManager = true;
            Central.SessionValues["ManagersConfig"]["ListActive"] = ManagerName.SelectedItem.Key;
            ShtanzGrid.UpdateItems();
        }

        private void Factory_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ShtanzGrid.UpdateItems();
        }
    }
}
