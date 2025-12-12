using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Debug;
using Client.Interfaces.Delivery;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
using Client.Interfaces.Production;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using Xceed.Wpf.Toolkit.Primitives;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static Client.Interfaces.Main.FormDialog;

namespace Client.Interfaces.DeliveryAddresses
{
    /// <summary>
    /// Доставка покупателю
    /// </summary>
    /// <author>motenko_ek</author>
    public partial class ResellerClientTab : ControlBase
    {
        public ResellerClientTab()
        {
            InitializeComponent();

            ControlSection = "reseller_client";
            RoleName = "[erp]delivery_addresses";
            ControlTitle = "Клиенты";
            DocumentationUrl = "/doc/l-pack-erp/delivery/delivery_addresses/reseller_client";
            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };
            OnKeyPressed = (KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };
            OnLoad = () =>
            {
                ResellerClientGridInit();
            };
            OnUnload = () =>
            {
                ResellerClientGrid.Destruct();
            };
            //OnFocusGot = () =>
            //{
            //    ResellerClientGrid.ItemsAutoUpdate = true;
            //    ResellerClientGrid.Run();
            //};
            //OnFocusLost = () =>
            //{
            //    ResellerClientGrid.ItemsAutoUpdate = false;
            //};
            OnNavigate = () =>
            {
                var resellerClientId = Parameters.CheckGet("reseller_client_id");
                if (!resellerClientId.IsNullOrEmpty())
                {
                    ResellerClientSearchText.Text = resellerClientId;
                    ResellerClientGrid.UpdateItems();
                }
            };

