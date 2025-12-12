using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.DeliveryAddresses;
using Client.Interfaces.Main;
using Client.Interfaces.Production.Converting.Idles;
using Client.Interfaces.ProductionCatalog;
using CodeReason.Reports;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
using System.Windows.Xps.Serialization;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static iTextSharp.text.pdf.qrcode.Version;

namespace Client.Interfaces.Production.Cmn.Idles
{
    /// <summary>
    /// Отчёт по простоям гофропереработки
    /// </summary>
    /// <author>motenko_ek</author>   
    public partial class IdlesTab : ControlBase
    {
        private bool RecBlock;
        private ListDataSet IdReasonDetail;
        private List<DataGridHelperColumn> Columns;
        private string From;
        private object To;
        private readonly int FactId;
        /// <summary>
        /// 0-БДМ, 1-ГА, 2-переработка, 3-упаковка 
        /// </summary>
        private readonly int ProdType;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="factId"></param>
        /// <param name="prodType">0-БДМ, 1-ГА, 2-переработка, 3-упаковка </param>
        /// <param name="roleName"></param>
        /// <param name="controlTitle"></param>
        public IdlesTab(int factId, int prodType, string roleName, string controlTitle)
        {
            InitializeComponent();

            FactId = factId;
            ProdType = prodType;
            RoleName = roleName;
            ControlTitle = controlTitle;
            DocumentationUrl = "/doc/l-pack-erp/converting/idles";

            LeftShiftButton.Click += LeftShiftButton_Click;
            RightShiftButton.Click += RightShiftButton_Click;
            TimeSpanSelectBox.SelectedItemChanged += (DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            {
                SetTimeSpan(TimeSpanSelectBox.SelectedItem.Key);
            };
            FromDate.TextChanged += (object sender, TextChangedEventArgs e) =>
            {
                if (!RecBlock)
                {
                    TimeSpanSelectBox.DropDownListBox.SelectedItem = null;
                    TimeSpanSelectBox.ValueTextBox.Text = "";
                }
                ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
            };
            ToDate.TextChanged += (object sender, TextChangedEventArgs e) =>
            {
                if (!RecBlock)
                {
                    TimeSpanSelectBox.DropDownListBox.SelectedItem = null;
                    TimeSpanSelectBox.ValueTextBox.Text = "";
                }
                ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
            };
            MachineSelectBox.DropDownBlock.Opened += LoadFilters;
            MachineSelectBox.SelectedItemChanged += (DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            {
                //Grid.UpdateItems();
                ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
            };
            ReasonSelectBox.DropDownBlock.Opened += LoadFilters;
            ReasonSelectBox.SelectedItemChanged += (DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            {
                FillIdReasonDetail();
                Grid.UpdateItems();
            };
            ReasonDetailSelectBox.DropDownBlock.Opened += LoadFilters;
            ReasonDetailSelectBox.SelectedItemChanged += (DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            {
                Grid.UpdateItems();
            };

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
            OnLoad = () =>
            {
                GridInit();
            };
            OnUnload = () =>
            {
                Grid.Destruct();
            };

            SetDefaults();

            Commander.SetCurrentGridName("Grid");

            Commander.SetCurrentGroup("grid_base");
            Commander.Add(new CommandItem()
            {
                Name = "refresh",
                Group = "grid_base",
                Enabled = true,
                Title = "Обновить",
                Description = "Обновить",
                ButtonUse = true,
                ButtonName = "ShowButton",
                MenuUse = true,
                Action = () =>
                {
                    if (DateTime.Compare(FromDate.Text.ToDateTime(), ToDate.Text.ToDateTime()) > 0)
                    {
                        DialogWindow.ShowDialog($"Дата начала должна быть меньше даты окончания.", "Проверка данных", "", DialogWindowButtons.OK);
                        return;
                    }
                    Grid.LoadItems();
                },
            });
            Commander.Add(new CommandItem()
            {
                Name = "help",
                Enabled = true,
                Title = "Справка",
                Description = "Показать справочную информацию",
                ButtonUse = true,
                ButtonName = "HelpButton",
                HotKey = "F1",
                Action = () =>
                {
                    Central.ShowHelp(DocumentationUrl);
                },
            });

            Commander.SetCurrentGroup("grid_item");
            Commander.Add(new CommandItem()
            {
                Name = "add",
                Enabled = true,
                Title = "Создать",
                MenuUse = true,
                HotKey = "Insert",
                ButtonUse = true,
                ButtonName = "CreateButton",
                AccessLevel = Client.Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    new IdleForm(new ItemMessage() { ReceiverName = ControlName, Action = "refresh" }, FactId, ProdType);
                },
            });
            Commander.Add(new CommandItem()
            {
                Name = "edit",
                Title = "Изменить",
                MenuUse = true,
                HotKey = "Return|DoubleCLick",
                ButtonUse = true,
                ButtonName = "EditButton",
                AccessLevel = Client.Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    var k = Grid.GetPrimaryKey();
                    var id = Grid.SelectedItem.CheckGet(k).ToInt();
                    if (id != 0)
                    {
                        new IdleForm(new ItemMessage() { ReceiverName = ControlName, Action = "refresh" }, FactId, ProdType, id);
                    }
                },
                CheckEnabled = () =>
                {
                    return Grid.SelectedItem != null
                        && Grid.SelectedItem.Count > 0;
                },
            });
            Commander.Add(new CommandItem()
            {
                Name = "delete",
                Title = "Удалить",
                MenuUse = true,
                ButtonUse = true,
                ButtonName = "DeleteButton",
                AccessLevel = Client.Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    Delete(Grid.SelectedItem);
                },
                CheckEnabled = () =>
                {
                    return Grid.SelectedItem != null
                        && Grid.SelectedItem.Count > 0;
                },
            });

            Commander.SetCurrentGroup("grid_export");
            Commander.Add(new CommandItem()
            {
                Name = "print",
                Group = "grid_base",
                Enabled = true,
                Title = "Печатать",
                Description = "Печатать",
                MenuUse = true,
                ButtonUse = true,
                ButtonName = "PrintButton",
                AccessLevel = Client.Common.Role.AccessMode.ReadOnly,
                Action = () =>
                {
                    Print();
                    //new GridPrintPreview($"{ControlTitle} за период с {From} по {To}. " +
                    //    $"Фильтр: cтанок-{MachineSelectBox.ValueTextBox.Text}, " +
                    //    $"тип-{ReasonSelectBox.ValueTextBox.Text}, " +
                    //    $"причина-{ReasonDetailSelectBox.ValueTextBox.Text}.", Columns, Grid.Items);
                },
            });
            Commander.Add(new CommandItem()
            {
                Name = "excel",
                Group = "grid_base",
                Enabled = true,
                Title = "В Excel",
                Description = "Экспортировать в Excel",
                MenuUse = true,
                ButtonUse = true,
                ButtonName = "ExcelButton",
                AccessLevel = Client.Common.Role.AccessMode.ReadOnly,
                Action = () =>
                {
                    ExportToExcel();
                },
            });

            Commander.Init(this);
        }

        /// <summary>
        /// Инициализация грида
        /// </summary>
        public void GridInit()
        {
            Columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=3,
                },
                new DataGridHelperColumn
                {
                    Header="Идентификатор",
                    Path="IDIDLES",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Станок",
                    Path="MACHINE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Узел станка",
                    Path="NAME_UNIT",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                    //Visible=ProdType != 1,
                },
                new DataGridHelperColumn
                {
                    Header = "Начало",
                    Path = "FROMDT",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 16,
                },
                new DataGridHelperColumn
                {
                    Header = "Окончание",
                    Path = "TODT",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 16,
                },
                new DataGridHelperColumn
                {
                    Header = "Время простоя",
                    Path = "DT",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 14,
                },
                new DataGridHelperColumn
                {
                    Header = "Смена",
                    Path = "WOTE_ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 3,
                },
                new DataGridHelperColumn
                {
                    Header = "Тип",
                    Path = "NAME",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 16,
                },
                new DataGridHelperColumn
                {
                    Header = "Причина",
                    Path = "DESCRIPTION",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 20,
                },
                new DataGridHelperColumn
                {
                    Header = "Описание",
                    Path = "REASON",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 30,
                },
                new DataGridHelperColumn
                {
                    Header = "Ответственная служба",
                    Path = "DEFECT_TYPE",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 20,
                },
                new DataGridHelperColumn
                {
                    Header = "Комментарии технической службы",
                    Path = "MEASURES_TAKEN",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 30,
                },
            };
            Grid.SetColumns(Columns);
            Grid.SetPrimaryKey("IDIDLES");
            Grid.SearchText = SearchText;
            Grid.Toolbar = GridToolbar;
            Grid.Commands = Commander;
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.AutoUpdateInterval = 0;
            Grid.ItemsAutoUpdate = false;
            Grid.EnableFiltering = true;
            Grid.QueryLoadItems = new RequestData()
            {
                //FIXME: naming: Production>Idle>List
                Module = "ProdIdle",
                Object = "Idle",
                Action = "GetList",
                AnswerSectionKey = "ITEMS",
                Timeout = 180000,
                BeforeRequest = (RequestData rd) =>
                {
                    rd.Params = new Dictionary<string, string>()
                            {
                                { "FACT_ID", FactId.ToString() },
                                { "PROD_TYPE", ProdType.ToString() },
                                { "ID_ST", MachineSelectBox.SelectedItem.Key },
                                { "FROM_DATE", FromDate.Text },
                                { "TO_DATE", ToDate.Text },
                            };
                },
                AfterUpdate = (RequestData rd, ListDataSet ds) =>
                {
                    From = FromDate.Text;
                    To = ToDate.Text;
                    ShowButton.Style = (Style)ShowButton.TryFindResource("Button");
                },
            };
            Grid.OnFilterItems = () =>
            {
                //var idSt = MachineSelectBox.SelectedItem.Key.ToInt();
                var idReason = ReasonSelectBox.SelectedItem.Key.ToInt();
                var idReasonDetail = ReasonDetailSelectBox.SelectedItem.Key.ToInt();
                Grid.Items = Grid.Items.FindAll(
                    row =>
                    //(idSt == 0 || idSt == row["ID_ST"].ToInt())
                    //&& 
                    (idReason == 0 || idReason == row["IDREASON"].ToInt())
                    && (idReasonDetail == 0 || idReasonDetail == row["ID_REASON_DETAIL"].ToInt())
                    );
            };
            Grid.Init();
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            FromDate.EditValue = DateTime.Now;

