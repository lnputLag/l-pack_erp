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
    /// Карточка возврата/хранения кип машины с макулатурой
    /// </summary>
    /// <author>greshnyh_ni</author>
    /// <changed>2025-11-18</changed>
    public partial class ScrapTransportReturning : ControlBase
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
        /// <summary>
        /// ссылка на накладную
        /// </summary>
        private int Nnakl = 0;
        /// <summary>
        /// Идентификатор партии
        /// </summary>
        private int IdP { get; set; }

        /// <summary>
        ///  ряд для хранения кип
        /// </summary>
        private string SkladRetunning { get; set; }
        /// <summary>
        ///  ячейка для хранения кип
        /// </summary>
        private string NumPlaceRetunning { get; set; }

        /// <summary>
        ///  Дата возврата кип
        /// </summary>
        private string DtReturn { get; set; }


        /// <summary>
        /// ссылка на плащадку БДМ1 -716, БДМ2 -1716, ЦЛТ - 2716
        /// </summary>
        public int IdSt { get; set; }
        /// <summary>
        /// статус машины при возврате/хранении
        /// </summary>
        public int StatusReturning { get; set; }

        private bool FirstRun = true;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="record"></param>
        public ScrapTransportReturning(Dictionary<string, string> record = null)
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
                //Central.Msg.SendMessage(new ItemMessage()
                //{
                //    ReceiverGroup = "Production",
                //    ReceiverName = ReceiverName,
                //    SenderName = ControlName,
                //    Action = "RefreshScrapTransportWeightGrid",
                //});
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
                    Path="NAME_POST",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=EditPostavshic,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                // склад
                new FormHelperField()
                {
                    Path="SKLAD",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=EditSklad,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                // ряд
                new FormHelperField()
                {
                    Path="NUM_PLACE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=EditNumPlace,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                // кип в ячейке
                new FormHelperField()
                {
                    Path="CNT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=EditBaleCount,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                // вес кип в ячейке
                new FormHelperField()
                {
                    Path="WEIGHT_FACT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=EditWeightFact,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                  //  { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="QTY_RETURN",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=EditQtyReturning,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="WEIGHT_RETURN",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=EditWeightReturning,
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
            // Выбор вид возврата
            {
                var list = new Dictionary<string, string>();
                list.Add("-1", "");
                list.Add("0", "Полный возврат партии макулатуры");
                list.Add("1", "Частичный возврат партии макулатуры");
                list.Add("2", "Ответственное хранение всей партии макулатуры.");
                list.Add("3", "Ответственное хранение части партии макулатуры.");

                TypeReturning.Items = list;
                TypeReturning.SetSelectedItemByKey("-1");

            }

            EditSklad.Text = string.Empty;
            EditNumPlace.Text = string.Empty;
            EditBaleCount.Text = "0";
            EditWeightFact.Text = "0";
            EditQtyReturning.Text = "0";
            EditWeightReturning.Text = "0";

            EditQtyReturning.IsEnabled = false;
            EditWeightReturning.IsEnabled = false;

            ScrapBaleButton.IsEnabled = false;              // Оставить на складе
            ReturnSupplierButton.IsEnabled = false;         // вернуть поставщику
            ScrapTransportAtrrEditButton.IsEnabled = false; // описание возвратных кип

            Form.SetDefaults();
        }

        public void Edit()
        {
            FirstRun = true;
            FrameTitle = $"Возврат/хранение кип, ИД {IdScrap}";
            GetData();      // получение данных по машине
        }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
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
        /// получение данных с сервера
        /// </summary>
        public async void GetData()
        {
            bool resume = true;

            DisableControls();

            if (resume)
            {
                var p = new Dictionary<string, string>();
                p.CheckAdd("ID_SCRAP", IdScrap.ToString());

                var q = new LPackClientQuery(); // 1.
                q.Request.SetParam("Module", "ProductionPm");
                q.Request.SetParam("Object", "ScrapPaper");
                q.Request.SetParam("Action", "ScrapTransportSelectSklad");
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

                        if (ds.Items.Count > 0)
                        {
                            var first = ds.Items.First();
                            if (first != null) // есть запись по машине
                            {
                                Nnakl = first.CheckGet("NNAKL").ToInt();
                                IdCategory = first.CheckGet("ID_CATEGORY").ToInt();
                                SkladRetunning = first.CheckGet("SKLAD_RETURN").ToString();
                                NumPlaceRetunning = first.CheckGet("NUM_PLACE_RETURN").ToInt().ToString();

                                DtReturn = first.CheckGet("DT_RETURN").ToString();

                                SkladShow();    // загрузка списка рядов для текущей площадки

                                FirstRun = false;

                                ShowButon(); // настройка доступности кнопок и названий

                                if (StatusReturning > 0)
                                {
                                    switch (StatusReturning)
                                    {
                                        case 27:
                                            TypeReturning.SetSelectedItemByKey("0");
                                            break;
                                        case 28:
                                            TypeReturning.SetSelectedItemByKey("1");
                                            break;
                                        case 29:
                                            TypeReturning.SetSelectedItemByKey("2");
                                            break;
                                        case 30:
                                            TypeReturning.SetSelectedItemByKey("3");
                                            break;
                                        default:
                                            break;
                                    }

                                    SkladKeping.SetSelectedItemByKey(SkladRetunning);
                                    NumPlaceShow();

                                    SaveButton.IsEnabled = false;                  // Нельзя сохранить
                                 //   ReturnSupplierButton.IsEnabled = false;        // вернуть поставщику
                                    ScrapTransportAtrrEditButton.IsEnabled = true;   // описание возвратных кип
                                  
                                    if (TypeReturning.SelectedItem.Key.ToInt() == 2
                                     || TypeReturning.SelectedItem.Key.ToInt() == 3)
                                    {
                                        if (!DtReturn.IsNullOrEmpty())
                                            ReturnSupplierButton.IsEnabled = false;     // уже был возврат поставщику
                                        else
                                            ReturnSupplierButton.IsEnabled = true;      // вернуть поставщику можно
                                    }

                                    if (StatusReturning >= 29)
                                    { // если известен вид хранения кип
                                        if ((Central.User.Login == "fedyanina_ev")
                                        || (Central.User.Login == "greshnyh_ni")
                                        || (Central.User.Login == "toroptseva_ln"))
                                        {
                                            if (DtReturn.IsNullOrEmpty())                 // еще не было возврата поставщику  
                                                ScrapBaleButton.IsEnabled = true;         // оставить на складе
                                            else
                                                ScrapBaleButton.IsEnabled = false;        // оставить на складе
                                        }
                                        else
                                        {
                                            ScrapBaleButton.IsEnabled = false;            // оставить на складе
                                        }
                                    }
                                }

                                Form.SetValues(ds); // заполняем данные формы
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

            EnableControls();
        }

        /// <summary>
        /// вызываем фому для описания партии кип
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScrapTransportAtrrEditButton_Click(object sender, RoutedEventArgs e)
        {
            // 0 - обычное описание качества кип, 1 - возвратные кипы
            var scrapTransportAttrNewRecord = new ScrapTransportAttrNew(Values as Dictionary<string, string>, 1);
            scrapTransportAttrNewRecord.ReceiverName = ControlName;
            scrapTransportAttrNewRecord.Edit();

        }

        /// <summary>
        /// загрузка списка рядов для выбранной площадки
        /// </summary>
        private void SkladShow()
        {
            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_ST", IdSt.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "ScrapPaper");
            q.Request.SetParam("Action", "PlacesSelectSklad");
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
                        var list = dataSet.GetItemsList("SKLAD", "SKLAD");
                        SkladKeping.Items = list;
                    }
                }
            }
        }

        /// <summary>
        /// загрузка списка ячеек для выбранного ряда и площадки
        /// </summary>
        private void NumPlaceShow()
        {
            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_ST", IdSt.ToString());
                p.CheckAdd("SKLAD", SkladKeping.SelectedItem.Value);
                p.CheckAdd("STATUS_RETURNING", StatusReturning.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "ScrapPaper");
            q.Request.SetParam("Action", "PlacesSelectNumPlace");
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
                        var list = dataSet.GetItemsList("NUM", "NUM");
                        NumPlaceKeping.Items = list;

                        if (StatusReturning > 0)
                            NumPlaceKeping.SetSelectedItemByKey(NumPlaceRetunning);
                    }
                }
            }
        }

        /// <summary>
        ///  при выборе ряда загрузим списоу ячеек в нем
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void SkladKeping_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (FirstRun)
                return;

            NumPlaceShow();
        }


        /// <summary>
        /// выбор вида хранения кип 
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void TypeReturning_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ShowButon();
        }

        private void ShowButon()
        {
            if (FirstRun)
                return;

            LabelSkladKeping.IsEnabled = false;
            SkladKeping.IsEnabled = false;
            LabelNumPlaceKeping.IsEnabled = false;
            NumPlaceKeping.IsEnabled = false;
            CellScrabDefectFlag.IsEnabled = false;
            EditQtyReturning.Text = "0";
            EditWeightReturning.Text = "0";
            ReturnSupplierButton.IsEnabled = false;

            if ((TypeReturning.SelectedItem.Key.ToInt() == 0)
                || (TypeReturning.SelectedItem.Key.ToInt() == 1))
            {   // полный возврат всех кип
                // частичный возврат всех кип
                LabelQtyReturning.Content = "Количество кип к возврату";
            }
            else
            if (TypeReturning.SelectedItem.Key.ToInt() == 2)
            {  // ответ. хранение всей партии
                LabelQtyReturning.Content = "Количество кип на хранении";
                LabelSkladKeping.IsEnabled = true;
                SkladKeping.IsEnabled = true;
                LabelNumPlaceKeping.IsEnabled = true;
                NumPlaceKeping.IsEnabled = true;
                CellScrabDefectFlag.IsEnabled = true;
                EditQtyReturning.Text = EditBaleCount.Text;
                EditWeightReturning.Text = EditWeightFact.Text;

                if (StatusReturning > 0)
                    ReturnSupplierButton.IsEnabled = true;
            }
            else
            if (TypeReturning.SelectedItem.Key.ToInt() == 3)
            {  // ответ. хранение части партии
                LabelQtyReturning.Content = "Количество кип на хранении";
                LabelSkladKeping.IsEnabled = true;
                SkladKeping.IsEnabled = true;
                LabelNumPlaceKeping.IsEnabled = true;
                NumPlaceKeping.IsEnabled = true;
                CellScrabDefectFlag.IsEnabled = false;
                EditQtyReturning.Text = "0";
                EditWeightReturning.Text = "0";
                EditQtyReturning.IsEnabled = true;
                EditWeightReturning.IsEnabled = true;

                if (StatusReturning > 0)
                    ReturnSupplierButton.IsEnabled = true;
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
                    if (TypeReturning.SelectedItem.Key.ToInt() == -1)
                    {
                        errorMsg = "Выберите вид возврата кип.";
                        resume = false;
                    }

                    if (TypeReturning.SelectedItem.Key.ToInt() == 2
                     || TypeReturning.SelectedItem.Key.ToInt() == 3)
                    { // на ответ. хранение всей партии
                      // на ответ. хранение части партии
                        if (EditQtyReturning.Text.ToInt() == 0)
                        {
                            errorMsg = "Необходимо ввести количество кип на хранение.";
                            resume = false;
                        }
                        if (CellScrabDefectFlag.IsChecked == false)
                        {  // если будет перемещения кип из ячейки разгрузки в другую
                            if (SkladKeping.SelectedItem.Value.IsNullOrEmpty())
                            {
                                errorMsg = "Необходимо указать ряд для хранения.";
                                resume = false;
                            }
                            else
                            if (NumPlaceKeping.SelectedItem.Value.IsNullOrEmpty())
                            {
                                errorMsg = "Необходимо указать ячейку для хранения.";
                                resume = false;
                            }
                        }

                    }
                }

                if (resume)
                {
                    SetSplash(true, "Ждите. Идет работа с кипами.");
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
        /// Изменение записи 
        /// </summary>
        public async void UpdateRecord(bool flagClose)
        {
            var resume = true;
            var q = new LPackClientQuery();

            var sklad_keping = "";
            var num_place_keping = 0;

            var t = TypeReturning.SelectedItem.Key.ToInt();
            switch (t) // тип возврата кип
            {
                case 0: // полный возврат всей партии макулатуры
                    StatusReturning = 27;
                    sklad_keping = "";
                    num_place_keping = 0;
                    break;

                case 1: // частичный возврат всей партии макулатуры
                    StatusReturning = 28;
                    sklad_keping = "";
                    num_place_keping = 0;
                    break;

                case 2: // ответ. хранение всей партии макаулатуры
                    StatusReturning = 29;
                    if (CellScrabDefectFlag.IsChecked.ToInt() == 0)
                    {
                        sklad_keping = SkladKeping.SelectedItem.Value.ToString();
                        num_place_keping = NumPlaceKeping.SelectedItem.Value.ToInt();
                    }
                    else
                    {
                        sklad_keping = EditSklad.Text;
                        num_place_keping = EditNumPlace.Text.ToInt();
                    }

                    break;

                case 3: // ответ. хранение части партии макаулатуры
                    StatusReturning = 30;
                    sklad_keping = SkladKeping.SelectedItem.Value.ToString();
                    num_place_keping = NumPlaceKeping.SelectedItem.Value.ToInt();
                    break;

                default:
                    break;
            }

            var v = Form.GetValues();

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_SCRAP", IdScrap.ToString());
                p.CheckAdd("STATUS_RETURNING", StatusReturning.ToString());
                p.CheckAdd("QTY_RETURNING", v.CheckGet("QTY_RETURN").ToString());
                p.CheckAdd("WEIGHT_RETURNING", v.CheckGet("WEIGHT_RETURN").ToString());
                p.CheckAdd("NNAKL", Nnakl.ToString());
                p.CheckAdd("SKLAD", v.CheckGet("SKLAD").ToString());
                p.CheckAdd("NUM_PLACE", v.CheckGet("NUM_PLACE").ToString());
                p.CheckAdd("CELL_SCRAB_DEFECT_FLAG", CellScrabDefectFlag.IsChecked.ToInt().ToString());
                p.CheckAdd("SKLAD_RETURNING", sklad_keping);
                p.CheckAdd("NUM_PLACE_RETURNING", num_place_keping.ToString());
            }

            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "ScrapPaper");
            q.Request.SetParam("Action", "PrihodUpdateReturningBale");

            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                {
                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverGroup = "Production",
                        ReceiverName = ReceiverName,
                        SenderName = ControlName,
                        Action = "CloseScrapTransport",
                    });
                    SetSplash(false);
                    Close();
                }
            }
            else
            {
                Form.SetStatus(q.Answer.Error.Message, 1);
            }
        }


        /// <summary>
        /// возврат поставщику всех кип на хранении
        /// просто списание всех кип в производство
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReturnSupplierButton_Click(object sender, RoutedEventArgs e)
        {
            var q = new LPackClientQuery();

            var message = "";
            message += $"Вы действительно хотите вернуть все кипы поставщику?";
            var dialog = new DialogWindow(message, "Внимание", "", DialogWindowButtons.OKCancel);
            var confirmResult = dialog.ShowDialog();
            if (confirmResult == true)
            {
                ReturnSupplierButton.IsEnabled = false;

                SetSplash(true, "Ждите. Идет возврат всех кип.");

                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID_SCRAP", IdScrap.ToString());
                }

                q.Request.SetParam("Module", "ProductionPm");
                q.Request.SetParam("Object", "ScrapPaper");
                q.Request.SetParam("Action", "ReturningBalePostavhic");

                q.Request.SetParams(p);
                q.Request.Timeout = 300000;
                q.Request.Attempts = 1;

                q.DoQuery();

                SetSplash(false);

                if (q.Answer.Status == 0)
                {
                    message = "";
                    message += $"Все кипы успешно списаны.";
                    dialog = new DialogWindow(message, "Внимание", "", DialogWindowButtons.OK);
                    confirmResult = dialog.ShowDialog();
                    ReturnSupplierButton.IsEnabled = false;
                }
                else
                {
                    Form.SetStatus(q.Answer.Error.Message, 1);
                }
            }
        }

        /// <summary>
        /// оставить на складе все кипы на хранении 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScrapBaleButton_Click(object sender, RoutedEventArgs e)
        {
            var q = new LPackClientQuery();

            var message = "";
            message += $"Вы действительно хотите оставить все кипы на складе?";
            var dialog = new DialogWindow(message, "Внимание", "", DialogWindowButtons.OKCancel);
            var confirmResult = dialog.ShowDialog();
            if (confirmResult == true)
            {
                ScrapBaleButton.IsEnabled = false;
                
                SetSplash(true, "Ждите. Идет перенос всех кип на склад.");
                
                var sklad_keping = "";
                var num_place_keping = 0;

                var t = TypeReturning.SelectedItem.Key.ToInt();
                switch (t) // тип возврата кип
                {
                    case 0: // полный возврат всей партии макулатуры
                        StatusReturning = 27;
                        sklad_keping = "";
                        num_place_keping = 0;
                        break;

                    case 1: // частичный возврат всей партии макулатуры
                        StatusReturning = 28;
                        sklad_keping = "";
                        num_place_keping = 0;
                        break;

                    case 2: // ответ. хранение всей партии макаулатуры
                        StatusReturning = 29;
                        if (CellScrabDefectFlag.IsChecked.ToInt() == 0)
                        {
                            sklad_keping = SkladKeping.SelectedItem.Value.ToString();
                            num_place_keping = NumPlaceKeping.SelectedItem.Value.ToInt();
                        }
                        else
                        {
                            sklad_keping = EditSklad.Text;
                            num_place_keping = EditNumPlace.Text.ToInt();
                        }

                        break;

                    case 3: // ответ. хранение части партии макаулатуры
                        StatusReturning = 30;
                        sklad_keping = SkladKeping.SelectedItem.Value.ToString();
                        num_place_keping = NumPlaceKeping.SelectedItem.Value.ToInt();
                        break;

                    default:
                        break;
                }

                var v = Form.GetValues();

                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID_SCRAP", IdScrap.ToString());
                    p.CheckAdd("STATUS_RETURNING", StatusReturning.ToString());
                    p.CheckAdd("QTY_RETURNING", v.CheckGet("QTY_RETURN").ToString());
                    p.CheckAdd("WEIGHT_RETURNING", v.CheckGet("WEIGHT_RETURN").ToString());
                    p.CheckAdd("NNAKL", Nnakl.ToString());
                    p.CheckAdd("SKLAD", v.CheckGet("SKLAD").ToString());
                    p.CheckAdd("NUM_PLACE", v.CheckGet("NUM_PLACE").ToString());
                    p.CheckAdd("SKLAD_RETURNING", sklad_keping);
                    p.CheckAdd("NUM_PLACE_RETURNING", num_place_keping.ToString());
                }

                q.Request.SetParam("Module", "ProductionPm");
                q.Request.SetParam("Object", "ScrapPaper");
                q.Request.SetParam("Action", "MoveBaleToSklad");

                q.Request.SetParams(p);
                q.Request.Timeout = 300000;
                q.Request.Attempts = 1;

                q.DoQuery();

                SetSplash(false);

                if (q.Answer.Status == 0)
                {
                    message = "";
                    message += $"Все кипы успешно оставлены на складе.";
                    dialog = new DialogWindow(message, "Внимание", "", DialogWindowButtons.OK);
                    confirmResult = dialog.ShowDialog();
                    ReturnSupplierButton.IsEnabled = false;
                }
                else
                {
                    Form.SetStatus(q.Answer.Error.Message, 1);
                }
            }
        }






        ///// end //////
    }


}

