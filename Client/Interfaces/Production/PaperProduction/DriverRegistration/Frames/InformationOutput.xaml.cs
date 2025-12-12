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
using System.IO;
using System.Reflection;
using NPOI.SS.Formula.Functions;
using DevExpress.Utils.IoC;


namespace Client.Interfaces.Production
{
    /// <summary>
    /// проверка данных полученных по коду бронирования
    /// </summary>
    /// <author>Грешных Н.И.</author>
    /// <version>1</version>
    public partial class InformationOutput : WizardFrame
    {
        public InformationOutput()
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
                    Path="ERROR_INFO",
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
                    Control=FioText,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    First=true,
                },
                new FormHelperField()
                {
                    Path="PHONE3",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=PhoneNumberText,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="MARKA_CAR",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CarModelDescriptionText,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NUMBER_CAR",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CarNumberText,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DTTM_SLOT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=DttmSlootText,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="BOOKING_CODE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control="void",
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
                    Path="WMST_ID",
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
                    Path="VENDOR_NAME",
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

                            var w = Wizard.Values;
                            var cod = w.CheckGet("BOOKING_CODE");
                            var fio = w.CheckGet("FIO"); 
                            var phone = w.CheckGet("PHONE3"); 
                            var avto = w.CheckGet("MARKA_CAR"); 
                            var nomer = w.CheckGet("NUMBER_CAR"); 
                            var dtSlota = w.CheckGet("DTTM_SLOT");
                            var id_ts = w.CheckGet("ID_TS");
                            var rmbu_id = w.CheckGet("RMBU_ID");
                            var wmst_id = w.CheckGet("WMST_ID");
                            var id_post = w.CheckGet("VENDOR_ID");
                            var name_post = w.CheckGet("VENDOR_NAME");
                            var reg_flag = w.CheckGet("REGISTRATION").ToInt();

                            if (reg_flag == 1) 
                            {
                                DttmSlootText.Text = "";
                            }
                            
                            var msg = "";

                            if (id_ts.ToInt() > 0)
                            {
                                msg = $"Самовывоз. Код брони {cod}. Info Машина {avto} {nomer}. Водитель {fio}. Телефон {phone}. Поставщик {name_post}. Дата слота {dtSlota}. IdTs = {id_ts}. Wmst_id = {wmst_id}. IdPost = {id_post}";
                            }
                            else if (rmbu_id.ToInt() > 0)
                            {
                                msg = $"Доставка.  Код брони {cod}. Info Машина {avto} {nomer}. Водитель {fio}. Телефон {phone}. Поставщик {name_post}. Дата слота {dtSlota}. Rrmbu_id = {rmbu_id}. Wmst_id = {wmst_id}. IdPost = {id_post}";
                            }

                            SaveLog(msg);
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
            NextButtonSet(true);
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
            Wizard.Navigate("PhoneNumber4");
        }

        /// <summary>
        /// нажали кнопку "Следующий"
        /// </summary>
        private void NextButtonClick(object sender, RoutedEventArgs e)
        {
            Wizard.Navigate("ConfirmSms3");
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
