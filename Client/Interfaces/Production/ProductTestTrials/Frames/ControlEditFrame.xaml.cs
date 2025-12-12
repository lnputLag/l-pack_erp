using System.Collections.Generic;
using System.Threading.Tasks;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;

namespace Client.Interfaces.Production.ProductTestTrials
{
    /// <summary>
    /// Логика взаимодействия для ControlEdit.xaml
    /// </summary>
    public partial class ControlEditFrame : ControlBase
    {
        public ControlEditFrame()
        {
            Id = 0;

            InitializeComponent();

            RoleName = "[erp]production_testing_trial";

            OnGetFrameTitle = () =>
            {
                var result = "";

                var id = Id.ToInt();

                if (id == 0)
                {
                    result = "Новый контроль температуры и влажности";
                }
                else
                {
                    result = $"Редактирование контроля температуры и влажности - {id}";
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
                        Action = Close,
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "save",
                        Enabled = true,
                        Title = "Сохранить",
                        ButtonUse = true,
                        ButtonName = "SaveButton",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = SaveItem
                    });
                }

                Commander.Init(this);
            }

            FormInit();
        }

        public ListDataSet ListData { get; set; }
        public FormHelper Form { get; set; }
        private int Id { get; set; }

        private void FormInit()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path = "HUMIDITY",
                    FieldType = FormHelperField.FieldTypeRef.Double,
                    Control = Humidity,
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> { },

                },
                new FormHelperField()
                {
                    Path = "TEMPERATURE",
                    FieldType = FormHelperField.FieldTypeRef.Double,
                    Control = Temperature,
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> { },

                },
            };
            Form.SetFields(fields);
        }

        private async void LoadItem()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductTestTrial");
            q.Request.SetParam("Object", "Control");
            q.Request.SetParam("Action", "RecordGet");
            q.Request.SetParam("TECO_ID", Id.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                if (result != null)
                {
                    ListData = ListDataSet.Create(result, "RECORD");
                }

                Form.SetValues(ListData);
            }
        }

        private async void SaveItem()
        {
            FormStatus.Text = "Сохранение...";

            var v = Form.GetValues();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductTestTrial");
            q.Request.SetParam("Object", "Control");
            q.Request.SetParam("Action", "RecordSave");
            q.Request.SetParam("TECO_ID", Id.ToString());
            q.Request.SetParam("HUMIDITY", v.CheckGet("HUMIDITY"));
            q.Request.SetParam("TEMPERATURE", v.CheckGet("TEMPERATURE"));

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                FormStatus.Text = "Сохранено";

                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                if (result != null)
                {
                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverName = "ControlGrid",
                        Action = "control_grid_update",
                        Message = $"{Id}"
                    });

                    Close();
                }
            }
            else
            {
                FormStatus.Text = "Произошла непредвиденная ошибка";
            }
        }


        public void Edit(int id)
        {
            Id = id;
            LoadItem();
            Show();
        }

        public void Create()
        {
            Show();
        }
    }
}
