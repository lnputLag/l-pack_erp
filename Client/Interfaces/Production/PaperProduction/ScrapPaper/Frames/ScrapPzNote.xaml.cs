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

namespace Client.Interfaces.Production.PaperProduction
{
    /// <summary>
    /// редактирование истории композиции
    /// </summary>
    /// <author>greshnyh_ni</author>
    public partial class ScrapPzNote : ControlBase
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

        public ScrapPzNote(Dictionary<string, string> record = null)
        {
            InitializeComponent();

            if (record != null)
            {
                ScpzId = record.CheckGet("SCPZ_ID").ToInt();
                IdSt = record.CheckGet("ID_ST").ToInt();
                IdTimes = record.CheckGet("ID_TIMES").ToInt();
                ScrapPzNoteBox.Text = record.CheckGet("NOTE");
            }

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
                    Path="NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ScrapPzNoteBox,
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
            FrameTitle = $"Изменение записи №{ScpzId}";
            Show();
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
            p.CheckAdd("ID_TIMES", IdTimes.ToString());
            p.CheckAdd("ID_ST", IdSt.ToString());
            p.CheckAdd("SCPZ_ID", ScpzId.ToString());
            p.CheckAdd("EMPL_ID", emplId);
            p.CheckAdd("NOTE", v.CheckGet("NOTE"));

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "ScrapPaper");
            q.Request.SetParam("Action", "ScrapPzLogSave");

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
                    Action = "RefreshScrapPzLog",
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
                    if (ScrapPzNoteBox.Text.IsNullOrEmpty())
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
