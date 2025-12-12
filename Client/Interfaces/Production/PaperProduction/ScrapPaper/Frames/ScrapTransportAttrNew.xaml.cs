using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
using Client.Interfaces.Production.Corrugator;
using DevExpress.DocumentServices.ServiceModel.DataContracts;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Editors.Internal;
using DevExpress.XtraPrinting.Native;
using Microsoft.Office.Interop.Excel;
using Microsoft.Win32;
using NCalc;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using Org.BouncyCastle.Crypto;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Client.Common.LPackClientRequest;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Client.Interfaces.Production.PaperProduction
{
    /// <summary>
    /// Описание качества кип с макулатурой
    /// </summary>
    /// <author>greshnyh_ni</author>
    /// <changed>2025-10-10</changed>
    public partial class ScrapTransportAttrNew : ControlBase
    {

        private FormHelper Form { get; set; }

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }
        /// <summary>
        /// Имя вкладки, окуда вызвана форма редактирования
        /// </summary>
        public string ReceiverName;

        /// <summary>
        /// 0 - обычное описание машины, 1 -описание возвратных кип
        /// </summary>
        public int ReturnFlag { get; set; }

        public Dictionary<string, string> Values { get; set; }

        /// <summary>
        /// категория макулатуры
        /// </summary>        
        private int IdCategory { get; set; }

        /// <summary>
        /// ИД машины
        /// </summary>
        private int IdScrap { get; set; }
        
        /// <summary>
        /// текущий вид макулатуры MC-5Б, MC-6Б, MC-8В, MC-11В
        /// </summary>
        private int CurrentVidBale { get; set; }

        /// <summary>
        ///  категория макулатуры строкой
        /// </summary>
        private string StrCategoryBale { get; set; }

        /// <summary>
        /// проверка введенных значений волокна% для каждого качества
        /// </summary>
        private bool CheckFiberPctAvgFlag { get; set; }

        private string[] Contantimation5 = {
                        "нет",
                        "примесь МС-5Б/3(\"полигонная\")",
                        "примесь МС-6Б(тарный картон с цветной печатью)",
                        "примесь МС-7Б(книги, журналы и др)",
                        "примесь МС-8В(газета)",
                        "примесь МС-9В(тубусы, уголки)",
                        "примесь МС-10В(яичные лотки)",
                        "примесь МС-11В(ламинированный картон)",
                        "целлофан/стрейч пленка",
                        "примесь парафинированного картона",
                        "примесь \"куриной/рыбной коробки\" с резким запахом",
                        "короба от химикатов с резким запахом",
                        "очаги и запах плесени",
                        "бытовой мусор",
                        "крепированная\" бумага",
                        "\"табачка\"",
                        "бумажные мешки с полиэтиленовым вкладышем",
                        "бумажные влагопрочные мешки"};

        private string[] Contantimation6 = {
                        "нет",
                        "примесь МС-7Б(книги, журналы и др)",
                        "примесь МС-9В(тубусы, уголки)",
                        "примесь МС-10В(яичные лотки)",
                        "примесь МС-11В",
                        "очаги и запах плесени",
                        "бумажные мешки не влагопрочные",
                        "бумажные влагопрочные мешки/с полиэтиленовым вкладышем",};

        string[] Contantimation8 = {
                        "нет",
                        "бытовой мусор",
                        "примесь МС-7Б(книги, журналы и др)",
                        "примесь МС-9В(тубусы, уголки)",
                        "примесь МС-10В(яичные лотки)",
                        "примесь МС-11В",
                        "очаги и запах плесени",
                        "бумажные мешки не влагопрочные",
                        "бумажные влагопрочные мешки/с полиэтиленовым вкладышем",};

        private string[] Contantimation11 = {
                        "нет",
                        "бытовой мусор",
                        "очаги и запах плесени",
                        "бумажные мешки не влагопрочные",
                        "бумажные влагопрочные мешки / с полиэтиленовым вкладышем",
                        "заготовки для упаковок молока и соков с низким содержанием волокна",
                        "полимерный материал без бумажной основы",
                        "длинный полосы, крупные включения",
                        "\"крепированная\" бумага",
                        "скрутки полимерной ленты / шпагата",
                        "скрутки проволоки",};

        /// <summary>
        /// 
        /// </summary>
        /// <param name="record"></param>
        public ScrapTransportAttrNew(Dictionary<string, string> record = null, int return_flag = 0)
        {
            InitializeComponent();

            if (record != null)
            {
                Values = record;
                IdCategory = Values.CheckGet("ID_CATEGORY").ToInt();
                IdScrap = Values.CheckGet("ID_SCRAP").ToInt();
                ReturnFlag = return_flag;
            }

            ControlSection = "scrap_paper";
            DocumentationUrl = "/doc/l-pack-erp/production/molded_container/machine_control";

            FrameName = ControlName;

            InitForm();
            SetDefaults();

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnKeyPressed = (KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    switch (e.Key)
                    {
                        case Key.F1:
                            Commander.ProcessCommand("help");
                            e.Handled = true;
                            break;
                    }
                }
            };

            OnLoad = () =>
            {
            };

            OnUnload = () =>
            {
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = "Production",
                    ReceiverName = ReceiverName,
                    SenderName = ControlName,
                    Action = "RefreshScrapTransportWeightGrid",
                });
            };

            OnFocusGot = () =>
            {
            };

            OnFocusLost = () =>
            {
            };

            OnNavigate = () =>
            {
            };

            {
                Commander.SetCurrentGroup("main");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "help",
                        Enabled = true,
                        Title = "Справка",
                        Description = "Показать справочную информацию",
                        ButtonUse = true,
                        ButtonName = "HelpButton",
                        HotKey = "F1",
                        Action = () =>
                        {
                            Central.ShowHelp(DocumentationUrl);
                        },
                    });
                }

                Commander.SetCurrentGroup("custom");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "save",
                        Enabled = true,
                        Title = "Сохранить",
                        Description = "Сохранить и закрыть",
                        ButtonUse = true,
                        ButtonName = "SaveButton",
                        //  HotKey = "Enter",
                        MenuUse = false,
                        Action = () =>
                        {
                            Save();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "close",
                        Enabled = true,
                        Title = "Отмена",
                        Description = "Отмена",
                        ButtonUse = true,
                        ButtonName = "CancelButton",
                        HotKey = "Escape",
                        MenuUse = false,
                        Action = () =>
                        {
                            Close();
                        },
                    });

                    Commander.Add(new CommandItem()
                    {
                        Name = "avg_humidity_1",
                        Enabled = true,
                        Title = "Расчет средней влажности",
                        Description = "Расчет средней влажности",
                        ButtonUse = true,
                        ButtonName = "AvgHumidity1Button",
                        MenuUse = false,
                        Action = () =>
                        {
                            ButtonAvgHumidity1Click();
                        },
                        CheckEnabled = () =>
                        {
                            var result = true;
                            //if (!EditH1_1.Text.IsNullOrEmpty()
                            //|| !EditH1_2.Text.IsNullOrEmpty()
                            //|| !EditH1_3.Text.IsNullOrEmpty()
                            //|| !EditH1_4.Text.IsNullOrEmpty()
                            //|| !EditH1_5.Text.IsNullOrEmpty()
                            //|| !EditH1_6.Text.IsNullOrEmpty()
                            //)
                            //{
                            //    result = true;
                            //}
                            return result;
                        },
                    });

                    Commander.Add(new CommandItem()
                    {
                        Name = "avg_humidity_2",
                        Enabled = true,
                        Title = "Расчет средней влажности",
                        Description = "Расчет средней влажности",
                        ButtonUse = true,
                        ButtonName = "AvgHumidity2Button",
                        MenuUse = false,
                        Action = () =>
                        {
                            ButtonAvgHumidity2Click();
                        },
                        CheckEnabled = () =>
                        {
                            var result = true;

                            return result;
                        },
                    });

                    Commander.Add(new CommandItem()
                    {
                        Name = "avg_humidity_3",
                        Enabled = true,
                        Title = "Расчет средней влажности",
                        Description = "Расчет средней влажности",
                        ButtonUse = true,
                        ButtonName = "AvgHumidity3Button",
                        MenuUse = false,
                        Action = () =>
                        {
                            ButtonAvgHumidity3Click();
                        },
                        CheckEnabled = () =>
                        {
                            var result = true;

                            return result;
                        },
                    });

                }

                Commander.Init(this);
            }

            EditPart1.Focus();
        }

        /// <summary>
        /// инициализация компонентов на форме
        /// </summary>
        private void InitForm()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                 // Фактическая влажность партии % (рассчитывается динамически)
                 new FormHelperField()
                 {
                     FieldType = FormHelperField.FieldTypeRef.Double,
                     Control = EditActualHumidity,
                     Filters = new Dictionary<FormHelperField.FieldFilterRef, object>
                     {
                     },
                 },
                 // Среднее значение волокна, % (рассчитывается динамически)
                 new FormHelperField()
                 {
                     FieldType = FormHelperField.FieldTypeRef.Double,
                     Control = EditFiberPctAvg,
                     Filters = new Dictionary<FormHelperField.FieldFilterRef, object>
                     {
                     },
                 },
                 // Входной контроль (описание)
                 new FormHelperField()
                 {
                    Path="NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=MemoNote,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                 },
            };

            Form.ToolbarControl = FormToolbar;
            Form.StatusControl = StatusBar;
            Form.SetFields(fields);
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            // заполняем данные по качествам
            SetQuality();
            Form.SetDefaults();
        }

        public void Edit()
        {
            FrameTitle = $"Машина {Values.CheckGet("NAME")}, поставщик {Values.CheckGet("POST_NAME")}, ИД {IdScrap}, {StrCategoryBale}";
            GetData();
        }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            //var e = Central.WM.KeyboardEventsArgs;
            //switch (e.Key)
            //{
            //    case Key.Escape:
            //        //   Close();
            //        e.Handled = true;
            //        break;
            //    case Key.Enter:
            //       // Save();
            //        e.Handled = true;
            //        break;
            //}
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
        }

        /// <summary>
        /// нажали кнопку расчета средней влажности кип первого качества 
        /// </summary>
        private void ButtonAvgHumidity1Click()
        {
            int count = 0;
            double humidityPercent = 0.0;

            if (!EditH1_1.Text.IsNullOrEmpty())
            {
                humidityPercent = humidityPercent + EditH1_1.Text.ToDouble();
                count++;
            }

            if (!EditH1_2.Text.IsNullOrEmpty())
            {
                humidityPercent = humidityPercent + EditH1_2.Text.ToDouble();
                count++;
            }
            if (!EditH1_3.Text.IsNullOrEmpty())
            {
                humidityPercent = humidityPercent + EditH1_3.Text.ToDouble();
                count++;
            }
            if (!EditH1_4.Text.IsNullOrEmpty())
            {
                humidityPercent = humidityPercent + EditH1_4.Text.ToDouble();
                count++;
            }
            if (!EditH1_5.Text.IsNullOrEmpty())
            {
                humidityPercent = humidityPercent + EditH1_5.Text.ToDouble();
                count++;
            }
            if (!EditH1_6.Text.IsNullOrEmpty())
            {
                humidityPercent = humidityPercent + EditH1_6.Text.ToDouble();
                count++;
            }

            if (count >= 1)
            {
                humidityPercent = Math.Round(humidityPercent / count, 0);
            }

            EditHumidityPercent1.Text = humidityPercent.ToString();
            Calculate_actual_humidity();
        }

        /// <summary>
        /// нажали кнопку расчета средней влажности кип второго качества 
        /// </summary>
        private void ButtonAvgHumidity2Click()
        {
            int count = 0;
            double humidityPercent = 0.0;

            if (!EditH2_1.Text.IsNullOrEmpty())
            {
                humidityPercent = humidityPercent + EditH2_1.Text.ToDouble();
                count++;
            }

            if (!EditH2_2.Text.IsNullOrEmpty())
            {
                humidityPercent = humidityPercent + EditH2_2.Text.ToDouble();
                count++;
            }
            if (!EditH2_3.Text.IsNullOrEmpty())
            {
                humidityPercent = humidityPercent + EditH2_3.Text.ToDouble();
                count++;
            }
            if (!EditH2_4.Text.IsNullOrEmpty())
            {
                humidityPercent = humidityPercent + EditH2_4.Text.ToDouble();
                count++;
            }
            if (!EditH2_5.Text.IsNullOrEmpty())
            {
                humidityPercent = humidityPercent + EditH2_5.Text.ToDouble();
                count++;
            }
            if (!EditH2_6.Text.IsNullOrEmpty())
            {
                humidityPercent = humidityPercent + EditH2_6.Text.ToDouble();
                count++;
            }

            if (count >= 1)
            {
                humidityPercent = Math.Round(humidityPercent / count, 0);
            }

            EditHumidityPercent2.Text = humidityPercent.ToString();
            Calculate_actual_humidity();
        }


        /// <summary>
        /// нажали кнопку расчета средней влажности кип третьего качества 
        /// </summary>
        private void ButtonAvgHumidity3Click()
        {
            int count = 0;
            double humidityPercent = 0.0;

            if (!EditH3_1.Text.IsNullOrEmpty())
            {
                humidityPercent = humidityPercent + EditH3_1.Text.ToDouble();
                count++;
            }

            if (!EditH3_2.Text.IsNullOrEmpty())
            {
                humidityPercent = humidityPercent + EditH3_2.Text.ToDouble();
                count++;
            }
            if (!EditH3_3.Text.IsNullOrEmpty())
            {
                humidityPercent = humidityPercent + EditH3_3.Text.ToDouble();
                count++;
            }
            if (!EditH3_4.Text.IsNullOrEmpty())
            {
                humidityPercent = humidityPercent + EditH3_4.Text.ToDouble();
                count++;
            }
            if (!EditH3_5.Text.IsNullOrEmpty())
            {
                humidityPercent = humidityPercent + EditH3_5.Text.ToDouble();
                count++;
            }
            if (!EditH3_6.Text.IsNullOrEmpty())
            {
                humidityPercent = humidityPercent + EditH3_6.Text.ToDouble();
                count++;
            }

            if (count >= 1)
            {
                humidityPercent = Math.Round(humidityPercent / count, 0);
            }

            EditHumidityPercent3.Text = humidityPercent.ToString();
            Calculate_actual_humidity();

        }

        /// <summary>
        /// заполняем по умолчанию все данные для качеств №1,2,3
        /// </summary>
        private void SetQuality()
        {
            // Обвязка
            {
                var list = new Dictionary<string, string>();
                list.Add("0", "");
                list.Add("1", "Проволока");
                list.Add("2", "Шпагат");
                list.Add("3", "Лента");
                list.Add("4", "Стрейч-пленка");

                BoxType1.Items = list;
                BoxType2.Items = list;
                BoxType3.Items = list;
            }

            //Размер кип
            {
                var list = new Dictionary<string, string>();
                list.Add("", "");
                list.Add("Стандартныерыхлые", "Стандартные рыхлые");
                list.Add("Стандартныеплотноспрессованные", "Стандартные плотноспрессованные");
                list.Add("Длинные", "Длинные");
                list.Add("Кубы", "Кубы");
                list.Add("Маленькиедо100кг", "Маленькие до 100 кг");
                list.Add("Маленькие,связанныепо2", "Маленькие, связанные по 2");

                BoxSizeBale1.Items = list;
                BoxSizeBale2.Items = list;
                BoxSizeBale3.Items = list;
            }

            // Влажность кип
            {
                var list = new Dictionary<string, string>();
                list.Add("", "");
                list.Add("Сухая", "Сухая");
                list.Add("Влажныетюкиснаружи(осадки)", "Влажные тюки снаружи (осадки)");
                list.Add("Равномерновлажная", "Равномерно влажная");
                list.Add("Мокрыепрослойки", "Мокрые прослойки");

                BoxHumidityNote1.Items = list;
                BoxHumidityNote2.Items = list;
                BoxHumidityNote3.Items = list;
            }

            // Вид кип
            {
                var list = new Dictionary<string, string>();
                list.Add("", "");
                list.Add("стандартныеплотноспрессованные", "стандартные плотноспрессованные");
                list.Add("стандартныерыхлые", "стандартные рыхлые");
                list.Add("маленькиедо100кг", "маленькие до 100кг");
                list.Add("кубы", "кубы");
                list.Add("длинные", "длинные");
                list.Add("рулоны/бобины", "рулоны/бобины");
                list.Add("листынаподдонах", "листы на поддонах");

                BoxTypeOfBale1.Items = list;
                BoxTypeOfBale2.Items = list;
                BoxTypeOfBale3.Items = list;
            }

            // Категория кип
            {
                var list = new Dictionary<string, string>();
                list.Add("", "");
                list.Add("\"допотребителя\"", "\"до потребителя\"");
                list.Add("\"послепотребителя\"", "\"после потребителя\"");

                BoxCategoryBale1.Items = list;
                BoxCategoryBale2.Items = list;
                BoxCategoryBale3.Items = list;
            }

            // Вид ламинации кип
            {
                var list = new Dictionary<string, string>();
                list.Add("", "");
                list.Add("безламинации", "без ламинации");
                list.Add("пленка", "пленка");
                list.Add("фольга", "фольга");
                list.Add("пленка+фольга", "пленка + фольга");

                BoxTypeOfLamination1.Items = list;
                BoxTypeOfLamination2.Items = list;
                BoxTypeOfLamination3.Items = list;
            }

            // Цвет волокна кип
            {
                var list = new Dictionary<string, string>();
                list.Add("", "");
                list.Add("белый", "белый");
                list.Add("бурый", "бурый");
                list.Add("серый", "серый");
                list.Add("бело-бурый", "бело - бурый");

                BoxFiberColor1.Items = list;
                BoxFiberColor2.Items = list;
                BoxFiberColor3.Items = list;
            }

            // Загрузка машины
            {
                var list = new Dictionary<string, string>();
                list.Add("", "");
                list.Add("Ровная", "Ровная");
                list.Add("Неровная(сосмещением)", "Неровная(со смещением)");
                list.Add("Ручная", "Ручная");
                list.Add("Заваленныетюки", "Заваленные тюки");

                BoxTypeLoading.Items = list;
            }

            // Стандартное примечание
            {
                var list = new Dictionary<string, string>();
                list.Add("", "");
                list.Add("Вбумагу", "В бумагу");
                list.Add("Вкартон", "В картон");
                list.Add("Вбумагу/картон", "В бумагу/ картон");
                list.Add("Подкидывать", "Подкидывать");
                list.Add("Наленту", "На ленту");
                list.Add("Нашредирование", "На шредирование");
                list.Add("Рубить/резать", "Рубить / резать");

                BoxStandartNote.Items = list;
            }

            switch (IdCategory)
            {
                case 1:
                case 2:
                case 3:
                case 33:
                case 34:
                    CurrentVidBale = 5;  // MC-5Б
                    StrCategoryBale = "(Категория MC-5Б)";

                    // качество
                    {
                        var list = new Dictionary<string, string>();
                        list.Add("0", "");
                        list.Add("\"допотребителя\",тех.обрезь", "\"до потребителя\", тех. обрезь");
                        list.Add("\"послепотребителя\"цветная", "\"после потребителя\" цветная");
                        list.Add("\"послепотребителя\"бурая", "\"после потребителя\" бурая");
                        list.Add("\"полигонная\"", "\"полигонная\"");
                        list.Add("измельченная\"послепотребителя\"", "измельченная \"после потребителя\"");
                        list.Add("\"рыбнаякоробка\"(запахрыбы)", "\"рыбная коробка\" (запах рыбы)");
                        list.Add("\"куринаякоробка\"", "\"куриная коробка\"");
                        list.Add("\"горячая\",\"послепотребителя\"", "\"горячая\", \"после потребителя\"");

                        BoxQualityBale1.Items = list;
                        BoxQualityBale2.Items = list;
                        BoxQualityBale3.Items = list;
                    }
                    // загрязнение
                    {
                        int i = 0;
                        while (i < Contantimation5.Length)
                        {
                            var s = Contantimation5[i];
                            Contamination1ListBox.Items.Add(new System.Windows.Controls.CheckBox() { Content = s, Height = 15, Width = 470 });
                            Contamination2ListBox.Items.Add(new System.Windows.Controls.CheckBox() { Content = s, Height = 15, Width = 470 });
                            Contamination3ListBox.Items.Add(new System.Windows.Controls.CheckBox() { Content = s, Height = 15, Width = 470 });
                            i++;
                        }
                    }
                    break;

                case 21:
                case 22:
                case 23:
                    CurrentVidBale = 11;  // MC-11В
                    StrCategoryBale = "(Категория MC-11В)";

                    // качество
                    {
                        var list = new Dictionary<string, string>();
                        list.Add("", "");
                        list.Add("браклам.упак.материала", "брак лам. упак. материала");
                        list.Add("бракнелам.упак.материала", "брак нелам. упак. материала");
                        list.Add("упаковкитипаTetraPakотмолокаисока", "упаковки типа Tetra Pak от молока и сока");
                        list.Add("упаковкитипаTetraPakотмолокаисокавполиэтиленовыхмешках", "упаковки типа Tetra Pak от молока и сока в полиэтиленовых мешках");
                        list.Add("высечкаламинированного/металлизированногокартона", "высечка ламинированного/металлизированного картона");
                        list.Add("высечкаламинированногогофорокартона", "высечка ламинированного гофорокартона");
                        list.Add("заготовкидляупаковокмолокаисоковсвысокимсодержаниемволокна", "заготовки для упаковок молока и соков с высоким содержанием волокна");
                        list.Add("заготовкидляупаковокмолокаисоковснизкимсодержаниемволокна", "заготовки для упаковок молока и соков с низким содержанием волокна");
                        list.Add("\"лапша\"", "\"лапша\"");

                        BoxQualityBale1.Items = list;
                        BoxQualityBale2.Items = list;
                        BoxQualityBale3.Items = list;
                    }

                    // загрязнение
                    {
                        int i = 0;
                        while (i < Contantimation11.Length)
                        {
                            var s = Contantimation11[i];
                            Contamination1ListBox.Items.Add(new System.Windows.Controls.CheckBox() { Content = s, Height = 15, Width = 480 });
                            Contamination2ListBox.Items.Add(new System.Windows.Controls.CheckBox() { Content = s, Height = 15, Width = 480 });
                            Contamination3ListBox.Items.Add(new System.Windows.Controls.CheckBox() { Content = s, Height = 15, Width = 480 });
                            i++;
                        }

                        BoxTypeOfBale1.IsEnabled = true;
                        BoxCategoryBale1.IsEnabled = true;
                        BoxTypeOfLamination1.IsEnabled = true;
                        BoxFiberColor1.IsEnabled = true;

                        BoxTypeOfBale2.IsEnabled = true;
                        BoxCategoryBale2.IsEnabled = true;
                        BoxTypeOfLamination2.IsEnabled = true;
                        BoxFiberColor2.IsEnabled = true;

                        BoxTypeOfBale3.IsEnabled = true;
                        BoxCategoryBale3.IsEnabled = true;
                        BoxTypeOfLamination3.IsEnabled = true;
                        BoxFiberColor3.IsEnabled = true;

                    }
                    break;

                case 31:
                case 32:
                    CurrentVidBale = 6;  // MC-6Б
                    StrCategoryBale = "(Категория MC-6Б)";

                    // качество
                    {
                        var list = new Dictionary<string, string>();
                        list.Add("", "");
                        list.Add("\"допотребителя\"цветная/белая", "\"до потребителя\" цветная/белая");
                        list.Add("\"допотребителя\"бурая", "\"до потребителя\" бурая");
                        list.Add("\"допотребителя\"серая", "\"до потребителя\" серая");
                        list.Add("\"послепотребителя\"цветная/белая", "\"после потребителя\" цветная/белая");
                        list.Add("\"послепотребителя\"бурая", "\"после потребителя\" бурая");
                        list.Add("\"послепотребителя\"серая", "\"после потребителя\" серая");

                        BoxQualityBale1.Items = list;
                        BoxQualityBale2.Items = list;
                        BoxQualityBale3.Items = list;
                    }

                    // загрязнение
                    {
                        int i = 0;
                        while (i < Contantimation6.Length)
                        {
                            var s = Contantimation6[i];
                            Contamination1ListBox.Items.Add(new System.Windows.Controls.CheckBox() { Content = s, Height = 15, Width = 480 });
                            Contamination2ListBox.Items.Add(new System.Windows.Controls.CheckBox() { Content = s, Height = 15, Width = 480 });
                            Contamination3ListBox.Items.Add(new System.Windows.Controls.CheckBox() { Content = s, Height = 15, Width = 480 });
                            i++;
                        }
                    }
                    break;

                case 61:
                    CurrentVidBale = 8;  // MC-8В
                    StrCategoryBale = "(Категория MC-8В)";

                    // качество
                    {
                        var list = new Dictionary<string, string>();
                        list.Add("", "");
                        list.Add("\"допотребителя\",газетыигазетнаябумага", "\"до потребителя\", газеты и газетная бумага");
                        list.Add("\"допотребителя\",отходыпечатнойпромышленности", "\"до потребителя\", отходы печатной промышленности");
                        list.Add("\"послепотребителя\",газетыигазетнаябумага", "\"после потребителя\", газеты и газетная бумага");
                        list.Add("\"допотребителя\",газетыигазетнаябумага", "\"до потребителя\", газеты и газетная бумага");

                        BoxQualityBale1.Items = list;
                        BoxQualityBale2.Items = list;
                        BoxQualityBale3.Items = list;
                    }

                    // загрязнение
                    {
                        int i = 0;
                        while (i < Contantimation8.Length)
                        {
                            var s = Contantimation8[i];
                            Contamination1ListBox.Items.Add(new System.Windows.Controls.CheckBox() { Content = s, Height = 15, Width = 480 });
                            Contamination2ListBox.Items.Add(new System.Windows.Controls.CheckBox() { Content = s, Height = 15, Width = 480 });
                            Contamination3ListBox.Items.Add(new System.Windows.Controls.CheckBox() { Content = s, Height = 15, Width = 480 });
                            i++;
                        }
                    }
                    break;


                default:
                    CurrentVidBale = 0;
                    StrCategoryBale = "";
                    break;
            }

            // это возвратная партия
            if (ReturnFlag == 1)
            {
                // Причина возврата
                {
                    var list = new Dictionary<string, string>();
                    list.Add("", "");
                    list.Add("п.4.1.4Высокая влажность", "п. 4.1.4  Высокая влажность");
                    list.Add("п.4.1.2Загрязнения>5%.", "п. 4.1.2 Загрязнения > 5 %.");
                    list.Add("п.4.1.2Макулатура\"Табачка\"", "п. 4.1.2 Макулатура \"Табачка\"");
                    list.Add("п.4.1.2Загрязнения>5%.Высокаядоляпримеси\"табачки\".", "п. 4.1.2 Загрязнения > 5 %.Высокая доля примеси \"табачки\".");
                    list.Add("ПриложениеА.Загрязнения>5%", "Приложение А. Загрязнения > 5 %");
                    list.Add("п.4.1.1Высокаядоляпримесимакулатурынизкихмарок.", "п.4.1.1 Высокая доля примеси макулатуры низких марок.");
                    list.Add("Полигонныйсбор,МС-5/Б3,теплая,парящая,очагиизапахплесени", "Полигонный сбор, МС -5 / Б3, теплая, парящая, очаги и запах плесени");
                    list.Add("Загрязнения>5%.ВысокаядоляМС-5Б/3(полигон).", "Загрязнения > 5 %.Высокая доля МС-5Б / 3(полигон).");
                    list.Add("Длинныеполосы.", "Длинные полосы.");
                    list.Add("Несоответствиетребованиямпроизводства", "Несоответствие требованиям производства");
                    BoxTypeLoadingReturn.Items = list;
                }

                LabelTypeLoading.Content = "Причина возврата";
                LabelStandartNote.Visibility = Visibility.Hidden;
                BoxTypeLoading.Visibility = Visibility.Hidden;
                BoxStandartNote.Visibility = Visibility.Hidden;
                QualityBale2.IsEnabled = false;
                QualityBale3.IsEnabled = false;

                BoxTypeLoadingReturn.Visibility = Visibility.Visible;
               
            }
        }

        /// <summary>
        /// получение данных с сервера
        /// </summary>
        public async void GetData()
        {
            DisableControls();

            bool resume = true;

            CheckFiberPctAvgFlag = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();
                p.CheckAdd("ID_SCRAP", IdScrap.ToString());
                p.CheckAdd("RETURN_FLAG", ReturnFlag.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "ProductionPm");
                q.Request.SetParam("Object", "ScrapPaper");
                q.Request.SetParam("Action", "ScrapTransportAttrSelectRecord");
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
                        var ds = ListDataSet.Create(result, "ITEMS");

                        if (ds.Items.Count > 0)
                        {

                            // общие данные
                            BoxTypeLoading.SetSelectedItemByKey(ds.Items[0].CheckGet("TYPE_LOADING").ToString().Replace(" ", ""));
                            BoxStandartNote.SetSelectedItemByKey(ds.Items[0].CheckGet("STANDARD_NOTE").ToString().Replace(" ", ""));

                            if (ReturnFlag == 1)
                            {
                                MemoNote.Text = ds.Items[0].CheckGet("RETURN_NOTE").ToString();
                                BoxTypeLoadingReturn.SetSelectedItemByKey(ds.Items[0].CheckGet("NOTE_RETURN").ToString().Replace(" ", ""));
                            }
                            else
                            {
                                Form.SetValues(ds);
                            }

                            foreach (var item in ds.Items)
                            {
                                switch (item.CheckGet("QUALITY").ToInt())
                                {
                                    // заполняем качество №1
                                    case 1:
                                        EditPart1.Text = ds.Items[0].CheckGet("PART").ToString();
                                        BoxType1.SetSelectedItemByKey((ds.Items[0].CheckGet("TYPE_TYING").ToInt() + 1).ToString());
                                        BoxSizeBale1.SetSelectedItemByKey(ds.Items[0].CheckGet("SIZE_BALE").ToString().Replace(" ", ""));
                                        BoxQualityBale1.SetSelectedItemByKey(ds.Items[0].CheckGet("QUALITY_BALE").ToString().Replace(" ", ""));
                                        BoxHumidityNote1.SetSelectedItemByKey(ds.Items[0].CheckGet("HUMIDITY_NOTE").ToString().Replace(" ", ""));
                                        BoxTypeOfBale1.SetSelectedItemByKey(ds.Items[0].CheckGet("TYPE_OF_BALE").ToString().Replace(" ", ""));
                                        BoxCategoryBale1.SetSelectedItemByKey(ds.Items[0].CheckGet("CATEGORY_BALE").ToString().Replace(" ", ""));
                                        BoxTypeOfLamination1.SetSelectedItemByKey(ds.Items[0].CheckGet("TYPE_OF_LAMINATION").ToString().Replace(" ", ""));
                                        BoxFiberColor1.SetSelectedItemByKey(ds.Items[0].CheckGet("FIBER_COLOR").ToString().Replace(" ", ""));
                                        EditW1_1.Text = ds.Items[0].CheckGet("W_1").ToString();
                                        EditW1_2.Text = ds.Items[0].CheckGet("W_2").ToString();
                                        EditW1_3.Text = ds.Items[0].CheckGet("W_3").ToString();
                                        EditW1_4.Text = ds.Items[0].CheckGet("W_4").ToString();
                                        EditW1_5.Text = ds.Items[0].CheckGet("W_5").ToString();
                                        EditW1_6.Text = ds.Items[0].CheckGet("W_6").ToString();

                                        EditH1_1.Text = ds.Items[0].CheckGet("H_1").ToString();
                                        EditH1_2.Text = ds.Items[0].CheckGet("H_2").ToString();
                                        EditH1_3.Text = ds.Items[0].CheckGet("H_3").ToString();
                                        EditH1_4.Text = ds.Items[0].CheckGet("H_4").ToString();
                                        EditH1_5.Text = ds.Items[0].CheckGet("H_5").ToString();
                                        EditH1_6.Text = ds.Items[0].CheckGet("H_6").ToString();

                                        EditHumidityPercent1.Text = ds.Items[0].CheckGet("HUMIDITY_PERCENT").ToString();
                                        EditFiberPct1.Text = ds.Items[0].CheckGet("FIBER_PCT").ToString();

                                        // загрязнение
                                        {
                                            // считанная строка из CONTAMINATION "загрязнение"
                                            var s1 = ds.Items[0].CheckGet("CONTAMINATION").ToString();
                                            if (!string.IsNullOrEmpty(s1))
                                            {
                                                var lines = s1.Split(new[] { ";" }, System.StringSplitOptions.RemoveEmptyEntries);
                                                foreach (var line in lines)
                                                {
                                                    foreach (var row in Contamination1ListBox.Items)
                                                    {
                                                        var c = (System.Windows.Controls.CheckBox)row;
                                                        if ((string)c.Content == line)
                                                        {
                                                            c.IsChecked = true;
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        if (!EditH1_1.Text.IsNullOrEmpty()
                                            || !EditH1_2.Text.IsNullOrEmpty()
                                            || !EditH1_3.Text.IsNullOrEmpty()
                                            || !EditH1_4.Text.IsNullOrEmpty()
                                            || !EditH1_5.Text.IsNullOrEmpty()
                                            || !EditH1_6.Text.IsNullOrEmpty())
                                        {
                                            AvgHumidity1Button.IsEnabled = true;
                                        }

                                        break;

                                    // заполняем качество №2
                                    case 2:
                                        EditPart2.Text = ds.Items[1].CheckGet("PART").ToString();
                                        BoxType2.SetSelectedItemByKey((ds.Items[1].CheckGet("TYPE_TYING").ToInt() + 1).ToString());
                                        BoxSizeBale2.SetSelectedItemByKey(ds.Items[1].CheckGet("SIZE_BALE").ToString().Replace(" ", ""));
                                        BoxQualityBale2.SetSelectedItemByKey(ds.Items[1].CheckGet("QUALITY_BALE").ToString().Replace(" ", ""));
                                        BoxHumidityNote2.SetSelectedItemByKey(ds.Items[1].CheckGet("HUMIDITY_NOTE").ToString().Replace(" ", ""));
                                        BoxTypeOfBale2.SetSelectedItemByKey(ds.Items[1].CheckGet("TYPE_OF_BALE").ToString().Replace(" ", ""));
                                        BoxCategoryBale2.SetSelectedItemByKey(ds.Items[1].CheckGet("CATEGORY_BALE").ToString().Replace(" ", ""));
                                        BoxTypeOfLamination2.SetSelectedItemByKey(ds.Items[1].CheckGet("TYPE_OF_LAMINATION").ToString().Replace(" ", ""));
                                        BoxFiberColor2.SetSelectedItemByKey(ds.Items[1].CheckGet("FIBER_COLOR").ToString().Replace(" ", ""));
                                        EditW2_1.Text = ds.Items[1].CheckGet("W_1").ToString();
                                        EditW2_2.Text = ds.Items[1].CheckGet("W_2").ToString();
                                        EditW2_3.Text = ds.Items[1].CheckGet("W_3").ToString();
                                        EditW2_4.Text = ds.Items[1].CheckGet("W_4").ToString();
                                        EditW2_5.Text = ds.Items[1].CheckGet("W_5").ToString();
                                        EditW2_6.Text = ds.Items[1].CheckGet("W_6").ToString();

                                        EditH2_1.Text = ds.Items[1].CheckGet("H_1").ToString();
                                        EditH2_2.Text = ds.Items[1].CheckGet("H_2").ToString();
                                        EditH2_3.Text = ds.Items[1].CheckGet("H_3").ToString();
                                        EditH2_4.Text = ds.Items[1].CheckGet("H_4").ToString();
                                        EditH2_5.Text = ds.Items[1].CheckGet("H_5").ToString();
                                        EditH2_6.Text = ds.Items[1].CheckGet("H_6").ToString();

                                        EditHumidityPercent2.Text = ds.Items[1].CheckGet("HUMIDITY_PERCENT").ToString();
                                        EditFiberPct2.Text = ds.Items[1].CheckGet("FIBER_PCT").ToString();

                                        // считанная строка из CONTAMINATION "загрязнение"
                                        var s2 = ds.Items[1].CheckGet("CONTAMINATION").ToString();
                                        if (!string.IsNullOrEmpty(s2))
                                        {
                                            var lines = s2.Split(new[] { ";" }, System.StringSplitOptions.RemoveEmptyEntries);
                                            foreach (var line in lines)
                                            {
                                                foreach (var row in Contamination2ListBox.Items)
                                                {
                                                    var c = (System.Windows.Controls.CheckBox)row;
                                                    if ((string)c.Content == line)
                                                    {
                                                        c.IsChecked = true;
                                                    }
                                                }
                                            }
                                        }

                                        if (!EditH2_1.Text.IsNullOrEmpty()
                                            || !EditH2_2.Text.IsNullOrEmpty()
                                            || !EditH2_3.Text.IsNullOrEmpty()
                                            || !EditH2_4.Text.IsNullOrEmpty()
                                            || !EditH2_5.Text.IsNullOrEmpty()
                                            || !EditH2_6.Text.IsNullOrEmpty())
                                        {
                                            AvgHumidity2Button.IsEnabled = true;
                                        }

                                        break;

                                    // заполняем качество №3
                                    case 3:
                                        EditPart3.Text = ds.Items[2].CheckGet("PART").ToString();
                                        BoxType3.SetSelectedItemByKey((ds.Items[2].CheckGet("TYPE_TYING").ToInt() + 1).ToString());
                                        BoxSizeBale3.SetSelectedItemByKey(ds.Items[2].CheckGet("SIZE_BALE").ToString().Replace(" ", ""));
                                        BoxQualityBale3.SetSelectedItemByKey(ds.Items[2].CheckGet("QUALITY_BALE").ToString().Replace(" ", ""));
                                        BoxHumidityNote3.SetSelectedItemByKey(ds.Items[2].CheckGet("HUMIDITY_NOTE").ToString().Replace(" ", ""));
                                        BoxTypeOfBale3.SetSelectedItemByKey(ds.Items[2].CheckGet("TYPE_OF_BALE").ToString().Replace(" ", ""));
                                        BoxCategoryBale3.SetSelectedItemByKey(ds.Items[2].CheckGet("CATEGORY_BALE").ToString().Replace(" ", ""));
                                        BoxTypeOfLamination3.SetSelectedItemByKey(ds.Items[2].CheckGet("TYPE_OF_LAMINATION").ToString().Replace(" ", ""));
                                        BoxFiberColor3.SetSelectedItemByKey(ds.Items[2].CheckGet("FIBER_COLOR").ToString().Replace(" ", ""));
                                        EditW3_1.Text = ds.Items[2].CheckGet("W_1").ToString();
                                        EditW3_2.Text = ds.Items[2].CheckGet("W_2").ToString();
                                        EditW3_3.Text = ds.Items[2].CheckGet("W_3").ToString();
                                        EditW3_4.Text = ds.Items[2].CheckGet("W_4").ToString();
                                        EditW3_5.Text = ds.Items[2].CheckGet("W_5").ToString();
                                        EditW3_6.Text = ds.Items[2].CheckGet("W_6").ToString();

                                        EditH3_1.Text = ds.Items[2].CheckGet("H_1").ToString();
                                        EditH3_2.Text = ds.Items[2].CheckGet("H_2").ToString();
                                        EditH3_3.Text = ds.Items[2].CheckGet("H_3").ToString();
                                        EditH3_4.Text = ds.Items[2].CheckGet("H_4").ToString();
                                        EditH3_5.Text = ds.Items[2].CheckGet("H_5").ToString();
                                        EditH3_6.Text = ds.Items[2].CheckGet("H_6").ToString();

                                        EditHumidityPercent3.Text = ds.Items[2].CheckGet("HUMIDITY_PERCENT").ToString();
                                        EditFiberPct3.Text = ds.Items[2].CheckGet("FIBER_PCT").ToString();

                                        // считанная строка из CONTAMINATION "загрязнение"
                                        var s3 = ds.Items[2].CheckGet("CONTAMINATION").ToString();
                                        if (!string.IsNullOrEmpty(s3))
                                        {
                                            var lines = s3.Split(new[] { ";" }, System.StringSplitOptions.RemoveEmptyEntries);
                                            foreach (var line in lines)
                                            {
                                                foreach (var row in Contamination3ListBox.Items)
                                                {
                                                    var c = (System.Windows.Controls.CheckBox)row;
                                                    if ((string)c.Content == line)
                                                    {
                                                        c.IsChecked = true;
                                                    }
                                                }
                                            }
                                        }

                                        if (!EditH3_1.Text.IsNullOrEmpty()
                                            || !EditH3_2.Text.IsNullOrEmpty()
                                            || !EditH3_3.Text.IsNullOrEmpty()
                                            || !EditH3_4.Text.IsNullOrEmpty()
                                            || !EditH3_5.Text.IsNullOrEmpty()
                                            || !EditH3_6.Text.IsNullOrEmpty())
                                        {
                                            AvgHumidity3Button.IsEnabled = true;
                                        }

                                        break;

                                    default:
                                        break;
                                }
                            }

                            // расчет фактической влажности партии макулатуры
                            Calculate_actual_humidity();

                            Calculate_avg_fiber_pct();

                        }

                        Show();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            EnableControls();
        }


        /// <summary>
        /// расчет фактической влажности партии макулатуры 
        /// </summary>
        private void Calculate_actual_humidity()
        {
            int v1 = 0;
            int v2 = 0;
            int v3 = 0;
            EditActualHumidity.Text = "";

            if (!EditPart1.Text.IsNullOrEmpty() && !EditHumidityPercent1.Text.IsNullOrEmpty())
                v1 = EditHumidityPercent1.Text.ToInt() * EditPart1.Text.ToInt();

            if (!EditPart2.Text.IsNullOrEmpty() && !EditHumidityPercent2.Text.IsNullOrEmpty())
                v2 = EditHumidityPercent2.Text.ToInt() * EditPart2.Text.ToInt();

            if (!EditPart3.Text.IsNullOrEmpty() && !EditHumidityPercent3.Text.IsNullOrEmpty())
                v3 = EditHumidityPercent3.Text.ToInt() * EditPart3.Text.ToInt();

            var w = (int)Math.Round((double)((v1 + v2 + v3) / 100), 0);
            EditActualHumidity.Text = w.ToString();
        }

        /// <summary>
        /// расчет среднего значения волокна% партии макулатуры
        /// </summary>
        private void Calculate_avg_fiber_pct()
        {
            double SumFiberPctValue = 0.0;
            int i = 0;
            CheckFiberPctAvgFlag = true;
            EditFiberPctAvg.Text = "0";

            if (!EditFiberPct1.Text.IsNullOrEmpty())
            {
                SumFiberPctValue += EditFiberPct1.Text.ToDouble();
                i++;
            }

            if (!EditFiberPct2.Text.IsNullOrEmpty())
            {
                SumFiberPctValue += EditFiberPct2.Text.ToDouble();
                i++;
            }

            if (!EditFiberPct3.Text.IsNullOrEmpty())
            {
                SumFiberPctValue += EditFiberPct3.Text.ToDouble();
                i++;
            }

            if (i > 0)
            {
                double AvgFiberPctValue = Math.Round(SumFiberPctValue / i, 2);
                EditFiberPctAvg.Text = AvgFiberPctValue.ToString();

                if (AvgFiberPctValue > 100)
                    CheckFiberPctAvgFlag = false;
            }
        }

        private void EditFiberPct1_TextChanged(object sender, TextChangedEventArgs e)
        {
            Calculate_avg_fiber_pct();
        }

        private void EditFiberPct2_TextChanged(object sender, TextChangedEventArgs e)
        {
            Calculate_avg_fiber_pct();
        }

        private void EditFiberPct3_TextChanged(object sender, TextChangedEventArgs e)
        {
            Calculate_avg_fiber_pct();
        }


        /// <summary>
        /// Проверки перед записью данных в БД
        /// </summary>
        public void Save()
        {
            if (Form.Validate())
            {
                bool resume = true;
                var v = Form.GetValues();
                string errorMsg = "";
                // сумма % всех частей (проволка+шпагат+лента) качества кип
                int sumPart = 0;

                if (resume)
                {
                    if (EditPart1.Text.IsNullOrEmpty())
                    {
                        errorMsg = "Не заполнено часть 1 %";
                        resume = false;
                    }

                    if (!EditPart1.Text.IsNullOrEmpty())
                        sumPart = EditPart1.Text.ToInt();

                    if (!EditPart2.Text.IsNullOrEmpty())
                        sumPart = sumPart + EditPart2.Text.ToInt();

                    if (!EditPart3.Text.IsNullOrEmpty())
                        sumPart = sumPart + EditPart3.Text.ToInt();

                    if (sumPart > 100)
                    {
                        errorMsg = "Сумма % частей не может быть больше 100";
                        resume = false;
                    }
                    else if (sumPart < 100)
                    {
                        errorMsg = "Сумма % частей не может быть меньше 100";
                        resume = false;
                    }
                    else if (sumPart == 0)
                    {
                        errorMsg = "Сумма % частей не может быть равна 0";
                        resume = false;
                    }

                    if (BoxTypeLoading.ValueTextBox.Text.IsNullOrEmpty())
                    {
                        errorMsg = "Выберите загрузку машины";
                        resume = false;
                    }

                    if (!CheckFiberPctAvgFlag)
                    {
                        errorMsg = "Среднее значение волокна не может быть больше 100%";
                        resume = false;
                    }
                }

                if (resume)
                {
                    UpdateRecord();
                }
                else
                {
                    Form.SetStatus(errorMsg, 1);
                }
            }
        }

        /// <summary>
        /// Изменение записи 
        /// </summary>
        public async void UpdateRecord()
        {
            var resume = true;

            DisableControls();

            var q = new LPackClientQuery();
            var p = new Dictionary<string, string>();

            // удаляем все данные по качеству кип для машины

            p.CheckAdd("ID_SCRAP", IdScrap.ToString());
            p.CheckAdd("RETURN_FLAG", ReturnFlag.ToString());

            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "ScrapPaper");
            q.Request.SetParam("Action", "ScrapTransportAttrDelete");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                // подготавливаем данные
                {
                    // сохраняем качество №1
                    if (!EditPart1.Text.IsNullOrEmpty())
                    {
                        var crc = 0;
                        var id_scrap = IdScrap;
                        var type_tying = BoxType1.SelectedItem.Key.ToInt() - 1;
                        var part = EditPart1.Text.ToInt();
                        var weight_bale = "";
                        var size_bale = BoxSizeBale1.ValueTextBox.Text;
                        var quality_bale = BoxQualityBale1.ValueTextBox.Text;
                        var humidity_note = BoxHumidityNote1.ValueTextBox.Text;

                        var humidity_percent = "";
                        if (!EditHumidityPercent1.Text.IsNullOrEmpty())
                            humidity_percent = EditHumidityPercent1.Text;

                        var w_1 = "";
                        if (!EditW1_1.Text.IsNullOrEmpty())
                            w_1 = EditW1_1.Text;
                        var w_2 = "";
                        if (!EditW1_2.Text.IsNullOrEmpty())
                            w_2 = EditW1_2.Text;
                        var w_3 = "";
                        if (!EditW1_3.Text.IsNullOrEmpty())
                            w_3 = EditW1_3.Text;
                        var w_4 = "";
                        if (!EditW1_4.Text.IsNullOrEmpty())
                            w_4 = EditW1_4.Text;
                        var w_5 = "";
                        if (!EditW1_5.Text.IsNullOrEmpty())
                            w_5 = EditW1_5.Text;
                        var w_6 = "";
                        if (!EditW1_6.Text.IsNullOrEmpty())
                            w_6 = EditW1_6.Text;

                        var h_1 = "";
                        if (!EditH1_1.Text.IsNullOrEmpty())
                            h_1 = EditH1_1.Text;
                        var h_2 = "";
                        if (!EditH1_2.Text.IsNullOrEmpty())
                            h_2 = EditH1_2.Text;
                        var h_3 = "";
                        if (!EditH1_3.Text.IsNullOrEmpty())
                            h_3 = EditH1_3.Text;
                        var h_4 = "";
                        if (!EditH1_4.Text.IsNullOrEmpty())
                            h_4 = EditH1_4.Text;
                        var h_5 = "";
                        if (!EditH1_5.Text.IsNullOrEmpty())
                            h_5 = EditH1_5.Text;
                        var h_6 = "";
                        if (!EditH1_6.Text.IsNullOrEmpty())
                            h_6 = EditH1_6.Text;

                        var fiber_pct = "";
                        if (!EditFiberPct1.Text.IsNullOrEmpty())
                            fiber_pct = EditFiberPct1.Text;

                        var return_flag = ReturnFlag;
                        var quality = "1";

                        var type_of_bale = BoxTypeOfBale1.ValueTextBox.Text;
                        var category_bale = BoxCategoryBale1.ValueTextBox.Text;
                        var type_of_lamination = BoxTypeOfLamination1.ValueTextBox.Text;
                        var fiber_color = BoxFiberColor1.ValueTextBox.Text;
                        var type_attr_flag = "1";

                        // расчитываем полноту заполнения описания машины
                        // (только для первого качества)
                        // в зависимости от типа макулатуры

                        // это макулатура
                        if (CurrentVidBale != 11)
                        {
                            if (BoxType1.SelectedItem.Key.ToInt() != -1)
                                crc += 2;

                            if (BoxSizeBale1.SelectedItem.Key.ToInt() != -1)
                                crc += 3;

                            if (BoxQualityBale1.SelectedItem.Key.ToInt() != -1)  //.ValueTextBox.Text.IsNullOrEmpty())
                                crc += 4;

                            if (BoxHumidityNote1.SelectedItem.Key.ToInt() != -1)
                                crc += 5;
                        }
                        else
                        {
                            //это тетра пак
                            if (BoxType1.SelectedItem.Key.ToInt() > 0)
                                crc += 2;

                            if (BoxSizeBale1.SelectedItem.Key.ToInt() != -1)
                                crc += 3;

                            if (BoxQualityBale1.SelectedItem.Key.ToInt() != -1)
                                crc += 4;

                            if (BoxHumidityNote1.SelectedItem.Key.ToInt() != -1)
                                crc += 5;

                            if (BoxTypeOfBale1.SelectedItem.Key.ToInt() != -1)
                                crc += 6;
                            if (BoxCategoryBale1.SelectedItem.Key.ToInt() != -1)
                                crc += 7;
                            if (BoxTypeOfLamination1.SelectedItem.Key.ToInt() != -1)
                                crc += 8;
                            if (BoxFiberColor1.SelectedItem.Key.ToInt() != -1)
                                crc += 9;
                        }

                        foreach (var row in Contamination1ListBox.Items)
                        {
                            var c = (System.Windows.Controls.CheckBox)row;
                            if (c.IsChecked == true)
                            {
                                crc += 10;
                                break;
                            }
                        }

                        var _crc = crc;

                        var contaminationStr = "";
                        foreach (var row in Contamination1ListBox.Items)
                        {
                            var c = (System.Windows.Controls.CheckBox)row;
                            if (c.IsChecked == true)
                            {
                                contaminationStr = contaminationStr + c.Content + ";";
                            }
                        }

                        p = new Dictionary<string, string>();
                        p.CheckAdd("ID_SCRAP", id_scrap.ToString());
                        p.CheckAdd("TYPE_TYING", type_tying.ToString());
                        p.CheckAdd("PART", part.ToString());
                        p.CheckAdd("WEIGHT_BALE", weight_bale.ToString());
                        p.CheckAdd("SIZE_BALE", size_bale.ToString());
                        p.CheckAdd("QUALITY_BALE", quality_bale.ToString());
                        p.CheckAdd("HUMIDITY_NOTE", humidity_note.ToString());
                        p.CheckAdd("HUMIDITY_PERCENT", humidity_percent.ToString());
                        p.CheckAdd("W_1", w_1.ToString());
                        p.CheckAdd("W_2", w_2.ToString());
                        p.CheckAdd("W_3", w_3.ToString());
                        p.CheckAdd("W_4", w_4.ToString());
                        p.CheckAdd("W_5", w_5.ToString());
                        p.CheckAdd("W_6", w_6.ToString());
                        p.CheckAdd("H_1", h_1.ToString());
                        p.CheckAdd("H_2", h_2.ToString());
                        p.CheckAdd("H_3", h_3.ToString());
                        p.CheckAdd("H_4", h_4.ToString());
                        p.CheckAdd("H_5", h_5.ToString());
                        p.CheckAdd("H_6", h_6.ToString());
                        p.CheckAdd("FIBER_PCT", fiber_pct.ToString());
                        p.CheckAdd("RETURN_FLAG", return_flag.ToString());
                        p.CheckAdd("QUALITY", quality.ToString());
                        p.CheckAdd("TYPE_OF_BALE", type_of_bale.ToString());
                        p.CheckAdd("CATEGORY_BALE", category_bale.ToString());
                        p.CheckAdd("TYPE_OF_LAMINATION", type_of_lamination.ToString());
                        p.CheckAdd("FIBER_COLOR", fiber_color.ToString());
                        p.CheckAdd("TYPE_ATTR_FLAG", type_attr_flag.ToString());
                        p.CheckAdd("CRC", _crc.ToString());
                        p.CheckAdd("CONTAMINATION", contaminationStr.ToString());

                        p.CheckAdd("TYPE_LOADING", BoxTypeLoading.ValueTextBox.Text);
                        p.CheckAdd("STANDARD_NOTE", BoxStandartNote.ValueTextBox.Text);
                        p.CheckAdd("NOTE", MemoNote.Text.ToString());
                        p.CheckAdd("NOTE_RETURN", BoxStandartNote.ValueTextBox.Text);

                        q = new LPackClientQuery();
                        q.Request.SetParam("Module", "ProductionPm");
                        q.Request.SetParam("Object", "ScrapPaper");
                        q.Request.SetParam("Action", "ScrapTransportAttrInsert");

                        q.Request.SetParams(p);

                        q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                        q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                        await Task.Run(() =>
                        {
                            q.DoQuery();
                        });

                        if (q.Answer.Status != 0)
                        {
                            Form.SetStatus(q.Answer.Error.Message, 1);
                            resume = false;
                        }
                    }

                    // сохраняем качество №2
                    if (!EditPart2.Text.IsNullOrEmpty())
                    {
                        var crc = 0;
                        var id_scrap = IdScrap;
                        var type_tying = BoxType2.SelectedItem.Key.ToInt() - 1;
                        var part = EditPart2.Text.ToInt();
                        var weight_bale = "";
                        var size_bale = BoxSizeBale2.ValueTextBox.Text;
                        var quality_bale = BoxQualityBale2.ValueTextBox.Text;
                        var humidity_note = BoxHumidityNote2.ValueTextBox.Text;

                        var humidity_percent = "";
                        if (!EditHumidityPercent2.Text.IsNullOrEmpty())
                            humidity_percent = EditHumidityPercent2.Text;

                        var w_1 = "";
                        if (!EditW2_1.Text.IsNullOrEmpty())
                            w_1 = EditW2_1.Text;
                        var w_2 = "";
                        if (!EditW2_2.Text.IsNullOrEmpty())
                            w_2 = EditW2_2.Text;
                        var w_3 = "";
                        if (!EditW2_3.Text.IsNullOrEmpty())
                            w_3 = EditW2_3.Text;
                        var w_4 = "";
                        if (!EditW2_4.Text.IsNullOrEmpty())
                            w_4 = EditW2_4.Text;
                        var w_5 = "";
                        if (!EditW2_5.Text.IsNullOrEmpty())
                            w_5 = EditW2_5.Text;
                        var w_6 = "";
                        if (!EditW2_6.Text.IsNullOrEmpty())
                            w_6 = EditW2_6.Text;

                        var h_1 = "";
                        if (!EditH2_1.Text.IsNullOrEmpty())
                            h_1 = EditH2_1.Text;
                        var h_2 = "";
                        if (!EditH2_2.Text.IsNullOrEmpty())
                            h_2 = EditH2_2.Text;
                        var h_3 = "";
                        if (!EditH2_3.Text.IsNullOrEmpty())
                            h_3 = EditH2_3.Text;
                        var h_4 = "";
                        if (!EditH2_4.Text.IsNullOrEmpty())
                            h_4 = EditH2_4.Text;
                        var h_5 = "";
                        if (!EditH2_5.Text.IsNullOrEmpty())
                            h_5 = EditH2_5.Text;
                        var h_6 = "";
                        if (!EditH2_6.Text.IsNullOrEmpty())
                            h_6 = EditH2_6.Text;

                        var fiber_pct = "";
                        if (!EditFiberPct2.Text.IsNullOrEmpty())
                            fiber_pct = EditFiberPct2.Text;

                        var return_flag = ReturnFlag;
                        var quality = "2";

                        var type_of_bale = BoxTypeOfBale2.ValueTextBox.Text;
                        var category_bale = BoxCategoryBale2.ValueTextBox.Text;
                        var type_of_lamination = BoxTypeOfLamination2.ValueTextBox.Text;
                        var fiber_color = BoxFiberColor2.ValueTextBox.Text;
                        var type_attr_flag = "1";

                        // расчитываем полноту заполнения описания машины
                        // (только для первого качества)
                        // в зависимости от типа макулатуры

                        // это макулатура
                        if (CurrentVidBale != 11)
                        {
                            if (BoxType2.SelectedItem.Key.ToInt() > 0)
                                crc += 2;

                            if (!BoxSizeBale2.ValueTextBox.Text.IsNullOrEmpty())
                                crc += 3;

                            if (!BoxQualityBale2.ValueTextBox.Text.IsNullOrEmpty())
                                crc += 4;

                            if (!BoxHumidityNote2.ValueTextBox.Text.IsNullOrEmpty())
                                crc += 5;
                        }
                        else
                        {
                            //это тетра пак
                            if (BoxType2.SelectedItem.Key.ToInt() > 0)
                                crc += 2;

                            if (!BoxSizeBale2.ValueTextBox.Text.IsNullOrEmpty())
                                crc += 3;

                            if (!BoxQualityBale2.ValueTextBox.Text.IsNullOrEmpty())
                                crc += 4;

                            if (!BoxHumidityNote2.ValueTextBox.Text.IsNullOrEmpty())
                                crc += 5;

                            if (!BoxTypeOfBale2.ValueTextBox.Text.IsNullOrEmpty())
                                crc += 6;
                            if (!BoxCategoryBale2.ValueTextBox.Text.IsNullOrEmpty())
                                crc += 7;
                            if (!BoxTypeOfLamination2.ValueTextBox.Text.IsNullOrEmpty())
                                crc += 8;
                            if (!BoxFiberColor2.ValueTextBox.Text.IsNullOrEmpty())
                                crc += 9;
                        }

                        foreach (var row in Contamination2ListBox.Items)
                        {
                            var c = (System.Windows.Controls.CheckBox)row;
                            if (c.IsChecked == true)
                            {
                                crc += 10;
                                break;
                            }
                        }

                        var _crc = crc;

                        var contaminationStr = "";
                        foreach (var row in Contamination2ListBox.Items)
                        {
                            var c = (System.Windows.Controls.CheckBox)row;
                            if (c.IsChecked == true)
                            {
                                contaminationStr = contaminationStr + c.Content + ";";
                            }
                        }

                        p = new Dictionary<string, string>();
                        p.CheckAdd("ID_SCRAP", id_scrap.ToString());
                        p.CheckAdd("TYPE_TYING", type_tying.ToString());
                        p.CheckAdd("PART", part.ToString());
                        p.CheckAdd("WEIGHT_BALE", weight_bale.ToString());
                        p.CheckAdd("SIZE_BALE", size_bale.ToString());
                        p.CheckAdd("QUALITY_BALE", quality_bale.ToString());
                        p.CheckAdd("HUMIDITY_NOTE", humidity_note.ToString());
                        p.CheckAdd("HUMIDITY_PERCENT", humidity_percent.ToString());
                        p.CheckAdd("W_1", w_1.ToString());
                        p.CheckAdd("W_2", w_2.ToString());
                        p.CheckAdd("W_3", w_3.ToString());
                        p.CheckAdd("W_4", w_4.ToString());
                        p.CheckAdd("W_5", w_5.ToString());
                        p.CheckAdd("W_6", w_6.ToString());
                        p.CheckAdd("H_1", h_1.ToString());
                        p.CheckAdd("H_2", h_2.ToString());
                        p.CheckAdd("H_3", h_3.ToString());
                        p.CheckAdd("H_4", h_4.ToString());
                        p.CheckAdd("H_5", h_5.ToString());
                        p.CheckAdd("H_6", h_6.ToString());
                        p.CheckAdd("FIBER_PCT", fiber_pct.ToString());
                        p.CheckAdd("RETURN_FLAG", return_flag.ToString());
                        p.CheckAdd("QUALITY", quality.ToString());
                        p.CheckAdd("TYPE_OF_BALE", type_of_bale.ToString());
                        p.CheckAdd("CATEGORY_BALE", category_bale.ToString());
                        p.CheckAdd("TYPE_OF_LAMINATION", type_of_lamination.ToString());
                        p.CheckAdd("FIBER_COLOR", fiber_color.ToString());
                        p.CheckAdd("TYPE_ATTR_FLAG", type_attr_flag.ToString());
                        p.CheckAdd("CRC", "");
                        p.CheckAdd("CONTAMINATION", contaminationStr.ToString());

                        p.CheckAdd("TYPE_LOADING", BoxTypeLoading.ValueTextBox.Text);
                        p.CheckAdd("STANDARD_NOTE", BoxStandartNote.ValueTextBox.Text);
                        p.CheckAdd("NOTE", MemoNote.Text.ToString());
                        p.CheckAdd("NOTE_RETURN", BoxStandartNote.ValueTextBox.Text);

                        q = new LPackClientQuery();
                        q.Request.SetParam("Module", "ProductionPm");
                        q.Request.SetParam("Object", "ScrapPaper");
                        q.Request.SetParam("Action", "ScrapTransportAttrInsert");

                        q.Request.SetParams(p);

                        q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                        q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                        await Task.Run(() =>
                        {
                            q.DoQuery();
                        });

                        if (q.Answer.Status != 0)
                        {
                            Form.SetStatus(q.Answer.Error.Message, 1);
                            resume = false;
                        }
                    }

                    // сохраняем качество №3
                    if (!EditPart3.Text.IsNullOrEmpty())
                    {
                        var crc = 0;
                        var id_scrap = IdScrap;
                        var type_tying = BoxType3.SelectedItem.Key.ToInt() - 1;
                        var part = EditPart3.Text.ToInt();
                        var weight_bale = "";
                        var size_bale = BoxSizeBale3.ValueTextBox.Text;
                        var quality_bale = BoxQualityBale3.ValueTextBox.Text;
                        var humidity_note = BoxHumidityNote3.ValueTextBox.Text;

                        var humidity_percent = "";
                        if (!EditHumidityPercent3.Text.IsNullOrEmpty())
                            humidity_percent = EditHumidityPercent3.Text;

                        var w_1 = "";
                        if (!EditW3_1.Text.IsNullOrEmpty())
                            w_1 = EditW3_1.Text;
                        var w_2 = "";
                        if (!EditW3_2.Text.IsNullOrEmpty())
                            w_2 = EditW3_2.Text;
                        var w_3 = "";
                        if (!EditW3_3.Text.IsNullOrEmpty())
                            w_3 = EditW3_3.Text;
                        var w_4 = "";
                        if (!EditW3_4.Text.IsNullOrEmpty())
                            w_4 = EditW3_4.Text;
                        var w_5 = "";
                        if (!EditW3_5.Text.IsNullOrEmpty())
                            w_5 = EditW3_5.Text;
                        var w_6 = "";
                        if (!EditW3_6.Text.IsNullOrEmpty())
                            w_6 = EditW3_6.Text;

                        var h_1 = "";
                        if (!EditH3_1.Text.IsNullOrEmpty())
                            h_1 = EditH3_1.Text;
                        var h_2 = "";
                        if (!EditH3_2.Text.IsNullOrEmpty())
                            h_2 = EditH3_2.Text;
                        var h_3 = "";
                        if (!EditH3_3.Text.IsNullOrEmpty())
                            h_3 = EditH3_3.Text;
                        var h_4 = "";
                        if (!EditH3_4.Text.IsNullOrEmpty())
                            h_4 = EditH3_4.Text;
                        var h_5 = "";
                        if (!EditH3_5.Text.IsNullOrEmpty())
                            h_5 = EditH3_5.Text;
                        var h_6 = "";
                        if (!EditH1_6.Text.IsNullOrEmpty())
                            h_6 = EditH1_6.Text;

                        var fiber_pct = "";
                        if (!EditFiberPct3.Text.IsNullOrEmpty())
                            fiber_pct = EditFiberPct3.Text;

                        var return_flag = ReturnFlag;
                        var quality = "3";

                        var type_of_bale = BoxTypeOfBale3.ValueTextBox.Text;
                        var category_bale = BoxCategoryBale3.ValueTextBox.Text;
                        var type_of_lamination = BoxTypeOfLamination3.ValueTextBox.Text;
                        var fiber_color = BoxFiberColor3.ValueTextBox.Text;
                        var type_attr_flag = "1";

                        // расчитываем полноту заполнения описания машины
                        // (только для первого качества)
                        // в зависимости от типа макулатуры

                        // это макулатура
                        if (CurrentVidBale != 11)
                        {
                            if (BoxType3.SelectedItem.Key.ToInt() != -1)
                                crc += 2;

                            if (BoxSizeBale3.SelectedItem.Key.ToInt() != -1)
                                crc += 3;

                            if (BoxQualityBale3.SelectedItem.Key.ToInt() != -1)
                                crc += 4;

                            if (BoxHumidityNote3.SelectedItem.Key.ToInt() != -1)
                                crc += 5;
                        }
                        else
                        {
                            //это тетра пак
                            if (BoxType3.SelectedItem.Key.ToInt() > 0)
                                crc += 2;

                            if (BoxSizeBale3.SelectedItem.Key.ToInt() != -1)
                                crc += 3;

                            if (BoxQualityBale3.SelectedItem.Key.ToInt() != -1)
                                crc += 4;

                            if (BoxHumidityNote3.SelectedItem.Key.ToInt() != -1)
                                crc += 5;

                            if (BoxTypeOfBale3.SelectedItem.Key.ToInt() != -1)
                                crc += 6;
                            if (BoxCategoryBale3.SelectedItem.Key.ToInt() != -1)
                                crc += 7;
                            if (BoxTypeOfLamination3.SelectedItem.Key.ToInt() != -1)
                                crc += 8;
                            if (BoxFiberColor3.SelectedItem.Key.ToInt() != -1)
                                crc += 9;
                        }

                        foreach (var row in Contamination3ListBox.Items)
                        {
                            var c = (System.Windows.Controls.CheckBox)row;
                            if (c.IsChecked == true)
                            {
                                crc += 10;
                                break;
                            }
                        }

                        var _crc = crc;

                        var contaminationStr = "";
                        foreach (var row in Contamination3ListBox.Items)
                        {
                            var c = (System.Windows.Controls.CheckBox)row;
                            if (c.IsChecked == true)
                            {
                                contaminationStr = contaminationStr + c.Content + ";";
                            }
                        }

                        p = new Dictionary<string, string>();
                        p.CheckAdd("ID_SCRAP", id_scrap.ToString());
                        p.CheckAdd("TYPE_TYING", type_tying.ToString());
                        p.CheckAdd("PART", part.ToString());
                        p.CheckAdd("WEIGHT_BALE", weight_bale.ToString());
                        p.CheckAdd("SIZE_BALE", size_bale.ToString());
                        p.CheckAdd("QUALITY_BALE", quality_bale.ToString());
                        p.CheckAdd("HUMIDITY_NOTE", humidity_note.ToString());
                        p.CheckAdd("HUMIDITY_PERCENT", humidity_percent.ToString());
                        p.CheckAdd("W_1", w_1.ToString());
                        p.CheckAdd("W_2", w_2.ToString());
                        p.CheckAdd("W_3", w_3.ToString());
                        p.CheckAdd("W_4", w_4.ToString());
                        p.CheckAdd("W_5", w_5.ToString());
                        p.CheckAdd("W_6", w_6.ToString());
                        p.CheckAdd("H_1", h_1.ToString());
                        p.CheckAdd("H_2", h_2.ToString());
                        p.CheckAdd("H_3", h_3.ToString());
                        p.CheckAdd("H_4", h_4.ToString());
                        p.CheckAdd("H_5", h_5.ToString());
                        p.CheckAdd("H_6", h_6.ToString());
                        p.CheckAdd("FIBER_PCT", fiber_pct.ToString());
                        p.CheckAdd("RETURN_FLAG", return_flag.ToString());
                        p.CheckAdd("QUALITY", quality.ToString());
                        p.CheckAdd("TYPE_OF_BALE", type_of_bale.ToString());
                        p.CheckAdd("CATEGORY_BALE", category_bale.ToString());
                        p.CheckAdd("TYPE_OF_LAMINATION", type_of_lamination.ToString());
                        p.CheckAdd("FIBER_COLOR", fiber_color.ToString());
                        p.CheckAdd("TYPE_ATTR_FLAG", type_attr_flag.ToString());
                        p.CheckAdd("CRC", "");
                        p.CheckAdd("CONTAMINATION", contaminationStr.ToString());

                        p.CheckAdd("TYPE_LOADING", BoxTypeLoading.ValueTextBox.Text);
                        p.CheckAdd("STANDARD_NOTE", BoxStandartNote.ValueTextBox.Text);
                        p.CheckAdd("NOTE", MemoNote.Text.ToString());
                        p.CheckAdd("NOTE_RETURN", BoxStandartNote.ValueTextBox.Text);

                        q = new LPackClientQuery();
                        q.Request.SetParam("Module", "ProductionPm");
                        q.Request.SetParam("Object", "ScrapPaper");
                        q.Request.SetParam("Action", "ScrapTransportAttrInsert");

                        q.Request.SetParams(p);

                        q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                        q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;


                        await Task.Run(() =>
                        {
                            q.DoQuery();
                        });

                        if (q.Answer.Status != 0)
                        {
                            Form.SetStatus(q.Answer.Error.Message, 1);
                            resume = false;
                        }
                    }


                    // необходимо дописать еще активити


                    if (resume)
                    {
                        // отправляем сообщени о закрытии окна
                        Central.Msg.SendMessage(new ItemMessage()
                        {
                            ReceiverGroup = "Production",
                            ReceiverName = ReceiverName,
                            SenderName = ControlName,
                            Action = "RefreshScrapTransportWeightGrid",
                        });

                        Close();
                    }
                }
            }
            //else
            //{
            //    Form.SetStatus(q.Answer.Error.Message, 1);
            //}

            EnableControls();
        }






        /////
    }


}
