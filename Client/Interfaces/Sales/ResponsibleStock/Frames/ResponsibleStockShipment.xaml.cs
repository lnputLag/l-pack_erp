using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Shipments;
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
    /// Окно редактированя отгрузки
    /// </summary>
    public partial class ResponsibleStockShipment : UserControl
    {
        public ResponsibleStockShipment()
        {
            FrameName = "ResponsibleStockShipment";

            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            Init();
            SetDefaults();

            if (Central.DebugMode)
            {
                DriverIdTextBox.Visibility = Visibility.Visible;
                TransporterIdTextBox.Visibility = Visibility.Visible;
            }
        }

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
        /// Датасет с данными для заполнения полей формы для существующей отгрузки
        /// </summary>
        public ListDataSet FormDataSet { get; set; }

        /// <summary>
        /// nrz.nsthet
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// id_ts
        /// </summary>
        public int TransportId { get; set; }

        public bool EditFlag { get; set; }

        /// <summary>
        /// dt
        /// </summary>
        public string Date { get; set; }

        /// <summary>
        /// tm
        /// </summary>
        public string Time { get; set; }

        /// <summary>
        /// NrCount
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Самовывоз. 1 = Да
        /// </summary>
        public string SelfShip { get; set; }

        public string NoteLogistician { get; set; }

        public int Otgr { get; set; }

        /// <summary>
        /// ID_PROD
        /// Идентификатор юридической сущности организации
        /// </summary>
        public int OrganizationId { get; set; }

        private bool TransferAllowedFlag { get; set; }

        private bool UnshippedFlag { get; set; }

        private int TransportCount { get; set; }

        /// <summary>
        /// Данные по выбранному водителю
        /// </summary>
        public Dictionary<string, string> SelectedDriver { get; set; }

        /// <summary>
        /// Идентификатор ведителя, полученный из бд или установленный по умолчанию
        /// Нужен для проверки того, что выбранный водитель поменялся
        /// </summary>
        public int DriverDefaultId { get; set; }

        /// <summary>
        /// Тип заявки:
        /// 1 -- Заявка для поставки на СОХ
        /// 2 -- Заявка для отгрузки заказа ИМ из Липецка
        /// </summary>
        public int OrderType { get; set; }

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
                    Path="DATE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=DateTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TIME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TimeTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DRIVER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=DriverTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TRANSPORT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="LOAD",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=NoteTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NOTE_LOGISTICIAN",
                    FieldType=FormHelperField.FieldTypeRef.String,
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
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();

            FormDataSet = new ListDataSet();
            SelectedDriver = new Dictionary<string, string>();
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

            Central.WM.Show(FrameName, "Отгрузка", true, "add", this);

            if (EditFlag)
            {
                if (TransportId > 0)
                {
                    GetData();
                }
            }
            else
            {
                SetDefaultData();
            }
        }

        /// <summary>
        /// Получение данных по существующей отгрузке
        /// </summary>
        public void GetData()
        {
            DisableControls();

            var p = new Dictionary<string, string>();
            p.Add("IDTS", TransportId.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "ResponsibleStock");
            q.Request.SetParam("Action", "GetShipment");
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
                    FormDataSet = dataSet;
                    var firstDictionary = FormDataSet.Items.First();

                    string date = firstDictionary.CheckGet("DATETS").ToDateTime().ToString("dd.MM.yyyy");
                    string time = firstDictionary.CheckGet("TMSHIP").ToDateTime().ToString("HH:mm");

                    DateTextBox.Text = date;
                    TimeTextBox.Text = time;
                    Date = firstDictionary.CheckGet("DATETS");
                    Time = firstDictionary.CheckGet("TMSHIP");

                    TransferAllowedFlag = firstDictionary.CheckGet("TRANSFER_ALLOWED_FLAG").ToBool();
                    UnshippedFlag = firstDictionary.CheckGet("UNSHIPPED_FLAG").ToBool();
                    NoteTextBox.Text = firstDictionary.CheckGet("COMMENTS");
                    Form.SetValueByPath("LOAD", firstDictionary.CheckGet("LOADED"));

                    DriverIdTextBox.Text = firstDictionary.CheckGet("ID_DR").ToInt().ToString();
                    DriverDefaultId = firstDictionary.CheckGet("ID_DR").ToInt();

                    TransporterIdTextBox.Text = firstDictionary.CheckGet("ID_TR").ToInt().ToString();
                    TransporterTextBox.Text = firstDictionary.CheckGet("TRANSPORTER_NAME").ToString();

                    Form.SetValueByPath("NOTE_LOGISTICIAN", NoteLogistician);
                }
            }
            else
            {
                q.ProcessError();
            }

            EnableControls();
        }

        /// <summary>
        /// Установка значений по умолчанию для новой отгрузки
        /// </summary>
        public void SetDefaultData()
        {
            TransferAllowedFlag = false;
            UnshippedFlag = false;

            if (string.IsNullOrEmpty(Date))
            {
                Form.SetValueByPath("DATE", DateTime.Now.ToString("dd.MM.yyyy"));
            }
            else
            {
                Form.SetValueByPath("DATE", Date.ToDateTime().ToString("dd.MM.yyyy"));
            }

            if (string.IsNullOrEmpty(Time))
            {
                TimeTextBox.Text = "08:00";
            }
            else 
            {
                TimeTextBox.Text = Time.ToDateTime().ToString("HH:mm"); 
            }

            if (SelfShip.ToInt() == 1)
            {
                DriverIdTextBox.Text = "1095";
                DriverDefaultId = 1095;

                SelectDriverButton.IsEnabled = false;
                SelectTranporterButton.IsEnabled = false;
            }
            else
            {
                DriverIdTextBox.Text = "2659";
                DriverDefaultId = 2659;

                SelectDriverButton.IsEnabled = true;
                SelectTranporterButton.IsEnabled = true;
            }

            if (!(OrganizationId > 0))
            {
                OrganizationId = 1;
            }

            Form.SetValueByPath("NOTE_LOGISTICIAN", NoteLogistician);
        }

        public void Save()
        {
            if (Form.Validate())
            {
                if (!string.IsNullOrEmpty(DateTextBox.Text) && !string.IsNullOrEmpty(TimeTextBox.Text) && SelectedDriver != null && SelectedDriver.Count > 0)
                {
                    if (EditFlag)
                    {
                        if (Date.ToDateTime().Date < DateTextBox.Text.ToDateTime())
                        {
                            var p = new Dictionary<string, string>();
                            p.Add("IDTS", TransportId.ToString());
                            p.Add("DT_FROM", Date);
                            p.Add("DT_TO", DateTextBox.Text);

                            var q = new LPackClientQuery();
                            q.Request.SetParam("Module", "Sales");
                            q.Request.SetParam("Object", "ResponsibleStock");
                            q.Request.SetParam("Action", "CheckProductionTaskForShipment");
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
                                    if (dataSet != null && dataSet.Items.Count > 0)
                                    {
                                        var functionResult = dataSet.Items.First().CheckGet("RESULT").ToInt();
                                        if (functionResult == 0)
                                        {
                                            string msg = $"Для позиции в данной отгрузке создано ПЗ. Обратитесь в ОПП для отвязывания позиции от ПЗ.{Environment.NewLine}После этого вы сможете изменить дату.";
                                            var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                                            d.ShowDialog();

                                            return;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                q.ProcessError();
                            }
                        }

                        if (SelectedDriver != null && SelectedDriver.Count > 0)
                        {
                            if (SelectedDriver.CheckGet("UNKNOWN_DRIVER").ToInt() == 0
                                && SelfShip.ToInt() == 0
                                && (
                                SelectedDriver.CheckGet("L").ToInt() == 0
                                || SelectedDriver.CheckGet("B").ToInt() == 0
                                || SelectedDriver.CheckGet("H").ToInt() == 0
                                )
                                )
                            {
                                string msg = "У транспортного средства отсутствуют габариты.";
                                var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                                d.ShowDialog();

                                return;
                            }
                        }

                        if (Date.ToDateTime().Date != DateTextBox.Text.ToDateTime()) // в делфи тут ещё проверка на роль [p]logist
                        {
                            string msg = "У отгрузки изменилась дата. Вы действительно подтверждаете изменения?";
                            if (DialogWindow.ShowDialog(msg, "Склад ответственного хранения", "", DialogWindowButtons.YesNo) == false)
                            {
                                return;
                            }
                        }

                        if (!(TransportId > 0) || UnshippedFlag || Date.ToDateTime().Date != DateTextBox.Text.ToDateTime() || Time.ToDateTime().ToString("HH:mm") != TimeTextBox.Text)
                        {
                            if (Otgr == 0
                                && (
                                (TimeTextBox.Text.ToDateTime().Hour == 0 && TransportCount >= 1)
                                || (TimeTextBox.Text.ToDateTime().Hour == 1 && TransportCount >= 1)
                                || (TimeTextBox.Text.ToDateTime().Hour == 2 && TransportCount >= 1)
                                || (TimeTextBox.Text.ToDateTime().Hour == 3 && TransportCount >= 1)
                                || (TimeTextBox.Text.ToDateTime().Hour == 4 && TransportCount >= 1)
                                || (TimeTextBox.Text.ToDateTime().Hour == 5 && TransportCount >= 1)
                                || (TimeTextBox.Text.ToDateTime().Hour == 6 && TransportCount >= 1)
                                || (TimeTextBox.Text.ToDateTime().Hour == 7 && TransportCount >= 1)
                                || (TimeTextBox.Text.ToDateTime().Hour == 8 && TransportCount >= 1)
                                || (TimeTextBox.Text.ToDateTime().Hour == 9 && TransportCount >= 1)
                                || (TimeTextBox.Text.ToDateTime().Hour == 10 && TransportCount >= 1)
                                || (TimeTextBox.Text.ToDateTime().Hour == 11 && TransportCount >= 1)
                                || (TimeTextBox.Text.ToDateTime().Hour == 12 && TransportCount >= 1)
                                || (TimeTextBox.Text.ToDateTime().Hour == 13 && TransportCount >= 1)
                                || (TimeTextBox.Text.ToDateTime().Hour == 14 && TransportCount >= 1)
                                || (TimeTextBox.Text.ToDateTime().Hour == 15 && TransportCount >= 1)
                                || (TimeTextBox.Text.ToDateTime().Hour == 16 && TransportCount >= 1)
                                || (TimeTextBox.Text.ToDateTime().Hour == 17 && TransportCount >= 1)
                                || (TimeTextBox.Text.ToDateTime().Hour == 18 && TransportCount >= 1)
                                || (TimeTextBox.Text.ToDateTime().Hour == 19 && TransportCount >= 1)
                                || (TimeTextBox.Text.ToDateTime().Hour == 20 && TransportCount >= 1)
                                || (TimeTextBox.Text.ToDateTime().Hour == 21 && TransportCount >= 1)
                                || (TimeTextBox.Text.ToDateTime().Hour == 22 && TransportCount >= 1)
                                || (TimeTextBox.Text.ToDateTime().Hour == 23 && TransportCount >= 1)
                                )
                                )
                            {
                                string msg = "На выбранное время поставить отгрузку нельзя из-за ограничений.";
                                var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                                d.ShowDialog();

                                return;
                            }
                            else if (Otgr == 1
                                && (
                                (TimeTextBox.Text.ToDateTime().Hour == 0 && TransportCount >= 4)
                                || (TimeTextBox.Text.ToDateTime().Hour == 1 && TransportCount >= 3)
                                || (TimeTextBox.Text.ToDateTime().Hour == 2 && TransportCount >= 3)
                                || (TimeTextBox.Text.ToDateTime().Hour == 3 && TransportCount >= 3)
                                || (TimeTextBox.Text.ToDateTime().Hour == 4 && TransportCount >= 7)
                                || (TimeTextBox.Text.ToDateTime().Hour == 5 && TransportCount >= 7)
                                || (TimeTextBox.Text.ToDateTime().Hour == 6 && TransportCount >= 7)
                                || (TimeTextBox.Text.ToDateTime().Hour == 7 && TransportCount >= 7)
                                || (TimeTextBox.Text.ToDateTime().Hour == 8 && TransportCount >= 7)
                                || (TimeTextBox.Text.ToDateTime().Hour == 9 && TransportCount >= 7)
                                || (TimeTextBox.Text.ToDateTime().Hour == 10 && TransportCount >= 7)
                                || (TimeTextBox.Text.ToDateTime().Hour == 11 && TransportCount >= 8)
                                || (TimeTextBox.Text.ToDateTime().Hour == 12 && TransportCount >= 8)
                                || (TimeTextBox.Text.ToDateTime().Hour == 13 && TransportCount >= 8)
                                || (TimeTextBox.Text.ToDateTime().Hour == 14 && TransportCount >= 8)
                                || (TimeTextBox.Text.ToDateTime().Hour == 15 && TransportCount >= 8)
                                || (TimeTextBox.Text.ToDateTime().Hour == 16 && TransportCount >= 8)
                                || (TimeTextBox.Text.ToDateTime().Hour == 17 && TransportCount >= 10)
                                || (TimeTextBox.Text.ToDateTime().Hour == 18 && TransportCount >= 10)
                                || (TimeTextBox.Text.ToDateTime().Hour == 19 && TransportCount >= 10)
                                || (TimeTextBox.Text.ToDateTime().Hour == 20 && TransportCount >= 10)
                                || (TimeTextBox.Text.ToDateTime().Hour == 21 && TransportCount >= 10)
                                || (TimeTextBox.Text.ToDateTime().Hour == 22 && TransportCount >= 8)
                                || (TimeTextBox.Text.ToDateTime().Hour == 23 && TransportCount >= 7)
                                )
                                )
                            {
                                string msg = "На выбранное время поставить отгрузку нельзя из-за ограничений.";
                                var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                                d.ShowDialog();

                                return;
                            }
                        }

                        if (DriverDefaultId != DriverIdTextBox.Text.ToInt())
                        {
                            if (SelectedDriver.CheckGet("L").ToInt() > 0
                                && SelectedDriver.CheckGet("B").ToInt() > 0
                                && SelectedDriver.CheckGet("H").ToInt() > 0)
                            {
                                string msg = "";

                                var p = new Dictionary<string, string>();
                                p.Add("IDTS", TransportId.ToString());

                                var q = new LPackClientQuery();
                                q.Request.SetParam("Module", "Sales");
                                q.Request.SetParam("Object", "ResponsibleStock");
                                q.Request.SetParam("Action", "GetTransportPackage");
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
                                        if (dataSet != null && dataSet.Items.Count > 0)
                                        {
                                            int packet = 0;
                                            foreach (var item in dataSet.Items)
                                            {
                                                packet = 0;

                                                if (item.CheckGet("LT").ToInt() >= SelectedDriver.CheckGet("B").ToInt() && item.CheckGet("BT").ToInt() >= SelectedDriver.CheckGet("B").ToInt())
                                                {
                                                    packet += 1;
                                                }

                                                if (item.CheckGet("HT").ToInt() >= SelectedDriver.CheckGet("h").ToInt())
                                                {
                                                    packet += 2;
                                                }

                                                if (packet == 1)
                                                {
                                                    msg += $"Габариты транспортного пакета {item.CheckGet("NAME")} превышают ширину транспорта.{Environment.NewLine}";
                                                }
                                                else if (packet == 2)
                                                {
                                                    msg += $"Габариты транспортного пакета {item.CheckGet("NAME")} превышают высоту транспорта.{Environment.NewLine}";
                                                }
                                                else if (packet == 3)
                                                {
                                                    msg += $"Габариты транспортного пакета {item.CheckGet("NAME")} превышают ширину и высоту транспорта.{Environment.NewLine}";
                                                }
                                            }

                                            if (!string.IsNullOrEmpty(msg))
                                            {
                                                msg += "Вы действительно хотите изменить водителя?";
                                                if (DialogWindow.ShowDialog(msg, "Склад ответственного хранения", "", DialogWindowButtons.YesNo) == false)
                                                {
                                                    return;
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

                            {
                                var p = new Dictionary<string, string>();
                                p.Add("IDTS", TransportId.ToString());

                                var q = new LPackClientQuery();
                                q.Request.SetParam("Module", "Sales");
                                q.Request.SetParam("Object", "ResponsibleStock");
                                q.Request.SetParam("Action", "UpdateDriverLog");
                                q.Request.SetParams(p);

                                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                                q.DoQuery();

                                if (q.Answer.Status == 0)
                                {
                                    bool succesfullFlag = false;

                                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                                    if (result != null)
                                    {
                                        var dataSet = ListDataSet.Create(result, "ITEMS");
                                        if (dataSet != null && dataSet.Items.Count > 0)
                                        {
                                            var idTs = dataSet.Items.First().CheckGet("IDTS").ToInt();
                                            if (idTs > 0)
                                            {
                                                succesfullFlag = true;
                                            }
                                        }
                                    }

                                    if (!succesfullFlag)
                                    {
                                        string msg = "Ошибка обновления данных водителя.";
                                        var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                                        d.ShowDialog();

                                        return;
                                    }
                                }
                                else
                                {
                                    q.ProcessError();
                                }
                            }

                            {
                                var p = new Dictionary<string, string>();
                                p.Add("IDTS", TransportId.ToString());

                                var q = new LPackClientQuery();
                                q.Request.SetParam("Module", "Sales");
                                q.Request.SetParam("Object", "ResponsibleStock");
                                q.Request.SetParam("Action", "UpdateTransportPayment");
                                q.Request.SetParams(p);

                                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                                q.DoQuery();

                                if (q.Answer.Status == 0)
                                {
                                    bool succesfullFlag = false;

                                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                                    if (result != null)
                                    {
                                        var dataSet = ListDataSet.Create(result, "ITEMS");
                                        if (dataSet != null && dataSet.Items.Count > 0)
                                        {
                                            var idTs = dataSet.Items.First().CheckGet("IDTS").ToInt();
                                            if (idTs > 0)
                                            {
                                                succesfullFlag = true;
                                            }
                                        }
                                    }

                                    if (!succesfullFlag)
                                    {
                                        string msg = "Ошибка обновления данных по оплате.";
                                        var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                                        d.ShowDialog();

                                        return;
                                    }
                                }
                                else
                                {
                                    q.ProcessError();
                                }
                            }
                        }

                        if (SelectedDriver != null && SelectedDriver.Count > 0)
                        {
                            if (DriverDefaultId != DriverIdTextBox.Text.ToInt() && SelectedDriver.CheckGet("UNKNOWN_DRIVER").ToInt() == 0)
                            {
                                var p = new Dictionary<string, string>();
                                p.Add("ID_D", DriverIdTextBox.Text);
                                p.Add("DATETS", DateTextBox.Text);

                                var q = new LPackClientQuery();
                                q.Request.SetParam("Module", "Sales");
                                q.Request.SetParam("Object", "ResponsibleStock");
                                q.Request.SetParam("Action", "GetShipmentsByDriver");
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
                                        if (dataSet != null && dataSet.Items.Count > 0)
                                        {
                                            string customerId = dataSet.Items.First().CheckGet("NAME_POK");

                                            if (customerId != null && !string.IsNullOrEmpty(customerId))
                                            {
                                                string msg = "Данный водитель уже привязан к отгрузке.";
                                                var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                                                d.ShowDialog();
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

                        {
                            var p = new Dictionary<string, string>();
                            p.Add("DATETS", $"{DateTextBox.Text} 00:00:00");
                            p.Add("TMSHIP", $"01.01.1900 {TimeTextBox.Text}");
                            p.Add("ID_DR", DriverIdTextBox.Text);
                            p.Add("IDTS", TransportId.ToString());
                            p.Add("COMMENTS", NoteTextBox.Text);
                            p.Add("ID_TR", TransporterIdTextBox.Text);
                            p.Add("VEHICLE_TYPE", "1");

                            var q = new LPackClientQuery();
                            q.Request.SetParam("Module", "Sales");
                            q.Request.SetParam("Object", "ResponsibleStock");
                            q.Request.SetParam("Action", "UpdateTransport");
                            q.Request.SetParams(p);

                            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                            q.DoQuery();

                            if (q.Answer.Status == 0)
                            {
                                bool succesfullflag = false;

                                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                                if (result != null)
                                {
                                    var dataSet = ListDataSet.Create(result, "ITEMS");
                                    if (dataSet != null && dataSet.Items.Count > 0)
                                    {
                                        int idTs = dataSet.Items.First().CheckGet("IDTS").ToInt();

                                        if (idTs > 0)
                                        {
                                            succesfullflag = true;
                                        }
                                    }
                                }

                                if (!succesfullflag)
                                {
                                    string msg = "Ошибка обновления данных по отгрузке.";
                                    var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                                    d.ShowDialog();

                                    return;
                                }
                                // Если всё выполнилось успешно, то зыкрываем окно
                                else
                                {
                                    // отправляем сообщение гриду заявок обновиться
                                    {
                                        Messenger.Default.Send(new ItemMessage()
                                        {
                                            ReceiverGroup = "ResponsibleStock",
                                            ReceiverName = "ResponsibleStockList",
                                            SenderName = "ResponsibleStockShipment",
                                            Action = "Refresh",
                                            Message = "",
                                        }
                                        );
                                    }

                                    string msg = "Успешное изменение данных по отгрузке.";
                                    var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                                    d.ShowDialog();

                                    Close();
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
                        if (Otgr == 0
                            && (
                            (TimeTextBox.Text.ToDateTime().Hour == 0 && TransportCount >= 1)
                            || (TimeTextBox.Text.ToDateTime().Hour == 1 && TransportCount >= 1)
                            || (TimeTextBox.Text.ToDateTime().Hour == 2 && TransportCount >= 1)
                            || (TimeTextBox.Text.ToDateTime().Hour == 3 && TransportCount >= 1)
                            || (TimeTextBox.Text.ToDateTime().Hour == 4 && TransportCount >= 1)
                            || (TimeTextBox.Text.ToDateTime().Hour == 5 && TransportCount >= 1)
                            || (TimeTextBox.Text.ToDateTime().Hour == 6 && TransportCount >= 1)
                            || (TimeTextBox.Text.ToDateTime().Hour == 7 && TransportCount >= 1)
                            || (TimeTextBox.Text.ToDateTime().Hour == 8 && TransportCount >= 1)
                            || (TimeTextBox.Text.ToDateTime().Hour == 9 && TransportCount >= 1)
                            || (TimeTextBox.Text.ToDateTime().Hour == 10 && TransportCount >= 1)
                            || (TimeTextBox.Text.ToDateTime().Hour == 11 && TransportCount >= 1)
                            || (TimeTextBox.Text.ToDateTime().Hour == 12 && TransportCount >= 1)
                            || (TimeTextBox.Text.ToDateTime().Hour == 13 && TransportCount >= 1)
                            || (TimeTextBox.Text.ToDateTime().Hour == 14 && TransportCount >= 1)
                            || (TimeTextBox.Text.ToDateTime().Hour == 15 && TransportCount >= 1)
                            || (TimeTextBox.Text.ToDateTime().Hour == 16 && TransportCount >= 1)
                            || (TimeTextBox.Text.ToDateTime().Hour == 17 && TransportCount >= 1)
                            || (TimeTextBox.Text.ToDateTime().Hour == 18 && TransportCount >= 1)
                            || (TimeTextBox.Text.ToDateTime().Hour == 19 && TransportCount >= 1)
                            || (TimeTextBox.Text.ToDateTime().Hour == 20 && TransportCount >= 1)
                            || (TimeTextBox.Text.ToDateTime().Hour == 21 && TransportCount >= 1)
                            || (TimeTextBox.Text.ToDateTime().Hour == 22 && TransportCount >= 1)
                            || (TimeTextBox.Text.ToDateTime().Hour == 23 && TransportCount >= 1)
                            )
                            )
                        {
                            string msg = "На выбранное время поставить отгрузку нельзя из-за ограничений.";
                            var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                            d.ShowDialog();

                            return;
                        }
                        else if (Otgr == 1
                                && (
                                (TimeTextBox.Text.ToDateTime().Hour == 0 && TransportCount >= 4)
                                || (TimeTextBox.Text.ToDateTime().Hour == 1 && TransportCount >= 3)
                                || (TimeTextBox.Text.ToDateTime().Hour == 2 && TransportCount >= 3)
                                || (TimeTextBox.Text.ToDateTime().Hour == 3 && TransportCount >= 3)
                                || (TimeTextBox.Text.ToDateTime().Hour == 4 && TransportCount >= 7)
                                || (TimeTextBox.Text.ToDateTime().Hour == 5 && TransportCount >= 7)
                                || (TimeTextBox.Text.ToDateTime().Hour == 6 && TransportCount >= 7)
                                || (TimeTextBox.Text.ToDateTime().Hour == 7 && TransportCount >= 7)
                                || (TimeTextBox.Text.ToDateTime().Hour == 8 && TransportCount >= 7)
                                || (TimeTextBox.Text.ToDateTime().Hour == 9 && TransportCount >= 7)
                                || (TimeTextBox.Text.ToDateTime().Hour == 10 && TransportCount >= 7)
                                || (TimeTextBox.Text.ToDateTime().Hour == 11 && TransportCount >= 8)
                                || (TimeTextBox.Text.ToDateTime().Hour == 12 && TransportCount >= 8)
                                || (TimeTextBox.Text.ToDateTime().Hour == 13 && TransportCount >= 8)
                                || (TimeTextBox.Text.ToDateTime().Hour == 14 && TransportCount >= 8)
                                || (TimeTextBox.Text.ToDateTime().Hour == 15 && TransportCount >= 8)
                                || (TimeTextBox.Text.ToDateTime().Hour == 16 && TransportCount >= 8)
                                || (TimeTextBox.Text.ToDateTime().Hour == 17 && TransportCount >= 10)
                                || (TimeTextBox.Text.ToDateTime().Hour == 18 && TransportCount >= 10)
                                || (TimeTextBox.Text.ToDateTime().Hour == 19 && TransportCount >= 10)
                                || (TimeTextBox.Text.ToDateTime().Hour == 20 && TransportCount >= 10)
                                || (TimeTextBox.Text.ToDateTime().Hour == 21 && TransportCount >= 10)
                                || (TimeTextBox.Text.ToDateTime().Hour == 22 && TransportCount >= 8)
                                || (TimeTextBox.Text.ToDateTime().Hour == 23 && TransportCount >= 7)
                                )
                                )
                        {
                            string msg = "На выбранное время поставить отгрузку нельзя из-за ограничений.";
                            var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                            d.ShowDialog();

                            return;
                        }

                        if (SelectedDriver != null && SelectedDriver.Count > 0)
                        {
                            if (SelectedDriver.CheckGet("UNKNOWN_DRIVER").ToInt() == 0)
                            {
                                var p = new Dictionary<string, string>();
                                p.Add("ID_D", DriverIdTextBox.Text);
                                p.Add("DATETS", DateTextBox.Text);

                                var q = new LPackClientQuery();
                                q.Request.SetParam("Module", "Sales");
                                q.Request.SetParam("Object", "ResponsibleStock");
                                q.Request.SetParam("Action", "GetShipmentsByDriver");
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
                                        if (dataSet != null && dataSet.Items.Count > 0)
                                        {
                                            string customerId = dataSet.Items.First().CheckGet("NAME_POK");

                                            if (customerId != null && !string.IsNullOrEmpty(customerId))
                                            {
                                                string msg = "Данный водитель уже привязан к отгрузке.";
                                                var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                                                d.ShowDialog();
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

                        {
                            var p = new Dictionary<string, string>();
                            p.Add("DATETS", $"{DateTextBox.Text} 00:00:00");
                            p.Add("TMSHIP", $"01.01.1900 {TimeTextBox.Text}");
                            p.Add("ID_DR", DriverIdTextBox.Text);
                            p.Add("COMMENTS", NoteTextBox.Text);
                            p.Add("ID_PROD_PAYMENT", OrganizationId.ToString());
                            p.Add("NSTHET", OrderId.ToString());
                            p.Add("VEHICLE_TYPE", "1");
                            p.Add("ID_TR", TransporterIdTextBox.Text);

                            if (OrderType == 1)
                            {
                                p.Add("KIND", "5");
                            }
                            else if (OrderType == 2)
                            {
                                p.Add("KIND", "0");
                            }

                            var q = new LPackClientQuery();
                            q.Request.SetParam("Module", "Sales");
                            q.Request.SetParam("Object", "ResponsibleStock");
                            q.Request.SetParam("Action", "SaveTransport");
                            q.Request.SetParams(p);

                            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                            q.DoQuery();

                            if (q.Answer.Status == 0)
                            {
                                bool succesfullflag = false;
                                int transportId = 0;

                                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                                if (result != null)
                                {
                                    var dataSet = ListDataSet.Create(result, "ITEMS");
                                    if (dataSet != null && dataSet.Items.Count > 0)
                                    {
                                        transportId = dataSet.Items.First().CheckGet("IDTS").ToInt();

                                        if (transportId > 0)
                                        {
                                            succesfullflag = true;
                                        }
                                    }
                                }

                                if (!succesfullflag)
                                {
                                    string msg = "Ошибка сохранения данных по отгрузке.";
                                    var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                                    d.ShowDialog();

                                    return;
                                }
                                // Если всё выполнилось успешно, то зыкрываем окно
                                else
                                {
                                    if (OrderType == 1)
                                    {
                                        if (CreateIncomingFileForFTP(transportId))
                                        {
                                            if (SendIncomingFileToFTP(OrderId.ToString()))
                                            {
                                                // отправляем сообщение гриду заявок обновиться
                                                {
                                                    Messenger.Default.Send(new ItemMessage()
                                                    {
                                                        ReceiverGroup = "ResponsibleStock",
                                                        ReceiverName = "ResponsibleStockList",
                                                        SenderName = "ResponsibleStockShipment",
                                                        Action = "Refresh",
                                                        Message = "",
                                                    }
                                                    );
                                                }

                                                Close();
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // отправляем сообщение гриду заявок обновиться
                                        {
                                            Messenger.Default.Send(new ItemMessage()
                                            {
                                                ReceiverGroup = "ResponsibleStock",
                                                ReceiverName = "ResponsibleStockList",
                                                SenderName = "ResponsibleStockShipment",
                                                Action = "Refresh",
                                                Message = "",
                                            }
                                            );
                                        }

                                        Close();
                                    }
                                }
                            }
                            else
                            {
                                q.ProcessError();
                            }
                        }

                        Date = DateTextBox.Text;
                    }
                }
                else
                {
                    string msg = "Заполните данные по отгрузке";
                    var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                string msg = "Не все поля заполнены корректно";
                var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }

        }

        /// <summary>
        /// Создание (предварительного) файла заявки на приход для отправки на сох по фтп
        /// </summary>
        public bool CreateIncomingFileForFTP(int transportId)
        {
            bool succesfulFlag = false;

            var p = new Dictionary<string, string>();
            p.Add("ID_TS", transportId.ToString());
            p.Add("NSTHET", OrderId.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "ResponsibleStock");
            q.Request.SetParam("Action", "CreateFileForFTP");
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

                    if (dataSet != null && dataSet.Items.Count > 0)
                    {
                        int id = dataSet.Items.First().CheckGet("ID_TS").ToInt();

                        if (id > 0)
                        {
                            succesfulFlag = true;
                        }
                    }
                }

                if (!succesfulFlag)
                {
                    string msg = $"Ошибка создания файла отгрузки №{OrderId} для выгрузки на FTP сервер";
                    var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
                else
                {
                    string msg = $"Успешное создание файла отгрузки №{OrderId} для выгрузки на FTP сервер";
                    var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                q.ProcessError();
            }

            return succesfulFlag;
        }

        /// <summary>
        /// Отправляем файл (предварительный) заявки на приход на FTP сервер
        /// </summary>
        public bool SendIncomingFileToFTP(string fileName)
        {
            bool succesfulFlag = false;
            //nsthet
            fileName = $"{fileName}.csv";

            var p = new Dictionary<string, string>();
            p.Add("FILE_NAME", fileName);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "ResponsibleStock");
            q.Request.SetParam("Action", "SendByFTP");
            q.Request.SetParams(p);

            q.Request.Timeout = 300000;
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
                        fileName = dataSet.Items.First().CheckGet("FILE_NAME");

                        if (!string.IsNullOrEmpty(fileName))
                        {
                            succesfulFlag = true;
                        }
                    }
                }

                if (!succesfulFlag)
                {
                    string msg = $"Ошибка отправления файла {fileName} на FTP сервер";
                    var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
                else
                {
                    string msg = $"Успешное отправление файла {fileName} на FTP сервера";
                    var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                q.ProcessError();
            }

            return succesfulFlag;
        }

        public void SelectDriver()
        {
            var i = new DriverListAll(true, "ResponsibleStock", "ResponsibleStockShipment", FrameName);
            i.Show();
        }

        public void SelectTransporter()
        {
            var i = new TransporterList(true, "ResponsibleStock", "ResponsibleStockShipment", FrameName);
            i.Show();
        }

        /// <summary>
        /// При выборе водителя получаем его данные
        /// </summary>
        public void GetDriverData()
        {
            if (DriverIdTextBox.Text.ToInt() > 0)
            {
                var p = new Dictionary<string, string>();
                p.Add("ID", DriverIdTextBox.Text);

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "ResponsibleStock");
                q.Request.SetParam("Action", "GetDriver");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    bool succesfullflag = false;

                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var dataSet = ListDataSet.Create(result, "ITEMS");
                        if (dataSet != null && dataSet.Items.Count > 0)
                        {
                            SelectedDriver = dataSet.Items.First();
                            DriverTextBox.Text = SelectedDriver.CheckGet("FIO");

                            succesfullflag = true;
                        }
                    }

                    if (!succesfullflag)
                    {
                        string msg = "Ошибка получения данных водителя.";
                        var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }
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
                SenderName = "ResponsibleStockShipment",
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
            Central.ShowHelp("/doc/l-pack-erp/");
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.IndexOf("ResponsibleStock") > -1)
            {
                if (m.ReceiverName.IndexOf("ResponsibleStockShipment") > -1)
                {
                    switch (m.Action)
                    {
                        case "SelectItem":
                            if (m.ContextObject != null)
                            {
                                var selectedDriver = (Dictionary<string, string>)m.ContextObject;
                                DriverTextBox.Text = selectedDriver.CheckGet("DRIVERNAME");
                                DriverIdTextBox.Text = selectedDriver.CheckGet("ID");
                            }
                            break;

                        case "SelectTransporter":
                            if (m.ContextObject != null)
                            {
                                var selectedTransporter = (Dictionary<string, string>)m.ContextObject;
                                TransporterTextBox.Text = selectedTransporter.CheckGet("TRANSPORTER_NAME");
                                TransporterIdTextBox.Text = selectedTransporter.CheckGet("TRANSPORTER_ID");
                            }
                            break;
                    }
                }
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void SelectDriverButton_Click(object sender, RoutedEventArgs e)
        {
            SelectDriver();
        }

        private void DriverIdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            GetDriverData();
        }

        private void SelectTranporterButton_Click(object sender, RoutedEventArgs e)
        {
            SelectTransporter();
        }

        private void TranporterIdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