            Commander.SetCurrentGroup("main");
            Commander.Add(new CommandItem()
            {
                Name = "help",
                Enabled = true,
                Title = "Справка",
                Description = "Показать справочную информацию",
                ButtonUse = true,
                ButtonName = "HelpButton",
                HotKey = "F1",
                Action = () =>
                {
                    Central.ShowHelp(DocumentationUrl);
                },
            });
            Commander.SetCurrentGridName("ResellerClientGrid");
            Commander.Add(new CommandItem()
            {
                Name = "reseller_client_refresh",
                Group = "grid_base",
                Enabled = true,
                Title = "Обновить",
                Description = "Обновить данные клиентов",
                ButtonUse = true,
                ButtonName = "ResellerClientRefreshButton",
                MenuUse = true,
                Action = () =>
                {
                    ResellerClientGrid.LoadItems();
                },
            });
            Commander.Add(new CommandItem()
            {
                Name = "reseller_client_add",
                Group = "grid_base",
                Enabled = true,
                Title = "Добавить клиента",
                MenuUse = true,
                HotKey = "Insert",
                ButtonUse = true,
                ButtonName = "ResellerClientAddButton",
                AccessLevel = Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    var i = new ResellerClientForm();
                    i.Create();
                },
                //CheckEnabled = () =>
                //{
                //    var result = false;
                //    var k = ResellerClientGrid.GetPrimaryKey();
                //    var row = ResellerClientGrid.SelectedItem;
                //    if (row.CheckGet(k).ToInt() != 0)
                //    {
                //        result = true;
                //    }
                //    return result;
                //},
            });
            Commander.SetCurrentGroup("item");
            Commander.Add(new CommandItem()
            {
                Name = "reseller_client_edit",
                Title = "Изменить клиента",
                MenuUse = true,
                HotKey = "Return|DoubleCLick",
                ButtonUse = true,
                ButtonName = "ResellerClientEditButton",
                AccessLevel = Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    var k = ResellerClientGrid.GetPrimaryKey();
                    var id = ResellerClientGrid.SelectedItem.CheckGet("ID_CLIENT").ToInt();
                    if (id != 0)
                    {
                        var i = new ResellerClientForm();
                        i.Edit(id);
                    }
                },
                CheckEnabled = () =>
                {
                    var result = false;
                    var k = ResellerClientGrid.GetPrimaryKey();
                    var row = ResellerClientGrid.SelectedItem;
                    if (row.CheckGet(k).ToInt() != 0)
                    {
                        result = true;
                    }
                    return result;
                },
            });
            Commander.Add(new CommandItem()
            {
                Name = "reseller_client_delete",
                Title = "Удалить клиента",
                MenuUse = true,
                ButtonUse = true,
                ButtonName = "ResellerClientDeleteButton",
                AccessLevel = Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    var id = ResellerClientGrid.SelectedItem.CheckGet("ID_CLIENT").ToInt();
                    if (id != 0)
                    {
                        DeleteResellerClient(ResellerClientGrid.SelectedItem);
                    }
                },
                CheckEnabled = () =>
                {
                    var result = false;
                    var k = ResellerClientGrid.GetPrimaryKey();
                    var row = ResellerClientGrid.SelectedItem;
                    if (row.CheckGet(k).ToInt() != 0)
                    {
                        result = true;
                    }
                    return result;
                },
            });
            Commander.Init(this);
        }

        /// <summary>
        /// инициализация грида клиентов
        /// </summary>
        public void ResellerClientGridInit()
        {
            var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД клиента",
                        Path="ID_CLIENT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Клиент",
                        Path="NAME_CLIENT",
                        ColumnType=ColumnTypeRef.String,
                        Width2=30,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Адрес",
                        Path="ADDRESS_DOC",
                        ColumnType=ColumnTypeRef.String,
                        Width2=60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Покупатель",
                        Path="NAME_POKUPATEL",
                        ColumnType=ColumnTypeRef.String,
                        Width2=30,
                    },
                };
            ResellerClientGrid.SetColumns(columns);
            ResellerClientGrid.SetPrimaryKey("ID_CLIENT");
            //ResellerClientGrid.SetSorting("ID_CLIENT", System.ComponentModel.ListSortDirection.Ascending);
            ResellerClientGrid.SearchText = ResellerClientSearchText;
            ResellerClientGrid.Toolbar = ResellerClientGridToolbar;
            ResellerClientGrid.Commands = Commander;
            ResellerClientGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            ResellerClientGrid.AutoUpdateInterval = 0;
            ResellerClientGrid.ItemsAutoUpdate = false;
            ResellerClientGrid.QueryLoadItems = new RequestData()
            {
                Module = "Delivery",
                Object = "ResellerClient",
                Action = "List",
                AnswerSectionKey = "ITEMS",
            };
            ResellerClientGrid.OnSelectItem = (Dictionary<string, string> selectedItem) =>
            {
                selectedItem = selectedItem;
            };
            ResellerClientGrid.Init();
        }

        private async void DeleteResellerClient(Dictionary<string, string> row = null)
        {
            if (ResellerClientGrid != null && ResellerClientGrid.Items != null && ResellerClientGrid.Items.Count > 0)
            {
                if (
                    ResellerClientGrid.SelectedItem != null && ResellerClientGrid.SelectedItem.Count > 0
                    )
                {
                    var message = $"Удалить клиента \"{row.CheckGet("NAME_CLIENT")}\"?";
                    if (DialogWindow.ShowDialog(message, "Клиент", "", DialogWindowButtons.NoYes) != true)
                    {
                        return;
                    }

                    bool succesfulFlag = false;
                    int clientId = 0;

                    var p = new Dictionary<string, string>();
                    p.Add("ID_CLIENT", row.CheckGet("ID_CLIENT"));

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Delivery");
                    q.Request.SetParam("Object", "ResellerClient");
                    q.Request.SetParam("Action", "Delete");
                    q.Request.SetParams(p);

                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            var dataSet = ListDataSet.Create(result, "ITEMS");

                            if (dataSet != null && dataSet.Items.Count > 0)
                            {
                                clientId = dataSet.Items.First().CheckGet("ID_CLIENT").ToInt();

                                if (clientId > 0)
                                {
                                    succesfulFlag = true;
                                }
                            }
                        }

                        if (succesfulFlag)
                        {
                            ResellerClientGrid.SelectRowPrev();
                            ResellerClientGrid.LoadItems();
                        }
                        else
                        {
                            string msg = $"Ошибка удаления клиента";
                            var d = new DialogWindow($"{msg}", "Клиент", "", DialogWindowButtons.OK);
                            d.ShowDialog();
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
}
