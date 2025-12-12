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

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Интерфейс для работы с сырьевыми группами
    /// </summary>
    /// <author>sviridov_ae</author>
    public partial class RawMaterialGroupList : UserControl
    {
        /// <summary>
        /// Конструктор класса
        /// </summary>
        public RawMaterialGroupList()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            SetDefaults();
            ProcessPermissions();
        }

        /// <summary>
        /// Лист с объектами RawGroupBlock для первой колонки главного грида интерфейса
        /// </summary>
        public List<RawGroupBlock> FirstBlocks { get; set; }

        /// <summary>
        /// Лист с объектами RawGroupBlock для второй колонки главного грида интерфейса
        /// </summary>
        public List<RawGroupBlock> SecondBlocks { get; set; }

        /// <summary>
        /// Лист с объектами RawGroupBlock для третьей колонки главного грида интерфейса
        /// </summary>
        public List<RawGroupBlock> ThirdBlocks { get; set; }

        /// <summary>
        /// Лист с объектами RawGroupBlock для четвёртой колонки главного грида интерфейса
        /// </summary>
        public List<RawGroupBlock> FourthBlocks { get; set; }

        /// <summary>
        /// Лист с объектами RawGroupBlock для пятой колонки главного грида интерфейса
        /// </summary>
        public List<RawGroupBlock> FifthBlocks { get; set; }

        /// <summary>
        /// Лист с объектами RawGroupBlock для шестой колонки главного грида интерфейса
        /// </summary>
        public List<RawGroupBlock> SixthBlocks { get; set; }

        /// <summary>
        /// Лист с объектами RawGroupBlock для седьмой колонки главного грида интерфейса
        /// </summary>
        public List<RawGroupBlock> SeventhBlocks { get; set; }

        /// <summary>
        /// Датасет с начальными данными по сырьевой группе для первого столбца
        /// </summary>
        public ListDataSet RawGroupStartedDs { get; set; }

        /// <summary>
        /// Датасет с данными по сырьевой группе (кол-во на БДМ) по датам
        /// </summary>
        public ListDataSet RawGroupByDateDs { get; set; }

        /// <summary>
        /// Лист с объектами RawGroup
        /// </summary>
        public List<RawMaterialGroup> RawGroupList { get; set; }

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

        /// <summary>
        /// Список с листами объектов типа RawGroupBlock и датами, которые им соответсвуют;
        /// Для того, чтобы можно было распределять данные по колонкам в зависимости от даты.
        /// </summary>
        public Dictionary<List<RawGroupBlock>, string> DicDateOfRawGroupBlockList { get; set; }

        /// <summary>
        /// Отступ между блоками в колонках грида
        /// </summary>
        public int OffsetBlocks { get; set; }

        /// <summary>
        /// Полученное по шине сообщений сообщене
        /// </summary>
        public Dictionary<string, string> Message { get; set; }
        
        /// <summary>
        /// Список блоков с информацией по строкам грида (название сырьевой группы, форма фактор);
        /// Border->StackPanel->TextBlock/TextBlock.
        /// </summary>
        public List<Border> BorderList { get; set; }

        public CompositionList CompositionList { get; set; }

        /// <summary>
        /// Список объектов RawMaterialGroup, для который при загрузке начальных данных был установлен флаг BlockedFlag == true
        /// </summary>
        public List<RawMaterialGroup> ListBlockedRawGroup { get; set; }

        /// <summary>
        /// Класс Сырьевая группа для хранения данных
        /// </summary>
        public class RawMaterialGroup
        {
            /// <summary>
            /// Класс Сырьевая группа для хранения данных
            /// </summary>
            public RawMaterialGroup(int id, string dt, string name = "", int quantityInStock = 0, int quantityInCorrugatedMachine = 0, int quantityInPaperMachine = 0, int quantityInOrder = 0, int paperWidth = 0, Dictionary<string, string> paperList = null, string priorityName = "", int priority = 0)
            {
                PaperList = new Dictionary<string, string>();

                if (paperList != null)
                {
                    PaperList = paperList;
                }

                Id = id;
                Name = name;
                QuantityInStock = quantityInStock;
                QuantityInCorrugatedMachine = quantityInCorrugatedMachine;
                QuantityInPaperMachine = quantityInPaperMachine;
                PaperWidth = paperWidth;
                Dt = dt;
                QuantityInOrder = quantityInOrder;
                PriorityName = priorityName;
                Priority = priority;

                ListOfOrderData = new List<Dictionary<string, string>>();
            }

            /// <summary>
            /// Словарь с данными по бумаге:
            /// Слой/Бумага
            /// </summary>
            public Dictionary<string, string> PaperList { get; set; }

            /// <summary>
            /// Ид сырьевой группы (ИД группы сырья из таблицы RAW_GROUP (ID_RAW_GROUP))
            /// </summary>
            public int Id { get; set; }

            /// <summary>
            /// Количество на складе
            /// </summary>
            public double QuantityInStock { get; set; }

            /// <summary>
            /// Количество на ГА
            /// </summary>
            public int QuantityInCorrugatedMachine { get; set; }

            /// <summary>
            /// Ширина бумаги
            /// </summary>
            public int PaperWidth { get; set; }

            /// <summary>
            /// Наименование сырьевой группы
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Количество на БДМ
            /// </summary>
            public int QuantityInPaperMachine { get; set; }

            /// <summary>
            /// Количество в заявках
            /// </summary>
            public double QuantityInOrder { get; set; }

            /// <summary>
            /// Дата для блока с этими данными (в формате ToString("dd.MM.yyyy"))
            /// </summary>
            public string Dt { get; set; }

            /// <summary>
            /// Приоритет сырьевой группы (словесный):
            /// 0 -- Не использовать;
            /// 1 -- Низкий;
            /// 2 -- Средний;
            /// 3 -- Высокий.
            /// </summary>
            public string PriorityName { get; set; }

            /// <summary>
            /// Приоритет сырьевой группы (числовой)
            /// </summary>
            public int Priority { get; set; }

            /// <summary>
            /// Лист с данными по заявкам на эту сырьевую группу
            /// </summary>
            public List<Dictionary<string, string>> ListOfOrderData { get; set; }

            /// <summary>
            /// Флаг того, что эта сырьевая группа заблокирована (да эту дату Dt)
            /// </summary>
            public bool BlockedFlag { get; set; }
        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel("[erp]raw_material_group_planner");
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
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            FirstBlocks = new List<RawGroupBlock>();
            SecondBlocks = new List<RawGroupBlock>();
            ThirdBlocks = new List<RawGroupBlock>();
            FourthBlocks = new List<RawGroupBlock>();
            FifthBlocks = new List<RawGroupBlock>();
            SixthBlocks = new List<RawGroupBlock>();
            SeventhBlocks = new List<RawGroupBlock>();

            BorderList = new List<Border>();

            RawGroupList = new List<RawMaterialGroup>();

            Column1DtShort = DateTime.Now.ToString("dd.MM.yyyy");
            Column2DtShort = DateTime.Now.AddDays(1).ToString("dd.MM.yyyy");
            Column3DtShort = DateTime.Now.AddDays(2).ToString("dd.MM.yyyy");
            Column4DtShort = DateTime.Now.AddDays(3).ToString("dd.MM.yyyy");
            Column5DtShort = DateTime.Now.AddDays(4).ToString("dd.MM.yyyy");
            Column6DtShort = DateTime.Now.AddDays(5).ToString("dd.MM.yyyy");
            Column7DtShort = DateTime.Now.AddDays(6).ToString("dd.MM.yyyy");


            DicDateOfRawGroupBlockList = new Dictionary<List<RawGroupBlock>, string>();

            DicDateOfRawGroupBlockList.Add(FirstBlocks, Column1DtShort);
            DicDateOfRawGroupBlockList.Add(SecondBlocks, Column2DtShort);
            DicDateOfRawGroupBlockList.Add(ThirdBlocks, Column3DtShort);
            DicDateOfRawGroupBlockList.Add(FourthBlocks, Column4DtShort);
            DicDateOfRawGroupBlockList.Add(FifthBlocks, Column5DtShort);
            DicDateOfRawGroupBlockList.Add(SixthBlocks, Column6DtShort);
            DicDateOfRawGroupBlockList.Add(SeventhBlocks, Column7DtShort);

            OffsetBlocks = 0;

            ListBlockedRawGroup = new List<RawMaterialGroup>();
        }

        /// <summary>
        /// Загрузка начальных данных
        /// </summary>
        public void LoadData()
        {
            if (RawGroupList.Count > 0)
            {
                SetDefaults();
            }

            GetStartedData();

            GetBlockedDataForRawGroup();

            GetDataForRawGroupByDate();
            
            CreateCompositionInterface();

            CalculateQtyStock();

            SetDataIntoListRawGroupsToListBlocks();

            //ListBlocksOrderBy();

            SetHeaderGridItems();

            StringFormater();

            UpdateAllColumns();


            if (ListBlockedRawGroup.Count > 0)
            {
                RecalculateByBlockedFlag(ListBlockedRawGroup);
            }
        }

        /// <summary>
        /// Создаём экземпляр вкладки Картон
        /// Получаем данные для этой вкладки
        /// </summary>
        public void CreateCompositionInterface()
        {
            CompositionList = (CompositionList)Central.WM.TabItems.FirstOrDefault(x => x.Key == "RawMaterialGroupPlanner_Composition").Value.Content;
            CompositionList.RawGroupList = RawGroupList;
            CompositionList.LoadData();

            foreach (var item in CompositionList.DicDateOfCartonList)
            {
                var listOfCartonBlock = item.Key;
                var rawGroupList = RawGroupList.Where(x => x.Dt == item.Value).ToList();

                foreach (var carton in listOfCartonBlock)
                {
                    if (carton.DicOFFactorByRawGroup.Count > 0)
                    {
                        foreach (var rg in carton.DicOFFactorByRawGroup)
                        {
                            var rawGroup = rawGroupList.FirstOrDefault(x => x.Id == rg.Key && x.PaperWidth == carton.TrimWidth);
                            if (rawGroup != null)
                            {
                                rawGroup.QuantityInOrder += carton.QtySqrMetr * rg.Value;
                                rawGroup.ListOfOrderData.AddRange(carton.ListOfOrderData);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Включение видимости полоски загрузки
        /// </summary>
        public void EnableLoadingBar()
        {
            //GridSplash.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Отключение видимости полоски загрузки
        /// </summary>
        public void DisableLoadingBar()
        {
            //GridSplash.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Получает начальные значения для первого столбика
        /// </summary>
        public void GetStartedData()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "RawMaterialGroup");
            q.Request.SetParam("Action", "RawGroupList");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;


            q.DoQuery();


            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    RawGroupStartedDs = ListDataSet.Create(result, "ITEMS");

                    if (RawGroupStartedDs.Items.Count > 0)
                    {
                        foreach (var item in RawGroupStartedDs.Items)
                        {
                            int id = item.CheckGet("ID_RAW_GROUP").ToInt();
                            string name = item.CheckGet("NAME");
                            int width = item.CheckGet("WIDTH").ToInt();
                            int quantityInStock = item.CheckGet("QTY_STOCK").ToInt();
                            int quantityInCorrugatedMachine = item.CheckGet("QTY_GA").ToInt();
                            string priorityName = item.CheckGet("PRIORITY_NAME");
                            int priority = item.CheckGet("PRIORITY").ToInt();

                            item.CheckAdd("DT", Column1DtShort);

                            var rawMaterialGroup = new RawMaterialGroup(id, Column1DtShort, name, quantityInStock, quantityInCorrugatedMachine, 0, 0, width, null, priorityName, priority);

                            RawGroupList.Add(rawMaterialGroup);

                            // Проходим циклом по оставшимся 6 дням и создаём для них(по сути для всего интерфейса) объекты RawMaterialGroup, которые в последствии будем заполнять
                            foreach (var item2 in DicDateOfRawGroupBlockList.Values)
                            {
                                if (item2 != Column1DtShort)
                                {
                                    var rawMaterialGroup2 = new RawMaterialGroup(id, item2, name, 0, 0, 0, 0, width, null, priorityName, priority);

                                    RawGroupList.Add(rawMaterialGroup2);
                                }
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
        /// Получаем данные по заблокированным сырьевым группам на ближайшие 7 дней
        /// </summary>
        public void GetBlockedDataForRawGroup()
        {
            var p = new Dictionary<string, string>();
            p.Add("DATE_FROM", Column1DtShort);
            p.Add("DATE_TO", Column7DtShort);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "RawMaterialGroup");
            q.Request.SetParam("Action", "ListBlockedRawGroup");

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
                        foreach (var blockedRawGroup in dataSet.Items)
                        {
                            var rawGroup = RawGroupList.FirstOrDefault(x => x.Id == blockedRawGroup.CheckGet("ID").ToInt() && x.Dt == blockedRawGroup.CheckGet("DT") && x.PaperWidth == blockedRawGroup.CheckGet("WIDTH").ToInt());
                            if (rawGroup != null)
                            {
                                rawGroup.BlockedFlag = true;
                                ListBlockedRawGroup.Add(rawGroup);
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
        /// Добавляет блоки (оглавления) для каждой строки грида (с данными по наименованию и форм фактору сырьевой группы)
        /// </summary>
        public void SetHeaderGridItems()
        {
            BorderList.Clear();
            HeaderGrid.Children.Clear();

            foreach (var item in FirstBlocks)
            {
                var b = new Border();
                b.Width = 150;
                b.Height = 52;
                b.BorderThickness = new Thickness(1,0,1,2);
                // Чёрный цвет
                var color = "#000000";
                var brush = color.ToBrush();
                b.BorderBrush = brush;

                var stackPanel = new StackPanel();
                stackPanel.Orientation = Orientation.Vertical;
                stackPanel.VerticalAlignment = VerticalAlignment.Center;

                var rawGroup = RawGroupList.FirstOrDefault(x => x.Dt == item.Dt && x.Id == item.Id && x.PaperWidth == item.PaperWidth);

                var nameTextBlock = new TextBlock();
                string name = "";
                if (rawGroup != null)
                {
                    name = rawGroup.Name;
                }

                nameTextBlock.Text = $"{name} {item.PaperWidth}";
                nameTextBlock.Name = "NameTextBlock";
                nameTextBlock.HorizontalAlignment = HorizontalAlignment.Center;
                nameTextBlock.VerticalAlignment = VerticalAlignment.Center;

                stackPanel.Children.Add(nameTextBlock);

                var priorityTextBlock = new TextBlock();
                string priority = "";
                if (rawGroup != null)
                {
                    priority = rawGroup.PriorityName;
                }

                priorityTextBlock.Text = priority;
                priorityTextBlock.HorizontalAlignment = HorizontalAlignment.Center;
                priorityTextBlock.VerticalAlignment = VerticalAlignment.Center;

                stackPanel.Children.Add(priorityTextBlock);

                {
                    var idRawGroupTextBlock = new TextBlock();
                    idRawGroupTextBlock.Name = "IdRawGroupTextBlock";
                    idRawGroupTextBlock.Text = item.Id.ToString();
                    idRawGroupTextBlock.Visibility = Visibility.Collapsed;
                    stackPanel.Children.Add(idRawGroupTextBlock);

                    var paperWidthTextBlock = new TextBlock();
                    paperWidthTextBlock.Name = "PaperWidthTextBlock";
                    paperWidthTextBlock.Text = item.PaperWidth.ToString();
                    paperWidthTextBlock.Visibility = Visibility.Collapsed;
                    stackPanel.Children.Add(paperWidthTextBlock);
                }

                b.Child = stackPanel;

                b.MouseRightButtonDown += (object sender, MouseButtonEventArgs e) =>
                {
                    string idRawGroup = "";
                    string paperWidth = "";
                    string name = "";

                    var borderOfSender = (Border)sender;
                    var stackPanelOfBorder =  (StackPanel)borderOfSender.Child;
                    var childreCollection = stackPanelOfBorder.Children;
                    foreach (var item in childreCollection)
                    {
                        var textBlock = (TextBlock)item;
                        if (textBlock.Name == "IdRawGroupTextBlock")
                        {
                            idRawGroup = textBlock.Text;
                        }
                        else if (textBlock.Name == "PaperWidthTextBlock")
                        {
                            paperWidth = textBlock.Text;
                        }
                        else if (textBlock.Name == "NameTextBlock")
                        {
                            name = textBlock.Text;
                        }
                    }

                    ContextMenu contextMenu = new ContextMenu();
                    var menuItem = new MenuItem { Header = "Изменить приоритет", IsEnabled = false };

                    if (Central.Navigator.GetRoleLevel("[erp]raw_material_group_planner") > Role.AccessMode.ReadOnly)
                    {
                        menuItem.IsEnabled = true;
                    }

                    menuItem.Click += (object sender, RoutedEventArgs e) =>
                    {
                        EditPriority(idRawGroup, paperWidth, name);
                    };
                    contextMenu.Items.Add(menuItem);
                    contextMenu.IsOpen = true;

                    e.Handled = true;

                };

                BorderList.Add(b);
            }

            int i = 0;
            foreach (var item in BorderList)
            {
                item.VerticalAlignment = VerticalAlignment.Top;
                item.HorizontalAlignment = HorizontalAlignment.Left;

                item.Margin = new Thickness(0, i * (item.Height + OffsetBlocks), 0, 0);

                i += 1;

                HeaderGrid.Children.Add(item);
            }
        }

        public void EditPriority(string idRawGroup, string paperWidth, string name)
        {
            var i = new RawMaterialGroupEditPriority(idRawGroup, paperWidth);
            i.Show(name);
        }

        /// <summary>
        /// Получаем данные по сырьевой группе (кол-во на БДМ) по датам
        /// </summary>
        public void GetDataForRawGroupByDate()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "RawMaterialGroup");
            q.Request.SetParam("Action", "RawGroupListByDate");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    RawGroupByDateDs = ListDataSet.Create(result, "ITEMS");
                    if (RawGroupByDateDs.Items.Count > 0)
                    {
                        foreach (var item in RawGroupByDateDs.Items)
                        {
                            var rawMaterialGroup = RawGroupList.FirstOrDefault(x => x.Id == item.CheckGet("ID").ToInt() && x.Dt == item.CheckGet("DT") && x.PaperWidth == item.CheckGet("WIDTH").ToInt());
                            if(rawMaterialGroup!=null)
                            {
                                rawMaterialGroup.QuantityInPaperMachine = item.CheckGet("QTY").ToInt();
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
        /// Рассчитывает значения поля Кол-во на складе для всех дней кроме первого
        /// </summary>
        public void CalculateQtyStock()
        {
            RawGroupList = RawGroupList.OrderBy(x => x.Dt.ToDateTime("dd.MM.yyyy")).ToList();

            foreach (var item in RawGroupList)
            {
                var dt = item.Dt;
                var dateTime = item.Dt.ToDateTime("dd.MM.yyyy");

                if (dt != Column1DtShort)
                {
                    // Рассчитываем значения для кол-во на Складе
                    // (Значение кол-во на Складе предыдущего дня + кол-во БДМ предыдущего дня) - (кол-во на ГА предыдущего дня + кол-во Заявка предыдущего дня)

                    var dateTimeOld = dateTime.AddDays(-1);
                    var dtOld = dateTimeOld.ToString("dd.MM.yyyy");

                    var rawGroupOld = RawGroupList.FirstOrDefault(x => x.Id == item.Id && x.Dt == dtOld && x.PaperWidth == item.PaperWidth);

                    if (rawGroupOld != null)
                    {
                        var quantityInStockOld = rawGroupOld.QuantityInStock;

                        var quantityInPaperMachineOld = rawGroupOld.QuantityInPaperMachine;

                        var quantityInCorrugatedMachineOld = rawGroupOld.QuantityInCorrugatedMachine;

                        double quantityInOrderOld = rawGroupOld.QuantityInOrder;

                        double quantityInStock = ((double)quantityInStockOld + (double)quantityInPaperMachineOld) - ((double)quantityInCorrugatedMachineOld + (double)quantityInOrderOld);

                        item.QuantityInStock = quantityInStock;
                    }
                }
            }
        }

        /// <summary>
        /// Создаём пустые экземпляры, чтобы в табличке не было пробелов
        /// </summary>
        public void CreateEmptyRawGroups()
        {
            var listRG = new List<RawMaterialGroup>();

            foreach (var item in RawGroupList)
            {
                if (item.Dt == Column1DtShort)
                {
                    listRG.Add(item);
                }
            }

            foreach (var item in listRG)
            {
                var id = item.Id;
                var width = item.PaperWidth;
                if (RawGroupList.FirstOrDefault(x => x.Id == id && x.PaperWidth == item.PaperWidth && x.Dt == Column2DtShort) == null)
                {
                    var rawMaterialGroup = new RawMaterialGroup(item.Id, Column2DtShort, "", 0, 0, 0, 0, item.PaperWidth);

                    RawGroupList.Add(rawMaterialGroup);
                }

                if (RawGroupList.FirstOrDefault(x => x.Id == id && x.PaperWidth == item.PaperWidth && x.Dt == Column3DtShort) == null)
                {
                    var rawMaterialGroup = new RawMaterialGroup(item.Id, Column3DtShort, "", 0, 0, 0, 0, item.PaperWidth);

                    RawGroupList.Add(rawMaterialGroup);
                }

                if (RawGroupList.FirstOrDefault(x => x.Id == id && x.PaperWidth == item.PaperWidth && x.Dt == Column4DtShort) == null)
                {
                    var rawMaterialGroup = new RawMaterialGroup(item.Id, Column4DtShort, "", 0, 0, 0, 0, item.PaperWidth);

                    RawGroupList.Add(rawMaterialGroup);
                }

                if (RawGroupList.FirstOrDefault(x => x.Id == id && x.PaperWidth == item.PaperWidth && x.Dt == Column5DtShort) == null)
                {
                    var rawMaterialGroup = new RawMaterialGroup(item.Id, Column5DtShort, "", 0, 0, 0, 0, item.PaperWidth);

                    RawGroupList.Add(rawMaterialGroup);
                }

                if (RawGroupList.FirstOrDefault(x => x.Id == id && x.PaperWidth == item.PaperWidth && x.Dt == Column6DtShort) == null)
                {
                    var rawMaterialGroup = new RawMaterialGroup(item.Id, Column6DtShort, "", 0, 0, 0, 0, item.PaperWidth);

                    RawGroupList.Add(rawMaterialGroup);
                }

                if (RawGroupList.FirstOrDefault(x => x.Id == id && x.PaperWidth == item.PaperWidth && x.Dt == Column7DtShort) == null)
                {
                    var rawMaterialGroup = new RawMaterialGroup(item.Id, Column7DtShort, "", 0, 0, 0, 0, item.PaperWidth);

                    RawGroupList.Add(rawMaterialGroup);
                }
            }

        }

        /// <summary>
        /// Распределение данных из листа с объектами RawGroup по соответствующим им (по дате) листам объектов RawGroupBlock (List<RawGroupBlock)
        /// </summary>
        public void SetDataIntoListRawGroupsToListBlocks()
        {
            ClearAllListBlocks();

            foreach (var item in RawGroupList)
            {
                var rawGroupBlock = new RawGroupBlock(item, item.Id, item.Dt, item.PaperWidth);
                rawGroupBlock.TopTextBlock.Text = ((item.QuantityInStock + item.QuantityInPaperMachine) - (item.QuantityInOrder + item.QuantityInCorrugatedMachine)).ToString();
                rawGroupBlock.OrderTextBlock.Text = ((double)item.QuantityInOrder).ToString();
                rawGroupBlock.StockTextBlock.Text = ((double)item.QuantityInStock).ToString();
                rawGroupBlock.CMTextBlock.Text = item.QuantityInCorrugatedMachine.ToString();
                rawGroupBlock.BDMTextBlock.Text = item.QuantityInPaperMachine.ToString();

                if (item.Dt == Column1DtShort)
                {
                    FirstBlocks.Add(rawGroupBlock);
                }
                else if (item.Dt == Column2DtShort)
                {
                    SecondBlocks.Add(rawGroupBlock);
                }
                else if (item.Dt == Column3DtShort)
                {
                    ThirdBlocks.Add(rawGroupBlock);
                }
                else if (item.Dt == Column4DtShort)
                {
                    FourthBlocks.Add(rawGroupBlock);
                }
                else if (item.Dt == Column5DtShort)
                {
                    FifthBlocks.Add(rawGroupBlock);
                }
                else if (item.Dt == Column6DtShort)
                {
                    SixthBlocks.Add(rawGroupBlock);
                }
                else if (item.Dt == Column7DtShort)
                {
                    SeventhBlocks.Add(rawGroupBlock);
                }
            }
        }

        /// <summary>
        /// Сортирует все блоки для всех колонок по Ид и Ширине бумаги (форм фактору)
        /// </summary>
        public void ListBlocksOrderBy()
        {
            FirstBlocks = FirstBlocks.OrderBy(x => x.Id).ThenBy(x => x.PaperWidth).ToList();
            SecondBlocks = SecondBlocks.OrderBy(x => x.Id).ThenBy(x => x.PaperWidth).ToList();
            ThirdBlocks = ThirdBlocks.OrderBy(x => x.Id).ThenBy(x => x.PaperWidth).ToList();
            FourthBlocks = FourthBlocks.OrderBy(x => x.Id).ThenBy(x => x.PaperWidth).ToList();
            FifthBlocks = FifthBlocks.OrderBy(x => x.Id).ThenBy(x => x.PaperWidth).ToList();
            SixthBlocks = SixthBlocks.OrderBy(x => x.Id).ThenBy(x => x.PaperWidth).ToList();
            SeventhBlocks = SeventhBlocks.OrderBy(x => x.Id).ThenBy(x => x.PaperWidth).ToList();
        }

        /// <summary>
        /// Преобразует значения всех текстблоков в нужный формат
        /// </summary>
        public void StringFormater()
        {
            foreach (var listBlock in DicDateOfRawGroupBlockList.Keys)
            {
                foreach (var item in listBlock)
                {
                    {
                        double dblStr = item.TopTextBlock.Text.ToDouble();
                        string str = dblStr.ToString("#,###,###,##0");
                        item.TopTextBlock.Text = str;
                    }

                    {
                        double dblStr = item.StockTextBlock.Text.ToDouble();
                        string str = dblStr.ToString("#,###,###,##0");
                        item.StockTextBlock.Text = str;
                    }

                    {
                        double dblStr = item.OrderTextBlock.Text.ToDouble();
                        string str = dblStr.ToString("#,###,###,##0");
                        item.OrderTextBlock.Text = str;
                    }

                    {
                        int intStr = item.CMTextBlock.Text.ToInt();
                        string str = intStr.ToString("#,###,###,##0");
                        item.CMTextBlock.Text = str;
                    }

                    {
                        int intStr = item.BDMTextBlock.Text.ToInt();
                        string str = intStr.ToString("#,###,###,##0");
                        item.BDMTextBlock.Text = str;
                    }
                }
            }
        }

        /// <summary>
        /// Очищает все листы List<RawGroupBlock>
        /// </summary>
        public void ClearAllListBlocks()
        {
            FirstBlocks.Clear();
            SecondBlocks.Clear();
            ThirdBlocks.Clear();
            FourthBlocks.Clear();
            FifthBlocks.Clear();
            SixthBlocks.Clear();
            SeventhBlocks.Clear();
        }

        /// <summary>
        /// Обновляет все колонки;
        /// Очищает их данные и присваивает ItemsSource повторно
        /// </summary>
        public void UpdateAllColumns()
        {
            //1
            {
                FirstGrid.Children.Clear();
                int i = 0;
                foreach (var item in FirstBlocks)
                {
                    item.VerticalAlignment = VerticalAlignment.Top;
                    item.HorizontalAlignment = HorizontalAlignment.Left;

                    item.Margin = new Thickness(0, i * (item.Height + OffsetBlocks), 0, 0);

                    i += 1;

                    FirstGrid.Children.Add(item);
                }

                FirstGridHeader.Text = FirstBlocks.First().Dt;
            }

            //2
            {
                SecondGrid.Children.Clear();
                int i = 0;
                foreach (var item in SecondBlocks)
                {
                    item.VerticalAlignment = VerticalAlignment.Top;
                    item.HorizontalAlignment = HorizontalAlignment.Left;

                    item.Margin = new Thickness(0, i * (item.Height + OffsetBlocks), 0, 0);

                    i += 1;

                    SecondGrid.Children.Add(item);
                }

                SecondGridHeader.Text = SecondBlocks.First().Dt;
            }

            //3
            {
                ThirdGrid.Children.Clear();
                int i = 0;
                foreach (var item in ThirdBlocks)
                {
                    item.VerticalAlignment = VerticalAlignment.Top;
                    item.HorizontalAlignment = HorizontalAlignment.Left;

                    item.Margin = new Thickness(0, i * (item.Height + OffsetBlocks), 0, 0);

                    i += 1;

                    ThirdGrid.Children.Add(item);
                }

                ThirdGridHeader.Text = ThirdBlocks.First().Dt;
            }

            //4
            {
                FourthGrid.Children.Clear();
                int i = 0;
                foreach (var item in FourthBlocks)
                {
                    item.VerticalAlignment = VerticalAlignment.Top;
                    item.HorizontalAlignment = HorizontalAlignment.Left;

                    item.Margin = new Thickness(0, i * (item.Height + OffsetBlocks), 0, 0);

                    i += 1;

                    FourthGrid.Children.Add(item);
                }

                FourthGridHeader.Text = FourthBlocks.First().Dt;
            }

            //5
            {
                FifthGrid.Children.Clear();
                int i = 0;
                foreach (var item in FifthBlocks)
                {
                    item.VerticalAlignment = VerticalAlignment.Top;
                    item.HorizontalAlignment = HorizontalAlignment.Left;

                    item.Margin = new Thickness(0, i * (item.Height + OffsetBlocks), 0, 0);

                    i += 1;

                    FifthGrid.Children.Add(item);
                }

                FifthGridHeader.Text = FifthBlocks.First().Dt;
            }

            //6
            {
                SixthGrid.Children.Clear();
                int i = 0;
                foreach (var item in SixthBlocks)
                {
                    item.VerticalAlignment = VerticalAlignment.Top;
                    item.HorizontalAlignment = HorizontalAlignment.Left;

                    item.Margin = new Thickness(0, i * (item.Height + OffsetBlocks), 0, 0);

                    i += 1;

                    SixthGrid.Children.Add(item);
                }

                SixthGridHeader.Text = SixthBlocks.First().Dt;
            }

            //7
            {
                SeventhGrid.Children.Clear();
                int i = 0;
                foreach (var item in SeventhBlocks)
                {
                    item.VerticalAlignment = VerticalAlignment.Top;
                    item.HorizontalAlignment = HorizontalAlignment.Left;

                    item.Margin = new Thickness(0, i * (item.Height + OffsetBlocks), 0, 0);

                    i += 1;

                    SeventhGrid.Children.Add(item);
                }

                SeventhGridHeader.Text = SeventhBlocks.First().Dt;
            }
        }

        /// <summary>
        /// Пересчитываем данные на основе поставленных галочек
        /// </summary>
        public void RecalculateByBlockedFlag(List<RawMaterialGroup> _сheckedRawMaterialGroupList = null)
        {
            // Устанавливаем коэффициенты по умолчанию для всех заявок
            foreach (var item in CompositionList.DsOfOrder.Items)
            {
                var order = CompositionList.ListOfOrder.FirstOrDefault(x => item.CheckGet("DT") == x.DT && item.CheckGet("IDC").ToInt() == x.IDC && item.CheckGet("QTY").ToInt() == x.QtyOrder && item.CheckGet("BLANK_WIDTH").ToInt() == x.BlankWidth && item.CheckGet("BLANK_LENGHT").ToInt() == x.BlankLength);

                order.DicOfTrimWidth.CheckAdd("1900", item.CheckGet("TRIM_WIDTH_1900"));

                order.DicOfTrimWidth.CheckAdd("2000", item.CheckGet("TRIM_WIDTH_2000"));

                order.DicOfTrimWidth.CheckAdd("2100", item.CheckGet("TRIM_WIDTH_2100"));

                order.DicOfTrimWidth.CheckAdd("2200", item.CheckGet("TRIM_WIDTH_2200"));

                order.DicOfTrimWidth.CheckAdd("2300", item.CheckGet("TRIM_WIDTH_2300"));

                order.DicOfTrimWidth.CheckAdd("2400", item.CheckGet("TRIM_WIDTH_2400"));

                order.DicOfTrimWidth.CheckAdd("2500", item.CheckGet("TRIM_WIDTH_2500"));

                order.DicOfTrimWidth.CheckAdd("2700", item.CheckGet("TRIM_WIDTH_2700"));

                order.DicOfTrimWidth.CheckAdd("2800", item.CheckGet("TRIM_WIDTH_2800"));
            }

            List<RawMaterialGroup> CheckedRawMaterialGroupList = new List<RawMaterialGroup>();

            if (_сheckedRawMaterialGroupList != null)
            {
                CheckedRawMaterialGroupList = _сheckedRawMaterialGroupList;
            }
            else
            {
                // Проходим по всем сырьевым группам и добавляем в лист те, у которых отмечен чек бокс  //FIXME (для оптимизации)возможно стоит сделать наоборот, сначала искать все RawGroupBlock с отмеченым чекбоксом, и потом для них уже находить RawMaterialGroup 
                foreach (var rawGroup in RawGroupList)
                {
                    if (rawGroup.Dt == Column1DtShort)
                    {
                        if (FirstBlocks.FirstOrDefault(x => x.Id == rawGroup.Id && x.PaperWidth == rawGroup.PaperWidth && x.Checked) != null)
                        {
                            CheckedRawMaterialGroupList.Add(rawGroup);
                        }
                    }
                    else if (rawGroup.Dt == Column2DtShort)
                    {
                        if (SecondBlocks.FirstOrDefault(x => x.Id == rawGroup.Id && x.PaperWidth == rawGroup.PaperWidth && x.Checked) != null)
                        {
                            CheckedRawMaterialGroupList.Add(rawGroup);
                        }
                    }
                    else if (rawGroup.Dt == Column3DtShort)
                    {
                        if (ThirdBlocks.FirstOrDefault(x => x.Id == rawGroup.Id && x.PaperWidth == rawGroup.PaperWidth && x.Checked) != null)
                        {
                            CheckedRawMaterialGroupList.Add(rawGroup);
                        }
                    }
                    else if (rawGroup.Dt == Column4DtShort)
                    {
                        if (FourthBlocks.FirstOrDefault(x => x.Id == rawGroup.Id && x.PaperWidth == rawGroup.PaperWidth && x.Checked) != null)
                        {
                            CheckedRawMaterialGroupList.Add(rawGroup);
                        }
                    }
                    else if (rawGroup.Dt == Column5DtShort)
                    {
                        if (FifthBlocks.FirstOrDefault(x => x.Id == rawGroup.Id && x.PaperWidth == rawGroup.PaperWidth && x.Checked) != null)
                        {
                            CheckedRawMaterialGroupList.Add(rawGroup);
                        }
                    }
                    else if (rawGroup.Dt == Column6DtShort)
                    {
                        if (SixthBlocks.FirstOrDefault(x => x.Id == rawGroup.Id && x.PaperWidth == rawGroup.PaperWidth && x.Checked) != null)
                        {
                            CheckedRawMaterialGroupList.Add(rawGroup);
                        }
                    }
                    else if (rawGroup.Dt == Column7DtShort)
                    {
                        if (SeventhBlocks.FirstOrDefault(x => x.Id == rawGroup.Id && x.PaperWidth == rawGroup.PaperWidth && x.Checked) != null)
                        {
                            CheckedRawMaterialGroupList.Add(rawGroup);
                        }
                    }
                }
            }

            foreach (var rawGroup in CheckedRawMaterialGroupList)
            {
                var width = rawGroup.PaperWidth;
                var idRawGroup = rawGroup.Id;
                var dt = rawGroup.Dt;

                List<int> idCartonList = new List<int>();

                foreach (var item in CompositionList.DicOfFactor)
                {
                    if (item.Value.ContainsKey(idRawGroup))
                    {
                        if (!idCartonList.Contains(item.Key))
                        {
                            idCartonList.Add(item.Key);
                        }
                    }
                }

                List<Order> _orderList = CompositionList.ListOfOrder.Where(x => x.DT == dt).ToList();

                // Список заявок на выбранную дату и с тем картоном, в котором используется выбранная сырьевая группа
                List<Order> orderList = new List<Order>();
                foreach (var idCarton in idCartonList)
                {
                    foreach (var order in _orderList)
                    {
                        if (order.IDC == idCarton)
                        {
                            orderList.Add(order);
                        }
                    }
                }

                // Проходим по заявкам с заданной датой и с тем картоном, в котором используется выбранная сырьевая группа
                // И заменяем в словаре с шириной обрези и коэффициентами запись с шириной обрези равной ширине выбранной сырьевой группы коэффициент на еденицу
                // Чтобы при выборе оптимальной обрези эта ширина обрези не использовалась
                foreach (var order in orderList)
                {
                    order.DicOfTrimWidth.CheckAdd(width.ToString(), "1");
                }
            }

            CompositionList.CalculateOrderData();

            CompositionList.CartonGrid.ClearItems();

            // Пересоздаём весь картон по пересчитанным заявкам и наполняем грид вкладки Картон
            CompositionList.CreateCartonBlock();

            // Очищаем значение поля Количество в заявках во всех сырьевых группах
            foreach (var rawGroup in RawGroupList)
            {
                rawGroup.QuantityInOrder = 0;
            }

            // Очищаем значение поля ListOfOrderData (список заявко на эту сырьевую группу) для всех RawMaterialGroup
            ClearOrderListForAllRawMaterialGroup();

            // Перезаполняем значение поля Количество в заявках во всех сырьевых группах по новому картону
            // Перезаполняем списки заявок (rawGroup.ListOfOrderData)
            foreach (var item in CompositionList.DicDateOfCartonList)
            {
                var listOfCartonBlock = item.Key;
                var rawGroupList = RawGroupList.Where(x => x.Dt == item.Value).ToList();

                foreach (var carton in listOfCartonBlock)
                {
                    if (carton.DicOFFactorByRawGroup.Count > 0)
                    {
                        foreach (var rg in carton.DicOFFactorByRawGroup)
                        {
                            var rawGroup = rawGroupList.FirstOrDefault(x => x.Id == rg.Key && x.PaperWidth == carton.TrimWidth);
                            if (rawGroup != null)
                            {
                                rawGroup.QuantityInOrder += carton.QtySqrMetr * rg.Value;
                                rawGroup.ListOfOrderData.AddRange(carton.ListOfOrderData);
                            }
                        }
                    }
                }
            }

            // Перерасчитываем значение поля Количество на складе для сырьевых групп
            CalculateQtyStock();

            // Обновляем данные в существующих блоках
            foreach (var rawGroup in RawGroupList)
            {
                if (rawGroup.Dt == Column1DtShort)
                {
                    var rawGroupBlock = FirstBlocks.FirstOrDefault(x => x.Id == rawGroup.Id && x.PaperWidth == rawGroup.PaperWidth);
                    rawGroupBlock.TopTextBlock.Text = ((rawGroup.QuantityInStock + rawGroup.QuantityInPaperMachine) - (rawGroup.QuantityInOrder + rawGroup.QuantityInCorrugatedMachine)).ToString();
                    rawGroupBlock.OrderTextBlock.Text = ((double)rawGroup.QuantityInOrder).ToString();
                    rawGroupBlock.StockTextBlock.Text = ((double)rawGroup.QuantityInStock).ToString();
                    rawGroupBlock.CMTextBlock.Text = rawGroup.QuantityInCorrugatedMachine.ToString();
                    rawGroupBlock.BDMTextBlock.Text = rawGroup.QuantityInPaperMachine.ToString();
                }
                else if (rawGroup.Dt == Column2DtShort)
                {
                    var rawGroupBlock = SecondBlocks.FirstOrDefault(x => x.Id == rawGroup.Id && x.PaperWidth == rawGroup.PaperWidth);
                    rawGroupBlock.TopTextBlock.Text = ((rawGroup.QuantityInStock + rawGroup.QuantityInPaperMachine) - (rawGroup.QuantityInOrder + rawGroup.QuantityInCorrugatedMachine)).ToString();
                    rawGroupBlock.OrderTextBlock.Text = ((double)rawGroup.QuantityInOrder).ToString();
                    rawGroupBlock.StockTextBlock.Text = ((double)rawGroup.QuantityInStock).ToString();
                    rawGroupBlock.CMTextBlock.Text = rawGroup.QuantityInCorrugatedMachine.ToString();
                    rawGroupBlock.BDMTextBlock.Text = rawGroup.QuantityInPaperMachine.ToString();
                }
                else if (rawGroup.Dt == Column3DtShort)
                {
                    var rawGroupBlock = ThirdBlocks.FirstOrDefault(x => x.Id == rawGroup.Id && x.PaperWidth == rawGroup.PaperWidth);
                    rawGroupBlock.TopTextBlock.Text = ((rawGroup.QuantityInStock + rawGroup.QuantityInPaperMachine) - (rawGroup.QuantityInOrder + rawGroup.QuantityInCorrugatedMachine)).ToString();
                    rawGroupBlock.OrderTextBlock.Text = ((double)rawGroup.QuantityInOrder).ToString();
                    rawGroupBlock.StockTextBlock.Text = ((double)rawGroup.QuantityInStock).ToString();
                    rawGroupBlock.CMTextBlock.Text = rawGroup.QuantityInCorrugatedMachine.ToString();
                    rawGroupBlock.BDMTextBlock.Text = rawGroup.QuantityInPaperMachine.ToString();
                }
                else if (rawGroup.Dt == Column4DtShort)
                {
                    var rawGroupBlock = FourthBlocks.FirstOrDefault(x => x.Id == rawGroup.Id && x.PaperWidth == rawGroup.PaperWidth);
                    rawGroupBlock.TopTextBlock.Text = ((rawGroup.QuantityInStock + rawGroup.QuantityInPaperMachine) - (rawGroup.QuantityInOrder + rawGroup.QuantityInCorrugatedMachine)).ToString();
                    rawGroupBlock.OrderTextBlock.Text = ((double)rawGroup.QuantityInOrder).ToString();
                    rawGroupBlock.StockTextBlock.Text = ((double)rawGroup.QuantityInStock).ToString();
                    rawGroupBlock.CMTextBlock.Text = rawGroup.QuantityInCorrugatedMachine.ToString();
                    rawGroupBlock.BDMTextBlock.Text = rawGroup.QuantityInPaperMachine.ToString();
                }
                else if (rawGroup.Dt == Column5DtShort)
                {
                    var rawGroupBlock = FifthBlocks.FirstOrDefault(x => x.Id == rawGroup.Id && x.PaperWidth == rawGroup.PaperWidth);
                    rawGroupBlock.TopTextBlock.Text = ((rawGroup.QuantityInStock + rawGroup.QuantityInPaperMachine) - (rawGroup.QuantityInOrder + rawGroup.QuantityInCorrugatedMachine)).ToString();
                    rawGroupBlock.OrderTextBlock.Text = ((double)rawGroup.QuantityInOrder).ToString();
                    rawGroupBlock.StockTextBlock.Text = ((double)rawGroup.QuantityInStock).ToString();
                    rawGroupBlock.CMTextBlock.Text = rawGroup.QuantityInCorrugatedMachine.ToString();
                    rawGroupBlock.BDMTextBlock.Text = rawGroup.QuantityInPaperMachine.ToString();
                }
                else if (rawGroup.Dt == Column6DtShort)
                {
                    var rawGroupBlock = SixthBlocks.FirstOrDefault(x => x.Id == rawGroup.Id && x.PaperWidth == rawGroup.PaperWidth);
                    rawGroupBlock.TopTextBlock.Text = ((rawGroup.QuantityInStock + rawGroup.QuantityInPaperMachine) - (rawGroup.QuantityInOrder + rawGroup.QuantityInCorrugatedMachine)).ToString();
                    rawGroupBlock.OrderTextBlock.Text = ((double)rawGroup.QuantityInOrder).ToString();
                    rawGroupBlock.StockTextBlock.Text = ((double)rawGroup.QuantityInStock).ToString();
                    rawGroupBlock.CMTextBlock.Text = rawGroup.QuantityInCorrugatedMachine.ToString();
                    rawGroupBlock.BDMTextBlock.Text = rawGroup.QuantityInPaperMachine.ToString();
                }
                else if (rawGroup.Dt == Column7DtShort)
                {
                    var rawGroupBlock = SeventhBlocks.FirstOrDefault(x => x.Id == rawGroup.Id && x.PaperWidth == rawGroup.PaperWidth);
                    rawGroupBlock.TopTextBlock.Text = ((rawGroup.QuantityInStock + rawGroup.QuantityInPaperMachine) - (rawGroup.QuantityInOrder + rawGroup.QuantityInCorrugatedMachine)).ToString();
                    rawGroupBlock.OrderTextBlock.Text = ((double)rawGroup.QuantityInOrder).ToString();
                    rawGroupBlock.StockTextBlock.Text = ((double)rawGroup.QuantityInStock).ToString();
                    rawGroupBlock.CMTextBlock.Text = rawGroup.QuantityInCorrugatedMachine.ToString();
                    rawGroupBlock.BDMTextBlock.Text = rawGroup.QuantityInPaperMachine.ToString();
                }
            }

            StringFormater();
        }

        /// <summary>
        /// По результатам замер оказался менее эффективным, чем полная перезагрузка интерфейса
        /// Пересчёт данных на осовании изменения приоритета
        /// </summary>
        public void RecalculateByPriority(Dictionary<string, string> contextObject)
        {
            // получаем блок сырьевоё группы, для которой изменили приоритет
            // меняем данные в блоке
            // меняем данные в соответствующем объекте класса
            // пересчитываем

            int idRawGroup = contextObject.CheckGet("ID_RAW_GROUP").ToInt();
            int paperWidth = contextObject.CheckGet("PAPER_WIDTH").ToInt();
            int priority = contextObject.CheckGet("PRIORITY").ToInt();
            string priorityName = "";

            switch (priority)
            {
                case 0:
                    priorityName = "Не использовать";
                    break;

                case 1:
                    priorityName = "Низкий";
                    break;

                case 2:
                    priorityName = "Средний";
                    break;

                case 3:
                    priorityName = "Высокий";
                    break;

                default:
                    break;
            }

            List<RawMaterialGroup> rawMaterialGroups = RawGroupList.Where(x => x.Id == idRawGroup && x.PaperWidth == paperWidth).ToList();
            foreach (var rawMaterialGroup in rawMaterialGroups)
            {
                rawMaterialGroup.Priority = priority;
                rawMaterialGroup.PriorityName = priorityName;
            }

            CompositionList.CalculateOrderData();

            CompositionList.CartonGrid.ClearItems();

            // Пересоздаём весь картон по пересчитанным заявкам и наполняем грид вкладки Картон
            CompositionList.CreateCartonBlock();

            // Очищаем значение поля Количество в заявках во всех сырьевых группах
            foreach (var rawGroup in RawGroupList)
            {
                rawGroup.QuantityInOrder = 0;
            }

            // Очищаем значение поля ListOfOrderData (список заявко на эту сырьевую группу) для всех RawMaterialGroup
            ClearOrderListForAllRawMaterialGroup();

            // Перезаполняем значение поля Количество в заявках во всех сырьевых группах по новому картону
            // Перезаполняем списки заявок (rawGroup.ListOfOrderData)
            foreach (var item in CompositionList.DicDateOfCartonList)
            {
                var listOfCartonBlock = item.Key;
                var rawGroupList = RawGroupList.Where(x => x.Dt == item.Value).ToList();

                foreach (var carton in listOfCartonBlock)
                {
                    if (carton.DicOFFactorByRawGroup.Count > 0)
                    {
                        foreach (var rg in carton.DicOFFactorByRawGroup)
                        {
                            var rawGroup = rawGroupList.FirstOrDefault(x => x.Id == rg.Key && x.PaperWidth == carton.TrimWidth);
                            if (rawGroup != null)
                            {
                                rawGroup.QuantityInOrder += carton.QtySqrMetr * rg.Value;
                                rawGroup.ListOfOrderData.AddRange(carton.ListOfOrderData);
                            }
                        }
                    }
                }
            }

            // Перерасчитываем значение поля Количество на складе для сырьевых групп
            CalculateQtyStock();

            SetHeaderGridItems();

            // Обновляем данные в существующих блоках
            foreach (var rawGroup in RawGroupList)
            {
                if (rawGroup.Dt == Column1DtShort)
                {
                    var rawGroupBlock = FirstBlocks.FirstOrDefault(x => x.Id == rawGroup.Id && x.PaperWidth == rawGroup.PaperWidth);
                    rawGroupBlock.TopTextBlock.Text = ((rawGroup.QuantityInStock + rawGroup.QuantityInPaperMachine) - (rawGroup.QuantityInOrder + rawGroup.QuantityInCorrugatedMachine)).ToString();
                    rawGroupBlock.OrderTextBlock.Text = ((double)rawGroup.QuantityInOrder).ToString();
                    rawGroupBlock.StockTextBlock.Text = ((double)rawGroup.QuantityInStock).ToString();
                    rawGroupBlock.CMTextBlock.Text = rawGroup.QuantityInCorrugatedMachine.ToString();
                    rawGroupBlock.BDMTextBlock.Text = rawGroup.QuantityInPaperMachine.ToString();
                }
                else if (rawGroup.Dt == Column2DtShort)
                {
                    var rawGroupBlock = SecondBlocks.FirstOrDefault(x => x.Id == rawGroup.Id && x.PaperWidth == rawGroup.PaperWidth);
                    rawGroupBlock.TopTextBlock.Text = ((rawGroup.QuantityInStock + rawGroup.QuantityInPaperMachine) - (rawGroup.QuantityInOrder + rawGroup.QuantityInCorrugatedMachine)).ToString();
                    rawGroupBlock.OrderTextBlock.Text = ((double)rawGroup.QuantityInOrder).ToString();
                    rawGroupBlock.StockTextBlock.Text = ((double)rawGroup.QuantityInStock).ToString();
                    rawGroupBlock.CMTextBlock.Text = rawGroup.QuantityInCorrugatedMachine.ToString();
                    rawGroupBlock.BDMTextBlock.Text = rawGroup.QuantityInPaperMachine.ToString();
                }
                else if (rawGroup.Dt == Column3DtShort)
                {
                    var rawGroupBlock = ThirdBlocks.FirstOrDefault(x => x.Id == rawGroup.Id && x.PaperWidth == rawGroup.PaperWidth);
                    rawGroupBlock.TopTextBlock.Text = ((rawGroup.QuantityInStock + rawGroup.QuantityInPaperMachine) - (rawGroup.QuantityInOrder + rawGroup.QuantityInCorrugatedMachine)).ToString();
                    rawGroupBlock.OrderTextBlock.Text = ((double)rawGroup.QuantityInOrder).ToString();
                    rawGroupBlock.StockTextBlock.Text = ((double)rawGroup.QuantityInStock).ToString();
                    rawGroupBlock.CMTextBlock.Text = rawGroup.QuantityInCorrugatedMachine.ToString();
                    rawGroupBlock.BDMTextBlock.Text = rawGroup.QuantityInPaperMachine.ToString();
                }
                else if (rawGroup.Dt == Column4DtShort)
                {
                    var rawGroupBlock = FourthBlocks.FirstOrDefault(x => x.Id == rawGroup.Id && x.PaperWidth == rawGroup.PaperWidth);
                    rawGroupBlock.TopTextBlock.Text = ((rawGroup.QuantityInStock + rawGroup.QuantityInPaperMachine) - (rawGroup.QuantityInOrder + rawGroup.QuantityInCorrugatedMachine)).ToString();
                    rawGroupBlock.OrderTextBlock.Text = ((double)rawGroup.QuantityInOrder).ToString();
                    rawGroupBlock.StockTextBlock.Text = ((double)rawGroup.QuantityInStock).ToString();
                    rawGroupBlock.CMTextBlock.Text = rawGroup.QuantityInCorrugatedMachine.ToString();
                    rawGroupBlock.BDMTextBlock.Text = rawGroup.QuantityInPaperMachine.ToString();
                }
                else if (rawGroup.Dt == Column5DtShort)
                {
                    var rawGroupBlock = FifthBlocks.FirstOrDefault(x => x.Id == rawGroup.Id && x.PaperWidth == rawGroup.PaperWidth);
                    rawGroupBlock.TopTextBlock.Text = ((rawGroup.QuantityInStock + rawGroup.QuantityInPaperMachine) - (rawGroup.QuantityInOrder + rawGroup.QuantityInCorrugatedMachine)).ToString();
                    rawGroupBlock.OrderTextBlock.Text = ((double)rawGroup.QuantityInOrder).ToString();
                    rawGroupBlock.StockTextBlock.Text = ((double)rawGroup.QuantityInStock).ToString();
                    rawGroupBlock.CMTextBlock.Text = rawGroup.QuantityInCorrugatedMachine.ToString();
                    rawGroupBlock.BDMTextBlock.Text = rawGroup.QuantityInPaperMachine.ToString();
                }
                else if (rawGroup.Dt == Column6DtShort)
                {
                    var rawGroupBlock = SixthBlocks.FirstOrDefault(x => x.Id == rawGroup.Id && x.PaperWidth == rawGroup.PaperWidth);
                    rawGroupBlock.TopTextBlock.Text = ((rawGroup.QuantityInStock + rawGroup.QuantityInPaperMachine) - (rawGroup.QuantityInOrder + rawGroup.QuantityInCorrugatedMachine)).ToString();
                    rawGroupBlock.OrderTextBlock.Text = ((double)rawGroup.QuantityInOrder).ToString();
                    rawGroupBlock.StockTextBlock.Text = ((double)rawGroup.QuantityInStock).ToString();
                    rawGroupBlock.CMTextBlock.Text = rawGroup.QuantityInCorrugatedMachine.ToString();
                    rawGroupBlock.BDMTextBlock.Text = rawGroup.QuantityInPaperMachine.ToString();
                }
                else if (rawGroup.Dt == Column7DtShort)
                {
                    var rawGroupBlock = SeventhBlocks.FirstOrDefault(x => x.Id == rawGroup.Id && x.PaperWidth == rawGroup.PaperWidth);
                    rawGroupBlock.TopTextBlock.Text = ((rawGroup.QuantityInStock + rawGroup.QuantityInPaperMachine) - (rawGroup.QuantityInOrder + rawGroup.QuantityInCorrugatedMachine)).ToString();
                    rawGroupBlock.OrderTextBlock.Text = ((double)rawGroup.QuantityInOrder).ToString();
                    rawGroupBlock.StockTextBlock.Text = ((double)rawGroup.QuantityInStock).ToString();
                    rawGroupBlock.CMTextBlock.Text = rawGroup.QuantityInCorrugatedMachine.ToString();
                    rawGroupBlock.BDMTextBlock.Text = rawGroup.QuantityInPaperMachine.ToString();
                }
            }

            StringFormater();
        }

        /// <summary>
        /// Проходим по всем объектам RawMaterialGroup и очищаем поле ListOfOrderData
        /// </summary>
        public void ClearOrderListForAllRawMaterialGroup()
        {
            foreach (var rawGroup in RawGroupList)
            {
                rawGroup.ListOfOrderData = new List<Dictionary<string, string>>();
            }
        }

        /// <summary>
        /// Ищет отмеченный (чекбоксом) блок в определённой (по дате) колонке
        /// </summary>
        public void SearchCheckedBlock()
        {
            if (Message != null)
            {
                if (Message.Count > 0)
                {
                    var dt = Message.CheckGet("DT");
                    var id = Message.CheckGet("ID").ToInt();
                    var width = Message.CheckGet("WIDTH").ToInt();

                    // Сырьевая группа, которую отметили чекбоксом
                    RawMaterialGroup rawGroup = RawGroupList.FirstOrDefault(x => x.Id == id && x.PaperWidth == width && x.Dt == dt);

                    // Лист с Ид картона, который использует отмеченную сырьевую группу
                    List<int> idCartonList = new List<int>();
                    // Лист с названиями картона/шириной, которые используют отмеченную сырьевую группу 
                    List<string> cartonDescriptionList = new List<string>();
                    foreach (var item in CompositionList.DicOfFactor)
                    {
                        if (item.Value.ContainsKey(id))
                        {
                            if (!idCartonList.Contains(item.Key))
                            {
                                idCartonList.Add(item.Key);
                                var cartonDescription = CompositionList.HeaderDs.Items.FirstOrDefault(x => x.CheckGet("IDC").ToInt() == item.Key).CheckGet("DESCRIPTION");
                                cartonDescription = $"{cartonDescription}/{width}";
                                cartonDescriptionList.Add(cartonDescription);
                            }
                        }
                    }

                    List<Dictionary<string, string>> сheckedHeaderAndDate = new List<Dictionary<string, string>>();

                    foreach (var description in cartonDescriptionList)
                    {
                        var dic = new Dictionary<string, string>();

                        dic.Add(dt, description);

                        сheckedHeaderAndDate.Add(dic);
                    }

                    CompositionList.CheckedHeaderAndDate.AddRange(сheckedHeaderAndDate);
                }
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
                ReceiverGroup = "Preproduction",
                ReceiverName = "",
                SenderName = "RawMaterialGroupList",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.IndexOf("Preproduction") > -1)
            {
                if (m.ReceiverName.IndexOf("RawMaterialGroupList") > -1)
                {
                    switch (m.Action)
                    {
                        case "Check":
                            //Message = (Dictionary<string, string>)m.ContextObject;
                            //SearchCheckedBlock();
                            break;

                        case "GetListOrder":
                            GetListOrder((Dictionary<string, string>)m.ContextObject);
                            break;

                        case "LoadData":
                            LoadData();
                            break;

                        case "RecalculateByPriority":
                            RecalculateByPriority((Dictionary<string, string>)m.ContextObject);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Получить список заявок для конкрентной сырьевой группы 
        /// </summary>
        /// <param name="message"></param>
        public void GetListOrder(Dictionary<string, string> message)
        {
            var dt = message.CheckGet("DT");
            var id = message.CheckGet("ID").ToInt();
            var width = message.CheckGet("WIDTH").ToInt();

            RawMaterialGroup rawGroup = RawGroupList.FirstOrDefault(x => x.Id == id && x.PaperWidth == width && x.Dt == dt);
            if (rawGroup != null)
            {
                List<Dictionary<string, string>> listOfOrderData = rawGroup.ListOfOrderData;

                if (listOfOrderData != null && listOfOrderData.Count > 0)
                {
                    var rawMaterialGroupListOrder = new RawMaterialGroupListOrder("RawMaterialGroupPlanner_RawMaterialGroup");
                    rawMaterialGroupListOrder.GridDataSet.Items = listOfOrderData;
                    rawMaterialGroupListOrder.Show($"Список заявок для сырьевой группы {rawGroup.Name}/{rawGroup.PaperWidth} на {rawGroup.Dt}");
                }
                else
                {
                    string msg = $"Для сырьевой группы {rawGroup.Name}/{rawGroup.PaperWidth} на {rawGroup.Dt} нет заявок";
                    var d = new DialogWindow($"{msg}", "Список заявок", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }        
        }

        /// <summary>
        /// Сохранение данных по статусу заблокированности сырьевых групп
        /// </summary>
        public void SaveBlockedData()
        {
            List<Dictionary<string, string>> listChangedBlocks = new List<Dictionary<string, string>>();

            foreach (var listRawGroupBlocks in DicDateOfRawGroupBlockList.Keys)
            {
                foreach (var rawBlock in listRawGroupBlocks)
                {
                    if (rawBlock.Blocked != rawBlock.Checked)
                    {
                        var dictionary = new Dictionary<string, string>();
                        dictionary.Add("ID_RAW_GROUP", rawBlock.Id.ToString());
                        dictionary.Add("WIDTH", rawBlock.PaperWidth.ToString());
                        dictionary.Add("DT", rawBlock.Dt);
                        dictionary.Add("BLOCKED", rawBlock.Blocked.ToInt().ToString());
                        listChangedBlocks.Add(dictionary);
                    }
                }
            }

            if (listChangedBlocks != null && listChangedBlocks.Count > 0)
            {
                var p = new Dictionary<string, string>();
                p.Add("CHANGED_BLOCKS", JsonConvert.SerializeObject(listChangedBlocks));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "RawMaterialGroup");
                q.Request.SetParam("Action", "UpdateBlockedRawGroup");

                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    int changedRowCount = 0;

                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds.Items.Count > 0)
                        {
                            changedRowCount = ds.Items.First().CheckGet("CHANGER_ROW_COUNT").ToInt();
                        }
                    }

                    if (changedRowCount > 0)
                    {
                        // Очищаем флаги блокировки для всех объектов RawMaterialGroup
                        ClearBlockedFlagForAllRawGroup();
                        // Обновляем флаг блокировки для объектов RawMaterialGroup по запросу в БД
                        GetBlockedDataForRawGroup();
                        // Обновляем флаг блокировки для всех визуальных блоков RawGroupBlock
                        UpdateBlockedFlagForAllRawGroupBlock();

                        var msg = $"Успешное обновление флага блокировки в выбранных сырьевых группах.{Environment.NewLine}Количество обновлённых данных = {changedRowCount}.";
                        var d = new DialogWindow($"{msg}", "Планирование сырьевых групп", "", DialogWindowButtons.OK);
                        d.ShowDialog();

                    }
                    else
                    {
                        var msg = $"Ошибка обновление флага блокировки в выбранных сырьевых группах.";
                        var d = new DialogWindow($"{msg}", "Планирование сырьевых групп", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
            else
            {
                var msg = $"Нет изменённых данных для сохранения.";
                var d = new DialogWindow($"{msg}", "Планирование сырьевых групп", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Очищаем поле BlockedFlag во всех объектах RawMaterialGroup
        /// </summary>
        public void ClearBlockedFlagForAllRawGroup()
        {
            foreach (var rawGroup in RawGroupList)
            {
                rawGroup.BlockedFlag = false;
            }
        }

        /// <summary>
        /// Обновляем значение флага Blocked всех RawGroupBlock по связанным с ними RawMaterialGroup
        /// </summary>
        public void UpdateBlockedFlagForAllRawGroupBlock()
        {
            foreach (var listRawGroupBlocks in DicDateOfRawGroupBlockList.Keys)
            {
                foreach (var rawBlock in listRawGroupBlocks)
                {
                    rawBlock.UpdateBlockedFlag();
                }
            }
        }

        /// <summary>
        /// Документация
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp-new/planing/planning_raw_group/raw_groups");
        }

        private void GetButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void RecalculateButton_Click(object sender, RoutedEventArgs e)
        {
            RecalculateByBlockedFlag();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveBlockedData();
        }
    }
}
