using Client.Common;
using Client.Interfaces.Main;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Xpf;
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
using System.Windows.Input;
using System.Windows.Navigation;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Отчёт по первым перемещениям готовой продукции с сигнода (обвязчика)
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class ReportForkliftDriverFirstMovingFromStrapper : ControlBase
    {
        public ReportForkliftDriverFirstMovingFromStrapper()
        {
            ControlTitle = "Перемещения с сигнода";
            RoleName = "[erp]warehouse_report";
            DocumentationUrl = "/doc/l-pack-erp/";
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
                FirstMovingGridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                FirstMovingGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                FirstMovingGrid.ItemsAutoUpdate = true;
                FirstMovingGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                FirstMovingGrid.ItemsAutoUpdate = false;
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

            Commander.SetCurrentGridName("FirstMovingGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "export_to_excel",
                    Title = "В Excel",
                    Description = "Экспортировать в Excel",
                    Group = "first_moving_grid_excel",
                    Enabled = true,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = ExportToExcelButton,
                    ButtonName = "ExportToExcelButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        ExcelReport();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "collapse",
                    Title = "Свернуть",
                    Description = "Свернуть все записи",
                    Group = "first_moving_grid_default",
                    Enabled = true,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = CollapseButton,
                    ButtonName = "CollapseButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        FirstMovingGrid.GridControl.CollapseAllGroups();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "expand",
                    Title = "Развернуть",
                    Description = "Развернуть все записи",
                    Group = "first_moving_grid_default",
                    Enabled = true,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = ExpandButton,
                    ButtonName = "ExpandButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        FirstMovingGrid.GridControl.ExpandAllGroups();
                    },
                });
            }

            Commander.Init(this);
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        private ListDataSet FirstMovingDataSet { get; set; }

        public int FactoryId = 1;

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
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            FirstMovingDataSet = new ListDataSet();

            Form.SetValueByPath("FROM_DATE", DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy"));
            Form.SetValueByPath("TO_DATE", DateTime.Now.ToString("dd.MM.yyyy"));

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

        public void FirstMovingGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Водитель",
                        Path="STAFF_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Path="MOVING_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата перемещения",
                        Path="PLACED_DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2=15,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Продукция",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=44,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Поддон",
                        Path="PALLET_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Место",
                        Path="PLACE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="В автомате",
                        Path="AUTO_SELECT_PLACE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Автоматический выбор ячейки",
                        Path="AUTO_SELECT_PLACE_FLAG",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                        Visible=false,
                    },

                    new DataGridHelperColumn
                    {
                        Header=" ",
                        Path="_",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=2,
                        MaxWidth=2000,
                    },
                };
                FirstMovingGrid.SetColumns(columns);
                FirstMovingGrid.SetPrimaryKey("MOVING_ID");
                FirstMovingGrid.AutoUpdateInterval = 5 * 60;
                FirstMovingGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                FirstMovingGrid.Toolbar = GridToolbar;
                FirstMovingGrid.SearchText = SearchText;
                FirstMovingGrid.OnLoadItems = FirstMovingGridLoadItems;
                FirstMovingGrid.Commands = Commander;
                FirstMovingGrid.Init();

                // Общие настройки
                {
                    Style style = (Style)this.TryFindResource("MyGroupSummaryContentStyle");
                    if (style != null)
                    {
                        FirstMovingGrid.GridView.GroupSummaryContentStyle = style;
                    }
                    FirstMovingGrid.GridControl.CustomSummary += GridControl_CustomSummary;
                }

                // Агрегатные функции
                {
                    {
                        DevExpress.Xpf.Grid.GridSummaryItem groupSummaryItem = new DevExpress.Xpf.Grid.GridSummaryItem();
                        groupSummaryItem.FieldName = "MOVING_ID";
                        groupSummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Count;
                        groupSummaryItem.DisplayFormat = "Количество перемещений: {0}";
                        groupSummaryItem.Alignment = DevExpress.Xpf.Grid.GridSummaryItemAlignment.Left;
                        FirstMovingGrid.GridControl.GroupSummary.Add(groupSummaryItem);
                    }

                    {
                        DevExpress.Xpf.Grid.GridSummaryItem groupSummaryItem = new DevExpress.Xpf.Grid.GridSummaryItem();
                        groupSummaryItem.FieldName = "AUTO_SELECT_PLACE_FLAG";
                        groupSummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Custom;
                        groupSummaryItem.DisplayFormat = "(По программе: {0} шт.";
                        groupSummaryItem.Alignment = DevExpress.Xpf.Grid.GridSummaryItemAlignment.Left;
                        groupSummaryItem.Tag = "COUNT_AUTO_SELECT_PLACE_1";
                        FirstMovingGrid.GridControl.GroupSummary.Add(groupSummaryItem);
                    }

                    {
                        DevExpress.Xpf.Grid.GridSummaryItem groupSummaryItem = new DevExpress.Xpf.Grid.GridSummaryItem();
                        groupSummaryItem.FieldName = "AUTO_SELECT_PLACE_FLAG";
                        groupSummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Custom;
                        groupSummaryItem.DisplayFormat = "Вручную: {0} шт.)";
                        groupSummaryItem.Alignment = DevExpress.Xpf.Grid.GridSummaryItemAlignment.Left;
                        groupSummaryItem.Tag = "COUNT_AUTO_SELECT_PLACE_0";
                        FirstMovingGrid.GridControl.GroupSummary.Add(groupSummaryItem);
                    }

                    {
                        DevExpress.Xpf.Grid.GridSummaryItem groupSummaryItem = new DevExpress.Xpf.Grid.GridSummaryItem();
                        groupSummaryItem.FieldName = "AUTO_SELECT_PLACE_FLAG";
                        groupSummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Custom;
                        groupSummaryItem.DisplayFormat = "(По программе: {0:P2}";
                        groupSummaryItem.Alignment = DevExpress.Xpf.Grid.GridSummaryItemAlignment.Left;
                        groupSummaryItem.Tag = "PERCENT_AUTO_SELECT_PLACE_1";
                        FirstMovingGrid.GridControl.GroupSummary.Add(groupSummaryItem);
                    }

                    {
                        DevExpress.Xpf.Grid.GridSummaryItem groupSummaryItem = new DevExpress.Xpf.Grid.GridSummaryItem();
                        groupSummaryItem.FieldName = "AUTO_SELECT_PLACE_FLAG";
                        groupSummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Custom;
                        groupSummaryItem.DisplayFormat = "Вручную: {0:P2})";
                        groupSummaryItem.Alignment = DevExpress.Xpf.Grid.GridSummaryItemAlignment.Left;
                        groupSummaryItem.Tag = "PERCENT_AUTO_SELECT_PLACE_0";
                        FirstMovingGrid.GridControl.GroupSummary.Add(groupSummaryItem);
                    }
                }
            }
        }

        private async void FirstMovingGridLoadItems()
        {
            if (Form.Validate())
            {
                bool resume = true;

                var f = Form.GetValueByPath("FROM_DATE_TIME").ToDateTime();
                var t = Form.GetValueByPath("TO_DATE_TIME").ToDateTime();

                if (DateTime.Compare(f, t) > 0)
                {
                    string msg = "Дата начала должна быть меньше даты окончания.";
                    var d = new DialogWindow($"{msg}", this.ControlTitle);
                    d.ShowDialog();
                    resume = false;
                }

                if (resume)
                {
                    if ((t - f).TotalDays > 7)
                    {
                        string msg = $"Доступен просмотр данных не более чем за 7 дней.{Environment.NewLine}Если вам нужен отчёт за больший период, пожалуйста, укажите интересующий вас диапазон дат и нажмите кнопку \"В Excel\"";
                        var d = new DialogWindow($"{msg}", this.ControlTitle);
                        d.ShowDialog();
                        resume = false;

                        FirstMovingGrid.SetBusy(false);
                    }

                    if (resume)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            EnableSplash();
                        });

                        var p = new Dictionary<string, string>();
                        p.Add("FROM_DTTM", Form.GetValueByPath("FROM_DATE_TIME"));
                        p.Add("TO_DTTM", Form.GetValueByPath("TO_DATE_TIME"));
                        p.Add("FACTORY_ID", $"{FactoryId}");

                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Stock");
                        q.Request.SetParam("Object", "Report");
                        q.Request.SetParam("Action", "ForkliftDriverFirstMovingFromStrapper");
                        q.Request.SetParams(p);

                        await Task.Run(() =>
                        {
                            q.DoQuery();
                        });

                        FirstMovingDataSet = new ListDataSet();
                        if (q.Answer.Status == 0)
                        {
                            var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                            if (result != null)
                            {
                                FirstMovingDataSet = ListDataSet.Create(result, "ITEMS");
                            }
                        }
                        else
                        {
                            q.ProcessError();
                        }
                        FirstMovingGrid.UpdateItems(FirstMovingDataSet);

                        // Группировка
                        {
                            var column = FirstMovingGrid.GridControl.Columns["STAFF_NAME"];
                            if (column != null)
                            {
                                column.GroupIndex = 0;
                            }
                        }

                        // Пересчёт агрегатных выражений
                        {
                            FirstMovingGrid.GridControl.UpdateGroupSummary();
                        }

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            DisableSplash();
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Костомные агрегатные функции
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GridControl_CustomSummary(object sender, DevExpress.Data.CustomSummaryEventArgs e)
        {
            try
            {
                DevExpress.Xpf.Grid.GridSummaryItem item = null;

                switch (e.SummaryProcess)
                {
                    case DevExpress.Data.CustomSummaryProcess.Start:

                        item = (DevExpress.Xpf.Grid.GridSummaryItem)e.Item;

                        if (item.SummaryType == DevExpress.Data.SummaryItemType.Custom)
                        {
                            e.TotalValue = 0;
                        }

                        if (item.Tag == "PERCENT_AUTO_SELECT_PLACE_0" || item.Tag == "PERCENT_AUTO_SELECT_PLACE_1")
                        {
                            e.TotalValueReady = true;
                        }

                        break;

                    case DevExpress.Data.CustomSummaryProcess.Calculate:

                        item = (DevExpress.Xpf.Grid.GridSummaryItem)e.Item;

                        switch (item.Tag)
                        {
                            case "COUNT_AUTO_SELECT_PLACE_1":
                                if (e.FieldValue != null && (int)e.FieldValue == 1)
                                {
                                    e.TotalValue = (int)e.TotalValue + 1;
                                }
                                break;

                            case "COUNT_AUTO_SELECT_PLACE_0":
                                if (e.FieldValue != null && (int)e.FieldValue == 0)
                                {
                                    e.TotalValue = (int)e.TotalValue + 1;
                                }
                                break;
                        }

                        break;

                    case DevExpress.Data.CustomSummaryProcess.Finalize:

                        item = (DevExpress.Xpf.Grid.GridSummaryItem)e.Item;
                        if (item.Tag == "PERCENT_AUTO_SELECT_PLACE_0" || item.Tag == "PERCENT_AUTO_SELECT_PLACE_1")
                        {
                            int countAuto1 = 0;
                            int countAuto0 = 0;

                            foreach (var groupSummaryItem in FirstMovingGrid.GridControl.GroupSummary)
                            {
                                if (groupSummaryItem.Tag == "COUNT_AUTO_SELECT_PLACE_1")
                                {
                                    countAuto1 = (int)FirstMovingGrid.GridControl.GetGroupSummaryValue(e.GroupRowHandle, groupSummaryItem);
                                }
                                else if (groupSummaryItem.Tag == "COUNT_AUTO_SELECT_PLACE_0")
                                {
                                    countAuto0 = (int)FirstMovingGrid.GridControl.GetGroupSummaryValue(e.GroupRowHandle, groupSummaryItem);
                                }
                            }

                            if (countAuto1 > 0 || countAuto0 > 0)
                            {
                                switch (item.Tag)
                                {
                                    case "PERCENT_AUTO_SELECT_PLACE_0":
                                        e.TotalValue = (double)countAuto0 / ((double)countAuto0 + (double)countAuto1);
                                        break;

                                    case "PERCENT_AUTO_SELECT_PLACE_1":
                                        e.TotalValue = (double)countAuto1 / ((double)countAuto0 + (double)countAuto1);
                                        break;
                                }
                            }
                        }

                        break;
                }
            }
            catch (Exception)
            {

            }
        }

        private async void ExcelReport()
        {
            bool resume = true;

            var f = Form.GetValueByPath("FROM_DATE_TIME").ToDateTime();
            var t = Form.GetValueByPath("TO_DATE_TIME").ToDateTime();

            if (DateTime.Compare(f, t) > 0)
            {
                string msg = "Дата начала должна быть меньше даты окончания.";
                var d = new DialogWindow($"{msg}", this.ControlTitle);
                d.ShowDialog();
                resume = false;
            }

            if (resume)
            {
                DisableControls();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    EnableSplash();
                });

                var p = new Dictionary<string, string>();
                p.Add("FROM_DTTM", Form.GetValueByPath("FROM_DATE_TIME"));
                p.Add("TO_DTTM", Form.GetValueByPath("TO_DATE_TIME"));
                p.Add("FACTORY_ID", $"{FactoryId}");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Stock");
                q.Request.SetParam("Object", "Report");
                q.Request.SetParam("Action", "ForkliftDriverFirstMovingFromStrapperExcel");
                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    if (q.Answer.Type == LPackClientAnswer.AnswerTypeRef.File)
                    {
                        Central.OpenFile(q.Answer.DownloadFilePath);
                    }
                }
                else
                {
                    q.ProcessError();
                }

                EnableControls();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DisableSplash();
                });
            }
        }

        public void Refresh()
        {
            FirstMovingGrid.LoadItems();
        }

        private void EnableSplash()
        {
            SplashControl.Message = $"Пожалуйста, подождите.{Environment.NewLine}Идёт загрузка данных.";
            SplashControl.Visible = true;
        }

        private void DisableSplash()
        {
            SplashControl.Message = "";
            SplashControl.Visible = false;
        }

        private void DisableControls()
        {
            GridToolbar.IsEnabled = false;
            FirstMovingGrid.ShowSplash();
        }

        private void EnableControls()
        {
            GridToolbar.IsEnabled = true;
            FirstMovingGrid.HideSplash();
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
