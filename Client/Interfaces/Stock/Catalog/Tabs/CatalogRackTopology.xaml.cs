using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Stock._WaterhouseControl;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
    /// Топология стеллажей
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class CatalogRackTopology : ControlBase
    {
        public CatalogRackTopology()
        {
            ControlTitle = "Топология стеллажей";
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
                RackAisleGridInit();
                RackSectionGridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                RackAisleGrid.Destruct();
                RackSectionGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                RackAisleGrid.ItemsAutoUpdate = true;
                RackAisleGrid.Run();

                RackSectionGrid.ItemsAutoUpdate = true;
                RackSectionGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                RackAisleGrid.ItemsAutoUpdate = false;

                RackSectionGrid.ItemsAutoUpdate = false;
            };

            {
                Commander.Add(new CommandItem()
                {
                    Name = "refresh",
                    Group = "main",
                    Enabled = true,
                    Title = "Обновить",
                    Description = "Обновить данные",
                    ButtonUse = false,
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

            Commander.SetCurrentGridName("RackAisleGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "refresh_rack_aisle",
                    Title = "Обновить",
                    Group = "rack_aisle_grid_default",
                    Enabled = true,
                    ButtonUse = true,
                    ButtonControl = RefresRackAisleButton,
                    ButtonName = "RefresRackAisleButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        RackAisleGrid?.LoadItems();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "add_rack_aisle",
                    Title = "Добавить",
                    Group = "rack_aisle_grid_default",
                    Enabled = true,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = AddRackAisleButton,
                    ButtonName = "AddRackAisleButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        AddRackAisle();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "edit_rack_aisle",
                    Title = "Изменить",
                    Group = "rack_aisle_grid_default",
                    Enabled = true,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = EditRackAisleButton,
                    ButtonName = "EditRackAisleButton",
                    HotKey = "Return|DoubleCLick",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        EditRackAisle();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (RackAisleGrid != null && RackAisleGrid.Items != null && RackAisleGrid.Items.Count > 0)
                        {
                            if (RackAisleGrid.SelectedItem != null && RackAisleGrid.SelectedItem.Count > 0)
                            {
                                if (!string.IsNullOrEmpty(RackAisleGrid.SelectedItem.CheckGet("WMRA_ID")))
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
                    Name = "delete_rack_aisle",
                    Title = "Удалить",
                    Group = "rack_aisle_grid_default",
                    Enabled = true,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = DeleteRackAisleButton,
                    ButtonName = "DeleteRackAisleButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        DeleteRackAisle();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (RackAisleGrid != null && RackAisleGrid.Items != null && RackAisleGrid.Items.Count > 0)
                        {
                            if (RackAisleGrid.SelectedItem != null && RackAisleGrid.SelectedItem.Count > 0)
                            {
                                if (!string.IsNullOrEmpty(RackAisleGrid.SelectedItem.CheckGet("WMRA_ID")))
                                {
                                    result = true;
                                }
                            }
                        }

                        return result;
                    },
                });
            }

            Commander.SetCurrentGridName("RackSectionGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "refresh_rack_section",
                    Title = "Обновить",
                    Group = "rack_section_grid_default",
                    Enabled = true,
                    ButtonUse = true,
                    ButtonControl = RefresRackSectionButton,
                    ButtonName = "RefresRackSectionButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        RackSectionGrid?.LoadItems();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "add_rack_section",
                    Title = "Добавить",
                    Group = "rack_section_grid_default",
                    Enabled = true,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = AddRackSectionButton,
                    ButtonName = "AddRackSectionButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        AddRackSection();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "edit_rack_section",
                    Title = "Изменить",
                    Group = "rack_section_grid_default",
                    Enabled = true,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = EditRackSectionButton,
                    ButtonName = "EditRackSectionButton",
                    HotKey = "DoubleCLick",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        EditRackSection();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (RackSectionGrid != null && RackSectionGrid.Items != null && RackSectionGrid.Items.Count > 0)
                        {
                            if (RackSectionGrid.SelectedItem != null && RackSectionGrid.SelectedItem.Count > 0)
                            {
                                if (!string.IsNullOrEmpty(RackSectionGrid.SelectedItem.CheckGet("WMRS_ID")))
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
                    Name = "delete_rack_section",
                    Title = "Удалить",
                    Group = "rack_section_grid_default",
                    Enabled = true,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = DeleteRackSectionButton,
                    ButtonName = "DeleteRackSectionButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        DeleteRackSection();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (RackSectionGrid != null && RackSectionGrid.Items != null && RackSectionGrid.Items.Count > 0)
                        {
                            if (RackSectionGrid.SelectedItem != null && RackSectionGrid.SelectedItem.Count > 0)
                            {
                                if (!string.IsNullOrEmpty(RackSectionGrid.SelectedItem.CheckGet("WMRS_ID")))
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

        private ListDataSet RackAisleGridDataSet { get; set; }

        private ListDataSet RackSectionGridDataSet { get; set; }

        public void Refresh()
        {
            RackAisleGrid.LoadItems();
            RackSectionGrid.LoadItems();
        }

        public void SetDefaults()
        {
            RackAisleGridDataSet = new ListDataSet();
            RackSectionGridDataSet = new ListDataSet();
        }

        /// <summary>
        /// Инициализация грида проходов
        /// </summary>
        public void RackAisleGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид прохода",
                        Path="WMRA_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="RACK_AISLE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Рекомендуемый проход",
                        Description="Рекомендуемый проход для постановки ГП в стеллаж",
                        Path="RECOMMENDED_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=18,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Ид зоны",
                        Path="WMZO_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид погрузчика",
                        Path="ACCO_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид прохода",
                        Path="IN_WMST_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид прохода",
                        Path="OUT_WMST_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                        Hidden=true,
                    },
                };
                RackAisleGrid.SetColumns(columns);
                RackAisleGrid.SearchText = RackAisleSearchBox;
                RackAisleGrid.OnLoadItems = RackAisleGridLoadItems;
                RackAisleGrid.SetPrimaryKey("WMRA_ID");
                RackAisleGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                RackAisleGrid.AutoUpdateInterval = 60;
                RackAisleGrid.Toolbar = RackAisleGridToolbar;

                RackAisleGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem != null && selectedItem.Count > 0 && RackAisleGrid.Items.FirstOrDefault(x => x.CheckGet("WMRA_ID").ToInt() == selectedItem.CheckGet("WMRA_ID").ToInt()) != null)
                    {
                    }
                    else
                    {
                        RackAisleGrid.SelectRowFirst();
                    }
                };

                RackAisleGrid.Commands = Commander;
                RackAisleGrid.UseProgressSplashAuto = false;
                RackAisleGrid.Init();
            }
        }

        public async void RackAisleGridLoadItems()
        {
            if (RackAisleGrid != null && RackAisleGrid.SelectedItem != null && RackAisleGrid.SelectedItem.Count > 0 && RackAisleGrid.Commands != null)
            {
                RackAisleGrid.Commands.Message = new ItemMessage() { Action = "refresh", Message = $"{RackAisleGrid.SelectedItem.CheckGet("WMRA_ID")}" };
            }

            var p = new Dictionary<string, string>();
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "Rack");
            q.Request.SetParam("Action", "List");
            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            RackAisleGridDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    RackAisleGridDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            else
            {
                q.ProcessError();
            }
            RackAisleGrid.UpdateItems(RackAisleGridDataSet);
        }

        public void AddRackAisle()
        {
            ProcessRackAisle();
        }

        public void EditRackAisle()
        {
            ProcessRackAisle("edit", RackAisleGrid.SelectedItem.CheckGet("WMRA_ID"));
        }

        public void DeleteRackAisle()
        {
            ProcessRackAisle("delete", RackAisleGrid.SelectedItem.CheckGet("WMRA_ID"));
        }

        public FormDialog RackAisleItem { get; set; }

        public void ProcessRackAisle(string mode = "create", string id = "")
        {
            string title = "";
            if (mode == "create")
            {
                title = "Новый проход";
            }
            else
            {
                title = "Проход";
            }

            RackAisleItem = new FormDialog()
            {
                RoleName = this.RoleName,
                FrameName = "RackAisle",
                Title = title,
                Fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="RACK_AISLE",
                        Description = "Проход",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                    new FormHelperField()
                    {
                        Path="WMZO_ID",
                        Description = "Зона хранения",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Width = 250,
                        ControlType = "SelectBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                    new FormHelperField()
                    {
                        Path="RECOMMENDED_FLAG",
                        Description = "Рекомендуемый проход для постановки ГП в стеллаж",
                        FieldType=FormHelperField.FieldTypeRef.Boolean,
                        ControlType="CheckBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },

                    new FormHelperField()
                    {
                        Path="IN_WMST_NAME",
                        Description = "Ячейка IN",
                        Width = 75,
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                        Fillers=new List<FormHelperFiller>
                        {
                            {
                                new FormHelperFiller(){
                                    Name="SelectStorage",
                                    Description="Выбрать ячейку",
                                    Caption="Выбрать",
                                    Action=(FormHelper form)=>
                                    {
                                        var selectCell = new SelectCell();
                                        selectCell.OnSelectedCell += SelectCellForStorageIn;
                                        selectCell.WarehouseSelectBox.SetSelectedItemByKey("1");
                                        selectCell.StorageAreaSelectBox.SetSelectedItemByKey("3");
                                        selectCell.Show();

                                        var result = RackAisleItem["IN_WMST_NAME"].Form.GetValueByPath("IN_WMST_NAME");
                                        return result;
                                    }
                                }
                            }
                        },
                    },
                    new FormHelperField()
                    {
                        Path="IN_WMST_ID",
                        Description = "Ячейка IN",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                        ControlType = "void",
                    },
                    new FormHelperField()
                    {
                        Path="OUT_WMST_NAME",
                        Description = "Ячейка OUT",
                        Width = 75,
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                        Fillers=new List<FormHelperFiller>
                        {
                            {
                                new FormHelperFiller(){
                                    Name="SelectStorage",
                                    Description="Выбрать ячейку",
                                    Caption="Выбрать",
                                    Action=(FormHelper form)=>
                                    {
                                        var selectCell = new SelectCell();
                                        selectCell.OnSelectedCell += SelectCellForStorageOut;
                                        selectCell.WarehouseSelectBox.SetSelectedItemByKey("1");
                                        selectCell.StorageAreaSelectBox.SetSelectedItemByKey("4");
                                        selectCell.Show();

                                        var result = RackAisleItem["OUT_WMST_NAME"].Form.GetValueByPath("OUT_WMST_NAME");
                                        return result;
                                    }
                                }
                            }
                        },
                    },
                    new FormHelperField()
                    {
                        Path="OUT_WMST_ID",
                        Description = "Ячейка OUT",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                        ControlType = "void",
                    },
                },
            };

            RackAisleItem["WMZO_ID"].OnAfterCreate += (FrameworkElement control) =>
            {
                FormHelper.ComboBoxInitHelper(control as SelectBox, "Warehouse", "Zone", "List", "WMZO_ID", "ZONE_FULL_NAME", null, true, true);
                (control as SelectBox).SetSelectedItemByKey("1");
                control.IsEnabled = true;
            };

            RackAisleItem.QueryGet = new RequestData()
            {
                Module = "Warehouse",
                Object = "Rack",
                Action = "Get",
            };

            RackAisleItem.AfterGet += (FormDialog fd) =>
            {
                fd.Open();
            };

            RackAisleItem.QuerySave = new RequestData()
            {
                Module = "Warehouse",
                Object = "Rack",
                Action = "Save",
            };

            RackAisleItem.AfterUpdate += (FormDialog fd) =>
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

            RackAisleItem.BeforeDelete += (Dictionary<string, string> p) =>
            {
                bool resume = false;

                var msg = "";
                {
                    msg = msg.Append($"Удалить проход?");
                    msg = msg.Append($"{p.CheckGet("RACK_AISLE")}", true);
                }

                var d = new DialogWindow($"{msg}", "Удаление", "", DialogWindowButtons.NoYes);
                if ((bool)d.ShowDialog())
                {
                    resume = true;
                }

                return resume;
            };

            RackAisleItem.QueryDelete = new RequestData()
            {
                Module = "Warehouse",
                Object = "Rack",
                Action = "Delete",
            };

            RackAisleItem.OnUnload = () =>
            {
                this.RackAisleItem = null;
            };

            RackAisleItem.PrimaryKey = "WMRA_ID";
            RackAisleItem.PrimaryKeyValue = id;
            RackAisleItem.Commander.Init(RackAisleItem);
            RackAisleItem.Run(mode);
        }

        public void SelectCellForStorageIn(Dictionary<string, string> storageGridSelectedItem)
        {
            RackAisleItem["IN_WMST_NAME"].Form.SetValueByPath("IN_WMST_NAME", storageGridSelectedItem.CheckGet("NUM"));
            RackAisleItem["IN_WMST_ID"].Form.SetValueByPath("IN_WMST_ID", storageGridSelectedItem.CheckGet("WMST_ID"));
        }

        public void SelectCellForStorageOut(Dictionary<string, string> storageGridSelectedItem)
        {
            RackAisleItem["OUT_WMST_NAME"].Form.SetValueByPath("OUT_WMST_NAME", storageGridSelectedItem.CheckGet("NUM"));
            RackAisleItem["OUT_WMST_ID"].Form.SetValueByPath("OUT_WMST_ID", storageGridSelectedItem.CheckGet("WMST_ID"));
        }

        /// <summary>
        /// Инициализация грида секций
        /// </summary>
        public void RackSectionGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид секции",
                        Path="WMRS_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="RACK_SECTION_NUM",
                        ColumnType=ColumnTypeRef.String,
                        Width2=12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид склада",
                        Path="WMWA_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                        Hidden=true,
                    },
                };
                RackSectionGrid.SetColumns(columns);
                RackSectionGrid.SearchText = RackSectionSearchBox;
                RackSectionGrid.OnLoadItems = RackSectionGridLoadItems;
                RackSectionGrid.SetPrimaryKey("WMRS_ID");
                RackSectionGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                RackSectionGrid.AutoUpdateInterval = 60;
                RackSectionGrid.Toolbar = RackSectionGridToolbar;

                RackSectionGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem != null && selectedItem.Count > 0 && RackSectionGrid.Items.FirstOrDefault(x => x.CheckGet("WMRS_ID").ToInt() == selectedItem.CheckGet("WMRS_ID").ToInt()) != null)
                    {
                    }
                    else
                    {
                        RackSectionGrid.SelectRowFirst();
                    }
                };

                RackSectionGrid.Commands = Commander;
                RackSectionGrid.UseProgressSplashAuto = false;
                RackSectionGrid.Init();
            }
        }

        public async void RackSectionGridLoadItems()
        {
            if (RackSectionGrid != null && RackSectionGrid.SelectedItem != null && RackSectionGrid.SelectedItem.Count > 0 && RackSectionGrid.Commands != null)
            {
                RackSectionGrid.Commands.Message = new ItemMessage() { Action = "refresh", Message = $"{RackSectionGrid.SelectedItem.CheckGet("WMRS_ID")}" };
            }

            var p = new Dictionary<string, string>();
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "RackSection");
            q.Request.SetParam("Action", "List");
            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            RackSectionGridDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    RackSectionGridDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            else
            {
                q.ProcessError();
            }
            RackSectionGrid.UpdateItems(RackSectionGridDataSet);
        }

        public void AddRackSection()
        {
            ProcessRackSection();
        }

        public void EditRackSection()
        {
            ProcessRackSection("edit", RackSectionGrid.SelectedItem.CheckGet("WMRS_ID"));
        }

        public void DeleteRackSection()
        {
            ProcessRackSection("delete", RackSectionGrid.SelectedItem.CheckGet("WMRS_ID"));
        }

        public void ProcessRackSection(string mode = "create", string id = "")
        {
            string title = "";
            if (mode == "create")
            {
                title = "Новая секция";
            }
            else
            {
                title = "Секция";
            }

            var rackSectionItem = new FormDialog()
            {
                RoleName = this.RoleName,
                FrameName = "RackSection",
                Title = title,
                Fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="RACK_SECTION_NUM",
                        Description = "Номер секции",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                    new FormHelperField()
                    {
                        Path="WMWA_ID",
                        Description = "Склад",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Width = 152,
                        ControlType = "SelectBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                },
            };

            rackSectionItem["WMWA_ID"].OnAfterCreate += (FrameworkElement control) =>
            {
                FormHelper.ComboBoxInitHelper(control as SelectBox, "Warehouse", "Warehouse", "List", "WMWA_ID", "WAREHOUSE", null, true, true);
                (control as SelectBox).SetSelectedItemByKey("1");
                control.IsEnabled = true;
            };

            rackSectionItem.QueryGet = new RequestData()
            {
                Module = "Warehouse",
                Object = "RackSection",
                Action = "Get",
            };

            rackSectionItem.AfterGet += (FormDialog fd) =>
            {
                fd.Open();
            };

            rackSectionItem.QuerySave = new RequestData()
            {
                Module = "Warehouse",
                Object = "RackSection",
                Action = "Save",
            };

            rackSectionItem.AfterUpdate += (FormDialog fd) =>
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

            rackSectionItem.BeforeDelete += (Dictionary<string, string> p) =>
            {
                bool resume = false;

                var msg = "";
                {
                    msg = msg.Append($"Удалить секцию?");
                    msg = msg.Append($"{p.CheckGet("RACK_SECTION_NUM")}", true);
                }

                var d = new DialogWindow($"{msg}", "Удаление", "", DialogWindowButtons.NoYes);
                if ((bool)d.ShowDialog())
                {
                    resume = true;
                }

                return resume;
            };

            rackSectionItem.QueryDelete = new RequestData()
            {
                Module = "Warehouse",
                Object = "RackSection",
                Action = "Delete",
            };

            rackSectionItem.PrimaryKey = "WMRS_ID";
            rackSectionItem.PrimaryKeyValue = id;
            rackSectionItem.Commander.Init(rackSectionItem);
            rackSectionItem.Run(mode);
        }
    }
}
