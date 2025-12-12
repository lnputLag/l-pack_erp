using System.Collections.Generic;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;

namespace Client.Interfaces.Service.Servers
{
    public class SendTechnicalWorkStatusForm : FormDialog
    {
        public SendTechnicalWorkStatusForm()
        {
            _mode = "edit";
            DocumentationUrl = "/doc/l-pack-erp-new/service_new/servers";
            
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

        private string _mode;

        public void Init()
        {
            var fillers = new List<FormHelperFiller>()
            {
                new FormHelperFiller()
                {
                    Name = "off",
                    Caption = "Off",
                    Style = "ButtonCompact",
                    Action = form => "0"
                },

                new FormHelperFiller()
                {
                    Name = "on",
                    Caption = "On",
                    Style = "ButtonCompact",
                    Action = form => "1"
                }
            };
            
            RoleName = "[erp]server";
            FrameName = "SendTechnicalWorkStatus";
            Title = "Смена статуса тех. раб";
            Fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path= "TEXT",
                    Description = "TEXT",
                    Default = "",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path= "TOOLTIP",
                    Description = "TOOLTIP",
                    Default="",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STATUS",
                    Description = "STATUS",
                    Default="",
                    Enabled = false,
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null}
                    },
                    Fillers=fillers,
                },     
            };

            OnGet += fd =>
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Service");
                q.Request.SetParam("Object", "Server");
                q.Request.SetParam("Action", "TechnicalWorkStatusGet");

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(q.Answer.Data);
                    fd.SetValues(result);

                    return true;
                }

                return false;
            };
            
            AfterGet += fd =>
            {
                fd.SaveButton.Content = "Отправить";
                fd.Open();
            };
            
            OnSave += fd =>
            {
                var result = false;
                var validationResult = fd.Validate();
                if(validationResult)
                {
                    var p = fd.GetValues();
                    result = SendStatusTechnicalWork(p);
                }
                return result;
            };

            AfterUpdate += fd =>
            {
                fd.Hide();
            };
            
            Commander.Init(this);
            Run(_mode);
        }

        private bool SendStatusTechnicalWork(Dictionary<string, string> p)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Service");
            q.Request.SetParam("Object", "Server");
            q.Request.SetParam("Action", "TechnicalWorkStatusUpdate");
            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<bool>(q.Answer.Data);
                return result;
            }

            q.ProcessError();
            return false;
        }
    }
}