using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using DevExpress.XtraExport.Csv;
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
    /// Отчёт по разработке чертежей
    /// </summary>
    public partial class TechnologicalMapConstructStatusReport : ControlBase
    {
        public TechnologicalMapConstructStatusReport()
        {
            InitializeComponent();
            ControlTitle = "Отчёт по разработке чертежей";
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
                StatusReportGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                Refresh();
                StatusReportGrid.ItemsAutoUpdate = true;
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                StatusReportGrid.ItemsAutoUpdate = false;
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
                            StatusReportGrid.ItemsExportExcel();
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
        /// Основной датасет
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
                        Header="Поступило",
                        Group="Статус",
                        Description="Поступило техкарт с чертежами",
                        Path="CREATED_CNT",
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
                                    result += row.CheckGet("CREATED_CNT").ToDouble();
                                }
                            }
                            return result;
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Принято",
                        Group="Статус",
                        Description="Принято в работу техкарт с чертежами",
                        Path="ACCEPTANCE_CNT",
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
                                    result += row.CheckGet("ACCEPTANCE_CNT").ToDouble();
                                }
                            }
                            return result;
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Не принято",
                        Group="Статус",
                        Description="Не принято в работу техкарт с чертежами",
                        Path="TODO_CNT",
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
                                    result += row.CheckGet("TODO_CNT").ToDouble();
                                }
                            }
                            return result;
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Отменено",
                        Group="Статус",
                        Description="Отменено техкарт с чертежами",
                        Path="CANCEL_CNT",
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
                                    result += row.CheckGet("CANCEL_CNT").ToDouble();
                                }
                            }
                            return result;
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Выполнено",
                        Group="Статус",
                        Description="Выполнено техкарт с чертежами",
                        Path="DONE_CNT",
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
                                    result += row.CheckGet("DONE_CNT").ToDouble();
                                }
                            }
                            return result;
                        },
                    },

                };

                StatusReportGrid.SetColumns(columns);
                StatusReportGrid.OnLoadItems = ReportGridLoadItems;
                StatusReportGrid.SetPrimaryKey("_ROWNUMBER");
                StatusReportGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

                StatusReportGrid.Init();
                StatusReportGrid.Run();
                StatusReportGrid.Focus();
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
            p.Add("DT_TO", Form.GetValueByPath("DT_TO").ToDateTime().ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "ProductionPreparationDepartmentReports");
            q.Request.SetParam("Action", "ListForConstructStatusReport");
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
            StatusReportGrid.UpdateItems(ReportGridDataSet);
            StatusReportGrid.Focus();

            EnableControls();
        }

        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
            StatusReportGrid.ShowSplash();
        }

        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
            StatusReportGrid.HideSplash();
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