            TimeSpanSelectBox.SetItems(new Dictionary<string, string>()
            {
                {"Смена",  "Смена"},
                {"День",  "День"},
                {"Неделя",  "Неделя"},
                {"Месяц",  "Месяц"},
            });
            TimeSpanSelectBox.SelectedItem = TimeSpanSelectBox.Items.First();

            MachineSelectBox.SetItems(new Dictionary<string, string>()
            {
                {"0",  "Все"},
            });
            MachineSelectBox.SelectedItem = MachineSelectBox.Items.First();

            ReasonSelectBox.SetItems(new Dictionary<string, string>()
            {
                {"0",  "Все"},
            });
            ReasonSelectBox.SelectedItem = ReasonSelectBox.Items.First();

            ReasonDetailSelectBox.SetItems(new Dictionary<string, string>()
            {
                {"0",  "Все"},
            });
            ReasonDetailSelectBox.SelectedItem = ReasonDetailSelectBox.Items.First();
        }

        private async void LoadFilters(object sender, EventArgs e)
        {
            if (IdReasonDetail != null) return;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProdIdle");
            q.Request.SetParam("Object", "Filter");
            q.Request.SetParam("Action", "GetValues");
            q.Request.SetParam("FACT_ID", FactId.ToString());
            q.Request.SetParam("PROD_TYPE", ProdType.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    IdReasonDetail = ListDataSet.Create(result, "ID_REASON_DETAIL");

                    var ds = ListDataSet.Create(result, "IDREASON");
                    ds.Items.Insert(0, new Dictionary<string, string>() { { "ID", "0" }, { "NAME", "Все" }});
                    ReasonSelectBox.SetItems(ds, "ID", "NAME");
                    ReasonSelectBox.SelectedItem = ReasonSelectBox.Items.First();

                    ds = ListDataSet.Create(result, "ID_ST");
                    ds.Items.Insert(0, new Dictionary<string, string>() { { "ID_ST", "0" }, { "NAME", "Все" }});
                    MachineSelectBox.SetItems(ds, "ID_ST", "NAME");
                    MachineSelectBox.SelectedItem = MachineSelectBox.Items.First();
                }
                else
                {
                    DialogWindow.ShowDialog("Неверный ответ", "Получение данных с сервера", "");
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        private void FillIdReasonDetail()
        {
            var id = ReasonSelectBox.SelectedItem.Key.ToInt();
            var items = new Dictionary<string, string>() { { "0", "Все" }, };

            if (IdReasonDetail != null)
            {
                foreach (var el in IdReasonDetail.Items)
                {
                    if (id == 0 || id == el["ID_IDLE_REASON"].ToInt())
                        items.Add(el["ID_IDLE_DETAILS"].ToString(), el["DESCRIPTION"].ToString());
                }
            }

            ReasonDetailSelectBox.SetItems(items);
            ReasonDetailSelectBox.SetSelectedItemFirst();
        }

        private void SetTimeSpan(string timeSpan)
        {
            var now = (DateTime)FromDate.EditValue;
            RecBlock = true;
            switch (timeSpan)
            {
                case "Смена":
                    if (now.Hour >= 20)
                    {
                        now = new DateTime(now.Year, now.Month, now.Day, 20, 0, 0);
                    }
                    else if (now.Hour >= 8)
                    {
                        now = new DateTime(now.Year, now.Month, now.Day, 8, 0, 0);
                    }
                    else
                    {
                        now = new DateTime(now.Year, now.Month, now.Day, 20, 0, 0);
                        now = now.AddDays(-1);
                    }
                    FromDate.EditValue = now;
                    ToDate.EditValue = now.AddHours(12);
                    break;
                case "День":
                    now = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
                    FromDate.EditValue = now;
                    ToDate.EditValue = now.AddDays(1);
                    break;
                case "Неделя":
                    now = now.AddDays(-((int)now.DayOfWeek + 6) % 7);
                    FromDate.EditValue = now;
                    ToDate.EditValue = now.AddDays(7);
                    break;
                case "Месяц":
                    now = new DateTime(now.Year, now.Month, 1, 0, 0, 0);
                    FromDate.EditValue = now;
                    ToDate.EditValue = now.AddMonths(1);
                    break;
            }
            RecBlock = false;
        }

        private void LeftShiftButton_Click(object sender, RoutedEventArgs e)
        {
            var from = (DateTime)FromDate.EditValue;
            RecBlock = true;
            if (TimeSpanSelectBox.SelectedItem.Key == "Месяц")
            {
                ToDate.EditValue = FromDate.EditValue;
                FromDate.EditValue = from.AddMonths(-1);
            }
            else
            {
                var ts = (DateTime)ToDate.EditValue - from;
                ToDate.EditValue = FromDate.EditValue;
                FromDate.EditValue = from - ts;
            }
            RecBlock = false;
        }

        private void RightShiftButton_Click(object sender, RoutedEventArgs e)
        {
            var to = (DateTime)ToDate.EditValue;
            RecBlock = true;
            if (TimeSpanSelectBox.SelectedItem.Key == "Месяц")
            {
                FromDate.EditValue = ToDate.EditValue;
                ToDate.EditValue = to.AddMonths(1);
            }
            else
            {
                var ts = to - (DateTime)FromDate.EditValue;
                FromDate.EditValue = ToDate.EditValue;
                ToDate.EditValue = to + ts;
            }
            RecBlock = false;
        }

        public async void Delete(Dictionary<string, string> row)
        {
            if (DialogWindow.ShowDialog($"Вы действительно хотите удалить простой {row.CheckGet("FROMDT")}?", "Удаление простоя", "", DialogWindowButtons.NoYes) != true) return;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "Idle");
            q.Request.SetParam("Action", "Delete");
            q.Request.SetParam("IDIDLES", row.CheckGet(Grid.GetPrimaryKey()));

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Grid.SelectRowPrev();
                Grid.LoadItems();
            }
            else
            {
                q.ProcessError();
            }
        }

