using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Client.Interfaces.Service.Servers
{
    /// <summary>
    /// форма отправки команды серверу
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2024-12-20</released>
    /// <changed>2024-12-20</changed>
    class SendCommandForm:FormDialog
    {
        public SendCommandForm()
        {
            Mode = "create";
            ReturnReceiverName = "";
            DocumentationUrl = "/doc/l-pack-erp-new/service_new/servers";

            OnKeyPressed = (KeyEventArgs e) =>
            {
                if(!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };

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

        }

        public string ReturnReceiverName {  get; set; }

        public void Init()
        {
            var fillers = new List<FormHelperFiller>();
            {
                fillers.Add(
                    new FormHelperFiller()
                    {
                        Name = "status",
                        Caption = "Status",
                        Style= "ButtonCompact",
                        Action = (FormHelper form) =>
                        {
                            return "Status";
                        }
                    }
                );
                fillers.Add(
                    new FormHelperFiller()
                    {
                        Name = "restart",
                        Caption = "Restart",
                        Style = "ButtonCompact",
                        Action = (FormHelper form) =>
                        {
                            return "Restart";
                        }
                    }
                );
                fillers.Add(
                    new FormHelperFiller()
                    {
                        Name = "kill",
                        Caption = "Kill",
                        Style = "ButtonCompact",
                        Action = (FormHelper form) =>
                        {
                            return "Kill";
                        }
                    }
                );
                fillers.Add(
                    new FormHelperFiller()
                    {
                        Name = "update",
                        Caption = "Update",
                        Style = "ButtonCompact",
                        Action = (FormHelper form) =>
                        {
                            return "Update";
                        }
                    }
                );
                fillers.Add(
                   new FormHelperFiller()
                   {
                       Name = "upgrade_pull",
                       Caption = "UpgradePull",
                       Style = "ButtonCompact",
                       Action = (FormHelper form) =>
                       {
                           return "UpgradePull";
                       }
                   }
               );

            }

            RoleName = "[erp]server";
            FrameName = "SendCommand";
            Title = $"Команда: ";
            Fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="UID",
                    Description = "UID",
                    Default=Cryptor.MakeUid(),
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path= "INSTANCE_NAME",
                    Description = "INSTANCE_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path= "TTL",
                    Description = "TTL",
                    Default="60",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CMD",
                    Description = "CMD",
                    Default="",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    Fillers=fillers,
                },             
            };

            OnGet += (FormDialog fd) =>
            {
                var p = new Dictionary<string, string>();
                p.CheckAdd(fd.PrimaryKey, fd.PrimaryKeyValue);
                fd.SetValues(p);
                return true;
            };
            AfterGet += (FormDialog fd) =>
            {
                fd.SaveButton.Content = "Отправить";
                fd.Open();
            };
            OnSave += (FormDialog fd) =>
            {
                var result = false;
                var validationResult = fd.Validate();
                if(validationResult)
                {
                    var p = fd.GetValues();
                    SendCommand2(p);

                    fd.InsertId = p.CheckGet(fd.PrimaryKey);
                    result = SendCommand2Result;
                }
                return result;
            };
            AfterUpdate += (FormDialog fd) =>
            {
                if(ReturnReceiverName.IsNullOrEmpty())
                {
                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverGroup = "",
                        ReceiverName = ReturnReceiverName,
                        SenderName = ControlName,
                        Action = "refresh",
                        Message = $"{fd.InsertId}",
                    });
                }
                fd.Hide();
            };
            PrimaryKey = "INSTANCE_NAME";
            Commander.Init(this);
            Run(Mode);
        }

        /// <summary>
        /// режим работы
        /// create,edit
        /// </summary>
        public string Mode {  get; set; }

        private bool SendCommand2Result { get; set; }
        private async void SendCommand2(Dictionary<string, string> item)
        {
            var resultData = new Dictionary<string, string>();
            SendCommand2Result = false;

            //var p = new Dictionary<string, string>();
            //{
            //    p.Add("TABLE_DIRECTORY", "");
            //    p.Add("TABLE_NAME", "instance_command");
            //    p.Add("PRIMARY_KEY", "UID");
            //    p.Add("PRIMARY_KEY_VALUE", item.CheckGet("UID"));
            //    // 1=global,2=local,3=net
            //    p.Add("STORAGE_TYPE", "3");
            //    p.Add("ITEMS", JsonConvert.SerializeObject(item));
            //}

            var p = new Dictionary<string, string>();
            {
                p.Add("INSTANCE_NAME", item.CheckGet("INSTANCE_NAME"));
                p.Add("COMMAND", item.CheckGet("CMD"));
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Service/Server");
            q.Request.SetParam("Object", "Command");
            q.Request.SetParam("Action", "Save");
            q.Request.SetParams(p);

            q.DoQuery();

            if(q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if(result != null)
                {
                    SendCommand2Result = true;
                }
            }
            else
            {
                q.ProcessError();
            }
        }

    }
}
