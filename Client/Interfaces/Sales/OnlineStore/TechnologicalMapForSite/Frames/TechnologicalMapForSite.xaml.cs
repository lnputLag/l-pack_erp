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

namespace Client.Interfaces.Sales
{
    /// <summary>
    /// Интерфейс создания и редактирования техкарты для выгрузки на сайт
    /// </summary>
    public partial class TechnologicalMapForSite : UserControl
    {
        public TechnologicalMapForSite()
        {
            FrameName = "TechnologicalMapForSite";

            TechCardGlobalFolder = Central.GetStorageNetworkPathByCode("techcards");
            if (string.IsNullOrEmpty(TechCardGlobalFolder))
            {
                TechCardGlobalFolder = "\\\\192.168.3.243\\техкарты\\";
            }

            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            Init();
            SetDefaults();
        }

        public TechnologicalMapForSite(string productCode)
        {
            FrameName = "TechnologicalMapForSite";

            TechCardGlobalFolder = Central.GetStorageNetworkPathByCode("techcards");
            if (string.IsNullOrEmpty(TechCardGlobalFolder))
            {
                TechCardGlobalFolder = "\\\\192.168.3.243\\техкарты\\";
            }

            ProductCode = productCode;

            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            Init();
            SetDefaults();
        }

        public TechnologicalMapForSite(string productCode, int technologicalMapForSiteId)
        {
            FrameName = "TechnologicalMapForSite";

            TechCardGlobalFolder = Central.GetStorageNetworkPathByCode("techcards");
            if (string.IsNullOrEmpty(TechCardGlobalFolder))
            {
                TechCardGlobalFolder = "\\\\192.168.3.243\\техкарты\\";
            }

            ProductCode = productCode;
            TechnologicalMapForSiteId = technologicalMapForSiteId;

            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            Init();
            SetDefaults();
        }

        /// <summary>
        /// Ид записи в таблице t.tk_online_store
        /// </summary>
        public int TechnologicalMapForSiteId { get; set; }

        /// <summary>
        /// Удалённая папка для сохранения файлов по тех картам 
        /// (параметр используется для сохранения ПДФ файлов)
        /// </summary>
        public string TechCardGlobalFolder { get; set; }

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
        /// Артикул
        /// </summary>
        public string ProductCode { get; set; }

        /// <summary>
        /// Датасет с данными по существующей техкарте (t.tk)
        /// </summary>
        public ListDataSet TechnologicalMapProductDataSet { get; set; }

        /// <summary>
        /// Датасет с данными по существуюещй техкарте для выгрузки на сайт (t.tk_online_store)
        /// </summary>
        public ListDataSet TechnologicalMapForSiteDataSet { get; set; }

        /// <summary>
        /// Количество успешно отработанных запросов для наполнения выпадающих списков
        /// </summary>
        public int CountCompletedDefaultQuery { get; set; }

