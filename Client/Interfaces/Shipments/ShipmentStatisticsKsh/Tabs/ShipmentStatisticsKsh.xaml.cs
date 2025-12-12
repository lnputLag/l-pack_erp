using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using DevExpress.Data.Svg;
using DevExpress.Xpf.Core.ReflectionExtensions;
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
    /// Статистика по отгрузкам площадки Кашира
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class ShipmentStatisticsKsh : ControlBase
    {
        public ShipmentStatisticsKsh()
        {
            ControlTitle = "Статистика по отгрузкам";
            InitializeComponent();

            //конструктор, будет вызван, когда объект создается
            //здесь создаются все внутренние структуры
            //впервые этот коллбэк будет вызван, когда данный таб станет активным
            //впервые (до этих пор, никакая работа внутри не происходит, что экономит ресурсы)
            OnLoad = () =>
            {
                SetDefaults();
                FormFieldRunAutoupdateTimer();
                ShipmentFailureByDayGridInit();
                PalletShipmentByWeekGridInit();
                PalletLoadingByWeekGridInit();
                ShipmentLoadingByWeekGridInit();
                ShipmentFailureMonthlyRunAutoupdateTimer();
                ShipmentFailureMonthlyLoadItems();

                ProcessPermissions();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                FormFieldAutoUpdateTimer?.Finish();
                ShipmentFailureMonthlyTimer?.Finish();
                ShipmentFailureByDayGrid.Destruct();
                PalletShipmentByWeekGrid.Destruct();
                PalletLoadingByWeekGrid.Destruct();
                ShipmentLoadingByWeekGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                FormFieldItemsAutoUpdate = true;

                ShipmentFailureByDayGrid.ItemsAutoUpdate = true;
                ShipmentFailureByDayGrid.Run();

                PalletShipmentByWeekGrid.ItemsAutoUpdate = true;
                PalletShipmentByWeekGrid.Run();

                PalletLoadingByWeekGrid.ItemsAutoUpdate = true;
                PalletLoadingByWeekGrid.Run();

                ShipmentLoadingByWeekGrid.ItemsAutoUpdate = true;
                ShipmentLoadingByWeekGrid.Run();

                FormFieldLoadItems();
                ShipmentFailureByDayGrid.LoadItems();
                PalletShipmentByWeekGrid.LoadItems();
                PalletLoadingByWeekGrid.LoadItems();
                ShipmentLoadingByWeekGrid.LoadItems();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                FormFieldItemsAutoUpdate = false;
                ShipmentFailureByDayGrid.ItemsAutoUpdate = false;
                PalletShipmentByWeekGrid.ItemsAutoUpdate = false;
                PalletLoadingByWeekGrid.ItemsAutoUpdate = false;
                ShipmentLoadingByWeekGrid.ItemsAutoUpdate = false;
            };
        }

        /// <summary>
        /// Идентификатор площадки
        /// </summary>
        private int FactoryId = 2;

        public ListDataSet FormFieldDataSet { get; set; }

        public ListDataSet ShipmentFailureGridDataSet { get; set; }

        public ListDataSet ShipmentFailureByDayGridDataSet { get; set; }

        public ListDataSet ShipmentFailureByMonthGridDataSet { get; set; }

        public ListDataSet PalletShipmentByWeekGridDataSet { get; set; }

        public ListDataSet PalletLoadingByWeekGridDataSet { get; set; }

        public ListDataSet ShipmentLoadingByWeekGridDataSet { get; set; }

        /// <summary>
        /// Количество попыток выполнения запроса
        /// </summary>
        private int RequestAttempts { get; set; }

        /// <summary>
        /// миллисекунды
        /// </summary>
        private int RequestTimeout { get; set; }

        /// <summary>
        /// Таймер авообновления данных формы
        /// </summary>
        private Common.Timeout FormFieldAutoUpdateTimer { get; set; }

        /// <summary>
        /// Флаг работы автообновления данных формы. Аналог Grid.ItemsAutoUpdate
        /// </summary>
        private bool FormFieldItemsAutoUpdate { get; set; }

        /// <summary>
        /// секунды
        /// </summary>
        private int FormFieldAutoUpdateInterval { get; set; }

        /// <summary>
        /// секунды
        /// </summary>
        private int ShipmentFailureByDayGridAutoUpdateInterval { get; set; }

        /// <summary>
        /// секунды
        /// </summary>
        private int PalletShipmentByWeekGridAutoUpdateInterval { get; set; }

        /// <summary>
        /// секунды
        /// </summary>
        private int PalletLoadingByWeekGridAutoUpdateInterval { get; set; }

        /// <summary>
        /// секунды
        /// </summary>
        private int ShipmentLoadingByWeekGridAutoUpdateInterval { get; set; }

        /// <summary>
        /// Количество цветов в диапазоне цветовой раскраски ячеек таблиц
        /// </summary>
        private int ColorCount { get; set; }

        /// <summary>
        /// Словарь с цветовой расскраской (Green -> Red).
        /// Количество записей должно быть не меньше, чем ColorCount.
        /// </summary>
        private Dictionary<int, string> ColorDictionary = new Dictionary<int, string>()
        {
            {0, "#FF9BF7A6"}, //aafd96 96f796
            {1, "#FFE2FDAF"},
            {2, HColor.Yellow},
            {3, HColor.Orange},
            {4, HColor.Red},
        };

        /// <summary>
        /// Обратный словарь с цветовой расскраской (Red -> Green)
        /// </summary>
        private Dictionary<int, string> ColorDictionaryReverse { get; set; }

        /// <summary>
        /// Словарь, для цветовой расскраски ячеек таблицы PalletLoadingByWeek в зависимости от значения в ячейке
        /// </summary>
        private Dictionary<double, string> PalletLoadingByWeekColorDictionary { get; set; }

        /// <summary>
        /// Максимальное значение среди ячеек таблицы PalletLoadingByWeek
        /// </summary>
        private double PalletLoadingByWeekMax { get; set; }

        /// <summary>
        /// Минимальное значение среди ячеек таблицы PalletLoadingByWeek
        /// </summary>
        private double PalletLoadingByWeekMin { get; set; }

        /// <summary>
        /// Словарь, для цветовой расскраски ячеек таблицы ShipmentLoadingByWeek в зависимости от значения в ячейке
        /// </summary>
        private Dictionary<double, string> ShipmentLoadingByWeekColorDictionary { get; set; }

        /// <summary>
        /// Максимальное значение среди ячеек таблицы ShipmentLoadingByWeek
        /// </summary>
        private double ShipmentLoadingByWeekMax { get; set; }

        /// <summary>
        /// Минимальное значение среди ячеек таблицы ShipmentLoadingByWeek
        /// </summary>
        private double ShipmentLoadingByWeekMin { get; set; }

        /// <summary>
        /// Словарь, для цветовой расскраски ячеек таблицы PalletShipmentByWeek в зависимости от значения в ячейке
        /// </summary>
        private Dictionary<double, string> PalletShipmentByWeekColorDictionary { get; set; }

        /// <summary>
        /// Максимальное значение среди ячеек таблицы PalletShipmentByWeek
        /// </summary>
        private double PalletShipmentByWeekMax { get; set; }

        /// <summary>
        /// Минимальное значение среди ячеек таблицы PalletShipmentByWeek
        /// </summary>
        private double PalletShipmentByWeekMin { get; set; }

        /// <summary>
        /// Таймер на получение данных по срывам отгрузок по месяцам для текущего месяца
        /// </summary>
        public Common.Timeout ShipmentFailureMonthlyTimer { get; set; }

        /// <summary>
        /// Интервал отправки запроса на получение данных по срывам отгрузок по месяцам для текущего месяца, секунды
        /// </summary>
        public int ShipmentFailureMonthlyInterval { get; set; }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            FormFieldDataSet = new ListDataSet();
            //ShipmentFailureGridDataSet = new ListDataSet();
            ShipmentFailureByDayGridDataSet = new ListDataSet();
            ShipmentFailureByMonthGridDataSet = new ListDataSet();
            PalletShipmentByWeekGridDataSet = new ListDataSet();
            PalletLoadingByWeekGridDataSet = new ListDataSet();
            ShipmentLoadingByWeekGridDataSet = new ListDataSet();

            RequestAttempts = 3;
            RequestTimeout = 30000;

            FormFieldAutoUpdateInterval = 300;
            ShipmentFailureByDayGridAutoUpdateInterval = 300;
            PalletShipmentByWeekGridAutoUpdateInterval = 300;
            PalletLoadingByWeekGridAutoUpdateInterval = 300;
            ShipmentLoadingByWeekGridAutoUpdateInterval = 300;

            ColorCount = 5;
            PalletLoadingByWeekColorDictionary = new Dictionary<double, string>();
            ShipmentLoadingByWeekColorDictionary = new Dictionary<double, string>();
            PalletShipmentByWeekColorDictionary = new Dictionary<double, string>();

            ColorDictionaryReverse = new Dictionary<int, string>();
            for (int i = 0; i < ColorDictionary.Count; i++)
            {
                ColorDictionaryReverse.Add(i, ColorDictionary[ColorDictionary.Count - 1 - i]);
            }

            ShipmentFailureMonthlyInterval = 3600;
        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel("[erp]shipment_statistics");
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
        }

        /// <summary>
        /// Получение значения цветовой расскраски ячейки, в зависимости от значения в  ячейке и соответсвтующего словаря расскраски
        /// </summary>
        /// <param name="value"></param>
        /// <param name="colorDictionary"></param>
        /// <returns></returns>
        public string GetColor(double value, Dictionary<double, string> colorDictionary)
        {
            string color = "";

            foreach (var item in colorDictionary)
            {
                if (value >= item.Key)
                {
                    color = item.Value;
                }
            }

            if (string.IsNullOrEmpty(color))
            {
                if (value > 0 && value < colorDictionary.Min(x => x.Key))
                {
                    color = colorDictionary[colorDictionary.Min(x => x.Key)];
                }
            }

            return color;
        }

        private void FormFieldRunAutoupdateTimer()
        {
            if (FormFieldAutoUpdateInterval > 0)
            {
                FormFieldAutoUpdateTimer = new Common.Timeout(
                    FormFieldAutoUpdateInterval,
                    () =>
                    {
                        if (FormFieldItemsAutoUpdate)
                        {
                            FormFieldLoadItems();
                        }
                    },
                    true,
                    false
                );
                {
                    FormFieldAutoUpdateTimer.Restart();
                }
            }
        }

        public async void FormFieldLoadItems()
        {
            var p = new Dictionary<string, string>();
            p.Add("FACTORY_ID", $"{FactoryId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Statistics");
            q.Request.SetParam("Action", "GetShipmentPlanDaily");
            q.Request.SetParams(p);

            q.Request.Timeout = RequestTimeout;
            q.Request.Attempts = RequestAttempts;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    FormFieldDataSet = ListDataSet.Create(result, "ITEMS");
                    FormFieldUpdateItems(FormFieldDataSet);
                }
            }
            else
            {
                q.SilentErrorProcess = true;
                q.ProcessError();
            }
        }

        public void FormFieldUpdateItems(ListDataSet dataSet)
        {
            if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
            {
                Dictionary<string, string> data = dataSet.Items.First();

                ShipmentAllowedCount.Text = data.CheckGet("SHIPMENT_ALLOWED_PLAN_CNT").ToDouble().ToString("###,###,##0");
                ShipmentPlanCount.Text = data.CheckGet("SHIPMENT_PLAN_CNT").ToDouble().ToString("###,###,##0");
                ShipmentFactCount.Text = data.CheckGet("SHIPPED_CNT").ToDouble().ToString("###,###,##0");
                PalletPlanCount.Text = data.CheckGet("SHIPMENT_PALLET_QTY").ToDouble().ToString("###,###,##0");
                PalletFactCount.Text = data.CheckGet("SHIPPED_PALLET_QTY").ToDouble().ToString("###,###,##0");
                SquarePlanCount.Text = data.CheckGet("SHIPMENT_SQUARE").ToDouble().ToString("###,###,##0");
                SquareFactCount.Text = data.CheckGet("SHIPPED_SQUARE").ToDouble().ToString("###,###,##0");
                PalletLoadingFact.Text = data.CheckGet("AVG_SHIPMENT_PALLET_TIME").ToDouble().ToString("###,###,##0.0");
                ShipmentLoadingFact.Text = data.CheckGet("AVG_SHIPMENT_TIME").ToDouble().ToString("###,###,##0.0");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void ShipmentFailureByDayGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Дата",
                        Path="DT",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format = "dd.MM.yyyy",
                        Width2=11,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    if (row.CheckGet("CURRENT_MONTH").ToInt() == 0)
                                    {
                                        color = HColor.Gray;
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
                        Header="Площадь, м2",
                        Group="Отгружено",
                        Description="Суммарная площадь отгруженной продукции",
                        Path="SQUARE",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2=15,
                        Totals = (List<Dictionary<string, string>> rows) =>
                        {
                            double result=0;
                            if(rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    if (row.CheckGet("CURRENT_MONTH").ToInt() == 1)
                                    {
                                        result += row.CheckGet("SQUARE").ToDouble();
                                    }
                                }
                            }
                            return result;
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Машин",
                        Group="Отгружено",
                        Description="Количество начатых отгрузок",
                        Path="SHIPPED_CNT",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2=11,
                        Totals = (List<Dictionary<string, string>> rows) =>
                        {
                            double result=0;
                            if(rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    if (row.CheckGet("CURRENT_MONTH").ToInt() == 1)
                                    {
                                        result += row.CheckGet("SHIPPED_CNT").ToDouble();
                                    }
                                }
                            }
                            return result;
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Склад",
                        Group="Сорвано",
                        Description="Сорвано отгрузок по вине склада",
                        Path="FAIL_STOCK_CNT",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2=10,
                        Totals = (List<Dictionary<string, string>> rows) =>
                        {
                            double result=0;
                            if(rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    if (row.CheckGet("CURRENT_MONTH").ToInt() == 1)
                                    {
                                        result += row.CheckGet("FAIL_STOCK_CNT").ToDouble();
                                    }
                                }
                            }
                            return result;
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Производство",
                        Group="Сорвано",
                        Description="Сорвано отгрузок по виде производства",
                        Path="FAIL_PROD_CNT",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2=10,
                        Totals = (List<Dictionary<string, string>> rows) =>
                        {
                            double result=0;
                            if(rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    if (row.CheckGet("CURRENT_MONTH").ToInt() == 1)
                                    {
                                        result += row.CheckGet("FAIL_PROD_CNT").ToDouble();
                                    }
                                }
                            }
                            return result;
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Самовывоз",
                        Group="Сорвано",
                        Description="Сорвано отгрузок самовывозом",
                        Path="FAIL_SELFSHIP_CNT",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2=10,
                        Totals = (List<Dictionary<string, string>> rows) =>
                        {
                            double result=0;
                            if(rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    if (row.CheckGet("CURRENT_MONTH").ToInt() == 1)
                                    {
                                        result += row.CheckGet("FAIL_SELFSHIP_CNT").ToDouble();
                                    }
                                }
                            }
                            return result;
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Доставка",
                        Group="Сорвано",
                        Description="Сорвано отгрузок с доставкой",
                        Path="FAIL_DELIVERY_CNT",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2=10,
                        Totals = (List<Dictionary<string, string>> rows) =>
                        {
                            double result=0;
                            if(rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    if (row.CheckGet("CURRENT_MONTH").ToInt() == 1)
                                    {
                                        result += row.CheckGet("FAIL_DELIVERY_CNT").ToDouble();
                                    }
                                }
                            }
                            return result;
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Path="ID",
                        ColumnType=ColumnTypeRef.String,
                        Width2=5,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Текущий месяц",
                        Path="CURRENT_MONTH",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=0,
                        Hidden=true,
                        Visible=false,
                    },
                };
                ShipmentFailureByDayGrid.SetColumns(columns);
                ShipmentFailureByDayGrid.OnLoadItems = ShipmentFailureByDayGridLoadItems;

                ShipmentFailureByDayGrid.SetPrimaryKey("ID");

                ShipmentFailureByDayGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                ShipmentFailureByDayGrid.AutoUpdateInterval = ShipmentFailureByDayGridAutoUpdateInterval;

                ShipmentFailureByDayGrid.GridView.VerticalScrollbarVisibility = ScrollBarVisibility.Disabled;
                ShipmentFailureByDayGrid.GridView.HorizontalScrollbarVisibility = ScrollBarVisibility.Disabled;

                ShipmentFailureByDayGrid.GridView.FontSize = 15;

                ShipmentFailureByDayGrid.Commands = Commander;

                ShipmentFailureByDayGrid.Init();
            }
        }

        public async void ShipmentFailureByDayGridLoadItems()
        {
            var p = new Dictionary<string, string>();
            p.Add("FACTORY_ID", $"{FactoryId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Statistics");
            q.Request.SetParam("Action", "ListShipmentFailed");
            q.Request.SetParams(p);

            q.Request.Timeout = RequestTimeout;
            q.Request.Attempts = RequestAttempts;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    ShipmentFailureByDayGridDataSet = ListDataSet.Create(result, "ITEMS");

                    if (ShipmentFailureGridDataSet == null)
                    {
                        ShipmentFailureGridDataSet = ShipmentFailureByDayGridDataSet.Clone();
                        ShipmentFailureGridDataSet.Items.Clear();
                    }

                    ShipmentFailureByDayGridUpdateItems();
                }
            }
            else
            {
                q.SilentErrorProcess = true;
                q.ProcessError();
            }
        }

        public async void ShipmentFailureMonthlyLoadItems()
        {
            var p = new Dictionary<string, string>();
            p.Add("FACTORY_ID", $"{FactoryId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Statistics");
            q.Request.SetParam("Action", "ListShipmentFailedMonthly");
            q.Request.SetParams(p);

            q.Request.Timeout = 90000;
            q.Request.Attempts = RequestAttempts;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    ShipmentFailureByMonthGridDataSet = ListDataSet.Create(result, "ITEMS");

                    if (ShipmentFailureGridDataSet == null)
                    {
                        ShipmentFailureGridDataSet = ShipmentFailureByMonthGridDataSet.Clone();
                        ShipmentFailureGridDataSet.Items.Clear();
                    }

                    {
                        var ds = ListDataSet.Create(result, "BEST");
                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                        {
                            SquareBestCount.Text = ds.Items[0].CheckGet("SHIPPED_BEST_SQUARE").ToDouble().ToString("###,###,##0");
                            ShipmentBestCount.Text = ds.Items[0].CheckGet("SHIPPED_BEST_CNT").ToDouble().ToString("###,###,##0");
                        }
                    }

                    ShipmentFailureByDayGridUpdateItems();
                }
            }
            else
            {
                q.SilentErrorProcess = true;
                q.ProcessError();
            }
        }

        public void ShipmentFailureByDayGridUpdateItems()
        {
            if (ShipmentFailureGridDataSet != null)
            {
                ShipmentFailureGridDataSet.Items.Clear();

                ShipmentFailureGridDataSet.Items.AddRange(ShipmentFailureByMonthGridDataSet.Items);
                ShipmentFailureGridDataSet.Items.AddRange(ShipmentFailureByDayGridDataSet.Items);

                ShipmentFailureByDayGrid.UpdateItems(ShipmentFailureGridDataSet);
            }
        }

        public void ShipmentFailureMonthlyRunAutoupdateTimer()
        {
            if (ShipmentFailureMonthlyInterval > 0)
            {
                ShipmentFailureMonthlyTimer = new Common.Timeout(
                    ShipmentFailureMonthlyInterval,
                    () =>
                    {
                        ShipmentFailureMonthlyLoadItems();
                    },
                    true,
                    false
                );
                {
                    ShipmentFailureMonthlyTimer.Restart();
                }
            }
        }

        public void PalletShipmentByWeekGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Неделя",
                        Path="WEEK",
                        ColumnType=ColumnTypeRef.String,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Пн.",
                        Path="PALLET_MON",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2=6,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    color = GetColor(row.CheckGet("PALLET_MON").ToDouble(), PalletShipmentByWeekColorDictionary);

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
                        Header="Вт.",
                        Path="PALLET_TUE",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2=6,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    color = GetColor(row.CheckGet("PALLET_TUE").ToDouble(), PalletShipmentByWeekColorDictionary);

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
                        Header="Ср.",
                        Path="PALLET_WED",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2=6,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    color = GetColor(row.CheckGet("PALLET_WED").ToDouble(), PalletShipmentByWeekColorDictionary);

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
                        Header="Чт.",
                        Path="PALLET_THU",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2=6,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    color = GetColor(row.CheckGet("PALLET_THU").ToDouble(), PalletShipmentByWeekColorDictionary);

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
                        Header="Пт.",
                        Path="PALLET_FRI",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2=6,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    color = GetColor(row.CheckGet("PALLET_FRI").ToDouble(), PalletShipmentByWeekColorDictionary);

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
                        Header="Сб.",
                        Path="PALLET_SAT",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2=6,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    color = GetColor(row.CheckGet("PALLET_SAT").ToDouble(), PalletShipmentByWeekColorDictionary);

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
                        Header="Вс.",
                        Path="PALLET_SUN",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2=6,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    color = GetColor(row.CheckGet("PALLET_SUN").ToDouble(), PalletShipmentByWeekColorDictionary);

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
                        Header="Всего",
                        Path="PALLET_TOTAL",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2=8,
                    },

                };
                PalletShipmentByWeekGrid.SetColumns(columns);
                PalletShipmentByWeekGrid.OnLoadItems = PalletShipmentByWeekGridLoadItems;
                PalletShipmentByWeekGrid.SetPrimaryKey("WEEK");
                PalletShipmentByWeekGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                PalletShipmentByWeekGrid.AutoUpdateInterval = PalletShipmentByWeekGridAutoUpdateInterval;

                PalletShipmentByWeekGrid.GridView.VerticalScrollbarVisibility = ScrollBarVisibility.Disabled;

                PalletShipmentByWeekGrid.GridView.FontSize = 15;

                PalletShipmentByWeekGrid.Commands = Commander;

                PalletShipmentByWeekGrid.Init();
            }
        }

        public async void PalletShipmentByWeekGridLoadItems()
        {
            var p = new Dictionary<string, string>();
            p.Add("FACTORY_ID", $"{FactoryId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Statistics");
            q.Request.SetParam("Action", "ListPalletShipmentCountWeekly");
            q.Request.SetParams(p);

            q.Request.Timeout = RequestTimeout;
            q.Request.Attempts = RequestAttempts;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "VALUES");
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        PalletShipmentByWeekMax = ds.Items.First().CheckGet("MAX_VALUE").ToDouble();
                        PalletShipmentByWeekMin = ds.Items.First().CheckGet("MIN_VALUE").ToDouble();

                        PalletShipmentByWeekColorDictionary = new Dictionary<double, string>();
                        double step = Math.Round(((double)PalletShipmentByWeekMax - (double)PalletShipmentByWeekMin) / (double)ColorCount, 0);
                        for (int i = 0; i < ColorCount; i++)
                        {
                            PalletShipmentByWeekColorDictionary.Add(Math.Round(PalletShipmentByWeekMin + (step * i), 0), ColorDictionaryReverse[i]);
                        }

                        PalletBestCount.Text = PalletShipmentByWeekMax.ToString("###,###,##0");
                    }

                    PalletShipmentByWeekGridDataSet = ListDataSet.Create(result, "ITEMS");
                    PalletShipmentByWeekGrid.UpdateItems(PalletShipmentByWeekGridDataSet);
                }
            }
            else
            {
                q.SilentErrorProcess = true;
                q.ProcessError();
            }
        }

        public void PalletLoadingByWeekGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Неделя",
                        Path="WEEK",
                        ColumnType=ColumnTypeRef.String,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Пн.",
                        Path="PALLET_TIME_MON",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N1",
                        Width2=5,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    color = GetColor(row.CheckGet("PALLET_TIME_MON").ToDouble(), PalletLoadingByWeekColorDictionary);

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
                        Header="Вт.",
                        Path="PALLET_TIME_TUE",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N1",
                        Width2=5,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    color = GetColor(row.CheckGet("PALLET_TIME_TUE").ToDouble(), PalletLoadingByWeekColorDictionary);

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
                        Header="Ср.",
                        Path="PALLET_TIME_WED",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N1",
                        Width2=5,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    color = GetColor(row.CheckGet("PALLET_TIME_WED").ToDouble(), PalletLoadingByWeekColorDictionary);

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
                        Header="Чт.",
                        Path="PALLET_TIME_THU",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N1",
                        Width2=5,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    color = GetColor(row.CheckGet("PALLET_TIME_THU").ToDouble(), PalletLoadingByWeekColorDictionary);

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
                        Header="Пт.",
                        Path="PALLET_TIME_FRI",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N1",
                        Width2=5,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    color = GetColor(row.CheckGet("PALLET_TIME_FRI").ToDouble(), PalletLoadingByWeekColorDictionary);

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
                        Header="Сб.",
                        Path="PALLET_TIME_SAT",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N1",
                        Width2=5,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    color = GetColor(row.CheckGet("PALLET_TIME_SAT").ToDouble(), PalletLoadingByWeekColorDictionary);

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
                        Header="Вс.",
                        Path="PALLET_TIME_SUN",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N1",
                        Width2=5,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    color = GetColor(row.CheckGet("PALLET_TIME_SUN").ToDouble(), PalletLoadingByWeekColorDictionary);

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
                        Header="Среднее",
                        Path="PALLET_TIME_AVG",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N1",
                        Width2=6,
                    },

                };
                PalletLoadingByWeekGrid.SetColumns(columns);
                PalletLoadingByWeekGrid.OnLoadItems = PalletLoadingByWeekGridLoadItems;
                PalletLoadingByWeekGrid.SetPrimaryKey("WEEK");
                PalletLoadingByWeekGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                PalletLoadingByWeekGrid.AutoUpdateInterval = PalletLoadingByWeekGridAutoUpdateInterval;

                PalletLoadingByWeekGrid.GridView.VerticalScrollbarVisibility = ScrollBarVisibility.Disabled;

                PalletLoadingByWeekGrid.GridView.FontSize = 15;

                PalletLoadingByWeekGrid.Commands = Commander;

                PalletLoadingByWeekGrid.Init();
            }
        }

        public async void PalletLoadingByWeekGridLoadItems()
        {
            var p = new Dictionary<string, string>();
            p.Add("FACTORY_ID", $"{FactoryId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Statistics");
            q.Request.SetParam("Action", "ListPalletLoadingTimeWeekly");
            q.Request.SetParams(p);

            q.Request.Timeout = RequestTimeout;
            q.Request.Attempts = RequestAttempts;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "VALUES");
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        PalletLoadingByWeekMax = ds.Items.First().CheckGet("MAX_VALUE").ToDouble();
                        PalletLoadingByWeekMin = ds.Items.First().CheckGet("MIN_VALUE").ToDouble();

                        PalletLoadingByWeekColorDictionary = new Dictionary<double, string>();
                        double step = Math.Round(((double)PalletLoadingByWeekMax - (double)PalletLoadingByWeekMin) / (double)ColorCount, 2);
                        for (int i = 0; i < ColorCount; i++)
                        {
                            PalletLoadingByWeekColorDictionary.Add(Math.Round(PalletLoadingByWeekMin + (step * i), 2), ColorDictionary[i]);
                        }

                        PalletLoadingBest.Text = PalletLoadingByWeekMin.ToString("###,###,##0.0");
                    }

                    PalletLoadingByWeekGridDataSet = ListDataSet.Create(result, "ITEMS");
                    PalletLoadingByWeekGrid.UpdateItems(PalletLoadingByWeekGridDataSet);
                }
            }
            else
            {
                q.SilentErrorProcess = true;
                q.ProcessError();
            }
        }

        public void ShipmentLoadingByWeekGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Неделя",
                        Path="WEEK",
                        ColumnType=ColumnTypeRef.String,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Пн.",
                        Path="SHIPMENT_TIME_MON",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N1",
                        Width2=5,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    color = GetColor(row.CheckGet("SHIPMENT_TIME_MON").ToDouble(), ShipmentLoadingByWeekColorDictionary);

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
                        Header="Вт.",
                        Path="SHIPMENT_TIME_TUE",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N1",
                        Width2=5,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    color = GetColor(row.CheckGet("SHIPMENT_TIME_TUE").ToDouble(), ShipmentLoadingByWeekColorDictionary);

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
                        Header="Ср.",
                        Path="SHIPMENT_TIME_WED",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N1",
                        Width2=5,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    color = GetColor(row.CheckGet("SHIPMENT_TIME_WED").ToDouble(), ShipmentLoadingByWeekColorDictionary);

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
                        Header="Чт.",
                        Path="SHIPMENT_TIME_THU",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N1",
                        Width2=5,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    color = GetColor(row.CheckGet("SHIPMENT_TIME_THU").ToDouble(), ShipmentLoadingByWeekColorDictionary);

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
                        Header="Пт.",
                        Path="SHIPMENT_TIME_FRI",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N1",
                        Width2=5,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    color = GetColor(row.CheckGet("SHIPMENT_TIME_FRI").ToDouble(), ShipmentLoadingByWeekColorDictionary);

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
                        Header="Сб.",
                        Path="SHIPMENT_TIME_SAT",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N1",
                        Width2=5,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    color = GetColor(row.CheckGet("SHIPMENT_TIME_SAT").ToDouble(), ShipmentLoadingByWeekColorDictionary);

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
                        Header="Вс.",
                        Path="SHIPMENT_TIME_SUN",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N1",
                        Width2=5,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    color = GetColor(row.CheckGet("SHIPMENT_TIME_SUN").ToDouble(), ShipmentLoadingByWeekColorDictionary);

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
                        Header="Среднее",
                        Path="SHIPMENT_TIME_AVG",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N1",
                        Width2=6,
                    },

                };
                ShipmentLoadingByWeekGrid.SetColumns(columns);
                ShipmentLoadingByWeekGrid.OnLoadItems = ShipmentLoadingByWeekGridLoadItems;
                ShipmentLoadingByWeekGrid.SetPrimaryKey("WEEK");
                ShipmentLoadingByWeekGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                ShipmentLoadingByWeekGrid.AutoUpdateInterval = ShipmentLoadingByWeekGridAutoUpdateInterval;

                ShipmentLoadingByWeekGrid.GridView.VerticalScrollbarVisibility = ScrollBarVisibility.Disabled;

                ShipmentLoadingByWeekGrid.GridView.FontSize = 15;

                ShipmentLoadingByWeekGrid.Commands = Commander;

                ShipmentLoadingByWeekGrid.Init();
            }
        }

        public async void ShipmentLoadingByWeekGridLoadItems()
        {
            var p = new Dictionary<string, string>();
            p.Add("FACTORY_ID", $"{FactoryId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Statistics");
            q.Request.SetParam("Action", "ListShipmentLoadingTimeWeekly");
            q.Request.SetParams(p);

            q.Request.Timeout = RequestTimeout;
            q.Request.Attempts = RequestAttempts;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "VALUES");
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        ShipmentLoadingByWeekMax = ds.Items.First().CheckGet("MAX_VALUE").ToDouble();
                        ShipmentLoadingByWeekMin = ds.Items.First().CheckGet("MIN_VALUE").ToDouble();

                        ShipmentLoadingByWeekColorDictionary = new Dictionary<double, string>();
                        double step = Math.Round(((double)ShipmentLoadingByWeekMax - (double)ShipmentLoadingByWeekMin) / (double)ColorCount, 2);
                        for (int i = 0; i < ColorCount; i++)
                        {
                            ShipmentLoadingByWeekColorDictionary.Add(Math.Round(ShipmentLoadingByWeekMin + (step * i), 2), ColorDictionary[i]);
                        }

                        ShipmentLoadingBest.Text = ShipmentLoadingByWeekMin.ToString("###,###,##0.0");
                    }

                    ShipmentLoadingByWeekGridDataSet = ListDataSet.Create(result, "ITEMS");
                    ShipmentLoadingByWeekGrid.UpdateItems(ShipmentLoadingByWeekGridDataSet);
                }
            }
            else
            {
                q.SilentErrorProcess = true;
                q.ProcessError();
            }
        }
    }
}
