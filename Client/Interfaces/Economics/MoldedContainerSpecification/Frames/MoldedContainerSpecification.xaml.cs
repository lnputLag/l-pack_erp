using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Economics.MoldedContainer
{
    /// <summary>
    /// Настройка спецификации литой тары
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class MoldedContainerSpecification : ControlBase
    {
        public MoldedContainerSpecification()
        {
            DocumentationUrl = "/doc/l-pack-erp/service/agent/agents";
            InitializeComponent();

            InitForm();
            SetDefaults();

            Commander.SetCurrentGroup("main");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "save",
                    Group = "main_form",
                    Enabled = true,
                    Title = "Сохранить",
                    Description = "Сохранение данных",
                    ButtonUse = true,
                    ButtonName = "SaveButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        Save();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "close",
                    Group = "main_form",
                    Enabled = true,
                    Title = "Отмена",
                    Description = "Закрыть форму без сохранения",
                    ButtonUse = true,
                    ButtonName = "CancelButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        Close();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "help",
                    Group = "main_form",
                    Enabled = true,
                    Description = "Справка",
                    ButtonUse = true,
                    ButtonName = "HelpButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        Central.ShowHelp(DocumentationUrl);
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
        /// Форма редактирования спецификации
        /// </summary>
        FormHelper Form { get; set; }
        /// <summary>
        /// Идентификатор спецификации
        /// </summary>
        public int SpecificationId { get; set; }
        /// <summary>
        /// Идентификатор договора, для которого создана спецификация. Обязательно при создании спецификации
        /// </summary>
        public int ContractId { get; set; }

        /// <summary>
        /// Инициализация формы
        /// </summary>
        private void InitForm()
        {
            Form = new FormHelper();

            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="SPEC_NUMBER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Number,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="BEGIN_DATE",
                    FieldType=FormHelperField.FieldTypeRef.DateTime,
                    Control=BeginDate,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="END_DATE",
                    FieldType=FormHelperField.FieldTypeRef.DateTime,
                    Control=EndDate,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="INVISIBLE_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=InvisibleFlag,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DIRECTION_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Direction,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="COMMENTS",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Comments,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="RETURNED_SIGNED_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=ReturnedSignedFlag,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NOTE_DESCRIPTION",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=NoteDescription,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NOTE_TEXT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Note,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };
            Form.SetFields(fields);
            Form.StatusControl = FormStatus;

            // Блокируем кнопку сохранения, пока не выполнена загрузка данных
            SaveButton.IsEnabled = false;
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();
            // При создании спецификации в дату начала ставим текущую дату
            BeginDate.Text = DateTime.Now.Date.ToString("dd.MM.yyyy");
        }

        /// <summary>
        /// Получение данных для полей формы из БД
        /// </summary>
        private async void GetData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Economics");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "GetSpecification");
            q.Request.SetParam("SPECIFICATION_ID", SpecificationId.ToString());

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
                    // Направления доставки
                    var directionDS = ListDataSet.Create(result, "DIRECTIONS");
                    Direction.Items = directionDS.GetItemsList("ID", "NAME");

                    // Данные спецификации
                    if (SpecificationId > 0)
                    {
                        var valueDS = ListDataSet.Create(result, "SPECIFICATION");
                        Form.SetValues(valueDS);

                        if (Number.Text.IsNullOrEmpty())
                        {
                            ControlTitle = $"Спецификация {SpecificationId}";
                        }
                        else
                        {
                            ControlTitle = $"Спецификация №{Number.Text}";
                        }
                    }
                    else
                    {
                        ControlTitle = "Новая спецификация";
                    }

                    // После загрузки разблокируем кнопку сохранения
                    SaveButton.IsEnabled = true;

                    Show();
                }
            }
        }

        /// <summary>
        /// Вход для отображения вкладки с данными
        /// </summary>
        /// <param name="specificationId"></param>
        public void Edit(int specificationId = 0)
        {
            SpecificationId = specificationId;
            ControlName = $"MoldedContainerSpecification_{specificationId}";
            GetData();
        }

        /// <summary>
        /// Показывает вкладку
        /// </summary>
        public void Show()
        {
            Central.WM.Show(ControlName, ControlTitle, true, "add", this);
        }

        /// <summary>
        /// Закрытие вкладки с формой
        /// </summary>
        public void Close()
        {
            Central.WM.Close(ControlName);
            Central.WM.SetActive(ReceiverName);
        }

        /// <summary>
        /// Проверки перед сохранением
        /// </summary>
        public void Save()
        {
            if (Form.Validate())
            {
                bool resume = true;
                var v = Form.GetValues();
                string errorMsg = "Не все поля заполнены верно";

                if (ContractId == 0 && SpecificationId == 0)
                {
                    resume = false;
                    errorMsg = "Не удалось определить договор для создания спецификации";
                }

                if (resume)
                {
                    SaveData(v);
                }
                else
                {
                    Form.SetStatus(errorMsg, 1);
                }
            }
        }

        /// <summary>
        /// Сохранение даннных формы в БД
        /// </summary>
        /// <param name="p"></param>
        private async void SaveData(Dictionary<string, string> p)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Economics");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "SaveSpecification");
            q.Request.SetParam("SPECIFICATION_ID", SpecificationId.ToString());
            q.Request.SetParam("CONTRACT_ID", ContractId.ToString());
            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    if (result.ContainsKey("ITEMS"))
                    {
                        Central.Msg.SendMessage(new ItemMessage()
                        {
                            ReceiverGroup = "Economics",
                            ReceiverName = ReceiverName,
                            SenderName = ControlName,
                            Action = "RefreshSpecifications",
                        });

                        Close();
                    }
                }
            }
        }
    }
}
