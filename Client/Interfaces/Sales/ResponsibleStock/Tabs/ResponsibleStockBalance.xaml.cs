using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
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
    /// Остатки поддонов на СОХ
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class ResponsibleStockBalance : UserControl
    {
        public ResponsibleStockBalance()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            SetDefaults();
            PalletGridInit();

            ProcessPermissions();
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Основной датасет с данными
        /// </summary>
        public ListDataSet PalletGridDataSet { get; set; }

        /// <summary>
        /// Выбранная запись в гриде
        /// </summary>
        public Dictionary<string, string> PalletGridSelectedItem { get; set; }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void PalletGridInit()
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
                        Path = "FROM_DATE",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = FromDate,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "TO_DATE",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = ToDate,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                };

                Form.SetFields(fields);

                Form.SetValueByPath("FROM_DATE", DateTime.Now.AddMonths(-1).ToString("dd.MM.yyyy"));
                Form.SetValueByPath("TO_DATE", DateTime.Now.ToString("dd.MM.yyyy"));
            }

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
                        MinWidth=30,
                        MaxWidth=32,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Path="PALLET_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус",
                        Path="PALLET_STATUS",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=60,
                        MaxWidth=105,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="PRODUCT_FULL_NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=120,
                        MaxWidth=350,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул",
                        Path="CODE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=120,
                        MaxWidth=120,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // Если это архивная тех карта
                                    if( row.CheckGet("ARCHIVE_FLAG").ToInt() > 0 )
                                    {
                                        color = HColor.Red;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Path="QUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=40,
                        MaxWidth=85,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Поддон",
                        Path="PALLET_DIMENSIONS",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=65,
                        MaxWidth=65,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Площадь",
                        Path="SQUARE",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N4",
                        MinWidth=65,
                        MaxWidth=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата поступления",
                        Path="INCOMING_DATE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=65,
                        MaxWidth=115,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата продажи",
                        Path="CONSUMPTION_DATE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=65,
                        MaxWidth=95,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Заявка на СОХ",
                        Path="INVOICE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=100,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Отгрузка на СОХ",
                        Path="TRANSPORT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=65,
                        MaxWidth=110,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Внутреннее наименование",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=120,
                        MaxWidth=350,
                    },

                    new DataGridHelperColumn
                    {
                        Header="ARCHIVE_FLAG",
                        Path="ARCHIVE_FLAG",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=55,
                        Hidden=true,
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
                PalletGrid.SetColumns(columns);

                PalletGrid.SearchText = SearchText;
                PalletGrid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                PalletGrid.OnSelectItem = selectedItem =>
                {
                    PalletGridSelectedItem = selectedItem;
                    ProcessPermissions();
                };

                PalletGrid.SetSorting("PALLET_ID", System.ComponentModel.ListSortDirection.Descending);

                //данные грида
                PalletGrid.OnLoadItems = PalletGridLoadItems;
                PalletGrid.Run();
            }
        }

        public async void PalletGridLoadItems()
        {
            DisableControls();

            var fromDate = Form.GetValueByPath("FROM_DATE");
            var toDate = Form.GetValueByPath("TO_DATE");

            if (DateTime.Compare(fromDate.ToDateTime(), toDate.ToDateTime()) > 0)
            {
                const string msg = "Дата начала должна быть меньше даты окончания.";
                var d = new DialogWindow($"{msg}", "Проверка данных");
                d.ShowDialog();

                EnableControls();
                return;
            }

            var p = new Dictionary<string, string>();
            p.Add("FROM_DATE", fromDate);
            p.Add("TO_DATE", toDate);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "ResponsibleStock");
            q.Request.SetParam("Action", "ListPallet");
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
                    var dataSet = ListDataSet.Create(result, "ITEMS");
                    PalletGridDataSet = dataSet;

                    PalletGrid.UpdateItems(PalletGridDataSet);
                }
            }
            else
            {
                q.ProcessError();
            }

            EnableControls();
        }

        public void SetDefaults()
        {
            PalletGridSelectedItem = new Dictionary<string, string>();
            PalletGridDataSet = new ListDataSet();
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {

        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel("[erp]responsible_stock");
            var userAccessMode = mode;
            switch (mode)
            {
                case Role.AccessMode.Special:
                    break;

                case Role.AccessMode.FullAccess:
                    break;

                case Role.AccessMode.ReadOnly:
                default:
                    break;
            }

            List<Button> buttons = UIUtil.GetVisualChilds<Button>(this.Content as DependencyObject);
            if (buttons != null && buttons.Count > 0)
            {
                foreach (var button in buttons)
                {
                    var buttonTagList = UIUtil.GetTagList(button);
                    var accessMode = Acl.FindTagAccessMode(buttonTagList);
                    if (accessMode > userAccessMode)
                    {
                        button.IsEnabled = false;
                    }
                }
            }

            if (PalletGrid != null && PalletGrid.Menu != null && PalletGrid.Menu.Count > 0)
            {
                foreach (var manuItem in PalletGrid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }
        }

        public void Refresh()
        {
            SetDefaults();
            PalletGridLoadItems();
        }

        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
        }

        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
        }

        /// <summary>
        /// Создание Excel файла по данным в гриде
        /// </summary>
        public async void ExportToExcel()
        {
            if (PalletGrid.Items != null)
            {
                if (PalletGrid.Items.Count > 0)
                {
                    var eg = new ExcelGrid();
                    var cols = PalletGrid.Columns;
                    eg.SetColumnsFromGrid(cols);
                    eg.Items = PalletGrid.Items;
                    eg.GridTitle = "Поддоны, хранившиеся на СОХ в период с " + Form.GetValueByPath("FROM_DATE") + " по " + Form.GetValueByPath("TO_DATE");
                    await Task.Run(() =>
                    {
                        eg.Make();
                    });
                }
            }
        }

        /// <summary>
        /// отображение справочной статьи
        /// (относительный путь)
        /// </summary>
        public void ShowHelp()
        {
            //FIXME: Нужно сделать документацию
            Central.ShowHelp("/doc/l-pack-erp/");
        }

        private void ResreshButton_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void ExcelButton_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcel();
        }
    }
}
