using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Client.Interfaces.Service
{
    /// <summary>
    /// форма просмотра информации
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2024-04-0</released>
    /// <changed>2024-04-05</changed>
    class ShowInfoForm : FormDialog
    {
        public ShowInfoForm()
        {
            Mode = "edit";

            OnKeyPressed = (KeyEventArgs e) =>
            {
                if(!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };
        }

        /// <summary>
        /// режим работы
        /// create,edit
        /// </summary>
        public string Mode { get; set; }

        public void Init()
        {
            RoleName = "[erp]client";
            FrameName = "ShowInfo";
            Title = $"Информация: ";
            Fields = new List<FormHelperField>()
            {
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
                    Path="_INFO",
                    Description = "INFO",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    ControlType="TextBox",
                    Params=new Dictionary<string, string>(){
                        { "ControlWidth" , "640"},
                        { "ControlHeight" , "480"},
                    },
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };
            QueryGet = new RequestData()
            {
                Module = "Service",
                Object = "Control",
                Action = "GetInfo",
            };
            AfterGet += (FormDialog fd) =>
            {
                var s = "";
                foreach(KeyValuePair<string, string> item in fd.Values)
                {
                    s = s.Append($"{item.Key}={item.Value}",true);
                }
                s = s.Trim();
                var p = new Dictionary<string, string>();
                p.CheckAdd("_INFO",s);
                p.CheckAdd("HOST_USER_ID", fd.Values.CheckGet("HOST_USER_ID"));
                fd.SetValues(p);
                fd.Open();
            };
            PrimaryKey = "HOST_USER_ID";
            Commander.Init(this);
            Run(Mode);
        }
    }
}
