using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Sales;
using Client.Interfaces.Service.Printing;
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

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Накладные отгрузки
    /// </summary>
    public partial class ShipmentDocumentList : ControlBase
    {
        /// <summary>
        /// Обязательные параметры:
        /// TransportId.
        /// </summary>
        public ShipmentDocumentList()
        {
            ControlTitle = "Накладные отгрузки";
            OnMessage = (ItemMessage message) => {
                DebugLog($"message=[{message.Message}]");
                if (message.ReceiverName == ControlName)
                {
                    ProcessCommand(message.Action);
                }
            };

            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, _ProcessMessages);

            //конструктор, будет вызван, когда объект создается
            //здесь создаются все внутренние структуры
            //впервые этот коллбэк будет вызван, когда данный таб станет активным
            //впервые (до этих пор, никакая работа внутри не происходит, что экономит ресурсы)
            OnLoad = () =>
            {
                SetDefaults();
                OrderGridInit();
                PositionGridInit();
                OrderPositionGridInit();
                ProcessPermissions();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                OrderGrid.Destruct();
                PositionGrid.Destruct();
                OrderPositionGrid.Destruct();
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
        /// Идентификатор отгрузки
        /// </summary>
        public int TransportId { get; set; }

        /// <summary>
        /// Выбранная запись в списке отгрузок
        /// </summary>
        public Dictionary<string, string> SelectedShipmentItem { get; set; }

        /// <summary>
        /// Датасет с данными для грида накладных по отгрузке
        /// </summary>
        public ListDataSet GridDataSet { get; set; }

        /// <summary>
        /// Датасет с данными по позициям выбранной накладной
        /// </summary>
        public ListDataSet PositionGridDataSet { get; set; }

        public ListDataSet OrderPositionGridDataSet { get; set; }

        public string FrameName { get; set; }

        /// <summary>
        /// отображение фрейма
        /// </summary>
        public void Show()
        {
            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 1;
            ControlName = $"{ControlName}_{TransportId}";
            FrameName = $"{ControlName}";
            Central.WM.Show(FrameName, $"Накладные отгрузки #{TransportId}", false, "add", this, "bottom");
        }

        public void SetDefaults()
        {
        }

        public void OrderGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид заявки",
                        Path="ORDER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер СФ",
                        Path="INVOICE_NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        Width2=9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Покупатель",
                        Path="CUSTOMER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=33,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Грузополучатель",
                        Path="CONSIGNEE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=33,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Водитель",
                        Path="DRIVER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=20,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Продавец",
                        Path="SELLER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=15,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер заявки",
                        Path="ORDER_NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        Width2=20,
                    },
                    new DataGridHelperColumn
                    {
                        Header="По заявке, шт.",
                        Path="ORDER_QUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Отгружено, шт.",
                        Path="CONSUMPTION_QUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Отгружено, позиций",
                        Path="CONSUMPTION_COUNT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Доверенность",
                        Path="PROXY_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Адресов доставки",
                        Path="ADDRESS_COUNT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Примечание кладовщиков",
                        Path="NOTE_GENERAL",
                        ColumnType=ColumnTypeRef.String,
                        Width2=19,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Примечание грузчиков",
                        Path="NOTE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=19,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Примечание к документам",
                        Path="NOTE_PRINT",
                        ColumnType=ColumnTypeRef.String,
                        Width2=19,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Менеджер",
                        Path="MANAGER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=12,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Ид покупателя",
                        Path="CUSTOMER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Расход по накладной",
                        Path="CONSUMPTION_COUNT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Самовывоз",
                        Path="SELFSHIP",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Посредник в ТН",
                        Path="DEALER_WAYBILL_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Файл доверенности",
                        Path="PROXY_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Посредник",
                        Path="DEALER_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид терминала",
                        Path="TERMINAL_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид накладной",
                        Path="INVOICE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Тип продукции в заявке",
                        Path="TYPE_ORDER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="INVOICE_TYPE",
                        Path="INVOICE_TYPE",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },

                };
                OrderGrid.SetColumns(columns);
                
                OrderGrid.OnLoadItems = OrderGridLoadItems;
                OrderGrid.PrimaryKey = "ORDER_ID";
                OrderGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                OrderGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem != null && selectedItem.Count > 0)
                    {
                        PositionGridLoadItems();
                        OrderPositionGridLoadItems();
                        UpdateActions(selectedItem);
                    }
                };

                OrderGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    // определение цветов фона строк
                    {
                        StylerTypeRef.BackgroundColor,
                        (Dictionary<string, string> row) =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            // Нет расходной накладной
                            if (row.CheckGet("INVOICE_ID").ToInt() == 0)
                            {
                                color = HColor.Blue;
                            }

                            // Ещё на терминале
                            if (!string.IsNullOrEmpty(row.CheckGet("TERMINAL_ID")))
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
                };

                // контекстное меню
                OrderGrid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    {
                        "PrintManager",
                        new DataGridContextMenuItem()
                        {
                            Header="Печать",
                            Action=()=>
                            {
                                PrintManager();
                            }
                        }
                    },
                    { "s0", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    {
                        "DeleteDocument",
                        new DataGridContextMenuItem()
                        {
                            Header="Удалить",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                DeleteDocument();
                            }
                        }
                    },
                };

                OrderGrid.Init();
            }
        }

        public async void OrderGridLoadItems()
        {
            DisableControls();

            var p = new Dictionary<string, string>();
            p.Add("TRANSPORT_ID", TransportId.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Shipment");
            q.Request.SetParam("Action", "ListDocument");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = 3;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    GridDataSet = ListDataSet.Create(result, "ITEMS");
                    OrderGrid.UpdateItems(GridDataSet);
                }
            }
            else
            {
                if (q.Answer.Error.Code != 7)
                {
                    q.ProcessError();
                }
            }

            EnableControls();
        }

        public void PositionGridInit()
        {
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
                        Width2=3,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Path="CONSUMPTION_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул",
                        Path="PRODUCT_CODE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=15,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Код товара",
                        Path="PRODUCT_KOD",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование продукции",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=35,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ поддона",
                        Path="PALLET_FULL_NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ рулона",
                        Path="ROLL_NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Path="QUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Цена",
                        Path="PRICE",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Проведен",
                        Path="COMPLETED_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Потребитель",
                        Path="CUSTOMER",
                        ColumnType=ColumnTypeRef.String,
                        Width2=17,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Адрес доставки",
                        Path="SHIP_ADRES",
                        ColumnType=ColumnTypeRef.String,
                        Width2=17,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Ид прихода",
                        Path="INCOMING_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид продукции",
                        Path="PRODUCT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул потребителя",
                        Path="PRODUCT_CUSTOMER_CODE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="НДС",
                        Path="CUSTOMER_VAT",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=5,
                        Format="N2",
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Площадь на поддоне",
                        Path="SQUARE",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Площадь изделия",
                        Path="PRODUCT_SQUARE",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Диаметр рулона",
                        Path="ROLL_DIAMETER",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Единица измерения",
                        Path="IZM_FIRST_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=5,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Сумма",
                        Path="SUMMARY_PRICE",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=5,
                        Format="N2",
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ задания",
                        Path="PRODUCTION_TASK_NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ поддона",
                        Path="PALLET_NUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид категории продукции",
                        Path="PRODUCT_IDK1",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид покупателя",
                        Path="CUSTOMER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                };
                PositionGrid.SetColumns(columns);
                PositionGrid.PrimaryKey = "CONSUMPTION_ID";
                PositionGrid.AutoUpdateInterval = 0;
                PositionGrid.UseSorting = false;
                PositionGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;

                PositionGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    // определение цветов фона строк
                    {
                        StylerTypeRef.BackgroundColor,
                        (Dictionary<string, string> row) =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            // Не проведённый расход
                            if (row.CheckGet("COMPLETED_FLAG").ToInt() == 0 && row.CheckGet("QUANTITY").ToInt() > 0)
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
                };

                // контекстное меню
                PositionGrid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    {
                        "AddConsumption",
                        new DataGridContextMenuItem()
                        {
                            Header="Добавить позицию",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                AddConsumption();
                            }
                        }
                    },
                    {
                        "EditConsumption",
                        new DataGridContextMenuItem()
                        {
                            Header="Изменить позицию",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                EditConsumption();
                            }
                        }
                    },
                    {
                        "DeleteConsumption",
                        new DataGridContextMenuItem()
                        {
                            Header="Удалить позицию",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                DeleteConsumption();
                            }
                        }
                    },
                    { "s0", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    {
                        "ConfirmConsumption",
                        new DataGridContextMenuItem()
                        {
                            Header="Провести позицию",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                ConfirmConsumption();
                            }
                        }
                    },
                    {
                        "UnconfirmConsumption",
                        new DataGridContextMenuItem()
                        {
                            Header="Отменить проведение позиции",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                UnconfirmConsumption();
                            }
                        }
                    },
                };

                PositionGrid.Init();
            }
        }

        public async void PositionGridLoadItems()
        {
            DisableControls();

            if (OrderGrid != null && OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0 && OrderGrid.SelectedItem.CheckGet("ORDER_ID").ToInt() > 0)
            {
                var p = new Dictionary<string, string>();
                p.Add("INVOICE_ID", OrderGrid.SelectedItem.CheckGet("ORDER_ID"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "Sale");
                q.Request.SetParam("Action", "ListConsumption");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = 3;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                PositionGridDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        PositionGridDataSet = ListDataSet.Create(result, "ITEMS");
                    }
                }
                else
                {
                    if (q.Answer.Error.Code != 7)
                    {
                        q.ProcessError();
                    }
                }
                PositionGrid.UpdateItems(PositionGridDataSet);
            }

            EnableControls();
        }

        public void OrderPositionGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Комплект",
                        Path="POSITION_NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        Width2=4,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Path="ORDER_POSITION_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="PRODUCT_SHIPPED_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=36,
                    },
                    new DataGridHelperColumn
                    {
                        Header="По заявке",
                        Path="QUANTITY_BY_ORDER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Отгружено",
                        Path="QUANTITY_SHIPPED",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.FontWeight,
                                row =>
                                {
                                    var fontWeight= new FontWeight();
                                    fontWeight=FontWeights.Bold;

                                    return fontWeight;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Под отгрузку",
                        Path="QUANTITY_IN_STOCK_BY_ORDER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Отклонение, шт",
                        Path="QUANTITY_DEVIATION",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Отклонение, %",
                        Path="QUANTITY_PERCENTAGE_DEVIATION",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=6,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // процентное отклонение отгруженного количества к количеству по заявке превышает допустимые значения
                                    if (!PercentageDeviation.CheckPercentageDeviation(row.CheckGet("QUANTITY_BY_ORDER").ToInt(), row["QUANTITY_PERCENTAGE_DEVIATION"].ToDouble()))
                                    {
                                        if (row["QUANTITY_PERCENTAGE_DEVIATION"].ToDouble() >= 0)
                                        {
                                            color = HColor.Red;
                                        }
                                        else
                                        {
                                            color = HColor.Yellow;
                                        }
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
                        Header="На складе",
                        Path="QUANTITY_IN_STOCK",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид заявки",
                        Path="ORDER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ROW_NUMBER",
                        Path="ROW_NUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                };
                OrderPositionGrid.SetColumns(columns);
                OrderPositionGrid.PrimaryKey = "ORDER_POSITION_ID";
                OrderPositionGrid.AutoUpdateInterval = 0;
                OrderPositionGrid.UseSorting = false;
                OrderPositionGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;

                OrderPositionGrid.Init();
            }
        }

        public async void OrderPositionGridLoadItems()
        {
            DisableControls();

            if (OrderGrid != null && OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0 && OrderGrid.SelectedItem.CheckGet("ORDER_ID").ToInt() > 0)
            {
                var p = new Dictionary<string, string>();
                p.Add("ORDER_ID", OrderGrid.SelectedItem.CheckGet("ORDER_ID"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "Shipment");
                q.Request.SetParam("Action", "ListDocumentOrder");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = 3;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                OrderPositionGridDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        OrderPositionGridDataSet = ListDataSet.Create(result, "ITEMS");
                    }
                }
                else
                {
                    if (q.Answer.Error.Code != 7)
                    {
                        q.ProcessError();
                    }
                }
                OrderPositionGrid.UpdateItems(OrderPositionGridDataSet);
            }

            EnableControls();
        }

        public void Refresh()
        {
            OrderGrid.LoadItems();
        }

        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
        }

        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
        }

        public void UpdateActions(Dictionary<string, string> selectedItem)
        {
            if (!string.IsNullOrEmpty(selectedItem.CheckGet("TERMINAL_ID")))
            {
                PrintButton.IsEnabled = false;
                OrderGrid.Menu["PrintManager"].Enabled = false;
            }
            else
            {
                PrintButton.IsEnabled = true;
                OrderGrid.Menu["PrintManager"].Enabled = true;
            }

            if (selectedItem.CheckGet("INVOICE_ID").ToInt() > 0
                && selectedItem.CheckGet("CONSUMPTION_COUNT").ToInt() == 0)
            {
                DeleteDocumentButton.IsEnabled = true;
                OrderGrid.Menu["DeleteDocument"].Enabled = true;
            }
            else
            {
                DeleteDocumentButton.IsEnabled = false;
                OrderGrid.Menu["DeleteDocument"].Enabled = false;
            }

            ProcessPermissions();
        }

        /// <summary>
        /// отображение справочной статьи
        /// (относительный путь)
        /// </summary>
        public void ShowHelp()
        {
            //FIXME документация

            Central.ShowHelp("/doc/l-pack-erp/");
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

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel(this.RoleName);
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

            if (OrderPositionGrid != null && OrderPositionGrid.Menu != null && OrderPositionGrid.Menu.Count > 0)
            {
                foreach (var manuItem in OrderPositionGrid.Menu)
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
        private void _ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverName.IndexOf("ShipmentDocumentList") > -1)
            {
                switch (m.Action)
                {
                    case "Refresh":
                        Refresh();
                        break;
                }
            }
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "ShipmentControl",
                SenderName = "ShipmentPlan",
                ReceiverName = "ShipmentsList",
                Action = "ShowShipmentAll",
                Message = "",
            });

            Central.WM.Close(FrameName);
        }

        public void CheckQuantityPercentageDeviation()
        {
            if (string.IsNullOrEmpty(OrderGrid.SelectedItem.CheckGet("INVOICE_NUMBER")) 
                && OrderPositionGrid.Items != null && OrderPositionGrid.Items.Count > 0)
            {
                string msg = "";
                foreach (var item in OrderPositionGrid.Items)
                {
                    if (item.CheckGet("QUANTITY_BY_ORDER").ToInt() > 0)
                    {
                        // процентное отклонение отгруженного количества к количеству по заявке превышает допустимые значения
                        if (!PercentageDeviation.CheckPercentageDeviation(item.CheckGet("QUANTITY_BY_ORDER").ToInt(), item.CheckGet("QUANTITY_PERCENTAGE_DEVIATION").ToDouble()))
                        {
                            msg = $"{msg}Отклонение по позиции заявки: {item.CheckGet("PRODUCT_SHIPPED_NAME")}." +
                                $"{Environment.NewLine}Количество в заявке: {item.CheckGet("QUANTITY_BY_ORDER").ToInt()}." +
                                $"{Environment.NewLine}Фактически погружено: {item.CheckGet("QUANTITY_SHIPPED").ToInt()}." +
                                $"{Environment.NewLine}Отклонение: {item.CheckGet("QUANTITY_PERCENTAGE_DEVIATION").ToDouble()}%." +
                                $"{Environment.NewLine}{Environment.NewLine}";
                        }
                    }
                }

                if (!string.IsNullOrEmpty(msg))
                {
                    var d = new DialogWindow($"{msg}", "Накладные отгрузки", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
        }

        /// <summary>
        /// Проверяем, есть ли по выбранной заявке поддоны в буфере.
        /// Если пурвичные документы уже распечатали, то не проверяем, чтобы не спамить
        /// </summary>
        public void CheckQuantityInBuffer()
        {
            if (OrderGrid != null && OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0 
                && OrderGrid.SelectedItem.CheckGet("ORDER_ID").ToInt() > 0
                && string.IsNullOrEmpty(OrderGrid.SelectedItem.CheckGet("INVOICE_NUMBER")))
            {
                var p = new Dictionary<string, string>();
                p.Add("ORDER_ID", OrderGrid.SelectedItem.CheckGet("ORDER_ID"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "Shipment");
                q.Request.SetParam("Action", "GetQuantityInByfferByOrder");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                        {
                            string msg = $"Внимание! В буфере есть поддоны под эту заявку!{Environment.NewLine}{Environment.NewLine}";

                            foreach (var item in ds.Items)
                            {
                                msg = $"{msg}Заявка: {item.CheckGet("ORDER_POSITION_ID").ToInt()}." +
                                    $"{Environment.NewLine}Поддон: {item.CheckGet("PALLET_FULL_NUMBER")}" +
                                    $"{Environment.NewLine}Количество: {item.CheckGet("QUANTITY").ToInt()}" +
                                    $"{Environment.NewLine}Продукция: {item.CheckGet("NAME")}" +
                                    $"{Environment.NewLine}Ячейка: {item.CheckGet("SKLAD")} {item.CheckGet("NUM_PLACE")}" +
                                    $"{Environment.NewLine}{Environment.NewLine}";
                            }

                            var d = new DialogWindow($"{msg}", "Накладные отгрузки", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        public void PrintManager()
        {
            if (OrderGrid != null && OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0 && OrderGrid.SelectedItem.CheckGet("ORDER_ID").ToInt() > 0)
            {
                CheckQuantityPercentageDeviation();
                CheckQuantityInBuffer();

                var documentPrintManager = new DocumentPrintManager();
                documentPrintManager.RoleName = this.RoleName;
                documentPrintManager.InvoiceId = OrderGrid.SelectedItem.CheckGet("ORDER_ID").ToInt();
                documentPrintManager.Show();
            }
        }

        public void DeleteDocument()
        {
            DisableControls();

            if (OrderGrid != null && OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0)
            {
                bool resume = false;

                {
                    string msg = $"Удалить накладную ?{Environment.NewLine}" +
                        $"Номер накладной: {OrderGrid.SelectedItem.CheckGet("ORDER_ID").ToInt()}.{Environment.NewLine}" +
                        $"Номер заявки: {OrderGrid.SelectedItem.CheckGet("ORDER_NUMBER")}.{Environment.NewLine}" +
                        $"Покупатель {OrderGrid.SelectedItem.CheckGet("CUSTOMER_NAME")}.";
                    var d = new DialogWindow($"{msg}", "Накладные отгрузки", "", DialogWindowButtons.YesNo);
                    if (d.ShowDialog() == true)
                    {
                        resume = true;
                    }
                }

                if (resume)
                {
                    var p = new Dictionary<string, string>();
                    p.Add("INVOICE_ID", OrderGrid.SelectedItem.CheckGet("INVOICE_ID"));

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sales");
                    q.Request.SetParam("Object", "Sale");
                    q.Request.SetParam("Action", "Delete");
                    q.Request.SetParams(p);
                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;
                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {
                        bool succesfullFlag = false;

                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            var dataSet = ListDataSet.Create(result, "ITEMS");
                            if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                            {
                                if (dataSet.Items.First().CheckGet("INVOICE_ID").ToInt() > 0)
                                {
                                    succesfullFlag = true;
                                }
                            }
                        }

                        if (succesfullFlag)
                        {
                            Refresh();
                        }
                        else
                        {
                            string msg = $"При удалении накладной произошла ошибка. Пожалуйста, сообщите о проблеме.";
                            var d = new DialogWindow($"{msg}", "Накладные отгрузки", "", DialogWindowButtons.OK);
                            d.ShowDialog();
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

        /// <summary>
        /// Добавление позиции расхода
        /// </summary>
        public void AddConsumption()
        {
            if (OrderGrid != null && OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0 && OrderGrid.SelectedItem.CheckGet("ORDER_ID").ToInt() > 0)
            {
                var window = new ConsumptionEdit();
                window.InvoiceId = OrderGrid.SelectedItem.CheckGet("ORDER_ID").ToInt();
                window.CustomerId = OrderGrid.SelectedItem.CheckGet("CUSTOMER_ID").ToInt();
                window.Show();
            }
        }

        /// <summary>
        /// Провести позицию
        /// </summary>
        public void ConfirmConsumption()
        {
            if (PositionGrid != null && PositionGrid.SelectedItem != null && PositionGrid.SelectedItem.Count > 0 
                && PositionGrid.SelectedItem.CheckGet("CONSUMPTION_ID").ToInt() > 0)
            {

                if (PositionGrid.SelectedItem.CheckGet("COMPLETED_FLAG").ToInt() == 1)
                {
                    return;
                }

                DisableControls();
                 
                bool succesfullFlag = false;

                var p = new Dictionary<string, string>();
                p.Add("INVOICE_TYPE", OrderGrid.SelectedItem.CheckGet("INVOICE_TYPE"));
                p.Add("CONSUMPTION_ID", PositionGrid.SelectedItem.CheckGet("CONSUMPTION_ID"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "Sale");
                q.Request.SetParam("Action", "ConfirmConsumption");
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
                        if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                        {
                            if (dataSet.Items.First().CheckGet("CONSUMPTION_ID").ToInt() > 0)
                            {
                                succesfullFlag = true;
                            }
                        }
                    }

                    if (succesfullFlag)
                    {
                        Refresh();
                    }
                    else
                    {
                        var msg = "Ошибка проведения позиции расхода. Пожалуйста, сообщите о проблеме.";
                        var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
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
                var msg = "Не выбрана позиция для проведения.";
                var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Отменить проведение позиции
        /// </summary>
        public void UnconfirmConsumption()
        {
            if (PositionGrid != null && PositionGrid.SelectedItem != null && PositionGrid.SelectedItem.Count > 0
                && PositionGrid.SelectedItem.CheckGet("CONSUMPTION_ID").ToInt() > 0)
            {
                if (PositionGrid.SelectedItem.CheckGet("COMPLETED_FLAG").ToInt() == 0)
                {
                    return;
                }

                DisableControls();

                var p = new Dictionary<string, string>();
                p.Add("CONSUMPTION_ID", PositionGrid.SelectedItem.CheckGet("CONSUMPTION_ID"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "Sale");
                q.Request.SetParam("Action", "UnconfirmConsumption");
                q.Request.SetParams(p);
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    bool succesfullFlag = false;

                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var dataSet = ListDataSet.Create(result, "ITEMS");
                        if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                        {
                            if (dataSet.Items.First().CheckGet("CONSUMPTION_ID").ToInt() > 0)
                            {
                                succesfullFlag = true;
                            }
                        }
                    }

                    if (succesfullFlag)
                    {
                        Refresh();
                    }
                    else
                    {
                        var msg = "Ошибка отмены проведения позиции расхода. Пожалуйста, сообщите о проблеме.";
                        var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
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
                var msg = "Не выбрана позиция для отмены проведения.";
                var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Редактирование выбранной позиции расхода
        /// </summary>
        public void EditConsumption()
        {
            if (PositionGrid != null && PositionGrid.SelectedItem != null && PositionGrid.SelectedItem.Count > 0
                && PositionGrid.SelectedItem.CheckGet("CONSUMPTION_ID").ToInt() > 0)
            {
                if (PositionGrid.SelectedItem.CheckGet("COMPLETED_FLAG").ToInt() == 1)
                {
                    var msg = "Отмените проведение позиции перед редактированием.";
                    var d = new DialogWindow($"{msg}", "Удаление позиции расхода", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                    return;
                }

                {
                    var msg = "Удалить позицию расхода?";
                    var d = new DialogWindow($"{msg}", "Удаление позиции расхода", "", DialogWindowButtons.NoYes);
                    if (d.ShowDialog() != true)
                    {
                        return;
                    }
                }

                DisableControls();

                var window = new ConsumptionEdit();
                window.InvoiceId = OrderGrid.SelectedItem.CheckGet("ORDER_ID").ToInt();
                window.ConsumptionId = PositionGrid.SelectedItem.CheckGet("CONSUMPTION_ID").ToInt();
                window.CompletedFlag = PositionGrid.SelectedItem.CheckGet("COMPLETED_FLAG").ToBool();
                window.CustomerId = PositionGrid.SelectedItem.CheckGet("CUSTOMER_ID").ToInt();
                window.ConsumptionData = PositionGrid.SelectedItem;
                window.Show();

                EnableControls();
            }
            else
            {
                var msg = "Не выбрана позиция для редактирования.";
                var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Удаление выбранной позиции расхода
        /// </summary>
        public void DeleteConsumption()
        {
            if (PositionGrid != null && PositionGrid.SelectedItem != null && PositionGrid.SelectedItem.Count > 0
                && PositionGrid.SelectedItem.CheckGet("CONSUMPTION_ID").ToInt() > 0)
            {
                if (PositionGrid.SelectedItem.CheckGet("COMPLETED_FLAG").ToInt() == 1)
                {
                    var msg = "Отмените проведение позиции перед удалением.";
                    var d = new DialogWindow($"{msg}", "Удаление позиции расхода", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                    return;
                }

                DisableControls();

                var p = new Dictionary<string, string>();
                p.Add("INVOICE_ID", OrderGrid.SelectedItem.CheckGet("ORDER_ID"));
                p.Add("CONSUMPTION_ID", PositionGrid.SelectedItem.CheckGet("CONSUMPTION_ID"));
                p.Add("INCOMING_ID", PositionGrid.SelectedItem.CheckGet("INCOMING_ID"));
                p.Add("CUSTOMER_ID", PositionGrid.SelectedItem.CheckGet("CUSTOMER_ID"));
                p.Add("COMPLETED_FLAG", PositionGrid.SelectedItem.CheckGet("COMPLETED_FLAG").ToInt().ToString());
                if (string.IsNullOrEmpty(PositionGrid.SelectedItem.CheckGet("PRODUCTION_TASK_NUMBER")))
                {
                    p.Add("EMPTY_NUMBER_FLAG", "1");
                }
                else
                {
                    p.Add("EMPTY_NUMBER_FLAG", "0");
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "Sale");
                q.Request.SetParam("Action", "DeleteConsumption");
                q.Request.SetParams(p);
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    bool succesfullFlag = false;

                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var dataSet = ListDataSet.Create(result, "ITEMS");
                        if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                        {
                            if (dataSet.Items.First().CheckGet("CONSUMPTION_ID").ToInt() > 0)
                            {
                                succesfullFlag = true;
                            }
                        }
                    }

                    if (succesfullFlag)
                    {
                        Refresh();
                    }
                    else
                    {
                        var msg = "Ошибка удаление позиции расхода. Пожалуйста, сообщите о проблеме.";
                        var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
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
                var msg = "Не выбрана позиция для удаления.";
                var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Вывод печатной формы карты склада
        /// </summary>
        private void PrintStockmap()
        {
            if (TransportId > 0)
            {
                var id = TransportId;
                var reporter = new ShipmentReport(id);
                reporter.PrintStockmap();
            }
        }

        /// <summary>
        /// Порядок загрузки
        /// </summary>
        private async void LoadingOrderNew()
        {
            if (TransportId > 0)
            {
                if (SelectedShipmentItem != null && SelectedShipmentItem.Count > 0)
                {
                    int id = TransportId;
                    var shipment = new Shipment(id);
                    shipment.ShowLoadingScheme(SelectedShipmentItem);
                }
                else
                {
                    var msg = "Нет данных по отгрузке.";
                    var d = new DialogWindow($"{msg}", "Детализация накладной", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
        }

        /// <summary>
        /// Установка настроек для принтера
        /// </summary>
        public void SetPrintSettings()
        {
            var i = new PrintingInterface();
            Close();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void ResreshButton_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            PrintManager();
        }

        private void DeleteDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteDocument();
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            SetPrintSettings();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }

        private void AddConsumptionButton_Click(object sender, RoutedEventArgs e)
        {
            AddConsumption();
        }

        private void EditConsumptionButton_Click(object sender, RoutedEventArgs e)
        {
            EditConsumption();
        }

        private void DeleteConsumptionButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteConsumption();
        }

        private void ConfirmSelectedConsumptionButton_Click(object sender, RoutedEventArgs e)
        {
            ConfirmConsumption();
        }

        private void UnconfirmSelectedConsumptionButton_Click(object sender, RoutedEventArgs e)
        {
            UnconfirmConsumption();
        }

        private void LoadingScheneButton_Click(object sender, RoutedEventArgs e)
        {
            LoadingOrderNew();
        }

        private void PrintMapButton_Click(object sender, RoutedEventArgs e)
        {
            PrintStockmap();
        }
    }
}
