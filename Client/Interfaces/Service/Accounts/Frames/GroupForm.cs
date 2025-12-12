using Client.Common;
using Client.Interfaces.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Client.Interfaces.Accounts
{
    /// <summary>
    /// форма редактирования группы ролей
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-11-08</released>
    /// <changed>2024-04-12</changed>
    class GroupForm : FormDialog
    {
        public GroupForm()
        {
            Mode = "";
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
            RoleName = "[erp]accounts";
            FrameName = "Group";
            Title = $"Группа";
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
                    Path="CODE",
                    Description="Код",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 32 },
                    },
                },
                new FormHelperField()
                {
                    Path="NAME",
                    Description="Наименование",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 64 },
                    },
                },
                new FormHelperField()
                {
                    Path="DESCRIPTION",
                    Description="Описание",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.MaxLen, 256 },
                    },
                },
            };
            QueryGet=new RequestData()
            {
                Module="Accounts",
                Object= "Group",
                Action="Get",
            };
            AfterGet+=(FormDialog fd)=>
            {
                //var s=fd.Values.CheckGet("ID");
                var s=fd.Values.CheckGet("NAME");
                switch(fd.Mode)
                {
                    case "create":
                        fd.FrameTitle=$"Новая группа";
                        break;

                    default:
                        fd.FrameTitle=$"Группа <{s}>";
                        break;
                }
                fd.Open();
            };

            QuerySave=new RequestData()
            {
                Module="Accounts",
                Object= "Group",
                Action="Save",
            };
            AfterUpdate+=(FormDialog fd)=>
            {
                fd.Hide();
                
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = "",
                    ReceiverName = "GroupTab",
                    SenderName = ControlName,
                    Action = "refresh",
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
