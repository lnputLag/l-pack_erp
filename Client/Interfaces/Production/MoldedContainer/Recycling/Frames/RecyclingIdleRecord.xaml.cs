using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
using DevExpress.XtraPrinting.Native;
using GalaSoft.MvvmLight.Messaging;
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

namespace Client.Interfaces.Production.MoldedContainer
{
    /// <summary>
    /// редактирование записи причина простоя станка Литой тары 
    /// </summary>
    /// <author>greshnyh_ni</author>
    public partial class RecyclingIdleRecord : ControlBase
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

        // ИД простоя
        public int DntmId { get; set; }
        // тип
        public int DotyId { get; set; }
        // причина 
        public int DoreId { get; set; }
        // примечание
        public string Note { get; set; }
        // номер станка
        public object MachineId { get; private set; }
        // дата начала простоя
        public string StartDt { get; set; }
        // дата окончания простоя
        public string EndDt { get; set; }

        private bool ReadOnly { get; set; }

        public RecyclingIdleRecord(Dictionary<string, string> p)
        {
            InitializeComponent();
            DntmId = p.CheckGet("IDIDLES").ToInt();
            MachineId = p.CheckGet("ID_ST").ToInt(); ;

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
            StartDttm.IsEnabled = true;
            EndDttm.IsEnabled = true;
        }

        private void FormInit()
        {
            Form = new FormHelper();

            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="void",
                },

