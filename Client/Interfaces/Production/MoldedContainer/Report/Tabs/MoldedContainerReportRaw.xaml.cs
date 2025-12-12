using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
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

namespace Client.Interfaces.Production.MoldedContainer
{
    /// <summary>
    /// Отчёт по сырью на литой таре (приход/расход)
    /// </summary>
    public partial class MoldedContainerReportRaw : ControlBase
    {
        public MoldedContainerReportRaw()
        {
            ControlTitle = "Отчёт по сырью";
            DocumentationUrl = "/doc/l-pack-erp/";
            RoleName = "[erp]molded_contnr_productn_repo";
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
                GridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                Grid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                Grid.ItemsAutoUpdate = true;
                Grid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                Grid.ItemsAutoUpdate = false;
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

                        if (Grid != null && Grid.Items != null && Grid.Items.Count > 0)
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

            Commander.Init(this);
        }

        private ListDataSet GridDataSet { get; set; }

        public FormHelper Form { get; set; }

        private int CurrentWarehouseId = 4;

        private int CurrentZoneId = 8;

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            GridDataSet = new ListDataSet();

            ConsumptionCheckBox.IsChecked = true;
            ArrivalCheckBox.IsChecked = true;

            RawSelectBox.Items.Add("0", "Всё");
            FormHelper.ComboBoxInitHelper(RawSelectBox, "Warehouse", "Inventory", "ListByZone", "ID", "NAME", new Dictionary<string, string>() { { "WMZO_ID", $"{CurrentZoneId}" } }, true);
            RawSelectBox.SetSelectedItemByKey("0");

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
        private void GridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Path="ID",
                        Description="Идентификатор операции",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 7,
                        TotalsType = TotalsTypeRef.Count,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата",
                        Path="DTTM",
                        Description="Дата операции",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format = "dd.MM.yyyy HH:mm:ss",
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Складская единица",
                        Path="ITEM_NAME",
                        Description="Наименование складской единицы",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 25,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Тип",
                        Path="TYPE_NAME",
                        Description="Тип операции",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество, шт.",
                        Path="QUANTITY",
                        Description="Количество по операции",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 12,
                        Totals = (List<Dictionary<string,string>> rows) =>
                        {
                            int summaryConsumptionQuantity = 0;
                            int summaryArrivalQuantity = 0;
                            int balanceQuantity = 0;

                            if (rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    summaryConsumptionQuantity += row["CONSUMPTION_QUANTITY"].ToInt();
                                    summaryArrivalQuantity += row["ARRIVAL_QUANTITY"].ToInt();
                                }
                            }

                            balanceQuantity = summaryArrivalQuantity - summaryConsumptionQuantity;
                            return $"{summaryArrivalQuantity}/{summaryConsumptionQuantity}/{balanceQuantity}";
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вес, кг.",
                        Path="WEIGHT",
                        Description="Вес складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 17,
                        Totals = (List<Dictionary<string,string>> rows) =>
                        {
                            int summaryConsumptionWeight = 0;
                            int summaryArrivalWeight = 0;
                            int balanceWeight = 0;

                            if (rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    summaryConsumptionWeight += row["CONSUMPTION_WEIGHT"].ToInt();
                                    summaryArrivalWeight += row["ARRIVAL_WEIGHT"].ToInt();
                                }
                            }

