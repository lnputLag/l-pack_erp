using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
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

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Логика взаимодействия для CompositionList.xaml
    /// </summary>
    public partial class CompositionList : UserControl
    {
        public CompositionList()
        {
            InitializeComponent();

            BorderList = new List<Border>();

            FirstBlocks = new List<Carton>();
            SecondBlocks = new List<Carton>();
            ThirdBlocks = new List<Carton>();
            FourthBlocks = new List<Carton>();
            FifthBlocks = new List<Carton>();
            SixthBlocks = new List<Carton>();
            SeventhBlocks = new List<Carton>();

            Column1DtShort = DateTime.Now.ToString("dd.MM.yyyy");
            Column2DtShort = DateTime.Now.AddDays(1).ToString("dd.MM.yyyy");
            Column3DtShort = DateTime.Now.AddDays(2).ToString("dd.MM.yyyy");
            Column4DtShort = DateTime.Now.AddDays(3).ToString("dd.MM.yyyy");
            Column5DtShort = DateTime.Now.AddDays(4).ToString("dd.MM.yyyy");
            Column6DtShort = DateTime.Now.AddDays(5).ToString("dd.MM.yyyy");
            Column7DtShort = DateTime.Now.AddDays(6).ToString("dd.MM.yyyy");

            DicDateOfCartonList = new Dictionary<List<Carton>, string>();

            DicDateOfCartonList.Add(FirstBlocks, Column1DtShort);
            DicDateOfCartonList.Add(SecondBlocks, Column2DtShort);
            DicDateOfCartonList.Add(ThirdBlocks, Column3DtShort);
            DicDateOfCartonList.Add(FourthBlocks, Column4DtShort);
            DicDateOfCartonList.Add(FifthBlocks, Column5DtShort);
            DicDateOfCartonList.Add(SixthBlocks, Column6DtShort);
            DicDateOfCartonList.Add(SeventhBlocks, Column7DtShort);

            OffsetBlocks = 0;

            CheckedHeaderAndDate = new List<Dictionary<string, string>>();

            Init();

        }

        /// <summary>
        /// Отступ между блоками в колонках грида
        /// </summary>
        public int OffsetBlocks { get; set; }

        /// <summary>
        /// Список с листами объектов типа Carton и датами, которые им соответсвуют;
        /// Для того, чтобы можно было распределять данные по колонкам в зависимости от даты.
        /// </summary>
        public Dictionary<List<Carton>, string> DicDateOfCartonList { get; set; }

        /// <summary>
        /// Текущая дата
        /// </summary>
        public string Column1DtShort { get; set; }

        /// <summary>
        /// Текущая дата + 1 день
        /// </summary>
        public string Column2DtShort { get; set; }

        /// <summary>
        /// Текущая дата + 2 дня
        /// </summary>
        public string Column3DtShort { get; set; }

        /// <summary>
        /// Текущая дата + 3 дня
        /// </summary>
        public string Column4DtShort { get; set; }

        /// <summary>
        /// Текущая дата + 4 дня
        /// </summary>
        public string Column5DtShort { get; set; }

        /// <summary>
        /// Текущая дата + 5 дней
        /// </summary>
        public string Column6DtShort { get; set; }

        /// <summary>
        /// Текущая дата + 6 дней
        /// </summary>
        public string Column7DtShort { get; set; }

        public ListDataSet DsOfComposition { get; set; }

        public ListDataSet HeaderDs { get; set; }

        public List<Composition> ListOfComposition { get; set; }

        public ListDataSet DsOfOrder { get; set; }

        public List<Order> ListOfOrder { get; set; }

        /// <summary>
        /// Лист с объектами Carton для первой колонки главного грида интерфейса
        /// </summary>
        public List<Carton> FirstBlocks { get; set; }

        /// <summary>
        /// Лист с объектами Carton для второй колонки главного грида интерфейса
        /// </summary>
        public List<Carton> SecondBlocks { get; set; }

        /// <summary>
        /// Лист с объектами Carton для третьей колонки главного грида интерфейса
        /// </summary>
        public List<Carton> ThirdBlocks { get; set; }

        /// <summary>
        /// Лист с объектами Carton для четвёртой колонки главного грида интерфейса
        /// </summary>
        public List<Carton> FourthBlocks { get; set; }

        /// <summary>
        /// Лист с объектами Carton для пятой колонки главного грида интерфейса
        /// </summary>
        public List<Carton> FifthBlocks { get; set; }

        /// <summary>
        /// Лист с объектами Carton для шестой колонки главного грида интерфейса
        /// </summary>
        public List<Carton> SixthBlocks { get; set; }

        /// <summary>
        /// Лист с объектами Carton для седьмой колонки главного грида интерфейса
        /// </summary>
        public List<Carton> SeventhBlocks { get; set; }

        /// <summary>
        /// Список блоков с информацией по строкам грида (название сырьевой группы, форма фактор);
        /// Border->StackPanel->TextBlock/TextBlock.
        /// </summary>
        public List<Border> BorderList { get; set; }

        /// <summary>
        /// Датасет с данными по коэффициенту для преобразованию метража картона в вес бумаги, которая используется для его производства
        /// </summary>
        public ListDataSet FactorOfRawGroupByCartonDs { get; set; }

        /// <summary>
        /// Список с данными по сырьевым группам, которые используются для производства картона и коэффициентами для преобразования метража картона в вес бумаги(сырьевой группы)
        /// </summary>
        public Dictionary<int, Dictionary<int, double>> DicOfFactor { get; set; }

        /// <summary>
        /// Лист из словарей, которые содержат название шапки строки (Наименование картона/Ширина) и дату, для которых отмечены сырьевые группы
        /// </summary>
        public List<Dictionary<string, string>> CheckedHeaderAndDate { get; set; }

        /// <summary>
        /// Список сырьевых групп
        /// </summary>
        public List<RawMaterialGroupList.RawMaterialGroup> RawGroupList { get; set; }

        public void LoadData()
        {
            GetHeaderRows();

            GetOrderData();

            GetFactorOfRawGroupByCarton();

            CalculateOrderData();

            CreateCartonBlock();
        }

        /// <summary>
        /// Получаем список с данными по коэффициенту для преобразования метража картона в вес сырьевой группы
        /// </summary>
        public void GetFactorOfRawGroupByCarton()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "RawMaterialGroup");
            q.Request.SetParam("Action", "ListFactorOfRawGroupByCarton");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    FactorOfRawGroupByCartonDs = ListDataSet.Create(result, "ITEMS");

                    DicOfFactor = new Dictionary<int, Dictionary<int, double>>();
                    foreach (var item in FactorOfRawGroupByCartonDs.Items)
                    {
                        if (DicOfFactor.ContainsKey(item.CheckGet("IDC").ToInt()))
                        {
                            var dic = DicOfFactor[item.CheckGet("IDC").ToInt()];
                            dic.Add(item.CheckGet("ID_RAW_GROUP").ToInt(), item.CheckGet("FACTOR").ToDouble());
                        }
                        else
                        {
                            var dic = new Dictionary<int, double>();
                            dic.Add(item.CheckGet("ID_RAW_GROUP").ToInt(), item.CheckGet("FACTOR").ToDouble());

                            DicOfFactor.Add(item.CheckGet("IDC").ToInt(), dic);
                        }
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        public void GetHeaderRows()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "RawMaterialGroup");
            q.Request.SetParam("Action", "CartonList");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    HeaderDs = ListDataSet.Create(result, "ITEMS");

                    foreach (var item in HeaderDs.Items)
                    {
                        item.CheckAdd("IDC", item.CheckGet("IDC").ToInt().ToString());
                        item.CheckAdd("WIDTH", item.CheckGet("WIDTH").ToInt().ToString());
                    }

                    HeaderDs.Items = HeaderDs.Items.OrderBy(x => x.CheckGet("IDC").ToInt()).ThenBy(x => x.CheckGet("WIDTH")).ToList();

                    if (HeaderDs.Items.Count > 0)
                    {

                    }
                }
                else
                {

                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Добавляет блоки (оглавления) для каждой строки грида (с данными по наименованию и форм фактору сырьевой группы)
        /// </summary>
        public void SetHeaderGridItems()
        {
            BorderList.Clear();

            foreach (var item in HeaderDs.Items)
            {
                var b = new Border();
                b.Width = 130;
                b.Height = 52;
                b.BorderThickness = new Thickness(1, 0, 1, 2);
                // Чёрный цвет
                var color = "#000000";
                var brush = color.ToBrush();
                b.BorderBrush = brush;

                var sp = new StackPanel();
                sp.Orientation = Orientation.Vertical;
                sp.VerticalAlignment = VerticalAlignment.Center;

                var tb = new TextBlock();
                tb.Text = item.CheckGet("IDC");
                tb.HorizontalAlignment = HorizontalAlignment.Center;
                tb.VerticalAlignment = VerticalAlignment.Center;

                var tb2 = new TextBlock();
                tb2.Text = item.CheckGet("WIDTH");
                tb2.HorizontalAlignment = HorizontalAlignment.Center;
                tb2.VerticalAlignment = VerticalAlignment.Center;

                sp.Children.Add(tb);
                sp.Children.Add(tb2);

                b.Child = sp;

                BorderList.Add(b);
            }
        }

        /// <summary>
        /// Получаем данные по композициям
        /// </summary>
        public void GetCompositionData()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "RawMaterialGroup");
            q.Request.SetParam("Action", "CompositionList");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    DsOfComposition = ListDataSet.Create(result, "ITEMS");

                    if (DsOfComposition.Items.Count > 0)
                    {
                        var dsList = DsOfComposition.Items.OrderBy(x => x.CheckGet("IDC")).ToList();

                        ListOfComposition = new List<Composition>();

                        foreach (var item in dsList)
                        {
                            if (ListOfComposition.FirstOrDefault(x => x.IDC == item.CheckGet("IDC").ToInt()) == null)
                            {
                                var c = new Composition();
                                c.IDC = item.CheckGet("IDC").ToInt();

                                var dic = new Dictionary<string, string>();
                                {
                                    var listByIDC = dsList.Where(x => x.CheckGet("IDC") == item.CheckGet("IDC")).ToList();

                                    foreach (var item2 in listByIDC)
                                    {
                                        dic.Add(item2.CheckGet("ID_RAW_GROUP"), item2.CheckGet("FACTOR"));
                                    }
                                }

                                c.PaperList = dic;

                                ListOfComposition.Add(c);
                            }
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
        /// Получаем данные по заявкам
        /// </summary>
        public void GetOrderData()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "RawMaterialGroup");
            q.Request.SetParam("Action", "OrderList");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    DsOfOrder = ListDataSet.Create(result, "ITEMS");

                    if (DsOfOrder.Items.Count > 0)
                    {
                        var dsList = DsOfOrder.Items.OrderBy(x => x.CheckGet("DT").ToDateTime("dd.MM.yyyy")).ToList();

                        ListOfOrder = new List<Order>();

                        foreach (var item in dsList)
                        {
                            var o = new Order();
                            o.DT = item.CheckGet("DT");
                            o.IDC = item.CheckGet("IDC").ToInt();
                            o.QtyOrder = item.CheckGet("QTY").ToInt();
                            o.BlankWidth = item.CheckGet("BLANK_WIDTH").ToInt();
                            o.BlankLength = item.CheckGet("BLANK_LENGHT").ToInt();
                            o.OrderId = item.CheckGet("ORDER_ID").ToInt();
                            o.OrderNumber = item.CheckGet("ORDER_NUMBER");
                            o.BuyerName = item.CheckGet("BUYER_NAME");
                            o.ProductName = item.CheckGet("PRODUCT_NAME");
                            o.ProductCode = item.CheckGet("PRODUCT_CODE");
                            o.CardboardName = item.CheckGet("CARDBOARD_NAME");

                            o.DicOfTrimWidth.CheckAdd("1900", item.CheckGet("TRIM_WIDTH_1900"));

                            o.DicOfTrimWidth.CheckAdd("2000", item.CheckGet("TRIM_WIDTH_2000"));

                            o.DicOfTrimWidth.CheckAdd("2100", item.CheckGet("TRIM_WIDTH_2100"));

                            o.DicOfTrimWidth.CheckAdd("2200", item.CheckGet("TRIM_WIDTH_2200"));

                            o.DicOfTrimWidth.CheckAdd("2300", item.CheckGet("TRIM_WIDTH_2300"));

                            o.DicOfTrimWidth.CheckAdd("2400", item.CheckGet("TRIM_WIDTH_2400"));

                            o.DicOfTrimWidth.CheckAdd("2500", item.CheckGet("TRIM_WIDTH_2500"));

                            o.DicOfTrimWidth.CheckAdd("2700", item.CheckGet("TRIM_WIDTH_2700"));

                            o.DicOfTrimWidth.CheckAdd("2800", item.CheckGet("TRIM_WIDTH_2800"));

                            ListOfOrder.Add(o);
                        }
                    }
                }
                else
                {

                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Считаем количество метров квадратных для каждой заявки
        /// (на этом этапе выбирается формат)
        /// </summary>
        public void CalculateOrderData()
        {
            foreach (var item in ListOfOrder)
            {
                Dictionary<int, double> listOfRawGroup = new Dictionary<int, double>();

                // Список с сырьевыми группами, которые используются для производства картона из заявки

                if (DicOfFactor.ContainsKey(item.IDC))
                {
                    listOfRawGroup = DicOfFactor[item.IDC];
                }
                //var listOfRawGroup = DicOfFactor[item.IDC];

                // width, summaryPriority
                var listOfPriority = new Dictionary<int, int>();

                // список форматов и их коэффициентов без пустых значений
                Dictionary<string, string> listOfTrimWidthFactor = item.DicOfTrimWidth.Where(x => !string.IsNullOrEmpty(x.Value) && x.Value.ToInt() != 1).ToDictionary(x => x.Key, x => x.Value);

                foreach (var widthByFactor in listOfTrimWidthFactor.Keys)
                {
                    int summaryPriority = 0;

                    foreach (var rawGroupByIdc in listOfRawGroup)
                    {
                        summaryPriority += RawGroupList.FirstOrDefault(x => x.Id == rawGroupByIdc.Key && x.PaperWidth == widthByFactor.ToInt()).Priority;
                    }

                    listOfPriority.Add(widthByFactor.ToInt(), summaryPriority);
                }

                int maxSummaryPriority = listOfPriority.Max(x => x.Value);
                KeyValuePair<string, string> trimWidth = new KeyValuePair<string, string>();

                // Вариант алгоритма выбора оптимального формата с предварительной валидацией данных на значения коэффициентов обрези
                {
                    var listOfPriorityByMaxSummaryPriority = listOfPriority.Where(x => x.Value == maxSummaryPriority).ToList();

                    if (listOfPriorityByMaxSummaryPriority.Count > 1)
                    {
                        List<KeyValuePair<string, string>> dic = new List<KeyValuePair<string, string>>();

                        foreach (var itemOfListOfPriorityByMaxSummaryPriority in listOfPriorityByMaxSummaryPriority)
                        {
                            dic.Add(listOfTrimWidthFactor.FirstOrDefault(x => x.Key.ToInt() == itemOfListOfPriorityByMaxSummaryPriority.Key));
                        }

                        dic = dic.OrderBy(x => x.Value.ToDouble()).ToList();
                        trimWidth = dic.First();
                    }
                    else
                    {
                        trimWidth = listOfTrimWidthFactor.FirstOrDefault(x => x.Key.ToInt() == listOfPriorityByMaxSummaryPriority.First().Key);
                    }
                }

                {
                    //var listWidthByMaxPriority = listOfPriority.Where(x => x.Value == maxSummaryPriority).Select(x => x.Key).ToList();
                    //List<KeyValuePair<string, string>> dic = new List<KeyValuePair<string, string>>();
                    //foreach (var itemListWidthByMaxPriority in listWidthByMaxPriority)
                    //{
                    //    dic.Add(item.DicOfTrimWidth.FirstOrDefault(x => x.Key.ToInt() == itemListWidthByMaxPriority));
                    //}
                    //List<KeyValuePair<string, string>> dic2 = new List<KeyValuePair<string, string>>();
                    //foreach (var itemOfDicOfTrimWidth in item.DicOfTrimWidth)
                    //{
                    //    if (!dic.Contains(itemOfDicOfTrimWidth))
                    //    {
                    //        dic2.Add(itemOfDicOfTrimWidth);
                    //    }
                    //}
                    //dic = dic.OrderBy(x => x.Value.ToDouble()).Where(x => x.Value.ToDouble() > 0).ToList();
                    //dic2 = dic2.OrderBy(x => x.Value.ToDouble()).Where(x => x.Value.ToDouble() > 0).ToList();
                    //dic.AddRange(dic2);
                    //trimWidth = dic.First();
                }

                //var dic = item.DicOfTrimWidth.OrderBy(x => x.Value.ToDouble()).Where(x => x.Value.ToDouble() > 0).ToList();
                //KeyValuePair<string, string> trimWidth = dic.First();

                item.TrimWidth = trimWidth.Key.ToInt();
                item.FactorTrimWidth = trimWidth.Value.ToDouble();

                int width = Math.Floor((double)item.TrimWidth / (double)item.BlankWidth).ToInt();

                int qty = Math.Ceiling((double)item.QtyOrder / (double)width).ToInt();

                // Коэффициент для преобразования из миллиметров в квадратные метры
                int factor1 = 1000000;

                double k = (double)qty * (double)item.BlankLength * (double)item.TrimWidth / (double)factor1;

                item.QtySqrMetr = k;

                item.TrimLength = qty * item.BlankLength;
            }
        }

        public void CreateCartonBlock()
        {
            var profiler2 = new Profiler();

            ClearAllListOfCarton();

            var ds = new ListDataSet();

            //Создаём полный набор всех необходимых объектов Картон для всех строк и столбцов, чтобы не возвращаться к созданию объектов и заниматься только наполнением
            foreach (var head in HeaderDs.Items)
            {
                var dic = new Dictionary<string, string>();

                dic.Add("NAME", $"{head.CheckGet("DESCRIPTION")}/{head.CheckGet("WIDTH")}");

                foreach (var item in DicDateOfCartonList)
                {
                    var cartonBlock = new Carton();
                    cartonBlock.Dt = item.Value;
                    cartonBlock.Idc = head.CheckGet("IDC").ToInt();
                    cartonBlock.TrimWidth = head.CheckGet("WIDTH").ToInt();

                    if (DicOfFactor.ContainsKey(cartonBlock.Idc))
                    {
                        cartonBlock.DicOFFactorByRawGroup = DicOfFactor[cartonBlock.Idc];
                    }

                    var orders = ListOfOrder.Where(x => x.DT == cartonBlock.Dt && x.IDC == cartonBlock.Idc && x.TrimWidth == cartonBlock.TrimWidth).ToList();
                    double qty = 0;
                    foreach (var order in orders)
                    {
                        qty += order.QtySqrMetr;

                        var dictionaryOfOrder = new Dictionary<string, string>();
                        dictionaryOfOrder.Add("ORDER_ID", order.OrderId.ToString());
                        dictionaryOfOrder.Add("ORDER_NUMBER", order.OrderNumber);
                        dictionaryOfOrder.Add("BUYER_NAME", order.BuyerName);
                        dictionaryOfOrder.Add("PRODUCT_NAME", order.ProductName);
                        dictionaryOfOrder.Add("PRODUCT_CODE", order.ProductCode);
                        dictionaryOfOrder.Add("CARDBOARD_NAME", order.CardboardName);
                        dictionaryOfOrder.Add("QUANTITY_IN_ORDER", order.QtyOrder.ToString());

                        cartonBlock.ListOfOrderData.Add(dictionaryOfOrder);
                    }
                    cartonBlock.QtySqrMetr = qty;

                    item.Key.Add(cartonBlock);

                    if (item.Value == Column1DtShort)
                    {
                        dic.Add("METER_1", cartonBlock.QtySqrMetr.ToString("#,###,###,##0"));
                    }
                    else if (item.Value == Column2DtShort)
                    {
                        dic.Add("METER_2", cartonBlock.QtySqrMetr.ToString("#,###,###,##0"));
                    }
                    else if (item.Value == Column3DtShort)
                    {
                        dic.Add("METER_3", cartonBlock.QtySqrMetr.ToString("#,###,###,##0"));
                    }
                    else if (item.Value == Column4DtShort)
                    {
                        dic.Add("METER_4", cartonBlock.QtySqrMetr.ToString("#,###,###,##0"));
                    }
                    else if (item.Value == Column5DtShort)
                    {
                        dic.Add("METER_5", cartonBlock.QtySqrMetr.ToString("#,###,###,##0"));
                    }
                    else if (item.Value == Column6DtShort)
                    {
                        dic.Add("METER_6", cartonBlock.QtySqrMetr.ToString("#,###,###,##0"));
                    }
                    else if (item.Value == Column7DtShort)
                    {
                        dic.Add("METER_7", cartonBlock.QtySqrMetr.ToString("#,###,###,##0"));
                    }
                }

                ds.Items.Add(dic);
            }

            CartonGrid.UpdateItems(ds);
        }

        public void ClearAllListOfCarton()
        {
            foreach (var item in DicDateOfCartonList)
            {
                item.Key.Clear();
            }
        }

        public void Init()
        {
            //инициализация грида
            {
                //колонки грида

                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Картон/Формат",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=37,
                        MaxWidth=250,
                    },
                    new DataGridHelperColumn
                    {
                        Header=Column1DtShort,
                        Path="METER_1",
                        ColumnType=ColumnTypeRef.String,
                        Width = 100,
                    },
                    new DataGridHelperColumn
                    {
                        Header=Column2DtShort,
                        Path="METER_2",
                        ColumnType=ColumnTypeRef.String,
                        Width = 100,
                    },
                    new DataGridHelperColumn
                    {
                        Header=Column3DtShort,
                        Path="METER_3",
                        ColumnType=ColumnTypeRef.String,
                        Width = 100,
                    },
                    new DataGridHelperColumn
                    {
                        Header=Column4DtShort,
                        Path="METER_4",
                        ColumnType=ColumnTypeRef.String,
                        Width = 100,
                    },
                    new DataGridHelperColumn
                    {
                        Header=Column5DtShort,
                        Path="METER_5",
                        ColumnType=ColumnTypeRef.String,
                        Width=100,
                    },
                    new DataGridHelperColumn
                    {
                        Header=Column6DtShort,
                        Path="METER_6",
                        ColumnType=ColumnTypeRef.String,
                        Width = 100,
                    },
                    new DataGridHelperColumn
                    {
                        Header=Column7DtShort,
                        Path="METER_7",
                        ColumnType=ColumnTypeRef.String,
                        Width = 100,
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
                CartonGrid.SetColumns(columns);

                //GridTkList.SetSorting("ID", ListSortDirection.Descending);

                CartonGrid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                //GridTkList.OnSelectItem = selectedItem =>
                //{
                //    UpdateActions(selectedItem);
                //};

                //данные грида
                //GridTkList.OnLoadItems = LoadItems;
                //GridTkList.OnFilterItems = FilterItems;
                //GridTkList.Run();

            }
        }

        public string SetCollor(Dictionary<string, string> row, string dtShort)
        {
            var color = "";

            if (CheckedHeaderAndDate.Count > 0)
            {
                foreach (Dictionary<string, string> headerAndDate in CheckedHeaderAndDate)
                {
                    if (row.CheckGet("NAME") == headerAndDate.First().Key && dtShort == headerAndDate.First().Value)
                    {
                        color = HColor.Yellow;
                    }
                }
            }

            return color;
        }
        public void CartonGridLoadItems()
        {
            var ds = new ListDataSet();

            foreach (var item in HeaderDs.Items)
            {
                var dic = new Dictionary<string, string>();

                dic.Add("NAME", $"{item.CheckGet("DESCRIPTION")}/{item.CheckGet("WIDTH")}");

                {
                    var carton = FirstBlocks.FirstOrDefault(x => x.Idc == item.CheckGet("IDC").ToInt() && x.TrimWidth == item.CheckGet("WIDTH").ToInt());
                    dic.Add("METER_1", carton.QtySqrMetr.ToString());
                }

                {
                    var carton = SecondBlocks.FirstOrDefault(x => x.Idc == item.CheckGet("IDC").ToInt() && x.TrimWidth == item.CheckGet("WIDTH").ToInt());
                    dic.Add("METER_2", carton.QtySqrMetr.ToString());
                }

                {
                    var carton = ThirdBlocks.FirstOrDefault(x => x.Idc == item.CheckGet("IDC").ToInt() && x.TrimWidth == item.CheckGet("WIDTH").ToInt());
                    dic.Add("METER_3", carton.QtySqrMetr.ToString());
                }

                {
                    var carton = FourthBlocks.FirstOrDefault(x => x.Idc == item.CheckGet("IDC").ToInt() && x.TrimWidth == item.CheckGet("WIDTH").ToInt());
                    dic.Add("METER_4", carton.QtySqrMetr.ToString());
                }

                {
                    var carton = FifthBlocks.FirstOrDefault(x => x.Idc == item.CheckGet("IDC").ToInt() && x.TrimWidth == item.CheckGet("WIDTH").ToInt());
                    dic.Add("METER_5", carton.QtySqrMetr.ToString());
                }

                {
                    var carton = SixthBlocks.FirstOrDefault(x => x.Idc == item.CheckGet("IDC").ToInt() && x.TrimWidth == item.CheckGet("WIDTH").ToInt());
                    dic.Add("METER_6", carton.QtySqrMetr.ToString());
                }

                {
                    var carton = SeventhBlocks.FirstOrDefault(x => x.Idc == item.CheckGet("IDC").ToInt() && x.TrimWidth == item.CheckGet("WIDTH").ToInt());
                    dic.Add("METER_7", carton.QtySqrMetr.ToString());
                }

                ds.Items.Add(dic);
            }

            CartonGrid.UpdateItems(ds);
        }

        private void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp-new/planing/planning_raw_group/compositions");
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void GetButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void ResreshButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
