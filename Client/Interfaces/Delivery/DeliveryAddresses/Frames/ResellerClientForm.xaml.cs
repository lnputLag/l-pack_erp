using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Delivery
{
    /// <summary>
    /// форма редактирования клиента
    /// </summary>
    /// <author>motenko_ek</author>
    /// <version>1</version>
    /// <released>2025-04-22</released>
    /// <changed>2025-04-22</changed>
    public partial class ResellerClientForm : ControlBase
    {
        public ResellerClientForm()
        {
            Id = 0;

            InitializeComponent();

            FrameMode = 0;
            OnGetFrameTitle = () =>
            {
                var result = "";

                var id = Id.ToInt();
                if (id == 0)
                {
                    result = $"Новый клиент";
                }
                else
                {
                    result = $"Клиент #{id}";
                }

                return result;
            };

            {
                Commander.Add(new CommandItem()
                {
                    Name = "save",
                    Enabled = true,
                    Title = "Сохранить",
                    Description = "",
                    ButtonUse = true,
                    ButtonName = "SaveButton",
                    HotKey = "Ctrl+Return",
                    Action = () =>
                    {
                        Save();
                    },
                }); ;
                Commander.Add(new CommandItem()
                {
                    Name = "cancel",
                    Enabled = true,
                    Title = "Отмена",
                    Description = "",
                    ButtonUse = true,
                    ButtonName = "CancelButton",
                    HotKey = "Escape",
                    Action = () =>
                    {
                        Hide();
                    },
                });
                Commander.Init(this);
            }

            Init();
            SetDefaults();
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }
        public int Id { get; set; }


        /// <summary>
        // инициализация компонентов
        /// </summary>
        public void Init()
        {
            Form = new FormHelper();

            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="ID_CLIENT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NAME_CLIENT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Name,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="ADDRESS_DOC",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Address,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="NAME_POKUPATEL",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Pokupatel,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
            };

            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
            Form.StatusControl = StatusBar;
        }

        public void SetDefaults()
        {
            Form.SetDefaults();
        }

        /// <summary>
        /// создание новой записи
        /// </summary>
        public void Create()
        {
            Id = 0;
            GetData();
        }

        /// <summary>
        /// редактирвоание записи
        /// </summary>
        /// <param name="id"></param>
        public void Edit(int id)
        {
            Id = id;
            GetData();
        }

        /// <summary>
        /// получение данных с сервера
        /// </summary>
        public async void GetData()
        {
            Form.SetBusy(true);

            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID_CLIENT", Id.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Delivery");
                q.Request.SetParam("Object", "ResellerClient");
                q.Request.SetParam("Action", "Get");
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
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            Form.SetValues(ds);
                        }

                        Show();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            Form.SetBusy(false);
        }

        /// <summary>
        /// подготовка данных
        /// </summary>
        public void Save()
        {
            bool resume = true;
            string error = "";

            //стандартная валидация данных средствами формы
            if (resume)
            {
                var validationResult = Form.Validate();
                if (!validationResult)
                {
                    resume = false;
                }
            }

            var v = Form.GetValues();

            //отправка данных
            if (resume)
            {
                SaveData(v);
            }
            else
            {
                Form.SetStatus(error, 1);
            }
        }

        /// <summary>
        /// отпаравка данных на сервер
        /// </summary>
        public async void SaveData(Dictionary<string, string> p)
        {
            Form.SetBusy(true);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Delivery");
            q.Request.SetParam("Object", "ResellerClient");
            q.Request.SetParam("Action", "Save");

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
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        var id = ds.GetFirstItemValueByKey("ID_CLIENT").ToInt();

                        if (id != 0)
                        {
                            Central.Msg.SendMessage(new ItemMessage()
                            {
                                ReceiverName = "ResellerClientTab",
                                SenderName = "ResellerClientForm",
                                Action = "reseller_client_refresh",
                                Message = $"{id}",
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

            Form.SetBusy(false);
        }
    }
}
