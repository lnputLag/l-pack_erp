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
    /// Остатки продукции на СОХ
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class ResponsibleStockProductBalance : UserControl
    {
        public ResponsibleStockProductBalance()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            SetDefaults();
            ProductGridInit();
            PalletGridInit();

            ProcessPermissions();
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Выбранная запись в гриде артикулов
        /// </summary>
        public Dictionary<string, string> ProductGridSelectedItem { get; set; }

        /// <summary>
        /// Данные по артикулам
        /// </summary>
        public ListDataSet ProductGridDataSet { get; set; }

        /// <summary>
        /// Выбранная запись в гриде поддонов
        /// </summary>
        public Dictionary<string, string> PalletGridSelectedItem { get; set; }

        /// <summary>
        /// Данные по поддонам по выбранному артикулу
        /// </summary>
        public ListDataSet PalletGridDataSet { get; set; }

        public void SetDefaults()
        {
            ProductGridSelectedItem = new Dictionary<string, string>();
            ProductGridDataSet = new ListDataSet();
            PalletGridSelectedItem = new Dictionary<string, string>();
            PalletGridDataSet = new ListDataSet();

            if (Form != null)
            {
                Form.SetDefaults();
            }
        }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void ProductGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Path="PRODUCT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул",
                        Path="PRODUCT_CODE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=65,
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
                        Header="Наименование",
                        Path="PRODUCT_FULL_NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=100,
                        MaxWidth=250,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Всего продукции на СОХ, шт",
                        Path="SUMMARY_PRODUCT_QUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=50,
                        MaxWidth=110,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Всего поддонов на СОХ, шт",
                        Path="SUMMARY_PALLET_COUNT",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=50,
                        MaxWidth=108,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Доступной продукции на СОХ, шт",
                        Path="FREE_PRODUCT_QUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=78,
                        MaxWidth=140,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    //Нет доступной продукции на СОХ
                                    if( row.CheckGet("FREE_PRODUCT_QUANTITY").ToInt() == 0 )
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
                        Header="Доступных поддонов на СОХ, шт",
                        Path="FREE_PALLET_COUNT",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=78,
                        MaxWidth=135,
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
                ProductGrid.SetColumns(columns);

                ProductGrid.SearchText = SearchText;
                ProductGrid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                ProductGrid.OnSelectItem = selectedItem =>
                {
                    ProductGridSelectedItem = selectedItem;
                    PalletGridLoadItems();

                    ProcessPermissions();
                };

                ProductGrid.SetSorting("ID", System.ComponentModel.ListSortDirection.Descending);

                //данные грида
                ProductGrid.OnLoadItems = ProductGridLoadItems;
                ProductGrid.Run();

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
                    };

                    Form.SetFields(fields);
                }
            }
        }

        public async void ProductGridLoadItems()
        {
            DisableControls();

            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "ResponsibleStock");
            q.Request.SetParam("Action", "ListProductBalance");
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
                    ProductGridDataSet = dataSet;

                    if (ProductGridDataSet != null && ProductGridDataSet.Items.Count > 0)
                    {
                        foreach (var item in ProductGridDataSet.Items)
                        {
                            item.CheckAdd("PRODUCT_FULL_NAME", $"{item.CheckGet("PRODUCT_NAME")} {item.CheckGet("PRODUCT_PROPERTY")}");
                        }
                    }

                    ProductGrid.UpdateItems(ProductGridDataSet);
                }
            }
            else
            {
                q.ProcessError();
            }

            EnableControls();
        }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void PalletGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Path="PALLET_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=57,
                        MaxWidth=57,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Код поддона",
                        Path="PALLET_CODE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=90,
                        MaxWidth=90,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Path="QUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=50,
                        MaxWidth=82,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус",
                        Path="PALLET_STATUS",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=55,
                        MaxWidth=95,
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

                // раскраска строк
                PalletGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    // определение цветов фона строк
                    {
                        StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            // Статус поддона - Создан
                            if(row.CheckGet("PALLET_STATUS_ID").ToInt() == 0)
                            {
                                color = HColor.Blue;
                            }
                            // Статус поддона - Забронирован в отгрузку
                            else if (row.CheckGet("PALLET_STATUS_ID").ToInt() == 4)
                            {
                                color = HColor.Yellow;
                            }
                            // Статус поддона - Брак
                            else if (row.CheckGet("PALLET_STATUS_ID").ToInt() == 3)
                            {
                                color = HColor.Red;
                            }
                            // Статус поддона - Отгружен клиенту
                            else if (row.CheckGet("PALLET_STATUS_ID").ToInt() == 5)
                            {
                                 color = HColor.Green;
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },
                };

                PalletGrid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                PalletGrid.OnSelectItem = selectedItem =>
                {
                    PalletGridSelectedItem = selectedItem;

                    ProcessPermissions();
                };

                PalletGrid.SetSorting("ID", System.ComponentModel.ListSortDirection.Descending);

                PalletGrid.Run();
            }
        }

        public async void PalletGridLoadItems()
        {
            if (ProductGridSelectedItem != null && ProductGridSelectedItem.Count > 0)
            {
                DisableControls();

                var p = new Dictionary<string, string>();
                p.Add("PRODUCT_ID", ProductGridSelectedItem.CheckGet("PRODUCT_ID"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "ResponsibleStock");
                q.Request.SetParam("Action", "ListPalletBalanceByProduct");
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

                        if (PalletGridDataSet != null && PalletGridDataSet.Items.Count > 0)
                        {
                            foreach (var item in PalletGridDataSet.Items)
                            {
                                long longByText = StringToLongConverter(item.CheckGet("PALLET_CODE"));
                                if (longByText > 0)
                                {
                                    item.CheckAdd("PALLET_CODE", longByText.ToString());
                                }
                            }
                        }

                        PalletGrid.UpdateItems(PalletGridDataSet);
                    }
                }
                else
                {
                    q.ProcessError();
                }

                EnableControls();
            }
        }

        public long StringToLongConverter(string text)
        {
            long longByText = 0;

            if (text.Contains("."))
            {
                int stopIndex = text.IndexOf(".");
                text = text.Substring(0, stopIndex);
            }

            try
            {
                longByText = long.Parse(text);
            }
            catch (Exception ex)
            {

            }

            return longByText;
        }

        public void Refresh()
        {
            SetDefaults();
            ProductGridLoadItems();
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
        /// Создание Excel файла по данным в гриде продукции
        /// </summary>
        public async void ExportToExcel()
        {
            if (ProductGrid.Items != null)
            {
                if (ProductGrid.Items.Count > 0)
                {
                    var eg = new ExcelGrid();
                    var cols = ProductGrid.Columns;
                    eg.SetColumnsFromGrid(cols);
                    eg.Items = ProductGrid.Items;
                    await Task.Run(() =>
                    {
                        eg.Make();
                    });
                }
            }
        }

        /// <summary>
        /// Создание Excel файла по данным в гриде позиций выбранной продукции
        /// </summary>
        public async void ExportToPositionExcel()
        {
            if (PalletGrid != null && PalletGrid.Items != null && PalletGrid.Items.Count > 0)
            {
                if (ProductGridSelectedItem != null && ProductGridSelectedItem.Count > 0)
                {
                    var eg = new ExcelGrid();
                    var cols = PalletGrid.Columns;
                    eg.SetColumnsFromGrid(cols);
                    eg.Items = PalletGrid.Items;
                    eg.GridTitle = $"Текущий баланс поддонов по продукции: {ProductGridSelectedItem.CheckGet("PRODUCT_FULL_NAME")}. Артикул: {ProductGridSelectedItem.CheckGet("PRODUCT_CODE")}. Дата отчёта: {DateTime.Now.ToString("dd.MM.yyyy HH:mm")}.";
                    await Task.Run(() =>
                    {
                        eg.Make();
                    });
                }
                else
                {
                    var msg = "Не выбрана продукция.";
                    var d = new DialogWindow($"{msg}", "Список продукции на СОХ", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                var msg = "Нет данных для выгрузки в Excel.";
                var d = new DialogWindow($"{msg}", "Список продукции на СОХ", "", DialogWindowButtons.OK);
                d.ShowDialog();
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

            if (ProductGrid != null && ProductGrid.Menu != null && ProductGrid.Menu.Count > 0)
            {
                foreach (var manuItem in ProductGrid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
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

        private void PositionExcelButton_Click(object sender, RoutedEventArgs e)
        {
            ExportToPositionExcel();
        }
    }
}
