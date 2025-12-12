using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Client.Interfaces.Main.DataGridHelperColumn;
using Client.Interfaces.Production;
using System.IO;

namespace Client.Interfaces.Sales
{
    /// <summary>
    /// Поставки на склад ответственного хранения
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class ResponsibleStockList : UserControl
    {
        public ResponsibleStockList()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            SetDefaults();

            OrderGridInit();
            PositionGridInit();

            if (Central.DebugMode)
            {
                CreateFileButton.Visibility = Visibility.Visible;
                SendFileButton.Visibility = Visibility.Visible;
                GetFileButton.Visibility = Visibility.Visible;
                CreateFileButton.IsEnabled = true;
                SendFileButton.IsEnabled = true;
                GetFileButton.IsEnabled = true;
            }
            else
            {
                CreateFileButton.Visibility = Visibility.Collapsed;
                SendFileButton.Visibility = Visibility.Collapsed;
                GetFileButton.Visibility = Visibility.Collapsed;
                CreateFileButton.IsEnabled = false;
                SendFileButton.IsEnabled = false;
                GetFileButton.IsEnabled = false;
            }

            ProcessPermissions();
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Основной датасет с данными по позициям
        /// </summary>
        public ListDataSet OrderGridDataSet { get; set; }

        /// <summary>
        /// Выбранная запись в гриде
        /// </summary>
        public Dictionary<string, string> OrderGridSelectedItem { get; set; }

        /// <summary>
        /// Основной датасет с данными по позициям
        /// </summary>
        public ListDataSet PositionGridDataSet { get; set; }

        /// <summary>
        /// Выбранная запись в гриде
        /// </summary>
        public Dictionary<string, string> PositionGridSelectedItem { get; set; }

        /// <summary>
        /// Идентификатор покупателя для запроса для верхнего грида
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// id_dir
        /// </summary>
        public int DirId { get; set; }

        public static string ProxyStorageCode = "order_proxy";

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void OrderGridInit()
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
                    };

