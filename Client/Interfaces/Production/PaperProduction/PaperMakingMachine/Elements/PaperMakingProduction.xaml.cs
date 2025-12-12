using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Client.Assets.HighLighters;
using System.Windows.Input;
using Newtonsoft.Json;
using System.Linq;
using System;
using System.Data;
using Org.BouncyCastle.Asn1.Crmf;
using static Client.Interfaces.Main.DataGridHelperColumn;
using System.ComponentModel;
using System.Security.Cryptography;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using NPOI.SS.Formula.Functions;
using SixLabors.ImageSharp.PixelFormats;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// блок "Произведено"
    /// </summary>
    /// <author>greshnyh_ni</author>   
    public partial class PaperMakingProduction : ControlBase
    {

        private List<Dictionary<string, string>> DataConfigOptionsList { get; set; }
        private bool GetDateConfigResult { get; set; }
        public int MachineId { get; internal set; }

        public PaperMakingProduction()
        {
            InitializeComponent();

            OnLoad = () =>
            {
                SetDefaults();
                Init();
                ProductionGridInit();
                TambourInfoGridInit();
            };


            OnUnload = () =>
            {
                ProductionGrid.Destruct();
                TambourInfoGrid.Destruct();
            };

            OnFocusGot = () =>
            {
                ProductionGrid.ItemsAutoUpdate = true;
                ProductionGrid.Run();
                TambourInfoGrid.ItemsAutoUpdate = true;
                TambourInfoGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                ProductionGrid.ItemsAutoUpdate = false;
                TambourInfoGrid.ItemsAutoUpdate = false;
            };
        }


        /// <summary>
        /// настройки по умолчанию 
        /// </summary>
        public void SetDefaults()
        {
            DataConfigOptionsList = new List<Dictionary<string, string>>();
            GetDateConfigResult = false;
        }

        /// <summary>
        /// Инициализация грида
        /// </summary>
        public void Init()
        {
            double nScale = 1.5;
            ProductionGrid.LayoutTransform = new ScaleTransform(nScale, nScale);
            TambourInfoGrid.LayoutTransform = new ScaleTransform(nScale, nScale);
        }

        /// <summary>
        /// инициализация грида ProductionGrid
        /// </summary>
        public void ProductionGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header = "ORD",
                        Path = "ORD",
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 2,
                        Visible = false,

                    },
                    new DataGridHelperColumn
                    {
                        Header = "Произведено",
                        Path = "NAME",
                        ColumnType = ColumnTypeRef.String,
                        Width2 = 13,

                    },
                    new DataGridHelperColumn
                    {
                        Header = "За смену",
                        Path = "SHIFT",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2 = 9,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Предыдущую",
                        Path = "SHIFT_PREV",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2 = 12,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Неделю",
                        Path = "WEEK",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Месяц",
                        Path = "MONTH",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2 = 8,
                    },
                };

                ProductionGrid.SetColumns(columns);
                ProductionGrid.SetPrimaryKey("ORD");
                //ProductionGrid.SetSorting("ORD", ListSortDirection.Ascending);
                ProductionGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                ProductionGrid.AutoUpdateInterval = 0;

                //данные грида
                ProductionGrid.OnLoadItems = ProductionGridLoadItems;
                ProductionGrid.Commands = Commander;
                ProductionGrid.Init();
            }
        }

        /// <summary>
        /// обновляем данные
        /// </summary>
        public void LoadItems()
        {
            ProductionGrid.LoadItems();
            TambourInfoGrid.LoadItems();
        }


        /// <summary>
        /// Загрузка данных для ProductionGrid 
        /// </summary>
        public async void ProductionGridLoadItems()
        {
            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_ST", MachineId.ToString());
            }

            try
            {
                var q = await LPackClientQuery.DoQueryAsync("ProductionPm", "Monitoring", "ProductionList", "ITEMS", p);

                if (q.Answer.Status == 0)
                {
                    if (q.Answer.QueryResult != null)
                    {
                        // заполняем грид данными
                        ProductionGrid.UpdateItems(q.Answer.QueryResult);

                        // заполняем данные по прогнозу
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                
                            if (MachineId == 1716)
                            {
                                var ds = ListDataSet.Create(result, "INFO");
                                TambourInfoGrid.UpdateItems(ds);
                            }
                            
                            //BDM1_CURRENT_WIDTH
                            var ds1 = ListDataSet.Create(result, "V1");
                            var V1 = 0;
                            if (ds1.Items.Count > 0)
                                V1 = ds1.Items[0].CheckGet("PARAM_VALUE").Replace(" ", "").ToInt();

                            //BDM1_FORECUST
                            var ds2 = ListDataSet.Create(result, "V2");
                            var V2 = ds2.Items[0].CheckGet("PARAM_VALUE").ToString().Replace(" ", "").ToInt();

                            //BDM1_OUTSTRIP
                            var ds3 = ListDataSet.Create(result, "V3");
                            var V3 = ds3.Items[0].CheckGet("PARAM_VALUE").ToString().Replace(" ", "").ToInt();

                            //BDM1_PLAN_PERFOMANCE_ALL_WEEK
                            var ds4 = ListDataSet.Create(result, "V4");
                            var V4 = ds4.Items[0].CheckGet("PARAM_VALUE").ToString().Replace(" ", "").ToInt();

                            //BDM1_PLAN_PERFOMANCE_WEEK
                            var ds5 = ListDataSet.Create(result, "V5");
                            var V5 = ds5.Items[0].CheckGet("PARAM_VALUE").ToString().Replace(" ", "").ToInt();

                            //BDM1_FACT_PERFOMANCE_WEEK
                            var ds6 = ListDataSet.Create(result, "V6");
                            var V6 = ds6.Items[0].CheckGet("PARAM_VALUE").ToString().Replace(" ", "").ToInt();

                            //BDM1_PERFORMANCE_M_M
                            var ds7 = ListDataSet.Create(result, "V7");
                            var V7 = ds7.Items[0].CheckGet("PARAM_VALUE").ToString().Replace(" ", "").ToInt();

                            //BDM1_PERFORMANCE_T_S
                            var ds8 = ListDataSet.Create(result, "V8");
                            var V8 = ds8.Items[0].CheckGet("PARAM_VALUE").ToString().Replace(" ", "").ToInt();

                            var ds9 = ListDataSet.Create(result, "V9");
                            var b = ds9.Items[0].CheckGet("B").ToInt();
                            var ro = ds9.Items[0].CheckGet("RO").ToInt();
                            var bdmSpeed = ds9.Items[0].CheckGet("BDM_SPEED").ToInt();
                            var idPz = ds9.Items[0].CheckGet("ID_PZ").ToInt();

                            // прогноз на месяц
                            ForecastMonth1.Text = ((int)Math.Round((double)V2 / 1000)).ToString();
                            ForecastMonth2.Text = Math.Abs((int)Math.Round(((double)V3 / 1000))).ToString();

                            if (V3 > 0)
                            {
                                ForecastMonth.Text = "Отставание";
                                ForecastMonth1.Foreground = HColor.RedFG.ToBrush();
                                ForecastMonth2.Foreground = HColor.RedFG.ToBrush();
                            }
                            else
                            {
                                ForecastMonth.Text = "Опережение";
                                ForecastMonth1.Foreground = HColor.GreenFG.ToBrush();
                                ForecastMonth2.Foreground = HColor.GreenFG.ToBrush();
                            }

                            // прогноз на неделю
                            ForecastWeek1.Text = V4.ToString();
                            var fl = V6 - V5;
                            ForecastWeek2.Text = ((int)Math.Round((double)fl)).ToString();

                            if (fl < 0)
                            {
                                ForecastWeek.Text = "Отставание";
                                ForecastWeek1.Foreground = HColor.RedFG.ToBrush();
                                ForecastWeek2.Foreground = HColor.RedFG.ToBrush();
                            }
                            else
                            {
                                ForecastWeek.Text = "Опережение";
                                ForecastWeek1.Foreground = HColor.GreenFG.ToBrush();
                                ForecastWeek2.Foreground = HColor.GreenFG.ToBrush();
                            }


                            PrsPerSmena.Text = $"{V8}";

                            // Скорость БДМ по плану из ПЗ LV5                           
                            PlanM.Text = bdmSpeed.ToString();
                            // Скорость БДМ реальная LV1
                            FaktM.Text = V7.ToString();

                            //LV2
                            PrsPerSmena.Text = (V8).ToString();
                            //LV3
                            PrsPerHour.Text = Math.Round(((double)V8 / 12), 1).ToString();

                            if (MachineId.ToInt() == 1716)
                            {
                                // Котосонов, производительность берем при формате 4800 и плотности из верхнего задания
                                //lblQtyNaklatPerHour.Caption := FloatToStr2((qrQtyNakat.FieldByName('ro').AsInteger / 1000) * 4.8 * speed_current * 0.06, 1) + ' т/ч';
                                //lblQtyNaklatPerSmena.Caption := FloatToStr2((qrQtyNakat.FieldByName('ro').AsInteger / 1000) * 4.8 * speed_current * 0.06 * 12, 0) + ' т/смену';
                                //LV4.Caption := FloatToStr2((qrQtyNakat.FieldByName('ro').AsInteger / 1000) * 4.8 * speed_current * 0.06 * 24, 0);
                                //LV6.Caption := FloatToStr2((QueryProiz_zad.FieldByName('BDM_Speed').asFloat / 1000) * qrQtyNakat.FieldByName('ro').AsInteger * 4.8 * 0.06 * 24, 0);

                                //lblQtyNaklatPerHour
                                NakatPerHour.Text = Math.Round((((double)ro / 1000) * 4.8 * V7 * 0.06), 1).ToString();
                                //lblQtyNaklatPerSmena
                                NakatPerSmena.Text = Math.Round((((double)ro / 1000) * 4.8 * V7 * 0.06 * 12), 0).ToString();
                                //LV4
                                FaktT.Text = (Math.Round(((double)ro / 1000) * 4.8 * V7 * 0.06 * 24, 0)).ToString();
                                //LV6
                                PlanT.Text = (Math.Round(((double)bdmSpeed / 1000) * ro * 4.8 * 0.06 * 24, 0)).ToString();

                                // производительность ПРС, считается от суммарного формата всех рулонов
                                //LV3.Caption := FloatToStr2((qrQtyNakat.FieldByName('ro').AsInteger / 1000) * (qrQtyNakat.FieldByName('b').AsInteger / 1000) * speed_current * 0.06, 1) + ' т/ч';
                                //LV2.Caption := FloatToStr2((qrQtyNakat.FieldByName('ro').AsInteger / 1000) * (qrQtyNakat.FieldByName('b').AsInteger / 1000) * speed_current * 0.06 * 12, 0) + ' т/смену';
                                PrsPerHour.Text = Math.Round(((double)ro / 1000) * ((double)(b / 1000) * V7 * 0.06), 1).ToString();
                                PrsPerSmena.Text = Math.Round(((double)ro / 1000) * ((double)(b / 1000) * V7 * 0.06 * 12), 0).ToString();
                            }
                            else
                            {
                                // производительность ПРС (факт)
                                //LV4
                                FaktT.Text = (Math.Round((double)V8 * 2, 0)).ToString();
                                //LV6.Caption := IntToStr(round(QueryProiz_zad.FieldByName('BDM_Speed').asFloat * B * Ro / 1000 / 1000 / 1000 * 60 * 24));
                                PlanT.Text = (Math.Round(((double)bdmSpeed * b * ro / 1000 / 1000 / 1000 * 60 * 24), 0)).ToString();

                                //lblQtyNaklatPerHour.Caption := FloatToStr2((qrQtyNakat.FieldByName('ro').AsInteger / 1000) * (StrToInt(s) / 1000) * (speed_current * 60 / 1000), 2) + ' т/ч';
                                NakatPerHour.Text = Math.Round((((double)ro / 1000) * ((double)V1 / 1000) * V7 * 0.06), 2).ToString();
                                //lblQtyNaklatPerSmena.Caption := FloatToStr2((qrQtyNakat.FieldByName('ro').AsInteger / 1000) * (StrToInt(s) / 1000) * (speed_current * 60 * 12 / 1000), 1) + ' т/смену';
                                NakatPerSmena.Text = Math.Round((((double)ro / 1000) * ((double)V1 / 1000) * V7 * 0.06 * 12), 1).ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }

        }

        /// <summary>
        /// инициализация грида TambourInfoGrid
        /// </summary>
        public void TambourInfoGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header = "№ тамбура",
                        Path = "NUM_TAMBOUR",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2 = 10,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Вес тамбура",
                        Path = "ROLL_WEIGHT",
                        ColumnType = ColumnTypeRef.Double,
                        Width2 = 10,
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Формат 1",
                        Path = "F1",
                        ColumnType = ColumnTypeRef.Double,
                        Width2 = 10,
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Формат 2",
                        Path = "F2",
                        ColumnType = ColumnTypeRef.Double,
                        Width2 = 10,
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Формат 3",
                        Path = "F3",
                        ColumnType = ColumnTypeRef.Double,
                        Width2 = 10,
                        TotalsType=TotalsTypeRef.Summ,
                    },

                    new DataGridHelperColumn
                    {
                        Header = "sum_b",
                        Path = "SUM_B",
                        ColumnType = ColumnTypeRef.Integer,
                        Width2 = 10,
                        Visible = false,
                    },
                };

                TambourInfoGrid.SetColumns(columns);
                TambourInfoGrid.SetPrimaryKey("ORD");
                //ProductionGrid.SetSorting("ORD", ListSortDirection.Ascending);
                TambourInfoGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                TambourInfoGrid.AutoUpdateInterval = 0;

                //данные грида
                TambourInfoGrid.OnLoadItems = ProductionGridLoadItems;
                TambourInfoGrid.Commands = Commander;
                TambourInfoGrid.Init();
            }
        }

        /// <summary>
        /// запрос на получение данных из CONFIGURATION_OPTIONS
        /// </summary>
        public async void GetData(List<Dictionary<string, string>> list)
        {
            GetDateConfigResult = false;

            var listString = JsonConvert.SerializeObject(list);

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("DATA_LIST", listString);
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "PMFire");
            q.Request.SetParam("Action", "GetData");
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
                    if (ds.Items.Count > 0)
                    {
                        DataConfigOptionsList = ds.Items;
                    }
                    GetDateConfigResult = true;
                }
            }

        }













    }
}
