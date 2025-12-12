using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
using Client.Interfaces.Production.Corrugator;
using DevExpress.XtraPrinting.Native;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Security.Cryptography;
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
    public partial class IdlesLogRecord : ControlBase
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
        // дата окончания простоя
        public string ToDt { get; set; }

        public int IdidlesBdm { get; set; }

        /// <summary>
        /// Id графика работы 
        /// </summary>
        public int GraphItemId { get; set; }

        /// <summary>
        /// Id текущей производственной смены 
        /// </summary>
        public int WorkShiftId { get; set; }

        private bool ReadOnly { get; set; }

        public IdlesLogRecord(int currentMachineId, Dictionary<string, string> record = null)
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
                ToDt = record.CheckGet("END_DTTM").ToString();
                Grammaj = 0;

                StartDttm.Text = FromDt;
                EndDttm.Text = ToDt;
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

            // получение прав пользователя
            ProcessPermissions();


            Values = new Dictionary<string, string>();

        }


        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            string role = "";
            ReadOnly = true;
            // Проверяем уровень доступа
            if (MachineId.ToInt() == 716)
            {
                role = "[erp]bdm_1_control";
            }
            else
            {
                role = "[erp]bdm_2_control";
            }

            var mode = Central.Navigator.GetRoleLevel(role);
            var userAccessMode = mode;

            switch (mode)
            {
                case Role.AccessMode.Special:
                    {
                        StartDttm.IsEnabled = false;
                        EndDttm.IsEnabled = false;
                        ReadOnly = false;
                    }

                    break;

                case Role.AccessMode.FullAccess:
                    {
                        StartDttm.IsEnabled = true;
                        EndDttm.IsEnabled = true;
                        ReadOnly = false;
                    }
                    break;

                case Role.AccessMode.ReadOnly:
                    {
                        StartDttm.IsEnabled = false;
                        EndDttm.IsEnabled = false;
                        SaveButton.IsEnabled = false;
                    }
                    break;
            }

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
                    Path="FROM_DTTM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control= StartDttm,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }, //не обязательно для заполнения
                },

                new FormHelperField()
                {
                    Path="TO_DTTM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control= EndDttm,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }, //не обязательно для заполнения
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
            if (IdIdles.ToInt() != 0)
            {
                FrameTitle = $"Редактирование простоя №{IdIdles.ToInt().ToString()}";
            }
            else
            {
                FrameTitle = $"Добавление простоя.";
            }

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
        private void DataGet()
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
                    if (IdIdles.ToInt() != 0)
                    {
                        v.CheckAdd("IDIDLES", IdIdles.ToString());
                        v.CheckAdd("IDIDLES_BDM", IdidlesBdm.ToString());

                        SaveData(v);
                    }
                    else
                    {
                        var res = GetCurTimeByDt(); // GetCurTime();

                        if (res)
                        {
                            res = QueryWork();
                            if (res)
                            {
                                SaveDataNew(v);
                            }
                        }
                    }
                }
                else
                {
                    Form.SetStatus(errorMsg, 1);
                }
            }
        }

        /// <summary>
        ///  Обновляем запись по простою
        /// </summary>
        /// <param name="p"></param>
        private async void SaveData(Dictionary<string, string> p)
        {
            var q = new LPackClientQuery();

            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "Monitoring");
            q.Request.SetParam("Action", "IdleSaveDt");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                if (ReceiverName == "PaperMachineReportIdles")
                {
                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup = "PaperMachine",
                        ReceiverName = ReceiverName,
                        SenderName = ControlName,
                        Action = "RefreshPaperMachineIdlesList",
                        Message = "",
                    });
                }
                else
                {
                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverGroup = "Production",
                        ReceiverName = ReceiverName,
                        SenderName = ControlName,
                        Action = "RefreshIdles",
                    });

                }

                Close();
            }
            else if (q.Answer.Error.Code == 145)
            {
                Form.SetStatus(q.Answer.Error.Message, 1);
            }

        }

        /// <summary>
        ///  Добавляем запись по простою
        /// </summary>
        /// <param name="p"></param>
        private async void SaveDataNew(Dictionary<string, string> p)
        {

            p.CheckAdd("ID_GRAPH", GraphItemId.ToString());

            var q = new LPackClientQuery();

            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "Monitoring");
            q.Request.SetParam("Action", "IdleAdd");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                if (ReceiverName == "PaperMachineReportIdles")
                {
                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup = "PaperMachine",
                        ReceiverName = ReceiverName,
                        SenderName = ControlName,
                        Action = "RefreshPaperMachineIdlesList",
                        Message = "",
                    });
                }
                else
                {
                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverGroup = "Production",
                        ReceiverName = ReceiverName,
                        SenderName = ControlName,
                        Action = "RefreshIdles",
                    });

                }

                Close();
            }
            else if (q.Answer.Error.Code == 145)
            {
                Form.SetStatus(q.Answer.Error.Message, 1);
            }
        }


        /// <summary>
        /// запрос на получение графика работы произв. линий (IdGraph)
        /// </summary>
        private bool QueryWork()
        {
            var result = true;

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_ST", MachineId.ToString());
                p.CheckAdd("ID_TIMES", WorkShiftId.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "Bdm");
            q.Request.SetParam("Action", "BdmWorkGraphSelectIdGraph");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var res = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (res != null)
                {
                    var ds = ListDataSet.Create(res, "ITEMS");
                    if (ds.Items.Count > 0)
                    {
                        var first = ds.Items.First();
                        if (first != null)
                        {
                            GraphItemId = first.CheckGet("ID_GRAPH").ToInt();
                        }
                    }
                }
            }
            else
            {
                var s = $"Error: QueryWork. Code=[{q.Answer.Error.Code}] Message=[{q.Answer.Error.Message}] Description=[{q.Answer.Error.Description}]";
                LogMsg(s);
                result = false;
            }

            return result;
        }

        /// <summary>
        /// возвращает id текущего времени работы бригад (IdTimes)
        /// </summary>
        private bool GetCurTime()
        {
            var result = true;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "Bdm");
            q.Request.SetParam("Action", "BdmGetCurTime");

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var res = JsonConvert.DeserializeObject<Dictionary<string, string>>(q.Answer.Data);
                if (res != null)
                {
                    WorkShiftId = res.CheckGet("ID").ToInt();
                }
            }
            else
            {
                var s = $"Error: GetCurTime. Code=[{q.Answer.Error.Code}] Message=[{q.Answer.Error.Message}] Description=[{q.Answer.Error.Description}]";
                LogMsg(s);
                result = false;
            }
            return result;
        }

        /// <summary>
        /// возвращает id времени работы бригад (IdTimes) для указанной даты
        /// </summary>
        private bool GetCurTimeByDt()
        {
            var result = true;

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("DATE", StartDttm.Text.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "Bdm");
            q.Request.SetParam("Action", "BdmGetTimeByDt");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var res = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (res != null)
                {
                    var ds = ListDataSet.Create(res, "ITEMS");
                    if (ds.Items.Count > 0)
                    {
                        var first = ds.Items.First();
                        if (first != null)
                        {
                            WorkShiftId = first.CheckGet("ID_TIMES").ToInt();
                        }
                    }
                }
            }
            else
            {
                var s = $"Error: QueryWork. Code=[{q.Answer.Error.Code}] Message=[{q.Answer.Error.Message}] Description=[{q.Answer.Error.Description}]";
                LogMsg(s);
                result = false;
            }
       
            return result;
        }


        //////////
    }
}
