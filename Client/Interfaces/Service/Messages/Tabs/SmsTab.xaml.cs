using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static Client.Interfaces.Main.FormDialog;

namespace Client.Interfaces.Messages
{
    /// <summary>
    /// сообщения смс
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-12-27</released>
    /// <changed>2023-12-27</changed>
    public partial class SmsTab : ControlBase
    {
        public SmsTab()
        {
            InitializeComponent();

            RoleName = "[erp]messages";
            ControlTitle = "СМС";
            DocumentationUrl = "/doc/l-pack-erp/service/messages/sms";

            OnMessage=(ItemMessage m)=>
            {
                if(m.ReceiverName == ControlName)
                {
                    ProcessCommand(m.Action,m);
                }
            };

            OnKeyPressed = (KeyEventArgs e) =>
            {
                if(!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };

            OnLoad =()=>
            {
                GridInit();
                SetDefaults();
            };

            OnUnload=()=>
            {
                Grid.Destruct();
            };

            OnFocusGot=()=>
            {
                Grid.ItemsAutoUpdate=true;
                Grid.Run();
            };

            OnFocusLost=()=>
            {
                Grid.ItemsAutoUpdate=false;
            };


        }

        public void GridInit()
        {
            {
                var columns = new List<DataGridHelperColumn>()
                {
                    new DataGridHelperColumn()
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="ИД",
                        Path="ID",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Отправить",
                        Path="DATE_SEND_PLANNED",
                        Doc="Дата отправки",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2=16,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Отправлено",
                        Path="DATE_SEND_ACTUAL",
                        Doc="Фактическая дата отправки",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2=16,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Получатель",
                        Path="PHONE_NUMBER",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width2=12,
                    },
                     new DataGridHelperColumn()
                    {
                        Header="Сообщение",
                        Path="TEXT",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width2=24,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Отправлено",
                        Path="SENT",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        Width2=10,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Ошибка",
                        Path="ERROR",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        Width2=10,
                    },
                };
                Grid.SetColumns(columns);
                Grid.SetPrimaryKey("ID");
                Grid.SetSorting("DATE_SEND_PLANNED", ListSortDirection.Descending);
                Grid.SearchText=SearchText;
                Grid.ColumnWidthMode= GridBox.ColumnWidthModeRef.Full;
                //Grid.OnLoadItems=LoadItems;
                Grid.QueryLoadItems = new RequestData()
                {
                    Module = "Messages",
                    Object = "Sms",
                    Action = "List",
                    BeforeRequest = (RequestData rd) =>
                    {
                        var p=new Dictionary<string, string>();
                        {
                            p.Add("DATE_FROM", DateFrom.Text);
                            p.Add("DATE_TO", DateTo.Text);
                        }
                        rd.Params = p;
                    }
                };
                Grid.OnSelectItem=(row) =>
                {
                };
                Grid.OnDblClick=(row) =>
                {
                    ProcessCommand("view");
                };
                Grid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    {
                        "create",
                        new DataGridContextMenuItem()
                        {
                            Header="Создать",
                            Action=()=>
                            {
                                ProcessCommand("create");
                            },
                        }
                    },
                    {
                        "view",
                        new DataGridContextMenuItem()
                        {
                            Header="Открыть",                            
                            Action=()=>
                            {
                                ProcessCommand("view");
                            },
                        }
                    },
                };

                Grid.AutoUpdateInterval=0;
                Grid.Descriription="Список сообщений смс для рассылки";
                Grid.Init();
                //Grid.DebugShowColumnsInfo();
                //Grid.ShowDescription();
            }
        }

        public void SetDefaults()
        {
            DateFrom.Text=DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy 00:00:00");
            DateTo.Text=DateTime.Now.AddDays(1).ToString("dd.MM.yyyy 00:00:00");
        }
       
        public void ProcessCommand(string command, ItemMessage m=null)
        {
            command=command.ClearCommand();
            if(!command.IsNullOrEmpty())
            {
                switch(command)
                {
                    case "create":
                        {
                            //var email=new EmailView();
                            //email.Create();
                            ProcessItem("create");
                        }
                        break;

                    case "view":
                        {
                            var id=Grid.SelectedItem.CheckGet("ID").ToInt();    
                            if(id != 0)
                            {
                                //var email=new EmailView();
                                //email.Edit(id);
                                ProcessItem("edit", id.ToString());
                            }
                        }
                        break;

                    case "refresh":
                    {
                        Grid.LoadItems();
                    }
                        break;

                    case "help":
                    {
                        ///Central.ShowHelp("/doc/l-pack-erp/production/production_tasks/complete");
                    }
                        break;
                }
            }
        }

