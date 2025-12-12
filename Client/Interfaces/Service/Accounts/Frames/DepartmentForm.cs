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
    /// форма редактирования отдела
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-11-08</released>
    /// <changed>2023-11-08</changed>
    class DepartmentForm:FormDialog
    {
        public DepartmentForm()
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
            RoleName = "[erp]accounts";
            FrameName = "Department";
            Title = $"Отдел";
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
                    Path="NAME",
                    Description="Название",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="SHORT_NAME",
                    Description="Сокращенное название",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            QueryGet=new RequestData()
            {
                Module="Accounts",
                Object="Department",
                Action="Get",
            };
            AfterGet+=(FormDialog fd)=>
            {
                //var s=fd.Values.CheckGet("ID");
                var s=fd.Values.CheckGet("NAME");
                switch(fd.Mode)
                {
                    case "create":
                        fd.FrameTitle=$"Новый отдел";
                        break;

                    default:
                        fd.FrameTitle=$"Отдел <{s}>";
                        break;
                }
                fd.Open();
            };

            QuerySave=new RequestData()
            {
                Module="Accounts",
                Object="Department",
                Action="Save",
            };

            AfterUpdate+=(FormDialog fd)=>
            {
                fd.Hide();
                
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = "",
                    ReceiverName = "DepartmentPositionTab",
                    SenderName = ControlName,
                    Action = "department_refresh",
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
