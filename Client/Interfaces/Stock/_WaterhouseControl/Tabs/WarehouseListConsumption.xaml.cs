using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using DevExpress.Mvvm.Xpf;
using Newtonsoft.Json;
using NLog.LayoutRenderers;
using System;
using System.Collections.Generic;
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
using static Client.Common.FormHelperField;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Список списаний складских единиц WMS
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class WarehouseListConsumption : ControlBase
    {
        public WarehouseListConsumption()
        {
            ControlTitle = "Списание складских единиц";
            DocumentationUrl = "/doc/l-pack-erp/";
            RoleName = "[erp]warehouse_control";
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
                FormInit();
                SetDefaults();
                ConsumptionGridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                ConsumptionGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                ConsumptionGrid.ItemsAutoUpdate = true;
                ConsumptionGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                ConsumptionGrid.ItemsAutoUpdate = false;
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
                    Name = "export_to_excel",
                    Group = "main",
                    Enabled = true,
                    Title = "В Excel",
                    Description = "Выгрузить данные в Excel",
                    ButtonUse = true,
                    ButtonControl = ExportToExcelButton,
                    ButtonName = "ExportToExcelButton",
                    Action = () =>
                    {
                        ExportExcel();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ConsumptionGrid != null && ConsumptionGrid.Items != null && ConsumptionGrid.Items.Count > 0)
                        {
                            result = true;
                        }

                        return result;
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

            Commander.SetCurrentGridName("ConsumptionGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "cancel_consumption",
                    Title = "Отменить списание",
                    Description = "Отменить списание выбранной позиции",
                    Group = "consumption_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = CancelConsumptionButton,
                    ButtonName = "CancelConsumptionButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        CancelConsumption();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (ConsumptionGrid != null && ConsumptionGrid.Items != null && ConsumptionGrid.Items.Count > 0)
                        {
                            if (ConsumptionGrid.SelectedItem != null && ConsumptionGrid.SelectedItem.Count > 0)
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

        private ListDataSet ConsumptionDataSet { get; set; }

        public FormHelper Form { get; set; }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            ConsumptionDataSet = new ListDataSet();

            WarehouseFilter.Items.Add("0", "Все склады");
            FormHelper.ComboBoxInitHelper(WarehouseFilter, "Warehouse", "Warehouse", "List", "WMWA_ID", "WAREHOUSE", null, true);
            WarehouseFilter.SelectedItem = new KeyValuePair<string, string>("0", "Все склады");

            var date = DateTime.Now;
            if (date.Hour >= 20 || date.Hour < 8)
            {
                if (date.Hour >= 20)
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.AddDays(1).ToString("dd.MM.yyyy")} 08:00:00");
                }
                else
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 08:00:00");
                }
            }
            else
            {
                Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 08:00:00");
                Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 20:00:00");
            }
        }

        public void FormInit()
        {
            //инициализация формы
            {
                Form = new FormHelper();

                //колонки формы
                var fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="FROM_DATE_TIME",
                        FieldType=FormHelperField.FieldTypeRef.DateTime,
                        Control=FromDateTime,
                        ControlType="dateedit",
                        Format="dd.MM.yyyy HH:mm:ss",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        }
                    },
                    new FormHelperField()
                    {
                        Path="TO_DATE_TIME",
                        FieldType=FormHelperField.FieldTypeRef.DateTime,
                        Control=ToDateTime,
                        ControlType="dateedit",
                        Format="dd.MM.yyyy HH:mm:ss",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        }
                    },
                };

                Form.SetFields(fields);
            }
        }

        /// <summary>
        /// настройка отображения грида
        /// </summary>
        private void ConsumptionGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header = "*",
                        Path = "_SELECTED",
                        ColumnType = ColumnTypeRef.Boolean,
                        Width2=3,
                        Editable = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД списания",
                        Path="CONSUMPTION_ID",
                        Description="Ид списания складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД складской единицы",
                        Path="ITEM_ID",
                        Description="Ид списанной складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата",
                        Path="CONSUMPTION_DATE",
                        Description="Дата списания складской единицы",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2 = 15,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Path="QUANTITY",
                        Description="Списанное количество",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вес, кг.",
                        Path="WEIGHT",
                        Description="Списанный вес",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Складская единица",
                        Path="ITEM_NAME",
                        Description="Наименование списанной складской единицы",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 61,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Хранилище",
                        Path="STORAGE_NAME",
                        Description="Наименование хранилища, откуда была списана складская единица",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Причина",
                        Path="CONSUMPTION_REASON",
                        Description="Описание причины списания",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Пользователь",
                        Path="USER_NAME",
                        Description="Имя пользователя, который выполнил списание",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ задачи",
                        Path="TASK_OUTER_ID",
                        Description="Внешний Ид задачи",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Склад",
                        Path="WAREHOUSE_NAME",
                        Description="Наименование склада, откуда была списана складская единица",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Зона",
                        Path="ZONE_NAME",
                        Description="Наименование зоны, откуда была списана складская единица",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 15,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД внешний",
                        Path="OUTER_ID",
                        Description="Внешний Ид складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                    },                  

                    new DataGridHelperColumn
                    {
                        Header="ИД прихода",
                        Path="INCOMING_ID",
                        Description="Ид прихода складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 10,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД хранилища",
                        Path="STORAGE_ID",
                        Description="Ид хранилища, откуда была списана складская единица",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 12,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД склада",
                        Path="WAREHOUSE_ID",
                        Description="Ид склада, откуда была списана складская единица",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 9,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД зоны",
                        Path="ZONE_ID",
                        Description="Ид зоны, откуда была списана складская единица",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 7,
                        Hidden=true,
                    },
                };
                ConsumptionGrid.SetColumns(columns);
                ConsumptionGrid.SetPrimaryKey("CONSUMPTION_ID");
                ConsumptionGrid.SearchText = ConsumptionSearchBox;
                //данные грида
                ConsumptionGrid.OnLoadItems = ConsumptionGridLoadItems;
                ConsumptionGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                ConsumptionGrid.AutoUpdateInterval = 60 * 5;
                ConsumptionGrid.Toolbar = ConsumptionGridToolbar;

                ConsumptionGrid.OnFilterItems = () =>
                {
                    if (ConsumptionGrid.Items != null && ConsumptionGrid.Items.Count > 0)
                    {
                        if (WarehouseFilter != null && WarehouseFilter.SelectedItem.Key != null)
                        {
                            var key = WarehouseFilter.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            switch (key)
                            {
                                // Все склады
                                case 0:
                                    items = ConsumptionGrid.Items;
                                    break;

                                default:
                                    items.AddRange(ConsumptionGrid.Items.Where(x => x.CheckGet("WAREHOUSE_ID").ToInt() == key));
                                    break;
                            }

                            ConsumptionGrid.Items = items;
                        }

                        if (ZoneFilter != null && ZoneFilter.SelectedItem.Key != null)
                        {
                            var key = ZoneFilter.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            switch (key)
                            {
                                // Все зоны
                                case 0:
                                    items = ConsumptionGrid.Items;
                                    break;

                                default:
                                    items.AddRange(ConsumptionGrid.Items.Where(x => x.CheckGet("ZONE_ID").ToInt() == key));
                                    break;
                            }

                            ConsumptionGrid.Items = items;
                        }
                    }
                };

                //при выборе строки в гриде, обновляются актуальные действия для записи
                ConsumptionGrid.OnSelectItem = selectedItem =>
                {
                };

                ConsumptionGrid.Commands = Commander;

                ConsumptionGrid.Init();
            }
        }

        /// <summary>
        /// Загрузка данными грида
        /// </summary>
        private async void ConsumptionGridLoadItems()
        {
            ConsumptionGrid.ShowSplash();

            if (Form.Validate())
            {
                bool resume = true;
                var f = Form.GetValueByPath("FROM_DATE_TIME").ToDateTime();
                var t = Form.GetValueByPath("TO_DATE_TIME").ToDateTime();
                if (DateTime.Compare(f, t) > 0)
                {
                    const string msg = "Дата начала должна быть меньше даты окончания.";
                    var d = new DialogWindow($"{msg}", "Проверка данных");
                    d.ShowDialog();
                    resume = false;
                }

                if (resume)
                {
                    var p = new Dictionary<string, string>();
                    p.Add("FROM_DATE", Form.GetValueByPath("FROM_DATE_TIME"));
                    p.Add("TO_DATE", Form.GetValueByPath("TO_DATE_TIME"));

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Warehouse");
                    q.Request.SetParam("Object", "Consumption");
                    q.Request.SetParam("Action", "ListByWarehouseAndZoneAndDate");
                    q.Request.SetParams(p);

                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    ConsumptionDataSet = new ListDataSet();
                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            ConsumptionDataSet = ListDataSet.Create(result, "ITEMS");
                        }
                    }
                    ConsumptionGrid.UpdateItems(ConsumptionDataSet);
                }
            }

            ConsumptionGrid.HideSplash();
        }

        public void CancelConsumption()
        {
            if (ConsumptionGrid != null && ConsumptionGrid.Items != null && ConsumptionGrid.Items.Count > 0)
            {
                var checkedRowList = ConsumptionGrid.GetItemsSelected();
                if (checkedRowList != null && checkedRowList.Count > 0)
                {
                    if (checkedRowList.Count > 1)
                    {
                        if (new DialogWindow($"Хотите отменить списание {checkedRowList.Count} позиций?", "Сообщение", "", DialogWindowButtons.YesNo).ShowDialog() == true)
                        {
                            bool errorFlag = false;

                            foreach (var checkedRow in checkedRowList)
                            {
                                var p = new Dictionary<string, string>();
                                p.Add("WMCO_ID", checkedRow.CheckGet("CONSUMPTION_ID"));

                                var q = new LPackClientQuery();
                                q.Request.SetParam("Module", "Warehouse");
                                q.Request.SetParam("Object", "Consumption");
                                q.Request.SetParam("Action", "Cancel");
                                q.Request.SetParams(p);

                                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                                q.DoQuery();

                                if (q.Answer.Status == 0)
                                {
                                    bool succesfullFlag = false;

                                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                                    if (result != null)
                                    {
                                        var ds = ListDataSet.Create(result, "ITEMS");
                                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                                        {
                                            if (ds.Items.First().CheckGet("ID").ToInt() == 0)
                                            {
                                                succesfullFlag = true;
                                            }
                                        }
                                    }

                                    if (!succesfullFlag)
                                    {
                                        errorFlag = true;
                                    }
                                }
                                else
                                {
                                    errorFlag = true;
                                }
                            }

                            if (errorFlag)
                            {
                                DialogWindow.ShowDialog($"При выполнении отмены списания произошла ошибка. Пожалуйста, сообщите о проблеме.");
                            }

                            Refresh();
                        }
                    }
                    else
                    {
                        var checkedRow = checkedRowList.First();
                        if (new DialogWindow($"Хотите отменить списание {checkedRow["ITEM_ID"].ToInt()} {checkedRow["ITEM_NAME"]} из ячейки {checkedRow["STORAGE_NAME"]}?", "Сообщение", "", DialogWindowButtons.YesNo).ShowDialog() == true)
                        {
                            var p = new Dictionary<string, string>();
                            p.Add("WMCO_ID", checkedRow.CheckGet("CONSUMPTION_ID"));

                            var q = new LPackClientQuery();
                            q.Request.SetParam("Module", "Warehouse");
                            q.Request.SetParam("Object", "Consumption");
                            q.Request.SetParam("Action", "Cancel");
                            q.Request.SetParams(p);

                            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                            q.DoQuery();

                            if (q.Answer.Status == 0)
                            {
                                bool succesfullFlag = false;

                                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                                if (result != null)
                                {
                                    var ds = ListDataSet.Create(result, "ITEMS");
                                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                                    {
                                        if (ds.Items.First().CheckGet("ID").ToInt() == 0)
                                        {
                                            succesfullFlag = true;
                                        }
                                    }
                                }

                                if (!succesfullFlag)
                                {
                                    DialogWindow.ShowDialog($"При выполнении отмены списания произошла ошибка. Пожалуйста, сообщите о проблеме.");
                                }
                                else
                                {
                                    Refresh();
                                }
                            }
                            else
                            {
                                q.ProcessError();
                            }
                        }
                    }
                }
                else
                {
                    if (ConsumptionGrid.SelectedItem != null && ConsumptionGrid.SelectedItem.Count > 0)
                    {
                        if (new DialogWindow($"Хотите отменить списание {ConsumptionGrid.SelectedItem["ITEM_ID"].ToInt()} {ConsumptionGrid.SelectedItem["ITEM_NAME"]} из ячейки {ConsumptionGrid.SelectedItem["STORAGE_NAME"]}?", "Сообщение", "", DialogWindowButtons.YesNo).ShowDialog() == true)
                        {
                            var p = new Dictionary<string, string>();
                            p.Add("WMCO_ID", ConsumptionGrid.SelectedItem.CheckGet("CONSUMPTION_ID"));

                            var q = new LPackClientQuery();
                            q.Request.SetParam("Module", "Warehouse");
                            q.Request.SetParam("Object", "Consumption");
                            q.Request.SetParam("Action", "Cancel");
                            q.Request.SetParams(p);

                            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                            q.DoQuery();

                            if (q.Answer.Status == 0)
                            {
                                bool succesfullFlag = false;

                                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                                if (result != null)
                                {
                                    var ds = ListDataSet.Create(result, "ITEMS");
                                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                                    {
                                        if (ds.Items.First().CheckGet("ID").ToInt() == 0)
                                        {
                                            succesfullFlag = true;
                                        }
                                    }
                                }

                                if (!succesfullFlag)
                                {
                                    DialogWindow.ShowDialog($"При выполнении отмены списания произошла ошибка. Пожалуйста, сообщите о проблеме.");
                                }
                                else
                                {
                                    Refresh();
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

        public void Refresh()
        {
            ConsumptionGrid.LoadItems();
        }

        public void GetZoneList()
        {
            ClearSelectBox(ZoneFilter);

            if (WarehouseFilter != null && WarehouseFilter.SelectedItem.Key != null)
            {
                ZoneFilter.Items.Add("0", "Все зоны");
                FormHelper.ComboBoxInitHelper(ZoneFilter, "Warehouse", "Zone", "ListByWarehouse", "WMZO_ID", "ZONE", new Dictionary<string, string>() { { "WMWA_ID", WarehouseFilter.SelectedItem.Key } }, true, false);
                ZoneFilter.SelectedItem = new KeyValuePair<string, string>("0", "Все зоны");
            }
            else
            {
                ZoneFilter.Items.Add("0", "Все зоны");
                ZoneFilter.SelectedItem = new KeyValuePair<string, string>("0", "Все зоны");
            }
        }

        /// <summary>
        /// Очищаем наполнение селектбокса
        /// </summary>
        /// <param name="selectBox"></param>
        private void ClearSelectBox(SelectBox selectBox)
        {
            selectBox.DropDownListBox.Items.Clear();
            selectBox.DropDownListBox.SelectedItem = null;
            selectBox.ValueTextBox.Text = "";
            selectBox.Items = new Dictionary<string, string>();
            selectBox.SelectedItem = new KeyValuePair<string, string>();
        }

        public async void ExportExcel()
        {
            ConsumptionGrid.ItemsExportExcel();
        }

        private void WarehouseFilter_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GetZoneList();
            ConsumptionGrid.UpdateItems();
        }

        private void ZoneFilter_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ConsumptionGrid.UpdateItems();
        }

        private void OnCurrentShift(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now;
            if (date.Hour >= 20 || date.Hour < 8)
            {
                if (date.Hour >= 20)
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.AddDays(1).ToString("dd.MM.yyyy")} 08:00:00");
                }
                else
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 08:00:00");
                }                
            }
            else
            {
                Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 08:00:00");
                Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 20:00:00");
            }

            Refresh();
        }

        private void OnPrevShift(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now.AddHours(-12);
            if (date.Hour >= 20 || date.Hour < 8)
            {
                if (date.Hour >= 20)
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{date.ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{date.AddDays(1).ToString("dd.MM.yyyy")} 08:00:00");
                }
                else
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{date.AddDays(-1).ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{date.ToString("dd.MM.yyyy")} 08:00:00");
                }
            }
            else
            {
                Form.SetValueByPath("FROM_DATE_TIME", $"{date.ToString("dd.MM.yyyy")} 08:00:00");
                Form.SetValueByPath("TO_DATE_TIME", $"{date.ToString("dd.MM.yyyy")} 20:00:00");
            }

            Refresh();
        }

        private void OnCurrentDay(object sender, RoutedEventArgs e)
        {
            Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.AddDays(1).ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void OnPrevDay(object sender, RoutedEventArgs e)
        {
            Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void OnCurrentWeek(object sender, RoutedEventArgs e)
        {
            DayOfWeek day = DateTime.Now.DayOfWeek;
            int days = day - DayOfWeek.Monday;
            DateTime date = DateTime.Now.AddDays(-days);

            Form.SetValueByPath("FROM_DATE_TIME", $"{date.Date.ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{date.Date.AddDays(7).ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void OnPrevWeek(object sender, RoutedEventArgs e)
        {
            DayOfWeek day = DateTime.Now.DayOfWeek;
            int days = day - DayOfWeek.Monday;
            DateTime date = DateTime.Now.AddDays(-days).AddDays(-7);

            Form.SetValueByPath("FROM_DATE_TIME", $"{date.Date.ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{date.Date.AddDays(7).ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void OnCurrentMonth(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now;

            Form.SetValueByPath("FROM_DATE_TIME", $"{new DateTime(date.Year, date.Month, 1).ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{new DateTime(date.Year, date.Month, 1).AddMonths(1).ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void OnPrevMonth(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now.AddMonths(-1);

            Form.SetValueByPath("FROM_DATE_TIME", $"{new DateTime(date.Year, date.Month, 1).ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{new DateTime(date.Year, date.Month, 1).AddMonths(1).ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }
    }
}
