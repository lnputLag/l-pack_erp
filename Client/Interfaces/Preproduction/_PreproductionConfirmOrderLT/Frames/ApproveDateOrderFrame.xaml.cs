using System.Collections.Generic;
using System.Threading.Tasks;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;

namespace Client.Interfaces.Preproduction.PreproductionConfirmOrderLt.Frames
{
    public partial class ApproveDateOrderFrame : ControlBase
    {
        /// <summary>
        /// Окно для изменения даты отгрузки заявки на ЛТ
        /// </summary>
        public ApproveDateOrderFrame()
        {
            OrderId = 0;
            
            InitializeComponent();
            InitForm();
            
            FrameMode = 2;
            OnGetFrameTitle = () =>
            {
                var result = $"Утвердить дату заявки {OrderId}";
                
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
                        Action = Save
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "cancel",
                        Enabled = true,
                        Title = "отмена",
                        ButtonUse = true,
                        ButtonName = "CancelButton",
                        Action = Close
                    });
                }
                Commander.Init(this);
            }
        }
        
        
        public int OrderId { get; set; }
        public string Date { get; set; }
        private FormHelper Form { get; set; }

        private void InitForm()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path = "OLD_DATA",
                    FieldType = FormHelperField.FieldTypeRef.DateTime,
                    Format = "dd.MM.yyyy",
                    Control = OldArrivalDate,
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> { },
                },
                new FormHelperField()
                {
                    Path = "NEW_DATA",
                    FieldType = FormHelperField.FieldTypeRef.DateTime,
                    Format = "dd.MM.yyyy",
                    Control = NewArrivalDate,
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>
                    {
                        { FormHelperField.FieldFilterRef.Required , null}
                    },
                },
            };
            Form.SetFields(fields);
        }


        /// <summary>
        /// Сохранение новой даты отгрузки заявки ЛТ
        /// </summary>
        private async void Save()
        {
            var resume = true;
            
            if (NewArrivalDate.Text.IsNullOrEmpty())
            {
                //FormStatus.Text = "Необходимо указать новую дату отгрузки";
                resume = false;
            }

            if (NewArrivalDate.Text == OldArrivalDate.Text)
            {
                //FormStatus.Text = "Новая дата отгрузки совпадает с текущей";
                resume = false;
            }

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "ConfirmOrderLT");
                q.Request.SetParam("Action", "ApproveDate");
                q.Request.SetParam("NSTHET", OrderId.ToString());
                q.Request.SetParam("NEW_DTTM", NewArrivalDate.Text);

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
                            Central.Msg.SendMessage(new ItemMessage()
                            {
                                ReceiverName = "OrderGrid",
                                Action = "refresh_order_grid",
                                Message = $"{OrderId}"
                            });
                        }
                        
                        Close();
                    }
                }
            }
        }


        public void Edit(int orderId, string date)
        {
            OrderId = orderId;
            Date = date;
            Width = 270;
            Height = 120;

            OldArrivalDate.Text = Date;

            var windowParameters = new Dictionary<string, string>
            {
                { "no_resize", "1" },
                { "no_maximize", "1" },
                { "no_minimize", "1" },
                { "center_screen", "1" }
            };

            Central.WM.FrameMode = FrameMode;
            var frameName = GetFrameName();
            var frameTitle = FrameTitle;

            if (OnGetFrameTitle != null)
            {
                frameTitle = OnGetFrameTitle.Invoke();
                frameName = GetFrameName();
            }

            Central.WM.Show(frameName, frameTitle, true, "add", this, p: windowParameters);


            
            //Show();
        }
    }
}