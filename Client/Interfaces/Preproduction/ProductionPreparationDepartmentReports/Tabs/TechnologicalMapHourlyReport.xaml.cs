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
    /// Почасовой срез выполненных дизайнерами и конструкторами техкарт
    /// </summary>
    public partial class TechnologicalMapHourlyReport : ControlBase
    {
        public TechnologicalMapHourlyReport()
        {
            InitializeComponent();
            ControlTitle = "Почасовой срез";
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
                HourlyReportGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                Refresh();
                HourlyReportGrid.ItemsAutoUpdate = true;
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                HourlyReportGrid.ItemsAutoUpdate = false;
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
                            HourlyReportGrid.ItemsExportExcel();
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
        /// <summary>
        /// Датасет со списком техкарт и их статусом
        /// </summary>
        public ListDataSet ReportDesignDoneGridDataSet { get; set; }

        /// <summary>
        /// Датасет выполненных расчетов оснастки
        /// </summary>
        public ListDataSet ReportRigCalcDataSet { get; set; }

        /// <summary>
        /// Датасет выполненных образцов
        /// </summary>
        public ListDataSet ReportSampleDataSet { get; set; }

        /// <summary>
        /// Датасет рабочих дней
        /// </summary>
        public ListDataSet WorkDays { get; set; }

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
            ReportDesignDoneGridDataSet = new ListDataSet();
            ReportRigCalcDataSet = new ListDataSet();
            ReportSampleDataSet = new ListDataSet();
            Form.SetValueByPath("DT_FROM", DateTime.Now.ToString("dd.MM.yyyy"));
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
                        Path="DATE",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format = "dd.MM.yyyy",
                        Width2=11,
                        DxEnableColumnSorting=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Время",
                        Path="TIME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=11,
                        DxEnableColumnSorting=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дизайнеры",
                        Group="Количество выполненных техкарт",
                        Description="Количество выполненных техкарт дизайнерами",
                        Path="DESIGNERS_DONE",
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
                                    result += row.CheckGet("DESIGNERS_DONE").ToDouble();
                                }
                            }
                            return result;
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Конструкторы",
                        Group="Количество выполненных техкарт",
                        Description="Количество выполненных техкарт конструкторами",
                        Path="CONSTRUCTOR_DONE",
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
                                    result += row.CheckGet("CONSTRUCTOR_DONE").ToDouble();
                                }
                            }
                            return result;
                        },
                    },

                    new DataGridHelperColumn
                    {
                        Header="Дизайнеры",
                        Group="Количество выполненных расчётов оснастки",
                        Description="Количество выполненных расчётов оснастки дизайнерами",
                        Path="DESIGNERS_RIG_DONE",
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
                                    result += row.CheckGet("DESIGNERS_RIG_DONE").ToDouble();
                                }
                            }
                            return result;
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Конструкторы",
                        Group="Количество выполненных расчётов оснастки",
                        Description="Количество выполненных расчётов оснастки конструкторами",
                        Path="CONSTRUCTOR_RIG_DONE",
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
                                    result += row.CheckGet("CONSTRUCTOR_RIG_DONE").ToDouble();
                                }
                            }
                            return result;
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="",
                        Group="Кол-во выполненных образцов",
                        Description="Количество выполненных образцов с завершенным чертежом",
                        Path="SAMPLE_COUNT",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2=30,
                        DxEnableColumnSorting=false,
                        Totals = (List<Dictionary<string, string>> rows) =>
                        {
                            double result=0;
                            if(rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    result += row.CheckGet("SAMPLE_COUNT").ToDouble();
                                }
                            }
                            return result;
                        },
                    },
                };

                HourlyReportGrid.SetColumns(columns);
                HourlyReportGrid.OnLoadItems = ReportGridLoadItems;
                HourlyReportGrid.SetPrimaryKey("_ROWNUMBER");
                HourlyReportGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

                HourlyReportGrid.Init();
                HourlyReportGrid.Run();
                HourlyReportGrid.Focus();
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
            q.Request.SetParam("Action", "ListForReports");
            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            ReportDesignDoneGridDataSet = new ListDataSet();
            ReportRigCalcDataSet = new ListDataSet();
            ReportSampleDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var reportList = new ListDataSet();
                    ReportDesignDoneGridDataSet = ListDataSet.Create(result, "DESIGN_DONE");
                    ReportRigCalcDataSet = ListDataSet.Create(result, "RIG_DONE");
                    ReportSampleDataSet = ListDataSet.Create(result, "SAMPLE");
                    var start_day = Form.GetValueByPath("DT_FROM").ToDateTime();
                    var end_day = Form.GetValueByPath("DT_TO").ToDateTime();
                    while (start_day <= end_day)
                    {
                        DateTime dateTime = start_day;
                        int hour = 7;
                        while (hour < 20)
                        {
                            int cntDesignDesigner = 0;
                            int cntDesignConstructor = 0;
                            int cntRigDesigner = 0;
                            int cntRigConstructor = 0;
                            int cntSample = 0;

                            DateTime dtStart = dateTime.AddHours(hour);
                            DateTime dtEnd = dateTime.AddHours(hour + 1);
                            foreach (var item in ReportRigCalcDataSet.Items)
                            {
                                if (item.CheckGet("DESIGNER_DTTM").ToDateTime() <= dtEnd
                                    && item.CheckGet("DESIGNER_DTTM").ToDateTime() >= dtStart)
                                {
                                    cntRigDesigner += 1;
                                }
                                if (item.CheckGet("CONSTRUCTOR_DTTM").ToDateTime() <= dtEnd
                                    && item.CheckGet("CONSTRUCTOR_DTTM").ToDateTime() >= dtStart)
                                {
                                    cntRigConstructor += 1;
                                }
                            }
                            foreach (var item in ReportDesignDoneGridDataSet.Items)
                            {
                                DateTime des = item.CheckGet("DESIGNER_DONE_DTTM").ToDateTime();
                                if (item.CheckGet("DESIGNER_DONE_DTTM").ToDateTime() <= dtEnd
                                    && item.CheckGet("DESIGNER_DONE_DTTM").ToDateTime() >= dtStart)
                                {
                                    cntDesignDesigner += 1;
                                }
                                if (item.CheckGet("CONSTRUCTOR_DONE_DTTM").ToDateTime() <= dtEnd
                                    && item.CheckGet("CONSTRUCTOR_DONE_DTTM").ToDateTime() >= dtStart)
                                {
                                    cntDesignConstructor += 1;
                                }
                            }
                            foreach (var item in ReportSampleDataSet.Items)
                            {
                                if (item.CheckGet("END_DESIGN_DTTM").ToDateTime() <= dtEnd
                                    && item.CheckGet("END_DESIGN_DTTM").ToDateTime() >= dtStart)
                                {
                                    cntSample += 1;
                                }
                            }
                            var hourReport = new Dictionary<string, string>()
                            {
                                { "DATE", dtStart.ToString("dd.MM.yyyy") },
                                { "TIME", dtStart.ToString("HH:mm")+" - " + dtEnd.ToString("HH:mm")},
                                { "DESIGNERS_DONE", cntDesignDesigner.ToString() },
                                { "CONSTRUCTOR_DONE", cntDesignConstructor.ToString() },
                                { "DESIGNERS_RIG_DONE", cntRigDesigner.ToString() },
                                { "CONSTRUCTOR_RIG_DONE", cntRigConstructor.ToString() },
                                { "SAMPLE_COUNT", cntSample.ToString() },

                            };
                            reportList.Items.Add(hourReport);
                            hour += 1;
                        }
                        start_day = start_day.AddDays(1);

                    }

                    ReportGridDataSet = reportList;

                }
            }
            HourlyReportGrid.UpdateItems(ReportGridDataSet);
            HourlyReportGrid.Focus();

            EnableControls();
        }

        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
            HourlyReportGrid.ShowSplash();
        }

        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
            HourlyReportGrid.HideSplash();
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
