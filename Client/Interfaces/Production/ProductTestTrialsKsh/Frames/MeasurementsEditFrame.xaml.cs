using Client.Common;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Client.Interfaces.Main;

namespace Client.Interfaces.Production.ProductTestTrialsKsh
{
    /// <summary>
    /// Логика взаимодействия для MeasurementsEdit.xaml
    /// </summary>
    public partial class MeasurementsEditFrame : ControlBase
    {
        public MeasurementsEditFrame()
        {
            TestId = 0;
            RoleName = "[erp]prod_testing_trial_ksh";

            InitializeComponent();
            InitForm();

            FrameMode = 0;

            OnGetFrameTitle = () =>
            {
                var result = "";
                var id = TestId;

                if (id != 0)
                {
                    result = $"Редактирование теста {id}";
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
                        Action = SaveData
                    });
                }

                Commander.Init(this);
            }
        }

        /// <summary>
        /// ID теста
        /// </summary>
        private int TestId { get; set; }

        /// <summary>
        /// Форма редактирования образца
        /// </summary>
        FormHelper EditTestForm { get; set; }

        public ListDataSet TestItem { get; set; }

        private void InitForm()
        {
            EditTestForm = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="HUMIDITY",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control= HumidityProduct,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ },
                },
                new FormHelperField()
                {
                    Path="HUMIDITY_GA",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control= HumidityWorkpiece,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ },
                },
                new FormHelperField()
                {
                    Path="NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control= Note,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ },
                },
                new FormHelperField()
                {
                    Path="BCT_24",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control= VST24,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ },
                },
                new FormHelperField()
                {
                    Path="ECT_24",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control= EST24,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ },
                },
                new FormHelperField()
                {
                    Path="ECT_GA_24",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control= EST24Zag,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ },
                },
                new FormHelperField()
                {
                    Path="THICKNESS_GA",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control= ThicknessWorkpiece,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ },
                },
                new FormHelperField()
                {
                    Path="THICKNESS",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control= ThicknessProduct,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ },
                },

            };

            EditTestForm.SetFields(fields);
            EditTestForm.StatusControl = FormStatus;
        }

        private async void GetData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductTestTrialKsh");
            q.Request.SetParam("Object", "Measurements");
            q.Request.SetParam("Action", "Get");
            q.Request.SetParam("ID", TestId.ToString());

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
                    TestItem = ListDataSet.Create(result, "GET_TEST");
                }

                EditTestForm.SetValues(TestItem);
                Show();
            }
        }

        private async void SaveData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductTestTrialKsh");
            q.Request.SetParam("Object", "Measurements");
            q.Request.SetParam("Action", "Save");
            q.Request.SetParam("ID", TestId.ToString());
            q.Request.SetParam("HUMIDITY", HumidityProduct.Text ?? null);
            q.Request.SetParam("HUMIDITY_GA", HumidityWorkpiece.Text ?? null);
            q.Request.SetParam("NOTE", Note.Text ?? null);
            q.Request.SetParam("BCT_24", VST24.Text ?? null);
            q.Request.SetParam("ECT_24", EST24.Text ?? null);
            q.Request.SetParam("ECT_GA_24", EST24Zag.Text ?? null);
            q.Request.SetParam("THICKNESS_GA", ThicknessWorkpiece.Text ?? null);
            q.Request.SetParam("THICKNESS", ThicknessProduct.Text ?? null);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                FormStatus.Text = "Сохраненно!";
                Close();
            } else
            {
                FormStatus.Text = "Ошибка сохранения!";
            }
        }

        public void Edit(int id)
        {
            TestId = id;
            GetData();
        }
    }
}
