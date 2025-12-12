using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Интерфейс управления справочником ТМЦ
    /// </summary>
    /// <author>eletskikh_ya</author>
    public partial class CatalogInventory : ControlBase
    {
        public CatalogInventory()
        {
            ControlTitle = "Справочник ТМЦ";
            DocumentationUrl = "/doc/l-pack-erp/";
            RoleName = "[erp]warehouse_directory";
            InitializeComponent();

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

            //конструктор, будет вызван, когда объект создается
            //здесь создаются все внутренние структуры
            //впервые этот коллбэк будет вызван, когда данный таб станет активным
            //впервые (до этих пор, никакая работа внутри не происходит, что экономит ресурсы)
            OnLoad = () =>
            {
                InventoryGridInit();
                SetDefaults();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                InventoryGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                InventoryGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
            };

            {
                Commander.Add(new CommandItem()
                {
                    Name = "refresh",
                    Group = "main",
                    Enabled = true,
                    Title = "Обновить",
                    Description = "Обновить данные",
                    ButtonUse = true,
                    ButtonControl = RefreshButton,
                    ButtonName = "RefreshButton",
                    Action = () =>
                    {
                        Refresh();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "create_inventory",
                    Group = "main",
                    Enabled = true,
                    Title = "Создать",
                    Description = "Создать ТМЦ",
                    ButtonUse = true,
                    ButtonControl = AddButton,
                    ButtonName = "AddButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        CreateInventory();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "help",
                    Group = "main",
                    Enabled = true,
                    Title = "Справка",
                    Description = "Показать справочную информацию",
                    ButtonUse = true,
                    ButtonControl = HelpButton,
                    ButtonName = "HelpButton",
                    HotKey = "F1",
                    Action = () =>
                    {
                        Central.ShowHelp(DocumentationUrl);
                    },
                });
            }

            Commander.SetCurrentGridName("InventoryGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "edit_inventory",
                    Title = "Изменить",
                    Group = "inventory_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = EditButton,
                    ButtonName = "EditButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        EditInventory();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (InventoryGrid != null && InventoryGrid.Items != null && InventoryGrid.Items.Count > 0)
                        {
                            if (InventoryGrid.SelectedItem != null && InventoryGrid.SelectedItem.Count > 0)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "delete_inventory",
                    Title = "Удалить",
                    Group = "inventory_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = DeleteButton,
                    ButtonName = "DeleteButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        DeleteInventory();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (InventoryGrid != null && InventoryGrid.Items != null && InventoryGrid.Items.Count > 0)
                        {
                            if (InventoryGrid.SelectedItem != null && InventoryGrid.SelectedItem.Count > 0)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
            }

            Commander.Init(this);
        }

        private ListDataSet InventoryDataSet { get; set; }

        private void SetDefaults()
        {
            InventoryDataSet = new ListDataSet();

            Zone.Items.Add("-1", "Номенклатура");
            FormHelper.ComboBoxInitHelper(Zone, "Warehouse", "Zone", "List", "WMZO_ID", "ZONE_FULL_NAME", null, true);
            Zone.SelectedItem = new KeyValuePair<string, string>("-1", "Номенклатура");
        }

        /// <summary>
        /// настройка отображения грида
        /// </summary>
        private void InventoryGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="WMII_ID",
                        Doc="ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="NAME",
                        Doc="Наименование",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 46
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ед. изм.",
                        Path="SHORT_NAME",
                        Doc="Единица измерения",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 7
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Path="QTY",
                        Doc="Количество по умолчанию",
                        ColumnType=ColumnTypeRef.Double,
                        Width2 = 10,
                        Format = "N0"
                    },
                    new DataGridHelperColumn
                    {
                        Header="Cрок хранения",
                        Path="SHELF_LIFE",
                        Doc="Срок хранения данного вида ТМЦ, месяцы",
                        ColumnType=ColumnTypeRef.Double,
                        Width2 = 10,
                        Format = "N0"
                    },
                    new DataGridHelperColumn
                    {
                        Header="Внешний ИД",
                        Path="OUTER_ID",
                        Doc="Внешний ИД",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 11
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид продукции",
                        Path="PRODUCT_ID",
                        Doc="Идентификатор продукции",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Включить",
                        Path="ENABLED",
                        ColumnType=ColumnTypeRef.Boolean,
                        Editable=true,
                        OnClickAction=(row,el) =>
                        {
                            if(Zone.SelectedItem.Key != null)
                            {
                                if(Zone.SelectedItem.Key == "-1")
                                {
                                    return false;
                                }
                                else
                                {
                                    ChangeInventoryState(!row.CheckGet("ENABLED").ToBool(), Zone.SelectedItem.Key.ToInt());
                                    return true;
                                }
                            }

                            return null;
                        },
                        Width2 = 9
                    },

                    new DataGridHelperColumn
                    {
                        Header="UNIT_ID",
                        Path="UNIT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },
                };
                InventoryGrid.SetColumns(columns);
                InventoryGrid.SetPrimaryKey("WMII_ID");
                InventoryGrid.SearchText = ItemsSearchBox;
                InventoryGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                InventoryGrid.Toolbar = ItemsGridToolbar;
                InventoryGrid.AutoUpdateInterval = 0;

                InventoryGrid.OnDblClick = selectedItem =>
                {
                    Edit(selectedItem["WMII_ID"].ToInt());
                };

                //данные грида
                InventoryGrid.OnLoadItems = InventoryGridLoadItems;

                InventoryGrid.Commands = Commander;

                InventoryGrid.Init();
            }
        }

        private async void InventoryGridLoadItems()
        {
            var p = new Dictionary<string, string>();

            if (Zone.SelectedItem.Key != null)
            {
                p.Add("WMZO_ID", Zone.SelectedItem.Key);
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "Inventory");
            q.Request.SetParam("Action", "List");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            InventoryDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    InventoryDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            InventoryGrid.UpdateItems(InventoryDataSet);
        }

        public void Refresh()
        {
            InventoryGridLoadItems();
        }

        public void CreateInventory()
        {
            Edit();
        }

        public void EditInventory()
        {
            if (InventoryGrid != null && InventoryGrid.SelectedItem != null && InventoryGrid.SelectedItem.Count > 0)
            {
                Edit(InventoryGrid.SelectedItem["WMII_ID"].ToInt());
            }
        }

        public void Edit(int id = 0)
        {
            if (Central.Navigator.GetRoleLevel("[erp]warehouse_directory") >= Role.AccessMode.FullAccess)
            {
                var editEnvintory = new FormExtend()
                {
                    FrameName = "Inventory",
                    ID = "WMII_ID",
                    Id = id,
                    Title = $"ТМЦ {id}",

                    QueryGet = new FormExtend.RequestData()
                    {
                        Module = "Warehouse",
                        Object = "Inventory",
                        Action = "Get"
                    },

                    QuerySave = new FormExtend.RequestData()
                    {
                        Module = "Warehouse",
                        Object = "Inventory",
                        Action = "Save"
                    },

                    Fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="NAME",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Description = "Наименование: *",
                        ControlType = "TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                            { FormHelperField.FieldFilterRef.MaxLen, 128 },
                        },

                        ControlWidth = FormHelperField.ControlWidthDegree.Large
                    },
                    new FormHelperField()
                    {
                        Path="UNIT_ID",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Description = "Единица измерения: *",
                        ControlType="SelectBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        },

                        ControlWidth = FormHelperField.ControlWidthDegree.Medium
                    },
                    new FormHelperField()
                    {
                        Path="QTY",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Description = "Количество:  ",
                        ControlType = "TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{

                            { FormHelperField.FieldFilterRef.DigitOnly, null},
                        },

                        ControlWidth = FormHelperField.ControlWidthDegree.Small
                    },
                    new FormHelperField()
                    {
                        Path="SHELF_LIFE",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Description = "Срок хранения, мес.:  ",
                        ControlType = "TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{

                            { FormHelperField.FieldFilterRef.DigitOnly, null},
                        },

                        ControlWidth = FormHelperField.ControlWidthDegree.Small
                    },
                    new FormHelperField()
                    {
                        Path="OUTER_ID",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Description = "Внешний ИД:  ",
                        ControlType = "TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.MaxLen, 128 },
                        },

                        ControlWidth = FormHelperField.ControlWidthDegree.Medium
                    },

                }
                };

                editEnvintory["UNIT_ID"].OnAfterCreate += (control) =>
                {
                    FormHelper.ComboBoxInitHelper(control as SelectBox, "Warehouse", "Item", "ListUnit", "UNIT_ID", "SHORT_NAME", null, true);
                };

                editEnvintory.OnAfterSave += (id, result) =>
                {
                    InventoryGrid.LoadItems();
                };

                editEnvintory.Show();
            }
        }

        private async void DeleteInventory()
        {
            if (InventoryGrid != null && InventoryGrid.SelectedItem != null && InventoryGrid.SelectedItem.Count > 0)
            {
                var dw = new DialogWindow($"Вы действительно хотите удалить ТМЦ {InventoryGrid.SelectedItem.CheckGet("NAME")}?", "Удаление тмц", "Подтверждение удаления тмц", DialogWindowButtons.NoYes);
                if (dw.ShowDialog() == true)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Warehouse");
                    q.Request.SetParam("Object", "Inventory");
                    q.Request.SetParam("Action", "Delete");
                    q.Request.SetParams(InventoryGrid.SelectedItem);

                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    if (q.Answer.Status == 0)
                    {
                        Refresh();
                    }
                }
            }
        }

        private async void ChangeInventoryState(bool flag, int ZoneId)
        {
            var p = new Dictionary<string, string>();

            p.Add("ATTACHE", flag ? "1" : "0");
            p.Add("WMII_ID", InventoryGrid.SelectedItem.CheckGet("WMII_ID"));
            p.Add("WMZO_ID", ZoneId.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "Inventory");
            q.Request.SetParam("Action", "SaveByZone");
            q.Request.SetParams(p);

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
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds.Items.Count > 0)
                    {
                        int id = ds.Items[0].CheckGet("ID").ToInt();
                        Refresh();
                    }
                }
            }
        }

        private void StorageFilter_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (InventoryGrid != null && InventoryGrid.Items != null && InventoryGrid.Items.Count > 0)
            {
                Refresh();
            }
        }
    }
}
