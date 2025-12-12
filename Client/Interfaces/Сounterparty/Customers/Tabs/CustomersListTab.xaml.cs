using System.Collections.Generic;
using Client.Common;
using static Client.Interfaces.Main.DataGridHelperColumn;
using Client.Interfaces.Main;
using Client.Interfaces.Сounterparty.Customers.Frames;
using Newtonsoft.Json;
using Task = System.Threading.Tasks.Task;

namespace Client.Interfaces.Сounterparty.Customers.Tabs
{
    public partial class CustomersListTab : ControlBase
    {
        public CustomersListTab()
        {
            InitializeComponent();

            ControlSection = "customer_list";
            RoleName = "[erp]customers";
            ControlTitle = "Потребители";

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == "CustomersGrid")
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnLoad = () =>
            {
                CustomersGridInit();
                BuyerGridInit();
            };
            
            OnUnload = () =>
            {
                CustomersGrid.Destruct();
                BuyerGrid.Destruct();
            };
            
            OnFocusGot = () =>
            {
                CustomersGrid.ItemsAutoUpdate = true;
                BuyerGrid.ItemsAutoUpdate = true;
            };
            
            OnFocusLost = () =>
            {
                CustomersGrid.ItemsAutoUpdate = false;
                BuyerGrid.ItemsAutoUpdate = false;
            };

