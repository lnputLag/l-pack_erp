using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Список оснастки литой тары на оплату и разрешение на заказ
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class ContainerRigListUnallowable : ControlBase
    {
        public ContainerRigListUnallowable()
        {
            InitializeComponent();

            ControlTitle = "Неразрешенные";
            DocumentationUrl = "/doc/l-pack-erp/preproduction/rig_management";
            RoleName = "[erp]rig_management";

            OnLoad = () =>
            {
                LoadRef();
                InitGrid();
            };

            OnUnload = () =>
            {
                Grid.Destruct();
            };

            OnFocusGot = () =>
            {
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
                Grid.Run();
            };

            OnFocusLost = () =>
            {
                Grid.ItemsAutoUpdate = false;
            };

            OnMessage = (ItemMessage msg) =>
            {
                if ((msg.ReceiverGroup == "PreproductionContainer")
                    || (msg.ReceiverGroup == "PreproductionSample"))
                {
                    if (msg.ReceiverName == ControlName)
                    {
                        ProcessCommand(msg.Action, msg);
                    }
                }
            };

            Commander.SetCurrentGroup("main");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "refresh",
                    Group = "grid_base",
                    Enabled = true,
                    Title = "Обновить",
                    Description = "Обновить данные",
                    ButtonUse = true,
                    ButtonName = "RefreshButton",
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        Grid.LoadItems();
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
                    HotKey = "F1",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        Central.ShowHelp(DocumentationUrl);
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "selectmanager",
                    Enabled = true,
                    Title = "",
                    Description = "Выбрать менеджеров",
                    ButtonUse = true,
                    ButtonName = "SelectManagerButton",
                    MenuUse = false,
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        SingleManager = false;
                        var selectManager = new SampleSelectManager();
                        selectManager.ReceiverName = ControlName;
                        selectManager.Show();
                    },
                });
            }
            Commander.SetCurrentGridName("Grid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "showtechcard",
                    Enabled = true,
                    Group = "operations",
                    Title = "Показать техкарту",
                    Description = "Показать техкарту",
                    ButtonUse = true,
                    ButtonName = "ShowTechCardButton",
                    MenuUse = true,
                    HotKey = "F3",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id > 0)
                        {
                            string path = Grid.SelectedItem.CheckGet("TC_PATH");
                            Central.OpenFile(path);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (row != null)
                        {
                            string path = row.CheckGet("TC_PATH");
                            if (!path.IsNullOrEmpty())
                            {
                                result = true;
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "alloworder",
                    Enabled = true,
                    Group = "operations",
                    Title = "Разрешить заказ",
                    Description = "Разрешить заказ оснастки для выбранной техкарты",
                    ButtonUse = true,
                    ButtonName = "AllowOrderButton",
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id > 0)
                        {
                            SetAllowance();
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = true;
                        return result;
                    },
                });
            }
            Commander.Init(this);
        }

        /// <summary>
        /// Признак фильтрации по менеджеру: false - по списку из сессии, true - по выбранному в выпадающем списке
        /// </summary>
        private bool SingleManager;

        /// <summary>
        /// Обработка общих сообщений
        /// </summary>
        /// <param name="action"></param>
        /// <param name="obj"></param>
        private void ProcessCommand(string command, ItemMessage obj = null)
        {
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "refresh":
                        Grid.LoadItems();
                        break;
                }
            }
        }

        private void InitGrid()
        {
            var roleLevel = Central.Navigator.GetRoleLevel(RoleName);
            var editableCheck = roleLevel == Role.AccessMode.FullAccess || roleLevel == Role.AccessMode.Special;
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header=" ",
                    Path="_SELECTED",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=5,
                    Editable=true,
                },
                new DataGridHelperColumn
                {
                    Header="№",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Покупатель",
                    Path="CUSTOMER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=30,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="PRODUCT_CODE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Наименование изделия",
                    Path="PRODUCT_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=40,
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

                        return true;
                    },
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
                    Header="Путь к техкарте",
                    Path="TC_PATH",
                    ColumnType=ColumnTypeRef.String,
                    Hidden=true,
                },
            };
            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("ID");
            Grid.SearchText = SearchText;
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.Toolbar = GridToolbar;
            Grid.OnLoadItems = LoadItems;
            Grid.Commands = Commander;
            Grid.OnFilterItems = FilterItems;

            Grid.Init();
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

        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        private async void LoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "ContainerRig");
            q.Request.SetParam("Action", "ListUnallowed");

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
                    var ds = ListDataSet.Create(result, "UNALLOWED_RIG");
                    Grid.UpdateItems(ds);
                }

            }
        }

        public void FilterItems()
        {
            if (Grid.Items != null && Grid.Items.Count > 0)
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
                    foreach (Dictionary<string, string> row in Grid.Items)
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
                    Grid.Items = items;
                }
            }
        }

        /// <summary>
        /// Записывает в БД поставленный признак выставления счета
        /// </summary>
        /// <param name="mapId">ID техкарты</param>
        private async void UpdateInvoiceFlag()
        {
            int invoice_flag = Grid.SelectedItem.CheckGet("INVOICE_FLAG").ToInt();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "ContainerRig");
            q.Request.SetParam("Action", "SetInvoice");
            q.Request.SetParam("ID", Grid.SelectedItem.CheckGet("ID"));
            q.Request.SetParam("INVOICE_FLAG", invoice_flag == 0 ? "1" : "0");

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
                    if (result.ContainsKey("ITEM"))
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
        /// Устанока флага разрешения заказа оснастки
        /// </summary>
        private async void SetAllowance()
        {
            var dw = new DialogWindow("Вы действительно хотите разрешить заказ оснастки?", "Разрешение на заказ", "", DialogWindowButtons.YesNo);
            if ((bool)dw.ShowDialog())
            {
                if (dw.ResultButton == DialogResultButton.Yes)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Rig");
                    q.Request.SetParam("Object", "ContainerRig");
                    q.Request.SetParam("Action", "SetAllowance");
                    q.Request.SetParam("ID", Grid.SelectedItem.CheckGet("ID"));
                    q.Request.SetParam("RIG_ALLOWANCE_FLAG", "1");

                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result.ContainsKey("ITEM"))
                        {
                            Grid.LoadItems();
                        }
                    }
                    else if (q.Answer.Error.Code == 145)
                    {
                        var de = new DialogWindow(q.Answer.Error.Message, "Разрешение на заказ");
                        de.ShowDialog();
                    }
                }
            }
        }

        private void ManagerName_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SingleManager = true;
            if (ManagerName.SelectedItem.Key == "-1")
            {
                //Central.SessionValues["ManagersConfig"]["ListActive"] = "";
            }
            Grid.UpdateItems();
        }
    }
}