        public async void LoadItems()
        {
            GridToolbar.IsEnabled=false;
            bool resume = true;

            if(resume)
            {
                var f = DateFrom.Text.ToDateTime();
                var t = DateTo.Text.ToDateTime();
                if(DateTime.Compare(f,t) > 0)
                {
                    //var msg="Дата начала должна быть меньше даты окончания."; 
                    //var d = new DialogWindow($"{msg}", "Проверка данных", "", DialogWindowButtons.OK);
                    //d.ShowDialog();
                    resume=false;
                }
            }

            if(resume)
            {
                
                var p = new Dictionary<string,string>();
                {
                    p.Add("FROM_DATE",DateFrom.Text);
                    p.Add("TO_DATE",DateTo.Text);
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Messages");
                q.Request.SetParam("Object", "Sms");
                q.Request.SetParam("Action", "List");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;
                
                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {

                        {
                            var ds=ListDataSet.Create(result,"ITEMS");
                            Grid.UpdateItems(ds);
                        }
                    }
                }      
            }

            GridToolbar.IsEnabled=true;
        }

        private void ProcessItem(string mode = "create", string id = "")
        {
            var smsForm = new FormDialog()
            {
                RoleName = "[erp]messages",
                FrameName = "Sms",
                Title = $"Смс",
                Fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="ID",
                        Description="ИД",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        ControlType="void",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="PHONE_NUMBER",
                        Description="Получатель",
                        Params=new Dictionary<string, string>()
                        {
                            { "MaxLength", "11"},
                        },
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                            { FormHelperField.FieldFilterRef.DigitOnly, null },
                        },
                    },
                    new FormHelperField()
                    {
                        Path="TEXT",
                        Description="Сообщение",
                        Params=new Dictionary<string, string>()
                        {
                            { "ControlHeight", "40"},
                        },
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                     new FormHelperField()
                    {
                        Path="DATE_SEND_PLANNED",
                        Description="Дата отправки",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                        Fillers=new List<FormHelperFiller>{
                            {
                                new FormHelperFiller(){
                                    Name="SelectDate",
                                    Description="Сейчас",
                                    Caption="Сейчас",
                                    //IconStyle="SelectImage",
                                    Action=(FormHelper form)=>
                                    {
                                        var result="";
                                        {
                                            result=DateTime.Now.AddSeconds(15).ToString("dd.MM.yyyy HH:mm:ss");
                                        }
                                        return result;
                                    }
                                }
                            },
                            /*
                            {
                                new FormHelperFiller(){
                                    Name="SelectDate",
                                    Description="Через 5 мин",
                                    Caption="5 мин",
                                    //IconStyle="SelectImage",
                                    Action=(FormHelper form)=>
                                    {
                                        var result="";
                                        {
                                            result=DateTime.Now.AddMinutes(5).ToString("dd.MM.yyyy HH:mm:ss");
                                        }
                                        return result;
                                    }
                                }
                            }
                            */
                        },
                    },
                },
            };
            smsForm.QueryGet = new RequestData()
            {
                Module = "Messages",
                Object = "Sms",
                Action = "Get",
            };
            smsForm.AfterGet += (FormDialog fd) =>
            {
                var s = fd.Values.CheckGet("ID");
                switch(fd.Mode)
                {
                    case "create":
                    fd.FrameTitle = $"Новое сообщение";
                    break;

                    default:
                    fd.FrameTitle = $"Сообщение #{s}";
                    break;
                }
                fd.Open();
            };
            smsForm.QuerySave = new RequestData()
            {
                Module = "Messages",
                Object = "Sms",
                Action = "Save",
            };
            smsForm.BeforeDelete += (Dictionary<string, string> p) =>
            {
                bool resume = false;

                var msg = "";
                {
                    msg = msg.Append($"Удалить запись?");
                    msg = msg.Append($"{p.CheckGet("ID")}", true);
                }

                var d = new DialogWindow($"{msg}", "Удаление", "", DialogWindowButtons.NoYes);
                if((bool)d.ShowDialog())
                {
                    resume = true;
                }

                return resume;
            };
            smsForm.OnDelete += (FormDialog fd) =>
            {
                var result = false;
                {
                    result = true;
                }
                return result;
            };

            smsForm.AfterUpdate += (FormDialog fd) =>
            {
                fd.Hide();

                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = "",
                    ReceiverName = "SmsTab",
                    SenderName = ControlName,
                    Action = "Refresh",
                    Message = $"{fd.InsertId}",
                });
            };

            smsForm.PrimaryKey = "ID";
            smsForm.PrimaryKeyValue = id;
            smsForm.Commander.Init(smsForm);
            smsForm.Run(mode);
        }

        private void ButtonOnClick(object sender, RoutedEventArgs e)
        {
            var b=(Button)sender;
            if(b != null)
            {
                var t=b.Tag.ToString();
                ProcessCommand(t);
            }
        }
    }


}
