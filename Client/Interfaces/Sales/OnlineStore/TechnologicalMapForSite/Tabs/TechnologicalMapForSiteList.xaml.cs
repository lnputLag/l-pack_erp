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
    /// Список позиций для выгрузки на сайт
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class TechnologicalMapForSiteList : UserControl
    {
        public TechnologicalMapForSiteList()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            Init();
            SetDefaults();

            ProcessPermissions();
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Основной датасет с данными по позициям
        /// </summary>
        public ListDataSet GridDataSet { get; set; }

        /// <summary>
        /// Выбранная запись в гриде
        /// </summary>
        public Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void Init()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Изменён",
                        Path="EDITED_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=70,
                        MaxWidth=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Path="ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование товара",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 270,
                        MaxWidth = 540,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид ТК",
                        Path="TECHNOLOGICAL_MAP_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth = 55,
                        MaxWidth = 55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид продукции",
                        Path="PRODUCT_ID2",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth = 55,
                        MaxWidth = 55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование по техкарте",
                        Path="TECHNOLOGICAL_MAP_NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 185,
                        MaxWidth = 370,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул",
                        Path="CODE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 115,
                        MaxWidth = 115,
                    },
                    new DataGridHelperColumn
                    {
                        Header="FEFCO",
                        Path="FEFCO_CODE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 55,
                        MaxWidth = 55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Марка",
                        Path="BRAND",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 70, 
                        MaxWidth = 70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Профиль",
                        Path="PROFILE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 70, 
                        MaxWidth = 70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Цвет",
                        Path="COLOR",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 70,
                        MaxWidth = 70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Длина",
                        Path="LENGTH",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth = 70, 
                        MaxWidth = 70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ширина",
                        Path="WIDTH",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth = 70,
                        MaxWidth = 70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Высота",
                        Path="HEIGTH",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth = 70, 
                        MaxWidth = 70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Длина ТП",
                        Path="PALLET_LENGTH",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth = 80,
                        MaxWidth = 80,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ширина ТП",
                        Path="PALLET_WIDTH",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth = 80,
                        MaxWidth = 80,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Высота ТП",
                        Path="PALLET_HEIGTH",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth = 80,
                        MaxWidth = 80,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Скидка",
                        Path="DISCOUNT",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth = 70,
                        MaxWidth = 70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Оптовая цена без НДС",
                        Path="PRODUCT_WHOLESALE_PRICE",
                        ColumnType=ColumnTypeRef.Double,
                        MinWidth = 115,
                        MaxWidth = 115,
                        Format = "N2",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Оптовая цена с НДС",
                        Path="PRODUCT_WHOLESALE_PRICE_VAT",
                        ColumnType=ColumnTypeRef.Double,
                        MinWidth = 102,
                        MaxWidth = 102,
                        Format = "N2",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наценка на розницу",
                        Path="PRODUCT_RETAIL_PERCENT",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth = 130,
                        MaxWidth = 130,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Розничная цена без НДС",
                        Path="PRODUCT_RETAIL_PRICE",
                        ColumnType=ColumnTypeRef.Double,
                        MinWidth = 128,
                        MaxWidth = 128,
                        Format = "N2",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Розничная цена с НДС",
                        Path="PRODUCT_RETAIL_PRICE_VAT",
                        ColumnType=ColumnTypeRef.Double,
                        MinWidth = 115,
                        MaxWidth = 115,
                        Format = "N2",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Оптовое кол-во поддонов",
                        Path="PALLET_COUNT",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth = 170,
                        MaxWidth = 170,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество на поддоне",
                        Path="COUNT_ON_PALLET",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth = 150,
                        MaxWidth = 150,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество в пачке",
                        Path="COUNT_IN_PACK",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth = 130,
                        MaxWidth = 130,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Минимальный заказ",
                        Path="MINIMUM_COUNT_FOR_ORDER",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth = 130,
                        MaxWidth = 130,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Шаг изменения количества",
                        Path="QUANTITY_STEP",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth = 170,
                        MaxWidth = 170,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Назначение",
                        Path="PRODUCT_TARGET",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 200,
                        MaxWidth = 250,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Раздел 1",
                        Path="PATH_ONE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 220,
                        MaxWidth = 220,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Раздел 2",
                        Path="PATH_TWO",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 135,
                        MaxWidth = 135,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Разделы по применению",
                        Path="PRODUCT_APPLICATION_SECTION",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 200,
                        MaxWidth = 500,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Подраздел по применению",
                        Path="PRODUCT_APPLICATION_SUBSECTION",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 170,
                        MaxWidth = 170,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Лейбл",
                        Path="PRODUCT_LABEL",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 55,
                        MaxWidth = 55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Описание",
                        Path="PRODUCT_DESCRIPTION",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 300,
                        MaxWidth = 500,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Путь до изображения",
                        Path="IMAGE_PATH",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 440,
                        MaxWidth = 440,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Инструкция по применению",
                        Path="PRODUCT_ASSEMBLY_INSTRUCTIONS",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 180,
                        MaxWidth = 360,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Объём",
                        Path="PRODUCT_VOLUME",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth = 55,
                        MaxWidth = 55,
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
                Grid.SetColumns(columns);

                // контекстное меню
                Grid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    {
                        "UpdatePriceOne",
                        new DataGridContextMenuItem()
                        {
                            Header="Обновить цены",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                UpdatePriceOne();
                            }
                        }
                    },
                    { "s0", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                };

                // раскраска строк
                Grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    // определение цветов фона строк
                    {
                        StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            // нет данных для выгрузки на сайт
                            if(string.IsNullOrEmpty(row.CheckGet("PRODUCT_NAME")))
                            {
                                color = HColor.Blue;
                            }

                            // архивная тех карта
                            if (row.CheckGet("ARCHIVE_FLAG").ToInt() > 0)
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
                };

                Grid.SearchText = SearchText;

                Grid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                Grid.OnSelectItem = selectedItem =>
                {
                    SelectedItem = selectedItem;
                    UpdateActions();
                };

                //двойной клик на строке откроет форму редактирования
                Grid.OnDblClick = selectedItem =>
                {
                    if (Central.Navigator.GetRoleLevel("[erp]online_store_assortment") > Role.AccessMode.ReadOnly)
                    {
                        Open();
                    }
                };

                //данные грида
                Grid.OnLoadItems = LoadItems;
                Grid.Run();

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

        public async void LoadItems()
        {
            DisableControls();

            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "TechnologicalMapForSite");
            q.Request.SetParam("Action", "List");
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

                    GridDataSet = dataSet;

                    if (GridDataSet != null && GridDataSet.Items.Count > 0)
                    {
                        foreach (var item in GridDataSet.Items)
                        {
                            double wholesalePrice = item.CheckGet("PRODUCT_WHOLESALE_PRICE").ToDouble();
                            double retailPercent = item.CheckGet("PRODUCT_RETAIL_PERCENT").ToDouble();

                            double retailPrice = 0;
                            double retailPriceVat = 0;

                            if (wholesalePrice > 0)
                            {
                                retailPrice = Math.Round(((wholesalePrice / 100) * retailPercent) + wholesalePrice, 2);
                            }

                            item.CheckAdd("PRODUCT_RETAIL_PRICE", retailPrice.ToString());
                            retailPriceVat = Math.Round(retailPrice * 0.2 + retailPrice, 2);
                            item.CheckAdd("PRODUCT_RETAIL_PRICE_VAT", retailPriceVat.ToString());
                        }
                    }

                    Grid.UpdateItems(GridDataSet);

                    UpdateButtonStyle();
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
            SelectedItem = new Dictionary<string, string>();
            GridDataSet = new ListDataSet();
            Form.SetDefaults();
        }

        public void UpdateActions()
        {
            OpenButton.IsEnabled = false;
            CreateByTechnologicalMapButton.IsEnabled = false;
            DeleteButton.IsEnabled = false;
            Grid.Menu["UpdatePriceOne"].Enabled = false;

            if (SelectedItem != null && SelectedItem.Count > 0)
            {
                if (SelectedItem.CheckGet("ID").ToInt() > 0)
                {
                    DeleteButton.IsEnabled = true;

                    Grid.Menu["UpdatePriceOne"].Enabled = true;

                    if (SelectedItem.CheckGet("ARCHIVE_FLAG").ToInt() == 0)
                    {
                        OpenButton.IsEnabled = true;
                    }
                }
                else if (!string.IsNullOrEmpty(SelectedItem.CheckGet("CODE")))
                {
                    CreateByTechnologicalMapButton.IsEnabled = true;
                }
            }

            ProcessPermissions();
        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel("[erp]online_store_assortment");
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
        /// Обновление внешнего вида кнопок
        /// </summary>
        public void UpdateButtonStyle()
        {
            CSVButton.Style = (Style)CSVButton.TryFindResource("Button");

            if (Grid.Items != null && Grid.Items.Count > 0)
            {
                var item = Grid.Items.FirstOrDefault(x => x.CheckGet("EDITED_FLAG").ToInt() == 1);

                if (item != null)
                {
                    CSVButton.Style = (Style)CSVButton.TryFindResource("FButtonPrimary");
                }
            }
        }

        /// <summary>
        /// Создание новой позиции для выгрузки на сайт
        /// </summary>
        public void CreateNew()
        {
            var i = new TechnologicalMapForSite();
            i.Show();
        }

        public void CreateByTechnologicalMap()
        {
            if (!string.IsNullOrEmpty(SelectedItem.CheckGet("CODE")))
            {
                var i = new TechnologicalMapForSite(SelectedItem.CheckGet("CODE"));
                i.Show();
            }
        }

        public void Open()
        {
            if (SelectedItem != null && SelectedItem.Count > 0)
            {
                if (SelectedItem.CheckGet("ID").ToInt() > 0 && !string.IsNullOrEmpty(SelectedItem.CheckGet("CODE")) && SelectedItem.CheckGet("ARCHIVE_FLAG").ToInt() == 0)
                {
                    var i = new TechnologicalMapForSite(SelectedItem.CheckGet("CODE"), SelectedItem.CheckGet("ID").ToInt());
                    i.Show();
                }
            }
        }

        /// <summary>
        /// Выгрузка существующих данных по позициям в CSV файл для обмена с сайтом
        /// </summary>
        public void ExportToCSV() 
        {
            DisableControls();

            string fullPathToCsvFile = "";

            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "TechnologicalMapForSite");
            q.Request.SetParam("Action", "ExportToCSV");
            q.Request.SetParams(p);

            q.Request.Timeout = 300000;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var dataSet = ListDataSet.Create(result, "ITEMS");

                    if (dataSet != null && dataSet.Items.Count > 0)
                    {
                        fullPathToCsvFile = dataSet.Items.First().CheckGet("FULL_PATH");
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            if (!string.IsNullOrEmpty(fullPathToCsvFile))
            {
                var msg = $"Успешное формирования CSV файла.{Environment.NewLine}Файл находится по пути: {fullPathToCsvFile}.";
                var d = new DialogWindow($"{msg}", "Техкарты для выгрузки на сайт", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
            else if (q.Answer.Status != 145)
            {
                var msg = $"Ошибка формирования CSV файла.";
                var d = new DialogWindow($"{msg}", "Техкарты для выгрузки на сайт", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }

            EnableControls();

            Grid.LoadItems();
        }

        /// <summary>
        /// Выгрузить остатки
        /// </summary>
        public void ExportBalances()
        {
            if (Grid.Items != null && Grid.Items.Count > 0)
            {
                DisableControls();

                string fullPathToCsvFile = "";

                var p = new Dictionary<string, string>();

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "TechnologicalMapForSite");
                q.Request.SetParam("Action", "ExportBalance");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var dataSet = ListDataSet.Create(result, "ITEMS");

                        if (dataSet != null && dataSet.Items.Count > 0)
                        {
                            fullPathToCsvFile = dataSet.Items.First().CheckGet("FULL_PATH");
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }

                if (!string.IsNullOrEmpty(fullPathToCsvFile))
                {
                    var msg = $"Успешное формирования CSV файла остатков.{Environment.NewLine}Файл находится по пути: {fullPathToCsvFile}.";
                    var d = new DialogWindow($"{msg}", "Техкарты для выгрузки на сайт", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
                else
                {
                    var msg = $"Ошибка формирования CSV файла остатков.";
                    var d = new DialogWindow($"{msg}", "Техкарты для выгрузки на сайт", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }

                EnableControls();
            }
        }

        /// <summary>
        /// Создание Excel файла по данным в гриде
        /// </summary>
        public async void ExportToExcel()
        {
            if (Grid.Items != null)
            {
                if (Grid.Items.Count > 0)
                {
                    var eg = new ExcelGrid();
                    var cols = Grid.Columns;
                    eg.SetColumnsFromGrid(cols);
                    eg.Items = Grid.Items;
                    await Task.Run(() =>
                    {
                        eg.Make();
                    });
                }
            }
        }

        /// <summary>
        /// УДаление выбранной техкарты для выгрузки на сайт
        /// </summary>
        public void Delete()
        {
            if (SelectedItem != null)
            {
                if (SelectedItem.Count > 0)
                {
                    if (SelectedItem.CheckGet("ID").ToInt() > 0)
                    {
                        string msg = "Удалить выбранную позицию?";
                        var d = new DialogWindow($"{msg}", "Техкарты для выгрузки на сайт", "", DialogWindowButtons.NoYes);
                        if (d.ShowDialog() == true)
                        {
                            DisableControls();

                            var p = new Dictionary<string, string>();
                            p.Add("ID", SelectedItem.CheckGet("ID").ToInt().ToString());

                            var q = new LPackClientQuery();
                            q.Request.SetParam("Module", "Sales");
                            q.Request.SetParam("Object", "TechnologicalMapForSite");
                            q.Request.SetParam("Action", "Delete");
                            q.Request.SetParams(p);

                            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                            q.DoQuery();

                            if (q.Answer.Status == 0)
                            {
                                int technologicalMapForSiteId = 0;

                                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                                if (result != null)
                                {
                                    var dataSet = ListDataSet.Create(result, "ITEMS");

                                    if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                                    {
                                        technologicalMapForSiteId = dataSet.Items.First().CheckGet("ID").ToInt();
                                    }
                                }

                                if (technologicalMapForSiteId > 0)
                                {
                                    LoadItems();

                                    msg = $"Успешное удаление выбранной позиции.";
                                    d = new DialogWindow($"{msg}", "Техкарты для выгрузки на сайт", "", DialogWindowButtons.OK);
                                    d.ShowDialog();
                                }
                                else
                                {
                                    msg = $"Ошибка удаления выбранной позиции.";
                                    d = new DialogWindow($"{msg}", "Техкарты для выгрузки на сайт", "", DialogWindowButtons.OK);
                                    d.ShowDialog();
                                }
                            }
                            else
                            {
                                q.ProcessError();
                            }

                            EnableControls();
                        }
                    }
                    else
                    {
                        var msg = $"Не возможно удалить выбранную позицию. Не найден идентификатор.";
                        var d = new DialogWindow($"{msg}", "Техкарты для выгрузки на сайт", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    var msg = $"Не выбрана позиция для удаления.";
                    var d = new DialogWindow($"{msg}", "Техкарты для выгрузки на сайт", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                var msg = $"Не выбрана позиция для удаления.";
                var d = new DialogWindow($"{msg}", "Техкарты для выгрузки на сайт", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Обновляем цены для всех техкарт для сайта по данным из расчёта цен(Л-ПАК) 
        /// </summary>
        public void UpdatePriceAll()
        {
            if (Grid != null && Grid.Items != null && Grid.Items.Count > 0)
            {
                DisableControls();

                var p = new Dictionary<string, string>();
                // 8862 -- pokupatel.id_pok (покупатель СОХ)
                p.Add("CUSTOMER_ID", "8862");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "TechnologicalMapForSite");
                q.Request.SetParam("Action", "UpdatePrice");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    int updatingResult = 0;

                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var dataSet = ListDataSet.Create(result, "ITEMS");
                        if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                        {
                            updatingResult = dataSet.Items.First().CheckGet("RESULT").ToInt();
                        }
                    }

                    if (updatingResult > 0)
                    {
                        LoadItems();

                        var msg = $"Успешное обновление цен. Необходимо отправить обновлённые цены на сайт и на СОХ.";
                        var d = new DialogWindow($"{msg}", "Техкарты для выгрузки на сайт", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                    else
                    {
                        var msg = $"Ошибка обновления цен. Пожалуйста, повторите операцию.";
                        var d = new DialogWindow($"{msg}", "Техкарты для выгрузки на сайт", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }

                EnableControls();
            }
        }

        /// <summary>
        /// Обновляем цены для выбранной техкарт для сайта по данным из расчёта цен(Л-ПАК) 
        /// </summary>
        public void UpdatePriceOne()
        {
            if (SelectedItem != null && SelectedItem.Count > 0 && SelectedItem.CheckGet("ID").ToInt() > 0)
            {
                DisableControls();

                var p = new Dictionary<string, string>();
                // 8862 -- pokupatel.id_pok (покупатель СОХ)
                p.Add("CUSTOMER_ID", "8862");
                p.Add("ID", SelectedItem.CheckGet("ID"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "TechnologicalMapForSite");
                q.Request.SetParam("Action", "UpdatePriceById");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    int updatingResult = 0;

                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var dataSet = ListDataSet.Create(result, "ITEMS");
                        if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                        {
                            updatingResult = dataSet.Items.First().CheckGet("RESULT").ToInt();
                        }
                    }

                    if (updatingResult > 0)
                    {
                        LoadItems();

                        var msg = $"Успешное обновление цен для выбранной позиции. Необходимо отправить обновлённые цены на сайт и на СОХ.";
                        var d = new DialogWindow($"{msg}", "Техкарты для выгрузки на сайт", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                    else
                    {
                        var msg = $"Ошибка обновления цен для выбранной позиции. Пожалуйста, повторите операцию.";
                        var d = new DialogWindow($"{msg}", "Техкарты для выгрузки на сайт", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }

                EnableControls();
            }
            else
            {
                var msg = $"Выбранная тех карта не заполнена.";
                var d = new DialogWindow($"{msg}", "Техкарты для выгрузки на сайт", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Sales",
                ReceiverName = "",
                SenderName = "TechnologicalMapForSiteList",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            Grid.Destruct();
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.IndexOf("Sales") > -1)
            {
                if (m.ReceiverName.IndexOf("TechnologicalMapForSiteList") > -1)
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            Grid.LoadItems();
                            break;
                    }
                }
            }
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
        /// отображение справочной статьи
        /// (относительный путь)
        /// </summary>
        public void ShowHelp()
        {
            //FIXME: Нужно сделать документацию
            Central.ShowHelp("/doc/l-pack-erp-new/application/online_shop/online_shop_tk");
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            Open();
        }

        private void ResreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadItems();
        }

        private void CreateByTechnologicalMapButton_Click(object sender, RoutedEventArgs e)
        {
            CreateByTechnologicalMap();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Delete();
        }

        private void BalancesButton_Click(object sender, RoutedEventArgs e)
        {
            ExportBalances();
        }

        private void CSVButton_Click(object sender, RoutedEventArgs e)
        {
            ExportToCSV();
        }

        private void ExcelButton_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcel();
        }


        private void UpdatePriceButton_Click(object sender, RoutedEventArgs e)
        {
            UpdatePriceAll();
        }
    }
}