        public void Print()
        {
            if (Grid.Items.Count == 0) return;

            ReportDocument reportDocument = new ReportDocument();

            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Client.Reports.Production.Idles.Report.xaml");

            if (stream == null) return;

            StreamReader reader = new StreamReader(stream);

            reportDocument.XamlData = reader.ReadToEnd();
            reportDocument.XamlImagePath = System.IO.Path.Combine(Environment.CurrentDirectory, @"Templates\");
            reader.Close();

            ReportData data = new ReportData();

            //общие данные
            var systemName = $"{Central.Parameters.SystemName} {Central.Parameters.BaseLabel}";
            data.ReportDocumentValues.Add("SystemName", systemName);
            data.ReportDocumentValues.Add("Today", DateTime.Now);

            data.ReportDocumentValues.Add("FromDate", From);
            data.ReportDocumentValues.Add("ToDate", To);
            data.ReportDocumentValues.Add("Title", $"{ControlTitle}. " +
                    $"Фильтр: cтанок-{MachineSelectBox.ValueTextBox.Text}, " +
                    $"тип-{ReasonSelectBox.ValueTextBox.Text}, " +
                    $"причина-{ReasonDetailSelectBox.ValueTextBox.Text}.");


            System.Data.DataTable table = new System.Data.DataTable("Items");

            table.Columns.Add("machine_name", typeof(string));
            table.Columns.Add("wote_id", typeof(string));
            table.Columns.Add("fromdt", typeof(string));
            table.Columns.Add("todt", typeof(string));
            table.Columns.Add("dt", typeof(string));
            table.Columns.Add("name", typeof(string));
            table.Columns.Add("description", typeof(string));
            table.Columns.Add("reason", typeof(string));
            table.Columns.Add("defect_type", typeof(string));
            table.Columns.Add("measures_taken", typeof(string));

            foreach (DataRowView item in Grid.GridControl.VisibleItems)
            {
                DataRow row = item?.Row;
                if (row == null) continue;

                table.Rows.Add(new object[]
                {
                        (string)row[2],
                        row[7].ToString(),
                        (string)row[4],
                        (string)row[5],
                        (string)row[6],
                        (string)row[8],
                        (string)row[9],
                        (string)row[10],
                        (string)row[11],
                        (string)row[12],
                });
            }
            data.DataTables.Add(table);

            data.ShowUnknownValues = false;

            XpsDocument xps = reportDocument.CreateXpsDocumentKey(data, "Idles");
            var pp = new PrintPreview(true);
            pp.documentViewer.Document = xps.GetFixedDocumentSequence();
            pp.Show();
        }
        private async void ExportToExcel()
        {
            Grid.SetBusy(true);

            var li = new List<Dictionary<string, string>>();
            foreach (DataRowView item in Grid.GridControl.VisibleItems)
            {
                DataRow row = item?.Row;
                if (row == null) continue;

                li.Add(new Dictionary<string, string>()
                {
                    { "FROMDT", (string)row[4]},
                    { "MACHINE_NAME", (string)row[2]},
                    { "NAME_UNIT", (string)row[3]},
                    { "WOTE_ID", row[7].ToString()},
                    { "DT", (string)row[6]},
                    { "NAME", (string)row[8]},
                    { "DESCRIPTION", (string)row[9]},
                    { "REASON", (string)row[10]},
                    { "DEFECT_TYPE", (string)row[11]},
                    { "MEASURES_TAKEN", (string)row[12]},
                });
            }

            var listString = JsonConvert.SerializeObject(li/*Grid.Items*/);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProdIdle");
            q.Request.SetParam("Object", "Idle");
            q.Request.SetParam("Action", "GetReport");
            q.Request.SetParam("DATA_LIST", listString);
            q.Request.SetParam("FROM", FromDate.Text);
            q.Request.SetParam("TO", ToDate.Text);

            q.Request.Timeout = 25000;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Central.OpenFile(q.Answer.DownloadFilePath);
            }
            else
            {
                q.ProcessError();
            }

            Grid.SetBusy(false);
        }
    }
}
