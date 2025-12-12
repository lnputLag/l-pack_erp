using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Список произведенных операций списания
    /// </summary>
    /// <author>Михеев И.С.</author>
    public partial class OperationWriteOffList : UserControl
    {
        public OperationWriteOffList()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            FormInit();
            SetDefaults();
            InitGrid();
            PreviewKeyDown += ProcessKeyboard;
            ProcessPermissions();
        }

        private ListDataSet DataSet { get; set; }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

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
                        Path = "ZONE",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = ConsumptionZoneSelectBox,
                        ControlType = "SelectBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "FACTORY_ID",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = FactorySelectBox,
                        ControlType = "SelectBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                };

                Form.SetFields(fields);
            }
        }

        public void SetDefaults()
        {
            FromDate.Text = DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy");
            ToDate.Text = DateTime.Now.ToString("dd.MM.yyyy");

            var list = new Dictionary<string, string>();
            list.Add("1", "ГА");
            list.Add("2", "Переработка");
            list.Add("3", "СГП");
            ConsumptionZoneSelectBox.Items = list;
            ConsumptionZoneSelectBox.SelectedItem = list.FirstOrDefault((x) => x.Key == "3");

            var factorySelectBoxItems = new Dictionary<string, string>();
            factorySelectBoxItems.Add("1", "Л-ПАК ЛИПЕЦК");
            factorySelectBoxItems.Add("2", "Л-ПАК КАШИРА");
            FactorySelectBox.SetItems(factorySelectBoxItems);
            FactorySelectBox.SetSelectedItemByKey("1");
        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel("[erp]warehouse_operations");
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

            if (Grid != null && Grid.Menu != null && Grid.Menu.Count > 0)
            {
                foreach (var manuItem in Grid.Menu)
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

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
        }


        /// <summary>
        /// Деструктор. Остановка вспомогательных процессов при закрытии вкладки
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии фрейма
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="Stock",
                ReceiverName = "",
                SenderName = "OperationWriteOffList",
                Action = "Closed",
            });
            
            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры гридов
            Grid.Destruct();
        }

        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "Дата списания",
                    Path = "DT_R",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 12,
                },
                new DataGridHelperColumn
                {
                    Header = "№ поддона",
                    Path = "NUM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                     Width2 = 10,
                },
                new DataGridHelperColumn
                {
                    Header = "Артикул",
                    Path = "ARTIKUL",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 15,
                },
                new DataGridHelperColumn
                {
                    Header = "Наименование",
                    Path = "NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 45,
                },
                new DataGridHelperColumn
                {
                    Header = "Тип продукции",
                    Path = "KATEGORY",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 14,
                },
                new DataGridHelperColumn
                {
                    Header = "Дата прихода",
                    Path = "DT_P",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 12,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество списанного товара, шт",
                    Path = "QTY",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2 = 9,
                },
                new DataGridHelperColumn
                {
                    Header = "Площадь",
                    Path = "SQUARE",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    Width2 = 8,
                },
                new DataGridHelperColumn
                {
                    Header = "Причина списания",
                    Path = "DESCRIPTION",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 45,
                },
                new DataGridHelperColumn
                {
                    Header = "Ячейка",
                    Path = "PLACE",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 7,
                },
                new DataGridHelperColumn
                {
                    Header = "Станок",
                    Path = "NAME_ST",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 7,
                },
                new DataGridHelperColumn
                {
                    Header = $"Сотрудник{Environment.NewLine}Сотрудник, который списал этот поддон",
                    Path = "STAFF_NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 15,
                },
                new DataGridHelperColumn
                {
                    Header = $"Водитель погрузчика{Environment.NewLine}Последний водитель, который перемещал этот поддон",
                    Path = "FORKLIFT_NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 15,
                },
                new DataGridHelperColumn
                {
                    Header = "Зона",
                    Path = "ZONE",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 7,
                },
                new DataGridHelperColumn
                {
                    Header = "ZONE_ID",
                    Path = "ZONE_ID",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2 = 5,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header = "IDR",
                    Path = "IDR",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2 = 5,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header = "CREATED",
                    Path = "CREATED",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2 = 5,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header = "FACTORY_ID",
                    Path = "FACTORY_ID",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2 = 5,
                    Hidden = true,
                },
            };
            Grid.SetColumns(columns);
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            Grid.SearchText = SearchText;
            Grid.OnLoadItems = LoadItems;

            Grid.Init();
            Grid.Run();
            Grid.Focus();
        }

        private async void LoadItems()
        {
            var f = FromDate.Text.ToDateTime();
            var t = ToDate.Text.ToDateTime();
            if (DateTime.Compare(f, t) > 0)
            {
                const string msg = "Дата начала должна быть меньше даты окончания.";
                var d = new DialogWindow($"{msg}", "Проверка данных");
                d.ShowDialog();
                return;
            }

            Grid.ShowSplash();
            GridToolbar.IsEnabled = false;

            var p = new Dictionary<string, string>();
            p.Add("FromDate", FromDate.Text);
            p.Add("ToDate", ToDate.Text);
            p.Add("ZONE_ID", Form.GetValueByPath("ZONE"));
            p.Add("FACTORY_ID", Form.GetValueByPath("FACTORY_ID"));

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Pallet");
            q.Request.SetParam("Action", "ListWriteOffHistory");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    DataSet = ListDataSet.Create(result, "List");
                    Grid.UpdateItems(DataSet);
                }
            }

            Grid.HideSplash();
            GridToolbar.IsEnabled = true;
        }

        private void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/warehouse/operations#block2");
        }

        private async void ExportToExcel()
        {
            var eg = new ExcelGrid
            {
                Columns = new List<ExcelGridColumn>
                {
                    new ExcelGridColumn("DT_R", "Дата списания", 80, ExcelGridColumn.ColumnTypeRef.String),
                    new ExcelGridColumn("NUM", "№ поддона", 50),
                    new ExcelGridColumn("ARTIKUL", "Артикул", 100),
                    new ExcelGridColumn("NAME","Наименование", 270),
                    new ExcelGridColumn("KATEGORY", "Тип продукции", 90),
                    new ExcelGridColumn("DT_P", "Дата прихода", 80, ExcelGridColumn.ColumnTypeRef.String),
                    new ExcelGridColumn("QTY", "Количество списания", 90, ExcelGridColumn.ColumnTypeRef.Integer),
                    new ExcelGridColumn("SQUARE", "Площадь", 90, ExcelGridColumn.ColumnTypeRef.Double),
                    new ExcelGridColumn("DESCRIPTION", "Причина списания", 120),
                    new ExcelGridColumn("PLACE", "Ячейка", 90),
                    new ExcelGridColumn("NAME_ST", "Станок", 90),
                    new ExcelGridColumn("STAFF_NAME", "Сотрудник", 120),
                    new ExcelGridColumn("FORKLIFT_NAME", "Водитель погрузчика", 120),
                },
                Items = Grid.GridItems,
                GridTitle = "Отчет списания на " + DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss")
            };

            await Task.Run(() =>
            {
                eg.Make();
            });
        }

        private void ProcessKeyboard(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F5:
                    Grid.LoadItems();
                    e.Handled = true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

                case Key.Home:
                    Grid.SetSelectToFirstRow();
                    e.Handled = true;
                    break;

                case Key.End:
                    Grid.SetSelectToLastRow();
                    e.Handled = true;
                    break;
            }
        }

        private void OnCurrentDay(object sender, RoutedEventArgs e)
        {
            FromDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
            ToDate.Text = DateTime.Now.AddDays(1).ToString("dd.MM.yyyy");

            LoadItems();
        }

        private void OnPrevDay(object sender, RoutedEventArgs e)
        {
            FromDate.Text = DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy");
            ToDate.Text = DateTime.Now.ToString("dd.MM.yyyy");

            LoadItems();
        }

        private void OnCurrentWeek(object sender, RoutedEventArgs e)
        {
            DayOfWeek day = DateTime.Now.DayOfWeek;
            int days = day - DayOfWeek.Monday;
            DateTime date = DateTime.Now.AddDays(-days);
            FromDate.Text = date.Date.ToString("dd.MM.yyyy");
            ToDate.Text = date.Date.AddDays(7).ToString("dd.MM.yyyy");

            LoadItems();
        }

        private void OnPrevWeek(object sender, RoutedEventArgs e)
        {
            DayOfWeek day = DateTime.Now.DayOfWeek;
            int days = day - DayOfWeek.Monday;
            DateTime date = DateTime.Now.AddDays(-days).AddDays(-7);
            FromDate.Text = date.Date.ToString("dd.MM.yyyy");
            ToDate.Text = date.Date.AddDays(7).ToString("dd.MM.yyyy");

            LoadItems();
        }

        private void OnCurrentMonth(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now;
            FromDate.Text = new DateTime(date.Year, date.Month, 1).ToString("dd.MM.yyyy");
            ToDate.Text = new DateTime(date.Year, date.Month, 1).AddMonths(1).ToString("dd.MM.yyyy");

            LoadItems();
        }

        private void OnPrevMonth(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now.AddMonths(-1);
            FromDate.Text = new DateTime(date.Year, date.Month, 1).ToString("dd.MM.yyyy");
            ToDate.Text = new DateTime(date.Year, date.Month, 1).AddMonths(1).ToString("dd.MM.yyyy");

            LoadItems();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            LoadItems();
        }

        private void ExportToExcelButton_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcel();
        }

        private void ConsumptionZoneSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (Grid != null && Grid.Initialized)
            {
                LoadItems();
            }
        }

        private void FactorySelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (Grid != null && Grid.Initialized)
            {
                LoadItems();
            }
        }
    }
}
