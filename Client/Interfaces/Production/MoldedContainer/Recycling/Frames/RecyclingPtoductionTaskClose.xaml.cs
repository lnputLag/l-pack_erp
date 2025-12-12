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
    /// Закрытие текущего активного задания для ЛТ
    /// </summary>
    /// <author>greshnyh_ni</author>
    /// <version>1</version>
    /// <released>2025-04-11</released>
    /// <changed>2025-04-11</changed>
    public partial class RecyclingPtoductionTaskClose : ControlBase
    {
        public RecyclingPtoductionTaskClose()
        {           
            InitializeComponent();
            
            ControlSection = "recycling_control";
            RoleName = "[erp]developer";
            ControlTitle = "Закрытие текущего ПЗ";
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
                    Path="ORDER_NOTE_GENERAL",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    ControlType="TextBox",
                    Control=OrderNoteGeneral,
                },
                new FormHelperField()
                {
                    Path="SUSPEND_NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="TASK_STATUS_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="TASK_NUMBER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="TASK_QUANTITY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="PRIHOD_QTY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="PRODUCTION_MACHINE_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="void",
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

                if (v.CheckGet("ORDER_NOTE_GENERAL").IsNullOrEmpty())
                {
                    resume = false;
                    error = "нет данных ORDER_NOTE_GENERAL";
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
        /// устанавливаем комментарий для текущего задания (prot_id)
        /// </summary>
        /// <param name="p"></param>
        public async void DataSave()
        {
            var complete = false;
            string error = "";
            var row = new Dictionary<string, string>();
            var v = Form.GetValues();

            var id = v.CheckGet("TASK_ID").ToInt();
            var status = v.CheckGet("TASK_STATUS_ID").ToInt();
            var num = v.CheckGet("TASK_NUMBER").ToString();
            var id_st = v.CheckGet("PRODUCTION_MACHINE_ID").ToInt();
            var suspend_note = v.CheckGet("SUSPEND_NOTE").ToString();

            Form.DisableControls();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Recycling");
            q.Request.SetParam("Action", "TaskNoteSave");

            var p = new Dictionary<string, string>();
            {
                p.Add("TASK_ID", v.CheckGet("TASK_ID").ToString());
                p.Add("ORDER_NOTE_GENERAL", v.CheckGet("ORDER_NOTE_GENERAL").ToString());
            }

            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {

                // закрываю задание

                var qq = new LPackClientQuery();
                qq.Request.SetParam("Module", "MoldedContainer");
                qq.Request.SetParam("Object", "Recycling");
                qq.Request.SetParam("Action", "Save");

                if ((status < 4) || (status == 8))
                {
                    qq.Request.SetParam("TASK_ID", id.ToString());
                }
                else
                {
                    qq.Request.SetParam("TASK_ID", "");
                }

                qq.Request.SetParam("PRODUCTION_MACHINE_ID", id_st.ToString());

                await Task.Run(() =>
                {
                    qq.DoQuery();
                });

                if (qq.Answer.Status == 0)
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
                    qq.ProcessError();
                }
            }
            else
            {
                error = q.GetError();
                LogMsg($"Ошибка записи примечания для задания {error}");
            }

            Form.EnableControls();
        }

    }
}