        /// <summary>
        /// ассоциативный список для связи фефко, наименования продукции и раздела 1 при автоматическом заполнении этих полей по техкарте
        /// </summary>
        public List<Dictionary<string, string>> AssociationList { get; set; }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void Init()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="PRODUCT_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductNameSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                new FormHelperField()
                {
                    Path="CODE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CodeTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },

                new FormHelperField()
                {
                    Path="PATH_ONE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=PathOneSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },

                new FormHelperField()
                {
                    Path="PATH_TWO",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=PathTwoSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },

                new FormHelperField()
                {
                    Path="IMAGE_PATH",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ImagePathTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },

                new FormHelperField()
                {
                    Path="FEFCO_CODE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=FefcoCodeSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },

                new FormHelperField()
                {
                    Path="COUNT_ON_PALLET",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CountOnPalletTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },

                new FormHelperField()
                {
                    Path="COUNT_IN_PACK",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CountInPackTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },

                new FormHelperField()
                {
                    Path="CARDBOARD_BRAND",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CardboardBrandSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },

                new FormHelperField()
                {
                    Path="CARDBOARD_PROFILE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CardboardProfileSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },

                new FormHelperField()
                {
                    Path="PRODUCT_TARGET",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductTargetSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                new FormHelperField()
                {
                    Path="PRODUCT_LENGTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ProductLengthTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },

                new FormHelperField()
                {
                    Path="PRODUCT_WIDTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ProductWidthTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },

                new FormHelperField()
                {
                    Path="PRODUCT_HEIGTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ProductHeigthTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                new FormHelperField()
                {
                    Path="CARDBOARD_COLOR",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CardboardColorSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },

                new FormHelperField()
                {
                    Path="PRODUCT_VOLUME",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ProductVolumeTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                new FormHelperField()
                {
                    Path="PALLET_LENGTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PalletLengthTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },

                new FormHelperField()
                {
                    Path="PALLET_WIDTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PalletWidthTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },

                new FormHelperField()
                {
                    Path="PALLET_HEIGTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PalletHeigthTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                new FormHelperField()
                {
                    Path="PRODUCT_ASSEMBLY_INSTRUCTIONS",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductAssemblyInstructionsTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                new FormHelperField()
                {
                    Path="PRODUCT_LABEL",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductLabelSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                new FormHelperField()
                {
                    Path="DISCOUNT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=DiscountTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                new FormHelperField()
                {
                    Path="PRODUCT_DESCRIPTION",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductDescriptionTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },

                new FormHelperField()
                {
                    Path="PRODUCT_APPLICATION_SECTION",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductApplicationSectionTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },

                new FormHelperField()
                {
                    Path="PRODUCT_APPLICATION_SUBSECTION",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductApplicationSubSectionsSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },

                new FormHelperField()
                {
                    Path="PRODUCT_COUNT_IN_STOCK",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ProductCountInStockTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                new FormHelperField()
                {
                    Path="PALLET_COUNT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PalletCountSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },

                new FormHelperField()
                {
                    Path="PRODUCT_RETAIL_PERCENT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ProductRetailPercentTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },

                new FormHelperField()
                {
                    Path="PRODUCT_WHOLESALE_PRICE",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=ProductWholesalePriceTextBox,
                    ControlType="TextBox",
                    Format="N2",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.IsNotZero, null },
                        { FormHelperField.FieldFilterRef.DigitCommaOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_WHOLESALE_PRICE_VAT",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=null,
                    ControlType="void",
                    Format="N2",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.IsNotZero, null },
                    },
                },

                new FormHelperField()
                {
                    Path="PRODUCT_RETAIL_PRICE",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=ProductRetailPriceTextBox,
                    ControlType="TextBox",
                    Format="N2",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },

                new FormHelperField()
                {
                    Path="MINIMUM_COUNT_FOR_ORDER",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=MinimumCountForOrderTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },

                new FormHelperField()
                {
                    Path="QUANTITY_STEP",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=QuantityStepTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },

                new FormHelperField()
                {
                    Path="TECHNOLOGICAL_MAP_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TechnologicalMapIdTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },

                new FormHelperField()
                {
                    Path="PRODUCT_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
        }

        /// <summary>
        /// Получаем стандарные данные для заполнения выпадающих списков
        /// </summary>
        public void LoadDataDefault()
        {
            CountCompletedDefaultQuery = 0;

            DisableControls();

            {
                // По умолчанию наценка 10%
                ProductRetailPercentTextBox.Text = "10";
            }

            // Будет заполняться через запрос
            // INFO временное решение
            {
                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                dictionary.Add("1", "1");
                dictionary.Add("2", "3");

                PalletCountSelectBox.SetItems(dictionary);

                // По умолчанию с 3 штук начинается опт.
                PalletCountSelectBox.SetSelectedItemByKey("2");
            }

            // Будет заполняться через запрос
            // INFO временное решение
            {
                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                dictionary.Add("1", "Четырехклапанный гофрокороб (FEFCO 0201)");
                dictionary.Add("2", "Коробка с ушками (FEFCO 0427)");
                dictionary.Add("3", "Упаковка для пиццы (FEFCO 0426)");
                dictionary.Add("4", "Короб четырехклапанный без крышки (FEFCO 0200)");
                dictionary.Add("5", "Самосборный короб (FEFCO 0427)");
                dictionary.Add("6", "Коробка для пиццы (FEFCO 0426)");
                dictionary.Add("7", "Лоток (FEFCO 0452)");
                dictionary.Add("8", "Лоток с боковым вырезом (FEFCO 0452)");
                dictionary.Add("9", "Лоток с двумя двойными стенками (FEFCO 0422)");
                dictionary.Add("10", "Скошенный лоток с двумя двойными стенками (FEFCO 0422)");
                dictionary.Add("11", "Короб с откидной крышкой (FEFCO 0426)");
                dictionary.Add("12", "Обечайка (FEFCO 0501, 0502, 0503)");
                dictionary.Add("13", "Гофрокартон листовой (FEFCO 0110)");

                ProductNameSelectBox.SetItems(dictionary);
            }

            // Будет заполняться через запрос
            // INFO временное решение
            {
                Dictionary<string, string> dictionary = new Dictionary<string, string>();

                dictionary.Add("1", "Четырехклапанный гофрокороб (FEFCO 0201)");
                dictionary.Add("2", "Самосборный короб (FEFCO 0427)"); //dictionary.Add("2", "Коробка с ушками (FEFCO 0427)");
                dictionary.Add("3", "Коробка для пиццы (FEFCO 0426)"); //dictionary.Add("3", "Упаковка для пиццы (FEFCO 0426)");
                dictionary.Add("4", "Короб четырехклапанный без крышки (FEFCO 0200)");
                dictionary.Add("5", "Самосборный короб (FEFCO 0427)");
                dictionary.Add("6", "Коробка для пиццы (FEFCO 0426)");
                dictionary.Add("7", "Лоток (FEFCO 0452)");
                dictionary.Add("8", "Лоток с боковым вырезом (FEFCO 0452)");
                dictionary.Add("9", "Лоток с двумя двойными стенками (FEFCO 0422)");
                dictionary.Add("10", "Скошенный лоток с двумя двойными стенками (FEFCO 0422)");
                dictionary.Add("11", "Короб с откидной крышкой (FEFCO 0426)");
                dictionary.Add("12", "Обечайка (FEFCO 0501, 0502, 0503)");
                dictionary.Add("13", "Гофрокартон листовой (FEFCO 0110)");

                PathOneSelectBox.SetItems(dictionary);
            }

            // Заполняем ассоциативный список для автоматического заполнения полей Наименование товара и Раздел 1 по фефко из техкарты
            {
                {
                    Dictionary<string, string> dictionary = new Dictionary<string, string>();
                    dictionary.Add("FEFCO_CODE", "0200");
                    dictionary.Add("PATH_ONE_VALUE", "Короб четырехклапанный без крышки (FEFCO 0200)");
                    dictionary.Add("PRODUCT_NAME_VALUE", "Короб четырехклапанный без крышки (FEFCO 0200)");
                    AssociationList.Add(dictionary);
                }

                {
                    Dictionary<string, string> dictionary = new Dictionary<string, string>();
                    dictionary.Add("FEFCO_CODE", "0201");
                    dictionary.Add("PATH_ONE_VALUE", "Четырехклапанный гофрокороб (FEFCO 0201)");
                    dictionary.Add("PRODUCT_NAME_VALUE", "Четырехклапанный гофрокороб (FEFCO 0201)");
                    AssociationList.Add(dictionary);
                }

                {
                    Dictionary<string, string> dictionary = new Dictionary<string, string>();
                    dictionary.Add("FEFCO_CODE", "0427");
                    dictionary.Add("PATH_ONE_VALUE", "Самосборный короб (FEFCO 0427)");
                    dictionary.Add("PRODUCT_NAME_VALUE", "Самосборный короб (FEFCO 0427)");
                    AssociationList.Add(dictionary);
                }

                {
                    Dictionary<string, string> dictionary = new Dictionary<string, string>();
                    dictionary.Add("FEFCO_CODE", "0426");
                    dictionary.Add("PATH_ONE_VALUE", "Коробка для пиццы (FEFCO 0426)");
                    dictionary.Add("PRODUCT_NAME_VALUE", "Коробка для пиццы (FEFCO 0426)");
                    AssociationList.Add(dictionary);
                }

                {
                    Dictionary<string, string> dictionary = new Dictionary<string, string>();
                    dictionary.Add("FEFCO_CODE", "0452");
                    dictionary.Add("PATH_ONE_VALUE", "Лоток (FEFCO 0452)");
                    dictionary.Add("PRODUCT_NAME_VALUE", "Лоток (FEFCO 0452)");
                    AssociationList.Add(dictionary);
                }

                {
                    Dictionary<string, string> dictionary = new Dictionary<string, string>();
                    dictionary.Add("FEFCO_CODE", "0422");
                    dictionary.Add("PATH_ONE_VALUE", "Лоток с двумя двойными стенками (FEFCO 0422)");
                    dictionary.Add("PRODUCT_NAME_VALUE", "Лоток с двумя двойными стенками (FEFCO 0422)");
                    AssociationList.Add(dictionary);
                }

                {
                    Dictionary<string, string> dictionary = new Dictionary<string, string>();
                    dictionary.Add("FEFCO_CODE", "0501");
                    dictionary.Add("PATH_ONE_VALUE", "Обечайка (FEFCO 0501, 0502, 0503)");
                    dictionary.Add("PRODUCT_NAME_VALUE", "Обечайка (FEFCO 0501, 0502, 0503)");
                    AssociationList.Add(dictionary);
                }

                {
                    Dictionary<string, string> dictionary = new Dictionary<string, string>();
                    dictionary.Add("FEFCO_CODE", "0502");
                    dictionary.Add("PATH_ONE_VALUE", "Обечайка (FEFCO 0501, 0502, 0503)");
                    dictionary.Add("PRODUCT_NAME_VALUE", "Обечайка (FEFCO 0501, 0502, 0503)");
                    AssociationList.Add(dictionary);
                }

                {
                    Dictionary<string, string> dictionary = new Dictionary<string, string>();
                    dictionary.Add("FEFCO_CODE", "0503");
                    dictionary.Add("PATH_ONE_VALUE", "Обечайка (FEFCO 0501, 0502, 0503)");
                    dictionary.Add("PRODUCT_NAME_VALUE", "Обечайка (FEFCO 0501, 0502, 0503)");
                    AssociationList.Add(dictionary);
                }

                {
                    Dictionary<string, string> dictionary = new Dictionary<string, string>();
                    dictionary.Add("FEFCO_CODE", "0110");
                    dictionary.Add("PATH_ONE_VALUE", "Гофрокартон листовой (FEFCO 0110)");
                    dictionary.Add("PRODUCT_NAME_VALUE", "Гофрокартон листовой (FEFCO 0110)");
                    AssociationList.Add(dictionary);
                }
            }

            // Будет заполняться через запрос
            // INFO временное решение
            {
                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                dictionary.Add("1", "Упаковка для маркетплейса");
                dictionary.Add("2", "Склад и логистика");
                dictionary.Add("3", "E-COMM | BEAUTY | SAMPLE BOX");
                dictionary.Add("4", "Кондитерские изделия");

                ProductTargetSelectBox.SetItems(dictionary);
            }

            // Будет заполняться через запрос
            // INFO временное решение
            {
                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                dictionary.Add("1", "Пятислойный картон");
                dictionary.Add("2", "Трехслойный картон");

                PathTwoSelectBox.SetItems(dictionary);
            }

            // Будет заполняться через запрос
            // INFO временное решение
            {
                //Dictionary<string, string> dictionary = new Dictionary<string, string>();
                //dictionary.Add("1", "Пятислойный картон");
                //dictionary.Add("2", "Трехслойный картон");

                //ProductApplicationSubSectionsSelectBox.SetItems(dictionary);

                ProductApplicationSubSectionsSelectBox.SetItems(PathTwoSelectBox.Items);
            }

            // Будет заполняться через запрос
            // INFO временное решение
            {
                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                dictionary.Add("0200", "0200"); //Короб четырехклапанный без крышки
                dictionary.Add("0201", "0201"); //Четырехклапанный гофрокороб
                dictionary.Add("0427", "0427"); //Самосборный короб
                dictionary.Add("0426", "0426"); //Коробка для пиццы //Короб с откидной крышкой
                dictionary.Add("0452", "0452"); //Лоток //Лоток с боковым вырезом
                dictionary.Add("0422", "0422"); //Лоток с двумя двойными стенками //Скошенный лоток с двумя двойными стенками
                dictionary.Add("0501", "0501"); //Обечайка 
                dictionary.Add("0502", "0502"); //Обечайка 
                dictionary.Add("0503", "0503"); //Обечайка 
                dictionary.Add("0110", "0110"); //Гофрокартон листовой

                FefcoCodeSelectBox.SetItems(dictionary);
            }

            // Будет заполняться через запрос
            // INFO временное решение
            {
                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                dictionary.Add("1", "хит");
                dictionary.Add("2", "new");

                ProductLabelSelectBox.SetItems(dictionary);
            }

            // ImagePath
            // INFO временное решение
            {
                ImagePathTextBox.Text = "http://lpak.indexis.ru/upload/iblock/ffd/ve7cfz12bbs1s72oh5rif0vx2f9rj24c.jpg";
            }

            LoadDataForBrand();
            LoadDataForProfile();
            LoadDataForColor();
        }

        /// <summary>
        /// Проверяем, что отработали запросы для наполнения выпадающих списков
        /// </summary>
        public void CheckCompleteSelectBoxFilling()
        {
            if (CountCompletedDefaultQuery == 3)
            {
                EnableControls();

                if (!string.IsNullOrEmpty(ProductCode))
                {
                    LoadDataByProductCode();

                    if (TechnologicalMapForSiteId > 0)
                    {
                        LoadDataByTechnologicalMapForSiteId();
                    }
                }
            }
        }

        /// <summary>
        /// Получаем данные для наполнения выпадающего списка марок картона
        /// </summary>
        public async void LoadDataForBrand()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "TechnologicalMapForSite");
            q.Request.SetParam("Action", "ListBrand");
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

                    CardboardBrandSelectBox.SetItems(dataSet, "ID", "NAME");

                    CountCompletedDefaultQuery += 1;
                    CheckCompleteSelectBoxFilling();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Получаем данные для наполнения выпадающего списка профилей картона
        /// </summary>
        public async void LoadDataForProfile()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "TechnologicalMapForSite");
            q.Request.SetParam("Action", "ListProfile");
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

                    CardboardProfileSelectBox.SetItems(dataSet, "ID", "NAME");

                    CountCompletedDefaultQuery += 1;
                    CheckCompleteSelectBoxFilling();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Получаем данные для наполнения выпадающего списка цвета картона
        /// </summary>
        public async void LoadDataForColor()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "TechnologicalMapForSite");
            q.Request.SetParam("Action", "ListColor");
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

                    CardboardColorSelectBox.SetItems(dataSet, "ID", "OUTER_NAME");

                    CountCompletedDefaultQuery += 1;
                    CheckCompleteSelectBoxFilling();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Получаем данные по техкарте, для автозаполнения соответствующих полей интерфейса
        /// </summary>
        public void LoadDataByProductCode()
        {
            DisableControls();

            var p = new Dictionary<string, string>();
            p.Add("CODE", ProductCode);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "TechnologicalMapForSite");
            q.Request.SetParam("Action", "GetTechnologicalMap");
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

                    TechnologicalMapProductDataSet = dataSet;

                    FormSetProductItems(TechnologicalMapProductDataSet);
                }
            }
            else
            {
                q.ProcessError();
            }

            EnableControls();
        }

        /// <summary>
        /// Устанавливаем значения в поля формы (данные по обычной тех карте t.tk)
        /// </summary>
        public void FormSetProductItems(ListDataSet dataSet)
        {
            if (dataSet != null && dataSet.Items.Count > 0)
            {
                var firstDictionary = dataSet.Items.First();

                if (firstDictionary.Count > 0)
                {
                    // TechnologicalMapId
                    {
                        TechnologicalMapIdTextBox.Text = firstDictionary.CheckGet("TECHNOLOGICAL_MAP_ID").ToInt().ToString();
                    }

                    // id2
                    {
                        Form.SetValueByPath("PRODUCT_ID", firstDictionary.CheckGet("PRODUCT_ID").ToInt().ToString());
                    }

                    // Code
                    {
                        CodeTextBox.Text = firstDictionary.CheckGet("CODE");
                    }

                    // FEFCO
                    // INFO временное решение
                    // так (key=value), потому что не наполняем селектбокс фефко, если будем наполнять, то сделать как с маркой.
                    {
                        if (!string.IsNullOrEmpty(firstDictionary.CheckGet("FEFCO_CODE")))
                        {
                            var fefcoCode = firstDictionary.CheckGet("FEFCO_CODE");

                            if (FefcoCodeSelectBox.Items != null && FefcoCodeSelectBox.Items.Count > 0)
                            {
                                var item = FefcoCodeSelectBox.Items.FirstOrDefault(x => x.Value == fefcoCode);

                                if (item.Key != null)
                                {
                                    FefcoCodeSelectBox.SelectedItem = item;

                                    // Если удалось получить фефко из техкарты, то автозаполняем поля раздел 1 и наименование товара
                                    {
                                        var associationDictionary = AssociationList.FirstOrDefault(x => x.CheckGet("FEFCO_CODE") == fefcoCode);

                                        if (associationDictionary != null && associationDictionary.Count > 0)
                                        {
                                            string productName = associationDictionary.CheckGet("PRODUCT_NAME_VALUE");
                                            string pathOne = associationDictionary.CheckGet("PATH_ONE_VALUE");

                                            // наименование товара
                                            if (ProductNameSelectBox.Items != null && ProductNameSelectBox.Items.Count > 0)
                                            {
                                                var productItem = ProductNameSelectBox.Items.FirstOrDefault(x => x.Value == productName);

                                                if (productItem.Key != null)
                                                {
                                                    ProductNameSelectBox.SelectedItem = productItem;
                                                }
                                            }

                                            // раздел 1
                                            if (PathOneSelectBox.Items != null && PathOneSelectBox.Items.Count > 0)
                                            {
                                                var pathOneItem = PathOneSelectBox.Items.FirstOrDefault(x => x.Value == pathOne);

                                                if (pathOneItem.Key != null)
                                                {
                                                    PathOneSelectBox.SelectedItem = pathOneItem;
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    FefcoCodeSelectBox.Items.Add(fefcoCode, fefcoCode);
                                    FefcoCodeSelectBox.SetSelectedItemByKey(fefcoCode);
                                }
                            }
                            else
                            {
                                FefcoCodeSelectBox.Items = new Dictionary<string, string>();

                                FefcoCodeSelectBox.Items.Add(fefcoCode, fefcoCode);
                                FefcoCodeSelectBox.SetSelectedItemByKey(fefcoCode);
                            }
                        }
                    }

                    // CountOnPallet
                    {
                        CountOnPalletTextBox.Text = firstDictionary.CheckGet("COUNT_ON_PALLET").ToInt().ToString();
                    }

                    // CountInPack
                    {
                        if (!string.IsNullOrEmpty(firstDictionary.CheckGet("COUNT_IN_PACK")))
                        {
                            CountInPackTextBox.Text = firstDictionary.CheckGet("COUNT_IN_PACK").ToInt().ToString();
                        }
                        else
                        {
                            CountInPackTextBox.Text = firstDictionary.CheckGet("COUNT_ON_PALLET").ToInt().ToString();
                        }

                    }

                    // CardboardBrand
                    {
                        if (CardboardBrandSelectBox.Items != null && CardboardBrandSelectBox.Items.Count > 0)
                        {
                            var item = CardboardBrandSelectBox.Items.FirstOrDefault(x => x.Value == firstDictionary.CheckGet("CARDBOARD_BRAND"));

                            if (item.Key != null)
                            {
                                CardboardBrandSelectBox.SelectedItem = item;
                            }
                            else
                            {
                                CardboardBrandSelectBox.Items.Add(firstDictionary.CheckGet("CARDBOARD_BRAND_ID"), firstDictionary.CheckGet("CARDBOARD_BRAND"));
                                CardboardBrandSelectBox.SetSelectedItemByKey(firstDictionary.CheckGet("CARDBOARD_BRAND_ID"));
                            }
                        }
                        else
                        {
                            CardboardBrandSelectBox.Items = new Dictionary<string, string>();

                            CardboardBrandSelectBox.Items.Add(firstDictionary.CheckGet("CARDBOARD_BRAND_ID"), firstDictionary.CheckGet("CARDBOARD_BRAND"));
                            CardboardBrandSelectBox.SetSelectedItemByKey(firstDictionary.CheckGet("CARDBOARD_BRAND_ID"));
                        }
                    }

                    // CarboardProfile
                    {
                        if (CardboardProfileSelectBox.Items != null && CardboardProfileSelectBox.Items.Count > 0)
                        {
                            var item = CardboardProfileSelectBox.Items.FirstOrDefault(x => x.Value == firstDictionary.CheckGet("CARDBOARD_PROFIL"));

                            if (item.Key != null)
                            {
                                CardboardProfileSelectBox.SelectedItem = item;
                            }
                            else
                            {
                                CardboardProfileSelectBox.Items.Add(firstDictionary.CheckGet("CARDBOARD_PROFIL_ID"), firstDictionary.CheckGet("CARDBOARD_PROFIL"));
                                CardboardProfileSelectBox.SetSelectedItemByKey(firstDictionary.CheckGet("CARDBOARD_PROFIL_ID"));
                            }
                        }
                        else
                        {
                            CardboardProfileSelectBox.Items = new Dictionary<string, string>();

                            CardboardProfileSelectBox.Items.Add(firstDictionary.CheckGet("CARDBOARD_PROFIL_ID"), firstDictionary.CheckGet("CARDBOARD_PROFIL"));
                            CardboardProfileSelectBox.SetSelectedItemByKey(firstDictionary.CheckGet("CARDBOARD_PROFIL_ID"));
                        }
                    }

                    // ProductLength
                    {
                        ProductLengthTextBox.Text = firstDictionary.CheckGet("PRODUCT_LENGTH").ToInt().ToString();
                    }

                    // ProductWidth
                    {
                        ProductWidthTextBox.Text = firstDictionary.CheckGet("PRODUCT_WIDTH").ToInt().ToString();
                    }

                    // ProductHeigth
                    {
                        ProductHeigthTextBox.Text = firstDictionary.CheckGet("PRODUCT_HEIGTH").ToInt().ToString();
                    }

                    // CardboardColor
                    {
                        if (CardboardColorSelectBox.Items != null && CardboardColorSelectBox.Items.Count > 0)
                        {
                            var item = CardboardColorSelectBox.Items.FirstOrDefault(x => x.Value == firstDictionary.CheckGet("CARDBOARD_COLOR"));

                            if (item.Key != null)
                            {
                                CardboardColorSelectBox.SelectedItem = item;
                            }
                            else
                            {
                                CardboardColorSelectBox.Items.Add(firstDictionary.CheckGet("CARDBOARD_COLOR_ID"), firstDictionary.CheckGet("CARDBOARD_COLOR"));
                                CardboardColorSelectBox.SetSelectedItemByKey(firstDictionary.CheckGet("CARDBOARD_COLOR_ID"));
                            }
                        }
                        else
                        {
                            CardboardColorSelectBox.Items = new Dictionary<string, string>();

                            CardboardColorSelectBox.Items.Add(firstDictionary.CheckGet("CARDBOARD_COLOR_ID"), firstDictionary.CheckGet("CARDBOARD_COLOR"));
                            CardboardColorSelectBox.SetSelectedItemByKey(firstDictionary.CheckGet("CARDBOARD_COLOR_ID"));
                        }
                    }

                    // PalletLength
                    {
                        PalletLengthTextBox.Text = firstDictionary.CheckGet("PALLET_LENGTH").ToInt().ToString();
                    }

                    // PalletWidth
                    {
                        PalletWidthTextBox.Text = firstDictionary.CheckGet("PALLET_WIDTH").ToInt().ToString();
                    }

                    // PalletHeigth
                    {
                        PalletHeigthTextBox.Text = firstDictionary.CheckGet("PALLET_HEIGTH").ToInt().ToString();
                    }

                    // PathTwo
                    // INFO временное решение
                    {
                        if (CardboardBrandSelectBox.Items != null && CardboardBrandSelectBox.Items.Count > 0 && CardboardBrandSelectBox.SelectedItem.Key != null)
                        {
                            if (CardboardBrandSelectBox.SelectedItem.Value.Contains("П"))
                            {
                                var item = PathTwoSelectBox.Items.FirstOrDefault(x => x.Value == "Пятислойный картон");
                                if (item.Key != null)
                                {
                                    PathTwoSelectBox.SelectedItem = item;
                                }
                            }
                            else if (CardboardBrandSelectBox.SelectedItem.Value.Contains("Т"))
                            {
                                var item = PathTwoSelectBox.Items.FirstOrDefault(x => x.Value == "Трехслойный картон");
                                if (item.Key != null)
                                {
                                    PathTwoSelectBox.SelectedItem = item;
                                }
                            }
                        }
                    }

                    // MinimumCountForOrder
                    {
                        MinimumCountForOrderTextBox.Text = CountOnPalletTextBox.Text;
                    }

                    // QuantityStep
                    {
                        QuantityStepTextBox.Text = CountOnPalletTextBox.Text;
                    }

                    // ProductApplicationSubSections
                    // INFO временное решение
                    {
                        if (CardboardBrandSelectBox.Items != null && CardboardBrandSelectBox.Items.Count > 0 && CardboardBrandSelectBox.SelectedItem.Key != null)
                        {
                            if (CardboardBrandSelectBox.SelectedItem.Value.Contains("П"))
                            {
                                var item = ProductApplicationSubSectionsSelectBox.Items.FirstOrDefault(x => x.Value == "Пятислойный картон");
                                if (item.Key != null)
                                {
                                    ProductApplicationSubSectionsSelectBox.SelectedItem = item;
                                }
                            }
                            else if (CardboardBrandSelectBox.SelectedItem.Value.Contains("Т"))
                            {
                                var item = ProductApplicationSubSectionsSelectBox.Items.FirstOrDefault(x => x.Value == "Трехслойный картон");
                                if (item.Key != null)
                                {
                                    ProductApplicationSubSectionsSelectBox.SelectedItem = item;
                                }
                            }
                        }
                    }

                    // ProductWholesalePriceTextBox
                    {
                        ProductWholesalePriceTextBox.Text = firstDictionary.CheckGet("PRODUCT_WHOLESALE_PRICE").ToDouble().ToString();
                    }
                }
            }
        }

        /// <summary>
        /// Получаем существующие данные по созданной техкарте для сайта
        /// </summary>
        public void LoadDataByTechnologicalMapForSiteId()
        {
            DisableControls();

            var p = new Dictionary<string, string>();
            p.Add("ID", TechnologicalMapForSiteId.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "TechnologicalMapForSite");
            q.Request.SetParam("Action", "Get");
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

                    TechnologicalMapForSiteDataSet = dataSet;

                    FormSetTechnologicalMapForSiteItems(TechnologicalMapForSiteDataSet);
                }
            }
            else
            {
                q.ProcessError();
            }

            EnableControls();
        }

        /// <summary>
        /// Устанавливаем значения в поля формы (по техкарте для сайта t.tk_online_store)
        /// </summary>
        public void FormSetTechnologicalMapForSiteItems(ListDataSet dataSet)
        {
            if (dataSet != null && dataSet.Items.Count > 0)
            {
                var firstDictionary = dataSet.Items.First();

                if (firstDictionary.Count > 0) 
                {
                    // ProductName
                    {
                        if (ProductNameSelectBox.Items != null && ProductNameSelectBox.Items.Count > 0)
                        {
                            var item = ProductNameSelectBox.Items.FirstOrDefault(x => x.Value == firstDictionary.CheckGet("PRODUCT_NAME"));

                            if (item.Key != null)
                            {
                                ProductNameSelectBox.SelectedItem = item;
                            }
                            else
                            {
                                ProductNameSelectBox.Items.Add(firstDictionary.CheckGet("PRODUCT_NAME"), firstDictionary.CheckGet("PRODUCT_NAME"));
                                ProductNameSelectBox.SetSelectedItemByKey(firstDictionary.CheckGet("PRODUCT_NAME"));
                            }
                        }
                        else
                        {
                            ProductNameSelectBox.Items = new Dictionary<string, string>();

                            ProductNameSelectBox.Items.Add(firstDictionary.CheckGet("PRODUCT_NAME"), firstDictionary.CheckGet("PRODUCT_NAME"));
                            ProductNameSelectBox.SetSelectedItemByKey(firstDictionary.CheckGet("PRODUCT_NAME"));
                        }
                    }

                    // PathOne
                    {
                        if (PathOneSelectBox.Items != null && PathOneSelectBox.Items.Count > 0)
                        {
                            var item = PathOneSelectBox.Items.FirstOrDefault(x => x.Value == firstDictionary.CheckGet("PATH_ONE"));

                            if (item.Key != null)
                            {
                                PathOneSelectBox.SelectedItem = item;
                            }
                            else
                            {
                                PathOneSelectBox.Items.Add(firstDictionary.CheckGet("PATH_ONE"), firstDictionary.CheckGet("PATH_ONE"));
                                PathOneSelectBox.SetSelectedItemByKey(firstDictionary.CheckGet("PATH_ONE"));
                            }
                        }
                        else
                        {
                            PathOneSelectBox.Items = new Dictionary<string, string>();

                            PathOneSelectBox.Items.Add(firstDictionary.CheckGet("PATH_ONE"), firstDictionary.CheckGet("PATH_ONE"));
                            PathOneSelectBox.SetSelectedItemByKey(firstDictionary.CheckGet("PATH_ONE"));
                        }
                    }

                    //PathTwo
                    {
                        if (PathTwoSelectBox.Items != null && PathTwoSelectBox.Items.Count > 0)
                        {
                            var item = PathTwoSelectBox.Items.FirstOrDefault(x => x.Value == firstDictionary.CheckGet("PATH_TWO"));

                            if (item.Key != null)
                            {
                                PathTwoSelectBox.SelectedItem = item;
                            }
                            else
                            {
                                PathTwoSelectBox.Items.Add(firstDictionary.CheckGet("PATH_TWO"), firstDictionary.CheckGet("PATH_TWO"));
                                PathTwoSelectBox.SetSelectedItemByKey(firstDictionary.CheckGet("PATH_TWO"));
                            }
                        }
                        else
                        {
                            PathTwoSelectBox.Items = new Dictionary<string, string>();

                            PathTwoSelectBox.Items.Add(firstDictionary.CheckGet("PATH_TWO"), firstDictionary.CheckGet("PATH_TWO"));
                            PathTwoSelectBox.SetSelectedItemByKey(firstDictionary.CheckGet("PATH_TWO"));
                        }
                    }

                    // ImagePath
                    {
                        ImagePathTextBox.Text = firstDictionary.CheckGet("IMAGE_PATH");
                    }

                    // FEFCO
                    {
                        if (FefcoCodeSelectBox.Items != null && FefcoCodeSelectBox.Items.Count > 0)
                        {
                            var item = FefcoCodeSelectBox.Items.FirstOrDefault(x => x.Value == firstDictionary.CheckGet("FEFCO_CODE"));

                            if (item.Key != null)
                            {
                                FefcoCodeSelectBox.SelectedItem = item;
                            }
                            else
                            {
                                FefcoCodeSelectBox.Items.Add(firstDictionary.CheckGet("FEFCO_CODE"), firstDictionary.CheckGet("FEFCO_CODE"));
                                FefcoCodeSelectBox.SetSelectedItemByKey(firstDictionary.CheckGet("FEFCO_CODE"));
                            }
                        }
                        else
                        {
                            FefcoCodeSelectBox.Items = new Dictionary<string, string>();

                            FefcoCodeSelectBox.Items.Add(firstDictionary.CheckGet("FEFCO_CODE"), firstDictionary.CheckGet("FEFCO_CODE"));
                            FefcoCodeSelectBox.SetSelectedItemByKey(firstDictionary.CheckGet("FEFCO_CODE"));
                        }
                    }

                    // ProductTarget
                    {
                        if (ProductTargetSelectBox.Items != null && ProductTargetSelectBox.Items.Count > 0)
                        {
                            var item = ProductTargetSelectBox.Items.FirstOrDefault(x => x.Value == firstDictionary.CheckGet("PRODUCT_TARGET"));

                            if (item.Key != null)
                            {
                                ProductTargetSelectBox.SelectedItem = item;
                            }
                            else
                            {
                                ProductTargetSelectBox.Items.Add(firstDictionary.CheckGet("PRODUCT_TARGET"), firstDictionary.CheckGet("PRODUCT_TARGET"));
                                ProductTargetSelectBox.SetSelectedItemByKey(firstDictionary.CheckGet("PRODUCT_TARGET"));
                            }
                        }
                        else
                        {
                            ProductTargetSelectBox.Items = new Dictionary<string, string>();

                            ProductTargetSelectBox.Items.Add(firstDictionary.CheckGet("PRODUCT_TARGET"), firstDictionary.CheckGet("PRODUCT_TARGET"));
                            ProductTargetSelectBox.SetSelectedItemByKey(firstDictionary.CheckGet("PRODUCT_TARGET"));
                        }
                    }

                    // ProductVolume
                    {
                        if (firstDictionary.CheckGet("PRODUCT_VOLUME").ToInt() > 0)
                        {
                            ProductVolumeTextBox.Text = firstDictionary.CheckGet("PRODUCT_VOLUME").ToInt().ToString();
                        }
                        else
                        {
                            ProductVolumeTextBox.Clear();
                        }
                    }

                    // ProductAssemblyInstruction
                    {
                        if (!string.IsNullOrEmpty(firstDictionary.CheckGet("PRODUCT_ASSEMBLY_INSTRUCTIONS")))
                        {
                            ProductAssemblyInstructionsTextBox.Text = firstDictionary.CheckGet("PRODUCT_ASSEMBLY_INSTRUCTIONS");
                        }
                        else
                        {
                            ProductAssemblyInstructionsTextBox.Clear();
                        }
                    }

                    // ProductLabel
                    {
                        if (ProductLabelSelectBox.Items != null && ProductLabelSelectBox.Items.Count > 0)
                        {
                            var item = ProductLabelSelectBox.Items.FirstOrDefault(x => x.Value == firstDictionary.CheckGet("PRODUCT_LABEL"));

                            if (item.Key != null)
                            {
                                ProductLabelSelectBox.SelectedItem = item;
                            }
                            else
                            {
                                ProductLabelSelectBox.Items.Add(firstDictionary.CheckGet("PRODUCT_LABEL"), firstDictionary.CheckGet("PRODUCT_LABEL"));
                                ProductLabelSelectBox.SetSelectedItemByKey(firstDictionary.CheckGet("PRODUCT_LABEL"));
                            }
                        }
                        else
                        {
                            ProductLabelSelectBox.Items = new Dictionary<string, string>();

                            ProductLabelSelectBox.Items.Add(firstDictionary.CheckGet("PRODUCT_LABEL"), firstDictionary.CheckGet("PRODUCT_LABEL"));
                            ProductLabelSelectBox.SetSelectedItemByKey(firstDictionary.CheckGet("PRODUCT_LABEL"));
                        }
                    }

                    // Discount
                    {
                        if (!string.IsNullOrEmpty(firstDictionary.CheckGet("DISCOUNT")))
                        {
                            DiscountTextBox.Text = firstDictionary.CheckGet("DISCOUNT").ToInt().ToString();
                        }
                    }

                    // ProductDescription
                    {
                        ProductDescriptionTextBox.Text = firstDictionary.CheckGet("PRODUCT_DESCRIPTION");
                    }

                    // ProductApplicationSection
                    {
                        ProductApplicationSectionTextBox.Text = firstDictionary.CheckGet("PRODUCT_APPLICATION_SECTION");
                    }

                    // ProductApplicationSubsection
                    {
                        if (ProductApplicationSubSectionsSelectBox.Items != null && ProductApplicationSubSectionsSelectBox.Items.Count > 0)
                        {
                            var item = ProductApplicationSubSectionsSelectBox.Items.FirstOrDefault(x => x.Value == firstDictionary.CheckGet("PRODUCT_APPLICATION_SUBSECTION"));

                            if (item.Key != null)
                            {
                                ProductApplicationSubSectionsSelectBox.SelectedItem = item;
                            }
                            else
                            {
                                ProductApplicationSubSectionsSelectBox.Items.Add(firstDictionary.CheckGet("PRODUCT_APPLICATION_SUBSECTION"), firstDictionary.CheckGet("PRODUCT_APPLICATION_SUBSECTION"));
                                ProductApplicationSubSectionsSelectBox.SetSelectedItemByKey(firstDictionary.CheckGet("PRODUCT_APPLICATION_SUBSECTION"));
                            }
                        }
                        else
                        {
                            ProductApplicationSubSectionsSelectBox.Items = new Dictionary<string, string>();

                            ProductApplicationSubSectionsSelectBox.Items.Add(firstDictionary.CheckGet("PRODUCT_APPLICATION_SUBSECTION"), firstDictionary.CheckGet("PRODUCT_APPLICATION_SUBSECTION"));
                            ProductApplicationSubSectionsSelectBox.SetSelectedItemByKey(firstDictionary.CheckGet("PRODUCT_APPLICATION_SUBSECTION"));
                        }
                    }

                    // PalletCount
                    {
                        if (PalletCountSelectBox.Items != null && PalletCountSelectBox.Items.Count > 0)
                        {
                            var item = PalletCountSelectBox.Items.FirstOrDefault(x => x.Value == firstDictionary.CheckGet("PALLET_COUNT"));

                            if (item.Key != null)
                            {
                                PalletCountSelectBox.SelectedItem = item;
                            }
                            else
                            {
                                PalletCountSelectBox.Items.Add(firstDictionary.CheckGet("PALLET_COUNT"), firstDictionary.CheckGet("PALLET_COUNT"));
                                PalletCountSelectBox.SetSelectedItemByKey(firstDictionary.CheckGet("PALLET_COUNT"));
                            }
                        }
                        else
                        {
                            PalletCountSelectBox.Items = new Dictionary<string, string>();

                            PalletCountSelectBox.Items.Add(firstDictionary.CheckGet("PALLET_COUNT"), firstDictionary.CheckGet("PALLET_COUNT"));
                            PalletCountSelectBox.SetSelectedItemByKey(firstDictionary.CheckGet("PALLET_COUNT"));
                        }
                    }

                    // ProductWholesalePrice
                    {
                        ProductWholesalePriceTextBox.Text = firstDictionary.CheckGet("PRODUCT_WHOLESALE_PRICE").ToDouble().ToString();
                    }

                    // MinimumCountForOrder
                    {
                        MinimumCountForOrderTextBox.Text = firstDictionary.CheckGet("MINIMUM_COUNT_FOR_ORDER").ToInt().ToString();
                    }

                    // QuantityStep
                    {
                        QuantityStepTextBox.Text = firstDictionary.CheckGet("QUANTITY_STEP").ToInt().ToString();
                    }

                    // ProductRetailPercent
                    {
                        ProductRetailPercentTextBox.Text = firstDictionary.CheckGet("PRODUCT_RETAIL_PERCENT").ToInt().ToString();
                    }
                }
            }
        }

        /// <summary>
        /// Проверяет и подготавливает данные перед сохранением
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> PrepareSave()
        {
            Dictionary<string, string> formValues = new Dictionary<string, string>();
             
            if (Form != null)
            {
                if (Form.Validate())
                {
                    string productNameValue = "";
                    var selectBoxContentChildrensForProductNameValue = ((System.Windows.Controls.Grid)ProductNameSelectBox.Content).Children;
                    foreach (var contentChildren in selectBoxContentChildrensForProductNameValue)
                    {
                        var type = contentChildren.GetType();
                        if (type.Name == "TextBox")
                        {
                            productNameValue = ((System.Windows.Controls.TextBox)contentChildren).Text;
                        }
                    }

                    if (!string.IsNullOrEmpty(productNameValue))
                    {
                        string productTargetValue = "";
                        var selectBoxContentChildrensForProductTargetValue = ((System.Windows.Controls.Grid)ProductTargetSelectBox.Content).Children;
                        foreach (var contentChildren in selectBoxContentChildrensForProductTargetValue)
                        {
                            var type = contentChildren.GetType();
                            if (type.Name == "TextBox")
                            {
                                productTargetValue = ((System.Windows.Controls.TextBox)contentChildren).Text;
                            }
                        }

                        if (!string.IsNullOrEmpty(productTargetValue))
                        {
                            formValues = Form.GetValues();
                            formValues.CheckAdd("PRODUCT_TARGET_VALUE", productTargetValue);
                            formValues.CheckAdd("PRODUCT_NAME_VALUE", productNameValue);
                            formValues.CheckAdd("PATH_ONE_VALUE", PathOneSelectBox.SelectedItem.Value);
                            formValues.CheckAdd("PATH_TWO_VALUE", PathTwoSelectBox.SelectedItem.Value);
                            formValues.CheckAdd("FEFCO_CODE_VALUE", FefcoCodeSelectBox.SelectedItem.Value);
                            formValues.CheckAdd("CARDBOARD_BRAND_VALUE", CardboardBrandSelectBox.SelectedItem.Value);
                            formValues.CheckAdd("CARDBOARD_PROFILE_VALUE", CardboardProfileSelectBox.SelectedItem.Value);
                            formValues.CheckAdd("CARDBOARD_COLOR_VALUE", CardboardColorSelectBox.SelectedItem.Value);
                            formValues.CheckAdd("PRODUCT_LABEL_VALUE", ProductLabelSelectBox.SelectedItem.Value);
                            formValues.CheckAdd("PRODUCT_APPLICATION_SUBSECTION_VALUE", ProductApplicationSubSectionsSelectBox.SelectedItem.Value);
                            formValues.CheckAdd("PALLET_COUNT_VALUE", PalletCountSelectBox.SelectedItem.Value);

                            formValues.CheckAdd("ID", TechnologicalMapForSiteId.ToString());

                            double wholesalePrice = ProductWholesalePriceTextBox.Text.ToDouble();
                            double wholesalePriceVat = Math.Round(wholesalePrice * 0.2 + wholesalePrice, 2);
                            formValues.CheckAdd("PRODUCT_WHOLESALE_PRICE_VAT", wholesalePriceVat.ToString());
                        }
                        else
                        {
                            var msg = "Поле НАЗНАЧЕНИЕ не может быть пустым.";
                            var d = new DialogWindow($"{msg}", "Техкарты для выгрузки на сайт", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                    else
                    {
                        var msg = "Поле НАИМЕНОВАНИЕ ТОВАРА не может быть пустым.";
                        var d = new DialogWindow($"{msg}", "Техкарты для выгрузки на сайт", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
            }

            return formValues;
        }

        /// <summary>
        /// Сохраняем данные по позиции
        /// </summary>
        /// <param name="formValues"></param>
        public void Save(Dictionary<string, string> formValues)
        {
            if (formValues != null && formValues.Count > 0)
            {
                // Если открыта существующая позиция, то обновляем данные
                if (formValues.CheckGet("ID").ToInt() > 0)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sales");
                    q.Request.SetParam("Object", "TechnologicalMapForSite");
                    q.Request.SetParam("Action", "Update");
                    q.Request.SetParams(formValues);

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
                                int technologicalMapForSiteId = dataSet.Items.First().CheckGet("TECHNOLOGICAL_MAP_ONLINE_STORE_ID").ToInt();

                                if (technologicalMapForSiteId > 0)
                                {
                                    var msg = "Успешное обновление карточки товара";
                                    var d = new DialogWindow($"{msg}", "Техкарты для выгрузки на сайт", "", DialogWindowButtons.OK);
                                    d.ShowDialog();

                                    // Отправляем сообщение обновиться гриду техкарт для выгрузки на сайт
                                    {
                                        Messenger.Default.Send(new ItemMessage()
                                        {
                                            ReceiverGroup = "Sales",
                                            ReceiverName = "TechnologicalMapForSiteList",
                                            SenderName = "TechnologicalMapForSite",
                                            Action = "Refresh",
                                            Message = "",
                                        }
                                        );
                                    }

                                    Close();
                                }
                            }
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
                // Если открыта новая, то сохраняем новые данные
                else
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sales");
                    q.Request.SetParam("Object", "TechnologicalMapForSite");
                    q.Request.SetParam("Action", "Save");
                    q.Request.SetParams(formValues);

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
                                int technologicalMapForSiteId = dataSet.Items.First().CheckGet("TECHNOLOGICAL_MAP_ONLINE_STORE_ID").ToInt();

                                if (technologicalMapForSiteId > 0)
                                {
                                    var msg = "Успешное создание карточки товара";
                                    var d = new DialogWindow($"{msg}", "Техкарты для выгрузки на сайт", "", DialogWindowButtons.OK);
                                    d.ShowDialog();

                                    // Отправляем сообщение обновиться гриду техкарт для выгрузки на сайт
                                    {
                                        Messenger.Default.Send(new ItemMessage()
                                        {
                                            ReceiverGroup = "Sales",
                                            ReceiverName = "TechnologicalMapForSiteList",
                                            SenderName = "TechnologicalMapForSite",
                                            Action = "Refresh",
                                            Message = "",
                                        }
                                        );
                                    }

                                    Close();
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
        }

        public void AddApplicationSection()
        {
            var window = new TechnologicalMapForSiteListApplicationSection("Раздел по применению", ProductTargetSelectBox.Items);
            window.Show();

            if (window.OkFlag)
            {
                Dictionary<string, string> dictionary = window.SelectedItem;
                ProductApplicationSectionTextBox.Text += $"{dictionary.First().Value}; ";
            }
        }

        /// <summary>
        /// Деактивация контроллов
        /// </summary>
        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
            MainGrid.IsEnabled = false;
        }

        /// <summary>
        /// Активация контроллов
        /// </summary>
        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
            MainGrid.IsEnabled = true;
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            TechnologicalMapProductDataSet = new ListDataSet();
            TechnologicalMapForSiteDataSet = new ListDataSet();
            AssociationList = new List<Dictionary<string, string>>();

            Form.SetDefaults();

            LoadDataDefault();
        }

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

            var dt = DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss");

            FrameName = $"{FrameName}_new_{dt}";

            Central.WM.Show(FrameName, "Новая ТК", true, "add", this);
        }

        /// <summary>
        /// Октрыть техкарту (Excel файл)
        /// </summary>
        public void OpenTachnologicalMap()
        {
            DisableControls();

            if (TechnologicalMapProductDataSet != null && TechnologicalMapProductDataSet.Items.Count > 0)
            {
                string fileName = TechnologicalMapProductDataSet.Items.First().CheckGet("PATH_NAME");
                string filePathConfirm = TechnologicalMapProductDataSet.Items.First().CheckGet("PATH_CONFIRM");
                string filePathNew = TechnologicalMapProductDataSet.Items.First().CheckGet("PATH_NEW");

                string fullfilePathConfirm = filePathConfirm + fileName;
                string fullfilePathNew = filePathNew + fileName;

                if (System.IO.File.Exists(fullfilePathConfirm))
                {
                    try
                    {
                        Central.OpenFile(fullfilePathConfirm);
                    }
                    catch (Exception)
                    {
                        var msg = $"Ошибка открытия Excel файла техкарты по пути {fullfilePathConfirm}.";
                        var d = new DialogWindow($"{msg}", "Техкарты для выгрузки на сайт", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else if (System.IO.File.Exists(fullfilePathNew))
                {
                    try
                    {
                        Central.OpenFile(fullfilePathNew);
                    }
                    catch (Exception)
                    {
                        var msg = $"Ошибка открытия Excel файла техкарты по пути {fullfilePathNew}.";
                        var d = new DialogWindow($"{msg}", "Техкарты для выгрузки на сайт", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    var msg = $"Excel файл техкарты не найден по пути {fullfilePathConfirm} и {fullfilePathNew}.";
                    var d = new DialogWindow($"{msg}", "Техкарты для выгрузки на сайт", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }

            EnableControls();
        }

        /// <summary>
        /// Расчёт розничной цены
        /// </summary>
        public void CalculateRetailPrice()
        {
            double wholesalePrice = ProductWholesalePriceTextBox.Text.ToDouble();
            double retailPercent = ProductRetailPercentTextBox.Text.ToDouble();

            double retailPrice = 0;

            if (wholesalePrice > 0)
            {
                retailPrice = Math.Round(((wholesalePrice / 100) * retailPercent) + wholesalePrice, 2);
            }

            ProductRetailPriceTextBox.Text = retailPrice.ToString();
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            Central.WM.Close(FrameName);

            //вся работа по утилизации ресурсов происходит в Destroy
            //он будет вызван при закрытии фрейма
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
                SenderName = "TechnologicalMapForSite",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// отображение справочной статьи
        /// (относительный путь)
        /// </summary>
        public void ShowHelp()
        {
            //FIXME: Нужно сделать документацию
            Central.ShowHelp("/doc/l-pack-erp-new/application/online_shop/online_shop_tk");
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {

        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var formValues = PrepareSave();
            if (formValues != null && formValues.Count > 0)
            {
                Save(formValues);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void AddSectionButton_Click(object sender, RoutedEventArgs e)
        {
            AddApplicationSection();
        }

        private void OpenTachnologicalMapButton_Click(object sender, RoutedEventArgs e)
        {
            OpenTachnologicalMap();
        }

        private void ProductWholesalePriceTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateRetailPrice();
        }

        private void ProductRetailPercentTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateRetailPrice();
        }
    }
}
