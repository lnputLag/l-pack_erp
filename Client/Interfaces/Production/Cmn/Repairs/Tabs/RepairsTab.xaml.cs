using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.DeliveryAddresses;
using Client.Interfaces.Main;
using Client.Interfaces.ProductionCatalog;
using CodeReason.Reports;
using DevExpress.Data.Helpers;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using NLog.LayoutRenderers;
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

namespace Client.Interfaces.Production.Cmn.Repairs
{
    /// <summary>
    /// Отчёт по простоям гофропереработки
    /// </summary>
    /// <author>motenko_ek</author>   
    public partial class RepairsTab : ControlBase
    {
        private List<DataGridHelperColumn> Columns;
        private readonly int FactId;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="factId"></param>
        /// <param name="prodType">0-БДМ, 1-ГА, 2-переработка, 3-упаковка </param>
        /// <param name="roleName"></param>
        /// <param name="controlTitle"></param>
        public RepairsTab(int factId, string roleName, string controlTitle)
        {
            InitializeComponent();

            FactId = factId;
            RoleName = roleName;
            ControlTitle = controlTitle;
            DocumentationUrl = "/doc/l-pack-erp/production/repair_tab";

            AllRepairsCheckBox.Click += (object sender, RoutedEventArgs e) => {
                ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
            };

            MachineSelectBox.DropDownBlock.Opened += LoadFilters;
            MachineSelectBox.SelectedItemChanged += (DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            {
                Grid.UpdateItems();
            };
            //DepartmentSelectBox.DropDownBlock.Opened += LoadFilters;
            DepartmentSelectBox.SelectedItemChanged += (DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            {
                Grid.UpdateItems();
            };
            LongTimeSelectBox.SelectedItemChanged += (DependencyObject d, DependencyPropertyChangedEventArgs e) =>
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
                    Grid.LoadItems();
                },
            });
            Commander.Add(new CommandItem()
            {
                Name = "add",
                Group = "grid_base",
                Enabled = true,
                Title = "Создать",
                MenuUse = true,
                HotKey = "Insert",
                ButtonUse = true,
                ButtonName = "CreateButton",
                AccessLevel = Client.Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    new RepairForm(new ItemMessage() { ReceiverName = ControlName, Action = "refresh" }, FactId);
                },
            });
            Commander.Add(new CommandItem()
            {
                Name = "print",
                Group = "grid_base",
                Enabled = true,
                Title = "Печать",
                Description = "Печать",
                MenuUse = true,
                ButtonUse = true,
                ButtonName = "PrintButton",
                AccessLevel = Client.Common.Role.AccessMode.ReadOnly,
                Action = () =>
                {
                    Print();
                    //new GridPrintPreview($"{ControlTitle} за период с {From} по {To}. " +
                    //    $"Фильтр: cтанок-{MachineSelectBox.ValueTextBox.Text}, " +
                    //    $"тип-{StatusSelectBox.ValueTextBox.Text}, " +
                    //    $"причина-{DepartmentSelectBox.ValueTextBox.Text}.", Columns, Grid.Items);
                },
            });
            Commander.Add(new CommandItem()
            {
                Name = "excel",
                Group = "grid_base",
                Enabled = true,
                Title = "В Excel",
                Description = "В Excel",
                MenuUse = true,
                ButtonUse = true,
                ButtonName = "ExcelButton",
                AccessLevel = Client.Common.Role.AccessMode.ReadOnly,
                Action = ExportToExcel
            });
            Commander.SetCurrentGroup("item");
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
                        new RepairForm(new ItemMessage() { ReceiverName = ControlName, Action = "refresh" }, FactId, id);
                    }
                },
                CheckEnabled = () =>
                {
                    return Grid.SelectedItem != null
                        && Grid.SelectedItem.Count > 0;
                },
            });
            //Commander.Add(new CommandItem()
            //{
            //    Name = "delete",
            //    Title = "Удалить",
            //    MenuUse = true,
            //    ButtonUse = true,
            //    ButtonName = "DeleteButton",
            //    AccessLevel = Client.Common.Role.AccessMode.FullAccess,
            //    Action = () =>
            //    {
            //        Delete(Grid.SelectedItem);
            //    },
            //    CheckEnabled = () =>
            //    {
            //        return Grid.SelectedItem != null
            //            && Grid.SelectedItem.Count > 0;
            //    },
            //});
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
                        Header="*",
                        Path="CHECKING",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=4,
                        Editable=true,
                        //OnAfterClickAction = (Dictionary<string, string> value, FrameworkElement element) =>
                        //{
                        //    if(value["CHECKING"]=="True") CheckBoxCount++;
                        //    else CheckBoxCount--;

                        //    ShippingAddressCopyButton.IsEnabled = CheckBoxCount > 0;

                        //    return true;
                        //},
                    },
                    new DataGridHelperColumn
                    {
                        Header="Идентификатор",
                        Path="RESC_ID",
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
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Описание",
                        Path = "TASK",
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Комментарий от мастера",
                        Path="NOTE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=30,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Механики",
                        Path="MEH",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=4,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Электрики",
                        Path="EL",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=4,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Технологи",
                        Path="TEH",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=4,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Продолжительный",
                        Path="LONG_TIME",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=4,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Постановка",
                        Path = "INI_DTTM",
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 16,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Начало",
                        Path = "START_DTTM",
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 16,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Окончание",
                        Path = "FINISH_DTTM",
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 16,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус",
                        Path="REPAIR_STATUS",
                        ColumnType=ColumnTypeRef.String,
                        Width2=16,
                    },
                };
            Grid.SetColumns(Columns);
            Grid.SetPrimaryKey("RESC_ID");
            Grid.SearchText = SearchText;
            Grid.Toolbar = GridToolbar;
            Grid.Commands = Commander;
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.AutoUpdateInterval = 0;
            Grid.ItemsAutoUpdate = false;
            Grid.EnableFiltering = true;
            Grid.QueryLoadItems = new RequestData()
            {
                Module = "ProdRepairs",
                Object = "Repairs",
                Action = "GetList",
                AnswerSectionKey = "ITEMS",
                Timeout = 10000,
                BeforeRequest = (RequestData rd) =>
                {
                    rd.Params = new Dictionary<string, string>()
                            {
                                { "FACT_ID", FactId.ToString() },
                                { "REPAIR_STATUS", AllRepairsCheckBox.IsChecked == true?"20":"10" },
                            };
                },
                AfterUpdate = (RequestData rd, ListDataSet ds) =>
                {
                    ShowButton.Style = (Style)ShowButton.TryFindResource("Button");
                },
            };
            Grid.OnFilterItems = () =>
            {
                var idSt = MachineSelectBox.SelectedItem.Key.ToInt();
                var idDepartment = DepartmentSelectBox.SelectedItem.Key.ToInt();
                var idLongTime = LongTimeSelectBox.SelectedItem.Key.ToInt();
                Grid.Items = Grid.Items.FindAll(
                    row =>
                    (idSt == 0 || idSt == row["ID_ST"].ToInt())
                    && (idDepartment == 0 || (
                        idDepartment == 10 && row["MEH"].ToInt() > 0
                        || idDepartment == 20 && row["EL"].ToInt() > 0
                        || idDepartment == 30 && row["TEH"].ToInt() > 0
                        )
                    )
                    && (idLongTime == 0 || idLongTime == row["LONG_TIME"].ToInt() + 1)
                );
            };
            Grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                {
                    StylerTypeRef.BackgroundColor,
                    (Dictionary<string, string> row) =>
                    {
                        return row.CheckGet("REPAIR_STATUS") switch
                        {
                            "Создан" => HColor.Blue.ToBrush(),
                            "Закончен" => HColor.Green.ToBrush(),
                            _ => DependencyProperty.UnsetValue,
                        };
                    }
                },
            };
            Grid.Init();
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            MachineSelectBox.SetItems(new Dictionary<string, string>()
            {
                {"0",  "Все"},
            });
            MachineSelectBox.SelectedItem = MachineSelectBox.Items.First();

            DepartmentSelectBox.SetItems(new Dictionary<string, string>()
            {
                {"0",  "Все"},
                {"10",  "Механики"},
                {"20",  "Электрики"},
                {"30",  "Технологи"},
            });
            DepartmentSelectBox.SelectedItem = DepartmentSelectBox.Items.First();

            LongTimeSelectBox.SetItems(new Dictionary<string, string>()
            {
                {"0",  "Все"},
                {"1",  "Непродолжительные"},
                {"2",  "Продолжительные"},
            });
            LongTimeSelectBox.SelectedItem = LongTimeSelectBox.Items.First();
        }

        private async void LoadFilters(object sender, EventArgs e)
        {
            if (MachineSelectBox.Items.Count > 1) return;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProdRepairs");
            q.Request.SetParam("Object", "Filter");
            q.Request.SetParam("Action", "GetValues");
            q.Request.SetParam("FACT_ID", FactId.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ID_ST");
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

        public async void Delete(Dictionary<string, string> row)
        {
            if (DialogWindow.ShowDialog($"Вы действительно хотите удалить {row.CheckGet("TASK")}?", "Удаление", "", DialogWindowButtons.NoYes) != true) return;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProdRepairs");
            q.Request.SetParam("Object", "Repairs");
            q.Request.SetParam("Action", "Delete");
            q.Request.SetParam(Grid.GetPrimaryKey(), row.CheckGet(Grid.GetPrimaryKey()));

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

            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Client.Reports.Production.Repairs.Report.xaml");

            if (stream == null) return;

            StreamReader reader = new StreamReader(stream);

            reportDocument.XamlData = reader.ReadToEnd();
            reportDocument.XamlImagePath = System.IO.Path.Combine(Environment.CurrentDirectory, @"Templates\");
            reader.Close();

            ReportData data = new ReportData();

            //общие данные
            data.ReportDocumentValues.Add("SystemName", $"{Central.Parameters.SystemName} {Central.Parameters.BaseLabel}");
            data.ReportDocumentValues.Add("Today", DateTime.Now);

            data.ReportDocumentValues.Add("Title", $"{ControlTitle}. " +
                    (AllRepairsCheckBox.IsChecked == true ? "Все задания на ремонт" : "Незавершенные задания") +
                    $"Фильтр: cтанок-{MachineSelectBox.ValueTextBox.Text}, " +
                    $"служба-{DepartmentSelectBox.ValueTextBox.Text}, " +
                    $"продолжительность-{LongTimeSelectBox.ValueTextBox.Text}."
                    );


            System.Data.DataTable table = new System.Data.DataTable("Items");

            table.Columns.Add("MACHINE_NAME", typeof(string));
            table.Columns.Add("NUM", typeof(string));
            table.Columns.Add("TASK", typeof(string));
            table.Columns.Add("DEP", typeof(string));
            //table.Columns.Add("H08", typeof(string));
            //table.Columns.Add("H09", typeof(string));
            //table.Columns.Add("H10", typeof(string));
            //table.Columns.Add("H11", typeof(string));
            //table.Columns.Add("H12", typeof(string));
            //table.Columns.Add("H13", typeof(string));
            //table.Columns.Add("H14", typeof(string));
            //table.Columns.Add("H15", typeof(string));
            //table.Columns.Add("H16", typeof(string));
            //table.Columns.Add("H17", typeof(string));
            //table.Columns.Add("H18", typeof(string));
            //table.Columns.Add("H19", typeof(string));
            //table.Columns.Add("H20", typeof(string));

            string machineName = null;
            int num = 0;
            var il = Grid.GridControl.VisibleItems;
            foreach (DataRowView item in Grid.GridControl.VisibleItems)
            {
                DataRow row = item?.Row;
                if(row == null
                    || !(bool)row[0]) continue;

                var mn = (string)row[2];
                if (machineName != mn)
                {
                    machineName = mn;
                    num = 0;
                }
                table.Rows.Add(new object[]
                {
                        machineName,
                        num.ToString(),
                        (string)row[3] + ". " + (string)row[4],
                        ((bool)row[6]?"Мех. ":"") +
                        ((bool)row[7]?"Эл. ":"") +
                        ((bool)row[8]?"Тех. ":""),
                });
                num++;
            }
            data.DataTables.Add(table);

            data.ShowUnknownValues = false;

            XpsDocument xps = reportDocument.CreateXpsDocumentKey(data, "Repairs");
            var pp = new PrintPreview(true);
            pp.documentViewer.Document = xps.GetFixedDocumentSequence();
            pp.Show();
        }
        private async void ExportToExcel()
        {
            Grid.SetBusy(true);

            var li = new List<Dictionary<string, string>>();
            foreach(DataRowView item in Grid.GridControl.VisibleItems)
            {
                DataRow row = item?.Row;
                if (row == null
                    || !(bool)row[0]) continue;

                li.Add(new Dictionary<string, string>()
                {
                    { "MACHINE_NAME", (string)row[2]},
                    { "NAME_UNIT", (string)row[3]},
                    { "TASK", (string)row[4]},
                    { "MEH", row[6].ToInt().ToString()},
                    { "EL", row[7].ToInt().ToString()},
                    { "TEH", row[8].ToInt().ToString()},
                });
            }

            var listString = JsonConvert.SerializeObject(li/*Grid.Items.Where((Dictionary<string, string> val) => val["CHECKING"].ToBool()).ToList()*/);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProdRepairs");
            q.Request.SetParam("Object", "Repairs");
            q.Request.SetParam("Action", "GetReport");
            q.Request.SetParam("DATA_LIST", listString);
            q.Request.SetParam("DATE", DateTime.Now.ToString());

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
