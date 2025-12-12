using Client.Common;
using Client.Interfaces.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Accounts
{
    /// <summary>
    /// форма изменения пароля
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2024-04-05</released>
    /// <changed>2024-04-05</changed>
    class PasswordForm : FormDialog
    {
        public PasswordForm(string mode="edit", string id="")
        {
            RoleName = "[erp]password";
            FrameName = "Password";
            Title = $"Изменение пароля";
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
                    Path="LOGIN",
                    Description="LOGIN",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NAME",
                    Description="NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PASSWORD",
                    Description="Новый пароль",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Params=new Dictionary<string, string>(){
                        { "MaxLength" , "32"},
                    },
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
            };
            QueryGet=new RequestData()
            {
                Module="Accounts",
                Object= "Account",
                Action="Get",
            };
            AfterGet+=(FormDialog fd)=>
            {
                var p = fd.Values;
                p.CheckAdd("PASSWORD", "");
                fd.SetValues(p);
                fd.Open();
            };
            QuerySave=new RequestData()
            {
                Module="Accounts",
                Object= "Account",
                Action="Save",
            };
            AfterUpdate+=(FormDialog fd)=>
            {
                fd.Hide();
            };
            PrimaryKey="ID";
            PrimaryKeyValue=id;
            Commander.Init(this);
            Run(mode);
        }
    }
}
