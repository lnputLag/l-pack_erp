using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static Client.Interfaces.Main.DataGridHelperColumn;
using Microsoft.Office.Interop;
using System.Windows.Input;
using Client.Assets.HighLighters;
using System.Windows.Media.Imaging;
using NPOI.Util;
using NPOI.SS.Formula.Functions;
using System.Diagnostics.Eventing.Reader;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Форма редактирования тех карты
    /// </summary>
    public partial class TechnologicalMap : ControlBase
    {
        public TechnologicalMap()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            ProcessPermissions();
            Init();
            SetDefaults();
            NotchesFirstGridInit();
            NotchesSecondGridInit();
            CreaseGridInit();

            Commander.SetCurrentGridName("TechnologicalMap");
            {
                Commander.SetCurrentGroup("main");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "save",
                        Group = "main",
                        Enabled = true,
                        Title = "Сохранить",
                        Description = "Сохранить/пересохранить и закрыть",
                        ButtonUse = true,
                        ButtonName = "SaveButton",
                        MenuUse = false,
                        Action = () =>
                        {
                            SaveAndCloseButton();
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "edit",
                        Group = "main",
                        Enabled = true,
                        Title = "Применить",
                        Description = "Сохранить/пересохранить без закрытия",
                        ButtonUse = true,
                        ButtonName = "EditButton",
                        MenuUse = false,
                        Action = () =>
                        {
                            EditButtonClick();
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "cancel",
                        Group = "main",
                        Enabled = true,
                        Title = "Отмена",
                        Description = "Закрыть без сохранения",
                        ButtonUse = true,
                        ButtonName = "CancelButton",
                        MenuUse = false,
                        Action = () =>
                        {
                            CancelButtonClick();
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "demo",
                        Group = "main",
                        Enabled = true,
                        Title = "Демо",
                        Description = "",
                        ButtonUse = true,
                        ButtonName = "DemoButton",
                        MenuUse = false,
                        Action = () =>
                        {
                            SetTestValues();
                        }
                    });
                }
                Commander.SetCurrentGroup("billet_and_confirm");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "calculate_blank",
                        Group = "billet_and_confirm",
                        Enabled = true,
                        Title = "Рассчитать размеры заготовок",
                        Description = "Рассчитать размеры заготовок",
                        ButtonUse = true,
                        ButtonName = "CalculateBlankSizeButton",
                        MenuUse = false,
                        Action = () =>
                        {
                            CalculateBlankSize();
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "save_billet",
                        Group = "billet_and_confirm",
                        Enabled = true,
                        Title = "Создать заготовку",
                        Description = "Внести заготовку в ассортимент",
                        ButtonUse = true,
                        ButtonName = "SaveBilletButton",
                        MenuUse = false,
                        Action = () =>
                        {
                            SaveBilletButtonClick();
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "confirm_button",
                        Group = "billet_and_confirm",
                        Enabled = true,
                        Title = "Создать товар",
                        Description = "Создать товар",
                        ButtonUse = true,
                        ButtonName = "ConfirmButton",
                        MenuUse = false,
                        Action = () =>
                        {
                            ConfirmButtonClick();
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            if (!(ProductKomplektId > 0) && !(ProductFirstId > 0) && TechnologicalMapIDFirst > 0
                                && (BlankFirstId > 0 || TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(1,225, 226, 227, 229))
                                && TypeExistExcel == 2)
                            {
                                result = true;
                            }
                            return result;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "resave_billet",
                        Group = "billet_and_confirm",
                        Enabled = true,
                        Title = "Пересоздать заготовку",
                        Description = "Обновить данные по заготовке в техкарте и ассортименте",
                        ButtonUse = true,
                        ButtonName = "ReSaveBillet",
                        MenuUse = false,
                        Action = () =>
                        {
                            ReSaveBilletClick();
                        }
                    });
                }
                Commander.SetCurrentGroup("package");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "calculate_dimension_of_transport_package2",
                        Group = "package",
                        Enabled = true,
                        Title = "Рассчитать ТП",
                        Description = "Рассчитать ТП",
                        ButtonUse = true,
                        ButtonName = "CalculateDimensionOfTransportPackage2Button",
                        MenuUse = false,
                        Action = () =>
                        {
                            CalculateDimensionOfTransportPackage2();
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "calculate_dimension_of_transport_package",
                        Group = "package",
                        Enabled = true,
                        Title = "Рассчитать ТП",
                        Description = "Рассчитать ТП",
                        ButtonUse = true,
                        ButtonName = "CalculateDimensionOfTransportPackageButton",
                        MenuUse = false,
                        Action = () =>
                        {
                            CalculateDimensionOfTransportPackage();
                        }
                    });
                }
                Commander.SetCurrentGroup("excel");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "create_excel_button",
                        Group = "excel",
                        Enabled = false,
                        Title = "Создать Excel",
                        Description = "Создать Excel файл техкарты",
                        ButtonUse = true,
                        ButtonName = "CreateExcelButton",
                        MenuUse = false,
                        Action = () =>
                        {
                            var values = GetDataForSave();
                            if (values != null && values.Count!=0)
                            {
                                ExcelDocumentCreate(values);
                                if (ProductFirstId > 0)
                                {
                                    ConvertExcelToPdf();
                                }
                            }
                            
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            if (TechnologicalMapIDFirst > 0 && TypeExistExcel == 0)
                            {
                                CreateExcelButton.Style = (System.Windows.Style)CreateExcelButton.TryFindResource("FButtonPrimary");
                                result = true;
                            }
                            else
                            {
                                CreateExcelButton.Style = (System.Windows.Style)CreateExcelButton.TryFindResource("Button");
                            }
                            return result;
                        }
                        
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "open_excel_button",
                        Group = "excel",
                        Enabled = false,
                        Title = "Открыть Excel",
                        Description = "Открыть Excel файл техкарты",
                        ButtonUse = true,
                        ButtonName = "OpenExcelButton",
                        MenuUse = false,
                        Action = () =>
                        {
                            OpenExcel();
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            if (TypeExistExcel != 0)
                            {
                                result = true;
                            }
                            return result;
                        }

                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "update_excel_button",
                        Group = "excel",
                        Enabled = false,
                        Title = "Перенести данные в Excel",
                        Description = "Перенести данные в Excel файл техкарты без пересоздания",
                        ButtonUse = true,
                        ButtonName = "UpdateExcelButton",
                        MenuUse = false,
                        Action = () =>
                        {
                            var values = GetDataForSave();
                            ExcelDocumentUpdate(values);
                            if (ProductFirstId > 0)
                            {
                                ConvertExcelToPdf();
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            if (TypeExistExcel.ContainsIn(1,2))
                            {
                                result = true;
                            }
                            return result;
                        }

                    });

                    Commander.Add(new CommandItem()
                    {
                        Name = "move_excel_button",
                        Group = "excel",
                        Enabled = false,
                        Title = "В рабочую папку",
                        Description = "Перенести файл техкарты в рабочую папку",
                        ButtonUse = true,
                        ButtonName = "MoveExcelButton",
                        MenuUse = false,
                        Action = () =>
                        {
                            string msg = "Перенести эксель файл в рабочую папку?";
                            var d = new DialogWindow($"{msg}", "ТК", "", DialogWindowButtons.NoYes);
                            if (d.ShowDialog() == true)
                            {
                                var values = GetDataForConfirmNew();
                                if (values.Count > 0)
                                {
                                    ExcelDocumentMove(values);
                                }
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            if (TypeExistExcel.ContainsIn(1, 3))
                            {
                                result = true;
                            }
                            if(TypeExistExcel == 1)
                            {
                                MoveExcelButton.Style = (System.Windows.Style)MoveExcelButton.TryFindResource("FButtonPrimary");
                            }
                            else
                            {
                                MoveExcelButton.Style = (System.Windows.Style)MoveExcelButton.TryFindResource("Button");
                            }

                            return result;
                        }

                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "move_to_archive_button",
                        Group = "excel",
                        Enabled = false,
                        Title = "В архив",
                        Description = "Перенести файл техкарты в архив",
                        ButtonUse = true,
                        ButtonName = "MoveToArchiveButton",
                        MenuUse = false,
                        Action = () =>
                        {
                            string msg = "Перенести техкарту в архив?";
                            var d = new DialogWindow($"{msg}", "ТК", "", DialogWindowButtons.NoYes);
                            if (d.ShowDialog() == true)
                            {
                                var values = GetDataForConfirmNew();
                                if (values.Count > 0)
                                {
                                    ExcelDocumentMoveToArchive(values);
                                }
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            if (TypeExistExcel == 2)
                            {
                                result = true;
                            }
                            return result;
                        }

                    });

                }
            }

            Commander.Init(this);

        }

        #region "Определение переменных"
        public string RoleName = "[erp]partition_technological_map";

        /// <summary>
        /// Локальная папка для сохранения файлов по тех картам 
        /// (сейчас только для ПДФ файлов)
        /// </summary>
        private string TechnologicalMapLocalFolder { get; set; }

        /// <summary>
        /// Удалённая папка для сохранения файлов по тех картам 
        /// (параметр используется для сохранения ПДФ файлов)
        /// </summary>
        private string TechnologicalMapGlobalFolder { get; set; }

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Основной ДатаСет формы
        /// </summary>
        public ListDataSet DataSetOfForm { get; set; }

        /// <summary>
        /// Основной ДатаСет грида Просечки 1
        /// </summary>
        public ListDataSet DataSetOfNotchesFirst { get; set; }

        /// <summary>
        /// Основной ДатаСет грида Рилёвок
        /// </summary>
        public ListDataSet DataSetOfCrease { get; set; }

        /// <summary>
        /// Основной ДатаСет грида Просечки 2
        /// </summary>
        public ListDataSet DataSetOfNotchesSecond { get; set; }

        /// <summary>
        /// выбранная строка в гриде Просечки 1
        /// </summary>
        public Dictionary<string, string> SelectedNotchesFirstItem { get; set; }

        /// <summary>
        /// выбранная строка в гриде Просечки 2
        /// </summary>
        public Dictionary<string, string> SelectedNotchesSecondItem { get; set; }

        /// <summary>
        /// выбранная строка в гриде Рилёвок
        /// </summary>
        public Dictionary<string, string> SelectedCreaseItem { get; set; }

        /// <summary>
        /// Полученное сообщение с размерами просечки
        /// </summary>
        public Dictionary<string, string> RowMessage { get; set; }

        /// <summary>
        /// ИД записи в таблице ТК для первой решётки
        /// </summary>
        public int TechnologicalMapIDFirst { get; set; }

        /// <summary>
        /// ИД записи в таблице ТК для второй решётки
        /// </summary>
        public int TechnologicalMapIDSecond { get; set; }

        /// <summary>
        /// ИД комплекта (t. TK_SET)
        /// </summary>
        public int IdSet { get; set; }

        /// <summary>
        /// Ид первой решётки в таблице TK_SET_DETAILS
        /// </summary>
        public int TechnologicalMapSetFirstId { get; set; }

        /// <summary>
        /// Ид второй решётки в таблице TK_SET_DETAILS
        /// </summary>
        public int TechnologicalMapSetSecondId { get; set; }

        /// <summary>
        /// Ид схемы производства (t. TOVARLINKSCHEMA)
        /// </summary>
        public int TLSFirstId { get; set; }

        /// <summary>
        /// Ид альтернативной схемы производства (t. TOVARLINKSCHEMA)
        /// </summary>
        public int TLSFirstAltId { get; set; }

        /// <summary>
        /// Ид схемы производства (t. TOVARLINKSCHEMA)
        /// </summary>
        public int TLSSecondId { get; set; }

        /// <summary>
        /// Ид альтернативной схемы производства (t. TOVARLINKSCHEMA)
        /// </summary>
        public int TLSSecondAltId { get; set; }

        /// <summary>
        /// Ид комплекта решёток из таблицы Товар
        /// </summary>
        public int ProductKomplektId { get; set; }

        /// <summary>
        /// Ид заготовки для первой решётки из таблицы Товар
        /// </summary>
        public int BlankFirstId { get; set; }

        /// <summary>
        /// Ид заготовки для второй решётки из таблицы Товар
        /// </summary>
        public int BlankSecondId { get; set; }

        /// <summary>
        /// Ид продукции (решётки) для первой решётки из таблицы Товар
        /// (Ддля решёток не в сборе)
        /// </summary>
        public int ProductFirstId { get; set; }

        /// <summary>
        /// Ид продукции (решётки) для второй решётки из таблицы Товар
        /// (Ддля решёток не в сборе)
        /// </summary>
        public int ProductSecondId { get; set; }

        /// <summary>
        /// Первые шесть символов нового артикула для решётки
        /// </summary>
        public string CodeSix { get; set; }

        /// <summary>
        /// (Наименование нового эксель файла)
        /// </summary>
        public string PathTechnologicalMapNew { get; set; }

        /// <summary>
        /// Датасет схем укладки на поддон
        /// </summary>
        public ListDataSet DataSetOfLayingSchemes { get; set; }

        /// <summary>
        /// Датасет профилей картона
        /// </summary>
        public ListDataSet DataSetOfProfile { get; set; }

        /// <summary>
        /// Датасет марок картона
        /// </summary>
        public ListDataSet DataSetOfBrand { get; set; }

        /// <summary>
        /// Датасет цветов картона
        /// </summary>
        public ListDataSet DataSetOfCollor { get; set; }

        /// <summary>
        /// Датасет картонов
        /// </summary>
        public ListDataSet DataSetOfCardboard { get; set; }

        /// <summary>
        /// Датасет картонов по данному набору параметров картона (Профиль, Марка, Цвет)
        /// </summary>
        public ListDataSet DataSetOfCardboardTarget { get; set; }

        /// <summary>
        /// Датасет схем производства
        /// </summary>
        public ListDataSet DataSetOfProductionScheme { get; set; }

        /// <summary>
        /// Пустой список; Используется для очистки селектбоксов в случае, если нет правильных данных для отображения в выпадающем списке; 
        /// (ID = ""; 
        /// NAME = "")
        /// </summary>
        public Dictionary<string, string> EmptyDictionary { get; set; }

        /// <summary>
        /// Данные грида просечек для первой решётки
        /// </summary>
        public Dictionary<string, string> NotchesValuesFirst { get; set; }

        /// <summary>
        /// Данные грида просечек для второй решётки
        /// </summary>
        public Dictionary<string, string> NotchesValuesSecond { get; set; }

        /// <summary>
        /// Датасет клиентов (покупателей)
        /// </summary>
        public ListDataSet DataSetOfClient { get; set; }

        /// <summary>
        /// Датасет потребителей
        /// </summary>
        public ListDataSet DataSetOfCustomer { get; set; }

        /// <summary>
        /// Датасет, связывающий покупателей с потребителями
        /// </summary>
        public ListDataSet DataSetClientToCustomer { get; set; }

        /// <summary>
        /// Даасет с ответом после работы нового механизма сохранения тех карты
        /// </summary>
        public ListDataSet DataSetFromSavedNew { get; set; }

        /// <summary>
        /// Датасет с данными по существующей тех карте; 
        /// (используется при получении данных тех карты для отображения выбранной тех карты в новом интерфейсе)
        /// </summary>
        public ListDataSet DataSetOfExistingTechnologicalMap { get; set; }

        /// <summary>
        /// Артикул первой решётки (по тех карте)
        /// </summary>
        public string CodeFirst { get; set; }

        /// <summary>
        /// Артикул второй решётки (по тех карте)
        /// </summary>
        public string CodeSecond { get; set; }

        /// <summary>
        /// Флаг того, что при загрузке интерфейса успешно расчитались все данные по заготовкам
        /// основываясь на сохранённых в бд данных по этим же заготовкам
        /// </summary>
        public bool SuccesfullGetBlankData { get; set; }

        /// <summary>
        /// Минимальная допустимая длина заготовки
        /// </summary>
        public int BlankLengthMin { get; set; }

        /// <summary>
        /// Флаг того, что вкладка редактирования доп требований к этой тех карте сейчас закрыта.
        /// true -- закрыта,
        /// false -- открыта.
        /// </summary>
        public bool DemandsTabIsClosed { get; set; }

        /// <summary>
        /// Ограничение на максимальную длину заготовки в зависимости от количества просечек на заготовке
        /// </summary>
        public Dictionary<int, int> MaxLengthByNotches { get; set; }
        /// <summary>
        /// Ограничение на максималддьную длину заготовки от количества ручьёв на заготовке
        /// </summary>
        public Dictionary<int, int> MaxLengthByStream { get; set; }

        /// <summary>
        /// Тип расположения Excel файла
        /// 0 - не создан, 1 - в новой, 2 - в рабочей, 3 - в архиве
        /// </summary>
        public int TypeExistExcel { get; set; }

        /// <summary>
        /// Флаг изменения количества рядов 2
        /// </summary>
        public bool FlagChangeQuantityRows2 { get; set; }
        #endregion

        #region Process
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

            if (NotchesFirstGrid != null && NotchesFirstGrid.Menu != null && NotchesFirstGrid.Menu.Count > 0)
            {
                foreach (var manuItem in NotchesFirstGrid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }

            if (NotchesSecondGrid != null && NotchesSecondGrid.Menu != null && NotchesSecondGrid.Menu.Count > 0)
            {
                foreach (var manuItem in NotchesSecondGrid.Menu)
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

        #endregion

        #region "Перестроение формы"

        #region "Заполнение SelectBox при инициализации"
        /// <summary>
        /// Прифодим форму к начальному виду
        /// </summary>
        public void SetDefaults()
        {
            FrameName = "TechnologicalMap";

            MaxLengthByNotches = new Dictionary<int, int> {
                {0, 1180},
                {1, 827},
                {2, 870},
                {3, 913},
                {4, 956},
                {5, 999},
                {6, 1042},
                {7, 1085},
                {8, 1128},
                {9, 1171},
                {10, 1180},
                {11, 1180},
                {12, 1180},
            };

            MaxLengthByStream = new Dictionary<int, int> {
                {1, 1080},
                {2, 1130},
                {3, 1180},
                {4, 1180},
                {5, 1180},
                {6, 1180},
            };

            DataSetOfProfile = new ListDataSet();
            DataSetOfBrand = new ListDataSet();
            DataSetOfProductionScheme = new ListDataSet();
            DataSetOfCardboardTarget = new ListDataSet();
            DataSetOfCardboard = new ListDataSet();

            EmptyDictionary = new Dictionary<string, string>();
            EmptyDictionary.Add("ID", "");
            EmptyDictionary.Add("NAME", "");

            NotchesValuesFirst = new Dictionary<string, string>();
            NotchesValuesSecond = new Dictionary<string, string>();

            if (Central.DebugMode != true)
            {
                DemoButton.Visibility = Visibility.Collapsed;
            }

            GridBilletFirst.IsEnabled = true;
            GridBilletSecond.IsEnabled = true;

            TechnologicalMapLocalFolder = "C:\\temp\\erp\\storage\\net\\techcards\\";
            TechnologicalMapGlobalFolder = Central.GetStorageNetworkPathByCode("techcard_pdf");
            if (string.IsNullOrEmpty(TechnologicalMapGlobalFolder))
            {
                TechnologicalMapGlobalFolder = @"\\file-server-1\external_services$\Techcard\PDF\";
            }

            BlankLengthMin = 500;

            DemandsTabIsClosed = true;

            TkBilletQuantityFirst2.IsEnabled = false;
            TkBilletQuantitySecond1.IsEnabled = false;

            TkSpecialMaterialName.Visibility = Visibility.Hidden;

            SetNameLable.Visibility = Visibility.Hidden;
            SetNameTextBox.Visibility = Visibility.Hidden;

            Form.SetDefaults();

            // Запускаем ряд функций для получения датасетов с данными для селектбоксов
            GetDataFromTypeProduct();
            GetDataFromIndependentSelectBoxes();
            GetDataFromProductionScheme();
            GetDataFromCardboard();
            GetDataFromCustomerAndClient();

        }

        /// <summary>
        /// Получение стартовых данных для селектбокса Тип продукции
        /// </summary>
        public void GetDataFromTypeProduct()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PartitionTechnologicalMap");
            q.Request.SetParam("Action", "ListTypeProduct");

            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    //Получение данных для селектбокса Тип продукции
                    var ds = ListDataSet.Create(result, "TYPE_PRODUCT");
                    TkGridTypeProduct.SetItems(ds, "ID", "NAME");
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Получение стартовых данных для независимых селектбоксов
        /// </summary>
        public void GetDataFromIndependentSelectBoxes()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PartitionTechnologicalMap");
            q.Request.SetParam("Action", "GetIndependentParametrs");

            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    //Получение данных для селектбокса На поддон
                    {
                        var ds = ListDataSet.Create(result, "PALLET");
                        TkPallet.SetItems(ds, "ID", "NAME");
                        TkPallet2.SetItems(ds, "ID", "NAME");
                    }

                    //Получение данных для селектбокса Схема укладки
                    {
                        DataSetOfLayingSchemes = ListDataSet.Create(result, "LAYING_SCHEME");
                        TkLayingScheme.SetItems(DataSetOfLayingSchemes, "ID", "NAME");
                        TkLayingScheme2.SetItems(DataSetOfLayingSchemes, "ID", "NAME");
                    }

                    //Получение данных для селектбокса Упаковка
                    {
                        var ds = ListDataSet.Create(result, "PACKAGING");
                        TkPackaging.SetItems(ds, "ID", "NAME");
                        TkPackaging2.SetItems(ds, "ID", "NAME");
                    }

                    //Получение данных для селектбокса Обвязка
                    {
                        var ds = ListDataSet.Create(result, "STRAPPING");
                        TkStrapping.SetItems(ds, "ID", "NAME");
                        TkStrapping2.SetItems(ds, "ID", "NAME");
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }
        /// <summary>
        /// Получение стартовых данных для селектбокса Схемы поля Производство
        /// </summary>
        public void GetDataFromProductionScheme()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PartitionTechnologicalMap");
            q.Request.SetParam("Action", "ListProductionScheme");

            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    //Получение данных для селектбокса Схемы
                    DataSetOfProductionScheme = ListDataSet.Create(result, "PRODUCTION_SCHEME");
                    TkProductionScheme.SetItems(DataSetOfProductionScheme, "ID", "NAME");
                    TkProductionScheme2.SetItems(DataSetOfProductionScheme, "ID", "NAME");
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Получение стартовых данных для селектбоксов Профиль, Марка, Цвет, Картон
        /// </summary>
        public void GetDataFromCardboard()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PartitionTechnologicalMap");
            q.Request.SetParam("Action", "GetCardboardParametrs");

            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    //Получение данных для селектбокса Профиль
                    {
                        DataSetOfProfile = ListDataSet.Create(result, "PROFILE");
                        TkProfile.SetItems(DataSetOfProfile, "ID", "NAME");
                    }

                    //Получение данных для селектбокса Марка
                    {
                        DataSetOfBrand = ListDataSet.Create(result, "BRAND");
                        TkBrand.SetItems(DataSetOfBrand, "ID", "NAME");
                    }

                    //Получение данных для селектбокса Цвет
                    {
                        DataSetOfCollor = ListDataSet.Create(result, "COLLOR");
                        TkCollor.SetItems(DataSetOfCollor, "ID", "OUTER_NAME");
                    }

                    //Получение данных для селектбокса Картон
                    {
                        DataSetOfCardboard = ListDataSet.Create(result, "CARDBOARD");
                        TkCardboard.SetItems(DataSetOfCardboard, "ID", "NAME");
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Получение стартовых данных для датасетов покупателей, потребителей и датасета, который связывает покупателей и потребителей
        /// </summary>
        public void GetDataFromCustomerAndClient()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PartitionTechnologicalMap");
            q.Request.SetParam("Action", "ListPokupatelAndCustomer");

            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    //Получение данных для селектбокса потребитель и покупатель

                    DataSetClientToCustomer = ListDataSet.Create(result, "ITEMS");
                    TkGridClient.SetItems(DataSetClientToCustomer, "POKUPATEL_ID", "MOLIZA_NAME");

                    // Покупатель
                    DataSetOfClient = new ListDataSet();
                    foreach (var item in DataSetClientToCustomer.Items)
                    {
                        var dic = new Dictionary<string, string>();

                        dic.Add("ID", item.CheckGet("POKUPATEL_ID"));
                        dic.Add("NAME", item.CheckGet("POKUPATEL_NAME"));

                        DataSetOfClient.Items.Add(dic);
                    }

                    // Потребитель
                    DataSetOfCustomer = new ListDataSet();
                    foreach (var item in DataSetClientToCustomer.Items)
                    {
                        var dic = new Dictionary<string, string>();

                        dic.Add("ID", item.CheckGet("CUSTOMER_ID"));
                        dic.Add("NAME", item.CheckGet("CUSTOMER_NAME"));
                        dic.Add("PATHTK_NEW", item.CheckGet("PATHTK_NEW"));
                        dic.Add("PATHTK_CONFIRM", item.CheckGet("PATHTK"));
                        dic.Add("PATHTK_ARCHIVE", item.CheckGet("PATHTK_ARCHIVE"));
                        dic.Add("CUSTOMER_SHORT", item.CheckGet("CUSTOMER_SHORT"));

                        DataSetOfCustomer.Items.Add(dic);
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        #endregion

        #region StateButtons
        /// <summary>
        /// Функция для получения расположения файла
        /// 0 - не создан, 1 - в новой, 2 - в рабочей, 3 - в архиве
        /// </summary>
        public void GetTypeExistFile()
        {
            TypeExistExcel = 0;
            if (TechnologicalMapIDFirst > 0)
            {
                var pathNew = DataSetOfCustomer.Items.FirstOrDefault(x => x.CheckGet("ID") == TkGridCustomer.SelectedItem.Key).CheckGet("PATHTK_NEW");
                var pathConfirm = DataSetOfCustomer.Items.FirstOrDefault(x => x.CheckGet("ID") == TkGridCustomer.SelectedItem.Key).CheckGet("PATHTK_CONFIRM");
                var pathArchive = DataSetOfCustomer.Items.FirstOrDefault(x => x.CheckGet("ID") == TkGridCustomer.SelectedItem.Key).CheckGet("PATHTK_ARCHIVE");
                var name = PathTechnologicalMapNew;

                if (!string.IsNullOrEmpty(PathTechnologicalMapNew))
                {
                    var fullPathTkNew = "";
                    var fullPathTkConfirm = "";
                    var fullPathTkArchive = "";

                    if (TkGridCustomer.SelectedItem.Key != null)
                    {
                        // Если файл новый
                        {
                            fullPathTkNew += DataSetOfCustomer.Items.FirstOrDefault(x => x.CheckGet("ID") == TkGridCustomer.SelectedItem.Key).CheckGet("PATHTK_NEW");
                            fullPathTkNew += PathTechnologicalMapNew;
                        }

                        // Если файл уже в основной папке
                        {
                            fullPathTkConfirm += DataSetOfCustomer.Items.FirstOrDefault(x => x.CheckGet("ID") == TkGridCustomer.SelectedItem.Key).CheckGet("PATHTK_CONFIRM");
                            fullPathTkConfirm += PathTechnologicalMapNew;
                        }

                        // Если файл уже в архиве
                        {
                            fullPathTkArchive += DataSetOfCustomer.Items.FirstOrDefault(x => x.CheckGet("ID") == TkGridCustomer.SelectedItem.Key).CheckGet("PATHTK_ARCHIVE");
                            fullPathTkArchive += PathTechnologicalMapNew;
                        }
                    }

                    if (System.IO.File.Exists(fullPathTkConfirm))
                    {
                        TypeExistExcel = 2;
                    }
                    else if (System.IO.File.Exists(fullPathTkNew))
                    {
                        TypeExistExcel = 1;
                    }
                    else if (System.IO.File.Exists(fullPathTkArchive))
                    {
                        TypeExistExcel = 3;
                    }
                    else
                    {
                        TypeExistExcel = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Установка активности кнопок
        /// </summary>
        public void SetButtons()
        {
            GetTypeExistFile();
            
            // Активность кнопки доп требований
            if (TechnologicalMapIDFirst > 0)
            {
                if (DemandsTabIsClosed)
                {
                    AddDemandsButton.IsEnabled = true;
                }
                else
                {
                    AddDemandsButton.IsEnabled = false;
                }
            }
            else
            {
                AddDemandsButton.IsEnabled = false;
            }


            // Активность полей для редактирования и пересохранения
            if (ProductKomplektId > 0 || ProductFirstId > 0)
            {
                TkProfile.IsEnabled = false;
                TkBrand.IsEnabled = false;
                TkCollor.IsEnabled = false;
                TkCardboard.IsEnabled = false;
                TkSpecialMaterial.IsEnabled = false;

                TkGridLengthFirst.IsEnabled = false;
                TkGridHeightFirst.IsEnabled = false;
                TkGridQuantityNotchesFirst.IsEnabled = false;
                TkGridQuantityFirst.IsEnabled = false;
                TkGridQuantityCrease.IsEnabled = false;
                TkGridLastCrease.IsEnabled = false;

                TkGridLengthSecond.IsEnabled = false;
                TkGridHeightSecond.IsEnabled = false;
                TkGridQuantityNotchesSecond.IsEnabled = false;
                TkGridQuantitySecond.IsEnabled = false;

                NotchesFirstGrid.IsEnabled = false;
                NotchesSecondGrid.IsEnabled = false;
                CreaseGrid.IsEnabled = false;

                TkProductionScheme.IsEnabled = false;
                TkProductionScheme2.IsEnabled = false;

                TkGridCustomer.IsEnabled = false;
                TkGridClient.IsEnabled = false;
            }
            else
            {
                TkProfile.IsEnabled = true;
                TkBrand.IsEnabled = true;
                TkCollor.IsEnabled = true;
                TkCardboard.IsEnabled = true;
                TkSpecialMaterial.IsEnabled = true;

                TkGridLengthFirst.IsEnabled = true;
                TkGridHeightFirst.IsEnabled = true;
                TkGridQuantityNotchesFirst.IsEnabled = true;
                TkGridQuantityFirst.IsEnabled = true;

                TkGridLengthSecond.IsEnabled = true;
                TkGridHeightSecond.IsEnabled = true;
                TkGridQuantityNotchesSecond.IsEnabled = true;
                TkGridQuantitySecond.IsEnabled = true;

                NotchesFirstGrid.IsEnabled = true;
                NotchesSecondGrid.IsEnabled = true;

                TkProductionScheme.IsEnabled = true;
                TkProductionScheme2.IsEnabled = true;

                TkGridCustomer.IsEnabled = true;
                TkGridClient.IsEnabled = true;
            }

            //Работа с заготовками
            // Тех карта не создана
            if (!(TechnologicalMapIDFirst > 0))
            {
                SaveBilletButton.IsEnabled = false;
                ReSaveBillet.IsEnabled = false;
            }
            // Тех карта создана
            else
            {
                if (BlankFirstId > 0)
                {
                    SaveBilletButton.IsEnabled = false;
                    ReSaveBillet.IsEnabled = true;
                }
                else
                {
                    SaveBilletButton.IsEnabled = true;
                    ReSaveBillet.IsEnabled = false;
                }
            }

            //Артикул
            if (TechnologicalMapIDFirst > 0 && string.IsNullOrEmpty(TkGridNumberFirst.Text) && string.IsNullOrEmpty(CodeFirst))
            {
                LabelArticul.TextDecorations = TextDecorations.Underline;
            }
            else
            {
                LabelArticul.TextDecorations = null;
            }

            // Расчёт габаритов ТП и укладки
            if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(1,8,14,15,100,226,229))
            {
                if (OnEdgeCheckBox.IsChecked == false)
                {
                    CalculateDimensionOfTransportPackageButton.IsEnabled = true;
                    CalculatePackingButton.IsEnabled = true;
                }
                else
                {
                    CalculateDimensionOfTransportPackageButton.IsEnabled = false;
                    CalculatePackingButton.IsEnabled = false;
                }

                if (OnEdge2CheckBox.IsChecked == false)
                {
                    CalculateDimensionOfTransportPackage2Button.IsEnabled = true;
                    CalculatePacking2Button.IsEnabled = true;
                }
                else
                {
                    CalculateDimensionOfTransportPackage2Button.IsEnabled = false;
                    CalculatePacking2Button.IsEnabled = false;
                }
            }
            else
            {
                CalculateDimensionOfTransportPackageButton.IsEnabled = false;
                CalculateDimensionOfTransportPackage2Button.IsEnabled = false;

                CalculatePackingButton.IsEnabled = false;
                CalculatePacking2Button.IsEnabled = false;
            }

            // работа с кнопками Отправить на подтверждение производством
            if (TechnologicalMapIDFirst > 0)
            {
                if (string.IsNullOrEmpty(TkProductionConfirmDateTextBox.Text))
                {
                    if (!(bool)TkProductionConfirmProcessingCheckBox.IsChecked)
                    {
                        SendTkToProductionConfirmButton.IsEnabled = true;

                        // Чёрный
                        var color = "#000000";
                        var brush = color.ToBrush();
                        TkProductionConfirmProcessingLabel.Foreground = brush;
                    }
                    else
                    {
                        SendTkToProductionConfirmButton.IsEnabled = false;

                        // Синий
                        var color = HColor.BlueFG;
                        var brush = color.ToBrush();
                        TkProductionConfirmProcessingLabel.Foreground = brush;
                    }
                }
                else
                {
                    if (!(bool)TkProductionConfirmProcessingCheckBox.IsChecked)
                    {
                        SendTkToProductionConfirmButton.IsEnabled = true;

                        // Зелёный
                        var color = HColor.GreenFG;
                        var brush = color.ToBrush();
                        TkProductionConfirmProcessingLabel.Foreground = brush;
                    }
                    else
                    {
                        SendTkToProductionConfirmButton.IsEnabled = false;

                        // Синий
                        var color = HColor.BlueFG;
                        var brush = color.ToBrush();
                        TkProductionConfirmProcessingLabel.Foreground = brush;
                    }
                }
            }
            else
            {
                SendTkToProductionConfirmButton.IsEnabled = false;

                // Чёрный
                var color = "#000000";
                var brush = color.ToBrush();
                TkProductionConfirmProcessingLabel.Foreground = brush;
            }

            this.Commander.UpdateActions();
        }
        #endregion

        #region StateForm
        /// <summary>
        /// При изменении выбранного типа продукции:
        /// Перестраиваем форму;
        /// Заполняем селектбоксы значениями, которые могут использоваться для этого типа продукции;
        /// Устанавливаем дефолтные значения полей и селектбоксов.
        /// </summary>
        public void ChangeTypeProduct(string typeProductId)
        {
            // 1. Перестраиваем форму
            EditFormVisual(typeProductId);
            FilterManagementByTypeProduct(typeProductId);

            // 2. Заполняем селектбоксы значениями, которые могут использоваться для этого типа продукции
            FillProductionSchemeByTypeProduct(typeProductId);
            FillProfileAndBrandByTypeProduct(typeProductId);
            FillTkPrepressingByTypeProduct(typeProductId);
            FillTkBilletCalculateTwoByTypeProduct(typeProductId);
            FillTypeExecution(typeProductId);

            // 3. Устанавливаем дефолтные значения полей и селектбоксов
            // Если открываем существующую тех карту, то не заполняем дефолтными значениями, так как они всё равно перезапишутся данными по этой тех карте
            if (!(IdSet > 0) && !(TechnologicalMapIDFirst > 0))
            {
                SelectDefaultProductionSchemeByTypeProduct(typeProductId);
                SelectDefaultProfileAndBrandAndColorByTypeProduct(typeProductId);
                SelectDefaultTkPrepressingByTypeProduct(typeProductId);
                SelectDefaultTkPackagingAndTkStrappingAndTkTypePackageWithByTypeProduct(typeProductId);
                SelectDefaultTkQuantityRowsByTypeProduct(typeProductId);
            }
            
            SetButtons();
        }

        /// <summary>
        /// Изменение визуальной части формы в записимости от переданного параметра типа продукции
        /// </summary>
        /// <param name="typeProductId"></param>
        public void EditFormVisual(string typeProductId)
        {
            List<Border> borderList = GetVisualChilds<Border>(this.Content as DependencyObject);
            string tag = "";
            string not_visible_tag = "";

            switch (typeProductId)
            {
                // Лист(стопы)
                case "1":
                    tag = "sheet_ream";
                    break;
                // Комплект решёток в сборе
                // Комплект решёток в сборе К
                case "12":
                case "225":
                    tag = "partition_assembled";
                    break;

                // Комплект решёток не в сборе
                // Комплект решёток не в сборе К
                case "100":
                case "229":
                    tag = "partition_not_assembled";
                    break;

                // П-образный комплект решёток в сборе К
                case "227":
                    tag = "partition_K";
                    break;

                // прокладка (стопы)
                case "14":
                    tag = "gasket_stack";
                    break;

                // прокладка (пачки)
                case "15":
                    tag = "gasket_pack";
                    break;
                case "8":
                case "9":
                    tag = "partition_single";
                    break;
                default:
                    tag = "";
                    break;
            }
            // Указываем тэг, который не будет отображен для класса
            switch (typeProductId)
            {
                case "1":
                    not_visible_tag = "not_for_sheet_ream";
                    break;
                case "225":
                case "227":
                case "229":
                case "9":
                    not_visible_tag = "for_boxed";
                    break;
                default:
                    not_visible_tag = "not_boxed";
                    break;
            }
            if (borderList != null && borderList.Count > 0)
            {
                foreach (Border border in borderList)
                {
                    if (border.Tag != null)
                    {
                        if (not_visible_tag != "" && border.Tag.ToString().Contains(not_visible_tag))
                        {
                            border.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            if (tag != "" && border.Tag.ToString().Contains(tag) || border.Tag.ToString().Contains("access_mode_full_access"))
                            {
                                border.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                border.Visibility = Visibility.Collapsed;
                            }
                        }

                    }
                    else
                    {
                        border.Visibility = Visibility.Visible;
                    }
                }
                List<Button> buttonsList = GetVisualChilds<Button>(this.Content as DependencyObject);
                if (buttonsList != null && buttonsList.Count > 0)
                {
                    foreach (Button button in buttonsList)
                    {
                        if (button.Tag != null)
                        {
                            if (not_visible_tag != "" && button.Tag.ToString().Contains(not_visible_tag))
                            {
                                button.Visibility = Visibility.Collapsed;
                            }
                            else
                            {
                                if ((tag != "" && button.Tag.ToString().Contains(tag)) || button.Tag.ToString().Contains("access_mode_full_access"))
                                {
                                    button.Visibility = Visibility.Visible;
                                }
                                else
                                {
                                    button.Visibility = Visibility.Collapsed;
                                }
                            }

                        }
                        else
                        {
                            button.Visibility = Visibility.Visible;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Устанавливаем активность первичных фильтров полей ввода для выбранного типа продукции
        /// </summary>
        public void FilterManagementByTypeProduct(string typeProductId)
        {
            if (typeProductId.ToInt().ContainsIn(100,229))
            {
                Form.RemoveFilter("WIDTH_FIRST", FormHelperField.FieldFilterRef.Required);
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "PRODUCTION_SCHEME2"), FormHelperField.FieldFilterRef.Required);
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "PALLET2"), FormHelperField.FieldFilterRef.Required);
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "LAYING_SCHEME2"), FormHelperField.FieldFilterRef.Required);
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "QUANTITY2"), FormHelperField.FieldFilterRef.Required);
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "QUANTITY2"), FormHelperField.FieldFilterRef.MaxLen, 4);
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "QUANTITY_PACK2"), FormHelperField.FieldFilterRef.Required);
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "QUANTITY_PACK2"), FormHelperField.FieldFilterRef.MaxLen, 2);
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "QUANTITY_ROWS2"), FormHelperField.FieldFilterRef.Required);
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "QUANTITY_ROWS2"), FormHelperField.FieldFilterRef.MaxLen, 2);
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "QUANTITY_BOX2"), FormHelperField.FieldFilterRef.Required);
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "PACKAGING2"), FormHelperField.FieldFilterRef.Required);
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "PACKAGE_LENGTH2"), FormHelperField.FieldFilterRef.Required);
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "PACKAGE_LENGTH2"), FormHelperField.FieldFilterRef.MaxLen, 4);
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "PACKAGE_WIDTH2"), FormHelperField.FieldFilterRef.Required);
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "PACKAGE_WIDTH2"), FormHelperField.FieldFilterRef.MaxLen, 4);
            }
            if (typeProductId.ToInt().ContainsIn(225, 229, 9))
            {
                Form.RemoveFilter("BILLET_LENGTH_FIRST", FormHelperField.FieldFilterRef.Required);
                Form.RemoveFilter("BILLET_LENGTH_FIRST", FormHelperField.FieldFilterRef.MaxLen);
                Form.RemoveFilter("BILLET_LENGTH_FIRST", FormHelperField.FieldFilterRef.Required);
                Form.RemoveFilter("BILLET_LENGTH_FIRST", FormHelperField.FieldFilterRef.MaxLen);
                Form.RemoveFilter("BILLET_SQUARE_FIRST", FormHelperField.FieldFilterRef.Required);
            }
            if (typeProductId.ToInt().ContainsIn(14,15,12, 100, 225, 229))
            {
                Form.RemoveFilter("WIDTH_FIRST", FormHelperField.FieldFilterRef.Required);
            }
            if (typeProductId.ToInt().ContainsIn(1, 8, 14, 15, 12, 225))
            {
                Form.RemoveFilter("PRODUCTION_SCHEME2", FormHelperField.FieldFilterRef.Required);
                Form.RemoveFilter("BILLET_SPRODUCT2", FormHelperField.FieldFilterRef.Required);
                Form.RemoveFilter("PALLET2", FormHelperField.FieldFilterRef.Required);
                Form.RemoveFilter("LAYING_SCHEME2", FormHelperField.FieldFilterRef.Required);
                Form.RemoveFilter("QUANTITY2", FormHelperField.FieldFilterRef.Required);
                Form.RemoveFilter("QUANTITY_PACK2", FormHelperField.FieldFilterRef.Required);
                Form.RemoveFilter("QUANTITY_ROWS2", FormHelperField.FieldFilterRef.Required);
                Form.RemoveFilter("QUANTITY_BOX2", FormHelperField.FieldFilterRef.Required);
                Form.RemoveFilter("PACKAGING2", FormHelperField.FieldFilterRef.Required);
                Form.RemoveFilter("PACKAGE_LENGTH2", FormHelperField.FieldFilterRef.Required);
                Form.RemoveFilter("PACKAGE_WIDTH2", FormHelperField.FieldFilterRef.Required);
                Form.RemoveFilter("PACKAGE_HEIGTH2", FormHelperField.FieldFilterRef.Required);
            }
            if(typeProductId.ToInt().ContainsIn(1, 8, 14, 15))
            {
                Form.RemoveFilter("NAME_SECOND", FormHelperField.FieldFilterRef.Required);
                Form.RemoveFilter("LENGTH_SECOND", FormHelperField.FieldFilterRef.Required);
                Form.RemoveFilter("HEIGHT_SECOND", FormHelperField.FieldFilterRef.Required);
                Form.RemoveFilter("QUANTITY_SECOND", FormHelperField.FieldFilterRef.Required);
                Form.RemoveFilter("QUANTITY_NOTCHES_SECOND", FormHelperField.FieldFilterRef.Required);
                Form.RemoveFilter("QUANTITY_FIRST", FormHelperField.FieldFilterRef.Required);
                Form.RemoveFilter("QUANTITY_NOTCHES_FIRST", FormHelperField.FieldFilterRef.Required);
            }
            if (typeProductId.ToInt().ContainsIn(1))
            {
                Form.RemoveFilter("PRODUCTION_SCHEME", FormHelperField.FieldFilterRef.Required);
            }
        }

        /// <summary>
        /// Получаем список всех объектов указанного типа
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static List<T> GetVisualChilds<T>(DependencyObject parent) where T : DependencyObject
        {
            List<T> childs = new List<T>();
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                DependencyObject v = VisualTreeHelper.GetChild(parent, i);
                if (v is T)
                    childs.Add(v as T);
                childs.AddRange(GetVisualChilds<T>(v));
            }
            return childs;
        }


        #endregion

        #region FillSelectBox

        /// <summary>
        /// Изменяем наполнение селектбокса вида исполнения для коробочных решеток
        /// </summary>
        public void FillTypeExecution(string typeProductId)
        {
            ClearSelectBox(TkGridTypeExecution);
            List<Dictionary<string, string>> typesExecution = new List<Dictionary<string, string>>()
            {
                new Dictionary<string, string>{
                    { "ID", "1" },
                    { "NAME", "Исполнение 1" },
                },
                new Dictionary<string, string>{
                    { "ID", "2" },
                    { "NAME", "Исполнение 2" },
                }
            };

            switch (typeProductId)
            {
                // Коробочные решётки
                case "225":
                case "227":
                case "229":
                    foreach (var item in typesExecution)
                    {
                        TkGridTypeExecution.Items.Add(item.CheckGet("ID"), item.CheckGet("NAME"));
                    }
                    break;
                default:
                    break;
            }
            TkGridTypeExecution.SelectedItem = TkGridTypeExecution.Items.FirstOrDefault(x => x.Key != "0");
        }

        /// <summary>
        /// Изменяем наполнение селектбокса схемы производства для выбранного типа продукции
        /// </summary>
        public void FillProductionSchemeByTypeProduct(string typeProductId)
        {
            if (DataSetOfProductionScheme != null && DataSetOfProductionScheme.Items != null && DataSetOfProductionScheme.Items.Count > 0)
            {
                List<Dictionary<string, string>> tkProductionSchemeDs = new List<Dictionary<string, string>>();
                switch (typeProductId)
                {
                    // Комплект решёток не в сборе
                    case "8":
                    case "9":
                    case "100":
                        ClearSelectBox(TkProductionScheme);
                        ClearSelectBox(TkProductionScheme2);
                        foreach (var item in DataSetOfProductionScheme.Items)
                        {
                            if (!item.CheckGet("NAME").Contains("Склад") && !item.CheckGet("NAME").Contains("RdAn") && !item.CheckGet("NAME").Contains("Г-КГ4"))
                            {
                                tkProductionSchemeDs.Add(item);
                            }
                        }

                        foreach (var item in tkProductionSchemeDs)
                        {
                            TkProductionScheme.Items.Add(item.CheckGet("ID"), item.CheckGet("NAME"));
                            TkProductionScheme2.Items.Add(item.CheckGet("ID"), item.CheckGet("NAME"));
                        }
                        break;

                    // Комплект решёток в сборе
                    case "12":
                        ClearSelectBox(TkProductionScheme);
                        ClearSelectBox(TkProductionScheme2);
                        foreach (var item in DataSetOfProductionScheme.Items)
                        {
                            if (!item.CheckGet("NAME").Contains("Склад"))
                            {
                                if (item.CheckGet("NAME").Contains("RdAn"))
                                {
                                    tkProductionSchemeDs.Add(item);
                                }
                            }
                        }

                        foreach (var item in tkProductionSchemeDs)
                        {
                            TkProductionScheme.Items.Add(item.CheckGet("ID"), item.CheckGet("NAME"));
                            TkProductionScheme2.Items.Add(item.CheckGet("ID"), item.CheckGet("NAME"));
                        }
                        break;

                    // Комплект решёток не в сборе К
                    case "229":
                        ClearSelectBox(TkProductionScheme);
                        ClearSelectBox(TkProductionScheme2);
                        foreach (var item in DataSetOfProductionScheme.Items)
                        {
                            if (item.CheckGet("ID").ToInt().ContainsIn(1350, 1360, 1477))
                            {
                                tkProductionSchemeDs.Add(item);
                            }
                        }

                        foreach (var item in tkProductionSchemeDs)
                        {
                            TkProductionScheme.Items.Add(item.CheckGet("ID"), item.CheckGet("NAME"));
                            TkProductionScheme2.Items.Add(item.CheckGet("ID"), item.CheckGet("NAME"));
                        }
                        break;

                    // Комплект решёток в сборе К
                    case "225":
                        ClearSelectBox(TkProductionScheme);
                        ClearSelectBox(TkProductionScheme2);
                        foreach (var item in DataSetOfProductionScheme.Items)
                        {
                            if (item.CheckGet("ID").ToInt().ContainsIn(1350, 1360, 1477))
                            {
                                tkProductionSchemeDs.Add(item);
                            }
                        }

                        foreach (var item in tkProductionSchemeDs)
                        {
                            TkProductionScheme.Items.Add(item.CheckGet("ID"), item.CheckGet("NAME"));
                        }
                        break;

                    // П-образный комплект решёток в сборе К
                    case "227":
                        ClearSelectBox(TkProductionScheme);
                        ClearSelectBox(TkProductionScheme2);
                        foreach (var item in DataSetOfProductionScheme.Items)
                        {
                            if (item.CheckGet("ID").ToInt().ContainsIn(1360, 1477))
                            {
                                tkProductionSchemeDs.Add(item);
                            }
                        }

                        foreach (var item in tkProductionSchemeDs)
                        {
                            TkProductionScheme.Items.Add(item.CheckGet("ID"), item.CheckGet("NAME"));
                            TkProductionScheme2.Items.Add(item.CheckGet("ID"), item.CheckGet("NAME"));
                        }
                        break;

                    // Прокладки (пачки)
                    case "14":
                    // Прокладки (стопы)
                    case "15":
                    // Прокладки К
                    case "226":
                        ClearSelectBox(TkProductionScheme);
                        ClearSelectBox(TkProductionScheme2);
                        foreach (var item in DataSetOfProductionScheme.Items)
                        {
                            TkProductionScheme.Items.Add(item.CheckGet("ID"), item.CheckGet("NAME"));
                            TkProductionScheme2.Items.Add(item.CheckGet("ID"), item.CheckGet("NAME"));
                        }
                        break;
                    default:
                        break;
                }
            }
        }
        /// <summary>
        /// Изменяем наполнение селектбоксов профиль и марка картона для выбранного типа продукции
        /// </summary>
        public void FillProfileAndBrandByTypeProduct(string typeProductId)
        {
            if (DataSetOfProfile != null && DataSetOfProfile.Items != null && DataSetOfProfile.Items.Count > 0
                && DataSetOfBrand != null && DataSetOfBrand.Items != null && DataSetOfBrand.Items.Count > 0)
            {
                List<Dictionary<string, string>> tkProfileDs = new List<Dictionary<string, string>>();
                List<Dictionary<string, string>> tkBrandDs = new List<Dictionary<string, string>>();
                List<Dictionary<string, string>> tkColorDs = new List<Dictionary<string, string>>();
                List<Dictionary<string, string>> tkCartonDs = new List<Dictionary<string, string>>();
                switch (typeProductId)
                {
                    // комплект решеток не в сборе
                    case "8":
                    case "100":
                        ClearSelectBox(TkProfile);
                        foreach (var item in DataSetOfProfile.Items)
                        {
                            if (item.CheckGet("ID") != "12" && item.CheckGet("ID") != "13")
                            {
                                tkProfileDs.Add(item);
                            }
                        }

                        foreach (var item in tkProfileDs)
                        {
                            TkProfile.Items.Add(item.CheckGet("ID"), item.CheckGet("NAME"));
                        }

                        ClearSelectBox(TkBrand);
                        foreach (var item in DataSetOfBrand.Items)
                        {
                            if (item.CheckGet("ID") != "1")
                            {
                                tkBrandDs.Add(item);
                            }
                        }

                        foreach (var item in tkBrandDs)
                        {
                            TkBrand.Items.Add(item.CheckGet("ID"), item.CheckGet("NAME"));
                        }

                        break;

                    // комплект решёток в сборе
                    case "12":
                        ClearSelectBox(TkProfile);
                        foreach (var item in DataSetOfProfile.Items)
                        {
                            if (item.CheckGet("ID") != "12" && item.CheckGet("ID") != "13")
                            {
                                tkProfileDs.Add(item);
                            }
                        }

                        foreach (var item in tkProfileDs)
                        {
                            TkProfile.Items.Add(item.CheckGet("ID"), item.CheckGet("NAME"));
                        }

                        ClearSelectBox(TkBrand);
                        foreach (var item in DataSetOfBrand.Items)
                        {
                            if (item.CheckGet("ID") != "1")
                            {
                                tkBrandDs.Add(item);
                            }
                        }

                        foreach (var item in tkBrandDs)
                        {
                            TkBrand.Items.Add(item.CheckGet("ID"), item.CheckGet("NAME"));
                        }

                        break;

                    // Комплект решёток не в сборе К
                    case "9":
                    case "229":
                        ClearSelectBox(TkProfile);
                        foreach (var item in DataSetOfProfile.Items)
                        {
                            if (item.CheckGet("ID") == "12")
                            {
                                tkProfileDs.Add(item);
                            }
                        }

                        foreach (var item in tkProfileDs)
                        {
                            TkProfile.Items.Add(item.CheckGet("ID"), item.CheckGet("NAME"));
                        }

                        ClearSelectBox(TkBrand);
                        foreach (var item in DataSetOfBrand.Items)
                        {
                            if (item.CheckGet("ID") == "1")
                            {
                                tkBrandDs.Add(item);
                            }
                        }

                        foreach (var item in tkBrandDs)
                        {
                            TkBrand.Items.Add(item.CheckGet("ID"), item.CheckGet("NAME"));
                        }
                        ClearSelectBox(TkCollor);
                        foreach (var item in DataSetOfCollor.Items)
                        {
                            if (item.CheckGet("ID") == "2")
                            {
                                tkColorDs.Add(item);
                            }
                        }

                        foreach (var item in tkColorDs)
                        {
                            TkCollor.Items.Add(item.CheckGet("ID"), item.CheckGet("OUTER_NAME"));
                        }

                        ClearSelectBox(TkCardboard);
                        foreach (var item in DataSetOfCardboard.Items)
                        {
                            if (item.CheckGet("ID") == "1175")
                            {
                                tkCartonDs.Add(item);
                            }
                        }

                        foreach (var item in tkCartonDs)
                        {
                            TkCardboard.Items.Add(item.CheckGet("ID"), item.CheckGet("NAME"));
                        }
                        break;
                    // Комплект решёток в сборе К
                    case "225":
                        ClearSelectBox(TkProfile);
                        foreach (var item in DataSetOfProfile.Items)
                        {
                            if (item.CheckGet("ID") == "12")
                            {
                                tkProfileDs.Add(item);
                            }
                        }

                        foreach (var item in tkProfileDs)
                        {
                            TkProfile.Items.Add(item.CheckGet("ID"), item.CheckGet("NAME"));
                        }

                        ClearSelectBox(TkBrand);
                        foreach (var item in DataSetOfBrand.Items)
                        {
                            if (item.CheckGet("ID") == "1")
                            {
                                tkBrandDs.Add(item);
                            }
                        }

                        foreach (var item in tkBrandDs)
                        {
                            TkBrand.Items.Add(item.CheckGet("ID"), item.CheckGet("NAME"));
                        }
                        ClearSelectBox(TkCollor);
                        foreach (var item in DataSetOfCollor.Items)
                        {
                            if (item.CheckGet("ID") == "2")
                            {
                                tkColorDs.Add(item);
                            }
                        }

                        foreach (var item in tkColorDs)
                        {
                            TkCollor.Items.Add(item.CheckGet("ID"), item.CheckGet("OUTER_NAME"));
                        }

                        ClearSelectBox(TkCardboard);
                        foreach (var item in DataSetOfCardboard.Items)
                        {
                            if (item.CheckGet("ID") == "1175")
                            {
                                tkCartonDs.Add(item);
                            }
                        }

                        foreach (var item in tkCartonDs)
                        {
                            TkCardboard.Items.Add(item.CheckGet("ID"), item.CheckGet("NAME"));
                        }
                        break;
                    // П-образный комплект решёток в сборе К
                    case "227":
                        ClearSelectBox(TkProfile);
                        foreach (var item in DataSetOfProfile.Items)
                        {
                            if (item.CheckGet("ID") == "12")
                            {
                                tkProfileDs.Add(item);
                            }
                        }

                        foreach (var item in tkProfileDs)
                        {
                            TkProfile.Items.Add(item.CheckGet("ID"), item.CheckGet("NAME"));
                        }

                        ClearSelectBox(TkBrand);
                        foreach (var item in DataSetOfBrand.Items)
                        {
                            if (item.CheckGet("ID") == "1")
                            {
                                tkBrandDs.Add(item);
                            }
                        }

                        foreach (var item in tkBrandDs)
                        {
                            TkBrand.Items.Add(item.CheckGet("ID"), item.CheckGet("NAME"));
                        }
                        ClearSelectBox(TkCollor);
                        foreach (var item in DataSetOfCollor.Items)
                        {
                            if (item.CheckGet("ID") == "2")
                            {
                                tkColorDs.Add(item);
                            }
                        }

                        foreach (var item in tkColorDs)
                        {
                            TkCollor.Items.Add(item.CheckGet("ID"), item.CheckGet("OUTER_NAME"));
                        }

                        ClearSelectBox(TkCardboard);
                        foreach (var item in DataSetOfCardboard.Items)
                        {
                            if (item.CheckGet("ID") == "1175")
                            {
                                tkCartonDs.Add(item);
                            }
                        }

                        foreach (var item in tkCartonDs)
                        {
                            TkCardboard.Items.Add(item.CheckGet("ID"), item.CheckGet("NAME"));
                        }
                        break;

                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Изменяем возможность взаимодейтсвовать с чекбоксом подпресовка поддона для выбранного типа продукции
        /// </summary>
        /// <param name="typeProductId"></param>
        public void FillTkPrepressingByTypeProduct(string typeProductId)
        {
            switch (typeProductId)
            {

                case "8":
                case "9":
                case "12":
                case "225":
                case "227":
                    TkPrepressing.IsEnabled = false;
                    TkPrepressing2.IsEnabled = false;
                    break;
                case "15":
                case "14":
                case "100":
                case "226":
                case "229":
                    TkPrepressing.IsEnabled = true;
                    TkPrepressing2.IsEnabled = true;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Изменяем возможность взаимодейтсвовать с чекбоксом считать для двух решёток для выбранного типа продукции
        /// </summary>
        /// <param name="typeProductId"></param>
        public void FillTkBilletCalculateTwoByTypeProduct(string typeProductId)
        {
            switch (typeProductId)
            {

                case "14":
                case "15":
                case "8":
                case "9":
                case "100":
                case "225":
                case "226":
                case "229":
                    TkBilletCalculateTwoFirst.IsEnabled = false;
                    TkBilletCalculateTwoSecond.IsEnabled = false;
                    break;

                case "12":
                case "227":
                    TkBilletCalculateTwoFirst.IsEnabled = true;
                    TkBilletCalculateTwoSecond.IsEnabled = true;
                    break;


                default:
                    break;
            }
        }

        #endregion

        #region SetSelectBox
        /// <summary>
        /// Выбираем схему производства по умолчанию для выбранного типа продукции
        /// </summary>
        public void SelectDefaultProductionSchemeByTypeProduct(string typeProductId)
        {
            switch (typeProductId)
            {
                // Комплект решёток не в сборе
                case "100":
                // Прокладки (пачки)
                case "14":
                // Прокладки (стопы)
                case "15":
                // Решетка
                case "8":
                // Прокладки К
                case "226":
                    TkProductionScheme.SelectedItem = TkProductionScheme.Items.FirstOrDefault(x => x.Key == "122");
                    TkProductionScheme2.SelectedItem = TkProductionScheme.Items.FirstOrDefault(x => x.Key == "122");
                    break;

                // Комплект решёток в сборе
                case "12":
                    TkProductionScheme.SelectedItem = TkProductionScheme.Items.FirstOrDefault(x => x.Key == "1546");
                    TkProductionScheme2.SelectedItem = TkProductionScheme.Items.FirstOrDefault(x => x.Key == "1546");
                    break;

                // Комплект решёток не в сборе К
                case "229":
                    TkProductionScheme.SelectedItem = TkProductionScheme.Items.FirstOrDefault(x => x.Key == "1350");
                    TkProductionScheme2.SelectedItem = TkProductionScheme.Items.FirstOrDefault(x => x.Key == "1350");
                    break;

                // Комплект решёток в сборе К
                case "225":
                    TkProductionScheme.SelectedItem = TkProductionScheme.Items.FirstOrDefault(x => x.Key == "1360");
                    TkProductionScheme2.SelectedItem = TkProductionScheme.Items.FirstOrDefault(x => x.Key == "1360");
                    break;

                // П-образный комплект решёток в сборе К
                case "227":
                    TkProductionScheme.SelectedItem = TkProductionScheme.Items.FirstOrDefault(x => x.Key == "1370");
                    TkProductionScheme2.SelectedItem = TkProductionScheme.Items.FirstOrDefault(x => x.Key == "1370");
                    break;
                case "1":
                    TkProductionScheme.Clear();
                    TkProductionScheme2.Clear();
                    TkProductionScheme.IsEnabled = false;
                    TkProductionScheme2.IsEnabled = false;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Выбираем профиль, марка и цвет картона по умолчанию для выбранного типа продукции
        /// </summary>
        public void SelectDefaultProfileAndBrandAndColorByTypeProduct(string typeProductId)
        {
            switch (typeProductId)
            {
                // комплект решеток не в сборе
                case "100":
                    TkProfile.SelectedItem = TkProfile.Items.FirstOrDefault(x => x.Key == "1");
                    TkBrand.SelectedItem = TkBrand.Items.FirstOrDefault(x => x.Key == "21");
                    TkCollor.SelectedItem = TkCollor.Items.FirstOrDefault(x => x.Key == "2");
                    break;

                // комплект решёток в сборе
                case "8":
                case "12":
                    TkProfile.SelectedItem = TkProfile.Items.FirstOrDefault(x => x.Key == "1");
                    TkBrand.SelectedItem = TkBrand.Items.FirstOrDefault(x => x.Key == "21");
                    TkCollor.SelectedItem = TkCollor.Items.FirstOrDefault(x => x.Key == "2");
                    break;

                // Комплект решёток не в сборе К
                case "229":
                    TkProfile.SelectedItem = TkProfile.Items.FirstOrDefault(x => x.Key == "12");
                    TkBrand.SelectedItem = TkBrand.Items.FirstOrDefault(x => x.Key == "1");
                    TkCollor.SelectedItem = TkCollor.Items.FirstOrDefault(x => x.Key == "2");
                    break;

                // Комплект решёток в сборе К
                case "225":
                    TkProfile.SelectedItem = TkProfile.Items.FirstOrDefault(x => x.Key == "12");
                    TkBrand.SelectedItem = TkBrand.Items.FirstOrDefault(x => x.Key == "1");
                    TkCollor.SelectedItem = TkCollor.Items.FirstOrDefault(x => x.Key == "2");
                    break;

                // П-образный комплект решёток в сборе К
                case "227":
                    TkProfile.SelectedItem = TkProfile.Items.FirstOrDefault(x => x.Key == "12");
                    TkBrand.SelectedItem = TkBrand.Items.FirstOrDefault(x => x.Key == "1");
                    TkCollor.SelectedItem = TkCollor.Items.FirstOrDefault(x => x.Key == "2");
                    break;

                // Прокладки (пачки)
                case "14":
                    break;

                // Прокладки (стопы)
                case "15":
                    break;

                // Прокладки К
                case "226":
                    break;

                default:
                    break;
            }

            CheckCardboardParametrs();
        }

        /// <summary>
        /// Выбираем значение чекбокса подпресовка поддона по умолчанию для выбранного типа продукции
        /// </summary>
        public void SelectDefaultTkPrepressingByTypeProduct(string typeProductId)
        {
            switch (typeProductId)
            {
                case "100":
                    TkPrepressing.IsChecked = true;
                    TkPrepressing2.IsChecked = true;
                    break;
                case "8":
                case "12":
                case "14":
                case "15":
                case "225":
                case "226":
                case "227":
                case "229":
                    TkPrepressing.IsChecked = false;
                    TkPrepressing2.IsChecked = false;
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Выбираем упаковку, обвязку и вид упаковки по умолчанию для выбранного типа продукции
        /// </summary>
        /// <param name="typeProductId"></param>
        public void SelectDefaultTkPackagingAndTkStrappingAndTkTypePackageWithByTypeProduct(string typeProductId)
        {
            switch (typeProductId)
            {
                case "100":
                case "229":
                case "12":
                case "225":
                    if (TkPackaging.Items.ContainsKey("1"))
                    {
                        TkPackaging.SelectedItem = TkPackaging.Items.FirstOrDefault(x => x.Key == "1");
                    }

                    if (TkPackaging2.Items.ContainsKey("1"))
                    {
                        TkPackaging2.SelectedItem = TkPackaging2.Items.FirstOrDefault(x => x.Key == "1");
                    }

                    if (TkStrapping.Items.ContainsKey("4"))
                    {
                        TkStrapping.SelectedItem = TkStrapping.Items.FirstOrDefault(x => x.Key == "4");
                    }

                    if (TkStrapping2.Items.ContainsKey("4"))
                    {
                        TkStrapping2.SelectedItem = TkStrapping2.Items.FirstOrDefault(x => x.Key == "4");
                    }

                    Form.SetValueByPath("TYPE_PACKAGE", "1");
                    Form.SetValueByPath("TYPE_PACKAGE2", "1");
                    CheckTkTypePackage();
                    CheckTkTypePackage2();
                    break;

                case "8":
                case "14":
                case "15":
                case "226":
                    if (TkPackaging.Items.ContainsKey("1"))
                    {
                        TkPackaging.SelectedItem = TkPackaging.Items.FirstOrDefault(x => x.Key == "1");
                    }

                    if (TkStrapping.Items.ContainsKey("4"))
                    {
                        TkStrapping.SelectedItem = TkStrapping.Items.FirstOrDefault(x => x.Key == "4");
                    }

                    Form.SetValueByPath("TYPE_PACKAGE", "1");
                    break;

                default:
                    break;
            }
        }
        public void SelectDefaultTkQuantityRowsByTypeProduct(string typeProductId)
        {
            switch (typeProductId)
            {
                case "1":
                case "14":
                    TkQuantityRows.Text = "1";
                    break;

                default:
                    break;
            }
        }

        #endregion

        #endregion


        #region "Инициализация"
        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void Init()
        {
            Form = new FormHelper();
            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                // Покупатель, тип, артикул
                new FormHelperField()
                {
                    Path="TYPE_PRODUCT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkGridTypeProduct,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {
                        string typeProductId = TkGridTypeProduct.SelectedItem.Key;
                        ChangeTypeProduct(typeProductId);
                        if(TechnologicalMapIDFirst > 0)
                        {
                            SelectDefaultProductionSchemeByTypeProduct(typeProductId);
                        }
                    },
                },
                new FormHelperField()
                {
                    Path="PARTITION_TYPE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkGridTypeExecution,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {
                    },
                    Validate = (f,v)=>
                    {
                        if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(225,229))
                        {
                            if (TkGridTypeExecution?.SelectedItem.Key?.ToInt() == 0)
                            {
                                f.ValidateResult = false;
                                f.ValidateProcessed = true;
                                f.ValidateMessage = "Вид исполнения не должен быть пустым";
                            }
                        }
                    }
                },
                new FormHelperField()
                {
                    Path="NUMBER_FIRST",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TkGridNumberFirst,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NUMBER_SECOND",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TkGridNumberSecond,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CLIENT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkGridClient,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {
                        CheckCustomerByClient();
                    },
                    Validate = (f, v) =>
                    {
                        if (TkGridClient.SelectedItem.Key.ToInt() <= 0)
                        {
                            f.ValidateResult = false;
                            f.ValidateProcessed = true;
                            f.ValidateMessage = "Заполните поле покупателя";
                        }
                    },
                    OnCreate = (f) =>
                    {
                        TkGridClient.SetSelectedItemFirst();
                    }
                },

                // Потребитель, описание, наименование
                new FormHelperField()
                {
                    Path="NAME_FIRST",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TkGridNameFirst,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 50 },
                        { FormHelperField.FieldFilterRef.DeniedCharacters, "'" },
                    },
                },
                new FormHelperField()
                {
                    Path="DETAILS",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TkGridDetails,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.MaxLen, 50 },
                    },
                },
                new FormHelperField()
                {
                    Path="NAME_SECOND",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TkGridNameSecond,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.MaxLen, 50 },
                        { FormHelperField.FieldFilterRef.DeniedCharacters, "'" },
                    },
                },
                new FormHelperField()
                {
                    Path="CUSTOMER",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkGridCustomer,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    Enabled = false,
                    Validate = (f, v) =>
                    {
                        if (TkGridCustomer.SelectedItem.Key.ToInt() <= 0)
                        {
                            f.ValidateResult = false;
                            f.ValidateProcessed = true;
                            f.ValidateMessage = "Заполните поле потребителя";
                        }
                    }
                },

                // Просечки
                new FormHelperField()
                {
                    Path="QUANTITY_CREASE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkGridQuantityCrease,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.MaxLen, 2 },
                    },
                    OnTextChange = (FormHelperField field, string value) =>
                    {
                        if (TkGridQuantityCrease.Text.ToInt() > 0)
                        {
                            CreaseGridLoadItems();
                            TkGridOneNotch.IsEnabled = false;
                            TkGridLastCrease.IsEnabled = true;
                        }
                        else
                        {
                            CreaseGrid?.ClearItems();
                            DataSetOfCrease?.Items?.Clear();
                            NotchesFirstGrid?.ClearItems();
                            DataSetOfNotchesFirst?.Items?.Clear();
                            TkGridLastCrease.Text = "";
                            TkGridLastCrease.IsEnabled = false;
                            TkGridOneNotch.IsEnabled = true;
                        }
                    },
                },
                new FormHelperField()
                {
                    Path="LAST_CREASE",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=TkGridLastCrease,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitCommaOnly, null },
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {

                    },
                    Enabled = false,
                    Validate = (f,v)=>
                    {
                        if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(8,14,15))
                        {
                            double sum = 0;
                            bool valid = true;
                            if (DataSetOfCrease != null && DataSetOfCrease.Items != null && DataSetOfCrease.Items.Count > 0)
                            {
                                foreach (var item in DataSetOfCrease.Items)
                                {
                                    sum += item.CheckGet("CONTENT").ToDouble();
                                }
                                // Добавляем контрольную длину 
                                sum += TkGridLastCrease.Text.ToDouble();
                                if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(14, 15))
                                {
                                    if (sum != TkGridHeightFirst.Text.ToDouble())
                                    {
                                        f.ValidateResult = false;
                                        f.ValidateProcessed = true;
                                        f.ValidateMessage = "Сумма рилевок и расстояния до края должны соответствовать ширине прокладки";
                                    }
                                }
                                else
                                {
                                    if (sum != TkGridLengthFirst.Text.ToDouble())
                                    {
                                        f.ValidateResult = false;
                                        f.ValidateProcessed = true;
                                        f.ValidateMessage = "Сумма просечек и расстояния до края должны соответствовать длине решётки";
                                    }
                                }
                                
                            }
                        }
                    },
                },

                // Решетка 1
                new FormHelperField()
                {
                    Path="LENGTH_FIRST",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkGridLengthFirst,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 4 },
                    },
                    OnTextChange = (FormHelperField field, string value) =>
                    {
                        CalculateSProduct();
                        SetProductionSchemeByHeight();
                        if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(225,229))
                        {
                            ValidateBoxedPartitionSize();
                        }
                    },
                },
                new FormHelperField()
                {
                    Path="HEIGHT_FIRST",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkGridHeightFirst,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 4 },
                        { FormHelperField.FieldFilterRef.MinValue, 50},
                    },
                    OnTextChange = (FormHelperField field, string value) =>
                    {
                        TkGridHeightSecond.Text = TkGridHeightFirst.Text;
                        CalculateSProduct();
                        SetProductionSchemeByHeight();
                        SetTkPrepressingByHeight();
                        if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(225,229))
                        {
                            ValidateBoxedPartitionSize();
                        }
                    },
                    Validate = (f,v)=>
                    {

                    }
                },
                new FormHelperField()
                {
                    Path="WIDTH_FIRST",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=TkGridWidthFirst,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 4 },
                    },
                },
                new FormHelperField()
                {
                    Path="ONE_NOTCH",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=TkGridOneNotch,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 4 },
                    },
                    OnTextChange = (FormHelperField field, string value) =>
                    {
                        if (TkGridOneNotch.Text.ToDouble()>0)
                        {
                            TkGridQuantityCrease.IsEnabled = false;
                            TkGridLastCrease.IsEnabled = false;
                            TkGridQuantityCrease.Text = null;
                            TkGridLastCrease.Text = null;

                        }
                        else
                        {
                            TkGridQuantityCrease.IsEnabled = true;
                        }
                    }
                },
                new FormHelperField()
                {
                    Path="QUANTITY_FIRST",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkGridQuantityFirst,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.MaxLen, 2 },
                    },
                    OnTextChange = (FormHelperField field, string value) =>
                    {
                        CalculateSProduct();
                        if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(225,229))
                        {
                            ValidateBoxedPartitionSize();
                        }
                        TkGridQuantityNotchesSecond.Text = TkGridQuantityFirst.Text;
                    },
                },
                new FormHelperField()
                {
                    Path="QUANTITY_NOTCHES_FIRST",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkGridQuantityNotchesFirst,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.MaxLen, 2 },
                    },
                    OnTextChange = (FormHelperField field, string value) =>
                    {
                        NotchesFirstGridLoadItems();
                        TkGridQuantitySecond.Text = TkGridQuantityNotchesFirst.Text;
                    },
                    Validate = (f,v)=>
                    {
                        int type = TkGridTypeProduct.SelectedItem.Key.ToInt();
                        if(type != 1)
                        {
                            if(type.ContainsIn(9, 12, 100, 225, 229))
                            {
                                if (TkGridQuantityNotchesFirst.Text.ToInt() == 0)
                                {
                                    f.ValidateResult = false;
                                    f.ValidateProcessed = true;
                                    f.ValidateMessage = "Количество просечек не должно равняться 0";
                                }
                            }
                            if (type.ContainsIn(9, 225, 229))
                            {
                                if (TkGridQuantityNotchesFirst.Text.ToInt() > 18)
                                {
                                    f.ValidateResult = false;
                                    f.ValidateProcessed = true;
                                    f.ValidateMessage = "Количество просечек не должно превышать 18";
                                }
                            }
                        }
                        
                    }
                },

                
                // Решетка 2
                new FormHelperField()
                {
                    Path="LENGTH_SECOND",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkGridLengthSecond,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.MaxLen, 4 },
                    },
                    OnTextChange = (FormHelperField field, string value) =>
                    {
                        CalculateSProduct();
                        if (TkGridTypeProduct.SelectedItem.Key.ToInt() == 229 || TkGridTypeProduct.SelectedItem.Key.ToInt() == 225 || TkGridTypeProduct.SelectedItem.Key.ToInt() == 227)
                        {
                            ValidateBoxedPartitionSize();
                        }
                    },
                    Validate = (f,v)=>
                    {
                        int type = TkGridTypeProduct.SelectedItem.Key.ToInt();
                        if(type != 1)
                        {
                            if(type.ContainsIn(12, 100, 225, 229))
                            {
                                if (TkGridLengthSecond.Text.ToInt() == 0)
                                {
                                    f.ValidateResult = false;
                                    f.ValidateProcessed = true;
                                    f.ValidateMessage = "Длина решетки не должна равняться 0";
                                }
                            }
                        }

                    }
                },
                new FormHelperField()
                {
                    Path="HEIGHT_SECOND",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkGridHeightSecond,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 4 },
                    },
                    OnTextChange = (FormHelperField field, string value) =>
                    {
                        CalculateSProduct();
                        SetTkPrepressingByHeight2();
                        if (TkGridTypeProduct.SelectedItem.Key.ToInt() == 229 || TkGridTypeProduct.SelectedItem.Key.ToInt() == 225 || TkGridTypeProduct.SelectedItem.Key.ToInt() == 227)
                        {
                            ValidateBoxedPartitionSize();
                        }
                    },
                },
                new FormHelperField()
                {
                    Path="QUANTITY_SECOND",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkGridQuantitySecond,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 2 },
                    },
                    OnTextChange = (FormHelperField field, string value) =>
                    {
                        CalculateSProduct();
                        TkGridQuantityNotchesFirst.Text = TkGridQuantitySecond.Text;
                        if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(225,229))
                        {
                            ValidateBoxedPartitionSize();
                        }
                    },
                },
                new FormHelperField()
                {
                    Path="QUANTITY_NOTCHES_SECOND",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkGridQuantityNotchesSecond,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 2 },
                    },
                    OnTextChange = (FormHelperField field, string value) =>
                    {
                        NotchesSecondLoadItems();
                        TkGridQuantityFirst.Text = TkGridQuantityNotchesSecond.Text;
                    },
                    Validate = (f,v)=>
                    {
                        int type = TkGridTypeProduct.SelectedItem.Key.ToInt();
                        if (type.ContainsIn(9, 225, 229))
                        {
                            if (TkGridQuantityNotchesSecond.Text.ToInt() > 18)
                            {
                                f.ValidateResult = false;
                                f.ValidateProcessed = true;
                                f.ValidateMessage = "Количество просечек не должно превышать 18";
                            }
                        }
                    }
                },

                // Заготовка 1
                new FormHelperField()
                {
                    Path="BILLET_CALCULATE_TWO_FIRST",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=TkBilletCalculateTwoFirst,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {
                        CheckCalculateTwo("1");
                    },
                },
                new FormHelperField()
                {
                    Path="BILLET_NAME_FIRST",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TkBilletNameFirst,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="BILLET_LENGTH_FIRST",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkBilletLegthFirst,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.MaxLen, 5 },
                    },
                    OnTextChange = (FormHelperField field, string value) =>
                    {
                        string s = "1";
                        CalculateBilletSquare(s);
                    },
                    Validate = (f,v)=>
                    {
                        if (TkBilletLegthFirst.Text.ToInt() > 0)
                        {
                            if (TkBilletLegthFirst.Text.ToInt() < BlankLengthMin)
                            {
                                f.ValidateResult = false;
                                f.ValidateProcessed = true;
                                f.ValidateMessage = "Длина заготовки меньше допустимой";
                            }
                        }
                    }
                },
                new FormHelperField()
                {
                    Path="BILLET_WIDTH_FIRST",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkBilletWidthFirst,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.MaxLen, 5 },
                    },
                    OnTextChange = (FormHelperField field, string value) =>
                    {
                        string s = "1";
                        CalculateBilletSquare(s);
                    },
                    Validate = (f,v)=>
                    {
                        if ((TkGridTypeProduct.SelectedItem.Key.ToInt() == 14 || TkGridTypeProduct.SelectedItem.Key.ToInt() == 15 || TkGridTypeProduct.SelectedItem.Key.ToInt() == 226)
                            && TkGridQuantityCrease.Text.ToInt()>0)
                        {
                            int min = 0;
                            int max = 1800;
                            switch (TkProductionScheme.SelectedItem.Key.ToInt())
                            {
                                case 602:
                                    min = 356;
                                    max = 1800;
                                    break;
                                case 122:
                                    min = 500;
                                    max = 1180;
                                    break;
                                default:
                                    break;
                            }
                            if ((TkBilletWidthFirst.Text.ToInt() > max || TkBilletWidthFirst.Text.ToInt() < min) && TkBilletWidthFirst.Text != "")
                            {
                                f.ValidateResult = false;
                                f.ValidateProcessed = true;
                                f.ValidateMessage = "Ширина заготовки должна находится в пределах от " + min + "мм до " + max + "мм";

                            }
                        }
                    }
                },
                new FormHelperField()
                {
                    Path="BILLET_SQUARE_FIRST",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=TkBilletSquareFirst,
                    ControlType="TextBox",
                    Format="N6",
                    OnTextChange = (FormHelperField field, string value) =>
                    {
                        CalculateSProduct();
                    },
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="BILLET_QUANTITY_FIRST1",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkBilletQuantityFirst1,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.MaxLen, 3 },
                    },
                    OnTextChange = (FormHelperField field, string value) =>
                    {
                        CalculateSProduct();
                    },
                },
                new FormHelperField()
                {
                    Path="BILLET_QUANTITY_FIRST2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkBilletQuantityFirst2,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.MaxLen, 3 },
                    },
                },

                // Заготовка 2
                new FormHelperField()
                {
                    Path="BILLET_CALCULATE_TWO_SECOND",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=TkBilletCalculateTwoSecond,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {
                        CheckCalculateTwo("2");
                    },
                },
                new FormHelperField()
                {
                    Path="BILLET_NAME_SECOND",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TkBilletNameSecond,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="BILLET_LENGTH_SECOND",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkBilletLegthSecond,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.MaxLen, 5 },
                    },
                    OnTextChange = (FormHelperField field, string value) =>
                    {
                        string s = "2";
                        CalculateBilletSquare(s);
                    },
                    Validate = (f,v)=>
                    {
                        if (TkBilletLegthSecond.Text.ToInt() > 0)
                        {
                            if (TkBilletLegthSecond.Text.ToInt() < BlankLengthMin)
                            {
                                f.ValidateResult = false;
                                f.ValidateProcessed = true;
                                f.ValidateMessage = "Длина заготовки меньше допустимой";
                            }
                        }
                    }
                },
                new FormHelperField()
                {
                    Path="BILLET_WIDTH_SECOND",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkBilletWidthSecond,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.MaxLen, 5 },
                    },
                    OnTextChange = (FormHelperField field, string value) =>
                    {
                        string s = "2";
                        CalculateBilletSquare(s);
                    },

                },
                new FormHelperField()
                {
                    Path="BILLET_SQUARE_SECOND",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=TkBilletSquareSecond,
                    ControlType="TextBox",
                    Format="N6",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="BILLET_QUANTITY_SECOND1",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkBilletQuantitySecond1,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.MaxLen, 3 },
                    },
                },
                new FormHelperField()
                {
                    Path="BILLET_QUANTITY_SECOND2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkBilletQuantitySecond2,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.MaxLen, 3 },
                    },
                },

                // Картон
                new FormHelperField()
                {
                    Path="PROFILE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkProfile,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {
                        GetBrandListByProfile();
                        CheckCardboardParametrs();
                        SetTkQuantityByProfile();
                    },
                },
                new FormHelperField()
                {
                    Path="BRAND",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkBrand,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {
                        CheckCardboardParametrs();
                    },
                },
                new FormHelperField()
                {
                    Path="COLLOR",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkCollor,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {
                        CheckCardboardParametrs();
                    },
                },
                new FormHelperField()
                {
                    Path="CARDBOARD",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkCardboard,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="SPECIAL_MATERIAL",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=TkSpecialMaterial,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                // Производство 
                new FormHelperField()
                {
                    Path="PRODUCTION_SCHEME",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkProductionScheme,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    Validate = (f,v)=>
                    {
                        if (TkGridTypeProduct.SelectedItem.Key.ToInt() == 14 || TkGridTypeProduct.SelectedItem.Key.ToInt() == 15 || TkGridTypeProduct.SelectedItem.Key.ToInt() == 226)
                        {
                            int min_h = 0;
                            int max_h = 1800;
                            int min_l = 0;
                            int max_l = 2500;
                            switch (TkProductionScheme.SelectedItem.Key.ToInt())
                            {
                                case 602:
                                    min_h = 97;
                                    max_h = 1800;
                                    min_l = 260;
                                    max_l = 1900;
                                    break;
                                case 122:
                                    min_h = 50;
                                    max_h = 1180;
                                    min_l = 0;
                                    max_l = 660;
                                    break;
                                default:
                                    break;
                            }
                            if ((TkGridHeightFirst.Text.ToInt() > max_h || TkGridHeightFirst.Text.ToInt() < min_h)
                                    && TkGridHeightFirst.Text != ""
                                || (TkGridLengthFirst.Text.ToInt() > max_l || TkGridLengthFirst.Text.ToInt() < min_l)
                                    && TkGridLengthFirst.Text !="")
                            {
                                // Красный
                                f.ValidateResult = false;
                                f.ValidateProcessed = true;
                                f.ValidateMessage = "Данная схема не подходит под заданные размеры";

                            }

                        }
                        else if(TkGridTypeProduct.SelectedItem.Key.ToInt() == 1)
                        {
                             f.ValidateResult = true;
                        }
                    },

                },
                new FormHelperField()
                {
                    Path="PRODUCTION_SCHEME2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkProductionScheme2,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="BILLET_SPRODUCT",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=TkBilletSProduct,
                    ControlType="TextBox",
                    Format="N6",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="BILLET_SPRODUCT2",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=TkBilletSProduct2,
                    ControlType="TextBox",
                    Format="N6",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                // Укладка 1
                new FormHelperField()
                {
                    Path="PALLET",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkPallet,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {
                    },
                },
                new FormHelperField()
                {
                    Path="LAYING_SCHEME",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkLayingScheme,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {
                        SetTkQuantityPack();
                        GetLayingSchemeImage();
                    },
                },
                new FormHelperField()
                {
                    Path="QUANTITY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkQuantity,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.MaxLen, 4 },
                    },
                    OnTextChange = (FormHelperField field, string value) =>
                    {
                        CalculateTkQuantityBox();
                    },
                },
                new FormHelperField()
                {
                    Path="QUANTITY_PACK",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkQuantityPack,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.MaxLen, 2 },
                    },
                    OnTextChange = (FormHelperField field, string value) =>
                    {
                        CalculateTkQuantityBox();
                    },
                },
                new FormHelperField()
                {
                    Path="QUANTITY_ROWS",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkQuantityRows,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.MaxLen, 2 },
                    },
                    OnTextChange = (FormHelperField field, string value) =>
                    {
                        CalculateTkQuantityBox();
                    },
                },
                new FormHelperField()
                {
                    Path="QUANTITY_BOX",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkQuantityBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },

                // Укладка 2
                new FormHelperField()
                {
                    Path="PALLET2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkPallet2,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {
                    },
                },
                new FormHelperField()
                {
                    Path="LAYING_SCHEME2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkLayingScheme2,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {
                        SetTkQuantityPack2();
                    },
                },
                new FormHelperField()
                {
                    Path="QUANTITY2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkQuantity2,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 4 },
                    },
                    OnTextChange = (FormHelperField field, string value) =>
                    {
                        CalculateTkQuantityBox2();
                    },
                },
                new FormHelperField()
                {
                    Path="QUANTITY_PACK2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkQuantityPack2,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 2 },
                    },
                    OnTextChange = (FormHelperField field, string value) =>
                    {
                        CalculateTkQuantityBox2();
                    },
                },
                new FormHelperField()
                {
                    Path="QUANTITY_ROWS2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkQuantityRows2,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 2 },
                    },
                    OnTextChange = (FormHelperField field, string value) =>
                    {
                        FlagChangeQuantityRows2 = true;
                        CalculateTkQuantityBox2();
                    },
                },
                new FormHelperField()
                {
                    Path="QUANTITY_BOX2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkQuantityBox2,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },

                // Упаковка 1
                new FormHelperField()
                {
                    Path="PREPRESSING",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=TkPrepressing,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {
                        TkPrepressing.Background = null;
                    }
                },
                new FormHelperField()
                {
                    Path="CORNERS",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=TkCorners,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ON_EDGE",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=OnEdgeCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {
                        OnEdgeCheckBox.Background = null;
                    }
                },
                new FormHelperField()
                {
                    Path="PACKAGING",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkPackaging,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="STRAPPING",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkStrapping,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TYPE_PACKAGE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    ControlType="RadioBox",
                    Control=TkTypePackage,
                    OnChange = (FormHelperField field, string value) =>
                    {
                        CheckTkTypePackage();
                    }
                },
                new FormHelperField()
                {
                    Path="PACKAGE_LENGTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkPackageLength,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 4 },
                    },
                },
                new FormHelperField()
                {
                    Path="PACKAGE_WIDTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkPackageWidth,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 4 },
                    },
                },
                new FormHelperField()
                {
                    Path="PACKAGE_HEIGTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkPackageHeigth,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.MaxLen, 4 },
                    },
                    Validate = (f,v)=>
                    {
                        if (TkPackageHeigth.Text.ToInt()>0 && TkMaxPackageHeigth.Text.ToInt()>0)
                        {
                            if (TkPackageHeigth.Text.ToInt()>TkMaxPackageHeigth.Text.ToInt())
                            {
                                // Красный
                                f.ValidateResult = false;
                                f.ValidateProcessed = true;
                                f.ValidateMessage = "Высота ТП не может превышать максимальную высоту ТП";

                            }

                        }
                    },
                },
                new FormHelperField()
                {
                    Path="MAX_PACKAGE_HEIGTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkMaxPackageHeigth,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.MaxLen, 4 },
                    },
                },

                // Упаковка 2
                new FormHelperField()
                {
                    Path="PREPRESSING2",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=TkPrepressing2,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {
                        TkPrepressing2.Background = null;
                    },
                },
                new FormHelperField()
                {
                    Path="CORNERS2",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=TkCorners2,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ON_EDGE2",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=OnEdge2CheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {
                        OnEdge2CheckBox.Background = null;
                    },
                },
                new FormHelperField()
                {
                    Path="PACKAGING2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkPackaging2,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="STRAPPING2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkStrapping2,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TYPE_PACKAGE2",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TkTypePackage2,
                    ControlType="RadioBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {
                        CheckTkTypePackage2();
                    },
                },
                new FormHelperField()
                {
                    Path="PACKAGE_LENGTH2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkPackageLength2,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 4 },
                    },
                },
                new FormHelperField()
                {
                    Path="PACKAGE_WIDTH2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkPackageWidth2,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 4 },
                    },
                },
                new FormHelperField()
                {
                    Path="PACKAGE_HEIGTH2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TkPackageHeigth2,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.MaxLen, 4 },
                    },
                    Validate = (f,v)=>
                    {
                        if (TkPackageHeigth2.Text.ToInt()>0 && TkMaxPackageHeigth.Text.ToInt()>0)
                        {
                            if (TkPackageHeigth2.Text.ToInt()>TkMaxPackageHeigth.Text.ToInt())
                            {
                                // Красный
                                f.ValidateResult = false;
                                f.ValidateProcessed = true;
                                f.ValidateMessage = "Высота ТП не может превышать максимальную высоту ТП";

                            }

                        }
                    },
                },
                
                // Примечания
                new FormHelperField()
                {
                    Path="NOTE_1_FOR_EXCEL",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Note1ForExcelTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NOTE_2_FOR_EXCEL",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Note2ForExcelTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NOTE_3_FOR_EXCEL",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Note3ForExcelTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                // Подтверждение производством
                new FormHelperField()
                {
                    Path="PRODUCTION_CONFIRM_PROCESSING",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=TkProductionConfirmProcessingCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCTION_CONFIRM_DATE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TkProductionConfirmDateTextBox,
                    ControlType="TextBox",
                    Format="dd.MM.yyyy",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="COMMENTS",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TkCommentTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                // Другое
                new FormHelperField()
                {
                    Path="FRAME_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="FRAME_ID2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

            };

            // Колбек стандартной валидации
            // Также вызывается при изменении данных в TextBox
            Form.OnValidate = (valid, message) =>
            {
                ValidateBilletSProduct();
                switch (TkGridTypeProduct.SelectedItem.Key.ToInt())
                {
                    case 100:
                    case 12:
                    case 229:
                    case 227:
                    case 225:
                    case 8:
                    case 9:
                    case 1:
                        CalculateSumNotches();
                        break;
                }
            };

            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
        }

        /// <summary>
        /// Инициализация грида просечек для первой решётки
        /// </summary>
        public void NotchesFirstGridInit()
        {
            {
                //список колонок грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Номер",
                        Path="NUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Расстояние",
                        Path="CONTENT",
                        ColumnType=ColumnTypeRef.Double,
                        MinWidth=60,
                        Format="N1",
                    },
                };

                NotchesFirstGrid.SetColumns(columns);
                NotchesFirstGrid.UseRowHeader = false;
                NotchesFirstGrid.SetSorting("NUMBER", ListSortDirection.Ascending);
                NotchesFirstGrid.PrimaryKey = "NUMBER";
                NotchesFirstGrid.AutoUpdateInterval = 0;

                NotchesFirstGrid.Init();

                NotchesFirstGrid.OnLoadItems = NotchesFirstGridLoadItems;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                NotchesFirstGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem.Count > 0)
                    {
                        SelectedNotchesFirstItem = selectedItem;
                    }
                };

                //двойной клик на строке откроет окно заполнения данных
                NotchesFirstGrid.OnDblClick = selectedItem =>
                {
                    NotchesFirstItemSetData(selectedItem);
                };
            }
        }

        /// <summary>
        /// Инициализация грида просечек для второй решётки
        /// </summary>
        public void NotchesSecondGridInit()
        {
            {
                //список колонок грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Номер",
                        Path="NUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Расстояние",
                        Path="CONTENT",
                        ColumnType=ColumnTypeRef.Double,
                        MinWidth=60,
                        Format="N1",
                    },
                };

                NotchesSecondGrid.SetColumns(columns);
                NotchesSecondGrid.UseRowHeader = false;
                NotchesSecondGrid.PrimaryKey = "NUMBER";
                NotchesSecondGrid.SetSorting("NUMBER", ListSortDirection.Ascending);
                NotchesSecondGrid.AutoUpdateInterval = 0;

                NotchesSecondGrid.Init();

                NotchesSecondGrid.OnLoadItems = NotchesSecondLoadItems;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                NotchesSecondGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem.Count > 0)
                    {
                        SelectedNotchesSecondItem = selectedItem;
                    }
                };

                //двойной клик на строке откроет окно заполнения данных
                NotchesSecondGrid.OnDblClick = selectedItem =>
                {
                    NotchesSecondItemSetData(selectedItem);
                };
            }
        }

        /// <summary>
        /// Инициализация грида рилёвок для прокладок
        /// </summary>
        public void CreaseGridInit()
        {
            {
                //список колонок грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Номер",
                        Path="NUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Расстояние",
                        Path="CONTENT",
                        ColumnType=ColumnTypeRef.Double,
                        MinWidth=60,
                        Format="N1",
                    },
                };

                CreaseGrid.SetColumns(columns);
                CreaseGrid.UseRowHeader = false;
                CreaseGrid.PrimaryKey = "NUMBER";
                CreaseGrid.SetSorting("NUMBER", ListSortDirection.Ascending);
                CreaseGrid.AutoUpdateInterval = 0;

                CreaseGrid.Init();

                CreaseGrid.OnLoadItems = CreaseGridLoadItems;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                CreaseGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem.Count > 0)
                    {
                        SelectedCreaseItem = selectedItem;
                    }
                };

                //двойной клик на строке откроет окно заполнения данных
                CreaseGrid.OnDblClick = selectedItem =>
                {
                    CreaseItemSetData(selectedItem);
                };
            }
        }


        #endregion

        #region Load

        /// <summary>
        /// Добавление строк в Грид просечек первой решётки по количеству в параметре Количество для первой решётки
        /// </summary>
        public void NotchesFirstGridLoadItems()
        {
            var row = TkGridQuantityNotchesFirst.Text.ToInt();
            DataSetOfNotchesFirst = new ListDataSet();

            for (int i = 1; i <= row; i++)
            {
                var dic = new Dictionary<string, string>();
                dic.Add("NUMBER", i.ToString());
                dic.Add("CONTENT", "");

                DataSetOfNotchesFirst.Items.Add(dic);
            }

            NotchesFirstGrid.UpdateItems(DataSetOfNotchesFirst);
            CalculateSumNotches();
        }

        /// <summary>
        /// Добавление строк в Грид просечек второй решётки по количеству в параметре Количество для второй решётки
        /// </summary>
        public void NotchesSecondLoadItems()
        {
            var row = TkGridQuantityNotchesSecond.Text.ToInt();
            DataSetOfNotchesSecond = new ListDataSet();

            for (int i = 1; i <= row; i++)
            {
                var dic = new Dictionary<string, string>();
                dic.Add("NUMBER", i.ToString());
                dic.Add("CONTENT", "");

                DataSetOfNotchesSecond.Items.Add(dic);
            }

            NotchesSecondGrid.UpdateItems(DataSetOfNotchesSecond);
            CalculateSumNotches();
        }

        /// <summary>
        /// Добавление строк в Грид рилёвок для прокладок по количеству в параметре Количество рилёвок
        /// </summary>
        public void CreaseGridLoadItems()
        {
            var row = TkGridQuantityCrease.Text.ToInt();
            DataSetOfCrease = new ListDataSet();

            for (int i = 1; i <= row; i++)
            {
                var dic = new Dictionary<string, string>();
                dic.Add("NUMBER", i.ToString());
                dic.Add("CONTENT", "");

                DataSetOfCrease.Items.Add(dic);
            }

            CreaseGrid.UpdateItems(DataSetOfCrease);
        }

        #endregion

        #region "Получение и обновление данных"
        /// <summary>
        /// Получение данных для сохранения тех карты
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> GetDataForSave()
        {
            Dictionary<string, string> formValues = new Dictionary<string, string>();
            if (Form != null)
            {
                if (Form.Validate())
                {
                    if (TkGridCustomer.SelectedItem.Key != null && TkGridClient.SelectedItem.Key != null)
                    {
                        if (string.IsNullOrEmpty(DataSetOfCustomer.Items.FirstOrDefault(x => x.CheckGet("ID") == TkGridCustomer.SelectedItem.Key).CheckGet("PATHTK_NEW")))
                        {
                            var msg = $"Внимание! У данного покупателя не указан путь к папке для сохранения новых Excel файлов.{Environment.NewLine}Продолжить?";
                            var d = new DialogWindow(msg, "ТК решётки", "", DialogWindowButtons.NoYes);
                            if (d.ShowDialog() == false)
                            {
                                return formValues;
                            }
                        }

                        formValues = Form.GetValues();

                        formValues.Add("CUSTOMER_NAME", TkGridCustomer.SelectedItem.Value.ToString());
                        formValues.Add("CUST_ID", TkGridCustomer.SelectedItem.Key.ToString());
                        formValues.Add("CLIENT_NAME", DataSetClientToCustomer.Items.FirstOrDefault(x => x.CheckGet("POKUPATEL_ID") == TkGridClient.SelectedItem.Key).CheckGet("POKUPATEL_NAME"));
                        formValues.CheckAdd("COLLOR_NAME", TkCollor.SelectedItem.Value.ToString());
                        formValues.CheckAdd("PROFILE_NAME", TkProfile.SelectedItem.Value.ToString());
                        formValues.CheckAdd("BRAND_NAME", TkBrand.SelectedItem.Value.ToString());
                        formValues.CheckAdd("PALLET_NAME", TkPallet.SelectedItem.Value.ToString());
                        if(TkProductionScheme?.SelectedItem != null && TkProductionScheme.SelectedItem.Value!=null)
                        {
                            formValues.CheckAdd("PRODUCTION_SCHEME_NAME", TkProductionScheme.SelectedItem.Value.ToString());
                        }
                        formValues.CheckAdd("PARTITION_TYPE", TkGridTypeExecution.SelectedItem.Key.ToInt().ToString());

                        if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(100,229))
                        {
                            formValues.CheckAdd("PALLET_NAME2", TkPallet2.SelectedItem.Value.ToString());
                            formValues.CheckAdd("TK_ID_SECOND", TechnologicalMapIDSecond.ToString());
                        }
                        else
                        {
                            formValues.CheckAdd("PALLET_NAME2", null);
                            formValues.CheckAdd("TK_ID_SECOND", null);
                        }
                        if (TkGridTypeProduct.SelectedItem.Key.ToInt() == 1)
                        {
                            formValues.CheckAdd("WIDTH_FIRST", TkGridOneNotch.Text.ToString());
                        }

                        formValues.CheckAdd("TK_ID_FIRST", TechnologicalMapIDFirst.ToString());
                        formValues.CheckAdd("TK_ID_SECOND", TechnologicalMapIDSecond.ToString());
                        formValues.CheckAdd("TK_SET", IdSet.ToString());
                        formValues.CheckAdd("ID_KOMPLEKT", ProductKomplektId.ToString());
                        formValues.CheckAdd("PRODUCT_ID2_FIRST", ProductFirstId.ToString());
                        formValues.CheckAdd("PRODUCT_ID2_SECOND", ProductSecondId.ToString());
                        formValues.CheckAdd("PATHTK_NEW", DataSetOfCustomer.Items.FirstOrDefault(x => x.CheckGet("ID") == TkGridCustomer.SelectedItem.Key).CheckGet("PATHTK_NEW"));
                        formValues.CheckAdd("PATHTK", PathTechnologicalMapNew);
                        formValues.CheckAdd("PATHTK_CONFIRM", DataSetOfCustomer.Items.FirstOrDefault(x => x.CheckGet("ID") == TkGridCustomer.SelectedItem.Key).CheckGet("PATHTK_CONFIRM"));
                        formValues.CheckAdd("PATHTK_ARCHIVE", DataSetOfCustomer.Items.FirstOrDefault(x => x.CheckGet("ID") == TkGridCustomer.SelectedItem.Key).CheckGet("PATHTK_ARCHIVE"));

                        formValues.CheckAdd("NAME_SET", SetNameTextBox.Text);

                        if (TkGridQuantityNotchesSecond.Text.ToInt() > 0 && NotchesFirstGrid?.Items?.Count > 0)
                        {
                            string listNotchesFirst = "";
                            foreach (var item in NotchesFirstGrid.Items)
                            {
                                var number = item.CheckGet("NUMBER");
                                var content = item.CheckGet("CONTENT");
                                if (content != "")
                                {
                                    var s = $"{number}:{content};";
                                    listNotchesFirst += s;
                                }
                            }

                            formValues.Add("LIST_NOTCHES_FIRST", listNotchesFirst);
                        }

                        if (TkGridQuantityNotchesSecond.Text.ToInt() > 0 && NotchesSecondGrid?.Items?.Count > 0)
                        {
                            string listNotchesSecond = "";
                            foreach (var item in NotchesSecondGrid.Items)
                            {
                                var number = item.CheckGet("NUMBER");
                                var content = item.CheckGet("CONTENT");
                                if (content != "")
                                {
                                    var s = $"{number}:{content};";
                                    listNotchesSecond += s;
                                }
                            }

                            formValues.Add("LIST_NOTCHES_SECOND", listNotchesSecond);
                        }

                        if (TkGridQuantityCrease.Text.ToInt() > 0 && CreaseGrid?.Items?.Count > 0)
                        {
                            string listCrease = "";
                            foreach (var item in CreaseGrid.Items)
                            {
                                var number = item.CheckGet("NUMBER");
                                var content = item.CheckGet("CONTENT");
                                if (content != "")
                                {
                                    if (content != "")
                                    {
                                        var s = $"{number}:{content};";
                                        listCrease += s;
                                    }
                                }
                            }
                            formValues.Remove("LIST_NOTCHES_FIRST");
                            formValues.Remove("QUANTITY_NOTCHES_FIRST");
                            var q_crease = formValues.CheckGet("QUANTITY_CREASE");
                            formValues.Add("LIST_NOTCHES_FIRST", listCrease);
                            formValues.Add("QUANTITY_NOTCHES_FIRST", q_crease);
                        }

                        // Для ТК Лист(стопы) в случае симметричных решеток
                        if (TkGridTypeProduct.SelectedItem.Key.ToInt()==1 && TkGridOneNotch.Text.ToInt() > 0)
                        {
                            string listCrease = "";
                            float notch2 = TkGridHeightFirst.Text.ToInt() - 2*TkGridOneNotch.Text.ToInt();
                            listCrease += $"1:{TkGridOneNotch.Text.ToInt()};";
                            listCrease += $"2:{notch2};";
                            listCrease += $"3:{TkGridOneNotch.Text.ToInt()};";
                            formValues.Remove("LIST_NOTCHES_FIRST");
                            formValues.Remove("QUANTITY_NOTCHES_FIRST");
                            var q_crease = formValues.CheckGet("QUANTITY_CREASE");
                            formValues.Add("LIST_NOTCHES_FIRST", listCrease);
                            formValues.Add("QUANTITY_NOTCHES_FIRST", "3");
                            }
                        }
                    else
                    {
                        var msg = "Заполните поля Покупатель и Потребитель";
                        var d = new DialogWindow($"{msg}", "Сохранение ТК", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    //var msg = "Проверьте корректность заполненных данных.";
                    var msg = $"Поля {Form.NotValidFileds} не валидны";
                    foreach (var t in Form.Fields)
                    {
                        if (t.Valid == false)
                        {
                            msg += t.Name + " не валидны";
                        }
                    }
                    var d = new DialogWindow($"{msg}", "Техкарта", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }

            return formValues;
        }

        /// <summary>
        /// Получение существующих данных по тех картам по Идентификатору комплекта
        /// </summary>
        /// <param name="setFormDataByDs">Флаг того, что поученные данные нужно записать я соответсвтующие поля</param>
        public void GetTechnologicalMapDataBySet(bool setFormDataByDs = true)
        {
            var p = new Dictionary<string, string>();
            p.Add("TK_SET", IdSet.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PartitionTechnologicalMap");
            q.Request.SetParam("Action", "GetData");

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
                    if (ds.Items.Count > 0)
                    {
                        DataSetOfExistingTechnologicalMap = ds;

                        if (setFormDataByDs)
                        {
                            SetFormDataByDs(DataSetOfExistingTechnologicalMap); 
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
        /// Получение существующих данных по тех карте по её Идентификатору
        /// </summary>
        /// <param name="setFormDataByDs">Флаг того, что поученные данные нужно записать я соответсвтующие поля</param>
        public void GetTechnologicalMapDataByTechnologicalMap(bool setFormDataByDs = true)
        {
            var p = new Dictionary<string, string>();
            p.Add("TECHNOLOGICAL_MAP_ID", TechnologicalMapIDFirst.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "GasketTechnologicalMap");
            q.Request.SetParam("Action", "GetData");

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
                    if (ds.Items.Count > 0)
                    {
                        DataSetOfExistingTechnologicalMap = ds;
                        if (setFormDataByDs)
                        {
                            SetFormDataByDs(DataSetOfExistingTechnologicalMap);
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
        /// Установка значений по тех карте из датасета в соответствующие ячейки
        /// </summary>
        /// <param name="dataSet">датасет с данными для заполнения</param>
        public void SetFormDataByDs(ListDataSet dataSet)
        {
            var dsFirst = dataSet.Items.First();

            TkGridTypeProduct.SetSelectedItemByKey(dsFirst.CheckGet("TYPE_PRODUCT"));
            TkGridNumberFirst.Text = dsFirst.CheckGet("NUMBER_FIRST");
            TkGridTypeExecution.SetSelectedItemByKey(dsFirst.CheckGet("PARTITION_TYPE"));
            CodeFirst = dsFirst.CheckGet("NUMBER_FIRST");
            TkGridNameFirst.Text = dsFirst.CheckGet("NAME_FIRST");
            TkGridNumberSecond.Text = dsFirst.CheckGet("NUMBER_SECOND");
            TkGridDetails.Text = dsFirst.CheckGet("DETAILS");
            CodeSecond = dsFirst.CheckGet("NUMBER_SECOND");
            TkGridNameSecond.Text = dsFirst.CheckGet("NAME_SECOND");
            TkGridClient.SetSelectedItemByKey(dsFirst.CheckGet("CLIENT"));

            if (dsFirst.CheckGet("CUSTOMER").ToInt() > 0)
            {
                TkGridCustomer.SetSelectedItemByKey(dsFirst.CheckGet("CUSTOMER"));
            }
            else
            {
                TkGridCustomer.SelectedItem = TkGridCustomer.Items.FirstOrDefault(x => x.Value == dsFirst.CheckGet("CUSTOMER_NAME"));
            }

            TkGridLengthFirst.Text = dsFirst.CheckGet("LENGTH_FIRST");
            TkGridHeightFirst.Text = dsFirst.CheckGet("HEIGHT_FIRST");
            TkGridQuantityNotchesFirst.Text = dsFirst.CheckGet("QUANTITY_NOTCHES_FIRST");
            TkGridLengthSecond.Text = dsFirst.CheckGet("LENGTH_SECOND");
            TkGridHeightSecond.Text = dsFirst.CheckGet("HEIGHT_SECOND");
            TkGridQuantityNotchesSecond.Text = dsFirst.CheckGet("QUANTITY_NOTCHES_SECOND");
            TkProfile.SetSelectedItemByKey(dsFirst.CheckGet("PROFILE"));
            TkBrand.SetSelectedItemByKey(dsFirst.CheckGet("BRAND"));
            TkCollor.SetSelectedItemByKey(dsFirst.CheckGet("COLLOR"));
            TkCardboard.SetSelectedItemByKey(dsFirst.CheckGet("CARDBOARD"));
            TkSpecialMaterial.IsChecked = dsFirst.CheckGet("SPECIAL_MATERIAL").ToBool();
            TkProductionScheme.SetSelectedItemByKey(dsFirst.CheckGet("PRODUCTION_SCHEME"));
            TkBilletSProduct.Text = dsFirst.CheckGet("BILLET_SPRODUCT").ToDouble().ToString();
            TkBilletLegthFirst.Text = dsFirst.CheckGet("BILLET_LENGTH_FIRST");
            TkBilletWidthFirst.Text = dsFirst.CheckGet("BILLET_WIDTH_FIRST");
            TkBilletSquareFirst.Text = dsFirst.CheckGet("BILLET_SQUARE_FIRST").ToDouble().ToString();

            TkBilletQuantityFirst1.Text = $"{dsFirst.CheckGet("BILLET_QUANTITY_FIRST1").ToInt() - dsFirst.CheckGet("BILLET_QUANTITY_FIRST2").ToInt()}";
            TkBilletQuantityFirst2.Text = dsFirst.CheckGet("BILLET_QUANTITY_FIRST2").ToInt().ToString();
            if (TkBilletQuantityFirst2.Text.ToInt() > 0)
            {
                TkBilletCalculateTwoFirst.IsChecked = true;
            }
            else
            {
                TkBilletCalculateTwoFirst.IsChecked = false;
            }
            CheckCalculateTwo("1");

            TkBilletLegthSecond.Text = dsFirst.CheckGet("BILLET_LENGTH_SECOND");
            TkBilletWidthSecond.Text = dsFirst.CheckGet("BILLET_WIDTH_SECOND");
            TkBilletSquareSecond.Text = dsFirst.CheckGet("BILLET_SQUARE_SECOND").ToDouble().ToString();

            TkBilletQuantitySecond2.Text = $"{dsFirst.CheckGet("BILLET_QUANTITY_SECOND2").ToInt() - dsFirst.CheckGet("BILLET_QUANTITY_SECOND1").ToInt()}";
            TkBilletQuantitySecond1.Text = dsFirst.CheckGet("BILLET_QUANTITY_SECOND1").ToInt().ToString();
            if (TkBilletQuantitySecond1.Text.ToInt() > 0)
            {
                TkBilletCalculateTwoSecond.IsChecked = true;
            }
            else
            {
                TkBilletCalculateTwoSecond.IsChecked = false;
            }
            CheckCalculateTwo("2");

            TkPallet.SetSelectedItemByKey(dsFirst.CheckGet("PALLET"));
            TkLayingScheme.SetSelectedItemByKey(dsFirst.CheckGet("LAYING_SCHEME"));
            if (dsFirst.CheckGet("QUANTITY").ToInt() > 0)
            {
                TkQuantity.Text = dsFirst.CheckGet("QUANTITY");
            }
            if (dsFirst.CheckGet("QUANTITY_PACK").ToInt() > 0)
            {
                TkQuantityPack.Text = dsFirst.CheckGet("QUANTITY_PACK");
            }
            else
            {
                TkQuantityPack.Text = "";
            }
            if (dsFirst.CheckGet("QUANTITY_ROWS").ToInt() > 0)
            {
                TkQuantityRows.Text = dsFirst.CheckGet("QUANTITY_ROWS");
            }

            TkQuantityBox.Text = dsFirst.CheckGet("QUANTITY_BOX");
            TkPrepressing.IsChecked = dsFirst.CheckGet("PREPRESSING").ToBool();
            TkCorners.IsChecked = dsFirst.CheckGet("CORNERS").ToBool();
            TkPackaging.SetSelectedItemByKey(dsFirst.CheckGet("PACKAGING"));
            TkStrapping.SetSelectedItemByKey(dsFirst.CheckGet("STRAPPING"));
            TkPackageLength.Text = dsFirst.CheckGet("PACKAGE_LENGTH");
            TkPackageWidth.Text = dsFirst.CheckGet("PACKAGE_WIDTH");
            TkPackageHeigth.Text = dsFirst.CheckGet("PACKAGE_HEIGTH");
            TkMaxPackageHeigth.Text = dsFirst.CheckGet("MAX_PACKAGE_HEIGTH");
            OnEdgeCheckBox.IsChecked = dsFirst.CheckGet("ON_EDGE").ToBool();
            Form.SetValueByPath("TYPE_PACKAGE", dsFirst.CheckGet("TYPE_PACKAGE"));
            OnEdgeCheckBox.Background = null;
            TkPrepressing.Background = null;
            Note1ForExcelTextBox.Text = dsFirst.CheckGet("NOTE_1_FOR_EXCEL");
            Note2ForExcelTextBox.Text = dsFirst.CheckGet("NOTE_2_FOR_EXCEL");
            Note3ForExcelTextBox.Text = dsFirst.CheckGet("NOTE_3_FOR_EXCEL");

            TkCommentTextBox.Text = dsFirst.CheckGet("COMMENTS");

            TkProductionConfirmDateTextBox.Text = dsFirst.CheckGet("PRODUCTION_CONFIRM_DATE");
            TkProductionConfirmProcessingCheckBox.IsChecked = dsFirst.CheckGet("PRODUCTION_CONFIRM_PROCESSING").ToInt() > 0;

            TkProductionScheme2.SetSelectedItemByKey(dsFirst.CheckGet("PRODUCTION_SCHEME2"));
            TkBilletSProduct2.Text = dsFirst.CheckGet("BILLET_SPRODUCT2").ToDouble().ToString();
            TkPallet2.SetSelectedItemByKey(dsFirst.CheckGet("PALLET2"));
            TkLayingScheme2.SetSelectedItemByKey(dsFirst.CheckGet("LAYING_SCHEME2"));
            TkQuantity2.Text = dsFirst.CheckGet("QUANTITY2");
            TkQuantityPack2.Text = dsFirst.CheckGet("QUANTITY_PACK2");
            TkQuantityRows2.Text = dsFirst.CheckGet("QUANTITY_ROWS2");
            TkQuantityBox2.Text = dsFirst.CheckGet("QUANTITY_BOX2");
            TkPrepressing2.IsChecked = dsFirst.CheckGet("PREPRESSING2").ToBool();
            TkCorners2.IsChecked = dsFirst.CheckGet("CORNERS2").ToBool();
            TkPackaging2.SetSelectedItemByKey(dsFirst.CheckGet("PACKAGING2"));
            TkStrapping2.SetSelectedItemByKey(dsFirst.CheckGet("STRAPPING2"));
            TkPackageLength2.Text = dsFirst.CheckGet("PACKAGE_LENGTH2");
            TkPackageWidth2.Text = dsFirst.CheckGet("PACKAGE_WIDTH2");
            TkPackageHeigth2.Text = dsFirst.CheckGet("PACKAGE_HEIGTH2");
            OnEdge2CheckBox.IsChecked = dsFirst.CheckGet("ON_EDGE2").ToBool();
            Form.SetValueByPath("TYPE_PACKAGE2", dsFirst.CheckGet("TYPE_PACKAGE2"));
            OnEdge2CheckBox.Background = null;
            TkPrepressing2.Background = null;

            TkBilletNameFirst.Text = dsFirst.CheckGet("BILLET_NAME_FIRST");
            TkBilletNameSecond.Text = dsFirst.CheckGet("BILLET_NAME_SECOND");
            TkTovarName.Text = dsFirst.CheckGet("TOVAR_NAME");
            TkTovarName2.Text = dsFirst.CheckGet("TOVAR_NAME2");

            TkGridQuantityFirst.Text = dsFirst.CheckGet("QUANTITY_FIRST");
            TkGridQuantitySecond.Text = dsFirst.CheckGet("QUANTITY_SECOND");

            Form.SetValueByPath("FRAME_ID", dsFirst.CheckGet("FRAME_ID"));
            Form.SetValueByPath("FRAME_ID2", dsFirst.CheckGet("FRAME_ID2"));

            if (!string.IsNullOrEmpty(dsFirst.CheckGet("NAME_SET")))
            {
                SetNameLable.Visibility = Visibility.Visible;
                SetNameTextBox.Visibility = Visibility.Visible;

                SetNameTextBox.Text = dsFirst.CheckGet("NAME_SET");
            }
            else
            {
                SetNameTextBox.Text = "";

                SetNameLable.Visibility = Visibility.Hidden;
                SetNameTextBox.Visibility = Visibility.Hidden;
            }

            if (!string.IsNullOrEmpty(dsFirst.CheckGet("SPECIAL_MATERIAL_NAME")))
            {
                TkSpecialMaterialName.Visibility = Visibility.Visible;
                TkSpecialMaterialName.Text = dsFirst.CheckGet("SPECIAL_MATERIAL_NAME");
            }
            else
            {
                TkSpecialMaterialName.Clear();
                TkSpecialMaterialName.Visibility = Visibility.Hidden;
            }
            if (!string.IsNullOrEmpty(dsFirst.CheckGet("ID_SET")))
            {
                IdSet = dsFirst.CheckGet("ID_SET").ToInt();
            }

            // Рисуем картинку укладки
            {
                //if (!string.IsNullOrEmpty(dsFirst.CheckGet("LAYING_SCHEME_IMAGE")))
                //{
                //    byte[] bytes = Convert.FromBase64String(dsFirst.CheckGet("LAYING_SCHEME_IMAGE"));
                //    var mem = new MemoryStream(bytes) { Position = 0 };
                //    var image = new BitmapImage();
                //    image.BeginInit();
                //    image.StreamSource = mem;
                //    image.EndInit();
                //    StackingImage.Source = image;
                //}
            }

            {
                List<Dictionary<string, string>> ListDicNotchesParametrsFirst = new List<Dictionary<string, string>>();

                var listNotchesFirst = dsFirst.CheckGet("LIST_NOTCHES_FIRST").ToString();
                string[] arrayStringNotchesFirst = listNotchesFirst.Split(';');

                foreach (var item in arrayStringNotchesFirst)
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        var dicStringNotchesFirstParametrs = new Dictionary<string, string>();

                        string[] arrayParametrsOfStringNotches = item.Split(':');

                        dicStringNotchesFirstParametrs.Add("NUMBER", arrayParametrsOfStringNotches[0]);
                        dicStringNotchesFirstParametrs.Add("CONTENT", arrayParametrsOfStringNotches[1].ToDouble().ToString());

                        ListDicNotchesParametrsFirst.Add(dicStringNotchesFirstParametrs);
                    }
                }

                ListDataSet dsNotchesFirst = new ListDataSet();

                dsNotchesFirst.Items = ListDicNotchesParametrsFirst;

                if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(8,14,15,226)
                     || (
                        TkGridTypeProduct.SelectedItem.Key.ToInt()==1
                        && dsFirst.CheckGet("TK_H").ToInt()==0
                     )
                )
                {
                    if (dsNotchesFirst.Items.Count != 0)
                    {
                        TkGridQuantityCrease.Text = dsNotchesFirst.Items.Count.ToString();
                        var s = 0.0;
                        foreach (var item in dsNotchesFirst.Items)
                        {
                            s += item.CheckGet("CONTENT").ToDouble();
                        }

                        DataSetOfCrease = dsNotchesFirst;
                        if (TkGridTypeProduct.SelectedItem.Key.ToInt() == 8)
                        {
                            TkGridLastCrease.Text = (TkGridLengthFirst.Text.ToDouble() - s).ToString();
                        }
                        else
                        {
                            TkGridLastCrease.Text = (TkGridHeightFirst.Text.ToDouble() - s).ToString();
                        }
                        CreaseGrid.UpdateItems(dsNotchesFirst);

                    }
                }
                else
                {
                    DataSetOfNotchesFirst = dsNotchesFirst;
                    NotchesFirstGrid.UpdateItems(dsNotchesFirst);
                }
            }

            {
                List<Dictionary<string, string>> ListDicNotchesParametrsSecond = new List<Dictionary<string, string>>();

                var listNotchesSecond = dsFirst.CheckGet("LIST_NOTCHES_SECOND").ToString();
                string[] arrayStringNotchesSecond = listNotchesSecond.Split(';');

                foreach (var item in arrayStringNotchesSecond)
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        var dicStringNotchesSecondParametrs = new Dictionary<string, string>();

                        string[] arrayParametrsOfStringNotches = item.Split(':');

                        dicStringNotchesSecondParametrs.Add("NUMBER", arrayParametrsOfStringNotches[0]);
                        dicStringNotchesSecondParametrs.Add("CONTENT", arrayParametrsOfStringNotches[1].ToDouble().ToString());

                        ListDicNotchesParametrsSecond.Add(dicStringNotchesSecondParametrs);
                    }
                }

                ListDataSet dsNotchesSecond = new ListDataSet();

                dsNotchesSecond.Items = ListDicNotchesParametrsSecond;

                DataSetOfNotchesSecond = dsNotchesSecond;

                NotchesSecondGrid.UpdateItems(dsNotchesSecond);
            }
            if (TkGridTypeProduct.SelectedItem.Key.ToInt() != 14 && TkGridTypeProduct.SelectedItem.Key.ToInt() != 15 && TkGridTypeProduct.SelectedItem.Key.ToInt() != 226)
            {
                CalculateSumNotches();
            }

            if (TkGridTypeProduct.SelectedItem.Key.ToInt() == 1)
            {
                TkGridOneNotch.Text = dsFirst.CheckGet("TK_H");
            }

            //Заполняем глобальные поля 
            {
                TechnologicalMapIDFirst = dsFirst.CheckGet("TK_ID_FIRST").ToInt();
                TechnologicalMapIDSecond = dsFirst.CheckGet("TK_ID_SECOND").ToInt();
                PathTechnologicalMapNew = dsFirst.CheckGet("PATHTK");

                BlankFirstId = dsFirst.CheckGet("ID2_FIRST").ToInt();
                BlankSecondId = dsFirst.CheckGet("ID2_SECOND").ToInt();
                ProductKomplektId = dsFirst.CheckGet("ID2_KOMPLEKT").ToInt();
                ProductFirstId = dsFirst.CheckGet("PRODUCT_ID2_FIRST").ToInt();
                ProductSecondId = dsFirst.CheckGet("PRODUCT_ID2_SECOND").ToInt();
            }
            SetButtons();
        }

        #endregion

        #region "Удаление данных"
        /// <summary>
        /// Очищаем наполнение селектбокса
        /// </summary>
        /// <param name="selectBox"></param>
        private void ClearSelectBox(SelectBox selectBox)
        {
            selectBox.DropDownListBox.Items.Clear();
            selectBox.DropDownListBox.SelectedItem = null;
            selectBox.ValueTextBox.Text = "";
            selectBox.Items = new Dictionary<string, string>();
            selectBox.SelectedItem = new KeyValuePair<string, string>();
        }

        #endregion


        #region "Размеры и рилёвки"
        
        /// <summary>
        /// Рассчитывает суммы просечек решёток, заполняет текстблок над гридом просечек, 
        /// сравнивает с длинной решётки и в зависимости от результата окрашивает текст текстблока.
        /// </summary>
        public void CalculateSumNotches()
        {
            // Расчёт суммы просечек первой решётки
            {
                double sum = 0;
                if (NotchesFirstGrid != null && NotchesFirstGrid.Items != null && NotchesFirstGrid.Items.Count > 0)
                {
                    foreach (var item in NotchesFirstGrid.Items)
                    {
                        sum += item.CheckGet("CONTENT").ToDouble();
                    }
                    // Дублируем первую просечку
                    sum += NotchesFirstGrid.Items.FirstOrDefault(x => x.CheckGet("NUMBER") == "1").CheckGet("CONTENT").ToDouble();
                    SumNotchesFirstTextBlock.Text = sum.ToString();

                    // Если сумма всех просечек решётки = длинне решётки
                    if (sum == TkGridLengthFirst.Text.ToDouble())
                    {
                        // Чёрный
                        var color = "#000000";
                        var brush = color.ToBrush();
                        SumNotchesFirstTextBlock.Foreground = brush;
                    }
                    else
                    {
                        // Красный
                        var color = "#ff0000";
                        var brush = color.ToBrush();
                        SumNotchesFirstTextBlock.Foreground = brush;
                    }
                }
            }

            // Расчёт суммы просечек второй решётки
            if(!TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(8,9))
            {
                double sum = 0;
                if (NotchesSecondGrid != null && NotchesSecondGrid.Items != null && NotchesSecondGrid.Items.Count > 0)
                {
                    foreach (var item in NotchesSecondGrid.Items)
                    {
                        sum += item.CheckGet("CONTENT").ToDouble();
                    }
                    // Дублируем первую просечку
                    sum += NotchesSecondGrid.Items.FirstOrDefault(x => x.CheckGet("NUMBER") == "1").CheckGet("CONTENT").ToDouble();
                    SumNotchesSecondTextBlock.Text = sum.ToString();

                    // Если сумма всех просечек решётки = длинне решётки
                    if (sum == TkGridLengthSecond.Text.ToDouble())
                    {
                        // Чёрный
                        var color = "#000000";
                        var brush = color.ToBrush();
                        SumNotchesSecondTextBlock.Foreground = brush;
                    }
                    else
                    {
                        // Красный
                        var color = "#ff0000";
                        var brush = color.ToBrush();
                        SumNotchesSecondTextBlock.Foreground = brush;
                    }
                }
            }
        }

        /// <summary>
        /// Расчет площади изделия 
        /// </summary>
        public void CalculateSProduct()
        {
            var type = TkGridTypeProduct.SelectedItem.Key.ToInt();

            if (type.ContainsIn(12,225))
            {
                if ((TkGridLengthFirst.Text.ToInt() > 0 && TkGridHeightFirst.Text.ToInt() > 0) || (TkGridLengthSecond.Text.ToInt() > 0 && TkGridHeightSecond.Text.ToInt() > 0))
                {
                    int lengthFirst;
                    int widthFirst;
                    int quantityFirst;

                    int lengthSecond;
                    int widthSecond;
                    int quantitySecond;

                    // Коэффициент преобразования размеров
                    int factor = 1000000;

                    double s;

                    lengthFirst = TkGridLengthFirst.Text.ToInt();
                    widthFirst = TkGridHeightFirst.Text.ToInt();
                    quantityFirst = TkGridQuantityFirst.Text.ToInt();

                    lengthSecond = TkGridLengthSecond.Text.ToInt();
                    widthSecond = TkGridHeightSecond.Text.ToInt();
                    quantitySecond = TkGridQuantitySecond.Text.ToInt();

                    // Продукция - комплект решёток, значит площадь продукциии - это суммарная площадь решёток, из которых состоит этот комплект
                    s = ((double)lengthFirst * (double)widthFirst * (double)quantityFirst / (double)factor) + ((double)lengthSecond * (double)widthSecond * (double)quantitySecond / (double)factor);

                    TkBilletSProduct.Text = s.ToString();
                }
                else
                {
                    TkBilletSProduct.Clear();
                }
            }
            else if (type.ContainsIn(14, 15, 8))
            {
                if (TkBilletSquareFirst.Text.ToDouble() > 0 && TkBilletQuantityFirst1.Text.ToInt() > 0)
                {
                    TkBilletSProduct.Text = Math.Round(TkBilletSquareFirst.Text.ToDouble() / TkBilletQuantityFirst1.Text.ToDouble(), 6).ToString();
                }
                else
                {
                    TkBilletSProduct.Clear();
                }
            }
            else
            {
                if (TkGridLengthFirst.Text.ToInt() > 0 && TkGridHeightFirst.Text.ToInt() > 0)
                {
                    int lengthFirst;
                    int widthFirst;
                    int quantityFirst;
                    // Коэффициент преобразования размеров
                    int factor = 1000000;
                    double s;

                    lengthFirst = TkGridLengthFirst.Text.ToInt();
                    widthFirst = TkGridHeightFirst.Text.ToInt();
                    quantityFirst = TkGridQuantityFirst.Text.ToInt();

                    // Продукция - одна решётка, значит площадь продукции - это площадь одной решётки
                    s = ((double)lengthFirst * (double)widthFirst / (double)factor);
                    TkBilletSProduct.Text = s.ToString();
                }
                else
                {
                    TkBilletSProduct.Clear();
                }

                if (TkGridLengthSecond.Text.ToInt() > 0 && TkGridHeightSecond.Text.ToInt() > 0)
                {
                    int lengthSecond;
                    int widthSecond;
                    int quantitySecond;
                    // Коэффициент преобразования размеров
                    int factor = 1000000;
                    double s;

                    lengthSecond = TkGridLengthSecond.Text.ToInt();
                    widthSecond = TkGridHeightSecond.Text.ToInt();
                    quantitySecond = TkGridQuantitySecond.Text.ToInt();

                    // Продукция - одна решётка, значит площадь продукции - это площадь одной решётки
                    s = ((double)lengthSecond * (double)widthSecond / (double)factor);
                    TkBilletSProduct2.Text = s.ToString();
                }
                else
                {
                    TkBilletSProduct2.Clear();
                }
            }
        }

        /// <summary>
        /// Установка размера просечки для выбранной строки в гриде просечек для первой решётки
        /// </summary>
        /// <param name="selectedItem"></param>
        public void NotchesFirstItemSetData(Dictionary<string, string> selectedItem)
        {
            foreach (var item in DataSetOfNotchesFirst.Items)
            {
                if (item.CheckGet("NUMBER") == selectedItem.CheckGet("NUMBER"))
                {
                    var content = item.CheckGet("CONTENT").ToDouble();
                    var notch = new NotchData(content);
                    if (TkGridTypeExecution.SelectedItem.Key.ToInt() == 1)
                    {
                        notch.PartitionType = 1;
                    }
                    else if (TkGridTypeExecution.SelectedItem.Key.ToInt() == 2)
                    {
                        notch.PartitionType = 2;
                    }
                    else
                    {
                        notch.PartitionType = 0;
                    }
                    notch.ProductClassId = TkGridTypeProduct.SelectedItem.Key.ToInt();
                    notch.Number = item.CheckGet("NUMBER").ToInt();
                    notch.GridNumber = 1;
                    notch.Show();
                }
            }
        }
        /// <summary>
        /// Установка размера просечки для выбранной строки в гриде просечек для второй решётки
        /// </summary>
        /// <param name="selectedItem"></param>
        public void NotchesSecondItemSetData(Dictionary<string, string> selectedItem)
        {
            foreach (var item in DataSetOfNotchesSecond.Items)
            {
                if (item.CheckGet("NUMBER") == selectedItem.CheckGet("NUMBER"))
                {
                    var content = item.CheckGet("CONTENT").ToDouble();
                    var notch = new NotchData(content);
                    if (TkGridTypeExecution.SelectedItem.Key.ToInt() == 1)
                    {
                        notch.PartitionType = 1;
                    }
                    else if (TkGridTypeExecution.SelectedItem.Key.ToInt() == 2)
                    {
                        notch.PartitionType = 2;
                    }
                    else
                    {
                        notch.PartitionType = 0;
                    }
                    notch.ProductClassId = TkGridTypeProduct.SelectedItem.Key.ToInt();
                    notch.Number = item.CheckGet("NUMBER").ToInt();
                    notch.GridNumber = 2;
                    notch.Show();
                }
            }
        }
        /// <summary>
        /// Установка размера рилёвки для выбранной строки в гриде рилёвок для прокладок
        /// </summary>
        /// <param name="selectedItem"></param>
        public void CreaseItemSetData(Dictionary<string, string> selectedItem)
        {
            foreach (var item in DataSetOfCrease.Items)
            {
                if (item.CheckGet("NUMBER") == selectedItem.CheckGet("NUMBER"))
                {
                    var content = item.CheckGet("CONTENT").ToDouble();
                    var notch = new NotchData(content);
                    notch.ProductClassId = TkGridTypeProduct.SelectedItem.Key.ToInt();
                    notch.Number = item.CheckGet("NUMBER").ToInt();
                    notch.GridNumber = 3;
                    notch.Show();
                }
            }
        }

        #endregion


        #region "Потребитель"
        private void LabelCustomer_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowCustomerData();
        }
        #endregion

        #region "Артикул"
        private void LabelArticul_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (LabelArticul.TextDecorations == TextDecorations.Underline)
            {
                string msg = "Получить артикул?";
                var d = new DialogWindow($"{msg}", "ТК решётки", "", DialogWindowButtons.NoYes);
                if (d.ShowDialog() == true)
                {
                    GetCode();
                }
            }
        }
        /// <summary>
        /// Получение первых 6 символов артикула
        /// </summary>
        public void GetCode()
        {
            if (TkGridCustomer.SelectedItem.Key.ToInt() > 0 && TkProfile.SelectedItem.Key.ToInt() > 0 && TkBrand.SelectedItem.Key.ToInt() > 0 && TkCollor.SelectedItem.Key.ToInt() > 0)
            {
                var p = new Dictionary<string, string>();
                p.Add("CUSTOMER_ID", TkGridCustomer.SelectedItem.Key.ToInt().ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "PartitionTechnologicalMap");
                q.Request.SetParam("Action", "GetCode");

                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            CodeSix = ds.Items.First().CheckGet("ARTIKUL");

                            if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(100, 229))
                            {
                                SetCodeForPartition(true);
                            }
                            else
                            {
                                SetCodeForSingleProduct(true);
                            }
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
            else
            {
                string msg = "Не все обязательные поля заполнены";
                var d = new DialogWindow($"{msg}", "Решётки в сборе", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Назначение артикулов решёткам
        /// </summary>
        public void SetCodeForPartition(bool setInTextBox = true)
        {
            // Первые 6 символов артикула (Код клиента. Код продукции клиента.).
            var codeFirst = $"{CodeSix}.";
            var codeSecond = $"{CodeSix}.";

            // Код изделия.
            codeFirst += "07.";
            codeSecond += "08.";

            // С печатью -- 1; Без печати -- 0.
            codeFirst += "0.";
            codeSecond += "0.";

            var idProf = TkProfile.SelectedItem.Key.ToString();

            // Профиль картона.
            switch (idProf)
            {
                case "1":
                    codeFirst += "0В.";
                    codeSecond += "0В.";
                    break;

                case "2":
                    codeFirst += "0С.";
                    codeSecond += "0С.";
                    break;

                case "3":
                    codeFirst += "0П.";
                    codeSecond += "0П.";
                    break;

                case "4":
                    codeFirst += "0Е.";
                    codeSecond += "0Е.";
                    break;

                case "5":
                    codeFirst += "05.";
                    codeSecond += "05.";
                    break;

                case "6":
                    codeFirst += "06.";
                    codeSecond += "06.";
                    break;

                case "7":
                    codeFirst += "07.";
                    codeSecond += "07.";
                    break;

                case "8":
                    codeFirst += "08.";
                    codeSecond += "08.";
                    break;

                case "9":
                    codeFirst += "09.";
                    codeSecond += "09.";
                    break;

                case "10":
                    codeFirst += "10.";
                    codeSecond += "10.";
                    break;

                case "11":
                    codeFirst += "11.";
                    codeSecond += "11.";
                    break;

                case "12":
                    codeFirst += "6К.";
                    codeSecond += "6К.";
                    break;

                case "13":
                    codeFirst += "8К.";
                    codeSecond += "8К.";
                    break;

                default:
                    break;
            }

            // Цвет картона.
            var idOuter = TkCollor.SelectedItem.Key.ToString();
            codeFirst += $"{idOuter}.";
            codeSecond += $"{idOuter}.";

            // Марка картона.
            var shortName = TkBrand.SelectedItem.Value.ToString();
            codeFirst += shortName;
            codeSecond += shortName;

            // Заполняем артикул в таблице t.Tk
            {
                var p = new Dictionary<string, string>();
                p.Add("NUMBER_FIRST", $"{codeFirst}");
                p.Add("NUMBER_SECOND", $"{codeSecond}");
                p.Add("TK_ID_FIRST", TechnologicalMapIDFirst.ToString());
                p.Add("TK_ID_SECOND", TechnologicalMapIDSecond.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "PartitionTechnologicalMap");
                q.Request.SetParam("Action", "SetCode");

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

                        CodeFirst = ds.Items.First().CheckGet("NUMBER_FIRST");
                        CodeSecond = ds.Items.First().CheckGet("NUMBER_SECOND");

                        if (!string.IsNullOrEmpty(CodeFirst))
                        {
                            string msg = "Артикул успешно получен. \nНе забудьте перенести файл техкарты в рабочую папку!";
                            var d = new DialogWindow($"{msg}", "Решётки в сборе", "", DialogWindowButtons.OK);
                            d.ShowDialog();

                            if (setInTextBox)
                            {
                                TkGridNumberFirst.Text = codeFirst;
                                TkGridNumberSecond.Text = codeSecond;
                            }
                        }
                        else
                        {
                            string msg = "Ошибка получения артикула.";
                            var d = new DialogWindow($"{msg}", "Решётки в сборе", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                    else
                    {
                        string msg = "Ошибка получения артикула.";
                        var d = new DialogWindow($"{msg}", "Решётки в сборе", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            SetButtons();
        }

        public void SetCodeForSingleProduct(bool setInTextBox = true)
        {
            // Первые 6 символов артикула (Код клиента. Код продукции клиента.).
            var code = $"{CodeSix}.";

            // Код изделия.
            switch (TkGridTypeProduct.SelectedItem.Key)
            {
                case "15":
                    code += "14.";
                    break;

                case "14":
                    code += "15.";
                    break;

                case "225":
                case "12":
                    code += "12.";
                    break;
                case "8":
                    code += "07.";
                    break;
                case "1":
                    code += "00.";
                    break;
                default:
                    break;
            }

            // С печатью -- 1; Без печати -- 0.
            code += "0.";

            var idProf = TkProfile.SelectedItem.Key.ToString();
            // Профиль картона.
            switch (idProf)
            {
                case "1":
                    code += "0В.";
                    break;

                case "2":
                    code += "0С.";
                    break;

                case "3":
                    code += "0П.";
                    break;

                case "4":
                    code += "0Е.";
                    break;

                case "5":
                    code += "05.";
                    break;

                case "6":
                    code += "06.";
                    break;

                case "7":
                    code += "07.";
                    break;

                case "8":
                    code += "08.";
                    break;

                case "9":
                    code += "09.";
                    break;

                case "10":
                    code += "10.";
                    break;

                case "11":
                    code += "11.";
                    break;

                case "12":
                    code += "6К.";
                    break;

                case "13":
                    code += "8К.";
                    break;

                default:
                    break;
            }

            // Цвет картона.
            var idOuter = TkCollor.SelectedItem.Key.ToString();
            code += $"{idOuter}.";

            // Марка картона.
            var shortName = TkBrand.SelectedItem.Value.ToString();
            code += shortName;

            // Заполняем артикул в таблице t.Tk
            {
                var p = new Dictionary<string, string>();
                p.Add("CODE", $"{code}");
                p.Add("TECHNOLOGICAL_MAP_ID", TechnologicalMapIDFirst.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "GasketTechnologicalMap");
                q.Request.SetParam("Action", "SetCode");

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
                        CodeFirst = ds.Items.First().CheckGet("NUMBER_FIRST");
                        if (!string.IsNullOrEmpty(CodeFirst))
                        {
                            string msg = "Артикул успешно получен. \nНе забудьте перенести файл техкарты в рабочую папку!";
                            var d = new DialogWindow($"{msg}", "Артикул техкарты", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                            if (setInTextBox)
                            {
                                TkGridNumberFirst.Text = code;
                            }

                        }
                        else
                        {
                            string msg = "Ошибка получения артикула.";
                            var d = new DialogWindow($"{msg}", "Артикул техкарты", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                    else
                    {
                        string msg = "Ошибка получения артикула.";
                        var d = new DialogWindow($"{msg}", "Артикул техкарты", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            SetButtons();
        }
        #endregion

        #region "Заготовки"
        private void ReSaveBilletClick()
        {
            string msg = $"Пересоздать заготовку и внести в ассортимент?";
            if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(12, 100))
            {
                msg = $"{msg}{Environment.NewLine}Пожалуйста, проверьте правильность заполнения полей количества решёток в заготовке.";
            }
            if (!TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(225, 229))
            {
                msg = $"{msg}{Environment.NewLine}Если вы меняли данные по заготовкам, пожалуйста не забудьте пересохранить тех карту.";
            }

            var d = new DialogWindow($"{msg}", "Создание заготовки", "", DialogWindowButtons.NoYes);
            if (d.ShowDialog() == true)
            {
                AddFilterForBillet();
                var values = GetDataForConfirmNew();
                if (Form.Valid)
                {
                    if (values.Count > 0)
                    {
                        SaveBillet(values);
                    }
                }
                RemoveFilterForBillet();
            }
        }
        private void SaveBilletButtonClick()
        {
            string msg = $"Внести заготовку в ассортимент?";
            if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(12,100))
            {
                msg = $"{msg}{Environment.NewLine}Пожалуйста, проверьте правильность заполнения полей количества решёток в заготовке.";
            }
            if (!TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(225, 229))
            {
                msg = $"{msg}{Environment.NewLine}Если вы меняли данные по заготовкам, пожалуйста не забудьте пересохранить тех карту.";
            }
            

            var d = new DialogWindow($"{msg}", "Создание заготовки", "", DialogWindowButtons.NoYes);
            if (d.ShowDialog() == true)
            {
                AddFilterForBillet();
                var values = GetDataForConfirmNew();
                if (values.Count > 0)
                {
                    SaveBillet(values);
                }
                RemoveFilterForBillet();
            }
        }
        /// <summary>
        /// Создание записей в таблице t.Tovar для заготовок
        /// </summary>
        /// <param name="p"></param>
        public async void SaveBillet(Dictionary<string, string> p)
        {
            if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(8,12,14,15,100,225,229))
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "TechnologicalMap");
                q.Request.SetParam("Action", "SaveBillet");
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
                        BlankFirstId = ds.Items.First().CheckGet("ID2_FIRST").ToInt();
                        BlankSecondId = ds.Items.First().CheckGet("ID2_SECOND").ToInt();
                        TkBilletNameFirst.Text = ds.Items.First().CheckGet("BILLET_NAME_FIRST");
                        TkBilletNameSecond.Text = ds.Items.First().CheckGet("BILLET_NAME_SECOND");
                        if (BlankFirstId > 0)
                        {
                            var msg = "Успешное создание заготовок";
                            var d = new DialogWindow($"{msg}", "Создание заготовок", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                    else
                    {
                        var msg = "Ошибка создания заготовки";
                        var d = new DialogWindow($"{msg}", "ТК прокладки", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }

                SetButtons();

            }
        }
        
        #endregion


        #region "Товар"
        /// <summary>
        /// Новый вариант подтверждения тех карты
        /// </summary>
        public async void Confirm(Dictionary<string, string> p)
        {
            if (TechnologicalMapIDFirst > 0)
        {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "TechnologicalMap");
                q.Request.SetParam("Action", "Confirm");
                q.Request.SetParams(p);
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                ConfirmButton.IsEnabled = false;
                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {

                    ConfirmButton.IsEnabled = true;
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");

                        BlankFirstId = ds.Items.First().CheckGet("ID2_FIRST").ToInt();
                        BlankSecondId = ds.Items.First().CheckGet("ID2_SECOND").ToInt();
                        ProductKomplektId = ds.Items.First().CheckGet("ID2_KOMPLEKT").ToInt();
                        ProductFirstId = ds.Items.First().CheckGet("PRODUCT_ID2_FIRST").ToInt();
                        ProductSecondId = ds.Items.First().CheckGet("PRODUCT_ID2_SECOND").ToInt();
                        TLSFirstId = ds.Items.First().CheckGet("ID_TLS_FIRST").ToInt();
                        TLSSecondId = ds.Items.First().CheckGet("ID_TLS_SECOND").ToInt();
                        TkTovarName.Text = ds.Items.First().CheckGet("TOVAR_NAME");
                        TkTovarName2.Text = ds.Items.First().CheckGet("TOVAR_NAME2");
                        if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(8, 14, 15))
                        {
                            ProductFirstId = ds.Items.First().CheckGet("ID2_KOMPLEKT").ToInt();
                        }


                        // Отправляем сообщение вкладке Решётки в сборе о необходимости обновить грид
                        {
                            var mes = new Dictionary<string, string>()
                            {
                                {"ID_TK", TechnologicalMapIDFirst.ToString() },
                                {"CUST_ID", (TkGridCustomer?.SelectedItem as dynamic)?.Key?.ToString() ?? "-1"},
                                {"TYPE_PRODUCT", (TkGridTypeProduct?.SelectedItem as dynamic)?.Key?.ToString() ?? "-1"},
                            };

                            var m = JsonConvert.SerializeObject(mes);

                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup = "Preproduction",
                                ReceiverName = "TechnologicalMapList",
                                SenderName = "TechnologicalMap",
                                Action = "Refresh",
                                Message = m,
                            }
                            );
                        }

                        var msg = "Изделие успешно внесено в ассортимент.";
                        var d = new DialogWindow($"{msg}", "Создание изделия", "", DialogWindowButtons.OK);
                        d.ShowDialog();

                    }
                }
                else
                {
                    ConfirmButton.IsEnabled = true;
                    q.ProcessError();
                }
            }

            SetButtons();
        }
        private void ConfirmButtonClick()
        {
            string msg = "Внести в асортимент?";
            var d = new DialogWindow($"{msg}", "Создание товара ТК", "", DialogWindowButtons.NoYes);
            if (d.ShowDialog() == true)
            {
                var values = GetDataForConfirmNew();
                if (values.Count > 0)
                {
                    Confirm(values);
                }
            }
        }

        #endregion

        #region "Производство"
        private void SendTkToProductionConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            SendTkToProductionConfirm();
        }
        /// <summary>
        /// Отправить тех карту на подтверждение производством
        /// </summary>
        public async void SendTkToProductionConfirm()
        {
            if (TechnologicalMapIDFirst > 0)
            {
                if (!(bool)TkProductionConfirmProcessingCheckBox.IsChecked)
                {
                    Dictionary<string, string> p = new Dictionary<string, string>();
                    p.Add("TK_ID_FIRST", TechnologicalMapIDFirst.ToString());
                    p.Add("TO_GILD", "1");

                    var q = new LPackClientQuery();

                    // Если продукция
                    // -- прокладки
                    if (TkGridTypeProduct.SelectedItem.Key.ToInt() == 14
                        || TkGridTypeProduct.SelectedItem.Key.ToInt() == 15
                        || TkGridTypeProduct.SelectedItem.Key.ToInt() == 226)
                    {
                        q.Request.SetParam("Module", "Preproduction");
                        q.Request.SetParam("Object", "GasketTechnologicalMap");
                        q.Request.SetParam("Action", "SendToProductionConfirm");
                    }
                    // Если продукция
                    // -- комплект решёток в сборе
                    // -- комплект решёток не в сборе
                    else if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(12,100,225,229))
                    {
                        q.Request.SetParam("Module", "Preproduction");
                        q.Request.SetParam("Object", "PartitionTechnologicalMap");
                        q.Request.SetParam("Action", "SendToProductionConfirm");

                        p.Add("TK_ID_SECOND", TechnologicalMapIDSecond.ToString());
                    }

                    q.Request.SetParams(p);
                    q.Request.Timeout = 30000;
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
                                TkProductionConfirmProcessingCheckBox.IsChecked = true;

                                var productionConfirmDate = ds.Items.First().CheckGet("PRODUCTION_CONFIRM_DATE");
                                if (!string.IsNullOrEmpty(productionConfirmDate))
                                {
                                    TkProductionConfirmDateTextBox.Text = productionConfirmDate;
                                }

                                // Отправляем сообщение вкладке Решётки в сборе о необходимости обновить грид
                                {
                                    var mes = new Dictionary<string, string>()
                                    {
                                        {"ID_TK", TechnologicalMapIDFirst.ToString() },
                                        {"CUST_ID", (TkGridCustomer?.SelectedItem as dynamic)?.Key?.ToString() ?? "-1"},
                                        {"TYPE_PRODUCT", (TkGridTypeProduct?.SelectedItem as dynamic)?.Key?.ToString() ?? "-1"},
                                    };

                                    var m = JsonConvert.SerializeObject(mes);

                                    Messenger.Default.Send(new ItemMessage()
                                    {
                                        ReceiverGroup = "Preproduction",
                                        ReceiverName = "TechnologicalMapList",
                                        SenderName = "TechnologicalMap",
                                        Action = "Refresh",
                                        Message = "",
                                    }
                                    );
                                }
                            }
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }

                    SetButtons();
                }
                else
                {
                    var msg = "Тех карта уже отправлена на подтверждение производством.";
                    var d = new DialogWindow($"{msg}", "ТК", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                var msg = "Необходимо сохранить тех карту перед отправкой на подтверждение производством.";
                var d = new DialogWindow($"{msg}", "ТК", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        #endregion

        #region "Упаковка"
        private void CalculatePackingButton_Click(object sender, RoutedEventArgs e)
        {
            CalculatePacking();
        }

        private void CalculatePacking2Button_Click(object sender, RoutedEventArgs e)
        {
            CalculatePacking2();
        }
        /// <summary>
        /// Автоматический расчёт оптимального поддона и укладки на поддон
        /// </summary>
        public void CalculatePacking()
        {
            // Если не укладка на ребро, то можем рассчитывать
            if ((bool)OnEdgeCheckBox.IsChecked == false)
            {
                // Получение оптимального поддона и укладки на поддон через функции БД
                var p = new Dictionary<string, string>();
                p.Add("LENGTH", Form.GetValueByPath("LENGTH_FIRST"));
                p.Add("HEIGHT", Form.GetValueByPath("HEIGHT_FIRST"));
                p.Add("TYPE_PRODUCT", Form.GetValueByPath("TYPE_PRODUCT"));
                p.Add("TYPE_PACKAGE", Form.GetValueByPath("TYPE_PACKAGE"));

                if (p.FirstOrDefault(x => x.Value.IsNullOrEmpty()).Key != null)
                {
                    var msg = "Заполните поля Длина, Ширина/Высота, Вид изделия и Вариант отгрузки.";
                    var d = new DialogWindow($"{msg}", "ТК решётки", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
                else
                {
                    p.Add("S", TkBilletSProduct.Text);
                    p.Add("IDTSCHEME", Form.GetValueByPath("PRODUCTION_SCHEME"));
                    p.Add("ID_PROF", Form.GetValueByPath("PROFILE"));
                    p.Add("IDC", Form.GetValueByPath("CARDBOARD"));
                    if (TkPallet != null && TkPallet.SelectedItem.Key.ToInt() > 0)
                    {
                        p.Add("ID_VAR", TkPallet.SelectedItem.Key.ToInt().ToString());
                    }
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Preproduction");
                    q.Request.SetParam("Object", "PartitionTechnologicalMap");
                    q.Request.SetParam("Action", "GetDefaultPacking");
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
                            if (ds != null && ds.Items != null && ds.Items.Count > 0 )
                            {
                                if (ds.Items.First().CheckGet("PALLET") != "0" && !ds.Items.First().CheckGet("PALLET").IsNullOrEmpty())
                                {
                                    TkPallet.SetSelectedItemByKey(ds.Items.First().CheckGet("PALLET"));
                                }
                                if (ds.Items.First().CheckGet("LAYING_SCHEME") != "0" && !ds.Items.First().CheckGet("LAYING_SCHEME").IsNullOrEmpty())
                                {
                                    TkLayingScheme.SetSelectedItemByKey(ds.Items.First().CheckGet("LAYING_SCHEME"));
                                }
                                if (ds.Items.First().CheckGet("STACK_QTY") != "0" && !ds.Items.First().CheckGet("STACK_QTY").IsNullOrEmpty())
                                {
                                    TkQuantity.Text = ds.Items.First().CheckGet("STACK_QTY");
                                }
                                if (ds.Items.First().CheckGet("QTY") != "0" && !ds.Items.First().CheckGet("QTY").IsNullOrEmpty())
                                {
                                    TkQuantity.Text = "";
                                    TkQuantityPack.Text = "";
                                    TkQuantityRows.Text = "";
                                    TkQuantityBox.Text = ds.Items.First().CheckGet("QTY");
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
            else
            {
                var msg = "Выбрана укладка на ребро. Запрещено автоматически рассчитывать укладку.";
                var d = new DialogWindow($"{msg}", "ТК решётки", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Автоматический расчёт оптимального поддона и укладки на поддон
        /// (для второй решётки для типа продукции Решётки не в сборе)
        /// </summary>
        public void CalculatePacking2()
        {
            // Если не укладка на ребро, то можем рассчитывать
            if ((bool)OnEdge2CheckBox.IsChecked == false)
            {
                // Получение оптимального поддона и укладки на поддон через функции БД
                var p = new Dictionary<string, string>();
                p.Add("TYPE_PACKAGE", Form.GetValueByPath("TYPE_PACKAGE2"));
                p.Add("TYPE_PRODUCT", Form.GetValueByPath("TYPE_PRODUCT"));
                p.Add("LENGTH", Form.GetValueByPath("LENGTH_SECOND"));
                p.Add("HEIGHT", Form.GetValueByPath("HEIGHT_SECOND"));

                if (p.FirstOrDefault(x => x.Value.IsNullOrEmpty()).Key != null)
                {
                    var msg = "Заполните поля Длина решётки, Высота решётки, Вид изделия и Вариант отгрузки.";
                    var d = new DialogWindow($"{msg}", "ТК решётки", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
                else
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Preproduction");
                    q.Request.SetParam("Object", "PartitionTechnologicalMap");
                    q.Request.SetParam("Action", "GetDefaultPacking");
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
                                if (ds.Items.First().CheckGet("PALLET") != "0" && !ds.Items.First().CheckGet("PALLET").IsNullOrEmpty())
                                {
                                    TkPallet2.SetSelectedItemByKey(ds.Items.First().CheckGet("PALLET"));
                                }
                                if (ds.Items.First().CheckGet("LAYING_SCHEME") != "0" && !ds.Items.First().CheckGet("LAYING_SCHEME").IsNullOrEmpty())
                                {
                                    TkLayingScheme2.SetSelectedItemByKey(ds.Items.First().CheckGet("LAYING_SCHEME"));
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
            else
            {
                var msg = "Выбрана укладка на ребро. Запрещено автоматически рассчитывать укладку.";
                var d = new DialogWindow($"{msg}", "ТК решётки", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        #endregion

        #region "Картон"
        /// <summary>
        /// Наполнение селектбокса марка картона по выбранному профилю картона
        /// </summary>
        public async void GetBrandListByProfile()
        {
            var p = new Dictionary<string, string>();
            p.Add("ID_PROF", TkProfile.SelectedItem.Key);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PartitionTechnologicalMap");
            q.Request.SetParam("Action", "GetMarkByProfile");

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
                    //Обновление данных для селектбокса Марка картона
                    var ds = ListDataSet.Create(result, "MARKS");
                    TkBrand.SetItems(ds, "ID", "NAME");
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        #endregion

        #region "Файл Excel"
       
        /// <summary>
        /// Создание нового эксель файла тех карты
        /// </summary>
        public async void ExcelDocumentCreate(Dictionary<string, string> p)
        {
            if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(1,8,14,15,12,100,225,229))
            {
                var excel = new TechnologicalMapExcel(p);
                var result = excel.CreateExcelFile();
                var msg = result.CheckGet("MSG");

                if (msg.IsNullOrEmpty())
                {
                    var d = new DialogWindow("Успешное создание Excel файла техкарты", "Создание Excel", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                    PathTechnologicalMapNew = result.CheckGet("FILE_NAME");
                    SetButtons();
                }
                else
                {
                    var d = new DialogWindow(msg, "Создание Excel", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }

            }
            GetTypeExistFile();
        }

        /// <summary>
        /// Обновляет текстовые данные существующего эксель файла тех карты
        /// </summary>
        public async void ExcelDocumentUpdate(Dictionary<string, string> p)
        {
            if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(1, 8, 14, 15, 12, 100, 225, 229))
            {
                var excel = new TechnologicalMapExcel(p);
                
                var values = GetDataForSave();
                Dictionary<string, string> firstData = new Dictionary<string, string>();
                
                if (values.Count > 0)
                {
                    if (DataSetOfExistingTechnologicalMap != null && DataSetOfExistingTechnologicalMap.Items != null && DataSetOfExistingTechnologicalMap.Items.Count > 0)
                    {
                        firstData = DataSetOfExistingTechnologicalMap.Items.First();
                    }
                }
                if (firstData != null && firstData.Count > 0)
                {
                    // Определяем необходимо ли переименовывать файл и менять картинки
                    if(TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(1,14, 15))
                    {
                        if (IdSet == 0 && PathTechnologicalMapNew != null && (
                            values.CheckGet("LENGTH_FIRST").ToInt() != firstData.CheckGet("LENGTH_FIRST").ToInt()
                            || values.CheckGet("HEIGHT_FIRST").ToInt() != firstData.CheckGet("HEIGHT_FIRST").ToInt()
                            || values.CheckGet("NAME_FIRST") != firstData.CheckGet("NAME_FIRST")
                            || values.CheckGet("TYPE_PACKAGE").ToInt() != firstData.CheckGet("TYPE_PACKAGE").ToInt()))
                        {
                            var d = new DialogWindow("Изменить наименование Excel файла?", "Перенос данных в Excel", "", DialogWindowButtons.NoYes);
                            if (d.ShowDialog() == true)
                            {
                                excel.FlagReameFile = true;
                            }
                            else
                            {
                                excel.FlagReameFile = false;
                            }
                        }
                        else
                        {
                            excel.FlagReameFile = false;
                        }
                        if (values.CheckGet("LENGTH_FIRST").ToInt() != firstData.CheckGet("LENGTH_FIRST").ToInt()
                        || values.CheckGet("HEIGHT_FIRST").ToInt() != firstData.CheckGet("HEIGHT_FIRST").ToInt()
                        || values.CheckGet("QUANTITY_NOTCHES_FIRST").ToInt() != firstData.CheckGet("QUANTITY_NOTCHES_FIRST").ToInt()
                        || values.CheckGet("LIST_NOTCHES_FIRST") != firstData.CheckGet("LIST_NOTCHES_FIRST")
                        )
                        {
                            excel.FlagRecreateExcelImg = true;
                        }
                        else
                        {
                            var d = new DialogWindow("Изменить картинку прокладки?", "Перенос данных в Excel", "", DialogWindowButtons.NoYes);
                            if (d.ShowDialog() == true)
                            {
                                excel.FlagRecreateExcelImg = true;
                            }
                            else
                            {
                                excel.FlagRecreateExcelImg = false;
                            }
                        }
                    }
                    if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(8, 12, 100, 225, 229))
                    {
                        if (IdSet == 0 && PathTechnologicalMapNew != null && 
                            (values.CheckGet("LENGTH_FIRST").ToInt() != firstData.CheckGet("LENGTH_FIRST").ToInt()
                            || values.CheckGet("HEIGHT_FIRST").ToInt() != firstData.CheckGet("HEIGHT_FIRST").ToInt()
                            || values.CheckGet("QUANTITY_FIRST").ToInt() != firstData.CheckGet("QUANTITY_FIRST").ToInt()
                            || values.CheckGet("LENGTH_SECOND").ToInt() != firstData.CheckGet("LENGTH_SECOND").ToInt()
                            || values.CheckGet("HEIGHT_SECOND").ToInt() != firstData.CheckGet("HEIGHT_SECOND").ToInt()
                            || values.CheckGet("QUANTITY_SECOND").ToInt() != firstData.CheckGet("QUANTITY_SECOND").ToInt()
                            || values.CheckGet("NAME_FIRST") != firstData.CheckGet("NAME_FIRST")
                            || values.CheckGet("TYPE_PACKAGE").ToInt() != firstData.CheckGet("TYPE_PACKAGE").ToInt()))
                        {
                            var d = new DialogWindow("Изменить наименование Excel файла?", "Перенос данных в Excel", "", DialogWindowButtons.NoYes);
                            if (d.ShowDialog() == true)
                            {
                                excel.FlagReameFile = true;
                            }
                            else
                            {
                                excel.FlagReameFile = false;
                            }
                        }
                        else
                        {
                            excel.FlagReameFile = false;
                        }

                        if (values.CheckGet("LENGTH_FIRST").ToInt() != firstData.CheckGet("LENGTH_FIRST").ToInt()
                        || values.CheckGet("HEIGHT_FIRST").ToInt() != firstData.CheckGet("HEIGHT_FIRST").ToInt()
                        || values.CheckGet("QUANTITY_NOTCHES_FIRST").ToInt() != firstData.CheckGet("QUANTITY_NOTCHES_FIRST").ToInt()
                        || values.CheckGet("LIST_NOTCHES_FIRST") != firstData.CheckGet("LIST_NOTCHES_FIRST"))
                        {
                            excel.FlagRecreateExcelImg = true;
                        }
                        else
                        {
                            var d = new DialogWindow("Изменить картинки решёток?", "Перенос данных в Excel", "", DialogWindowButtons.NoYes);
                            if (d.ShowDialog() == true)
                            {
                                excel.FlagRecreateExcelImg = true;
                                excel.FlagRecreateExcelImg2 = true;
                            }
                            else
                            {
                                excel.FlagRecreateExcelImg = false;
                                excel.FlagRecreateExcelImg2 = false;
                            }
                        }

                        if (values.CheckGet("LENGTH_SECOND").ToInt() != firstData.CheckGet("LENGTH_SECOND").ToInt()
                        || values.CheckGet("HEIGHT_SECOND").ToInt() != firstData.CheckGet("HEIGHT_SECOND").ToInt()
                        || values.CheckGet("QUANTITY_NOTCHES_SECOND").ToInt() != firstData.CheckGet("QUANTITY_NOTCHES_SECOND").ToInt()
                        || values.CheckGet("LIST_NOTCHES_SECOND") != firstData.CheckGet("LIST_NOTCHES_SECOND"))
                        {
                            excel.FlagRecreateExcelImg2 = true;
                        }
                    }
                    
                }

                var result = excel.RecreateExcelFile();
                var msg = result.CheckGet("MSG");

                if (msg.IsNullOrEmpty())
                {
                    PathTechnologicalMapNew = result.CheckGet("FILE_NAME");
                    
                    if (excel.FlagReameFile)
                    {
                        var d = new DialogWindow($"Эксель файл тех карты успешно пересохранён с новым названием" + Environment.NewLine + $"{PathTechnologicalMapNew}", "Перенос данных в Excel", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                    else
                    {
                        var d = new DialogWindow("Эксель файл тех карты успешно пересохранён со старым названием.", "Перенос данных в Excel", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    var d = new DialogWindow(msg, "Перенос данных в Excel", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
        }
        
        /// <summary>
        /// Открытие эксел файла тех карты
        /// </summary>
        public void OpenExcel()
        {
            if (!string.IsNullOrEmpty(PathTechnologicalMapNew))
            {
                var fullPathTkNew = "";
                var fullPathTk = "";
                var fullPathTkArchive = "";

                // Если файл новый
                {
                    fullPathTkNew += DataSetOfCustomer.Items.FirstOrDefault(x => x.CheckGet("ID") == TkGridCustomer.SelectedItem.Key).CheckGet("PATHTK_NEW");
                    fullPathTkNew += PathTechnologicalMapNew;
                }

                // Если файл уже в основной папке
                {
                    fullPathTk += DataSetOfCustomer.Items.FirstOrDefault(x => x.CheckGet("ID") == TkGridCustomer.SelectedItem.Key).CheckGet("PATHTK_CONFIRM");
                    fullPathTk += PathTechnologicalMapNew;
                }

                // Если файл уже в архиве
                {
                    fullPathTkArchive += DataSetOfCustomer.Items.FirstOrDefault(x => x.CheckGet("ID") == TkGridCustomer.SelectedItem.Key).CheckGet("PATHTK_ARCHIVE");
                    fullPathTkArchive += PathTechnologicalMapNew;
                }

                //путь к новому файлу для локальной отладки
                //fullPathTk = $"c:\\temp\\erp\\storage\\tk\\{PathTkNew}.xls";

                if (System.IO.File.Exists(fullPathTk))
                {
                    Central.OpenFile(fullPathTk);
                }
                else if (System.IO.File.Exists(fullPathTkNew))
                {
                    Central.OpenFile(fullPathTkNew);
                }
                else if (System.IO.File.Exists(fullPathTkArchive))
                {
                    Central.OpenFile(fullPathTkArchive);
                }
                else
                {
                    var msg = "Excel файл тех карты не найден";
                    var d = new DialogWindow($"{msg}", "ТК", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
        }

        /// <summary>
        /// Конвертация эксель файла в ПДФ
        /// </summary>
        public void ConvertExcelToPdf()
        {
            if (!string.IsNullOrEmpty(PathTechnologicalMapNew))
            {
                var fullPathTkNew = "";
                var fullPathTkConfirm = "";
                var fullPathTkArchive = "";
                var fullPathTk = "";

                // Локальный путь для сохранения нового пдф файла
                string fileFullLocalPathPdf = $"";
                // Серверный путь для сохранения нового пдф файла
                string fileFullGlobalPathPdf = $"";

                // Если файл новый
                {
                    fullPathTkNew += DataSetOfCustomer.Items.FirstOrDefault(x => x.CheckGet("ID") == TkGridCustomer.SelectedItem.Key).CheckGet("PATHTK_NEW");
                    fullPathTkNew += PathTechnologicalMapNew;
                }

                //Если файл уже в основной папке
                {
                    fullPathTkConfirm += DataSetOfCustomer.Items.FirstOrDefault(x => x.CheckGet("ID") == TkGridCustomer.SelectedItem.Key).CheckGet("PATHTK_CONFIRM");
                    fullPathTkConfirm += PathTechnologicalMapNew;
                }

                // Если файл уже в архиве
                {
                    fullPathTkArchive += DataSetOfCustomer.Items.FirstOrDefault(x => x.CheckGet("ID") == TkGridCustomer.SelectedItem.Key).CheckGet("PATHTK_ARCHIVE");
                    fullPathTkArchive += PathTechnologicalMapNew;
                }

                if (System.IO.File.Exists(fullPathTkConfirm))
                {
                    fullPathTk = fullPathTkConfirm;
                }
                else if (System.IO.File.Exists(fullPathTkNew))
                {
                    fullPathTk = fullPathTkNew;
                }
                else if (System.IO.File.Exists(fullPathTkArchive))
                {
                    fullPathTk = fullPathTkArchive;
                }
                else
                {
                    var msg = "Excel файл тех карты не найден";
                    var d = new DialogWindow($"{msg}", "ТК решётки", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }

                if (!string.IsNullOrEmpty(fullPathTk))
                {
                    // Create COM Objects
                    Microsoft.Office.Interop.Excel.Application excelApplication;
                    Microsoft.Office.Interop.Excel.Workbook excelWorkbook;

                    // Create new instance of Excel
                    excelApplication = new Microsoft.Office.Interop.Excel.Application();

                    // Make the process invisible to the user
                    excelApplication.ScreenUpdating = false;

                    // Make the process silent
                    excelApplication.DisplayAlerts = false;

                    // Open the workbook that you wish to export to PDF
                    excelWorkbook = excelApplication.Workbooks.Open(fullPathTk);

                    // If the workbook failed to open, stop, clean up, and bail out
                    if (excelWorkbook == null)
                    {
                        excelApplication.Quit();

                        excelApplication = null;
                        excelWorkbook = null;
                    }
                    else
                    {
                        var exportSuccessful = true;

                        try
                        {
                            string path = TechnologicalMapLocalFolder;
                            string folder = "_PDF";
                            string fileName = $"{TechnologicalMapIDFirst}.pdf";

                            fileFullLocalPathPdf = $"{path}{folder}\\{fileName}";

                            if (System.IO.Directory.Exists($"{path}{folder}"))
                            {
                                // Call Excel's native export function (valid in Office 2007 and Office 2010, AFAIK)
                                excelWorkbook.ExportAsFixedFormat(Microsoft.Office.Interop.Excel.XlFixedFormatType.xlTypePDF, fileFullLocalPathPdf);
                            }
                            else
                            {
                                System.IO.Directory.CreateDirectory($"{path}{folder}");
                                if (System.IO.Directory.Exists($"{path}{folder}"))
                                {
                                    // Call Excel's native export function (valid in Office 2007 and Office 2010, AFAIK)
                                    excelWorkbook.ExportAsFixedFormat(Microsoft.Office.Interop.Excel.XlFixedFormatType.xlTypePDF, fileFullLocalPathPdf);
                                }
                                else
                                {
                                    string msg = $"Не удалось подготовить папку для PDF файла";
                                    var d = new DialogWindow($"{msg}", "ТК решётки в сборе", "", DialogWindowButtons.OK);
                                    d.ShowDialog();
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            // Mark the export as failed for the return value...
                            exportSuccessful = false;

                            string msg = $"Не удалось преобразовать эксель файл в PDF. {ex.Message}";
                            var d = new DialogWindow($"{msg}", "ТК решётки в сборе", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                        finally
                        {
                            // Close the workbook, quit the Excel, and clean up regardless of the results...
                            excelWorkbook.Close();
                            excelApplication.Quit();

                            excelApplication = null;
                            excelWorkbook = null;

                            if (System.IO.File.Exists(fileFullLocalPathPdf))
                            {
                                string path = TechnologicalMapGlobalFolder;
                                string folder = $"{(int)(TechnologicalMapIDFirst / 10000)}";
                                string fileName = $"{TechnologicalMapIDFirst}.pdf";

                                fileFullGlobalPathPdf = System.IO.Path.Combine(path, folder, fileName);

                                string fileFolderGlobalPathPdf = System.IO.Path.Combine(path, folder);
                                System.IO.Directory.CreateDirectory(fileFolderGlobalPathPdf);
                                System.IO.File.Copy(fileFullLocalPathPdf, fileFullGlobalPathPdf, true);

                                if (System.IO.File.Exists(fileFullGlobalPathPdf))
                                {
                                    string msg = "PDF файл успешно создан";
                                    var d = new DialogWindow($"{msg}", "ТК решётки в сборе", "", DialogWindowButtons.OK);
                                    d.ShowDialog();
                                }
                                else
                                {
                                    string msg = "Не удалось прикрепить PDF файл к комлекту техкарт.";
                                    var d = new DialogWindow($"{msg}", "ТК решётки в сборе", "", DialogWindowButtons.OK);
                                    d.ShowDialog();
                                }
                            }
                            else
                            {
                                string msg = $"Не удалось преобразовать эксель файл в PDF.";
                                var d = new DialogWindow($"{msg}", "ТК решётки в сборе", "", DialogWindowButtons.OK);
                                d.ShowDialog();
                            }
                        }
                    }
                }
            }
            else
            {
                string msg = "Не задан путь к эксель файлу для преобразования в PDF";
                var d = new DialogWindow($"{msg}", "ТК решётки в сборе", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }
        /// <summary>
        /// Перемещаем эксель файл техкарты решёток из папки для новых файлов в рабочую папку
        /// </summary>
        /// <param name="p"></param>
        public async void ExcelDocumentMove(Dictionary<string, string> p)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "TechnologicalMap");
            q.Request.SetParam("Action", "ExcelDocumentMove");
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
                        var excel = new TechnologicalMapExcel(p);
                        var msg2 = excel.MoveToWork();

                        if (string.IsNullOrEmpty(msg2))
                        {
                            var msg = "Эксель файл успешно перемещён в рабочую папку";
                            var d = new DialogWindow($"{msg}", "Перемещение техкарты", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                        else
                        {
                            var msg = "Ошибка перемещения эксель файла в рабочую папку";
                            var d = new DialogWindow($"{msg}\n{msg2}", "Перемещение техкарты", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            SetButtons();
        }
        public async void ExcelDocumentMoveToArchive(Dictionary<string, string> p)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "TechnologicalMap");
            q.Request.SetParam("Action", "ExcelDocumentMoveToArchive");
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
                        var excel = new TechnologicalMapExcel(p);
                        var msg2 = excel.MoveToArchive();

                        if (string.IsNullOrEmpty(msg2))
                        {
                            var msg = "Техкарта успешно перемещёна в архив";
                            var d = new DialogWindow($"{msg}", "Перемещение техкарты", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                        else
                        {
                            var msg = "Ошибка перемещения эксель файла в архив";
                            var d = new DialogWindow($"{msg}\n{msg2}", "Перемещение техкарты", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            SetButtons();
        }

        #endregion


        #region "Требования"
        private void AddDemandsButton_Click(object sender, RoutedEventArgs e)
        {
            AddDemands();
        }
        /// <summary>
        /// Открытие окна дополнительных требований к тех карте
        /// </summary>
        public void AddDemands()
        {
            var d = new TechnologicalMapDemands();
            d.FirstTechnologicalMapId = TechnologicalMapIDFirst;
            d.SecondTechnologicalMapId = TechnologicalMapIDSecond;
            d.TypeProduct = Form.GetValueByPath("TYPE_PRODUCT").ToInt();
            d.Show(FrameName);

            DemandsTabIsClosed = false;

            SetButtons();
        }
        #endregion

        #region "Проверка валидации полей"
        /// <summary>
        /// Проверка валидности формы. Проверяет ширину(высоту) изделия на ограничения станка. Если ширина не входит в допустимые границы, то Form.Valid = false.
        /// </summary>
        public void ValidateProductHeigth()
        {
            // если продукция из коробочного картона
            if (TkGridTypeProduct.SelectedItem.Key.ToInt() == 229 || TkGridTypeProduct.SelectedItem.Key.ToInt() == 225 || TkGridTypeProduct.SelectedItem.Key.ToInt() == 227)
            {
                // если высота решётки меньше 55
                if (TkGridHeightFirst.Text.ToInt() < 55)
                {
                    // Красный
                    var color = "#ffee0000";
                    var bc = new BrushConverter();
                    var brush = (Brush)bc.ConvertFrom(color);
                    TkGridHeightFirst.BorderBrush = brush;
                    TkGridHeightFirst.ToolTip = "Высота коробочной решётки должна быть не менее 55";

                    Form.Valid = false;
                }
                else
                {
                    var color = "#ffcccccc";
                    var bc = new BrushConverter();
                    var brush = (Brush)bc.ConvertFrom(color);
                    TkGridHeightFirst.BorderBrush = brush;
                    TkGridHeightFirst.ToolTip = "";
                }

                // если высота решётки меньше 55
                if (TkGridHeightSecond.Text.ToInt() < 55)
                {
                    // Красный
                    var color = "#ffee0000";
                    var bc = new BrushConverter();
                    var brush = (Brush)bc.ConvertFrom(color);
                    TkGridHeightSecond.BorderBrush = brush;
                    TkGridHeightSecond.ToolTip = "Высота коробочной решётки должна быть не менее 55";

                    Form.Valid = false;
                }
                else
                {
                    var color = "#ffcccccc";
                    var bc = new BrushConverter();
                    var brush = (Brush)bc.ConvertFrom(color);
                    TkGridHeightSecond.BorderBrush = brush;
                    TkGridHeightSecond.ToolTip = "";
                }
            }
            else if (TkGridTypeProduct.SelectedItem.Key.ToInt() == 14 || TkGridTypeProduct.SelectedItem.Key.ToInt() == 15 || TkGridTypeProduct.SelectedItem.Key.ToInt() == 226)
            {
                int min = 0;
                int max = 1800;
                switch (TkProductionScheme.SelectedItem.Key.ToInt())
                {
                    case 602:
                        min = 97;
                        max = 1800;
                        break;
                    case 122:
                        min = 50;
                        max = 1180;
                        break;
                    default:
                        break;
                }
                if ((TkGridHeightFirst.Text.ToInt() > max || TkGridHeightFirst.Text.ToInt() < min) && TkGridHeightFirst.Text != "")
                {
                    // Красный
                    var color = "#ffee0000";
                    var bc = new BrushConverter();
                    var brush = (Brush)bc.ConvertFrom(color);
                    TkGridHeightFirst.BorderBrush = brush;
                    TkGridHeightFirst.ToolTip = "Ширина прокладки должна находится в пределах от " + min + "мм до " + max + "мм";

                    Form.Valid = false;

                }
                else
                {
                    var color = "#ffcccccc";
                    var bc = new BrushConverter();
                    var brush = (Brush)bc.ConvertFrom(color);
                    TkGridHeightFirst.BorderBrush = brush;
                    TkGridHeightFirst.ToolTip = "";
                }
            }
        }
        /// <summary>
        /// Дополнительная проверка после стандартной валидации;
        /// Проверяет выбранную схему производства и высоту решётки, если высота решётки меньше минимального допустимого значения для выбранной схемы, то Form.Valid = false;
        /// </summary>
        public void ValidateProductionScheme()
        {
            // Для решёток в сборе
            if (TkGridTypeProduct.SelectedItem.Key.ToInt() == 12)
            {
                if (TkProductionScheme.SelectedItem.Key != null)
                {
                    bool valid = true;

                    if ((TkGridHeightFirst.Text.ToInt() > 0 && TkGridHeightFirst.Text.ToInt() < 90)
                        || (TkGridHeightSecond.Text.ToInt() > 0 && TkGridHeightSecond.Text.ToInt() < 90))
                    {
                        if (TkProductionScheme.SelectedItem.Key.ToInt() == 1546)
                        {
                            Form.Valid = false;
                            valid = false;
                        }
                    }

                    // Если в схеме производства есть RdSc 1, то минимальная ширина просечки 43
                    if (TkProductionScheme.SelectedItem.Value.Contains("Рода") && !TkProductionScheme.SelectedItem.Value.Contains("Рода2"))
                    {
                        if (NotchesFirstGrid != null && NotchesFirstGrid.Items != null && NotchesFirstGrid.Items.Count > 0)
                        {
                            foreach (var notchesItem in NotchesFirstGrid.Items)
                            {
                                if (notchesItem.CheckGet("CONTENT").ToDouble() > 0 && notchesItem.CheckGet("CONTENT").ToDouble() < 43 && notchesItem.CheckGet("NUMBER").ToInt() != 1)
                                {
                                    Form.Valid = false;
                                    valid = false;
                                }
                            }
                        }

                        if (NotchesSecondGrid != null && NotchesSecondGrid.Items != null && NotchesSecondGrid.Items.Count > 0)
                        {
                            foreach (var notchesItem in NotchesSecondGrid.Items)
                            {
                                if (notchesItem.CheckGet("CONTENT").ToDouble() > 0 && notchesItem.CheckGet("CONTENT").ToDouble() < 43 && notchesItem.CheckGet("NUMBER").ToInt() != 1)
                                {
                                    Form.Valid = false;
                                    valid = false;
                                }
                            }
                        }
                    }

                    if (valid)
                    {
                        // Серый
                        var color = "#FFCCCCCC";
                        var bc = new BrushConverter();
                        var brush = (Brush)bc.ConvertFrom(color);
                        TkProductionScheme.BorderBrush = brush;
                        TkProductionScheme.ToolTip = "";
                    }
                    else
                    {
                        // Красный
                        var color = "#ffee0000";
                        var bc = new BrushConverter();
                        var brush = (Brush)bc.ConvertFrom(color);
                        TkProductionScheme.BorderBrush = brush;
                        TkProductionScheme.ToolTip = "Выбранная схема производства не подходит для заданных решёток";
                    }
                }
            }
            // Для решёток не в сборе
            else if (TkGridTypeProduct.SelectedItem.Key.ToInt() == 100)
            {
                if (TkProductionScheme.SelectedItem.Key != null)
                {
                    bool valid = true;

                    if (TkGridHeightFirst.Text.ToInt() > 0 && TkGridHeightFirst.Text.ToInt() < 90)
                    {
                        if (TkProductionScheme.SelectedItem.Key.ToInt() == 1550)
                        {
                            Form.Valid = false;
                            valid = false;
                        }
                    }

                    // Если в схеме производства есть RdSc 1, то минимальная ширина просечки 43
                    if (TkProductionScheme.SelectedItem.Value.Contains("Рода") && !TkProductionScheme.SelectedItem.Value.Contains("Рода2"))
                    {
                        if (NotchesFirstGrid != null && NotchesFirstGrid.Items != null && NotchesFirstGrid.Items.Count > 0)
                        {
                            foreach (var notchesItem in NotchesFirstGrid.Items)
                            {
                                if (notchesItem.CheckGet("CONTENT").ToDouble() > 0 && notchesItem.CheckGet("CONTENT").ToDouble() < 43 && notchesItem.CheckGet("NUMBER").ToInt() != 1)
                                {
                                    Form.Valid = false;
                                    valid = false;
                                }
                            }
                        }
                    }

                    if (valid)
                    {
                        // Серый
                        var color = "#FFCCCCCC";
                        var bc = new BrushConverter();
                        var brush = (Brush)bc.ConvertFrom(color);
                        TkProductionScheme.BorderBrush = brush;
                        TkProductionScheme.ToolTip = "";
                    }
                    else
                    {
                        // Красный
                        var color = "#ffee0000";
                        var bc = new BrushConverter();
                        var brush = (Brush)bc.ConvertFrom(color);
                        TkProductionScheme.BorderBrush = brush;
                        TkProductionScheme.ToolTip = "Выбранная схема производства не подходит для заданных решёток";
                    }
                }

                if (TkProductionScheme2.SelectedItem.Key != null)
                {
                    bool valid = true;

                    if (TkGridHeightSecond.Text.ToInt() > 0 && TkGridHeightSecond.Text.ToInt() < 90)
                    {
                        if (TkProductionScheme2.SelectedItem.Key.ToInt() == 1550)
                        {
                            Form.Valid = false;
                            valid = false;
                        }
                    }

                    // Если в схеме производства есть RdSc 1, то минимальная ширина просечки 43
                    if (TkProductionScheme2.SelectedItem.Value.Contains("Рода") && !TkProductionScheme2.SelectedItem.Value.Contains("Рода2"))
                    {
                        if (NotchesSecondGrid != null && NotchesSecondGrid.Items != null && NotchesSecondGrid.Items.Count > 0)
                        {
                            foreach (var notchesItem in NotchesSecondGrid.Items)
                            {
                                if (notchesItem.CheckGet("CONTENT").ToDouble() > 0 && notchesItem.CheckGet("CONTENT").ToDouble() < 43 && notchesItem.CheckGet("NUMBER").ToInt() != 1)
                                {
                                    Form.Valid = false;
                                    valid = false;
                                }
                            }
                        }
                    }

                    if (valid)
                    {
                        // Серый
                        var color = "#FFCCCCCC";
                        var bc = new BrushConverter();
                        var brush = (Brush)bc.ConvertFrom(color);
                        TkProductionScheme2.BorderBrush = brush;
                        TkProductionScheme2.ToolTip = "";
                    }
                    else
                    {
                        // Красный
                        var color = "#ffee0000";
                        var bc = new BrushConverter();
                        var brush = (Brush)bc.ConvertFrom(color);
                        TkProductionScheme2.BorderBrush = brush;
                        TkProductionScheme2.ToolTip = "Выбранная схема производства не подходит для заданных решёток";
                    }
                }
            }
            // Для прокладок
            else if (TkGridTypeProduct.SelectedItem.Key.ToInt() == 14 || TkGridTypeProduct.SelectedItem.Key.ToInt() == 15 || TkGridTypeProduct.SelectedItem.Key.ToInt() == 226)
            {
                if (TkProductionScheme.SelectedItem.Key != null)
                {
                    bool valid = true;
                    if (TkGridQuantityCrease.Text.ToInt() > 0)
                    {
                        if (TkProductionScheme.SelectedItem.Key.ToInt() != 602 && TkProductionScheme.SelectedItem.Key.ToInt() != 122)
                        {
                            Form.Valid = false;
                            valid = false;
                        }
                    }
                    if (valid)
                    {
                        // Серый
                        var color = "#FFCCCCCC";
                        var bc = new BrushConverter();
                        var brush = (Brush)bc.ConvertFrom(color);
                        TkProductionScheme.BorderBrush = brush;
                        TkProductionScheme.ToolTip = "";
                    }
                    else
                    {
                        // Красный
                        var color = "#ffee0000";
                        var bc = new BrushConverter();
                        var brush = (Brush)bc.ConvertFrom(color);
                        TkProductionScheme.BorderBrush = brush;
                        TkProductionScheme.ToolTip = "Выбранная схема производства не подходит для заданных прокладок";
                    }
                }
            }

        }
        /// <summary>
        /// Проверка валидности формы. Проверяет ширину(высоту) заготовки на ограничения станка. Если ширина не входит в допустимые границы, то Form.Valid = false.
        /// </summary>
        public void ValidateBilletHeigth()
        {
            if ((TkGridTypeProduct.SelectedItem.Key.ToInt() == 14 || TkGridTypeProduct.SelectedItem.Key.ToInt() == 15 || TkGridTypeProduct.SelectedItem.Key.ToInt() == 226)
                    && TkGridQuantityCrease.Text.ToInt() > 0)
            {
                int min = 0;
                int max = 1800;
                switch (TkProductionScheme.SelectedItem.Key.ToInt())
                {
                    case 602:
                        min = 356;
                        max = 1800;
                        break;
                    case 122:
                        min = 500;
                        max = 1180;
                        break;
                    default:
                        break;
                }
                if ((TkBilletWidthFirst.Text.ToInt() > max || TkBilletWidthFirst.Text.ToInt() < min) && TkBilletWidthFirst.Text != "")
                {
                    // Красный
                    var color = "#ffee0000";
                    var bc = new BrushConverter();
                    var brush = (Brush)bc.ConvertFrom(color);
                    TkBilletWidthFirst.BorderBrush = brush;
                    TkBilletWidthFirst.ToolTip = "Ширина заготовки должна находится в пределах от " + min + "мм до " + max + "мм";

                    Form.Valid = false;

                }
                else
                {
                    var color = "#ffcccccc";
                    var bc = new BrushConverter();
                    var brush = (Brush)bc.ConvertFrom(color);
                    TkBilletWidthFirst.BorderBrush = brush;
                    TkBilletWidthFirst.ToolTip = "";
                }
            }
        }

        /// <summary>
        /// Проверка валидности формы. Проверяет длину заготовки на ограничения станка. Если длина не входит в допустимые границы, то Form.Valid = false.
        /// </summary>
        public void ValidateBilletLength()
        {
            if (TkGridTypeProduct.SelectedItem.Key.ToInt() == 14 || TkGridTypeProduct.SelectedItem.Key.ToInt() == 15 || TkGridTypeProduct.SelectedItem.Key.ToInt() == 226)
            {
                int min = 0;
                int max = 2500;
                switch (TkProductionScheme.SelectedItem.Key.ToInt())
                {
                    case 602:
                        min = 260;
                        max = 1900;
                        break;
                    case 122:
                        min = 0;
                        max = 2500;
                        break;
                    default:
                        break;
                }
                if ((TkGridLengthFirst.Text.ToInt() > max || TkGridLengthFirst.Text.ToInt() < min) && TkGridLengthFirst.Text != "")
                {
                    // Красный
                    var color = "#ffee0000";
                    var bc = new BrushConverter();
                    var brush = (Brush)bc.ConvertFrom(color);
                    TkGridLengthFirst.BorderBrush = brush;
                    TkGridLengthFirst.ToolTip = "Длина заготовки должна находится в пределах от " + min + "мм до " + max + "мм";

                    Form.Valid = false;

                }
                else
                {
                    var color = "#ffcccccc";
                    var bc = new BrushConverter();
                    var brush = (Brush)bc.ConvertFrom(color);
                    TkGridLengthFirst.BorderBrush = brush;
                    TkGridLengthFirst.ToolTip = "";
                }
            }
        }

        /// <summary>
        /// Дополнительная проверка после стандартной валидации;
        /// Проверяет значение поля площадь изделия, если = 0, то Form.Valid = false;
        /// </summary>
        public void ValidateBilletSProduct()
        {
            // Для решёток в сборе
            if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(12,225))
            {
                if ((TkGridLengthFirst.Text.ToInt() > 0 && TkGridHeightFirst.Text.ToInt() > 0)
                    || (TkGridLengthSecond.Text.ToInt() > 0 && TkGridHeightSecond.Text.ToInt() > 0))
                {
                    if (TkBilletSProduct.Text.ToDouble() > 0)
                    {
                        // Серый
                        var color = "#FFCCCCCC";
                        var bc = new BrushConverter();
                        var brush = (Brush)bc.ConvertFrom(color);
                        TkBilletSProduct.BorderBrush = brush;
                        TkBilletSProduct.ToolTip = "";
                    }
                    else
                    {
                        // Красный
                        var color = "#ffee0000";
                        var bc = new BrushConverter();
                        var brush = (Brush)bc.ConvertFrom(color);
                        TkBilletSProduct.BorderBrush = brush;
                        TkBilletSProduct.ToolTip = "Значение должно быть больше 0";

                        Form.Valid = false;
                    }
                }
            }
            // Для решёток не в сборе
            else if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(100,229))
            {
                if (TkGridLengthFirst.Text.ToInt() > 0 && TkGridHeightFirst.Text.ToInt() > 0)
                {
                    if (TkBilletSProduct.Text.ToDouble() > 0)
                    {
                        // Серый
                        var color = "#FFCCCCCC";
                        var bc = new BrushConverter();
                        var brush = (Brush)bc.ConvertFrom(color);
                        TkBilletSProduct.BorderBrush = brush;
                        TkBilletSProduct.ToolTip = "";
                    }
                    else
                    {
                        // Красный
                        var color = "#ffee0000";
                        var bc = new BrushConverter();
                        var brush = (Brush)bc.ConvertFrom(color);
                        TkBilletSProduct.BorderBrush = brush;
                        TkBilletSProduct.ToolTip = "Значение должно быть больше 0";

                        Form.Valid = false;
                    }
                }

                if (TkGridLengthSecond.Text.ToInt() > 0 && TkGridHeightSecond.Text.ToInt() > 0)
                {
                    if (TkBilletSProduct2.Text.ToDouble() > 0)
                    {
                        // Серый
                        var color = "#FFCCCCCC";
                        var bc = new BrushConverter();
                        var brush = (Brush)bc.ConvertFrom(color);
                        TkBilletSProduct2.BorderBrush = brush;
                        TkBilletSProduct2.ToolTip = "";
                    }
                    else
                    {
                        // Красный
                        var color = "#ffee0000";
                        var bc = new BrushConverter();
                        var brush = (Brush)bc.ConvertFrom(color);
                        TkBilletSProduct2.BorderBrush = brush;
                        TkBilletSProduct2.ToolTip = "Значение должно быть больше 0";

                        Form.Valid = false;
                    }
                }
            }
        }
        /// <summary>
        /// Автоматический выбор схемы производства если ширина(высота) изделия не соответствует стандартной схеме производства
        /// </summary>
        public void SetProductionSchemeByHeight()
        {
            if (TkGridTypeProduct.SelectedItem.Key.ToInt() == 12)
            {
                if (TkGridHeightFirst.Text.ToInt() >= 80 && TkGridHeightFirst.Text.ToInt() < 90)
                {
                    TkProductionScheme.SelectedItem = TkProductionScheme.Items.FirstOrDefault(x => x.Key.ToInt() == 42);
                }
                else if (TkGridHeightFirst.Text.ToInt() >= 90)
                {
                    TkProductionScheme.SelectedItem = TkProductionScheme.Items.FirstOrDefault(x => x.Key.ToInt() == 1546);
                }
            }
            if (TkGridTypeProduct.SelectedItem.Key.ToInt() == 14 || TkGridTypeProduct.SelectedItem.Key.ToInt() == 15 || TkGridTypeProduct.SelectedItem.Key.ToInt() == 226)
            {
                int h = TkGridHeightFirst.Text.ToInt();
                int l = TkGridLengthFirst.Text.ToInt();
                if (h == 0 || l == 0 || (h <= 1180 && h >= 50 && l < 660))
                {
                    TkProductionScheme.SetSelectedItemByKey("122");
                }
                else
                {
                    TkProductionScheme.SetSelectedItemByKey("602");
                }
            }
        }
        public struct size
        {
            public int MinHeight;
            public int MaxHeight;
            public int MinWidth1;
            public int MaxWidth1;
            public int MinWidth2;
            public int MaxWidth2;
            public int MaxKol1;
            public int MaxKol2;
        }
        /// <summary>
        /// Валидация размеров коробочных решеток
        /// </summary>
        public void ValidateBoxedPartitionSize()
        {
            bool resume = true;
            List<size> list = new List<size>()
           {
               new size{MinHeight=80, MaxHeight = 300, MinWidth1 = 58, MaxWidth1 = 650, MinWidth2 = 58, MaxWidth2 = 650, MaxKol1=6, MaxKol2=24 },
               new size{MinHeight=80, MaxHeight = 300, MinWidth1 = 58, MaxWidth1 = 570, MinWidth2 = 58, MaxWidth2 = 600, MaxKol1=4, MaxKol2=12 },
               new size{MinHeight=55, MaxHeight = 300, MinWidth1 = 58, MaxWidth1 = 550, MinWidth2 = 58, MaxWidth2 = 330, MaxKol1=3, MaxKol2=35 },
               new size{MinHeight=55, MaxHeight = 250, MinWidth1 = 58, MaxWidth1 = 550, MinWidth2 = 58, MaxWidth2 = 500, MaxKol1=4, MaxKol2=10 }
           };
            List<size> list2 = new List<size>(list);
            foreach (size size in list2)
            {
                if (TkGridLengthFirst.Text.ToInt() != 0 && (TkGridLengthFirst.Text.ToInt() > size.MaxWidth1 || TkGridLengthFirst.Text.ToInt() < size.MinWidth1))
                {
                    list.Remove(size);
                }
                if (list.Count == 0)
                {
                    resume = false;

                    var color = "#ffee0000";
                    var bc = new BrushConverter();
                    var brush = (Brush)bc.ConvertFrom(color);
                    TkGridLengthFirst.BorderBrush = brush;
                    TkGridLengthFirst.ToolTip = "Длина первой решётки должна находится в пределах от 55 до 650";

                    //Form.Valid = false;
                }

            }
            list2 = list.ToList();
            if (resume)
            {
                foreach (size size in list2)
                {
                    if (TkGridHeightFirst.Text.ToInt() != 0 && (TkGridHeightFirst.Text.ToInt() > size.MaxHeight || TkGridHeightFirst.Text.ToInt() < size.MinHeight))
                    {
                        list.Remove(size);
                    }
                    if (list.Count == 0)
                    {
                        resume = false;

                        var color = "#ffee0000";
                        var bc = new BrushConverter();
                        var brush = (Brush)bc.ConvertFrom(color);
                        TkGridHeightFirst.BorderBrush = brush;
                        TkGridHeightFirst.ToolTip = "Высота решёток должна находится в пределах от 55 до 300";

                        //Form.Valid = false;
                    }

                }
                list2 = list.ToList();
            }
            if (resume)
            {
                foreach (size size in list2)
                {
                    if (TkGridQuantityFirst.Text.ToInt() != 0 && TkGridQuantityFirst.Text.ToInt() > size.MaxKol1)
                    {
                        list.Remove(size);
                    }
                    if (list.Count == 0)
                    {
                        resume = false;

                        var color = "#ffee0000";
                        var bc = new BrushConverter();
                        var brush = (Brush)bc.ConvertFrom(color);
                        TkGridQuantityFirst.BorderBrush = brush;
                        TkGridQuantityFirst.ToolTip = "Количество первых решёток не должно превышать " + size.MaxKol1;

                        //Form.Valid = false;
                    }

                }
                list2 = list.ToList();
            }
            if (resume)
            {
                foreach (size size in list2)
                {
                    if (TkGridLengthSecond.Text.ToInt() != 0 && (TkGridLengthSecond.Text.ToInt() > size.MaxWidth2 || TkGridLengthSecond.Text.ToInt() < size.MinWidth2))
                    {
                        list.Remove(size);
                    }
                    if (list.Count == 0)
                    {
                        resume = false;

                        var color = "#ffee0000";
                        var bc = new BrushConverter();
                        var brush = (Brush)bc.ConvertFrom(color);
                        TkGridLengthSecond.BorderBrush = brush;
                        TkGridLengthSecond.ToolTip = "Длина второй решётки должна находится в пределах от " + size.MinWidth2 + " до " + size.MaxWidth2;

                        //Form.Valid = false;
                    }

                }
                list2 = list.ToList();
            }
            if (resume)
            {
                foreach (size size in list2)
                {
                    if (TkGridQuantitySecond.Text.ToInt() != 0 && TkGridQuantitySecond.Text.ToInt() > size.MaxKol2)
                    {
                        list.Remove(size);
                    }
                    if (list.Count == 0)
                    {
                        resume = false;

                        var color = "#ffee0000";
                        var bc = new BrushConverter();
                        var brush = (Brush)bc.ConvertFrom(color);
                        TkGridQuantitySecond.BorderBrush = brush;
                        TkGridQuantitySecond.ToolTip = "Количество вторых решёток не должно превышать " + size.MaxKol2;

                        //Form.Valid = false;
                    }

                }
                list2 = list.ToList();
            }
        }
        #endregion

        #region "Демо"
        /// <summary>
        /// Заполнение полей формы тестовыми данными
        /// </summary>
        public void SetTestValues()
        {
            TkGridNameFirst.Text = "реш(5прос)";
            TkGridNameSecond.Text = "реш(3прос)";

            TkGridDetails.Text = "test_details";

            TkGridCustomer.SelectedItem = TkGridCustomer.Items.FirstOrDefault(x => x.Key.ToInt() == 1409);
            TkGridClient.SelectedItem = TkGridClient.Items.FirstOrDefault(x => x.Key.ToInt() == 4303);

            TkGridLengthFirst.Text = "343";
            TkGridHeightFirst.Text = "170";
            TkGridQuantityFirst.Text = "3";
            TkGridQuantityNotchesFirst.Text = "5";

            TkGridLengthSecond.Text = "291";
            TkGridHeightSecond.Text = "170";
            TkGridQuantitySecond.Text = "5";
            TkGridQuantityNotchesSecond.Text = "3";

            TkBilletLegthFirst.Text = "686";
            TkBilletWidthFirst.Text = "1180";
            TkBilletQuantityFirst1.Text = "28";

            TkBilletLegthSecond.Text = "925";
            TkBilletWidthSecond.Text = "1180";
            TkBilletCalculateTwoSecond.IsChecked = true;
            TkBilletQuantitySecond2.Text = "28";
            TkBilletQuantitySecond1.Text = "14";

            TkPallet.SelectedItem = TkPallet.Items.FirstOrDefault(x => x.Key.ToInt() == 4);
            TkLayingScheme.SelectedItem = TkLayingScheme.Items.FirstOrDefault(x => x.Key.ToInt() == 479);
            TkQuantity.Text = "20";
            TkQuantityPack.Text = "6";
            TkQuantityRows.Text = "11";
            TkQuantityBox.Text = "880";

            TkPackageLength.Text = "1200";
            TkPackageWidth.Text = "896";
            TkPackageHeigth.Text = "2020";

            TkProductionScheme.SelectedItem = TkProductionScheme.Items.FirstOrDefault(x => x.Key.ToInt() == 1546);
        }

        #endregion

        #region OpenTk
        /// <summary>
        /// Отображение фрейма техкарты по id_tk
        /// </summary>
        public void ShowByTechnologicalMap(int id_tk)
        {
            var dt = DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss");
            FrameName = $"{FrameName}_{id_tk.ToString()}";

            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 1;
            Central.WM.Show(FrameName, $"ТК №{id_tk.ToString()}", true, "add", this);

            if (id_tk > 0)
            {
                TechnologicalMapIDFirst = id_tk;
                GetTechnologicalMapDataByTechnologicalMap();
            }
        }
        /// <summary>
        /// Отображение фрейма (новая техкарта)
        /// </summary>
        public void Show()
        {
            var dt = DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss");
            FrameName = $"{FrameName}_new_{dt}";

            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 1;
            Central.WM.Show(FrameName, "Новая ТК", true, "add", this);

            SetButtons();
        }
        #endregion

        #region CloseTk
        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            bool allowedCloseFlag = CheckCloseAllowance();
            if (allowedCloseFlag)
            {
                Central.WM.Close(FrameName);

                //вся работа по утилизации ресурсов происходит в Destroy
                //он будет вызван при закрытии фрейма
            }
        }
        /// <summary>
        /// Дополнительная проверка перед закрытием формы
        /// Проводим проверки и вызываем всплывающее окно с дополнительной информацией и запросом на подтверждение закрытия формы
        /// 1. Проверяем, что если создан артикул, то и созданы заготовка и товар
        /// </summary>
        public bool CheckCloseAllowance()
        {
            bool resultFlag;
            // флаг того, что нужно показывать сообщение
            bool messageFlag = false;
            // текст отображаемого сообщения
            string msg = "";

            // Если есть артикул
            if (!string.IsNullOrEmpty(TkGridNumberFirst.Text))
            {
                // Если не создана позиция в ассортименте
                if (string.IsNullOrEmpty(TkTovarName.Text))
                {
                    messageFlag = true;
                    msg = "Техкарте присвоен артикул, но не создана позиция в ассотрименте";
                }
                // Если не создана заготовка
                else if (string.IsNullOrEmpty(TkBilletNameFirst.Text) && !TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(1, 225, 229))
                {
                    messageFlag = true;
                    msg = "Техкарте присвоен артикул, но не создана заготовка";
                }
            }

            if (messageFlag)
            {
                resultFlag = DialogWindow.ShowDialog($"Внимание!{Environment.NewLine}{msg}.{Environment.NewLine}Вы действительно хотите выйти?", "Решётки в сборе", "", DialogWindowButtons.NoYes) == true;
                return resultFlag;
            }
            else
            {
                resultFlag = true;
                return resultFlag;
            }
        }

        private void CancelButtonClick()
        {
            var p = new Dictionary<string, string>()
            {
                {"ID_TK", TechnologicalMapIDFirst.ToString() },
                {"CUST_ID", (TkGridCustomer?.SelectedItem as dynamic)?.Key?.ToString() ?? "-1"},
                {"TYPE_PRODUCT", (TkGridTypeProduct?.SelectedItem as dynamic)?.Key?.ToString() ?? "-1"},
            };

            var m = JsonConvert.SerializeObject(p);

            Central.Msg.SendMessage(new ItemMessage()
            {
                ReceiverGroup = "Preproduction",
                ReceiverName = "TechnologicalMapList",
                SenderName = "TechnologicalMap",
                Action = "Refresh",
                Message = m,
            });
            Close();
        }

        #endregion

        #region EditAndSaveTk
        private void EditButtonClick()
        {
            DisableControls();
            if (!(TechnologicalMapIDFirst > 0))
            {
                string msg = "Сохранить тех карту?";
                var d = new DialogWindow($"{msg}", "ТК решётки", "", DialogWindowButtons.NoYes);
                if (d.ShowDialog() == true)
                {
                    var values = GetDataForSave();
                    if (values.Count > 0)
                    {
                        Save(values);
                    }
                }
            }
            else
            {
                if (BlankFirstId > 0 || !(string.IsNullOrEmpty(TkBilletNameFirst.Text)))
                {
                    var msg = $"Позиция в ассортименте для заготовки уже создана. {Environment.NewLine}Если вы меняли данные по заготовкам, пожалуйста не забудьте пересоздать заготовку после этой операции.";
                    var d = new DialogWindow($"{msg}", "ТК решётки", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }

                {
                    string msg = "Изменить тех карту?";

                    if (ProductKomplektId > 0 || ProductFirstId > 0)
                    {
                        msg = "Позиция в ассортименте для товара уже создана. Изменить тех карту?";
                    }

                    var d = new DialogWindow($"{msg}", "ТК решётки", "", DialogWindowButtons.NoYes);
                    if (d.ShowDialog() == true)
                    {
                        var values = GetDataForSave();
                        if (values.Count > 0)
                        {
                            Edit(values);
                        }
                    }
                }
            }
            EnableControls();
        }
        private void SaveAndCloseButton()
        {
            DisableControls();
            var is_partition = 0;
            var msg_type_tk = "";
            switch (TkGridTypeProduct.SelectedItem.Key.ToInt())
            {
                case 1:
                    msg_type_tk = "ТК лист(стопы)";
                    break;
                case 14:
                case 15:
                case 226:
                    is_partition = 0;
                    msg_type_tk = "ТК прокладки";
                    break;
                case 12:
                case 100:
                case 229:
                case 225:
                case 227:
                    is_partition = 1;
                    msg_type_tk = "ТК решётки";
                    break;
                default:
                    is_partition = 1;
                    msg_type_tk = "ТК решётки";
                    break;
            }

            if (!(TechnologicalMapIDFirst > 0))
            {
                string msg = "Сохранить тех карту и закрыть?";
                var d = new DialogWindow($"{msg}", msg_type_tk, "", DialogWindowButtons.NoYes);
                if (d.ShowDialog() == true)
                {
                    var values = GetDataForSave();
                    if (values.Count > 0)
                    {
                        Save(values, true);
                    }
                }
            }
            else
            {
                if (BlankFirstId > 0 || !(string.IsNullOrEmpty(TkBilletNameFirst.Text)))
                {
                    var msg = $"Позиция в ассортименте для заготовки уже создана. {Environment.NewLine}Если вы меняли данные по заготовкам, пожалуйста не забудьте пересоздать заготовку после этой операции.";
                    var d = new DialogWindow($"{msg}", msg_type_tk, "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }

                {
                    string msg = "Изменить тех карту и закрыть?";

                    if (ProductKomplektId > 0 || ProductFirstId > 0)
                    {
                        msg = "Позиция в ассортименте для товара уже создана. Изменить тех карту и закрыть?";
                    }

                    var d = new DialogWindow($"{msg}", msg_type_tk, "", DialogWindowButtons.NoYes);
                    if (d.ShowDialog() == true)
                    {
                        var values = GetDataForSave();
                        if (values.Count > 0)
                        {
                            Edit(values, true);
                        }
                    }
                }
            }
            EnableControls();
        }
        /// <summary>
        /// Сохранение тех карты
        /// </summary>
        /// <param name="p">Данные техкарты для сохранения</param>
        /// <param name="closeFrameFlag">Флаг того, что после сохранения техкарты нужно закрыть вкладку</param>
        public async void Save(Dictionary<string, string> p, bool closeFrameFlag = false)
        {
            if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(12,225,100,229))
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "PartitionTechnologicalMap");
                q.Request.SetParam("Action", "Save");

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
                        DataSetFromSavedNew = ds;

                        TechnologicalMapIDFirst = ds.Items.First().CheckGet("TK_ID_FIRST").ToInt();
                        TechnologicalMapIDSecond = ds.Items.First().CheckGet("TK_ID_SECOND").ToInt();
                        IdSet = ds.Items.First().CheckGet("ID_SET").ToInt();
                        TechnologicalMapSetFirstId = ds.Items.First().CheckGet("ID_TK_SET_FIRST").ToInt();
                        TechnologicalMapSetSecondId = ds.Items.First().CheckGet("ID_TK_SET_SECOND").ToInt();
                        DataSetOfExistingTechnologicalMap = ds;

                        var msg = "Техкарта успешно создана.";
                        var d = new DialogWindow($"{msg}", "ТК решётки", "", DialogWindowButtons.OK);
                        d.ShowDialog();

                        // Отправляем сообщение вкладке Решётки в сборе о необходимости обновить грид
                        {
                            var mes = new Dictionary<string, string>()
                            {
                                {"ID_TK", TechnologicalMapIDFirst.ToString() },
                                {"CUST_ID", (TkGridCustomer?.SelectedItem as dynamic)?.Key?.ToString() ?? "-1"},
                                {"TYPE_PRODUCT", (TkGridTypeProduct?.SelectedItem as dynamic)?.Key?.ToString() ?? "-1"},
                            };

                            var m = JsonConvert.SerializeObject(mes);
                            Central.Msg.SendMessage(new ItemMessage()
                            {
                                ReceiverGroup = "Preproduction",
                                ReceiverName = "TechnologicalMapList",
                                SenderName = "TechnologicalMap",
                                Action = "Refresh",
                                Message = m,
                            }
                            );
                        }

                        if (closeFrameFlag)
                        {
                            Close();
                        }
                    }
                    else
                    {
                        var msg = "Ошибка создания тех карты";
                        var d = new DialogWindow($"{msg}", "ТК решётки", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }

                SetButtons();
            }
            else if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(1, 8, 14, 15))
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "GasketTechnologicalMap");
                q.Request.SetParam("Action", "Save");

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
                        DataSetFromSavedNew = ds;

                        TechnologicalMapIDFirst = ds.Items.First().CheckGet("TK_ID_FIRST").ToInt();
                        DataSetOfExistingTechnologicalMap = ds;

                        var mes = new Dictionary<string, string>()
                            {
                                {"ID_TK", TechnologicalMapIDFirst.ToString() },
                                {"CUST_ID", (TkGridCustomer?.SelectedItem as dynamic)?.Key?.ToString() ?? "-1"},
                                {"TYPE_PRODUCT", (TkGridTypeProduct?.SelectedItem as dynamic)?.Key?.ToString() ?? "-1"},
                            };

                        var m = JsonConvert.SerializeObject(mes);

                        // Отправляем сообщение вкладке Решётки в сборе о необходимости обновить грид
                        {
                            Central.Msg.SendMessage(new ItemMessage()
                            {
                                ReceiverGroup = "Preproduction",
                                ReceiverName = "TechnologicalMapList",
                                SenderName = "TechnologicalMap",
                                Action = "Refresh",
                                Message = m,
                            }
                            );
                        }

                        var msg = "Техкарта успешно создана.";
                        var d = new DialogWindow($"{msg}", "Создание техкарты", "", DialogWindowButtons.OK);
                        d.ShowDialog();

                        if (closeFrameFlag)
                        {
                            Close();
                        }
                    }
                    else
                    {
                        var msg = "Ошибка создания тех карты";
                        var d = new DialogWindow($"{msg}", "Создание техкарты", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }

                SetButtons();
            }
        }
        /// <summary>
        /// Изменение тех карты
        /// </summary>
        /// <param name="p">Сохраняемые параметры</param>
        /// <param name="closeFrameFlag">Флаг закрытия вкладки после похранения изменений по тех карте</param>
        public async void Edit(Dictionary<string, string> p, bool closeFrameFlag = false)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PartitionTechnologicalMap");
            q.Request.SetParam("Action", "Update");

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
                    DataSetFromSavedNew = ds;
                    TechnologicalMapIDFirst = ds.Items.First().CheckGet("TK_ID_FIRST").ToInt();
                    TechnologicalMapIDSecond = ds.Items.First().CheckGet("TK_ID_SECOND").ToInt();
                    IdSet = ds.Items.First().CheckGet("ID_SET").ToInt();
                    TechnologicalMapSetFirstId = ds.Items.First().CheckGet("ID_TK_SET_FIRST").ToInt();
                    TechnologicalMapSetSecondId = ds.Items.First().CheckGet("ID_TK_SET_SECOND").ToInt();
                    {
                        var msg = $"Техкарта успешно обновлена.{Environment.NewLine}Пожалуйста, убедитесь в том, что эксель файл содержит верные данные";
                        var d = new DialogWindow($"{msg}", "Обновление техкарты", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                    var firstData = DataSetOfExistingTechnologicalMap.Items.First();
                    if (IdSet == 0 && PathTechnologicalMapNew != null && PathTechnologicalMapNew != ds.Items.First().CheckGet("PATHTK"))
                    {
                        if (p.CheckGet("LENGTH_FIRST").ToInt() != firstData.CheckGet("LENGTH_FIRST").ToInt()
                        || p.CheckGet("HEIGHT_FIRST").ToInt() != firstData.CheckGet("HEIGHT_FIRST").ToInt()
                        || p.CheckGet("QUANTITY_FIRST").ToInt() != firstData.CheckGet("QUANTITY_FIRST").ToInt()
                        || p.CheckGet("LENGTH_SECOND").ToInt() != firstData.CheckGet("LENGTH_SECOND").ToInt()
                        || p.CheckGet("HEIGHT_SECOND").ToInt() != firstData.CheckGet("HEIGHT_SECOND").ToInt()
                        || p.CheckGet("QUANTITY_SECOND").ToInt() != firstData.CheckGet("QUANTITY_SECOND").ToInt()
                        || p.CheckGet("NAME_FIRST").ToInt() != firstData.CheckGet("NAME_FIRST").ToInt()
                        || p.CheckGet("TYPE_PACKAGE").ToInt() != firstData.CheckGet("TYPE_PACKAGE").ToInt())
                        {
                            var msg = $"Обновить название Excel файла?";
                            var d = new DialogWindow($"{msg}", "ТК решётки", "", DialogWindowButtons.NoYes);
                            if (d.ShowDialog() == true)
                            {
                                var excel = new TechnologicalMapExcel(p);
                                excel.FileNameNew = ds.Items.First().CheckGet("PATHTK");
                                excel.RenameExcelFile();

                                PathTechnologicalMapNew = ds.Items.First().CheckGet("PATHTK");
                                DataSetOfExistingTechnologicalMap.Items.First().Remove("LENGTH_FIRST");
                                DataSetOfExistingTechnologicalMap.Items.First().Remove("HEIGHT_FIRST");
                                DataSetOfExistingTechnologicalMap.Items.First().Remove("QUANTITY_FIRST");
                                DataSetOfExistingTechnologicalMap.Items.First().Remove("LENGTH_SECOND");
                                DataSetOfExistingTechnologicalMap.Items.First().Remove("HEIGHT_SECOND");
                                DataSetOfExistingTechnologicalMap.Items.First().Remove("QUANTITY_SECOND");
                                DataSetOfExistingTechnologicalMap.Items.First().Remove("NAME_FIRST");
                                DataSetOfExistingTechnologicalMap.Items.First().Remove("TYPE_PACKAGE");
                                DataSetOfExistingTechnologicalMap.Items.First().CheckAdd("LENGTH_FIRST", p.CheckGet("LENGTH_FIRST"));
                                DataSetOfExistingTechnologicalMap.Items.First().CheckAdd("HEIGHT_FIRST", p.CheckGet("HEIGHT_FIRST"));
                                DataSetOfExistingTechnologicalMap.Items.First().CheckAdd("QUANTITY_FIRST", p.CheckGet("QUANTITY_FIRST"));
                                DataSetOfExistingTechnologicalMap.Items.First().CheckAdd("LENGTH_SECOND", p.CheckGet("LENGTH_SECOND"));
                                DataSetOfExistingTechnologicalMap.Items.First().CheckAdd("HEIGHT_SECOND", p.CheckGet("HEIGHT_SECOND"));
                                DataSetOfExistingTechnologicalMap.Items.First().CheckAdd("QUANTITY_SECOND", p.CheckGet("QUANTITY_SECOND"));
                                DataSetOfExistingTechnologicalMap.Items.First().CheckAdd("NAME_FIRST", p.CheckGet("NAME_FIRST"));
                                DataSetOfExistingTechnologicalMap.Items.First().CheckAdd("TYPE_PACKAGE", p.CheckGet("TYPE_PACKAGE"));
                            }
                        }

                    }
                    // Отправляем сообщение вкладке Решётки в сборе о необходимости обновить грид
                    {
                        var mes = new Dictionary<string, string>()
                            {
                                {"ID_TK", TechnologicalMapIDFirst.ToString() },
                                {"CUST_ID", (TkGridCustomer?.SelectedItem as dynamic)?.Key?.ToString() ?? "-1"},
                                {"TYPE_PRODUCT", (TkGridTypeProduct?.SelectedItem as dynamic)?.Key?.ToString() ?? "-1"},
                            };

                        var m = JsonConvert.SerializeObject(mes);

                        Central.Msg.SendMessage(new ItemMessage()
                        {
                            ReceiverGroup = "Preproduction",
                            ReceiverName = "TechnologicalMapList",
                            SenderName = "TechnologicalMap",
                            Action = "Refresh",
                            Message = m,
                        }
                        );
                    }

                    if (closeFrameFlag)
                    {
                        Close();
                    }

                }
                else
                {
                    var msg = "Ошибка обновления техкарты";
                    var d = new DialogWindow($"{msg}", "Обновление техкарты", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                q.ProcessError();
            }

            SetButtons();
        }

        #endregion

        #region "Вспомогательные функции"
        private void DisableControls()
        {
            FormToolbar.IsEnabled = false;
        }
        private void EnableControls()
        {
            FormToolbar.IsEnabled = true;
        }
        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.IndexOf("Preproduction") > -1)
            {
                if (m.ReceiverName.IndexOf("TechnologicalMap") > -1)
                {
                    switch (m.Action)
                    {
                        case "Save":
                            RowMessage = (Dictionary<string, string>)m.ContextObject;
                            var grid_number = RowMessage.CheckGet("GRID_NUMBER").ToInt();
                            if (grid_number != 0)
                            {
                                switch (grid_number)
                                {
                                    case 1:
                                        foreach (var item in DataSetOfNotchesFirst.Items)
                                        {
                                            if (item.CheckGet("NUMBER") == RowMessage.CheckGet("NUMBER"))
                                            {
                                                var content = RowMessage.CheckGet("CONTENT").ToDouble().ToString();
                                                item.CheckAdd("CONTENT", content);
                                            }
                                        }
                                        NotchesFirstGrid.UpdateItems(DataSetOfNotchesFirst);
                                        CalculateSumNotches();
                                        break;
                                    case 2:
                                        foreach (var item in DataSetOfNotchesSecond.Items)
                                        {
                                            if (item.CheckGet("NUMBER") == RowMessage.CheckGet("NUMBER"))
                                            {
                                                var content = RowMessage.CheckGet("CONTENT").ToDouble().ToString();
                                                item.CheckAdd("CONTENT", content);
                                            }
                                        }
                                        NotchesSecondGrid.UpdateItems(DataSetOfNotchesSecond);
                                        CalculateSumNotches();
                                        break;
                                    case 3:
                                        if (DataSetOfCrease != null)
                                        {
                                            foreach (var item in DataSetOfCrease.Items)
                                            {
                                                if (item.CheckGet("NUMBER") == RowMessage.CheckGet("NUMBER"))
                                                {
                                                    var content = RowMessage.CheckGet("CONTENT").ToDouble().ToString();
                                                    item.CheckAdd("CONTENT", content);
                                                }
                                            }
                                            CreaseGrid.UpdateItems(DataSetOfCrease);
                                            var tmp = TkGridLastCrease.Text;
                                            TkGridLastCrease.Clear();
                                            TkGridLastCrease.Text = tmp;
                                        }
                                        
                                        break;
                                    default:
                                        break;
                                }


                            }
                            break;

                        case "UpdateCustomerList":
                            var selectedItemKey = TkGridClient.SelectedItem.Key;
                            GetDataFromCustomerAndClient();
                            if (!string.IsNullOrEmpty(TkGridClient.Items.FirstOrDefault(x => x.Key == selectedItemKey).Key))
                            {
                                TkGridClient.SetSelectedItemByKey(selectedItemKey);
                                CheckCustomerByClient();
                            }
                            break;

                        case "Closed":
                            string message = m.Message;
                            if (message == FrameName)
                            {
                                DemandsTabIsClosed = true;
                                SetButtons();
                            }
                            break;

                        case "UpdateOnEdge":
                            var contextObject = (Dictionary<string, string>)m.ContextObject;

                            if (Form.GetValueByPath("ON_EDGE") != contextObject.CheckGet("ON_EDGE"))
                            {
                                OnEdgeCheckBox.Background = HColor.Yellow.ToBrush();
                            }
                            Form.SetValueByPath("ON_EDGE", contextObject.CheckGet("ON_EDGE"));

                            if (Form.GetValueByPath("ON_EDGE2") != contextObject.CheckGet("ON_EDGE2"))
                            {
                                OnEdge2CheckBox.Background = HColor.Yellow.ToBrush();
                            }
                            Form.SetValueByPath("ON_EDGE2", contextObject.CheckGet("ON_EDGE2"));

                            break;
                    }
                }
            }
        }
        /// <summary>
        /// обработка ввода с клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;
            }
        }
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }
        /// <summary>
        /// отображение справочной статьи
        /// (относительный путь)
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/preproduction/tk_grid/TkGrid");
        }

        #endregion

        #region Functions
        
        /// <summary>
        /// Включает валидацию для полей заготовки
        /// </summary>
        public void AddFilterForBillet()
        {
            if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(8, 12, 100, 14, 15))
            {
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_LENGTH_FIRST"), FormHelperField.FieldFilterRef.Required);
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_WIDTH_FIRST"), FormHelperField.FieldFilterRef.Required);
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_SQUARE_FIRST"), FormHelperField.FieldFilterRef.Required);
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_QUANTITY_FIRST1"), FormHelperField.FieldFilterRef.Required);
            }
            // Прокладки
            if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(14, 15))
            {
                // Прокладки с рилёвками
                if (TkGridQuantityCrease.Text.ToInt() > 0)
                {
                    //На станке КГ-4
                    if (TkProductionScheme.SelectedItem.Key.ToInt() == 602)
                    {
                        Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_LENGTH_FIRST"), FormHelperField.FieldFilterRef.MaxValue, 1900);
                        Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_LENGTH_FIRST"), FormHelperField.FieldFilterRef.MinValue, 260);
                        Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_WIDTH_FIRST"), FormHelperField.FieldFilterRef.MaxValue, 1800);
                        Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_WIDTH_FIRST"), FormHelperField.FieldFilterRef.MinValue, 356);
                    }
                    else
                    {
                        Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_LENGTH_FIRST"), FormHelperField.FieldFilterRef.MaxValue, 2500);
                        Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_LENGTH_FIRST"), FormHelperField.FieldFilterRef.MinValue, 0);
                        Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_WIDTH_FIRST"), FormHelperField.FieldFilterRef.MaxValue, 1180);
                        Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_WIDTH_FIRST"), FormHelperField.FieldFilterRef.MinValue, 500);
                    }
                }
                // Прокладки без рилёвок
                else
                {
                    //На станке КГ-4
                    if (TkProductionScheme.SelectedItem.Key.ToInt() == 602)
                    {
                        Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_LENGTH_FIRST"), FormHelperField.FieldFilterRef.MaxValue, 1800);
                        Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_LENGTH_FIRST"), FormHelperField.FieldFilterRef.MinValue, 356);
                        Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_WIDTH_FIRST"), FormHelperField.FieldFilterRef.MaxValue, 1900);
                        Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_WIDTH_FIRST"), FormHelperField.FieldFilterRef.MinValue, 260);
                    }
                    else
                    {
                        // количество ручьёв
                        int k = TkBilletLegthFirst.Text.ToInt() / TkGridLengthFirst.Text.ToInt();
                        Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_LENGTH_FIRST"), FormHelperField.FieldFilterRef.MaxValue, 1180);
                        Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_LENGTH_FIRST"), FormHelperField.FieldFilterRef.MinValue, 500);
                        Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_WIDTH_FIRST"), FormHelperField.FieldFilterRef.MaxValue, 2500);
                        Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_WIDTH_FIRST"), FormHelperField.FieldFilterRef.MinValue, 0);
                    }
                }

            }
            // Решётки
            else
            {
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_LENGTH_FIRST"), FormHelperField.FieldFilterRef.MaxValue, 1800);
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_LENGTH_FIRST"), FormHelperField.FieldFilterRef.MinValue, 500);
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_WIDTH_FIRST"), FormHelperField.FieldFilterRef.MaxValue, 2500);
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_WIDTH_FIRST"), FormHelperField.FieldFilterRef.MinValue, 0);
            }



            if (Form.GetValueByPath("BILLET_LENGTH_SECOND").ToInt() > 0 || Form.GetValueByPath("BILLET_WIDTH_SECOND").ToInt() > 0)
            {
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_LENGTH_SECOND"), FormHelperField.FieldFilterRef.Required);
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_WIDTH_SECOND"), FormHelperField.FieldFilterRef.Required);
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_SQUARE_SECOND"), FormHelperField.FieldFilterRef.Required);
                // Прокладки
                if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(14,15))
                {
                    // Прокладки с рилёвками
                    if (TkGridQuantityCrease.Text.ToInt() > 0)
                    {
                        //На станке КГ-4
                        if (TkProductionScheme.SelectedItem.Key.ToInt() == 602)
                        {
                            Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_LENGTH_SECOND"), FormHelperField.FieldFilterRef.MaxValue, 1900);
                            Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_LENGTH_SECOND"), FormHelperField.FieldFilterRef.MinValue, 260);
                            Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_WIDTH_SECOND"), FormHelperField.FieldFilterRef.MaxValue, 1800);
                            Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_WIDTH_SECOND"), FormHelperField.FieldFilterRef.MinValue, 356);
                        }
                        else
                        {
                            Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_LENGTH_SECOND"), FormHelperField.FieldFilterRef.MaxValue, 2500);
                            Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_LENGTH_SECOND"), FormHelperField.FieldFilterRef.MinValue, 0);
                            Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_WIDTH_SECOND"), FormHelperField.FieldFilterRef.MaxValue, 1180);
                            Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_WIDTH_SECOND"), FormHelperField.FieldFilterRef.MinValue, 500);
                        }
                    }
                    // Прокладки без рилёвок
                    else
                    {
                        //На станке КГ-4
                        if (TkProductionScheme.SelectedItem.Key.ToInt() == 602)
                        {
                            Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_LENGTH_SECOND"), FormHelperField.FieldFilterRef.MaxValue, 1800);
                            Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_LENGTH_SECOND"), FormHelperField.FieldFilterRef.MinValue, 356);
                            Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_WIDTH_SECOND"), FormHelperField.FieldFilterRef.MaxValue, 1900);
                            Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_WIDTH_SECOND"), FormHelperField.FieldFilterRef.MinValue, 260);
                        }
                        else
                        {
                            Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_LENGTH_SECOND"), FormHelperField.FieldFilterRef.MaxValue, 1180);
                            Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_LENGTH_SECOND"), FormHelperField.FieldFilterRef.MinValue, 500);
                            Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_WIDTH_SECOND"), FormHelperField.FieldFilterRef.MaxValue, 2500);
                            Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_WIDTH_SECOND"), FormHelperField.FieldFilterRef.MinValue, 0);
                        }
                    }

                }
                // Решётки
                else
                {
                    Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_LENGTH_SECOND"), FormHelperField.FieldFilterRef.MaxValue, 1800);
                    Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_LENGTH_SECOND"), FormHelperField.FieldFilterRef.MinValue, 500);
                    Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_WIDTH_SECOND"), FormHelperField.FieldFilterRef.MaxValue, 2500);
                    Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BILLET_WIDTH_SECOND"), FormHelperField.FieldFilterRef.MinValue, 0);
                }
            }
        }
        /// <summary>
        /// Отключает валидацию для полей заготовки
        /// </summary>
        public void RemoveFilterForBillet()
        {
            Form.RemoveFilter("BILLET_LENGTH_FIRST", FormHelperField.FieldFilterRef.Required);
            Form.RemoveFilter("BILLET_WIDTH_FIRST", FormHelperField.FieldFilterRef.Required);
            Form.RemoveFilter("BILLET_SQUARE_FIRST", FormHelperField.FieldFilterRef.Required);
            Form.RemoveFilter("BILLET_QUANTITY_FIRST1", FormHelperField.FieldFilterRef.Required);

            Form.RemoveFilter("BILLET_LENGTH_FIRST", FormHelperField.FieldFilterRef.MaxValue);
            Form.RemoveFilter("BILLET_WIDTH_FIRST", FormHelperField.FieldFilterRef.MaxValue);
            Form.RemoveFilter("BILLET_LENGTH_FIRST", FormHelperField.FieldFilterRef.MinValue);
            Form.RemoveFilter("BILLET_WIDTH_FIRST", FormHelperField.FieldFilterRef.MinValue);

            Form.RemoveFilter("BILLET_LENGTH_SECOND", FormHelperField.FieldFilterRef.Required);
            Form.RemoveFilter("BILLET_WIDTH_SECOND", FormHelperField.FieldFilterRef.Required);
            Form.RemoveFilter("BILLET_SQUARE_SECOND", FormHelperField.FieldFilterRef.Required);

            Form.RemoveFilter("BILLET_LENGTH_SECOND", FormHelperField.FieldFilterRef.MaxValue);
            Form.RemoveFilter("BILLET_WIDTH_SECOND", FormHelperField.FieldFilterRef.MaxValue);
            Form.RemoveFilter("BILLET_LENGTH_SECOND", FormHelperField.FieldFilterRef.MinValue);
            Form.RemoveFilter("BILLET_WIDTH_SECOND", FormHelperField.FieldFilterRef.MinValue);
        }
        /// <summary>
        /// Открывает окно редактирования потребителя
        /// </summary>
        public void ShowCustomerData()
        {
            if (TkGridCustomer.SelectedItem.Key != null)
            {
                var custId = TkGridCustomer.SelectedItem.Key.ToInt();
                var customerData = new CustomerData();
                customerData.Show(custId);
            }
            else
            {
                var msg = "Пожалуйста выберите потребителя для редактирования";
                var d = new DialogWindow($"{msg}", "ТК решётки в сборе", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }
        /// <summary>
        /// отображение фрейма (техкарта по комплекту)
        /// </summary>
        public void ShowBySet(int id_set)
        {
            var dt = DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss");
            FrameName = $"{FrameName}_{id_set.ToString()}";

            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 1;
            Central.WM.Show(FrameName, $"Комплект №{id_set.ToString()}", true, "add", this);

            if (id_set > 0)
            {
                IdSet = id_set;
                GetTechnologicalMapDataBySet();
            }
        }
        public void CopyByTechnologicalMap(int id_tk)
        {
            TechnologicalMapIDFirst = id_tk;
            GetTechnologicalMapDataByTechnologicalMap(false);
            SetCopyDataFromForm(DataSetOfExistingTechnologicalMap);
            TechnologicalMapIDFirst = 0;

            Show();
        }
        /// <summary>
        /// Проверка заготовки по ограничениям (количество ручьёв, количество просечек, длина, _ширина)
        /// </summary>
        /// <returns></returns>
        public bool CheckBlankLimit(int currentLength, int currentNotches, int currentStream)
        {
            // Максималное количество просечек на заготовке
            int zMaxNotches = 12;
            // Максимальное количество ручьёв на заготовке
            int zMaxStream = 6;
            // Максимальная длина заготовки. (По умолчанию 1180)
            int zMaxLength = 1180;

            // Если превышено количество ручьёв, то сразу говорим о превышении
            if (currentStream > zMaxStream)
            {
                return false;
            }

            // Если превышено количество просечек, то сразу говорим о превышении
            if (currentNotches > zMaxNotches)
            {
                return false;
            }

            // Определяем максимальную длину заготовки в зависимости от количества просечек и ручьёв
            int maxLengthFromDictionary = GetBlankMaxLength(currentNotches, currentStream);
            if (maxLengthFromDictionary > 0)
            {
                zMaxLength = maxLengthFromDictionary;
            }

            if (currentLength > zMaxLength)
            {
                return false;
            }

            return true;
        }
        /// <summary>
        /// Получаем значение максимальной длины заготовки по суммарному количеству просечек на заготовке и суммарному количесву ручьёв на заготовке
        /// </summary>
        /// <param name="currentNotches"></param>
        /// <param name="currentStream"></param>
        /// <returns></returns>
        public int GetBlankMaxLength(int currentNotches, int currentStream)
        {
            int maxLengthByCurrentNotches = MaxLengthByNotches.CheckGet(currentNotches).ToInt();
            int maxLengthByCurrentStream = MaxLengthByStream.CheckGet(currentStream).ToInt();

            if (maxLengthByCurrentNotches <= maxLengthByCurrentStream)
            {
                return maxLengthByCurrentNotches;
            }
            else
            {
                return maxLengthByCurrentStream;
            }
        }
        /// <summary>
        /// Алгоритм расчёта размеров заготовок
        /// </summary>
        /// <param name="flagForGetDate"></param>
        public void CalculateBlankSize()
        {
            // Максимальная ширина заготовки
            int zMaxWidth = 2500;

            // Припуск к ширине заготовки
            int zWidthOverage = 70;
            // Если выбранная продукция - это прокладки
            // и если высота прокладки больше чем 660, то рассчитываем припуск(обрезь)
            if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(8,14,15,226))
            {
                if (TkGridHeightFirst.Text.ToInt() > 660)
                {
                    zWidthOverage = 90 + ((TkGridHeightFirst.Text.ToInt() - 660) * 2);
                }
            }

            // Ширина первой заготовки
            int zWidthFirst = 0;

            // Максималное количество просечек на заготовке
            int zMaxNotches = 12;
            // Максимальное количество ручьёв на заготовке
            int zMaxStream = 6;
            // Максимальная длина заготовки
            //int zMaxLength = 1180;
            // Минимальная длина заготовки (не всегда используется)
            int zMinLength = BlankLengthMin;
            //int zMinLength = 500;

            // Максимальное значение, на которое можно округлить(вверх) длину заготовки, чтобы получить минимальную допустимую длину
            int zOffsetMinLength = 20;

            // Длина первой заготовки
            int zLengthFirst = 0;

            // Количество решёток на заготовке по ширине
            int qtyGridsByWidth = 0;

            // Длина второй заготовки
            int zLengthSecond = 0;
            // Ширина второй заготовки
            int zWidthSecond = 0;

            // Количество первых решёток из первой заготовки
            int zQtyFirstGridByFirstBillet = 0;
            // Количество вторых решёток из первой заготовки
            int zQtySecondGridByFirstBillet = 0;
            // Количество первых решёток из второй заготовки
            int zQtyFirstGridBySecondBillet = 0;
            // Количество вторых решёток из второй заготовки
            int zQtySecondGridBySecondBillet = 0;

            // Расчёт ширины заготовки
            {
                var width = zMaxWidth;

                width = width - zWidthOverage;

                double doubleWidth = (double)((double)width / (double)TkGridHeightFirst.Text.ToDouble());

                width = doubleWidth.ToInt();

                qtyGridsByWidth = doubleWidth.ToInt();

                width = width * TkGridHeightFirst.Text.ToInt();

                width = width + zWidthOverage;

                if (width >= 1860 && width <= 1900)
                {
                    width = 1900;
                }
                else if (width >= 1960 && width <= 2000)
                {
                    width = 2000;
                }
                else if (width >= 2060 && width <= 2100)
                {
                    width = 2100;
                }
                else if (width >= 2160 && width <= 2200)
                {
                    width = 2200;
                }
                else if (width >= 2260 && width <= 2300)
                {
                    width = 2300;
                }
                else if (width >= 2360 && width <= 2400)
                {
                    width = 2400;
                }
                else if (width >= 2460 && width <= 2500)
                {
                    width = 2500;
                }

                if (width > 2500)
                {
                    string msg = $"Ошибка расчёта габаритов заготовки. Ширина заготовки расчиталась как {width} > 2500";
                    var d = new DialogWindow($"{msg}", "ТК", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
                else
                {
                    zWidthFirst = width;
                }
            }

            if (zWidthFirst > 0)
            {
                // Если выбранная продукция - это комплект решёток в сборе
                if (TkGridTypeProduct.SelectedItem.Key.ToInt() == 12)
                {
                    // Расчёт длины заготовки
                    {
                        // Вариант (1 к 2) или (2 к 1)
                        if ((TkGridQuantityNotchesFirst.Text.ToInt() == 1 && TkGridQuantityNotchesSecond.Text.ToInt() == 2) || (TkGridQuantityNotchesFirst.Text.ToInt() == 2 && TkGridQuantityNotchesSecond.Text.ToInt() == 1))
                        {
                            // Проверяем, что весь комплект помещается на заготовке 
                            if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * TkGridQuantityFirst.Text.ToInt() + TkGridLengthSecond.Text.ToInt() * TkGridQuantitySecond.Text.ToInt(), 4, 3))
                            {
                                // Количество комплектов
                                int kKomplekt = 1;

                                // Сумма длин решёток для комплекта
                                var l = TkGridLengthFirst.Text.ToInt() * TkGridQuantityFirst.Text.ToInt() + TkGridLengthSecond.Text.ToInt() * TkGridQuantitySecond.Text.ToInt();

                                // Сумма просечек в комплекте
                                var qtyNotches = TkGridQuantityNotchesFirst.Text.ToInt() * TkGridQuantityFirst.Text.ToInt() + TkGridQuantityNotchesSecond.Text.ToInt() * TkGridQuantitySecond.Text.ToInt();

                                // Количество ручьёв для получения комплекта из заготовки 
                                var qtyStream = TkGridQuantityFirst.Text.ToInt() + TkGridQuantitySecond.Text.ToInt();

                                // Ищем максимальное количество комплектов, которое можно разместить на заготовке
                                var iterator = kKomplekt;

                                while (CheckBlankLimit(l * iterator, qtyNotches * iterator, qtyStream * iterator))
                                {
                                    kKomplekt = iterator;
                                    iterator += 1;
                                }

                                l = l * kKomplekt;

                                zQtyFirstGridByFirstBillet = qtyGridsByWidth * TkGridQuantityFirst.Text.ToInt() * kKomplekt;
                                zQtySecondGridByFirstBillet = qtyGridsByWidth * TkGridQuantitySecond.Text.ToInt() * kKomplekt;


                                zLengthFirst = l;
                            }
                            // Весь комплект не помещаетя на заготовке
                            else
                            {
                                // Если весь комплект не помещаетсяна заготовке, то пытаемся разместить обе решётки на одну заготовку и использовать добивочную
                                if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * 1, 3, 2))
                                {
                                    // Сумма длин решёток для половины комплекта
                                    zLengthFirst = TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * 1;

                                    zQtyFirstGridByFirstBillet = qtyGridsByWidth * 1;
                                    zQtySecondGridByFirstBillet = qtyGridsByWidth * 1;

                                    // Ищем добивочную заготовку
                                    if (TkGridQuantityNotchesSecond.Text.ToInt() == 2)
                                    {
                                        // Находим решётку, у которой количество в комплекте больше, находим её количество в первой заготовке
                                        int q = zQtySecondGridByFirstBillet;
                                        // Делим найденное количество на количество этой решётки в комплекте
                                        double doubleQ = (double)((double)q / (double)TkGridQuantitySecond.Text.ToDouble());
                                        // Берём целую часть от полученного числа
                                        q = doubleQ.ToInt();
                                        // Умножаем получившееся число на количество второй решётки в комплекте (той, у которой количество в комплекте меньше)
                                        q = q * TkGridQuantityFirst.Text.ToInt();
                                        // Сравниваем получившееся число с количеством решёток из первой заготовки для первой решётки (той, у которой количество в комплекте меньше)
                                        // Если получившееся число больше, то добивочная решётка та, у которой количество в комплекте меньше
                                        if (q > zQtyFirstGridByFirstBillet)
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте меньше, а значит количество просечек больше

                                            // Находим количество решёток, которое помещается на вторую заготовку

                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtyFirstGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;

                                        }
                                        // Если получившееся число меньше, то добивочная решётка та, у которой количество в комплекте больше
                                        else
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте больше, а значит количество просечек меньше

                                            // Находим количество решёток, которое помещается на вторую заготовку

                                            // Количество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }
                                    }
                                    else
                                    {
                                        // Находим решётку, у которой количество в комплекте больше, находим её количество в первой заготовке
                                        int q = zQtyFirstGridByFirstBillet;
                                        // Делим найденное количество на количество этой решётки в комплекте
                                        double doubleQ = (double)((double)q / (double)TkGridQuantityFirst.Text.ToDouble());
                                        // Берём целую часть от полученного числа
                                        q = doubleQ.ToInt();
                                        // Умножаем получившееся число на количество второй решётки в комплекте (той, у которой количество в комплекте меньше)
                                        q = q * TkGridQuantitySecond.Text.ToInt();
                                        // Сравниваем получившееся число с количеством решёток из первой заготовки для второй решётки (той, у которой количество в комплекте меньше)
                                        // Если получившееся число больше, то добивочная решётка та, у которой количество в комплекте меньше
                                        if (q > zQtySecondGridByFirstBillet)
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте меньше, а значит количество просечек больше

                                            // Находим количество решёток, которое помещается на вторую заготовку

                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;

                                        }
                                        // Если получившееся число меньше, то добивочная решётка та, у которой количество в комплекте больше
                                        else
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте больше, а значит количество просечек меньше

                                            // Находим количество решёток, которое помещается на вторую заготовку

                                            // Количество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtyFirstGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }
                                    }
                                }
                                // Размещаем каждую решётку на своей заготовке
                                else
                                {
                                    // Первая заготовка
                                    {
                                        // Находим количество решёток, которое помещается на первую заготовку
                                        // Количество ручьёв на заготовке
                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;

                                        while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthFirst = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * qtyStreamByBillet;
                                    }

                                    // Вторая заготовка
                                    {
                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;

                                        while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                        zWidthSecond = zWidthFirst;
                                        zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                    }
                                }
                            }
                        }
                        // Вариант (1 к 1)
                        else if (TkGridQuantityNotchesFirst.Text.ToInt() == 1 && TkGridQuantityNotchesSecond.Text.ToInt() == 1)
                        {
                            // Проверяем, что весь комплект помещается на заготовке 
                            if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * TkGridQuantityFirst.Text.ToInt() + TkGridLengthSecond.Text.ToInt() * TkGridQuantitySecond.Text.ToInt(), 2, 2))
                            {
                                // Проверяем, что решётки одинаковые
                                if (TkGridLengthFirst.Text.ToInt() == TkGridLengthSecond.Text.ToInt())
                                {
                                    // Сумма длин решёток для комплекта
                                    var l = TkGridLengthFirst.Text.ToInt() * TkGridQuantityFirst.Text.ToInt() + TkGridLengthSecond.Text.ToInt() * TkGridQuantitySecond.Text.ToInt();

                                    // Сумма просечек в комплекте
                                    var qtyNotches = TkGridQuantityNotchesFirst.Text.ToInt() * TkGridQuantityFirst.Text.ToInt() + TkGridQuantityNotchesSecond.Text.ToInt() * TkGridQuantitySecond.Text.ToInt();

                                    // Количество ручьёв для получения комплекта из заготовки 
                                    var qtyStream = TkGridQuantityFirst.Text.ToInt() + TkGridQuantitySecond.Text.ToInt();

                                    // Кличество ручьёв на заготовке
                                    int qtyStreamByBillet = 0;

                                    // Ищем максимальное количество решёток, которое можно разместить на заготовке
                                    {
                                        var iterator = qtyStream;
                                        while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthFirst = TkGridLengthFirst.Text.ToInt() * qtyStreamByBillet;

                                        var qtyGrids = qtyStreamByBillet * qtyGridsByWidth;

                                        if (qtyGrids % 2 == 0)
                                        {
                                            zQtyFirstGridByFirstBillet = qtyGrids / 2;
                                            zQtySecondGridByFirstBillet = qtyGrids / 2;
                                        }
                                        else
                                        {
                                            qtyGrids = qtyGrids - 1;

                                            zQtyFirstGridByFirstBillet = (qtyGrids / 2) + 1;
                                            zQtySecondGridByFirstBillet = qtyGrids / 2;
                                        }
                                    }
                                }
                                // Решётки не одинаковые
                                else
                                {
                                    // Сумма длин решёток для комплекта
                                    var l = TkGridLengthFirst.Text.ToInt() * TkGridQuantityFirst.Text.ToInt() + TkGridLengthSecond.Text.ToInt() * TkGridQuantitySecond.Text.ToInt();

                                    // Сумма просечек в комплекте
                                    var qtyNotches = TkGridQuantityNotchesFirst.Text.ToInt() * TkGridQuantityFirst.Text.ToInt() + TkGridQuantityNotchesSecond.Text.ToInt() * TkGridQuantitySecond.Text.ToInt();

                                    // Количество ручьёв для получения комплекта из заготовки 
                                    var qtyStream = TkGridQuantityFirst.Text.ToInt() + TkGridQuantitySecond.Text.ToInt();

                                    // Кличество ручьёв на заготовке
                                    int qtyStreamByBillet = 0;

                                    // Количество комплектов на заготовке
                                    int qtyComplectByBillet = 0;

                                    // Ищем максимальное количество решёток, которое можно разместить на заготовке
                                    {
                                        // Количество комплектов, которое можно разместить на заготовке
                                        var iterator = 1;
                                        // Если длина первой решётки*количество первой решётки в комплекте*количество комплектов
                                        // + длина второй решётки*количество второй решётки в комплекте*количество комплектов не быльше максимального допустимого значения длины заготовки
                                        // и количество просечек первой решётки*количество первой решётки в комплекте*количество комплектов
                                        // + количество просечек второй решётки*количество второй решётки в комплекте*количество комплектов не больше максимального допустимого значения просечек заготовки
                                        // и количество ручьёв для изготовления одного комплекта*количество комплектов не больше максимального допустимого значения ручьёв заготовки
                                        while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * TkGridQuantityFirst.Text.ToInt() * iterator + TkGridLengthSecond.Text.ToInt() * TkGridQuantitySecond.Text.ToInt() * iterator,
                                                TkGridQuantityNotchesFirst.Text.ToInt() * TkGridQuantityFirst.Text.ToInt() * iterator + TkGridQuantityNotchesSecond.Text.ToInt() * TkGridQuantitySecond.Text.ToInt() * iterator,
                                                iterator * qtyStream))
                                        {
                                            qtyComplectByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthFirst = TkGridLengthFirst.Text.ToInt() * TkGridQuantityFirst.Text.ToInt() * qtyComplectByBillet + TkGridLengthSecond.Text.ToInt() * TkGridQuantitySecond.Text.ToInt() * qtyComplectByBillet;

                                        zQtyFirstGridByFirstBillet = TkGridQuantityFirst.Text.ToInt() * qtyComplectByBillet * qtyGridsByWidth;
                                        zQtySecondGridByFirstBillet = TkGridQuantitySecond.Text.ToInt() * qtyComplectByBillet * qtyGridsByWidth;
                                    }
                                }
                            }
                            // Весь комплект не помещаетя на заготовке
                            else
                            {
                                // Размещаем каждую решётку на своей заготовке

                                // Первая заготовка
                                {
                                    // Находим количество решёток, которое помещается на первую заготовку
                                    // Количество ручьёв на заготовке
                                    int qtyStreamByBillet = 0;
                                    var iterator = 1;

                                    while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                    {
                                        qtyStreamByBillet = iterator;
                                        iterator += 1;
                                    }

                                    zLengthFirst = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                    zQtyFirstGridByFirstBillet = qtyGridsByWidth * qtyStreamByBillet;
                                }

                                // Вторая заготовка
                                {
                                    int qtyStreamByBillet = 0;
                                    var iterator = 1;

                                    while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                    {
                                        qtyStreamByBillet = iterator;
                                        iterator += 1;
                                    }

                                    zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                    zWidthSecond = zWidthFirst;
                                    zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                }
                            }
                        }
                        // Вариант (2 к 2)
                        else if (TkGridQuantityNotchesFirst.Text.ToInt() == 2 && TkGridQuantityNotchesSecond.Text.ToInt() == 2)
                        {
                            // Проверяем, что весь комплект помещается на заготовке 
                            if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * TkGridQuantityFirst.Text.ToInt() + TkGridLengthSecond.Text.ToInt() * TkGridQuantitySecond.Text.ToInt(), 8, 4))
                            {
                                // Проверяем, что решётки одинаковые
                                if (TkGridLengthFirst.Text.ToInt() == TkGridLengthSecond.Text.ToInt())
                                {
                                    // Сумма длин решёток для комплекта
                                    var l = TkGridLengthFirst.Text.ToInt() * TkGridQuantityFirst.Text.ToInt() + TkGridLengthSecond.Text.ToInt() * TkGridQuantitySecond.Text.ToInt();

                                    // Сумма просечек в комплекте
                                    var qtyNotches = TkGridQuantityNotchesFirst.Text.ToInt() * TkGridQuantityFirst.Text.ToInt() + TkGridQuantityNotchesSecond.Text.ToInt() * TkGridQuantitySecond.Text.ToInt();

                                    // Количество ручьёв для получения комплекта из заготовки 
                                    var qtyStream = TkGridQuantityFirst.Text.ToInt() + TkGridQuantitySecond.Text.ToInt();

                                    // Кличество ручьёв на заготовке
                                    int qtyStreamByBillet = 0;

                                    // Ищем максимальное количество решёток, которое можно разместить на заготовке
                                    {
                                        var iterator = qtyStream;
                                        while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthFirst = TkGridLengthFirst.Text.ToInt() * qtyStreamByBillet;

                                        var qtyGrids = qtyStreamByBillet * qtyGridsByWidth;

                                        if (qtyGrids % 2 == 0)
                                        {
                                            zQtyFirstGridByFirstBillet = qtyGrids / 2;
                                            zQtySecondGridByFirstBillet = qtyGrids / 2;
                                        }
                                        else
                                        {
                                            qtyGrids = qtyGrids - 1;

                                            zQtyFirstGridByFirstBillet = (qtyGrids / 2) + 1;
                                            zQtySecondGridByFirstBillet = qtyGrids / 2;
                                        }
                                    }
                                }
                                // Решётки не одинаковые
                                else
                                {
                                    // Сумма длин решёток для комплекта
                                    var l = TkGridLengthFirst.Text.ToInt() * TkGridQuantityFirst.Text.ToInt() + TkGridLengthSecond.Text.ToInt() * TkGridQuantitySecond.Text.ToInt();

                                    // Сумма просечек в комплекте
                                    var qtyNotches = TkGridQuantityNotchesFirst.Text.ToInt() * TkGridQuantityFirst.Text.ToInt() + TkGridQuantityNotchesSecond.Text.ToInt() * TkGridQuantitySecond.Text.ToInt();

                                    // Количество ручьёв для получения комплекта из заготовки 
                                    var qtyStream = TkGridQuantityFirst.Text.ToInt() + TkGridQuantitySecond.Text.ToInt();

                                    // Кличество ручьёв на заготовке
                                    int qtyStreamByBillet = 0;

                                    // Количество комплектов на заготовке
                                    int qtyComplectByBillet = 0;

                                    // Ищем максимальное количество решёток, которое можно разместить на заготовке
                                    {
                                        // Количество комплектов, которое можно разместить на заготовке
                                        var iterator = 1;
                                        // Если длина первой решётки*количество первой решётки в комплекте*количество комплектов
                                        // + длина второй решётки*количество второй решётки в комплекте*количество комплектов не быльше максимального допустимого значения длины заготовки
                                        // и количество просечек первой решётки*количество первой решётки в комплекте*количество комплектов
                                        // + количество просечек второй решётки*количество второй решётки в комплекте*количество комплектов не больше максимального допустимого значения просечек заготовки
                                        // и количество ручьёв для изготовления одного комплекта*количество комплектов не больше максимального допустимого значения ручьёв заготовки
                                        while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * TkGridQuantityFirst.Text.ToInt() * iterator + TkGridLengthSecond.Text.ToInt() * TkGridQuantitySecond.Text.ToInt() * iterator,
                                                TkGridQuantityNotchesFirst.Text.ToInt() * TkGridQuantityFirst.Text.ToInt() * iterator + TkGridQuantityNotchesSecond.Text.ToInt() * TkGridQuantitySecond.Text.ToInt() * iterator,
                                                iterator * qtyStream))
                                        {
                                            qtyComplectByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthFirst = TkGridLengthFirst.Text.ToInt() * TkGridQuantityFirst.Text.ToInt() * qtyComplectByBillet + TkGridLengthSecond.Text.ToInt() * TkGridQuantitySecond.Text.ToInt() * qtyComplectByBillet;

                                        zQtyFirstGridByFirstBillet = TkGridQuantityFirst.Text.ToInt() * qtyComplectByBillet * qtyGridsByWidth;
                                        zQtySecondGridByFirstBillet = TkGridQuantitySecond.Text.ToInt() * qtyComplectByBillet * qtyGridsByWidth;
                                    }
                                }
                            }
                            // Весь комплект не помещаетя на заготовке
                            else
                            {
                                // Пытаемся резместь половину комплекта на одну заготовку
                                if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * 1, 4, 2))
                                {
                                    // Сумма длин решёток для половины комплекта
                                    zLengthFirst = TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * 1;

                                    zQtyFirstGridByFirstBillet = qtyGridsByWidth * 1;
                                    zQtySecondGridByFirstBillet = qtyGridsByWidth * 1;
                                }
                                // Размещаем каждую решётку на своей заготовке
                                else
                                {
                                    // Первая заготовка
                                    {
                                        // Находим количество решёток, которое помещается на первую заготовку
                                        // Количество ручьёв на заготовке
                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;

                                        while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthFirst = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * qtyStreamByBillet;
                                    }

                                    // Вторая заготовка
                                    {
                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;

                                        while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                        zWidthSecond = zWidthFirst;
                                        zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                    }
                                }
                            }
                        }
                        // Вариант (3 к 1) или (1 к 3)
                        else if ((TkGridQuantityNotchesFirst.Text.ToInt() == 1 && TkGridQuantityNotchesSecond.Text.ToInt() == 3) || (TkGridQuantityNotchesFirst.Text.ToInt() == 3 && TkGridQuantityNotchesSecond.Text.ToInt() == 1))
                        {
                            // Проверяем, что весь комплект помещается на заготовке
                            if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * TkGridQuantityFirst.Text.ToInt() + TkGridLengthSecond.Text.ToInt() * TkGridQuantitySecond.Text.ToInt(), 6, 4))
                            {
                                // Сумма длин решёток для комплекта
                                var l = TkGridLengthFirst.Text.ToInt() * TkGridQuantityFirst.Text.ToInt() + TkGridLengthSecond.Text.ToInt() * TkGridQuantitySecond.Text.ToInt();

                                zLengthFirst = l;

                                // Количество ручьёв для получения комплекта из заготовки 
                                var qtyStream = TkGridQuantityFirst.Text.ToInt() + TkGridQuantitySecond.Text.ToInt();

                                // Кличество ручьёв на заготовке
                                int qtyStreamByBillet = 0;

                                // Количество ручьёв на заготовке равно количеству решёток для комплекта
                                qtyStreamByBillet = qtyStream;

                                var qtyGrids = qtyStreamByBillet * qtyGridsByWidth;

                                zQtyFirstGridByFirstBillet = qtyGridsByWidth * TkGridQuantityFirst.Text.ToInt();
                                zQtySecondGridByFirstBillet = qtyGridsByWidth * TkGridQuantitySecond.Text.ToInt();
                            }
                            // Весь комплект не помещается на заготовке, используем добивочную
                            else
                            {
                                // -- Для первой заготовки 2 шт. 1 реш(с 1 прос). и 1 шт. 2 реш(c 3 прос).
                                // -- Для первой заготовки 1 шт. 1 реш(с 1 прос). и 1 шт. 2 реш(с 3 прос).
                                // -- Для первой заготовки 1--3 шт. 1 реш(с 1 прос).

                                //Если у первой решётки количество просечек 1
                                if (TkGridQuantityNotchesFirst.Text.ToInt() == 1)
                                {
                                    var l1 = TkGridLengthFirst.Text.ToInt();
                                    var l2 = TkGridLengthSecond.Text.ToInt();

                                    // -- Для первой заготовки 2 шт. 1 реш. и 1 шт. 2 реш.
                                    // 2+1=3<6 2*1+1*3=5<12
                                    if (CheckBlankLimit(l1 * 2 + l2 * 1, 5, 3))
                                    {
                                        zLengthFirst = l1 * 2 + l2 * 1;

                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * 2;
                                        zQtySecondGridByFirstBillet = qtyGridsByWidth * 1;

                                        // Ищем добивочную заготовку
                                        // Находим решётку, у которой количество в комплекте больше, находим её количество в первой заготовке
                                        int q = zQtyFirstGridByFirstBillet;
                                        // Делим найденное количество на количество этой решётки в комплекте
                                        double doubleQ = (double)((double)q / (double)TkGridQuantityFirst.Text.ToDouble());
                                        // Берём целую часть от полученного числа
                                        q = doubleQ.ToInt();
                                        // Умножаем получившееся число на количество второй решётки в комплекте (той, у которой количество в комплекте меньше)
                                        q = q * TkGridQuantitySecond.Text.ToInt();
                                        // Сравниваем получившееся число с количеством решёток из первой заготовки для второй решётки (той, у которой количество в комплекте меньше)
                                        // Если получившееся число больше, то добивочная решётка та, у которой количество в комплекте меньше
                                        if (q > zQtySecondGridByFirstBillet)
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте меньше, а значит количество просечек больше

                                            // -- 1 шт. решётки с 3 просечкми (сумма просечек 3 < 12)
                                            // -- 2 шт. решётки с 3 просечкми (сумма просечек 6 < 12)
                                            // -- 3 шт. решётки с 3 просечкми (сумма просечек 9 < 12)
                                            // -- 4 шт. решётки с 3 просечкми (сумма просечек 12 = 12)

                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;

                                        }
                                        // Если получившееся число меньше, то добивочная решётка та, у которой количество в комплекте больше
                                        else
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте больше, а значит количество просечек меньше

                                            // Находим количество решёток, которое помещается на вторую заготовку

                                            // Количество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtyFirstGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }
                                    }
                                    // -- Для первой заготовки 1 шт. 1 реш(с 1 прос). и 1 шт. 2 реш(с 3 прос).
                                    else if (CheckBlankLimit(l1 * 1 + l2 * 1, 4, 2))
                                    {
                                        zLengthFirst = l1 * 1 + l2 * 1;

                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * 1;
                                        zQtySecondGridByFirstBillet = qtyGridsByWidth * 1;

                                        // Ищем добивочную заготовку
                                        // Находим решётку, у которой количество в комплекте больше, находим её количество в первой заготовке
                                        int q = zQtyFirstGridByFirstBillet;
                                        // Делим найденное количество на количество этой решётки в комплекте
                                        double doubleQ = (double)((double)q / (double)TkGridQuantityFirst.Text.ToDouble());
                                        // Берём целую часть от полученного числа
                                        q = doubleQ.ToInt();
                                        // Умножаем получившееся число на количество второй решётки в комплекте (той, у которой количество в комплекте меньше)
                                        q = q * TkGridQuantitySecond.Text.ToInt();
                                        // Сравниваем получившееся число с количеством решёток из первой заготовки для второй решётки (той, у которой количество в комплекте меньше)
                                        // Если получившееся число больше, то добивочная решётка та, у которой количество в комплекте меньше
                                        if (q > zQtySecondGridByFirstBillet)
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте меньше, а значит количество просечек больше

                                            // -- 1 шт. решётки с 3 просечкми (сумма просечек 3 < 12)
                                            // -- 2 шт. решётки с 3 просечкми (сумма просечек 6 < 12)
                                            // -- 3 шт. решётки с 3 просечкми (сумма просечек 9 < 12)
                                            // -- 4 шт. решётки с 3 просечкми (сумма просечек 12 = 12)

                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;

                                        }
                                        // Если получившееся число меньше, то добивочная решётка та, у которой количество в комплекте больше
                                        else
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте больше, а значит количество просечек меньше

                                            // Находим количество решёток, которое помещается на вторую заготовку

                                            // Количество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtyFirstGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }
                                    }
                                    // -- Для первой заготовки 1--3 шт. 1 реш(с 1 прос).
                                    else
                                    {
                                        // Для первой заготовки
                                        {
                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthFirst = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();

                                            zQtyFirstGridByFirstBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }

                                        // Для второй заготовки
                                        {
                                            // Количество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }

                                    }
                                }
                                else if (TkGridQuantityNotchesSecond.Text.ToInt() == 1)
                                {
                                    var l1 = TkGridLengthFirst.Text.ToInt();
                                    var l2 = TkGridLengthSecond.Text.ToInt();

                                    // -- Для первой заготовки 2 шт. 1 реш. и 1 шт. 2 реш.
                                    // 2+1=3<6 2*1+1*3=5<12
                                    if (CheckBlankLimit(l1 * 1 + l2 * 2, 5, 3))
                                    {
                                        zLengthFirst = l1 * 1 + l2 * 2;

                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * 1;
                                        zQtySecondGridByFirstBillet = qtyGridsByWidth * 2;

                                        // Ищем добивочную заготовку
                                        // Находим решётку, у которой количество в комплекте больше, находим её количество в первой заготовке
                                        int q = zQtySecondGridByFirstBillet;
                                        // Делим найденное количество на количество этой решётки в комплекте
                                        double doubleQ = (double)((double)q / (double)TkGridQuantitySecond.Text.ToDouble());
                                        // Берём целую часть от полученного числа
                                        q = doubleQ.ToInt();
                                        // Умножаем получившееся число на количество второй решётки в комплекте (той, у которой количество в комплекте меньше)
                                        q = q * TkGridQuantityFirst.Text.ToInt();
                                        // Сравниваем получившееся число с количеством решёток из первой заготовки для второй решётки (той, у которой количество в комплекте меньше)
                                        // Если получившееся число больше, то добивочная решётка та, у которой количество в комплекте меньше
                                        if (q > zQtyFirstGridByFirstBillet)
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте меньше, а значит количество просечек больше

                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtyFirstGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;

                                        }
                                        // Если получившееся число меньше, то добивочная решётка та, у которой количество в комплекте больше
                                        else
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте больше, а значит количество просечек меньше

                                            // Находим количество решёток, которое помещается на вторую заготовку

                                            // Количество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }
                                    }
                                    // -- Для первой заготовки 1 шт. 1 реш(с 1 прос). и 1 шт. 2 реш(с 3 прос).
                                    else if (CheckBlankLimit(l1 * 1 + l2 * 1, 4, 2))
                                    {
                                        zLengthFirst = l1 * 1 + l2 * 1;

                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * 1;
                                        zQtySecondGridByFirstBillet = qtyGridsByWidth * 1;

                                        // Ищем добивочную заготовку
                                        // Находим решётку, у которой количество в комплекте больше, находим её количество в первой заготовке
                                        int q = zQtySecondGridByFirstBillet;
                                        // Делим найденное количество на количество этой решётки в комплекте
                                        double doubleQ = (double)((double)q / (double)TkGridQuantitySecond.Text.ToDouble());
                                        // Берём целую часть от полученного числа
                                        q = doubleQ.ToInt();
                                        // Умножаем получившееся число на количество второй решётки в комплекте (той, у которой количество в комплекте меньше)
                                        q = q * TkGridQuantityFirst.Text.ToInt();
                                        // Сравниваем получившееся число с количеством решёток из первой заготовки для второй решётки (той, у которой количество в комплекте меньше)
                                        // Если получившееся число больше, то добивочная решётка та, у которой количество в комплекте меньше
                                        if (q > zQtyFirstGridByFirstBillet)
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте меньше, а значит количество просечек больше

                                            // Количество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtyFirstGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;

                                        }
                                        // Если получившееся число меньше, то добивочная решётка та, у которой количество в комплекте больше
                                        else
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте больше, а значит количество просечек меньше

                                            // Находим количество решёток, которое помещается на вторую заготовку

                                            // Количество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }
                                    }
                                    // -- Для первой заготовки 1--3 шт. 1 реш(с 1 прос).
                                    else
                                    {
                                        // Для первой заготовки
                                        {
                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthFirst = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();

                                            zQtyFirstGridByFirstBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }

                                        // Для второй заготовки
                                        {
                                            // Количество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }
                                    }
                                }
                            }
                        }
                        // Вариант (2 к 3) или (3 к 2) 
                        else if ((TkGridQuantityNotchesFirst.Text.ToInt() == 3 && TkGridQuantityNotchesSecond.Text.ToInt() == 2) || (TkGridQuantityNotchesFirst.Text.ToInt() == 2 && TkGridQuantityNotchesSecond.Text.ToInt() == 3))
                        {
                            // Проверяем, что весь комплект помещается на одной заготовке (в этом случае можно проверять соответствие только по длине, т.к. по количеству ручьёв у нас 3+2=5<6 и по количеству просечек 2*3+3*2=12=12)
                            // Весь комплект помещается на одной заготовке
                            if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * TkGridQuantityFirst.Text.ToInt() + TkGridLengthSecond.Text.ToInt() * TkGridQuantitySecond.Text.ToInt(), 12, 5))
                            {
                                // Сумма длин решёток для комплекта
                                var l = TkGridLengthFirst.Text.ToInt() * TkGridQuantityFirst.Text.ToInt() + TkGridLengthSecond.Text.ToInt() * TkGridQuantitySecond.Text.ToInt();

                                zLengthFirst = l;

                                // Количество ручьёв для получения комплекта из заготовки 
                                var qtyStream = TkGridQuantityFirst.Text.ToInt() + TkGridQuantitySecond.Text.ToInt();

                                // Кличество ручьёв на заготовке
                                int qtyStreamByBillet = 0;

                                // Количество ручьёв на заготовке равно количеству решёток для комплекта
                                qtyStreamByBillet = qtyStream;

                                var qtyGrids = qtyStreamByBillet * qtyGridsByWidth;

                                zQtyFirstGridByFirstBillet = qtyGridsByWidth * TkGridQuantityFirst.Text.ToInt();
                                zQtySecondGridByFirstBillet = qtyGridsByWidth * TkGridQuantitySecond.Text.ToInt();

                                //zQtyFirstGridByFirstBillet = (qtyGrids / (TkGridQuantityFirst.Text.ToInt() + TkGridQuantitySecond.Text.ToInt())) * TkGridQuantityFirst.Text.ToInt();
                                //zQtySecondGridByFirstBillet = (qtyGrids / (TkGridQuantityFirst.Text.ToInt() + TkGridQuantitySecond.Text.ToInt())) * TkGridQuantitySecond.Text.ToInt();

                                // Возможно тут можно было бы добавить проверку на то, что может поместиться ещё одна решётка на заготовку. Хотя про это разговора не было.
                            }
                            // Весь комплект не помещается на заготовке, будет использоваться вторая заготовка
                            else
                            {
                                // Для вариантов (2 к 3)(3 к 2) и (3 к 4)(4 к 3) ипользуется следующее распределение:
                                // -- Для первой заготовки 2 шт. 1 реш. и 1 шт. 2 реш.
                                // -- Для второй заготовки 1шт. 1 реш. и 2 шт. 2 реш.

                                var l1 = TkGridLengthFirst.Text.ToInt();
                                var l2 = TkGridLengthSecond.Text.ToInt();

                                // Проверяем, что это распределение не противоречит условиям.
                                // По количеству ручьёв у нас 2+1=3<6 и 1+2=3<6
                                // По количеству просечек у нас 2*3+1*2=8<12 и 2*2+1*3=7<12
                                if (CheckBlankLimit(l1 * 2 + l2 * 1, TkGridQuantityNotchesFirst.Text.ToInt() * 2 + TkGridQuantityNotchesSecond.Text.ToInt() * 1, 3)
                                    && CheckBlankLimit(l1 * 1 + l2 * 2, TkGridQuantityNotchesFirst.Text.ToInt() * 1 + TkGridQuantityNotchesSecond.Text.ToInt() * 2, 3))
                                {
                                    zLengthFirst = l1 * 2 + l2 * 1;

                                    zQtyFirstGridByFirstBillet = qtyGridsByWidth * 2;
                                    zQtySecondGridByFirstBillet = qtyGridsByWidth * 1;

                                    zLengthSecond = l1 * 1 + l2 * 2;
                                    zWidthSecond = zWidthFirst;

                                    zQtyFirstGridBySecondBillet = qtyGridsByWidth * 1;
                                    zQtySecondGridBySecondBillet = qtyGridsByWidth * 2;
                                }
                                // Распределение противоречит условиям
                                else
                                {
                                    // размещаем по одной решётке на первой заготовке и используем добивочную заготовку
                                    if (CheckBlankLimit(l1 + l2, 5, 2))
                                    {
                                        zLengthFirst = l1 + l2;

                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * 1;
                                        zQtySecondGridByFirstBillet = qtyGridsByWidth * 1;

                                        // Ищем добивочную заготовку
                                        // Если  количество первой решётки в комплекте больше, чем второй
                                        if (TkGridQuantityFirst.Text.ToInt() == 3)
                                        {
                                            // Находим количество этой решётки в первой заготовке
                                            int q = zQtyFirstGridByFirstBillet;
                                            // Делим найденное количество на количество этой решётки в комплекте
                                            double doubleQ = (double)((double)q / (double)TkGridQuantityFirst.Text.ToDouble());
                                            // Берём целую часть от полученного числа
                                            q = doubleQ.ToInt();
                                            // Умножаем получившееся число на количество второй решётки в комплекте (той, у которой количество в комплекте меньше)
                                            q = q * TkGridQuantitySecond.Text.ToInt();

                                            // Сравниваем получившееся число с количеством решёток из первой заготовки для второй решётки (той, у которой количество в комплекте меньше)
                                            // Если получившееся число больше, то добивочная решётка та, у которой количество в комплекте меньше
                                            if (q > zQtySecondGridByFirstBillet)
                                            {
                                                // Добивочная решётка та, у которой количество в комплекте меньше, а значит количество просечек больше
                                                // В данном случае мы можем расположить решётки на вторую заготовку двумя способами
                                                // -- 1 шт. решётки с 3 просечкми (сумма просечек 3 < 12)
                                                // -- 2 шт. решётки с 3 просечкми (сумма просечек 6 < 12)
                                                // -- 3 шт. решётки с 3 просечкми (сумма просечек 9 < 12)
                                                // -- 4 шт. решётки с 3 просечкми (сумма просечек 12 = 12)

                                                // Количество ручьёв на заготовке
                                                int qtyStreamByBillet = 0;
                                                var iterator = 1;

                                                while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                                {
                                                    qtyStreamByBillet = iterator;
                                                    iterator += 1;
                                                }

                                                zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                                zWidthSecond = zWidthFirst;

                                                zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                            }
                                            // Если получившееся число меньше, то добивочная решётка та, у которой количество в комплекте больше
                                            else
                                            {
                                                // Добивочная решётка та, у которой количество в комплекте больше, а значит количество просечек меньше

                                                // Находим количество решёток, которое помещается на вторую заготовку

                                                // Кличество ручьёв на заготовке
                                                int qtyStreamByBillet = 0;
                                                var iterator = 1;

                                                while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                                {
                                                    qtyStreamByBillet = iterator;
                                                    iterator += 1;
                                                }

                                                zLengthSecond = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                                zWidthSecond = zWidthFirst;

                                                zQtyFirstGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                            }
                                        }
                                        // Если количество второй решётки в комплекте больше, чем первой
                                        else if (TkGridQuantitySecond.Text.ToInt() == 3)
                                        {
                                            // Расчёт второй заготовки
                                            // Находим решётку, у которой количество в комплекте больше, находим её количество в первой заготовке
                                            int q = zQtySecondGridByFirstBillet;
                                            // Делим найденное количество на количество этой решётки в комплекте
                                            double doubleQ = (double)((double)q / (double)TkGridQuantitySecond.Text.ToDouble());
                                            // Берём целую часть от полученного числа
                                            q = doubleQ.ToInt();
                                            // Умножаем получившееся число на количество второй решётки в комплекте (той, у которой количество в комплекте меньше)
                                            q = q * TkGridQuantityFirst.Text.ToInt();
                                            // Сравниваем получившееся число с количеством решёток из первой заготовки для второй решётки (той, у которой количество в комплекте меньше)
                                            // Если получившееся число больше, то добивочная решётка та, у которой количество в комплекте меньше
                                            if (q > zQtyFirstGridByFirstBillet)
                                            {
                                                // Добивочная решётка та, у которой количество в комплекте меньше, а значит количество просечек больше
                                                // В данном случае мы можем расположить решётки на вторую заготовку двумя способами
                                                // -- 1 шт. решётки с 3 просечкми (сумма просечек 3 < 12)
                                                // -- 2 шт. решётки с 3 просечкми (сумма просечек 6 < 12)
                                                // -- 3 шт. решётки с 3 просечкми (сумма просечек 9 < 12)
                                                // -- 4 шт. решётки с 3 просечкми (сумма просечек 12 = 12)

                                                // Кличество ручьёв на заготовке
                                                int qtyStreamByBillet = 0;
                                                var iterator = 1;

                                                while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                                {
                                                    qtyStreamByBillet = iterator;
                                                    iterator += 1;
                                                }

                                                zLengthSecond = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                                zWidthSecond = zWidthFirst;

                                                zQtyFirstGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                            }
                                            // Если получившееся число меньше, то добивочная решётка та, у которой количество в комплекте больше
                                            else
                                            {
                                                // Добивочная решётка та, у которой количество в комплекте больше, а значит количество просечек меньше

                                                // Находим количество решёток, которое помещается на вторую заготовку

                                                // Кличество ручьёв на заготовке
                                                int qtyStreamByBillet = 0;
                                                var iterator = 1;

                                                while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                                {
                                                    qtyStreamByBillet = iterator;
                                                    iterator += 1;
                                                }

                                                zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                                zWidthSecond = zWidthFirst;

                                                zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                            }
                                        }
                                    }
                                    // Размещаем каждую решётку на своей заготовке
                                    else
                                    {
                                        // Для первой заготовки
                                        {
                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthFirst = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();

                                            zQtyFirstGridByFirstBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }

                                        // Для второй заготовки
                                        {
                                            // Количество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }
                                    }
                                }
                            }
                        }
                        // Вариант (2 к 6) или (6 к 2)
                        else if ((TkGridQuantityNotchesFirst.Text.ToInt() == 6 && TkGridQuantityNotchesSecond.Text.ToInt() == 2) || (TkGridQuantityNotchesFirst.Text.ToInt() == 2 && TkGridQuantityNotchesSecond.Text.ToInt() == 6))
                        {
                            // Весь комплект не поместится на заготовке, т.к. по количеству ручьёв у нас 2+6=8>6
                            // Скорее всего они будут пытаться комбинировать разные решётки на одной заготовке, чтобы по ходу производства сразу их собирать
                            // (а значит вариант, когда 6 решёток на одной заготовке и 2 решётки на второй рассматриваем в последнюю очередь)
                            // Варианты:
                            // 1 реш с 6 просечками и 3 реш с 2 просечками (просечки 1*6+3*2=12=12 ручьи 1+3=4<6)
                            // 1 реш с 6 просечками и 2 реш с 2 просечками (просечки 1*6+2*2=10<12 ручьи 1+2=3<6)
                            // 1 реш с 6 просечками и 1 реш с 2 просечками (просечки 1*6+1*2=8<12 ручьи 1+1=2<6)

                            // Если первая решётка имеет 2 просечки
                            if (TkGridQuantityNotchesFirst.Text.ToInt() == 2)
                            {
                                // Проверяем варианты
                                // 1 реш с 6 просечками и 3 реш с 2 просечками (просечки 1*6+3*2=12=12 ручьи 1+3=4<6)
                                // 1 реш с 6 просечками и 2 реш с 2 просечками (просечки 1*6+2*2=10<12 ручьи 1+2=3<6)
                                // 1 реш с 6 просечками и 1 реш с 2 просечками (просечки 1*6+1*2=8<12 ручьи 1+1=2<6)
                                for (int i = 3; i > 0; i--)
                                {
                                    // Проверяем, что не нарушено условие по максимальной длине заготовки
                                    if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * i + TkGridLengthSecond.Text.ToInt() * 1,
                                        TkGridQuantityNotchesFirst.Text.ToInt() * i + TkGridQuantityNotchesSecond.Text.ToInt() * 1,
                                        i + 1))
                                    {
                                        zLengthFirst = TkGridLengthFirst.Text.ToInt() * i + TkGridLengthSecond.Text.ToInt() * 1;
                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * i;
                                        zQtySecondGridByFirstBillet = qtyGridsByWidth * 1;

                                        // Расчёт второй заготовки
                                        // Находим решётку, у которой количество в комплекте больше, находим её количество в первой заготовке
                                        int q = zQtyFirstGridByFirstBillet;
                                        // Делим найденное количество на количество этой решётки в комплекте
                                        double doubleQ = (double)((double)q / (double)TkGridQuantityFirst.Text.ToDouble());
                                        // Берём целую часть от полученного числа
                                        q = doubleQ.ToInt();
                                        // Умножаем получившееся число на количество второй решётки в комплекте (той, у которой количество в комплекте меньше)
                                        q = q * TkGridQuantitySecond.Text.ToInt();
                                        // Сравниваем получившееся число с количеством решёток из первой заготовки для второй решётки (той, у которой количество в комплекте меньше)
                                        // Если получившееся число больше, то добивочная решётка та, у которой количество в комплекте меньше
                                        if (q > zQtySecondGridByFirstBillet)
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте меньше, а значит количество просечек больше
                                            // В данном случае мы можем расположить решётки на вторую заготовку двумя способами
                                            // -- 1 шт. решётки с 6 просечкми (сумма просечек 6 < 12)
                                            // -- 2 шт. решётки с 6 просечкми (сумма просечек 12 = 12)

                                            // Количество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }
                                        // Если получившееся число меньше, то добивочная решётка та, у которой количество в комплекте больше
                                        else
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте больше, а значит количество просечек меньше

                                            // Находим количество решёток, которое помещается на вторую заготовку

                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtyFirstGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }

                                        break;
                                    }
                                }
                            }
                            // Если вторая решётка имеет 2 просечки
                            else if (TkGridQuantityNotchesSecond.Text.ToInt() == 2)
                            {
                                // Проверяем варианты
                                // 1 реш с 6 просечками и 3 реш с 2 просечками (просечки 1*6+3*2=12=12 ручьи 1+3=4<6)
                                // 1 реш с 6 просечками и 2 реш с 2 просечками (просечки 1*6+2*2=10<12 ручьи 1+2=3<6)
                                // 1 реш с 6 просечками и 1 реш с 2 просечками (просечки 1*6+1*2=8<12 ручьи 1+1=2<6)
                                for (int i = 3; i > 0; i--)
                                {
                                    // Проверяем, что не нарушено условие по максимальной длинне заготовки
                                    if (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * i + TkGridLengthFirst.Text.ToInt() * 1,
                                        TkGridQuantityNotchesSecond.Text.ToInt() * i + TkGridQuantityNotchesFirst.Text.ToInt() * 1,
                                        i + 1))
                                    {
                                        zLengthFirst = TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * i;
                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * 1;
                                        zQtySecondGridByFirstBillet = qtyGridsByWidth * i;

                                        // Расчёт второй заготовки
                                        // Находим решётку, у которой количество в комплекте больше, находим её количество в первой заготовке
                                        int q = zQtySecondGridByFirstBillet;
                                        // Делим найденное количество на количество этой решётки в комплекте
                                        double doubleQ = (double)((double)q / (double)TkGridQuantitySecond.Text.ToDouble());
                                        // Берём целую часть от полученного числа
                                        q = doubleQ.ToInt();
                                        // Умножаем получившееся число на количество второй решётки в комплекте (той, у которой количество в комплекте меньше)
                                        q = q * TkGridQuantityFirst.Text.ToInt();
                                        // Сравниваем получившееся число с количеством решёток из первой заготовки для второй решётки (той, у которой количество в комплекте меньше)
                                        // Если получившееся число больше, то добивочная решётка та, у которой количество в комплекте меньше
                                        if (q > zQtyFirstGridByFirstBillet)
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте меньше, а значит количество просечек больше
                                            // В данном случае мы можем расположить решётки на вторую заготовку двумя способами
                                            // -- 1 шт. решётки с 6 просечкми (сумма просечек 6 < 12)
                                            // -- 2 шт. решётки с 6 просечкми (сумма просечек 12 = 12)

                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtyFirstGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }
                                        // Если получившееся число меньше, то добивочная решётка та, у которой количество в комплекте больше
                                        else
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте больше, а значит количество просечек меньше

                                            // Находим количество решёток, которое помещается на вторую заготовку

                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }

                                        break;
                                    }
                                }
                            }

                            // Если не удалось разместить разные решётки на одной заготовке, то размещаем каждую решётку на своей заготовке
                            if (zLengthFirst == 0)
                            {
                                // Для первой заготовки
                                {
                                    // Кличество ручьёв на заготовке
                                    int qtyStreamByBillet = 0;
                                    var iterator = 1;

                                    while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                    {
                                        qtyStreamByBillet = iterator;
                                        iterator += 1;
                                    }

                                    zLengthFirst = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();

                                    zQtyFirstGridByFirstBillet = qtyGridsByWidth * qtyStreamByBillet;
                                }

                                // Для второй заготовки
                                {
                                    // Количество ручьёв на заготовке
                                    int qtyStreamByBillet = 0;
                                    var iterator = 1;

                                    while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                    {
                                        qtyStreamByBillet = iterator;
                                        iterator += 1;
                                    }

                                    zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                    zWidthSecond = zWidthFirst;

                                    zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                }
                            }
                        }
                        // Вариант (3 к 3)
                        else if (TkGridQuantityNotchesFirst.Text.ToInt() == 3 && TkGridQuantityNotchesSecond.Text.ToInt() == 3)
                        {
                            // Весь комплект не помещяется на зоготовк из-за количества просечек (3*3+3*3=18)
                            // но и добивочную загогтовку нет смысла использовать так-как решётки одинаковые
                            // поэтому попробуем просто размещать максимально возможное количество решёток на одной заготовке

                            // Проверяем что решётки одинаковые
                            if (TkGridLengthFirst.Text.ToInt() == TkGridLengthSecond.Text.ToInt())
                            {
                                // Кличество ручьёв на заготовке
                                int qtyStreamByBillet = 0;
                                var iterator = 1;

                                while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                {
                                    qtyStreamByBillet = iterator;
                                    iterator += 1;
                                }

                                zLengthFirst = TkGridLengthFirst.Text.ToInt() * qtyStreamByBillet;
                                var qtyGrids = qtyStreamByBillet * qtyGridsByWidth;

                                if (qtyGrids % 2 == 0)
                                {
                                    zQtyFirstGridByFirstBillet = qtyGrids / 2;
                                    zQtySecondGridByFirstBillet = qtyGrids / 2;
                                }
                                else
                                {
                                    qtyGrids = qtyGrids - 1;

                                    zQtyFirstGridByFirstBillet = (qtyGrids / 2) + 1;
                                    zQtySecondGridByFirstBillet = qtyGrids / 2;
                                }
                            }
                            // Решётки не одинаковые
                            else
                            {
                                // не используется 
                                {
                                    // Варианты размещения (по количеству просечек максимальное количество решёток, которое можно разместить на заготовке = 4; 3*4=12=12)

                                    // Наверное самый оптимальнй вариант без использования второй заготовки
                                    // 2 шт. 1 реш.(3 прос.) и 2 шт. 2 реш.(3 прос.) (количество просечек 2*3+2*3=12=12)

                                    // Менее оптимальный, но тоже довольно логичный вариант
                                    // 2 шт. 1 реш.(3 прос.) и 1 шт. 2 реш.(3 прос.) (количество просечек 2*3+1*3=9<12)
                                    // Тогда в качестве добивочной можно попробовать обратный вариант 1 шт. 1 реш.(3 прос.) и 2 шт. 2. реш.(3 прос.) (количество просечек 1*3+2*3=9<12)
                                    //  1 шт. 1 реш.(3 прос.) и 1 шт. 2 реш.(3 прос.) (количество просечек 1*3+1*3=6<12)
                                    // Тогда можно обойтись без добивочной заготовки

                                    // Наверное наиболее не логичный вариант из вариантов с размещением обеих решёток на заготовке
                                    // 3 шт. 1 реш.(3 прос.) и 1 шт. 2 реш.(3 прос.) (количество просечек 3*3+1*3=12=12)
                                    // Тогда на добивочную заготовку скорее всего пойдёт толькр вторая решётка
                                }

                                // Размещаем либо в 2 либо в 4 ручья, ориентироваться на min и max длину заготовки, используется только одна заготовка
                                // 4 ручья -- 2 шт. 1 реш.(3 прос.) и 2 шт. 2 реш.(3 прос.) (речьи 2+2=4<6) (просечки 2*3+2*3=12=12)
                                // 2 ручья -- 1 шт. 1 реш.(3 прос.) и 1 шт. 2 реш.(3 прос.) (речьи 1+1=2<6) (просечки 1*3+1*3=6<12)

                                // проверяем вариант 4 ручья -- 2 шт. 1 реш.(3 прос.) и 2 шт. 2 реш.(3 прос.) (ручьи 2+2=4<6) (просечки 2*3+2*3=12=12)
                                if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * 2 + TkGridLengthSecond.Text.ToInt() * 2,
                                    TkGridQuantityNotchesFirst.Text.ToInt() * 2 + TkGridQuantityNotchesSecond.Text.ToInt() * 2,
                                    4))
                                {
                                    zLengthFirst = TkGridLengthFirst.Text.ToInt() * 2 + TkGridLengthSecond.Text.ToInt() * 2;

                                    zQtyFirstGridByFirstBillet = qtyGridsByWidth * 2;
                                    zQtySecondGridByFirstBillet = qtyGridsByWidth * 2;
                                }
                                // 2 ручья -- 1 шт. 1 реш.(3 прос.) и 1 шт. 2 реш.(3 прос.) (ручьи 1+1=2<6) (просечки 1*3+1*3=6<12)
                                else if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * 1,
                                    TkGridQuantityNotchesFirst.Text.ToInt() * 1 + TkGridQuantityNotchesSecond.Text.ToInt() * 1,
                                    2))
                                {
                                    zLengthFirst = TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * 1;

                                    zQtyFirstGridByFirstBillet = qtyGridsByWidth * 1;
                                    zQtySecondGridByFirstBillet = qtyGridsByWidth * 1;
                                }

                                // Если не удалось разместить разные решётки на одной заготовке, то размещаем каждую решётку на своей заготовке
                                if (zLengthFirst == 0)
                                {
                                    // Для первой заготовки
                                    {
                                        // Кличество ручьёв на заготовке
                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;

                                        while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthFirst = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();

                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * qtyStreamByBillet;
                                    }

                                    // Для второй заготовки
                                    {
                                        // Количество ручьёв на заготовке
                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;

                                        while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                        zWidthSecond = zWidthFirst;

                                        zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                    }
                                }
                            }
                        }
                        // Вариант (4 к 1) или (1 к 4)
                        else if ((TkGridQuantityNotchesFirst.Text.ToInt() == 4 && TkGridQuantityNotchesSecond.Text.ToInt() == 1) || (TkGridQuantityNotchesFirst.Text.ToInt() == 1 && TkGridQuantityNotchesSecond.Text.ToInt() == 4))
                        {
                            // -- 1 шт. реш  с 4 просечками и 1 шт. реш с 1 просечкой (просечки 5<12) (речьёв 2<6)
                            // -- 1 шт. реш  с 4 просечками и 2 шт. реш с 1 просечкой (просечки 6<12) (речьёв 3<6)
                            // -- 1 шт. реш  с 4 просечками и 3 шт. реш с 1 просечкой (просечки 7<12) (речьёв 4<6)
                            // -- 1 шт. реш  с 4 просечками и 4 шт. реш с 1 просечкой (просечки 8<12) (речьёв 5<6)

                            // Если первая решётка имеет 1 просечку
                            if (TkGridQuantityNotchesFirst.Text.ToInt() == 1)
                            {
                                for (int i = 4; i > 0; i--)
                                {
                                    // Проверяем, что не нарушено условие по максимальной длинне заготовки
                                    if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * i + TkGridLengthSecond.Text.ToInt() * 1,
                                        TkGridQuantityNotchesFirst.Text.ToInt() * i + TkGridQuantityNotchesSecond.Text.ToInt() * 1,
                                        i + 1))
                                    {
                                        zLengthFirst = TkGridLengthFirst.Text.ToInt() * i + TkGridLengthSecond.Text.ToInt() * 1;
                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * i;
                                        zQtySecondGridByFirstBillet = qtyGridsByWidth * 1;

                                        // Если полный комплект (-- 1 шт. реш  с 4 просечками и 4 шт. реш с 1 просечкой),
                                        // то не используем добивочную решётку
                                        if (i == 4)
                                        {
                                            break;
                                        }

                                        // Расчёт второй заготовки
                                        // Находим решётку, у которой количество в комплекте больше, находим её количество в первой заготовке
                                        int q = zQtyFirstGridByFirstBillet;
                                        // Делим найденное количество на количество этой решётки в комплекте
                                        double doubleQ = (double)((double)q / (double)TkGridQuantityFirst.Text.ToDouble());
                                        // Берём целую часть от полученного числа
                                        q = doubleQ.ToInt();
                                        // Умножаем получившееся число на количество второй решётки в комплекте (той, у которой количество в комплекте меньше)
                                        q = q * TkGridQuantitySecond.Text.ToInt();
                                        // Сравниваем получившееся число с количеством решёток из первой заготовки для второй решётки (той, у которой количество в комплекте меньше)
                                        // Если получившееся число больше, то добивочная решётка та, у которой количество в комплекте меньше
                                        if (q > zQtySecondGridByFirstBillet)
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте меньше, а значит количество просечек больше
                                            // В данном случае мы можем расположить решётки на вторую заготовку двумя способами
                                            // -- 1 шт. решётки с 4 просечкми (сумма просечек 4 < 12)
                                            // -- 2 шт. решётки с 4 просечкми (сумма просечек 8 < 12)
                                            // -- 3 шт. решётки с 4 просечкми (сумма просечек 12 = 12)

                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }
                                        // Если получившееся число меньше, то добивочная решётка та, у которой количество в комплекте больше
                                        else
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте больше, а значит количество просечек меньше

                                            // Находим количество решёток, которое помещается на вторую заготовку

                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtyFirstGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }

                                        break;
                                    }
                                }
                            }
                            // Если вторая решётка имеет 1 просечку
                            else
                            {
                                for (int i = 4; i > 0; i--)
                                {
                                    // Проверяем, что не нарушено условие по максимальной длинне заготовки
                                    if (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * i + TkGridLengthFirst.Text.ToInt() * 1,
                                        TkGridQuantityNotchesSecond.Text.ToInt() * i + TkGridQuantityNotchesFirst.Text.ToInt() * 1,
                                        i + 1))
                                    {
                                        zLengthFirst = TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * i;
                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * 1;
                                        zQtySecondGridByFirstBillet = qtyGridsByWidth * i;

                                        // Если полный комплект (-- 1 шт. реш  с 4 просечками и 4 шт. реш с 1 просечкой),
                                        // то не используем добивочную решётку
                                        if (i == 4)
                                        {
                                            break;
                                        }

                                        // Расчёт второй заготовки
                                        // Находим решётку, у которой количество в комплекте больше, находим её количество в первой заготовке
                                        int q = zQtySecondGridByFirstBillet;
                                        // Делим найденное количество на количество этой решётки в комплекте
                                        double doubleQ = (double)((double)q / (double)TkGridQuantitySecond.Text.ToDouble());
                                        // Берём целую часть от полученного числа
                                        q = doubleQ.ToInt();
                                        // Умножаем получившееся число на количество второй решётки в комплекте (той, у которой количество в комплекте меньше)
                                        q = q * TkGridQuantityFirst.Text.ToInt();
                                        // Сравниваем получившееся число с количеством решёток из первой заготовки для второй решётки (той, у которой количество в комплекте меньше)
                                        // Если получившееся число больше, то добивочная решётка та, у которой количество в комплекте меньше
                                        if (q > zQtyFirstGridByFirstBillet)
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте меньше, а значит количество просечек больше
                                            // В данном случае мы можем расположить решётки на вторую заготовку двумя способами
                                            // -- 1 шт. решётки с 4 просечкми (сумма просечек 4 < 12)
                                            // -- 2 шт. решётки с 4 просечкми (сумма просечек 8 < 12)
                                            // -- 3 шт. решётки с 4 просечкми (сумма просечек 12 = 12)

                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtyFirstGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }
                                        // Если получившееся число меньше, то добивочная решётка та, у которой количество в комплекте больше
                                        else
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте больше, а значит количество просечек меньше

                                            // Находим количество решёток, которое помещается на вторую заготовку

                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }

                                        break;
                                    }
                                }
                            }

                            // Если не удалось разместить разные решётки на одной заготовке, то размещаем каждую решётку на своей заготовке
                            if (zLengthFirst == 0)
                            {
                                // Для первой заготовки
                                {
                                    // Кличество ручьёв на заготовке
                                    int qtyStreamByBillet = 0;
                                    var iterator = 1;

                                    while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                    {
                                        qtyStreamByBillet = iterator;
                                        iterator += 1;
                                    }

                                    zLengthFirst = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();

                                    zQtyFirstGridByFirstBillet = qtyGridsByWidth * qtyStreamByBillet;
                                }

                                // Для второй заготовки
                                {
                                    // Количество ручьёв на заготовке
                                    int qtyStreamByBillet = 0;
                                    var iterator = 1;

                                    while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                    {
                                        qtyStreamByBillet = iterator;
                                        iterator += 1;
                                    }

                                    zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                    zWidthSecond = zWidthFirst;

                                    zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                }
                            }
                        }
                        // Вариант (2 к 4) или (4 к 2) 
                        else if ((TkGridQuantityNotchesFirst.Text.ToInt() == 4 && TkGridQuantityNotchesSecond.Text.ToInt() == 2) || (TkGridQuantityNotchesFirst.Text.ToInt() == 2 && TkGridQuantityNotchesSecond.Text.ToInt() == 4))
                        {
                            // В примере создавалось 0,5 комплекта
                            // -- 1 шт. реш. с 4 просечками и 2 шт. реш. с 2 просечками (сумма просечек 8 < 12) (ручьёв 1+2=3<6)

                            // Если первая решётка имеет 4 просечки
                            if (TkGridQuantityNotchesFirst.Text.ToInt() == 4)
                            {
                                // Проверяем вариант -- 1 шт. реш. с 4 просечками и 2 шт. реш. с 2 просечками
                                if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * 2,
                                    TkGridQuantityNotchesFirst.Text.ToInt() * 1 + TkGridQuantityNotchesSecond.Text.ToInt() * 2,
                                    3))
                                {
                                    zLengthFirst = TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * 2;
                                    zQtyFirstGridByFirstBillet = qtyGridsByWidth * 1;
                                    zQtySecondGridByFirstBillet = qtyGridsByWidth * 2;
                                }
                                // Пытаемся разместить обе решётку на одну заготовку и используем добивочную
                                else if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * 1,
                                    TkGridQuantityNotchesFirst.Text.ToInt() * 1 + TkGridQuantityNotchesSecond.Text.ToInt() * 1,
                                    2))
                                {
                                    zLengthFirst = TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * 1;
                                    zQtyFirstGridByFirstBillet = qtyGridsByWidth * 1;
                                    zQtySecondGridByFirstBillet = qtyGridsByWidth * 1;

                                    // Расчёт второй заготовки
                                    // Находим решётку, у которой количество в комплекте больше, находим её количество в первой заготовке
                                    int q = zQtySecondGridByFirstBillet;
                                    // Делим найденное количество на количество этой решётки в комплекте
                                    double doubleQ = (double)((double)q / (double)TkGridQuantitySecond.Text.ToDouble());
                                    // Берём целую часть от полученного числа
                                    q = doubleQ.ToInt();
                                    // Умножаем получившееся число на количество второй решётки в комплекте (той, у которой количество в комплекте меньше)
                                    q = q * TkGridQuantityFirst.Text.ToInt();
                                    // Сравниваем получившееся число с количеством решёток из первой заготовки для второй решётки (той, у которой количество в комплекте меньше)
                                    // Если получившееся число больше, то добивочная решётка та, у которой количество в комплекте меньше
                                    if (q > zQtyFirstGridByFirstBillet)
                                    {
                                        // Добивочная решётка та, у которой количество в комплекте меньше, а значит количество просечек больше

                                        // Кличество ручьёв на заготовке
                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;

                                        while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthSecond = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                        zWidthSecond = zWidthFirst;

                                        zQtyFirstGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;

                                    }
                                    // Если получившееся число меньше, то добивочная решётка та, у которой количество в комплекте больше
                                    else
                                    {
                                        // Добивочная решётка та, у которой количество в комплекте больше, а значит количество просечек меньше

                                        // Находим количество решёток, которое помещается на вторую заготовку

                                        // Кличество ручьёв на заготовке

                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;

                                        while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                        zWidthSecond = zWidthFirst;

                                        zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                    }
                                }
                            }
                            // Еслм вторая решётка имеет 4 просечки
                            else if (TkGridQuantityNotchesSecond.Text.ToInt() == 4)
                            {
                                // Проверяем вариант -- 1 шт. реш. с 4 просечками и 2 шт. реш. с 2 просечками
                                if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * 2 + TkGridLengthSecond.Text.ToInt() * 1,
                                    TkGridQuantityNotchesFirst.Text.ToInt() * 2 + TkGridQuantityNotchesSecond.Text.ToInt() * 1,
                                    3))
                                {
                                    zLengthFirst = TkGridLengthFirst.Text.ToInt() * 2 + TkGridLengthSecond.Text.ToInt() * 1;
                                    zQtyFirstGridByFirstBillet = qtyGridsByWidth * 2;
                                    zQtySecondGridByFirstBillet = qtyGridsByWidth * 1;
                                }
                                // Пытаемся разместить обе решётку на одну заготовку и используем добивочную
                                else if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * 1,
                                     TkGridQuantityNotchesFirst.Text.ToInt() * 1 + TkGridQuantityNotchesSecond.Text.ToInt() * 1,
                                     2))
                                {
                                    zLengthFirst = TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * 1;
                                    zQtyFirstGridByFirstBillet = qtyGridsByWidth * 1;
                                    zQtySecondGridByFirstBillet = qtyGridsByWidth * 1;

                                    // Расчёт второй заготовки
                                    // Находим решётку, у которой количество в комплекте больше, находим её количество в первой заготовке
                                    int q = zQtyFirstGridByFirstBillet;
                                    // Делим найденное количество на количество этой решётки в комплекте
                                    double doubleQ = (double)((double)q / (double)TkGridQuantityFirst.Text.ToDouble());
                                    // Берём целую часть от полученного числа
                                    q = doubleQ.ToInt();
                                    // Умножаем получившееся число на количество второй решётки в комплекте (той, у которой количество в комплекте меньше)
                                    q = q * TkGridQuantitySecond.Text.ToInt();
                                    // Сравниваем получившееся число с количеством решёток из первой заготовки для второй решётки (той, у которой количество в комплекте меньше)
                                    // Если получившееся число больше, то добивочная решётка та, у которой количество в комплекте меньше
                                    if (q > zQtySecondGridByFirstBillet)
                                    {
                                        // Добивочная решётка та, у которой количество в комплекте меньше, а значит количество просечек больше

                                        // Кличество ручьёв на заготовке
                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;

                                        while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                        zWidthSecond = zWidthFirst;

                                        zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;

                                    }
                                    // Если получившееся число меньше, то добивочная решётка та, у которой количество в комплекте больше
                                    else
                                    {
                                        // Добивочная решётка та, у которой количество в комплекте больше, а значит количество просечек меньше

                                        // Находим количество решёток, которое помещается на вторую заготовку

                                        // Кличество ручьёв на заготовке
                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;

                                        while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthSecond = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                        zWidthSecond = zWidthFirst;

                                        zQtyFirstGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                    }
                                }
                            }

                            // Если не удалость разместить разные решётки на одной заготовке, то размещаем каждую решётку на свою заготовку
                            if (zLengthFirst == 0)
                            {
                                // Для первой заготовки
                                {
                                    // Кличество ручьёв на заготовке
                                    int qtyStreamByBillet = 0;
                                    var iterator = 1;

                                    while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                    {
                                        qtyStreamByBillet = iterator;
                                        iterator += 1;
                                    }

                                    zLengthFirst = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();

                                    zQtyFirstGridByFirstBillet = qtyGridsByWidth * qtyStreamByBillet;
                                }

                                // Для второй заготовки
                                {
                                    // Количество ручьёв на заготовке
                                    int qtyStreamByBillet = 0;
                                    var iterator = 1;

                                    while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                    {
                                        qtyStreamByBillet = iterator;
                                        iterator += 1;
                                    }

                                    zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                    zWidthSecond = zWidthFirst;

                                    zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                }
                            }
                        }
                        // Вариант (4 к 3) или (3 к 4) 
                        else if ((TkGridQuantityNotchesFirst.Text.ToInt() == 3 && TkGridQuantityNotchesSecond.Text.ToInt() == 4) || (TkGridQuantityNotchesFirst.Text.ToInt() == 4 && TkGridQuantityNotchesSecond.Text.ToInt() == 3))
                        {
                            // Для вариантов (2 к 3)(3 к 2) и (3 к 4)(4 к 3) ипользуется следующее распределение:
                            // -- Для первой заготовки 2 шт. 1 реш. и 1 шт. 2 реш.
                            // -- Для второй заготовки 1шт. 1 реш. и 2 шт. 2 реш.

                            var l1 = TkGridLengthFirst.Text.ToInt();
                            var l2 = TkGridLengthSecond.Text.ToInt();

                            // Проверяем, что это распределение не противоречит условиям.
                            // По количеству ручьёв у нас 2+1=3<6 и 1+2=3<6
                            // По количеству просечек у нас 2*4+1*3=11<12 и 2*3+1*4=10<12
                            if (CheckBlankLimit(l1 * 2 + l2 * 1, TkGridQuantityNotchesFirst.Text.ToInt() * 2 + TkGridQuantityNotchesSecond.Text.ToInt() * 1, 3)
                                && CheckBlankLimit(l1 * 1 + l2 * 2, TkGridQuantityNotchesFirst.Text.ToInt() * 1 + TkGridQuantityNotchesSecond.Text.ToInt() * 2, 3))
                            {
                                zLengthFirst = l1 * 2 + l2 * 1;

                                zQtyFirstGridByFirstBillet = qtyGridsByWidth * 2;
                                zQtySecondGridByFirstBillet = qtyGridsByWidth * 1;

                                zLengthSecond = l1 * 1 + l2 * 2;
                                zWidthSecond = zWidthFirst;

                                zQtyFirstGridBySecondBillet = qtyGridsByWidth * 1;
                                zQtySecondGridBySecondBillet = qtyGridsByWidth * 2;
                            }
                            // Распределение противоречит условиям
                            else
                            {
                                // Размещаем по одной решётке на первой заготовке и используем добивочную заготовку
                                if (CheckBlankLimit(l1 * 1 + l2 * 1, TkGridQuantityNotchesFirst.Text.ToInt() * 1 + TkGridQuantityNotchesSecond.Text.ToInt() * 1, 2))
                                {
                                    zLengthFirst = l1 + l2;

                                    zQtyFirstGridByFirstBillet = qtyGridsByWidth * 1;
                                    zQtySecondGridByFirstBillet = qtyGridsByWidth * 1;

                                    // Ищем добивочную заготовку
                                    // Если  количество первой решётки в комплекте больше, чем второй
                                    if (TkGridQuantityFirst.Text.ToInt() == 4)
                                    {
                                        // Находим количество этой решётки в первой заготовке
                                        int q = zQtyFirstGridByFirstBillet;
                                        // Делим найденное количество на количество этой решётки в комплекте
                                        double doubleQ = (double)((double)q / (double)TkGridQuantityFirst.Text.ToDouble());
                                        // Берём целую часть от полученного числа
                                        q = doubleQ.ToInt();
                                        // Умножаем получившееся число на количество второй решётки в комплекте (той, у которой количество в комплекте меньше)
                                        q = q * TkGridQuantitySecond.Text.ToInt();

                                        // Сравниваем получившееся число с количеством решёток из первой заготовки для второй решётки (той, у которой количество в комплекте меньше)
                                        // Если получившееся число больше, то добивочная решётка та, у которой количество в комплекте меньше
                                        if (q > zQtySecondGridByFirstBillet)
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте меньше, а значит количество просечек больше
                                            // В данном случае мы можем расположить решётки на вторую заготовку двумя способами
                                            // -- 1 шт. решётки с 4 просечкми (сумма просечек 4 < 12)
                                            // -- 2 шт. решётки с 4 просечкми (сумма просечек 8 < 12)
                                            // -- 3 шт. решётки с 4 просечкми (сумма просечек 12 = 12)

                                            // Количество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }
                                        // Если получившееся число меньше, то добивочная решётка та, у которой количество в комплекте больше
                                        else
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте больше, а значит количество просечек меньше

                                            // Находим количество решёток, которое помещается на вторую заготовку

                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtyFirstGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }
                                    }
                                    // Если количество второй решётки в комплекте больше, чем первой
                                    else if (TkGridQuantitySecond.Text.ToInt() == 4)
                                    {
                                        // Расчёт второй заготовки
                                        // Находим решётку, у которой количество в комплекте больше, находим её количество в первой заготовке
                                        int q = zQtySecondGridByFirstBillet;
                                        // Делим найденное количество на количество этой решётки в комплекте
                                        double doubleQ = (double)((double)q / (double)TkGridQuantitySecond.Text.ToDouble());
                                        // Берём целую часть от полученного числа
                                        q = doubleQ.ToInt();
                                        // Умножаем получившееся число на количество второй решётки в комплекте (той, у которой количество в комплекте меньше)
                                        q = q * TkGridQuantityFirst.Text.ToInt();
                                        // Сравниваем получившееся число с количеством решёток из первой заготовки для второй решётки (той, у которой количество в комплекте меньше)
                                        // Если получившееся число больше, то добивочная решётка та, у которой количество в комплекте меньше
                                        if (q > zQtyFirstGridByFirstBillet)
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте меньше, а значит количество просечек больше
                                            // В данном случае мы можем расположить решётки на вторую заготовку двумя способами
                                            // -- 1 шт. решётки с 4 просечкми (сумма просечек 4 < 12)
                                            // -- 2 шт. решётки с 4 просечкми (сумма просечек 8 < 12)
                                            // -- 3 шт. решётки с 4 просечкми (сумма просечек 12 = 12)

                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtyFirstGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }
                                        // Если получившееся число меньше, то добивочная решётка та, у которой количество в комплекте больше
                                        else
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте больше, а значит количество просечек меньше

                                            // Находим количество решёток, которое помещается на вторую заготовку

                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }
                                    }
                                }
                                // Размещаем каждую решётку на своей заготовке
                                else
                                {
                                    // Для первой заготовки
                                    {
                                        // Кличество ручьёв на заготовке
                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;

                                        while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthFirst = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();

                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * qtyStreamByBillet;
                                    }

                                    // Для второй заготовки
                                    {
                                        // Количество ручьёв на заготовке
                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;

                                        while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                        zWidthSecond = zWidthFirst;

                                        zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                    }
                                }
                            }
                        }
                        // Вариант (4 к 4)
                        else if (TkGridQuantityNotchesFirst.Text.ToInt() == 4 && TkGridQuantityNotchesSecond.Text.ToInt() == 4)
                        {
                            // В этом случае, если решётки одинаковые, то используем одну заготовку
                            // Количество просечек у всего комплекта превышеат 12, поэтому используем 3 ручья (3*4=12=12)

                            //  Проверяем, что решётки одинаковые
                            if (TkGridLengthFirst.Text.ToInt() == TkGridLengthSecond.Text.ToInt())
                            {
                                // 3 ручья * 4 просечки=12=12
                                // 2 ручья * 4 просечки=8<12
                                // 1 ручей * 4 просечки=4<12
                                for (int i = 3; i > 0; i--)
                                {
                                    if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * i, TkGridQuantityNotchesFirst.Text.ToInt() * i, i))
                                    {
                                        zLengthFirst = TkGridLengthFirst.Text.ToInt() * i;

                                        var qtyGrids = i * qtyGridsByWidth;

                                        if (qtyGrids % 2 == 0)
                                        {
                                            zQtyFirstGridByFirstBillet = qtyGrids / 2;
                                            zQtySecondGridByFirstBillet = qtyGrids / 2;
                                        }
                                        else
                                        {
                                            qtyGrids = qtyGrids - 1;

                                            zQtyFirstGridByFirstBillet = (qtyGrids / 2) + 1;
                                            zQtySecondGridByFirstBillet = qtyGrids / 2;
                                        }

                                        break;
                                    }
                                }
                            }
                            // Решётки не одинаковые
                            else
                            {
                                // не используется
                                {
                                    // Варианты размещения решёток на заоготовке
                                    // 2 шт. 1 реш.(4 прос.) и 1 шт. 2 реш.(4 прос.) (количество просечек 2*4+1*4=12=12)
                                    // 1 шт. 1 реш.(4 прос.) и 2 шт. 2 реш.(4 прос.) (количество просечек 1*4+2*4=12=12)
                                    // 1 шт. 1 реш.(4 прос.) и 1 шт. 2 реш.(4 прос.) (количество просечек 1*4+1*4=8<12)
                                    // 3 шт. 1 реш.(4 прос.) (количество просечек 3*4=12=12)
                                    // 2 шт. 1 реш.(4 прос.) (количество просечек 2*4=8<12)
                                    // 1 шт. 1 реш.(4 прос.) (количество просечек 1*4=4<12)


                                    // Проверяем самые оптимальные варианты
                                    // 2 шт. 1 реш.(4 прос.) и 1 шт. 2 реш.(4 прос.) (количество просечек 2*4+1*4=12=12)
                                    // 1 шт. 1 реш.(4 прос.) и 2 шт. 2 реш.(4 прос.) (количество просечек 1*4+2*4=12=12)
                                }


                                // Используется 2 заготовки

                                // Основной вариант:
                                // 1-я заготовка: 2 шт. 1 реш.(4 прос. (по возможности той, которая длинее)) и 1 шт. 2.реш.(4 прос.) (количество просечек 2*4+1*4=12=12)
                                // 2-я заготовка: добивочная с оставшейся решёткой

                                // первая решётка длинее второй
                                if (TkGridLengthFirst.Text.ToInt() > TkGridLengthSecond.Text.ToInt())
                                {
                                    // 1-я заготовка: 2 шт. 1 реш.(4 прос. (той, которая длинее)) и 1 шт. 2.реш.(4 прос.) (количество просечек 2*4+1*4=12=12)
                                    if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * 2 + TkGridLengthSecond.Text.ToInt() * 1,
                                        TkGridQuantityNotchesFirst.Text.ToInt() * 2 + TkGridQuantityNotchesSecond.Text.ToInt() * 1,
                                        3))
                                    {
                                        zLengthFirst = TkGridLengthFirst.Text.ToInt() * 2 + TkGridLengthSecond.Text.ToInt() * 1;

                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * 2;
                                        zQtySecondGridByFirstBillet = qtyGridsByWidth * 1;


                                        // Добивочная решётка та, которой в первой заготовке меньше
                                        // Ищем максимальное количество решёток, которое можно разместить на второй заготовке.

                                        // Кличество ручьёв на заготовке
                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;

                                        while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                        zWidthSecond = zWidthFirst;

                                        zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                    }
                                    // 1-я заготовка: 2 шт. 1 реш.(4 прос. (той, которая короче)) и 1 шт. 2.реш.(4 прос.) (количество просечек 2*4+1*4=12=12)
                                    else if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * 2,
                                        TkGridQuantityNotchesFirst.Text.ToInt() * 1 + TkGridQuantityNotchesSecond.Text.ToInt() * 2,
                                        3))
                                    {
                                        zLengthFirst = TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * 2;

                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * 1;
                                        zQtySecondGridByFirstBillet = qtyGridsByWidth * 2;


                                        // Добивочная решётка та, которой в первой заготовке меньше
                                        // Ищем максимальное количество решёток, которое можно разместить на второй заготовке.

                                        // Кличество ручьёв на заготовке
                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;

                                        while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthSecond = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                        zWidthSecond = zWidthFirst;

                                        zQtyFirstGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                    }
                                    //если не проходим по длине, то берём 2 ручья: 1 шт. 1 реш.(4 прос.) и 1 шт. 2 реш.(4 прос.) (количество просечек 1*4+1*4=8<12)
                                    else if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * 1,
                                        TkGridQuantityNotchesFirst.Text.ToInt() * 1 + TkGridQuantityNotchesSecond.Text.ToInt() * 1,
                                        2))
                                    {
                                        zLengthFirst = TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * 1;

                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * 1;
                                        zQtySecondGridByFirstBillet = qtyGridsByWidth * 1;
                                    }
                                }
                                // вторая решётка длинее первой
                                else
                                {
                                    // 1-я заготовка: 2 шт. 1 реш.(4 прос. (той, которая длинее)) и 1 шт. 2.реш.(4 прос.) (количество просечек 2*4+1*4=12=12)
                                    if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * 2,
                                        TkGridQuantityNotchesFirst.Text.ToInt() * 1 + TkGridQuantityNotchesSecond.Text.ToInt() * 2,
                                        3))
                                    {
                                        zLengthFirst = TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * 2;

                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * 1;
                                        zQtySecondGridByFirstBillet = qtyGridsByWidth * 2;


                                        // Добивочная решётка та, которой в первой заготовке меньше
                                        // Ищем максимальное количество решёток, которое можно разместить на второй заготовке.

                                        // Кличество ручьёв на заготовке
                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;

                                        while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthSecond = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                        zWidthSecond = zWidthFirst;

                                        zQtyFirstGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                    }
                                    // 1-я заготовка: 2 шт. 1 реш.(4 прос. (той, которая короче)) и 1 шт. 2.реш.(4 прос.) (количество просечек 2*4+1*4=12=12)
                                    else if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * 2 + TkGridLengthSecond.Text.ToInt() * 1,
                                        TkGridQuantityNotchesFirst.Text.ToInt() * 2 + TkGridQuantityNotchesSecond.Text.ToInt() * 1,
                                        3))
                                    {
                                        zLengthFirst = TkGridLengthFirst.Text.ToInt() * 2 + TkGridLengthSecond.Text.ToInt() * 1;

                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * 2;
                                        zQtySecondGridByFirstBillet = qtyGridsByWidth * 1;


                                        // Добивочная решётка та, которой в первой заготовке меньше
                                        // Ищем максимальное количество решёток, которое можно разместить на второй заготовке.

                                        // Кличество ручьёв на заготовке
                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;

                                        while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                        zWidthSecond = zWidthFirst;

                                        zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                    }
                                    //если не проходим по длине, то берём 2 ручья: 1 шт. 1 реш.(4 прос.) и 1 шт. 2 реш.(4 прос.) (количество просечек 1*4+1*4=8<12)
                                    else if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * 1,
                                        TkGridQuantityNotchesFirst.Text.ToInt() * 1 + TkGridQuantityNotchesSecond.Text.ToInt() * 1,
                                        2))
                                    {
                                        zLengthFirst = TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * 1;

                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * 1;
                                        zQtySecondGridByFirstBillet = qtyGridsByWidth * 1;
                                    }
                                }

                                // Если не удалость разместить разные решётки на одной заготовке, то размещаем каждую решётку на свою заготовку
                                if (zLengthFirst == 0)
                                {
                                    // Для первой заготовки
                                    {
                                        // Кличество ручьёв на заготовке
                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;

                                        while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthFirst = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();

                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * qtyStreamByBillet;
                                    }

                                    // Для второй заготовки
                                    {
                                        // Количество ручьёв на заготовке
                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;

                                        while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                        zWidthSecond = zWidthFirst;

                                        zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                    }
                                }
                            }
                        }
                        // Вариант (5 к 1) или (1 к 5)
                        else if ((TkGridQuantityNotchesFirst.Text.ToInt() == 1 && TkGridQuantityNotchesSecond.Text.ToInt() == 5) || (TkGridQuantityNotchesFirst.Text.ToInt() == 5 && TkGridQuantityNotchesSecond.Text.ToInt() == 1))
                        {
                            // -- 1 шт. реш с 5 прос и 5 шт. реш с 1 прос (просечки 10<12) (ручьёв 6=6)
                            // -- 1 шт. реш с 5 прос и 4 шт. реш с 1 прос (просечки 9<12) (ручьёв 5=6)
                            // -- 1 шт. реш с 5 прос и 3 шт. реш с 1 прос (просечки 8<12) (ручьёв 4=6)
                            // -- 1 шт. реш с 5 прос и 2 шт. реш с 1 прос (просечки 7<12) (ручьёв 3=6)
                            // -- 1 шт. реш с 5 прос и 1 шт. реш с 1 прос (просечки 6<12) (ручьёв 2=6)

                            // Если первая решётка имеет 1 просечку
                            if (TkGridQuantityNotchesFirst.Text.ToInt() == 1)
                            {
                                for (int i = 5; i > 0; i--)
                                {
                                    // Проверяем, что не нарушено условие по максимальной длинне заготовки
                                    if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * i + TkGridLengthSecond.Text.ToInt() * 1,
                                        TkGridQuantityNotchesFirst.Text.ToInt() * i + TkGridQuantityNotchesSecond.Text.ToInt() * 1,
                                        i + 1))
                                    {
                                        zLengthFirst = TkGridLengthFirst.Text.ToInt() * i + TkGridLengthSecond.Text.ToInt() * 1;
                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * i;
                                        zQtySecondGridByFirstBillet = qtyGridsByWidth * 1;

                                        // Если полный комплект  (-- 1 шт. реш с 5 прос и 5 шт. реш с 1 прос (просечки 10<12) (ручьёв 6=6))
                                        // то не используем добивочную решётку
                                        if (i == 5)
                                        {
                                            break;
                                        }

                                        // Расчёт второй заготовки
                                        // Находим решётку, у которой количество в комплекте больше, находим её количество в первой заготовке
                                        int q = zQtyFirstGridByFirstBillet;
                                        // Делим найденное количество на количество этой решётки в комплекте
                                        double doubleQ = (double)((double)q / (double)TkGridQuantityFirst.Text.ToDouble());
                                        // Берём целую часть от полученного числа
                                        q = doubleQ.ToInt();
                                        // Умножаем получившееся число на количество второй решётки в комплекте (той, у которой количество в комплекте меньше)
                                        q = q * TkGridQuantitySecond.Text.ToInt();
                                        // Сравниваем получившееся число с количеством решёток из первой заготовки для второй решётки (той, у которой количество в комплекте меньше)
                                        // Если получившееся число больше, то добивочная решётка та, у которой количество в комплекте меньше
                                        if (q > zQtySecondGridByFirstBillet)
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте меньше, а значит количество просечек больше
                                            // В данном случае мы можем расположить решётки на вторую заготовку двумя способами
                                            // -- 1 шт. решётки с 5 просечкми (сумма просечек 5 < 12)
                                            // -- 2 шт. решётки с 5 просечкми (сумма просечек 10 < 12)

                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }
                                        // Если получившееся число меньше, то добивочная решётка та, у которой количество в комплекте больше
                                        else
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте больше, а значит количество просечек меньше

                                            // Находим количество решёток, которое помещается на вторую заготовку

                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtyFirstGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }

                                        break;
                                    }
                                }
                            }
                            // Если вторая решётка имеет 1 просечку
                            else
                            {
                                for (int i = 5; i > 0; i--)
                                {
                                    // Проверяем, что не нарушено условие по максимальной длинне заготовки
                                    if (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * i + TkGridLengthFirst.Text.ToInt() * 1,
                                        TkGridQuantityNotchesSecond.Text.ToInt() * i + TkGridQuantityNotchesFirst.Text.ToInt() * 1,
                                        i + 1))
                                    {
                                        zLengthFirst = TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * i;
                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * 1;
                                        zQtySecondGridByFirstBillet = qtyGridsByWidth * i;

                                        // Если полный комплект  (-- 1 шт. реш с 5 прос и 5 шт. реш с 1 прос (просечки 10<12) (ручьёв 6=6))
                                        // то не используем добивочную решётку
                                        if (i == 5)
                                        {
                                            break;
                                        }

                                        // Расчёт второй заготовки
                                        // Находим решётку, у которой количество в комплекте больше, находим её количество в первой заготовке
                                        int q = zQtySecondGridByFirstBillet;
                                        // Делим найденное количество на количество этой решётки в комплекте
                                        double doubleQ = (double)((double)q / (double)TkGridQuantitySecond.Text.ToDouble());
                                        // Берём целую часть от полученного числа
                                        q = doubleQ.ToInt();
                                        // Умножаем получившееся число на количество второй решётки в комплекте (той, у которой количество в комплекте меньше)
                                        q = q * TkGridQuantityFirst.Text.ToInt();
                                        // Сравниваем получившееся число с количеством решёток из первой заготовки для второй решётки (той, у которой количество в комплекте меньше)
                                        // Если получившееся число больше, то добивочная решётка та, у которой количество в комплекте меньше
                                        if (q > zQtyFirstGridByFirstBillet)
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте меньше, а значит количество просечек больше
                                            // В данном случае мы можем расположить решётки на вторую заготовку двумя способами
                                            // -- 1 шт. решётки с 5 просечкми (сумма просечек 5 < 12)
                                            // -- 2 шт. решётки с 5 просечкми (сумма просечек 10 < 12)

                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtyFirstGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }
                                        // Если получившееся число меньше, то добивочная решётка та, у которой количество в комплекте больше
                                        else
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте больше, а значит количество просечек меньше

                                            // Находим количество решёток, которое помещается на вторую заготовку

                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }

                                        break;
                                    }
                                }
                            }

                            // Если не удалость разместить разные решётки на одной заготовке, то размещаем каждую решётку на свою заготовку
                            if (zLengthFirst == 0)
                            {
                                // Для первой заготовки
                                {
                                    // Кличество ручьёв на заготовке
                                    int qtyStreamByBillet = 0;
                                    var iterator = 1;

                                    while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                    {
                                        qtyStreamByBillet = iterator;
                                        iterator += 1;
                                    }

                                    zLengthFirst = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();

                                    zQtyFirstGridByFirstBillet = qtyGridsByWidth * qtyStreamByBillet;
                                }

                                // Для второй заготовки
                                {
                                    // Количество ручьёв на заготовке
                                    int qtyStreamByBillet = 0;
                                    var iterator = 1;

                                    while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                    {
                                        qtyStreamByBillet = iterator;
                                        iterator += 1;
                                    }

                                    zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                    zWidthSecond = zWidthFirst;

                                    zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                }
                            }
                        }
                        // Вариант (5 к 2) или (2 к 5)
                        else if ((TkGridQuantityNotchesFirst.Text.ToInt() == 2 && TkGridQuantityNotchesSecond.Text.ToInt() == 5) || (TkGridQuantityNotchesFirst.Text.ToInt() == 5 && TkGridQuantityNotchesSecond.Text.ToInt() == 2))
                        {
                            // При таком наборе просечек мы можем расположить реш1тки на заготовку одним из следующих способов (чтобы сохранялось условие сумма просечек на заготовке <= 12)
                            // -- 1 шт. решётки с 5 просечкми и 1 шт. ршётки с 2 просечками (сумма просечек 7 < 12) (ручьёв 2<6)
                            // -- 1 шт. решётки с 5 просечкми и 2 шт. ршётки с 2 просечками (сумма просечек 9 < 12) (ручьёв 3<6)
                            // -- 1 шт. решётки с 5 просечкми и 3 шт. ршётки с 2 просечками (сумма просечек 11 < 12) (ручьёв 4<6)

                            // Если первая решётка имеет 2 просечки
                            if (TkGridQuantityNotchesFirst.Text.ToInt() == 2)
                            {
                                for (int i = 3; i > 0; i--)
                                {
                                    if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * i + TkGridLengthSecond.Text.ToInt() * 1,
                                        TkGridQuantityNotchesFirst.Text.ToInt() * i + TkGridQuantityNotchesSecond.Text.ToInt() * 1,
                                        i + 1))
                                    {
                                        zLengthFirst = TkGridLengthFirst.Text.ToInt() * i + TkGridLengthSecond.Text.ToInt() * 1;
                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * i;
                                        zQtySecondGridByFirstBillet = qtyGridsByWidth * 1;

                                        // Расчёт второй заготовки
                                        // Находим решётку, у которой количество в комплекте больше, находим её количество в первой заготовке
                                        int q = zQtyFirstGridByFirstBillet;
                                        // Делим найденное количество на количество этой решётки в комплекте
                                        double doubleQ = (double)((double)q / (double)TkGridQuantityFirst.Text.ToDouble());
                                        // Берём целую часть от полученного числа
                                        q = doubleQ.ToInt();
                                        // Умножаем получившееся число на количество второй решётки в комплекте (той, у которой количество в комплекте меньше)
                                        q = q * TkGridQuantitySecond.Text.ToInt();
                                        // Сравниваем получившееся число с количеством решёток из первой заготовки для второй решётки (той, у которой количество в комплекте меньше)
                                        // Если получившееся число больше, то добивочная решётка та, у которой количество в комплекте меньше
                                        if (q > zQtySecondGridByFirstBillet)
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте меньше, а значит количество просечек больше
                                            // В данном случае мы можем расположить решётки на вторую заготовку двумя способами
                                            // -- 1 шт. решётки с 5 просечкми (сумма просечек 5 < 12)
                                            // -- 2 шт. решётки с 5 просечкми (сумма просечек 10 < 12)

                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }
                                        // Если получившееся число меньше, то добивочная решётка та, у которой количество в комплекте больше
                                        else
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте больше, а значит количество просечек меньше

                                            // Находим количество решёток, которое помещается на вторую заготовку

                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtyFirstGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }

                                        break;
                                    }
                                }
                            }
                            // Если вторая решётка имеет 2 просечки
                            else
                            {
                                for (int i = 3; i > 0; i--)
                                {
                                    if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * i,
                                        TkGridQuantityNotchesFirst.Text.ToInt() * 1 + TkGridQuantityNotchesSecond.Text.ToInt() * i,
                                        i + 1))
                                    {
                                        zLengthFirst = TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * i;
                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * 1;
                                        zQtySecondGridByFirstBillet = qtyGridsByWidth * i;

                                        // Расчёт второй заготовки
                                        // Находим решётку, у которой количество в комплекте больше, находим её количество в первой заготовке
                                        int q = zQtySecondGridByFirstBillet;
                                        // Делим найденное количество на количество этой решётки в комплекте
                                        double doubleQ = (double)((double)q / (double)TkGridQuantitySecond.Text.ToDouble());
                                        // Берём целую часть от полученного числа
                                        q = doubleQ.ToInt();
                                        // Умножаем получившееся число на количество второй решётки в комплекте (той, у которой количество в комплекте меньше)
                                        q = q * TkGridQuantityFirst.Text.ToInt();
                                        // Сравниваем получившееся число с количеством решёток из первой заготовки для второй решётки (той, у которой количество в комплекте меньше)
                                        // Если получившееся число больше, то добивочная решётка та, у которой количество в комплекте меньше
                                        if (q > zQtyFirstGridByFirstBillet)
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте меньше, а значит количество просечек больше
                                            // В данном случае мы можем расположить решётки на вторую заготовку двумя способами
                                            // -- 1 шт. решётки с 5 просечкми (сумма просечек 5 < 12)
                                            // -- 2 шт. решётки с 5 просечкми (сумма просечек 10 < 12)

                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtyFirstGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }
                                        // Если получившееся число меньше, то добивочная решётка та, у которой количество в комплекте больше
                                        else
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте больше, а значит количество просечек меньше

                                            // Находим количество решёток, которое помещается на вторую заготовку

                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }

                                        break;
                                    }
                                }
                            }

                            // Если не удалость разместить разные решётки на одной заготовке, то размещаем каждую решётку на свою заготовку
                            if (zLengthFirst == 0)
                            {
                                // Для первой заготовки
                                {
                                    // Кличество ручьёв на заготовке
                                    int qtyStreamByBillet = 0;
                                    var iterator = 1;

                                    while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                    {
                                        qtyStreamByBillet = iterator;
                                        iterator += 1;
                                    }

                                    zLengthFirst = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();

                                    zQtyFirstGridByFirstBillet = qtyGridsByWidth * qtyStreamByBillet;
                                }

                                // Для второй заготовки
                                {
                                    // Количество ручьёв на заготовке
                                    int qtyStreamByBillet = 0;
                                    var iterator = 1;

                                    while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                    {
                                        qtyStreamByBillet = iterator;
                                        iterator += 1;
                                    }

                                    zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                    zWidthSecond = zWidthFirst;

                                    zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                }
                            }
                        }
                        // Вариант (5 к 3) или (3 к 5) 
                        else if ((TkGridQuantityNotchesFirst.Text.ToInt() == 3 && TkGridQuantityNotchesSecond.Text.ToInt() == 5) || (TkGridQuantityNotchesFirst.Text.ToInt() == 5 && TkGridQuantityNotchesSecond.Text.ToInt() == 3))
                        {
                            // При таком наборе просечек мы можем расположить решётки на заготовку одним из следующих способов (чтобы сохранялось условие сумма просечек на заготовке <= 12)
                            // -- 1 шт. решётки с 5 просечкми и 1 шт. ршётки с 3 просечками (сумма просечек 8 < 12) (ручьёв 2<6)
                            // -- 1 шт. решётки с 5 просечкми и 2 шт. ршётки с 3 просечками (сумма просечек 11 < 12) (ручьёв 3<6)

                            // Если первая решётка имеет 3 просечки
                            if (TkGridQuantityNotchesFirst.Text.ToInt() == 3)
                            {
                                for (int i = 2; i > 0; i--)
                                {
                                    if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * i + TkGridLengthSecond.Text.ToInt() * 1,
                                       TkGridQuantityNotchesFirst.Text.ToInt() * i + TkGridQuantityNotchesSecond.Text.ToInt() * 1,
                                       i + 1))
                                    {
                                        zLengthFirst = TkGridLengthFirst.Text.ToInt() * i + TkGridLengthSecond.Text.ToInt() * 1;
                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * i;
                                        zQtySecondGridByFirstBillet = qtyGridsByWidth * 1;

                                        // Расчёт второй заготовки
                                        // Находим решётку, у которой количество в комплекте больше, находим её количество в первой заготовке
                                        int q = zQtyFirstGridByFirstBillet;
                                        // Делим найденное количество на количество этой решётки в комплекте
                                        double doubleQ = (double)((double)q / (double)TkGridQuantityFirst.Text.ToDouble());
                                        // Берём целую часть от полученного числа
                                        q = doubleQ.ToInt();
                                        // Умножаем получившееся число на количество второй решётки в комплекте (той, у которой количество в комплекте меньше)
                                        q = q * TkGridQuantitySecond.Text.ToInt();
                                        // Сравниваем получившееся число с количеством решёток из первой заготовки для второй решётки (той, у которой количество в комплекте меньше)
                                        // Если получившееся число больше, то добивочная решётка та, у которой количество в комплекте меньше
                                        if (q > zQtySecondGridByFirstBillet)
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте меньше, а значит количество просечек больше
                                            // В данном случае мы можем расположить решётки на вторую заготовку двумя способами
                                            // -- 1 шт. решётки с 5 просечкми (сумма просечек 5 < 12)
                                            // -- 2 шт. решётки с 5 просечкми (сумма просечек 10 < 12)

                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }
                                        // Если получившееся число меньше, то добивочная решётка та, у которой количество в комплекте больше
                                        else
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте больше, а значит количество просечек меньше

                                            // Находим количество решёток, которое помещается на вторую заготовку

                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtyFirstGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }

                                        break;
                                    }
                                }
                            }
                            // Если вторая решётка имеет 3 просечки
                            else
                            {
                                for (int i = 2; i > 0; i--)
                                {
                                    if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * i,
                                        TkGridQuantityNotchesFirst.Text.ToInt() * 1 + TkGridQuantityNotchesSecond.Text.ToInt() * i,
                                        1 + i))
                                    {
                                        zLengthFirst = TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * i;
                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * 1;
                                        zQtySecondGridByFirstBillet = qtyGridsByWidth * i;

                                        // Расчёт второй заготовки
                                        // Находим решётку, у которой количество в комплекте больше, находим её количество в первой заготовке
                                        int q = zQtySecondGridByFirstBillet;
                                        // Делим найденное количество на количество этой решётки в комплекте
                                        double doubleQ = (double)((double)q / (double)TkGridQuantitySecond.Text.ToDouble());
                                        // Берём целую часть от полученного числа
                                        q = doubleQ.ToInt();
                                        // Умножаем получившееся число на количество второй решётки в комплекте (той, у которой количество в комплекте меньше)
                                        q = q * TkGridQuantityFirst.Text.ToInt();
                                        // Сравниваем получившееся число с количеством решёток из первой заготовки для второй решётки (той, у которой количество в комплекте меньше)
                                        // Если получившееся число больше, то добивочная решётка та, у которой количество в комплекте меньше
                                        if (q > zQtyFirstGridByFirstBillet)
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте меньше, а значит количество просечек больше
                                            // В данном случае мы можем расположить решётки на вторую заготовку двумя способами
                                            // -- 1 шт. решётки с 5 просечкми (сумма просечек 5 < 12)
                                            // -- 2 шт. решётки с 5 просечкми (сумма просечек 10 < 12)

                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtyFirstGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }
                                        // Если получившееся число меньше, то добивочная решётка та, у которой количество в комплекте больше
                                        else
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте больше, а значит количество просечек меньше

                                            // Находим количество решёток, которое помещается на вторую заготовку

                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }

                                        break;
                                    }
                                }
                            }

                            // Если не удалость разместить разные решётки на одной заготовке, то размещаем каждую решётку на свою заготовку
                            if (zLengthFirst == 0)
                            {
                                // Для первой заготовки
                                {
                                    // Кличество ручьёв на заготовке
                                    int qtyStreamByBillet = 0;
                                    var iterator = 1;

                                    while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                    {
                                        qtyStreamByBillet = iterator;
                                        iterator += 1;
                                    }

                                    zLengthFirst = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();

                                    zQtyFirstGridByFirstBillet = qtyGridsByWidth * qtyStreamByBillet;
                                }

                                // Для второй заготовки
                                {
                                    // Количество ручьёв на заготовке
                                    int qtyStreamByBillet = 0;
                                    var iterator = 1;

                                    while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                    {
                                        qtyStreamByBillet = iterator;
                                        iterator += 1;
                                    }

                                    zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                    zWidthSecond = zWidthFirst;

                                    zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                }
                            }
                        }
                        // Вариант (5 к 4) или (4 к 5) 
                        else if ((TkGridQuantityNotchesFirst.Text.ToInt() == 4 && TkGridQuantityNotchesSecond.Text.ToInt() == 5) || (TkGridQuantityNotchesFirst.Text.ToInt() == 5 && TkGridQuantityNotchesSecond.Text.ToInt() == 4))
                        {
                            // При таком наборе просечек мы можем расположить решётки на заготовку одним из следующих способом (чтобы сохранялось условие сумма просечек на заготовке <= 12)
                            // -- 1 шт. решётки с 5 просечкми и 1 шт. решётки с 4 просечками (сумма просечек 9 < 12) (ручьёв 2<6)
                            // -- 1 шт. решётки с 5 просечкми (сумма просечек 5 < 12) (ручьёв 1<6)
                            // -- 1 шт. решётки с 4 просечками (сумма просечек 4 < 12) (ручьёв 1<6)

                            // Проверяем, могут ли поместиться обе решётки на заготовке
                            if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() + TkGridLengthSecond.Text.ToInt(),
                                TkGridQuantityNotchesFirst.Text.ToInt() + TkGridQuantityNotchesSecond.Text.ToInt(),
                                2))
                            {
                                // Используем вариант -- 1 шт. решётки с 5 просечкми и 1 шт. ршётки с 4 просечками

                                // Если первая решётка имеет 4 просечки
                                if (TkGridQuantityNotchesFirst.Text.ToInt() == 4)
                                {
                                    zLengthFirst = TkGridLengthFirst.Text.ToInt() + TkGridLengthSecond.Text.ToInt();

                                    zQtyFirstGridByFirstBillet = qtyGridsByWidth;
                                    zQtySecondGridByFirstBillet = qtyGridsByWidth;

                                    // Расчёт второй заготовки
                                    // Находим решётку, у которой количество в комплекте больше, находим её количество в первой заготовке
                                    int q = zQtyFirstGridByFirstBillet;
                                    // Делим найденное количество на количество этой решётки в комплекте
                                    double doubleQ = (double)((double)q / (double)TkGridQuantityFirst.Text.ToDouble());
                                    // Берём целую часть от полученного числа
                                    q = doubleQ.ToInt();
                                    // Умножаем получившееся число на количество второй решётки в комплекте (той, у которой количество в комплекте меньше)
                                    q = q * TkGridQuantitySecond.Text.ToInt();
                                    // Сравниваем получившееся число с количеством решёток из первой заготовки для второй решётки (той, у которой количество в комплекте меньше)
                                    // Если получившееся число больше, то добивочная решётка та, у которой количество в комплекте меньше
                                    if (q > zQtySecondGridByFirstBillet)
                                    {
                                        // Добивочная решётка та, у которой количество в комплекте меньше, а значит количество просечек больше
                                        // В данном случае мы можем расположить решётки на вторую заготовку двумя способами
                                        // -- 1 шт. решётки с 5 просечкми (сумма просечек 5 < 12)
                                        // -- 2 шт. решётки с 5 просечкми (сумма просечек 10 < 12)

                                        // Кличество ручьёв на заготовке
                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;

                                        while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                        zWidthSecond = zWidthFirst;

                                        zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                    }
                                    // Если получившееся число меньше, то добивочная решётка та, у которой количество в комплекте больше
                                    else
                                    {
                                        // Добивочная решётка та, у которой количество в комплекте больше, а значит количество просечек меньше

                                        // Находим количество решёток, которое помещается на вторую заготовку

                                        // Кличество ручьёв на заготовке
                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;

                                        while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthSecond = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                        zWidthSecond = zWidthFirst;

                                        zQtyFirstGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                    }

                                }
                                // Если вторая решётка имеет 4 просечки
                                else
                                {
                                    zLengthFirst = TkGridLengthFirst.Text.ToInt() + TkGridLengthSecond.Text.ToInt();

                                    zQtyFirstGridByFirstBillet = qtyGridsByWidth;
                                    zQtySecondGridByFirstBillet = qtyGridsByWidth;

                                    // Расчёт второй заготовки
                                    // Находим решётку, у которой количество в комплекте больше, находим её количество в первой заготовке
                                    int q = zQtySecondGridByFirstBillet;
                                    // Делим найденное количество на количество этой решётки в комплекте
                                    double doubleQ = (double)((double)q / (double)TkGridQuantitySecond.Text.ToDouble());
                                    // Берём целую часть от полученного числа
                                    q = doubleQ.ToInt();
                                    // Умножаем получившееся число на количество второй решётки в комплекте (той, у которой количество в комплекте меньше)
                                    q = q * TkGridQuantityFirst.Text.ToInt();
                                    // Сравниваем получившееся число с количеством решёток из первой заготовки для второй решётки (той, у которой количество в комплекте меньше)
                                    // Если получившееся число больше, то добивочная решётка та, у которой количество в комплекте меньше
                                    if (q > zQtyFirstGridByFirstBillet)
                                    {
                                        // Добивочная решётка та, у которой количество в комплекте меньше, а значит количество просечек больше
                                        // В данном случае мы можем расположить решётки на вторую заготовку двумя способами
                                        // -- 1 шт. решётки с 5 просечкми (сумма просечек 5 < 12)
                                        // -- 2 шт. решётки с 5 просечкми (сумма просечек 10 < 12)

                                        // Кличество ручьёв на заготовке
                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;

                                        while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthSecond = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                        zWidthSecond = zWidthFirst;

                                        zQtyFirstGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                    }
                                    // Если получившееся число меньше, то добивочная решётка та, у которой количество в комплекте больше
                                    else
                                    {
                                        // Добивочная решётка та, у которой количество в комплекте больше, а значит количество просечек меньше

                                        // Находим количество решёток, которое помещается на вторую заготовку

                                        // Кличество ручьёв на заготовке
                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;

                                        while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                        zWidthSecond = zWidthFirst;

                                        zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                    }
                                }
                            }
                            // Обе решётки не могут поместиться на заготовке; скорее всего одна решётка пойдёт на одну заготовку, а вторая на другую.
                            else
                            {
                                // Определяем, сколько решёток одного типа может поместиться на заготовку

                                //Для первой решётки
                                {
                                    // Кличество ручьёв на заготовке
                                    int qtyStreamByBillet = 0;
                                    var iterator = 1;

                                    while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                    {
                                        qtyStreamByBillet = iterator;
                                        iterator += 1;
                                    }

                                    zLengthFirst = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();

                                    zQtyFirstGridByFirstBillet = qtyGridsByWidth * qtyStreamByBillet;
                                }

                                //Для второй
                                {
                                    // Кличество ручьёв на заготовке
                                    int qtyStreamByBillet = 0;
                                    var iterator = 1;

                                    while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                    {
                                        qtyStreamByBillet = iterator;
                                        iterator += 1;
                                    }

                                    zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                    zWidthSecond = zWidthFirst;

                                    zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                }
                            }
                        }
                        // Вариант (5 к 5)
                        else if (TkGridQuantityNotchesFirst.Text.ToInt() == 5 && TkGridQuantityNotchesSecond.Text.ToInt() == 5)
                        {
                            // 1 -- По одной решётке каждого типа на одну заготовку
                            // 2 -- Каждая решётка на своей заготовке

                            // 1 -- По одной решётке каждого типа на одну заготовку
                            if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() + TkGridLengthSecond.Text.ToInt(), 10, 2))
                            {
                                // Сумма длин решёток для половины комплекта
                                zLengthFirst = TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * 1;

                                zQtyFirstGridByFirstBillet = qtyGridsByWidth * 1;
                                zQtySecondGridByFirstBillet = qtyGridsByWidth * 1;
                            }
                            // 2 -- Каждая решётка на своей заготовке
                            else
                            {
                                // Первая заготовка
                                {
                                    // Находим количество решёток, которое помещается на первую заготовку
                                    // Количество ручьёв на заготовке
                                    int qtyStreamByBillet = 0;
                                    var iterator = 1;

                                    while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                    {
                                        qtyStreamByBillet = iterator;
                                        iterator += 1;
                                    }

                                    zLengthFirst = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                    zQtyFirstGridByFirstBillet = qtyGridsByWidth * qtyStreamByBillet;
                                }

                                // Вторая заготовка
                                {
                                    int qtyStreamByBillet = 0;
                                    var iterator = 1;

                                    while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                    {
                                        qtyStreamByBillet = iterator;
                                        iterator += 1;
                                    }

                                    zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                    zWidthSecond = zWidthFirst;
                                    zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                }
                            }
                        }
                        // Вариант (6 к 1) или (1 к 6) 
                        else if ((TkGridQuantityNotchesFirst.Text.ToInt() == 6 && TkGridQuantityNotchesSecond.Text.ToInt() == 1) || (TkGridQuantityNotchesFirst.Text.ToInt() == 1 && TkGridQuantityNotchesSecond.Text.ToInt() == 6))
                        {
                            // -- 1 шт. решётки с 6 просечкми и 5 шт. решётки с 1 просечками (сумма просечек 11 < 12) (ручьёв 6=6)
                            // -- 1 шт. решётки с 6 просечкми и 4 шт. решётки с 1 просечками (сумма просечек 10 < 12) (ручьёв 5<6)
                            // -- 1 шт. решётки с 6 просечкми и 3 шт. решётки с 1 просечками (сумма просечек 9 < 12) (ручьёв 4<6)
                            // -- 1 шт. решётки с 6 просечкми и 2 шт. решётки с 1 просечками (сумма просечек 8 < 12) (ручьёв 3<6)
                            // -- 1 шт. решётки с 6 просечкми и 1 шт. решётки с 1 просечками (сумма просечек 7 < 12) (ручьёв 2<6)
                            // -- 1-6 шт. решётки с 1 просечками (сумма просечек 1-6 < 12) (ручьёв 1-6=6)
                            // -- 1 шт. решётки с 6 просечками (сумма просечек 6 < 12) (ручьёв 1<6)

                            //Если первая решётка имеет 6 просечек
                            if (TkGridQuantityNotchesFirst.Text.ToInt() == 6)
                            {
                                // Проверяем вариант с разными решётками на одной заготовке
                                for (int i = 5; i >= 0; i--)
                                {
                                    // Проверяем, что проходим по максимальной ширине заготовки. По максимальным количествам ручьёв и просечек мы проходим.
                                    if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * i,
                                        TkGridQuantityNotchesFirst.Text.ToInt() * 1 + TkGridQuantityNotchesSecond.Text.ToInt() * i,
                                        1 + i))
                                    {
                                        zLengthFirst = TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * i;
                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * 1;
                                        zQtySecondGridByFirstBillet = qtyGridsByWidth * i;

                                        // Расчёт второй заготовки
                                        // Скорее всего вся вторая заготовка будет отведена под решётку с 1 просечками
                                        // Находим максимальное кол-во решёток с 1 просечками, которое поместится на заготовку с сохранением условий размещения на заготовке

                                        // Количество ручьёв на заготовке
                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;
                                        while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                        zWidthSecond = zWidthFirst;

                                        zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;

                                        break;
                                    }
                                }
                            }
                            //Если вторая решётка имеет 6 просечек
                            else if (TkGridQuantityNotchesSecond.Text.ToInt() == 6)
                            {
                                // Проверяем вариант с разными решётками на одной заготовке
                                for (int i = 5; i >= 0; i--)
                                {
                                    // Проверяем, что проходим по максимальной ширине заготовки. По максимальным количествам ручьёв и просечек мы проходим.
                                    if (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * 1 + TkGridLengthFirst.Text.ToInt() * i,
                                        TkGridQuantityNotchesSecond.Text.ToInt() * 1 + TkGridQuantityNotchesFirst.Text.ToInt() * i,
                                        1 + i))
                                    {
                                        zLengthFirst = TkGridLengthSecond.Text.ToInt() * 1 + TkGridLengthFirst.Text.ToInt() * i;
                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * i;
                                        zQtySecondGridByFirstBillet = qtyGridsByWidth * 1;


                                        // Расчёт второй заготовки
                                        // Скорее всего вся вторая заготовка будет отведена под решётку с 1 просечками
                                        // Находим максимальное кол-во решёток с 1 просечками, которое поместится на заготовку с сохранением условий размещения на заготовке

                                        // Количество ручьёв на заготовке
                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;
                                        while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthSecond = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                        zWidthSecond = zWidthFirst;

                                        zQtyFirstGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;

                                        break;
                                    }
                                }
                            }
                        }
                        // Вариант (6 к 3) или (3 к 6) 
                        else if ((TkGridQuantityNotchesFirst.Text.ToInt() == 6 && TkGridQuantityNotchesSecond.Text.ToInt() == 3) || (TkGridQuantityNotchesFirst.Text.ToInt() == 3 && TkGridQuantityNotchesSecond.Text.ToInt() == 6))
                        {
                            // Возможные варианты расположения решёток на заготовке
                            // -- 1 реш. с 6 просечками и 2 реш. с 3 просечками (сумма просечек 12 = 12) (ручьёв 3<6)
                            // -- 1 реш. с 6 просечками и 1 реш. с 3 просечками (сумма просечек 9 < 12) (ручьёв 2<6)
                            // -- 2 реш. с 6 просечками (сумма просечек 12 = 12) (ручьёв 2<6)
                            // -- 1 реш. с 6 просечками (сумма просечек 6 < 12) (ручьёв 1<6)
                            // -- 1-4 реш. с 3 просечками (сумма просечек 3-12 <= 12) (ручьёв 1-4<6)

                            // Если первая решётка имеет 6 просечек
                            if (TkGridQuantityNotchesFirst.Text.ToInt() == 6)
                            {
                                // Проверяем -- 1 реш. с 6 просечками и 2 реш. с 3 просечками (сумма просечек 12 = 12)
                                if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * 2,
                                    TkGridQuantityNotchesFirst.Text.ToInt() * 1 + TkGridQuantityNotchesSecond.Text.ToInt() * 2,
                                    3))
                                {
                                    zLengthFirst = TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * 2;
                                    zQtyFirstGridByFirstBillet = qtyGridsByWidth * 1;
                                    zQtySecondGridByFirstBillet = qtyGridsByWidth * 2;
                                }
                                // Проверяем -- 1 реш. с 6 просечками и 1 реш. с 3 просечками (сумма просечек 9 < 12)
                                else if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * 1,
                                    TkGridQuantityNotchesFirst.Text.ToInt() * 1 + TkGridQuantityNotchesSecond.Text.ToInt() * 1,
                                    2))
                                {
                                    zLengthFirst = TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * 1;
                                    zQtyFirstGridByFirstBillet = qtyGridsByWidth * 1;
                                    zQtySecondGridByFirstBillet = qtyGridsByWidth * 1;

                                    // Находим добивочную заготовку
                                    // Скорее всего добивочная решётка та, у которой количество просечек 3

                                    // Находим количество второй решётки на второй заготовке

                                    // Количество ручьёв на заготовке
                                    int qtyStreamByBillet = 0;
                                    var iterator = 1;
                                    while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                    {
                                        qtyStreamByBillet = iterator;
                                        iterator += 1;
                                    }

                                    zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                    zWidthSecond = zWidthFirst;

                                    zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                }
                                // Каждая решётка на своей заготовке. Проверяем оставшиеся варианты
                                else
                                {
                                    for (int i = 2; i > 0; i--)
                                    {
                                        if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * i, TkGridQuantityNotchesFirst.Text.ToInt() * i, i))
                                        {
                                            zLengthFirst = TkGridLengthFirst.Text.ToInt() * i;
                                            zQtyFirstGridByFirstBillet = qtyGridsByWidth * i;

                                            break;
                                        }
                                    }

                                    // Находим количество второй решётки на второй заготовке

                                    // Количество ручьёв на заготовке
                                    int qtyStreamByBillet = 0;
                                    var iterator = 1;
                                    while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                    {
                                        qtyStreamByBillet = iterator;
                                        iterator += 1;
                                    }

                                    zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                    zWidthSecond = zWidthFirst;

                                    zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                }
                            }
                            // Если вторая решётка имеет 6 просечек
                            else if (TkGridQuantityNotchesSecond.Text.ToInt() == 6)
                            {
                                // Проверяем -- 1 реш. с 6 просечками и 2 реш. с 3 просечками (сумма просечек 12 = 12)
                                if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * 2 + TkGridLengthSecond.Text.ToInt() * 1,
                                    TkGridQuantityNotchesFirst.Text.ToInt() * 2 + TkGridQuantityNotchesSecond.Text.ToInt() * 1,
                                    3))
                                {
                                    zLengthFirst = TkGridLengthFirst.Text.ToInt() * 2 + TkGridLengthSecond.Text.ToInt() * 1;
                                    zQtyFirstGridByFirstBillet = qtyGridsByWidth * 2;
                                    zQtySecondGridByFirstBillet = qtyGridsByWidth * 1;
                                }
                                // Проверяем -- 1 реш. с 6 просечками и 1 реш. с 3 просечками (сумма просечек 9 < 12)
                                else if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * 1,
                                    TkGridQuantityNotchesFirst.Text.ToInt() * 1 + TkGridQuantityNotchesSecond.Text.ToInt() * 1,
                                    2))
                                {
                                    zLengthFirst = TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * 1;
                                    zQtyFirstGridByFirstBillet = qtyGridsByWidth * 1;
                                    zQtySecondGridByFirstBillet = qtyGridsByWidth * 1;

                                    // Находим добивочную заготовку
                                    // Скорее всего добивочная решётка та, у которой количество просечек 3

                                    // Находим количество первой решётки на второй заготовке

                                    // Количество ручьёв на заготовке
                                    int qtyStreamByBillet = 0;
                                    var iterator = 1;
                                    while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                    {
                                        qtyStreamByBillet = iterator;
                                        iterator += 1;
                                    }

                                    zLengthSecond = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                    zWidthSecond = zWidthFirst;

                                    zQtyFirstGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                }
                                // Каждая решётка на своей заготовке. Проверяем оставшиеся варианты
                                else
                                {
                                    for (int i = 2; i > 0; i--)
                                    {
                                        if (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * i, TkGridQuantityNotchesSecond.Text.ToInt() * i, i))
                                        {
                                            zLengthSecond = TkGridLengthSecond.Text.ToInt() * i;
                                            zQtySecondGridBySecondBillet = qtyGridsByWidth * i;

                                            break;
                                        }
                                    }

                                    // Находим количество первой решётки на первой заготовке

                                    // Количество ручьёв на заготовке
                                    int qtyStreamByBillet = 0;
                                    var iterator = 1;
                                    while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                    {
                                        qtyStreamByBillet = iterator;
                                        iterator += 1;
                                    }

                                    zLengthFirst = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                    zWidthSecond = zWidthFirst;

                                    zQtyFirstGridByFirstBillet = qtyGridsByWidth * qtyStreamByBillet;
                                }
                            }
                        }
                        // Вариант (7 к 1) или (1 к 7) 
                        else if ((TkGridQuantityNotchesFirst.Text.ToInt() == 7 && TkGridQuantityNotchesSecond.Text.ToInt() == 1) || (TkGridQuantityNotchesFirst.Text.ToInt() == 1 && TkGridQuantityNotchesSecond.Text.ToInt() == 7))
                        {
                            // -- 1 шт. решётки с 7 просечкми и 5 шт. решётки с 1 просечками (сумма просечек 12 = 12) (ручьёв 6=6)
                            // -- 1 шт. решётки с 7 просечкми и 4 шт. решётки с 1 просечками (сумма просечек 11 < 12) (ручьёв 5<6)
                            // -- 1 шт. решётки с 7 просечкми и 3 шт. решётки с 1 просечками (сумма просечек 10 < 12) (ручьёв 4<6)
                            // -- 1 шт. решётки с 7 просечкми и 2 шт. решётки с 1 просечками (сумма просечек 9 < 12) (ручьёв 3<6)
                            // -- 1 шт. решётки с 7 просечкми и 1 шт. решётки с 1 просечками (сумма просечек 8 < 12) (ручьёв 2<6)
                            // -- 1-6 шт. решётки с 1 просечками (сумма просечек 1-6 < 12) (ручьёв 1-6=6)
                            // -- 1 шт. решётки с 7 просечками (сумма просечек 7 < 12) (ручьёв 1<6)

                            //Если первая решётка имеет 7 просечек
                            if (TkGridQuantityNotchesFirst.Text.ToInt() == 7)
                            {
                                // Проверяем вариант с разными решётками на одной заготовке
                                for (int i = 5; i >= 0; i--)
                                {
                                    // Проверяем, что проходим по максимальной ширине заготовки. По максимальным количествам ручьёв и просечек мы проходим.
                                    if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * i,
                                        TkGridQuantityNotchesFirst.Text.ToInt() * 1 + TkGridQuantityNotchesSecond.Text.ToInt() * i,
                                        1 + i))
                                    {
                                        zLengthFirst = TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * i;
                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * 1;
                                        zQtySecondGridByFirstBillet = qtyGridsByWidth * i;

                                        // Расчёт второй заготовки
                                        // Скорее всего вся вторая заготовка будет отведена под решётку с 1 просечками
                                        // Находим максимальное кол-во решёток с 1 просечками, которое поместится на заготовку с сохранением условий размещения на заготовке

                                        // Количество ручьёв на заготовке
                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;
                                        while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                        zWidthSecond = zWidthFirst;

                                        zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;

                                        break;
                                    }
                                }
                            }
                            //Если вторая решётка имеет 7 просечек
                            else if (TkGridQuantityNotchesSecond.Text.ToInt() == 7)
                            {
                                // Проверяем вариант с разными решётками на одной заготовке
                                for (int i = 5; i >= 0; i--)
                                {
                                    // Проверяем, что проходим по максимальной ширине заготовки. По максимальным количествам ручьёв и просечек мы проходим.
                                    if (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * 1 + TkGridLengthFirst.Text.ToInt() * i,
                                        TkGridQuantityNotchesSecond.Text.ToInt() * 1 + TkGridQuantityNotchesFirst.Text.ToInt() * i,
                                        1 + i))
                                    {
                                        zLengthFirst = TkGridLengthSecond.Text.ToInt() * 1 + TkGridLengthFirst.Text.ToInt() * i;
                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * i;
                                        zQtySecondGridByFirstBillet = qtyGridsByWidth * 1;


                                        // Расчёт второй заготовки
                                        // Скорее всего вся вторая заготовка будет отведена под решётку с 1 просечками
                                        // Находим максимальное кол-во решёток с 1 просечками, которое поместится на заготовку с сохранением условий размещения на заготовке

                                        // Количество ручьёв на заготовке
                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;
                                        while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthSecond = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                        zWidthSecond = zWidthFirst;

                                        zQtyFirstGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;

                                        break;
                                    }
                                }
                            }
                        }
                        // Вариант (7 к 2) или (2 к 7) 
                        else if ((TkGridQuantityNotchesFirst.Text.ToInt() == 7 && TkGridQuantityNotchesSecond.Text.ToInt() == 2) || (TkGridQuantityNotchesFirst.Text.ToInt() == 2 && TkGridQuantityNotchesSecond.Text.ToInt() == 7))
                        {
                            // При таком наборе просечек мы можем расположить решётки на заготовку одним из следующих способом (чтобы сохранялось условие сумма просечек на заготовке <= 12)
                            // -- 1 шт. решётки с 7 просечкми и 2 шт. решётки с 2 просечками (сумма просечек 11 < 12) (ручьёв 3<6)
                            // -- 1 шт. решётки с 7 просечкми и 1 шт. решётки с 2 просечками (сумма просечек 9 < 12) (ручьёв 2<6)
                            // -- 1 шт. решётки с 7 просечками (сумма просечек 7 < 12) (ручьёв 1<6)
                            // -- 1-5 шт. решётки с 2 просечками (сумма просечек 2-10 < 12) (ручьёв 1-5<6)

                            //Если первая решётка имеет 7 просечек
                            if (TkGridQuantityNotchesFirst.Text.ToInt() == 7)
                            {
                                for (int i = 2; i >= 0; i--)
                                {
                                    if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * i,
                                        TkGridQuantityNotchesFirst.Text.ToInt() * 1 + TkGridQuantityNotchesSecond.Text.ToInt() * i,
                                        1 + i))
                                    {
                                        zLengthFirst = TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * i;
                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * 1;
                                        zQtySecondGridByFirstBillet = qtyGridsByWidth * i;

                                        // Расчёт второй заготовки
                                        // Скорее всего вся вторая заготовка будет отведена под решётку с 2 просечками
                                        // Находим максимальное кол-во решёток с 2 просечками, которое поместится на заготовку с сохранением условий размещения на заготовке

                                        // Количество ручьёв на заготовке
                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;

                                        while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                        zWidthSecond = zWidthFirst;

                                        zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;

                                        break;
                                    }
                                }
                            }
                            //Если вторая решётка имеет 7 просечек
                            else if (TkGridQuantityNotchesSecond.Text.ToInt() == 7)
                            {
                                for (int i = 2; i >= 0; i--)
                                {
                                    if (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * 1 + TkGridLengthFirst.Text.ToInt() * i,
                                        TkGridQuantityNotchesSecond.Text.ToInt() * 1 + TkGridQuantityNotchesFirst.Text.ToInt() * i,
                                        1 + i))
                                    {
                                        zLengthFirst = TkGridLengthSecond.Text.ToInt() * 1 + TkGridLengthFirst.Text.ToInt() * i;
                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * i;
                                        zQtySecondGridByFirstBillet = qtyGridsByWidth * 1;

                                        // Расчёт второй заготовки
                                        // Скорее всего вся вторая заготовка будет отведена под решётку с 2 просечками
                                        // Находим максимальное кол-во решёток с 2 просечками, которое поместится на заготовку с сохранением условий размещения на заготовке

                                        // Количество ручьёв на заготовке
                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;

                                        while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthSecond = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                        zWidthSecond = zWidthFirst;

                                        zQtyFirstGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;

                                        break;
                                    }
                                }
                            }
                        }
                        // Вариант (7 к 4) или (4 к 7) 
                        else if ((TkGridQuantityNotchesFirst.Text.ToInt() == 7 && TkGridQuantityNotchesSecond.Text.ToInt() == 4) || (TkGridQuantityNotchesFirst.Text.ToInt() == 4 && TkGridQuantityNotchesSecond.Text.ToInt() == 7))
                        {
                            // -- 1 шт. решётки с 7 просечкми и 1 шт. решётки с 4 просечками (сумма просечек 11 < 12) (ручьёв 2=6)
                            // -- 1 шт. решётки с 7 просечками (сумма просечек 7 < 12) (ручьёв 1<6)
                            // -- 1-3 шт. решётки с 4 просечками (сумма просечек 4-12 <= 12) (ручьёв 1-3=6)

                            //Если первая решётка имеет 7 просечек
                            if (TkGridQuantityNotchesFirst.Text.ToInt() == 7)
                            {
                                // Проверяем вариант с разными решётками на одной заготовке
                                for (int i = 1; i >= 0; i--)
                                {
                                    // Проверяем, что проходим по максимальной ширине заготовки. По максимальным количествам ручьёв и просечек мы проходим.
                                    if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * i,
                                        TkGridQuantityNotchesFirst.Text.ToInt() * 1 + TkGridQuantityNotchesSecond.Text.ToInt() * i,
                                        1 + i))
                                    {
                                        zLengthFirst = TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * i;
                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * 1;
                                        zQtySecondGridByFirstBillet = qtyGridsByWidth * i;

                                        // Расчёт второй заготовки
                                        // Скорее всего вся вторая заготовка будет отведена под решётку с 4 просечками
                                        // Находим максимальное кол-во решёток с 4 просечками, которое поместится на заготовку с сохранением условий размещения на заготовке

                                        // Количество ручьёв на заготовке
                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;
                                        while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                        zWidthSecond = zWidthFirst;

                                        zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;

                                        break;
                                    }
                                }
                            }
                            //Если вторая решётка имеет 7 просечек
                            else if (TkGridQuantityNotchesSecond.Text.ToInt() == 7)
                            {
                                // Проверяем вариант с разными решётками на одной заготовке
                                for (int i = 1; i >= 0; i--)
                                {
                                    // Проверяем, что проходим по максимальной ширине заготовки. По максимальным количествам ручьёв и просечек мы проходим.
                                    if (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * 1 + TkGridLengthFirst.Text.ToInt() * i,
                                        TkGridQuantityNotchesSecond.Text.ToInt() * 1 + TkGridQuantityNotchesFirst.Text.ToInt() * i,
                                        1 + i))
                                    {
                                        zLengthFirst = TkGridLengthSecond.Text.ToInt() * 1 + TkGridLengthFirst.Text.ToInt() * i;
                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * i;
                                        zQtySecondGridByFirstBillet = qtyGridsByWidth * 1;

                                        // Расчёт второй заготовки
                                        // Скорее всего вся вторая заготовка будет отведена под решётку с 1 просечками
                                        // Находим максимальное кол-во решёток с 1 просечками, которое поместится на заготовку с сохранением условий размещения на заготовке

                                        // Количество ручьёв на заготовке
                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;
                                        while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthSecond = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                        zWidthSecond = zWidthFirst;

                                        zQtyFirstGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;

                                        break;
                                    }
                                }
                            }

                        }
                        // Вариант (7 к 5) или (5 к 7)
                        else if ((TkGridQuantityNotchesFirst.Text.ToInt() == 7 && TkGridQuantityNotchesSecond.Text.ToInt() == 5) || (TkGridQuantityNotchesFirst.Text.ToInt() == 5 && TkGridQuantityNotchesSecond.Text.ToInt() == 7))
                        {
                            // -- 1 шт. решётки с 7 просечкми и 1 шт. решётки с 5 просечками (сумма просечек 12 = 12) (ручьёв 2=6)
                            // -- 1 шт. решётки с 7 просечками (сумма просечек 7 < 12) (ручьёв 1<6)
                            // -- 1-2 шт. решётки с 5 просечками (сумма просечек 5-10 < 12) (ручьёв 1-2=6)

                            //Если первая решётка имеет 7 просечек
                            if (TkGridQuantityNotchesFirst.Text.ToInt() == 7)
                            {
                                // Проверяем вариант с разными решётками на одной заготовке
                                for (int i = 1; i >= 0; i--)
                                {
                                    // Проверяем, что проходим по максимальной ширине заготовки. По максимальным количествам ручьёв и просечек мы проходим.
                                    if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * i,
                                        TkGridQuantityNotchesFirst.Text.ToInt() * 1 + TkGridQuantityNotchesSecond.Text.ToInt() * i,
                                        1 + i))
                                    {
                                        zLengthFirst = TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * i;
                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * 1;
                                        zQtySecondGridByFirstBillet = qtyGridsByWidth * i;

                                        // Расчёт второй заготовки
                                        // Находим решётку, у которой количество в комплекте больше, находим её количество в первой заготовке
                                        int q = zQtySecondGridByFirstBillet;
                                        // Делим найденное количество на количество этой решётки в комплекте
                                        double doubleQ = (double)((double)q / (double)TkGridQuantitySecond.Text.ToDouble());
                                        // Берём целую часть от полученного числа
                                        q = doubleQ.ToInt();
                                        // Умножаем получившееся число на количество второй решётки в комплекте (той, у которой количество в комплекте меньше)
                                        q = q * TkGridQuantityFirst.Text.ToInt();
                                        // Сравниваем получившееся число с количеством решёток из первой заготовки для второй решётки (той, у которой количество в комплекте меньше)
                                        // Если получившееся число больше, то добивочная решётка та, у которой количество в комплекте меньше
                                        if (q > zQtyFirstGridByFirstBillet)
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте меньше, а значит количество просечек больше
                                            // В данном случае мы можем расположить решётки на вторую заготовку двумя способами
                                            // -- 1 шт. решётки с 5 просечкми (сумма просечек 5 < 12)
                                            // -- 2 шт. решётки с 5 просечкми (сумма просечек 10 < 12)

                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtyFirstGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }
                                        // Если получившееся число меньше, то добивочная решётка та, у которой количество в комплекте больше
                                        else
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте больше, а значит количество просечек меньше

                                            // Находим количество решёток, которое помещается на вторую заготовку

                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }

                                        break;
                                    }
                                }

                                // Если не удалось разместить обе решётки на одной заготовке, то размещвем каждую решётку на своей заготовке
                                if (zLengthFirst == 0)
                                {
                                    //Для первой решётки
                                    {
                                        // Кличество ручьёв на заготовке
                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;

                                        while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthFirst = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();

                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * qtyStreamByBillet;
                                    }

                                    //Для второй
                                    {
                                        // Кличество ручьёв на заготовке
                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;

                                        while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                        zWidthSecond = zWidthFirst;

                                        zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                    }
                                }
                            }
                            //Если вторая решётка имеет 7 просечек
                            else if (TkGridQuantityNotchesSecond.Text.ToInt() == 7)
                            {
                                // Проверяем вариант с разными решётками на одной заготовке
                                for (int i = 1; i >= 0; i--)
                                {
                                    // Проверяем, что проходим по максимальной ширине заготовки. По максимальным количествам ручьёв и просечек мы проходим.
                                    if (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * 1 + TkGridLengthFirst.Text.ToInt() * i,
                                        TkGridQuantityNotchesSecond.Text.ToInt() * 1 + TkGridQuantityNotchesFirst.Text.ToInt() * i,
                                        1 + i))
                                    {
                                        zLengthFirst = TkGridLengthSecond.Text.ToInt() * 1 + TkGridLengthFirst.Text.ToInt() * i;
                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * i;
                                        zQtySecondGridByFirstBillet = qtyGridsByWidth * 1;

                                        // Расчёт второй заготовки
                                        // Находим решётку, у которой количество в комплекте больше, находим её количество в первой заготовке
                                        int q = zQtyFirstGridByFirstBillet;
                                        // Делим найденное количество на количество этой решётки в комплекте
                                        double doubleQ = (double)((double)q / (double)TkGridQuantityFirst.Text.ToDouble());
                                        // Берём целую часть от полученного числа
                                        q = doubleQ.ToInt();
                                        // Умножаем получившееся число на количество второй решётки в комплекте (той, у которой количество в комплекте меньше)
                                        q = q * TkGridQuantitySecond.Text.ToInt();
                                        // Сравниваем получившееся число с количеством решёток из первой заготовки для второй решётки (той, у которой количество в комплекте меньше)
                                        // Если получившееся число больше, то добивочная решётка та, у которой количество в комплекте меньше
                                        if (q > zQtySecondGridByFirstBillet)
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте меньше, а значит количество просечек больше
                                            // В данном случае мы можем расположить решётки на вторую заготовку двумя способами
                                            // -- 1 шт. решётки с 5 просечкми (сумма просечек 5 < 12)
                                            // -- 2 шт. решётки с 5 просечкми (сумма просечек 10 < 12)

                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }
                                        // Если получившееся число меньше, то добивочная решётка та, у которой количество в комплекте больше
                                        else
                                        {
                                            // Добивочная решётка та, у которой количество в комплекте больше, а значит количество просечек меньше

                                            // Находим количество решёток, которое помещается на вторую заготовку

                                            // Кличество ручьёв на заготовке
                                            int qtyStreamByBillet = 0;
                                            var iterator = 1;

                                            while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                            {
                                                qtyStreamByBillet = iterator;
                                                iterator += 1;
                                            }

                                            zLengthSecond = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                            zWidthSecond = zWidthFirst;

                                            zQtyFirstGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                        }

                                        break;
                                    }
                                }

                                // Если не удалось разместить обе решётки на одной заготовке, то размещвем каждую решётку на своей заготовке
                                if (zLengthFirst == 0)
                                {
                                    //Для первой решётки
                                    {
                                        // Кличество ручьёв на заготовке
                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;

                                        while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthFirst = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();

                                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * qtyStreamByBillet;
                                    }

                                    //Для второй
                                    {
                                        // Кличество ручьёв на заготовке
                                        int qtyStreamByBillet = 0;
                                        var iterator = 1;

                                        while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                        {
                                            qtyStreamByBillet = iterator;
                                            iterator += 1;
                                        }

                                        zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                        zWidthSecond = zWidthFirst;

                                        zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                                    }
                                }
                            }
                        }
                        // Вариант (8 к 4) или (4 к 8)
                        else if ((TkGridQuantityNotchesFirst.Text.ToInt() == 8 && TkGridQuantityNotchesSecond.Text.ToInt() == 4) || (TkGridQuantityNotchesFirst.Text.ToInt() == 4 && TkGridQuantityNotchesSecond.Text.ToInt() == 8))
                        {
                            // -- 1 реш. с 8 просечками и 1 реш. с 4 проческами (сумма просечек 8 + 4 = 12 = 12)
                            // -- 1 реш. с 8 просечками (сумма просечек 8 < 12)
                            // -- 1-3 реш. с 4 просечками (сумма просечек 4-12 <= 12)

                            // Если первая решётка имеет 8 просечек
                            if (TkGridQuantityNotchesFirst.Text.ToInt() == 8)
                            {
                                // Проверяем -- 1 реш. с 8 просечками и 1 реш. с 4 просечками (сумма просечек 12 = 12)
                                if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * 1,
                                    TkGridQuantityNotchesFirst.Text.ToInt() * 1 + TkGridQuantityNotchesSecond.Text.ToInt() * 1,
                                    2))
                                {
                                    zLengthFirst = TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * 1;
                                    zQtyFirstGridByFirstBillet = qtyGridsByWidth * 1;
                                    zQtySecondGridByFirstBillet = qtyGridsByWidth * 1;
                                }
                                // Проверяем -- 1 реш. с 8 просечками (сумма просечек 8 < 12)
                                else if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * 1, TkGridQuantityNotchesFirst.Text.ToInt() * 1, 1))
                                {
                                    zLengthFirst = TkGridLengthFirst.Text.ToInt() * 1;
                                    zQtyFirstGridByFirstBillet = qtyGridsByWidth * 1;
                                }

                                // Находим количество второй решётки (с 4 просечками) на второй заготовке

                                // Количество ручьёв на заготовке
                                int qtyStreamByBillet = 0;
                                var iterator = 1;
                                while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                                {
                                    qtyStreamByBillet = iterator;
                                    iterator += 1;
                                }

                                zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                                zWidthSecond = zWidthFirst;

                                zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                            }
                            // Если вторая решётка имеет 8 просечек
                            else if (TkGridQuantityNotchesSecond.Text.ToInt() == 8)
                            {
                                // Проверяем -- 1 реш. с 8 просечками и 1 реш. с 4 просечками (сумма просечек 12 = 12)
                                if (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * 1,
                                    TkGridQuantityNotchesFirst.Text.ToInt() * 1 + TkGridQuantityNotchesSecond.Text.ToInt() * 1,
                                    2))
                                {
                                    zLengthSecond = TkGridLengthFirst.Text.ToInt() * 1 + TkGridLengthSecond.Text.ToInt() * 1;
                                    zQtyFirstGridBySecondBillet = qtyGridsByWidth * 1;
                                    zQtySecondGridBySecondBillet = qtyGridsByWidth * 1;
                                }
                                // Проверяем -- 1 реш. с 8 просечками (сумма просечек 8 < 12)
                                else if (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * 1, TkGridQuantityNotchesSecond.Text.ToInt() * 1, 1))
                                {
                                    zLengthSecond = TkGridLengthSecond.Text.ToInt() * 1;
                                    zQtySecondGridBySecondBillet = qtyGridsByWidth * 1;
                                }

                                // Находим количество второй решётки (с 4 просечками) на второй заготовке

                                // Количество ручьёв на заготовке
                                int qtyStreamByBillet = 0;
                                var iterator = 1;
                                while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                                {
                                    qtyStreamByBillet = iterator;
                                    iterator += 1;
                                }

                                zLengthFirst = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                                zWidthSecond = zWidthFirst;

                                zQtyFirstGridByFirstBillet = qtyGridsByWidth * qtyStreamByBillet;
                            }
                        }
                        else
                        {
                            var msg = "Автоматический расчёт длины заготовки невозможен";
                            var d = new DialogWindow($"{msg}", "ТК решётки в сборе", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                }
                // Если выбранная продукция - это комплект решёток не в сборе
                else if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(8,100))
                {
                    // Размещаем каждую решётку на своей заготовке

                    // Первая заготовка
                    {
                        // Находим количество решёток, которое помещается на первую заготовку
                        // Количество ручьёв на заготовке
                        int qtyStreamByBillet = 0;
                        var iterator = 1;

                        while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                        {
                            qtyStreamByBillet = iterator;
                            iterator += 1;
                        }

                        zLengthFirst = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                        zQtyFirstGridByFirstBillet = qtyGridsByWidth * qtyStreamByBillet;
                    }

                    // Вторая заготовка
                    if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(100))
                    {
                        int qtyStreamByBillet = 0;
                        var iterator = 1;

                        while (CheckBlankLimit(TkGridLengthSecond.Text.ToInt() * iterator, TkGridQuantityNotchesSecond.Text.ToInt() * iterator, iterator))
                        {
                            qtyStreamByBillet = iterator;
                            iterator += 1;
                        }

                        zLengthSecond = qtyStreamByBillet * TkGridLengthSecond.Text.ToInt();
                        zWidthSecond = zWidthFirst;
                        zQtySecondGridBySecondBillet = qtyGridsByWidth * qtyStreamByBillet;
                    }
                }
                // Если выбранная продукция - это прокладки
                else if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(14,15,226))
                {

                    int qtyStreamByBillet = 0;

                    //Если прокладка с рилевками
                    if (TkGridQuantityCrease.Text.ToInt() > 0)
                    {
                        int s = 0;
                        int max = 0;
                        int i = 1;

                        //Расчет ширины для КГ-4
                        if (TkProductionScheme.SelectedItem.Key.ToInt() == 602)
                        {
                            max = 1800;
                            do
                            {
                                s += TkGridHeightFirst.Text.ToInt();
                                i++;
                            } while (s < max);
                            qtyGridsByWidth = 1;
                            zLengthFirst = TkGridLengthFirst.Text.ToInt();

                            zMinLength = 260;
                        }
                        //Расчет ширины для RdSc
                        else
                        {
                            max = 1030;
                            do
                            {
                                s += TkGridHeightFirst.Text.ToInt();
                                if (i < 3)
                                {
                                    max += 50;
                                }
                                i++;
                            } while (s < max);
                            int reminder = 2430 - (int)(2430 / TkGridLengthFirst.Text.ToInt()) * TkGridLengthFirst.Text.ToInt();
                            zLengthFirst = 2500 - reminder;
                            qtyGridsByWidth = (int)(2430 / TkGridLengthFirst.Text.ToInt());
                        }
                        qtyStreamByBillet = i - 2;
                        zWidthFirst = s - TkGridHeightFirst.Text.ToInt();


                    }
                    // Прокладка без рилевок
                    else
                    {
                        // Находим количество прокладок, которое помещается на заготовку
                        // Количество ручьёв на заготовке
                        var iterator = 1;

                        while (CheckBlankLimit(TkGridLengthFirst.Text.ToInt() * iterator, TkGridQuantityNotchesFirst.Text.ToInt() * iterator, iterator))
                        {
                            qtyStreamByBillet = iterator;
                            iterator += 1;
                        }

                        zLengthFirst = qtyStreamByBillet * TkGridLengthFirst.Text.ToInt();
                    }

                    zQtyFirstGridByFirstBillet = qtyGridsByWidth * qtyStreamByBillet;
                }
            }

            if (zLengthFirst != 0)
            {
                if (zLengthFirst < zMinLength && zLengthFirst >= (zMinLength - zOffsetMinLength))
                {
                    zLengthFirst = zMinLength;
                }
                else if (zLengthFirst < zMinLength && zLengthFirst < (zMinLength - zOffsetMinLength))
                {
                    var msg = "Внимание! Длина первой заготовки меньше допустимой.";
                    var d = new DialogWindow($"{msg}", "ТК", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }

            if (zLengthSecond != 0)
            {
                if (zLengthSecond < zMinLength && zLengthSecond >= (zMinLength - zOffsetMinLength))
                {
                    zLengthSecond = zMinLength;
                }
                else if (zLengthSecond < zMinLength && zLengthSecond < (zMinLength - zOffsetMinLength))
                {
                    var msg = "Внимание! Длина второй заготовки меньше допустимой.";
                    var d = new DialogWindow($"{msg}", "ТК", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }

            {
                TkBilletLegthFirst.Clear();
                TkBilletWidthFirst.Clear();
                TkBilletLegthFirst.Text = zLengthFirst.ToString();
                TkBilletWidthFirst.Text = zWidthFirst.ToString();
                if (zWidthFirst > 0 && zLengthFirst > 0)
                {
                    string s = "1";
                    CalculateBilletSquare(s);
                }

                TkBilletLegthSecond.Clear();
                TkBilletWidthSecond.Clear();
                if (zWidthSecond > 0 && zLengthSecond > 0)
                {
                    TkBilletLegthSecond.Text = zLengthSecond.ToString();
                    TkBilletWidthSecond.Text = zWidthSecond.ToString();
                    string s = "2";
                    CalculateBilletSquare(s);
                }

                TkBilletQuantityFirst1.Clear();
                if (zQtyFirstGridByFirstBillet > 0)
                {
                    TkBilletQuantityFirst1.Text = zQtyFirstGridByFirstBillet.ToString();
                }

                TkBilletQuantityFirst2.Clear();
                if (zQtySecondGridByFirstBillet > 0)
                {
                    TkBilletCalculateTwoFirst.IsChecked = true;
                    string s = "1";
                    CheckCalculateTwo(s);

                    TkBilletQuantityFirst2.Text = zQtySecondGridByFirstBillet.ToString();

                    if (string.IsNullOrEmpty(TkBilletQuantityFirst1.Text))
                    {
                        TkBilletQuantityFirst1.Text = "0";
                    }
                }

                TkBilletQuantitySecond2.Clear();
                if (zQtySecondGridBySecondBillet > 0)
                {
                    TkBilletQuantitySecond2.Text = zQtySecondGridBySecondBillet.ToString();
                }

                TkBilletQuantitySecond1.Clear();
                if (zQtyFirstGridBySecondBillet > 0)
                {
                    TkBilletCalculateTwoSecond.IsChecked = true;
                    string s = "2";
                    CheckCalculateTwo(s);

                    TkBilletQuantitySecond1.Text = zQtyFirstGridBySecondBillet.ToString();

                    if (string.IsNullOrEmpty(TkBilletQuantitySecond2.Text))
                    {
                        TkBilletQuantitySecond2.Text = "0";
                    }
                }
            }
        }
        /// <summary>
        /// Получение данных для нового метода подтверждения тех карты
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> GetDataForConfirmNew()
        {
            Dictionary<string, string> formValues = new Dictionary<string, string>();
            if (Form != null)
            {
                if (Form.Validate())
                {
                    if (TkGridCustomer.SelectedItem.Key != null && TkGridClient.SelectedItem.Key != null)
                    {
                        if (!string.IsNullOrEmpty(DataSetOfCustomer.Items.FirstOrDefault(x => x.CheckGet("ID") == TkGridCustomer.SelectedItem.Key).CheckGet("PATHTK_CONFIRM")))
                        {
                            if (!string.IsNullOrEmpty(TkGridNumberFirst.Text))
                            {
                                formValues = Form.GetValues();

                                formValues.Add("CUSTOMER_NAME", TkGridCustomer.SelectedItem.Value.ToString());
                                formValues.Add("CLIENT_NAME", DataSetClientToCustomer.Items.FirstOrDefault(x => x.CheckGet("POKUPATEL_ID") == TkGridClient.SelectedItem.Key).CheckGet("POKUPATEL_NAME"));
                                formValues.CheckAdd("COLLOR_NAME", TkCollor.SelectedItem.Value.ToString());
                                formValues.CheckAdd("PROFILE_NAME", TkProfile.SelectedItem.Value.ToString());
                                formValues.CheckAdd("BRAND_NAME", TkBrand.SelectedItem.Value.ToString());
                                formValues.CheckAdd("PALLET_NAME", TkPallet.SelectedItem.Value.ToString());

                                if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(100,229))
                                {
                                    formValues.CheckAdd("PALLET_NAME2", TkPallet2.SelectedItem.Value.ToString());
                                }
                                else if(TkGridTypeProduct.SelectedItem.Key.ToInt()==1)
                                {
                                    formValues.CheckAdd("WIDTH_FIRST", TkGridOneNotch.Text.ToString());
                                }

                                formValues.CheckAdd("PATHTK_NEW", DataSetOfCustomer.Items.FirstOrDefault(x => x.CheckGet("ID") == TkGridCustomer.SelectedItem.Key).CheckGet("PATHTK_NEW"));
                                formValues.CheckAdd("PATHTK", PathTechnologicalMapNew);
                                formValues.CheckAdd("PATHTK_CONFIRM", DataSetOfCustomer.Items.FirstOrDefault(x => x.CheckGet("ID") == TkGridCustomer.SelectedItem.Key).CheckGet("PATHTK_CONFIRM"));
                                formValues.CheckAdd("PATHTK_ARCHIVE", DataSetOfCustomer.Items.FirstOrDefault(x => x.CheckGet("ID") == TkGridCustomer.SelectedItem.Key).CheckGet("PATHTK_ARCHIVE"));
                                formValues.CheckAdd("CUSTOMER_SHORT", DataSetOfCustomer.Items.FirstOrDefault(x => x.CheckGet("ID") == TkGridCustomer.SelectedItem.Key).CheckGet("CUSTOMER_SHORT"));

                                formValues.CheckAdd("TK_ID_FIRST", TechnologicalMapIDFirst.ToString());
                                formValues.CheckAdd("TK_ID_SECOND", TechnologicalMapIDSecond.ToString());
                                formValues.CheckAdd("ID2_FIRST", BlankFirstId.ToString());
                                formValues.CheckAdd("ID2_SECOND", BlankSecondId.ToString());
                            }
                            else
                            {
                                var msg = "Не задан артикул";
                                var d = new DialogWindow($"{msg}", "ТК решётки", "", DialogWindowButtons.OK);
                                d.ShowDialog();
                            }
                        }
                        else
                        {
                            var msg = "У данного покупателя не указан путь к папке для сохранения Excel файлов";
                            var d = new DialogWindow($"{msg}", "ТК решётки", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                    else
                    {
                        var msg = "Заполните поля Покупатель и Потребитель";
                        var d = new DialogWindow($"{msg}", "ТК решётки", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    var msg = "Проверьте корректность введёных данных";
                    var d = new DialogWindow($"{msg}", "Техкарта", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }

            return formValues;
        }
        /// <summary>
        /// Копирование тех карты;
        /// Получает данные по выбранной тех карте и передаёт их в новую форму 
        /// (без артикулов и заготовок, без id, только текст в нужные поля)
        /// </summary>
        /// <param name="id_set"></param>
        public void CopyBySet(int id_set)
        {
            IdSet = id_set;
            GetTechnologicalMapDataBySet(false);
            SetCopyDataFromForm(DataSetOfExistingTechnologicalMap);
            IdSet = 0;

            Show();
        }

        /// <summary>
        /// установка значений в поля формы для копии техкарты
        /// </summary>
        /// <param name="dataSet"></param>
        public void SetCopyDataFromForm(ListDataSet dataSet)
        {
            if (dataSet.Items.Count > 0)
            {
                var ds = dataSet.Items.First();

                TkGridTypeProduct.SetSelectedItemByKey(ds.CheckGet("TYPE_PRODUCT"));
                TkGridNameFirst.Text = $"[КОПИЯ]{ds.CheckGet("NAME_FIRST")}";
                TkGridNameSecond.Text = $"[КОПИЯ]{ds.CheckGet("NAME_SECOND")}";
                TkGridClient.SetSelectedItemByKey(ds.CheckGet("CLIENT"));

                if (ds.CheckGet("CUSTOMER").ToInt() > 0)
                {
                    TkGridCustomer.SetSelectedItemByKey(ds.CheckGet("CUSTOMER"));
                }
                else
                {
                    TkGridCustomer.SelectedItem = TkGridCustomer.Items.FirstOrDefault(x => x.Value == ds.CheckGet("CUSTOMER_NAME"));
                }

                TkGridDetails.Text = ds.CheckGet("DETAILS");
                TkGridLengthFirst.Text = ds.CheckGet("LENGTH_FIRST");
                TkGridHeightFirst.Text = ds.CheckGet("HEIGHT_FIRST");
                TkGridQuantityNotchesFirst.Text = ds.CheckGet("QUANTITY_NOTCHES_FIRST");
                TkGridLengthSecond.Text = ds.CheckGet("LENGTH_SECOND");
                TkGridHeightSecond.Text = ds.CheckGet("HEIGHT_SECOND");
                TkGridQuantityNotchesSecond.Text = ds.CheckGet("QUANTITY_NOTCHES_SECOND");
                TkProfile.SetSelectedItemByKey(ds.CheckGet("PROFILE"));
                TkBrand.SetSelectedItemByKey(ds.CheckGet("BRAND"));
                TkCollor.SetSelectedItemByKey(ds.CheckGet("COLLOR"));
                TkCardboard.SetSelectedItemByKey(ds.CheckGet("CARDBOARD"));
                TkSpecialMaterial.IsChecked = ds.CheckGet("SPECIAL_MATERIAL").ToBool();
                TkProductionScheme.SetSelectedItemByKey(ds.CheckGet("PRODUCTION_SCHEME"));
                TkBilletSProduct.Text = ds.CheckGet("BILLET_SPRODUCT").ToDouble().ToString();
                TkBilletLegthFirst.Text = ds.CheckGet("BILLET_LENGTH_FIRST");
                TkBilletWidthFirst.Text = ds.CheckGet("BILLET_WIDTH_FIRST");
                TkBilletSquareFirst.Text = ds.CheckGet("BILLET_SQUARE_FIRST").ToDouble().ToString();
                TkBilletLegthSecond.Text = ds.CheckGet("BILLET_LENGTH_SECOND");
                TkBilletWidthSecond.Text = ds.CheckGet("BILLET_WIDTH_SECOND");
                TkBilletSquareSecond.Text = ds.CheckGet("BILLET_SQUARE_SECOND").ToDouble().ToString();
                TkPallet.SetSelectedItemByKey(ds.CheckGet("PALLET"));
                TkLayingScheme.SetSelectedItemByKey(ds.CheckGet("LAYING_SCHEME"));
                TkQuantity.Text = ds.CheckGet("QUANTITY");
                TkQuantityPack.Text = ds.CheckGet("QUANTITY_PACK");
                TkQuantityRows.Text = ds.CheckGet("QUANTITY_ROWS");
                TkQuantityBox.Text = ds.CheckGet("QUANTITY_BOX");
                TkPrepressing.IsChecked = ds.CheckGet("PREPRESSING").ToBool();
                TkCorners.IsChecked = ds.CheckGet("CORNERS").ToBool();
                TkPackaging.SetSelectedItemByKey(ds.CheckGet("PACKAGING"));
                TkStrapping.SetSelectedItemByKey(ds.CheckGet("STRAPPING"));
                TkPackageLength.Text = ds.CheckGet("PACKAGE_LENGTH");
                TkPackageWidth.Text = ds.CheckGet("PACKAGE_WIDTH");
                TkPackageHeigth.Text = ds.CheckGet("PACKAGE_HEIGTH");
                Form.SetValueByPath("TYPE_PACKAGE", ds.CheckGet("TYPE_PACKAGE"));
                OnEdgeCheckBox.IsChecked = ds.CheckGet("ON_EDGE").ToBool();
                OnEdgeCheckBox.Background = null;

                TkProductionScheme2.SetSelectedItemByKey(ds.CheckGet("PRODUCTION_SCHEME2"));
                TkBilletSProduct2.Text = ds.CheckGet("BILLET_SPRODUCT2").ToDouble().ToString();
                TkPallet2.SetSelectedItemByKey(ds.CheckGet("PALLET2"));
                TkLayingScheme2.SetSelectedItemByKey(ds.CheckGet("LAYING_SCHEME2"));
                TkQuantity2.Text = ds.CheckGet("QUANTITY2");
                TkQuantityPack2.Text = ds.CheckGet("QUANTITY_PACK2");
                TkQuantityRows2.Text = ds.CheckGet("QUANTITY_ROWS2");
                TkQuantityBox2.Text = ds.CheckGet("QUANTITY_BOX2");
                TkPrepressing2.IsChecked = ds.CheckGet("PREPRESSING2").ToBool();
                TkCorners2.IsChecked = ds.CheckGet("CORNERS2").ToBool();
                TkPackaging2.SetSelectedItemByKey(ds.CheckGet("PACKAGING2"));
                TkStrapping2.SetSelectedItemByKey(ds.CheckGet("STRAPPING2"));
                TkPackageLength2.Text = ds.CheckGet("PACKAGE_LENGTH2");
                TkPackageWidth2.Text = ds.CheckGet("PACKAGE_WIDTH2");
                TkPackageHeigth2.Text = ds.CheckGet("PACKAGE_HEIGTH2");
                Form.SetValueByPath("TYPE_PACKAGE2", ds.CheckGet("TYPE_PACKAGE2"));
                OnEdge2CheckBox.IsChecked = ds.CheckGet("ON_EDGE2").ToBool();
                OnEdge2CheckBox.Background = null;

                TkGridQuantityFirst.Text = ds.CheckGet("QUANTITY_FIRST");
                TkGridQuantitySecond.Text = ds.CheckGet("QUANTITY_SECOND");

                // Просечки первой решётки
                {
                    List<Dictionary<string, string>> ListDicNotchesParametrsFirst = new List<Dictionary<string, string>>();

                    var listNotchesFirst = ds.CheckGet("LIST_NOTCHES_FIRST").ToString();
                    string[] arrayStringNotchesFirst = listNotchesFirst.Split(';');

                    foreach (var item in arrayStringNotchesFirst)
                    {
                        if (!string.IsNullOrEmpty(item))
                        {
                            var dicStringNotchesFirstParametrs = new Dictionary<string, string>();

                            string[] arrayParametrsOfStringNotches = item.Split(':');

                            dicStringNotchesFirstParametrs.Add("NUMBER", arrayParametrsOfStringNotches[0]);
                            dicStringNotchesFirstParametrs.Add("CONTENT", arrayParametrsOfStringNotches[1].ToDouble().ToString());

                            ListDicNotchesParametrsFirst.Add(dicStringNotchesFirstParametrs);
                        }
                    }

                    ListDataSet dsNotchesFirst = new ListDataSet();

                    dsNotchesFirst.Items = ListDicNotchesParametrsFirst;

                    NotchesFirstGrid.UpdateItems(dsNotchesFirst);
                }

                // Просечки второй решётки
                {
                    List<Dictionary<string, string>> ListDicNotchesParametrsSecond = new List<Dictionary<string, string>>();

                    var listNotchesSecond = ds.CheckGet("LIST_NOTCHES_SECOND").ToString();
                    string[] arrayStringNotchesSecond = listNotchesSecond.Split(';');

                    foreach (var item in arrayStringNotchesSecond)
                    {
                        if (!string.IsNullOrEmpty(item))
                        {
                            var dicStringNotchesSecondParametrs = new Dictionary<string, string>();

                            string[] arrayParametrsOfStringNotches = item.Split(':');

                            dicStringNotchesSecondParametrs.Add("NUMBER", arrayParametrsOfStringNotches[0]);
                            dicStringNotchesSecondParametrs.Add("CONTENT", arrayParametrsOfStringNotches[1].ToDouble().ToString());

                            ListDicNotchesParametrsSecond.Add(dicStringNotchesSecondParametrs);
                        }
                    }

                    ListDataSet dsNotchesSecond = new ListDataSet();

                    dsNotchesSecond.Items = ListDicNotchesParametrsSecond;

                    NotchesSecondGrid.UpdateItems(dsNotchesSecond);
                }

                CalculateSumNotches();

                CheckTkTypePackage();
                CheckTkTypePackage2();
            }
        }

        #endregion


        #region DependensFunctions
        /// <summary>
        /// Проверка, что все поля для Картона заполнены (Профиль, Марка, Цвет);
        /// Если все поля заполнены, то корректируем наполнение селектбокса картона по заполненным параметрам (Профиль, Марка, Цвет).
        /// </summary>
        public void CheckCardboardParametrs()
        {
            if (TkProfile.SelectedItem.Key.ToInt() > 0 && TkBrand.SelectedItem.Key.ToInt() > 0 && TkCollor.SelectedItem.Key.ToInt() > 0)
            {
                var id_prof = TkProfile.SelectedItem.Key;
                var id_marka = TkBrand.SelectedItem.Key;
                var id_outer = TkCollor.SelectedItem.Key;

                var dic = new Dictionary<string, string>();

                foreach (var item in DataSetOfCardboard.Items)
                {
                    if (item.CheckGet("ID_PROF") == id_prof && item.CheckGet("ID_MARKA") == id_marka && item.CheckGet("ID_OUTER") == id_outer)
                    {
                        dic.Add(item.CheckGet("ID"), item.CheckGet("NAME"));
                    }
                }

                if (dic.Count > 0)
                {
                    TkCardboard.SetItems(dic);
                    TkCardboard.SetSelectedItemFirst();
                }
                else
                {
                    ClearSelectBox(TkCardboard);
                    //TkCardboard.ValueTextBox.Clear();
                    TkCardboard.Items.Add(EmptyDictionary.CheckGet("ID"), EmptyDictionary.CheckGet("NAME"));
                    TkCardboard.SelectedItem = TkCardboard.Items.First();
                }
            }
        }

        /// <summary>
        /// Для продукции прокладки (пачки) подставляем значение для количества в пачке по умолчанию в зависимости от выбранного профиля
        /// </summary>
        public void SetTkQuantityByProfile()
        {
            // Тип продукции - прокладки (пачки)
            if (TkGridTypeProduct.SelectedItem.Key.ToInt() == 15)
            {
                if (TkProfile.SelectedItem.Key != null)
                {
                    switch (TkProfile.SelectedItem.Key)
                    {
                        // B
                        case "1":
                            TkQuantity.Text = "100";
                            break;

                        // C
                        case "2":
                            TkQuantity.Text = "100";
                            break;

                        // BC
                        case "3":
                            TkQuantity.Text = "50";
                            break;

                        // E
                        case "4":
                            TkQuantity.Text = "150";
                            break;

                        default:
                            break;
                    }
                }
            }
        }
        /// <summary>
        /// Проверка, что необходимые для просчёта площади заготовки заполнены; 
        /// Автоматический просчёт площади заготовки и заполнение соответствующего параметра.
        /// </summary>
        /// <param name="billetProduction">Условный идентификатор поля. С лева на право (1; 2).</param>
        public void CalculateBilletSquare(string billetProduction)
        {
            int legth;
            int width;
            int koef = 1000000;
            double square;

            switch (billetProduction)
            {
                case "1":
                    if (TkBilletLegthFirst.Text.ToInt() > 0 && TkBilletWidthFirst.Text.ToInt() > 0)
                    {
                        legth = TkBilletLegthFirst.Text.ToInt();
                        width = TkBilletWidthFirst.Text.ToInt();
                        square = (double)legth * (double)width / (double)koef;
                        TkBilletSquareFirst.Text = square.ToString();
                    }
                    else
                    {
                        TkBilletSquareFirst.Clear();
                    }
                    break;

                case "2":
                    if (TkBilletLegthSecond.Text.ToInt() > 0 && TkBilletWidthSecond.Text.ToInt() > 0)
                    {
                        legth = TkBilletLegthSecond.Text.ToInt();
                        width = TkBilletWidthSecond.Text.ToInt();
                        square = (double)legth * (double)width / (double)koef;
                        TkBilletSquareSecond.Text = square.ToString();
                    }
                    else
                    {
                        TkBilletSquareSecond.Clear();
                    }
                    break;

                default:
                    break;
            }
        }
        /// <summary>
        /// Поиск соответствующего потребителя для выбранного покупателя через датасет от таблицы t.Moliza
        /// </summary>
        public void CheckCustomerByClient()
        {
            var key = TkGridClient.SelectedItem.Key;
            var dicItems = new Dictionary<string, string>();

            if (DataSetClientToCustomer?.Items.Count > 0)
            {
                foreach (var item in DataSetClientToCustomer.Items)
                {
                    if (item.CheckGet("POKUPATEL_ID") == key)
                    {
                        dicItems.Add(item.CheckGet("CUSTOMER_ID"), item.CheckGet("CUSTOMER_NAME"));
                    }
                }

            }

            TkGridCustomer.SetItems(dicItems);

            if (TkGridCustomer.Items.Count > 0)
            {
                TkGridCustomer.SetSelectedItemFirst();
            }
            else
            {
                ClearSelectBox(TkGridCustomer);
                //TkGridCustomer.ValueTextBox.Clear();

                TkGridCustomer.Items.Add(EmptyDictionary.CheckGet("ID"), EmptyDictionary.CheckGet("NAME"));
                TkGridCustomer.SelectedItem = TkGridCustomer.Items.First();
            }
        }
        /// <summary>
        /// Проверяет, нажат ли чекбокс (Считать для двух продукций);
        /// Меняет состояние поля для второй продукции.
        /// </summary>
        /// <param name="billetProduction"></param>
        public void CheckCalculateTwo(string billetProduction)
        {
            switch (billetProduction)
            {
                case "1":
                    if (TkBilletCalculateTwoFirst.IsChecked.ToInt() == 1)
                    {
                        TkBilletQuantityFirst2.IsEnabled = true;
                    }
                    else
                    {
                        TkBilletQuantityFirst2.Clear();
                        TkBilletQuantityFirst2.IsEnabled = false;
                    }

                    break;

                case "2":
                    if (TkBilletCalculateTwoSecond.IsChecked.ToInt() == 1)
                    {
                        TkBilletQuantitySecond1.IsEnabled = true;
                    }
                    else
                    {
                        TkBilletQuantitySecond1.Clear();
                        TkBilletQuantitySecond1.IsEnabled = false;
                    }

                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Автоматический расчёт поля Количество Ящиков в зависимости от полей (Количество в пачке, Количество в ряду, Количество рядов)
        /// </summary>
        public void CalculateTkQuantityBox()
        {
            TkQuantityBox.Clear();
            if (TkQuantity.Text.ToInt() > 0 && TkQuantityPack.Text.ToInt() > 0 && TkQuantityRows.Text.ToInt() > 0)
            {
                var tkQuantity = TkQuantity.Text.ToInt();
                var tkQuantityPack = TkQuantityPack.Text.ToInt();
                var tkQuantityRows = TkQuantityRows.Text.ToInt();

                var tkQuantityBox = tkQuantity * tkQuantityPack * tkQuantityRows;
                TkQuantityBox.Text = tkQuantityBox.ToString();
            }
        }

        /// <summary>
        /// Автоматический расчёт поля Количество Ящиков в зависимости от полей (Количество в пачке, Количество в ряду, Количество рядов)
        /// (для второй решётки для типа продукции Решётки не в сборе)
        /// </summary>
        public void CalculateTkQuantityBox2()
        {
            TkQuantityBox2.Clear();
            if (TkQuantity2.Text.ToInt() > 0 && TkQuantityPack2.Text.ToInt() > 0 && TkQuantityRows2.Text.ToInt() > 0)
            {
                var tkQuantity = TkQuantity2.Text.ToInt();
                var tkQuantityPack = TkQuantityPack2.Text.ToInt();
                var tkQuantityRows = TkQuantityRows2.Text.ToInt();

                var tkQuantityBox = tkQuantity * tkQuantityPack * tkQuantityRows;
                TkQuantityBox2.Text = tkQuantityBox.ToString();
            }
        }
        /// <summary>
        /// Расчёт габаритов транспортного пакета в зависимости от габаритов решётки и выбранной схемы укладки
        /// </summary>
        public void CalculateDimensionOfTransportPackage()
        {
            // Если не укладка на ребро, то можем рассчитывать
            if ((bool)OnEdgeCheckBox.IsChecked == false)
            {
                // Получение габаритов ТП через функции БД
                var p = new Dictionary<string, string>();
                var msg = "";
                if (Form.GetValueByPath("TYPE_PRODUCT").IsNullOrEmpty())
                {
                    msg += " тип продукции";
                }
                else
                {
                    p.Add("TYPE_PRODUCT", Form.GetValueByPath("TYPE_PRODUCT"));
                }
                if (Form.GetValueByPath("LENGTH_FIRST").IsNullOrEmpty())
                {
                    msg += " длина,";
                }
                else
                {
                    p.Add("LENGTH", Form.GetValueByPath("LENGTH_FIRST"));
                }
                if (Form.GetValueByPath("HEIGHT_FIRST").IsNullOrEmpty())
                {
                    msg += " ширина,";
                }
                else
                {
                    p.Add("HEIGHT", Form.GetValueByPath("HEIGHT_FIRST"));
                }
                if (Form.GetValueByPath("TYPE_PRODUCT").ToInt() != 1)
                {
                    if (Form.GetValueByPath("PRODUCTION_SCHEME").IsNullOrEmpty())
                    {
                        msg += " схема производства";
                    }
                    else
                    {
                        p.Add("PRODUCTION_SCHEME", Form.GetValueByPath("PRODUCTION_SCHEME"));
                    }
                }

                if (Form.GetValueByPath("PROFILE").IsNullOrEmpty())
                {
                    msg += " профиль,";
                }
                else
                {
                    p.Add("PROFILE", Form.GetValueByPath("PROFILE"));
                }
                if (Form.GetValueByPath("CARDBOARD").IsNullOrEmpty())
                {
                    msg += " картон";
                }
                else
                {
                    p.Add("CARDBOARD", Form.GetValueByPath("CARDBOARD"));
                }

                if (Form.GetValueByPath("PALLET").IsNullOrEmpty())
                {
                    msg += " поддон,";
                }
                else
                {
                    p.Add("PALLET", Form.GetValueByPath("PALLET"));
                }
                if (Form.GetValueByPath("LAYING_SCHEME").IsNullOrEmpty())
                {
                    msg += " схема укладки,";
                }
                else
                {
                    p.Add("LAYING_SCHEME", Form.GetValueByPath("LAYING_SCHEME"));
                }

                if (Form.GetValueByPath("TYPE_PACKAGE").ToInt() == 1 && Form.GetValueByPath("QUANTITY_ROWS").IsNullOrEmpty())
                {
                    msg += " количество рядов,";
                }
                else
                {
                    p.Add("QUANTITY_ROWS", Form.GetValueByPath("QUANTITY_ROWS"));
                }
                if (Form.GetValueByPath("TYPE_PACKAGE").ToInt() == 1 && Form.GetValueByPath("QUANTITY").IsNullOrEmpty())
                {
                    msg += " кол-во в пачке,";
                }
                else
                {
                    p.Add("QUANTITY", Form.GetValueByPath("QUANTITY"));
                }
                p.Add("TYPE_PACKAGE", Form.GetValueByPath("TYPE_PACKAGE"));



                if (!msg.IsNullOrEmpty())
                {
                    msg = $"Заполните поля {msg}";
                    msg = msg.Substring(0, msg.Length - 1);
                    msg += ".";
                    var d = new DialogWindow($"{msg}", "ТК решётки", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
                else
                {
                    p.Add("FRAME_ID", Form.GetValueByPath("FRAME_ID"));

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Preproduction");
                    q.Request.SetParam("Object", "PartitionTechnologicalMap");
                    q.Request.SetParam("Action", "GetDimensionOfTransportPackage");
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
                                if (ds.Items.First().CheckGet("PACKAGE_LENGTH")!="" && ds.Items.First().CheckGet("PACKAGE_LENGTH").ToInt() > 0)
                                {
                                    Form.SetValueByPath("PACKAGE_LENGTH", ds.Items.First().CheckGet("PACKAGE_LENGTH"));
                                }
                                if (ds.Items.First().CheckGet("PACKAGE_WIDTH") != "" && ds.Items.First().CheckGet("PACKAGE_WIDTH").ToInt() > 0)
                                {
                                    Form.SetValueByPath("PACKAGE_WIDTH", ds.Items.First().CheckGet("PACKAGE_WIDTH"));
                                }
                                if (ds.Items.First().CheckGet("PACKAGE_HEIGTH") != "" && ds.Items.First().CheckGet("PACKAGE_HEIGTH").ToInt() > 0)
                                {
                                    Form.SetValueByPath("PACKAGE_HEIGTH", ds.Items.First().CheckGet("PACKAGE_HEIGTH"));
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
            else
            {
                var msg = "Выбрана укладка на ребро. Запрещено автоматически рассчитывать габариты ТП.";
                var d = new DialogWindow($"{msg}", "ТК решётки", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Расчёт габаритов транспортного пакета в зависимости от габаритов решётки и выбранной схемы укладки
        /// (для второй решётки для типа продукции Решётки не в сборе)
        /// </summary>
        public void CalculateDimensionOfTransportPackage2()
        {
            // Если не укладка на ребро, то можем рассчитывать
            if ((bool)OnEdge2CheckBox.IsChecked == false)
            {
                // Получение габаритов ТП через функции БД
                var p = new Dictionary<string, string>();
                p.Add("PROFILE", Form.GetValueByPath("PROFILE"));
                p.Add("QUANTITY_ROWS", Form.GetValueByPath("QUANTITY_ROWS2"));
                p.Add("QUANTITY", Form.GetValueByPath("QUANTITY2"));
                p.Add("CARDBOARD", Form.GetValueByPath("CARDBOARD"));
                p.Add("TYPE_PACKAGE", Form.GetValueByPath("TYPE_PACKAGE2"));
                p.Add("TYPE_PRODUCT", Form.GetValueByPath("TYPE_PRODUCT"));
                p.Add("PRODUCTION_SCHEME", TkProductionScheme.SelectedItem.Key.ToString());
                p.Add("LENGTH", Form.GetValueByPath("LENGTH_SECOND"));
                p.Add("HEIGHT", Form.GetValueByPath("HEIGHT_SECOND"));
                p.Add("PALLET", Form.GetValueByPath("PALLET2"));
                p.Add("LAYING_SCHEME", Form.GetValueByPath("LAYING_SCHEME2"));

                if (p.FirstOrDefault(x => x.Value.IsNullOrEmpty()).Key != null)
                {
                    var msg = "Заполните поля Длина решётки, Высота решётки, Вид изделия, Вариант отгрузки, Схема производства, Профиль, Картон, Количество рядов и Количество в пачке.";
                    var d = new DialogWindow($"{msg}", "ТК решётки", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
                else
                {
                    p.Add("FRAME_ID", Form.GetValueByPath("FRAME_ID2"));

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Preproduction");
                    q.Request.SetParam("Object", "PartitionTechnologicalMap");
                    q.Request.SetParam("Action", "GetDimensionOfTransportPackage");
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
                                if (!ds.Items.First().CheckGet("PACKAGE_LENGTH").IsNullOrEmpty() && ds.Items.First().CheckGet("PACKAGE_LENGTH").ToInt() > 0)
                                {
                                    Form.SetValueByPath("PACKAGE_LENGTH2", ds.Items.First().CheckGet("PACKAGE_LENGTH"));
                                }
                                if (!ds.Items.First().CheckGet("PACKAGE_WIDTH").IsNullOrEmpty() && ds.Items.First().CheckGet("PACKAGE_WIDTH").ToInt() > 0)
                                {
                                    Form.SetValueByPath("PACKAGE_WIDTH2", ds.Items.First().CheckGet("PACKAGE_WIDTH"));
                                }
                                if (!ds.Items.First().CheckGet("PACKAGE_HEIGTH").IsNullOrEmpty() && ds.Items.First().CheckGet("PACKAGE_HEIGTH").ToInt() > 0)
                                {
                                    Form.SetValueByPath("PACKAGE_HEIGTH2", ds.Items.First().CheckGet("PACKAGE_HEIGTH"));
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
            else
            {
                var msg = "Выбрана укладка на ребро. Запрещено автоматически рассчитывать габариты ТП.";
                var d = new DialogWindow($"{msg}", "ТК решётки", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }


        /// <summary>
        /// Заполнение поля Пачек в ряду по выбранной Схеме укладки на поддон
        /// </summary>
        public void SetTkQuantityPack()
        {
            var id = TkLayingScheme.SelectedItem.Key;
            var kol = DataSetOfLayingSchemes?.Items?.FirstOrDefault(x => x.CheckGet("ID") == id).CheckGet("KOL");
            TkQuantityPack.Text = kol;
        }

        public void GetLayingSchemeImage()
        {
            if (TkGridTypeProduct != null && Form != null)
            {
                // Если тип продукции -- прокладки
                if (TkGridTypeProduct.SelectedItem.Key.ToInt() == 14
                    || TkGridTypeProduct.SelectedItem.Key.ToInt() == 15
                    || TkGridTypeProduct.SelectedItem.Key.ToInt() == 226)
                {
                    if (string.IsNullOrEmpty(Form.GetValueByPath("LAYING_SCHEME")))
                    {
                        StackingImage.Source = null;
                    }
                    else
                    {
                        var p = new Dictionary<string, string>();
                        p.Add("LAYING_SCHEME", Form.GetValueByPath("LAYING_SCHEME"));

                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Preproduction");
                        q.Request.SetParam("Object", "GasketTechnologicalMap");
                        q.Request.SetParam("Action", "GetLayingSchemeImage");

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
                                    byte[] bytes = Convert.FromBase64String(ds.Items.First().CheckGet("JPG"));
                                    var mem = new MemoryStream(bytes) { Position = 0 };
                                    var image = new BitmapImage();
                                    image.BeginInit();
                                    image.StreamSource = mem;
                                    image.EndInit();
                                    StackingImage.Source = image;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Заполнение поля Пачек в ряду по выбранной Схеме укладки на поддон
        /// (для второй решётки для типа продукции Решётки не в сборе)
        /// </summary>
        public void SetTkQuantityPack2()
        {
            var id = TkLayingScheme2.SelectedItem.Key;
            var kol = DataSetOfLayingSchemes?.Items?.FirstOrDefault(x => x.CheckGet("ID") == id).CheckGet("KOL");
            TkQuantityPack2.Text = kol;
        }

        

        /// <summary>
        /// Проверяет состояние радиобаттонов Тип упаковки, в записимости от их состоятия меняет наполнения списка TkTypePackage: 
        /// Если нажат "С упаковкой":
        /// (ID -- 1,
        /// NAME -- с  упак.);
        /// Если нажат "Без упаковки":
        /// (ID -- 0,
        /// NAME -- россыпью);
        /// </summary>
        public void CheckTkTypePackage()
        {
            var type = Form.GetValueByPath("TYPE_PACKAGE");
            if (type == "1")
            {
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "QUANTITY_PACK"), FormHelperField.FieldFilterRef.Required);
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "QUANTITY_ROWS"), FormHelperField.FieldFilterRef.Required);
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "QUANTITY"), FormHelperField.FieldFilterRef.Required);
                Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "PACKAGE_HEIGTH"), FormHelperField.FieldFilterRef.Required);
            }
            else
            {
                Form.RemoveFilter("QUANTITY_PACK", FormHelperField.FieldFilterRef.Required);
                Form.RemoveFilter("QUANTITY_ROWS", FormHelperField.FieldFilterRef.Required);
                Form.RemoveFilter("QUANTITY", FormHelperField.FieldFilterRef.Required);
                Form.RemoveFilter("PACKAGE_HEIGTH", FormHelperField.FieldFilterRef.Required);
            }
        }

        /// <summary>
        /// Проверяет состояние радиобаттонов Тип упаковки, в записимости от их состоятия меняет наполнения списка TkTypePackage: 
        /// Если нажат "С упаковкой":
        /// (ID -- 1,
        /// NAME -- с  упак.);
        /// Если нажат "Без упаковки":
        /// (ID -- 0,
        /// NAME -- россыпью);
        /// (для второй решётки для типа продукции Решётки не в сборе)
        /// </summary>
        public void CheckTkTypePackage2()
        {
            if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(100, 229))
            {
                var type = Form.GetValueByPath("TYPE_PACKAGE2");
                if (type == "1")
                {
                    Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "PACKAGE_HEIGTH2"), FormHelperField.FieldFilterRef.Required);
                }
                else
                {
                    Form.RemoveFilter("PACKAGE_HEIGTH2", FormHelperField.FieldFilterRef.Required);
                }
            }
            else
            {
                Form.RemoveFilter("PACKAGE_HEIGTH2", FormHelperField.FieldFilterRef.Required);
            }
        }
        /// <summary>
        /// Автоматическая установка свойства Подпресовка поддона для типа продукции Решётки не в сборе в зависимости от высоты первой решётки 
        /// </summary>
        public void SetTkPrepressingByHeight()
        {
            if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(100,229))
            {
                var color = HColor.Yellow;
                var brush = color.ToBrush();

                if (TkGridHeightFirst.Text.ToInt() <= 120)
                {
                    TkPrepressing.IsChecked = false;
                    TkPrepressing.Background = brush;

                    OnEdgeCheckBox.IsChecked = true;
                    OnEdgeCheckBox.Background = brush;
                }
                else
                {
                    TkPrepressing.IsChecked = true;
                    TkPrepressing.Background = brush;

                    OnEdgeCheckBox.IsChecked = false;
                    OnEdgeCheckBox.Background = brush;
                }
            }
            else if (TkGridTypeProduct.SelectedItem.Key.ToInt().ContainsIn(225, 12))
            {
                TkPrepressing.IsChecked = false;
                TkPrepressing.IsEnabled = false;
            }
            else
            {
                TkPrepressing.IsEnabled = true;
            }
        }

        /// <summary>
        /// Автоматическая установка свойства Подпресовка поддона для типа продукции Решётки не в сборе в зависимости от высоты второй решётки 
        /// </summary>
        public void SetTkPrepressingByHeight2()
        {
            if (TkGridTypeProduct.SelectedItem.Key.ToInt() == 100)
            {
                var color = HColor.Yellow;
                var brush = color.ToBrush();

                if (TkGridHeightSecond.Text.ToInt() <= 120)
                {
                    TkPrepressing2.IsChecked = false;
                    TkPrepressing2.Background = brush;

                    OnEdge2CheckBox.IsChecked = true;
                    OnEdge2CheckBox.Background = brush;
                }
                else
                {
                    TkPrepressing2.IsChecked = true;
                    TkPrepressing2.Background = brush;

                    OnEdge2CheckBox.IsChecked = false;
                    OnEdge2CheckBox.Background = brush;
                }
            }
        }

        #endregion

        private void CalculateBilletSProductButton_Click(object sender, RoutedEventArgs e)
        {
            CalculateSProduct();
        }
    }
}
