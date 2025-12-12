using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Состояние склада.
    /// Слева отображается список ячеек склада.
    /// При выборе ячейки справа отображаются поддоны, находящиеся в этой ячейке.
    /// </summary>
    /// <author>sviridov_ae</author>
    public partial class WarehouseConditionList : UserControl
    {
        public WarehouseConditionList()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            FormInit();
            SetDefaults();
            StockCellGridInit();
            CellPalletGridInit();
            ProcessPermissions();
        }

        public string StockRow { get; set; }

        public string StockNumber { get; set; }

        /// <summary>
        /// форма с полями для фильтрации данных
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// данные из выбранной в гриде строки
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel("[erp]warehouse_condition");
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

            if (StockCellGrid != null && StockCellGrid.Menu != null && StockCellGrid.Menu.Count > 0)
            {
                foreach (var manuItem in StockCellGrid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }

            if (CellPalletGrid != null && CellPalletGrid.Menu != null && CellPalletGrid.Menu.Count > 0)
            {
                foreach (var manuItem in CellPalletGrid.Menu)
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

        private void FormInit()
        {
            //колонки формы
            {
                Form = new FormHelper();

                var fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="SEARCH",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=Search,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                };

                Form.SetFields(fields);

                //перед установкой значений
                Form.BeforeSet = (Dictionary<string, string> v) =>
                {

                };

                //после установки значений
                Form.AfterSet = (Dictionary<string, string> v) =>
                {
                    //фокус на кнопку обновления
                    RefreshButton.Focus();
                };
            }
        }

        private void StockCellGridInit()
        {
            //инициализация грида
            {
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ряд",
                        Path="SKLAD_ROW",
                        ColumnType=ColumnTypeRef.String,
                        Width=55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ячейка",
                        Path="SKLAD_NUM",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Тип ячейки",
                        Path="TYPE",
                        ColumnType=ColumnTypeRef.String,
                        Format="dd.MM.yyyy HH:mm",
                        Width=80,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД отгрузки",
                        Path="ID_TS",
                        ColumnType=ColumnTypeRef.Integer,
                        Format="dd.MM.yyyy HH:mm",
                        Width=85,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Паллет",
                        Path="CNT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Паллет под отгрузку",
                        Path="CNT_TS",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=130,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Паллет не под отгрузку",
                        Path="CNT_UTS",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=145,
                    },
                    new DataGridHelperColumn
                    {
                        Header=" ",
                        Path="_",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=5,
                        MaxWidth=900,
                    },
                };
                StockCellGrid.SetColumns(columns);

                StockCellGrid.OnSelectItem = (Dictionary<string, string> selectedItem) =>
                {
                    if (selectedItem.Count > 0)
                    {
                        StockRow = selectedItem.CheckGet("SKLAD_ROW");
                        StockNumber = selectedItem.CheckGet("SKLAD_NUM");

                        CellPalletGridLoadItems();
                        UpdateActions(selectedItem);
                    }
                };

                StockCellGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>
                {
                    {
                        DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result = DependencyProperty.UnsetValue;
                            var color = "";

                                if (row["CNT_UTS"].ToInt() > 0)
                                {
                                    color = HColor.Orange;
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                        }
                    },
                };

                StockCellGrid.OnLoadItems = StockCellGridLoadItems;
                StockCellGrid.OnFilterItems = FilterItems;
                StockCellGrid.SetSorting("SKLAD_ROW", ListSortDirection.Ascending);
                StockCellGrid.SearchText = Search;
                StockCellGrid.Init();
                StockCellGrid.Run();
                StockCellGrid.Focus();
            }
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();

            var factorySelectBoxItems = new Dictionary<string, string>();
            factorySelectBoxItems.Add("1", "Л-ПАК ЛИПЕЦК");
            factorySelectBoxItems.Add("2", "Л-ПАК КАШИРА");
            FactorySelectBox.SetItems(factorySelectBoxItems);
            FactorySelectBox.SetSelectedItemByKey("1");
        }

        private void CellPalletGridInit()
        {
            //инициализация грида правой таблички
            {
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД поддона",
                        Path="ID_PODDON",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ поддона",
                        Path="PZ_NUM",
                        ColumnType=ColumnTypeRef.String,
                        Width=55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        Format="dd.MM.yyyy HH:mm",
                        Width=300,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул",
                        Path="ARTIKUL",
                        ColumnType=ColumnTypeRef.String,
                        Format="dd.MM.yyyy HH:mm",
                        Width=115,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Path="KOL",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД отгрузки",
                        Path="ID_TS",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД заявки паллета",
                        Path="ID_ORDER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Заявка",
                        Path="ORDER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width=250,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Заявка ПЗ",
                        Path="PZ_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width=250,
                    },
                    new DataGridHelperColumn
                    {
                        Header=" ",
                        Path="_",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=5,
                        MaxWidth=900,
                    },
                };

                CellPalletGrid.SetColumns(columns);
                CellPalletGrid.OnLoadItems = CellPalletGridLoadItems;
                CellPalletGrid.Init();
                CellPalletGrid.Run();
            }
        }

        public async void StockCellGridLoadItems()
        {
            RefreshButton.Style = (Style)RefreshButton.TryFindResource("Button");

            DisableControls();
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Condition");
            q.Request.SetParam("Action", "List");
            q.Request.SetParam("FACTORY_ID", FactorySelectBox.SelectedItem.Key);
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
                    var itemsDs = ListDataSet.Create(result, "ITEMS");
                    StockCellGrid.UpdateItems(itemsDs);
                }                
            }
            EnableControls();
        }

        public async void CellPalletGridLoadItems()
        {
            DisableControls();
            var p = new Dictionary<string, string>();

            p.Add("STOCK_ROW", StockRow);
            p.Add("STOCK_NUMBER", StockNumber);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Condition");
            q.Request.SetParam("Action", "ListPallet");

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
                    var itemsDs = ListDataSet.Create(result, "ITEMS");
                    CellPalletGrid.UpdateItems(itemsDs);
                }
            }
            EnableControls();
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            //Group ProductionTask
            if (m.ReceiverGroup.IndexOf("Stock") > -1)
            {
                if (m.ReceiverName == "StockConditionList")
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            StockCellGrid.LoadItems();
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// фильтрация записей
        /// </summary>
        public async void FilterItems()
        {
            if (StockCellGrid.GridItems != null)
            {
                if (StockCellGrid.GridItems.Count > 0)
                {
                    //фильтрация строк
                    {

                    }
                }
            }
        }

        /// <summary>
        /// обновление методов работы с выбранной в гриде строкой
        /// </summary>
        /// <param name="selectedItem"></param>
        public void UpdateActions(Dictionary<string, string> selectedItem)
        {
            if (selectedItem != null)
            {
                SelectedItem = selectedItem;
            }
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
            StockCellGrid.ShowSplash();
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
            StockCellGrid.HideSplash();
        }

        /// <summary>
        /// деструктор интерфейса
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Stock",
                ReceiverName = "",
                SenderName = "StockConditionList",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            StockCellGrid.Destruct();
        }

        /// <summary>
        /// обработка ввода с клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.F5:
                    StockCellGrid.LoadItems();
                    e.Handled = true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

                case Key.Home:
                    StockCellGrid.SetSelectToFirstRow();
                    e.Handled = true;
                    break;

                case Key.End:
                    StockCellGrid.SetSelectToLastRow();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// отображение справочной статьи
        /// (относительный путь)
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/warehouse/condition");
        }

        /// <summary>
        /// экспорт в Excel
        /// </summary>
        private async void Export()
        {
            if (StockCellGrid != null)
            {
                if (StockCellGrid.Items.Count > 0)
                {
                    var eg = new ExcelGrid();
                    var cols = StockCellGrid.Columns;
                    eg.SetColumnsFromGrid(cols);
                    eg.Items = StockCellGrid.Items;

                    await Task.Run(() =>
                    {
                        eg.Make();
                    });
                }
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            Export();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            StockCellGrid.LoadItems();
        }

        private void FactorySelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RefreshButton.Style = (Style)RefreshButton.TryFindResource("FButtonPrimary");
        }
    }
}
