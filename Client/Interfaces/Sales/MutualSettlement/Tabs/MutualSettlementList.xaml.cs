using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Service.Printing;
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

namespace Client.Interfaces.Sales
{
    /// <summary>
    /// Список взаиморасчётов с КА
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class MutualSettlementList : ControlBase
    {
        public MutualSettlementList()
        {
            ControlTitle = "Взаиморасчёты";
            RoleName = "[erp]mutual_settlement";
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
                MutualSettlementGridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                MutualSettlementGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                MutualSettlementGrid.ItemsAutoUpdate = true;
                MutualSettlementGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                MutualSettlementGrid.ItemsAutoUpdate = false;
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
                    ButtonControl = ResreshButton,
                    ButtonName = "ResreshButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
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
                    ButtonName = "HelpButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    HotKey = "F1",
                    Action = () =>
                    {
                        Central.ShowHelp(DocumentationUrl);
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "export_to_excel",
                    Group = "main",
                    Enabled = true,
                    Title = "В Excel",
                    Description = "Выгрузить данные в Excel файл",
                    ButtonUse = true,
                    ButtonName = "ExportToExcelButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        MutualSettlementGrid.ItemsExportExcel();
                    },
                });
            }

            Commander.Init(this);
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Датасет с данными грида приходных накладных
        /// </summary>
        public ListDataSet MutualSettlementGridDataSet { get; set; }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
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
                        Path="SEARCH",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=SearchText,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path = "INVOICE_DATE_FROM",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = InvoiceDateFrom,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "INVOICE_DATE_TO",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = InvoiceDateTo,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                };

                Form.SetFields(fields);
            }
        }

        private void SetDefaults()
        {
            MutualSettlementGridDataSet = new ListDataSet();

            Form.SetDefaults();

            Form.SetValueByPath("INVOICE_DATE_FROM", DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy"));
            Form.SetValueByPath("INVOICE_DATE_TO", DateTime.Now.ToString("dd.MM.yyyy"));
        }

        private void MutualSettlementGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Description = "Идентификатор",
                        Path="ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                    },
                };
                MutualSettlementGrid.SetColumns(columns);
                MutualSettlementGrid.SearchText = SearchText;
                MutualSettlementGrid.OnLoadItems = MutualSettlementGridLoadItems;
                MutualSettlementGrid.SetPrimaryKey("INVOICE_ID");
                MutualSettlementGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                MutualSettlementGrid.AutoUpdateInterval = 5 * 60;
                MutualSettlementGrid.Toolbar = GridToolbar;
                MutualSettlementGrid.Commands = Commander;
                MutualSettlementGrid.UseProgressSplashAuto = false;
                MutualSettlementGrid.Init();
            }
        }

        public async void MutualSettlementGridLoadItems()
        {
            bool resume = true;

            var f = Form.GetValueByPath("INVOICE_DATE_FROM").ToDateTime();
            var t = Form.GetValueByPath("INVOICE_DATE_TO").ToDateTime();

            if (DateTime.Compare(f, t) > 0)
            {
                const string msg = "Дата начала должна быть меньше даты окончания.";
                var d = new DialogWindow($"{msg}", this.ControlTitle);
                d.ShowDialog();
                resume = false;
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();
                p.Add("INVOICE_DATE_FROM", Form.GetValueByPath("INVOICE_DATE_FROM"));
                p.Add("INVOICE_DATE_TO", Form.GetValueByPath("INVOICE_DATE_TO"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "");
                q.Request.SetParam("Object", "");
                q.Request.SetParam("Action", "");
                q.Request.SetParams(p);
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                MutualSettlementGridDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        MutualSettlementGridDataSet = ListDataSet.Create(result, "ITEMS");
                    }
                }
                else
                {
                    q.ProcessError();
                }
                MutualSettlementGrid.UpdateItems(MutualSettlementGridDataSet);
            }
        }

        private void SetPrintSettings()
        {
            var i = new PrintingInterface();
        }

        public void SetFormDateInCurrentMounth()
        {
            var date = DateTime.Now;
            Form.SetValueByPath("INVOICE_DATE_FROM", new DateTime(date.Year, date.Month, 1).ToString("dd.MM.yyyy"));
            Form.SetValueByPath("INVOICE_DATE_TO", new DateTime(date.Year, date.Month, 1).AddMonths(1).AddDays(-1).ToString("dd.MM.yyyy"));

            Refresh();
        }

        public void Refresh()
        {
            MutualSettlementGrid.LoadItems();
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            SetPrintSettings();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }

        private void CurrentMounthMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SetFormDateInCurrentMounth();
        }
    }
}
