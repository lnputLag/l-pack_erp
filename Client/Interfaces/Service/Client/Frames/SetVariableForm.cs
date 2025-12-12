using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Client.Interfaces.Service
{
    /// <summary>
    /// форма установки кастомных переменных
    /// redis:config:client:variables
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2025-11-25</released>
    /// <changed>2025-11-25</changed>
    class SetVariableForm: FormDialog
    {
        public SetVariableForm()
        {
            Mode = "create";
            Row=new Dictionary<string, string>();

            OnKeyPressed = (KeyEventArgs e) =>
            {
                if(!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };
        }

        public string ReturnReceiverName {  get; set; }

        public void Init()
        {
            var fillers = new List<FormHelperFiller>();

            RoleName = "[erp]client";
            FrameName = "SetVariable";            
            Title = $"Переменные: ";
            Fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="APPLICATION_NAME",
                    Description = "APPLICATION NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    Fillers=new List<FormHelperFiller>(){
                        new FormHelperFiller()
                        {
                            Name = "l-pack_erp",
                            Caption = "client",
                            Action = (FormHelper form) =>
                            {
                                return "l-pack_erp";
                            }
                        },
                        new FormHelperFiller()
                        {
                            Name = "l-pack_erp_agent",
                            Caption = "agent",
                            Action = (FormHelper form) =>
                            {
                                return "l-pack_erp_agent";
                            }
                        },
                    },
                },
                new FormHelperField()
                {
                    Path="LOGIN",
                    Description = "LOGIN",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="HOST_USER_ID",
                    Description = "HOST_USER_ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="FACTORY_ID",
                    Description = "FACTORY ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    Fillers=new List<FormHelperFiller>(){
                        new FormHelperFiller()
                        {
                            Name = "1",
                            Caption = "Липецк",
                            Action = (FormHelper form) =>
                            {
                                return "1";
                            }
                        },
                        new FormHelperFiller()
                        {
                            Name = "2",
                            Caption = "Кашира",
                            Action = (FormHelper form) =>
                            {
                                return "2";
                            }
                        },
                    },
                },
                new FormHelperField()
                {
                    Path="SEGMENT",
                    Description = "SEGMENT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    Fillers=new List<FormHelperFiller>(){
                        new FormHelperFiller()
                        {
                            Name = "office",
                            Caption = "office",
                            Action = (FormHelper form) =>
                            {
                                return "office";
                            }
                        },
                        new FormHelperFiller()
                        {
                            Name = "office_it_developer",
                            Caption = "dev",
                            Action = (FormHelper form) =>
                            {
                                return "office_it_developer";
                            }
                        },
                        new FormHelperFiller()
                        {
                            Name = "production_corrugator_reel",
                            Caption = "reel",
                            Action = (FormHelper form) =>
                            {
                                return "production_corrugator_reel";
                            }
                        },
                        new FormHelperFiller()
                        {
                            Name = "production_corrugator_operator",
                            Caption = "operator",
                            Action = (FormHelper form) =>
                            {
                                return "production_corrugator_operator";
                            }
                        },
                        new FormHelperFiller()
                        {
                            Name = "stock_finished-goods-warehouse",
                            Caption = "fgw",
                            Action = (FormHelper form) =>
                            {
                                return "stock_finished-goods-warehouse";
                            }
                        },
                    },
                },
                new FormHelperField()
                {
                    Path="DESCRIPTION",
                    Description = "DESCRIPTION",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            OnGet += (FormDialog fd) =>
            {
                var r = false;
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Service");
                q.Request.SetParam("Object", "ClientVariable");
                q.Request.SetParam("Action", "Get");

                var p = fd.GetValues();                

                p.CheckAdd("APPLICATION_NAME",Row.CheckGet("APPLICATION_NAME").ToLower());
                p.CheckAdd("LOGIN",Row.CheckGet("LOGIN").ToLower());
                p.CheckAdd("HOST_USER_ID",Row.CheckGet("HOST_USER_ID").ToLower());
                q.Request.SetParams(p);

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        var first=ds.GetFirstItem();

                        first.CheckAdd("APPLICATION_NAME",Row.CheckGet("APPLICATION_NAME").ToLower());
                        first.CheckAdd("LOGIN",Row.CheckGet("LOGIN").ToLower());
                        first.CheckAdd("HOST_USER_ID",Row.CheckGet("HOST_USER_ID").ToLower());

                        fd.SetValues(first);
                        r=true;
                    }
                }
                return r;
            };
            AfterGet += (FormDialog fd) =>
            {
                fd.SaveButton.Content = "Установить";
                fd.Open();
            };
            OnSave += (FormDialog fd) =>
            {
                var r = false;
                var validationResult = fd.Validate();
                if(validationResult)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Service");
                    q.Request.SetParam("Object", "ClientVariable");
                    q.Request.SetParam("Action", "Save");
            
                    var p = fd.GetValues();
                    q.Request.SetParams(p);
                    q.DoQuery();
                    if(q.Answer.Status == 0)
                    {
                        r = true;
                    }
                    else
                    {
                        q.ProcessError();
                    }

                    fd.InsertId = p.CheckGet(fd.PrimaryKey);
                }
                return r;
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
            Commander.Init(this);
            Run(Mode);
        }

        /// <summary>
        /// режим работы
        /// create,edit
        /// </summary>
        public string Mode {  get; set; }
        public Dictionary<string, string> Row {  get; set; }
    }
}
