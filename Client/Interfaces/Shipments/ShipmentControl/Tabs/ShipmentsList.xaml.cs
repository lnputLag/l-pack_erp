using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
using Client.Interfaces.Service;
using Client.Interfaces.Service.Printing;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Printing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static DevExpress.Data.Filtering.Helpers.SubExprHelper.ThreadHoppingFiltering;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Управление отгрузками, вкладка "список"
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>2</version>
    /// <released>2021-12-07</released>    
    /// <changed>2023-08-23</changed>
    public partial class ShipmentsList : UserControl
    {
        /// <summary>
        /// Инициализация
        /// </summary>
        public ShipmentsList()
        {
            InitializeComponent();

            ShipmentsGridAutoUpdateInterval = (int)(60 * 1);
            DriversGridAutoUpdateInterval = 60 * 1;
            TerminalsGridAutoUpdateInterval = 60 * 1;
            GetShipmentCountToPrintTimerInterval = 10;

            ObjectId = Cryptor.MakeRandom();
            ShipmentId = 0;
            SelectingDriver = false;
            ProductionType = 0;
            HideComplete = false;
            ShipmentDate = "";
            ShipmentTime = "";
            ArriveDriver = new Dictionary<string, string>();


            SetDefaults();

            ShipmentsGridInit();
            PositionsGridInit();
            DriversGridInit();
            TerminalsGridInit();
            GetShipmentCountToPrint();
            RunGetShipmentCountToPrintTimer();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, _ProcessMessages);
            Central.Msg.Register(ProcessMessages);

            ProcessPermissions();
        }

        public string RoleName = "[erp]shipment_control";

        public int ShipmentsGridAutoUpdateInterval { get; set; }
        public int DriversGridAutoUpdateInterval { get; set; }
        public int TerminalsGridAutoUpdateInterval { get; set; }

        /// <summary>
        /// ID инстанса (для отладки)
        /// </summary>
        public int ObjectId { get; set; }

        /// <summary>
        /// датасет, содержащий данные для таблицы отгрузок
        /// </summary>
        public ListDataSet ShipmentsDS { get; set; }

        /// <summary>
        /// датасет, содержащий данные для таблицы водителей
        /// </summary>
        public ListDataSet DriversDS { get; set; }

        /// <summary>
        /// датасет, содержащий данные для таблицы товарных позиций в отгрузке
        /// </summary>
        public ListDataSet PositionsDS { get; set; }

        /// <summary>
        /// датасет, содержащий данные для таблицы тарминалов
        /// </summary>
        public ListDataSet TerminalsDS { get; set; }

        /// <summary>
        /// выбранная отгрузка
        /// </summary>
        Dictionary<string, string> SelectedShipmentItem { get; set; }
        /// <summary>
        /// выбранный водитель
        /// </summary>
        Dictionary<string, string> SelectedDriverItem { get; set; }
        /// <summary>
        /// выбранная позиция отгрузки
        /// </summary>
        Dictionary<string, string> SelectedPositionItem { get; set; }
        /// <summary>
        /// выбранный терминал (список терминалов)
        /// </summary>
        Dictionary<string, string> SelectedTerminalItem { get; set; }

        /// <summary>
        /// id выбранной отгрузки (первый грид)
        /// </summary>
        public int ShipmentId { get; set; }

        /// <summary>
        /// тип продукции выбранной отгрузки
        /// </summary>
        public int ProductionType { get; set; }

        /// <summary>
        /// Признак скрывать завершенные отгрузки
        /// </summary>
        public bool HideComplete { get; set; }

        /// <summary>
        /// Стандартный текст для информационного поля с количеством отгрузок, для которых нужно распечатать документы
        /// </summary>

        private static string ShipmentCountToPrintText = "Требуется печать: ";

        /// <summary>
        /// Количество завершённых отгрузок, для которых нужно распечатать документы
        /// </summary>
        private int ShipmentToPrintCount { get; set; }

        /// <summary>
        /// Ссылка на таб плана отгрузок
        /// </summary>
        private ShipmentPlan ShipmentPlan { get; set; }

        /// <summary>
        /// Таймер для вызова функции получения количества отгрузок в текущих производственных сутках, для которых нужно распечатать документы
        /// </summary>
        public DispatcherTimer GetShipmentCountToPrintTimer { get; set; }

        /// <summary>
        /// Интервал работы таймера для вызова функции получения количества отгрузок в текущих производственных сутках, для которых нужно распечатать документы
        /// </summary>
        public int GetShipmentCountToPrintTimerInterval { get; set; }

        /// <summary>
        /// Идентификатор площадки
        /// </summary>
        private int FactoryId = 1;

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "ShipmentControl",
                ReceiverName = "",
                SenderName = "ShipmentsList",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
            Central.Msg.UnRegister(ProcessMessages);

            //останавливаем таймеры грида
            ShipmentsGrid.Destruct();
            DriversGrid.Destruct();
            PositionsGrid.Destruct();
            TerminalsGrid.Destruct();

            if (GetShipmentCountToPrintTimer != null)
            {
                GetShipmentCountToPrintTimer.Stop();
            }
        }

        /// <summary>
        /// Установка значений по умолчанию. Заполнение выпадающих списков
        /// </summary>
        public void SetDefaults()
        {
            FromDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
            ToDate.Text = DateTime.Now.ToString("dd.MM.yyyy");

            {
                var list = new Dictionary<string, string>();
                list.Add("-1", "Все типы");
                list.Add("0", "Изделия");
                list.Add("2", "Рулоны");
                list.Add("8", "ТМЦ");
                Types.Items = list;
                Types.SelectedItem = list.FirstOrDefault((x) => x.Key == "-1");
            }

            {
                var list = new Dictionary<string, string>();
                list.Add("-1", "Все виды доставки");
                list.Add("0", "Без самовывоза");
                list.Add("1", "Самовывоз");
                list.Add("2", "Самовывоз без доверенности");
                DeliveryTypes.Items = list;
                DeliveryTypes.SelectedItem = list.FirstOrDefault((x) => x.Key == "-1");
            }
            
            {
                var list = new Dictionary<string, string>();
                list.Add("-1", "Все отгрузки");
                list.Add("1", "Неразрешенные");
                list.Add("2", "Разрешенные");
                list.Add("3", "Неотгруженные");
                list.Add("4", "Транспорт");
                list.Add("5", "На терминале");
                list.Add("6", "Отгруженные");
                list.Add("7", "На печать");
                list.Add("8", "Возврат поддонов");
                list.Add("9", "Расход возвратных поддонов");
                list.Add("10", "Автоотгрузка");
                list.Add("11", "Поставка ТМЦ");
                ShipmentTypes.Items = list;
                ShipmentTypes.SelectedItem = list.FirstOrDefault((x) => x.Key == "-1");
            }

            SearchText.Text = "";
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

            if (ShipmentsGrid != null && ShipmentsGrid.Menu != null && ShipmentsGrid.Menu.Count > 0)
            {
                foreach (var manuItem in ShipmentsGrid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }

            if (TerminalsGrid != null && TerminalsGrid.Menu != null && TerminalsGrid.Menu.Count > 0)
            {
                foreach (var manuItem in TerminalsGrid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }

            {
                var manuItemTagList = UIUtil.GetTagList(SettingsButtonMenuItem);
                var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                if (accessMode > userAccessMode)
                {
                    SettingsButtonMenuItem.IsEnabled = false;
                    SettingsButtonMenuItem.Visibility = Visibility.Collapsed;
                }
            }
        }

        public bool AutorunComplete { get; set; }
        public void Autorun()
        {
            if (!AutorunComplete)
            {
                AutorunComplete = true;
            }
        }

        /// <summary>
        /// Инициализация строки итоговых значений
        /// </summary>
        public void InitShipmentsTotals()
        {
            TotalsSquareValue.Text = "";
            TotalsWeightValue.Text = "";

            TotalsSquareValue.Visibility = Visibility.Collapsed;
            TotalsSquareUnit.Visibility = Visibility.Collapsed;
            TotalsWeightValue.Visibility = Visibility.Collapsed;
            TotalsWeightUnit.Visibility = Visibility.Collapsed;
            TotalsTitle.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Заполнение строки итоговых значений
        /// </summary>
        /// <param name="ds"></param>
        public void InitShipmentsTotals(ListDataSet ds)
        {
            if (ds.Initialized)
            {
                var item = ds.GetFirstItem();

                bool values = false;

                {
                    var x = item.CheckGet("SQUARE");
                    if (!string.IsNullOrEmpty(x))
                    {
                        TotalsSquareValue.Text = x.ToInt().ToString();
                        TotalsSquareValue.Visibility = Visibility.Visible;
                        TotalsSquareUnit.Visibility = Visibility.Visible;
                        values = true;
                    }
                }

                {
                    var x = item.CheckGet("WEIGHT");
                    if (!string.IsNullOrEmpty(x))
                    {
                        TotalsWeightValue.Text = x.ToInt().ToString();
                        TotalsWeightValue.Visibility = Visibility.Visible;
                        TotalsWeightUnit.Visibility = Visibility.Visible;
                        values = true;
                    }
                }

                if (values)
                {
                    TotalsTitle.Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// Инициализация таблицы отгрузок
        /// </summary>
        public void ShipmentsGridInit()
        {
            InitShipmentsTotals();

            //грид1, список отгрузок
            {
                //список колонок грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=40,
                        Width2=4,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД Отгрузки",
                        Path="ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=50,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата отгрузки",
                        Path="SHIPMENTDATE",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM",
                        MinWidth=35,
                        Width2=5,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Дт/вр отгрузки",
                        Path="SHIPMENTDATETIME",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM HH:mm",
                        MinWidth=70,
                        Width2=9,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    //перенесена на другой день
                                    if (row.ContainsKey("UNSHIPPED"))
                                    {
                                        if( row["UNSHIPPED"].ToInt() == 1 )
                                        {
                                            color = HColor.Orange;
                                        }
                                    }

                                    //опоздавшая
                                    if (row.ContainsKey("LATE"))
                                    {
                                        if( row["LATE"].ToInt() == 1 )
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
                    new DataGridHelperColumn()
                    {
                        Header="Транспорт",
                        Path="TRANSPORT",
                        MinWidth=35,
                        MaxWidth=80,
                        ColumnType=ColumnTypeRef.Boolean,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Дт/вр приезда водителя",
                        Path="DRIVERARRIVEDATETIME",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm",
                        MinWidth=95,
                        Width2=12,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Дт/вр выезда водителя",
                        Path="DRIVERDEPARTDATETIME",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm",
                        MinWidth=95,
                        Width2=12,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Готова к отгрузке",
                        Path="READY",
                        MinWidth=35,
                        MaxWidth=80,
                        ColumnType=ColumnTypeRef.Boolean,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Статус",
                        Path="STATUS",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=70,
                        Width2=10,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.ForegroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    if( row.CheckGet("STATUSID").ToInt() == 10 )
                                    {
                                        color = HColor.BlueFG;
                                    }
                                    if( row.CheckGet("STATUSID").ToInt() == 5 )
                                    {
                                        color = HColor.BlackFG;
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
                    new DataGridHelperColumn()
                    {
                        Header="% готовности",
                        Path="PROGRESS",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=40,
                        Width2=5,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    //готова
                                    if( row.CheckGet("READY").ToInt() == 1 )
                                    {
                                        color = HColor.VioletPink;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                            {
                                StylerTypeRef.ForegroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    if( row.CheckGet("STATUSID").ToInt() == 10 )
                                    {
                                        color = HColor.BlueFG;
                                    }
                                    if( row.CheckGet("STATUSID").ToInt() == 5 )
                                    {
                                        color = HColor.BlackFG;
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
                    new DataGridHelperColumn()
                    {
                        Header="Самовывоз",
                        Path="EXPORTACCEPTED",
                        MinWidth=35,
                        MaxWidth=80,
                        ColumnType=ColumnTypeRef.Boolean,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Ручная схема погрузчки",
                        Path="MANUAL_LOADING_SCHEME_FLAG",
                        MinWidth=35,
                        MaxWidth=80,
                        ColumnType=ColumnTypeRef.Boolean,
                        Description="Признак того, что схема погрузки создана вручную",
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Упаковка",
                        Path="PACKAGINGTYPETEXT",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=40,
                        Width2=4,
                    },

                    new DataGridHelperColumn()
                    {
                        Header="Перевозчик",
                        Path="CARRIER",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=70,
                        Width2=20,
                    },
                     new DataGridHelperColumn()
                    {
                        Header="Грузополучатель",
                        Path="СONSIGNEE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=120,
                        Width2=18,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Водитель",
                        Path="DRIVER",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=120,
                        Width2=16,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Автомобиль",
                        Path="CAR",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=120,
                        Width2=16,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Телефон",
                        Path="PHONE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=90,
                        Width2=11,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Доверенность",
                        Path="ATTORNEYLETTER",
                        MinWidth=35,
                        MaxWidth=80,
                        ColumnType=ColumnTypeRef.Boolean,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Паспорт",
                        Path="PASSPORT",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=120,
                        Width2=16,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Покупатель",
                        Path="BUYER",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=120,
                        Width2=16,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // тип отгрузки - поставка ТМЦ
                                    if( row.CheckGet("PRODUCTIONTYPE").ToInt() == 8 )
                                    {
                                        color = HColor.VioletPink;
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

                    new DataGridHelperColumn()
                    {
                        Header="Адрес доставки",
                        Path="SHIPMENTADDRESS",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=120,
                        Width2=34,
                    },

                    new DataGridHelperColumn()
                    {
                        Header="Количество позиций",
                        Path="QUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=50,
                        Width2=4,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Количество заявок",
                        Path="ORDERSCOUNT",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=50,
                        Width2=4,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Заявки",
                        Path="ORDERSLIST",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=100,
                        Width2=8,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Загруженность",
                        Path="LOADED",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=40,
                        Width2=5,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Площадь",
                        Path="SQUARE",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=50,
                        Width2=5,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Масса",
                        Path="WEIGHT",
                        ColumnType=ColumnTypeRef.Double,
                        Format = "N1",
                        MinWidth=50,
                        Width2=5,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Цена=0",
                        Path="ZEROPRICE",
                        MinWidth=35,
                        MaxWidth=80,
                        ColumnType=ColumnTypeRef.Boolean,
                        Description="Признак того, что документы нужно печатать с ценой = 0",
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Образец",
                        Path="SAMPLE",
                        MinWidth=35,
                        MaxWidth=80,
                        ColumnType=ColumnTypeRef.Boolean,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Клише",
                        Path="CLICHE",
                        MinWidth=35,
                        MaxWidth=80,
                        ColumnType=ColumnTypeRef.Boolean,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Штанцформа",
                        Path="SHTANTSFORM",
                        MinWidth=35,
                        MaxWidth=80,
                        ColumnType=ColumnTypeRef.Boolean,
                    },
                    
                    new DataGridHelperColumn()
                    {
                        Header="Тендер",
                        Path="TENDERDOC",
                        MinWidth=35,
                        MaxWidth=80,
                        ColumnType=ColumnTypeRef.Boolean,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Карта проезда",
                        Path="ADDRESSCNT",
                        MinWidth=35,
                        MaxWidth=80,
                        ColumnType=ColumnTypeRef.Boolean,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Запрещена",
                        Path="FORBIDDEN",
                        MinWidth=35,
                        MaxWidth=80,
                        ColumnType=ColumnTypeRef.Boolean,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Возврат поддонов",
                        Path="PALLETRETURN",
                        MinWidth=35,
                        MaxWidth=80,
                        ColumnType=ColumnTypeRef.Boolean,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Расход возвратных поддонов",
                        Path="RETURNABLEPALLETCHECK",
                        MinWidth=35,
                        MaxWidth=80,
                        ColumnType=ColumnTypeRef.Boolean,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Дт. фактического окончания производства",
                        Path="PRODUCTIONFINISHACTUALLY",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm",
                        MinWidth=95,
                        Width2=14,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Дт/вр окончания производства",
                        Path="PRODUCTIONFINISH",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm",
                        MinWidth=95,
                        Width2=14,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Дт/вр начала отгрузки по плану",
                        Path="SHIPMENTSTARTPLAN",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm",
                        MinWidth=95,
                        Width2=14,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Примечания",
                        Path="COMMENTS",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=100,
                        Width2=16,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="APPLICATIONTRANSPORTID",
                        Path="APPLICATIONTRANSPORTID",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="DOCUMENT_PRINT_FLAG",
                        Path="DOCUMENT_PRINT_FLAG",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="ONSM_ID",
                        Path="ONSM_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                };
                ShipmentsGrid.SetColumns(columns);
                ShipmentsGrid.UseRowHeader=false;
                // раскраска строк
                ShipmentsGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    // определение цветов фона строк
                    {
                        StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            //создана накладная: зеленый
                            //факт совершения отгрузки -- это запись в таблице naklrashod
                            if(row.CheckGet("APPLICATIONTRANSPORTID").ToInt()!=0)
                            {
                                color = HColor.Green;
                            }

                            //водитель приехал, отгрузка готова: розовый
                            if(
                                row.CheckGet("READY").ToInt() == 1
                                && row.CheckGet("TRANSPORT").ToBool() == true

                            )
                            {
                                color = HColor.VioletPink;
                            }

                            //необходимо провести расход возвратных поддонов
                            if(row.CheckGet("RETURNABLEPALLETCHECK").ToInt()==1)
                            {
                                color = HColor.Pink;
                            }

                            {
                                //отгрузка запрещена: голубой
                                if(row.CheckGet("FINISHED").ToInt()==0)
                                {
                                    color = HColor.Blue;
                                }
                            }

                            // зеленый -- отгрузка совершена. Используется только для отгрузок на СОХ. У отгрузок на СОХ нет naklrashod, поэтому обычная проверка на отгрузку тут не сработает.
                            // nrz.status = 4 => responsible_stock_status = 1 => зеленый(отгрузка совершена)
                            if (row.CheckGet("RESPONSIBLE_STOCK_STATUS").ToInt() == 1)
                            {
                                color = HColor.Green;
                            }

                            // зелёный -- если тип отгрузки - поставка ТМЦ, и отгрузка была привязана к терминалу (заполнено поле driver_log.dttm_entry)
                            if (row.CheckGet("PRODUCTIONTYPE").ToInt() == 8 && !string.IsNullOrEmpty(row.CheckGet("DLDTTMENTRY")))
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

                    // определение цветов шрифта строк
                    {
                        StylerTypeRef.ForegroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";


                            //создана накладная: зеленый
                            if(!string.IsNullOrEmpty(row.CheckGet("DLDTTMENTRY")))
                            {
                                //на терминале: синий
                                if (row.ContainsKey("ATTERMINAL"))
                                {
                                    if(row["ATTERMINAL"].ToInt() == 1)
                                    {
                                        color = HColor.BlueFG;
                                    }
                                }
                            }
                            
                            {
                                //отгрузка запрещена: голубой
                                if(row.CheckGet("FINISHED").ToInt() == 0)
                                {
                                    //не отгружено, установлена цена: зеленый
                                    if(row.ContainsKey("FINISHED") && row.ContainsKey("PRICEIS"))
                                    {
                                        if(row["FINISHED"].ToInt() == 0)
                                        {
                                            if(row["PRICEIS"].ToInt() == 1)
                                            {
                                                color=HColor.GreenFG;
                                            }
                                        }
                                    }
                                }
                            }
                            
                            // Отгрузка совершена, но первичные документы ещё не распечатаны
                            if (row.CheckGet("C").ToInt() > 0 && row.CheckGet("ATTERMINAL").ToInt() == 0 && row.CheckGet("DOCUMENT_PRINT_FLAG").ToInt() == 0)
                            {
                                color = HColor.MagentaFG;
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },
                };

                ShipmentsGrid.SetSorting("_ROWNUMBER");
                ShipmentsGrid.SearchText = SearchText;

                ShipmentsGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

                // контекстное меню
                ShipmentsGrid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    {
                        "Edit",
                        new DataGridContextMenuItem()
                        {
                            Header="Изменить",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                Edit();
                            }
                        }
                    },
                    {
                        "Comment",
                        new DataGridContextMenuItem()
                        {
                            Header="Изменить примечание",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                SetComment();
                            }
                        }
                    },
                    {
                        "UnbindDriver",
                        new DataGridContextMenuItem()
                        {
                            Header="Отвязать водителя",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                UnbindDriver();
                            }
                        }
                    },

                    {
                        "TimeoutReason",
                        new DataGridContextMenuItem()
                        {
                            Header="Причина опоздания",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                SetLateReason();
                            }
                        }
                    },

                    {
                        "EditShipmentTime",
                        new DataGridContextMenuItem()
                        {
                            Header="Изменить время отгрузки",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                EditShipmentTime();
                            }
                        }
                    },

                    { "s1", new DataGridContextMenuItem(){
                        Header="-",
                    }},

                    {
                        "ShipmentInfo",
                        new DataGridContextMenuItem()
                        {
                            Header="Информация об отгрузке",
                            Action=()=>
                            {
                                ShowShipmentInfo();
                            }
                        }
                    },

                    {
                        "ShipmentDocumentList",
                        new DataGridContextMenuItem()
                        {
                            Header="Список документов",
                            Action=()=>
                            {
                                ShipmentDocumentList();
                            }
                        }
                    },

                    {
                        "LoadingHistoryExtend",
                        new DataGridContextMenuItem
                        {
                            Header="История изменения отгрузки",
                            Action=()=>
                            {
                                ShowHistoryExtend();
                            }
                        }
                    },

                    {
                        "LoadingOrder",
                        new DataGridContextMenuItem
                        {
                            Header="Порядок загрузки",
                            Action=()=>
                            {
                                LoadingOrderNew();
                            }
                        }
                    },

                    {
                        "LoadingOrderTwo",
                        new DataGridContextMenuItem
                        {
                            Header="Изменить порядок загрузки",
                            Visible = GetLoadingOrderTwoVisible(),
                            Action=()=>
                            {
                                LoadingOrderTwo();
                            }
                        }
                    },
                    {
                        "TestLoadingOrder",
                        new DataGridContextMenuItem
                        {
                            Tag = "access_mode_full_access",
                            Visible = Central.DebugMode,
                            Header="(Тест схем погрузки)",
                            Action=()=>
                            {
                                TestLoadingOrder();
                            }
                        }
                    },


                    { "s2", new DataGridContextMenuItem(){
                        Header="-",
                    }},

                    {
                        "CreateAutoShipment",
                        new DataGridContextMenuItem()
                        {
                            Header="Автосоздание отгрузки",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                CreateAutoShipment();
                            }
                        }
                    },
                    {
                        "PalletConsumption",
                        new DataGridContextMenuItem()
                        {
                            Header="Расход поддонов",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                PalletConsumption();
                            }
                        }
                    },
                };


                //при выборе строки в гриде, обновляются актуальные действия для записи
                ShipmentsGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem.Count > 0)
                    {
                        UpdateShipmentActions(selectedItem);
                    }
                };

                //данные грида
                ShipmentsGrid.OnLoadItems = LoadShipmentItems;
                ShipmentsGrid.OnFilterItems = FilterShipmentItems;

                ShipmentsGrid.AutoUpdateInterval = ShipmentsGridAutoUpdateInterval;
                ShipmentsGrid.ItemsAutoUpdate = false;

                ShipmentsGrid.Init();
                ShipmentsGrid.Run();

                ShipmentsGrid.Focus();
            }

        }

        private void ShowShipmentInfo()
        {
            if (SelectedShipmentItem != null)
            {
                if (SelectedShipmentItem.ContainsKey("ID"))
                {
                    if (SelectedShipmentItem["ID"].ToInt() != 0)
                    {
                        var h = new ShipmentInformation();
                        h.Id = SelectedShipmentItem["ID"].ToInt();
                        h.Init();
                        h.Open();
                    }
                }
            }
        }

        /// <summary>
        /// обновление записей
        /// </summary>
        public async void LoadShipmentItems()
        {
            InitShipmentsTotals();

            ShipmentsGridToolbar.IsEnabled = false;
            ShipmentsGrid.ShowSplash();
            PositionsGrid.ShowSplash();

            bool resume = true;
            var f = FromDate.Text.ToDateTime();
            var t = ToDate.Text.ToDateTime();

            if (resume)
            {
                if (DateTime.Compare(f, t) > 0)
                {
                    var msg = "Дата начала должна быть меньше даты окончания.";
                    var d = new DialogWindow($"{msg}", "Проверка данных");
                    d.ShowDialog();
                    resume = false;
                }
            }

            if (resume)
            {
                if (DateTime.Compare(f.AddDays(30), t) < 0)
                {
                    var msg = "Интервал между датами не должен превышать 30 дней.";
                    var d = new DialogWindow($"{msg}", "Проверка данных");
                    d.ShowDialog();
                    resume = false;
                }
            }

            if (resume)
            {
                if (PositionsGrid != null)
                {
                    PositionsGrid.ClearItems();
                }

                var p = new Dictionary<string, string>();
                {
                    p.Add("FROM_DATE", FromDate.Text);
                    p.Add("TO_DATE", ToDate.Text);
                    p.Add("TYPE_ID", Types.SelectedItem.Key);
                    p.Add("FACTORY_ID", $"{FactoryId}");
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "Shipment");
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
                        {
                            ShipmentsDS = ListDataSet.Create(result, "ITEMS");
                            ShipmentsGrid.UpdateItems(ShipmentsDS);
                        }
                    }
                }

                Autorun();
            }

            ShipmentsGridToolbar.IsEnabled = true;
            ShipmentsGrid.HideSplash();
            PositionsGrid.HideSplash();

            LoadShipmentsTotals();
        }

        /// <summary>
        /// Получаем колчество отгрузок в текущих производственных сутках, для которых нужно распечатать документы
        /// </summary>
        public async void GetShipmentCountToPrint()
        {
            string dateFrom = "";
            string dateTo = "";

            // Какой диапазон дат пользователи выбрали в интерфейсе для списка отгрузок, за такой диапазон и будем искать закрытые отгрузки
            dateFrom = $"{FromDate.Text}";
            dateTo = $"{ToDate.Text}";

            var p = new Dictionary<string, string>();
            {
                p.Add("FROM_DATE", dateFrom);
                p.Add("TO_DATE", dateTo);
                p.Add("FACTORY_ID", $"{FactoryId}");
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Shipment");
            q.Request.SetParam("Action", "GetCountToPrint");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            ShipmentToPrintCount = 0;
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        ShipmentToPrintCount = ds.Items.First().CheckGet("SHIPMENT_COUNT").ToInt();
                    }
                }
            }

            // Считаем количество отгрузок, для которых нужно распечатать документы
            {
                ShipmentCountToPrintButton.Content = $"{ShipmentCountToPrintText}{ShipmentToPrintCount}";
                if (ShipmentToPrintCount > 0)
                {
                    ShipmentCountToPrintButton.IsEnabled = true;
                }
                else
                {
                    ShipmentCountToPrintButton.IsEnabled = false;
                }

                if (ShipmentPlan != null && ShipmentPlan.ShipmentCountToPrintButton != null)
                {
                    ShipmentPlan.ShipmentCountToPrintButton.Content = ShipmentCountToPrintButton.Content;
                    ShipmentPlan.ShipmentCountToPrintButton.IsEnabled = ShipmentCountToPrintButton.IsEnabled;
                }
                else
                {
                    if (Central.WM.TabItems.FirstOrDefault(x => x.Key == "ShipmentsControl_Plan").Value != null)
                    {
                        ShipmentPlan = (ShipmentPlan)Central.WM.TabItems.FirstOrDefault(x => x.Key == "ShipmentsControl_Plan").Value.Content;
                        if (ShipmentPlan != null && ShipmentPlan.ShipmentCountToPrintButton != null)
                        {
                            ShipmentPlan.ShipmentCountToPrintButton.Content = ShipmentCountToPrintButton.Content;
                            ShipmentPlan.ShipmentCountToPrintButton.IsEnabled = ShipmentCountToPrintButton.IsEnabled;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Запуск работы таймера для вызова функции получения количества отгрузок в текущих производственных сутках, для которых нужно распечатать документы
        /// </summary>
        public void RunGetShipmentCountToPrintTimer()
        {
            if (GetShipmentCountToPrintTimerInterval != 0)
            {
                if (GetShipmentCountToPrintTimer == null)
                {
                    GetShipmentCountToPrintTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0, 0, GetShipmentCountToPrintTimerInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", GetShipmentCountToPrintTimerInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("ShipmentsList_RunGetShipmentCountToPrintTimer", row);
                    }

                    GetShipmentCountToPrintTimer.Tick += (s, e) =>
                    {
                        GetShipmentCountToPrint();
                    };
                }

                if (GetShipmentCountToPrintTimer.IsEnabled)
                {
                    GetShipmentCountToPrintTimer.Stop();
                }

                GetShipmentCountToPrintTimer.Start();
            }
        }

        /// <summary>
        /// фильтрация записей в таблице отгрузок
        /// </summary>
        public void FilterShipmentItems()
        {
            if (ShipmentsGrid.GridItems != null)
            {
                if (ShipmentsGrid.GridItems.Count > 0)
                {
                    //обработка строк
                    foreach (Dictionary<string, string> row in ShipmentsGrid.GridItems)
                    {

                        {
                            //телефон
                            if (row.ContainsKey("PHONE"))
                            {
                                row["PHONE"] = DataFormatter.CellPhone(row["PHONE"]);
                            }

                            row.CheckAdd("STOCKPERCENT", row.CheckGet("PROGRESS"));

                            //готовность к отгрузке
                            var ready = 0;
                            if (row.CheckGet("STATUSID").ToInt() == 5)
                            {
                                if (row.CheckGet("APPLICATIONTRANSPORTID").ToInt() == 0)
                                {
                                    if (row.CheckGet("PROGRESS").ToInt() > 0)
                                    {
                                        ready = 1;
                                    }
                                }
                            }
                            row.CheckAdd("READY", ready.ToString());
                        }
                    }

                    //фильтрация строк

                    //тип
                    /*
                        list.Add("-1","Все типы");
                        list.Add("0","Изделия");
                        list.Add("1","Бумага");

                        SQL: ProductionTypeId=
                            2 бумага
                            * гофра
                     */
                    bool doFilteringByType = false;

                    int type = -1;
                    if (Types.SelectedItem.Key != null)
                    {
                        doFilteringByType = true;
                        type = Types.SelectedItem.Key.ToInt();
                    }


                    //DeliveryTypes
                    /*
                        list.Add("-1","Все виды доставки");
                        list.Add("0","Без самовывоза");
                        list.Add("1","Самовывоз");
                        list.Add("2","Самовывоз без доверенности");

                      
                     */
                    bool doFilteringByDeliveryType = false;

                    int deliveryType = -1;
                    if (DeliveryTypes.SelectedItem.Key != null)
                    {
                        doFilteringByDeliveryType = true;
                        deliveryType = DeliveryTypes.SelectedItem.Key.ToInt();
                    }


                    //ShipmentTypes
                    bool doFilteringByShipmentType = false;

                    int shipmentType = -1;
                    if (ShipmentTypes.SelectedItem.Key != null)
                    {
                        doFilteringByShipmentType = true;
                        shipmentType = ShipmentTypes.SelectedItem.Key.ToInt();
                    }



                    if (
                        doFilteringByType
                        || doFilteringByDeliveryType
                        || doFilteringByShipmentType
                        || HideComplete
                    )
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (var row in ShipmentsGrid.GridItems)
                        {
                            bool includeByType = false;
                            bool includeByDeliveryType = false;
                            bool includeByShipmentType = false;

                            if (doFilteringByType)
                            {
                                includeByType = false;
                                switch (type)
                                {
                                    //-1 Все
                                    default:
                                        includeByType = true;
                                        break;

                                    //Изделия 
                                    case 0:
                                        if (row.ContainsKey("PRODUCTIONTYPE"))
                                        {
                                            if (row["PRODUCTIONTYPE"].ToInt() != 2)
                                            {
                                                includeByType = true;
                                            }
                                        }
                                        break;

                                    //Бумага
                                    case 2:
                                        if (row.ContainsKey("PRODUCTIONTYPE"))
                                        {
                                            if (row["PRODUCTIONTYPE"].ToInt() == 2)
                                            {
                                                includeByType = true;
                                            }
                                        }
                                        break;

                                    //ТМЦ
                                    case 8:
                                        if (row.ContainsKey("PRODUCTIONTYPE"))
                                        {
                                            if (row["PRODUCTIONTYPE"].ToInt() == 8)
                                            {
                                                includeByType = true;
                                            }
                                        }
                                        break;
                                }
                            }

                            if (doFilteringByDeliveryType)
                            {
                                includeByDeliveryType = false;
                                switch (deliveryType)
                                {
                                    //-1 Все
                                    default:
                                        includeByDeliveryType = true;
                                        break;

                                    //Без самовывоза 
                                    case 0:
                                        if (row.ContainsKey("EXPORTACCEPTED"))
                                        {
                                            if (row["EXPORTACCEPTED"].ToInt() != 1)
                                            {
                                                includeByDeliveryType = true;
                                            }
                                        }
                                        break;

                                    //Самовывоз 
                                    case 1:
                                        if (row.ContainsKey("EXPORTACCEPTED"))
                                        {
                                            if (row["EXPORTACCEPTED"].ToInt() == 1)
                                            {
                                                includeByDeliveryType = true;
                                            }
                                        }
                                        break;

                                    //Самовывоз без доверенности 
                                    case 2:
                                        var a = false;
                                        if (row.ContainsKey("EXPORTACCEPTED"))
                                        {
                                            if (row["EXPORTACCEPTED"].ToInt() == 1)
                                            {
                                                a = true;
                                            }
                                        }

                                        var b = false;
                                        if (row.ContainsKey("ATTORNEYLETTER"))
                                        {
                                            if (row["ATTORNEYLETTER"].ToInt() == 0)
                                            {
                                                b = true;
                                            }
                                        }

                                        if (a && b)
                                        {
                                            includeByDeliveryType = true;
                                        }
                                        break;
                                }
                            }

                            if (doFilteringByShipmentType)
                            {

                                //FIXME: naming: C

                                includeByShipmentType = false;
                                switch (shipmentType)
                                {
                                    //-1 Все
                                    default:
                                        includeByShipmentType = true;
                                        break;

                                    //Неразрешенные 
                                    case 1:
                                        {
                                            var a = false;
                                            if (row.ContainsKey("FINISHED"))
                                            {
                                                if (row["FINISHED"].ToInt() == 0)
                                                {
                                                    a = true;
                                                }
                                            }

                                            var b = false;
                                            if (row.ContainsKey("C"))
                                            {
                                                if (row["C"].ToInt() == 0)
                                                {
                                                    b = true;
                                                }
                                            }

                                            if (a && b)
                                            {
                                                includeByShipmentType = true;
                                            }
                                        }
                                        break;

                                    //Разрешенные (не голубой)
                                    case 2:
                                        {
                                            var a = false;
                                            if (row.ContainsKey("FINISHED"))
                                            {
                                                if (row["FINISHED"].ToInt() == 1)
                                                {
                                                    a = true;
                                                }
                                            }

                                            var b = false;
                                            if (row.ContainsKey("C"))
                                            {
                                                if (row["C"].ToInt() == 0)
                                                {
                                                    b = true;
                                                }
                                            }

                                            if (a && b)
                                            {
                                                includeByShipmentType = true;
                                            }
                                        }
                                        break;

                                    //Неотгруженные (незеленый + оранжевый string.IsNullOrEmpty(x["DlDttmEntry"]) + на терминале)
                                    case 3:
                                        if (row.ContainsKey("C"))
                                        {
                                            if (row["C"].ToInt() == 0 || (row["C"].ToInt() > 0 && row["ATTERMINAL"].ToInt() == 1))
                                            {
                                                includeByShipmentType = true;
                                            }
                                        }
                                        break;

                                    //транспорт
                                    case 4:
                                        if (row.CheckGet("TRANSPORT").ToInt() == 1)
                                        {
                                            includeByShipmentType = true;
                                        }
                                        break;

                                    // На терминале
                                    case 5:
                                        if (row.CheckGet("ATTERMINAL").ToInt() == 1)
                                        {
                                            includeByShipmentType = true;
                                        }
                                        break;

                                    //Отгруженные
                                    case 6:
                                        if (row.ContainsKey("C"))
                                        {
                                            if (row["C"].ToInt() > 0 && row["ATTERMINAL"].ToInt() == 0)
                                            {
                                                includeByShipmentType = true;
                                            }
                                        }
                                        break;

                                    // На печать
                                    case 7:
                                        if (row.CheckGet("C").ToInt() > 0 && row.CheckGet("ATTERMINAL").ToInt() == 0 && row.CheckGet("DOCUMENT_PRINT_FLAG").ToInt() == 0)
                                        {
                                            includeByShipmentType = true;
                                        }
                                        break;

                                    //возврат поддонов
                                    case 8:
                                        if (row.CheckGet("PALLETRETURN").ToInt() == 1)
                                        {
                                            includeByShipmentType = true;
                                        }
                                        break;

                                    //расход возвратных поддонов
                                    case 9:
                                        if (row.CheckGet("RETURNABLEPALLETCHECK").ToInt() == 1)
                                        {
                                            includeByShipmentType = true;
                                        }
                                        break;

                                    //автоотгрузка
                                    case 10:

                                        if (row["C"].ToInt() > 0)
                                        {
                                            if (
                                                row.CheckGet("EXPORTACCEPTED").ToInt() == 1
                                                && row.CheckGet("ATTERMINAL").ToInt() == 0
                                            )
                                            {
                                                includeByShipmentType = true;
                                            }
                                        }
                                        break;

                                    //поставка ТМЦ
                                    case 11:

                                        if (
                                            row.CheckGet("PRODUCTIONTYPE").ToInt() == 8
                                        )
                                        {
                                            includeByShipmentType = true;
                                        }
                                        break;
                                }
                            }

                            //скрыть отгруженные
                            if (HideComplete)
                            {
                                if (!string.IsNullOrEmpty(row.CheckGet("DLDTTMENTRY")))
                                {
                                    includeByShipmentType = false;
                                }
                            }


                            if (
                                includeByType
                                && includeByDeliveryType
                                && includeByShipmentType
                            )
                            {
                                items.Add(row);
                            }
                        }
                        ShipmentsGrid.GridItems = items;


                        /*
                            если есть позиции отгрузки в гриде
                            проверим, есть ли соотв. отгрузка в списке отгрузок
                            (к которой относятся эти позиции)
                            если нет, очистим список позиций
                         */
                        if (PositionsDS != null && ShipmentsDS != null)
                        {
                            if (PositionsDS.Items.Count > 0)
                            {
                                if (ShipmentId != 0)
                                {
                                    if (ShipmentsDS.Items.Count > 0)
                                    {
                                        var exists = false;
                                        foreach (Dictionary<string, string> item in items)
                                        {
                                            if (item.CheckGet("ID").ToInt() == ShipmentId)
                                            {
                                                exists = true;
                                            }
                                        }
                                        if (!exists)
                                        {
                                            PositionsGrid.ClearItems();
                                        }
                                    }
                                }
                            }
                        }

                    }

                }
                else
                {
                    SelectedShipmentItem = new Dictionary<string, string>();
                    UpdateShipmentActions(SelectedShipmentItem);
                }



            }

        }


        /// <summary>
        /// итоги под гридом Отгрузки (верхний грид)
        /// </summary>
        public async void LoadShipmentsTotals()
        {
            bool complete = false;
            ListDataSet totalsDS = new ListDataSet();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Shipment");
            q.Request.SetParam("Action", "GetTotals");

            q.Request.SetParam("FromDate", FromDate.Text);
            q.Request.SetParam("ToDate", ToDate.Text);
            q.Request.SetParam("FACTORY_ID", $"{FactoryId}");

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
                    totalsDS = ListDataSet.Create(result, "TOTALS");
                    if (totalsDS.Items.Count > 0)
                    {
                        complete = true;
                    }
                }
            }

            if (complete)
            {
                InitShipmentsTotals(totalsDS);
            }
            else
            {
                InitShipmentsTotals();
            }
        }

        /// <summary>
        /// обновление методов работы с выбранной записью в таблице отгрузок
        /// </summary>
        /// <param name="selectedItem"></param>
        public void UpdateShipmentActions(Dictionary<string, string> selectedItem)
        {
            SelectedShipmentItem = selectedItem;

            BindDriverButton.IsEnabled = false;
            PrintButton.IsEnabled = false;
            PrintDriverBootCardButton.IsEnabled = false;
            PrintMapButton.IsEnabled = false;
            PrintRouteMapsButton.IsEnabled = false;
            PrintProxyDocsButton.IsEnabled = false;
            PrintAllButton.IsEnabled = false;
            BindTerminalButton.IsEnabled = true;
            ResponsibleStockDocumentsButton.IsEnabled = false;
            ShipmentDocumentListButton.IsEnabled = false;

            // видимость пунктов в выпадающем меню
            // изначально все недостуно
            ShipmentsGrid.Menu["CreateAutoShipment"].Enabled = false;
            ShipmentsGrid.Menu["PalletConsumption"].Enabled = false;
            ShipmentsGrid.Menu["LoadingOrder"].Enabled = false;
            ShipmentsGrid.Menu["Edit"].Enabled = false;
            ShipmentsGrid.Menu["EditShipmentTime"].Enabled = false;
            ShipmentsGrid.Menu["LoadingOrderTwo"].Enabled = false;
            ShipmentsGrid.Menu["ShipmentDocumentList"].Enabled = false;
            ShipmentsGrid.Menu["ShipmentInfo"].Enabled = false;

            ShipmentId = 0;

            if (SelectedShipmentItem != null)
            {
                if (SelectedShipmentItem.ContainsKey("ID"))
                {
                    if (SelectedShipmentItem["ID"].ToInt() != 0)
                    {
                        ShipmentId = SelectedShipmentItem["ID"].ToInt();

                        ShipmentsGrid.Menu["Edit"].Enabled = true;

                        if (SelectedShipmentItem["PRODUCTIONTYPE"].ToInt() != 8)
                        {
                            ShipmentsGrid.Menu["LoadingOrder"].Enabled = true;
                            ShipmentsGrid.Menu["ShipmentDocumentList"].Enabled = true;
                            ShipmentsGrid.Menu["ShipmentInfo"].Enabled = true;

                            ShipmentDocumentListButton.IsEnabled = true;

                            PrintButton.IsEnabled = true;
                        }
                        

                        //при выборе отгрузки посмотрим на тип продукции
                        //в гриде "позиции" будет показан набор полей под соотв. тип
                        //ProductionType 2=бумага, *=изделия
                        var productionType = 2;
                        if (SelectedShipmentItem.ContainsKey("PRODUCTIONTYPE"))
                        {
                            productionType = SelectedShipmentItem["PRODUCTIONTYPE"].ToInt();
                        }
                        ProductionType = productionType;

                        //отгруженные, на терминале, зеленый
                        if (!string.IsNullOrEmpty(SelectedShipmentItem.CheckGet("DLDTTMENTRY")))
                        {
                            BindDriverButton.IsEnabled = false;
                        }
                        else
                        {
                            BindDriverButton.IsEnabled = true;

                            // Помимо отгрузки проверяем выбранного водителя, прежде чем давать доступ к привязке водителя к отгрузке
                            if (SelectedDriverItem != null)
                            {
                                if (!string.IsNullOrEmpty(SelectedDriverItem.CheckGet("ENTRYDATE")))
                                {
                                    BindDriverButton.IsEnabled = false;
                                }
                            }
                        }
                        
                        //отгрузка запрещена: голубой
                        if (SelectedShipmentItem.CheckGet("FINISHED").ToInt() == 0)
                        {
                            BindTerminalButton.IsEnabled = false;
                        }

                        PrintDriverBootCardButton.IsEnabled = true;
                        PrintMapButton.IsEnabled = true;
                        PrintAllButton.IsEnabled = true;

                        if (
                            SelectedShipmentItem.CheckGet("ATTORNEYLETTER").ToBool()
                            || SelectedShipmentItem.CheckGet("TENDERDOC").ToBool()
                        )
                        {
                            PrintProxyDocsButton.IsEnabled = true;
                        }


                        if (SelectedShipmentItem.ContainsKey("ADDRESSCNT"))
                        {
                            if (SelectedShipmentItem["ADDRESSCNT"].ToInt() > 0)
                            {
                                PrintRouteMapsButton.IsEnabled = true;
                            }
                        }

                        if (
                            SelectedShipmentItem.ContainsKey("C")
                            && SelectedShipmentItem.ContainsKey("EXPORTACCEPTED")
                            && SelectedShipmentItem.ContainsKey("ATTERMINAL")
                        )
                        {
                            if (
                                SelectedShipmentItem["C"].ToInt() > 0
                                && SelectedShipmentItem["EXPORTACCEPTED"].ToInt() == 1
                                && SelectedShipmentItem["ATTERMINAL"].ToInt() == 0
                            )
                            {
                                if (SelectedShipmentItem["PRODUCTIONTYPE"].ToInt() != 8)
                                {
                                    ShipmentsGrid.Menu["CreateAutoShipment"].Enabled = true;
                                }
                            }
                        }


                        if (
                            SelectedShipmentItem.CheckGet("C").ToInt() > 0
                            && SelectedShipmentItem.CheckGet("RETURNABLE_PDN_POK").ToInt() == 1
                            && SelectedShipmentItem.CheckGet("ATTERMINAL").ToInt() == 0
                        )
                        {
                            if (SelectedShipmentItem["PRODUCTIONTYPE"].ToInt() != 8)
                            {
                                ShipmentsGrid.Menu["PalletConsumption"].Enabled = true;
                            }
                        }

                        if (Central.DebugMode)
                        {
                            //ShipmentsGrid.Menu["CreateAutoShipment"].Enabled = true;
                        }

                        // Если выбрана отгрузка на сох и отгрузка не на терминала,
                        // и есть запись по перемещению поддонов для поставки на сох (отгрузка была привязана к терминалу)
                        if (SelectedShipmentItem.CheckGet("PRODUCTIONTYPE").ToInt() == 5 
                            && SelectedShipmentItem.CheckGet("ATTERMINAL").ToInt() == 0
                            && SelectedShipmentItem.CheckGet("ONSM_ID").ToInt() > 0)
                        {
                            ResponsibleStockDocumentsButton.IsEnabled = true;
                        }

                        if (!(SelectedShipmentItem.CheckGet("C").ToInt() > 0 && SelectedShipmentItem.CheckGet("ATTERMINAL").ToInt() == 0))
                        {
                            ShipmentsGrid.Menu["EditShipmentTime"].Enabled = true;

                            if (SelectedShipmentItem["PRODUCTIONTYPE"].ToInt() != 8)
                            {
                                ShipmentsGrid.Menu["LoadingOrderTwo"].Enabled = true;
                            }
                        }
                    }
                }
            }

            if (Central.DebugMode)
            {
                ShipmentsGrid.Menu["LoadingOrderTwo"].Enabled = true;
            }

            CheckTerminalActions();

            PositionsGridInit();
            PositionsGrid.ClearItems();
            LoadPositionItems();

            ProcessPermissions();
        }

        /// <summary>
        /// Инициализация таблицы водителей
        /// </summary>
        public void DriversGridInit()
        {
            //грид2, список водителей
            {
                var columns = new List<DataGridHelperColumn>()
                {
                    new DataGridHelperColumn
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=50,
                        Width2=4,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="ИД",
                        Path="ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=50,
                        Width2=6,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Приезд",
                        Path="ARRIVEDATE",
                        Format="dd.MM HH:mm",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width=70,
                        Width2=8,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // перенесённая отгрузка
                                    if (row.CheckGet("UNSHIPPED").ToInt() > 0)
                                    {
                                        color = HColor.Orange;
                                    }
                                    // опоздавшая отгрузка
                                    else if (row.CheckGet("LATE").ToInt() > 0)
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
                    new DataGridHelperColumn()
                    {
                        Header="Въезд",
                        Path="ENTRYDATE",
                        Format="dd.MM HH:mm",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width=70,
                        Width2=8,
                    },

                    new DataGridHelperColumn()
                    {
                        Header="Терминал",
                        Path="TERMINALNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=4,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Водитель",
                        Path="DRIVERNAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=80,
                        Width2=20,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // Флаг того, что водитель зарегистрировался удалённо (через сайт)
                                    if (row.CheckGet("REMOTE_REGISTRATION_FLAG").ToInt() > 0)
                                    {
                                        color = HColor.VioletDark; //"#FF9400D3";
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
                    new DataGridHelperColumn()
                    {
                        Header="Автомобиль",
                        Path="CAR",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=100,
                        MaxWidth=250,
                        Width2=20,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Телефон",
                        Path="DRIVERPHONE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=90,
                        MaxWidth=110,
                        Width2=11,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Покупатель",
                        Path="BUYER",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=80,
                        Width2=18,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Паспорт",
                        Path="PASSPORT",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=50,
                        Width2=16,
                    },

                    new DataGridHelperColumn()
                    {
                        Header="ИД водителя",
                        Path="DRIVERID",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Не привязан",
                        Path="TRANSPORTID",
                        Doc="Водитель не привязан к отгрузке",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Удалённая регистрация",
                        Path="REMOTE_REGISTRATION_FLAG",
                        Doc="Флаг того, что водитель зарегистрировался удалённо (через сайт)",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                };
                DriversGrid.SetColumns(columns);
                DriversGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    {
                        StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            //водитель въехал
                            if (row.ContainsKey("ENTRYDATE"))
                            {
                                if(!string.IsNullOrEmpty(row["ENTRYDATE"]))
                                {
                                    color = HColor.Green;
                                }
                            }

                            //отгрузка не привязана
                            if(row.CheckGet("TRANSPORTID").ToInt()==0)
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

                DriversGrid.PrimaryKey = "_ROWNUMBER";
                DriversGrid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);

                DriversGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
                DriversGrid.Name = "shipment_list_driver";

                //при выборе строки в гриде, обновляются актуальные действия для записи
                DriversGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem.Count > 0)
                    {
                        UpdateDriverActions(selectedItem);
                    }
                };

                //двойной клик на строке откроет форму редактирования
                DriversGrid.OnDblClick = selectedItem =>
                {
                    if (Central.Navigator.GetRoleLevel(this.RoleName) > Role.AccessMode.ReadOnly)
                    {
                        EditDriver();
                    }
                };

                //данные грида
                DriversGrid.OnLoadItems = LoadDriverItems;
                DriversGrid.OnFilterItems = FilterDriverItems;

                DriversGrid.AutoUpdateInterval = DriversGridAutoUpdateInterval;
                DriversGrid.ItemsAutoUpdate = false;

                //ShipmentsGrid.Register("ShipmentsControl_List");
                DriversGrid.Init();
                DriversGrid.Run();

                DriversGrid.Focus();
            }
        }

        /// <summary>
        /// обновление записей в таблице водителей
        /// </summary>
        public async void LoadDriverItems()
        {
            DriversGridToolbar.IsEnabled = false;
            DriversGrid.ShowSplash();

            bool resume = true;

            if (resume)
            {
                var f = FromDate.Text.ToDateTime();
                var t = ToDate.Text.ToDateTime();
                if (DateTime.Compare(f, t) > 0)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();
                p.Add("FACTORY_ID", $"{FactoryId}");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments/ShipmentKsh");
                q.Request.SetParam("Object", "TransportDriver");
                q.Request.SetParam("Action", "List");
                q.Request.SetParams(p);

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
                        {
                            DriversDS = ListDataSet.Create(result, "ITEMS");
                            DriversGrid.UpdateItems(DriversDS);
                        }
                    }
                }
            }

            DriversGridToolbar.IsEnabled = true;
            DriversGrid.HideSplash();
        }

        /// <summary>
        /// фильтрация записей в таблице водителей
        /// </summary>
        public void FilterDriverItems()
        {
            if (DriversGrid.GridItems != null)
            {
                if (DriversGrid.GridItems.Count > 0)
                {
                    //фильтрация строк
                    foreach (Dictionary<string, string> row in DriversGrid.GridItems)
                    {
                        {
                            //телефон
                            if (row.ContainsKey("DRIVERPHONE"))
                            {
                                row["DRIVERPHONE"] = DataFormatter.CellPhone(row["DRIVERPHONE"]);
                            }
                        }
                    }

                }
            }
        }

        /// <summary>
        /// обновление методов работы с выбранной записью в таблице водителей
        /// </summary>
        /// <param name="selectedItem">выбранная строка водителя</param>
        public void UpdateDriverActions(Dictionary<string, string> selectedItem)
        {
            SelectedDriverItem = selectedItem;

            /*
                Белый водитель -- привязан к отгрузке
                Синий -- не привязан
                Зеленый -- въехал
        
                Добавить -- покажет интерфейс: 
                    ожидаемые водители:
                        отметить
                    все водители
                        отметить, добавить

                Изменить
                    для белых водителей поле "покупатель" ридонли
                    для синих может быть изменено

                Удалить
                    удалить можно только тех, кто не въехал

                Отметить убытие
                    можно только тех, кто не въехал

                Отменить въезд
                    только для тех, кто кто не привязан к терминалу

                Показать отгрузку 
                    (только для белых и зеленых)
            */

            AddButton.IsEnabled = true;
            EditButton.IsEnabled = false;
            DeleteButton.IsEnabled = false;
            MarkDepartureButton.IsEnabled = false;
            CancelEntryButton.IsEnabled = false;
            ShowShipmentButton.IsEnabled = false;
            PrintButton2.IsEnabled = true;

            BindDriverButton.IsEnabled = true;

            if (SelectedDriverItem != null)
            {
                if (SelectedDriverItem.ContainsKey("ID"))
                {
                    if (SelectedDriverItem["ID"].ToInt() != 0)
                    {
                        EditButton.IsEnabled = true;

                        if (SelectedDriverItem.ContainsKey("ENTRYDATE"))
                        {
                            if (!string.IsNullOrEmpty(SelectedDriverItem["ENTRYDATE"]))
                            {
                                //въехал
                                if (SelectedDriverItem.ContainsKey("TERMINALNUMBER"))
                                {
                                    if (string.IsNullOrEmpty(SelectedDriverItem["TERMINALNUMBER"]))
                                    {
                                        //не привязан к терминалу
                                        MarkDepartureButton.IsEnabled = true;
                                        CancelEntryButton.IsEnabled = true;
                                    }
                                }

                                BindDriverButton.IsEnabled = false;

                            }
                            else
                            {
                                //не въехал
                                DeleteButton.IsEnabled = true;
                                MarkDepartureButton.IsEnabled = true;

                                // Помимо водителя проверяем ещё и отгрузку, прежде чем давать доступ к привязке водителя к отгрузке
                                if (SelectedShipmentItem != null)
                                {
                                    if (!string.IsNullOrEmpty(SelectedShipmentItem.CheckGet("DLDTTMENTRY")))
                                    {
                                        BindDriverButton.IsEnabled = false;
                                    }
                                }
                            }
                        }


                        if (SelectedDriverItem.ContainsKey("TRANSPORTID"))
                        {
                            if (!string.IsNullOrEmpty(SelectedDriverItem["TRANSPORTID"]))
                            {
                                //привязан к отгрузке
                                ShowShipmentButton.IsEnabled = true;
                            }
                        }

                    }
                }
            }

            ProcessPermissions();
        }

        /// <summary>
        /// Инициализация таблицы с товарными позициями в отгрузке
        /// </summary>
        public void PositionsGridInit()
        {
            //грид3, позиции
            {
                //список колонок грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Комплект",
                        Path="POSITIONNUM",
                        ColumnType=ColumnTypeRef.String,
                        Width=30,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД Заявки",
                        Path="APPLICATIONID",
                        ColumnType=ColumnTypeRef.String,
                        Width=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул",
                        Path="VENDORCODE",
                        ColumnType=ColumnTypeRef.String,
                        Width=120,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="PRODUCTNAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Диаметр рулона",
                        Path="ROLLDIAMETER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ограничение количества",
                        Path="QUANTITYLIMIT",
                        ColumnType=ColumnTypeRef.String,
                        Width = 60
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Поддонов по заявке, шт.",
                        Path="PALLETQUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=40,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество в заявке",
                        Path="QUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=40,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Рулонов",
                        Path="ROLLQUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Склад под отгр.",
                        Path="INSTOCKQUANTITYAPPLICATION",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Склад всего",
                        Path="INSTOCKQUANTITYTOTAL",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Отгружено",
                        Path="SHIPPEDQUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Отклонение, %",
                        Path="QUANTITY_PERCENTAGE_DEVIATION",
                        ColumnType=ColumnTypeRef.Double,
                        Width=50,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // Если позиция отгружена
                                    if (row.CheckGet("STATUSID").ToInt() == 0)
                                    {
                                        // процентное отклонение отгруженного количества к количеству по заявке превышает допустимые значения
                                        if (!PercentageDeviation.CheckPercentageDeviation(row.CheckGet("QUANTITY").ToInt(), row["QUANTITY_PERCENTAGE_DEVIATION"].ToDouble()))
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
                    new DataGridHelperColumn()
                    {
                        Header="Вес брутто по заявке, кг.",
                        Path="WEIGHT_GROSS",
                        ColumnType=ColumnTypeRef.Double,
                        Width=60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус",
                        Path="STATUS",
                        ColumnType=ColumnTypeRef.String,
                        Width=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Цена без НДС",
                        Path="PRICE",
                        ColumnType=ColumnTypeRef.Double,
                        Width=55,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Цена с НДС",
                        Path="PRICEVAT",
                        ColumnType=ColumnTypeRef.Double,
                        Width=55,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Ед. измерения",
                        Path="UNITOFMEASUREMENT",
                        ColumnType=ColumnTypeRef.String,
                        Width=40,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Схема производства",
                        Path="PRODUCTIONSCHEME",
                        ColumnType=ColumnTypeRef.String,
                        Width=50,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Упаковка",
                        Path="PACKAGING",
                        ColumnType=ColumnTypeRef.String,
                        Width=50,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Покупатель",
                        Path="CUSTOMER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width = 80,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Адрес доставки",
                        Path="DELIVERYADDRESS",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=100,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Примеч. кладовщику",
                        Path="STOREKEEPERNOTE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=40,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Примеч. грузчику",
                        Path="PORTERNOTE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=40,
                    },
                };

                //подстроим набор колонок в зависимости от типа продукции выбранной отгрузки
                //ProductionType 2=бумага, *=изделия
                List<string> hiddenColumns = new List<string>();
                switch (ProductionType)
                {
                    //бумага (рулоны)
                    case 2:
                        hiddenColumns.Add("VENDORCODE");
                        hiddenColumns.Add("QUANTITYLIMIT");
                        //hiddenColumns.Add("SQUARE");
                        hiddenColumns.Add("PRODUCTIONSCHEME");
                        hiddenColumns.Add("PACKAGING");
                        hiddenColumns.Add("PALLETQUANTITY");
                        break;

                    //изделия
                    default:
                        hiddenColumns.Add("ROLLDIAMETER");
                        hiddenColumns.Add("ROLLQUANTITY");
                        break;
                }
                foreach (DataGridHelperColumn c in columns)
                {
                    var k = c.Path;
                    if (hiddenColumns.Contains(k))
                    {
                        c.Hidden = true;
                    }
                }

                PositionsGrid.SetColumns(columns);
                PositionsGrid.SetSorting("ID", ListSortDirection.Descending);
                PositionsGrid.Init();

                {
                    var positionsGridColumnPositionListParameter = Central.User.UserParameterList.FirstOrDefault(x => x.Interface == this.GetType().Name && x.Name == $"PositionsGridColumnPositionList_{ProductionType}");
                    if (positionsGridColumnPositionListParameter != null)
                    {
                        string columnPositionData = positionsGridColumnPositionListParameter.Value;
                        var columnPositionDataItems = columnPositionData.Split(';');
                        if (columnPositionDataItems != null && columnPositionDataItems.Length > 0)
                        {
                            foreach (var columnPosition in columnPositionDataItems)
                            {
                                var positionData = columnPosition.Split(':');
                                if (positionData != null && positionData.Length == 2)
                                {
                                    var column = PositionsGrid.Grid.Columns.FirstOrDefault(x => x.SortMemberPath == positionData[0]);
                                    if (column != null)
                                    {
                                        column.DisplayIndex = positionData[1].ToInt();
                                    }
                                }
                            }
                        }
                    }
                }

                //при выборе строки в гриде, обновляются актуальные действия для записи
                PositionsGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem.Count > 0)
                    {

                    }
                };

                PositionsGrid.OnLoadItems = LoadPositionItems;

                PositionsGrid.Grid.ColumnReordered += Grid_ColumnReordered;
            }
        }

        private void Grid_ColumnReordered(object sender, DataGridColumnEventArgs e)
        {
            var columnList = PositionsGrid.Grid.Columns.OrderBy(x => x.DisplayIndex).ToList();
            string columnPositionData = "";
            foreach (var column in columnList)
            {
                columnPositionData = $"{columnPositionData}{column.SortMemberPath}:{column.DisplayIndex};";
            }

            var positionsGridColumnPositionListParameter = Central.User.UserParameterList.FirstOrDefault(x => x.Interface == this.GetType().Name && x.Name == $"PositionsGridColumnPositionList_{ProductionType}");
            if (positionsGridColumnPositionListParameter != null)
            {
                positionsGridColumnPositionListParameter.Value = columnPositionData;
            }
            else
            {
                positionsGridColumnPositionListParameter = new UserParameter(this.GetType().Name, $"PositionsGridColumnPositionList_{ProductionType}", columnPositionData, "Позиции колонок грида позиций отгрузки");
                Central.User.UserParameterList.Add(positionsGridColumnPositionListParameter);
            }
        }

        /// <summary>
        /// обновление записей в таблице товарных позиций
        /// </summary>
        public async void LoadPositionItems()
        {
            PositionsGrid.ShowSplash();

            bool resume = true;

            if (resume)
            {
                if (ShipmentId == 0)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                var f = FromDate.Text.ToDateTime();
                var t = ToDate.Text.ToDateTime();
                if (DateTime.Compare(f, t) > 0)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();

                {
                    p.Add("SHIPMENT_ID", ShipmentId.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "Position");
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
                    var t = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (t != null)
                    {
                        PositionsDS = ListDataSet.Create(t, "ITEMS");
                        PositionsGrid.UpdateItems(PositionsDS);
                    }
                }
            }

            PositionsGrid.HideSplash();
        }

        /// <summary>
        /// Инициализация таблицы терминалов
        /// </summary>
        public void TerminalsGridInit()
        {
            //грид4, терминалы
            {
                //список колонок грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="№ терминала",
                        Path="TERMINAL_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Терминал",
                        Path="TERMINAL_NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=40,
                        MaxWidth=45,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Покупатель",
                        Path="BUYER_NAME",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=100,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // Отгрузка заблокирована из-за несоответствия габаритов транспорта
                                    if (row.CheckGet("SHPMENT_BLOCKED_FLAG").ToInt() > 0)
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
                        Header="Перевозчик",
                        Path="DRIVER_NAME",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Автомобиль",
                        Path="CAR",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Постановка",
                        Path="BIND_DATE",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                        Format="dd.MM HH:mm",
                        MinWidth=70,
                        MaxWidth=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Погрузчик",
                        Path="FORKLIFTDRIVER_NAME",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Все" +
                        $"{Environment.NewLine}Да - Показать погрузчику все поддоны" +
                        $"{Environment.NewLine}Неполные - Показать погрузчику все неполные поддоны" +
                        $"{Environment.NewLine}Нет - Не показывать погрузчику все поддоны",
                        Description="Показать погрузчику все поддоны",
                        Path="SHOW_ALL_PALLET_FLAG_VALUE",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=35,
                        MaxWidth=40,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Все{Environment.NewLine}Показать погрузчику все поддоны",
                        Description="Показать погрузчику все поддоны",
                        Path="SHOW_ALL_PALLET_FLAG",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width=0,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Отгрузка заблокирована{Environment.NewLine}Отгрузка заблокирована из-за несоответствия габаритов транспорта",
                        Description="Отгрузка заблокирована из-за несоответствия габаритов транспорта",
                        Path="SHPMENT_BLOCKED_FLAG",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width=0,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="",
                        Path="FORKLIFTDRIVER_ID",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width=0,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="",
                        Path="TERMINAL_STATUS",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width=0,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="",
                        Path="KIND",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width=0,
                        Hidden=true,
                    },
                };

                TerminalsGrid.SetColumns(columns);
                TerminalsGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    {
                        StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            //нет отгрузки -- голубой
                            //есть отгрузка
                            //    Статус привязки к терминалу: 
                            //        0 - отгрузка, 
                            //        1 - внешняя перестройка -- желтый
                            //            дополнительные операции по подготовке транспорта
                            
                            if(string.IsNullOrEmpty(row.CheckGet("BUYER_NAME")))
                            {
                                color=HColor.Blue;
                            }

                            if(row.CheckGet("TERMINAL_STATUS").ToInt()==1)
                            {
                                color=HColor.Yellow;
                            }

                            if (row.CheckGet("BLOCKED_FLAG") == "1")
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

                TerminalsGrid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);

                // контекстное меню
                TerminalsGrid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    {
                        "Blocked",
                        new DataGridContextMenuItem()
                        {
                            Header="Заблокировать",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                BlockedTerminal();
                            }
                        }
                    },
                    {
                        "UnBlocked",
                        new DataGridContextMenuItem()
                        {
                            Header="Разблокировать",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                UnBlockedTerminal();
                            }
                        }
                    },
                    { "s1", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    {
                        "SetShowAllPalletFlag",
                        new DataGridContextMenuItem()
                        {
                            Header="Показать погрузчику все поддоны",
                            Tag = "access_mode_special",
                            Action=()=>
                            {
                                SetShowPalletFlag(1);
                            }
                        }
                    },
                    {
                        "SetShowIncompletePalletFlag",
                        new DataGridContextMenuItem()
                        {
                            Header="Показать погрузчику все неполные поддоны",
                            Tag = "access_mode_special",
                            Action=()=>
                            {
                                SetShowPalletFlag(2);
                            }
                        }
                    },
                    { "s2", new DataGridContextMenuItem(){
                        Header="-",
                    }},
                    {
                        "RemoveShipmentBlockFlag",
                        new DataGridContextMenuItem()
                        {
                            Header="Снять блокировку отгрузки из-за несоответствия габаритов ТС",
                            Tag = "access_mode_special",
                            Action=()=>
                            {
                                RemoveShipmentBlockFlag();
                            }
                        }
                    },
                };

                //при выборе строки в гриде, обновляются актуальные действия для записи
                TerminalsGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem.Count > 0)
                    {
                        UpdateTerminalActions(selectedItem);
                    }
                };

                TerminalsGrid.OnLoadItems = LoadTerminalItems;
                TerminalsGrid.OnFilterItems = FilterTerminalItems;

                TerminalsGrid.AutoUpdateInterval = TerminalsGridAutoUpdateInterval;
                TerminalsGrid.ItemsAutoUpdate = false;

                //ShipmentsGrid.Register("ShipmentsControl_List");
                TerminalsGrid.Init();
                TerminalsGrid.Run();
            }
        }

        public async void SetShowPalletFlag(int type)
        {
            var p = new Dictionary<string, string>();
            {
                p.Add("TERMINAL_ID", SelectedTerminalItem.CheckGet("TERMINAL_ID"));
                p.Add("SHOW_ALL_PALLET_FLAG", $"{type}");
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Terminal");
            q.Request.SetParam("Action", "UpdateShowAllPalletFlag");

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
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        if (ds.Items.First().CheckGet("TERMINAL_ID").ToInt() > 0)
                        {
                            TerminalsGrid.LoadItems();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Снять флаг блокировки отгрузки из-за несоответствия габаритов транспортного средства
        /// </summary>
        public async void RemoveShipmentBlockFlag()
        {
            var p = new Dictionary<string, string>();
            {
                p.Add("TERMINAL_ID", SelectedTerminalItem.CheckGet("TERMINAL_ID"));
                p.Add("SHPMENT_BLOCKED_FLAG", "0");
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Terminal");
            q.Request.SetParam("Action", "SetShpmentBlockedFlag");

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
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        if (ds.Items.First().CheckGet("TERMINAL_ID").ToInt() > 0)
                        {
                            TerminalsGrid.LoadItems();
                        }
                    }
                }
            }
        }

        public void BlockedTerminal()
        {
            int flag = 1;
            UpdateTerminalBlockedFlag(flag);
        }

        public void UnBlockedTerminal()
        {
            int flag = 0;
            UpdateTerminalBlockedFlag(flag);
        }

        public async void UpdateTerminalBlockedFlag(int flag)
        {
            var p = new Dictionary<string, string>();
            p.CheckAdd("BLOCKED_FLAG", flag.ToString());
            p.CheckAdd("ID_TER", SelectedTerminalItem.CheckGet("TERMINAL_ID"));

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Terminal");
            q.Request.SetParam("Action", "SetBlockedFlag");

            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            int operationResult = 0;

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    if (result.ContainsKey("ITEMS"))
                    {
                        var formDS = result["ITEMS"];
                        formDS?.Init();
                        operationResult = formDS.GetFirstItemValueByKey("RESULT").ToInt();
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            if (operationResult == 0)
            {
                string msg = "Ошибка изменения статуса блокировки терминала";
                var d = new DialogWindow($"{msg}", "Изменение статуса блокировки терминала", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
            else
            {
                LoadTerminalItems();
            }
        }

        /// <summary>
        /// обновление записей
        /// </summary>
        public async void LoadTerminalItems()
        {
            TerminalsGrid.ShowSplash();

            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();
                p.Add("FACTORY_ID", $"{FactoryId}");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments/ShipmentKsh");
                q.Request.SetParam("Object", "Terminal");
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
                        TerminalsDS = ListDataSet.Create(result, "ITEMS");
                        TerminalsGrid.UpdateItems(TerminalsDS);
                    }
                }
            }

            TerminalsGrid.HideSplash();
        }

        /// <summary>
        /// фильтрация записей
        /// </summary>
        public void FilterTerminalItems()
        {
            if (TerminalsGrid.GridItems != null)
            {
                if (TerminalsGrid.GridItems.Count > 0)
                {
                    //фильтрация строк

                    //обработка строк
                    foreach (Dictionary<string, string> row in TerminalsGrid.GridItems)
                    {
                        {
                            var t = row.CheckGet("DRIVER_NAME");
                            t = t.SurnameInitials();
                            row.CheckAdd("DRIVER_NAME", t);
                        }

                        {
                            var t = row.CheckGet("FORKLIFTDRIVER_NAME");
                            t = t.SurnameInitials();
                            row.CheckAdd("FORKLIFTDRIVER_NAME", t);
                        }
                    }

                }
            }
        }


        /// <summary>
        /// обновление методов работы с выбранной записью
        /// </summary>
        public void UpdateTerminalActions(Dictionary<string, string> selectedItem)
        {
            SelectedTerminalItem = selectedItem;
            CheckTerminalActions();
        }

        public void CheckTerminalActions()
        {
            BindTerminalButton.IsEnabled = false;
            UnbindTerminalButton.IsEnabled = false;

            TerminalsGrid.Menu["SetShowAllPalletFlag"].Enabled = false;
            TerminalsGrid.Menu["SetShowIncompletePalletFlag"].Enabled= false;
            TerminalsGrid.Menu["RemoveShipmentBlockFlag"].Enabled = false;

            var bindTerminalA = false;
            var bindTerminalB = false;

            if (SelectedTerminalItem != null)
            {
                if (string.IsNullOrEmpty(SelectedTerminalItem.CheckGet("BUYER_NAME")))
                {
                    bindTerminalA = true;
                }
                else
                {
                    UnbindTerminalButton.IsEnabled = true;
                }

                if (SelectedTerminalItem.CheckGet("TRANSPORT_ID").ToInt() > 0)
                {
                    if (SelectedTerminalItem.CheckGet("KIND").ToInt() != 8)
                    {
                        if (SelectedTerminalItem.CheckGet("SHOW_ALL_PALLET_FLAG").ToInt() != 1)
                        {
                            TerminalsGrid.Menu["SetShowAllPalletFlag"].Enabled = true;
                        }
                        
                        if (SelectedTerminalItem.CheckGet("SHOW_ALL_PALLET_FLAG").ToInt() != 2)
                        {
                            TerminalsGrid.Menu["SetShowIncompletePalletFlag"].Enabled = true;
                        }
                    }

                    if (SelectedTerminalItem.CheckGet("SHPMENT_BLOCKED_FLAG").ToInt() > 0)
                    {
                        TerminalsGrid.Menu["RemoveShipmentBlockFlag"].Enabled = true;
                    }
                }
            }

            if (SelectedShipmentItem != null)
            {
                //отгрузка запрещена: голубой
                if (SelectedShipmentItem.CheckGet("FINISHED").ToInt() == 0)
                {
                    bindTerminalB = false;
                }
                else
                {
                    bindTerminalB = true;
                }
            }

            if (bindTerminalA && bindTerminalB)
            {
                BindTerminalButton.IsEnabled = true;
            }

            if (SelectedShipmentItem != null)
            {
                // Количество этих отгрузок уже привязанных к терминалам
                int qtyBindigTerminal = 0;

                if (TerminalsDS != null && TerminalsDS.Items != null && TerminalsDS.Items.Count > 0)
                {
                    foreach (var item in TerminalsDS.Items)
                    {
                        // Если такая отгрузка уже привязана к терминалу
                        if (item.CheckGet("TRANSPORT_ID").ToInt() == SelectedShipmentItem.CheckGet("ID").ToInt())
                        {
                            qtyBindigTerminal += 1;

                            // проверяем, что у транспорта есть прицеп
                            // если есть, то разрешаем привязку
                            // если нет, то запрещаем привязку
                            if (SelectedShipmentItem.CheckGet("TRAILER_FLAG").ToInt() == 0)
                            {
                                BindTerminalButton.IsEnabled = false;
                            }

                            // Если эта отгрузка уже 2 раза привязана к терминалам (для фургона и прицепа), то запрещаем привязывать ещё раз
                            if (qtyBindigTerminal >= 2)
                            {
                                BindTerminalButton.IsEnabled = false;
                            }
                        }
                    }
                }
            }


            if (SelectedTerminalItem.CheckGet("BLOCKED_FLAG") == "1")
            {
                TerminalsGrid.Menu["Blocked"].Visible = false;
                TerminalsGrid.Menu["UnBlocked"].Visible = true;

                BindTerminalButton.IsEnabled = false;
                UnbindTerminalButton.IsEnabled = false;
            }
            else
            {
                TerminalsGrid.Menu["Blocked"].Visible = true;
                TerminalsGrid.Menu["UnBlocked"].Visible = false;
            }

            ProcessPermissions();
        }

        /// <summary>
        /// редактирование данных
        /// </summary>
        public void Edit()
        {
            if (SelectedShipmentItem != null)
            {
                int id = SelectedShipmentItem.CheckGet("ID").ToInt();
                if (id != 0)
                {
                    var h = new ShipmentEdit();
                    h.Edit(id);
                }
            }

        }

        /// <summary>
        /// Перенос отгрузки на другое время
        /// </summary>
        public void EditShipmentTime()
        {
            if (ShipmentsGrid != null && ShipmentsGrid.SelectedItem != null && ShipmentsGrid.SelectedItem.Count > 0)
            {
                var i = new ShipmentDateChange();
                i.ShipmentId = ShipmentsGrid.SelectedItem.CheckGet("ID").ToInt();
                i.ShipmentType = ShipmentsGrid.SelectedItem.CheckGet("SHIPMENTTYPE").ToInt();
                i.Show();
            }
        }

        /// <summary>
        /// Автосоздание отгрузки
        /// </summary>
        public void CreateAutoShipment()
        {
            if (SelectedShipmentItem != null && SelectedDriverItem != null)
            {
                if (SelectedShipmentItem.ContainsKey("ID"))
                {
                    var view = new ShipmentAuto
                    {
                        IdTs = SelectedShipmentItem["ID"],
                    };
                    view.Edit();
                }
            }

        }

        /// <summary>
        /// Расход поддонов
        /// </summary>
        private void PalletConsumption()
        {
            if (SelectedShipmentItem != null && SelectedDriverItem != null)
            {
                if (SelectedShipmentItem.ContainsKey("ID"))
                {

                    Central.LoadServerParams();

                    var view = new PalletConsumption
                    {
                        IdTs = SelectedShipmentItem["ID"],
                    };
                    view.Edit();
                }
            }
        }
        /// <summary>
        /// Ввод причин опоздания
        /// </summary>
        public void SetLateReason()
        {
            if (SelectedShipmentItem != null)
            {
                var id = SelectedShipmentItem.CheckGet("ID").ToInt();
                var h = new ShipmentReasonOfLateness();
                h.Edit(id);
            }
        }

        /// <summary>
        /// Добавление примечания
        /// </summary>
        public void SetComment()
        {
            if (SelectedShipmentItem != null)
            {
                var id = SelectedShipmentItem.CheckGet("ID").ToInt();
                var h = new ShipmentComment();
                h.Edit(id);
            }
        }

        /// <summary>
        /// показать расширенную историю работы с отгрузкой
        /// </summary>
        public void ShowHistoryExtend()
        {
            if (SelectedShipmentItem != null)
            {
                var id = SelectedShipmentItem.CheckGet("ID").ToInt();
                var h = new ShipmentHistoryExtended();
                h.ShipmentId = id;
                h.Init();
            }
        }

        /// <summary>
        /// отвязать водителя
        /// </summary>
        public async void UnbindDriver()
        {
            bool resume = true;
            int shipmentId = 0;

            if (resume)
            {
                if (SelectedShipmentItem != null)
                {
                    shipmentId = SelectedShipmentItem.CheckGet("ID").ToInt();
                    if (shipmentId == 0)
                    {
                        resume = false;
                    }
                }
            }

            if (resume)
            {
                var msg = "";
                msg = $"{msg}Отвязать водителя от отгрузки?\n";

                if (!string.IsNullOrEmpty(SelectedShipmentItem.CheckGet("BUYER")))
                {
                    msg = $"{msg}{SelectedShipmentItem.CheckGet("BUYER")}\n";
                }

                if (!string.IsNullOrEmpty(SelectedShipmentItem.CheckGet("DRIVER")))
                {
                    msg = $"{msg}{SelectedShipmentItem.CheckGet("DRIVER")}\n";
                }

                var d = new DialogWindow($"{msg}", "Отвязка водителя", "", DialogWindowButtons.NoYes);
                if (d.ShowDialog() != true)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("ID", shipmentId.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "Shipment");
                q.Request.SetParam("Action", "UnbindDriver");

                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            var id = ds.GetFirstItemValueByKey("ID").ToInt();

                            if (id != 0)
                            {
                                //отправляем сообщение гриду о необходимости обновить данные
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup = "ShipmentControl",
                                    ReceiverName = "DriverList,ShipmentList",
                                    SenderName = "ShipmentList",
                                    Action = "Refresh",
                                });
                            }
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }



        /// <summary>
        /// Порядок загрузки
        /// </summary>
        private void LoadingOrderNew()
        {
            if (SelectedShipmentItem != null)
            {
                int id = SelectedShipmentItem.CheckGet("ID").ToInt();
                var shipment = new Shipment(id);
                shipment.ShowLoadingScheme(SelectedShipmentItem);
            }
        }

        private void TestLoadingOrder()
        {
            if (ShipmentsGrid != null && ShipmentsGrid.Items != null && ShipmentsGrid.Items.Count > 0)
            {
                var shipmentItems = ShipmentsGrid.Items.Where(x => x.CheckGet("PRODUCTION_TYPE_ID").ToInt() != 3 && x.CheckGet("LOADINGSCHEMESTATUS").ToInt() == 0).Take(20).ToList();
                foreach (var item in shipmentItems)
                {
                    var shipment = new Shipment(item.CheckGet("ID").ToInt());
                    shipment.ShowLoadingScheme(item);
                }
            }
        }

        private bool GetLoadingOrderTwoVisible()
        {
            bool result = false;

            var mode = Central.Navigator.GetRoleLevel("[erp]manually_loading_scheme");
            switch (mode)
            {
                case Role.AccessMode.Special:
                case Role.AccessMode.FullAccess:
                case Role.AccessMode.ReadOnly:
                    result = true; 
                    break;
                
                default:
                    result = false;
                    break;
            }

            return result;
        }

        /// <summary>
        /// Загрузка нового интерфейса Схема погрузки 2
        /// </summary>
        private void LoadingOrderTwo()
        {

            //loadingOrderTwo.Edit(id);

            if (SelectedShipmentItem != null)
            {
                if (SelectedShipmentItem.CheckGet("PACKAGINGTYPETEXT") == "су" || SelectedShipmentItem.CheckGet("PACKAGINGTYPETEXT") == "бу")
                {
                    if (SelectedShipmentItem.CheckGet("LOADINGSCHEMESTATUS") == "1")
                    {
                        string msg = "Автоматическая схема погрузки для данного транспортного средства запрещена менеджером.";
                        var d = new DialogWindow($"{msg}", "Схема погрузки", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                    else if (SelectedShipmentItem.CheckGet("LOADINGSCHEMESTATUS") == "2")
                    {
                        int id = SelectedShipmentItem.CheckGet("ID").ToInt();
                        var shipment = new Shipment(id);
                        shipment.ShowLoadingScheme(SelectedShipmentItem);
                    }
                    else
                    {
                        var id = SelectedShipmentItem.CheckGet("ID").ToInt();
                        var loadingOrderTwo = new ShipmentShemeTwo();
                        loadingOrderTwo.ReturnTabName = "ShipmentsControl_List";
                        loadingOrderTwo.ShipmentId = id;
                        loadingOrderTwo.Init();
                    }
                }
                else
                {
                    string msg = "Для данной отгрузки нет схемы, т.к. она содержит бумагу в рулонах";
                    var d = new DialogWindow($"{msg}", "Схема погрузки 2", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
        }

        public bool HasVisibleContextMenu()
        {
            bool result = false;

            if (Central.User.Roles.FirstOrDefault(x => x.Value.Code.Contains("[f]admin")).Key != null)
            {
                result = true;
            }

            if (!Central.DebugMode)
            {
                result = false;
            }

            return result;
        }

        /// <summary>
        /// обработка сообщений
        /// </summary>
        /// <param name="message"></param>
        public void ProcessMessages(ItemMessage message)
        {
            if (message != null)
            {
                if (
                    message.SenderName == "WindowManager"
                    && message.ReceiverName == "ShipmentsControl_List"
                )
                {
                    switch (message.Action)
                    {
                        case "FocusGot":
                            ShipmentsGrid.ItemsAutoUpdate = true;
                            DriversGrid.ItemsAutoUpdate = true;
                            TerminalsGrid.ItemsAutoUpdate = true;

                            //ShipmentsGrid.LoadItems();
                            //DriversGrid.LoadItems();
                            //ShipmentsGrid.LoadItems();
                            break;

                        case "FocusLost":
                            ShipmentsGrid.ItemsAutoUpdate = false;
                            DriversGrid.ItemsAutoUpdate = false;
                            TerminalsGrid.ItemsAutoUpdate = false;
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void _ProcessMessages(ItemMessage m)
        {
            //Group ShipmentControl
            if (m.ReceiverGroup.IndexOf("ShipmentControl") > -1)
            {
                if (
                    m.ReceiverName.IndexOf("ShipmentsList") > -1
                    || m.ReceiverName.IndexOf("ShipmentList") > -1
                )
                {
                    switch (m.Action)
                    {
                        case "Save":
                            {
                                var p = new Dictionary<string, string>();
                                if (m.ContextObject != null)
                                {
                                    p = (Dictionary<string, string>)m.ContextObject;
                                }
                                AddDriverSetDatetime(p);
                            }
                            break;

                        case "SelectById":
                            {
                                ShipmentsGrid.LoadItems();

                                var id = m.Message.ToInt();
                                ShipmentsGrid.SetSelectedItemId(id);
                            }
                            break;

                        case "Refresh":
                            ShipmentsGrid.LoadItems();
                            break;

                        case "ShowShipmentToPrint":
                            Central.Navigator.ProcessURL("l-pack://l-pack_erp/stock/shipment_control/list");
                            ShowShipmentToPrint();
                            break;

                        case "ShowShipmentAll":
                            ShowShipmentAll();
                            break;
                    }
                }

                if (m.ReceiverName.IndexOf("DriverList") > -1)
                {
                    switch (m.Action)
                    {
                        case "Refresh":

                            if (m.ContextObject != null)
                            {
                                var v = (Dictionary<string, string>)m.ContextObject;
                                var driverlogId = v.CheckGet("DRIVERLOGID").ToInt();
                                DriversGrid.SetSelectedItemId(driverlogId);
                            }
                            DriversGrid.LoadItems();
                            break;

                        // регистрация приехавшего водителя
                        case "SelectItem":
                            {
                                var p = new Dictionary<string, string>();
                                if (m.ContextObject != null)
                                {
                                    p = (Dictionary<string, string>)m.ContextObject;
                                }
                                ArriveDriver = p;
                                AddDriverComplete();
                            }
                            break;

                    }
                }

                if (m.ReceiverName.IndexOf("TerminalList") > -1)
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            TerminalsGrid.LoadItems();
                            break;
                    }
                }
            }

            if (m.Action == "Closed")
            {
                switch (m.SenderName)
                {
                    case "LateReason":
                        EditButton.IsEnabled = true;
                        break;

                    case "BindDriver":
                        BindDriverButton.IsEnabled = true;
                        break;

                    case "BindTerminal":
                        BindTerminalButton.IsEnabled = true;
                        break;

                    case "DriverListExpected":
                        SelectingDriver = false;
                        break;

                    case "DriverListAll":
                        SelectingDriver = false;
                        break;
                }
            }
        }

        /// <summary>
        /// обработчик системы навигации по URL
        /// </summary>
        public void ProcessNavigation()
        {

        }


        /// <summary>
        /// обработчик нажатий клавиатуры
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.F5:
                    ShipmentsGrid.LoadItems();
                    DriversGrid.LoadItems();
                    TerminalsGrid.LoadItems();
                    e.Handled = true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

                case Key.Home:
                    ShipmentsGrid.SetSelectToFirstRow();
                    e.Handled = true;
                    break;

                case Key.End:
                    ShipmentsGrid.SetSelectToLastRow();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// Вызов справки
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/shipments/control/listing");
        }

        /// <summary>
        /// Вызов настроек
        /// </summary>
        public void ShowSettings()
        {
            var settings = new ShipmentSettings(0);
            settings.ReceiverTabName = "ShipmentsControl_List";
            settings.Edit();
        }

        /// <summary>
        /// флаг-защита от неверных срабатываний
        /// сообщение о выборе водителя будет обработано,
        /// только если флаг поднят
        /// </summary>
        private bool SelectingDriver { get; set; }

        /// <summary>
        /// вызов интерфейса добавления водителя
        /// </summary>
        public void AddDriver()
        {
            if (SelectingDriver)
            {
                Central.WM.RemoveTab($"AddDriver");
                SelectingDriver = false;
            }

            SelectingDriver = true;
            var i = new AddDriver();
            Central.WM.SetLayer("add");

            try
            {
                ((DriverListExpected)Central.WM.TabItems.FirstOrDefault(x => x.Key == "AddDriver_ExpectedDrivers").Value.Content).ParentFrameType = DriverListExpected.ParentFrameTypeDefault.ShipmentsList;
                ((DriverListExpected)Central.WM.TabItems.FirstOrDefault(x => x.Key == "AddDriver_ExpectedDrivers").Value.Content).Types.SetSelectedItemByKey("-2");
                ((DriverListExpected)Central.WM.TabItems.FirstOrDefault(x => x.Key == "AddDriver_ExpectedDrivers").Value.Content).LoadItems();
                ((DriverListAll)Central.WM.TabItems.FirstOrDefault(x => x.Key == "AddDriver_AllDrivers").Value.Content).ParentFrameType = DriverListAll.ParentFrameTypeDefault.ShipmentsList;
            }
            catch (Exception)
            {
            }
        }

        public string ShipmentDate { get; set; }
        public string ShipmentTime { get; set; }
        public Dictionary<string, string> ArriveDriver { get; set; }

        public void AddDriverSetDatetime(Dictionary<string, string> p)
        {
            var shipmentId = ArriveDriver.CheckGet("SHIPMENTID").ToInt();

            if (shipmentId != 0)
            {
                if (shipmentId == p.CheckGet("SHIPMENT_ID").ToInt())
                {
                    ShipmentDate = p.CheckGet("SHIPMENT_DATE");
                    ShipmentTime = p.CheckGet("SHIPMENT_TIME");
                    AddDriverComplete(true);
                }
            }

        }

        /// <summary>
        /// водитель выбран
        /// </summary>
        public async void AddDriverComplete(bool noConfirm = false)
        {
            var resume = true;
            var item = ArriveDriver;

            var driverId = 0;
            var expected = false;
            var shipmentId = 0;
            var checkDateTime = false;

            if (resume)
            {
                driverId = item.CheckGet("ID").ToInt();
                if (driverId == 0)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                if (!noConfirm)
                {
                    var msg = "";
                    msg = $"{msg}Отметить приезд водителя?\n";

                    /*
                    if(Central.DebugMode)
                    {
                        if(!string.IsNullOrEmpty(item.CheckGet("ID")))
                        {
                            msg=$"{msg}ИД водителя: {item.CheckGet("ID")}\n";
                        }

                        if(!string.IsNullOrEmpty(item.CheckGet("SHIPMENTID")))
                        {
                            msg=$"{msg}ИД отгрузки: {item.CheckGet("SHIPMENTID")}\n";
                        }
                    }
                    */

                    if (!string.IsNullOrEmpty(item.CheckGet("DRIVERNAME")))
                    {
                        msg = $"{msg}Водитель: {item.CheckGet("DRIVERNAME")}\n";
                    }

                    if (!string.IsNullOrEmpty(item.CheckGet("CARMARK")))
                    {
                        msg = $"{msg}Авто: {item.CheckGet("CARMARK")}\n";
                    }

                    if (!string.IsNullOrEmpty(item.CheckGet("CARNUMBER")))
                    {
                        msg = $"{msg}Номер: {item.CheckGet("CARNUMBER")}\n";
                    }

                    var d = new DialogWindow($"{msg}", "Приезд водителя", "", DialogWindowButtons.NoYes);
                    if (d.ShowDialog() != true)
                    {
                        resume = false;
                    }
                }
            }

            if (resume)
            {
                // Если отгрузка не в рулонах
                if (item.CheckGet("SHIPMENTTYPE").ToInt() != 2)
                {
                    if (
                        item.CheckGet("UNSHIPPED").ToInt() == 1
                        || item.CheckGet("LATE").ToInt() == 1
                    )
                    {
                        checkDateTime = true;
                    }
                }

            }

            if (resume)
            {
                if (checkDateTime)
                {
                    if (
                        string.IsNullOrEmpty(ShipmentDate)
                        || string.IsNullOrEmpty(ShipmentTime)
                    )
                    {
                        /*
                            уточняем дату и время отгрузки
                            придет сообщение
                                ReceiverGroup   ="ShipmentControl",
                                SenderName      ="ShipmentDateTime",
                                ReceiverName    ="BindDriverView",
                                Action          ="Save",

                            SHIPMENT_ID
                            SHIPMENT_DATE
                            SHIPMENT_TIME
                            */

                        var i = new ShipmentDateTime();
                        i.ShipmentType = item.CheckGet("SHIPMENTTYPE").ToInt();
                        i.ShipmentId = item.CheckGet("SHIPMENTID").ToInt();
                        i.ReceiverName = "ShipmentsList";
                        i.Edit();
                        resume = false;
                    }
                }
            }


            var p = new Dictionary<string, string>();
            if (resume)
            {
                shipmentId = item.CheckGet("SHIPMENTID").ToInt();
                if (shipmentId != 0)
                {
                    expected = true;
                    p.CheckAdd("SHIPMENT_DATE", ShipmentDate);
                    p.CheckAdd("SHIPMENT_TIME", ShipmentTime);
                    p.CheckAdd("SET_DATETIME", "1");
                }
                else
                {
                    p.CheckAdd("TYPE_ORDER", "0");
                }

                p.CheckAdd("ID", driverId.ToString());
                p.CheckAdd("EXPECTED", expected.ToInt().ToString());
                p.CheckAdd("SHIPMENT_ID", shipmentId.ToString());

                SetArrived(p);
            }
        }

        /*
        /// <summary>
        /// водитель выбран
        /// </summary>
        public async void AddDriverComplete2(Dictionary<string,string> item)
        {
            var resume=true;

            var driverId=0;
            var expected=false;
            var shipmentId=0;

            if (resume)
            {
                driverId=item.CheckGet("ID").ToInt();
                if (driverId == 0)
                {
                    resume = false;
                }
            }

            var p=new Dictionary<string,string>();
            if(resume)
            {
                if(item.ContainsKey("SHIPMENTID"))
                {
                    shipmentId=item.CheckGet("SHIPMENTID").ToInt();
                    if(shipmentId!=0)
                    {
                        expected=true;
                    }
                }

                p.CheckAdd("ID",driverId.ToString());
                p.CheckAdd("EXPECTED",expected.ToInt().ToString());
                p.CheckAdd("SHIPMENT_ID",shipmentId.ToString());

                SetArrived(p);
            }
        }
        */

        /// <summary>
        /// отпарвка запроса "отметить водителя как приехавшего"
        /// </summary>
        /// <param name="p"></param>
        public async void SetArrived(Dictionary<string, string> p)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "TransportDriver");
            q.Request.SetParam("Action", "SetArrived");

            q.Request.SetParam("_OBJECT_ID", ObjectId.ToString());
            q.Request.SetParam("FACTORY_ID", $"{FactoryId}");

            q.Request.SetParams(p);

            q.Request.Timeout = 10000;
            q.Request.Attempts = 0;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    var id = ds.GetFirstItemValueByKey("ID").ToInt();

                    if (id != 0)
                    {
                        Messenger.Default.Send(new ItemMessage()
                        {
                            ReceiverGroup = "ShipmentControl",
                            ReceiverName = "DriverList,ShipmentList",
                            SenderName = "ShipmentList",
                            Action = "Refresh",
                            Message = id.ToString(),
                        });

                        SelectingDriver = false;
                        Central.WM.RemoveTab($"AddDriver");
                    }

                }

            }
            else
            {
                q.ProcessError();
            }

            ShipmentDate = "";
            ShipmentTime = "";
        }


        /// <summary>
        /// Открытие вкладки редактирования данных водителя
        /// </summary>
        public void EditDriver()
        {
            var id = 0;
            var driverLogId = 0;
            if (SelectedDriverItem != null)
            {
                if (SelectedDriverItem.ContainsKey("DRIVERID"))
                {
                    id = SelectedDriverItem["DRIVERID"].ToInt();
                }
            }
            if (SelectedDriverItem != null)
            {
                if (SelectedDriverItem.ContainsKey("ID"))
                {
                    driverLogId = SelectedDriverItem["ID"].ToInt();
                }
            }

            if (id != 0)
            {
                var driver = new Driver
                {
                    Id = id,
                    DriverLogId = driverLogId
                };
                driver.ReturnTabName = "ShipmentsControl_List";
                driver.Edit(id);
                EditButton.IsEnabled = false;
            }
        }

        /// <summary>
        /// Проверяем, стоит ли отгрузка на терминале.
        /// Если стоит, то запрашиваем подтвержение на выполнение последующих действий.
        /// </summary>
        /// <param name="transportId"></param>
        /// <returns></returns>
        public bool CheckSipmentAtTerminal(int transportId)
        {
            bool result = false;

            var p = new Dictionary<string, string>();
            p.Add("TRANSPORT_ID", $"{transportId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Terminal");
            q.Request.SetParam("Action", "CheckShipment");

            q.Request.SetParams(p);

            q.Request.Timeout = 10000;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var queryResult = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (queryResult != null)
                {
                    var ds = ListDataSet.Create(queryResult, "ITEMS");
                    if (ds != null && ds.Items != null)
                    {
                        if (ds.Items.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(ds.Items.First().CheckGet("TERMINAL_ID")))
                            {
                                string msg = "Отгрузка уже стоит на терминале. Вы хотите продолжить?";
                                var d = new DialogWindow($"{msg}", "Приезд водителя", "", DialogWindowButtons.NoYes);
                                if (d.ShowDialog() == true)
                                {
                                    result = true;
                                }
                            }
                            else
                            {
                                result = true;
                            }
                        }
                        else
                        {
                            result = true;
                        }
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            return result;
        }

        /// <summary>
        /// Привязка водителя к терминалу
        /// </summary>
        public void BindTerminal()
        {
            var resume = true;

            var id = 0;
            var forkliftDriverId = 0;
            var terminalId = 0;
            var terminalName = "";
            //18.06 DD.MM
            var shipmentDate = "";


            if (resume)
            {
                if (
                    SelectedShipmentItem == null
                    || SelectedTerminalItem == null
                )
                {
                    var msg = "Выберите отгрузку и свободный терминал.";
                    var d = new DialogWindow($"{msg}", "Привязка к терминалу", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                    resume = false;
                }
            }

            if (resume)
            {
                if (SelectedShipmentItem != null)
                {
                    if (SelectedShipmentItem.ContainsKey("ID"))
                    {
                        id = SelectedShipmentItem["ID"].ToInt();
                    }
                    if (SelectedShipmentItem.ContainsKey("SHIPMENTDATE"))
                    {
                        shipmentDate = SelectedShipmentItem["SHIPMENTDATE"].ToString();
                    }
                }
                else
                {
                    resume = false;
                }
            }

            if (resume)
            {

                if (SelectedTerminalItem != null)
                {
                    //forklift_driver.id_fd
                    if (SelectedTerminalItem.ContainsKey("FORKLIFTDRIVERID"))
                    {
                        forkliftDriverId = SelectedTerminalItem["FORKLIFTDRIVERID"].ToInt();
                    }

                    //terminal.id_ter
                    if (SelectedTerminalItem.ContainsKey("TERMINAL_ID"))
                    {
                        terminalId = SelectedTerminalItem["TERMINAL_ID"].ToInt();
                    }
                    if (SelectedTerminalItem.ContainsKey("TERMINALNAME"))
                    {
                        terminalName = SelectedTerminalItem["TERMINALNAME"].ToString();
                    }
                }
                else
                {
                    resume = false;
                }
            }

            if (resume)
            {
                if (!CheckSipmentAtTerminal(id))
                {
                    resume = false;
                }
            }

            if (resume)
            {
                var bindTerminal = new BindTerminal();
                bindTerminal.ShipmentId = id;
                bindTerminal.TerminalId = terminalId;
                bindTerminal.ShipmentType = SelectedShipmentItem.CheckGet("PRODUCTIONTYPE").ToInt();
                bindTerminal.Edit();
                BindTerminalButton.IsEnabled = false;
            }

        }

        /// <summary>
        /// Отвязка отгрузки от терминала
        /// </summary>
        public async void UnbindTerminal()
        {
            if (SelectedTerminalItem != null)
            {
                var h = new BindTerminal();
                h.Unbind(SelectedTerminalItem);
            }
        }

        public void BindDriver2()
        {

            var resume = true;
            var bindDriver = new BindDriver();

            if (resume)
            {
                if (
                    SelectedShipmentItem == null
                    || SelectedDriverItem == null
                )
                {
                    var msg = "Выберите отгрузку и водителя.";
                    var d = new DialogWindow($"{msg}", "Привязка водителя", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                    resume = false;
                }
            }

            if (resume)
            {
                if (SelectedShipmentItem != null)
                {
                    if (SelectedShipmentItem.ContainsKey("ID"))
                    {
                        bindDriver.Id = SelectedShipmentItem["ID"].ToInt();
                    }
                }
                else
                {
                    resume = false;
                }
            }

            if (resume)
            {
                if (SelectedDriverItem != null)
                {
                    if (SelectedDriverItem.ContainsKey("ID"))
                    {
                        bindDriver.DriverLogId = SelectedDriverItem["ID"].ToInt();
                    }
                    if (SelectedDriverItem.ContainsKey("DRIVERID"))
                    {
                        bindDriver.DriverId = SelectedDriverItem["DRIVERID"].ToInt();
                    }
                }
                else
                {
                    resume = false;
                }
            }

            if (resume)
            {
                if (ShipmentsDS != null)
                {
                    bindDriver.ShipmentsDS = ShipmentsDS;
                }
                else
                {
                    resume = false;
                }

                if (DriversDS != null)
                {
                    bindDriver.DriversDS = DriversDS;
                }
                else
                {
                    resume = false;
                }
            }

            // Проверяем, что этот водитель не привязан к более ранней отгрузке
            if (resume)
            {
                resume = CheckDriverEarlyShipment(bindDriver.DriverId, SelectedShipmentItem.CheckGet("SHIPMENTDATETIME"));
            }

            if (resume)
            {
                bindDriver.Edit();
                BindDriverButton.IsEnabled = false;
            }
        }

        public bool CheckDriverEarlyShipment(int driverId, string shipmentDate)
        {
            bool checkResult = true;

            if (!string.IsNullOrEmpty(shipmentDate))
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("DRIVER_ID", driverId.ToString());
                    p.Add("SHIPMENT_DATE", shipmentDate);
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "Shipment");
                q.Request.SetParam("Action", "GetDriverEarlyShipment");

                q.Request.SetParams(p);

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                        {
                            var msg = $"Водитель уже привязан к более ранней неотгруженной отгрузке.{Environment.NewLine}" +
                                $"Сначала отгрузите отгрузку от {ds.Items[0].CheckGet("SHIPMENT_DATETIME")}";
                            var d = new DialogWindow($"{msg}", "Привязка водителя", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                            checkResult = false;
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                    checkResult = false;
                }
            }

            return checkResult;
        }

        /// <summary>
        /// Удаление водителя из списка приехавших
        /// </summary>
        public async void DeleteDriver()
        {
            var resume = true;

            var driverId = GetSelectedDriverId();
            if (resume)
            {
                if (driverId == 0)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                var msg = "";
                msg = $"{msg}Удалить водителя из списка приехавших?\n";

                if (SelectedDriverItem.ContainsKey("DRIVERNAME"))
                {
                    msg = $"{msg}{SelectedDriverItem["DRIVERNAME"]}\n";
                }

                var d = new DialogWindow($"{msg}", "Приезд водителя", "", DialogWindowButtons.NoYes);
                if (d.ShowDialog() != true)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("ID", driverId.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "TransportDriver");
                q.Request.SetParam("Action", "DeleteArrived");

                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            var id = ds.GetFirstItemValueByKey("ID").ToInt();

                            if (id != 0)
                            {
                                //отправляем сообщение гриду о необходимости обновить данные
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup = "ShipmentControl",
                                    ReceiverName = "DriverList,ShipmentList",
                                    SenderName = "ShipmentList",
                                    Action = "Refresh",
                                });
                            }
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// Отметка убытия водителя
        /// </summary>
        public async void MarkDriverDeparture()
        {
            var resume = true;

            var driverId = GetSelectedDriverId();
            if (resume)
            {
                if (driverId == 0)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                var msg = "";
                msg = $"{msg}Отметить убытие водителя?\n";

                if (SelectedDriverItem.ContainsKey("DRIVERNAME"))
                {
                    msg = $"{msg}{SelectedDriverItem["DRIVERNAME"]}\n";
                }

                var d = new DialogWindow($"{msg}", "Убытие водителя", "", DialogWindowButtons.NoYes);
                if (d.ShowDialog() != true)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("ID", driverId.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "TransportDriver");
                q.Request.SetParam("Action", "MarkDeparture");

                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            var id = ds.GetFirstItemValueByKey("ID").ToInt();

                            if (id != 0)
                            {
                                //отправляем сообщение гриду о необходимости обновить данные
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup = "ShipmentControl",
                                    ReceiverName = "DriverList,ShipmentList",
                                    SenderName = "ShipmentList",
                                    Action = "Refresh",
                                });

                            }
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// Отмена въезда водителя
        /// </summary>
        public async void CancelDriverEntry()
        {
            var resume = true;

            var driverId = GetSelectedDriverId();
            if (resume)
            {
                if (driverId == 0)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                var msg = "";
                msg = $"{msg}Отменить въезд водителя?\n";

                if (SelectedDriverItem.ContainsKey("DRIVERNAME"))
                {
                    msg = $"{msg}{SelectedDriverItem["DRIVERNAME"]}\n";
                }

                var d = new DialogWindow($"{msg}", "Въезд водителя", "", DialogWindowButtons.NoYes);
                if (d.ShowDialog() != true)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("ID", driverId.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "TransportDriver");
                q.Request.SetParam("Action", "CancelEntry");

                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            var id = ds.GetFirstItemValueByKey("ID").ToInt();

                            if (id != 0)
                            {
                                //отправляем сообщение гриду о необходимости обновить данные
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup = "ShipmentControl",
                                    ReceiverName = "DriverList,ShipmentList",
                                    SenderName = "ShipmentList",
                                    Action = "Refresh",
                                });

                            }
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// Установка активных строк в отгрузках и терминалах для выбранного водителя
        /// </summary>
        public void ShowShipment()
        {
            if (SelectedDriverItem != null)
            {
                if (SelectedDriverItem.ContainsKey("TRANSPORTID"))
                {
                    var id = SelectedDriverItem["TRANSPORTID"].ToInt();
                    if (id != 0)
                    {
                        ShipmentsGrid.SelectRowByKey(id);
                    }
                }
                if (SelectedDriverItem.ContainsKey("TERMINALNUMBER"))
                {
                    var id = SelectedDriverItem["TERMINALNUMBER"].ToInt();
                    if (id != 0)
                    {
                        TerminalsGrid.SelectRowByKey(id);
                    }
                }
            }
        }

        /// <summary>
        /// Получает ID выбранного водителя
        /// </summary>
        /// <returns></returns>
        public int GetSelectedDriverId()
        {
            var result = 0;

            if (SelectedDriverItem != null)
            {
                if (SelectedDriverItem.ContainsKey("ID"))
                {
                    var id = SelectedDriverItem["ID"].ToInt();

                    if (id != 0)
                    {
                        result = id;
                    }
                }
            }

            return result;
        }


        /// <summary>
        /// Вывод печатной формы доверенности
        /// </summary>
        private void PrintProxy()
        {
            if (SelectedShipmentItem != null)
            {
                var id = SelectedShipmentItem.CheckGet("ID").ToInt();
                var reporter = new ShipmentReport(id);
                reporter.PrintProxy();
            }
        }

        /// <summary>
        /// Вывод печатной формы загрузочной карты водителя
        /// </summary>
        private void PrintBootcard()
        {
            if (SelectedShipmentItem != null)
            {
                var id = SelectedShipmentItem.CheckGet("ID").ToInt();
                var reporter = new ShipmentReport(id);
                reporter.PrintBootcard();
            }
        }

        /// <summary>
        /// Вывод печатной формы задания на отгрузку
        /// </summary>
        private void PrintShipmenttask()
        {
            if (SelectedShipmentItem != null)
            {
                var id = SelectedShipmentItem.CheckGet("ID").ToInt();
                var reporter = new ShipmentReport(id);
                reporter.PrintShipmenttask();
            }
        }

        /// <summary>
        /// Вывод печатной формы карты склада
        /// </summary>
        private void PrintStockmap()
        {
            if (SelectedShipmentItem != null)
            {
                var id = SelectedShipmentItem.CheckGet("ID").ToInt();
                var reporter = new ShipmentReport(id);
                reporter.PrintStockmap();
            }
        }

        /// <summary>
        /// Вывод печатной формы карты проезда
        /// </summary>
        private void PrintRoutemap()
        {
            if (SelectedShipmentItem != null)
            {
                var id = SelectedShipmentItem.CheckGet("ID").ToInt();
                var reporter = new ShipmentReport(id);
                reporter.PrintRoutemap();
            }
        }

        /// <summary>
        /// печать всех документов
        /// </summary>
        private void PrintAll()
        {
            if (SelectedShipmentItem != null)
            {
                var id = SelectedShipmentItem.CheckGet("ID").ToInt();
                var reporter = new ShipmentReport(id);
                reporter.PrintProxy(true);
                reporter.PrintBootcard(true);
                reporter.PrintShipmenttask(true);
                reporter.PrintStockmap(true);
                reporter.PrintRoutemap(true);
            }
        }

        private void ShowAll()
        {
            if (SelectedShipmentItem != null)
            {
                var id = SelectedShipmentItem.CheckGet("ID").ToInt();
                var reporter = new ShipmentReport(id);
                reporter.PrintProxy();
                reporter.PrintBootcard();
                reporter.PrintShipmenttask();
                reporter.PrintStockmap();
                reporter.PrintRoutemap();
            }
        }

        /// <summary>
        /// Печать документов на отгрузку на СОХ        
        /// </summary>
        public void PrintResponsibleStockDocuments()
        {
            if (SelectedShipmentItem != null && SelectedShipmentItem.CheckGet("PRODUCTIONTYPE").ToInt() == 5)
            {
                // Проверяем, что по этой отгрузке были перемещения поддонов на СОХ
                if (CheckMovingItem())
                {
                    var id = SelectedShipmentItem.CheckGet("ID").ToInt();
                    var reporter = new ShipmentReport(id);
                    reporter.PrintMovementInvoiceDocuments();
                }
            }
        }

        /// <summary>
        /// Проверяем наличие записей в таблице online_store_moving_item по выбранной отгрузке
        /// </summary>
        public bool CheckMovingItem()
        {
            bool succesfullFlag = false;

            if (SelectedShipmentItem != null)
            {
                string shipmentId = SelectedShipmentItem.CheckGet("ID");

                var p = new Dictionary<string, string>();
                p.Add("SHIPMENT_ID", shipmentId);

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "ResponsibleStock");
                q.Request.SetParam("Action", "GetMovingItemCount");

                q.Request.SetParams(p);

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var dataSet = ListDataSet.Create(result, "ITEMS");
                        if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                        {
                            int countMovingRecord = dataSet.Items.First().CheckGet("COUNT_MOVING_RECORD").ToInt();
                            int quantityMovingProduct = dataSet.Items.First().CheckGet("QUANTITY_MOVING_PRODUCT").ToInt();
                            int quantityOrderProduct = dataSet.Items.First().CheckGet("QUANTITY_ORDER_PRODUCT").ToInt();

                            // Если количество записей перемещения поддонов в фуру больше 0
                            if (countMovingRecord > 0)
                            {
                                // Если количество перемещённой продукции больше или равно количеству заказанной продукции
                                if (quantityMovingProduct > 0 && quantityOrderProduct > 0 && quantityMovingProduct >= quantityOrderProduct)
                                {
                                    succesfullFlag = true;
                                }
                                else
                                {
                                    var msg = $"Внимание!" +
                                        $"{Environment.NewLine}Погружено меньше продукции, чем было заказано." +
                                        $"{Environment.NewLine}Возможно загрузка поддонов в транспортное средство ещё не завершена." +
                                        $"{Environment.NewLine}" +
                                        $"{Environment.NewLine}Вы действительно хотите распечатать документы с меньшим количеством продукции, чем было заказано?";
                                    var d = new DialogWindow($"{msg}", "Отгрузка на СОХ", "", DialogWindowButtons.NoYes);
                                    if (d.ShowDialog() == true)
                                    {
                                        succesfullFlag = true;
                                    }
                                }
                            }
                            else
                            {
                                var msg = "По выбранной отгрузке нет совершённых перемещений. Пожалуйста, убедитесь что загрузка поддонов в транспортное средство завершена.";
                                var d = new DialogWindow($"{msg}", "Отгрузка на СОХ", "", DialogWindowButtons.OK);
                                d.ShowDialog();
                            }
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            return succesfullFlag;
        }

        /// <summary>
        /// Открытие вкладки накладных по выбранной отгрузке
        /// </summary>
        private void ShipmentDocumentList()
        {
            if (SelectedShipmentItem != null && SelectedShipmentItem.Count > 0)
            {
                var shipmentDocumentList = new ShipmentDocumentList();
                shipmentDocumentList.RoleName = this.RoleName;
                shipmentDocumentList.TransportId = SelectedShipmentItem.CheckGet("ID").ToInt();
                shipmentDocumentList.SelectedShipmentItem = SelectedShipmentItem;
                shipmentDocumentList.Show();
            }
        }

        /// <summary>
        /// Показать закрытые отгрузки, для которых нужно распечатать документы
        /// </summary>
        private void ShowShipmentToPrint()
        {
            if (ShipmentToPrintCount > 0)
            {
                SearchText.Clear();
                // На печать
                ShipmentTypes.SetSelectedItemByKey("7");

                // Получаем данные для грида отгрузок, чтобы получить актуальные статусы отгрузок
                ShipmentsGrid.LoadItems();
                GetShipmentCountToPrint();
            }
        }

        /// <summary>
        /// Сбросить фильтрацию по типу и показать все отгрузки
        /// </summary>
        private void ShowShipmentAll()
        {
            SearchText.Clear();
            // Все отгрузки
            ShipmentTypes.SetSelectedItemByKey("-1");
            GetShipmentCountToPrint();
        }

        /// <summary>
        /// Вывод печатной формы со списком водителей
        /// </summary>
        private void MakeDriverReport()
        {
            var reporter = new DriverReporter();
            reporter.Drivers = DriversDS.Items;
            reporter.MakeDriverReport();
        }

        private void Refresh()
        {
            ShipmentsGrid.LoadItems();
            DriversGrid.LoadItems();
            TerminalsGrid.LoadItems();
        }


        /// <summary>
        /// Обработчик нажатия на кнопку обновления (Показать)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку привязки водителя
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BindDriverButton_Click(object sender, RoutedEventArgs e)
        {
            BindDriver2();
        }

        /// <summary>
        /// Обработчик изменения выбранной записи в выпадающем списке типов изделий
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void Types_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ShipmentsGrid.UpdateItems();
        }


        /// <summary>
        /// Обработчик нажатия на кнопку печати загрузочной карты водителя
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrintDriverBootCardButton_Click(object sender, RoutedEventArgs e)
        {
            PrintBootcard();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку печати карты склада
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrintMapButton_Click(object sender, RoutedEventArgs e)
        {
            PrintStockmap();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку печати карты проезда
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrintRouteMapsButton_Click(object sender, RoutedEventArgs e)
        {
            PrintRoutemap();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку печати доверенности
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrintProxyDocsButton_Click(object sender, RoutedEventArgs e)
        {
            PrintProxy();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку печати всех документов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrintAllButton_Click(object sender, RoutedEventArgs e)
        {
            PrintAll();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку печати списка водителей
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            MakeDriverReport();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку добавления водителя 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddDriver();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку удаления водителя
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteDriver();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку отметки убытия водителя
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MarkDepartureButton_Click(object sender, RoutedEventArgs e)
        {
            MarkDriverDeparture();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку отмены въезда водителя
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelEntryButton_Click(object sender, RoutedEventArgs e)
        {
            CancelDriverEntry();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку выделения отгрузки и терминала выбранного водителя
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowShipmentButton_Click(object sender, RoutedEventArgs e)
        {
            ShowShipment();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку редактирования водителя
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            EditDriver();
        }

        /// <summary>
        /// Обработчик изменения выбранной записи в выпадающем списке типов доставки 
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void DeliveryTypes_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ShipmentsGrid.UpdateItems();
        }

        /// <summary>
        /// Обработчик изменения выбранной записи в выпадающем списке типов отгрузки
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void ShipmentTypes_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ShipmentsGrid.UpdateItems();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку привязки к терминалу
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BindTerminalButton_Click(object sender, RoutedEventArgs e)
        {
            bool resume = true;

            if (SelectedShipmentItem != null)
            {
                // == 0 не привязан водитель все остальное привязан

                if (SelectedShipmentItem.CheckGet("DLIDTS").ToInt() == 0)
                {
                    // водитель не привязан, задаим дополнительный вопрос
                    resume = false;

                    var dw = new DialogWindow($"Вы действительно хотите привязать отгрузку к терминалу без зарегистрированного водителя?", "Привязка отгрузки", "", DialogWindowButtons.NoYes);
                    if (dw.ShowDialog() == true)
                    {
                        resume = true;
                    }

                }
            }

            if (resume)
            {
                BindTerminal();
            }
        }

        /// <summary>
        /// Обработчик нажатия на кнопку вызова справки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку отвязки от терминала
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UnbindTerminalButton_Click(object sender, RoutedEventArgs e)
        {
            UnbindTerminal();
        }

        /// <summary>
        /// Обработчик нажатия на чекбокс скрытия отгруженных
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HideCompleteCheckbox_Click(object sender, RoutedEventArgs e)
        {
            HideComplete = (bool)HideCompleteCheckbox.IsChecked;
            ShipmentsGrid.UpdateItems();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку печати задания на отгрузку
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrintShipmentOrderBootCardButton_Click(object sender, RoutedEventArgs e)
        {
            PrintShipmenttask();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку настройки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsBurgerMenu.IsOpen = true;
        }

        private void ResponsibleStockDocumentsButton_Click(object sender, RoutedEventArgs e)
        {
            PrintResponsibleStockDocuments();
        }

        private void ShipmentDocumentListButton_Click(object sender, RoutedEventArgs e)
        {
            ShipmentDocumentList();
        }

        /// <summary>
        /// открытие списка машин с допуском на СГП
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenGateButton_Click(object sender, RoutedEventArgs e)
        {
            Central.WM.AddTab("transport_access", "Допуск автотранспорта");
            Central.WM.CheckAddTab<ExpectedCarList>("pending", "Ожидаемый допуск", false, "transport_access", "bottom");
            Central.WM.SetActive("pending");

        }

        private void PrintButtonBurgerMenu_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }

        private void ShipmentCountToPrintButton_Click(object sender, RoutedEventArgs e)
        {
            ShowShipmentToPrint();
        }

        private void ShowAllButton_Click(object sender, RoutedEventArgs e)
        {
            ShowAll();
        }

        private void SettingsButtonMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ShowSettings();
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            var i = new PrintingInterface();
        }
    }
}