                  new FormHelperField()
                {
                    Path="DOTY_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=GroupReason,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DORE_ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    ControlType="SelectBox",
                    Control=Reason,
                },
                new FormHelperField()
                {
                    Path="NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProblemTxt,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                new FormHelperField()
                {
                    Path="START_DTTM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control= StartDttm,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }, //не обязательно для заполнения
                },
                new FormHelperField()
                {
                    Path="END_DTTM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control= EndDttm,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }, //не обязательно для заполнения
                },

                new FormHelperField()
                {
                    Path="ID_ST",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    ControlType="SelectBox",
                    Control=Machines,
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
            var list = new Dictionary<string, string>();
            list.Add("311", "Принтер BST[СPH-3430]-1");
            list.Add("321", "Этикетир. BST[TBH-2438]-1");
            list.Add("312", "Принтер AAEI [301 P]");
            list.Add("322", "Этикетир AAEI [301 L]");
            list.Add("331", "Упаковка ЛТ");

            list.Add("301", "Пресс BST[ЕС9600]-2 A");
            list.Add("302", "Пресс BST[ЕС9600]-2 B");
            list.Add("303", "Пресс BST[ЕС9600]-1 A");
            list.Add("304", "Пресс BST[ЕС9600]-1 B");
            list.Add("305", "ВФМ BST[ЕС9600]-2");
            list.Add("306", "ВФМ BST[ЕС9600]-1");

            Machines.Items = list;

            if (DntmId.ToInt() != 0)
            {
                Machines.SetSelectedItemByKey(MachineId.ToString());
            }

            Form.SetDefaults();
            FormStatus.Visibility = Visibility.Hidden;
        }

        public void Create()
        {
            Show();
        }

        public void Edit()
        {
            if (DntmId.ToInt() != 0)
            {
                FrameTitle = $"Редактирование простоя №{DntmId.ToInt().ToString()}";
                Machines.IsReadOnly = true ;
            }
            else
            {
                FrameTitle = $"Добавление простоя.";
                Machines.IsReadOnly = false;
            }

            SetDefaults();
            Form.SetValues(Values);

            DataGet();
        }

        /// <summary>
        /// получаем данные по простоям Downtime
        /// </summary>
        private void DataGet()
        {
            var complete = false;
            string error = "";

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("DNTM_ID", DntmId.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Recycling");
            q.Request.SetParam("Action", "DowntimeRecord");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    // данные по простою
                    var ds = ListDataSet.Create(result, "ITEMS");

                    // тип
                    DotyId = ds.Items.FirstOrDefault().CheckGet("DOTY_ID").ToInt();
                    // причина 
                    DoreId = ds.Items.FirstOrDefault().CheckGet("DORE_ID").ToInt();
                    // примечание
                    Note = ds.Items.FirstOrDefault().CheckGet("NOTE").ToString();
                    ProblemTxt.Text = Note;

                    if (DntmId != 0)
                    {
                        StartDt = ds.Items.FirstOrDefault().CheckGet("START_DTTM").ToString();
                        EndDt = ds.Items.FirstOrDefault().CheckGet("END_DTTM").ToString();
                        StartDttm.Text = StartDt;
                        EndDttm.Text = EndDt;
                    }
                    else
                    {
                        var today = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                        StartDttm.Text = today;
                        EndDttm.Text = today;
                    }

                    // это не автоматически созданный системой простой 
                    if (ds.Items.FirstOrDefault().CheckGet("CREATED_ACCO_ID").ToInt() != 595)
                    {
                        StartDttm.IsEnabled = true;
                        EndDttm.IsEnabled = true;
                    }

                    // тип простоев
                    var ds2 = ListDataSet.Create(result, "TYPE");
                    GroupReason.Items = ds2.GetItemsList("ID", "NAME");
                    if (GroupReason.Items.Count > 0)
                    {
                        GroupReason.SetSelectedItemByKey(DotyId.ToString());
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
                p.CheckAdd("DOTY_ID", GroupReason.SelectedItem.Key.ToInt().ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Recycling");
            q.Request.SetParam("Action", "DowntimeTypeRefList");
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
                        Reason.SetSelectedItemByKey(DoreId.ToString());
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
                    if (Machines.SelectedItem.Key.ToInt() == 0)
                    {
                        errorMsg = "Не все поля заполнены верно";
                        resume = false;
                    }
                }
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
                    if (DntmId.ToInt() != 0)
                    {
                        v.CheckAdd("DNTM_ID", DntmId.ToString());
                        SaveData(v);
                    }
                    else
                    {
                        SaveDataNew(v);
                    }
                }
                else
                {
                    Form.SetStatus(errorMsg, 1);
                }
            }
        }

        /// <summary>
        /// Обновление данных по простою в БД
        /// </summary>
        /// <param name="p"></param>
        private async void SaveData(Dictionary<string, string> p)
        {
            var q = new LPackClientQuery();

            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Recycling");
            q.Request.SetParam("Action", "IdlesSave");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                if (ReceiverName == "RecyclingMachineReportIdles")
                {
                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup = "MoldedContainer",
                        ReceiverName = ReceiverName,
                        SenderName = ControlName,
                        Action = "RefreshRecyclingIdlesList",
                        Message = "",
                        ContextObject = p,
                    });
                }
                else
                {
                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverGroup = "MoldedContainer",
                        ReceiverName = ReceiverName,
                        SenderName = ControlName,
                        Action = "RefreshRecyclingIdles",
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
            p.CheckAdd("ID_ST", Machines.SelectedItem.Key.ToInt().ToString());
            p.CheckAdd("CREATED_ACCO_ID", Central.User.AccountId.ToString());
            p.CheckAdd("START_DTTM", StartDttm.Text);
            p.CheckAdd("END_DTTM", EndDttm.Text);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Recycling");
            q.Request.SetParam("Action", "AddIdles");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                if (ReceiverName == "RecyclingMachineReportIdles")
                {
                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup = "MoldedContainer",
                        ReceiverName = ReceiverName,
                        SenderName = ControlName,
                        Action = "RefreshRecyclingIdlesList",
                        Message = "",
                        ContextObject = p,
                    });
                }
                else
                {
                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverGroup = "MoldedContainer",
                        ReceiverName = ReceiverName,
                        SenderName = ControlName,
                        Action = "RefreshRecyclingIdles",
                    });
                }

                Close();
            }
            else if (q.Answer.Error.Code == 145)
            {
                Form.SetStatus(q.Answer.Error.Message, 1);
            }
        }




    }
}
