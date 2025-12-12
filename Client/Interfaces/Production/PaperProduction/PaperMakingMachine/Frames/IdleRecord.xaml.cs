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
    /// редактирование записи причина простоя БДМ 
    /// </summary>
    /// <author>greshnyh_ni</author>
    public partial class IdleRecord : ControlBase
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

        public Dictionary<string, string> Values { get; set; }

        public int IdIdles { get; set; }

        // группа
        public int Idreason { get; set; }
        // причина 
        public int IdReasonDetail { get; set; }
        // граммаж
        public int Grammaj { get; set; }
        // примечание
        public string ReasonStr { get; set; }
        // номер станка
        public object MachineId { get; private set; }
        // дата начала простоя
        public string FromDt { get; set; }

        //
        public int IdidlesBdm { get; set; }


        public IdleRecord(int currentMachineId, Dictionary<string, string> record = null)
        {
            InitializeComponent();
            MachineId = currentMachineId;

            if (record != null)
            {
                IdIdles = record.CheckGet("IDIDLES").ToInt();
                Idreason = record.CheckGet("IDREASON").ToInt();
                IdReasonDetail = record.CheckGet("ID_REASON_DETAIL").ToInt();
                ReasonStr = record.CheckGet("REASON").ToString();
                FromDt = record.CheckGet("FROMDT").ToString();
                Grammaj = 0;
            }

            ControlSection = "paper_machine_control";
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

            Values = new Dictionary<string, string>();

        }

        private void FormInit()
        {
            Form = new FormHelper();

            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="IDIDLES",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="void",
                },

                  new FormHelperField()
                {
                    Path="IDREASON",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=GroupReason,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ID_REASON_DETAIL",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    ControlType="SelectBox",
                    Control=Reason,
                },
                new FormHelperField()
                {
                    Path="ID_BDM_SPEED",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    ControlType="SelectBox",
                    Control=Grammage,
                },
                new FormHelperField()
                {
                    Path="BDM_SPEED",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=SpeedTxt,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="REASON",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProblemTxt,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };
            Form.SetFields(fields);
            Form.StatusControl = FormStatus;

            double nScale = 1.5;
            GridParent.LayoutTransform = new ScaleTransform(nScale, nScale);
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
            Form.SetValues(Values);
            Show();
        }

        public void Edit()
        {
            FrameTitle = $"Редактирование простоя №{IdIdles.ToInt().ToString()}";
            GetIdleInfo();
            DataGet();
        }

        /// <summary>
        /// получаем данные (граммаж и скорость при начале простоя) IdIdles
        /// </summary>
        private void GetIdleInfo()
        {
            var complete = false;
            string error = "";

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_ST", MachineId.ToString());
                p.CheckAdd("ID_IDLES", IdIdles.ToString());
                p.CheckAdd("DT_FROM", FromDt.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "Monitoring");
            q.Request.SetParam("Action", "IdleInfo");
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
                    Grammaj = ds.Items.FirstOrDefault().CheckGet("ID_BDM_SPEED").ToInt();
                    SpeedTxt.Text = ds.Items.FirstOrDefault().CheckGet("BDM_SPEED").ToInt().ToString();
                    IdidlesBdm = ds.Items.FirstOrDefault().CheckGet("IDIDLES_BDM").ToInt();
                    ProblemTxt.Text = ReasonStr;

                    complete = true;
                }
            }
            else
            {
                error = q.GetError();
            }

            if (!complete)
            {
                LogMsg($"Ошибка при получении информации по простою {error}");
            }
        }


        /// <summary>
        /// получаем данные по простоям IdIdles
        /// </summary>
        private  void DataGet()
        {
            var complete = false;
            string error = "";

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_ST", MachineId.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "Monitoring");
            q.Request.SetParam("Action", "IdleGroup");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    // группы простоев
                    var ds = ListDataSet.Create(result, "ITEMS");
                    GroupReason.Items = ds.GetItemsList("ID", "NAME");
                    if (GroupReason.Items.Count > 0)
                    {
                        // var first = ds.Items[0];
                        // GroupReason.SetSelectedItemByKey(first["ID"]);
                        GroupReason.SetSelectedItemByKey(Idreason.ToString());
                    }

                    // граммаж
                    ds = ListDataSet.Create(result, "ITEMS2");
                    Grammage.Items = ds.GetItemsList("ID", "RO");
                    if (Grammage.Items.Count > 0)
                    {
                        Grammage.SetSelectedItemByKey(Grammaj.ToString());
                    }

                    complete = true;

                }
            }
            else
            {
                //q.ProcessError();
                error = q.GetError();
            }

            if (complete)
            {

                Show();
            }
            else
            {
                LogMsg($"Ошибка при получении данных по простою {error}");
            }
        }

        // выбрали запись из группы
        private void GroupReason_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (GroupReason.SelectedItem.Key.ToInt() > 0)
            {
                GetReasonData();
            }
        }

        /// <summary>
        /// Получение причин простоя при смене группы простоя
        /// </summary>
        private async void GetReasonData()
        {
            var complete = false;
            string error = "";

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_REASON", GroupReason.SelectedItem.Key.ToInt().ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "Monitoring");
            q.Request.SetParam("Action", "IdleReason");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    // причина простоев
                    var ds = ListDataSet.Create(result, "ITEMS");
                    Reason.Items = ds.GetItemsList("ID", "NAME");
                    if (Reason.Items.Count > 0)
                    {
                        Reason.SetSelectedItemByKey(IdReasonDetail.ToString());
                    }
                    complete = true;
                }
            }
            else
            {
                //q.ProcessError();
                error = q.GetError();
            }

            if (!complete)
            {
                LogMsg($"Ошибка при получении данных по причине простоя {error}");
            }
        }

        /// <summary>
        /// Проверки перед записью данных в БД
        /// </summary>
        private void Save()
        {
            if (Form.Validate())
            {
                bool resume = true;
                var v = Form.GetValues();
                string errorMsg = "";

                if (resume)
                {
                    if (ProblemTxt.Text.IsNullOrEmpty())
                    {
                        errorMsg = "Не все поля заполнены верно";
                        resume = false;
                    }
                }

                if (resume)
                {
                    v.CheckAdd("IDIDLES", IdIdles.ToString());
                    v.CheckAdd("IDIDLES_BDM", IdidlesBdm.ToString());

                    SaveData(v);
                }
                else
                {
                    Form.SetStatus(errorMsg, 1);
                }
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
            q.Request.SetParam("Action", "IdleSave");
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
                    Action = "RefreshIdles",
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
