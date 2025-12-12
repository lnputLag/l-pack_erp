using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Production.MoldedContainer
{
 
    /// <summary>
    /// Приостановка текущего активного задания для ЛТ
    /// </summary>
    /// <author>greshnyh_ni</author>
    /// <version>1</version>
    /// <released>2024-09-12</released>
    /// <changed>2024-09-12</changed>
    public partial class RecyclingPtoductionTaskSuspend : ControlBase
    {
        public RecyclingPtoductionTaskSuspend()
        {           
            InitializeComponent();
            
            ControlSection = "recycling_control";
            RoleName = "[erp]developer";
            ControlTitle = "Приостановка текущего ПЗ";
            DocumentationUrl = "/doc/l-pack-erp/production/molded_container/machine_control";

            FrameName = ControlName;
            PrimaryKeyValue = "0";

            OnMessage = (ItemMessage m) =>
            {
                if(m.ReceiverName == ControlName)
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
                        HotKey="Enter",
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
                    Commander.Add(new CommandItem()
                    {
                        Name = "key_pressed",
                        Enabled = true,
                        ActionMessage = (ItemMessage message) =>
                        {
                            var m = message.Message;
                            Form.ProcessExtInput(m);

                        },
                    });
                }

                Commander.Init(this);
            }

            Values = new Dictionary<string, string>();

            FormInit();
        }

        private FormHelper Form { get; set; }
        public Dictionary<string,string> Values { get; set; }
        /// <summary>
        /// Имя вкладки, окуда вызвана форма редактирования
        /// </summary>
        public string ReceiverName;

        private void FormInit()
        {
            Form = new FormHelper();

            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="TASK_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="SUSPEND_NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    ControlType="TextBox",
                    Control=SuspendNote,
                },                
            };
            Form.SetFields(fields);
            Form.StatusControl = FormStatus;            
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            Form.SetDefaults();
            FormStatus.Visibility = Visibility.Hidden; 
        }

        public void Create()
        {
            SetDefaults();
            FrameTitle = $"Задание {Values.CheckGet("TASK_ID").ToInt().ToString()}";
            Form.SetValues(Values);
            Show();
        }

        private void Save()
        {
            bool resume = true;
            string error = "";

            if (resume)
            {
                var validationResult = Form.Validate();
                if (!validationResult)
                {
                    resume = false;
                }
            }

            var v = Form.GetValues();
            if (resume)
            {
                if (v.CheckGet("TASK_ID").ToInt() == 0)
                {
                    resume = false;
                    error = "нет данных TASK_ID";
                }

                if (v.CheckGet("SUSPEND_NOTE").IsNullOrEmpty())
                {
                    resume = false;
                    error = "нет данных SUSPEND_NOTE";
                }
            }

            if (resume)
            {
                DataSave();
            }
            else
            {
                LogMsg($"Ошибка при проверке формы [{ControlName}] {error}");
                Form.SetStatus(error, 1);
            }
        }

       
        /// <summary>
        /// устанавливаем статус (8 - Приостановлено) для текущего задания (prot_id)
        /// </summary>
        /// <param name="p"></param>
        public async void DataSave()
        {
            var complete = false;
            string error = "";
            var row = new Dictionary<string, string>();
            var v = Form.GetValues();

            Form.DisableControls();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Recycling");
            q.Request.SetParam("Action", "TaskStatusSave");

            var p = new Dictionary<string, string>();
            {
                p.Add("TASK_ID", v.CheckGet("TASK_ID").ToString());
                p.Add("PRTS_ID", "8");
                p.Add("SUSPEND_NOTE", v.CheckGet("SUSPEND_NOTE").ToString());
            }

            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverName = ReceiverName,
                    SenderName = ControlName,
                    Action = "refresh",
                });

                Close();
            }
            else
            {
                //q.ProcessError();
                error = q.GetError();
                LogMsg($"Ошибка при смене статуса задания {error}");
            }

            Form.EnableControls();
        }



    }
}