                Form.SetFields(fields);
            }

            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
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
                        Header="Дата отгрузки",
                        Path="SHIPPING_DATE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=110,
                        MaxWidth=110,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата доставки",
                        Path="DELIVERY_DATE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=110,
                        MaxWidth=110,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ Заявки",
                        Path="NUMBER_ORDER",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=80,
                        MaxWidth=80,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус отгрузки",
                        Path="SHIPMENT_STATUS",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=55,
                        MaxWidth=120,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // При согласовании была изменена дата
                                    if(row.CheckGet("STATUS").ToInt() == 23)
                                    {
                                        color = HColor.Yellow;
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
                        Header="Тендер",
                        Path="TENDER_STATUS",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=57,
                        MaxWidth=80,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                        // Если по этой заявке на завершился тендер
                                        if(row.CheckGet("TENDER_STATUS_ID").ToInt() < 3 && row.CheckGet("TRANSPORT_ID").ToInt() > 0)
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
                        Header="Заявка СОХ",
                        Path="DOCUMENT_STATUS",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=57,
                        MaxWidth=82,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // Если по этой заявке не обработан файл в ИС СОХ
                                    if (row.CheckGet("TRANSPORT_ID").ToInt() > 0 
                                        && row.CheckGet("DOCUMENT_STATUS_ID").ToInt() != 1 
                                        && row.CheckGet("TYPE_ORDER").ToInt() == 3)
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
                        Header="Покупатель",
                        Path="CUSTOMER",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=80,
                        MaxWidth=200,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Примечание кладовщику",
                        Path="NOTE_STOCKMAN",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=160,
                        MaxWidth=200,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Примечание грузчику",
                        Path="NOTE_LOADER",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=160,
                        MaxWidth=200,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Примечание логисту",
                        Path="NOTE_LOGISTICIAN",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=160,
                        MaxWidth=200,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Самовывоз",
                        Path="SELFSHIP",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=55,
                        MaxWidth=55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Доверенность",
                        Path="PROXY_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=55,
                        MaxWidth=55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Перевозчик",
                        Path="CARRIER",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=160,
                        MaxWidth=200,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Водитель",
                        Path="DRIVER",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=160,
                        MaxWidth=200,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Автомобиль",
                        Path="CAR",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=160,
                        MaxWidth=200,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Телефон",
                        Path="PHONE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=160,
                        MaxWidth=200,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Паспорт",
                        Path="PASSPORT",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=160,
                        MaxWidth=200,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид отгрузки",
                        Path="TRANSPORT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус позиций",
                        Path="POSITION_STATUS",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=160,
                        MaxWidth=200,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Дт/вр приезда водителя",
                        Path="DRIVER_ARRIVE",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm",
                        MinWidth=95,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Дт/вр выезда водителя",
                        Path="DRIVER_DEPART",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm",
                        MinWidth=95,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Изменённая дата отгрузки",
                        Path="PROPOSED_DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy",
                        MinWidth=95,
                    },

                    new DataGridHelperColumn()
                    {
                        Header="TYPE_ORDER",
                        Path="TYPE_ORDER",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид накладной расхода",
                        Path="INVOICE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=55,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Дата печати отгрузочных документов",
                        Path="INVOICE_PRINT_DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy",
                        MinWidth=95,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Файл доверенности",
                        Path="PROXY",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=80,
                        MaxWidth=80,
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
                OrderGrid.SetColumns(columns);

                // раскраска строк
                OrderGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    // определение цветов фона строк
                    {
                        StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            if (row.CheckGet("STATUS").ToInt() == 4 
                                || !string.IsNullOrEmpty(row.CheckGet("INVOICE_PRINT_DTTM")))
                            {
                                color = HColor.Green;
                            }

                            if (string.IsNullOrEmpty(row.CheckGet("ID_TS")))
                            {
                                color = HColor.Blue;
                            }

                            if (row.CheckGet("STATUS").ToInt() == 2)
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

                OrderGrid.SearchText = SearchText;
                OrderGrid.Init();

                // контекстное меню
                OrderGrid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    {
                        "EditOrder",
                        new DataGridContextMenuItem()
                        {
                            Header="Изменить заявку",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                EditOrder();
                            }
                        }
                    },
                    {
                        "CancelOrder",
                        new DataGridContextMenuItem()
                        {
                            Header="Отменить заявку",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                CancelOrder();
                            }
                        }
                    },
                    {
                        "DeleteOrder",
                        new DataGridContextMenuItem()
                        {
                            Header="Удалить заявку",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                DeleteOrder();
                            }
                        }
                    },
                    { "s0", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    {
                        "SendForAgreement",
                        new DataGridContextMenuItem()
                        {
                            Header="Отправить на согласование",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                SendForAgreement();
                            }
                        }
                    },
                    {
                        "ConfirmDateChanges",
                        new DataGridContextMenuItem()
                        {
                            Header="Подтвердить изменённую дату",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                ConfirmDateChanges();
                            }
                        }
                    },
                    { "s1", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    {
                        "CreateShipment",
                        new DataGridContextMenuItem()
                        {
                            Header="Создать отгрузку",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                CreateShipment();
                            }
                        }
                    },
                    {
                        "EditShipment",
                        new DataGridContextMenuItem()
                        {
                            Header="Изменить отгрузку",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                EditShipment();
                            }
                        }
                    },
                    {
                        "DeleteShipment",
                        new DataGridContextMenuItem()
                        {
                            Header="Удалить отгрузку",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                DeleteShipment();
                            }
                        }
                    },
                    { "s2", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    {
                        "AllowShipment",
                        new DataGridContextMenuItem()
                        {
                            Header="Разрешить отгрузку",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                EditAllowShipment(1);
                            }
                        }
                    },
                    {
                        "DisallowShipment",
                        new DataGridContextMenuItem()
                        {
                            Header="Запретить отгрузку",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                EditAllowShipment(0);
                            }
                        }
                    },
                    { "s3", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    {
                        "GetLoadingMap",
                        new DataGridContextMenuItem()
                        {
                            Header="Порядок загрузки",
                            Action=()=>
                            {
                                GetLoadingMap();
                            }
                        }
                    },
                    { "s4", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    {
                        "AddProxy",
                        new DataGridContextMenuItem()
                        {
                            Header="Добавить доверенность",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                AddProxy();
                            }
                        }
                    },
                    {
                        "OpenProxy",
                        new DataGridContextMenuItem()
                        {
                            Header="Открыть доверенность",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                OpenProxy();
                            }
                        }
                    },
                    {
                        "DeleteProxy",
                        new DataGridContextMenuItem()
                        {
                            Header="Удалить доверенность",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                DeleteProxy();
                            }
                        }
                    },
                };

                //при выборе строки в гриде, обновляются актуальные действия для записи
                OrderGrid.OnSelectItem = selectedItem =>
                {
                    OrderGridSelectedItem = selectedItem;

                    if (OrderGridSelectedItem != null)
                    {
                        UpdateButtons();
                        PositionGridLoadItems();
                    }
                };

                OrderGrid.OnFilterItems = () =>
                {
                    if (OrderGrid.GridItems != null)
                    {
                        if (OrderGrid.GridItems.Count > 0)
                        {
                            if (TypeSelectBox.SelectedItem.Key != null)
                            {
                                int key = TypeSelectBox.SelectedItem.Key.ToInt();
                                var items = new List<Dictionary<string, string>>();

                                switch (key)
                                {
                                    // Все типы отгрузок
                                    case 0:
                                        items = OrderGrid.GridItems;
                                        break;

                                    // Поставка на СОХ
                                    case 1:
                                        items.AddRange(OrderGrid.GridItems.Where(x => x.CheckGet("TYPE_ORDER").ToInt() == 3));
                                        break;

                                    // Отгрузка продукции ИМ из Липецка
                                    case 2:
                                        items.AddRange(OrderGrid.GridItems.Where(x => x.CheckGet("TYPE_ORDER").ToInt() == 1));
                                        break;

                                    default:
                                        items = OrderGrid.GridItems;
                                        break;
                                }

                                OrderGrid.GridItems = items;
                            }

                            if (StatusSelectBox.SelectedItem.Key != null)
                            {
                                var key = StatusSelectBox.SelectedItem.Key.ToInt();
                                var items = new List<Dictionary<string, string>>();

                                switch (key)
                                {
                                    // Все статусы
                                    case -1:
                                        items = OrderGrid.GridItems;
                                        break;

                                    default:
                                        items.AddRange(OrderGrid.GridItems.Where(x => x.CheckGet("STATUS").ToInt() == key));
                                        break;
                                }

                                OrderGrid.GridItems = items;
                            }

                            if (HideCompletedCheckBox != null)
                            {
                                var items = new List<Dictionary<string, string>>();

                                if (HideCompletedCheckBox.IsChecked == true)
                                {
                                    items.AddRange(OrderGrid.GridItems.Where(row => row.CheckGet("STATUS").ToInt() != 4));
                                }
                                else
                                {
                                    items = OrderGrid.GridItems;
                                }

                                OrderGrid.GridItems = items;
                            }
                        }
                    }
                };

                OrderGrid.SetSorting("ID", System.ComponentModel.ListSortDirection.Descending);

                //данные грида
                OrderGrid.OnLoadItems = OrderGridLoadItems;
                OrderGrid.Run();
            }
        }

        public async void OrderGridLoadItems()
        {
            DisableControls();

            var p = new Dictionary<string, string>();
            p.Add("CUSTOMER_ID", CustomerId.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "ResponsibleStock");
            q.Request.SetParam("Action", "ListOrder");
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
                    OrderGridDataSet = dataSet;

                    OrderGrid.UpdateItems(OrderGridDataSet);
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
        public void PositionGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Path="IDORDERDATES",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул",
                        Path="CODE",
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
                        MaxWidth=200,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Path="QUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=85,
                        MaxWidth=85,
                    },
                    new DataGridHelperColumn
                    {
                        Header="По заявке, шт",
                        Path="COUNT_IN_ORDER",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=75,
                        MaxWidth=85,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Всего на складе, шт",
                        Path="COUNT_IN_STOCK",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=85,
                        MaxWidth=105,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Погружено, шт",
                        Path="COUNT_ALREADY_LOADED",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=75,
                        MaxWidth=85,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Цена без НДС",
                        Path="PRICE_WITHOUT_NDS",
                        ColumnType=ColumnTypeRef.Double,
                        MinWidth=67,
                        MaxWidth=95,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Цена с НДС",
                        Path="PRICE_WITH_NDS",
                        ColumnType=ColumnTypeRef.Double,
                        MinWidth=55,
                        MaxWidth=85,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ед. измерения",
                        Path="UNITS",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=56,
                        MaxWidth=56,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Площадь",
                        Path="SQUARE",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N4",
                        MinWidth=70,
                        MaxWidth=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Паллет, шт",
                        Path="COUNT_PALLET",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=56,
                        MaxWidth=77,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус",
                        Path="STATUS_NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=56,
                        MaxWidth=95,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Запрет раскроя",
                        Path="NO_CUTTING",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=55,
                        MaxWidth=105,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Производственное задание",
                        Path="PRODUCTION_TASK_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=55,
                        MaxWidth=175,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Примечание кладовщику",
                        Path="NOTE_STOCKMAN",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=160,
                        MaxWidth=200,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Примечание грузчику",
                        Path="NOTE_LOADER",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=160,
                        MaxWidth=200,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Внутреннее наименование",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=100,
                        MaxWidth=200,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид продукции",
                        Path="ID2",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=55,
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
                        Header="Ид статуса",
                        Path="STATUS",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=56,
                        MaxWidth=77,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Перемещено, шт",
                        Path="COUNT_ALREADY_MOVED",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=75,
                        MaxWidth=85,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Отгружено, шт",
                        Path="COUNT_ALREADY_SHIPPED",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=75,
                        MaxWidth=85,
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
                PositionGrid.SetColumns(columns);

                // раскраска строк
                PositionGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    // определение цветов фона строк
                    {
                        StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            if(row.CheckGet("NO_CUTTING").ToInt() == 1)
                            {
                                color = HColor.Red;
                            }

                            if (string.IsNullOrEmpty(row.CheckGet("PRICE_WITHOUT_NDS")))
                            {
                                color = HColor.Blue;
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },
                };

                PositionGrid.Init();

                // контекстное меню
                PositionGrid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    {
                        "AllowCutting",
                        new DataGridContextMenuItem()
                        {
                            Header="Разрешить раскрой",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                EditAllowCutting(0);
                            }
                        }
                    },
                    {
                        "DisallowCutting",
                        new DataGridContextMenuItem()
                        {
                            Header="Запретить раскрой",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                EditAllowCutting(1);
                            }
                        }
                    },
                };

                //при выборе строки в гриде, обновляются актуальные действия для записи
                PositionGrid.OnSelectItem = selectedItem =>
                {
                    PositionGridSelectedItem = selectedItem;
                    UpdateButtons();
                };

                PositionGrid.Run();
            }
        }

        public async void PositionGridLoadItems()
        {
            DisableControls();

            PositionGridSelectedItem = new Dictionary<string, string>();

            var p = new Dictionary<string, string>();
            p.Add("NSTHET", OrderGridSelectedItem.CheckGet("ID"));

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "ResponsibleStock");
            q.Request.SetParam("Action", "ListShippingPosition");
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
                    PositionGridDataSet = dataSet;
                    PositionGrid.UpdateItems(PositionGridDataSet);
                }
            }
            else
            {
                q.ProcessError();
            }

            EnableControls();
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

            if (OrderGrid != null && OrderGrid.Menu != null && OrderGrid.Menu.Count > 0)
            {
                foreach (var manuItem in OrderGrid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }

            if (PositionGrid != null && PositionGrid.Menu != null && PositionGrid.Menu.Count > 0)
            {
                foreach (var manuItem in PositionGrid.Menu)
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

        public void UpdateButtons()
        {
            EditButton.IsEnabled = false;
            DeleteButton.IsEnabled = false;

            OrderGrid.Menu["EditOrder"].Enabled = false;
            OrderGrid.Menu["DeleteOrder"].Enabled = false;
            OrderGrid.Menu["CreateShipment"].Enabled = false;
            OrderGrid.Menu["EditShipment"].Enabled = false;
            OrderGrid.Menu["DeleteShipment"].Enabled = false;
            OrderGrid.Menu["AllowShipment"].Enabled = false;
            OrderGrid.Menu["DisallowShipment"].Enabled = false;
            OrderGrid.Menu["SendForAgreement"].Enabled = false;
            OrderGrid.Menu["ConfirmDateChanges"].Enabled = false;
            OrderGrid.Menu["AddProxy"].Enabled = false;
            OrderGrid.Menu["OpenProxy"].Enabled = false;
            OrderGrid.Menu["DeleteProxy"].Enabled = false;

            OrderGrid.Menu["GetLoadingMap"].Enabled = true;

            AddPositionButton.IsEnabled = false;
            EditPositionButton.IsEnabled = false;
            DeletePositionButton.IsEnabled = false;

            if (PositionGrid.Menu.Count > 0)
            {
                PositionGrid.Menu["AllowCutting"].Enabled = false;
                PositionGrid.Menu["DisallowCutting"].Enabled = false;
            }

            if (OrderGridSelectedItem != null)
            {
                if (!string.IsNullOrEmpty(OrderGridSelectedItem.CheckGet("ID")))
                {
                    if (OrderGridSelectedItem.CheckGet("STATUS").ToInt() == 1)
                    {
                        EditButton.IsEnabled = true;
                        OrderGrid.Menu["EditOrder"].Enabled = true;
                        AddPositionButton.IsEnabled = true;

                        if (OrderGridSelectedItem.CheckGet("ORDER_NSTHET").ToInt() == 0)
                        {
                            DeleteButton.IsEnabled = true;
                            OrderGrid.Menu["DeleteOrder"].Enabled = true;
                        }

                        if (PositionGridSelectedItem != null && PositionGridSelectedItem.Count > 0)
                        {
                            OrderGrid.Menu["SendForAgreement"].Enabled = true;
                        }
                    }
                    else if (OrderGridSelectedItem.CheckGet("STATUS").ToInt() == 23)
                    {
                        OrderGrid.Menu["ConfirmDateChanges"].Enabled = true;
                    }

                    if (string.IsNullOrEmpty(OrderGridSelectedItem.CheckGet("PROXY")))
                    {
                        OrderGrid.Menu["AddProxy"].Enabled = true;
                    }
                    else
                    {
                        OrderGrid.Menu["OpenProxy"].Enabled = true;
                        OrderGrid.Menu["DeleteProxy"].Enabled = true;
                    }
                }

                if (string.IsNullOrEmpty(OrderGridSelectedItem.CheckGet("ID_TS")))
                {
                    if (OrderGridSelectedItem.CheckGet("STATUS").ToInt() == 0)
                    {
                        OrderGrid.Menu["CreateShipment"].Enabled = true;
                    }
                }
                else
                {
                    OrderGrid.Menu["GetLoadingMap"].Enabled = true;

                    if (OrderGridSelectedItem.CheckGet("STATUS").ToInt() == 0)
                    {
                        OrderGrid.Menu["EditShipment"].Enabled = true;

                        if (OrderGridSelectedItem.CheckGet("TENDER").ToInt() == 0)
                        {
                            OrderGrid.Menu["DeleteShipment"].Enabled = true;
                        }

                        if (OrderGridSelectedItem.CheckGet("FINISH").ToInt() == 0)
                        {
                            OrderGrid.Menu["AllowShipment"].Enabled = true;
                        }
                        else if (OrderGridSelectedItem.CheckGet("FINISH").ToInt() == 1)
                        {
                            OrderGrid.Menu["DisallowShipment"].Enabled = true;
                        }
                    }
                }
            }

            if (PositionGridSelectedItem != null)
            {
                if (OrderGridSelectedItem.CheckGet("STATUS").ToInt() == 1 || OrderGridSelectedItem.CheckGet("STATUS").ToInt() == 2)
                {
                    if (!string.IsNullOrEmpty(PositionGridSelectedItem.CheckGet("IDORDERDATES")))
                    {
                        EditPositionButton.IsEnabled = true;

                        if (PositionGridSelectedItem.CheckGet("PRODUCTION_TASK_FLAG").ToInt() == 0)
                        {
                            DeletePositionButton.IsEnabled = true;
                        }
                    }

                    if (PositionGridSelectedItem.CheckGet("NO_CUTTING").ToInt() == 1)
                    {
                        PositionGrid.Menu["AllowCutting"].Enabled = true;
                    }
                    else if (PositionGridSelectedItem.CheckGet("NO_CUTTING").ToInt() == 0)
                    {
                        PositionGrid.Menu["DisallowCutting"].Enabled = true;
                    }
                }
            }

            ProcessPermissions();
        }

        /// <summary>
        /// Отпарвить на согласование
        /// </summary>
        public void SendForAgreement()
        {
            DisableControls();

            if (OrderGridSelectedItem != null)
            {
                bool succesfulFlag = false;

                var p = new Dictionary<string, string>();
                p.Add("ID", OrderGridSelectedItem.CheckGet("ID"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "ResponsibleStock");
                q.Request.SetParam("Action", "SendForAgreement");
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
                            int resultId = dataSet.Items.First().CheckGet("ID").ToInt();
                            if (resultId > 0)
                            {
                                succesfulFlag = true;
                            }
                        }
                    }

                    if (succesfulFlag)
                    {
                        Refresh();
                    }
                    else
                    {
                        var msg = "Ошибка изменения статуса отгрузки";
                        var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            EnableControls();
        }

        /// <summary>
        /// Подтвердить изменённую дату
        /// </summary>
        public void ConfirmDateChanges()
        {
            DisableControls();

            if (OrderGridSelectedItem != null)
            {
                var p = new Dictionary<string, string>();
                p.Add("ID", OrderGridSelectedItem.CheckGet("ID"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "ResponsibleStock");
                q.Request.SetParam("Action", "ConfirmShipmentDateChanges");
                q.Request.SetParams(p);

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    bool succesfulFlag = false;

                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var dataSet = ListDataSet.Create(result, "ITEMS");

                        if (dataSet != null && dataSet.Items.Count > 0)
                        {
                            int resultId = dataSet.Items.First().CheckGet("ID").ToInt();

                            if (resultId > 0)
                            {
                                succesfulFlag = true;
                            }
                        }
                    }

                    if (!succesfulFlag)
                    {
                        string msg = "Ошибка подтверждения изменённой даты. Пожалуйста, сообщите о проблеме.";
                        var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                    else
                    {
                        Refresh();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
            else
            {
                var msg = "Не выбрана заявка";
                DialogWindow.ShowDialog($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
            }

            EnableControls();
        }

        /// <summary>
        /// Разрешить/запретить отгрузку
        /// </summary>
        public void EditAllowShipment(int finish)
        {
            DisableControls();

            if (OrderGridSelectedItem != null)
            {
                bool succesfulFlag = false;

                string idTs = OrderGridSelectedItem.CheckGet("ID_TS");

                var p = new Dictionary<string, string>();
                p.Add("ID_TS", idTs);
                p.Add("FINISH", finish.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "ResponsibleStock");
                q.Request.SetParam("Action", "UpdateTransportFinish");
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
                            int resultIdTs = dataSet.Items.First().CheckGet("ID_TS").ToInt();

                            if (resultIdTs > 0)
                            {
                                succesfulFlag = true;
                            }
                        }
                    }

                    if (!succesfulFlag)
                    {
                        var msg = "Ошибка изменения статуса отгрузки";
                        var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                    else
                    {
                        var msg = "Успешное изменение статуса отгрузки";
                        var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                        d.ShowDialog();

                        Refresh();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            EnableControls();
        }

        /// <summary>
        /// Разрешить/Запретить раскрой
        /// </summary>
        public void EditAllowCutting(int noCutting)
        {
            DisableControls();

            if (PositionGridSelectedItem != null)
            {
                bool succesfulFlag = false;

                string idorderdates = PositionGridSelectedItem.CheckGet("IDORDERDATES");

                var p = new Dictionary<string, string>();
                p.Add("IDORDERDATES", idorderdates);
                p.Add("NO_CUTTING", noCutting.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "ResponsibleStock");
                q.Request.SetParam("Action", "UpdateNoncutting");
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
                            int resultIdorderdates = dataSet.Items.First().CheckGet("IDORDERDATES").ToInt();

                            if (resultIdorderdates > 0)
                            {
                                succesfulFlag = true;
                            }
                        }
                    }

                    if (!succesfulFlag)
                    {
                        var msg = "Ошибка изменения статуса раскроя";
                        var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                    else
                    {
                        var msg = "Успешное изменение статуса раскроя";
                        var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                        d.ShowDialog();

                        Refresh();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            EnableControls();
        }

        /// <summary>
        /// Изменение статуса заявки на производство на "Отменена"
        /// </summary>
        public void CancelOrder()
        {
            DisableControls();

            if (OrderGridSelectedItem != null)
            {
                var msg = "Вы действительно хотите отменить заявку?";
                if (DialogWindow.ShowDialog($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.YesNo) == true)
                {
                    bool succesfulFlag = false;

                    var p = new Dictionary<string, string>();
                    p.Add("ID", OrderGridSelectedItem.CheckGet("ID"));
                    p.Add("STATUS", "2");

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sales");
                    q.Request.SetParam("Object", "ResponsibleStock");
                    q.Request.SetParam("Action", "UpdateOrderStatus");
                    q.Request.SetParams(p);

                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            var dataSet = ListDataSet.Create(result, "ITEMS");

                            if (dataSet != null && dataSet.Items.Count > 0)
                            {
                                int resultTransportId = dataSet.Items.First().CheckGet("ID").ToInt();

                                if (resultTransportId > 0)
                                {
                                    succesfulFlag = true;
                                }
                            }
                        }

                        if (!succesfulFlag)
                        {
                            msg = "Ошибка отмены заявки. Пожалуйста, сообщите о проблеме.";
                            var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                        else
                        {
                            Refresh();
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
            }
            else
            {
                var msg = "Не выбрана заявка";
                DialogWindow.ShowDialog($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
            }

            EnableControls();
        }

        public void DeleteOrder()
        {
            DisableControls();

            if (OrderGridSelectedItem != null)
            {
                var msg = "Вы действительно хотите удалить заявку?";

                if (DialogWindow.ShowDialog($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.YesNo) == true)
                {
                    bool succesfulFlag = false;

                    string nsthet = OrderGridSelectedItem.CheckGet("ID");

                    var p = new Dictionary<string, string>();
                    p.Add("NSTHET", nsthet);

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sales");
                    q.Request.SetParam("Object", "ResponsibleStock");
                    q.Request.SetParam("Action", "DeleteOrder");
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
                                int resultNsthet = dataSet.Items.First().CheckGet("NSTHET").ToInt();

                                if (resultNsthet > 0)
                                {
                                    succesfulFlag = true;
                                }
                            }
                        }

                        if (!succesfulFlag)
                        {
                            msg = "Ошибка удаления заявки";
                            var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                        else
                        {
                            msg = "Успешное удаление заявки";
                            var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                            d.ShowDialog();

                            Refresh();
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
            }

            EnableControls();
        }

        public void DeletePosition()
        {
            DisableControls();

            if (PositionGridSelectedItem != null)
            {
                var msg = "Вы действительно хотите удалить позицию в заявке?";

                if (DialogWindow.ShowDialog($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.YesNo) == true)
                {
                    bool succesfulFlag = false;

                    string idorderdates = PositionGridSelectedItem.CheckGet("IDORDERDATES");

                    var p = new Dictionary<string, string>();
                    p.Add("IDORDERDATES", idorderdates);

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sales");
                    q.Request.SetParam("Object", "ResponsibleStock");
                    q.Request.SetParam("Action", "DeleteOrderPosition");
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
                                int resultIdorderdates = dataSet.Items.First().CheckGet("IDORDERDATES").ToInt();

                                if (resultIdorderdates > 0)
                                {
                                    succesfulFlag = true;
                                }
                            }
                        }

                        if (!succesfulFlag)
                        {
                            msg = "Ошибка удаления позиции в заявке";
                            var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                        else
                        {
                            msg = "Успешное удаление позиции в заявке";
                            var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                            d.ShowDialog();

                            Refresh();
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
            }

            EnableControls();
        }

        public void DeleteShipment()
        {
            DisableControls();

            if (OrderGridSelectedItem != null)
            {
                var msg = "Вы действительно хотите удалить заявку из отгрузки?";

                if (DialogWindow.ShowDialog($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.YesNo) == true)
                {
                    bool succesfulFlag = false;

                    string nsthet = OrderGridSelectedItem.CheckGet("ID");
                    string idTs = OrderGridSelectedItem.CheckGet("ID_TS");

                    var p = new Dictionary<string, string>();
                    p.Add("NSTHET", nsthet);
                    p.Add("ID_TS", idTs);

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sales");
                    q.Request.SetParam("Object", "ResponsibleStock");
                    q.Request.SetParam("Action", "DeleteShipment");
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
                                int resultNsthet = dataSet.Items.First().CheckGet("NSTHET").ToInt();

                                if (resultNsthet > 0)
                                {
                                    succesfulFlag = true;
                                }
                            }
                        }

                        if (!succesfulFlag)
                        {
                            msg = "Ошибка удаления заявки из отгрузки";
                            var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                        else
                        {
                            msg = "Успешное удаление заявки из отгрузки";
                            var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                            d.ShowDialog();

                            Refresh();
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
            }

            EnableControls();
        }

        public async void GetLoadingMap()
        {
            DisableControls();

            if (OrderGridSelectedItem != null)
            {
                if (OrderGridSelectedItem.CheckGet("ID_TS").ToInt() > 0)
                {
                    string idTs = OrderGridSelectedItem.CheckGet("ID_TS");

                    var p = new Dictionary<string, string>();
                    p.Add("ID", idTs);

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Shipments");
                    q.Request.SetParam("Object", "Loading");
                    q.Request.SetParam("Action", "GetMap");
                    q.Request.SetParams(p);

                    q.Request.Timeout = 15000;
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
                }
                else if (OrderGridSelectedItem.CheckGet("ID").ToInt() > 0)
                {
                    string nsthet = OrderGridSelectedItem.CheckGet("ID");

                    var p = new Dictionary<string, string>();
                    p.Add("ID", nsthet);

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Shipments");
                    q.Request.SetParam("Object", "Loading");
                    q.Request.SetParam("Action", "GetMapByNsthet");
                    q.Request.SetParams(p);

                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
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
                }
            }

            EnableControls();
        }

        /// <summary>
        /// Добавление файла доверенности
        /// </summary>
        public void AddProxy()
        {
            DisableControls();

            // Проверяем, что выбрана заявка
            if (OrderGridSelectedItem != null && OrderGridSelectedItem.Count > 0)
            {
                string storagePath = Central.GetStorageNetworkPathByCode(ProxyStorageCode);

                // Проверяем, что знаем путь для доверенностей
                if (!string.IsNullOrEmpty(storagePath))
                {
                    // Проверяем, что директория существует
                    if (System.IO.Directory.Exists(storagePath))
                    {
                        var fd = new Microsoft.Win32.OpenFileDialog();
                        fd.CheckFileExists = true;
                        fd.CheckPathExists = true;
                        fd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                        if ((bool)fd.ShowDialog())
                        {
                            string fileFullPath = fd.FileName;

                            // Проверяем, что пользователь выбрал файл
                            if (!string.IsNullOrEmpty(fileFullPath))
                            {
                                string fileExtension = System.IO.Path.GetExtension(fileFullPath);
                                string newFileName = $"{OrderGridSelectedItem.CheckGet("ID")}{fileExtension}";
                                string newFileFullPath = System.IO.Path.Combine(storagePath, newFileName);

                                bool rusume = false;

                                // пробуем сохранить файл доверенности в папку
                                try
                                {
                                    System.IO.File.Copy(fileFullPath, newFileFullPath, false);
                                    rusume = true;
                                }
                                catch (Exception ex)
                                {
                                    string msg = $"При сохранении файла доверенности произошла ошибка. {ex.Message}. Пожалуйста, сообщите о проблеме.";
                                    var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                                    d.ShowDialog();
                                }

                                // Если успешно сохранили, то обновляем данные в БД
                                if (rusume)
                                {
                                    if (UpdateProxyData(OrderGridSelectedItem.CheckGet("ID"), newFileName))
                                    {
                                        Refresh();

                                        string msg = "Успешное добавление доверенности";
                                        var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                                        d.ShowDialog();
                                    }
                                }
                            }
                            else
                            {
                                string msg = $"Не выбран файл доверенности.";
                                var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                                d.ShowDialog();
                            }
                        }
                    }
                    else
                    {
                        string msg = $"Папка {storagePath} для сохранения файла доверенности не найдена.";
                        var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    string msg = "Ошибка получения пути к папке для доверенностей. Пожалуйста, сообщите о проблеме.";
                    var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                string msg = $"Не выбрана заявка.";
                var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }

            EnableControls();
        }

        /// <summary>
        /// Удаление файла доверенности
        /// </summary>
        public void DeleteProxy()
        {
            DisableControls();

            // Проверяем, что выбрана заявка
            if (OrderGridSelectedItem != null && OrderGridSelectedItem.Count > 0)
            {
                // спрашиваем пользователя
                var wd = new DialogWindow($"Удалить доверенность для заявки {OrderGridSelectedItem.CheckGet("ID")} №{OrderGridSelectedItem.CheckGet("NUMBER_ORDER")} ?",
                    "Склад ответственного хранения", "", DialogWindowButtons.NoYes);
                if (wd.ShowDialog() == true) 
                {
                    string storagePath = Central.GetStorageNetworkPathByCode(ProxyStorageCode);

                    // Проверяем, что знаем путь для доверенностей
                    if (!string.IsNullOrEmpty(storagePath))
                    {
                        // Проверяем, что директория существует
                        if (System.IO.Directory.Exists(storagePath))
                        {
                            string fileFullPath = System.IO.Path.Combine(storagePath, OrderGridSelectedItem.CheckGet("PROXY"));

                            // Проверяем, что файл доверенности существует
                            if (System.IO.File.Exists(fileFullPath))
                            {
                                bool rusume = false;

                                try
                                {
                                    System.IO.File.Delete(fileFullPath);
                                    rusume = true;
                                }
                                catch (Exception ex)
                                {
                                    string msg = $"При удалении файла доверенности произошла ошибка. {ex.Message}. Пожалуйста, сообщите о проблеме.";
                                    var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                                    d.ShowDialog();
                                }

                                // Если успешно удалили, то обновляем данные в БД
                                if (rusume)
                                {
                                    if (UpdateProxyData(OrderGridSelectedItem.CheckGet("ID"), ""))
                                    {
                                        Refresh();

                                        string msg = "Успешное удаление доверенности";
                                        var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                                        d.ShowDialog();
                                    }
                                }
                            }
                            else
                            {
                                string msg = $"Файл доверенности {fileFullPath} не найден.";
                                var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                                d.ShowDialog();
                            }
                        }
                        else
                        {
                            string msg = $"Папка {storagePath} для сохранения файла доверенности не найдена.";
                            var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                    else
                    {
                        string msg = "Ошибка получения пути к папке для доверенностей. Пожалуйста, сообщите о проблеме.";
                        var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
            }
            else
            {
                string msg = $"Не выбрана заявка.";
                var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }

            EnableControls();
        }

        /// <summary>
        /// Открытие файла доверенности
        /// </summary>
        public void OpenProxy()
        {
            // Проверяем, что выбрана заявка
            if (OrderGridSelectedItem != null && OrderGridSelectedItem.Count > 0)
            {
                string storagePath = Central.GetStorageNetworkPathByCode(ProxyStorageCode);

                // Проверяем, что знаем путь для доверенностей
                if (!string.IsNullOrEmpty(storagePath))
                {
                    // Проверяем, что директория существует
                    if (System.IO.Directory.Exists(storagePath))
                    {
                        string fileFullPath = System.IO.Path.Combine(storagePath, OrderGridSelectedItem.CheckGet("PROXY"));

                        // Проверяем, что файл доверенности существует
                        if (System.IO.File.Exists(fileFullPath))
                        {
                            Central.OpenFile(fileFullPath);
                        }
                        else
                        {
                            string msg = $"Файл доверенности {fileFullPath} не найден.";
                            var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                    else
                    {
                        string msg = $"Папка {storagePath} для сохранения файла доверенности не найдена.";
                        var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    string msg = "Ошибка получения пути к папке для доверенностей. Пожалуйста, сообщите о проблеме.";
                    var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                string msg = $"Не выбрана заявка.";
                var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Обновление данных по доверенности в заявке
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="proxyFileName"></param>
        /// <returns></returns>
        public bool UpdateProxyData(string orderId, string proxyFileName)
        {
            bool resultFlag = false;

            var p = new Dictionary<string, string>();
            p.Add("ID", orderId);
            p.Add("PROXY_FILE_NAME", proxyFileName);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "ResponsibleStock");
            q.Request.SetParam("Action", "UpdateProxy");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestTimeoutDefault;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                bool succesfulFlag = false;

                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var dataSet = ListDataSet.Create(result, "ITEMS");
                    if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                    {
                        if (!string.IsNullOrEmpty(dataSet.Items.First().CheckGet("ID")))
                        {
                            succesfulFlag = true;
                        }
                    }
                }

                if (!succesfulFlag)
                {
                    string msg = "Ошибка обновления данных по доверенности в заявке. Пожалуйста, сообщите о проблеме.";
                    var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
                else
                {
                    resultFlag = true;
                }
            }
            else
            {
                q.ProcessError();
            }

            return resultFlag;
        }

        public void AddOrder()
        {
            var i = new ResponsibleStockOrder();
            i.OrderType = 1;
            i.Show();
        }

        public void EditOrder()
        {
            if (OrderGridSelectedItem != null && OrderGridSelectedItem.CheckGet("STATUS").ToInt() != 0)
            {
                var i = new ResponsibleStockOrder();
                i.OrderType = 1;
                i.OrderId = OrderGridSelectedItem.CheckGet("ID").ToInt();
                i.ReadOnlyNoteLogistician = OrderGridSelectedItem.CheckGet("TENDER").ToBool();
                i.Show();
            }
        }

        public void AddPositionOrder()
        {
            if (OrderGridSelectedItem != null)
            {
                List<string> selectedCodeList = new List<string>();
                if (PositionGrid != null && PositionGrid.Items != null && PositionGrid.Items.Count > 0)
                {
                    foreach (var item in PositionGrid.Items)
                    {
                        selectedCodeList.Add(item.CheckGet("CODE"));
                    }
                }

                var i = new ResponsibleStockOrderPosition();
                i.SelectedCodeList = selectedCodeList;
                i.Id = OrderGridSelectedItem.CheckGet("ID").ToInt();
                i.ContractId = OrderGridSelectedItem.CheckGet("ID_DOG").ToInt();
                i.AddressId = 71476;
                i.DirId = DirId;
                i.Finish = OrderGridSelectedItem.CheckGet("FINISH").ToInt();
                i.TransportId = OrderGridSelectedItem.CheckGet("ID_TS").ToInt();

                if (string.IsNullOrEmpty(OrderGridSelectedItem.CheckGet("DATA")))
                {
                    i.Data = OrderGridSelectedItem.CheckGet("DELIVERY_DATE");
                }
                else
                {
                    i.Data = OrderGridSelectedItem.CheckGet("DATA");
                }

                i.Show();
            }
        }

        public void EditPositionOrder()
        {
            if (OrderGridSelectedItem != null)
            {
                List<string> selectedCodeList = new List<string>();
                if (PositionGrid != null && PositionGrid.Items != null && PositionGrid.Items.Count > 0)
                {
                    foreach (var item in PositionGrid.Items)
                    {
                        selectedCodeList.Add(item.CheckGet("CODE"));
                    }
                }

                var i = new ResponsibleStockOrderPosition();
                i.SelectedCodeList = selectedCodeList;
                i.Id = OrderGridSelectedItem.CheckGet("ID").ToInt();
                i.ContractId = OrderGridSelectedItem.CheckGet("ID_DOG").ToInt();
                i.OrderId = PositionGridSelectedItem.CheckGet("IDORDERDATES").ToInt();
                i.AddressId = 71476;
                i.DirId = DirId;
                i.Finish = OrderGridSelectedItem.CheckGet("FINISH").ToInt();
                i.TransportId = OrderGridSelectedItem.CheckGet("ID_TS").ToInt();

                if (string.IsNullOrEmpty(OrderGridSelectedItem.CheckGet("DATA")))
                {
                    i.Data = OrderGridSelectedItem.CheckGet("DELIVERY_DATE");
                }
                else
                {
                    i.Data = OrderGridSelectedItem.CheckGet("DATA");
                }

                i.Show();
            }
        }

        public void CreateShipment()
        {
            if (OrderGridSelectedItem != null)
            {
                bool succesfulFlag = true;
                string queryResultTime = "";
                string queryResultDeliveryTime = "";

                if (succesfulFlag)
                {
                    succesfulFlag = false;

                    var p = new Dictionary<string, string>();
                    p.Add("NSTHET", OrderGridSelectedItem.CheckGet("ID"));
                    p.Add("OTGR", "1");
                    p.Add("DATE", OrderGridSelectedItem.CheckGet("DATA"));

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sales");
                    q.Request.SetParam("Object", "ResponsibleStock");
                    q.Request.SetParam("Action", "GetTimeShip");
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
                                queryResultTime = dataSet.Items.First().CheckGet("TIME_SHIP");
                                succesfulFlag = true;
                            }
                        }

                        if (!succesfulFlag)
                        {
                            string msg = "Ошибка пересчёта времени отгрузки";
                            var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }

                if (succesfulFlag)
                {
                    if (OrderGridSelectedItem.CheckGet("SELFSHIP").ToInt() == 0)
                    {
                        succesfulFlag = false;

                        var p = new Dictionary<string, string>();
                        p.Add("NSTHET", OrderGridSelectedItem.CheckGet("ID"));

                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Sales");
                        q.Request.SetParam("Object", "ResponsibleStock");
                        q.Request.SetParam("Action", "GetMaxShipTime");
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
                                    if (!string.IsNullOrEmpty(dataSet.Items.First().CheckGet("TIME")))
                                    {
                                        queryResultDeliveryTime = dataSet.Items.First().CheckGet("TIME");
                                    }
                                    else
                                    {
                                        queryResultDeliveryTime = "0";
                                    }

                                    succesfulFlag = true;
                                }
                            }

                            if (!succesfulFlag)
                            {
                                string msg = "";
                                var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                                d.ShowDialog();
                            }
                        }
                        else
                        {
                            q.ProcessError();
                        }
                    }
                }


                if (succesfulFlag)
                {
                    succesfulFlag = false;

                    var i = new ResponsibleStockShipment();
                    i.EditFlag = false;
                    i.SelfShip = OrderGridSelectedItem.CheckGet("SELFSHIP");
                    i.NoteLogistician = OrderGridSelectedItem.CheckGet("NOTE_LOGISTICIAN");

                    if (OrderGridSelectedItem.CheckGet("TYPE_ORDER").ToInt() == 3)
                    {
                        i.OrderType = 1;
                    }
                    else if (OrderGridSelectedItem.CheckGet("TYPE_ORDER").ToInt() == 1)
                    {
                        i.OrderType = 2;
                    }
                    
                    i.Otgr = 1;
                    i.OrganizationId = OrderGridSelectedItem.CheckGet("ID_PROD").ToInt();
                    i.Time = queryResultTime;
                    i.OrderId = OrderGridSelectedItem.CheckGet("ID").ToInt();

                    if ((queryResultTime.ToDateTime("dd.MM.yyyy HH:mm:ss").Hour < 8) && (queryResultDeliveryTime.ToInt() > 6))
                    {
                        i.Date = OrderGridSelectedItem.CheckGet("DATA").ToDateTime().AddDays(1).ToString();
                    }
                    else
                    {
                        i.Date = OrderGridSelectedItem.CheckGet("DATA");
                    }

                    i.Show();
                }
            }
        }

        public void EditShipment()
        {
            if (OrderGridSelectedItem != null)
            {
                var i = new ResponsibleStockShipment();
                i.EditFlag = true;
                i.TransportId = OrderGridSelectedItem.CheckGet("ID_TS").ToInt();
                i.SelfShip = OrderGridSelectedItem.CheckGet("SELFSHIP");
                i.Count = 0;
                //i.TimeShipFlag = false;
                i.NoteLogistician = OrderGridSelectedItem.CheckGet("NOTE_LOGISTICIAN");
                //i.TenderFlag = OrderGridSelectedItem.CheckGet("TENDER").ToBool();
                i.Otgr = 1;
                i.Show();
            }
        }

        public void SetDefaults()
        {
            CustomerId = 8498;
            DirId = 26;

            OrderGridSelectedItem = new Dictionary<string, string>();
            OrderGridDataSet = new ListDataSet();
            PositionGridSelectedItem = new Dictionary<string, string>();
            PositionGridDataSet = new ListDataSet();

            var typeSelectBoxItems = new Dictionary<string, string>();
            typeSelectBoxItems.Add("0", "Все типы отгрузок");
            typeSelectBoxItems.Add("1", "Поставка на СОХ");
            typeSelectBoxItems.Add("2", "Отгрузка из Липецка");
            TypeSelectBox.SetItems(typeSelectBoxItems);
            TypeSelectBox.SetSelectedItemByKey("0");

            var statusSelectBoxItems = new Dictionary<string, string>();
            statusSelectBoxItems.Add("-1", "Все статусы");
            statusSelectBoxItems.Add("0", "Согласовано");
            statusSelectBoxItems.Add("1", "Новая");
            statusSelectBoxItems.Add("2", "Отменена");
            statusSelectBoxItems.Add("4", "Отгружено");
            statusSelectBoxItems.Add("6", "На согласовании");
            statusSelectBoxItems.Add("23", "Требуется подтверждение");
            StatusSelectBox.SetItems(statusSelectBoxItems);
            StatusSelectBox.SetSelectedItemByKey("-1");
            
            if (Form != null)
            {
                Form.SetDefaults();
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
                SenderName = "ResponsibleStockList",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            OrderGrid.Destruct();
            PositionGrid.Destruct();
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.IndexOf("ResponsibleStock") > -1)
            {
                if (m.ReceiverName.IndexOf("ResponsibleStockList") > -1)
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            Refresh();
                            break;
                    }
                }
            }
        }

        public void Refresh()
        {
            //SetDefaults();

            OrderGridSelectedItem = new Dictionary<string, string>();
            OrderGridDataSet = new ListDataSet();
            PositionGridSelectedItem = new Dictionary<string, string>();
            PositionGridDataSet = new ListDataSet();

            OrderGridLoadItems();
        }

        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
            //OrderGrid.ShowSplash();
        }

        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
            //OrderGrid.HideSplash();
        }

        /// <summary>
        /// отображение справочной статьи
        /// (относительный путь)
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/sales/responsible_stock/responsible_stock_list");
        }

        /// <summary>
        /// Получаем выбранный файл с FTP сервера
        /// </summary>
        public async void GetFileFromFTP()
        {
            string fileName = "";
            bool succesfulFlag = false;

            var i = new ComplectationCMQuantity(fileName);
            i.Show("Имя файла");

            if (i.OkFlag)
            {
                fileName = i.QtyString;
            }

            var p = new Dictionary<string, string>();
            p.Add("FILE_NAME", fileName);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "ResponsibleStock");
            q.Request.SetParam("Action", "GetByFTP");
            q.Request.SetParams(p);

            q.Request.Timeout = 300000;
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

                    if (dataSet != null && dataSet.Items.Count > 0)
                    {
                        fileName = dataSet.Items.First().CheckGet("FILE_NAME");

                        if (!string.IsNullOrEmpty(fileName))
                        {
                            succesfulFlag = true;
                        }
                    }
                }

                if (!succesfulFlag)
                {
                    string msg = $"Ошибка получения файла {fileName} с FTP сервера";
                    var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
                else
                {
                    string msg = $"Успешное получение файла {fileName} с FTP сервера";
                    var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Отправляем выбранный файл на FTP сервер
        /// </summary>
        public async void SendFileToFTP()
        {
            string fileName = "";
            bool succesfulFlag = false;

            var i = new ComplectationCMQuantity(fileName);
            i.Show("Имя файла");

            if (i.OkFlag)
            {
                fileName = i.QtyString;
            }

            var p = new Dictionary<string, string>();
            p.Add("FILE_NAME", fileName);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "ResponsibleStock");
            q.Request.SetParam("Action", "SendByFTP");
            q.Request.SetParams(p);

            q.Request.Timeout = 300000;
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

                    if (dataSet != null && dataSet.Items.Count > 0)
                    {
                        fileName = dataSet.Items.First().CheckGet("FILE_NAME");

                        if (!string.IsNullOrEmpty(fileName))
                        {
                            succesfulFlag = true;
                        }
                    }
                }

                if (!succesfulFlag)
                {
                    string msg = $"Ошибка отправления файла {fileName} на FTP сервер";
                    var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
                else
                {
                    string msg = $"Успешное отправление файла {fileName} на FTP сервера";
                    var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        public async void CreateFileForFTP()
        {
            if (OrderGridSelectedItem != null)
            {
                bool succesfulFlag = false;
                int idTs = 0;

                var p = new Dictionary<string, string>();
                p.Add("ID_TS", OrderGridSelectedItem.CheckGet("ID_TS"));
                p.Add("NSTHET", OrderGridSelectedItem.CheckGet("ID"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "ResponsibleStock");
                q.Request.SetParam("Action", "CreateFileForFTP");
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

                        if (dataSet != null && dataSet.Items.Count > 0)
                        {
                            idTs = dataSet.Items.First().CheckGet("ID_TS").ToInt();

                            if (idTs > 0)
                            {
                                succesfulFlag = true;
                            }
                        }
                    }

                    if (!succesfulFlag)
                    {
                        string msg = $"Ошибка создания файла отгрузки №{idTs} для выгрузки на FTP сервер";
                        var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                    else
                    {
                        string msg = $"Успешное создание файла отгрузки №{idTs} для выгрузки на FTP сервер";
                        var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// Создание Excel файла по данным в гриде заявок на отгрузку
        /// </summary>
        public async void ExportToExcel()
        {
            if (OrderGrid.Items != null)
            {
                if (OrderGrid.Items.Count > 0)
                {
                    var eg = new ExcelGrid();
                    var cols = OrderGrid.Columns;
                    eg.SetColumnsFromGrid(cols);
                    eg.Items = OrderGrid.Items;
                    await Task.Run(() =>
                    {
                        eg.Make();
                    });
                }
            }
        }

        /// <summary>
        /// Создание Excel файла по данным в гриде позиций выбранной заявки на отгрузку
        /// </summary>
        public async void ExportToPositionExcel()
        {
            if (PositionGrid != null && PositionGrid.Items != null && PositionGrid.Items.Count > 0)
            {
                if (OrderGridSelectedItem != null && OrderGridSelectedItem.Count > 0)
                {
                    var eg = new ExcelGrid();
                    var cols = PositionGrid.Columns;
                    eg.SetColumnsFromGrid(cols);
                    eg.Items = PositionGrid.Items;
                    eg.GridTitle = $"Данные по позициям заявки на отгрузку СОХ. Ид отгрузки: {OrderGridSelectedItem.CheckGet("ID")}. Дата отгрузки: {OrderGridSelectedItem.CheckGet("SHIPPING_DATE")}.";
                    await Task.Run(() =>
                    {
                        eg.Make();
                    });
                }
                else
                {
                    var msg = "Не выбрана заявка на отгрузку.";
                    var d = new DialogWindow($"{msg}", "Поставка на СОХ", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                var msg = "Нет данных для выгрузки в Excel.";
                var d = new DialogWindow($"{msg}", "Поставка на СОХ", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void ResreshButton_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddOrder();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            EditOrder();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteOrder();
        }

        private void AddPositionButton_Click(object sender, RoutedEventArgs e)
        {
            AddPositionOrder();
        }

        private void EditPositionButton_Click(object sender, RoutedEventArgs e)
        {
            EditPositionOrder();
        }

        private void DeletePositionButton_Click(object sender, RoutedEventArgs e)
        {
            DeletePosition();
        }

        private void SendFileButton_Click(object sender, RoutedEventArgs e)
        {
            SendFileToFTP();
        }

        private void GetFileButton_Click(object sender, RoutedEventArgs e)
        {
            GetFileFromFTP();
        }

        private void CreateFileButton_Click(object sender, RoutedEventArgs e)
        {
            CreateFileForFTP();
        }

        private void ExcelButton_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcel();
        }

        private void ExcelPositionButton_Click(object sender, RoutedEventArgs e)
        {
            ExportToPositionExcel();
        }

        private void StatusSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            OrderGrid.UpdateItems();
        }

        private void TypeSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            OrderGrid.UpdateItems();
        }

        private void HideCompletedCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            OrderGrid.UpdateItems();
        }

        private void HideCompletedCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            OrderGrid.UpdateItems();
        }
    }
}
