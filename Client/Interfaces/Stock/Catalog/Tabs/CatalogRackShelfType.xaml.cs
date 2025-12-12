using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
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
    /// Тип полки стеллажа
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class CatalogRackShelfType : ControlBase
    {
        public CatalogRackShelfType()
        {
            ControlTitle = "Тип полки стеллажа";
            RoleName = "[erp]warehouse_directory";
            DocumentationUrl = "/doc/l-pack-erp/";
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
                SetDefaults();
                RackShelfTypeGridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                RackShelfTypeGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                RackShelfTypeGrid.ItemsAutoUpdate = true;
                RackShelfTypeGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                RackShelfTypeGrid.ItemsAutoUpdate = false;
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
                    ButtonControl = RefreshRackShelfTypeButton,
                    ButtonName = "RefreshRackShelfTypeButton",
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

            Commander.SetCurrentGridName("RackShelfTypeGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "add_rack_shelf_type",
                    Title = "Добавить",
                    Group = "rack_shelf_type_grid_default",
                    Enabled = true,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = AddRackShelfTypeButton,
                    ButtonName = "AddRackShelfTypeButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        AddRackShelfType();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "edit_rack_shelf_type",
                    Title = "Изменить",
                    Group = "rack_shelf_type_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = EditRackShelfTypeButton,
                    ButtonName = "EditRackShelfTypeButton",
                    HotKey = "Return|DoubleCLick",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        EditRackShelfType();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (RackShelfTypeGrid != null && RackShelfTypeGrid.Items != null && RackShelfTypeGrid.Items.Count > 0)
                        {
                            if (RackShelfTypeGrid.SelectedItem != null && RackShelfTypeGrid.SelectedItem.Count > 0)
                            {
                                if (!string.IsNullOrEmpty(RackShelfTypeGrid.SelectedItem.CheckGet("WRST_ID")))
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
                    Name = "delete_rack_shelf_type",
                    Title = "Удалить",
                    Group = "rack_shelf_type_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = DeleteRackShelfTypeButton,
                    ButtonName = "DeleteRackShelfTypeButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        DeleteRackShelfType();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (RackShelfTypeGrid != null && RackShelfTypeGrid.Items != null && RackShelfTypeGrid.Items.Count > 0)
                        {
                            if (RackShelfTypeGrid.SelectedItem != null && RackShelfTypeGrid.SelectedItem.Count > 0)
                            {
                                if (!string.IsNullOrEmpty(RackShelfTypeGrid.SelectedItem.CheckGet("WRST_ID")))
                                {
                                    result = true;
                                }
                            }
                        }

                        return result;
                    },
                });
            }

            Commander.Init(this);
        }

        private ListDataSet RackShelfTypeGridDataSet { get; set; }

        public void SetDefaults()
        {
            RackShelfTypeGridDataSet = new ListDataSet();
        }

        /// <summary>
        /// Инициализация грида типов полок стеллажа
        /// </summary>
        public void RackShelfTypeGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид типа полки",
                        Path="WRST_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="SHELF_TYPE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=20,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Максимальный вес",
                        Path="WEIGHT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=16,
                    },
                };
                RackShelfTypeGrid.SetColumns(columns);
                RackShelfTypeGrid.SearchText = RackShelfTypeSearchBox;
                RackShelfTypeGrid.OnLoadItems = RackShelfTypeGridLoadItems;
                RackShelfTypeGrid.SetPrimaryKey("WRST_ID");
                RackShelfTypeGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                RackShelfTypeGrid.AutoUpdateInterval = 60;
                RackShelfTypeGrid.Toolbar = RackShelfTypeGridToolbar;

                RackShelfTypeGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem != null && selectedItem.Count > 0 && RackShelfTypeGrid.Items.FirstOrDefault(x => x.CheckGet("WRST_ID").ToInt() == selectedItem.CheckGet("WRST_ID").ToInt()) != null)
                    {
                    }
                    else
                    {
                        RackShelfTypeGrid.SelectRowFirst();
                    }
                };

                RackShelfTypeGrid.Commands = Commander;
                RackShelfTypeGrid.UseProgressSplashAuto = false;
                RackShelfTypeGrid.Init();
            }
        }

        public async void RackShelfTypeGridLoadItems()
        {
            if (RackShelfTypeGrid != null && RackShelfTypeGrid.SelectedItem != null && RackShelfTypeGrid.SelectedItem.Count > 0 && RackShelfTypeGrid.Commands != null)
            {
                RackShelfTypeGrid.Commands.Message = new ItemMessage() { Action = "refresh", Message = $"{RackShelfTypeGrid.SelectedItem.CheckGet("WRST_ID")}" };
            }

            var p = new Dictionary<string, string>();
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "RackShelfType");
            q.Request.SetParam("Action", "List");
            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            RackShelfTypeGridDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    RackShelfTypeGridDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            else
            {
                q.ProcessError();
            }
            RackShelfTypeGrid.UpdateItems(RackShelfTypeGridDataSet);
        }

        public void Refresh()
        {
            RackShelfTypeGrid.LoadItems();
        }

        public void AddRackShelfType()
        {
            ProcessRackShelfType();
        }

        public void EditRackShelfType()
        {
            ProcessRackShelfType("edit", RackShelfTypeGrid.SelectedItem.CheckGet("WRST_ID"));
        }

        public void DeleteRackShelfType()
        {
            ProcessRackShelfType("delete", RackShelfTypeGrid.SelectedItem.CheckGet("WRST_ID"));
        }

        public void ProcessRackShelfType(string mode = "create", string id = "")
        {
            string title = "";
            if (mode == "create")
            {
                title = "Новый тип полки стеллажа";
            }
            else
            {
                title = "Тип полки стеллажа";
            }

            var rackShelfTypeItem = new FormDialog()
            {
                RoleName = this.RoleName,
                FrameName = "RackShelfType",
                Title = title,
                Fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="SHELF_TYPE",
                        Description = "Наименование типа",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                    new FormHelperField()
                    {
                        Path="WEIGHT",
                        Description = "Максимальный вес",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.DigitOnly, null },
                        },
                        Comments = new List<FormHelperComment>
                        {
                            new FormHelperComment()
                            {
                                Name = "WEIGHT_UNIT_NAME",
                                Content = "кг.",
                            }
                        },
                    },
                },
            };

            rackShelfTypeItem.QueryGet = new RequestData()
            {
                Module = "Warehouse",
                Object = "RackShelfType",
                Action = "Get",
            };

            rackShelfTypeItem.AfterGet += (FormDialog fd) =>
            {
                fd.Open();
            };

            rackShelfTypeItem.QuerySave = new RequestData()
            {
                Module = "Warehouse",
                Object = "RackShelfType",
                Action = "Save",
            };

            rackShelfTypeItem.BeforeDelete += (Dictionary<string, string> p) =>
            {
                bool resume = false;

                var msg = "";
                {
                    msg = msg.Append($"Удалить тип полки стеллажа?");
                    msg = msg.Append($"{p.CheckGet("SHELF_TYPE")}", true);
                }

                var d = new DialogWindow($"{msg}", "Удаление", "", DialogWindowButtons.NoYes);
                if ((bool)d.ShowDialog())
                {
                    resume = true;
                }

                return resume;
            };

            rackShelfTypeItem.QueryDelete = new RequestData()
            {
                Module = "Warehouse",
                Object = "RackShelfType",
                Action = "Delete",
            };

            rackShelfTypeItem.AfterUpdate += (FormDialog fd) =>
            {
                fd.Hide();

                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = "CatalogRack",
                    ReceiverName = this.ControlName,
                    SenderName = fd.FrameName,
                    Action = "Refresh",
                    Message = $"{fd.InsertId}",
                });
            };

            rackShelfTypeItem.PrimaryKey = "WRST_ID";
            rackShelfTypeItem.PrimaryKeyValue = id;
            rackShelfTypeItem.Commander.Init(rackShelfTypeItem);
            rackShelfTypeItem.Run(mode);
        }
    }
}
