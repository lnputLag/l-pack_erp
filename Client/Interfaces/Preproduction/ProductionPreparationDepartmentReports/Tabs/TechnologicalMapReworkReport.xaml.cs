using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Данные по расходу картона (композиций) на заданный диапазон дат
    /// </summary>
    public partial class TechnologicalMapReworkReport : ControlBase
    {
        public TechnologicalMapReworkReport()
        {
            InitializeComponent();
            ControlTitle = "Отчёт по доработкам";
            RoleName = "[erp]reports_technological_map";

            OnMessage = (ItemMessage message) => {

                DebugLog($"message=[{message.Message}]");

                if (message.ReceiverName == ControlName)
                {
                    ProcessCommand(message.Action);
                }
            };

            //конструктор, будет вызван, когда объект создается
            //здесь создаются все внутренние структуры
            //впервые этот коллбэк будет вызван, когда данный таб станет активным
            //впервые (до этих пор, никакая работа внутри не происходит, что экономит ресурсы)
            OnLoad = () =>
            {
                FormInit();
                ReportGridInit();
                SetDefaults();
                ProcessPermissions();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                ReworkReportGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                Refresh();
                ReworkReportGrid.ItemsAutoUpdate = true;
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                ReworkReportGrid.ItemsAutoUpdate = false;
            };

            Commander.SetCurrentGridName("TechnologicalMapListGrid");
            {
                Commander.SetCurrentGroup("item");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "refresh",
                        Group = "refresh",
                        Enabled = true,
                        Title = "Показать",
                        Description = "Обновить таблицу техкарт",
                        ButtonUse = true,
                        ButtonName = "RefreshButton",
                        MenuUse = true,
                        Action = () =>
                        {
                            ReportGridLoadItems();
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "export_to_excel",
                        Group = "grid_excel",
                        Enabled = true,
                        Title = "В Excel",
                        Description = "Выгрузить данные в Excel файл",
                        ButtonUse = true,
                        ButtonName = "ExportToExcelButton",
                        Action = () =>
                        {
                            ReworkReportGrid.ItemsExportExcel();
                        },
                    });
                }

            }
            Commander.Init(this);
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Основной датасет с данными по доработкам
        /// </summary>
        public ListDataSet ReportGridDataSet { get; set; }

        public void FormInit()
        {
            //инициализация формы
            {
                Form = new FormHelper();

                //список колонок формы
                var fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path = "DT_FROM",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = DtFrom,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "DT_TO",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = DtTo,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                };

                Form.SetFields(fields);
            }
        }

        public void SetDefaults()
        {
            ReportGridDataSet = new ListDataSet();
            Form.SetValueByPath("DT_FROM", DateTime.Now.AddMonths(-1).ToString("dd.MM.yyyy"));
            Form.SetValueByPath("DT_TO", DateTime.Now.ToString("dd.MM.yyyy"));

            Refresh();
        }

        public void Refresh()
        {
            RefreshButton.Style = (Style)RefreshButton.TryFindResource("Button");
            ReportGridLoadItems();
        }

        /// <summary>
        /// Инициализация грида
        /// </summary>
        public void ReportGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {

                    new DataGridHelperColumn
                    {
                        Header="Дата",
                        Path="DT",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format = "dd.MM.yyyy",
                        Width2=11,
                        DxEnableColumnSorting=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Кол-во доработок",
                        Description="Всего техкарт, отправленоных на доработку",
                        Path="REWORKS",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2=16,
                        DxEnableColumnSorting=false,
                        Totals = (List<Dictionary<string, string>> rows) =>
                        {
                            double result=0;
                            if(rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    result += row.CheckGet("REWORKS").ToInt();
                                }
                            }
                            return result;
                        },
                    },

                    new DataGridHelperColumn
                    {
                        Header="Изменение дизайна",
                        Group="Причина",
                        Description="Отправлено на доработку по причине изменения дизайна",
                        Path="REWORK_DESIGN",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2=17,
                        DxEnableColumnSorting=false,
                        Totals = (List<Dictionary<string, string>> rows) =>
                        {
                            double result=0;
                            if(rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    result += row.CheckGet("REWORK_DESIGN").ToDouble();
                                }
                            }
                            return result;
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Корректировка запроса",
                        Group="Причина",
                        Description="Отправлено на доработку по причине корректировки запроса",
                        Path="REWORK_CHANGE",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2=20,
                        DxEnableColumnSorting=false,
                        Totals = (List<Dictionary<string, string>> rows) =>
                        {
                            double result=0;
                            if(rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    result += row.CheckGet("REWORK_CHANGE").ToDouble();
                                }
                            }
                            return result;
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Неполные/некорректные данные",
                        Group="Причина",
                        Description="Отправлено на доработку по причине неполных/некоректных данных",
                        Path="REWORK_CORRECTION",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2=27,
                        DxEnableColumnSorting=false,
                        Totals = (List<Dictionary<string, string>> rows) =>
                        {
                            double result=0;
                            if(rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    result += row.CheckGet("REWORK_CORRECTION").ToDouble();
                                }
                            }
                            return result;
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Другое",
                        Group="Причина",
                        Description="Отправлено на доработку по другой причине",
                        Path="REWORK_OTHER",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2=10,
                        DxEnableColumnSorting=false,
                        Totals = (List<Dictionary<string, string>> rows) =>
                        {
                            double result=0;
                            if(rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    result += row.CheckGet("REWORK_OTHER").ToDouble();
                                }
                            }
                            return result;
                        },
                    },
                };

                ReworkReportGrid.SetColumns(columns);
                ReworkReportGrid.OnLoadItems = ReportGridLoadItems;
                ReworkReportGrid.SetPrimaryKey("_ROWNUMBER");
                ReworkReportGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

                ReworkReportGrid.Init();
                ReworkReportGrid.Run();
                ReworkReportGrid.Focus();
            }
        }

        /// <summary>
        /// Получаем данные для заполнения грида
        /// </summary>
        public async void ReportGridLoadItems()
        {
            DisableControls();

            var p = new Dictionary<string, string>();
            p.Add("DT_FROM", Form.GetValueByPath("DT_FROM").ToDateTime().ToString());
            p.Add("DT_TO", Form.GetValueByPath("DT_TO").ToDateTime().AddDays(1).ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "ProductionPreparationDepartmentReports");
            q.Request.SetParam("Action", "ListReworks");
            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            ReportGridDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    ReportGridDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            ReworkReportGrid.UpdateItems(ReportGridDataSet);
            ReworkReportGrid.Focus();

            EnableControls();
        }

        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
            ReworkReportGrid.ShowSplash();
        }

        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
            ReworkReportGrid.HideSplash();
        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            
        }

        public void ProcessCommand(string command)
        {
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "refresh":
                        {
                            Refresh();
                        }
                        break;

                    case "help":
                        {
                            Central.ShowHelp("/doc/l-pack-erp/preproduction/preproduction_confirm_order/");
                        }
                        break;
                }
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessCommand("help");
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }


        private void FactorySelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RefreshButton.Style = (Style)RefreshButton.TryFindResource("FButtonPrimary");
        }
    }
}
