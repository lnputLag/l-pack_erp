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
using System.Runtime.CompilerServices;
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
    public partial class DeliveryFromSupplierTab : ControlBase
    {
        public DeliveryFromSupplierTab()
        {
            InitializeComponent();

            ShippingAddressCopyButton.Click += ShippingAddressCopyButtonClick;
            ShippingAddressPasteButton.Click += ShippingAddressPasteButtonClick;

            ControlSection = "delivery_from_supplier";
            RoleName = "[erp]delivery_addresses";
            ControlTitle = "Доставка от поставщика";
            DocumentationUrl = "/doc/l-pack-erp/delivery/delivery_addresses/delivery_from_supplier";
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
                SupplierGridInit();
                ShippingAddressGridInit();
            };
            OnUnload = () =>
            {
                SupplierGrid.Destruct();
                ShippingAddressGrid.Destruct();
            };
            //OnFocusGot = () =>
            //{
            //    SupplierGrid.ItemsAutoUpdate = true;
            //    SupplierGrid.Run();
            //};
            //OnFocusLost = () =>
            //{
            //    SupplierGrid.ItemsAutoUpdate = false;
            //};
            OnNavigate = () =>
            {
                var postavshicId = Parameters.CheckGet("supplier_id");
                if (!postavshicId.IsNullOrEmpty())
                {
                    SupplierSearchText.Text = postavshicId;
                    SupplierGrid.UpdateItems();
                }

                var shipAdresId = Parameters.CheckGet("shipping_address_id");
                if (!shipAdresId.IsNullOrEmpty())
                {
                    ShippingAddressSearchText.Text = shipAdresId;
                    ShippingAddressGrid.UpdateItems();
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
            Commander.SetCurrentGridName("SupplierGrid");
            Commander.Add(new CommandItem()
            {
                Name = "supplier_refresh",
                Group = "grid_base",
                Enabled = true,
                Title = "Обновить",
                Description = "Обновить данные грузополучателей",
                ButtonUse = true,
                ButtonName = "SupplierRefreshButton",
                MenuUse = true,
                Action = () =>
                {
                    SupplierGrid.LoadItems();
                },
            });
            Commander.SetCurrentGroup("item");
            Commander.SetCurrentGridName("ShippingAddressGrid");
            Commander.Add(new CommandItem()
            {
                Name = "shipping_address_refresh",
                Group = "grid_base",
                Enabled = true,
                Title = "Обновить",
                Description = "Обновить данные адресов",
                ButtonUse = false,
                //ButtonName = "SupplierRefreshButton",
                MenuUse = true,
                ActionMessage = (ItemMessage message) =>
                {
                    IdAdres = message.Message;
                    ShippingAddressGrid.LoadItems();
                },
            });
            Commander.Add(new CommandItem()
            {
                Name = "shipping_address_add",
                Title = "Добавить адрес",
                MenuUse = true,
                HotKey = "Insert",
                ButtonUse = true,
                ButtonName = "ShippingAddressAddButton",
                AccessLevel = Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    new ShippingAddressForm(new ItemMessage()
                    {
                        ReceiverName = ControlName,
                        Action = "shipping_address_refresh",
                    }, SupplierGrid.SelectedItem.CheckGet("ID_POST").ToInt(), true);
                },
                CheckEnabled = () =>
                {
                    var result = false;
                    var k = SupplierGrid.GetPrimaryKey();
                    var row = SupplierGrid.SelectedItem;
                    if (row.CheckGet(k).ToInt() != 0)
                    {
                        result = true;
                    }
                    return result;
                },
            });
            Commander.SetCurrentGroup("item");
            Commander.Add(new CommandItem()
            {
                Name = "shipping_address_edit",
                Title = "Изменить адрес",
                MenuUse = true,
                HotKey = "Return|DoubleCLick",
                ButtonUse = true,
                ButtonName = "ShippingAddressEditButton",
                AccessLevel = Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    var k = ShippingAddressGrid.GetPrimaryKey();
                    var id = ShippingAddressGrid.SelectedItem.CheckGet("ID_ADRES").ToInt();
                    if (id != 0)
                    {
                        new ShippingAddressForm(new ItemMessage()
                        {
                            ReceiverName = ControlName,
                            Action = "shipping_address_refresh",
                        }, SupplierGrid.SelectedItem.CheckGet("ID_POST").ToInt(), true, id);
                    }
                },
                CheckEnabled = () =>
                {
                    var result = false;
                    var k = ShippingAddressGrid.GetPrimaryKey();
                    var row = ShippingAddressGrid.SelectedItem;
                    if (row.CheckGet(k).ToInt() != 0)
                    {
                        result = true;
                    }
                    return result;
                },
            });
            Commander.Add(new CommandItem()
            {
                Name = "shipping_address_delete",
                Title = "Удалить адрес",
                MenuUse = true,
                ButtonUse = true,
                ButtonName = "ShippingAddressDeleteButton",
                AccessLevel = Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    var id = ShippingAddressGrid.SelectedItem.CheckGet("ID_ADRES").ToInt();
                    if (id != 0)
                    {
                        DeleteShippingAddress(ShippingAddressGrid.SelectedItem);
                    }
                },
                CheckEnabled = () =>
                {
                    var result = false;
                    var k = ShippingAddressGrid.GetPrimaryKey();
                    var row = ShippingAddressGrid.SelectedItem;
                    if (row.CheckGet(k).ToInt() != 0)
                    {
                        result = true;
                    }
                    return result;
                },
            });
            Commander.Add(new CommandItem()
            {
                Name = "shipping_address_show",
                Title = "Показать схему проезда",
                MenuUse = true,
                ButtonUse = true,
                ButtonName = "ShippingAddressShowButton",
                AccessLevel = Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    var fileName = GetFileName();

                    if (!fileName.IsNullOrEmpty())
                    {
                        try { System.Diagnostics.Process.Start(fileName); }
                        catch (Exception ex) { DialogWindow.ShowDialog(ex.Message, ex.Source, ""); }
                    }
                },
                CheckEnabled = () =>
                {
                    return !GetFileName().IsNullOrEmpty();
                },
            });
            Commander.Init(this);
        }

        private int CheckBoxCount = 0;
        private List<int> IdToCopy = new List<int>();
        private string IdAdres;

        /// <summary>
        /// инициализация грида грузополучателей
        /// </summary>
        public void SupplierGridInit()
        {
            var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД поставщика",
                        Path="ID_POST",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Поставщик",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=30,
                    },
                };
            SupplierGrid.SetColumns(columns);
            SupplierGrid.SetPrimaryKey("ID_POST");
            //SupplierGrid.SetSorting("ID_POST", System.ComponentModel.ListSortDirection.Ascending);
            SupplierGrid.SearchText = SupplierSearchText;
            SupplierGrid.Toolbar = SupplierGridToolbar;
            SupplierGrid.Commands = Commander;
            SupplierGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            SupplierGrid.AutoUpdateInterval = 0;
            SupplierGrid.ItemsAutoUpdate = false;
            SupplierGrid.QueryLoadItems = new RequestData()
            {
                Module = "Delivery",
                Object = "Supplier",
                Action = "List",
                AnswerSectionKey = "ITEMS",
                BeforeRequest = (RequestData rd) =>
                {
                    rd = rd;
                }
            };
            SupplierGrid.OnSelectItem = (row) =>
            {
                ShippingAddressGrid.LoadItems();
            };
            SupplierGrid.Init();
        }

        /// <summary>
        /// инициализация грида адресов доставки
        /// </summary>
        public void ShippingAddressGridInit()
        {
            var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="*",
                        Path="CHECKING",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=4,
                        Editable=true,
                        OnAfterClickAction = (Dictionary<string, string> value, FrameworkElement element) =>
                        {
                            if(value["CHECKING"]=="True") CheckBoxCount++;
                            else CheckBoxCount--;

                            ShippingAddressCopyButton.IsEnabled = CheckBoxCount > 0;

                            return true;
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД адреса",
                        Path="ID_ADRES",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Адрес",
                        Path="ADDRESS",
                        ColumnType=ColumnTypeRef.String,
                        Width2=60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Направление",
                        Path="CITY",
                        ColumnType=ColumnTypeRef.String,
                        Width2=15,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Схема проезда",
                        Path="DRIVEWAY_CHECK",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=4,
                    },
                    //new DataGridHelperColumn
                    //{
                    //    Header="Время отгрузки",
                    //    Path="TM",
                    //    ColumnType=ColumnTypeRef.String,
                    //    Width2=15,
                    //},
                    //new DataGridHelperColumn
                    //{
                    //    Header="Возврат УПД",
                    //    Path="RETURN_UPD_FLAG",
                    //    ColumnType=ColumnTypeRef.Boolean,
                    //    Width2=4,
                    //},
                    //new DataGridHelperColumn
                    //{
                    //    Header="Возврат ТН",
                    //    Path="RETURN_TN_FLAG",
                    //    ColumnType=ColumnTypeRef.Boolean,
                    //    Width2=4,
                    //},
                    new DataGridHelperColumn
                    {
                        Header="Архивный",
                        Path="ARCHIVE_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=4,
                    },
                    //new DataGridHelperColumn
                    //{
                    //    Header="Клиент",
                    //    Path="NAME_CLIENT",
                    //    ColumnType=ColumnTypeRef.String,
                    //    Width2=20,
                    //},
                    new DataGridHelperColumn
                    {
                        Header="Еврофуры недопустимы",
                        Path="EUROTRUCK_BAN_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=4,
                    },
                    //new DataGridHelperColumn
                    //{
                    //    Header="Примечание погрузчику",
                    //    Path="NOTE_LOADER",
                    //    ColumnType=ColumnTypeRef.String,
                    //    Width2=15,
                    //},
                };
            ShippingAddressGrid.SetColumns(columns);
            ShippingAddressGrid.SetPrimaryKey("ID_ADRES");
            //ShippingAddressGrid.SetSorting("ID_ADRES", System.ComponentModel.ListSortDirection.Ascending);
            ShippingAddressGrid.SearchText = ShippingAddressSearchText;
            ShippingAddressGrid.Toolbar = ShippingAddressGridToolbar;
            ShippingAddressGrid.Commands = Commander;
            ShippingAddressGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            ShippingAddressGrid.AutoUpdateInterval = 0;
            ShippingAddressGrid.ItemsAutoUpdate = false;
            ShippingAddressGrid.QueryLoadItems = new RequestData()
            {
                Module = "Delivery",
                Object = "ShippingAddress",
                Action = "List",
                BeforeRequest = (RequestData rd) =>
                    {
                        CheckBoxCount = 0;
                        ShippingAddressCopyButton.IsEnabled = false;

                        rd.Params = new Dictionary<string, string>()
                            {
                                { "ID_POST", SupplierGrid.SelectedItem.CheckGet("ID_POST") },
                            };
                    },
                AfterUpdate = (RequestData rd, ListDataSet ds) =>
                {
                    ShippingAddressGrid.SelectRowByKey(IdAdres);
                }
            };
            ShippingAddressGrid.Init();
        }

        private async void DeleteShippingAddress(Dictionary<string, string> row)
        {
            var message = $"Удалить адрес \"{row.CheckGet("ADDRESS")}\"?";
            if (DialogWindow.ShowDialog(message, "Адрес", "", DialogWindowButtons.NoYes) != true)return;

            var fileName = row.CheckGet("FILE_NAME");
            if (!fileName.IsNullOrEmpty())
            {
                fileName = "\\\\file-server-4\\external_services$\\DeliveryAddress\\" + fileName;
                try { System.IO.File.Delete(fileName); }
                catch { DialogWindow.ShowDialog("Не удалось удалить файл проезда. Попробуйте удалить самостоятельно\n" + fileName, "Адрес", "", DialogWindowButtons.OK); }
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Delivery");
            q.Request.SetParam("Object", "ShippingAddress");
            q.Request.SetParam("Action", "Delete");
            q.Request.SetParam("ID_ADRES", row.CheckGet("ID_ADRES"));
            q.DoQuery();
            if (q.Answer.Status == 0)
            {
                IdToCopy.Remove(row.CheckGet("ID_ADRES").ToInt());
                ShippingAddressPasteButton.IsEnabled = IdToCopy.Count > 0;
                ShippingAddressGrid.SelectRowPrev();
                ShippingAddressGrid.LoadItems();
            }
            else
            {
                q.ProcessError();
            }
        }

        private void ShippingAddressCopyButtonClick(object sender, RoutedEventArgs e)
        {
            IdToCopy.Clear();
            foreach (var item in ShippingAddressGrid.Items)
                if (item["CHECKING"].ToBool()) IdToCopy.Add(item["ID_ADRES"].ToInt());

            ShippingAddressPasteButton.IsEnabled = IdToCopy.Count > 0;
        }

        private void ShippingAddressPasteButtonClick(object sender, RoutedEventArgs e)
        {
            if (IdToCopy.Count > 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Delivery");
                q.Request.SetParam("Object", "ShippingAddress");
                q.Request.SetParam("Action", "Copy");
                q.Request.SetParam("ID_ADRES", JsonConvert.SerializeObject(IdToCopy));
                q.Request.SetParam("ID_POST", SupplierGrid.SelectedItem["ID_POST"]);

                q.DoQuery();
                if (q.Answer.Status == 0)
                {
                    bool succesfulFlag = false;

                    var result = JsonConvert.DeserializeObject<List<int>>(q.Answer.Data);
                    if (result != null)
                    {
                        if (result != null && result.Count == IdToCopy.Count)
                        {
                            succesfulFlag = true;
                        }
                    }

                    if (succesfulFlag)
                    {
                        ShippingAddressGrid.LoadItems();
                    }
                    else
                    {
                        DialogWindow.ShowDialog("Ошибка вставки адресов", "Адрес");
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        private string GetFileName()
        {
            var fileName = ShippingAddressGrid.SelectedItem.CheckGet("FILE_NAME");
            if (fileName.IsNullOrEmpty())
            {
                fileName = ShippingAddressGrid.SelectedItem.CheckGet("DRIVEWAY");
            }
            else
            {
                fileName = "\\\\file-server-4\\external_services$\\DeliveryAddress\\" + fileName;
            }

            return fileName;
        }
    }
}
