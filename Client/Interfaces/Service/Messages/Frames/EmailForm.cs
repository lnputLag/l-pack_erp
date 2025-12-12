using Client.Common;
using Client.Interfaces.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Client.Interfaces.Messages
{
    /// <summary>
    /// форма редактирования сообщения e-mail
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-11-08</released>
    /// <changed>2024-09-03</changed>
    class EmailForm:FormDialog
    {
        public EmailForm()
        {
            OnKeyPressed = (KeyEventArgs e) =>
            {
                if(!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };
        }

        public void Init(string mode="create", string id="")
        {
            RoleName = "[erp]messages";
            FrameName = "Email";
            Title = $"Сообщение";
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
                    Path="SENDER",
                    Description="Отправитель",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="RECIPIENT",
                    Description="Получатель",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="RECIPIENTCOPY",
                    Description="Получатель2",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SUBJECT",
                    Description="Тема",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                 new FormHelperField()
                {
                    Path="SENDDATE",
                    Description="Дата отправки",
                    FieldType=FormHelperField.FieldTypeRef.DateTime,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="MESSAGE",
                    Description="Текст сообщения",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    Params=new Dictionary<string, string>()
                    {
                        { "ControlHeight", "150" },
                    },
                },
                new FormHelperField()
                {
                    Path="CODE",
                    Description="Код ошибки",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    Params=new Dictionary<string, string>()
                    {
                        { "ControlHeight", "50" },
                    },
                },
            };

            QueryGet=new RequestData()
            {
                Module= "Messages",
                Object= "Email",
                Action="Get",
            };
            AfterGet+=(FormDialog fd)=>
            {
                var s=fd.Values.CheckGet("ID");
                //var s=fd.Values.CheckGet("NAME");
                switch(fd.Mode)
                {
                    case "create":
                        fd.FrameTitle=$"Новый E-mail";
                        break;

                    default:
                        fd.FrameTitle=$"E-mail <{s}>";
                        break;
                }
                fd.Open();
            };

            QuerySave=new RequestData()
            {
                Module= "Messages",
                Object= "Email",
                Action= "Save",
            };

            AfterUpdate+=(FormDialog fd)=>
            {
                fd.Hide();
                
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = "",
                    ReceiverName = "EmailTab",
                    SenderName = ControlName,
                    Action = "email_refresh",
                    Message = $"{fd.InsertId}",
                });
            };

            PrimaryKey="ID";
            PrimaryKeyValue=id;
            Commander.Init(this);
            Run(mode);
        }
    }
}
