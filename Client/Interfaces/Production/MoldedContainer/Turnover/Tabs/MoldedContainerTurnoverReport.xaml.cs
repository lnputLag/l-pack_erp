using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
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

namespace Client.Interfaces.Production.MoldedContainer
{
    /// <summary>
    /// Оборотная ведомость литой тары
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class MoldedContainerTurnoverReport : ControlBase
    {
        public MoldedContainerTurnoverReport()
        {
            ControlTitle = "Оборотная ведомость ЛТ";
            DocumentationUrl = "/doc/l-pack-erp/";
            RoleName = "[erp]molded_contnr_turnover";
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
                        Path="FROM_DATE",
                        FieldType=FormHelperField.FieldTypeRef.Date,
                        Control=FromDateTime,
                        ControlType="dateedit",
                        Format="dd.MM.yyyy",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        }
                    },
                    new FormHelperField()
                    {
                        Path="TO_DATE",
                        FieldType=FormHelperField.FieldTypeRef.Date,
                        Control=ToDateTime,
                        ControlType="dateedit",
                        Format="dd.MM.yyyy",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        }
                    },
                };

                Form.SetFields(fields);
            }
        }

        public void SetDefaults()
        {
            GridDataSet = new ListDataSet();

            Form.SetValueByPath("FROM_DATE", $"{DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy")}");
            Form.SetValueByPath("TO_DATE", $"{DateTime.Now.ToString("dd.MM.yyyy")}");

            var productCategorySelectBox = new Dictionary<string, string>();
            productCategorySelectBox.Add("-1", "Все типы продукции");
            productCategorySelectBox.Add("121", "Макулатура");
            productCategorySelectBox.Add("124", "Отходы ЛТ");
            productCategorySelectBox.Add("26", "Заготовка литой тары");
            productCategorySelectBox.Add("16", "ГП литой тары");
            ProductCategorySelectBox.SetItems(productCategorySelectBox);
            ProductCategorySelectBox.SetSelectedItemByKey("-1");
        }

        public void Refresh()
        {
            Grid.LoadItems();
        }

        public void GridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Description = "Идентификатор номенклатуры",
                        Path="INVENTORY_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Description = "Номенклатурное наименование",
                        Path="INVENTORY_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Description = "Суммарное количество на начало периода, шт.",
                        Path="BEFORE_QUANTITY",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Group="На начало периода",
                        Width2 = 10,
                        TotalsType = TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вес",
                        Description = "Суммарный вес на начало периода, кг.",
                        Path="BEFORE_WEIGHT",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Group="На начало периода",
                        Width2 = 10,
                        TotalsType = TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Description = "Суммарное количество в приходе, шт.",
                        Path="ARRIVAL_QUANTITY",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Group="Приход",
                        Width2 = 10,
                        TotalsType = TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вес",
                        Description = "Суммарный вес в приходе, кг.",
                        Path="ARRIVAL_WEIGHT",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Group="Приход",
                        Width2 = 10,
                        TotalsType = TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Description = "Суммарное количество в расходе, шт.",
                        Path="CONSUMPTION_QUANTITY",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Group="Расход",
                        Width2 = 10,
                        TotalsType = TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вес",
                        Description = "Суммарный вес в расходе, кг.",
                        Path="CONSUMPTION_WEIGHT",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Group="Расход",
                        Width2 = 10,
                        TotalsType = TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Description = "Суммарное количество на конец периода, шт.",
                        Path="QUANTITY",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Group="На конец периода",
                        Width2 = 10,
                        TotalsType = TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вес",
                        Description = "Суммарный вес на конец периода, кг.",
                        Path="WEIGHT",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Group="На конец периода",
                        Width2 = 10,
                        TotalsType = TotalsTypeRef.Summ,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Ид категори",
                        Description = "Идентификатор категории продукции",
                        Path="KATEGORY_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 5,
                        Hidden=true,
                    },

                };
                Grid.SetColumns(columns);
                Grid.SetPrimaryKey("INVENTORY_ID");
                Grid.SearchText = GridSearchBox;
                //данные грида
                Grid.OnLoadItems = GridLoadItems;
                Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                Grid.AutoUpdateInterval = 60 * 5;
                Grid.Toolbar = GridToolbar;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                Grid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem != null && selectedItem.Count > 0)
                    {
                        if (Grid != null && Grid.Items != null && Grid.Items.Count > 0)
                        {
                            if (Grid.Items.FirstOrDefault(x => x.CheckGet("INVENTORY_ID").ToInt() == selectedItem.CheckGet("INVENTORY_ID").ToInt()) == null)
                            {
                                Grid.SelectRowFirst();
                            }
                        }
                    }
                };

                Grid.OnFilterItems = GridFilterItems;

                Grid.Commands = Commander;

                Grid.Init();
            }
        }

        public async void GridLoadItems()
        {
            Grid.ShowSplash();

            if (Form.Validate())
            {
                var fromDate = Form.GetValueByPath("FROM_DATE");
                var toDate = Form.GetValueByPath("TO_DATE");

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

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "MoldedContainer");
                q.Request.SetParam("Object", "Turnover");
                q.Request.SetParam("Action", "List");
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

        public void GridFilterItems()
        {
            if (Grid.Items != null && Grid.Items.Count > 0)
            {
                // Фильтрация накладных по покупателю
                // -1 -- Все покупатели
                if (ProductCategorySelectBox.SelectedItem.Key != null)
                {
                    var key = ProductCategorySelectBox.SelectedItem.Key.ToInt();
                    var items = new List<Dictionary<string, string>>();

                    switch (key)
                    {
                        // Все категории продукции
                        case -1:
                            items = Grid.Items;
                            break;

                        default:
                            items.AddRange(Grid.Items.Where(x => x.CheckGet("KATEGORY_ID").ToInt() == key));
                            break;
                    }

                    Grid.Items = items;
                }
            }

            if (Grid != null && Grid.SelectedItem != null && Grid.SelectedItem.Count > 0)
            {
                Grid.Commands.Message = new ItemMessage() { Action = "refresh", Message = $"{Grid.SelectedItem.CheckGet("INVENTORY_ID")}" };
            }
        }

        public async void ExportExcel()
        {
            var totalList = Grid.GetTotals();
            Grid.ItemsExportExcel($"Отчёт по обороту на литой таре с {Form.GetValueByPath("FROM_DATE")} по {Form.GetValueByPath("TO_DATE")}. " +
                $"На начало периода/Приход/Расход/На конец периода: {totalList.CheckGet("BEFORE_QUANTITY")}/{totalList.CheckGet("ARRIVAL_QUANTITY")}/{totalList.CheckGet("CONSUMPTION_QUANTITY")}/{totalList.CheckGet("QUANTITY")} шт. {totalList.CheckGet("BEFORE_WEIGHT")}/{totalList.CheckGet("ARRIVAL_WEIGHT")}/{totalList.CheckGet("CONSUMPTION_WEIGHT")}/{totalList.CheckGet("WEIGHT")} кг.");
        }

        private void OnCurrentDay(object sender, RoutedEventArgs e)
        {
            Form.SetValueByPath("FROM_DATE", $"{DateTime.Now.ToString("dd.MM.yyyy")}");
            Form.SetValueByPath("TO_DATE", $"{DateTime.Now.ToString("dd.MM.yyyy")}");

            Refresh();
        }

        private void OnPrevDay(object sender, RoutedEventArgs e)
        {
            Form.SetValueByPath("FROM_DATE", $"{DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy")}");
            Form.SetValueByPath("TO_DATE", $"{DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy")}");

            Refresh();
        }

        private void OnCurrentWeek(object sender, RoutedEventArgs e)
        {
            DayOfWeek day = DateTime.Now.DayOfWeek;
            int days = day - DayOfWeek.Monday;
            DateTime date = DateTime.Now.AddDays(-days);

            Form.SetValueByPath("FROM_DATE", $"{date.Date.ToString("dd.MM.yyyy")}");
            Form.SetValueByPath("TO_DATE", $"{date.Date.AddDays(7).ToString("dd.MM.yyyy")}");

            Refresh();
        }

        private void OnPrevWeek(object sender, RoutedEventArgs e)
        {
            DayOfWeek day = DateTime.Now.DayOfWeek;
            int days = day - DayOfWeek.Monday;
            DateTime date = DateTime.Now.AddDays(-days).AddDays(-7);

            Form.SetValueByPath("FROM_DATE", $"{date.Date.ToString("dd.MM.yyyy")}");
            Form.SetValueByPath("TO_DATE", $"{date.Date.AddDays(7).ToString("dd.MM.yyyy")}");

            Refresh();
        }

        private void OnCurrentMonth(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now;

            Form.SetValueByPath("FROM_DATE", $"{new DateTime(date.Year, date.Month, 1).ToString("dd.MM.yyyy")}");
            Form.SetValueByPath("TO_DATE", $"{new DateTime(date.Year, date.Month, 1).AddMonths(1).ToString("dd.MM.yyyy")}");

            Refresh();
        }

        private void OnPrevMonth(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now.AddMonths(-1);

            Form.SetValueByPath("FROM_DATE", $"{new DateTime(date.Year, date.Month, 1).ToString("dd.MM.yyyy")}");
            Form.SetValueByPath("TO_DATE", $"{new DateTime(date.Year, date.Month, 1).AddMonths(1).ToString("dd.MM.yyyy")}");

            Refresh();
        }

        private void ProductKategorySelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }
    }
}
