using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
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
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Интерфейс для работы с операциями wms
    /// </summary>
    public partial class WarehouseOperation : ControlBase
    {
        public WarehouseOperation()
        {
            ControlTitle = "Операции";
            DocumentationUrl = "/doc/l-pack-erp/warehouse/warehouseControl/warehouseOperations";
            RoleName = "[erp]warehouse_control";
            InitializeComponent();

            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

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
                OperationGridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                OperationGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                OperationGrid.ItemsAutoUpdate = true;
                OperationGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                OperationGrid.ItemsAutoUpdate = false;
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

                        if (OperationGrid != null && OperationGrid.Items != null && OperationGrid.Items.Count > 0)
                        {
                            result = true;
                        }

                        return result;
                    },
                });
            }

            Commander.SetCurrentGridName("OperationGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "add_operation",
                    Title = "Добавить",
                    Description = "Добавить новую операцию",
                    Group = "operation_grid_default",
                    Enabled = true,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = AddButton,
                    ButtonName = "AddButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        AddOperation();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "edit_operation",
                    Title = "Изменить",
                    Description = "Изменить операцию",
                    Group = "operation_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = EditButton,
                    ButtonName = "EditButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    HotKey = "DoubleCLick",
                    Action = () =>
                    {
                        EditOperation();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (OperationGrid != null && OperationGrid.SelectedItem != null && OperationGrid.SelectedItem.Count > 0)
                        {
                            result = true;
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "delete_operation",
                    Title = "Удалить",
                    Description = "Удалить операцию",
                    Group = "operation_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = DeleteButton,
                    ButtonName = "DeleteButton",
                    AccessLevel = Role.AccessMode.Special,
                    Action = () =>
                    {
                        DeleteOperation();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (OperationGrid != null && OperationGrid.SelectedItem != null && OperationGrid.SelectedItem.Count > 0)
                        {
                            if (OperationGrid.SelectedItem.CheckGet("WMOS_ID").ToInt() == 1)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "accept_operation",
                    Title = "Принять",
                    Description = "Принять операцию",
                    Group = "operation_grid_edit_status",
                    Enabled = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        OperationEditStatus(2);
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (OperationGrid != null && OperationGrid.SelectedItem != null && OperationGrid.SelectedItem.Count > 0)
                        {
                            if (OperationGrid.SelectedItem.CheckGet("WMOS_ID").ToInt() == 1)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "cancel_operation",
                    Title = "Отменить",
                    Description = "Отменить операцию",
                    Group = "operation_grid_edit_status",
                    Enabled = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        OperationEditStatus(3);
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (OperationGrid != null && OperationGrid.SelectedItem != null && OperationGrid.SelectedItem.Count > 0)
                        {
                            if (OperationGrid.SelectedItem.CheckGet("WMOS_ID").ToInt() == 2 || OperationGrid.SelectedItem.CheckGet("WMOS_ID").ToInt() == 1)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "complete_operation",
                    Title = "Завершить",
                    Description = "Завершить операцию",
                    Group = "operation_grid_edit_status",
                    Enabled = false,
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        OperationEditStatus(4);
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (OperationGrid != null && OperationGrid.SelectedItem != null && OperationGrid.SelectedItem.Count > 0)
                        {
                            if (OperationGrid.SelectedItem.CheckGet("WMOS_ID").ToInt() == 2)
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

        private ListDataSet OperationGridDataSet { get; set; }

        public FormHelper Form { get; set; }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            OperationGridDataSet = new ListDataSet();

            WarehouseFilter.Items.Add("0", "Все склады");
            FormHelper.ComboBoxInitHelper(WarehouseFilter, "Warehouse", "Warehouse", "List", "WMWA_ID", "WAREHOUSE", null, true);
            WarehouseFilter.SetSelectedItemByKey("0");

            Dictionary<string, string> statusFilterItems = new Dictionary<string, string>();
            statusFilterItems.Add("0", "Все статусы");
            statusFilterItems.Add("12", "В работе");
            statusFilterItems.Add("1", "Новая");
            statusFilterItems.Add("2", "Принята");
            statusFilterItems.Add("3", "Отменена");
            statusFilterItems.Add("4", "Завершена");
            StatusFilter.AddItems(statusFilterItems);
            StatusFilter.SetSelectedItemByKey("12");

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
        private void OperationGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Description="Идентификатор операции",
                        Path="WMOP_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Создание",
                        Description="Дата создания операции",
                        Path="CREATED_DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm",
                        Width2=12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Тип",
                        Description="Тип операции",
                        Path="OPERATION",
                        ColumnType=ColumnTypeRef.String,
                        Width2=17,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Description="Наименование операции",
                        Path="OPERATION_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=47,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус",
                        Description="Статус задачи",
                        Path="STATUS",
                        ColumnType=ColumnTypeRef.String,
                        Width2=9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид складской единицы",
                        Description="Идентификатор складской единицы по операции",
                        Path="WMIT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Складская единица",
                        Description="Складская единица по операции",
                        Path="ITEM_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=37,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Начальная ячейка",
                        Description="Начальная ячейка по операции",
                        Path="STORAGE_FROM",
                        ColumnType=ColumnTypeRef.String,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Конечная ячейка",
                        Description="Конечная ячейка по операции",
                        Path="STORAGE_TO",
                        ColumnType=ColumnTypeRef.String,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Склад",
                        Description="Склад операции",
                        Path="WAREHOUSE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=11,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Зона",
                        Description="Зона операции",
                        Path="ZONE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=19,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Принятие",
                        Description="Дата принятия операции",
                        Path="ACCEPTED_DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm",
                        Width2=12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Выполнение",
                        Description="Дата выполнения операции",
                        Path="COMPLETED_DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm",
                        Width2=12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Пользователь",
                        Description="Пользователь по операции",
                        Path="FULL_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=13,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид задачи",
                        Description="Идентификатор задачи по операции",
                        Path="WMTA_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование задачи",
                        Description="Наименование задачи по операции",
                        Path="TASK_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=16,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Ид пользователя",
                        Description="Идентификатор пользователя по операции",
                        Path="ACCO_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид начальной ячейки",
                        Description="Идентификатор начальной ячейки по операции",
                        Path="FROM_WMST_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид конечной ячейки",
                        Description="Идентификатор конечной ячейки по операции",
                        Path="TO_WMST_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид статуса",
                        Description="Идентификатор статуса операции",
                        Path="WMOS_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид типа",
                        Description="Идентификатор типа операции",
                        Path="WMOT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид зоны",
                        Description="Идентификатор зоны операции",
                        Path="WMZO_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид склада",
                        Description="Идентификатор склада операции",
                        Path="WMWA_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Описание",
                        Description="Описание операции",
                        Path="DESCRIPTION",
                        ColumnType=ColumnTypeRef.String,
                        Width2=8,
                        Visible=false,
                    },
                };
                OperationGrid.SetColumns(columns);
                OperationGrid.SetPrimaryKey("WMOP_ID");
                OperationGrid.SearchText = ItemsSearchBox;
                //данные грида
                OperationGrid.OnLoadItems = OperationGridLoadItems;
                OperationGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                OperationGrid.AutoUpdateInterval = 60 * 5;
                OperationGrid.Toolbar = OperationGridToolbar;

                // цветовая маркировка строк
                OperationGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                {
                    // Цвета фона строк
                    {
                        DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = string.Empty;

                            switch (row.CheckGet("WMOS_ID").ToInt())
                            {
                                case 1:
                                    break;

                                // Принята
                                case 2:
                                    color = HColor.Blue;
                                    break;

                                // Отменена
                                case 3:
                                    color = HColor.Red;
                                    break;

                                // Выполнена
                                case 4:
                                    color = HColor.Green;
                                    break;

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

                //при выборе строки в гриде, обновляются актуальные действия для записи
                OperationGrid.OnFilterItems = () =>
                {
                    if (OperationGrid.Items != null && OperationGrid.Items.Count > 0)
                    {
                        if (WarehouseFilter != null && WarehouseFilter.SelectedItem.Key != null)
                        {
                            var key = WarehouseFilter.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            switch (key)
                            {
                                // Все склады
                                case 0:
                                    items = OperationGrid.Items;
                                    break;

                                default:
                                    items.AddRange(OperationGrid.Items.Where(x => x.CheckGet("WMWA_ID").ToInt() == key));
                                    break;
                            }

                            OperationGrid.Items = items;
                        }

                        if (StatusFilter != null && StatusFilter.SelectedItem.Key != null)
                        {
                            var key = StatusFilter.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            switch (key)
                            {
                                // Все статусы
                                case 0:
                                    items = OperationGrid.Items;
                                    break;

                                // В работе
                                case 12:
                                    items.AddRange(OperationGrid.Items.Where(x => x.CheckGet("WMOS_ID").ToInt() == 1 || x.CheckGet("WMOS_ID").ToInt() == 2));
                                    break;

                                default:
                                    items.AddRange(OperationGrid.Items.Where(x => x.CheckGet("WMOS_ID").ToInt() == key));
                                    break;
                            }

                            OperationGrid.Items = items;
                        }
                    }
                };

                //при выборе строки в гриде, обновляются актуальные действия для записи
                OperationGrid.OnSelectItem = selectedItem =>
                {

                };

                OperationGrid.Commands = Commander;

                OperationGrid.Init();
            }
        }

        private async void OperationGridLoadItems()
        {
            if (Form.Validate())
            {
                var p = new Dictionary<string, string>();
                p.Add("FROM_DATE", Form.GetValueByPath("FROM_DATE_TIME"));
                p.Add("TO_DATE", Form.GetValueByPath("TO_DATE_TIME"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Warehouse");
                q.Request.SetParam("Object", "Operation");
                q.Request.SetParam("Action", "ListByDate");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                OperationGridDataSet.Items.Clear();
                OperationGridDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        OperationGridDataSet = ListDataSet.Create(result, "ITEMS");
                    }
                }
                OperationGrid.UpdateItems(OperationGridDataSet);
            }
        }

        public void Refresh()
        {
            OperationGrid.LoadItems();
        }

        private void AddOperation()
        {
            var w = new WarehouseOperationItem();
            w.Show();
        }

        private void EditOperation()
        {
            if (OperationGrid.SelectedItem != null && OperationGrid.SelectedItem.Count > 0)
            {
                var w = new WarehouseOperationItem();
                w.OperationId = OperationGrid.SelectedItem.CheckGet("WMOP_ID").ToInt();
                w.Show();
            }
            else
            {
                DialogWindow.ShowDialog($"Не выбрана операция для изменения.", this.ControlTitle);
            }
        }

        private void DeleteOperation()
        {
            if (OperationGrid.SelectedItem != null && OperationGrid.SelectedItem.Count > 0)
            {
                var dw = new DialogWindow($"Вы действительно хотите удалить операцию {OperationGrid.SelectedItem.CheckGet("OPERATION_NAME")}?", this.ControlTitle, "", DialogWindowButtons.NoYes);
                if (dw.ShowDialog() == true)
                {
                    var p = new Dictionary<string, string>();
                    p.Add("WMOP_ID", OperationGrid.SelectedItem.CheckGet("WMOP_ID"));

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Warehouse");
                    q.Request.SetParam("Object", "Operation");
                    q.Request.SetParam("Action", "Delete");
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
                            var dataSet = ListDataSet.Create(result, "ITEMS");
                            if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                            {
                                if (dataSet.Items.First().CheckGet("WMOP_ID").ToInt() > 0)
                                {
                                    succesfullFlag = true;
                                }
                            }
                        }

                        if (succesfullFlag)
                        {
                            Refresh();

                            var msg = "Успешное удаление операции.";
                            var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                        else
                        {
                            var msg = "Ошибка удаления операции. Пожалуйста, сообщите о проблеме.";
                            var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
            }
            else
            {
                DialogWindow.ShowDialog($"Не выбрана операция для удаления.", this.ControlTitle);
            }
        }

        private void OperationEditStatus(int status)
        {
            if (OperationGrid.SelectedItem != null && OperationGrid.SelectedItem.Count > 0)
            {
                string newStatusName = "";
                switch (status)
                {
                    case 2:
                        newStatusName = "Принята";
                        break;

                    case 3:
                        newStatusName = "Отменена";
                        break;

                    case 4:
                        newStatusName = "Завершена";
                        break;

                    default:
                        break;
                }

                var dw = new DialogWindow($"Вы действительно хотите изменить статус операции {OperationGrid.SelectedItem.CheckGet("OPERATION_NAME")} на {newStatusName} ?", this.ControlTitle, "", DialogWindowButtons.NoYes);
                if (dw.ShowDialog() == true)
                {
                    var p = new Dictionary<string, string>();
                    p.Add("WMOP_ID", OperationGrid.SelectedItem.CheckGet("WMOP_ID"));
                    p.Add("WMOS_ID", $"{status}");

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Warehouse");
                    q.Request.SetParam("Object", "Operation");
                    q.Request.SetParam("Action", "UpdateState");
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
                            var dataSet = ListDataSet.Create(result, "ITEMS");
                            if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                            {
                                if (dataSet.Items.First().CheckGet("ID").ToInt() == 0)
                                {
                                    succesfullFlag = true;
                                }
                            }
                        }

                        if (succesfullFlag)
                        {
                            Refresh();

                            var msg = "Успешное изменение статуса операции.";
                            var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                        else
                        {
                            var msg = "Ошибка изменения статуса операции. Пожалуйста, сообщите о проблеме.";
                            var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
            }
            else
            {
                DialogWindow.ShowDialog($"Не выбрана операция для изменения статуса.", this.ControlTitle);
            }
        }

        public void ExportExcel()
        {
            OperationGrid.ItemsExportExcel();
        }

        private void WarehouseFilter_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            OperationGrid.UpdateItems();
        }

        private void StatusFilter_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            OperationGrid.UpdateItems();
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

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.IndexOf("WarehouseControl") > -1)
            {
                //Обновить грид в зависимости от того кто отослал сообщение
                if (m.ReceiverName.IndexOf("WarehouseOperation") > -1)
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            Refresh();
                            OperationGrid.SelectRowByKey(m.Message);
                            break;
                    }
                }
            }
        }
    }
}
