using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Microsoft.Office.Interop.Excel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Полки стеллажей
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class WaterhouseRackShelf : ControlBase
    {
        public WaterhouseRackShelf()
        {
            ControlTitle = "Полки стеллажей";
            RoleName = "[erp]warehouse_control";
            DocumentationUrl = "/doc/l-pack-erp/";
            InitializeComponent();

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnKeyPressed = (System.Windows.Input.KeyEventArgs e) =>
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
                SetDefaults();
                RackShelfGridInit();
                StorageGridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                RackShelfGrid.Destruct();
                StorageGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                RackShelfGrid.ItemsAutoUpdate = true;
                RackShelfGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                RackShelfGrid.ItemsAutoUpdate = false;
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
                    ButtonControl = RefresRackShelfButton,
                    ButtonName = "RefresRackShelfButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        Refresh();
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
                    ButtonName = "HelpButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    HotKey = "F1",
                    Action = () =>
                    {
                        Central.ShowHelp(DocumentationUrl);
                    },
                });
            }

            Commander.SetCurrentGridName("RackShelfGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "add_rack_shelf",
                    Title = "Добавить",
                    Group = "rack_shelf_grid_default",
                    Enabled = true,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = AddRackShelfButton,
                    ButtonName = "AddRackShelfButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        AddRackShelf();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "edit_rack_shelf",
                    Title = "Изменить",
                    Group = "rack_shelf_grid_default",
                    Enabled = true,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = EditRackShelfButton,
                    ButtonName = "EditRackShelfButton",
                    HotKey = "Return|DoubleCLick",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        EditRackShelf();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (RackShelfGrid != null && RackShelfGrid.Items != null && RackShelfGrid.Items.Count > 0)
                        {
                            if (RackShelfGrid.SelectedItem != null && RackShelfGrid.SelectedItem.Count > 0)
                            {
                                if (!string.IsNullOrEmpty(RackShelfGrid.SelectedItem.CheckGet("WMRH_ID")))
                                {
                                    result = true;
                                }
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "delete_rack_shelf",
                    Title = "Удалить",
                    Group = "rack_shelf_grid_default",
                    Enabled = true,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = DeleteRackShelfButton,
                    ButtonName = "DeleteRackShelfButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        DeleteRackShelf();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (RackShelfGrid != null && RackShelfGrid.Items != null && RackShelfGrid.Items.Count > 0)
                        {
                            if (RackShelfGrid.SelectedItem != null && RackShelfGrid.SelectedItem.Count > 0)
                            {
                                if (!string.IsNullOrEmpty(RackShelfGrid.SelectedItem.CheckGet("WMRH_ID")))
                                {
                                    result = true;
                                }
                            }
                        }

                        return result;
                    },
                });
            }

            Commander.SetCurrentGridName("StorageGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "add_storage_rack_shelf",
                    Title = "Добавить",
                    Group = "storage_grid_default",
                    Enabled = true,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = AddStorageRackShelfButton,
                    ButtonName = "AddStorageRackShelfButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        AddStorageRackShelf();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (RackShelfGrid != null && RackShelfGrid.Items != null && RackShelfGrid.Items.Count > 0)
                        {
                            if (RackShelfGrid.SelectedItem != null && RackShelfGrid.SelectedItem.Count > 0)
                            {
                                if (!string.IsNullOrEmpty(RackShelfGrid.SelectedItem.CheckGet("WMRH_ID")))
                                {
                                    if (StorageGrid != null && StorageGrid.Items != null && StorageGrid.Items.Count > 0)
                                    {
                                        if (StorageGrid.SelectedItem != null && StorageGrid.SelectedItem.Count > 0)
                                        {
                                            if (StorageGrid.SelectedItem.CheckGet("STATUS").ToInt() == 2)
                                            {
                                                result = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "edit_storage_rack_shelf",
                    Title = "Изменить",
                    Group = "storage_grid_default",
                    Enabled = true,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = EditStorageRackShelfButton,
                    ButtonName = "EditStorageRackShelfButton",
                    HotKey = "DoubleCLick",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        EditStorageRackShelf();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (RackShelfGrid != null && RackShelfGrid.Items != null && RackShelfGrid.Items.Count > 0)
                        {
                            if (RackShelfGrid.SelectedItem != null && RackShelfGrid.SelectedItem.Count > 0)
                            {
                                if (!string.IsNullOrEmpty(RackShelfGrid.SelectedItem.CheckGet("WMRH_ID")))
                                {
                                    if (StorageGrid != null && StorageGrid.Items != null && StorageGrid.Items.Count > 0)
                                    {
                                        if (StorageGrid.SelectedItem != null && StorageGrid.SelectedItem.Count > 0)
                                        {
                                            if (StorageGrid.SelectedItem.CheckGet("STATUS").ToInt() == 1)
                                            {
                                                result = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "delete_storage_rack_shelf",
                    Title = "Удалить",
                    Group = "storage_grid_default",
                    Enabled = true,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = DeleteStorageRackShelfButton,
                    ButtonName = "DeleteStorageRackShelfButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        DeleteStorageRackShelf();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (RackShelfGrid != null && RackShelfGrid.Items != null && RackShelfGrid.Items.Count > 0)
                        {
                            if (RackShelfGrid.SelectedItem != null && RackShelfGrid.SelectedItem.Count > 0)
                            {
                                if (!string.IsNullOrEmpty(RackShelfGrid.SelectedItem.CheckGet("WMRH_ID")))
                                {
                                    if (StorageGrid != null && StorageGrid.Items != null && StorageGrid.Items.Count > 0)
                                    {
                                        if (StorageGrid.SelectedItem != null && StorageGrid.SelectedItem.Count > 0)
                                        {
                                            if (StorageGrid.SelectedItem.CheckGet("STATUS").ToInt() == 1)
                                            {
                                                result = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        return result;
                    },
                });
            }

            Commander.Init(this);
        }

        private ListDataSet RackShelfGridDataSet { get; set; }

        private ListDataSet StorageGridDataSet { get; set; }

        public void Refresh()
        {
            RackShelfGrid.LoadItems();
        }

        public void SetDefaults()
        {
            RackShelfGridDataSet = new ListDataSet();
            StorageGridDataSet = new ListDataSet();
        }

        /// <summary>
        /// Инициализация грида полок
        /// </summary>
        public void RackShelfGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид полки",
                        Path="WMRH_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="RACK_SHELF_NUM",
                        ColumnType=ColumnTypeRef.String,
                        Width2=12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Тип",
                        Path="SHELF_TYPE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=20,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ряд",
                        Path="ROW_NUM",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Секция",
                        Path="RACK_SECTION_NUM",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Уровень",
                        Path="LEVEL_NUM",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Склад",
                        Path="WAREHOUSE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вместимость, кг.",
                        Path="WEIGHT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=11,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Хранилищ, шт.",
                        Description = "Количество хранилищ, размещённое на полке",
                        Path="STORAGE_COUNT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид типа полки",
                        Path="WRST_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид ряда",
                        Path="WMRO_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид секции",
                        Path="WMRS_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид уровня",
                        Path="WMLE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид склада",
                        Path="WMWA_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                };
                RackShelfGrid.SetColumns(columns);
                RackShelfGrid.SearchText = RackShelfSearchBox;
                RackShelfGrid.OnLoadItems = RackShelfGridLoadItems;
                RackShelfGrid.SetPrimaryKey("WMRH_ID");
                RackShelfGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                RackShelfGrid.AutoUpdateInterval = 60;
                RackShelfGrid.Toolbar = RackShelfGridToolbar;

                RackShelfGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem != null && selectedItem.Count > 0 && RackShelfGrid.Items.FirstOrDefault(x => x.CheckGet("WMRH_ID").ToInt() == selectedItem.CheckGet("WMRH_ID").ToInt()) != null)
                    {
                        StorageGridLoadItems();
                    }
                    else
                    {
                        RackShelfGrid.SelectRowFirst();
                    }
                };

                RackShelfGrid.Commands = Commander;
                RackShelfGrid.UseProgressSplashAuto = false;
                RackShelfGrid.Init();
            }
        }

        public async void RackShelfGridLoadItems()
        {
            if (RackShelfGrid != null && RackShelfGrid.SelectedItem != null && RackShelfGrid.SelectedItem.Count > 0 && RackShelfGrid.Commands != null)
            {
                RackShelfGrid.Commands.Message = new ItemMessage() { Action = "refresh", Message = $"{RackShelfGrid.SelectedItem.CheckGet("WMRH_ID")}" };
            }

            var p = new Dictionary<string, string>();
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "RackShelf");
            q.Request.SetParam("Action", "List");
            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            RackShelfGridDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    RackShelfGridDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            else
            {
                q.ProcessError();
            }
            RackShelfGrid.UpdateItems(RackShelfGridDataSet);
        }

        public void AddRackShelf()
        {
            var i = new RackShelf();
            i.Show();
        }

        public void EditRackShelf()
        {
            var i = new RackShelf();
            i.RackShelfId = RackShelfGrid.SelectedItem.CheckGet("WMRH_ID").ToInt();
            i.Show();
        }

        public void DeleteRackShelf()
        {
            var msg = "";
            msg = msg.Append($"Удалить полку?");
            msg = msg.Append($"{RackShelfGrid.SelectedItem.CheckGet("RACK_SHELF_NUM")}", true);

            var d = new DialogWindow($"{msg}", "Удаление", "", DialogWindowButtons.NoYes);
            if ((bool)d.ShowDialog())
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("WMRH_ID", RackShelfGrid.SelectedItem.CheckGet("WMRH_ID"));
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Warehouse");
                q.Request.SetParam("Object", "RackShelf");
                q.Request.SetParam("Action", "Delete");
                q.Request.SetParams(p);

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                        {
                            if (!string.IsNullOrEmpty( ds.Items.First().CheckGet("ID")))
                            {
                                Refresh();
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

        public void StorageGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид хранилища",
                        Path="WMST_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=11,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Хранилище",
                        Path="NUM",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Полка",
                        Path="RACK_SHELF_NUM",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Положение",
                        Path="POSITION_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Path="WSRS_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид положения",
                        Path="POSITION",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид полки",
                        Path="WMRH_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="STATUS",
                        Path="STATUS",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                };
                StorageGrid.SetColumns(columns);
                StorageGrid.SetPrimaryKey("WMST_ID");
                StorageGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                StorageGrid.AutoUpdateInterval = 0;
                StorageGrid.SearchText = StorageSearchBox;

                // цветовая маркировка строк
                StorageGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                {
                    // Цвета фона строк
                    {
                        DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = string.Empty;

                            int status = row.CheckGet("STATUS").ToInt();
                            switch(status)
                            {
                                case 1: // Хранилище привязано к выбраннй полке
                                    color = HColor.Green;
                                    break;

                                case 3: // Хранилище привязано к другой полке
                                    color = HColor.Red;
                                    break;

                                case 2: // Хранилище не привязано ни к одной полке
                                default:
                                    break;
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },
                };

                StorageGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem != null && selectedItem.Count > 0 && StorageGrid.Items.FirstOrDefault(x => x.CheckGet("WMST_ID").ToInt() == selectedItem.CheckGet("WMST_ID").ToInt()) != null)
                    {
                    }
                    else
                    {
                        StorageGrid.SelectRowFirst();
                    }
                };

                StorageGrid.Commands = Commander;
                StorageGrid.UseProgressSplashAuto = false;
                StorageGrid.Init();
            }
        }

        public async void StorageGridLoadItems()
        {
            if (StorageGrid != null && StorageGrid.SelectedItem != null && StorageGrid.SelectedItem.Count > 0 && StorageGrid.Commands != null)
            {
                StorageGrid.Commands.Message = new ItemMessage() { Action = "refresh", Message = $"{StorageGrid.SelectedItem.CheckGet("WMST_ID")}" };
            }

            if (RackShelfGrid != null && RackShelfGrid.SelectedItem != null && RackShelfGrid.SelectedItem.Count > 0)
            {
                var p = new Dictionary<string, string>();
                p.Add("WMLE_ID", RackShelfGrid.SelectedItem.CheckGet("WMLE_ID"));
                p.Add("WMRO_ID", RackShelfGrid.SelectedItem.CheckGet("WMRO_ID"));
                p.Add("WMWA_ID", RackShelfGrid.SelectedItem.CheckGet("WMWA_ID"));
                p.Add("WMRH_ID", RackShelfGrid.SelectedItem.CheckGet("WMRH_ID"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Warehouse");
                q.Request.SetParam("Object", "RackShelf");
                q.Request.SetParam("Action", "ListStorage");
                q.Request.SetParams(p);
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                StorageGridDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        StorageGridDataSet = ListDataSet.Create(result, "ITEMS");
                    }
                }
                else
                {
                    q.ProcessError();
                }
                StorageGrid.UpdateItems(StorageGridDataSet);
            }
        }

        public void AddStorageRackShelf()
        {
            ProcessStorageRackShelf("create", "", 
                StorageGrid.SelectedItem.CheckGet("WMST_ID"), RackShelfGrid.SelectedItem.CheckGet("WMRH_ID"),
                StorageGrid.SelectedItem.CheckGet("NUM"), RackShelfGrid.SelectedItem.CheckGet("RACK_SHELF_NUM"));
        }

        public void EditStorageRackShelf()
        {
            ProcessStorageRackShelf("edit", StorageGrid.SelectedItem.CheckGet("WSRS_ID"),
                StorageGrid.SelectedItem.CheckGet("WMST_ID"), RackShelfGrid.SelectedItem.CheckGet("WMRH_ID"),
                StorageGrid.SelectedItem.CheckGet("NUM"), RackShelfGrid.SelectedItem.CheckGet("RACK_SHELF_NUM"));
        }

        public void DeleteStorageRackShelf()
        {
            ProcessStorageRackShelf("delete", StorageGrid.SelectedItem.CheckGet("WSRS_ID"),
                StorageGrid.SelectedItem.CheckGet("WMST_ID"), RackShelfGrid.SelectedItem.CheckGet("WMRH_ID"),
                StorageGrid.SelectedItem.CheckGet("NUM"), RackShelfGrid.SelectedItem.CheckGet("RACK_SHELF_NUM"));
        }

        public void ProcessStorageRackShelf(string mode = "create", string id = "", 
            string storageId = "", string rackShelfId = "",
            string storageName = "", string rackShelfName = "") 
        {
            string title = "";
            if (mode == "create")
            {
                title = "Добавление хранилища на полку стеллажа";
            }
            else
            {
                title = "Изменение хранилища на полке стеллажа";
            }

            var storageRackShelf = new FormDialog()
            {
                RoleName = this.RoleName,
                FrameName = "StorageRackShelf",
                Title = title,
                Fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="RACK_SHELF_NUM",
                        Description = "Полка",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="NUM",
                        Description = "Хранилище",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="POSITION",
                        Description = "Положение",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Width = 152,
                        ControlType = "SelectBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                },
            };

            storageRackShelf["POSITION"].OnAfterCreate += (FrameworkElement control) =>
            {
                var box = control as SelectBox;
                box.Items = new Dictionary<string, string>()
                {
                    {"1", "Левая у балки"},
                    {"2", "Центральная"},
                    {"3", "Правая у балки"},
                };
                box.UpdateListItems(box.Items);

                control.IsEnabled = true;
            };

            storageRackShelf["RACK_SHELF_NUM"].OnAfterCreate += (FrameworkElement control) =>
            {
                (control as System.Windows.Controls.TextBox).IsReadOnly = true;
            };

            storageRackShelf["NUM"].OnAfterCreate += (FrameworkElement control) =>
            {
                (control as System.Windows.Controls.TextBox).IsReadOnly = true;
            };

            storageRackShelf.QueryGet = new RequestData()
            {
                Module = "Warehouse",
                Object = "StorageRackShelf",
                Action = "Get",
            };

            storageRackShelf.AfterGet += (FormDialog fd) =>
            {
                fd.Open();
            };

            storageRackShelf.QuerySave = new RequestData()
            {
                Module = "Warehouse",
                Object = "StorageRackShelf",
                Action = "Save",
            };

            storageRackShelf.BeforeGet += (Dictionary<string, string> parameters) =>
            {
                bool resume = true;
                if (string.IsNullOrEmpty(id))
                {
                    resume = false;

                    Dictionary<string, string> d = new Dictionary<string, string>();
                    d.CheckAdd("RACK_SHELF_NUM", rackShelfName);
                    d.CheckAdd("NUM", storageName);
                    storageRackShelf.SetValues(d);
                    storageRackShelf.Open();
                }
                return resume;
            };

            storageRackShelf.BeforeSave += (Dictionary<string, string> parameters) =>
            {
                parameters.CheckAdd("WMST_ID", storageId);
                parameters.CheckAdd("WMRH_ID", rackShelfId);

                return true;
            };

            storageRackShelf.BeforeDelete += (Dictionary<string, string> p) =>
            {
                bool resume = false;

                var msg = "";
                {
                    msg = msg.Append($"Удалить хранилище {p.CheckGet("NUM")} с полки {p.CheckGet("RACK_SHELF_NUM")}?");
                }

                var d = new DialogWindow($"{msg}", "Удаление", "", DialogWindowButtons.NoYes);
                if ((bool)d.ShowDialog())
                {
                    resume = true;
                }

                return resume;
            };

            storageRackShelf.QueryDelete = new RequestData()
            {
                Module = "Warehouse",
                Object = "StorageRackShelf",
                Action = "Delete",
            };

            storageRackShelf.AfterUpdate += (FormDialog fd) =>
            {
                fd.Hide();

                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = "WarehouseControl",
                    ReceiverName = this.ControlName,
                    SenderName = fd.FrameName,
                    Action = "Refresh",
                    Message = $"{fd.InsertId}",
                });
            };

            storageRackShelf.PrimaryKey = "WSRS_ID";
            storageRackShelf.PrimaryKeyValue = id;
            storageRackShelf.Commander.Init(storageRackShelf);
            storageRackShelf.Run(mode);
        }
    }
}
