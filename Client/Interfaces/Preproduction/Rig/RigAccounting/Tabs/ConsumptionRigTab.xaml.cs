using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Взаимодействие с расходами оснасток. Добавление/изменение/ТК/Excel
    /// </summary>
    /// <author>volkov_as</author>
    public partial class ConsumptionRigTab : ControlBase
    {
        public ConsumptionRigTab()
        {
            InitializeComponent();

            ControlSection = "consumption";
            RoleName = "[erp]rig_movement";
            ControlTitle = "Расход оснастки";
            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/osnastka/rashod_osn";

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == "PreproductionRig")
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };


            OnLoad = () =>
            {
                SetDefaults();
                RigConsumptionGridInit();
            };

            OnUnload = () =>
            {
                RigConsumtionGrid.Destruct();
            };

            OnFocusGot = () =>
            {
                RigConsumtionGrid.ItemsAutoUpdate = true;
            };

            OnFocusLost = () =>
            {
                RigConsumtionGrid.ItemsAutoUpdate = false;
            };

            {
                Commander.SetCurrentGroup("main");
                {
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
                }

                Commander.SetCurrentGridName("GridConsumtionRig");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "consumption_refresh",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить данные",
                        ButtonUse = true,
                        ButtonName = "Refresh",
                        MenuUse = true,
                        Action = () =>
                        {
                            RigConsumtionGrid.LoadItems();
                        },
                    });

                    Commander.SetCurrentGroup("item");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "consumption_create",
                            Title = "Создать",
                            MenuUse = true,
                            Enabled = true,
                            ButtonUse = true,
                            ButtonName = "AddButton",
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                if (RigConsumtionGrid.GetItemsSelected().Count > 0)
                                {
                                    List<Dictionary<string, string>> selectedItems = RigConsumtionGrid.GetItemsSelected();
                                    SelectedIds.Clear();

                                    var rigAddForm = new ConsumptionCreateFrame();
                                    rigAddForm.FactoryId = selectedItems[0].CheckGet("FACT_ID").ToInt();
                                    rigAddForm.Create(selectedItems);
                                }
                                else
                                {
                                    var msg = "Для создания расхода выберите одну и более записей!";
                                    var d = new DialogWindow(msg, "Проверка данных");
                                    d.ShowDialog();
                                }
                            }
                        });

                        Commander.Add(new CommandItem()
                        {
                            Name = "consumption_edit",
                            Title = "Изменить",
                            MenuUse = true,
                            Enabled = true,
                            ButtonUse = true,
                            ButtonName = "EditButton",
                            HotKey = "Return|DoubleCLick",
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var rigForm = new EditRecordRigFrame();
                                rigForm.Edit(RigConsumtionGrid.SelectedItem.CheckGet("RIG_ID").ToInt());
                            },
                        });

                        Commander.Add(new CommandItem()
                        {
                            Name = "consumption_write_off",
                            Title = "Списать",
                            MenuUse = true,
                            Enabled = true,
                            ButtonUse = true,
                            ButtonName = "WriteOff",
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                RigWriteOff();
                            }
                        });
                        
                        Commander.Add(new CommandItem()
                        {
                            Name = "consumption_cancel_write_off",
                            Title = "Отменить списание",
                            MenuUse = true,
                            Enabled = true,
                            ButtonUse = true,
                            ButtonName = "CancelWriteOff",
                            AccessLevel = Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var row = RigConsumtionGrid.SelectedItem;
                                var idr = row.CheckGet("EXPENSE_ID").ToInt();
                                var nsthet = row.CheckGet("NSTHET").ToInt();
                                
                                var dialog = new DialogWindow($"Отмена списания оснастки - {row.CheckGet("RIG_ID").ToInt()}?", "Оснастка", "", DialogWindowButtons.YesNo);

                                dialog.ShowDialog();

                                if (dialog.DialogResult == true)
                                {
                                    CancelWriteOffHandler(idr, nsthet);
                                }
                            },
                            CheckEnabled = () =>
                            {
                                var row = RigConsumtionGrid.SelectedItem;
                                if (row.CheckGet("EXPENSE_ID") != "")
                                {
                                    return true;
                                }
                                return false;
                            }
                        });
                    }

                    Commander.SetCurrentGroup("GridFunction");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "consumption_technological_map_show",
                            Title = "Показать ТК",
                            MenuUse = true,
                            Enabled = true,
                            ButtonUse = true,
                            ButtonName = "ShowTK",
                            AccessLevel = Common.Role.AccessMode.ReadOnly,
                            Action = () =>
                            {
                                TechnologicalMapShow();
                            }
                        });

                        Commander.Add(new CommandItem()
                        {
                            Name = "consumption_export_to_excel",
                            Title = "Экспорт в Excel",
                            Enabled = true,
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "Excel",
                            AccessLevel = Common.Role.AccessMode.ReadOnly,
                            Action = () =>
                            {
                                RigConsumtionGrid.ItemsExportExcel();
                            }
                        });
                    }
                }

                Commander.Init(this);
            }
        }

        private int Count { get; set; } = 0;
        public ListDataSet RigList { get; set; }
        public FormHelper Form { get; set; }        
        public Dictionary<string, string> SelectedItem { get; set; }
        /// <summary>
        /// Список выбранных ID для восстановления выбора после обновления
        /// </summary>
        private List<int> SelectedIds { get; set; }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            var rigTypeItems = new Dictionary<string, string>()
            {
                { "-1", "Все" },
                { "14", "Штанцформы" },
                { "15", "Печатные формы" },
                { "18", "Печатные формы ЛТ" }
            };
            RigType.Items = rigTypeItems;
            RigType.SetSelectedItemByKey("-1");

            var factoryTypeItems = new Dictionary<string, string>
            {
                { "1", "Л-ПАК ЛИПЕЦК" },
                { "2", "Л-ПАК КАШИРА" }
            };
            FactoryType.Items = factoryTypeItems;
            FactoryType.SetSelectedItemByKey("1");

            SelectedIds = new List<int>();
        }

        /// <summary>
        /// Инициализация грида Rig
        /// </summary>
        public void RigConsumptionGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="*",
                    Path="_SELECTED",
                    ColumnType = ColumnTypeRef.Boolean,
                    Width2=7,
                    Exportable = false,
                    Editable = true,
                    OnClickAction = (row, el) =>
                    {
                        int id = row.CheckGet("RIG_ID").ToInt();
                        if ((row.CheckGet("RIG_NAME") != null) &&
                            (row.CheckGet("EXPENSE_ID").ToInt() == 0))
                        {
                            Central.Dbg("Selected item: " + row.CheckGet("RIG_NAME") + "| " + row + " " + el);

                            bool selected = row.CheckGet("_SELECTED").ToBool();
                            if (selected)
                            {
                                if (SelectedIds.Contains(id))
                                {
                                    SelectedIds.Remove(id);
                                }
                            }
                            else
                            {
                                SelectedIds.Add(id);
                            }

                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header="ИД оснастки",
                    Path="RIG_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                    Visible = true,
                    Exportable = true,
                },
                new DataGridHelperColumn
                {
                     Header="Наименование",
                     Path="RIG_NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=46
                },

                new DataGridHelperColumn
                {
                     Header="Поставщик",
                     Path="VENDOR_NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=19
                },

                new DataGridHelperColumn
                {
                     Header="Цена покупки, руб.",
                     Path="INCOMING_PRICE",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=16
                },
                new DataGridHelperColumn
                {
                     Header="Цена продажи, руб.",
                     Path="EXPENSE_PRICE",
                     ColumnType=ColumnTypeRef.Double,
                     Width2=16
                },
                new DataGridHelperColumn
                {
                     Header="Дата расхода",
                     Path="EXPENSE_DATE",
                     ColumnType=ColumnTypeRef.DateTime,
                     Width2=12
                },
                new DataGridHelperColumn
                {
                     Header="Покупатель",
                     Path="PURCHASER_NAME",
                     ColumnType= DataGridHelperColumn.ColumnTypeRef.String,
                     Width2=21,
                     Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                     {
                         {
                             StylerTypeRef.BackgroundColor,
                             row =>
                             {
                                 var result = DependencyProperty.UnsetValue;
                                 var color = "";

                                 if (row.CheckGet("PREPAYMENT_FLAG").ToInt() != 0)
                                 {
                                     color = HColor.OrangeFG;
                                 }

                                 if (!string.IsNullOrEmpty(color))
                                 {
                                     result = color.ToBrush();
                                 }

                                 return result;
                             }
                         }
                     }
                },
                new DataGridHelperColumn
                {
                     Header="ТТН",
                     Path="EXPENSE_NUMBER",
                     ColumnType=ColumnTypeRef.String,
                     Description = "Товарно-транспортная накладная",
                     Width2=6
                },
                new DataGridHelperColumn
                {
                     Header="Продавец",
                     Path="SELLER_NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=21
                },
                new DataGridHelperColumn
                {
                    Header="Примечание",
                    Path="NOTE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=17
                },
                new DataGridHelperColumn
                {
                    Header="Предоплата",
                    Path="PREPAYMENT_FLAG",
                    ColumnType = ColumnTypeRef.Boolean,
                    Width2 = 10
                },
                new DataGridHelperColumn
                {
                    Header="Отсрочка дней",
                    Path="CONDITIONS",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 10
                },
                new DataGridHelperColumn
                {
                    Header = "Запрет на перевыставление счета",
                    Path = "BAN_REARRANGE_FLAG",
                    ColumnType = ColumnTypeRef.Boolean,
                    Width2 = 10
                },
                new DataGridHelperColumn
                {
                    Header = "Примечание по перевыставлению счета",
                    Path = "BAN_REARRANGE_NOTE",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 40
                },
                new DataGridHelperColumn
                {
                    Header="IDK1",
                    Path="IDK1",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=0,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header="ID1",
                    Path="ID1",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=0,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header="KOL",
                    Path="KOL",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=0,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header="ID",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=0,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "NSTHET",
                    Path = "NSTHET",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 10,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "EXPENSE_ID",
                    Path = "EXPENSE_ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 10,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "FACT_ID",
                    Path = "FACT_ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 10,
                    Visible = false
                }

            };

            RigConsumtionGrid.SetColumns(columns);
            RigConsumtionGrid.SearchText = SearchText;
            RigConsumtionGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            RigConsumtionGrid.OnLoadItems = LoadItems;
            RigConsumtionGrid.Toolbar = GridToolbar;
            RigConsumtionGrid.SetPrimaryKey("RIG_ID");

            RigConsumtionGrid.OnFilterItems = () =>
            {
                if (RigConsumtionGrid.Items.Count > 0)
                {
                    {
                        bool showAll = false;
                        bool filterByType = false;
                        bool factoryFilter = false;
                        var v = Form.GetValues();

                        if (v.CheckGet("SHOW_ALL").ToBool())
                        {
                            showAll = true;
                        }

                        int rigTypeId = v.CheckGet("RIG_TYPE").ToInt();
                        if (rigTypeId > 0)
                        {
                            filterByType = true;
                        }

                        int factoryType = v.CheckGet("FACTORY_TYPE").ToInt();
                        if (factoryType > 0)
                        {
                            factoryFilter = true;
                        }

                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in RigConsumtionGrid.Items)
                        {
                            bool includeConsumed = true;
                            bool includeByType = true;
                            bool includeFactory = true;

                            if (!showAll)
                            {
                                includeConsumed = false;

                                if ((row.CheckGet("RIG_NAME") != null)
                                    && (row.CheckGet("EXPENSE_ID").ToInt() == 0))
                                {
                                    includeConsumed = true;
                                }
                            }

                            if (filterByType)
                            {
                                includeByType = false;
                                if (row.CheckGet("IDK1").ToInt() == rigTypeId)
                                {
                                    includeByType = true;
                                }
                            }

                            if (factoryFilter)
                            {
                                includeFactory = false;
                                if (row.CheckGet("FACT_ID").ToInt() == factoryType)
                                {
                                    includeFactory = true;
                                }
                            }

                            if (includeByType && includeConsumed && includeFactory)
                            {
                                items.Add(row);
                            }
                        }

                        RigConsumtionGrid.Items = items;
                    }
                }
            };

            RigConsumtionGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                {
                    StylerTypeRef.BackgroundColor,
                    (Dictionary<string, string> row) =>
                    {
                        var result = DependencyProperty.UnsetValue;
                        var color = "";

                        if (row.CheckGet("EXPENSE_ID").ToInt() != 0)
                        {
                            color = HColor.Green;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result = color.ToBrush();
                        }

                        return result;
                    }
                }
            };

            RigConsumtionGrid.OnDblClick = (Dictionary<string, string> selectedItem) =>
            {
                var rigForm = new EditRecordRigFrame();
                rigForm.Edit(selectedItem.CheckGet("RIG_ID").ToInt());
            };

            RigConsumtionGrid.Commands = Commander;
            RigConsumtionGrid.Init();


            {
                Form = new FormHelper();
                var field = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="DATE_FROM",
                        FieldType=FormHelperField.FieldTypeRef.DateTime,
                        Default = DateTime.Now.AddMonths(-1).ToString("dd.MM.yyyy"),
                        Control= FromDate,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                        OnChange = (FormHelperField field, string value) =>
                        {
                            Refresh.Style = (System.Windows.Style)Refresh.TryFindResource("FButtonPrimary");
                        }
                    },
                    new FormHelperField()
                    {
                        Path="DATE_TO",
                        FieldType=FormHelperField.FieldTypeRef.DateTime,
                        Default = DateTime.Now.ToString("dd.MM.yyyy"),
                        Control= ToDate,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                        OnChange = (FormHelperField field, string value) =>
                        {
                            Refresh.Style = (System.Windows.Style)Refresh.TryFindResource("FButtonPrimary");
                        }
                    },
                    new FormHelperField()
                    {
                        Path = "SHOW_ALL",
                        FieldType = FormHelperField.FieldTypeRef.Boolean,
                        Control = SelectGreenRow,
                        ControlType = "CheckBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{},
                        OnChange = (FormHelperField field, string value)=>{
                            RigConsumtionGrid.UpdateItems();
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "SELECT_CA",
                        FieldType = FormHelperField.FieldTypeRef.Boolean,
                        Control = SelectContrlAgent,
                        ControlType = "CheckBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{},
                        OnChange = (FormHelperField field, string value)=>{
                            var selected = (bool)SelectContrlAgent.IsChecked;

                            if (RigConsumtionGrid.Items != null)
                            {
                                if (RigConsumtionGrid.Items.Count > 0)
                                {
                                    foreach(Dictionary<string, string> row in RigConsumtionGrid.Items)
                                    {
                                        if (selected && (row.CheckGet("EXPENSE_ID").ToInt() == 0))
                                        {
                                            row.CheckAdd("_SELECTED", "1");
                                        } 
                                        else
                                        {
                                            row.CheckAdd("_SELECTED", "0");
                                        }
                                    }

                                    RigConsumtionGrid.UpdateItems();
                                }
                            }
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "RIG_TYPE",
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = RigType,
                        ControlType = "SelectBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{},
                        OnChange = (FormHelperField field, string value)=>{
                            RigConsumtionGrid.UpdateItems();
                        },
                    },
                    new FormHelperField
                    {
                        Path = "FACTORY_TYPE",
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = FactoryType,
                        ControlType = "SelectBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{},
                        OnChange = (field, value) =>
                        {
                            SelectedIds.Clear();
                            RigConsumtionGrid.UpdateItems();
                        }
                    }
                };
                Form.SetFields(field);
                Form.SetDefaults();

            }
        }

        /// <summary>
        /// Загрузка данных Rig
        /// </summary>
        private async void LoadItems()
        {
            bool resume = true;
            var v = Form.GetValues();

            {
                var f = v.CheckGet("DATE_FROM").ToDateTime();
                var t = v.CheckGet("DATE_TO").ToDateTime();
                if (DateTime.Compare(f, t) > 0)
                {
                    var msg = "Дата начала периода не может быть больше даты окончания периода";
                    var d = new DialogWindow(msg, "Проверка данных");
                    d.ShowDialog();
                    resume = false;
                }
            }

            if (resume)
            {
                var p = Form.GetValues();
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Rig");
                q.Request.SetParam("Object", "Consumption");
                q.Request.SetParam("Action", "List");
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
                        RigList = ListDataSet.Create(result, "ITEMS");
                        RigConsumtionGrid.UpdateItems(RigList);

                        //Восстанавливаем выделение
                        if (SelectedIds.Count > 0)
                        {
                            foreach (var item in RigList.Items)
                            {
                                if (SelectedIds.Contains(item.CheckGet("RIG_ID").ToInt()))
                                {
                                    item.CheckAdd("_SELECTED", "1");
                                }
                            }
                            RigConsumtionGrid.UpdateItems();
                        }

                        // Обновление стиля кнопки
                        Refresh.Style = (System.Windows.Style)Refresh.TryFindResource("Button");
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
            }
        }

        /// <summary>
        /// Обработчик для кнопки "Показать ТК"
        /// </summary>
        private void TechnologicalMapShow()
        {
            if (RigConsumtionGrid.Items.Count > 0)
            {
                if (RigConsumtionGrid.SelectedItem != null)
                {
                    var path = RigConsumtionGrid.SelectedItem.CheckGet("PATHTK");
                    if (!string.IsNullOrEmpty(path))
                    {
                        if (File.Exists(path))
                        {
                            Central.OpenFile(path);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Функция для списание оснасток
        /// </summary>
        private void RigWriteOff()
        {
            var idRig = "";
            List<Dictionary<string, string>> selectedItems = new List<Dictionary<string, string>>();
            if (RigConsumtionGrid.GetItemsSelected().Count > 0)
            {
                selectedItems = RigConsumtionGrid.GetItemsSelected();
                foreach (var item in selectedItems)
                {
                    idRig += $" №{item.CheckGet("RIG_ID")},";
                }

                if (idRig.EndsWith(","))
                {
                    idRig = idRig.TrimEnd(',');
                } 
            }
            else
            {
                selectedItems.Add(RigConsumtionGrid.SelectedItem);
                idRig = RigConsumtionGrid.SelectedItem.CheckGet("RIG_ID");
            }
            
            var dialog = new DialogWindow($"Списать оснастку -{idRig}?", "Списание оснастки", "", DialogWindowButtons.YesNo);
            dialog.ShowDialog();
            if (dialog.DialogResult == true)
            {
                SelectedIds.Clear();
                NewPackingListCreate(selectedItems);
            }
        }
        
        /// <summary>
        /// Отмена операции списания оснастки
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private async void CancelWriteOffHandler(int idr, int nsthet)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "Consumption");
            q.Request.SetParam("Action", "CancelWriteOff");
            q.Request.SetParam("IDR", idr.ToString());
            q.Request.SetParam("NSTHET", nsthet.ToString());

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                RigConsumtionGrid.LoadItems();
            }
        }

        /// <summary>
        /// Создание новой накладной
        /// </summary>
        private async void NewPackingListCreate(List<Dictionary<string, string>> selectedItems)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "PackingList");
            q.Request.SetParam("Action", "Create");
            q.Request.SetParam("FACTORY_ID", selectedItems[0].CheckGet("FACT_ID"));

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
                    var newNakl = ListDataSet.Create(result, "ITEM");
                    var naklNsthet = newNakl.Items[0].CheckGet("NSTHET");
                    // Списываем в новую накладную записи
                    foreach (var item in selectedItems)
                    {
                        Count++;
                        RigAddInPackingList(
                            naklNsthet,
                            item.CheckGet("IDK1"),
                            item.CheckGet("ID1"),
                            item.CheckGet("RIG_ID"),
                            item.CheckGet("KOL"),
                            item.CheckGet("ID"));
                    }
                }
            }

            RigConsumtionGrid.LoadItems();
        }
        
        /// <summary>
        /// Добавление оснастки в накладную
        /// </summary>
        /// <param name="packingListId">Идентификатор расходной накладной (Счет)</param>
        /// <param name="productCategoryId">Идентификатор категории товара</param>
        /// <param name="departmentId">Идентификатор отдела из которого расходуется товар</param>
        /// <param name="productId">Идентификатор товара</param>
        /// <param name="amount">Количество (1)</param>
        /// <param name="incomeId">Идентфикатор прихода - используется как идентификатор поддона</param>
        private async void RigAddInPackingList(string packingListId, string productCategoryId, string departmentId, string productId, string amount, string incomeId)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "Consumption");
            q.Request.SetParam("Action", "Create");
            q.Request.SetParam("NSTHET", packingListId);
            q.Request.SetParam("IDK1", productCategoryId);
            q.Request.SetParam("ID1", departmentId);
            q.Request.SetParam("ID2", productId);
            q.Request.SetParam("KOL", amount);
            q.Request.SetParam("IDP", incomeId);

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
                    var result2 = ListDataSet.Create(result, "ITEMS");
                    var idr = result2.Items[0].CheckGet("IDR");

                    PackingListWriteOff(idr);
                }
            }
        }


        /// <summary>
        /// Вызов процедуры для проведения оснастки
        /// </summary>
        /// <param name="recordId">Идентификатор записи в настоящей таблице</param>
        private async void PackingListWriteOff(string recordId)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "PackingList");
            q.Request.SetParam("Action", "MakeWriteOff");
            q.Request.SetParam("IDR", recordId);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Count--;
            }

            if (Count == 0)
            {
                RigConsumtionGrid.LoadItems();
            }
        }
    }
}
