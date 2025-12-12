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
using DevExpress.Xpf.Bars.Native;
using NPOI.SS.Formula.Functions;
using System.IO;
using System.Reflection;
using DevExpress.Utils.IoC;


namespace Client.Interfaces.Production
{
    /// <summary>
    /// Ввод кода бронирования тайм-слота
    /// </summary>
    /// <author>Грешных Н.И.</author>
    /// <version>1</version>
    public partial class BookingCode : WizardFrame
    {
        public BookingCode()
        {
            InitializeComponent();

            if (Central.InDesignMode())
            {
                return;
            }

            InitForm();
            SetDefaults();
            Central.Msg.Register(ProcessMessage);
        }

        /// <summary>
        /// Код ответа при проверке кода бронирования
        /// </summary>
        private int ErrorCode { get; set; }

        /// <summary>
        /// Ид машины
        /// </summary>
        private int IdTs { get; set; }

        /// <summary>
        /// Ид машины Rmbu_Id
        /// </summary>
        private int Rmbu_Id { get; set; }

        /// <summary>
        /// Ид слота
        /// </summary>
        private int Wmts_Id { get; set; }

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
                    Path="BOOKING_CODE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=BookingCodeText,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ERROR_INFO",
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
                    Path="ID_D",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="FIO",
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
                    Path="MARKA_CAR",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NUMBER_CAR",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DTTM_SLOT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="VENDOR_ID",
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

            NextButtonSet(false);
        }

