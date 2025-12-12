using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Stock;
using DevExpress.Xpo.DB;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.PaperProduction
{
    /// <summary>
    /// Отчеты по приему машин с макулатурой на БДМ
    /// <author>Greshnyh_ni</author>
    /// <version>1</version>
    /// <released>2025-09-01</released>
    /// <changed>2025-09-12</changed>
    /// </summary>
    public partial class ScrapPaperReportTab : ControlBase
    {
        public FormHelper Form { get; set; }

        /// <summary>
        /// название отчета
        /// </summary>
        private string TitleReport = "";

        /// <summary>
        /// список ID2 выбранные чек-боксы с категорией макулатуры
        /// </summary>
        private string StrFilter = "";

        /// <summary>
        /// данные из выбранной в гриде отчетов строки
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// Признак начала формирования отчета
        /// </summary>
        private bool ReportIsRun { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ScrapPaperReportTab()
        {
            ControlTitle = "Отчеты по макулатуре";
            DocumentationUrl = "/doc/l-pack-erp-new/lt/scrap_paper_bdm";
            RoleName = "[erp]scrap_paper_bdm";

            InitializeComponent();

            Form = null;

            //регистрация обработчика сообщений
            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnKeyPressed = (System.Windows.Input.KeyEventArgs e) =>
            {

                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };

            //конструктор, будет вызван, когда объект создается
            //здесь создаются все внутренние структуры
            //впервые этот коллбэк будет вызван, когда данный таб станет активным
            //впервые (до этих пор, никакая работа внутри не происходит, что экономит ресурсы)
            OnLoad = () =>
            {
                FormInit();
                Report1GridInit();
                Report2GridInit();
                Report3GridInit();
                Report4GridInit();
                Report13GridInit();
                Report14GridInit();
                Report15GridInit();
                Report16GridInit();
                Report17GridInit();
                Report18GridInit();
                Report19GridInit();
                Report20GridInit();
                Report21GridInit();
                Report22GridInit();
                Report26GridInit();
                Report27GridInit();
                Report28GridInit();
                Report29GridInit();

                ScrapTansportAttrGridInit();
                SetDefaults();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                Report1Grid.Destruct();
                Report2Grid.Destruct();
                Report3Grid.Destruct();
                Report4Grid.Destruct();
                Report13Grid.Destruct();
                Report14Grid.Destruct();
                Report15Grid.Destruct();
                Report16Grid.Destruct();
                Report17Grid.Destruct();
                Report18Grid.Destruct();
                Report19Grid.Destruct();
                Report20Grid.Destruct();
                Report21Grid.Destruct();
                Report22Grid.Destruct();
                Report26Grid.Destruct();
                Report27Grid.Destruct();
                Report28Grid.Destruct();
                Report29Grid.Destruct();
                ScrapTansportAttrGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
            };

            OnNavigate = () =>
            {
                //var login = Parameters.CheckGet("login");
                //if (!login.IsNullOrEmpty())
                //{
                //    AccountGridSearch.Text = login;
                //}
            };

            Commander.SetCurrentGroup("main");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "refresh",
                    Enabled = true,
                    Title = "Показать",
                    Description = "Обновить данные",
                    ButtonUse = true,
                    ButtonName = "ShowReportButton",
                    MenuUse = true,
                    Action = () =>
                    {
                        //     Refresh();

                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        if (ReportList.SelectedItem.Key.ToInt() != 0)
                        {
                            result = true;
                        }
                        return result;
                    },

                });
                Commander.Add(new CommandItem()
                {
                    Name = "excel",
                    Enabled = true,
                    Title = "Выгрузить отчет в Excel",
                    Description = "Выгрузить отчет в Excel файл",
                    ButtonUse = true,
                    ButtonName = "ReportButton",
                    //   HotKey = "F1",
                    Action = () =>
                    {
                        ExportToExcel();
                    },
                });
            }

            Commander.Init(this);

        }

        /// <summary>
        // инициализация компонентов формы
        /// </summary>
        public void FormInit()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="ID_ST",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Default="0",
                    Control=IdStList,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange=(FormHelperField f, string v)=>
                    {
                       ReportListUpdateItems();
                    },
                    OnCreate = (FormHelperField f) =>
                    {
                        var list = new Dictionary<string, string>();
                        list.Add("716", "БДМ1");
                        list.Add("1716", "БДМ2");
                        list.Add("2716", "ЛТ");

                        var c=(SelectBox)f.Control;
                        if(c != null)
                        {
                            c.Items=list;
                            c.SelectedItem = list.FirstOrDefault((x) => x.Key == "716");
                        }
                    },
                },

                new FormHelperField()
                {
                    Path="FROM_DATE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ReportDtFrom,
                    Default=DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy"),
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="BEG_SMENA",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Default="0",
                    Control=BegSmenaList,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnCreate = (FormHelperField f) =>
                    {
                        var list = new Dictionary<string, string>();
                        list.Add("1", "Д");
                        list.Add("2", "Н");

                        var c=(SelectBox)f.Control;
                        if(c != null)
                        {
                            c.Items=list;
                         //   c.SelectedItem = list.FirstOrDefault((x) => x.Key == "1");
                        }
                    },
                },
                new FormHelperField()
                {
                    Path="END_SMENA",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Default="0",
                    Control=EndSmenaList,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnCreate = (FormHelperField f) =>
                    {
                        var list = new Dictionary<string, string>();
                        list.Add("1", "Д");
                        list.Add("2", "Н");

                        var c=(SelectBox)f.Control;
                        if(c != null)
                        {
                            c.Items=list;
                        }
                    },
                },
                new FormHelperField()
                {
                    Path="TO_DATE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ReportDtTo,
                    Default=DateTime.Now.ToString("dd.MM.yyyy"),
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SEARCH_TOVAR",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=SearchTovar,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                new FormHelperField()
                {
                    Path="POSTAVSHIC",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Default="0",
                    Control=PostavshicList,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange=(FormHelperField f, string v)=>
                    {
                        Report1Grid.UpdateItems();
                    },
                    QueryLoadItems = new RequestData()
                    {
                        Module = "PaperProduction",
                        Object = "TransportDriver",
                        Action = "ListVendor",
                        AnswerSectionKey="ITEMS",

                        OnComplete = (FormHelperField f,ListDataSet ds) =>
                        {
                            var list=ds.GetItemsList("ID","NAME");
                            var c=(SelectBox)f.Control;
                            if(c != null)
                            {
                                c.Items=list;
                            }
                        },
                    },
                },
            };

            Form.SetFields(fields);
            Form.SetDefaults();
        }

        public void SetDefaults()
        {
            Form.SetDefaults();
            CheckBoxPaperAllSet(false);  // отключаем все чек -боксы
            ShowReportButton.IsEnabled = false;
            AllTimeCheckBox.IsEnabled = false;

            ReportListUpdateItems();

            // свернуть грид атрибутов
            GridAttr.Height = new GridLength(0);
            GridReport.Height = new GridLength(1, GridUnitType.Star);
        }

        /// <summary>
        /// выбор поставщика из списка
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void PostavshicList_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Refresh();
        }

        /// <summary>
        ////заполняем список отчетов в зависимости от выбранной площадки
        /// </summary>
        private void ReportListUpdateItems()
        {
            var list = new Dictionary<string, string>();
            ReportList.Items.Clear();
            list.Add("0", "Выберите отчет");

            // общие отчеты для всех площадок
            list.Add("1", "1.  Кипы по ячейкам  (МС-5,6,8)");
            list.Add("2", "2.  Приход макулатуры  (МС-5,6,8)");
            list.Add("3", "3.  Расход макулатуры  (МС-5,6,8)");
            list.Add("4", "4.  Отчет по качеству (МС-5,6,8)");

            // отчеты только для БДМ1
            if (IdStList.SelectedItem.Key.ToInt() == 716)
            {
                list.Add("20", "20. Приход тех. обрези (БДМ1)");
                list.Add("21", "21. Расход тех. обрези  (БДМ1)");

                // общий для всех
                list.Add("22", "22. Состояние склада на дату");
                list.Add("26", "26. Заказ поставщику на макулатуру");

                list.Add("28", "28. Поступления в ячейку N-1");
            }
            else // отчеты только для БДМ2
            if (IdStList.SelectedItem.Key.ToInt() == 1716)
            {
                list.Add("13", "13. МС-11В. Кипы по ячейкам");
                list.Add("14", "14. МС-11В. Приход макулатуры");
                list.Add("15", "15. МС-11В. Расход макулатуры");
                list.Add("16", "16. МС-11В. Отчет по качеству");
                list.Add("17", "17. Полиэтиленовая смесь. Кипы по ячейкам");
                list.Add("18", "18. Полиэтиленовая смесь. Приход");
                list.Add("19", "19. Полиэтиленовая смесь. Расход");

                // общий для всех
                list.Add("22", "22. Состояние склада на дату");
                list.Add("26", "26. Заказ поставщику на макулатуру");

                list.Add("27", "27. Отчет о возвратах");
                list.Add("29", "29. Перемещение забракованных рулонов");
            }
            else // отчеты только для ЦЛТ
            if (IdStList.SelectedItem.Key.ToInt() == 2716)
            {
                list.Add("13", "13. МС-11В. Кипы по ячейкам");
                list.Add("14", "14. МС-11В. Приход макулатуры");
                list.Add("15", "15. МС-11В. Расход макулатуры");
                list.Add("16", "16. МС-11В. Отчет по качеству");

                // общий для всех
                list.Add("22", "22. Состояние склада на дату");
                list.Add("26", "26. Заказ поставщику на макулатуру");
            }

            ReportList.Items = list;
            ReportList.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");

        }

        public void SetSplash(bool inProgressFlag, string msg = "")
        {

            SplashControl.Visible = inProgressFlag;
            SplashControl.Message = msg;
        }

        /// <summary>
        ///  нажали кнопку "Показать"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowReportButton_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        /// <summary>
        ///  нажали кнопку "в Excel"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExcelButton_Click(object sender, RoutedEventArgs e)
        {
            //       ExportToExcel();
        }

        /// <summary>
        ////выгрузка отчета в Excel
        /// </summary>
        private async void ExportToExcel()
        {
            SetSplash(true, "Идет выгрузка отчета в файл...");
            var items = ReportList.SelectedItem.Key.ToInt();
            var list = Report1Grid.Items;

            if (Form.Validate())
            {
                switch (items)
                {
                    case 1:  // 1. "МС-5Б. Тюки по ячейкам"
                        list = Report1Grid.Items;
                        break;
                    case 2:  // 2. "МС-5Б. Приход макулатуры"
                        list = Report2Grid.Items;
                        break;
                    case 3:  // 3. "МС-5Б. Расход макулатуры"
                        list = Report3Grid.Items;
                        break;
                    case 4:  // 4. "МС-5Б. Отчет по качеству"
                        list = Report4Grid.Items;
                        break;
                    case 13:  // 13. МС-11В. Кипы по ячейкам
                        list = Report13Grid.Items;
                        break;
                    case 14:  // 14. МС-11В. Приход макулатуры
                        list = Report14Grid.Items;
                        break;
                    case 15:  // 15. МС-11В. Расход макулатуры
                        list = Report15Grid.Items;
                        break;
                    case 16:  // 16. МС-11В. Отчет по качеству
                        list = Report16Grid.Items;
                        break;
                    case 17:  // 17. Полиэтиленовая смесь. Тюки по ячейкам
                        list = Report17Grid.Items;
                        break;
                    case 18:  // 18. Полиэтиленовая смесь. Приход
                        list = Report18Grid.Items;
                        break;
                    case 19:  // 19. Полиэтиленовая смесь. Расход
                        list = Report19Grid.Items;
                        break;
                    case 20:  // 20. Приход Т (БДМ1)
                        list = Report20Grid.Items;
                        break;
                    case 21:  // 21. Расход  Т (БДМ1)
                        list = Report21Grid.Items;
                        break;
                    case 22:  // 22. МС-5Б. Склад на дату
                        list = Report22Grid.Items;
                        break;
                    case 26:  // 26. Заказ поставщику на макулатуру
                        list = Report26Grid.Items;
                        break;
                    case 27:  // 27. "Отчет о возвратах"
                        list = Report27Grid.Items;
                        break;
                    case 28:  // 28. "Поступление макулатуры в ячейку N-1"
                        list = Report28Grid.Items;
                        break;
                    case 29:  // 29. "Перемещение забракованных рулонов"
                        list = Report29Grid.Items;
                        break;
                    default:
                        break;
                }

                var listString = JsonConvert.SerializeObject(list);

                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("NUM", ReportList.SelectedItem.Key.ToInt().ToString());
                    p.CheckAdd("DATA_LIST", listString);
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "ProductionPm");
                q.Request.SetParam("Object", "ScrapPaper");
                q.Request.SetParam("Action", "ReportAllToExcel");
                q.Request.SetParams(p);

                q.Request.Timeout = 25000;
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
            SetSplash(false);
        }

        /// <summary>
        /// отключаем/включаем все чек-боксы
        /// </summary>
        private void CheckBoxPaperAllSet(bool stat)
        {
            PaperAllCheckBox.IsEnabled = PaperMC5BCheckBox.IsEnabled = PaperMC6BCheckBox.IsEnabled = PaperMC8BCheckBox.IsEnabled = PaperMC11BCheckBox.IsEnabled = stat;
            PaperAllCheckBox.IsChecked = PaperMC5BCheckBox.IsChecked = PaperMC6BCheckBox.IsChecked = PaperMC8BCheckBox.IsChecked = PaperMC11BCheckBox.IsChecked = false;
        }

        /// <summary>
        /// выбираем отчет (определяем доступность кнопок и чек-боксов)
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void ReportList_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

            HideAllGrid();
            // свернуть грид атрибутов
            GridAttr.Height = new GridLength(0);
            GridReport.Height = new GridLength(1, GridUnitType.Star);

            AllTimeCheckBox.IsEnabled = false;
            BegSmenaList.IsEnabled = false;
            EndSmenaList.IsEnabled = false;
            ReportDtTo.IsEnabled = true;

            ShowReportButton.IsEnabled = false;
            ReportButton.IsEnabled = false;

            ReportIsRun = false;

            var items = ReportList.SelectedItem.Key.ToInt();

            if (items > 0)
            {
                ShowReportButton.IsEnabled = true;
            }
            else
                return;

            switch (items)
            {

                case 1:  // 1. "МС-5Б. Тюки по ячейкам"
                    BegSmenaList.IsEnabled = true;
                    EndSmenaList.IsEnabled = true;
                    CheckBoxPaperAllSet(true);  // включаем все чек-боксы
                    PaperMC11BCheckBox.IsChecked = false;
                    PaperMC11BCheckBox.IsEnabled = false;
                    PaperAllCheckBox.IsChecked = PaperMC5BCheckBox.IsChecked = PaperMC6BCheckBox.IsChecked = PaperMC8BCheckBox.IsChecked = true;
                    Report1Grid.Visibility = Visibility.Visible;

                    break;

                case 2:  // 2. "МС-5Б. Приход макулатуры"
                    BegSmenaList.IsEnabled = true;
                    EndSmenaList.IsEnabled = true;
                    CheckBoxPaperAllSet(true);  // включаем все чек-боксы
                    PaperMC11BCheckBox.IsChecked = false;
                    PaperMC11BCheckBox.IsEnabled = false;
                    PaperAllCheckBox.IsChecked = PaperMC5BCheckBox.IsChecked = PaperMC6BCheckBox.IsChecked = PaperMC8BCheckBox.IsChecked = true;
                    Report2Grid.Visibility = Visibility.Visible;

                    break;

                case 3:  // 3. "МС-5Б. Расход макулатуры"
                    BegSmenaList.IsEnabled = true;
                    EndSmenaList.IsEnabled = true;
                    CheckBoxPaperAllSet(true);  // включаем все чек-боксы
                    PaperMC11BCheckBox.IsChecked = false;
                    PaperMC11BCheckBox.IsEnabled = false;
                    PaperAllCheckBox.IsChecked = PaperMC5BCheckBox.IsChecked = PaperMC6BCheckBox.IsChecked = PaperMC8BCheckBox.IsChecked = true;
                    Report3Grid.Visibility = Visibility.Visible;

                    break;
                case 4:  // 4. "МС-5Б. Отчет по качеству"
                    AllTimeCheckBox.IsEnabled = true;
                    BegSmenaList.IsEnabled = true;
                    EndSmenaList.IsEnabled = true;
                    CheckBoxPaperAllSet(true);  // включаем все чек-боксы
                    PaperMC11BCheckBox.IsChecked = false;
                    PaperMC11BCheckBox.IsEnabled = false;
                    PaperAllCheckBox.IsChecked = PaperMC5BCheckBox.IsChecked = PaperMC6BCheckBox.IsChecked = PaperMC8BCheckBox.IsChecked = true;

                    // развернуть грид атрибутов
                    GridAttr.Height = new GridLength(100);
                    GridReport.Height = new GridLength(2, GridUnitType.Star);

                    Report4Grid.Visibility = Visibility.Visible;

                    break;
                case 13:  // 13. МС-11В. Кипы по ячейкам
                    BegSmenaList.IsEnabled = true;
                    EndSmenaList.IsEnabled = true;
                    CheckBoxPaperAllSet(false);  // выключаем все чек-боксы
                    PaperMC11BCheckBox.IsChecked = true;
                    PaperMC11BCheckBox.IsEnabled = true;
                    Report13Grid.Visibility = Visibility.Visible;

                    break;
                case 14:  // 14. МС-11В. Приход макулатуры
                    BegSmenaList.IsEnabled = true;
                    EndSmenaList.IsEnabled = true;
                    CheckBoxPaperAllSet(false);  // выключаем все чек-боксы
                    PaperMC11BCheckBox.IsChecked = true;
                    PaperMC11BCheckBox.IsEnabled = true;
                    Report14Grid.Visibility = Visibility.Visible;

                    break;
                case 15:  // 15. МС-11В. Расход макулатуры
                    BegSmenaList.IsEnabled = true;
                    EndSmenaList.IsEnabled = true;
                    CheckBoxPaperAllSet(false);  // выключаем все чек-боксы
                    PaperMC11BCheckBox.IsChecked = true;
                    PaperMC11BCheckBox.IsEnabled = true;
                    Report15Grid.Visibility = Visibility.Visible;

                    break;
                case 16:  // 16. МС-11В. Отчет по качеству
                    AllTimeCheckBox.IsEnabled = true;
                    BegSmenaList.IsEnabled = true;
                    EndSmenaList.IsEnabled = true;
                    CheckBoxPaperAllSet(false);  // выключаем все чек-боксы
                    PaperMC11BCheckBox.IsChecked = true;
                    PaperMC11BCheckBox.IsEnabled = true;
                    // развернуть грид атрибутов
                    GridAttr.Height = new GridLength(100);
                    GridReport.Height = new GridLength(2, GridUnitType.Star);
                    Report16Grid.Visibility = Visibility.Visible;

                    break;
                case 17:  // 17. Полиэтиленовая смесь. Тюки по ячейкам
                    BegSmenaList.IsEnabled = true;
                    EndSmenaList.IsEnabled = true;
                    Report17Grid.Visibility = Visibility.Visible;

                    break;
                case 18:  // 18. Полиэтиленовая смесь. Приход
                    Report18Grid.Visibility = Visibility.Visible;

                    break;
                case 19:  // 19. Полиэтиленовая смесь. Расход
                    Report19Grid.Visibility = Visibility.Visible;

                    break;
                case 20:  // 20. Приход Т (БДМ1)
                    Report20Grid.Visibility = Visibility.Visible;

                    break;
                case 21:  // 21. Расход  Т (БДМ1)
                    Report21Grid.Visibility = Visibility.Visible;

                    break;
                case 22:  // 22. МС-5Б. Склад на дату
                    ReportDtTo.IsEnabled = false;
                    BegSmenaList.IsEnabled = true;
                    EndSmenaList.IsEnabled = false;
                    CheckBoxPaperAllSet(true);  // включаем все чек-боксы
                    PaperAllCheckBox.IsChecked = PaperMC5BCheckBox.IsChecked = PaperMC6BCheckBox.IsChecked = PaperMC8BCheckBox.IsChecked = PaperMC11BCheckBox.IsChecked = true;
                    Report22Grid.Visibility = Visibility.Visible;

                    break;
                case 26:  // 26. Заказ поставщику на макулатуру
                          //BegSmenaList.IsEnabled = false;
                          //EndSmenaList.IsEnabled = false;
                    Report26Grid.Visibility = Visibility.Visible;

                    break;

                case 27:  // 27. "Отчет о возвратах"
                    AllTimeCheckBox.IsEnabled = true;
                    BegSmenaList.IsEnabled = true;
                    EndSmenaList.IsEnabled = true;
                    Report27Grid.Visibility = Visibility.Visible;

                    break;

                case 28:  // 28. "Поступление макулатуры в ячейку N-1"
                          //BegSmenaList.IsEnabled = false;
                          //EndSmenaList.IsEnabled = false;
                    Report28Grid.Visibility = Visibility.Visible;

                    break;
                case 29:  // 29. "Перемещение забракованных рулонов"
                          //BegSmenaList.IsEnabled = false;
                          //EndSmenaList.IsEnabled = false;
                    Report29Grid.Visibility = Visibility.Visible;

                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// отметить все флажки с категории макулатуры
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PaperAllCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            PaperAllCheckBox.IsChecked =
            PaperMC5BCheckBox.IsChecked =
            PaperMC6BCheckBox.IsChecked =
            PaperMC8BCheckBox.IsChecked = true;
        }

        /// <summary>
        /// убрать все флажки с категории макулатуры
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PaperAllCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            PaperAllCheckBox.IsChecked =
            PaperMC5BCheckBox.IsChecked =
            PaperMC6BCheckBox.IsChecked =
            PaperMC8BCheckBox.IsChecked =
            PaperMC11BCheckBox.IsChecked = false;
        }


        /// <summary>
        /// формирует строку с кодами (ID2) из отмеченных категорий макулатуры
        /// </summary>
        private string SetFilterStr()
        {
            var str = "";

            if (PaperMC5BCheckBox.IsChecked == true)
            {
                str += "35011, 35012, 35013, 540422, 581848,";
            }

            if (PaperMC6BCheckBox.IsChecked == true)
            {
                str += "335156, 335157,";
            }

            if (PaperMC8BCheckBox.IsChecked == true)
            {
                str += "498524,";
            }

            if ((PaperMC11BCheckBox.IsChecked == true) && (PaperMC11BCheckBox.IsEnabled == true))
            {
                str += "35007, 261785, 261786,";
            }

            str += ")";
            str = str.Replace(",)", "");

            return str;
        }

        /// <summary>
        /// скрываем все гриды с отчетами
        /// </summary>
        private void HideAllGrid()
        {
            Report1Grid.Visibility = Visibility.Hidden;
            Report2Grid.Visibility = Visibility.Hidden;
            Report3Grid.Visibility = Visibility.Hidden;
            Report4Grid.Visibility = Visibility.Hidden;
            Report13Grid.Visibility = Visibility.Hidden;
            Report14Grid.Visibility = Visibility.Hidden;
            Report15Grid.Visibility = Visibility.Hidden;
            Report16Grid.Visibility = Visibility.Hidden;
            Report17Grid.Visibility = Visibility.Hidden;
            Report18Grid.Visibility = Visibility.Hidden;
            Report19Grid.Visibility = Visibility.Hidden;
            Report20Grid.Visibility = Visibility.Hidden;
            Report21Grid.Visibility = Visibility.Hidden;
            Report22Grid.Visibility = Visibility.Hidden;
            Report26Grid.Visibility = Visibility.Hidden;
            Report27Grid.Visibility = Visibility.Hidden;
            Report28Grid.Visibility = Visibility.Hidden;
            Report29Grid.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Возвращает цвет  ячейки для списка 
        /// </summary>
        public static object GetColorRolls(string fieldName, Dictionary<string, string> row)
        {
            var result = DependencyProperty.UnsetValue;
            var color = "";

            if (fieldName == "ID_STATUS_STR")
            {
                // продукция возвращена
                if (row.CheckGet("DT_RETURN").IsNullOrEmpty())
                {
                    color = $"#FBC2C6";
                } 

            }

            if (fieldName == "WEIGHT_FACT") 
            {
                // продукция возвращена
                if (row.CheckGet("AUTO_INPUT").ToInt() > 0)
                {
                    color = $"#FF0000";
                }
            }

            if (!color.IsNullOrEmpty())
            {
                result = color.ToBrush();
            }

            return result;
        }


        /// <summary>
        ////показать отчет
        /// </summary>
        private void Refresh()
        {
            SetSplash(true, "Идет формирование отчета...");

            StrFilter = "";
            var items = ReportList.SelectedItem.Key.ToInt();

            switch (items)
            {
                case 1:  // 1. "МС-5Б. Тюки по ячейкам"
                    StrFilter = SetFilterStr(); // получаем отмеченные категории макулатуры

                    if (StrFilter == ")")
                    {
                        SetSplash(false);
                        return;
                    }
                    break;

                case 2:  // 2. "МС-5Б. Приход макулатуры"
                    StrFilter = SetFilterStr(); // получаем отмеченные категории макулатуры

                    if (StrFilter == ")")
                    {
                        SetSplash(false);
                        return;
                    }
                    break;
                case 3:  // 3. "МС-5Б. Расход макулатуры"
                    StrFilter = SetFilterStr(); // получаем отмеченные категории макулатуры

                    if (StrFilter == ")")
                    {
                        SetSplash(false);
                        return;
                    }
                    break;
                case 4:  // 4. "МС-5Б. Отчет по качеству"
                    StrFilter = SetFilterStr(); // получаем отмеченные категории макулатуры

                    if (StrFilter == ")")
                    {
                        SetSplash(false);
                        return;
                    }

                    break;
                case 13:  // 13. МС-11В. Кипы по ячейкам
                    break;
                case 14:  // 14. МС-11В. Приход макулатуры
                    break;
                case 15:  // 15. МС-11В. Расход макулатуры
                    break;
                case 16:  // 16. МС-11В. Отчет по качеству
                    break;
                case 17:  // 17. Полиэтиленовая смесь. Тюки по ячейкам
                    break;
                case 18:  // 18. Полиэтиленовая смесь. Приход
                    break;
                case 19:  // 19. Полиэтиленовая смесь. Расход
                    break;
                case 20:  // 20. Приход Т (БДМ1)
                    break;
                case 21:  // 21. Расход  Т (БДМ1)
                    break;
                case 22:  // 22. МС-5Б. Склад на дату
                    StrFilter = SetFilterStr(); // получаем отмеченные категории макулатуры

                    if (StrFilter == ")")
                    {
                        SetSplash(false);
                        return;
                    }

                    break;
                case 26:  // 26. Заказ поставщику на макулатуру
                    break;
                case 27:  // 27. "Отчет о возвратах"
                    break;
                case 28:  // 28. "Поступление макулатуры в ячейку N-1"
                    break;
                case 29:  // 29. "Перемещение забракованных рулонов"
                    break;
                default:
                    break;
            }

            if (items > 0)
            {
                ReportIsRun = true;
                ReportGridLoadItems();
            }
        }

        /// <summary>
        /// загрузка данных для отчета 
        /// </summary>
        /// <param name="num"></param>
        public async void ReportGridLoadItems()
        {
            bool resume = true;

            var items = ReportList.SelectedItem.Key.ToInt();

            if (items > 0 && ReportIsRun)
            {
                var dtFrom = ReportDtFrom.Text;
                var dtTo = ReportDtTo.Text;

                if (BegSmenaList.SelectedItem.Key.ToInt() == 1)
                {
                    dtFrom = ReportDtFrom.Text + " 08:00:00";
                }
                else
                if (BegSmenaList.SelectedItem.Key.ToInt() == 2)
                {
                    dtFrom = ReportDtFrom.Text + " 20:00:00";
                }

                if (EndSmenaList.SelectedItem.Key.ToInt() == 1)
                {
                    dtTo = ReportDtTo.Text + " 20:00:00";
                }
                else
                if (EndSmenaList.SelectedItem.Key.ToInt() == 2)
                {
                    dtTo = (ReportDtTo.Text).ToDateTime().AddHours(32).ToString();
                }

                if (AllTimeCheckBox.IsChecked == true && items == 4)
                {
                    items = 41;
                }

                if (AllTimeCheckBox.IsChecked == true && items == 16)
                {
                    items = 161;
                }

                if (resume)
                {
                    var q = new LPackClientQuery();

                    var p = new Dictionary<string, string>();
                    {
                        p.CheckAdd("NUM", items.ToString());
                        p.CheckAdd("ID_ST", IdStList.SelectedItem.Key.ToInt().ToString());
                        p.CheckAdd("STR", StrFilter);
                        p.CheckAdd("DT_FROM", dtFrom);
                        p.CheckAdd("DT_TO", dtTo);

                        if (AllTimeCheckBox.IsChecked == true)
                            p.CheckAdd("ALL", "1");
                        else
                            p.CheckAdd("ALL", "0");
                    }

                    q.Request.SetParam("Module", "ProductionPm");
                    q.Request.SetParam("Object", "ScrapPaper");
                    q.Request.SetParam("Action", "ScrapPaperReport");
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

                            switch (items)
                            {
                                case 1:  // 1. "МС-5Б. Тюки по ячейкам"
                                    Report1Grid.UpdateItems(ds);
                                    break;
                                case 2:  // 2. "МС-5Б. Приход макулатуры"
                                    Report2Grid.UpdateItems(ds);
                                    break;
                                case 3:  // 3. "МС-5Б. Расход макулатуры"
                                    Report3Grid.UpdateItems(ds);
                                    break;
                                case 4:  // 4. "МС-5Б. Отчет по качеству"
                                    Report4Grid.UpdateItems(ds);
                                    break;
                                case 41:  // 4. "МС-5Б. Отчет по качеству"  весь диапазон
                                    Report4Grid.UpdateItems(ds);
                                    break;
                                case 13:  // 13. МС-11В. Кипы по ячейкам
                                    Report13Grid.UpdateItems(ds);
                                    break;
                                case 14:  // 14. МС-11В. Приход макулатуры
                                    Report14Grid.UpdateItems(ds);
                                    break;
                                case 15:  // 15. МС-11В. Расход макулатуры
                                    Report15Grid.UpdateItems(ds);
                                    break;
                                case 16:  // 16. МС-11В. Отчет по качеству
                                    Report16Grid.UpdateItems(ds);
                                    break;
                                case 161:  // 16. МС-11В. Отчет по качеству весь диапазон
                                    Report16Grid.UpdateItems(ds);
                                    break;
                                case 17:  // 17. Полиэтиленовая смесь. Тюки по ячейкам
                                    Report17Grid.UpdateItems(ds);
                                    break;
                                case 18:  // 18. Полиэтиленовая смесь. Приход
                                    Report18Grid.UpdateItems(ds);
                                    break;
                                case 19:  // 19. Полиэтиленовая смесь. Расход
                                    Report19Grid.UpdateItems(ds);
                                    break;
                                case 20:  // 20. Приход Т (БДМ1)
                                    Report20Grid.UpdateItems(ds);
                                    break;
                                case 21:  // 21. Расход  Т (БДМ1)
                                    Report21Grid.UpdateItems(ds);
                                    break;
                                case 22:  // 22. МС-5Б. Склад на дату
                                    Report22Grid.UpdateItems(ds);
                                    break;
                                case 26:  // 26. Заказ поставщику на макулатуру
                                    Report26Grid.UpdateItems(ds);
                                    break;
                                case 27:  // 27. "Отчет о возвратах"
                                    Report27Grid.UpdateItems(ds);
                                    break;
                                case 28:  // 28. "Поступление макулатуры в ячейку N-1"
                                    Report28Grid.UpdateItems(ds);
                                    break;
                                case 29:  // 29. "Перемещение забракованных рулонов"
                                    Report29Grid.UpdateItems(ds);
                                    break;
                                default:
                                    break;
                            }
                            ReportButton.IsEnabled = true;
                        }
                        else
                            ReportButton.IsEnabled = false;
                    }
                    SetSplash(false);
                    ReportIsRun = false;
                }

            }
        }

        /// <summary>
        /// загрузка данных для грида с описанием свойств выбранной машины 
        /// </summary>
        /// <param name="num"></param>
        public async void ScrapTansportAttrGridLoadItems()
        {
            bool resume = true;

            var idScrap = SelectedItem.CheckGet("ID_SCRAP").ToInt();

            if (idScrap == 0)
                return;

            if (resume)
            {
                var q = new LPackClientQuery();

                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID_SCRAP", idScrap.ToString());
                }

                q.Request.SetParam("Module", "ProductionPm");
                q.Request.SetParam("Object", "ScrapPaper");
                q.Request.SetParam("Action", "ScrapTansportAttr");
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
                        ScrapTansportAttrGrid.UpdateItems(ds);
                        ScrapTansportAttrGrid.SelectRowFirst();
                    }
                }
            }
        }


        #region Описание гридов с отчетами

        /// <summary>
        /// грид со свойствами машины
        /// </summary>
        private void ScrapTansportAttrGridInit()
        {
            //инициализация грида
            var columns = new List<DataGridHelperColumn>
            {
                 new DataGridHelperColumn
                 {
                     Header="№",
                     Path="_ROWNUMBER",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=3,
                     Visible=false,
                 },
                 new DataGridHelperColumn
                 {
                     Header="ID_SCRAP",
                     Path="ID_SCRAP",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                     Visible=false,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Обвязка",
                     Path="TYPE_TYING",
                     ColumnType=ColumnTypeRef.String,
                     Width2=12,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Часть от полной машины",
                     Path="PART",
                     ColumnType=ColumnTypeRef.String,
                     Width2=10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Вес кип",
                     Path="WEIGHT_BALE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=12,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Средняя влажность",
                     Path="HUMIDITY_PERCENT",
                     ColumnType=ColumnTypeRef.String,
                     Width2=18,
                 },

                 new DataGridHelperColumn
                 {
                     Header="Волокно, %",
                     Path="FIBER_PCT",
                     ColumnType=ColumnTypeRef.String,
                     Width2=18,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Размер кип",
                     Path="SIZE_BALE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=20,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Качество кип",
                     Path="QUALITY_BALE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=20,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Влажность",
                     Path="HUMIDITY_NOTE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=20,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Загрязнение",
                     Path="CONTAMINATION_BALE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=60,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Тип ламинации",
                     Path="TYPE_OF_LAMINATION",
                     ColumnType=ColumnTypeRef.String,
                     Width2=20,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Цвет волокна",
                     Path="FIBER_COLOR",
                     ColumnType=ColumnTypeRef.String,
                     Width2=20,
                 },

            };

            ScrapTansportAttrGrid.SetColumns(columns);
            ScrapTansportAttrGrid.SetPrimaryKey("_ROWNUMBER");
            //данные грида
            ScrapTansportAttrGrid.OnLoadItems = ScrapTansportAttrGridLoadItems;
            ScrapTansportAttrGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            ScrapTansportAttrGrid.AutoUpdateInterval = 0;
            ScrapTansportAttrGrid.EnableSortingGrid = false;
            ScrapTansportAttrGrid.Init();
        }

        /// <summary>
        /// 1. "МС-5Б. Тюки по ячейкам"
        /// </summary>
        private void Report1GridInit()
        {
            //инициализация грида
            var columns = new List<DataGridHelperColumn>
            {
                 new DataGridHelperColumn
                 {
                     Header="№",
                     Path="RN",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=3,
                     Visible=false,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Ряд",
                     Path="SKLAD",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Ячейка",
                     Path="NUM_PLACE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Дата",
                     Path="DATA",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy",
                     Width2 = 8,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Номер акта",
                     Path="NUM_AKT_STR",
                     ColumnType=ColumnTypeRef.String,
                     Width2=10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Поставщик",
                     Path="NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=24,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Категория",
                     Path="CATEGORY",
                     ColumnType=ColumnTypeRef.String,
                     Width2=18,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Вес по док-там",
                     Path = "WEIGHT_DOK",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Кип по док-там",
                     Path = "QUANTITY_BAL_DOC",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Вес на складе",
                     Path = "WEIGHT_SKLAD",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Кип на складе",
                     Path = "KOL",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Средний",
                     Path = "AVERAGE_WEIGHT",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Первая кипа",
                     Path="BAL_FIRST",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Последняя кипа",
                     Path="BAL_EOF",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Время разгрузки",
                     Path = "DT_UNLOADING",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header="МЦК",
                     Path="WASTEPAPER_CELLULOSE_QUALITY",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Сухая",
                     Path="WASTEPAPER_DRY",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Источник",
                     Path="WASTEPAPER_SOURCE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Стандартное примечание",
                     Path="STANDARD_NOTE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=16,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Примечание",
                     Path="NOTE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=50,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "days",
                     Path = "DAYS",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =2,
                     Visible = false,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "id_post",
                     Path = "ID_POST",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =2,
                     Visible = false,
                 },
            };

            // раскраска всей строки
            Report1Grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                    {
                        StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            var days = row.CheckGet("DAYS").ToInt();

                            if (days > 31) // прошло более 31 дня с момента разгрузки
                            {
                                color = "#FBC2C6"; // cветло-розовый  Background := RGB(251, 194, 198);
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },

            };

            Report1Grid.SetColumns(columns);
            Report1Grid.SetPrimaryKey("RN");
            Report1Grid.SetSorting("SKLAD", ListSortDirection.Ascending);

            //  Report1Grid.SetSorting("SKLAD", ListSortDirection.Ascending);
            Report1Grid.SearchText = SearchTovar;
            //данные грида
            Report1Grid.OnLoadItems = ReportGridLoadItems;
            Report1Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Report1Grid.AutoUpdateInterval = 0;
            Report1Grid.EnableSortingGrid = false;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Report1Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    //  SelectedItem = selectedItem;
                }
            };

            // двойной клик по строке
            // Report1Grid.OnDblClick = EditShow;
            // фильтрация по поставщику

            Report1Grid.OnFilterItems = () =>
            {
                var v = Form.GetValues();
                var postID = v.CheckGet("POSTAVSHIC").ToInt();

                if (Report1Grid.Items.Count > 0)
                {
                    if (postID != 0)
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in Report1Grid.Items)
                        {
                            if (row.CheckGet("ID_POST").ToInt() == postID)
                            {
                                items.Add(row);
                            }
                            Report1Grid.Items = items;
                        }
                    }
                }
            };

            Report1Grid.Commands = Commander;
            Report1Grid.Init();
        }

        /// <summary>
        /// 2. "МС-5Б. Приход макулатуры"
        /// </summary>
        private void Report2GridInit()
        {
            //инициализация грида
            var columns = new List<DataGridHelperColumn>
            {
                 new DataGridHelperColumn
                 {
                     Header="#",
                     Path="_ROWNUMBER",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=3,
                     Visible=false,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Дата регистрации",
                     Path="CREATED_DTTM",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Дата полной",
                     Path="DT_BRUTTO",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Дата пустой",
                     Path="DT_NETTO",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "В очереди",
                     Path = "TIME_CNT",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Бригада",
                     Path="BRIGADE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Машина на заводе",
                     Path = "CAR_CNT",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Номер акта",
                     Path="NUM_AKT_STR",
                     ColumnType=ColumnTypeRef.String,
                     Width2=10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Машина",
                     Path="CAR_NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Поставщик",
                     Path="POST_NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=24,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Категория",
                     Path="CATEGORY",
                     ColumnType=ColumnTypeRef.String,
                     Width2=18,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Влажность %",
                     Path = "HUMIDITY",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Факт. влажность",
                     Path = "ACTUAL_HUMIDITY",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Загрязнение %",
                     Path = "CONTAMINATION",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Вес по док-там",
                     Path = "WEIGHT_DOK",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Вес фактический",
                     Path = "WEIGHT_NETTO",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Вес прихода",
                     Path = "WEIGHT_FACT",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                     Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => GetColorRolls("WEIGHT_FACT", row)
                            },
                        },
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Вес на складе",
                     Path = "KOL",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Кип по док-там",
                     Path = "QUANTITY_BAL_DOC",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Кип принято",
                     Path = "BALE_PRIH",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Кип на складе",
                     Path = "BALE_CNT",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Ячейка",
                     Path="NUM_PLACE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Средний",
                     Path = "AVERAGE_WEIGHT",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Первая кипа",
                     Path="BAL_FIRST",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Последняя кипа",
                     Path="BAL_EOF",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Время разгрузки",
                     Path = "DT_UNLOADING",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Водитель погрузчика",
                     Path="STAFF_NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=16,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Входной контроль",
                     Path="INPUT_CONTROL_FLAG",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Терминал",
                     Path="NUM_TERMINAL",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="МЦК",
                     Path="WASTEPAPER_CELLULOSE_QUALITY",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Сухая",
                     Path="WASTEPAPER_DRY",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Стандартное примечание",
                     Path="STANDARD_NOTE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=16,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Примечание",
                     Path="NOTE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=50,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Предполагаемое время разгрузки",
                     Path="DT_UNLOADING_TIME",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Разница (мин)",
                     Path = "TIME_DIFFERENCE",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =2,

                 },
                 new DataGridHelperColumn
                 {
                     Header = "days",
                     Path = "DAYS",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =2,
                     Visible = false,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "id_post",
                     Path = "ID_POST",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =2,
                     Visible = false,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "auto_input",
                     Path = "AUTO_INPUT",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =2,
                     Visible = false,
                 },                 
            };

            // раскраска всей строки
            Report2Grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                    {
                        StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            var days = row.CheckGet("DAYS").ToInt();

                            if (days > 31) // прошло более 31 дня с момента разгрузки
                            {
                                color = "#FBC2C6"; // cветло-розовый  Background := RGB(251, 194, 198);
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },

            };

            Report2Grid.SetColumns(columns);
            Report2Grid.SetPrimaryKey("_ROWNUMBER");
            //  Report2Grid.SetSorting("SKLAD", ListSortDirection.Ascending);

            Report2Grid.SearchText = SearchTovar;
            //данные грида
            Report2Grid.OnLoadItems = ReportGridLoadItems;
            Report2Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Report2Grid.AutoUpdateInterval = 0;
            Report2Grid.EnableSortingGrid = false;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Report2Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    //     SelectedItem = selectedItem;
                }
            };

            // двойной клик по строке
            // Report1Grid.OnDblClick = EditShow;
            // фильтрация по поставщику

            Report2Grid.OnFilterItems = () =>
            {
                var v = Form.GetValues();
                var postID = v.CheckGet("POSTAVSHIC").ToInt();

                if (Report2Grid.Items.Count > 0)
                {
                    if (postID != 0)
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in Report2Grid.Items)
                        {
                            if (row.CheckGet("ID_POST").ToInt() == postID)
                            {
                                items.Add(row);
                            }
                            Report2Grid.Items = items;
                        }
                    }
                }
            };

            Report2Grid.Commands = Commander;
            Report2Grid.Init();
        }

        /// <summary>
        /// 3. "МС-5Б. расход макулатуры"
        /// </summary>
        private void Report3GridInit()
        {
            //инициализация грида
            var columns = new List<DataGridHelperColumn>
            {
                 new DataGridHelperColumn
                 {
                     Header="#",
                     Path="_ROWNUMBER",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=3,
                     Visible=false,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Дата полной машины",
                     Path="DT_FULL",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Контролер прихода",
                     Path="CUSTOMER",
                     ColumnType=ColumnTypeRef.String,
                     Width2=16,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Дата списания",
                     Path="DATA",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy",
                     Width2 = 10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Бригада",
                     Path="BRIGADE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Номер акта",
                     Path="NUM_AKT_STR",
                     ColumnType=ColumnTypeRef.String,
                     Width2=10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Машина",
                     Path="SCRAP_NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Поставщик",
                     Path="POST_NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=24,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Категория",
                     Path="CATEGORY",
                     ColumnType=ColumnTypeRef.String,
                     Width2=18,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Влажность %",
                     Path = "HUMIDITY",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Факт. влажность",
                     Path = "ACTUAL_HUMIDITY",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Загрязнение %",
                     Path = "CONTAMINATION",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Вес прихода",
                     Path = "WEIGHT_FACT",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                     Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                     {
                        {
                            StylerTypeRef.BackgroundColor,
                            row => GetColorRolls("WEIGHT_FACT", row)
                        },
                     },
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Вес на складе",
                     Path = "NOW_KG",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Вес списано",
                     Path = "SUM_KOL",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Кип принято",
                     Path = "PRIH_QTY",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Кип на складе",
                     Path = "NOW_QTY",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Кип списано",
                     Path = "QTY",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Ряд",
                     Path="SKLAD",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                new DataGridHelperColumn
                 {
                     Header="Ячейка",
                     Path="NUM_PLACE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Входной контроль",
                     Path="INPUT_CONTROL_FLAG",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Первая кипа",
                     Path="MIN_TM",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Последняя кипа",
                     Path="MAX_TM",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Время списания",
                     Path = "DT_UNLOADING",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header="МЦК",
                     Path="WASTEPAPER_CELLULOSE_QUALITY",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Сухая",
                     Path="WASTEPAPER_DRY",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="NSTHET",
                     Path="NSTHET",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Стандартное примечание",
                     Path="STANDARD_NOTE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=16,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Примечание",
                     Path="NOTE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=50,
                 },
                 new DataGridHelperColumn
                 {
                     Header="AUTO_INPUT",
                     Path="AUTO_INPUT",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=10,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "days",
                     Path = "DAYS",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =2,
                     Visible = false,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "id_post",
                     Path = "ID_POST",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =2,
                     Visible = false,
                 },
            };

            // раскраска всей строки
            Report3Grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                    {
                        StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            var days = row.CheckGet("DAYS").ToInt();

                            if (days > 31) // прошло более 31 дня с момента разгрузки
                            {
                                color = "#FBC2C6"; // cветло-розовый  Background := RGB(251, 194, 198);
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },

            };

            Report3Grid.SetColumns(columns);
            Report3Grid.SetPrimaryKey("_ROWNUMBER");
            //  Report2Grid.SetSorting("SKLAD", ListSortDirection.Ascending);

            Report3Grid.SearchText = SearchTovar;
            //данные грида
            Report3Grid.OnLoadItems = ReportGridLoadItems;
            Report3Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Report3Grid.AutoUpdateInterval = 0;
            Report3Grid.EnableSortingGrid = false;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Report3Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    //   SelectedItem = selectedItem;
                }
            };

            // двойной клик по строке
            // Report1Grid.OnDblClick = EditShow;
            // фильтрация по поставщику

            Report3Grid.OnFilterItems = () =>
            {
                var v = Form.GetValues();
                var postID = v.CheckGet("POSTAVSHIC").ToInt();

                if (Report3Grid.Items.Count > 0)
                {
                    if (postID != 0)
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in Report3Grid.Items)
                        {
                            if (row.CheckGet("ID_POST").ToInt() == postID)
                            {
                                items.Add(row);
                            }
                            Report3Grid.Items = items;
                        }
                    }
                }
            };

            Report3Grid.Commands = Commander;
            Report3Grid.Init();
        }

        /// <summary>
        /// 4. "МС-5Б. Отчет по качеству"
        /// </summary>
        private void Report4GridInit()
        {
            //инициализация грида
            var columns = new List<DataGridHelperColumn>
            {
                 new DataGridHelperColumn
                 {
                     Header="#",
                     Path="_ROWNUMBER",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=3,
                     Visible=false,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Время брутто",
                     Path="DT_FULL",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Бригада",
                     Path="BRIGADE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Контролер",
                     Path="CUSTOMER",
                     ColumnType=ColumnTypeRef.String,
                     Width2=16,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Номер акта",
                     Path="NUM_AKT_STR",
                     ColumnType=ColumnTypeRef.String,
                     Width2=10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Машина",
                     Path="SCRAP_NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Поставщик",
                     Path="POST_NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=24,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Категория",
                     Path="CATEGORY",
                     ColumnType=ColumnTypeRef.String,
                     Width2=18,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Влажность %",
                     Path = "HUMIDITY",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Факт. влажность",
                     Path = "ACTUAL_HUMIDITY",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Загрязнение %",
                     Path = "CONTAMINATION",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Вес прихода",
                     Path = "WEIGHT_FACT",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                     Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => GetColorRolls("WEIGHT_FACT", row)
                            },
                        },

                 },
                 new DataGridHelperColumn
                 {
                     Header = "Вес на складе",
                     Path = "NOW_KG",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Кип на складе",
                     Path = "PRIH_QTY",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Вес 1 кипы",
                     Path = "AVERAGE_WEIGHT",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Ряд",
                     Path="SKLAD",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Ячейка",
                     Path="NUM_PLACE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Загрузка",
                     Path="TYPE_LOADING",
                     ColumnType=ColumnTypeRef.String,
                     Width2=10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Обвязка",
                     Path="TYING",
                     ColumnType=ColumnTypeRef.String,
                     Width2=14,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Стандартное примечание",
                     Path="STANDARD_NOTE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=16,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Качество",
                     Path="QUALITY_BALE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=20,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Загрязнение",
                     Path="CONTAMINATION_BALE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=20,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Примечание",
                     Path="NOTE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=20,
                 },
                 new DataGridHelperColumn
                 {
                     Header="МЦК",
                     Path="WASTEPAPER_CELLULOSE_QUALITY",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Сухая",
                     Path="WASTEPAPER_DRY",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Источник",
                     Path="WASTEPAPER_SOURCE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=12,
                 },
                 new DataGridHelperColumn
                 {
                     Header="AUTO_INPUT",
                     Path="AUTO_INPUT",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=10,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "days",
                     Path = "DAYS",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =2,
                     Visible = false,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "id_post",
                     Path = "ID_POST",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =2,
                     Visible = false,
                 },
                 new DataGridHelperColumn
                 {
                     Header="ID_SCRAP",
                     Path="ID_SCRAP",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=3,
                     Visible=false,
                 },
            };


            Report4Grid.SetColumns(columns);
            Report4Grid.SetPrimaryKey("_ROWNUMBER");
            Report4Grid.SearchText = SearchTovar;
            //данные грида
            Report4Grid.OnLoadItems = ReportGridLoadItems;
            Report4Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Report4Grid.AutoUpdateInterval = 0;
            Report4Grid.EnableSortingGrid = false;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Report4Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    SelectedItem = selectedItem;
                    ScrapTansportAttrGridLoadItems();
                }
            };

            // двойной клик по строке
            // Report1Grid.OnDblClick = EditShow;
            // фильтрация по поставщику

            Report4Grid.OnFilterItems = () =>
            {
                var v = Form.GetValues();
                var postID = v.CheckGet("POSTAVSHIC").ToInt();

                if (Report4Grid.Items.Count > 0)
                {
                    if (postID != 0)
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in Report4Grid.Items)
                        {
                            if (row.CheckGet("ID_POST").ToInt() == postID)
                            {
                                items.Add(row);
                            }
                            Report4Grid.Items = items;
                        }
                    }
                }
            };

            Report4Grid.Commands = Commander;
            Report4Grid.Init();
        }

        /// <summary>
        /// 13. "МС-5Б. Тюки по ячейкам"
        /// </summary>
        private void Report13GridInit()
        {
            //инициализация грида
            var columns = new List<DataGridHelperColumn>
            {
                 new DataGridHelperColumn
                 {
                     Header="№",
                     Path="_ROWNUMBER",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=3,
                     Visible=false,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Ряд",
                     Path="SKLAD",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Ячейка",
                     Path="NUM_PLACE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Дата",
                     Path="DATA",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy",
                     Width2 = 8,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Номер акта",
                     Path="NUM_AKT_STR",
                     ColumnType=ColumnTypeRef.String,
                     Width2=12,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Поставщик",
                     Path="NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=24,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Категория",
                     Path="CATEGORY",
                     ColumnType=ColumnTypeRef.String,
                     Width2=18,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Шредирование",
                     Path="WASTEPAPER_SHREDDING",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Рулоны",
                     Path="ROLLS_FLAG",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="После потребителя",
                     Path="BEFORE_CONSUMER_FLAG",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Вес по док-там",
                     Path = "WEIGHT_DOK",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Кип по док-там",
                     Path = "QUANTITY_BAL_DOC",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Вес на складе",
                     Path = "WEIGHT_SKLAD",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Кип на складе",
                     Path = "KOL",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Средний",
                     Path = "AVERAGE_WEIGHT",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Первая кипа",
                     Path="BAL_FIRST",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Последняя кипа",
                     Path="BAL_EOF",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Время разгрузки",
                     Path = "DT_UNLOADING",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Стандартное примечание",
                     Path="STANDARD_NOTE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=16,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Примечание",
                     Path="NOTE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=50,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "days",
                     Path = "DAYS",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =2,
                     Visible = false,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "id_post",
                     Path = "ID_POST",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =2,
                     Visible = false,
                 },
            };

            // раскраска всей строки
            Report13Grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                    {
                        StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            var days = row.CheckGet("DAYS").ToInt();

                            if (days > 31) // прошло более 31 дня с момента разгрузки
                            {
                                color = "#FBC2C6"; // cветло-розовый  Background := RGB(251, 194, 198);
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },

            };

            Report13Grid.SetColumns(columns);
            Report13Grid.SetPrimaryKey("_ROWNUMBER");
            Report13Grid.SetSorting("SKLAD", ListSortDirection.Ascending);
            Report13Grid.SearchText = SearchTovar;
            //данные грида
            Report13Grid.OnLoadItems = ReportGridLoadItems;
            Report13Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Report13Grid.AutoUpdateInterval = 0;
            Report13Grid.EnableSortingGrid = false;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Report13Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    //  SelectedItem = selectedItem;
                }
            };

            // двойной клик по строке
            // Report1Grid.OnDblClick = EditShow;
            // фильтрация по поставщику

            Report13Grid.OnFilterItems = () =>
            {
                var v = Form.GetValues();
                var postID = v.CheckGet("POSTAVSHIC").ToInt();

                if (Report13Grid.Items.Count > 0)
                {
                    if (postID != 0)
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in Report13Grid.Items)
                        {
                            if (row.CheckGet("ID_POST").ToInt() == postID)
                            {
                                items.Add(row);
                            }
                            Report13Grid.Items = items;
                        }
                    }
                }
            };

            Report13Grid.Commands = Commander;
            Report13Grid.Init();
        }

        /// <summary>
        /// 14. "МС-11В. Приход макулатуры"
        /// </summary>
        private void Report14GridInit()
        {
            //инициализация грида
            var columns = new List<DataGridHelperColumn>
            {
                 new DataGridHelperColumn
                 {
                     Header="#",
                     Path="_ROWNUMBER",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=3,
                     Visible=false,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Дата регистрации",
                     Path="CREATED_DTTM",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Дата полной",
                     Path="DT_BRUTTO",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Дата пустой",
                     Path="DT_NETTO",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "В очереди",
                     Path = "TIME_CNT",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Бригада",
                     Path="BRIGADE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Машина на заводе",
                     Path = "CAR_CNT",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Номер акта",
                     Path="NUM_AKT_STR",
                     ColumnType=ColumnTypeRef.String,
                     Width2=10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Машина",
                     Path="CAR_NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Поставщик",
                     Path="POST_NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=24,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Категория",
                     Path="CATEGORY",
                     ColumnType=ColumnTypeRef.String,
                     Width2=18,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Шредирование",
                     Path="WASTEPAPER_SHREDDING",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Рулоны",
                     Path="ROLLS_FLAG",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="После потребителя",
                     Path="BEFORE_CONSUMER_FLAG",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Влажность %",
                     Path = "HUMIDITY",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Факт. влажность",
                     Path = "ACTUAL_HUMIDITY",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Загрязнение %",
                     Path = "CONTAMINATION",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Вес по док-там",
                     Path = "WEIGHT_DOK",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Вес фактический",
                     Path = "WEIGHT_NETTO",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Вес прихода",
                     Path = "WEIGHT_FACT",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                     Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => GetColorRolls("WEIGHT_FACT", row)
                            },
                        },
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Вес на складе",
                     Path = "KOL",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Кип по док-там",
                     Path = "QUANTITY_BAL_DOC",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Кип принято",
                     Path = "BALE_PRIH",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Кип на складе",
                     Path = "BALE_CNT",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Ячейка",
                     Path="NUM_PLACE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Средний",
                     Path = "AVERAGE_WEIGHT",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Первая кипа",
                     Path="BAL_FIRST",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Последняя кипа",
                     Path="BAL_EOF",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Время разгрузки",
                     Path = "DT_UNLOADING",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Входной контроль",
                     Path="INPUT_CONTROL_FLAG",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Терминал",
                     Path="NUM_TERMINAL",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Водитель погрузчика",
                     Path="STAFF_NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=16,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Стандартное примечание",
                     Path="STANDARD_NOTE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=16,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Примечание",
                     Path="NOTE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=50,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Предполагаемое время разгрузки",
                     Path="DT_UNLOADING_TIME",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Разница (мин)",
                     Path = "TIME_DIFFERENCE",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =2,

                 },
                 new DataGridHelperColumn
                 {
                     Header="AUTO_INPUT",
                     Path="AUTO_INPUT",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=10,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "days",
                     Path = "DAYS",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =2,
                     Visible = false,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "id_post",
                     Path = "ID_POST",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =2,
                     Visible = false,
                 },
    
            };

            // раскраска всей строки
            Report14Grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                    {
                        StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            var days = row.CheckGet("DAYS").ToInt();

                            if (days > 31) // прошло более 31 дня с момента разгрузки
                            {
                                color = "#FBC2C6"; // cветло-розовый  Background := RGB(251, 194, 198);
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },

            };

            Report14Grid.SetColumns(columns);
            Report14Grid.SetPrimaryKey("_ROWNUMBER");
            //  Report2Grid.SetSorting("SKLAD", ListSortDirection.Ascending);

            Report14Grid.SearchText = SearchTovar;
            //данные грида
            Report14Grid.OnLoadItems = ReportGridLoadItems;
            Report14Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Report14Grid.AutoUpdateInterval = 0;
            Report14Grid.EnableSortingGrid = false;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Report14Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    //     SelectedItem = selectedItem;
                }
            };

            // двойной клик по строке
            // Report1Grid.OnDblClick = EditShow;
            // фильтрация по поставщику

            Report14Grid.OnFilterItems = () =>
            {
                var v = Form.GetValues();
                var postID = v.CheckGet("POSTAVSHIC").ToInt();

                if (Report14Grid.Items.Count > 0)
                {
                    if (postID != 0)
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in Report14Grid.Items)
                        {
                            if (row.CheckGet("ID_POST").ToInt() == postID)
                            {
                                items.Add(row);
                            }
                            Report14Grid.Items = items;
                        }
                    }
                }
            };

            Report14Grid.Commands = Commander;
            Report14Grid.Init();
        }

        /// <summary>
        /// 15. "МС-11В. Расход макулатуры"
        /// </summary>
        private void Report15GridInit()
        {
            //инициализация грида
            var columns = new List<DataGridHelperColumn>
            {
                 new DataGridHelperColumn
                 {
                     Header="#",
                     Path="_ROWNUMBER",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=3,
                     Visible=false,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Дата полной машины",
                     Path="DT_FULL",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Контролер прихода",
                     Path="CUSTOMER",
                     ColumnType=ColumnTypeRef.String,
                     Width2=16,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Дата списания",
                     Path="DATA",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy",
                     Width2 = 10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Бригада",
                     Path="BRIGADE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Номер акта",
                     Path="NUM_AKT_STR",
                     ColumnType=ColumnTypeRef.String,
                     Width2=12,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Машина",
                     Path="SCRAP_NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Поставщик",
                     Path="POST_NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=24,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Категория",
                     Path="CATEGORY",
                     ColumnType=ColumnTypeRef.String,
                     Width2=18,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Шредирование",
                     Path="WASTEPAPER_SHREDDING",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Рулоны",
                     Path="ROLLS_FLAG",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="После потребителя",
                     Path="BEFORE_CONSUMER_FLAG",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Влажность %",
                     Path = "HUMIDITY",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Факт. влажность",
                     Path = "ACTUAL_HUMIDITY",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Загрязнение %",
                     Path = "CONTAMINATION",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "ВЕС на складе",
                     Path = "WEIGHT_FACT",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Вес списано",
                     Path = "SUM_KOL",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },

                 new DataGridHelperColumn
                 {
                     Header = "ВЕС остаток",
                     Path = "NOW_KG",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Кип принято",
                     Path = "PRIH_QTY",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Кип списано",
                     Path = "QTY",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Кип на складе",
                     Path = "NOW_QTY",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Ряд",
                     Path="SKLAD",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                new DataGridHelperColumn
                 {
                     Header="Ячейка",
                     Path="NUM_PLACE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Входной контроль",
                     Path="INPUT_CONTROL_FLAG",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Первая кипа",
                     Path="MIN_TM",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Последняя кипа",
                     Path="MAX_TM",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Время списания",
                     Path = "DT_UNLOADING",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header="ИД расхода",
                     Path="NSTHET",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=12,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Стандартное примечание",
                     Path="STANDARD_NOTE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=16,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Примечание",
                     Path="NOTE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=50,
                 },
                 new DataGridHelperColumn
                 {
                     Header="AUTO_INPUT",
                     Path="AUTO_INPUT",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="NNAKL",
                     Path="NNAKL",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=10,
                     Visible=false,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "days",
                     Path = "DAYS",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =2,
                     Visible = false,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "id_post",
                     Path = "ID_POST",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =2,
                     Visible = false,
                 },
            };

            // раскраска всей строки
            Report15Grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                    {
                        StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            var days = row.CheckGet("DAYS").ToInt();

                            if (days > 31) // прошло более 31 дня с момента разгрузки
                            {
                                color = "#FBC2C6"; // cветло-розовый  Background := RGB(251, 194, 198);
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },

            };

            Report15Grid.SetColumns(columns);
            Report15Grid.SetPrimaryKey("_ROWNUMBER");
            //  Report2Grid.SetSorting("SKLAD", ListSortDirection.Ascending);

            Report15Grid.SearchText = SearchTovar;
            //данные грида
            Report15Grid.OnLoadItems = ReportGridLoadItems;
            Report15Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Report15Grid.AutoUpdateInterval = 0;
            Report15Grid.EnableSortingGrid = false;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Report15Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    //   SelectedItem = selectedItem;
                }
            };

            // двойной клик по строке
            // Report1Grid.OnDblClick = EditShow;
            // фильтрация по поставщику

            Report15Grid.OnFilterItems = () =>
            {
                var v = Form.GetValues();
                var postID = v.CheckGet("POSTAVSHIC").ToInt();

                if (Report15Grid.Items.Count > 0)
                {
                    if (postID != 0)
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in Report15Grid.Items)
                        {
                            if (row.CheckGet("ID_POST").ToInt() == postID)
                            {
                                items.Add(row);
                            }
                            Report15Grid.Items = items;
                        }
                    }
                }
            };

            Report15Grid.Commands = Commander;
            Report15Grid.Init();
        }

        /// <summary>
        /// 16. "МС-11В. Отчет по качеству"
        /// </summary>
        private void Report16GridInit()
        {
            //инициализация грида
            var columns = new List<DataGridHelperColumn>
            {
                 new DataGridHelperColumn
                 {
                     Header="#",
                     Path="_ROWNUMBER",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=3,
                     Visible=false,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Акт",
                     Path="TRANSPORT_FILE_COUNT",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Время брутто",
                     Path="DT_FULL",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Бригада",
                     Path="BRIGADE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Контролер",
                     Path="CUSTOMER",
                     ColumnType=ColumnTypeRef.String,
                     Width2=16,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Номер акта",
                     Path="NUM_AKT_STR",
                     ColumnType=ColumnTypeRef.String,
                     Width2=10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Машина",
                     Path="SCRAP_NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Поставщик",
                     Path="POST_NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=24,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Категория",
                     Path="CATEGORY",
                     ColumnType=ColumnTypeRef.String,
                     Width2=18,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Шредирование",
                     Path="WASTEPAPER_SHREDDING",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Рулоны",
                     Path="ROLLS_FLAG",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="После потребителя",
                     Path="BEFORE_CONSUMER_FLAG",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Влажность %",
                     Path = "HUMIDITY",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Факт. влажность",
                     Path = "ACTUAL_HUMIDITY",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Загрязнение %",
                     Path = "CONTAMINATION",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Среднее волокно,%",
                     Path = "FIBER_PCT_AVG",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Вес прихода",
                     Path = "WEIGHT_FACT",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                     Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => GetColorRolls("WEIGHT_FACT", row)
                            },
                        },
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Вес на складе",
                     Path = "NOW_KG",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Кип на складе",
                     Path = "PRIH_QTY",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Вес 1 кипы",
                     Path = "AVERAGE_WEIGHT",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Ряд",
                     Path="SKLAD",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Ячейка",
                     Path="NUM_PLACE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Загрузка",
                     Path="TYPE_LOADING",
                     ColumnType=ColumnTypeRef.String,
                     Width2=10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Обвязка",
                     Path="TYING",
                     ColumnType=ColumnTypeRef.String,
                     Width2=14,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Качество",
                     Path="QUALITY_BALE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=20,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Стандартное примечание",
                     Path="STANDARD_NOTE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=16,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Загрязнение",
                     Path="CONTAMINATION_BALE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=20,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Тип лиминации",
                     Path="TYPE_OF_LAMINATION",
                     ColumnType=ColumnTypeRef.String,
                     Width2=16,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Цвет волокна",
                     Path="FIBER_COLOR",
                     ColumnType=ColumnTypeRef.String,
                     Width2=16,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Примечание",
                     Path="NOTE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=20,
                 },
                 new DataGridHelperColumn
                 {
                     Header="AUTO_INPUT",
                     Path="AUTO_INPUT",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=10,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "days",
                     Path = "DAYS",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =2,
                     Visible = false,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "id_post",
                     Path = "ID_POST",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =4,
                     Visible = true,
                 },
                 new DataGridHelperColumn
                 {
                     Header="ID_SCRAP",
                     Path="ID_SCRAP",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=3,
                     Visible=false,
                 },
                 
            };

            Report16Grid.SetColumns(columns);
            Report16Grid.SetPrimaryKey("_ROWNUMBER");
            Report16Grid.SearchText = SearchTovar;
            //данные грида
            Report16Grid.OnLoadItems = ReportGridLoadItems;
            Report16Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Report16Grid.AutoUpdateInterval = 0;
            Report16Grid.EnableSortingGrid = false;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Report16Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    SelectedItem = selectedItem;
                    ScrapTansportAttrGridLoadItems();
                }
            };

            // двойной клик по строке
            // Report1Grid.OnDblClick = EditShow;
            // фильтрация по поставщику

            Report16Grid.OnFilterItems = () =>
            {
                var v = Form.GetValues();
                var postID = v.CheckGet("POSTAVSHIC").ToInt();

                if (Report16Grid.Items.Count > 0)
                {
                    if (postID != 0)
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in Report16Grid.Items)
                        {
                            if (row.CheckGet("ID_POST").ToInt() == postID)
                            {
                                items.Add(row);
                            }
                            Report16Grid.Items = items;
                        }
                    }
                }
            };

            Report16Grid.Commands = Commander;
            Report16Grid.Init();
        }

        /// <summary>
        /// 17. Полиэтиленовая смесь. Тюки по ячейкам
        /// </summary>
        private void Report17GridInit()
        {
            //инициализация грида
            var columns = new List<DataGridHelperColumn>
            {
                 new DataGridHelperColumn
                 {
                     Header="#",
                     Path="_ROWNUMBER",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=3,
                     Visible=false,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Дата формирования ячейки",
                     Path="DATA",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy",
                     Width2 = 21,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Ряд",
                     Path="SKLAD",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Ячейка",
                     Path="NUM_PLACE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Вес",
                     Path = "WEIGHT",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Количество кип",
                     Path = "KOL",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =13,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Средний",
                     Path = "AVERAGE_WEIGHT",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Первая кипа",
                     Path="BAL_FIRST",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Последняя кипа",
                     Path="BAL_EOF",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Время формирования, мин",
                     Path = "DT_UNLOADING",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =21,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "days",
                     Path = "DAYS",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =2,
                     Visible = false,
                 },
            };

            Report17Grid.SetColumns(columns);
            Report17Grid.SetPrimaryKey("_ROWNUMBER");
            Report17Grid.SearchText = SearchTovar;
            //данные грида
            Report17Grid.OnLoadItems = ReportGridLoadItems;
            Report17Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Report17Grid.AutoUpdateInterval = 0;
            Report17Grid.EnableSortingGrid = false;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Report17Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    SelectedItem = selectedItem;
                }
            };

            Report17Grid.Commands = Commander;
            Report17Grid.Init();
        }

        /// <summary>
        /// 18. Полиэтиленовая смесь. Приход
        /// </summary>
        private void Report18GridInit()
        {
            //инициализация грида
            var columns = new List<DataGridHelperColumn>
            {
                 new DataGridHelperColumn
                 {
                     Header="#",
                     Path="_ROWNUMBER",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=3,
                     Visible=false,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Дата",
                     Path="CREATED_DTTM",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy",
                     Width2 = 10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Бригада",
                     Path="BRIGADE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=8,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Первая кипа",
                     Path="BAL_FIRST",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Последняя кипа",
                     Path="BAL_EOF",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Ряд",
                     Path="SKLAD",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Ячейка",
                     Path="NUM_PLACE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Вес на складе",
                     Path = "KOL",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =13,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Кип на складе",
                     Path = "BALE_CNT",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =13,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Средний вес кипы",
                     Path = "AVERAGE_WEIGHT",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =16,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Вес прихода",
                     Path = "WEIGHT_FACT",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =12,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Время формирования, мин",
                     Path = "DT_UNLOADING",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =21,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "ИД накладной",
                     Path = "NNAKL",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =14,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "days",
                     Path = "DAYS",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =2,
                     Visible = false,
                 },
            };

            Report18Grid.SetColumns(columns);
            Report18Grid.SetPrimaryKey("_ROWNUMBER");
            Report18Grid.SearchText = SearchTovar;
            //данные грида
            Report18Grid.OnLoadItems = ReportGridLoadItems;
            Report18Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Report18Grid.AutoUpdateInterval = 0;
            Report18Grid.EnableSortingGrid = false;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Report18Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    SelectedItem = selectedItem;
                }
            };

            Report18Grid.Commands = Commander;
            Report18Grid.Init();
        }

        /// <summary>
        /// 19. Полиэтиленовая смесь. Расход
        /// </summary>
        private void Report19GridInit()
        {
            //инициализация грида
            var columns = new List<DataGridHelperColumn>
            {
                 new DataGridHelperColumn
                 {
                     Header="#",
                     Path="_ROWNUMBER",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=3,
                     Visible=false,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Дата",
                     Path="CREATED_DTTM",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy",
                     Width2 = 10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Бригада",
                     Path="BRIGADE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Покупатель",
                     Path="POK_NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=24,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Номер акта",
                     Path="NUM_AKT_STR",
                     ColumnType=ColumnTypeRef.String,
                     Width2=10,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Машина",
                     Path = "SCRAP_NAME",
                     ColumnType = ColumnTypeRef.String,
                     Width2 =12,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Загрязнение %",
                     Path = "CONTAMINATION",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Вес в машине",
                     Path = "WEIGHT_FULL",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =13,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Вес списано",
                     Path = "SUM_KOL",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =13,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Ряд",
                     Path="SKLAD",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Ячейка",
                     Path="NUM_PLACE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=12,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Кип",
                     Path = "QTY",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =13,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Первая кипа",
                     Path="BAL_FIRST",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Последняя кипа",
                     Path="BAL_EOF",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Время загрузки, мин",
                     Path = "DT_LOADING",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =21,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "ИД расхода",
                     Path = "NSTHET",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =14,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "days",
                     Path = "DAYS",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =2,
                     Visible = false,
                 },
            };

            Report19Grid.SetColumns(columns);
            Report19Grid.SetPrimaryKey("_ROWNUMBER");
            Report19Grid.SearchText = SearchTovar;
            //данные грида
            Report19Grid.OnLoadItems = ReportGridLoadItems;
            Report19Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Report19Grid.AutoUpdateInterval = 0;
            Report19Grid.EnableSortingGrid = false;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Report19Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    SelectedItem = selectedItem;
                }
            };

            Report19Grid.Commands = Commander;
            Report19Grid.Init();
        }

        /// <summary>
        /// 20. Приход тех. обрези (БДМ1)
        /// </summary>
        private void Report20GridInit()
        {
            //инициализация грида
            var columns = new List<DataGridHelperColumn>
            {
                 new DataGridHelperColumn
                 {
                     Header="#",
                     Path="_ROWNUMBER",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=3,
                     Visible=false,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Дата",
                     Path="DATA",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy",
                     Width2 = 10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Бригада",
                     Path="BRIGADE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Категория",
                     Path="CATEGORY",
                     ColumnType=ColumnTypeRef.String,
                     Width2=18,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Ряд",
                     Path="SKLAD",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Ячейка",
                     Path="NUM_PLACE",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Количество кип",
                     Path = "KOL",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =13,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Вес прихода",
                     Path = "BALE_PRIH",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =13,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Первая кипа",
                     Path="BAL_FIRST",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Последняя кипа",
                     Path="BAL_EOF",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "ИД прихода",
                     Path = "NNAKL",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =14,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Источник",
                     Path="WASTEPAPER_SOURCE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=24,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "days",
                     Path = "DAYS",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =2,
                     Visible = false,
                 },
            };

            Report20Grid.SetColumns(columns);
            Report20Grid.SetPrimaryKey("_ROWNUMBER");
            Report20Grid.SearchText = SearchTovar;
            //данные грида
            Report20Grid.OnLoadItems = ReportGridLoadItems;
            Report20Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Report20Grid.AutoUpdateInterval = 0;
            Report20Grid.EnableSortingGrid = false;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Report20Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    SelectedItem = selectedItem;
                }
            };

            Report20Grid.Commands = Commander;
            Report20Grid.Init();
        }


        /// <summary>
        /// 21. Расход тех. обрези (БДМ1)
        /// </summary>
        private void Report21GridInit()
        {
            //инициализация грида
            var columns = new List<DataGridHelperColumn>
            {
                 new DataGridHelperColumn
                 {
                     Header="#",
                     Path="_ROWNUMBER",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=3,
                     Visible=false,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Дата",
                     Path="DATA",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy",
                     Width2 = 10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Бригада",
                     Path="BRIGADE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Категория",
                     Path="CATEGORY",
                     ColumnType=ColumnTypeRef.String,
                     Width2=18,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Ряд",
                     Path="SKLAD",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Ячейка",
                     Path="NUM_PLACE",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Количество кип",
                     Path = "KOL",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =13,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Вес списано",
                     Path = "SUM_KOL",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =13,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Первая кипа",
                     Path="BAL_FIRST",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Последняя кипа",
                     Path="BAL_EOF",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "ИД расхода",
                     Path = "NSTHET",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =14,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Источник",
                     Path="WASTEPAPER_SOURCE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=24,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "days",
                     Path = "DAYS",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =2,
                     Visible = false,
                 },
            };

            Report21Grid.SetColumns(columns);
            Report21Grid.SetPrimaryKey("_ROWNUMBER");
            Report21Grid.SearchText = SearchTovar;
            //данные грида
            Report21Grid.OnLoadItems = ReportGridLoadItems;
            Report21Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Report21Grid.AutoUpdateInterval = 0;
            Report21Grid.EnableSortingGrid = false;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Report21Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    SelectedItem = selectedItem;
                }
            };

            Report21Grid.Commands = Commander;
            Report21Grid.Init();
        }

        /// <summary>
        /// 22. МС-5Б. Склад на дату
        /// </summary>
        private void Report22GridInit()
        {
            //инициализация грида
            var columns = new List<DataGridHelperColumn>
            {
                 new DataGridHelperColumn
                 {
                     Header="#",
                     Path="_ROWNUMBER",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=3,
                     Visible=false,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Ряд",
                     Path="SKLAD",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Ячейка",
                     Path="NUM_PLACE",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=8,
                 },
                new DataGridHelperColumn
                 {
                     Header="Дата",
                     Path="DATA",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy",
                     Width2 = 10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Поставщик",
                     Path="POST_NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=24,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Машина",
                     Path="NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Номер акта",
                     Path="NUM_AKT_STR",
                     ColumnType=ColumnTypeRef.String,
                     Width2=10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Категория",
                     Path="CATEGORY",
                     ColumnType=ColumnTypeRef.String,
                     Width2=28,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Кип",
                     Path = "KOL",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Вес на складе",
                     Path = "WEIGHT_SKLAD",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =12,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Средний",
                     Path = "AVERAGE_WEIGHT",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Первая кипа",
                     Path="BAL_FIRST",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Последняя кипа",
                     Path="BAL_EOF",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Время разгрузки",
                     Path = "DT_UNLOADING",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header="МЦК",
                     Path="WASTEPAPER_CELLULOSE_QUALITY",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Сухая",
                     Path="WASTEPAPER_DRY",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Источник",
                     Path="WASTEPAPER_SOURCE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=24,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "ИД прихода",
                     Path = "NNAKL",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =14,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "days",
                     Path = "DAYS",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =2,
                     Visible = false,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "id_post",
                     Path = "ID_POST",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =2,
                     Visible = false,
                 },
            };

            // раскраска всей строки
            Report22Grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                    {
                        StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            var days = row.CheckGet("DAYS").ToInt();

                            if (days > 31) // прошло более 31 дня с момента разгрузки
                            {
                                color = "#FBC2C6"; // cветло-розовый  Background := RGB(251, 194, 198);
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },

            };

            Report22Grid.SetColumns(columns);
            Report22Grid.SetPrimaryKey("_ROWNUMBER");
            //  Report2Grid.SetSorting("SKLAD", ListSortDirection.Ascending);

            Report22Grid.SearchText = SearchTovar;
            //данные грида
            Report22Grid.OnLoadItems = ReportGridLoadItems;
            Report22Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Report22Grid.AutoUpdateInterval = 0;
            Report22Grid.EnableSortingGrid = false;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Report22Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    //     SelectedItem = selectedItem;
                }
            };

            // двойной клик по строке
            // Report1Grid.OnDblClick = EditShow;
            // фильтрация по поставщику

            Report22Grid.OnFilterItems = () =>
            {
                var v = Form.GetValues();
                var postID = v.CheckGet("POSTAVSHIC").ToInt();

                if (Report22Grid.Items.Count > 0)
                {
                    if (postID != 0)
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in Report22Grid.Items)
                        {
                            if (row.CheckGet("ID_POST").ToInt() == postID)
                            {
                                items.Add(row);
                            }
                            Report22Grid.Items = items;
                        }
                    }
                }
            };

            Report22Grid.Commands = Commander;
            Report22Grid.Init();
        }

        /// <summary>
        /// 26. Заказ поставщику на поставку макулатуры 
        /// </summary>
        private void Report26GridInit()
        {
            //инициализация грида
            var columns = new List<DataGridHelperColumn>
            {
                 new DataGridHelperColumn
                 {
                     Header="#",
                     Path="_ROWNUMBER",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=3,
                     Visible=false,
                 },
                new DataGridHelperColumn
                 {
                     Header="Начало поставки",
                     Path="SUPPLY_START_DT",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy",
                     Width2 = 16,
                 },
                new DataGridHelperColumn
                 {
                     Header="Окончание поставки",
                     Path="SUPPLY_END_DT",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy",
                     Width2 = 16,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Самовывоз",
                     Path="SELFSHIP0",
                     ColumnType=ColumnTypeRef.String,
                     Width2=10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Доставка",
                     Path="SELFSHIP1",
                     ColumnType=ColumnTypeRef.String,
                     Width2=10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Поставщик",
                     Path="NAME_POST",
                     ColumnType=ColumnTypeRef.String,
                     Width2=45,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Покупатель",
                     Path="PRODAVEZ",
                     ColumnType=ColumnTypeRef.String,
                     Width2=15,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Наименование",
                     Path="NAME_TOVAR",
                     ColumnType=ColumnTypeRef.String,
                     Width2=34,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Количество (кг.)",
                     Path = "QTY",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =12,
                     Format="{### ### ###}",
                     TotalsType=TotalsTypeRef.Summ,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "По накладным (кг.)",
                     Path = "QTY_NAKLPRIH",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =16,
                     Format="{### ### ###}",
                     TotalsType=TotalsTypeRef.Summ,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Осталось (кг.)",
                     Path = "OSTATOK_SCRAB",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =14,
                     Format="{### ### ###}",
                     TotalsType=TotalsTypeRef.Summ,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "days",
                     Path = "DAYS",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =2,
                     Visible = false,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "id_post",
                     Path = "ID_POST",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =2,
                     Visible = false,
                 },
            };

            Report26Grid.SetColumns(columns);
            Report26Grid.SetPrimaryKey("_ROWNUMBER");
            //  Report2Grid.SetSorting("SKLAD", ListSortDirection.Ascending);

            Report26Grid.SearchText = SearchTovar;
            //данные грида
            Report26Grid.OnLoadItems = ReportGridLoadItems;
            Report26Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Report26Grid.AutoUpdateInterval = 0;
            Report26Grid.EnableSortingGrid = false;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Report26Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    //     SelectedItem = selectedItem;
                }
            };

            // двойной клик по строке
            // Report1Grid.OnDblClick = EditShow;
            // фильтрация по поставщику

            Report26Grid.OnFilterItems = () =>
            {
                var v = Form.GetValues();
                var postID = v.CheckGet("POSTAVSHIC").ToInt();

                if (Report26Grid.Items.Count > 0)
                {
                    if (postID != 0)
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in Report26Grid.Items)
                        {
                            if (row.CheckGet("ID_POST").ToInt() == postID)
                            {
                                items.Add(row);
                            }
                            Report26Grid.Items = items;
                        }
                    }
                }
            };

            Report26Grid.Commands = Commander;
            Report26Grid.Init();
        }

        /// <summary>
        /// 27. "Отчет о возвратах"
        /// </summary>
        private void Report27GridInit()
        {
            //инициализация грида
            var columns = new List<DataGridHelperColumn>
            {
                 new DataGridHelperColumn
                 {
                     Header="#",
                     Path="_ROWNUMBER",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=3,
                     Visible=false,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Дата полной",
                     Path="DT_FULL",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Дата размещения",
                     Path="DT_ACCEPT",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Дата возврата",
                     Path="DT_RETURN",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Бригада",
                     Path="BRIGADE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Контролер",
                     Path="NAME_CONTROL",
                     ColumnType=ColumnTypeRef.String,
                     Width2=16,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Вид возврата",
                     Path="STATUS_NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=30,
                     Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row => GetColorRolls("ID_STATUS_STR", row)
                            },
                        },

                 },
                 new DataGridHelperColumn
                 {
                     Header="№ акта",
                     Path="ACT_RETURN_NUM",
                     ColumnType=ColumnTypeRef.String,
                     Width2=10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Машина",
                     Path="CAR_NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=16,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Поставщик",
                     Path="NAME_POST",
                     ColumnType=ColumnTypeRef.String,
                     Width2=28,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Категория",
                     Path="CATEGORY",
                     ColumnType=ColumnTypeRef.String,
                     Width2=20,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Влажность %",
                     Path = "HUMIDITY",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Загрязнение %",
                     Path = "CONTAMINATION",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Вес возврата",
                     Path = "WEIGHT_RETURN",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                     TotalsType=TotalsTypeRef.Summ,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Вес на складе",
                     Path = "KOL",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Кип возврата",
                     Path = "QTY_RETURN",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                     TotalsType=TotalsTypeRef.Summ,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Вес прихода",
                     Path = "WEIGHT_FACT",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Причина возврата",
                     Path="NOTE_RETURN",
                     ColumnType=ColumnTypeRef.String,
                     Width2=50,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Кип на складе",
                     Path = "BALE_CNT",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =8,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Ряд",
                     Path="SKLAD",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                new DataGridHelperColumn
                 {
                     Header="Ячейка",
                     Path="NUM_PLACE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=6,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Обвязка",
                     Path="TYPE_TYING",
                     ColumnType=ColumnTypeRef.String,
                     Width2=12,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Примечание",
                     Path="NOTE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=50,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "days",
                     Path = "DAYS",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =2,
                     Visible = false,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "id_post",
                     Path = "ID_POST",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =2,
                     Visible = false,
                 },
            };

            // раскраска всей строки
            Report27Grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                    {
                        StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            var days = row.CheckGet("DAYS").ToInt();

                            if (days > 31) // прошло более 31 дня с момента разгрузки
                            {
                                color = "#FBC2C6"; // cветло-розовый  Background := RGB(251, 194, 198);
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },

            };

            Report27Grid.SetColumns(columns);
            Report27Grid.SetPrimaryKey("_ROWNUMBER");
            //  Report2Grid.SetSorting("SKLAD", ListSortDirection.Ascending);

            Report27Grid.SearchText = SearchTovar;
            //данные грида
            Report27Grid.OnLoadItems = ReportGridLoadItems;
            Report27Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Report27Grid.AutoUpdateInterval = 0;
            Report27Grid.EnableSortingGrid = false;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Report27Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    //     SelectedItem = selectedItem;
                }
            };

            // двойной клик по строке
            // Report1Grid.OnDblClick = EditShow;
            // фильтрация по поставщику

            Report27Grid.OnFilterItems = () =>
            {
                var v = Form.GetValues();
                var postID = v.CheckGet("POSTAVSHIC").ToInt();

                if (Report27Grid.Items.Count > 0)
                {
                    if (postID != 0)
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in Report27Grid.Items)
                        {
                            if (row.CheckGet("ID_POST").ToInt() == postID)
                            {
                                items.Add(row);
                            }
                            Report27Grid.Items = items;
                        }
                    }
                }
            };

            Report27Grid.Commands = Commander;
            Report27Grid.Init();
        }

        /// <summary>
        /// 28. "Поступление макулатуры в ячейку N-1"
        /// </summary>
        private void Report28GridInit()
        {
            //инициализация грида
            var columns = new List<DataGridHelperColumn>
            {
                 new DataGridHelperColumn
                 {
                     Header="#",
                     Path="_ROWNUMBER",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=3,
                     Visible=false,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Производитель",
                     Path="NAME_MAKER",
                     ColumnType=ColumnTypeRef.String,
                     Width2=16,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Наименование бумаги",
                     Path="NAME_PAPER",
                     ColumnType=ColumnTypeRef.String,
                     Width2=24,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Номер рулона",
                     Path="NUM_ROLL",
                     ColumnType=ColumnTypeRef.String,
                     Width2=16,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Ид рулона",
                     Path = "IDP",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =12,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Кол-во",
                     Path = "KOL_ALL",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =10,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Остаток",
                     Path = "KOL_RESIDUE",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Дт прихода",
                     Path="DATA",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "ИД накладной",
                     Path = "NNAKL",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =12,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Причина дефекта",
                     Path="DESCRIPTION",
                     ColumnType=ColumnTypeRef.String,
                     Width2=24,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Примечание",
                     Path="NOTE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=48,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Причина списания",
                     Path="NAME_REASON",
                     ColumnType=ColumnTypeRef.String,
                     Width2=50,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "days",
                     Path = "DAYS",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =2,
                     Visible = false,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "id_post",
                     Path = "ID_POST",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =2,
                     Visible = false,
                 },
            };

            Report28Grid.SetColumns(columns);
            Report28Grid.SetPrimaryKey("_ROWNUMBER");
            //  Report2Grid.SetSorting("SKLAD", ListSortDirection.Ascending);

            Report28Grid.SearchText = SearchTovar;
            //данные грида
            Report28Grid.OnLoadItems = ReportGridLoadItems;
            Report28Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Report28Grid.AutoUpdateInterval = 0;
            Report28Grid.EnableSortingGrid = false;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Report28Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    //     SelectedItem = selectedItem;
                }
            };

            Report28Grid.Commands = Commander;
            Report28Grid.Init();
        }

        /// <summary>
        /// 29. "Перемещение забракованных рулонов"
        /// </summary>
        private void Report29GridInit()
        {
            //инициализация грида
            var columns = new List<DataGridHelperColumn>
            {
                 new DataGridHelperColumn
                 {
                     Header="#",
                     Path="_ROWNUMBER",
                     ColumnType=ColumnTypeRef.Integer,
                     Width2=3,
                     Visible=false,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Дата прихода в N-1 (склад БДМ 1)",
                     Path="DATA_PLACED",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Производитель",
                     Path="NAME_MAKER",
                     ColumnType=ColumnTypeRef.String,
                     Width2=20,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Источник",
                     Path="NAME_CS",
                     ColumnType=ColumnTypeRef.String,
                     Width2=20,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Наименование бумаги",
                     Path="NAME_PAPER",
                     ColumnType=ColumnTypeRef.String,
                     Width2=24,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Номер рулона",
                     Path="NUM",
                     ColumnType=ColumnTypeRef.String,
                     Width2=16,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "Остаток",
                     Path = "KOL",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =10,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Дата перемещения на склад БДМ2",
                     Path="DT",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Автор перемещения",
                     Path="STAFF_NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=12,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Дата списания рулона в производство БДМ 2",
                     Path="TM",
                     ColumnType=ColumnTypeRef.DateTime,
                     Format="dd.MM.yyyy HH:mm:ss",
                     Width2 = 14,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Причина дефекта",
                     Path="DESCRIPTION",
                     ColumnType=ColumnTypeRef.String,
                     Width2=24,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Примечание",
                     Path="NOTE",
                     ColumnType=ColumnTypeRef.String,
                     Width2=48,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Причина списания  рулона",
                     Path="NAME",
                     ColumnType=ColumnTypeRef.String,
                     Width2=24,
                 },
                 new DataGridHelperColumn
                 {
                     Header="Автор списания",
                     Path="STAFF_NAME_R",
                     ColumnType=ColumnTypeRef.String,
                     Width2=12,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "ИД рулона",
                     Path = "IDP",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =12,
                 },
                 new DataGridHelperColumn
                 {
                     Header = "days",
                     Path = "DAYS",
                     ColumnType = ColumnTypeRef.Integer,
                     Width2 =2,
                     Visible = false,
                 },
            };

            Report29Grid.SetColumns(columns);
            Report29Grid.SetPrimaryKey("_ROWNUMBER");
            //  Report2Grid.SetSorting("SKLAD", ListSortDirection.Ascending);

            Report29Grid.SearchText = SearchTovar;
            //данные грида
            Report29Grid.OnLoadItems = ReportGridLoadItems;
            Report29Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Report29Grid.AutoUpdateInterval = 0;
            Report29Grid.EnableSortingGrid = false;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Report29Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    //     SelectedItem = selectedItem;
                }
            };

            Report29Grid.Commands = Commander;
            Report29Grid.Init();
        }

        #endregion
                
        /// <summary>
        /// очистка данных гридов при смене площадки 
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void IdStList_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Report1Grid.ClearItems();
            Report2Grid.ClearItems();
            Report3Grid.ClearItems();
            Report4Grid.ClearItems();
            Report13Grid.ClearItems();
            Report14Grid.ClearItems();
            Report15Grid.ClearItems();
            Report16Grid.ClearItems();
            Report17Grid.ClearItems();
            Report18Grid.ClearItems();
            Report19Grid.ClearItems();
            Report20Grid.ClearItems();
            Report21Grid.ClearItems();
            Report22Grid.ClearItems();
            Report26Grid.ClearItems();
            Report27Grid.ClearItems();
            Report28Grid.ClearItems();
            Report29Grid.ClearItems();
            ScrapTansportAttrGrid.ClearItems();
        }
    }
}