                            balanceWeight = summaryArrivalWeight - summaryConsumptionWeight;
                            return $"{summaryArrivalWeight}/{summaryConsumptionWeight}/{balanceWeight}";
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Хранилище",
                        Path="STORAGE_NAME",
                        Description="Наименование хранилища по операции",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Пользователь",
                        Path="USER_NAME",
                        Description="Пользователь совершивший операцию",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид скл. ед.",
                        Path="ITEM_ID",
                        Description="Идентификатор складской единицы",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 7,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Ид пользователя",
                        Path="ACCOUNT_ID",
                        Description="Идентификатор пользователя",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид хранилища",
                        Path="STORAGE_ID",
                        Description="Идентификатор хранилища по операции",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номенклатура",
                        Path="INVENTORY_NAME",
                        Description="Номенклатурное наименование",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 5,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид типа",
                        Path="TYPE_ID",
                        Description="Идентификатор типа операции",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид номенклатуры",
                        Path="INVENTORY_ID",
                        Description="Идентификатор номенклатуры",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ед. Изм.",
                        Path="UNIT_SHORT_NAME",
                        Description="Единица измерения",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 5,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Расход, шт.",
                        Path="CONSUMPTION_QUANTITY",
                        Description="Количество в расходе",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Приход, шт.",
                        Path="ARRIVAL_QUANTITY",
                        Description="Количество в приходе",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Расход, кг.",
                        Path="CONSUMPTION_WEIGHT",
                        Description="Вес в расходе",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Приход, кг.",
                        Path="ARRIVAL_WEIGHT",
                        Description="Вес в приходе",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden = true,
                    },

                };
                Grid.SetColumns(columns);
                Grid.SetPrimaryKey("ID");
                Grid.SearchText = GridSearchBox;

                //данные грида
                Grid.OnLoadItems = GridLoadItems;
                Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                Grid.AutoUpdateInterval = 60 * 5;
                Grid.Toolbar = GridToolbar;

                Grid.OnFilterItems = () =>
                {
                    if (Grid.Items != null && Grid.Items.Count > 0)
                    {
                        if (ConsumptionCheckBox.IsChecked != true)
                        {
                            var items = new List<Dictionary<string, string>>();
                            items.AddRange(Grid.Items.Where(x => x.CheckGet("TYPE_ID").ToInt() != 2));
                            Grid.Items = items;
                        }

                        if (ArrivalCheckBox.IsChecked != true)
                        {
                            var items = new List<Dictionary<string, string>>();
                            items.AddRange(Grid.Items.Where(x => x.CheckGet("TYPE_ID").ToInt() != 1));
                            Grid.Items = items;
                        }

                        if (RawSelectBox != null && RawSelectBox.SelectedItem.Key != null)
                        {
                            var key = RawSelectBox.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            switch (key)
                            {
                                // Все
                                case 0:
                                    items = Grid.Items;
                                    break;

                                default:
                                    items.AddRange(Grid.Items.Where(x => x.CheckGet("INVENTORY_ID").ToInt() == key));
                                    break;
                            }

                            Grid.Items = items;
                        }
                    }
                };

                Grid.Commands = Commander;

                Grid.Init();
            }
        }

        /// <summary>
        /// Загрузка данными грида
        /// </summary>
        private async void GridLoadItems()
        {
            Grid.ShowSplash();

            if (Form.Validate())
            {
                var fromDate = Form.GetValueByPath("FROM_DATE_TIME");
                var toDate = Form.GetValueByPath("TO_DATE_TIME");

                if (DateTime.Compare(fromDate.ToDateTime(), toDate.ToDateTime()) > 0)
                {
                    const string msg = "Дата начала должна быть меньше даты окончания.";
                    var d = new DialogWindow($"{msg}", "Проверка данных");
                    d.ShowDialog();

                    return;
                }

                var p = new Dictionary<string, string>();
                p.Add("FROM_DATE", fromDate);
                p.Add("TO_DATE", toDate);
                p.Add("WAREHOUSE_ID", $"{CurrentWarehouseId}");
                p.Add("ZONE_ID", $"{CurrentZoneId}");

                // Server\Modules\Production\MoldedContainer\Report\ReportOperationList.cs
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "MoldedContainer");
                q.Request.SetParam("Object", "Report");
                q.Request.SetParam("Action", "OperationList");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                GridDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        GridDataSet = ListDataSet.Create(result, "ITEMS");
                    }
                }
                Grid.UpdateItems(GridDataSet);
            }

            Grid.HideSplash();
        }

        public void Refresh()
        {
            Grid.LoadItems();
        }

        public async void ExportExcel()
        {
            var totalList = Grid.GetTotals();
            Grid.ItemsExportExcel($"Отчёт по сырью на литой таре с {Form.GetValueByPath("FROM_DATE_TIME")} по {Form.GetValueByPath("TO_DATE_TIME")}. " +
                $"Приход/Расход/Баланс: {totalList.CheckGet("QUANTITY")} шт. {totalList.CheckGet("WEIGHT")} кг.");
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

        private void RawSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid?.UpdateItems();
        }

        private void ConsumptionCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Grid?.UpdateItems();
        }

        private void ConsumptionCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Grid?.UpdateItems();
        }

        private void ArrivalCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Grid?.UpdateItems();
        }

        private void ArrivalCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Grid?.UpdateItems();
        }
    }
}