            {
                Commander.SetCurrentGridName("CustomersGrid");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "refresh_customers_grid",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить данные",
                        ButtonUse = true,
                        ButtonName = "RefreshButton",
                        MenuUse = true,
                        Action = () =>
                        {
                            CustomersGrid.LoadItems();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "export_customers_grid",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "В Excel",
                        Description = "Экспортировать данные в Excel",
                        ButtonUse = true,
                        ButtonName = "ExportToExcel",
                        MenuUse = true,
                        Action = () =>
                        {
                            CustomersGrid.ItemsExportExcel();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "edit_customers",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Изменить",
                        ButtonUse = true,
                        ButtonName = "EditCustomers",
                        MenuUse = true,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var row = CustomersGrid.SelectedItem.CheckGet("CUST_ID");
                            
                            var e = new CustomersEditFrame();
                            e.Edit(row.ToInt());
                        },
                    });
                }
                Commander.SetCurrentGridName("BuyerGrid");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "refresh_buyer_grid",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить данные",
                        ButtonUse = true,
                        ButtonName = "RefreshBuyerButton",
                        MenuUse = true,
                        Action = () =>
                        {
                            var item = CustomersGrid.SelectedItem;
                            LoadBuyers(item.CheckGet("CUST_ID").ToInt().ToString());
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "add_buyer_grid",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Добавить",
                        ButtonUse = true,
                        ButtonName = "AddBuyerButton",
                        MenuUse = true,
                        AccessLevel= Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var itemId = CustomersGrid.SelectedItem.CheckGet("CUST_ID");
                            
                            var cr = new LinkCustomersProductFrame();
                            cr.Create(itemId);
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "delete_buyer_grid",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Удалить",
                        ButtonUse = true,
                        ButtonName = "DeleteBuyerButton",
                        MenuUse = true,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var item = BuyerGrid.SelectedItem;
                            var idPok = item.CheckGet("ID_POK").ToInt().ToString();
                            var idCust = item.CheckGet("CUST_ID").ToInt().ToString();

                            var dialog = new DialogWindow($"Удалить покупателя -{idPok}?", "Удаление записи", "",
                                DialogWindowButtons.YesNo);

                            dialog.ShowDialog();

                            if (dialog.DialogResult == true)
                            {
                                DeleteBuyer(idPok: idPok, custId: idCust);
                            }
                        },
                    });
                }
                Commander.Init(this);
            }
        }

        private ListDataSet CustomersList { get; set; }
        private ListDataSet BuyerList { get; set; }

        private void CustomersGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Потребитель",
                    Path="CUSTOMER",
                    ColumnType=ColumnTypeRef.String,
                    Width2=26,
                },
                new DataGridHelperColumn
                {
                    Header = "Группа",
                    Path = "CUSTGROUP",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 20,
                },
                new DataGridHelperColumn
                {
                    Header = "Кр.н потребителя",
                    Path = "CUSTOMER_SHORT",
                    Description = "Краткое наименование потребителя",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 20,
                },
                new DataGridHelperColumn
                {
                    Header="Посредник",
                    Path="DEALER",
                    ColumnType=ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn
                {
                    Header="Ед.изм",
                    Description = "Единица измерения",
                    Path="NAME_VALUE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=7,
                },
                new  DataGridHelperColumn
                {
                    Header = "Срок годности потребителя (в месяцах)",
                    Path = "SHELF_LIFE",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 11,
                },
                new  DataGridHelperColumn
                {
                    Header = "Марка листов в д-ах",
                    Path = "TOVAR_DETAILS",
                    ColumnType = ColumnTypeRef.Boolean,
                    Width2 = 16,
                },
                new  DataGridHelperColumn
                {
                    Header = "Марка в док-тах",
                    Path = "TOVAR_DETAILS_ALL",
                    ColumnType = ColumnTypeRef.Boolean,
                    Width2 = 13,
                },
                new DataGridHelperColumn
                {
                    Header = "ИД",
                    Path = "CUST_ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 5,
                }
            };
            CustomersGrid.SetColumns(columns);
            CustomersGrid.SetPrimaryKey("CUST_ID");
            CustomersGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            CustomersGrid.SearchText = SearchText;
            CustomersGrid.Toolbar = CustomersToolbar;
            CustomersGrid.OnLoadItems = LoadCustomers;
            CustomersGrid.Commands = Commander;

            CustomersGrid.OnSelectItem = item =>
            {
                LoadBuyers(item.CheckGet("CUST_ID").ToInt().ToString());
            };
            
            CustomersGrid.Init();
        }

        private void BuyerGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Покупатель",
                    Path="NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=26,
                },
                new DataGridHelperColumn
                {
                    Header="Изделия потребителя",
                    Path="CUSTOMER",
                    ColumnType=ColumnTypeRef.String,
                    Width2=26,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Description = "Идентификатор покупателя",
                    Path="ID_POK",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=7,
                },
            };
            BuyerGrid.SetColumns(columns);
            BuyerGrid.SetPrimaryKey("ID_POK");
            BuyerGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            BuyerGrid.Commands = Commander;
            
            BuyerGrid.Init();
        }

        private async void LoadBuyers(string custId)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Сounterparty");
            q.Request.SetParam("Object", "Customers");
            q.Request.SetParam("Action", "GiveListWithBuyer");
            q.Request.SetParam("CUST_ID", custId);
            
            q.Request.Timeout = Central.Parameters.RequestTimeoutDefault;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;
            
            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                
                if (result!= null)
                {
                    BuyerList = ListDataSet.Create(result, "ITEMS");
                }
                
                BuyerGrid.UpdateItems(BuyerList);

                if (BuyerList.Items.Count > 0)
                {
                    DeleteBuyerButton.IsEnabled = true;
                    BuyerGrid.SelectRowFirst();
                }
                else
                {
                    DeleteBuyerButton.IsEnabled = false;
                }
            }
        }

        private async void DeleteBuyer(string idPok, string custId)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Сounterparty");
            q.Request.SetParam("Object", "Customers");
            q.Request.SetParam("Action", "DeleteBuyer");
            q.Request.SetParam("ID_POK", idPok);
            q.Request.SetParam("CUST_ID", custId);
            
            q.Request.Timeout = Central.Parameters.RequestTimeoutDefault;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;
            
            await Task.Run(() =>
            {
                q.DoQuery();
            });
            
            if (q.Answer.Status == 0)
            {
                LoadBuyers(custId);
            }
            else
            {
                q.ProcessError();
            }
        }
        
        private async void LoadCustomers()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Сounterparty");
            q.Request.SetParam("Object", "Customers");
            q.Request.SetParam("Action", "List");

            q.Request.Timeout = Central.Parameters.RequestTimeoutDefault;
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
                    CustomersList = ListDataSet.Create(result, "ITEMS");
                }
                
                CustomersGrid.UpdateItems(CustomersList);
            }
        }
    }
}