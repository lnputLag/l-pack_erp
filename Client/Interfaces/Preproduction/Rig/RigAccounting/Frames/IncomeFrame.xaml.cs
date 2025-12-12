using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Интерфейс для работы с накладной во вкладке "Приход оснастки" верхний грид.
    /// </summary>
    /// <author>volkov_as</author>
    public partial class IncomeFrame : ControlBase
    {

        public IncomeFrame()
        {
            PackingListId = 0;

            InitializeComponent();

            FormInit();

            RoleName = "[erp]rig_movement";

            FrameMode = 0;
            OnGetFrameTitle = () =>
            {
                var result = "";

                var id = PackingListId.ToInt();

                if (id == 0)
                {
                    result = "Новый приход накладная";
                }
                else
                {
                    result = $"Приход накладная {PackingListId}";
                }

                return result;
            };
            
            {
                Commander.SetCurrentGroup("main");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "save",
                        Enabled = true,
                        Title = "Сохранить",
                        ButtonUse = true,
                        ButtonName = "SaveButton",
                        AccessLevel = Common.Role.AccessMode.FullAccess,
                        Action = PackingListSave
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "cancel",
                        Enabled = true,
                        Title = "Отмена",
                        ButtonUse = true,
                        ButtonName = "CancelButton",
                        Action = Close
                    });
                }
                Commander.Init(this);
            }

        }

        public ListDataSet ListData { get; set; }
        public FormHelper Form { get; set; }

        private int PackingListId { get; set; }

        private void FormInit()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path = "DATA",
                    FieldType = FormHelperField.FieldTypeRef.DateTime,
                    Format = "dd.MM.yyyy",
                    Control = FromDate,
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> { },
                },
                new FormHelperField()
                {
                    Path = "NAME_NAKL",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = NaklNumber,
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> { },
                },
                new FormHelperField()
                {
                    Path = "NAMESF",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = SfNumber,
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> { },
                },
                new FormHelperField()
                {
                    Path = "POSTAVSHIC_RIG",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Default = "0",
                    Control = SelectProvider,
                    ControlType = "SelectBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>
                    {
                    },
                    OnChange = (FormHelperField f, string v) => { },
                    QueryLoadItems = new RequestData()
                    {
                        Module = "Rig",
                        Object = "Supplier",
                        Action = "List",
                        AnswerSectionKey = "ITEMS",
                        OnComplete = (FormHelperField f, ListDataSet ds) =>
                        {
                            var row = new Dictionary<string, string>()
                            {
                            };
                            ds.ItemsPrepend(row);
                            var list = ds.GetItemsList("ID_POST", "NAME");
                            var c = (SelectBox)f.Control;
                            if (c != null)
                            {
                                c.Items = list;
                            }
                        }
                    }
                },
                new FormHelperField()
                {
                    Path = "LPACK_LIST",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Default = "1",
                    Control = SelectBuyer,
                    ControlType = "SelectBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>
                    {
                    },
                    OnChange = (FormHelperField f, string v) => { },
                    QueryLoadItems = new RequestData()
                    {
                        Module = "Rig",
                        Object = "Buyer",
                        Action = "ListLpack",
                        AnswerSectionKey = "ITEMS",
                        OnComplete = (FormHelperField f, ListDataSet ds) =>
                        {
                            var row = new Dictionary<string, string>()
                            {
                            };
                            ds.ItemsPrepend(row);
                            var list = ds.GetItemsList("ID_PROD", "NAME");
                            var c = (SelectBox)f.Control;
                            if (c != null)
                            {
                                c.Items = list;
                            }
                        }
                    }
                }
            };

            Form.SetFields(fields);

            Form.StatusControl = FormStatus;
        }

        /// <summary>
        /// Загрузка накладной в случае открытия фрейма в режиме редактирование
        /// </summary>
        private async void ItemLoad()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "Income");
            q.Request.SetParam("Action", "Get");
            q.Request.SetParam("NNAKL", PackingListId.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                if (result != null)
                {
                    ListData = ListDataSet.Create(result, "ITEM");
                }

                Form.SetValues(ListData);

                SelectProvider.SetSelectedItemByKey(ListData.GetFirstItemValueByKey("ID_POST"));
                SelectProvider.IsEnabled = false;


                SelectBuyer.SetSelectedItemByKey(ListData.GetFirstItemValueByKey("ID_PROD"));
            }
        }

        /// <summary>
        /// Сохрвнение приход накладной
        /// </summary>
        private async void PackingListSave()
        {
            var f = Form.GetValues();

            FormStatus.Text = "Сохранение";

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "Income");
            q.Request.SetParam("Action", "Save");
            q.Request.SetParam("NNAKL", PackingListId.ToString());
            q.Request.SetParam("DATA", f.CheckGet("DATA"));
            q.Request.SetParam("NAME_NAKL", f.CheckGet("NAME_NAKL"));
            q.Request.SetParam("NAMESF", f.CheckGet("NAMESF"));
            q.Request.SetParam("ID_PROD", SelectBuyer.SelectedItem.Key);
            q.Request.SetParam("ID_POST", f.CheckGet("POSTAVSHIC_RIG"));
            q.Request.SetParam("FACTORY_ID", SelectBuyer.SelectedItem.Key.ToInt() != 427 ? "1" : "2");

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

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
                    var result2 = ListDataSet.Create(result, "ITEM");

                    var id = (PackingListId != 0) ? PackingListId.ToString() : result2.GetFirstItemValueByKey("NNAKL");

                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverName = "IncomeGrid",
                        Action = "consignment_note_update",
                        Message = $"{id}",
                    });

                    Close();
                }
            }
        }


        /// <summary>
        /// Редактирование прихода накладная
        /// </summary>
        /// <param name="id"></param>
        public void Edit(int id)
        {
            PackingListId = id;
            ItemLoad();
            Show();
        }


        /// <summary>
        /// Создание нового прихода накладная
        /// </summary>
        public void Create()
        {
            Show();
        }
    }
}
