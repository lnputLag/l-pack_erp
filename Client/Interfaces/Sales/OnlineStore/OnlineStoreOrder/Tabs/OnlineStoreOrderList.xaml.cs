using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Production;
using Client.Interfaces.Service.Printing;
using CodeReason.Reports;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
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
using System.Windows.Xps.Packaging;
using static Client.Interfaces.Main.DataGridHelperColumn;
using Task = System.Threading.Tasks.Task;

namespace Client.Interfaces.Sales
{
    /// <summary>
    /// Список заказов интернет-магазина
    /// </summary>
    /// <author>sviridov_ae</author>
    public partial class OnlineStoreOrderList : ControlBase
    {
        public OnlineStoreOrderList()
        {
            ControlTitle = "Заказы интернет-магазина";
            OnMessage = (ItemMessage message) => {
                DebugLog($"message=[{message.Message}]");
                if (message.ReceiverName == ControlName)
                {
                    ProcessCommand(message.Action);
                }
            };

            //конструктор, будет вызван, когда объект создается
            //здесь создаются все внутренние структуры
            //впервые этот коллбэк будет вызван, когда данный таб станет активным
            //впервые (до этих пор, никакая работа внутри не происходит, что экономит ресурсы)
            OnLoad = () =>
            {
                InitializeComponent();

                //регистрация обработчика сообщений
                Messenger.Default.Register<ItemMessage>(this, ProcessMessages);
                
                OrderGridInit();
                SetDefaults();
                PositionGridInit();
                ProcessPermissions();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                OrderGrid.Destruct();
                PositionGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                OrderGrid.ItemsAutoUpdate = true;
                OrderGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                OrderGrid.ItemsAutoUpdate = false;
            };
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Основной датасет с данными по заявкам
        /// </summary>
        public ListDataSet OrderGridDataSet { get; set; }

        /// <summary>
        /// Выбранная запись в гриде
        /// </summary>
        public Dictionary<string, string> OrderGridSelectedItem { get; set; }

        /// <summary>
        /// Основной датасет с данными по позициям заявки
        /// </summary>
        public ListDataSet PositionGridDataSet { get; set; }

        /// <summary>
        /// Выбранная запись в гриде
        /// </summary>
        public Dictionary<string, string> PositionGridSelectedItem { get; set; }

        /// <summary>
        /// Статусы отгрузки
        /// </summary>
        public enum ShipmentStatusRef
        {
            //Заявка отправлена
            ApplicationSend = 1,
            //Заявка подтверждена
            ApplicationConfirmed = 2,
            //Документы отправлены
            DocumentsSend = 3,
            //Отгрузка невыполнена
            ShipmentFailed = 4,
            //Отгрузка выполнена
            ShipmentCompleted = 5,
            //Нет заявки
            NonShipment = 0,
        }

        public ShipmentStatusRef ShipmentStatus  { get;set;}

        public void SetDefaults()
        {
            OrderGridDataSet = new ListDataSet();
            OrderGridSelectedItem = new Dictionary<string, string>();
            PositionGridDataSet = new ListDataSet();
            PositionGridSelectedItem = new Dictionary<string, string>();

            var statusSelectBoxItems = new Dictionary<string, string>();
            statusSelectBoxItems.Add("0", "Все статусы");
            statusSelectBoxItems.Add("1", "В работе");
            statusSelectBoxItems.Add("2", "Не корректные");
            statusSelectBoxItems.Add("3", "Новый");
            statusSelectBoxItems.Add("4", "Сформирован счёт");
            statusSelectBoxItems.Add("5", "Оплачен");
            statusSelectBoxItems.Add("6", "Заявка отправлена");
            statusSelectBoxItems.Add("7", "Заявка принята");
            statusSelectBoxItems.Add("8", "Документы отправлены");
            statusSelectBoxItems.Add("9", "Выполнен");
            statusSelectBoxItems.Add("10", "Проведён");
            statusSelectBoxItems.Add("11", "Заявка отменена");
            statusSelectBoxItems.Add("12", "Отменён");
            StatusSelectBox.SetItems(statusSelectBoxItems);
            StatusSelectBox.SetSelectedItemByKey("0");

            var shippingPointSelectBoxItems = new Dictionary<string, string>();
            shippingPointSelectBoxItems.Add("0", "Все склады");
            shippingPointSelectBoxItems.Add("1", "Москва");
            shippingPointSelectBoxItems.Add("2", "Липецк");
            ShippingPointSelectBox.SetItems(shippingPointSelectBoxItems);
            ShippingPointSelectBox.SetSelectedItemByKey("0");

            if (Form != null)
            {
                Form.SetDefaults();
            }
        }

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
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Покупатель",
                        Path="BUYER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=30,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер заказа",
                        Path="ORDER_NUM",
                        ColumnType=ColumnTypeRef.String,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус",
                        Path="STATUS",
                        ColumnType=ColumnTypeRef.String,
                        Width2=15,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                        //Не выполнился
                                        if( row.CheckGet("SHIPMENT_STATUS").ToInt() == 4 )
                                        {
                                            color = HColor.Red;
                                        }
                                        // Заявка принята
                                        else if( row.CheckGet("SHIPMENT_STATUS").ToInt() == 2 )
                                        {
                                            color = HColor.Green;
                                        }
                                        // Заявка отправлена
                                        else if( row.CheckGet("SHIPMENT_STATUS").ToInt() == 1 )
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
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата заказа",
                        Path="ORDER_DT",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy",
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Стоимость заказа",
                        Path="SUM_PRICE_VAT_DELIVERY",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width2=9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Платёжное поручение",
                        Path="OPERATION_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=9,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                        // По данному покупателю есть не обработанные платёжные документы и к этому заказу не привязано ни одно платёжное поручение
                                        if( row.CheckGet("NEW_OPERATION_FLAG").ToInt() == 1 )
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
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Оплачено",
                        Path="OPERATION_SUMM",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width2=9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Точка отгрузки",
                        Path="SHIPPING_POINT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Самовывоз",
                        Path="PICKUP_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Адрес доставки",
                        Path="DELIVERY_ADDRESS",
                        ColumnType=ColumnTypeRef.String,
                        Width2=53,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата отгрузки",
                        Path="DELIVERY_DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy",
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Доставка",
                        Doc="Стоимость доставки",
                        Path="DELIVERY_PRICE",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вес",
                        Doc="Вес брутто",
                        Path="WEIGHT_GROSS",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Площадь",
                        Doc="Площадь, м2",
                        Path="SQUARE",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N4",
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Поддоны",
                        Doc="Количество поддонов в заказе",
                        Path="QUANTITY_PALLET",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Счёт",
                        Path="RECEIPT_NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="УПД",
                        Path="NAME_SF",
                        ColumnType=ColumnTypeRef.String,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Заявка на производство",
                        Path="ORDER_NSTHET",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Накладная расхода",
                        Path="NSTHET",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Договор",
                        Path="CONTRACT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Продавец",
                        Path="SELLER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата создания",
                        Path="CREATED_DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy",
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Тип покупателя",
                        Path="CUSTOMER_TYPE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Отрасль",
                        Description="Отрасль деятельности покупателя",
                        Path="CUSTOMER_INDUSTRY",
                        ColumnType=ColumnTypeRef.String,
                        Width2=14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Регион",
                        Description="Регион покупателя",
                        Path="CUSTOMER_REGION",
                        ColumnType=ColumnTypeRef.String,
                        Width2=13,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид статуса",
                        Doc="Для сортировки",
                        Path="STATUS_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Точка отгрузки",
                        Doc="(oso.shipping_point)",
                        Path="SHIPPING_POINT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД покупателя",
                        Doc="(pok.id_pok)",
                        Path="CUSTOMER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Тип покупателя",
                        Doc="(pok.type_customer)",
                        Path="CUSTOMER_TYPE",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                        Hidden=true,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Ид накладной по заявке",
                        Doc="(Идентификатор накладной расхода по заявке на производство (для отгрузок ИМ ил Липецка))",
                        Path="ORDER_BASE_NSTHET",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата печати накладной по заявке",
                        Doc="(ата печати накладной расхода по заявке на производство (для отгрузок ИМ ил Липецка))",
                        Path="ORDER_BASE_PRINT_DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2=7,
                        Hidden=true,
                    },
                };
                OrderGrid.SetColumns(columns);
                OrderGrid.SearchText = SearchText;
                OrderGrid.SetSorting("STATUS_ID", System.ComponentModel.ListSortDirection.Ascending);
                OrderGrid.OnLoadItems = OrderGridLoadItems;
                OrderGrid.PrimaryKey = "ID";
                OrderGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

                OrderGrid.OnFilterItems = () =>
                {
                    if (OrderGrid.GridItems != null)
                    {
                        if (OrderGrid.GridItems.Count > 0)
                        {
                            if (StatusSelectBox.SelectedItem.Key != null)
                            {
                                var key = StatusSelectBox.SelectedItem.Key.ToInt();
                                var items = new List<Dictionary<string, string>>();

                                switch (key)
                                {
                                    // Все статусы
                                    case 0:
                                        items = OrderGrid.GridItems;
                                        break;

                                    // В работе
                                    case 1:
                                        items.AddRange(OrderGrid.GridItems.Where(x => x.CheckGet("COMPLITED_FLAG").ToInt() != 1 && x.CheckGet("CANCELED_FLAG").ToInt() != 1 && x.CheckGet("SHIPMENT_STATUS").ToInt() != 4));
                                        break;

                                    // Не корректные
                                    case 2:
                                        items.AddRange(OrderGrid.GridItems.Where(x => x.CheckGet("SHIPMENT_STATUS").ToInt() == 4 || x.CheckGet("CANCELED_FLAG").ToInt() == 1));
                                        break;

                                    // Новый
                                    case 3:
                                        items.AddRange(OrderGrid.GridItems.Where(x => x.CheckGet("STATUS") == "Новый"));
                                        break;

                                    // Сформирован счёт
                                    case 4:
                                        items.AddRange(OrderGrid.GridItems.Where(x => x.CheckGet("STATUS") == "Сформирован счёт"));
                                        break;

                                    // Оплачен
                                    case 5:
                                        items.AddRange(OrderGrid.GridItems.Where(x => x.CheckGet("STATUS") == "Оплачен"));
                                        break;

                                    // Заявка отправлена
                                    case 6:
                                        items.AddRange(OrderGrid.GridItems.Where(x => x.CheckGet("SHIPMENT_STATUS").ToInt() == 1));
                                        break;

                                    // Заявка принята
                                    case 7:
                                        items.AddRange(OrderGrid.GridItems.Where(x => x.CheckGet("SHIPMENT_STATUS").ToInt() == 2));
                                        break;

                                    // Документы отправлены
                                    case 8:
                                        items.AddRange(OrderGrid.GridItems.Where(x => x.CheckGet("SHIPMENT_STATUS").ToInt() == 3));
                                        break;

                                    // Выполнен
                                    case 9:
                                        items.AddRange(OrderGrid.GridItems.Where(x => x.CheckGet("SHIPMENT_STATUS").ToInt() == 5 && x.CheckGet("COMPLITED_FLAG").ToInt() != 1));
                                        break;

                                    // Проведён
                                    case 10:
                                        items.AddRange(OrderGrid.GridItems.Where(x => x.CheckGet("COMPLITED_FLAG").ToInt() == 1));
                                        break;

                                    // Заявка отменена
                                    case 11:
                                        items.AddRange(OrderGrid.GridItems.Where(x => x.CheckGet("SHIPMENT_STATUS").ToInt() == 4));
                                        break;

                                    // Отменён
                                    case 12:
                                        items.AddRange(OrderGrid.GridItems.Where(x => x.CheckGet("CANCELED_FLAG").ToInt() == 1));
                                        break;

                                    default:
                                        items = OrderGrid.GridItems;
                                        break;
                                }

                                OrderGrid.GridItems = items;
                            }

                            if (ShippingPointSelectBox.SelectedItem.Key != null)
                            {
                                int key = ShippingPointSelectBox.SelectedItem.Key.ToInt();
                                var items = new List<Dictionary<string, string>>();

                                switch (key)
                                {
                                    // Все точки отгрузки
                                    case 0:
                                        items = OrderGrid.GridItems;
                                        break;

                                    // Москва
                                    case 1:
                                        items.AddRange(OrderGrid.GridItems.Where(x => x.CheckGet("SHIPPING_POINT").ToInt() == 1));
                                        break;

                                    // Липецк
                                    case 2:
                                        items.AddRange(OrderGrid.GridItems.Where(x => x.CheckGet("SHIPPING_POINT").ToInt() == 2));
                                        break;

                                    default:
                                        items = OrderGrid.GridItems;
                                        break;
                                }

                                OrderGrid.GridItems = items;
                            }
                        }
                    }
                };

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
                            
