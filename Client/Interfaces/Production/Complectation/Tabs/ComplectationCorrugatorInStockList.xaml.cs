using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Stock;
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

namespace Client.Interfaces.Production.Complectation
{
    /// <summary>
    /// Список поддонов, перемещённых в комплектацию ГА на СГП за выбранный период
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class ComplectationCorrugatorInStockList : ControlBase
    {
        public ComplectationCorrugatorInStockList()
        {
            ControlTitle = "Поддоны в К-2";
            DocumentationUrl = "/doc/l-pack-erp/";
            RoleName = "[erp]complectation_list";
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
                PalletGridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                PalletGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                PalletGrid.ItemsAutoUpdate = true;
                PalletGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                PalletGrid.ItemsAutoUpdate = false;
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

                        if (PalletGrid != null && PalletGrid.Items != null && PalletGrid.Items.Count > 0)
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

            Commander.SetCurrentGridName("PalletGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "print_label",
                    Group = "pallet_grid_default",
                    Enabled = true,
                    Title = "Ярлык",
                    Description = "Печать ярлыка для выбранного поддона",
                    MenuUse = true,
                    Action = () =>
                    {
                        PrintLabel(PalletGrid.SelectedItem.CheckGet("PALLET_ID"));
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (PalletGrid != null && PalletGrid.Items != null && PalletGrid.Items.Count > 0)
                        {
                            if (PalletGrid.SelectedItem != null && PalletGrid.SelectedItem.Count > 0)
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

        private ListDataSet PalletGridDataSet { get; set; }

        private FormHelper Form { get; set; }

        private void FormInit()
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
        /// установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            PalletGridDataSet = new ListDataSet();

            {
                var list = new Dictionary<string, string>();
                list.Add("0", "Все смены");
                list.Add("1", "Смена 1");
                list.Add("2", "Смена 2");
                list.Add("3", "Смена 3");
                list.Add("4", "Смена 4");
                WorkTeamSelectBox.Items = list;
                WorkTeamSelectBox.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");
            }

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

        private void PalletGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 4,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид поддона",
                        Path="PALLET_ID",
                        Description="Идентификатор поддона",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Поддон",
                        Path="PALLET_FULL_NUMBER",
                        Description="Полный номер поддона",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Path="QUANTITY",
                        Description="Количество на поддоне, шт.",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул",
                        Path="PRODUCT_CODE",
                        Description="Артикул продукции",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 15,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Продукция",
                        Path="PRODUCT_NAME",
                        Description="Наименование продукции",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 40,
                    },
                    new DataGridHelperColumn
                    {
                        Header="В К-2",
                        Path="IN_DTTM",
                        Description="Дата перемещения в ячейку комплектации ГА на СГП",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Из К-2",
                        Path="OUT_DTTM",
                        Description="Дата перемещения из ячейки комплектации ГА на СГП",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Продолжительность в К-2",
                        Path="SPENT_TM",
                        Description="Время, которое поддон провёл в ячейке комплектации ГА на СГП",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="HH:mm:ss",
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Причина попадания",
                        Path="REASON",
                        Description="Причина попадания поддона в ячейку комплектации ГА на СГП",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 64,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Смена",
                        Path="SHIFT_NUMBER",
                        Description="Номер производственной смены",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Станок",
                        Path="MACHINE_NAME",
                        Description="Станок, на котором произвели этот поддон",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата оприходования",
                        Path="CREATED_DTTM",
                        Description="Дата оприходования поддона",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2 = 14,
                    },                    

                    new DataGridHelperColumn
                    {
                        Header="Ид прихода",
                        Path="INCOMING_ID",
                        Description="Идентификатор прихода",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 4,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид станка",
                        Path="MACHINE_ID",
                        Description="Идентификатор станка, на котором произвели этот поддон",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 4,
                        Visible = false,
                    },
                };
                PalletGrid.SetColumns(columns);
                PalletGrid.SetPrimaryKey("_ROWNUMBER");
                PalletGrid.SearchText = SearchText;
                PalletGrid.OnLoadItems = PalletGridLoadItems;
                PalletGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                PalletGrid.AutoUpdateInterval = 60 * 5;
                PalletGrid.Toolbar = GridToolbar;
                PalletGrid.OnFilterItems = () =>
                {
                    if (PalletGrid.Items != null && PalletGrid.Items.Count > 0)
                    {
                        if (WorkTeamSelectBox != null && WorkTeamSelectBox.SelectedItem.Key != null)
                        {
                            var key = WorkTeamSelectBox.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            switch (key)
                            {
                                // Все склады
                                case 0:
                                    items = PalletGrid.Items;
                                    break;

                                default:
                                    items.AddRange(PalletGrid.Items.Where(x => x.CheckGet("SHIFT_NUMBER").ToInt() == key));
                                    break;
                            }

                            PalletGrid.Items = items;
                        }
                    }
                };
                PalletGrid.Commands = Commander;
                PalletGrid.Init();
            }
        }

        /// <summary>
        /// Загрузка данными грида
        /// </summary>
        private async void PalletGridLoadItems()
        {
            SplashControl.Visible = true;

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
                    q.Request.SetParam("Module", "Complectation");
                    q.Request.SetParam("Object", "Operation");
                    q.Request.SetParam("Action", "ListCorrugatorInStockByDttm");
                    q.Request.SetParams(p);

                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    PalletGridDataSet = new ListDataSet();
                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            PalletGridDataSet = ListDataSet.Create(result, "ITEMS");
                        }
                    }
                    PalletGrid.UpdateItems(PalletGridDataSet);
                }
            }

            SplashControl.Visible = false;
        }

        private async void ExportExcel()
        {
            PalletGrid.ItemsExportExcel();
        }

        /// <summary>
        /// Установка настроек для принтера
        /// </summary>
        private void SetPrintSettings()
        {
            LabelReport2.SetPrintingProfile();
        }

        private void PrintLabel(string palletId)
        {
            LabelReport2 report = new LabelReport2(true);
            report.PrintLabel(palletId);
        }

        public void Refresh()
        {
            PalletGrid.LoadItems();
        }

        private void WorkTeamSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PalletGrid.UpdateItems();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            SetPrintSettings();
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
