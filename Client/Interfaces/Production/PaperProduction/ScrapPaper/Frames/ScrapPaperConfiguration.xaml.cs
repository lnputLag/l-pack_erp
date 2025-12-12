using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
using Client.Interfaces.Production.Corrugator;
using DevExpress.XtraPrinting.Native;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
using static DevExpress.Mvvm.Native.TaskLinq;

namespace Client.Interfaces.Production.PaperProduction
{
    /// <summary>
    /// редактирование настроек программы
    /// </summary>
    /// <author>greshnyh_ni</author>
    public partial class ScrapPaperConfiguration : ControlBase
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

        public string Note { get; set; }

        /// <summary>
        ///  ИД записи в SCRAP_PZ
        /// </summary>
        private int ScpzId { get; set; }

        private int IdSt { get; set; }
        private int IdTimes { get; set; }

        public Dictionary<string, string> Values { get; set; }

        public delegate void OnCloseDelegate();
        public OnCloseDelegate OnClose;

        public ScrapPaperConfiguration()
        {
            InitializeComponent();


            ControlSection = "scrap_paper";
            //  RoleName = "[erp]developer";
            DocumentationUrl = "/doc/l-pack-erp/production/molded_container/machine_control";

            FrameName = ControlName;

            FormInit();

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnKeyPressed = (KeyEventArgs e) =>
            {

            };

            OnLoad = () =>
            {
            };

            OnUnload = () =>
            {

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
                        Name = "ok",
                        Enabled = true,
                        Title = "ОК",
                        ButtonUse = true,
                        ButtonName = "SaveButton",
                      //  HotKey = "Enter",
                        Action = () =>
                        {
                            Save();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "cancel",
                        Enabled = true,
                        Title = "Отмена",
                        ButtonUse = true,
                        ButtonName = "CancelButton",
                        HotKey = "Escape",
                        Action = () =>
                        {
                            Close();
                        },
                    });
                }

                Commander.Init(this);
            }

            Values = new Dictionary<string, string>();


            EditNumberBale.IsEnabled = false;
            ScrapTransportCategoryControlCheckBox.IsEnabled = false;
            EditComPortBdm1.IsEnabled = false;
            EditComPortBdm2.IsEnabled = false;
            EditLayrentIpBdm1.IsEnabled = false;
            EditLayrentIpBdm2.IsEnabled = false;

            if (Central.User.Login == "greshnyh_ni")
            {
                EditNumberBale.IsEnabled = true;
                ScrapTransportCategoryControlCheckBox.IsEnabled = true;
                SmsVisibleControlCheckBox.IsEnabled = true;
                EditComPortBdm1.IsEnabled = true;
                EditComPortBdm2.IsEnabled = true;
                EditLayrentIpBdm1.IsEnabled = true;
                EditLayrentIpBdm2.IsEnabled = true;
            }

            if (Central.User.Login == "fedyanina_ev")
            {
                EditNumberBale.IsEnabled = true;
                ScrapTransportCategoryControlCheckBox.IsEnabled = true;
            }
        }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        private void FormInit()
        {
            Form = new FormHelper();

            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="BDM_CONTROL_POST_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ScrapTransportCategoryControlCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="POLIETILEN_IN_CELL_QTY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=EditNumberBale,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="BDM_SMS_VISIBLE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=SmsVisibleControlCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="COM_PORT_BDM_1",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=EditComPortBdm1,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="COM_PORT_BDM_2",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=EditComPortBdm2,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="LAYRENT_IP_BDM_1",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=EditLayrentIpBdm1,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="LAYRENT_IP_BDM_2",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=EditLayrentIpBdm2,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

            };
            Form.SetFields(fields);
        }

        public void SetDefaults()
        {
            Form.SetDefaults();
            FormStatus.Visibility = Visibility.Hidden;
        }

        public void Edit()
        {
            FrameTitle = $"Изменение настроек";
            GetData();
           
        }

        /// <summary>
        /// получение данных с сервера
        /// </summary>
        public async void GetData()
        {
            DisableControls();

            bool resume = true;

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "ProductionPm");
                q.Request.SetParam("Object", "ScrapPaper");
                q.Request.SetParam("Action", "ConfigurationOptionsGet");

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
                        //Form.SetValues(ds);
                        foreach (Dictionary<string, string> row in ds.Items)
                        {
                            var val = row.CheckGet("PARAM_VALUE").ToString();
                            var id = row.CheckGet("COOP_ID").ToInt();

                            switch (id)
                            {
                                case 108:
                                    EditNumberBale.Text = val;
                                    break;
                                case 118:
                                    EditComPortBdm1.Text = val;
                                    break;
                                case 119:
                                    EditComPortBdm2.Text = val;
                                    break;
                                case 120:
                                    EditLayrentIpBdm1.Text = val;
                                    break;
                                case 121:
                                    EditLayrentIpBdm2.Text = val;
                                    break;
                                case 242:
                                    ScrapTransportCategoryControlCheckBox.IsChecked = val.ToBool();
                                    break;
                                case 243:
                                    SmsVisibleControlCheckBox.IsChecked = val.ToBool();
                                    break;
                                default:
                                    break;
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
        /// Изменение записи 
        /// </summary>
        public async void UpdateRecord()
        {
            DisableControls();

            string emplId = Central.User.EmployeeId.ToString();

            var v = Form.GetValues();

            var p = new Dictionary<string, string>();
            if (ScrapTransportCategoryControlCheckBox.IsChecked == true)
                p.CheckAdd("VALUE1", "1");
            else
                p.CheckAdd("VALUE1", "0");

            p.CheckAdd("VALUE2", v.CheckGet("POLIETILEN_IN_CELL_QTY").ToString());

            if (SmsVisibleControlCheckBox.IsChecked == true)
                p.CheckAdd("VALUE3", "1");
            else
                p.CheckAdd("VALUE3", "0");

            p.CheckAdd("VALUE4", v.CheckGet("COM_PORT_BDM_1").ToString());
            p.CheckAdd("VALUE5", v.CheckGet("COM_PORT_BDM_2").ToString());
            p.CheckAdd("VALUE6", v.CheckGet("LAYRENT_IP_BDM_1").ToString());
            p.CheckAdd("VALUE7", v.CheckGet("LAYRENT_IP_BDM_2").ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "ScrapPaper");
            q.Request.SetParam("Action", "ConfigurationOptionsSave");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = "Production",
                    ReceiverName = ReceiverName,
                    SenderName = ControlName,
                    Action = "RefreshSetup",
                });

                Close();
            }
            else
            {
                Form.SetStatus(q.Answer.Error.Message, 1);
            }

            EnableControls();
        }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.Escape:
                    //   Close();
                    e.Handled = true;
                    break;
                case Key.Enter:
                   // Save();
                    e.Handled = true;
                    break;
            }
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

                if (resume)
                {
                    if (EditNumberBale.Text.IsNullOrEmpty())
                    {
                        errorMsg = "Не все поля заполнены верно";
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


   
        /////
    }
}