                            //Отменен
                            if(row.CheckGet("CANCELED_FLAG").ToInt() == 1)
                            {
                                color = HColor.Red;
                            }
                            //Проведён
                            else if (row.CheckGet("COMPLITED_FLAG").ToInt() == 1)
                            {
                                color = HColor.Green;
                            }
                            //Оплачен
                            else if (row.CheckGet("PAID_FLAG").ToInt() == 1)
                            {
                                color = HColor.White;
                            }
                            //Сформирован счёт
                            else if (!string.IsNullOrEmpty(row.CheckGet("RECEIPT_NUMBER")))
                            {
                                color = HColor.Yellow;
                            }
                            //Новый
                            else if (row.CheckGet("STATUS") == "Новый")
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

                // контекстное меню
                OrderGrid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    {
                        "EditOnlineStoreOrder",
                        new DataGridContextMenuItem()
                        {
                            Header="Изменить",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                //EditOnlineStoreOrder();
                            }
                        }
                    },
                    {
                        "DeleteOnlineStoreOrder",
                        new DataGridContextMenuItem()
                        {
                            Header="Удалить",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                //DeleteOnlineStoreOrder();
                            }
                        }
                    },
                    { "s0", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    {
                        "CreateReceiptFile",
                        new DataGridContextMenuItem()
                        {
                            Header="Сформировать счёт",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                CreateReceiptFile();
                            }
                        }
                    },
                    {
                        "BindCashReceipt",
                        new DataGridContextMenuItem()
                        {
                            Header="Привязать платёж",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                BindCashReceipt();
                            }
                        }
                    },
                    {
                        "UpdatePaidFlag",
                        new DataGridContextMenuItem()
                        {
                            Header="Отметить оплату счёта",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                UpdatePaidFlag(1);
                            }
                        }
                    },
                    {
                        "UpdateNoPaidFlag",
                        new DataGridContextMenuItem()
                        {
                            Header="Отменить отметку оплаты счёта",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                UpdatePaidFlag(0);
                            }
                        }
                    },
                    {
                        "ReprintReceiptFile",
                        new DataGridContextMenuItem()
                        {
                            Header="Повторная печать счёта",
                            Action=()=>
                            {
                                ReprintReceiptFile();
                            }
                        }
                    },
                    { "s1", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    {
                        "SendApplicationForShipment",
                        new DataGridContextMenuItem()
                        {
                            Header="Отправить заявку",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                SendApplicationForShipment();
                            }
                        }
                    },
                    {
                        "EditShipmentDate",
                        new DataGridContextMenuItem()
                        {
                            Header="Изменить дату отгрузки",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                EditShipmentDate();
                            }
                        }
                    },
                    {
                        "ReprintMovementInvoiceExcelFile",
                        new DataGridContextMenuItem()
                        {
                            Header="Повторная печать ТН",
                            Action=()=>
                            {
                                ReprintMovementInvoiceExcelFile();
                            }
                        }
                    },
                    { "s2", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    {
                        "CreateUPDFile",
                        new DataGridContextMenuItem()
                        {
                            Header="Сформировать отгрузочные документы",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                CreateUPDFile();
                            }
                        }
                    },
                    {
                        "ReprintUPDFile",
                        new DataGridContextMenuItem()
                        {
                            Header="Повторная печать отгрузочных документов",
                            Action=()=>
                            {
                                ReprintUPDFile();
                            }
                        }
                    },
                    { "s3", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    {
                        "ConfirmConsumption",
                        new DataGridContextMenuItem()
                        {
                            Header="Провести",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                ConfirmConsumption();
                            }
                        }
                    },
                    { "s4", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    {
                        "CancelApplication",
                        new DataGridContextMenuItem()
                        {
                            Header="Отменить заявку",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                CancelApplication();
                            }
                        }
                    },
                    {
                        "BackToWork",
                        new DataGridContextMenuItem()
                        {
                            Header="Вернуть в работу",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                BackToWork();
                            }
                        }
                    },
                    {
                        "CancelOnlineStoreOrder",
                        new DataGridContextMenuItem()
                        {
                            Header="Отменить заказ",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                CancelOnlineStoreOrder();
                            }
                        }
                    },
                    { "spacer5", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    {
                        "GotoBitrix",
                        new DataGridContextMenuItem()
                        {
                            Header="Битрикс",
                            Action=()=>
                            {
                                GotoBitrix();
                            }
                        }
                    },

                    { "s5", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    {
                        "CreateOrder",
                        new DataGridContextMenuItem()
                        {
                            Header="Создать заявку на производство",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                CreateOrder();
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
                        PositionGridLoadItems();
                        SetActionEnabled();
                    }
                };

                OrderGrid.Init();
            }
        }

        public async void OrderGridLoadItems()
        {
            DisableControls(false);

            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "OnlineStoreOrder");
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
        /// инициализация грида позиций по выбранной заявке
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
                        Path="ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="PRODUCT_FULL_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=54,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Габариты",
                        Path="DIMENSIONS",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вес",
                        Doc="Вес брутто",
                        Path="WEIGHT_GROSS",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Площадь",
                        Doc="Площадь, м2",
                        Path="SQUARE",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N4",
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул",
                        Path="CODE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=15,
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
                        Path="QUNATITY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Забронированно",
                        Path="RESERVED_QUNATITY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Поддонов",
                        Path="RESERVED_INFO",
                        ColumnType=ColumnTypeRef.String,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Без НДС",
                        Doc="Цена без НДС",
                        Path="PRICE",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width2=9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Без НДС *",
                        Doc="Цена без НДС с доставкой",
                        Path="PRICE_DELIVERY",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="С НДС",
                        Doc="Цена с НДС",
                        Path="PRICE_VAT",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="С НДС *",
                        Doc="Цена с НДС с доставкой",
                        Path="PRICE_VAT_DELIVERY",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Заказ",
                        Path="ONSR_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид товара",
                        Path="PRODUCT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Внутреннее наименование",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=39,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Позиция полностью забронированна",
                        Path="RESERVED_FLAG",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden = true,
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
                };
                PositionGrid.SetColumns(columns);
                PositionGrid.AutoUpdateInterval = 0;
                PositionGrid.PrimaryKey = "ID";
                PositionGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

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
                            
                            // Для позиции зарезервированны все необходимые поддоны
                            if(row.CheckGet("RESERVED_FLAG").ToInt() == 1)
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

                // контекстное меню
                PositionGrid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    {
                        "EditPosition",
                        new DataGridContextMenuItem()
                        {
                            Header="Изменить позицию заказа",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                EditOrderPosition();
                            }
                        }
                    },
                    {
                        "DeletePosition",
                        new DataGridContextMenuItem()
                        {
                            Header="Удалить позицию заказа",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                DeleteOrderPosition();
                            }
                        }
                    },
                    { "s0", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    {
                        "Reserve",
                        new DataGridContextMenuItem()
                        {
                            Header="Забронировать поддоны для позиции заказа",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                Reserve();
                            }
                        }
                    },
                    {
                        "Unreserve",
                        new DataGridContextMenuItem()
                        {
                            Header="Снять бронь с поддонов для позиции заказа",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                Unreserve();
                            }
                        }
                    },
                };

                //при выборе строки в гриде, обновляются актуальные действия для записи
                PositionGrid.OnSelectItem = selectedItem =>
                {
                    PositionGridSelectedItem = selectedItem;
                    SetActionEnabled();
                };

                PositionGrid.Init();
            }
        }

        public async void PositionGridLoadItems()
        {
            DisableControls(false);

            var p = new Dictionary<string, string>();
            p.Add("ID", OrderGridSelectedItem.CheckGet("ID"));

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "OnlineStoreOrder");
            q.Request.SetParam("Action", "ListOrderPosition");
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

                    if (PositionGridDataSet != null && PositionGridDataSet.Items != null && PositionGridDataSet.Items.Count > 0)
                    {
                        foreach (var item in PositionGridDataSet.Items)
                        {
                            if (item.CheckGet("RESERVED_QUNATITY").ToInt() == item.CheckGet("QUNATITY").ToInt())
                            {
                                item.CheckAdd("RESERVED_FLAG", "1");
                            }
                            else
                            {
                                item.CheckAdd("RESERVED_FLAG", "0");
                            }
                        }
                    }

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

        /// <summary>
        /// Создание новой заявки интернет магазина
        /// </summary>
        public void AddOrder()
        {
            var i = new OnlineStoreOrder();
            i.Show();
        }

        public void GotoBitrix()
        {
            var id=OrderGrid.SelectedItem.CheckGet("CUSTOMER_ID").ToInt();
            if(id != 0)
            {
                var url=$"https://bitrix.l-pak.ru/l-pack/pub/service4.php?action=redirect&target=company_deals&id={id}";
                Central.OpenFile(url);
            }
        }

        /// <summary>
        /// Отмена заказа интернет-магазина
        /// </summary>
        public void CancelOnlineStoreOrder()
        {
            var message = $"Отменить выбранный заказ интернет-магазина?";
            if (DialogWindow.ShowDialog(message, "Заказы интернет-магазина", "", DialogWindowButtons.NoYes) != true)
            {
                return;
            }
            else
            {
                // 1 -- Проверяем, что нет заявки на отгрузку
                // 2 -- Проверяем, что счёт не оплачен
                // 3 -- Снимаем бронь с поддонов
                // 4 -- Обновляем поле CANCELED_FLAG в таблице online_store_order

                bool succesfulFlag = true;

                // 1 -- Проверяем, что нет заявки на отгрузку
                if (succesfulFlag)
                {
                    if (OrderGridSelectedItem.CheckGet("SHIPMENT_STATUS").ToInt() == 0 || OrderGridSelectedItem.CheckGet("SHIPMENT_STATUS").ToInt() == 4)
                    {
                        succesfulFlag = true;
                    }
                    else
                    {
                        succesfulFlag = false;

                        string msg = $"Нельзя отменить заказ, для которого отправлена заявка";
                        var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }

                // 2 -- Проверяем, что счёт не оплачен
                if (succesfulFlag)
                {
                    // Если есть привязанное платёжное поручение, то запрещаем отменять заказ
                    if (OrderGridSelectedItem.CheckGet("OPERATION_SUMM").ToInt() > 0)
                    {
                        succesfulFlag = false;

                        string msg = $"Нельзя отменить заказ, который уже оплачен";
                        var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                    else
                    {
                        succesfulFlag = true;
                    }
                }

                // 3 -- Снимаем бронь с поддонов
                if (succesfulFlag)
                {
                    succesfulFlag = UnreserveAll(false);
                }

                // 4 -- Обновляем поле CANCELED_FLAG в таблице online_store_order
                if (succesfulFlag)
                {
                    succesfulFlag = UpdateCanceledFlag(1);
                }

                if (succesfulFlag)
                {
                    Refresh();
                }
            }
        }

        /// <summary>
        /// Возвращаем заказ интернте-магазина в работу после отмены заявки.
        /// </summary>
        public void BackToWork()
        {
            string msg = $"Внимание. Вы собираетесь повторно сформировать заявку по заказу. Пожалуйста, убедитесь что СОХ уведомлён об этом.";
            var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
            d.ShowDialog();

            SendApplicationForShipment();
        }

        /// <summary>
        /// Анулирование заявки на отгрузку
        /// </summary>
        public void CancelApplication()
        {
            bool succesfulFlag = true;
            string comment = "";
            int orderId = 0;

            // Получаем причину отмены заявки
            if (succesfulFlag)
            {
                succesfulFlag = false;

                var i = new ComplectationCMQuantity("", true);
                i.Show("Причина отмены заявки");
                if (i.OkFlag)
                {
                    comment = i.QtyString;

                    if (!string.IsNullOrEmpty(comment))
                    {
                        succesfulFlag = true;
                    }
                    else
                    {
                        string msg = $"Пожалуйста, укажите причину отмены заявки";
                        var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
            }

            // Формируем csv файл анулирования заявки
            if (succesfulFlag)
            {
                CreateDeclineOutFileForFTP(comment, out orderId);
            }

            // Отправляем csv файл анулирования заявки по ftp
            if (succesfulFlag)
            {
                SendDeclineOutFileForFTP(orderId);
            }

            // Обновляем статус заказа
            if (succesfulFlag)
            {
                succesfulFlag = UpdateShipmentStatus((int)ShipmentStatusRef.ShipmentFailed);
            }

            if (succesfulFlag)
            {
                Refresh();
            }

        }

        public bool UpdateCanceledFlag(int canceledFlag)
        {
            bool succesfulFlag = false;

            var p = new Dictionary<string, string>();
            p.Add("ORDER_ID", OrderGridSelectedItem.CheckGet("ID"));
            p.Add("CANCELED_FLAG", canceledFlag.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "OnlineStoreOrder");
            q.Request.SetParam("Action", "UpdateCanceledFlag");
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
                        int orderId = dataSet.Items.First().CheckGet("ORDER_ID").ToInt();

                        if (orderId > 0)
                        {
                            succesfulFlag = true;
                        }
                    }
                }

                if (succesfulFlag)
                {
                    string msg = $"Успешная отмена заказа интернет-магазина.";
                    var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
                else
                {
                    string msg = $"Ошибка отмены заказа интернет-магазина.";
                    var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                q.ProcessError();
            }

            return succesfulFlag;
        }

        /// <summary>
        /// Создаём csv файл анултирования заявки на отгрузку для отправки на ftp
        /// </summary>
        public bool CreateDeclineOutFileForFTP(string comment, out int _orderId)
        {
            bool succesfulFlag = false;
            int orderId = 0;

            var p = new Dictionary<string, string>();
            p.Add("ORDER_ID", OrderGridSelectedItem.CheckGet("ID"));
            p.Add("COMMENT", comment);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "OnlineStoreOrder");
            q.Request.SetParam("Action", "CreateDeclineOutFileForFTP");
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
                        orderId = dataSet.Items.First().CheckGet("ORDER_ID").ToInt();
                        if (orderId > 0)
                        {
                            succesfulFlag = true;
                        }
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            _orderId = orderId;
            return succesfulFlag;
        }

        /// <summary>
        /// Отправляем csv файл отмены заявки на отгрузку на FTP сервер
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public async void SendDeclineOutFileForFTP(int orderId)
        {
            bool succesfulFlag = false;

            string fileName = $"{orderId}_DECLINEOUT.csv";

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
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Сформировать счёт на оплату
        /// </summary>
        public async void CreateReceiptFile()
        {
            DisableControls(true, "Формирование счёта на оплату");

            if (OrderGridSelectedItem != null && OrderGridSelectedItem.Count > 0)
            {
                var p = new Dictionary<string, string>();
                p.Add("ID", OrderGridSelectedItem.CheckGet("ID"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "OnlineStoreOrder");
                q.Request.SetParam("Action", "CreateReceiptFile");
                q.Request.SetParams(p);
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    if (Central.DebugMode)
                    {
                        Central.OpenFile(q.Answer.DownloadFilePath);
                    }
                    else
                    {
                        ConvertDocToPdf(q.Answer.DownloadFilePath);
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            EnableControls();

            CreateAndSendCustomerDataToFTP();
            Refresh();
        }

        /// <summary>
        /// Повторная печать счёта на оплату
        /// </summary>
        public async void ReprintReceiptFile()
        {
            DisableControls(true, "Формирование счёта на оплату");

            if (OrderGridSelectedItem != null && OrderGridSelectedItem.Count > 0)
            {
                var p = new Dictionary<string, string>();
                p.Add("ID", OrderGridSelectedItem.CheckGet("ID"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "OnlineStoreOrder");
                q.Request.SetParam("Action", "ReprintReceiptFile");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    if (Central.DebugMode)
                    {
                        Central.OpenFile(q.Answer.DownloadFilePath);
                    }
                    else
                    {
                        ConvertDocToPdf(q.Answer.DownloadFilePath);
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
        /// Преобразует выбранный doc документ в pdf и сохраняем в выбранном месте
        /// </summary>
        public void ConvertDocToPdf(string docFilePath)
        {
            // Полный путь и название для нового файла
            string newFileFullName = "";

            // Получаем данные по текущему временному файлу, полученному в ответе от сервера
            var fileInfo = new FileInfo(docFilePath);
            string name = fileInfo.Name;
            string ext = fileInfo.Extension;

            // Заменяем в наименовании файла расширение на ПДФ
            int index = name.IndexOf(ext);
            name = name.Substring(0, index);
            name += ".pdf";

            // Получаем место сохранения нового файла
            var fd = new SaveFileDialog();
            fd.FileName = $"{name}";
            var fdResult = fd.ShowDialog();
            if (fdResult == true)
            {
                if (!string.IsNullOrEmpty(fd.FileName))
                {
                    newFileFullName = fd.FileName;
                }
            }

            // Преобразовываем файл в ПДФ и сохраняем по выбранному пути
            if (!string.IsNullOrEmpty(newFileFullName))
            {
                // Create COM Objects
                Microsoft.Office.Interop.Word.Application application;
                Microsoft.Office.Interop.Word.Document document;

                // Create new instance of Word
                application = new Microsoft.Office.Interop.Word.Application();

                // Make the process invisible to the user
                application.ScreenUpdating = false;

                // Make the process silent
                application.DisplayAlerts = 0;

                // Open the document that you wish to export to PDF
                document = application.Documents.Open(docFilePath);

                // If the document failed to open, stop, clean up, and bail out
                if (document == null)
                {
                    application.Quit();

                    application = null;
                    document = null;

                    string msg = $"Ошибка открытия документа";
                    var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
                else
                {
                    var exportSuccessful = true;

                    try
                    {
                        // Call Word's native export function (valid in Office 2007 and Office 2010, AFAIK)
                        document.ExportAsFixedFormat(newFileFullName, Microsoft.Office.Interop.Word.WdExportFormat.wdExportFormatPDF);
                    }
                    catch (System.Exception ex)
                    {
                        // Mark the export as failed for the return value...
                        exportSuccessful = false;

                        string msg = $"Не удалось преобразовать файл в PDF. {ex.Message}";
                        var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                    finally
                    {
                        // Close the document, quit the Word, and clean up regardless of the results...
                        document.Close();
                        application.Quit();

                        application = null;
                        document = null;

                        if (System.IO.File.Exists(newFileFullName))
                        {
                            string msg = "PDF файл успешно создан";
                            var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                        else
                        {
                            string msg = $"Не удалось преобразовать файл в PDF.";
                            var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Генерируем csv файл с данные по контрагенту и отправляет на сох по фтп
        /// </summary>
        public async void CreateAndSendCustomerDataToFTP()
        {
            // Генерируем csv файл с данными по контрагенту
            int customerId = 0;
            bool succesfulFlag = false;

            var p = new Dictionary<string, string>();
            p.Add("ORDER_ID", OrderGridSelectedItem.CheckGet("ID"));

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "OnlineStoreOrder");
            q.Request.SetParam("Action", "CreateCustomerFileForFTP");
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
                        customerId = dataSet.Items.First().CheckGet("CUSTOMER_ID").ToInt();
                        if (customerId > 0)
                        {
                            succesfulFlag = true;
                        }
                    }
                }

                if (!succesfulFlag)
                {
                    string msg = $"Ошибка создания файла контрагента №{customerId} для выгрузки на FTP сервер. Пожалуйста, сообщите об ошибке.";
                    var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
                else
                {
                    // Отправляем csv файл по выбранному контрагенту на FTP
                    SendCustomerDataToFTP(customerId);
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Отправляем csv файл по выбранному контрагенту на FTP
        /// </summary>
        /// <param name="customerId"></param>
        public async void SendCustomerDataToFTP(int customerId)
        {
            bool succesfulFlag = false;
            string fileName = $"{customerId}_COMPANY.csv";

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
                    string msg = $"Ошибка отправления файла контрагента {fileName} на FTP сервер. Пожалуйста, сообщите об ошибке.";
                    var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
                else
                {
                    string msg = $"Успешное отправление файла контрагента {fileName} на FTP сервера";
                    var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Создание позиции заказа
        /// </summary>
        public void AddOrderPosition()
        {
            if (OrderGrid != null && OrderGrid.GridItems != null && OrderGrid.GridItems.Count > 0)
            {
                if (OrderGridSelectedItem != null && OrderGridSelectedItem.Count > 0)
                {
                    var i = new OnlineStoreOrderPosition();
                    i.OrderId = OrderGridSelectedItem.CheckGet("ID").ToInt();

                    // Данные по уже добавленным позициям
                    List<string> selectedCodeList = new List<string>();
                    if (PositionGrid != null && PositionGrid.GridItems != null && PositionGrid.GridItems.Count > 0)
                    {
                        foreach (var item in PositionGrid.GridItems)
                        {
                            selectedCodeList.Add(item.CheckGet("CODE"));
                        }
                    }
                    
                    i.SelectedCodeList = selectedCodeList;

                    i.Show();
                }
            }
        }

        /// <summary>
        /// Редактирование позиции заказа
        /// </summary>
        public void EditOrderPosition()
        {
            if (PositionGrid != null && PositionGrid.GridItems != null && PositionGrid.GridItems.Count > 0)
            {
                if (
                    PositionGridSelectedItem != null && PositionGridSelectedItem.Count > 0
                    && OrderGridSelectedItem != null && OrderGridSelectedItem.Count > 0
                    )
                {
                    var i = new OnlineStoreOrderPosition();
                    i.OrderId = OrderGridSelectedItem.CheckGet("ID").ToInt();
                    i.PositionId = PositionGridSelectedItem.CheckGet("ID").ToInt();
                    i.Show();
                }
            }
        }

        /// <summary>
        /// Удаление позиции заказа
        /// </summary>
        public void DeleteOrderPosition()
        {
            if (PositionGrid != null && PositionGrid.GridItems != null && PositionGrid.GridItems.Count > 0)
            {
                if (
                    PositionGridSelectedItem != null && PositionGridSelectedItem.Count > 0
                    && OrderGridSelectedItem != null && OrderGridSelectedItem.Count > 0
                    )
                {
                    var message = $"Удалить выбранную позицию заказа?";
                    if (DialogWindow.ShowDialog(message, "Заказы интернет-магазина", "", DialogWindowButtons.NoYes) != true)
                    {
                        return;
                    }

                    bool succesfulFlag = false;
                    int positionId = 0;

                    var p = new Dictionary<string, string>();
                    p.Add("POSITION_ID", PositionGridSelectedItem.CheckGet("ID"));
                    p.Add("ORDER_ID", OrderGridSelectedItem.CheckGet("ID"));

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sales");
                    q.Request.SetParam("Object", "OnlineStoreOrder");
                    q.Request.SetParam("Action", "DeleteOrderPosition");
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
                                positionId = dataSet.Items.First().CheckGet("ID").ToInt();

                                if (positionId > 0)
                                {
                                    succesfulFlag = true;
                                }
                            }
                        }

                        if (succesfulFlag)
                        {
                            Refresh();
                            string msg = $"Успешное удаление позиции заказа";
                            var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                        else
                        {
                            string msg = $"Ошибка удаления позиции заказа";
                            var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
            }
        }

        /// <summary>
        /// Привязать платёжное поручение к заказу
        /// </summary>
        public void BindCashReceipt()
        {
            var i = new CashReceiptList(OrderGridSelectedItem.CheckGet("ID_POK").ToInt() , OrderGridSelectedItem.CheckGet("ID").ToInt());
            i.Show();

            if (i.SuccesfullBindFlag)
            {
                Refresh();
            }
        }

        /// <summary>
        /// Обновление флага Оплачен для заказа
        /// </summary>
        public void UpdatePaidFlag(int paidFlag)
        {
            if (OrderGridSelectedItem != null && OrderGridSelectedItem.Count > 0)
            {
                // Если отменяем оплату
                if (paidFlag == 0)
                {
                    var message = $"Отменить оплату счёта?";
                    if (!string.IsNullOrEmpty(OrderGridSelectedItem.CheckGet("OPERATION_ID")))
                    {
                        message = $"Внимание!{Environment.NewLine}К заказу привязано платёжное поручение!{Environment.NewLine}Отменить оплату счёта?";
                    }

                    if (DialogWindow.ShowDialog(message, "Заказы интернет-магазина", "", DialogWindowButtons.NoYes) != true)
                    {
                        return;
                    }
                }
                // Если отмечаем оплату счёта
                else if (paidFlag == 1)
                {
                    var message = $"Отметить оплату счёта?";
                    // Если платёжное поручение не привязано, то запрашиваем подтверждение перед отметкой оплаты
                    if (string.IsNullOrEmpty(OrderGridSelectedItem.CheckGet("OPERATION_ID")))
                    {
                        message = $"Внимание!{Environment.NewLine}К заказу не привязано платёжное поручение!{Environment.NewLine}Отметить оплату счёта?";
                    }

                    if (DialogWindow.ShowDialog(message, "Заказы интернет-магазина", "", DialogWindowButtons.NoYes) != true)
                    {
                        return;
                    }
                }

                int orderId = 0;
                bool succesfulFlag = false;

                var p = new Dictionary<string, string>();
                p.Add("ORDER_ID", OrderGridSelectedItem.CheckGet("ID"));
                p.Add("PAID_FLAG", paidFlag.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "OnlineStoreOrder");
                q.Request.SetParam("Action", "UpdatePaidFlag");
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
                            orderId = dataSet.Items.First().CheckGet("ORDER_ID").ToInt();

                            if (orderId > 0)
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
                        string msg = $"Ошибка изменения статуса оплаты заказа";
                        var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
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
        /// Формируются данные для заявки на отгрузку на СОХ
        /// 1 -- Заполняется дата отгрузки и, если не самовывоз, то заполняем данные для заявки на доставку и формируется файл заявки на доставку
        /// 2 -- Сохраняется дата доставки
        /// 3 -- отправляется csv заявка на отгрузку
        /// 4 -- Обновляется статус записи online_store_order
        /// 5 -- Если не самовывоз, то формируем ТН
        /// </summary>
        public void SendApplicationForShipment()
        {
            string date = "";
            bool succesfulFlag = true;

            // 0
            if (succesfulFlag)
            {
                string msg = $"Пожалуйста, предварительно проверьте, что заказ оплачен клиентом.{Environment.NewLine}Продолжить ?";
                if (DialogWindow.ShowDialog(msg, "Заказ интернет-магазина", "", DialogWindowButtons.NoYes) != true)
                {
                    succesfulFlag = false;
                }
            }

            // 1
            if (succesfulFlag)
            {
                succesfulFlag = false;

                var i = new OnlineStoreOrderDeliveryDetails(OrderGridSelectedItem.CheckGet("ID").ToInt(), OrderGridSelectedItem.CheckGet("PICKUP_FLAG").ToInt(), "OnlineStoreOrderList");
                i.Show();

                // INFO Временно убрано ограничение на выбираемую дату отгрузки с СОХ. (По просьбе Кириллова Станислава)
                if (!string.IsNullOrEmpty(i.SelectedDeliveryDate))
                {
                    date = i.SelectedDeliveryDate;
                    succesfulFlag = true;
                }
            }

            // 2
            if (succesfulFlag)
            {
                succesfulFlag = SetDeliveryDate(date);
            }

            int orderId = 0;

            // 3.1
            if (succesfulFlag)
            {
                succesfulFlag = CreatePalletOutFileForFTP(date, out orderId);
            }

            // 3.2
            if (succesfulFlag)
            {
                SendPalletOutFileForFTP(orderId);
            }

            // 4
            if (succesfulFlag)
            {
                succesfulFlag = UpdateShipmentStatus((int)ShipmentStatusRef.ApplicationSend);
            }

            // 5
            if (succesfulFlag)
            {
                // Если не самовывоз, то формируем ТН
                if (OrderGridSelectedItem.CheckGet("PICKUP_FLAG").ToInt() == 0)
                {
                    MovementInvoiceExcelReport();
                }
            }

            if (succesfulFlag)
            {
                Refresh();
            }
        }

        /// <summary>
        /// Изменение даты отгрузки для выбранного заказа
        /// </summary>
        public void EditShipmentDate()
        {
            string date = "";
            bool succesfulFlag = true;

            // 1. Заполнение новой даты отгрузки
            if (succesfulFlag)
            {
                succesfulFlag = false;

                // В случае редактирования даты запрещаем редактировать другие поля
                var i = new OnlineStoreOrderDeliveryDetails(OrderGridSelectedItem.CheckGet("ID").ToInt(), 1, "OnlineStoreOrderList");
                i.Show();

                // INFO Временно убрано ограничение на выбираемую дату отгрузки с СОХ. (По просьбе Кириллова Станислава)
                if (!string.IsNullOrEmpty(i.SelectedDeliveryDate))
                {
                    date = i.SelectedDeliveryDate;
                    succesfulFlag = true;
                }
            }

            // 2. Сохранение новой даты отгрузки в БД
            if (succesfulFlag)
            {
                succesfulFlag = SetDeliveryDate(date);
            }

            if (succesfulFlag)
            {
                Refresh();

                string msg = $"Внимание! Необходимо проинформировать СОХ об изменении даты отгрузки.";
                var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        public bool UpdateShipmentStatus(int shipmentStatus)
        {
            bool succesfulFlag = false;

            var p = new Dictionary<string, string>();
            p.Add("ORDER_ID", OrderGridSelectedItem.CheckGet("ID"));
            p.Add("SHIPMENT_STATUS", shipmentStatus.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "OnlineStoreOrder");
            q.Request.SetParam("Action", "UpdateShipmentStatus");
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
                        int orderId = dataSet.Items.First().CheckGet("ORDER_ID").ToInt();

                        if (orderId > 0)
                        {
                            succesfulFlag = true;
                        }
                    }
                }

                if (succesfulFlag)
                {
                    string msg = $"Успешное изменение статуса отправки файла заявки на отгрузку";
                    var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
                else
                {
                    string msg = $"Ошибка изменения статуса отправки файла заявки на отгрузку";
                    var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                q.ProcessError();
            }

            return succesfulFlag;
        }

        /// <summary>
        /// Повторая печать документов на отгрузку (УПД)
        /// </summary>
        public void ReprintUPDFile()
        {
            string paymentOrder = "";
            if (string.IsNullOrEmpty(OrderGridSelectedItem.CheckGet("OPERATION_ID")))
            {
                var message = $"Внимание!{Environment.NewLine}К заказу не привязано платёжное поручение!" +
                    $"{Environment.NewLine}Ввести данные платёжного поручения вручную?";
                var d = new DialogWindow($"{message}", "Заказ интернет-магазина", "", DialogWindowButtons.NoYes);

                if (d.ShowDialog() == true)
                {
                    var i = new ComplectationCMQuantity("", true);
                    i.Label.ToolTip = "Данные в формате: [номер платёжного поручения] от [дата платёжного поручения]";
                    i.Show("Платёжное поручение:");
                    if (i.OkFlag)
                    {
                        paymentOrder = i.QtyString;
                        if (string.IsNullOrEmpty(paymentOrder))
                        {
                            string msg = $"Пожалуйста, укажите номер и дату платёжного поручения.";
                            d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                            d.ShowDialog();

                            return;
                        }
                    }
                }
                else
                {
                    return;
                }
            }

            PrintUniversalTransferReport(paymentOrder);
        }

        /// <summary>
        /// Создаём записи в таблицах naklrashod и rashod
        /// И формируем УПД
        /// </summary>
        public void CreateUPDFile()
        {
            string paymentOrder = "";

            if (string.IsNullOrEmpty(OrderGridSelectedItem.CheckGet("OPERATION_ID")))
            {
                var message = $"Внимание!{Environment.NewLine}К заказу не привязано платёжное поручение!" +
                    $"{Environment.NewLine}Ввести данные платёжного поручения вручную?";
                var d = new DialogWindow($"{message}", "Заказ интернет-магазина", "", DialogWindowButtons.NoYes);

                if (d.ShowDialog() == true)
                {
                    var i = new ComplectationCMQuantity("", true);
                    i.Label.ToolTip = "Данные в формате: [номер платёжного поручения] от [дата платёжного поручения]";
                    i.Show("Платёжное поручение:");
                    if (i.OkFlag)
                    {
                        paymentOrder = i.QtyString;
                        if (string.IsNullOrEmpty(paymentOrder))
                        {
                            string msg = $"Пожалуйста, укажите номер и дату платёжного поручения.";
                            d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                            d.ShowDialog();

                            return;
                        }
                    }
                }
                else
                {
                    return;
                }
            }

            DisableControls(true, "Создание накладной");

            if (CreateConsumption())
            {
                PrintUniversalTransferReport(paymentOrder);

                if (UpdateShipmentStatus((int)ShipmentStatusRef.DocumentsSend))
                {

                }
            }

            EnableControls();

            Refresh();
        }

        /// <summary>
        /// Создаём записи в таблицах naklrashod и rashod
        /// </summary>
        public bool CreateConsumption()
        {
            bool outResult = false;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "OnlineStoreOrder");
            q.Request.SetParam("Action", "CreateConsumption");

            q.Request.SetParam("ORDER_ID", OrderGridSelectedItem.CheckGet("ID"));
            q.Request.SetParam("CUSTOMER_ID", OrderGridSelectedItem.CheckGet("ID_PROD"));

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var dataSet = ListDataSet.Create(result, "ITEMS");

                    if (dataSet != null && dataSet.Items.Count > 0)
                    {
                        int invoiceId = dataSet.Items.First().CheckGet("INVOICE_ID").ToInt();
                        string invoiceNumber = dataSet.Items.First().CheckGet("INVOICE_NUMBER");

                        if (invoiceId > 0 && !string.IsNullOrEmpty(invoiceNumber))
                        {
                            outResult = true;
                        }
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            return outResult;
        }

        /// <summary>
        /// Проводим расход (naklrashod и rashod)
        /// </summary>
        public bool ConfirmConsumption()
        {
            bool succesfulFlag = false;

            var p = new Dictionary<string, string>();
            p.Add("ORDER_ID", OrderGridSelectedItem.CheckGet("ID"));

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "OnlineStoreOrder");
            q.Request.SetParam("Action", "ConfirmConsumption");
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
                        int invoiceId = dataSet.Items.First().CheckGet("INVOICE_ID").ToInt();

                        if (invoiceId > 0)
                        {
                            succesfulFlag = true;
                        }
                    }
                }

                if (succesfulFlag)
                {
                    string msg = $"Успешное проведение продажи";
                    var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                    d.ShowDialog();

                    Refresh();
                }
                else
                {
                    string msg = $"Ошибка проведения продажи";
                    var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                q.ProcessError();
            }

            return succesfulFlag;
        }

        /// <summary>
        /// Генерация УПД
        /// </summary>
        public async void PrintUniversalTransferReport(string paymentOrder = "")
        {
            // Липецк
            if (OrderGridSelectedItem.CheckGet("SHIPPING_POINT").ToInt() == 2)
            {
                DocumentPrintManager documentPrintManager = new DocumentPrintManager();
                documentPrintManager.RoleName = this.RoleName;
                documentPrintManager.HiddenInterface = true;
                documentPrintManager.SetSignotory(210, "Никитина Л.Г.");
                documentPrintManager.FormInit();
                documentPrintManager.SetDefaults();
                documentPrintManager.Form.SetValues(new Dictionary<string, string>() { { DocumentPrintManager.UniversalTransferDocumentFieldPath, "1" } });
                documentPrintManager.InvoiceId = OrderGridSelectedItem.CheckGet("NSTHET").ToInt();
                documentPrintManager.LoadInvoiceData();

                if (Central.DebugMode)
                {
                    documentPrintManager.HtmlDocument();
                }
                else
                {
                    documentPrintManager.ViewDocument();
                }
            }
            // Москва
            else if (OrderGridSelectedItem.CheckGet("SHIPPING_POINT").ToInt() == 1)
            {
                var p = new Dictionary<string, string>();
                var q = new LPackClientQuery();

                string documentFormatName = DocumentPrintManager.GetDocumentFormatName(DocumentPrintManager.BaseDocumentFormat.PDF);
                if (Central.DebugMode)
                {
                    documentFormatName = DocumentPrintManager.GetDocumentFormatName(DocumentPrintManager.BaseDocumentFormat.HTML);
                }

                p.Add("ID", OrderGridSelectedItem.CheckGet("ID"));
                p.Add("PAYMENT_ORDER", paymentOrder);
                p.Add("DOCUMENT_FORMAT_NAME", documentFormatName);

                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "OnlineStoreOrder");
                q.Request.SetParam("Action", "GetUniversalTransferDocument");

                q.Request.SetParams(p);
                q.Request.Timeout = DocumentPrintManager.GetDocumentTimeout;
                q.Request.Attempts = 1;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    if (q.Answer.Type == LPackClientAnswer.AnswerTypeRef.File)
                    {
                        var printHelper = new PrintHelper();
                        printHelper.PrintingProfile = PrintingSettings.DocumentPrinter.ProfileName;
                        printHelper.Init();
                        var printingResult = printHelper.ShowPreview(q.Answer.DownloadFilePath);
                        printHelper.Dispose();
                    }
                    else
                    {
                    }
                }
                else
                {
                    q.SilentErrorProcess = true;
                    q.ProcessError();
                }
            }
           


            //ListDataSet positionDataSet = new ListDataSet();
            //ListDataSet resultSummDataSet = new ListDataSet();
            //// Получаем данные для отчёта
            //{
            //    var q = new LPackClientQuery();
            //    q.Request.SetParam("Module", "Sales");
            //    q.Request.SetParam("Object", "OnlineStoreOrder");
            //    q.Request.SetParam("Action", "ListForUniversalTransferReport");

            //    q.Request.SetParam("ID", OrderGridSelectedItem.CheckGet("ID"));

            //    await Task.Run(() =>
            //    {
            //        q.DoQuery();
            //    });

            //    if (q.Answer.Status == 0)
            //    {
            //        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
            //        if (result != null)
            //        {
            //            positionDataSet = ListDataSet.Create(result, "ITEMS");
            //            resultSummDataSet = ListDataSet.Create(result, "RESULT_SUMM");
            //        }
            //    }
            //    else
            //    {
            //        q.ProcessError();
            //    }
            //}

            //if (positionDataSet != null && positionDataSet.Items.Count > 0 && positionDataSet.Items.First().Count > 0)
            //{
            //    bool resume = true;

            //    var culture = new System.Globalization.CultureInfo("");
            //    var reportDocument = new ReportDocument();

            //    //var reportTemplate = "C:\\Users\\sviridov_ae\\source\\repos\\l-pack_erp\\Client\\Reports\\Sales\\UniversalTransferReport.xaml";
            //    //var stream = File.OpenRead(reportTemplate);

            //    var reportTemplate = "Client.Reports.Sales.UniversalTransferReport.xaml";
            //    Assembly CurrentAssembly = Assembly.GetExecutingAssembly();
            //    var stream = CurrentAssembly.GetManifestResourceStream(reportTemplate);

            //    if (resume)
            //    {
            //        if (stream == null)
            //        {
            //            Central.Dbg($"Cant load report template [{reportTemplate}]");
            //            resume = false;
            //        }
            //    }

            //    if (resume)
            //    {
            //        var reader = new StreamReader(stream);

            //        reportDocument.XamlData = reader.ReadToEnd();
            //        reportDocument.XamlImagePath = System.IO.Path.Combine(Environment.CurrentDirectory, @"Templates\");
            //        reader.Close();

            //        string tpl = reportDocument.XamlData;

            //        ReportData data = new ReportData();

            //        //общие данные
            //        var systemName = $"{Central.Parameters.SystemName} {Central.Parameters.BaseLabel}";

            //        var firstDictionary = positionDataSet.Items.First();
            //        string todayDttm = DateTime.Now.ToString("dd.MM.yyyy");

            //        // Основные данные
            //        data.ReportDocumentValues.Add("DOCUMENT_NUMBER", firstDictionary.CheckGet("DOCUMENT_NUMBER"));

            //        if (!string.IsNullOrEmpty(firstDictionary.CheckGet("DELIVERY_DTTM")))
            //        {
            //            data.ReportDocumentValues.Add("DOCUMENT_DTTM", firstDictionary.CheckGet("DELIVERY_DTTM"));
            //        }
            //        else
            //        {
            //            data.ReportDocumentValues.Add("DOCUMENT_DTTM", todayDttm);
            //        }

            //        string placeForData = "\"       \"              20      год";
            //        data.ReportDocumentValues.Add("PLACE_FOR_DATA", placeForData);

            //        string senderAddress = firstDictionary.CheckGet("SENDER_ADRES");
            //        data.ReportDocumentValues.Add("SENDER_ADDRESS", senderAddress);
            //        string senderName = firstDictionary.CheckGet("SENDER_NAME");
            //        data.ReportDocumentValues.Add("SENDER_NAME", senderName);

            //        data.ReportDocumentValues.Add("BUYER_NAME", firstDictionary.CheckGet("BUYER_NAME"));
            //        data.ReportDocumentValues.Add("BUYER_ADDRESS", firstDictionary.CheckGet("BUYER_ADDRESS"));
            //        data.ReportDocumentValues.Add("BUYER_INN", firstDictionary.CheckGet("BUYER_INN"));
            //        data.ReportDocumentValues.Add("BUYER_KPP", firstDictionary.CheckGet("BUYER_KPP"));

            //        data.ReportDocumentValues.Add("RECIPIENT_NAME", firstDictionary.CheckGet("BUYER_NAME"));
            //        data.ReportDocumentValues.Add("RECIPIENT_ADDRESS", firstDictionary.CheckGet("RECIPIENT_ADDRESS"));
            //        data.ReportDocumentValues.Add("RECIPIENT_INN", firstDictionary.CheckGet("BUYER_INN"));
            //        data.ReportDocumentValues.Add("RECIPIENT_KPP", firstDictionary.CheckGet("BUYER_KPP"));

            //        data.ReportDocumentValues.Add("SELLER_ADDRESS", firstDictionary.CheckGet("SELLER_ADDRESS"));
            //        data.ReportDocumentValues.Add("SELLER_NAME", firstDictionary.CheckGet("SELLER_NAME"));
            //        data.ReportDocumentValues.Add("SELLER_BIG_NAME", firstDictionary.CheckGet("SELLER_BIG_NAME"));
            //        data.ReportDocumentValues.Add("SELLER_INN", firstDictionary.CheckGet("SELLER_INN"));
            //        data.ReportDocumentValues.Add("SELLER_KPP", firstDictionary.CheckGet("SELLER_KPP"));

            //        if (!string.IsNullOrEmpty(firstDictionary.CheckGet("RECEIPT_NUMBER")) && !string.IsNullOrEmpty(firstDictionary.CheckGet("RECEIPT_DTTM")))
            //        {
            //            data.ReportDocumentValues.Add("RECEIPT", $"документ об оплате № {firstDictionary.CheckGet("RECEIPT_NUMBER").ToString()} от {firstDictionary.CheckGet("RECEIPT_DTTM")}");
            //        }
            //        else
            //        {
            //            if (!string.IsNullOrEmpty(paymentOrder))
            //            {
            //                data.ReportDocumentValues.Add("RECEIPT", $"документ об оплате № {paymentOrder}");
            //            }
            //            else
            //            {
            //                data.ReportDocumentValues.Add("RECEIPT", $"");
            //            }
            //        }

            //        // таблица с позициями
            //        DataTable table = new DataTable("Positions");
            //        table.Columns.Add("ROW_NUMBER", typeof(string));
            //        table.Columns.Add("PRODUCT_NAME", typeof(string));
            //        table.Columns.Add("IZM_CODE", typeof(string));
            //        table.Columns.Add("IZM", typeof(string));
            //        table.Columns.Add("PRODUCT_QUANTITY", typeof(string));
            //        table.Columns.Add("BASED_PRICE", typeof(string));
            //        table.Columns.Add("PRICE_WITHOUT_NDS", typeof(string));
            //        table.Columns.Add("PRICE_NDS", typeof(string));
            //        table.Columns.Add("PRICE_WITH_NDS", typeof(string));

            //        // условный порядковый номер позиции
            //        int counter = 0;
            //        // Количество страниц документа
            //        int pageCount = 0;

            //        var decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

            //        foreach (Dictionary<string, string> position in positionDataSet.Items)
            //        {
            //            counter += 1;

            //            table.Rows.Add(new object[]
            //            {
            //                counter.ToString(),
            //                position.CheckGet("PRODUCT_NAME"),
            //                796.ToString(),
            //                "шт",
            //                position.CheckGet("KOL").Replace(decimalSeparator, "."),
            //                Math.Round(decimal.Parse(position["CENAPRODR_R"].Replace(".", decimalSeparator)), 2, MidpointRounding.AwayFromZero).ToString().Replace(decimalSeparator, ".").ToDouble().ToString("#.00"),
            //                Math.Round(decimal.Parse(position["SUMMABEZNDS"].Replace(".", decimalSeparator)), 2, MidpointRounding.AwayFromZero).ToString().Replace(decimalSeparator, ".").ToDouble().ToString("#.00"),
            //                position["SUMMANDS"].Replace(decimalSeparator, ".").ToDouble().ToString("#.00"),
            //                Math.Round(decimal.Parse(position["SUMMA"].Replace(".", decimalSeparator)), 2, MidpointRounding.AwayFromZero).ToString().Replace(decimalSeparator, ".").ToDouble().ToString("#.00"),
            //            });
            //        }

            //        data.DataTables.Add(table);

            //        data.ReportDocumentValues.Add("SUMMARY_PRICE_WITHOUT_NDS", resultSummDataSet.Items.First().CheckGet("RESULT_SUM").ToString().Replace(decimalSeparator, ".").ToDouble().ToString("#.00"));
            //        data.ReportDocumentValues.Add("SUMMARY_PRICE_NDS", resultSummDataSet.Items.First().CheckGet("RESULT_NDS").ToString().Replace(decimalSeparator, ".").ToDouble().ToString("#.00"));
            //        data.ReportDocumentValues.Add("SUMMARY_PRICE_WITH_NDS", resultSummDataSet.Items.First().CheckGet("RESULT_SUM_VAT").ToString().Replace(decimalSeparator, ".").ToDouble().ToString("#.00")); 

            //        data.ReportDocumentValues.Add("PAGE_COUNT", "________");

            //        string positionRownumberFirst = "0";
            //        if (positionDataSet.Items.Count > 0)
            //        {
            //            positionRownumberFirst = "1";
            //        }
            //        data.ReportDocumentValues.Add("POSITION_ROWNUMBER_FIRST", positionRownumberFirst);
            //        data.ReportDocumentValues.Add("POSITION_ROWNUMBER_LAST", counter.ToString());

            //        // штрихкод
            //        string barcode = "";
            //        {
            //            // nsthet
            //            string barcodeInvoiceId = $"{firstDictionary.CheckGet("INVOICE_ID")}";
            //            int barcodeInvoiceIdLength = barcodeInvoiceId.Length;
            //            for (int i = barcodeInvoiceIdLength; i < 10; i++)
            //            {
            //                barcodeInvoiceId = $"0{barcodeInvoiceId}";
            //            }

            //            // id_pok
            //            string barcodeCustomerId = $"{firstDictionary.CheckGet("RECIPIENT_ID")}";
            //            int barcodeCustomerIdLength = barcodeCustomerId.Length;
            //            for (int i = barcodeCustomerIdLength; i < 10; i++)
            //            {
            //                barcodeCustomerId = $"0{barcodeCustomerId}";
            //            }

            //            // summa
            //            string barcodeSumma = $"{resultSummDataSet.Items.First().CheckGet("RESULT_SUM_VAT").ToString().Replace(decimalSeparator, ".").ToDouble() * 100}";
            //            int barcodeSummaLength = barcodeSumma.Length;
            //            for (int i = barcodeSummaLength; i < 10; i++)
            //            {
            //                barcodeSumma = $"0{barcodeSumma}";
            //            }

            //            barcode = $"{barcodeInvoiceId}{barcodeSumma}{barcodeCustomerId}";
            //        }

            //        //BARCODE
            //        data.ReportDocumentValues.Add("BARCODE", barcode);

            //        string job = "";
            //        string fio = "";
            //        if (firstDictionary.CheckGet("SHIPPING_POINT").ToInt() == 2)
            //        {
            //            job = firstDictionary.CheckGet("JOB");
            //            fio = firstDictionary.CheckGet("FIO"); // "Никитина Л.Г.";
            //        }
            //        data.ReportDocumentValues.Add("JOB", job);
            //        data.ReportDocumentValues.Add("FIO", fio);

            //        try
            //        {
            //            reportDocument.XamlData = tpl;

            //            XpsDocument xps = reportDocument.CreateXpsDocumentKey(data, "MovementInvoiceReport");
            //            var pp = new PrintPreview();
            //            pp.documentViewer.Document = xps.GetFixedDocumentSequence();
            //            pp.Show();
            //        }
            //        catch (Exception ex)
            //        {
            //            var msg = "";
            //            msg = $"{msg}Не удалось создать транспортную накладную.{Environment.NewLine}";
            //            msg = $"{msg}Пожалуйста, запустите создание документа снова.";
            //            var d = new DialogWindow(msg, "Ошибка создания документа", "", DialogWindowButtons.OK);
            //            d.ShowDialog();
            //        }
            //    }
            //}
        }

        /// <summary>
        /// Установка настроек для принтера
        /// </summary>
        public void SetPrintSettings()
        {
            var i = new PrintingInterface();
            Close();
        }

        /// <summary>
        /// Повторная печать ТН
        /// </summary>
        public void ReprintMovementInvoiceExcelFile()
        {
            MovementInvoiceExcelReport();
        }

        /// <summary>
        /// Генерация эксель файла транспортной накладной для СОХ
        /// </summary>
        public async void MovementInvoiceExcelReport()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "OnlineStoreOrder");
            q.Request.SetParam("Action", "CreateMovementInvoiceExcelReport");

            q.Request.SetParam("ID", OrderGridSelectedItem.CheckGet("ID"));

            q.Request.Timeout = 25000;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                //ConvertDocToPdf(q.Answer.DownloadFilePath);
                Central.SaveFile(q.Answer.DownloadFilePath, true);
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Заполняем поле дата доставки для выбранного заказа (online_store_order)
        /// </summary>
        public bool SetDeliveryDate(string date)
        {
            bool succesfulFlag = false;

            var p = new Dictionary<string, string>();
            p.Add("ORDER_ID", OrderGridSelectedItem.CheckGet("ID"));
            p.Add("DELIVERY_DATE", date);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "OnlineStoreOrder");
            q.Request.SetParam("Action", "UpdateDeliveryDate");
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
                        int orderId = dataSet.Items.First().CheckGet("ORDER_ID").ToInt();
                        if (orderId > 0)
                        {
                            succesfulFlag = true;
                        }
                    }
                }

                if (succesfulFlag)
                {
                    string msg = $"Успешное сохранение даты доставки";
                    var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
                else
                {
                    string msg = $"Ошибка сохранения даты доставки";
                    var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                q.ProcessError();
            }

            return succesfulFlag;
        }

        /// <summary>
        /// Создаём csv файл заявки на отгрузку для отправки по фтп
        /// </summary>
        public bool CreatePalletOutFileForFTP(string deliveryDate, out int _orderId)
        {
            int orderId = 0;
            bool succesfulFlag = false;

            var p = new Dictionary<string, string>();
            p.Add("ORDER_ID", OrderGridSelectedItem.CheckGet("ID"));
            p.Add("DELIVERY_DATE", deliveryDate);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "OnlineStoreOrder");
            q.Request.SetParam("Action", "CreateFileForFTP");
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
                        orderId = dataSet.Items.First().CheckGet("ORDER_ID").ToInt();
                        if (orderId > 0)
                        {
                            succesfulFlag = true;
                        }
                    }
                }

                if (!succesfulFlag)
                {
                    string msg = $"Ошибка создания файла заявки на отгрузку №{orderId} для выгрузки на FTP сервер. Пожалуйста, сообщите о проблеме.";
                    var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
                else
                {
                    string msg = $"Успешное создание файла заявки на отгрузку №{orderId} для выгрузки на FTP сервер";
                    var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                q.ProcessError();
            }

            _orderId = orderId;
            return succesfulFlag;
        }

        public async void SendPalletOutFileForFTP(int orderId)
        {
            bool succesfulFlag = true;
            string fileName = $"{orderId}_PALOUT.csv";

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
                    string msg = $"Ошибка отправления файла заявки на отгрузку {fileName} на FTP сервер. Пожалуйста, сообщите о проблеме.";
                    var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
                else
                {
                    string msg = $"Успешное отправление файла заявки на отгрузку {fileName} на FTP сервера";
                    var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Снятие брони со всех поддонов выбранного заказа
        /// </summary>
        public bool UnreserveAll(bool refreshFlag = true)
        {
            bool succesfulFlag = false;

            if (PositionGrid != null && PositionGrid.GridItems != null && PositionGrid.GridItems.Count > 0)
            {
                var p = new Dictionary<string, string>();
                p.Add("ORDER_ID", OrderGridSelectedItem.CheckGet("ID"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "OnlineStoreOrder");
                q.Request.SetParam("Action", "UnreservPalletByOrder");
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
                            int orderId = dataSet.Items.First().CheckGet("ORDER_ID").ToInt();

                            if (orderId > 0)
                            {
                                succesfulFlag = true;
                            }
                        }
                    }

                    if (succesfulFlag)
                    {
                        string msg = $"Успешное снятие брони со всех поддонов заказа.";
                        var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                    else
                    {
                        string msg = $"Ошибка снятия брони со всех поддонов заказа.";
                        var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }

                if (refreshFlag)
                {
                    Refresh();
                }
            }
            // Нет поддонов, значит нет брони. Поэтому succesfulFlag = true
            else
            {
                succesfulFlag = true;
            }

            return succesfulFlag;
        }

        /// <summary>
        /// Отмена бронирования поддонов под выбранную позицию заказа
        /// </summary>
        public void Unreserve()
        {
            if (PositionGrid != null && PositionGrid.GridItems != null && PositionGrid.GridItems.Count > 0)
            {
                if (PositionGridSelectedItem != null && PositionGridSelectedItem.Count > 0 && PositionGridSelectedItem.CheckGet("RESERVED_FLAG").ToInt() == 1) 
                {
                    var p = new Dictionary<string, string>();
                    p.Add("POSITION_ID", PositionGridSelectedItem.CheckGet("ID"));

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sales");
                    q.Request.SetParam("Object", "OnlineStoreOrder");
                    q.Request.SetParam("Action", "UnreservPallet");
                    q.Request.SetParams(p);

                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    q.DoQuery();

                    int positionId = 0;

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            var dataSet = ListDataSet.Create(result, "ITEMS");
                            if (dataSet != null && dataSet.Items.Count > 0)
                            {
                                positionId = dataSet.Items.First().CheckGet("ID").ToInt();
                            }
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }

                    if (positionId == 0)
                    {
                        string msg = $"Ошибка снятия брони с поддонов для выбранной позиции заказа.";
                        var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                    else
                    {
                        Refresh();

                        string msg = $"Успешное снятие брони с поддонов для выбранной позиции заказа.";
                        var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
            }
        }

        /// <summary>
        /// Забронировать поддоны в отгрузку (для одной позиции заявки)
        /// </summary>
        public void Reserve()
        {
            if (PositionGrid != null && PositionGrid.GridItems != null && PositionGrid.GridItems.Count > 0)
            {
                if (PositionGridSelectedItem != null && PositionGridSelectedItem.Count > 0 && PositionGridSelectedItem.CheckGet("RESERVED_FLAG").ToInt() == 0)
                {
                    var p = new Dictionary<string, string>();
                    p.Add("POSITION_ID", PositionGridSelectedItem.CheckGet("ID"));

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sales");
                    q.Request.SetParam("Object", "OnlineStoreOrder");
                    q.Request.SetParam("Action", "ReservPallet");
                    q.Request.SetParams(p);

                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    q.DoQuery();

                    int positionId = 0;

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            var dataSet = ListDataSet.Create(result, "ITEMS");
                            if (dataSet != null && dataSet.Items.Count > 0)
                            {
                                positionId = dataSet.Items.First().CheckGet("ID").ToInt();
                            }
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }

                    if (positionId == 0)
                    {
                        string msg = $"При бронировании поддонов произошла ошибка.";
                        var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                    else
                    {
                        Refresh();

                        string msg = $"Успешное бронирование поддонов для выбранной позиции заказа.";
                        var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
            }
        }

        /// <summary>
        /// Забронировать поддоны в отгрузку (для всех позиций заявки)
        /// </summary>
        public void ReserveAll()
        {
            if (PositionGrid != null && PositionGrid.GridItems != null && PositionGrid.GridItems.Count > 0)
            {
                bool succesfullFlag = true;

                foreach (var position in PositionGrid.GridItems)
                {
                    if (position.CheckGet("RESERVED_FLAG").ToInt() == 0)
                    {
                        var p = new Dictionary<string, string>();
                        p.Add("POSITION_ID", position.CheckGet("ID"));

                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Sales");
                        q.Request.SetParam("Object", "OnlineStoreOrder");
                        q.Request.SetParam("Action", "ReservPallet");
                        q.Request.SetParams(p);

                        q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                        q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                        q.DoQuery();

                        int positionId = 0;

                        if (q.Answer.Status == 0)
                        {
                            var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                            if (result != null)
                            {
                                var dataSet = ListDataSet.Create(result, "ITEMS");
                                if (dataSet != null && dataSet.Items.Count > 0)
                                {
                                    positionId = dataSet.Items.First().CheckGet("ID").ToInt();
                                }
                            }
                        }
                        else
                        {
                            q.ProcessError();
                        }

                        if (positionId == 0)
                        {
                            succesfullFlag = false;
                        }
                    }
                }

                if (!succesfullFlag)
                {
                    string msg = $"При бронировании поддонов произошла ошибка.";
                    var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }

                Refresh();
            }
        }

        /// <summary>
        /// Создание заявки на производство
        /// </summary>
        public void CreateOrder()
        {
            if (OrderGridSelectedItem != null)
            {
                var i = new ResponsibleStockOrder();
                i.OrderType = 2;
                i.OnlineStoreOrderId = OrderGridSelectedItem.CheckGet("ID").ToInt();
                i.CustomerId = OrderGridSelectedItem.CheckGet("ID_POK").ToInt();
                i.ContractId = OrderGridSelectedItem.CheckGet("IDDOG").ToInt();
                i.OrderNumberTextBox.Text = OrderGridSelectedItem.CheckGet("ORDER_NUM");
                i.OnlineStoreOrderPositionList = PositionGrid.Items;
                i.Show();
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
                SenderName = "OnlineStoreOrderList",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            OrderGrid.Destruct();
            PositionGrid.Destruct();
        }

        public void Refresh()
        {
            OrderGridDataSet = new ListDataSet();
            OrderGridSelectedItem = new Dictionary<string, string>();
            PositionGridDataSet = new ListDataSet();
            PositionGridSelectedItem = new Dictionary<string, string>();

            OrderGridLoadItems();
        }

        /// <summary>
        /// Установка активности доступных действий
        /// </summary>
        public void SetActionEnabled()
        {
            EditOrderButton.IsEnabled = false;
            DeleteOrderButton.IsEnabled = false;
            AddPositionButton.IsEnabled = false;
            EditPositionButton.IsEnabled = false;
            DeletePositionButton.IsEnabled = false;
            CancelApplicationButton.IsEnabled = false;
            ReserveAllButton.IsEnabled = false;
            UnreserveAllButton.IsEnabled = false;

            OrderGrid.Menu["EditOnlineStoreOrder"].Enabled = false;
            OrderGrid.Menu["DeleteOnlineStoreOrder"].Enabled = false;
            OrderGrid.Menu["CreateReceiptFile"].Enabled = false;
            OrderGrid.Menu["UpdatePaidFlag"].Enabled = false;
            OrderGrid.Menu["SendApplicationForShipment"].Enabled = false;
            OrderGrid.Menu["CreateUPDFile"].Enabled = false;
            OrderGrid.Menu["CancelApplication"].Enabled = false;
            OrderGrid.Menu["ReprintReceiptFile"].Enabled = false;
            OrderGrid.Menu["ReprintUPDFile"].Enabled = false;
            OrderGrid.Menu["UpdateNoPaidFlag"].Enabled = false;
            OrderGrid.Menu["ConfirmConsumption"].Enabled = false;
            OrderGrid.Menu["BindCashReceipt"].Enabled = false;
            OrderGrid.Menu["ReprintMovementInvoiceExcelFile"].Enabled = false;
            OrderGrid.Menu["CancelOnlineStoreOrder"].Enabled = false;
            OrderGrid.Menu["BackToWork"].Enabled = false;
            OrderGrid.Menu["EditShipmentDate"].Enabled = false;
            PositionGrid.Menu["EditPosition"].Enabled = false;
            PositionGrid.Menu["DeletePosition"].Enabled = false;
            PositionGrid.Menu["Reserve"].Enabled = false;
            PositionGrid.Menu["Unreserve"].Enabled = false;

            OrderGrid.Menu["CreateOrder"].Enabled = false;

            if (OrderGridSelectedItem != null && OrderGridSelectedItem.Count > 0)
            {
                // Не сформирован счёт
                if (string.IsNullOrEmpty(OrderGridSelectedItem.CheckGet("RECEIPT_NUMBER")))
                {
                    OrderGrid.Menu["EditOnlineStoreOrder"].Enabled = true;
                    EditOrderButton.IsEnabled = true;
                    OrderGrid.Menu["DeleteOnlineStoreOrder"].Enabled = true;
                    DeleteOrderButton.IsEnabled = true;

                    AddPositionButton.IsEnabled = true;

                    if (PositionGrid != null && PositionGrid.GridItems != null && PositionGrid.GridItems.Count > 0)
                    {
                        OrderGrid.Menu["CreateReceiptFile"].Enabled = true;

                        if (PositionGridSelectedItem != null && PositionGridSelectedItem.Count > 0)
                        {
                            EditPositionButton.IsEnabled = true;
                            DeletePositionButton.IsEnabled = true;
                            PositionGrid.Menu["EditPosition"].Enabled = true;
                            PositionGrid.Menu["DeletePosition"].Enabled = true;
                        }
                    }
                }
                else
                {
                    if (OrderGridSelectedItem.CheckGet("PAID_FLAG").ToInt() == 0)
                    {
                        OrderGrid.Menu["UpdatePaidFlag"].Enabled = true;
                    }
                    else if (OrderGridSelectedItem.CheckGet("SHIPMENT_STATUS").ToInt() < 1)
                    {
                        OrderGrid.Menu["UpdateNoPaidFlag"].Enabled = true;
                    }

                    if (string.IsNullOrEmpty(OrderGridSelectedItem.CheckGet("OPERATION_ID")))
                    {
                        OrderGrid.Menu["BindCashReceipt"].Enabled = true;
                    }

                    OrderGrid.Menu["ReprintReceiptFile"].Enabled = true;
                }

                if (OrderGridSelectedItem.CheckGet("SHIPMENT_STATUS").ToInt() == 0 || OrderGridSelectedItem.CheckGet("SHIPMENT_STATUS").ToInt() == 4)
                {
                    if (OrderGridSelectedItem.CheckGet("OPERATION_SUMM").ToInt() == 0)
                    {
                        OrderGrid.Menu["CancelOnlineStoreOrder"].Enabled = true;
                    }
                }

                if (PositionGrid != null && PositionGrid.GridItems != null && PositionGrid.GridItems.Count > 0)
                {
                    if (OrderGridSelectedItem.CheckGet("CANCELED_FLAG").ToInt() == 0)
                    {
                        ReserveAllButton.IsEnabled = true;
                    }

                    if (PositionGridSelectedItem != null && PositionGridSelectedItem.Count > 0)
                    {
                        if (PositionGridSelectedItem.CheckGet("RESERVED_FLAG").ToInt() == 0)
                        {
                            if (OrderGridSelectedItem.CheckGet("CANCELED_FLAG").ToInt() == 0)
                            {
                                PositionGrid.Menu["Reserve"].Enabled = true;
                            }
                        }
                        else
                        {
                            if (OrderGridSelectedItem.CheckGet("SHIPMENT_STATUS").ToInt() == 0 || OrderGridSelectedItem.CheckGet("SHIPMENT_STATUS").ToInt() == 4)
                            {
                                PositionGrid.Menu["Unreserve"].Enabled = true;
                            }
                        }
                    }

                    if (PositionGrid.GridItems.Count(x => x.CheckGet("RESERVED_FLAG").ToInt() == 1) > 0)
                    {
                        if (OrderGridSelectedItem.CheckGet("SHIPMENT_STATUS").ToInt() == 0 || OrderGridSelectedItem.CheckGet("SHIPMENT_STATUS").ToInt() == 4)
                        {
                            UnreserveAllButton.IsEnabled = true;
                        }
                    }
                }

                if (OrderGridSelectedItem.CheckGet("PAID_FLAG").ToInt() == 1 && OrderGridSelectedItem.CheckGet("SHIPMENT_STATUS").ToInt() < 1)
                {
                    if (PositionGrid != null && PositionGrid.GridItems != null && PositionGrid.GridItems.Count > 0)
                    {
                        if (PositionGrid.GridItems.Count == PositionGrid.GridItems.Count(x => x.CheckGet("RESERVED_FLAG").ToInt() == 1))
                        {
                            OrderGrid.Menu["SendApplicationForShipment"].Enabled = true;
                        }
                    }
                }

                if (OrderGridSelectedItem.CheckGet("PAID_FLAG").ToInt() == 1 && OrderGridSelectedItem.CheckGet("SHIPMENT_STATUS").ToInt() == 4)
                {
                    if (PositionGrid != null && PositionGrid.GridItems != null && PositionGrid.GridItems.Count > 0)
                    {
                        if (PositionGrid.GridItems.Count == PositionGrid.GridItems.Count(x => x.CheckGet("RESERVED_FLAG").ToInt() == 1))
                        {
                            OrderGrid.Menu["BackToWork"].Enabled = true;
                        }
                    }
                }

                if (OrderGridSelectedItem.CheckGet("SHIPMENT_STATUS").ToInt() >= 1)
                {
                    if (OrderGridSelectedItem.CheckGet("SHIPMENT_STATUS").ToInt() <= 3)
                    {
                        OrderGrid.Menu["EditShipmentDate"].Enabled = true;
                    }

                    if (OrderGridSelectedItem.CheckGet("SHIPMENT_STATUS").ToInt() < 3)
                    {
                        CancelApplicationButton.IsEnabled = true;
                        OrderGrid.Menu["CancelApplication"].Enabled = true;
                    }

                    if (OrderGridSelectedItem.CheckGet("PAID_FLAG").ToInt() == 1 && OrderGridSelectedItem.CheckGet("PICKUP_FLAG").ToInt() == 0)
                    {
                        OrderGrid.Menu["ReprintMovementInvoiceExcelFile"].Enabled = true;
                    }
                }

                if (string.IsNullOrEmpty(OrderGridSelectedItem.CheckGet("NSTHET")))
                {
                    if (OrderGridSelectedItem.CheckGet("SHIPMENT_STATUS").ToInt() >= 1)
                    {
                        if (OrderGridSelectedItem.CheckGet("SHIPPING_POINT").ToInt() == 1)
                        {
                            OrderGrid.Menu["CreateUPDFile"].Enabled = true;
                        }
                    }
                }
                else
                {
                    OrderGrid.Menu["ReprintUPDFile"].Enabled = true;
                }

                // Если открузка выполнена, но мы не проводили документы
                if (((OrderGridSelectedItem.CheckGet("SHIPMENT_STATUS").ToInt() == 5)
                    || (!string.IsNullOrEmpty(OrderGridSelectedItem.CheckGet("ORDER_BASE_PRINT_DTTM")) && OrderGridSelectedItem.CheckGet("SHIPPING_POINT").ToInt() == 2))
                    && OrderGridSelectedItem.CheckGet("COMPLITED_FLAG").ToInt() == 0)
                {
                    OrderGrid.Menu["ConfirmConsumption"].Enabled = true;
                }

                // Если это отгрузка ил Липецка и счёт оплачен и не создана заявку на гофропроизводство
                // , то можем сформировать заявку на производство
                if (OrderGridSelectedItem.CheckGet("PAID_FLAG").ToInt() == 1
                    && OrderGridSelectedItem.CheckGet("SHIPPING_POINT").ToInt() == 2
                    && string.IsNullOrEmpty(OrderGridSelectedItem.CheckGet("ORDER_NSTHET")))
                {
                    OrderGrid.Menu["CreateOrder"].Enabled = true;
                }
            }

            ProcessPermissions();
        }

        /// <summary>
        /// Создание Excel файла по данным в гриде заказов интернет-магазина
        /// </summary>
        public async void ExportToExcel()
        {
            if (OrderGrid.GridItems != null)
            {
                if (OrderGrid.GridItems.Count > 0)
                {
                    var eg = new ExcelGrid();
                    var cols = OrderGrid.Columns;
                    eg.SetColumnsFromGrid(cols);
                    eg.Items = OrderGrid.GridItems;
                    await Task.Run(() =>
                    {
                        eg.Make();
                    });
                }
            }
        }

        /// <summary>
        /// Создание Excel файла по данным в гриде позиций выбранного заказа интернет-магазина
        /// </summary>
        public async void ExportToPositionExcel()
        {
            if (PositionGrid != null && PositionGrid.GridItems != null && PositionGrid.GridItems.Count > 0)
            {
                if (OrderGridSelectedItem != null && OrderGridSelectedItem.Count > 0)
                {
                    var eg = new ExcelGrid();
                    var cols = PositionGrid.Columns;
                    eg.SetColumnsFromGrid(cols);
                    eg.Items = PositionGrid.GridItems;
                    eg.GridTitle = $"Данные по позициям заказа интернет магазина. Заказ номер {OrderGridSelectedItem.CheckGet("ORDER_NUM")} от {OrderGridSelectedItem.CheckGet("ORDER_DT")}.";
                    await Task.Run(() =>
                    {
                        eg.Make();
                    });
                }
                else
                {
                    var msg = "Не выбран заказ интернет-магазина.";
                    var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                var msg = "Нет данных для выгрузки в Excel.";
                var d = new DialogWindow($"{msg}", "Заказ интернет-магазина", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        public async void CustomerExcelReport()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "OnlineStoreOrder");
            q.Request.SetParam("Action", "GetCustomerExcelReportData");
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
                    if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                    {
                        var eg = new ExcelGrid
                        {
                            Columns = new List<ExcelGridColumn>
                            {
                                new ExcelGridColumn("CUSTOMER_NAME", "Покупатель", 203),
                                new ExcelGridColumn("PRODUCT_CODE", "Артикул", 100),
                                new ExcelGridColumn("PRODUCT_NAME", "Продукция", 250),
                                new ExcelGridColumn("PRODUCT_QUANTITY", "Количество", 50, ExcelGridColumn.ColumnTypeRef.Integer),
                                new ExcelGridColumn("PRICE_VAT", "Цена с НДС", 50, ExcelGridColumn.ColumnTypeRef.Double),
                                new ExcelGridColumn("ORDER_DT", "Дата заказа", 55),
                                new ExcelGridColumn("DELIVERY_DT", "Дата доставки", 55),
                                new ExcelGridColumn("ORDER_ID", "Заказ", 30, ExcelGridColumn.ColumnTypeRef.Integer),
                                new ExcelGridColumn("SHIPPING_POINT_NAME", "Точка отгрузки", 70)
                            },
                            Items = dataSet.Items,
                            GridTitle = $"Отчет по контрагентам на {DateTime.Now:dd.MM.yyyy hh:mm:ss}. "
                        };

                        await Task.Run(() =>
                        {
                            eg.Make();
                        });
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        public void DisableControls(bool splashVisible = true, string splashMessage = "Загрузка")
        {
            GridToolbar.IsEnabled = false;

            if (splashVisible)
            {
                SplashControl.Visible = true;
                SplashControl.Message = splashMessage;
            }
        }

        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
            SplashControl.Visible = false;
        }

        /// <summary>
        /// отображение справочной статьи
        /// (относительный путь)
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp-new/application/online_shop/online_shop_orders");
            //Central.ShowHelp("/doc/l-pack-erp/sales/online_store/online_store_order");
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.IndexOf("OnlineStoreOrder") > -1)
            {
                if (m.ReceiverName.IndexOf("OnlineStoreOrderList") > -1)
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

        public void ProcessCommand(string command)
        {
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "refresh":
                    {
                        Refresh();
                    }
                    break;
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

        private void AddPositionButton_Click(object sender, RoutedEventArgs e)
        {
            AddOrderPosition();
        }

        private void EditPositionButton_Click(object sender, RoutedEventArgs e)
        {
            EditOrderPosition();
        }

        private void DeletePositionButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteOrderPosition();
        }

        private void DeleteOrderButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void EditOrderButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AddOrderButton_Click(object sender, RoutedEventArgs e)
        {
            AddOrder();
        }

        private void ReserveAllButton_Click(object sender, RoutedEventArgs e)
        {
            ReserveAll();
        }

        private void CancelApplicationButton_Click(object sender, RoutedEventArgs e)
        {
            CancelApplication();
        }

        private void UnreserveAllButton_Click(object sender, RoutedEventArgs e)
        {
            UnreserveAll();
        }

        private void StatusSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            OrderGrid.UpdateItems();
        }

        private void ExcelButton_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcel();
        }

        private void PositionExcelButton_Click(object sender, RoutedEventArgs e)
        {
            ExportToPositionExcel();
        }

        private void CustomerExcelReportButton_Click(object sender, RoutedEventArgs e)
        {
            CustomerExcelReport();
        }

        private void ShippingPointSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            OrderGrid.UpdateItems();
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            SetPrintSettings();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }
    }
}
