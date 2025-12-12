using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Input;
using System.Diagnostics.Eventing.Reader;
using DevExpress.Xpf.Core.DragDrop.Native;
using System.IO;
using System.Reflection;
using DevExpress.Utils.IoC;


namespace Client.Interfaces.Production
{
    /// <summary>
    /// Вывод информации водителю перед записью в базу
    /// </summary>
    /// <author>Грешных Н.И.</author>
    /// <version>1</version>
    public partial class Info : WizardFrame
    {
        public Info()
        {
            InitializeComponent();
            
            if(Central.InDesignMode()){
                return;
            }

            ItemId=0;
            IdTs = 0;
            RmbuId = 0;
            WmtsId = 0;
            Barcode ="";

            InitForm();
            SetDefaults();
            Central.Msg.Register(ProcessMessage);
        }

        public int ItemId{get;set;}
        public string Barcode {get;set;}
        private int IdTs { get; set; }
        private int RmbuId { get; set; }
        private int WmtsId { get; set; }

        /// <summary>
        /// Инициализация формы
        /// </summary>
        public void InitForm()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="PHONE_NUMBER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },          
                },
                  new FormHelperField()
                {
                    Path="PHONE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ITEM_ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },          
                },
                new FormHelperField()
                {
                    Path="PHONE3",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ID_TS",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ID_A",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CAR_MODEL_ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="RMBU_ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="WMTS_ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="REGISTRATION",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            Form.SetFields(fields);
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            //установка значений по умолчанию
            Form.SetDefaults();
            NextButtonSet(true);
        }

        /// <summary>
        /// </summary>
        private void SetPhoneNumber()
        {
            PhoneNumber.Text=$"";
            var v = Wizard.Values;
            var n = "";

            if ((v.CheckGet("CARGO_TYPE").ToInt() == 4) || (v.CheckGet("CARGO_TYPE").ToInt() == 5))
            {
                n = Form.GetValueByPath("PHONE");
            } else
            if ((v.CheckGet("CARGO_TYPE").ToInt() == 6))
            {
                n = Form.GetValueByPath("PHONE3");
            }
            else
            {
                n = Form.GetValueByPath("PHONE_NUMBER");
            }

            if (!n.IsNullOrEmpty())
            {
                PhoneNumber.Text = $"На ваш телефон {n}";
            }
        }

        /// <summary>
        /// обработка сообщений
        /// </summary>
        /// <param name="message"></param>
        public void ProcessMessage(ItemMessage message)
        {
            if(message!=null)
            {
                if(message.ReceiverName==ControlName)
                {
                    switch (message.Action)
                    {
                        //фрейм загружен 
                        case "Showed":
                            SetDefaults();
                            LoadValues();  
                            SetPhoneNumber();
                            var v = Wizard.Values;
                            if ((v.CheckGet("CARGO_TYPE").ToInt() == 6) && (v.CheckGet("REGISTRATION").ToInt() == 0))
                            {
                                Save2();
                            } // регистрация машины после установки нового времени слота
                            else if ((v.CheckGet("CARGO_TYPE").ToInt() == 6) && (v.CheckGet("REGISTRATION").ToInt() == 1))
                            {
                               var res =  Save3();
                            }
                            else
                            {
                                Save();
                            }
                                
                            break;

                        //ввод с экранной клавиатуры
                        case "KeyPressed":
                          
                            break;
                    }
                }
            }
        }
                
        /// <summary>
        /// проверка, можно ли активировать кнопку "далее"
        /// </summary>
        private void Validate()
        {
            if((ItemId!=0) || (IdTs!= 0) || (RmbuId != 0))
            {
                Form.SetValueByPath("ITEM_ID",ItemId.ToString());
                SaveValues();
                NextButtonSet(true);
            }
            else
            {
                NextButtonSet(false);
            }
        }

