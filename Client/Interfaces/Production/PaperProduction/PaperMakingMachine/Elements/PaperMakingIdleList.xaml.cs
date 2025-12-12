using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Client.Assets.HighLighters;
using System;
using System.Windows.Input;
using Newtonsoft.Json;
using System.Linq;
using static Client.Interfaces.Main.DataGridHelperColumn;
using Xceed.Wpf.Toolkit;
using Org.BouncyCastle.Asn1.Crmf;
using System.Security.Cryptography;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Список простоев и детальная информация по ним
    /// </summary>
    /// <author>greshnyh_ni</author>   
    public partial class PaperMakingIdleList : ControlBase
    {
        public PaperMakingIdleList()
        {
            InitializeComponent();

            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverName == ControlName)
                {
                    ProcessMessage(msg);
                    //Commander.ProcessCommand(m.Action, m);
                }
            };


            //конструктор, будет вызван, когда объект создается
            //здесь создаются все внутренние структуры
            //впервые этот коллбэк будет вызван, когда данный таб станет активным
            //впервые (до этих пор, никакая работа внутри не происходит, что экономит ресурсы)
            OnLoad = () =>
            {

                SetDefaults();
                Init();
                IdleGridInit();

            };


            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                IdleGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                IdleGrid.ItemsAutoUpdate = true;
                IdleGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                IdleGrid.ItemsAutoUpdate = false;
            };
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            ColapseIdlesFlag = 0;

            Col0.Width = new GridLength(0, GridUnitType.Star);
            Col1.Width = new GridLength(0, GridUnitType.Star);
            Col2.Width = new GridLength(0, GridUnitType.Star);
            Col3.Width = new GridLength(0, GridUnitType.Star);

        }

        /// <summary>
        /// Инициализация грида
        /// </summary>
        public void Init()
        {
            //получаем id текущего времени работы бригад (IdTimes)
            GetCurTime();
            double nScale = 1.5;
            IdleGrid.LayoutTransform = new ScaleTransform(nScale, nScale);
        }

        /// <summary>
        /// Id текущей производственной смены 
        /// </summary>
        public int WorkShiftId { get; set; }

        /// <summary>
        /// Id предыдущей производственной смены 
        /// </summary>
        public int WorkShiftPredId { get; set; }

        public Dictionary<string, string> SelectedIdleItem { get; set; }
        public int MachineId { get; internal set; }

        /// <summary>
        /// признак свертки грида простоев
        /// </summary>
        public int ColapseIdlesFlag { get; set; }

        /// <summary>
        /// инициализация грида IdleGrid
        /// </summary>
        public void IdleGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Минут",
                        Path="DT",
                        Doc="минут простоя",
                        ColumnType=ColumnTypeRef.Double,
                        Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Начало",
                        Path="FROMDT2",
                        Doc="Начало простоя",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Окончание",
                        Path="TODT2",
                        Doc="Окончание простоя",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Причина",
                        Path="NAME",
                        Doc="Причина простоя",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 32,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Описание",
                        Path="REASON",
                        Doc="Описание простоя",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Граммаж",
                        Path="ROLL_RO",
                        ColumnType=ColumnTypeRef.String,
                        Width=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Скорость",
                        Path="BDM_SPEED_IDLES",
                        ColumnType=ColumnTypeRef.String,
                        Width=72,
                    },
                    new DataGridHelperColumn
                    {
                        Header="IDIDLES",
                        Path="IDIDLES",
                        Doc="",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 10,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ID_GRAPH",
                        Path="ID_GRAPH",
                        Doc="",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 10,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="IDREASON",
                        Path="IDREASON",
                        Doc="",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 10,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ID_REASON_DETAIL",
                        Path="ID_REASON_DETAIL",
                        Doc="",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 10,
                        Visible=false,
                    },
                };

                IdleGrid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    { "Change", new DataGridContextMenuItem(){
                        Header="Изменить",
                        Action=()=>
                        {
                            IdleReasonEditShow(SelectedIdleItem);
                        }
                    }},
                };


                IdleGrid.SetColumns(columns);
                IdleGrid.SetPrimaryKey("IDIDLES");
                //ProductionGrid.SetSorting("ORD", ListSortDirection.Ascending);
                IdleGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                IdleGrid.AutoUpdateInterval = 0;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                IdleGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem.Count > 0)
                    {
                        SelectedIdleItem = selectedItem;
                    }
                };

                // двойной клик по строке
                IdleGrid.OnDblClick = IdleReasonEditShow;

                //данные грида
                IdleGrid.OnLoadItems = IdleGridLoadItems;
                IdleGrid.Commands = Commander;
                IdleGrid.Init();
            }
        }

        /// <summary>
        /// получение записей для IdleGrid
        /// </summary>
        public async void IdleGridLoadItems()
        {
            bool resume = true;
            int num = 0;

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID_ST", MachineId.ToString());
                    p.CheckAdd("ID_TIMES", WorkShiftId.ToString());
                    if ((bool)IdTimesPredCheckBox.IsChecked)
                    {
                        p.CheckAdd("NUM", "1");
                        num = 1;
                    }
                    else
                    {
                        p.CheckAdd("NUM", "0");
                    }
                }
                var q = new LPackClientQuery();

                q.Request.SetParam("Module", "ProductionPm");
                q.Request.SetParam("Object", "Monitoring");
                q.Request.SetParam("Action", "IdlesList");
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
                        IdleGrid.UpdateItems(ds);

                        if (num == 0)
                        {
                            // расшифровка простоев для текущей смены
                            var ds1 = ListDataSet.Create(result, "IDLES");

                            if (ds1.Items.Count > 0)
                            {
                                //за текущую смену
                                // количество
                                V02.Text = ds1.Items[0].CheckGet("CNT").ToInt().ToString();
                                // время
                                V03.Text = ds1.Items[0].CheckGet("HHMM").ToString();
                                //процент
                                V04.Text = ds1.Items[0].CheckGet("PROZ").ToString();

                                //за предыдущую смену
                                // количество
                                V12.Text = ds1.Items[1].CheckGet("CNT").ToInt().ToString();
                                // время
                                V13.Text = ds1.Items[1].CheckGet("HHMM").ToString();
                                //процент
                                V14.Text = ds1.Items[1].CheckGet("PROZ").ToString();

                                // за неделю
                                // количество
                                V22.Text = ds1.Items[2].CheckGet("CNT").ToInt().ToString();
                                // время
                                V23.Text = ds1.Items[2].CheckGet("HHMM").ToString();
                                //процент
                                V24.Text = ds1.Items[2].CheckGet("PROZ").ToString();

                                //за месяц
                                // количество
                                V32.Text = ds1.Items[3].CheckGet("CNT").ToInt().ToString();
                                // время
                                V33.Text = ds1.Items[3].CheckGet("HHMM").ToString();
                                //процент
                                V34.Text = ds1.Items[3].CheckGet("PROZ").ToString();
                            }

                            // расшифровка данных по технологическим, техническим и ППР простоям
                            // за текущую, пред. смену, неделю месяц
                            var ds2 = ListDataSet.Create(result, "IDLES2");

                            if (ds2.Items.Count > 0)
                            {
                                // НАКАТ
                                //TechnolCnt.Text = ds2.Items[0].CheckGet("S").ToString() + " ч";
                                //TechCnt.Text = ds2.Items[1].CheckGet("S").ToString() + " ч";
                                //PprCnt.Text = ds2.Items[2].CheckGet("S").ToString() + " ч";

                                //// ПРС
                                //PrsTechnolCnt.Text = ds2.Items[3].CheckGet("S").ToString() + " ч";
                                //PrsTechCnt.Text = ds2.Items[4].CheckGet("S").ToString() + " ч";
                                //PrsPprCnt.Text = ds2.Items[5].CheckGet("S").ToString() + " ч";

                                foreach (var item in ds2.Items)
                                {
                                    var ord = item.CheckGet("ORD").ToInt();
                                    var reason = item.CheckGet("IDREASON").ToInt();
                                    var s = item.CheckGet("S").ToString();

                                    switch (ord)
                                    {
                                        // текущая смена
                                        case 1:
                                            switch (reason)
                                            {
                                                //технологические
                                                case 18:
                                                    T1.Text = s;
                                                    break;
                                                //ППР
                                                case 21:
                                                    T3.Text = s;
                                                    break;
                                                // технические
                                                case 24:
                                                    T2.Text = s;
                                                    break;
                                            }
                                            break;
                                        // предыдущая смена
                                        case 2:
                                            switch (reason)
                                            {
                                                //технологические
                                                case 18:
                                                    T4.Text = s;
                                                    break;
                                                //ППР
                                                case 21:
                                                    T6.Text = s;
                                                    break;
                                                // технические
                                                case 24:
                                                    T5.Text = s;
                                                    break;
                                            }
                                            break;
                                        // неделя
                                        case 3:
                                            switch (reason)
                                            {
                                                //технологические
                                                case 18:
                                                    T7.Text = s;
                                                    break;
                                                //ППР
                                                case 21:
                                                    T9.Text = s;
                                                    break;
                                                // технические
                                                case 24:
                                                    T8.Text = s;
                                                    break;
                                            }
                                            break;
                                        //месяц
                                        case 4:
                                            switch (reason)
                                            {
                                                //технологические
                                                case 18:
                                                    T10.Text = s;
                                                    break;
                                                //ППР
                                                case 21:
                                                    T12.Text = s;
                                                    break;
                                                // технические
                                                case 24:
                                                    T11.Text = s;
                                                    break;
                                            }
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Редактирование причины простоя
        /// </summary>
        private void IdleReasonEditShow(Dictionary<string, string> selectedItem)
        {
            var idleRecord = new IdleRecord(MachineId, selectedItem as Dictionary<string, string>);
            //idleRecord.OnClose += IdleGridLoadItems;
            idleRecord.ReceiverName = ControlName;
            idleRecord.Edit();
        }

        public void LoadItems()
        {
            GetCurTime();
            IdleGrid.LoadItems();
        }

        /// <summary>
        /// возвращает id текущего времени работы бригад (IdTimes)
        /// </summary>
        private bool GetCurTime()
        {
            var result = true;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "Bdm");
            q.Request.SetParam("Action", "BdmGetCurTime");

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var res = JsonConvert.DeserializeObject<Dictionary<string, string>>(q.Answer.Data);
                if (res != null)
                {
                    WorkShiftId = res.CheckGet("ID").ToInt();
                }
            }
            else
            {
                var s = $"Error: GetCurTime. Code=[{q.Answer.Error.Code}] Message=[{q.Answer.Error.Message}] Description=[{q.Answer.Error.Description}]";
                LogMsg(s);
                result = false;
            }

            {
                var s = $"Запрос на сервер. GetCurTime Code=[{q.Answer.Error.Code}]. Получен IdTimes=[{WorkShiftId}].";
                LogMsg(s);
            }
            return result;
        }

        /// <summary>
        /// нажали "Пред. смена"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IdTimesPredCheckBox_Click(object sender, RoutedEventArgs e)
        {
            LoadItems();
        }

        /// <summary>
        /// Обработка общих сообщений
        /// </summary>
        /// <param name="action"></param>
        /// <param name="obj"></param>
        private void ProcessMessage(ItemMessage obj = null)
        {
            string action = obj.Action;
            switch (action)
            {
                case "RefreshIdles":
                    LoadItems();

                    break;
            }
        }

        // раскрыть простои
        private void ButtonOpenIdles_Click(object sender, RoutedEventArgs e)
        {
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Production",
                ReceiverName = "Idles",
                SenderName = ControlName,
                Action = "RollUpIdles",
            });

            if (ColapseIdlesFlag == 0)
            {
                // развернуть
                ColapseIdlesFlag = 1;
                ButtonOpenIdles.Content = "Свернуть список простоев";
                Col0.Width = new GridLength(1, GridUnitType.Star);
                Col1.Width = new GridLength(3, GridUnitType.Pixel);
                Col2.Width = new GridLength(1, GridUnitType.Star);
                Col3.Width = new GridLength(3, GridUnitType.Pixel);
                ProductionGridLoadItems();
            }
            else
            {
                // свернуть
                ColapseIdlesFlag = 0;
                ButtonOpenIdles.Content = "Раскрыть список простоев";
                Col0.Width = new GridLength(0, GridUnitType.Star);
                Col1.Width = new GridLength(0, GridUnitType.Star);
                Col2.Width = new GridLength(0, GridUnitType.Star);
                Col3.Width = new GridLength(0, GridUnitType.Star);

            }
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
                        // заполняем данные по прогнозу
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
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




    }
}
