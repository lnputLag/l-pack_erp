using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
using DevExpress.XtraPrinting.Native;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// редактирование записи технолога для операторов БДМ 
    /// </summary>
    /// <author>greshnyh_ni</author>
    public partial class LogbookDecisionRecord : ControlBase
    {
        private FormHelper Form { get; set; }

        /// <summary>
        /// Имя вкладки, откуда вызвана форма редактирования
        /// </summary>
        public string ReceiverName;

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        // Id записи в таблице stanok_logbook
        public int IdLogbook { get; set; }

        public LogbookDecisionRecord(int Id, string str)
        {
            InitializeComponent();
            IdLogbook = Id;
            DicisionTxt.Text = str;

            ControlSection = "paper_machine_control";
            //  RoleName = "[erp]developer";
            DocumentationUrl = "/doc/l-pack-erp/production/molded_container/machine_control";

            FrameName = ControlName;

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
                        HotKey = "Enter",
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

            double nScale = 1.5;
            GridParent.LayoutTransform = new ScaleTransform(nScale, nScale);
        }

        public void Edit()
        {
            FrameTitle = $"Редактирование записи №{IdLogbook.ToInt().ToString()}";
            Show();
        }

        /// <summary>
        /// Проверки перед записью данных в БД
        /// </summary>
        private void Save()
        {
            bool resume = true;
            string errorMsg = "";

            if (resume)
            {
                if (DicisionTxt.Text.IsNullOrEmpty())
                {
                    errorMsg = "Заполните рекомендацию.";
                    resume = false;
                }
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID_LOGBOOK", IdLogbook.ToString());
                    p.CheckAdd("DECISION", DicisionTxt.Text);
                }
                SaveData(p);
            }
            else
            {
                Form.SetStatus(errorMsg, 1);
            }
        }

        /// <summary>
        /// Запись данных в БД
        /// </summary>
        /// <param name="p"></param>
        private async void SaveData(Dictionary<string, string> p)
        {
            var q = new LPackClientQuery();

            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "Monitoring");
            q.Request.SetParam("Action", "DecisionSave");
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
                    Action = "RefreshDecision",
                });

                Close();
            }
            else if (q.Answer.Error.Code == 145)
            {
                Form.SetStatus(q.Answer.Error.Message, 1);
            }
        }
    }
}
