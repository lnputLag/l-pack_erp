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
    public partial class TechnologicalMapDetailedReport : ControlBase
    {
        public TechnologicalMapDetailedReport()
        {
            InitializeComponent();
            ControlTitle = "Отчёт по техкартам";
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
                ReportGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                Refresh();
                ReportGrid.ItemsAutoUpdate = true;
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                ReportGrid.ItemsAutoUpdate = false;
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
                            ReportGrid.ItemsExportExcel();
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
                        Header="Покупатель",
                        Path="NAME_POK",
                        ColumnType=ColumnTypeRef.String,
                        Width2=35,
                    },

                    new DataGridHelperColumn
                    {
                        Header="В разработке",
                        Group="Заявки из расчета цены",
                        Description="Заявки, находящиеся в разработке с расчетом цены",
                        Path="QTY_WEB_PC_WORK",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2=12,
                        DxEnableColumnSorting=false,
                        Totals = (List<Dictionary<string, string>> rows) =>
                        {
                            double result=0;
                            if(rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    result += row.CheckGet("QTY_WEB_PC_WORK").ToDouble();
                                }
                            }
                            return result;
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Готовые",
                        Group="Заявки из расчета цены",
                        Description="Готовые заявки с расчетом цены",
                        Path="QTY_WEB_PC",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2=12,
                        DxEnableColumnSorting=false,
                        Totals = (List<Dictionary<string, string>> rows) =>
                        {
                            double result=0;
                            if(rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    result += row.CheckGet("QTY_WEB_PC").ToDouble();
                                }
                            }
                            return result;
                        },
                    },

                    new DataGridHelperColumn
                    {
                        Header="В разработке",
                        Group="Заявки без расчета цены",
                        Description="Заявки, находящиеся в разработке без расчета цены",
                        Path="QTY_WEB_WORK",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2=12,
                        DxEnableColumnSorting=false,
                        Totals = (List<Dictionary<string, string>> rows) =>
                        {
                            double result=0;
                            if(rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    result += row.CheckGet("QTY_WEB_WORK").ToDouble();
                                }
                            }
                            return result;
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Сделано",
                        Group="Заявки без расчета цены",
                        Description="Готовые заявки без расчета цены",
                        Path="QTY_WEB",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2=12,
                        DxEnableColumnSorting=false,
                        Totals = (List<Dictionary<string, string>> rows) =>
                        {
                            double result=0;
                            if(rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    result += row.CheckGet("QTY_WEB").ToDouble();
                                }
                            }
                            return result;
                        },
                    },

                    new DataGridHelperColumn
                    {
                        Header="Сделано без заявки",
                        Description="Сделано техкарт без заявки",
                        Path="QTY_SELF",
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
                                    result += row.CheckGet("QTY_SELF").ToDouble();
                                }
                            }
                            return result;
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Техкарт создано",
                        Description="Количество созданных техкарт",
                        Path="QTY_TOVAR",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2=14,
                        DxEnableColumnSorting=false,
                        Totals = (List<Dictionary<string, string>> rows) =>
                        {
                            double result=0;
                            if(rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    result += row.CheckGet("QTY_TOVAR").ToDouble();
                                }
                            }
                            return result;
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Техкарт в заявке на отгрузку",
                        Description="Количество техкарт указанных в заявке на отгрузку",
                        Path="QTY_ORDER",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2=21,
                        DxEnableColumnSorting=false,
                        Totals = (List<Dictionary<string, string>> rows) =>
                        {
                            double result=0;
                            if(rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    result += row.CheckGet("QTY_ORDER").ToDouble();
                                }
                            }
                            return result;
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Доля техкарт без заявок, %",
                        Description="Доля техкарт без заявок, %",
                        Path="PERCENT_NOT_ORDER",
                        ColumnType=ColumnTypeRef.String,
                        Width2=21,
                        DxEnableColumnSorting=false,
                        Totals = (List<Dictionary<string, string>> rows) =>
                        {
                            double result=0;
                            int cnt = 0;
                            if(rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    if (!row.CheckGet("PERCENT_NOT_ORDER").IsNullOrEmpty())
                                    {
                                        result += row.CheckGet("PERCENT_NOT_ORDER").ToDouble();
                                        cnt++;
                                    }
                                }
                            }
                            result = Math.Round(result / cnt, 4);
                            return result;
                        },
                    },

                    new DataGridHelperColumn
                    {
                        Header="Запрет заказа ТК без РЦ",
                        Description="Флаг запрета заказа техкарты без расчета цены",
                        Path="BAN_IS",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=23,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата создания",
                        Path="DTTM_CREATED",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format = "dd.MM.yyyy",
                        Width2=11,
                    },
                    new DataGridHelperColumn
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=3,
                        Visible = false
                    },
                };

                ReportGrid.SetColumns(columns);
                ReportGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                {
                    // Цвета фона строк
                    {
                        DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=System.Windows.DependencyProperty.UnsetValue;
                            var color = "";
                            if(row.CheckGet("QTY_TOVAR").ToInt()>=10 && row.CheckGet("PERCENT_NOT_ORDER").ToDouble() >= 50)
                            {
                                    color = HColor.Red;
                            }
                            
                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    }
                };

                ReportGrid.SearchText = GridSearch;
                ReportGrid.OnLoadItems = ReportGridLoadItems;
                ReportGrid.SetPrimaryKey("_ROWNUMBER");
                ReportGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

                ReportGrid.Init();
                ReportGrid.Run();
                ReportGrid.Focus();
            }
        }

        /// <summary>
        /// Получаем данные для заполнения грида
        /// </summary>
        public async void ReportGridLoadItems()
        {
            DisableControls();

            var p = new Dictionary<string, string>();
            p.Add("DT_FROM", Form.GetValueByPath("DT_FROM").ToDateTime().ToString("dd.MM.yyyy"));
            p.Add("DT_TO", Form.GetValueByPath("DT_TO").ToDateTime().AddDays(1).ToString("dd.MM.yyyy"));

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "ProductionPreparationDepartmentReports");
            q.Request.SetParam("Action", "ListForDetailedReport");
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
                    var ds = ListDataSet.Create(result, "ITEMS");
                    ReportGridDataSet = ds;
                    ReportGrid.UpdateItems(ds);
                }

            }

            EnableControls();
        }

        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
            ReportGrid.ShowSplash();
        }

        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
            ReportGrid.HideSplash();
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
