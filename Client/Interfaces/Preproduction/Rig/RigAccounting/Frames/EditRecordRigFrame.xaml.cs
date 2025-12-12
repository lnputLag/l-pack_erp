using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Окно для редактирования именования и примечания расхода оснастки
    /// </summary>
    /// <author>volkov_as</author>
    public partial class EditRecordRigFrame : ControlBase
    {
        public EditRecordRigFrame()
        {
            InitializeComponent();

            RoleName = "[erp]rig_movement";

            FormInit();

            FrameMode = 0;

            OnGetFrameTitle = () =>
            {
                var result = "";
                var id = Id;

                if (id != 0)
                {
                    result = $"Расход оснастки - {id}";
                }

                return result;
            };

            {
                Commander.SetCurrentGroup("main");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "cancel",
                        Enabled = true,
                        Title = "Отмена",
                        ButtonUse = true,
                        ButtonName = "CancelButton",
                        Action = () =>
                        {
                            Close();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "save",
                        Enabled = true,
                        Title = "Сохранить",
                        ButtonUse = true,
                        ButtonName = "SaveButton",
                        AccessLevel = Common.Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            Save();
                        }
                    });
                }

                Commander.Init(this);
            }

        }

        /// <summary>
        /// идентификатор записи, с которой работает форма
        /// (primary key записи таблицы)
        /// </summary>
        public int Id { get; set; }

        public ListDataSet RigItem { get; set; }

        /// <summary>
        /// Форма редактирования образца
        /// </summary>
        FormHelper Form { get; set; }

        private void FormInit()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="ID2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="RIG_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=NameRigBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ },
                },
                new FormHelperField()
                {
                    Path="NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=NoteBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ },
                },
                new FormHelperField()
                {
                    Path = "DETAILS",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = DetailsBox,
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> { },
                }
            };

            Form.SetFields(fields);
            Form.StatusControl = FormStatus;
        }

        /// <summary>
        /// Получении данных о оснастке
        /// </summary>
        private async void DataGet()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "Consumption");
            q.Request.SetParam("Action", "Get");
            q.Request.SetParam("ID2", Id.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                if (result != null)
                {
                    RigItem = ListDataSet.Create(result, "ITEM");
                }

                Form.SetValues(RigItem);

                Show();
            }


        }

        /// <summary>
        /// Запрос на сохранение данных
        /// </summary>
        private async void DataSave()
        {
            var f = Form.GetValues();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "Consumption");
            q.Request.SetParam("Action", "Save");
            q.Request.SetParam("ID2", Id.ToString());
            q.Request.SetParam("RIG_NAME", f.CheckGet("RIG_NAME"));
            q.Request.SetParam("NOTE", f.CheckGet("NOTE"));
            q.Request.SetParam("DETAILS", f.CheckGet("DETAILS"));


            await Task.Run(() => { q.DoQuery(); });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    {
                        var ds = ListDataSet.Create(result, "ITEM");
                        var id = ds.GetFirstItemValueByKey("ID2").ToInt();

                        if (id != 0)
                        {
                            Central.Msg.SendMessage(new ItemMessage()
                            {
                                ReceiverName = "PreproductionRig",
                                Action = "consumption_refresh",
                                Message = $"{Id}"
                            });

                            Close();
                        }
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Проверка данных перед сохранением
        /// </summary>
        private void Save()
        {
            var p = Form.GetValues();
            bool resume = true;
            string msg = "";

            if (p.CheckGet("RIG_NAME").Length == 0)
            {
                resume = false;
                msg = "Не указано наименование оснастки";
            }

            if (resume)
            {
                DataSave();
            }
            else
            {
                FormStatus.Text = msg;
            }
        }

        /// <summary>
        /// Функция для открытия фрейма
        /// </summary>
        /// <param name="id">Индификатор оснастки</param>
        public void Edit(int id)
        {
            Id = id;
            DataGet();
        }
    }
}
