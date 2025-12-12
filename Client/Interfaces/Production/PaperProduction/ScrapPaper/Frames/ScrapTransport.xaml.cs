using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
using Client.Interfaces.Production.Corrugator;
using Client.Interfaces.Shipments;
using DevExpress.DocumentServices.ServiceModel.DataContracts;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Editors;
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
using System.Security.Cryptography;
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
    /// Карточка с данными по машине с макулатурой
    /// </summary>
    /// <author>greshnyh_ni</author>
    /// <changed>2025-12-01</changed>
    public partial class ScrapTransport : ControlBase
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
        public Dictionary<string, string> Values { get; set; }

        /// <summary>
        /// категория макулатуры
        /// </summary>        
        private int IdCategory { get; set; }
        /// <summary>
        /// ИД машины
        /// </summary>
        private int IdScrap { get; set; }
        private bool ChangeFlag { get; set; }
        private bool ChangeDatasf { get; set; }
        private string NameProd { get; set; }
        /// <summary>
        /// предыдущая категория макулатуры
        /// </summary>
        private int IdCategoryOld { get; set; }
        /// <summary>
        ///  родительская категория макулатуры
        /// </summary>
        private int IdCategoryParent { get; set; }
        /// <summary>
        /// признак бумаги ТП 
        /// </summary>
        private int PaperTetrapakFlag { get; set; }

        /// <summary>
        /// старое название машины
        /// </summary>
        private string NameCarOld { get; set; }
        /// <summary>
        /// ИД поставщика
        /// </summary>
        private int IdPost { get; set; }

        /// <summary>
        /// старое название поставщика
        /// </summary>
        private string NamePostavshicOld { get; set; }

        /// <summary>
        /// предыдущая выбранная макулатуры MC-5Б
        /// </summary>
        private int ScrapCategoryOld5 { get; set; }
        /// <summary>
        /// предыдущая выбранная макулатуры MC-6Б
        /// </summary>
        private int ScrapCategoryOld6 { get; set; }
        /// <summary>
        /// предыдущая выбранная макулатуры MC-8В
        /// </summary>
        private int ScrapCategoryOld8 { get; set; }
        /// <summary>
        /// предыдущая выбранная макулатуры MC-11В
        /// </summary>
        private int ScrapCategoryOld11 { get; set; }
        /// <summary>
        /// предыдущий вес
        /// </summary>
        private int WeigthFactOld { get; set; }
        /// <summary>
        /// первоначальный поставщик макулатуры
        /// </summary>
        private int IdPostOld { get; set; }
        /// <summary>
        /// статус машины
        /// </summary>
        private int IdStatus { get; set; }
        /// <summary>
        /// предыдущий статус машины
        /// </summary>
        private int OldIdStatus { get; set; }

        /// <summary>
        /// предыдущий способ заполнения весов машины
        /// </summary>
        private int OldAutoInput { get; set; }

        /// <summary>
        /// ссылка на машину
        /// </summary>
        private int SurmId { get; set; }
        /// <summary>
        /// старая ссылка на машину
        /// </summary>
        private int OldSurmId { get; set; }


        /// <summary>
        /// ссылка на накладную
        /// </summary>
        private int Nnakl = 0;
        /// <summary>
        /// 0 - не изменялся вес
        /// 1 - автоматически брутто
        /// 2 - автоматически нетто
        /// 3 - ручное брутто
        /// 4 - ручное нетто
        /// </summary>
        private int IsAuto { get; set; }
        /// <summary>
        /// Ответ от платы Laurent, которая опрашивает датчики на весовой
        /// </summary>
        private string AnswerFromLaurent { get; set; }
        /// <summary>
        /// Текущий вес, который получаем с весов
        /// </summary>
        private int CurrentWeigth { get; set; }
        /// <summary>
        /// Идентификатор партии
        /// </summary>
        private int IdP { get; set; }
        /// <summary>
        /// Количество кип
        /// </summary>
        private int KolBale { get; set; }
        /// <summary>
        /// 1 - признак проведения прихода
        /// </summary>
        private int Provedeno { get; set; }
        /// <summary>
        /// статус машины при возврате/хранении
        /// </summary>
        private int StatusReturning { get; set; }
        /// <summary>
        /// Ряд разгрузки
        /// </summary>
        private string Sklad { get; set; }
        /// <summary>
        /// Ячейка разгрузки
        /// </summary>
        private int NumPlace { get; set; }
        /// <summary>
        /// вес принятой на ответ. хранение партии макулатуры
        /// </summary>
        private int WeigthReturn { get; set; }

        /// <summary>
        /// Номер акта разгрузки
        /// </summary>
        private int ActPrihod { get; set; }
        /// <summary>
        /// Номер акта разгрузки строкой
        /// </summary>
        private string ActPrihodStr { get; set; }

        /// <summary>
        /// Номер акта возврата
        /// </summary>
        private int ActReturnung { get; set; }

        /// <summary>
        /// ИД задачи на разгрузку макулатуры на ЛТ
        /// </summary>
        private int WmtaId { get; set; }
        /// <summary>
        /// ИД2 макулатуры
        /// </summary>
        private int Id2 { get; set; }

        /// <summary>
        /// 0- обычное, 1- вход. контроль, 2 -на ленту
        /// </summary>
        private int InputControlFlag { get; set; }

        /// <summary>
        /// IP порт табло на весовой БДМ1
        /// северное табло (въезд со стороны города на Лпак) IP 192.168.16.74:5000
        /// южное табло (выезд со стороны Лпак в город) IP 192.168.16.73:5000
        /// </summary>
        private string IpPort { get; set; }
        /// <summary>
        /// сообщение выводимое на табло
        /// </summary>
        private string Mess { get; set; }

        /// <summary>
        /// ссылка на плащадку БДМ1 -716, БДМ2 -1716, ЦЛТ - 2716
        /// </summary>
        public int IdSt { get; set; }
        /// <summary>
        /// контроль поставляемой категории макулатуры поставщиком в соответствии с договором.
        /// </summary>
        public bool ControlPostavshicFlag { get; set; }
        /// <summary>
        /// Номер Com порта на автом. весовой БДМ1
        /// </summary>
        public string Bdm1ComPort { get; set; }
        /// <summary>
        /// Номер Com порта на автом. весовой БДМ2
        /// </summary>
        public string Bdm2ComPort { get; set; }
        /// <summary>
        /// Ip адрес Laurent платы на весовой БДМ1
        /// </summary>
        public string Bdm1LaurentIp { get; set; }
        /// <summary>
        /// Ip адреса Laurent платы на весовой БДМ2
        /// </summary>
        public string Bdm2LaurentIp { get; set; }
        /// <summary>
        /// подключать автоматически весы при открытии формы
        /// </summary>
        public bool WeightOpenFlag { get; set; }
        /// <summary>
        /// признак очистки табло на БДМ1
        /// </summary>
        public bool ClearTabloBdm1Flag { get; set; }

        private bool FirstRun = true;

        /// <summary>
        /// дата взвешивания полной машины
        /// </summary>
        private string DtFull { get; set; }

        /// <summary>
        ///  произнак успешного списания кип в производство после оприходования
        /// </summary>
        private bool RashodBaleSuccessfully = false;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="record"></param>
        public ScrapTransport(Dictionary<string, string> record = null)
        {
            InitializeComponent();
            IdScrap = 0;

            if (record != null)
            {
                Values = record;
                IdScrap = Values.CheckGet("ID_SCRAP").ToInt();
            }

            ControlSection = "scrap_paper";
            DocumentationUrl = "/doc/l-pack-erp/production/molded_container/machine_control";

            FrameName = ControlName;

            InitForm();
            SetDefaults();
            PostavshicShow();

            OnMessage = (ItemMessage m) =>
                {
                    if (m.ReceiverName == ControlName)
                    {
                        ProcessMessage(m);
                        // Commander.ProcessCommand(m.Action, m);
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
                }

                Commander.Init(this);
            }

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
                case "CloseScrapTransport":
                    Close();
                    break;
            }
        }

        /// <summary>
        /// инициализация компонентов на форме
        /// </summary>
        private void InitForm()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                // Машина
                 new FormHelperField()
                 {
                    Path="NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=EditName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    { FormHelperField.FieldFilterRef.Required, null },
                    },
                 },
                // поставщик
                new FormHelperField()
                {
                    Path="ID_POST",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Postavshic,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                // водитель
                new FormHelperField()
                {
                    Path="SURM_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Driver,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                // счет-фактура
                new FormHelperField()
                {
                    Path="NUM_DOC",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=EditNumDoc,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                // дата счет/фактуры
                new FormHelperField()
                {
                    Path="DATASF",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control= NaklprihDataSf,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }, //не обязательно для заполнения
                },

                // масса по документу
                new FormHelperField()
                {
                    Path="WEIGHT_DOK",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=EditWeightDok,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                        //{ FormHelperField.FieldFilterRef.Required, null },
                    },

                },
                // кип по документу
                new FormHelperField()
                {
                    Path="QUANTITY_BAL_DOC",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=EditQuantityBalDoc,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                // масса брутто
                new FormHelperField()
                {
                    Path="WEIGHT_FULL",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=EditWeightFull,

                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    //{ FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                // масса нетто
                new FormHelperField()
                {
                    Path="WEIGHT_EMPTY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=EditWeightEmpty,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    //{ FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                // масса поддонов
                new FormHelperField()
                {
                    Path="WEIGHT_PODDON",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=EditWeightPoddon,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    //{ FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                // влажность
                new FormHelperField()
                {
                    Path="HUMIDITY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=EditHumidity,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                // загрязнение
                new FormHelperField()
                {
                    Path="CONTAMINATION",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=EditContamination,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                // принято на склад
                new FormHelperField()
                {
                    Path="WEIGHT_FACT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=EditWeightFact,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                // на хранении
                new FormHelperField()
                {
                    Path="WEIGHT_RETURNING",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=LabelWeightReturning,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                // категория макулатуры
                new FormHelperField()
                {
                    Path="ID_CATEGORY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ScrapCategory,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    { FormHelperField.FieldFilterRef.Required, null },
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
            // очищаем данные формы
            ClearForm();
            Form.SetDefaults();
        }

        /// <summary>
        ////очистка полей формы
        /// </summary>
        private void ClearForm()
        {
            SelfshipInfo.Background = HColor.White.ToBrush();
            SelfshipInfo.Foreground = HColor.BlackFG.ToBrush();
            SelfshipInfo.Text = string.Empty;
            LabelCountBale.Content = "0";
            LabelScrabBale.Content = "";
            LabelQtyReturn.Content = "0";
            LabelScrabBaleReturn.Content = "";
            EditName.Text = "";
            EditNumDoc.Text = "0";
            EditWeightFact.Text = "0";
            LabelWeightReturning.Text = "0";
            ErrorTxt.Text = "";

            SaveButton.IsEnabled = true;
            // PostponeCarButton.IsEnabled = false;    // отложить
            // ReturningCarButton.IsEnabled = false;   // возврат
            PrihodCloseButton.IsEnabled = false;    // оприходовать
            OnSkladBitBtn.IsEnabled = false;        // принято на склад
            AktPrintButton.IsEnabled = false;       // акт приемки
            ActReturningButton.IsEnabled = false;   // акт возврата
            DeleteBaleButton.IsEnabled = false;     // удалить кипу
            ButtonAddBale.IsEnabled = false;        // добавить кипу
        }

        public void Edit()
        {
            FirstRun = true;
            if (IdScrap > 0)
                FrameTitle = $"Редактирование машины, ИД {IdScrap}";
            else
                FrameTitle = $"Добавление машины";
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
           
        private void Mc5BRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!FirstRun)
                CategoryShow(0);
        }

        private void Mc6BRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!FirstRun)
                CategoryShow(30);
        }

        private void Mc8BRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!FirstRun)
                CategoryShow(60);
        }

        private void Mc11BRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!FirstRun)
                CategoryShow(20);
        }

        /// <summary>
        ///  меняем категорию макулатуры
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void ScrapCategory_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            WastepaperCelluloseQuality.IsEnabled = false;
            WastepaperDry.IsEnabled = false;
            // WastepaperCelluloseQuality.IsChecked = false;
            // WastepaperDry.IsChecked = false;
            BeforeConsumerFlag.IsEnabled = false;
            AfterConsumerFlag.IsEnabled = false;

            WastepaperShredding.IsEnabled = false;
            RollsFlag.IsEnabled = false;
            //   WastepaperShredding.IsChecked = false;
            //   RollsFlag.IsChecked = false;

            if ((ScrapCategory.SelectedItem.Key.ToInt() == 1)   // МС-5Б1 и вторичка
            || (ScrapCategory.SelectedItem.Key.ToInt() == 34))
            {
                WastepaperCelluloseQuality.IsEnabled = true;
                WastepaperDry.IsEnabled = true;
            }
            else
            if ((ScrapCategory.SelectedItem.Key.ToInt() == 21)  // ТетраПак
            || (ScrapCategory.SelectedItem.Key.ToInt() == 22)
            || (ScrapCategory.SelectedItem.Key.ToInt() == 23))
            {
                WastepaperShredding.IsEnabled = true;
                RollsFlag.IsEnabled = true;
                BeforeConsumerFlag.IsEnabled = true;
                AfterConsumerFlag.IsEnabled = true;
            }
        }

        /// <summary>
        ///  меняем дату накладной
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NaklprihDataSf_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (FirstRun == true)
                return;

            ChangeDatasf = true;
        }

        /// <summary>
        /// Загрузить список поставщиков макулатуры
        /// </summary>
        private void PostavshicShow()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "ScrapPaper");
            q.Request.SetParam("Action", "PostavshicSelect"); //2.

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
                        var list = dataSet.GetItemsList("ID_POST", "NAME");
                        Postavshic.Items = list;
                    }
                }
            }
        }

        /// <summary>
        /// Загрузить список категории макулатуры для выбранного вида
        /// </summary>
        private void CategoryShow(int category)
        {
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "ProductionPm");
                q.Request.SetParam("Object", "ScrapPaper");
                q.Request.SetParam("Action", "ScrapCategorySelect"); //4.
                q.Request.SetParam("ID_CATEGORY_PARENT", category.ToString()); // родительская категория

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
                            var list = dataSet.GetItemsList("ID_CATEGORY", "CATEGORY");
                            ScrapCategory.Items = list;

                            if (Mc5BRadioButton.IsChecked == true) // МС-5Б
                            {
                                if (ScrapCategoryOld5 != 0)
                                    ScrapCategory.SetSelectedItemByKey(ScrapCategoryOld5.ToString());
                                else
                                    ScrapCategory.SetSelectedItemByKey("2");
                            }
                            else
                            if (Mc6BRadioButton.IsChecked == true) // МС-6Б
                            {
                                if (ScrapCategoryOld6 != 0)
                                    ScrapCategory.SetSelectedItemByKey(ScrapCategoryOld6.ToString());
                                else
                                    ScrapCategory.SetSelectedItemByKey("32");
                            }
                            else
                            if (Mc8BRadioButton.IsChecked == true) // МС-8В
                            {
                                if (ScrapCategoryOld8 != 0)
                                    ScrapCategory.SetSelectedItemByKey(ScrapCategoryOld8.ToString());
                                else
                                    ScrapCategory.SetSelectedItemByKey("61");
                            }
                            else
                            if (Mc11BRadioButton.IsChecked == true) // МС-11В
                            {
                                if (ScrapCategoryOld11 != 0)
                                    ScrapCategory.SetSelectedItemByKey(ScrapCategoryOld11.ToString());
                                else
                                    ScrapCategory.SetSelectedItemByKey("23");
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        ///  сменили поставщика
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void Postavshic_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (FirstRun)
                return;

            DriverShow();

            ChangeFlag = true;
        }

        /// <summary>
        /// Загрузить список водителей (самовывоз) для выбранного поставщика
        /// </summary>
        private void DriverShow()
        {
            {
                var items = new Dictionary<string, string>();
                Driver.Clear();
                Driver.Items = items;

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "ProductionPm");
                q.Request.SetParam("Object", "ScrapPaper");
                q.Request.SetParam("Action", "DriverSelect"); //3.
                q.Request.SetParam("NNAKL", Nnakl.ToString());
                q.Request.SetParam("ID_POST", Postavshic.SelectedItem.Key.ToString());
                q.Request.SetParam("NAME_TS", EditName.Text.ToString());

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
                            var list = dataSet.GetItemsList("SURM_ID", "NAME");
                            Driver.Items = list;
                            Driver.SetSelectedItemByKey($"{SurmId}.0");

                            SelfshipInfo.Background = HColor.Yellow.ToBrush();
                            SelfshipInfo.Foreground = HColor.RedFG.ToBrush();
                            var str = "САМОВЫВОЗ, прикрепите водителя после разгрузки.";
                            if (SurmId > 0)
                            {
                                str = "САМОВЫВОЗ.";
                            }

                            SelfshipInfo.Text = str;
                            SelfshipInfo.Tag = 3;
                        }
                        else
                        {
                            SelfshipInfo.Background = HColor.GreenFG.ToBrush();
                            SelfshipInfo.Foreground = HColor.White.ToBrush();
                            SelfshipInfo.Text = "ПОСТАВЩИКОМ.";
                            SelfshipInfo.Tag = 1;
                        }

                        // получаем доступные категории поставляемой поставщиком макулатуры
                        if (ControlPostavshicFlag)
                        {
                            Mc5BRadioButton.IsEnabled = false;
                            Mc6BRadioButton.IsEnabled = false;
                            Mc8BRadioButton.IsEnabled = false;
                            Mc11BRadioButton.IsEnabled = false;
                        }
                        else
                        {
                            Mc5BRadioButton.Foreground = Brushes.Red;
                            Mc6BRadioButton.Foreground = Brushes.Red;
                            Mc8BRadioButton.Foreground = Brushes.Red;
                            Mc11BRadioButton.Foreground = Brushes.Red;
                        }

                        var dataSet2 = ListDataSet.Create(result, "ITEMS2");
                        if (dataSet2 != null && dataSet2.Items != null && dataSet2.Items.Count > 0)
                        {
                            foreach (Dictionary<string, string> row in dataSet2.Items)
                            {
                                var id_category = row.CheckGet("ID_CATEGORY").ToInt();

                                switch (id_category)
                                {
                                    case 0:
                                        if (ControlPostavshicFlag)
                                            Mc5BRadioButton.IsEnabled = true;
                                        else
                                            Mc5BRadioButton.Foreground = Brushes.Green;
                                        break;
                                    case 20:
                                        if (ControlPostavshicFlag)
                                            Mc11BRadioButton.IsEnabled = true;
                                        else
                                            Mc11BRadioButton.Foreground = Brushes.Green;

                                        break;
                                    case 30:
                                        if (ControlPostavshicFlag)
                                            Mc6BRadioButton.IsEnabled = true;
                                        else
                                            Mc6BRadioButton.Foreground = Brushes.Green;

                                        break;
                                    case 60:
                                        if (ControlPostavshicFlag)
                                            Mc8BRadioButton.IsEnabled = true;
                                        else
                                            Mc8BRadioButton.Foreground = Brushes.Green;

                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        ///  изменили название машины
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (FirstRun)
                return;

            ChangeFlag = true;
        }

        /// <summary>
        /// изменили вес по документам
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditWeightDok_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (FirstRun)
                return;

            // 18.03.2021 Торопцева изменила расчет фактического веса
            int fact = 0;
            if (!EditWeightFull.Text.IsNullOrEmpty()
             && EditWeightFull.Text.ToInt() != 0
            && !EditWeightEmpty.Text.IsNullOrEmpty()
            && EditWeightEmpty.Text.ToInt() != 0
            && !EditWeightPoddon.Text.IsNullOrEmpty())
            {
                fact = EditWeightFull.Text.ToInt() - EditWeightEmpty.Text.ToInt() - EditWeightPoddon.Text.ToInt() - WeigthReturn;
            }
            LabelWeightFact.Content = $"{fact} от {DtFull}";

            if (IdStatus == 4)
            {
                if (!EditWeightEmpty.Text.IsNullOrEmpty())
                {
                    PrihodCloseButton.IsEnabled = true;
                }
                else
                    PrihodCloseButton.IsEnabled = false;
            }
            else
                PrihodCloseButton.IsEnabled = false;

            ChangeFlag = true;
        }

        /// <summary>
        ///  нажали кнопку "Принять на склад"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSkladBitBtn_Click(object sender, RoutedEventArgs e)
        {
            PrihodOnSklad();
        }

        /// <summary>
        ///  расчет фактически принятой на склад партии макулатуры
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrihodOnSklad()
        {
            var errorMsg = string.Empty;
            bool resume = true;

            ErrorTxt.Text = errorMsg;
            EditWeightFact.Text = "0";

            if (EditWeightDok.Text.IsNullOrEmpty())
            {
                errorMsg = "Заполните массу по документу";
                resume = false;
            }

            if (resume)
            {
                // Это ВИП поставщики макулатуры, верим им
                if (
                    Postavshic.SelectedItem.Key.ToInt() == 6801     //УПАКОВОЧНЫЕ СИСТЕМЫ АО
                 || Postavshic.SelectedItem.Key.ToInt() == 7762     //ТАНДЕР АО
                 || Postavshic.SelectedItem.Key.ToInt() == 8083     //АРХБУМ АО
                 || Postavshic.SelectedItem.Key.ToInt() == 7914     //ПОЛИГРАФИЯ-СЛАВЯНКА ООО 
                 || Postavshic.SelectedItem.Key.ToInt() == 8149     //ПЭКЭДЖИНГ КУБАНЬ ЗАО
                 || Postavshic.SelectedItem.Key.ToInt() == 8269     //VIPA LAUSANNE SA ООО
                 || Postavshic.SelectedItem.Key.ToInt() == 8268     //Глобал Логистикс ООО
                 || Postavshic.SelectedItem.Key.ToInt() == 10555    //ДИКСИ ЮГ АО    
                 || Postavshic.SelectedItem.Key.ToInt() == 10708    //АГРОТОРГ ООО
                 || Postavshic.SelectedItem.Key.ToInt() == 11365    //СЛАДКАЯ ЖИЗНЬ Н.Н. ООО
                 || Postavshic.SelectedItem.Key.ToInt() == 11372    //КОПЕЙКА-МОСКВА ООО
                 || Postavshic.SelectedItem.Key.ToInt() == 12471    //Л-ПАК КАШИРА ООО
                 || Postavshic.SelectedItem.Key.ToInt() == 10974    //АГРОАСПЕКТ ООО
                 )
                {
                    EditWeightFact.Text = EditWeightDok.Text;
                }
                else
                {
                    if (!EditWeightFull.Text.IsNullOrEmpty()
                     && !EditHumidity.Text.IsNullOrEmpty()
                     && !EditContamination.Text.IsNullOrEmpty())
                    {

                        int editWeightEmpty = 0;
                        if (!EditWeightFull.Text.IsNullOrEmpty()
                         && !EditWeightEmpty.Text.IsNullOrEmpty()
                         && !EditWeightPoddon.Text.IsNullOrEmpty())
                        {
                            var fact = EditWeightFull.Text.ToInt() - EditWeightEmpty.Text.ToInt() - EditWeightPoddon.Text.ToInt() - WeigthReturn;
                            editWeightEmpty = EditWeightFull.Text.ToInt() - fact;
                        }

                        var p = new Dictionary<string, string>();
                        p.CheckAdd("V_WEIGHT_FULL", EditWeightFull.Text);
                        p.CheckAdd("V_WEIGHT_EMPTY", editWeightEmpty.ToString());
                        p.CheckAdd("V_HUMIDITY", EditHumidity.Text);
                        p.CheckAdd("V_CONTAMINATION", EditContamination.Text);

                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "ProductionPm");
                        q.Request.SetParam("Object", "ScrapPaper");
                        q.Request.SetParam("Action", "GetWeightFact");
                        q.Request.SetParams(p);
                        q.DoQuery();

                        if (q.Answer.Status == 0)
                        {
                            var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                            if (result != null)
                            {
                                // получили вес нетто с учетом влажности и загрязнения
                                var ds = ListDataSet.Create(result, "ITEMS");
                                if (ds.Items.Count > 0)
                                {
                                    var first = ds.Items.First();
                                    if (first != null)
                                    {
                                        var ves = first.CheckGet("NETTO").ToInt();

                                        if ((ves <= EditWeightDok.Text.ToInt() + 100)
                                            && (ves >= EditWeightDok.Text.ToInt() - 100)
                                            && EditWeightDok.Text.ToInt() != 0
                                            && EditHumidity.Text.ToInt() == 12
                                            && EditContamination.Text.ToInt() == 1
                                            )
                                        {
                                            EditWeightFact.Text = EditWeightDok.Text;
                                        }
                                        else
                                        {
                                            EditWeightFact.Text = ves.ToString();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                ErrorTxt.Text = errorMsg;
            }
        }


        /// <summary>
        /// Проверка данных перед записью в БД
        /// </summary>
        public void Save()
        {
            if (Form.Validate())
            {
                bool resume = true;
                var v = Form.GetValues();
                string errorMsg = "";

                if (resume)
                {
                    if (IdScrap == 0)
                    {
                        if (EditName.Text.IsNullOrEmpty())
                        {
                            errorMsg = "Введите название машины.";
                            resume = false;
                        }

                        if (Postavshic.SelectedItem.Key.ToString().IsNullOrEmpty())
                        {
                            errorMsg = "Выберите название поставщика.";
                            resume = false;
                        }

                        if (ScrapCategory.SelectedItem.Key.ToString().IsNullOrEmpty())
                        {
                            errorMsg = "Выберите категорию макулатуры.";
                            resume = false;
                        }
                    }
                    else
                    {
                        GetGurrentStatus();  // получаем текущий статус машины
                        if (IdStatus != -1)
                        {
                            if (IdStatus != OldIdStatus)
                            {
                                errorMsg = "Статус машины изменился. Нажмите отмена и откройте форму заново.";
                                resume = false;
                            }
                        }
                        else
                        {
                            errorMsg = "Не получен текущий статус машины.";
                            resume = false;
                        }
                    }

                    if (InputControlFlag2RadioButton.IsChecked == true && Sklad != "ЛН")
                    {
                        errorMsg = "Если выбрано списание машины на ленту, должен быть указан ряд ЛН";
                        resume = false;
                    }

                    if (IdStatus == 5
                    // && ChangeFlag == true
                    && Central.User.Login != "greshnyh_ni"
                    && Central.User.Login != "fedyanina_ev")
                    {
                        errorMsg = "Машина проведена. Любые изменения недопустимы.";
                        resume = false;
                    }
                }

                if (resume)
                {
                    UpdateRecord(true);
                }
                else
                {
                    //   Form.SetStatus(errorMsg, 1);
                    var dialog = new DialogWindow(errorMsg, "Внимание", "", DialogWindowButtons.OK);
                    dialog.ShowDialog();
                }
            }
        }

        /// <summary>
        /// Добавление/Изменение записи 
        /// </summary>
        public async void UpdateRecord(bool flagClose)
        {
            var resume = true;
            var q = new LPackClientQuery();
            var rez = true;
            var empl_id = Central.User.EmployeeId;
            var scrap_category = ScrapCategory.SelectedItem.Key.ToInt();
            var input_control_flag = 0;

            if (InputControlFlag1RadioButton.IsChecked == true)
            {
                input_control_flag = 1;
            }
            else
            if (InputControlFlag2RadioButton.IsChecked == true)
            {
                input_control_flag = 2;
            }

            var p = new Dictionary<string, string>();
            {

                p.CheckAdd("WEIGHT_DOK", EditWeightDok.Text.ToString());
                p.CheckAdd("WEIGHT_FULL", EditWeightFull.Text.ToString());
                p.CheckAdd("WEIGHT_EMPTY", EditWeightEmpty.Text.ToString());
                p.CheckAdd("WEIGHT_PODDON", EditWeightPoddon.Text.ToString());
                p.CheckAdd("WEIGHT_FACT", EditWeightFact.Text.ToString());

                p.CheckAdd("NAME", EditName.Text.ToString());
                p.CheckAdd("ID_CATEGORY", ScrapCategory.SelectedItem.Key.ToInt().ToString());
                p.CheckAdd("HUMIDITY", EditHumidity.Text.ToString());
                p.CheckAdd("CONTAMINATION", EditContamination.Text.ToString());
                p.CheckAdd("ID_ST", IdSt.ToString());
                p.CheckAdd("ID_POST", Postavshic.SelectedItem.Key.ToInt().ToString());

                p.CheckAdd("EMPL_ID", empl_id.ToString());
                p.CheckAdd("NUM_DOC", EditNumDoc.Text.ToString());
                p.CheckAdd("QUANTITY_BAL_DOC", EditQuantityBalDoc.Text.ToString());

                if (scrap_category > 20 && scrap_category < 30)
                    p.CheckAdd("PAPER_TETRAPAK_FLAG", "1");
                else
                    p.CheckAdd("PAPER_TETRAPAK_FLAG", "0");

                p.CheckAdd("INPUT_CONTROL_FLAG", input_control_flag.ToString());

                if (WastepaperCelluloseQuality.IsChecked == true)
                    p.CheckAdd("WASTEPAPER_CELLULOSE_QUALITY", "1");
                else
                    p.CheckAdd("WASTEPAPER_CELLULOSE_QUALITY", "0");

                if (WastepaperDry.IsChecked == true)
                    p.CheckAdd("WASTEPAPER_DRY", "1");
                else
                    p.CheckAdd("WASTEPAPER_DRY", "0");

                if (WastepaperShredding.IsChecked == true)
                    p.CheckAdd("WASTEPAPER_SHREDDING", "1");
                else
                    p.CheckAdd("WASTEPAPER_SHREDDING", "0");

                if (RollsFlag.IsChecked == true)
                    p.CheckAdd("ROLLS_FLAG", "1");
                else
                    p.CheckAdd("ROLLS_FLAG", "0");

                if (BeforeConsumerFlag.IsChecked == true)
                    p.CheckAdd("BEFORE_CONSUMER_FLAG", "0");
                else
                    p.CheckAdd("BEFORE_CONSUMER_FLAG", "1");
            }

            if (IdScrap == 0)
            {   // новая машина
                q.Request.SetParam("Module", "ProductionPm");
                q.Request.SetParam("Object", "ScrapPaper");
                q.Request.SetParam("Action", "ScrapTransportInsert");
                p.CheckAdd("DT_FULL", DtFull.ToString());

            }
            else
            {   // старая машина
                q.Request.SetParam("Module", "ProductionPm");
                q.Request.SetParam("Object", "ScrapPaper");
                q.Request.SetParam("Action", "ScrapTransportUpdate");

                p.CheckAdd("ID_SCRAP", IdScrap.ToString());

                var newAutoInput = 0;
                // Обновляем поле типа заполнения веса
                if (OldAutoInput == 0)
                {
                    switch (IsAuto)
                    {
                        case 1:               // автом. брутто
                            newAutoInput = 0;
                            break;

                        case 2:               // автом. нетто  
                            if (OldAutoInput == 0)
                                newAutoInput = 0;
                            else
                                newAutoInput = 1;
                            break;

                        case 3:
                            newAutoInput = 1;
                            break;

                        case 4:
                            if (OldAutoInput == 0)
                                newAutoInput = 2;
                            else
                                newAutoInput = 3;

                            break;

                        default:
                            break;
                    }

                }
                else
                    newAutoInput = OldAutoInput;

                p.CheckAdd("AUTO_INPUT", newAutoInput.ToString());

                if ((IdStatus == 1) && EditWeightFull.Text.ToInt() > 0)
                {
                    IdStatus = 2;
                    var today = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                    DtFull = today;
                }

                p.CheckAdd("DT_FULL", DtFull.ToString());
                p.CheckAdd("ID_STATUS", IdStatus.ToString());

            }

            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                if ((IdCategoryOld != ScrapCategory.SelectedItem.Key.ToInt()) && IdStatus == 5)
                { // сменили марку макулатуры после оприходования на склад
                    string message = "";
                    message += $"Вы сменили категорию макулатуры после оприходования";
                    message += $"\nвсех (или части) кип на склад.";
                    message += $"\n\nНЕОБХОДИМО провести исправления прихода машины.";
                    var dialog = new DialogWindow(message, Central.ProgramTitle, "", DialogWindowButtons.OK);
                    var confirmResult = dialog.ShowDialog();
                }

                if (WeigthFactOld != EditWeightFact.Text.ToInt() && IdStatus == 5)
                {   // изменился вес принятой макулатуры на склад
                    string message = "";
                    message += $"Изменился вес принятых кип на склад.";
                    message += $"\n\nНЕОБХОДИМО провести исправления прихода машины.";
                    var dialog = new DialogWindow(message, Central.ProgramTitle, "", DialogWindowButtons.OK);
                    var confirmResult = dialog.ShowDialog();
                }

                if ((Postavshic.SelectedItem.Key.ToInt() != IdPostOld) && IdStatus == 4)
                {   // машина разгружена и изменился поставщик макулатуры
                    string message = "";
                    message += $"Изменился поставщик макулатуры.";
                    message += $"\n\nИсправить номер Акта?.";
                    var dialog = new DialogWindow(message, Central.ProgramTitle, "", DialogWindowButtons.OKCancel);
                    var confirmResult = dialog.ShowDialog();
                    if (confirmResult == true)
                    {
                        rez = SetActScrap();  // присваиваем номер акта
                    }
                }

                if (rez == true)
                {
                    if (ChangeDatasf == true)
                    {   // если менялась дата счета фактуры, тогда изменяем её в таблице накладных
                        rez = NaklprihUpdateDatasf();
                    }
                }

                if (rez == true)
                {
                    if (Nnakl > 0 && (OldSurmId != Driver.SelectedItem.Key.ToInt()))
                    {
                        rez = NaklprihUpdateSurmId(); // привязываем водителя к накладной
                    }
                }

                if (rez == true && flagClose == true)
                {
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
            else
            {
                Form.SetStatus(q.Answer.Error.Message, 1);
            }
        }

        /// <summary>
        /// получаем текущий статус машины 
        /// </summary>
        public void GetGurrentStatus()
        {
            var resume = true;
            IdStatus = -1;

            var q = new LPackClientQuery();

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_SCRAP", IdScrap.ToString());
            }

            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "ScrapPaper");
            q.Request.SetParam("Action", "ScrapTransportSelectIdStatus");

            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            //await Task.Run(() =>
            //{
            //    q.DoQuery();
            //});

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    // данные по машине
                    var ds = ListDataSet.Create(result, "ITEMS");

                    if (ds.Items.Count > 0)
                    {
                        var first = ds.Items.First();
                        if (first != null) // есть запись по машине
                        {
                            IdStatus = first.CheckGet("ID_STATUS").ToInt();
                        }
                    }
                }
            }
            else
            {
                Form.SetStatus(q.Answer.Error.Message, 1);
            }
        }

        /// <summary>
        /// Присваиваем Акт приемки/загрузки машины
        /// </summary>
        public bool SetActScrap()
        {
            var resume = true;
            var q = new LPackClientQuery();

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("I_ID_SCRAP", IdScrap.ToString());
            }

            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "ScrapPaper");
            q.Request.SetParam("Action", "SetScrapTransportAktNum");

            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status != 0)
            {
                Form.SetStatus(q.Answer.Error.Message, 1);
                resume = false;
            }
            return resume;
        }

        /// <summary>
        /// обновляем дату счет-фактуры 
        /// </summary>
        public bool NaklprihUpdateDatasf()
        {
            var resume = true;
            var q = new LPackClientQuery();

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("NNAKL", Nnakl.ToString());
                p.CheckAdd("DATASF", NaklprihDataSf.Text.ToDateTime().ToString());
            }

            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "ScrapPaper");
            q.Request.SetParam("Action", "NaklprihUpdateDatasf");

            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            //await Task.Run(() =>
            //{
            //    q.DoQuery();
            //});

            if (q.Answer.Status != 0)
            {
                Form.SetStatus(q.Answer.Error.Message, 1);
                resume = false;
            }

            return resume;
        }

        /// <summary>
        ///  обновляем водителя в накладной
        /// </summary>
        public bool NaklprihUpdateSurmId()
        {
            var resume = true;
            var q = new LPackClientQuery();

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("SURM_ID", Driver.SelectedItem.Key.ToInt().ToString());
                p.CheckAdd("NNAKL", Nnakl.ToString());
            }
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "ScrapPaper");
            q.Request.SetParam("Action", "NaklprihUpdateSurmId");

            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();
            //await Task.Run(() =>
            //{
            //    q.DoQuery();
            //});

            if (q.Answer.Status != 0)
            {
                Form.SetStatus(q.Answer.Error.Message, 1);
                resume = false;
            }

            return resume;
        }

        /// <summary>
        ///  нажали кнопку Отложить
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PostponeCarButton_Click(object sender, RoutedEventArgs e)
        {
            string message = "";
            if (IdStatus == 4)
            {
                message += $"Вы действительно хотите перенести машину в отложенную?";
                message += $"\n\nРазгруженные кипы будут не доступны для использования.";
                var dialog = new DialogWindow(message, "Внимание", "", DialogWindowButtons.OKCancel);
                var confirmResult = dialog.ShowDialog();
                if (confirmResult == true)
                {
                    var rez = ScrapTransportUpdateIdStatus(6);
                    if (rez == true)
                    {
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
            else if (IdStatus == 6)
            {
                message += $"Вы действительно хотите вернуть машину в работу?";
                var dialog = new DialogWindow(message, "Внимание", "", DialogWindowButtons.OKCancel);
                var confirmResult = dialog.ShowDialog();
                if (confirmResult == true)
                {
                    var rez = ScrapTransportUpdateIdStatus(4);
                    if (rez == true)
                    {
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
        }

        /// <summary>
        ///  обновляем статус машины
        /// </summary>
        /// <returns></returns>
        public bool ScrapTransportUpdateIdStatus(int status)
        {
            var resume = true;
            var q = new LPackClientQuery();

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_SCRAP", IdScrap.ToString());
                p.CheckAdd("ID_STATUS", status.ToString());
            }
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "ScrapPaper");
            q.Request.SetParam("Action", "ScrapTransportUpdateIdStatus");

            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status != 0)
            {
                Form.SetStatus(q.Answer.Error.Message, 1);
                resume = false;
            }

            return resume;
        }


        /// <summary>
        /// получаем количество кип из прихода для данной машины
        /// </summary>
        /// <returns></returns>
        public bool ScrapTransportGetCountBale()
        {
            var resume = true;
            var q = new LPackClientQuery();

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_SCRAP", IdScrap.ToString());
            }
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "ScrapPaper");
            q.Request.SetParam("Action", "ScrapBaleSelectCount");

            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    // получили вес нетто с учетом влажности и загрязнения
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds.Items.Count > 0)
                    {
                        var first = ds.Items.First();
                        if (first != null)
                        {
                            LabelCountBale.Content = first.CheckGet("CNT").ToInt().ToString();
                            LabelQtyReturn.Content = first.CheckGet("QTY_RETURNING").ToInt().ToString();
                        }
                    }
                }
            }
            else
            {
                Form.SetStatus(q.Answer.Error.Message, 1);
                resume = false;
            }

            return resume;
        }


        /// <summary>
        /// Добавляем одну кипу в приход машины 
        /// </summary>
        /// <returns></returns>
        public bool ScrapTransportAddBale()
        {
            var resume = true;
            var q = new LPackClientQuery();

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_SCRAP", IdScrap.ToString());
                p.CheckAdd("ID2", Id2.ToString());
                p.CheckAdd("SKLAD", Sklad.ToString());
                p.CheckAdd("NUM_PLACE", NumPlace.ToString());
                p.CheckAdd("ID_ST", IdSt.ToString());
                p.CheckAdd("WMTA_ID", WmtaId.ToString());

            }
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "ScrapPaper");
            q.Request.SetParam("Action", "ScrapTransportAddBale");

            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status != 0)
            {
                Form.SetStatus(q.Answer.Error.Message, 1);
                resume = false;
            }

            return resume;
        }

        /// <summary>
        /// удаляем одну кипу из прихода 
        /// </summary>
        /// <returns></returns>
        public bool ScrapTransportDeleteBale()
        {
            var resume = true;
            var q = new LPackClientQuery();

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_SCRAP", IdScrap.ToString());

            }
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "ScrapPaper");
            q.Request.SetParam("Action", "ScrapTransportDeleteBale");

            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status != 0)
            {
                Form.SetStatus(q.Answer.Error.Message, 1);
                resume = false;
            }

            return resume;
        }

        /// <summary>
        /// удалить одну кипу из прихода 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteBaleButton_Click(object sender, RoutedEventArgs e)
        {
            if (LabelCountBale.Content.ToInt() > 0 && IdStatus < 5)
            { // количество разгруженных кип >0 и машина еще не оприходована
                var message = "";
                message += $"Вы точно хотите удалить ОДНУ кипу из этой машины?";
                var dialog = new DialogWindow(message, "Внимание", "", DialogWindowButtons.OKCancel);

                var confirmResult = dialog.ShowDialog();
                if (confirmResult == true)
                {
                    var rez = ScrapTransportDeleteBale();
                    if (rez == true)
                    {
                        ScrapTransportGetCountBale(); // обновляем текущее значение разгруженных кип
                    }
                }
            }
        }

        /// <summary>
        /// добавить одну кипу в приход
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonAddBale_Click(object sender, RoutedEventArgs e)
        {
            if (LabelCountBale.Content.ToInt() > 0 && IdStatus < 5)
            { // количество разгруженных кип >0 и машина еще не оприходована
                var message = "";
                message += $"Вы точно хотите добавить ОДНУ кипу в эту машину?";
                var dialog = new DialogWindow(message, "Внимание", "", DialogWindowButtons.OKCancel);

                var confirmResult = dialog.ShowDialog();
                if (confirmResult == true)
                {
                    var rez = ScrapTransportAddBale();
                    if (rez == true)
                    {
                        ScrapTransportGetCountBale(); // обновляем текущее значение разгруженных кип
                    }
                }
            }

        }

        /// <summary>
        /// обновляем данные по кипам в приходе на основани данных по машине
        /// </summary>
        /// <returns></returns>
        public async Task<bool> PrihodUpdateBale()
        {
            var resume = true;
            var q = new LPackClientQuery();

            var idProd = "23"; // БумПак

            if (Mc5BRadioButton.IsChecked == false)
                idProd = "2"; // Л-Пак

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("NNAKL", Nnakl.ToString());
                p.CheckAdd("DELIVERY", SelfshipInfo.Tag.ToString());
                p.CheckAdd("ID_PROD", idProd);

                p.CheckAdd("ID_SCRAP", IdScrap.ToString());
                p.CheckAdd("ID_CATEGORY", ScrapCategory.SelectedItem.Key.ToInt().ToString());
                p.CheckAdd("STATUS_RETURNING", StatusReturning.ToString());

            }
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "ScrapPaper");
            q.Request.SetParam("Action", "PrihodUpdateBale");

            q.Request.SetParams(p);

            q.Request.Timeout = 300000; // Central.Parameters.RequestGridTimeout;
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
            else
            {
                if (resume)
                {
                    if (IdStatus == 4)
                    { // если машина разгружалась сразу на ленту, предлагаем её сразу списать
                        if (InputControlFlag2RadioButton.IsChecked == true)
                        {
                            var message = "";
                            message += $"Вы хотите сразу списать макулатуру";
                            message += $"\nиз машины в производство?";
                            var dialog = new DialogWindow(message, "Внимание", "", DialogWindowButtons.OKCancel);
                            var confirmResult = dialog.ShowDialog();
                            if (confirmResult == true)
                            {
                                var result = await RashodAllBale(); // окончание оприходования и списания машины
                                if (result == -1)
                                {
                                    ErrorTxt.Text = "Списание  всех кип не завершено.";
                                    resume = false;
                                }
                            }
                        }
                        else
                        if (InputControlFlag1RadioButton.IsChecked == true)
                        {  // входной контроль 3-х кип

                            var result = await RashodAllBale();
                            if (result == -1)
                            {
                                ErrorTxt.Text = "Списание 3-х кип не завершено.";
                                resume = false;
                            }
                        }
                    }
                }
            }

            return resume;
        }

        /// <summary>
        /// списание в производство всех кип из ряда ЛН 
        /// </summary>
        /// <returns></returns>
        //public async void RashodAllBale()
        public async Task<int> RashodAllBale()
        {
            RashodBaleSuccessfully = false;
            var q = new LPackClientQuery();

            if (InputControlFlag == 2)
                SplashControl.Message = "Ждите. Идет списание всех кип.";
            else if (InputControlFlag == 1)
                SplashControl.Message = "Ждите. Идет списание 3-х кип.";

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_SCRAP", IdScrap.ToString());
                p.CheckAdd("SKLAD", Sklad.ToString());
                p.CheckAdd("NUM_PLACE", NumPlace.ToString());
                p.CheckAdd("STATUS_RETURNING", StatusReturning.ToString());
                p.CheckAdd("INPUT_CONTROL_FLAG", InputControlFlag.ToString());
            }

            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "ScrapPaper");
            q.Request.SetParam("Action", "RashodAllBale");

            q.Request.SetParams(p);

            q.Request.Timeout = 300000;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });


            if (q.Answer.Status != 0)
            {
                //Form.SetStatus(q.Answer.Error.Message, 1);
                return -1;
            }
            else
            {
                RashodBaleSuccessfully = true;
                SetSplash(false);
                return 0;
            }
        }


        /// <summary>
        ///  отображение окна долгих операций
        /// </summary>
        /// <param name="inProgressFlag"></param>
        /// <param name="msg"></param>
        private void SetSplash(bool inProgressFlag, string msg = "")
        {
            SplashControl.Visible = inProgressFlag;
            SplashControl.Message = msg;
        }

        /// <summary>
        /// Оприходование машины с макулатурой 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void PrihodCloseButton_Click(object sender, RoutedEventArgs e)
        {
            var resume = true;
            int oldStatus = -1;
            var message = "";

            SaveButton.IsEnabled = false;
            CancelButton.IsEnabled = true;
            PostponeCarButton.IsEnabled = false;
            ReturningCarButton.IsEnabled = false;
            PrihodCloseButton.IsEnabled = false;
            AktPrintButton.IsEnabled = false;
            ActReturningButton.IsEnabled = false;

            GetGurrentStatus(); // получаем текущий статус машины
            if (IdStatus != -1)
            {
                oldStatus = IdStatus;

                // проверки на всякий случай

                if (IdStatus < 4)
                { // машина еще не разгружена полностью
                    message = "";
                    message += $"Сначала закройте разгрузку.";
                    message += $"\n\nНельзя оприходовать машину не закрыв разгрузку!";
                    var dialog = new DialogWindow(message, "Внимание", "", DialogWindowButtons.OK);
                    var confirmResult = dialog.ShowDialog();
                    resume = false;
                }
                else
                if (IdStatus == 5)
                { // машина взвешена пустая и уехала
                    message = "";
                    message += $"Машина проведена.";
                    message += $"\n\nЛюбые изменения недопустимы.";
                    var dialog = new DialogWindow(message, "Внимание", "", DialogWindowButtons.OK);
                    var confirmResult = dialog.ShowDialog();
                    resume = false;
                }

                if (resume)
                {
                    // проверяем , если бумага тетрпак, тогда должная быть указана категория
                    if (PaperTetrapakFlag == 1 && IdCategoryParent != 20)
                    {
                        message = "";
                        message += $"Необходимо заполнить категорию.";
                        message += $"\nэта бумага от ТП";
                        var dialog = new DialogWindow(message, "Внимание", "", DialogWindowButtons.OK);
                        var confirmResult = dialog.ShowDialog();
                        resume = false;
                    }
                }

                if (resume)
                {
                    if ((EditQuantityBalDoc.Text.ToInt() > 0)
                        && (EditQuantityBalDoc.Text.ToInt() != LabelCountBale.Content.ToInt()))
                    {
                        message = "";
                        message += $"Количество кип по документам не соответствует";
                        message += $"\nколичеству разгруженных кип.";
                        message += $"\n\nВсё равно завершить взвешивание?";
                        var dialog = new DialogWindow(message, "Внимание", "", DialogWindowButtons.OKCancel);
                        var confirmResult = dialog.ShowDialog();
                        if (confirmResult != true)
                            resume = false;
                    }
                }

                if (resume)
                {
                    message = "";
                    message += $"Вы действительно хотите завершить";
                    message += $"\nвзвешивание машины?";
                    var dialog = new DialogWindow(message, "Внимание", "", DialogWindowButtons.OKCancel);
                    var confirmResult = dialog.ShowDialog();
                    if (confirmResult != true)
                        resume = false;
                }

                if (resume)
                {
                    SetSplash(true, "Ждите. Идет оприходование машины");

                    PrihodOnSklad();                     //принудительно рассчитываем принятый вес на склад с учетов веса кип на ответ. хранении
                    UpdateRecord(false);                 //сохраняем данные по машине
                    resume = await PrihodUpdateBale();   //оприходуем машину и списываем (при необходимости кипы в производство)
                }

                if (resume)
                {   // окончание оприходования машины

                    message = "";
                    message += $"Машина оприходована успешно.";

                    var dialog = new DialogWindow(message, "Внимание", "", DialogWindowButtons.OK);
                    var confirmResult = dialog.ShowDialog();

                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverGroup = "Production",
                        ReceiverName = ReceiverName,
                        SenderName = ControlName,
                        Action = "RefreshScrapTransportWeightGrid",
                    });

                    Close();
                }
                else
                {
                    SetSplash(false);
                    ErrorTxt.Text = "Оприходовании кип не завершено.";
                }
            }
            else
            {
                ErrorTxt.Text = "Ошибка получения статуса машины. Нажмите отмена для выхода.";
                resume = false;
            }
        }

        private void InputControlFlag0RadioButton_Click(object sender, RoutedEventArgs e)
        {
            if (FirstRun)
                return;

            InputControlFlag = 0;
        }

        private void InputControlFlag1RadioButton_Click(object sender, RoutedEventArgs e)
        {
            if (FirstRun)
                return;

            InputControlFlag = 1;

        }

        private void InputControlFlag2RadioButton_Click(object sender, RoutedEventArgs e)
        {
            if (FirstRun)
                return;

            InputControlFlag = 2;

        }

        /// <summary>
        /// возврат/ответ. хранение кип с макулатурой 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReturningCarButton_Click(object sender, RoutedEventArgs e)
        {
            var scrapTransportReturningRecord = new ScrapTransportReturning(Values as Dictionary<string, string>);
            scrapTransportReturningRecord.ReceiverName = ControlName;
            scrapTransportReturningRecord.IdSt = IdSt;
            scrapTransportReturningRecord.StatusReturning = StatusReturning;

            scrapTransportReturningRecord.Edit();

        }

        /// <summary>
        /// Формируем Акт приемки макулатуры/ТетраПак
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AktPrintButton_Click(object sender, RoutedEventArgs e)
        {
            if (Mc11BRadioButton.IsChecked == true)
                ActToExcel(2);  // формируем Акт приемки Тетра Пак
            else
                ActToExcel(1);  // формируем Акт приемки макулатуры
        }

        /// <summary>
        /// Формируем Акт возврата (прием на ответ. хранение кип)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ActReturningButton_Click(object sender, RoutedEventArgs e)
        {
            ActToExcel(3);
        }

        /// <summary>
        /// Формирование Актов приемки (в Excel)
        /// </summary>
        private async void ActToExcel(int type)
        {
            var items = new Dictionary<string, string>();

            var list = new List<Dictionary<string, string>>();

            SetSplash(true, "Идет формирование документа ...");

            var dt = DtFull.ToDateTime().ToString("dd.MM.yyyy");

            var fact = "";

            fact = LabelWeightFact.Content.ToString();
            fact = fact.Substring(0, 5);

            double e26 = 0;
            double e27 = 0;

            double v1 = 100 - EditHumidity.Text.ToInt();
            double v2 = v1 / 88;
            double v3 = v2 * fact.ToInt();
            e26 = v3.ToInt();
            e27 = (e26 * (EditContamination.Text.ToInt() - 1)/100);
                     

            if (type == 1)
            {
                items.Add("EDIT_NUM_DOC", EditNumDoc.Text.ToString());
                items.Add("NAKLPRIH_DATA_SF", NaklprihDataSf.Text.ToString());
                items.Add("POSTAVSHIC", Postavshic.SelectedItem.Value.ToString());
                items.Add("NAME", EditName.Text);
                if (IdCategory == 33)
                    items.Add("SCRAP_NAME", "Макулатура группы Б - среднего качества, код ОКПД2 38.32.32.200");
                else
                    items.Add("SCRAP_NAME", $"Макулатура бытовая, марка {ScrapCategory.SelectedItem.Value}");

                items.Add("WEIGHT_DOK", EditWeightDok.Text.ToString());
                items.Add("WEIGHT_FULL", EditWeightFull.Text.ToString());
                items.Add("WEIGHT_EMPTY", EditWeightEmpty.Text.ToString());
                items.Add("WEIGHT_PODDON", EditWeightPoddon.Text.ToString());
                items.Add("WEIGHT_FACT", fact);
                items.Add("NAME_PROD", NameProd.ToString());
                items.Add("NUM_AKT", ActPrihodStr.ToString());
                items.Add("DT_FULL", dt.ToString());

                items.Add("HUMIDITY", EditHumidity.Text.ToString());
                items.Add("CONTAMINATION", EditContamination.Text.ToString());
                items.Add("SCRAP_CATEGORY", ScrapCategory.SelectedItem.Value.ToString());
                items.Add("EDIT_WEIGHT_FACT", EditWeightFact.Text.ToString());
                items.Add("E26", e26.ToString());
                items.Add("E27", e27.ToString());
            } else
            if (type == 2)
            {
                items.Add("EDIT_NUM_DOC", EditNumDoc.Text.ToString());
                items.Add("NAKLPRIH_DATA_SF", NaklprihDataSf.Text.ToString());
                items.Add("POSTAVSHIC", Postavshic.SelectedItem.Value.ToString());
                items.Add("NAME", EditName.Text);
                items.Add("WEIGHT_DOK", EditWeightDok.Text.ToString());
                items.Add("WEIGHT_FULL", EditWeightFull.Text.ToString());
                items.Add("WEIGHT_EMPTY", EditWeightEmpty.Text.ToString());
                items.Add("WEIGHT_PODDON", EditWeightPoddon.Text.ToString());
                items.Add("WEIGHT_FACT", fact);
                items.Add("NUM_AKT", ActPrihodStr.ToString());
                items.Add("DT_FULL", dt.ToString());
                items.Add("EDIT_WEIGHT_FACT", EditWeightFact.Text.ToString());
            }
            else
            if (type == 3)
            {
                items.Add("ID_SCRAP", IdScrap.ToString());
            }

            list.Add(items);

            var listString = JsonConvert.SerializeObject(list);

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("NUM", type.ToString());
                p.CheckAdd("DATA_LIST", listString);
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "ScrapPaper");
            q.Request.SetParam("Action", "ActScrab");
            q.Request.SetParams(p);

            q.Request.Timeout = 25000;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            SetSplash(false);

            if (q.Answer.Status == 0)
            {
                Central.OpenFile(q.Answer.DownloadFilePath);
            }
            else
            {
                q.ProcessError();
            }

        }


        /// <summary>
        /// получение данных с сервера
        /// </summary>
        public async void GetData()
        {
            DisableControls();

            bool resume = true;

            IsAuto = 0;             // обнуляем признак ввода веса, все в автомате
            ScrapCategoryOld5 = 0;
            ScrapCategoryOld6 = 0;
            ScrapCategoryOld8 = 0;
            ScrapCategoryOld11 = 0;

            if (resume)
            {
                var p = new Dictionary<string, string>();
                p.CheckAdd("ID_SCRAP", IdScrap.ToString());

                var q = new LPackClientQuery(); // 1.
                q.Request.SetParam("Module", "ProductionPm");
                q.Request.SetParam("Object", "ScrapPaper");
                q.Request.SetParam("Action", "ScrapTransportSelectRecord");
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
                        // данные по машине
                        var ds = ListDataSet.Create(result, "ITEMS");

                        // проверка на правильность загрузки данных
                        var ds2 = ListDataSet.Create(result, "ITEMS2");
                        if (ds2.Items.Count > 0)
                        {
                            var first = ds2.Items.First();
                            if (first != null)
                            {
                                IdPostOld = first.CheckGet("ID_POST").ToInt();
                                NameCarOld = first.CheckGet("AUTO_NAME").ToString();
                                NamePostavshicOld = first.CheckGet("POST_NAME").ToString();
                            }
                        }

                        if (ds.Items.Count > 0)
                        {
                            var first = ds.Items.First();
                            if (first != null) // есть запись по машине
                            {
                                OldAutoInput = first.CheckGet("AUTO_INPUT").ToInt();
                                IdCategory = first.CheckGet("ID_CATEGORY").ToInt();
                                IdCategoryOld = IdCategory;
                                IdCategoryParent = first.CheckGet("ID_CATEGORY_PARENT").ToInt();
                                PaperTetrapakFlag = first.CheckGet("PAPER_TETRAPAK_FLAG").ToInt();

                                IdStatus = first.CheckGet("ID_STATUS").ToInt();
                                OldIdStatus = IdStatus;
                                StatusReturning = first.CheckGet("STATUS_RETURNING").ToInt();
                                Provedeno = first.CheckGet("PROVEDENO").ToInt();
                                WmtaId = first.CheckGet("WMTA_ID").ToInt();
                                Id2 = first.CheckGet("ID2").ToInt();
                                WeigthFactOld = first.CheckGet("WEIGHT_FACT").ToInt();
                                IdPost = first.CheckGet("ID_POST").ToInt();
                                IdPostOld = IdPost;
                                Sklad = first.CheckGet("SKLAD").ToString();
                                NumPlace = first.CheckGet("NUM_PLACE").ToInt();
                                Nnakl = first.CheckGet("NNAKL").ToInt();
                                NameProd = first.CheckGet("NAME_PROD").ToString();
                                SurmId = first.CheckGet("SURM_ID").ToInt();
                                OldSurmId = SurmId;
                                WastepaperCelluloseQuality.IsChecked = first.CheckGet("WASTEPAPER_CELLULOSE_QUALITY").ToInt() == 1 ? true : false;
                                WastepaperDry.IsChecked = first.CheckGet("WASTEPAPER_DRY").ToInt() == 1 ? true : false;
                                WastepaperShredding.IsChecked = first.CheckGet("WASTEPAPER_SHREDDING").ToInt() == 1 ? true : false;
                                RollsFlag.IsChecked = first.CheckGet("ROLLS_FLAG").ToInt() == 1 ? true : false;
                                BeforeConsumerFlag.IsChecked = first.CheckGet("BEFORE_CONSUMER_FLAG").ToInt() == 0 ? true : false;
                                AfterConsumerFlag.IsChecked = first.CheckGet("BEFORE_CONSUMER_FLAG").ToInt() == 1 ? true : false;

                                ActPrihod = first.CheckGet("NUM_AKT").ToInt();
                                ActPrihodStr = first.CheckGet("NUM_AKT_STR").ToString();
                                ActReturnung = first.CheckGet("ACT_RETURNING_NUM").ToInt();
                                KolBale = first.CheckGet("CNT").ToInt();

                                if (Nnakl != 0)
                                {
                                    NaklprihDataSf.IsEnabled = true;
                                    EditNumDoc.IsEnabled = true;
                                    ChangeDatasf = false;
                                }
                                else
                                {
                                    NaklprihDataSf.IsEnabled = false;
                                    EditNumDoc.IsEnabled = false;
                                }

                                InputControlFlag = first.CheckGet("INPUT_CONTROL_FLAG").ToInt();

                                if (first.CheckGet("INPUT_CONTROL_FLAG").ToInt() == 0)
                                    InputControlFlag0RadioButton.IsChecked = true;
                                else if (first.CheckGet("INPUT_CONTROL_FLAG").ToInt() == 1)
                                    InputControlFlag1RadioButton.IsChecked = true;
                                if (first.CheckGet("INPUT_CONTROL_FLAG").ToInt() == 2)
                                    InputControlFlag2RadioButton.IsChecked = true;

                                if (StatusReturning == 27 || StatusReturning == 28)
                                    WeigthReturn = 0;
                                else
                                    WeigthReturn = first.CheckGet("WEIGHT_RETURNING").ToInt();

                                LabelWeightReturning.Text = WeigthReturn.ToString();

                                switch (IdStatus)
                                {
                                    case 1:
                                        StatusInfo.Text = "Зарегистрировалась";
                                        break;
                                    case 2:
                                        StatusInfo.Text = "Взвешена Брутто";
                                        break;
                                    case 3:
                                        StatusInfo.Text = "Разгрузка начата";
                                        break;
                                    case 4:
                                        StatusInfo.Text = "Разгрузка закончена";
                                        break;
                                    case 5:
                                        StatusInfo.Text = "Взвешена Нетто";
                                        break;
                                    case 6:
                                        StatusInfo.Text = "Разгрузка закончена (отложена)";
                                        break;
                                    default:
                                        break;
                                }

                                switch (IdStatus)
                                {
                                    case 3:
                                    case 4:
                                    case 5:
                                    case 6:
                                        LabelCountBale.Content = KolBale.ToString();
                                        if (Sklad != first.CheckGet("SOURCE_SKLAD").ToString()
                                        || (NumPlace != first.CheckGet("SOURCE_NUM").ToInt()))
                                        {
                                            LabelScrabBale.Content = $"{first.CheckGet("SOURCE_SKLAD")}{first.CheckGet("SOURCE_NUM")}->{Sklad}{NumPlace}";
                                        }
                                        else
                                            LabelScrabBale.Content = $"{Sklad}{NumPlace}";
                                        break;
                                    default:
                                        break;
                                }

                                switch (StatusReturning)
                                {
                                    case 27:
                                        StatusInfo.Text = "Полный возврат партии макулатуры";
                                        break;
                                    case 28:
                                        StatusInfo.Text = "Частичный возврат партии макулатуры";
                                        break;
                                    case 29:
                                        StatusInfo.Text = "Ответственное хранение всей партии макулатуры";
                                        break;
                                    case 30:
                                        StatusInfo.Text = "Ответственное хранение части партии макулатуры";
                                        break;
                                    default:
                                        break;
                                }

                                int id_category_parent = 0;

                                // вид макулатуры
                                // МС-5Б1
                                if (IdCategory < 10 || IdCategory == 33 || IdCategory == 34)
                                {
                                    Mc5BRadioButton.IsChecked = true;
                                    id_category_parent = 0;
                                    ScrapCategoryOld5 = IdCategory; // ScrapCategory.SelectedItem.Key.ToInt();
                                }
                                else
                                // МС-6Б1
                                if (IdCategory > 30 && IdCategory < 60)
                                {
                                    Mc6BRadioButton.IsChecked = true;
                                    id_category_parent = 30;
                                    ScrapCategoryOld6 = IdCategory; //ScrapCategory.SelectedItem.Key.ToInt();
                                }
                                else
                                // МС-8B
                                if (IdCategory > 30 && IdCategory > 60)
                                {
                                    Mc8BRadioButton.IsChecked = true;
                                    id_category_parent = 60;
                                    ScrapCategoryOld8 = IdCategory; //ScrapCategory.SelectedItem.Key.ToInt();
                                }
                                else
                                // Tetra Pak
                                {
                                    Mc11BRadioButton.IsChecked = true;
                                    id_category_parent = 20;
                                    ScrapCategoryOld11 = IdCategory; // ScrapCategory.SelectedItem.Key.ToInt();
                                }

                                // Загрузить список категории макулатуры для выбранного вида
                                CategoryShow(id_category_parent);

                                // вид списание машины
                                int inputControlFlag = first.CheckGet("INPUT_CONTROL_FLAG").ToInt();

                                // вид списание машины
                                if (inputControlFlag == 0)
                                    InputControlFlag0RadioButton.IsChecked = true;
                                else if (inputControlFlag == 1)
                                    InputControlFlag1RadioButton.IsChecked = true;
                                else if (inputControlFlag == 2)
                                    InputControlFlag2RadioButton.IsChecked = true;

                                // показываем номер акта приемки машины
                                if (ActPrihod > 0)
                                {
                                    AktPrintButton.Content = $"Акт приемки {ActPrihodStr}";
                                    AktPrintButton.IsEnabled = true;
                                }

                                // показываем номер акта возврата машины
                                if (ActReturnung > 0)
                                {
                                    ActReturningButton.Content = $"Акт возврата {first.CheckGet("ACT_RETURNING_NUM")}";
                                    ActReturningButton.IsEnabled = true;
                                }

                                if (first.CheckGet("QTY_RETURNING").ToInt() > 0)
                                {
                                    LabelQtyReturn.Content = first.CheckGet("QTY_RETURNING").ToInt().ToString();
                                    LabelScrabBaleReturn.Content = $"{first.CheckGet("SKLAD_RETURNING")}{first.CheckGet("NUM_PLACE_RETURNING").ToInt()}";
                                }

                                Form.SetValues(ds); // заполняем данные формы

                                EditWeightDok.Text = first.CheckGet("WEIGHT_DOK").ToInt().ToString();
                                EditWeightFull.Text = first.CheckGet("WEIGHT_FULL").ToInt().ToString();
                                EditWeightEmpty.Text = first.CheckGet("WEIGHT_EMPTY").ToInt().ToString();
                                EditWeightPoddon.Text = first.CheckGet("WEIGHT_PODDON").ToInt().ToString();
                                EditWeightFact.Text = first.CheckGet("WEIGHT_FACT").ToInt().ToString();

                                int fact = 0;
                                if (!EditWeightFull.Text.IsNullOrEmpty()
                                    && EditWeightFull.Text.ToInt() != 0
                                    && !EditWeightEmpty.Text.IsNullOrEmpty()
                                    && EditWeightEmpty.Text.ToInt() != 0
                                    && !EditWeightPoddon.Text.IsNullOrEmpty())
                                {
                                    fact = EditWeightFull.Text.ToInt() - EditWeightEmpty.Text.ToInt() - EditWeightPoddon.Text.ToInt() - WeigthReturn;
                                }

                                if (IdStatus == 1)
                                {
                                    var today = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                                    LabelWeightFact.Content = $"0     от {today}";
                                    DtFull = today;
                                }
                                else
                                {
                                    DtFull = first.CheckGet("DT_FULL").ToString();
                                    var dt = first.CheckGet("DT_FULL").ToDateTime().ToString("dd.MM.yyyy HH:mm");
                                    LabelWeightFact.Content = $"{fact} от {dt}";

                                }

                                FirstRun = false;
                                DriverShow();
                            }
                        }

                        if (WeightOpen.IsChecked == true) // подключены весы
                        {
                            // Прописываем надпись на кнопке взвешивания
                            InsertWeigthBitBtn.IsEnabled = true;

                            if ((IdStatus == 1)
                             || (IdStatus == 2)
                             || (IdStatus == 3))
                            {
                                InsertWeigthBitBtn.Content = "Заполнить БРУТТО";
                            }
                            else
                            if ((IdStatus == 4)
                             || (IdStatus == 5))
                            {
                                InsertWeigthBitBtn.Content = "Заполнить НЕТТО";
                            }
                            else
                            {
                                InsertWeigthBitBtn.Content = "Взвеситься нельзя";
                                InsertWeigthBitBtn.IsEnabled = false;
                            }
                        }
                        else
                        {
                            InsertWeigthBitBtn.Content = "Заполнить";
                            InsertWeigthBitBtn.IsEnabled = false;

                        }

                        if (IdStatus == 4)  // машина разгружена
                        {
                            if (!EditWeightEmpty.Text.IsNullOrEmpty()
                                && EditWeightEmpty.Text != "0")
                            {
                                PostponeCarButton.IsEnabled = true;   // отложить
                                ReturningCarButton.IsEnabled = true;  // возврат
                                PrihodCloseButton.IsEnabled = true;   // оприходовать
                                OnSkladBitBtn.IsEnabled = true;       // принято на склад
                            }

                            if (KolBale > 0)
                            {
                                DeleteBaleButton.IsEnabled = true;
                                ButtonAddBale.IsEnabled = true;
                            }
                        }

                        if (IdStatus == 5)  // машина взвешена пустая и уехала
                        {
                            PrihodCloseButton.IsEnabled = false;
                            PostponeCarButton.IsEnabled = false;   // отложить
                            if (StatusReturning != 0)
                            {
                                ActReturningButton.IsEnabled = true;  // акт возврата
                                ReturningCarButton.IsEnabled = true;  // возврат
                            }
                            else
                            {
                                ActReturningButton.IsEnabled = false;  // акт возврата
                                ReturningCarButton.IsEnabled = false;  // возврат
                            }

                            if ((Central.User.Login == "fedyanina_ev")
                             || (Central.User.Login == "greshnyh_ni")
                             || (Central.User.Login == "toroptseva_ln"))
                                SaveButton.IsEnabled = true;
                            else
                                SaveButton.IsEnabled = false;
                        }
                        else
                        if (IdStatus == 6)  // машина разгружена и отложена
                        {
                            SaveButton.IsEnabled = false;
                            PrihodCloseButton.IsEnabled = false;
                            AktPrintButton.IsEnabled = false;      // акт приемки
                            ActReturningButton.IsEnabled = false;  // акт возврата
                            PostponeCarButton.IsEnabled = true;    // отложить
                            PostponeCarButton.Content = "Вернуть";
                            ReturningCarButton.IsEnabled = false;  // возврат
                        }
                        else
                        if (IdStatus == 27)  // полный возврат принятой партии
                        {
                            SaveButton.IsEnabled = false;
                            PostponeCarButton.IsEnabled = false;   // отложить
                            ReturningCarButton.IsEnabled = true;   // возврат
                            PrihodCloseButton.IsEnabled = false;   // оприходовать
                            OnSkladBitBtn.IsEnabled = false;       // принято на склад
                            AktPrintButton.IsEnabled = false;      // акт приемки
                            ActReturningButton.IsEnabled = true;   // акт возврата
                        }
                        else
                        if (IdStatus == 28)  // частичный возврат принятой партии
                        {
                            if (Provedeno == 1)
                            {
                                SaveButton.IsEnabled = false;
                                PrihodCloseButton.IsEnabled = false;   // оприходовать                              
                                PostponeCarButton.IsEnabled = false;   // отложить
                                OnSkladBitBtn.IsEnabled = false;       // принято на склад
                                ReturningCarButton.IsEnabled = true;   // возврат
                                AktPrintButton.IsEnabled = true;       // акт приемки
                                ActReturningButton.IsEnabled = true;   // акт возврата
                            }
                            else
                            {
                                PrihodCloseButton.IsEnabled = true;   // оприходовать                              
                                PostponeCarButton.IsEnabled = true;   // отложить
                                ReturningCarButton.IsEnabled = true;   // возврат
                                OnSkladBitBtn.IsEnabled = true;       // принято на склад
                                AktPrintButton.IsEnabled = true;       // акт приемки
                                ActReturningButton.IsEnabled = true;   // акт возврата
                            }
                        }
                        else
                        if (IdStatus == 29)  // ответст. хранение всей принятой партии
                        {
                            SaveButton.IsEnabled = true;
                            PostponeCarButton.IsEnabled = false;   // отложить
                            ReturningCarButton.IsEnabled = true;   // возврат
                            PrihodCloseButton.IsEnabled = true;    // оприходовать                              
                            OnSkladBitBtn.IsEnabled = false;       // принято на склад
                            AktPrintButton.IsEnabled = false;       // акт приемки
                            ActReturningButton.IsEnabled = true;   // акт возврата
                        }
                        else
                        if (IdStatus == 30)  // ответст. хранение части принятой партии
                        {
                            if (Provedeno == 1)
                            {
                                PostponeCarButton.IsEnabled = false;   // отложить
                                ReturningCarButton.IsEnabled = true;   // возврат                                
                                PrihodCloseButton.IsEnabled = false;   // оприходовать                              
                                OnSkladBitBtn.IsEnabled = false;       // принято на склад
                                AktPrintButton.IsEnabled = true;       // акт приемки
                                ActReturningButton.IsEnabled = true;   // акт возврата
                            }
                            else
                            {
                                PostponeCarButton.IsEnabled = true;   // отложить
                                ReturningCarButton.IsEnabled = true;  // возврат                                
                                PrihodCloseButton.IsEnabled = true;   // оприходовать                              
                                OnSkladBitBtn.IsEnabled = true;       // принято на склад
                                AktPrintButton.IsEnabled = true;      // акт приемки
                                ActReturningButton.IsEnabled = true;  // акт возврата
                            }
                        }

                        Show();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            if (IdScrap == 0)  // это новая вручную создаваемая машина
            {
                var today = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                IdStatus = 1;  //машина приехала
                StatusInfo.Text = "Зарегистрировалась";
                LabelWeightFact.Content = $"0     от {today}";
                DtFull = today;
                LabelCountBale.Content = "0";

                Mc5BRadioButton.IsChecked = true;
                ScrapCategoryOld5 = 33;
                ScrapCategoryOld6 = 31;
                ScrapCategoryOld8 = 61;
                ScrapCategoryOld11 = 23;
                CategoryShow(0); // Загрузить список категории макулатуры для МС-5Б

                EditWeightDok.Text = "0";
                EditQuantityBalDoc.Text = "0";
                EditWeightFull.Text = "0";
                EditWeightEmpty.Text = "0";
                EditWeightPoddon.Text = "0";
                EditWeightFact.Text = "0";
                LabelWeightReturning.Text = "0";
                EditHumidity.Text = "12";
                EditContamination.Text = "1";
                AktPrintButton.IsEnabled = false;       // акт приемки
                ActReturningButton.IsEnabled = false;   // акт возврата
                FirstRun = false;
            }

            EnableControls();
        }




        ///// end //////

    }

}






