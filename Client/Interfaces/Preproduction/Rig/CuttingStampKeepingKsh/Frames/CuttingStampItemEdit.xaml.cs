using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Форма редактирования элемента штанцформы
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class CuttingStampItemEdit : ControlBase
    {
        public CuttingStampItemEdit()
        {
            InitializeComponent();

            DocumentationUrl = "/doc/l-pack-erp/preproduction/tk_grid/molded_container";

            InitForm();
            SetDefaults();

            OnLoad = () =>
            {
            };

            OnUnload = () =>
            {
            };

            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverName == ControlName)
                {
                    ProcessMessage(msg);
                }
            };

            Commander.SetCurrentGroup("main");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "save",
                    Enabled = true,
                    Title = "Сохранить",
                    Description = "Сохранить/пересохранить и закрыть",
                    ButtonUse = true,
                    ButtonName = "SaveButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        Save();
                    },
                    CheckEnabled = () =>
                    {
                        var result = true;

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "close",
                    Enabled = true,
                    Title = "Отмена",
                    Description = "Отмена",
                    ButtonUse = true,
                    ButtonName = "CancelButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        Close();
                    },
                });
            }
            Commander.Init(this);
        }

        /// <summary>
        /// Имя вкладки, окуда вызвана форма редактирования
        /// </summary>
        public string ReceiverName;
        /// <summary>
        /// Форма редактирования техкарты
        /// </summary>
        FormHelper Form { get; set; }
        /// <summary>
        /// Идентификатор редактируемой штанцформы
        /// </summary>
        public int CuttingStampItemId { get; set; }
        /// <summary>
        /// Статус штанцформы
        /// </summary>
        private int StampStatusId { get; set; }
        /// <summary>
        /// Количество использований полумуфты
        /// </summary>
        private int ItemUsageCnt { get; set; }
        /// <summary>
        /// Предыдущий ответ клиента
        /// </summary>
        private int OldAnswerType { get; set; }
        /// <summary>
        /// Типы ответа клиента
        /// </summary>
        private Dictionary<string, string> AnswerTypeItems { get; set; }

        /// <summary>
        /// Обработка сообщений из шины сообщений
        /// </summary>
        /// <param name="msg"></param>
        public void ProcessMessage(ItemMessage msg)
        {
            string action = msg.Action;
            action = action.ClearCommand();
            if (!action.IsNullOrEmpty())
            {

            }
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            AnswerTypeItems = new Dictionary<string, string>
            {
                { "0", " " },
                { "1", "Оставить на 30 дней" },
                { "2", "Утилизировать" },
                { "3", "Передать клиенту" },
                { "4", "Оставить на 45 дней" },
                { "5", "Оставить на 60 дней" },
            };
            AnswerType.Items = AnswerTypeItems;
            Form.SetDefaults();
            AnswerType.SetSelectedItemByKey("0");
        }

        /// <summary>
        /// инициализация формы редактирования
        /// </summary>
        public void InitForm()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>
            {
                new FormHelperField()
                {
                    Path="NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Name,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="OWNER_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control = Owner,
                    ControlType = "SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="MAINTENANCE_CNT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control = Maintenance,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="OWNER_ANSWER_TYPE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control = AnswerType,
                    ControlType = "SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="OWNER_ANSWER_DTTM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control = null,
                    ControlType = "void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };
            Form.SetFields(fields);
            Form.StatusControl = FormStatus;
        }

        /// <summary>
        /// Получение данных для формы редактирования
        /// </summary>
        private async void GetData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "CuttingStamp");
            q.Request.SetParam("Action", "GetItem");
            q.Request.SetParam("ID", CuttingStampItemId.ToString());

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
                    //Плательщики
                    var ownerDS = ListDataSet.Create(result, "OWNERS");
                    Owner.Items = ownerDS.GetItemsList("ID", "NAME");

                    var ds = ListDataSet.Create(result, "STAMP_ITEM");

                    if (ds.Items.Count > 0)
                    {
                        var rec = ds.Items[0];
                        ItemUsageCnt = rec.CheckGet("USAGE_CNT").ToInt();
                        OldAnswerType = rec.CheckGet("OWNER_ANSWER_TYPE").ToInt();
                    }

                    Form.SetValues(ds);
                    Show();
                }
            }
        }

        /// <summary>
        /// Запуск редактирования элемента
        /// </summary>
        /// <param name="id"></param>
        public void Edit(int id)
        {
            CuttingStampItemId = id;
            ControlName = $"CuttingStampItem_{id}";
            ControlTitle = $"Полумуфта {id}";
            GetData();
        }

        /// <summary>
        /// Отображение формы редактирования
        /// </summary>
        public void Show()
        {
            Central.WM.Show(ControlName, ControlTitle, true, "add", this);
        }

        /// <summary>
        /// Закрытие формы
        /// </summary>
        public void Close()
        {
            Central.WM.Close(ControlName);
            Central.WM.SetActive(ReceiverName);
        }

        /// <summary>
        /// Проверки перед сохранением формы
        /// </summary>
        public void Save()
        {
            if (Form.Validate())
            {
                var v = Form.GetValues();
                v.CheckAdd("ID", CuttingStampItemId.ToString());

                int answerType = v.CheckGet("OWNER_ANSWER_TYPE").ToInt();
                if (answerType == 0)
                {
                    v.CheckAdd("OWNER_ANSWER_DTTM", "");
                }
                else
                {
                    if (OldAnswerType == 0)
                    {
                        v.CheckAdd("OWNER_ANSWER_DTTM", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
                    }
                }


                SaveData(v);
            }
        }

        /// <summary>
        /// Сохранение данных заказа в БД
        /// </summary>
        /// <param name="data"></param>
        public async void SaveData(Dictionary<string, string> data)
        {
            SaveButton.IsEnabled = false;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "CuttingStamp");
            q.Request.SetParam("Action", "SaveItem");
            q.Request.SetParams(data);

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
                    if (result.ContainsKey("ITEM"))
                    {
                        //Если ответ не пустой, отправляем сообщение Гриду о необходимости обновить данные
                        Central.Msg.SendMessage(new ItemMessage()
                        {
                            ReceiverGroup = "Preproduction/Rig",
                            ReceiverName = ReceiverName,
                            SenderName = ControlName,
                            Action = "Refresh",
                        });

                        Close();
                    }
                }
            }
            else if (q.Answer.Error.Code == 145)
            {
                Form.SetStatus(q.Answer.Error.Message, 1);
            }

            SaveButton.IsEnabled = true;
        }

        private void IncrementButton_Click(object sender, RoutedEventArgs e)
        {
            int maintenance = Maintenance.Text.ToInt();
            Maintenance.Text = (maintenance + 500000).ToString();
        }

        private void DecrementButton_Click(object sender, RoutedEventArgs e)
        {
            int maintenance = Maintenance.Text.ToInt();
            int newMaintenance = maintenance - 500000;
            if ((newMaintenance > 0) && (newMaintenance > ItemUsageCnt))
            {
                Maintenance.Text = newMaintenance.ToString();
            }
            else
            {
                Form.SetStatus("Слишком маленикое значение", 1);
            }
        }
    }
}