        /// <summary>
        /// генерация штрихкода (старая, больше не используется, ШК формируется на триггере SCRAP_TRANSPORT_INIT_BI_TRG)
        /// </summary>
        private void BarcodeGenerate(int machineId)
        {
            Barcode = "";
            string barcodeOld = "171600000000";
            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID_ST", machineId.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "PaperProduction");
                q.Request.SetParam("Object", "TransportDriver");
                q.Request.SetParam("Action", "ListBarCode");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                //await Task.Run(() =>
                //{
                    q.DoQuery();
                //});

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        var id=ds.GetFirstItemValueByKey("BARCODE").ToString();
                        if(!id.IsNullOrEmpty())
                        {
                            barcodeOld=id;
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            Barcode = barcodeOld.Substring(0, 4);
            var nextVal = (barcodeOld.Substring(4, 8).ToInt() + 7).ToString();
            Barcode = Barcode + nextVal.PadLeft(8, '0');
        }

        /// <summary>
        /// активация/деактивация кнопки "далее"
        /// </summary>
        /// <param name="mode"></param>
        private void NextButtonSet(bool mode=true)
        {
            if(NextButton!=null)
            {
                if(mode)
                {
                    NextButton.IsEnabled=true;
                    NextButton.Opacity=1.0;
                    NextButton.Style=(Style)NextButton.TryFindResource("TouchFormButtonPrimaryBig");
                }
                else
                {
                    NextButton.IsEnabled=false;
                    NextButton.Opacity=0.5;
                    NextButton.Style=(Style)NextButton.TryFindResource("TouchFormButtonBig");
                }
            }
        }

        /// <summary>
        /// нажали кнопку "Домой"
        /// </summary>
        private void HomeButtonClick(object sender, RoutedEventArgs e)
        {
            Wizard.Navigate(0);
        }

        /// <summary>
        /// нажали кнопку "Предыдущий"
        /// </summary>
        private void PriorButtonClick(object sender, RoutedEventArgs e)
        {
            var v = Wizard.Values;
            if ((v.CheckGet("CARGO_TYPE").ToInt() == 4) || (v.CheckGet("CARGO_TYPE").ToInt() == 5))
            {
                Wizard.Navigate("ConfirmSms2");
            }
            else
            if ((v.CheckGet("CARGO_TYPE").ToInt() == 6) )
            {
                Wizard.Navigate("ConfirmSms3");
            }
            else
            {
                Wizard.Navigate(-1);
            }

        }

        /// <summary>
        /// нажали кнопку "Следующий"
        /// </summary>
        private void NextButtonClick(object sender, RoutedEventArgs e)
        {
            Wizard.Navigate(1);
        }

        private void SaveLog(string msg)
        {
            // сохраняем данные в файл (перезаписываем)
            var pathInfo = new FileInfo(Assembly.GetExecutingAssembly().Location);
            string printFile = $"{pathInfo.Directory}\\logfile.txt";
            try
            {
                var today = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                string text = $"{today} => {msg}";

                File.AppendAllText(printFile, $"{text}\n");


            }
            catch (Exception ex) { }
        }


        private void Save()
        {
            bool resume = true;
            var p = new Dictionary<string, string>();

            var v = Wizard.Values;
            var puspose = "";

            if (v.CheckGet("CARGO_TYPE").ToInt() == 4)
            {
                puspose = "2";
            }
            else
            if (v.CheckGet("CARGO_TYPE").ToInt() == 5)
            {
                puspose = "3";
            }

            if ((v.CheckGet("CARGO_TYPE").ToInt() == 4) || (v.CheckGet("CARGO_TYPE").ToInt() == 5))
            {

                p.Add("PURPOSE", puspose);
                p.Add("SURNAME", v.CheckGet("SURNAME"));
                p.Add("NAME", v.CheckGet("NAME"));
                p.Add("MIDDLE_NAME", v.CheckGet("MIDDLE_NAME"));
                p.Add("PHONE", v.CheckGet("PHONE").Substring(1, 11));
                p.Add("CAR_MODEL", v.CheckGet("CAR_MODEL_ID"));
                p.Add("TRUCK_NUMBER", v.CheckGet("TRUCK_NUMBER"));
                p.Add("TRAILER_NUMBER", v.CheckGet("TRAILER_NUMBER"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "TransportDriver");
                q.Request.SetParam("Action", "Register");
                q.Request.SetParams(p);

                q.DoQuery();

                if (q.Answer.Status != 0)
                {
                    q.ProcessError();
                }

                resume = false;
            }

            if (resume)
            {
                //  BarcodeGenerate(v.CheckGet("MACHINE_ID").ToInt()); // ШК будет генерироваться при создании записи в scrap_transport
            }

            if (resume)
            {
                p.CheckAdd("ID_POST", v.CheckGet("VENDOR_ID"));

                switch (v.CheckGet("CARGO_TYPE").ToInt())
                {
                    //typeDescription="Я привез макулатуру";
                    case 1:
                        {
                            p.CheckAdd("ID_STATUS", "1");
                            p.CheckAdd("CONTAMINATION", "1");
                            p.CheckAdd("ID_CATEGORY", "33");
                        }
                        break;

                    //typeDescription="Я приехал за полиэтиленовой смесью";
                    case 2:
                        {
                            p.CheckAdd("ID_STATUS", "11");
                            p.CheckAdd("CONTAMINATION", "15");
                            //p.CheckAdd("ID_CATEGORY", "41");
                            p.CheckAdd("ID_CATEGORY", "42");
                            p.CheckAdd("ID_POST", "0");
                        }
                        break;

                    //typeDescription="Я привез химию";
                    case 3:
                        {
                            p.CheckAdd("ID_STATUS", "41");
                            p.CheckAdd("CONTAMINATION", "15");
                            p.CheckAdd("ID_CATEGORY", "51");
                            p.CheckAdd("ID_POST", "0");
                        }
                        break;
                }

                p.CheckAdd("NAME", $"{v.CheckGet("CAR_MODEL_DESCRIPTION")} {v.CheckGet("CAR_NUMBER")}");
                //p.CheckAdd("BARCODE", Barcode.ToString());
                p.CheckAdd("BARCODE", "");
                p.CheckAdd("PHONE_NUMBER", v.CheckGet("PHONE_NUMBER").Substring(1, 10));
                p.CheckAdd("ID_ST", v.CheckGet("MACHINE_ID"));

            }

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "PaperProduction");
                q.Request.SetParam("Object", "TransportDriver");
                q.Request.SetParam("Action", "CreateRegistration");

                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                //await Task.Run(() =>
                //{
                q.DoQuery();
                //});

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        ItemId = ds.GetFirstItemValueByKey("ID").ToInt();
                        if (ItemId != 0)
                        {
                            Validate();
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        ////добавляем регистрацию машины по коду брони
        /// </summary>
        private void Save2()
        {
            bool resume = true;
            var p = new Dictionary<string, string>();

            var v = Wizard.Values;
            var wmts_id = v.CheckGet("WMTS_ID");
            var cod = v.CheckGet("BOOKING_CODE");
            var vid = "0";

            p.CheckAdd("WMTS_ID", wmts_id);
            p.CheckAdd("VID", vid);

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "PaperProduction");
                q.Request.SetParam("Object", "TransportDriver");
                q.Request.SetParam("Action", "CreateRegistrationCarNew");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                //await Task.Run(() =>
                //{
                q.DoQuery();
                //});

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        ItemId = ds.GetFirstItemValueByKey("ID").ToInt();
                        if (ItemId != 0)
                        {
                            Validate();

                            var msg = $"Код: {cod}. УСПЕШНАЯ РЕГИСТРАЦИЯ.";
                            SaveLog(msg);

                            // если это площадка БДМ1, то меняем id_st  в  
                            var is_st =  v.CheckGet("MACHINE_ID").ToInt();
                            if (is_st ==  716)
                            {
                                var p2 = new Dictionary<string, string>();
                                p2.CheckAdd("ID_ST", "716");
                                p2.CheckAdd("ID_SCRAP", ItemId.ToString());

                                q.Request.SetParam("Module", "PaperProduction");
                                q.Request.SetParam("Object", "TransportDriver");
                                q.Request.SetParam("Action", "SaveIdStCar");
                                q.Request.SetParams(p2);

                                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;
                                q.DoQuery();
                            }

                        }
                    }
                }
                else
                {
                    var msg = $"Код: {cod}. ОШИБКА РЕГИСТРАЦИИ. Код ошибки {q.Answer.Status}";
                    SaveLog(msg);

                    q.ProcessError();
                }
            }
        }


        /// <summary>
        ////добавляем регистрацию машины по Wmts_id после аолучения нового времени слота
        /// </summary>
        private bool Save3()
        {
            bool resume = true;
            var p = new Dictionary<string, string>();

            var v = Wizard.Values;
            var wmts_id = v.CheckGet("WMTS_ID");
            var cod = v.CheckGet("BOOKING_CODE");
            var vid = "1";

            p.CheckAdd("WMTS_ID", wmts_id);
            p.CheckAdd("VID", vid);

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "PaperProduction");
                q.Request.SetParam("Object", "TransportDriver");
                q.Request.SetParam("Action", "CreateRegistrationCarNew");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                //await Task.Run(() =>
                //{
                q.DoQuery();
                //});

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        ItemId = ds.GetFirstItemValueByKey("ID").ToInt();
                        if (ItemId != 0)
                        {
                            Validate();

                            var msg = $"Код: {cod}. Изменили время слота. УСПЕШНАЯ РЕГИСТРАЦИЯ.";
                            SaveLog(msg);
                        }
                    }
                }
                else
                {
                    var msg = $"Код: {cod}. ОШИБКА РЕГИСТРАЦИИ. Код ошибки {q.Answer.Status}";
                    SaveLog(msg);
                    resume = false;
                    q.ProcessError();
                }
            }
            return resume;
        }
    }
}