        /// <summary>
        /// обработка сообщений
        /// </summary>
        /// <param name="message"></param>
        public void ProcessMessage(ItemMessage message)
        {
            if (message != null)
            {
                if (message.ReceiverName == ControlName)
                {
                    switch (message.Action)
                    {
                        //фрейм загружен 
                        case "Showed":
                            SetDefaults();
                            LoadValues();
                            Validate();
                            break;

                        //ввод с экранной клавиатуры
                        case "KeyPressed":
                            ChangeValue(message.Message);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// ввод в поле из виртуальной клавиатуры
        /// </summary>
        /// <param name="symbol"></param>
        private void ChangeValue(string symbol)
        {
            if (IsActive() && !string.IsNullOrEmpty(symbol))
            {
                var s = Form.GetValueByPath("BOOKING_CODE");
                switch (symbol)
                {
                    case "BACK_SPACE":
                        if (s.Length > 0)
                        {
                            s = s.Substring(0, (s.Length - 1));
                        }
                        break;

                    default:
                        if (s.Length < 6)
                        {
                            s = s + symbol;
                        }
                        break;
                }
                Form.SetValueByPath("BOOKING_CODE", s);
            }

            Validate();
        }

        /// <summary>
        /// проверка, можно ли активировать кнопку "далее"
        /// </summary>
        private void Validate()
        {
            //если в поле "код брони" блок текста длиной 6 символов, можно продолжать
            var s = Form.GetValueByPath("BOOKING_CODE");
            if (s.Length == 6)
            {
                NextButtonSet(true);
                SaveValues();
            }
            else
            {
                NextButtonSet(false);
            }
        }

        /// <summary>
        /// активация/деактивация кнопки "далее"
        /// </summary>
        /// <param name="mode"></param>
        private void NextButtonSet(bool mode = true)
        {
            if (NextButton != null)
            {
                if (mode)
                {
                    NextButton.IsEnabled = true;
                    NextButton.Opacity = 1.0;
                    NextButton.Style = (Style)NextButton.TryFindResource("TouchFormButtonPrimaryBig");
                }
                else
                {
                    NextButton.IsEnabled = false;
                    NextButton.Opacity = 0.5;
                    NextButton.Style = (Style)NextButton.TryFindResource("TouchFormButtonBig");
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
            Wizard.Navigate("CargoType");
        }

        /// <summary>
        /// нажали кнопку "Следующий"
        /// </summary>
        private void NextButtonClick(object sender, RoutedEventArgs e)
        {
            // вызываем проверку введенного кода бронирования
            var res = CheckCode();
           
            if (ErrorCode == 0)
            {
                // ввод номера телефона
                Wizard.Navigate("PhoneNumber4");
            }
            else // код брони не найден
            if (ErrorCode == 1)
            {
                Wizard.Navigate("ErrorMessageOut");
            }
            else // время слота просрочено
            if (ErrorCode == 2)
            {
                Wizard.Navigate("ErrorMessageOut2");
            }
            else // уже есть регистрация
            if (ErrorCode == 3)
            {
                Wizard.Navigate("ErrorMessageOut");
            }

            //if (res == true)
            //{
            //    if (IdTs > 0)
            //    {
            //        Wizard.Navigate("InformationOutput");
            //    }
            //    else if (Rmbu_Id > 0)
            //    {
            //        Wizard.Navigate("CarModel4");
            //    }
            //}
            //else
            //{
            //    Wizard.Navigate("ErrorMessageOut");
            //}
        }

        /// <summary>
        /// проверка введенного кода брони и получение id_ts машины
        /// </summary>
        /// <returns></returns>
        private bool CheckCode()
        {
            var resume = true;
            var p = new Dictionary<string, string>();
            var v = Wizard.Values;

            ErrorCode = -1;

            p.Add("CODE", v.CheckGet("BOOKING_CODE"));

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "PaperProduction");
                q.Request.SetParam("Object", "TransportDriver");
                q.Request.SetParam("Action", "CheckCodeNew");

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
                        ErrorCode = ds.GetFirstItemValueByKey("ERROR").ToInt();
                        Wmts_Id = ds.GetFirstItemValueByKey("WMTS_ID").ToInt();

                        // получаем информацию по машине
                        ds = ListDataSet.Create(result, "INFO");
                        var fio = ds.GetFirstItemValueByKey("DRIVER_NAME").ToString();
                        var marka = ds.GetFirstItemValueByKey("MARKA").ToString();
                        var car_num = ds.GetFirstItemValueByKey("CAR_NUM").ToString();
                        var phone = ds.GetFirstItemValueByKey("PHONE").ToString();
                        var id_d = ds.GetFirstItemValueByKey("ID_D").ToInt().ToString();
                        var dttm = ds.GetFirstItemValueByKey("DATE_UNLOADING").ToString();
                        var id_ts = ds.GetFirstItemValueByKey("ID_TS").ToInt().ToString();
                        var rmbu_id = ds.GetFirstItemValueByKey("RMBU_ID").ToInt().ToString();
                        var code = ds.GetFirstItemValueByKey("CODE").ToString();
                        var idpost = ds.GetFirstItemValueByKey("ID_POST").ToInt().ToString();
                        var name_post = ds.GetFirstItemValueByKey("POST_NAME").ToString();

                        IdTs = id_ts.ToInt();
                        Rmbu_Id = rmbu_id.ToInt();

                        Form.SetValueByPath("WMTS_ID", Wmts_Id.ToString());
                        Form.SetValueByPath("FIO", fio);
                        Form.SetValueByPath("MARKA_CAR", marka);
                        Form.SetValueByPath("NUMBER_CAR", car_num);
                        Form.SetValueByPath("PHONE3", phone);
                        Form.SetValueByPath("ID_D", id_d);
                        Form.SetValueByPath("DTTM_SLOT", dttm);
                        Form.SetValueByPath("ID_TS", id_ts);
                        Form.SetValueByPath("RMBU_ID", rmbu_id);
                        Form.SetValueByPath("BOOKING_CODE", code);
                        Form.SetValueByPath("VENDOR_ID", idpost);
                        Form.SetValueByPath("VENDOR_NAME", name_post);
                        Form.SetValueByPath("REGISTRATION", "0");

                        if (ErrorCode == 0)
                        {
                            Form.SetValueByPath("ERROR_INFO", "");
                            //REGISTRATION

                             var msg = "";

                            if (IdTs > 0)
                            {
                                msg = $"Самовывоз. Код брони {code} найден. Машина {marka} {car_num}. Водитель {fio}. Телефон {phone}. Поставщик {name_post}. IdTs = {IdTs}. Wmts_id = {Wmts_Id}";
                            }
                            else if (Rmbu_Id > 0)
                            {
                                msg = $"Доставка.  Код брони {code} найден. Машина {marka} {car_num}. Водитель {fio}. Телефон {phone}. Поставщик {name_post}. Rmbu_Id = {Rmbu_Id}. Wmts_id = {Wmts_Id}";
                            }
                            SaveLog(msg);
                        }
                        else
                        if (ErrorCode == 1)
                        {
                            Form.SetValueByPath("ERROR_INFO", $"Введенный код {v.CheckGet("BOOKING_CODE")} не найден.");
                            resume = false;
                            var msg = $"Код брони {v.CheckGet("BOOKING_CODE")} не найден.";
                            SaveLog(msg);
                        }
                        else
                        if (ErrorCode == 2)
                        {
                            Form.SetValueByPath("ERROR_INFO", "Время " + dttm + " слота просрочено.");
                            resume = false;
                            var msg = $"Код брони {code} найден. Время {dttm} слота просрочено. Wmts_id = {Wmts_Id}";
                            SaveLog(msg);
                            Form.SetValueByPath("REGISTRATION", "1");
                        }
                        else
                        if (ErrorCode == 3)
                        {
                            Form.SetValueByPath("ERROR_INFO", "Регистрация на разгрузку уже сделана.");
                            resume = false;
                            var msg = $"Код брони {v.CheckGet("BOOKING_CODE")} найден. Регистрация на разгрузку уже сделана.";
                            SaveLog(msg);
                        }
                        SaveValues();
                    }
                }
                else
                {
                    q.ProcessError();
                    resume = false;
                }
            }
            return resume;
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

    }
}
