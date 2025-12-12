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
    /// форма отправки команды
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-11-08</released>
    /// <changed>2024-04-05</changed>
    class SendCommandForm: FormDialog
    {
        public SendCommandForm()
        {
            Mode = "create";
            Scope = "";
            ReturnReceiverName = "";
            CurrentReceiverType = ReceiverType.Single;

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
            var fillers2 = new List<FormHelperFiller>();

            if(Scope == "user" || Scope == "mobile")
            {
                fillers.Add(
                    new FormHelperFiller()
                    {
                        Name = "SHOW_NOTIFICATION",
                        Caption = "SHOW_NOTIFICATION",
                        Action = (FormHelper form) =>
                        {
                            var ts = DateTime.Now.ToString("yyyy-MM-dd_HH:mm");

                            var v = new Dictionary<string, string>();
                            v.CheckAdd("COMMAND", "SHOW_NOTIFICATION");
                            v.CheckAdd("TYPE", "2");
                            v.CheckAdd("TITLE", "Test notification");
                            v.CheckAdd("CODE", $"res_{ts}");
                            v.CheckAdd("CLASS", $"res_{ts}");
                            v.CheckAdd("CONTENT", "Sed facilisis viverra nisi, sed gravida mauris");
                            v.CheckAdd("LINK", "");
                            v.CheckAdd("SCOPE", Scope);
                            form.SetValues(v);

                            return "";
                        }
                    }
                );
            }

            if(Scope == "user" || Scope == "mobile")
            {
                fillers.Add(
                    new FormHelperFiller()
                    {
                        Name = "SHOW_NOTIFICATION_UPDATE",
                        Caption = "SHOW_NOTIFICATION (UPDATE)",
                        Action = (FormHelper form) =>
                        {
                            var ts = DateTime.Now.ToString("yyyy-MM-dd_HH:mm");

                            var v = new Dictionary<string, string>();
                            v.CheckAdd("COMMAND", "SHOW_NOTIFICATION");
                            v.CheckAdd("TYPE", "1");
                            v.CheckAdd("TITLE", "Щелкните здесь, чтобы обновить");
                            v.CheckAdd("CODE", $"res_{ts}");
                            v.CheckAdd("CLASS", $"res_{ts}");
                            v.CheckAdd("CONTENT", "Необходимо обновление программы");
                            v.CheckAdd("LINK", "l-pack://l-pack_erp/documentation/update");
                            v.CheckAdd("SCOPE", Scope);
                            form.SetValues(v);

                            return "";
                        }
                    }
                );
            }

            if (Scope == "user")
            {
                fillers.Add(
                    new FormHelperFiller()
                    {
                        Name = "SHOW_NOTIFICATION_SERVER_UPDATING",
                        Caption = "SHOW_NOTIFICATION (Сервер обновляется)",
                        Action = (FormHelper form) =>
                        {
                            var ts = DateTime.Now.ToString("yyyy-MM-dd_HH:mm");

                            var v = new Dictionary<string, string>();
                            v.CheckAdd("COMMAND", "SHOW_NOTIFICATION");
                            v.CheckAdd("TYPE", "1");
                            v.CheckAdd("TITLE", "Пожалуйста, подождите, идёт обновление");
                            v.CheckAdd("CODE", $"res_{ts}");
                            v.CheckAdd("CLASS", $"res_{ts}");
                            v.CheckAdd("CONTENT", "В течение 5 минут возможны задержки в работе программы");
                            v.CheckAdd("LINK", "");
                            v.CheckAdd("SCOPE", Scope);
                            form.SetValues(v);

                            return "";
                        }
                    }
                );
            }

            if (Scope == "user" || Scope == "agent")
            {
                fillers.Add(
                    new FormHelperFiller()
                    {
                        Name = "_TEST_STRING",
                        Caption = "_TEST_STRING",
                        Action = (FormHelper form) =>
                        {
                            var ts = DateTime.Now.ToString("yyyy-MM-dd_HH:mm");

                            var v = new Dictionary<string, string>();
                            v.CheckAdd("COMMAND", "_TEST_STRING");
                            v.CheckAdd("MESSAGE", "Test");

                            v.CheckAdd("TYPE", "");
                            v.CheckAdd("TITLE", "");
                            v.CheckAdd("CODE", "");
                            v.CheckAdd("CLASS", "");
                            v.CheckAdd("CONTENT", "");
                            v.CheckAdd("LINK", "");
                            v.CheckAdd("SCOPE", Scope);
                            form.SetValues(v);

                            return "";
                        }
                    }
                );
            }

            if(Scope == "user" || Scope == "agent")
            {
                fillers.Add(
                    new FormHelperFiller()
                    {
                        Name = "SYSTEM_RESTART",
                        Caption = "SYSTEM_RESTART",
                        Action = (FormHelper form) =>
                        {
                            var ts = DateTime.Now.ToString("yyyy-MM-dd_HH:mm");

                            var v = new Dictionary<string, string>();
                            v.CheckAdd("COMMAND", "SYSTEM_RESTART");
                            v.CheckAdd("MESSAGE", "");

                            v.CheckAdd("TYPE", "");
                            v.CheckAdd("TITLE", "");
                            v.CheckAdd("CODE", "");
                            v.CheckAdd("CLASS", "");
                            v.CheckAdd("CONTENT", "");
                            v.CheckAdd("LINK", "");
                            v.CheckAdd("SCOPE", Scope);
                            form.SetValues(v);

                            return "";
                        }
                    }
                );
            }

            if(Scope == "user" || Scope == "agent")
            {
                fillers.Add(
                    new FormHelperFiller()
                    {
                        Name = "SYSTEM_SHUTDOWN",
                        Caption = "SYSTEM_SHUTDOWN",
                        Action = (FormHelper form) =>
                        {
                            var ts = DateTime.Now.ToString("yyyy-MM-dd_HH:mm");

                            var v = new Dictionary<string, string>();
                            v.CheckAdd("COMMAND", "SYSTEM_SHUTDOWN");
                            v.CheckAdd("MESSAGE", "");

                            v.CheckAdd("TYPE", "");
                            v.CheckAdd("TITLE", "");
                            v.CheckAdd("CODE", "");
                            v.CheckAdd("CLASS", "");
                            v.CheckAdd("CONTENT", "");
                            v.CheckAdd("LINK", "");
                            v.CheckAdd("SCOPE", Scope);
                            form.SetValues(v);

                            return "";
                        }
                    }
                );
            }

            if(Scope == "user" )
            {
                fillers.Add(
                    new FormHelperFiller()
                    {
                        Name = "DO_HOP",
                        Caption = "DO_HOP",
                        Action = (FormHelper form) =>
                        {
                            var ts = DateTime.Now.ToString("yyyy-MM-dd_HH:mm");

                            var v = new Dictionary<string, string>();
                            v.CheckAdd("COMMAND", "DO_HOP");
                            v.CheckAdd("MESSAGE", "");

                            v.CheckAdd("TYPE", "");
                            v.CheckAdd("TITLE", "");
                            v.CheckAdd("CODE", "");
                            v.CheckAdd("CLASS", "");
                            v.CheckAdd("CONTENT", "");
                            v.CheckAdd("LINK", "");
                            v.CheckAdd("SCOPE", Scope);
                            form.SetValues(v);

                            return "";
                        }
                    }
                );
            }


            if(Scope == "user" || Scope == "agent" || Scope=="mobile")
            {
                fillers.Add(
                    new FormHelperFiller()
                    {
                        Name = "SYSTEM_UPDATE_CONFIG_SERVER",
                        Caption = "SET_SERVER",
                        Action = (FormHelper form) =>
                        {
                            var ts = DateTime.Now.ToString("yyyy-MM-dd_HH:mm");

                            var v = new Dictionary<string, string>();
                            v.CheckAdd("COMMAND", "SYSTEM_UPDATE_CONFIG_SERVER");
                            v.CheckAdd("MESSAGE", "http://192.168.3.60:5678,http://192.168.3.184:5678,http://192.168.3.204:5678");

                            v.CheckAdd("TYPE", "");
                            v.CheckAdd("TITLE", "");
                            v.CheckAdd("CODE", "");
                            v.CheckAdd("CLASS", "");
                            v.CheckAdd("CONTENT", "");
                            v.CheckAdd("LINK", "");
                            v.CheckAdd("SCOPE", Scope);
                            form.SetValues(v);

                            return "";
                        }
                    }
                );
                fillers2.Add(
                    new FormHelperFiller()
                    {
                        Name = "SYSTEM_UPDATE_CONFIG_LP_OFFICE",
                        Caption = "LP_OFFICE",
                        Action = (FormHelper form) =>
                        {
                            var ts = DateTime.Now.ToString("yyyy-MM-dd_HH:mm");

                            var v = new Dictionary<string, string>();
                            v.CheckAdd("COMMAND", "SYSTEM_UPDATE_CONFIG_SERVER");
                            v.CheckAdd("MESSAGE", "http://192.168.3.34:5678,http://192.168.3.242:5678,http://192.168.3.245:5678");

                            v.CheckAdd("TYPE", "");
                            v.CheckAdd("TITLE", "");
                            v.CheckAdd("CODE", "");
                            v.CheckAdd("CLASS", "");
                            v.CheckAdd("CONTENT", "");
                            v.CheckAdd("LINK", "");
                            v.CheckAdd("SCOPE", Scope);
                            form.SetValues(v);

                            return "";
                        }
                    }
                );
                fillers2.Add(
                    new FormHelperFiller()
                    {
                        Name = "SYSTEM_UPDATE_CONFIG_LP_PROD",
                        Caption = "LP_PROD",
                        Action = (FormHelper form) =>
                        {
                            var ts = DateTime.Now.ToString("yyyy-MM-dd_HH:mm");

                            var v = new Dictionary<string, string>();
                            v.CheckAdd("COMMAND", "SYSTEM_UPDATE_CONFIG_SERVER");
                            v.CheckAdd("MESSAGE", "http://192.168.3.86:5678,http://192.168.3.241:5678,http://192.168.3.246:5678");

                            v.CheckAdd("TYPE", "");
                            v.CheckAdd("TITLE", "");
                            v.CheckAdd("CODE", "");
                            v.CheckAdd("CLASS", "");
                            v.CheckAdd("CONTENT", "");
                            v.CheckAdd("LINK", "");
                            v.CheckAdd("SCOPE", Scope);
                            form.SetValues(v);

                            return "";
                        }
                    }
                );
                fillers2.Add(
                    new FormHelperFiller()
                    {
                        Name = "SYSTEM_UPDATE_CONFIG_KS_OFFICE",
                        Caption = "KS_OFFICE",
                        Action = (FormHelper form) =>
                        {
                            var ts = DateTime.Now.ToString("yyyy-MM-dd_HH:mm");

                            var v = new Dictionary<string, string>();
                            v.CheckAdd("COMMAND", "SYSTEM_UPDATE_CONFIG_SERVER");
                            v.CheckAdd("MESSAGE", "http://172.16.3.43:5678,http://172.16.3.44:5678,http://172.16.3.50:5678");

                            v.CheckAdd("TYPE", "");
                            v.CheckAdd("TITLE", "");
                            v.CheckAdd("CODE", "");
                            v.CheckAdd("CLASS", "");
                            v.CheckAdd("CONTENT", "");
                            v.CheckAdd("LINK", "");
                            v.CheckAdd("SCOPE", Scope);
                            form.SetValues(v);

                            return "";
                        }
                    }
                );
            }

            if(Scope == "agent")
            {
                fillers.Add(
                    new FormHelperFiller()
                    {
                        Name = "SYSTEM_UPDATE_CONFIG_INSTALLATION_PLACE",
                        Caption = "SET_INSTALLATION_PLACE",
                        Action = (FormHelper form) =>
                        {
                            var ts = DateTime.Now.ToString("yyyy-MM-dd_HH:mm");

                            var v = new Dictionary<string, string>();
                            v.CheckAdd("COMMAND", "SYSTEM_UPDATE_CONFIG_INSTALLATION_PLACE");
                            v.CheckAdd("MESSAGE", "");

                            v.CheckAdd("TYPE", "");
                            v.CheckAdd("TITLE", "");
                            v.CheckAdd("CODE", "");
                            v.CheckAdd("CLASS", "");
                            v.CheckAdd("CONTENT", "");
                            v.CheckAdd("LINK", "");
                            v.CheckAdd("SCOPE", Scope);
                            form.SetValues(v);

                            return "";
                        }
                    }
                );
            }

            if(Scope == "agent")
            {
                // Обновляет файл настройки сервисов
                fillers.Add(
                    new FormHelperFiller()
                    {
                        Name = "SYSTEM_UPDATE_CONFIG",
                        Caption = "SYSTEM_UPDATE_CONFIG",
                        Action = (FormHelper form) =>
                        {
                            var ts = DateTime.Now.ToString("yyyy-MM-dd_HH:mm");

                            var v = new Dictionary<string, string>();
                            v.CheckAdd("COMMAND", "SYSTEM_UPDATE_CONFIG");
                            v.CheckAdd("MESSAGE", "");

                            v.CheckAdd("TYPE", "");
                            v.CheckAdd("TITLE", "");
                            v.CheckAdd("CODE", "");
                            v.CheckAdd("CLASS", "");
                            v.CheckAdd("CONTENT", "");
                            v.CheckAdd("LINK", "");
                            v.CheckAdd("SCOPE", Scope);
                            form.SetValues(v);

                            return "";
                        }
                    }
                );
            }

            RoleName = "[erp]client";
            FrameName = "SendCommand";
            Title = $"Команда: ";
            Fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="CODE",
                    Description = "Код",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CLASS",
                    Description = "Класс",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                //list.Add("1", "(1) Побудительное сообщение");   развернутое
                //list.Add("2", "(2) Информационное сообщение");  свернутое              
                //list.Add("9", "(9) Системное");                 системное, невидимое
                new FormHelperField()
                {
                    Path="TYPE",
                    Description = "Тип",
                    Default="1",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TITLE",
                    Description = "Заголовок",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CONTENT",
                    Description = "Сообщение",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="LINK",
                    Description = "Ссылка",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                new FormHelperField()
                {
                    Path="HOST_USER_ID",
                    Description = "ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    Enabled = false,
                    OnCreate = (FormHelperField f) =>
                    {
                        if (CurrentReceiverType == ReceiverType.Single)
                        {
                            f.Enabled = true;
                            if (f.Control is FrameworkElement)
                            {
                                ((FrameworkElement)f.Control).IsEnabled = true;
                            }
                        }
                        else
                        {
                            f.Enabled = false;
                            if (f.Control is FrameworkElement)
                            {
                                ((FrameworkElement)f.Control).IsEnabled = false;
                            }
                        }
                    }
                },
                new FormHelperField()
                {
                    Path="GROUP_NAME",
                    Description = "Группа",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="SelectBox",
                    Width = 320,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    Enabled = false,
                    OnCreate = (FormHelperField f) =>
                    {
                        if (CurrentReceiverType == ReceiverType.ByGroup)
                        {
                            f.Enabled = true;
                            if (f.Control is FrameworkElement)
                            {
                                ((FrameworkElement)f.Control).IsEnabled = true;
                            }
                        }
                        else
                        {
                            f.Enabled = false;
                            if (f.Control is FrameworkElement)
                            {
                                ((FrameworkElement)f.Control).IsEnabled = false;
                            }
                        }

                        var c= CurrentReceiverType == ReceiverType.ByGroup ? (SelectBox)f.Control : new SelectBox();

                        var column = new List<DataGridHelperColumn>
                        {
                            new DataGridHelperColumn
                            {
                                Header="Код",
                                Path="CODE",
                                ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                                Width2=20,
                            },
                            new DataGridHelperColumn
                            {
                                Header="Название",
                                Path="NAME",
                                ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                                Width2=20,
                            },
                            new DataGridHelperColumn
                            {
                                Header="Описание",
                                Path="DESCRIPTION",
                                ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                                Width2=20,
                            },
                            new DataGridHelperColumn
                            {
                                Header="ИД",
                                Path="ID",
                                ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                                Width2=6,
                            },
                        };
                        c.GridColumns = column;
                        c.SelectedItemValue = "NAME CODE";

                        c.ListBoxMinWidth = 500;
                        c.ListBoxMinHeight = 300;
                        c.Style = FindResource("CustomFormField");
                        c.DataType = SelectBox.DataTypeRef.Grid;
                        c.GridPrimaryKey= "ID";
                    },
                    QueryLoadItems = new RequestData()
                    {
                        Module = "Accounts",
                        Object = "Group",
                        Action = "List",
                        AnswerSectionKey = "ITEMS",
                        OnComplete = (f, ds) =>
                        {
                            var c = (SelectBox)f.Control;
                            c.GridDataSet = ds;
                            FieldSetValueActual("GROUP_NAME");
                        }
                    }
                },
                new FormHelperField()
                {
                    Path="ROLE_NAME",
                    Description = "Роль",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="SelectBox",
                    Width = 320,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    Enabled = false,
                    OnCreate = (FormHelperField f) =>
                    {
                        if (CurrentReceiverType == ReceiverType.ByRole)
                        {
                            f.Enabled = true;
                            if (f.Control is FrameworkElement)
                            {
                                ((FrameworkElement)f.Control).IsEnabled = true;
                            }
                        }
                        else
                        {
                            f.Enabled = false;
                            if (f.Control is FrameworkElement)
                            {
                                ((FrameworkElement)f.Control).IsEnabled = false;
                            }
                        }

                        var c= CurrentReceiverType == ReceiverType.ByRole ? (SelectBox)f.Control : new SelectBox();

                        var column = new List<DataGridHelperColumn>
                        {
                            new DataGridHelperColumn()
                            {
                                Header = "Наименование",
                                Path = "NAME",
                                ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                                Width2 = 20,
                            },
                            new DataGridHelperColumn()
                            {
                                Header = "Код",
                                Path = "CODE",
                                ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                                Width2 = 20,
                            },
                            new DataGridHelperColumn()
                            {
                                Header = "ИД",
                                Path = "ID",
                                ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                                Width2 = 8,
                            },
                        };
                        c.GridColumns = column;
                        c.SelectedItemValue = "NAME CODE";

                        c.ListBoxMinWidth = 500;
                        c.ListBoxMinHeight = 300;
                        c.Style = FindResource("CustomFormField");
                        c.DataType = SelectBox.DataTypeRef.Grid;
                        c.GridPrimaryKey= "ID";
                    },
                    QueryLoadItems = new RequestData()
                    {
                        Module = "Accounts",
                        Object = "Role",
                        Action = "ListNew",
                        AnswerSectionKey = "ITEMS",
                        OnComplete = (f, ds) =>
                        {
                            var c = (SelectBox)f.Control;
                            c.GridDataSet = ds;
                            FieldSetValueActual("ROLE_NAME");
                        }
                    }
                },
                new FormHelperField()
                {
                    Path="ALL_USER_FLAG",
                    Description = "Все пользователи",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    Enabled = false,
                    OnCreate = (FormHelperField f) =>
                    {
                        if (CurrentReceiverType == ReceiverType.All)
                        {
                            f.Enabled = true;
                            if (f.Control is FrameworkElement)
                            {
                                ((FrameworkElement)f.Control).IsEnabled = true;
                            }
                        }
                        else
                        {
                            f.Enabled = false;
                            if (f.Control is FrameworkElement)
                            {
                                ((FrameworkElement)f.Control).IsEnabled = false;
                            }
                        }
                    }
                },

                new FormHelperField()
                {
                    Path="COMMAND",
                    Description = "Команда",
                    Default="SHOW_NOTIFICATION",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="MESSAGE",
                    Description = "Сообщение",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SCOPE",
                    Description = "Сфера",
                    Default="",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="_VERSION",
                    Description = "Версия",
                    Default="1",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="USERS",
                    Description = "USERS",
                    Default="1",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="_TPL3",
                    Description = "Команды",
                    ControlType="",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="_TPL4",
                    Description = "",
                    ControlType="",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    Fillers=fillers,
                },
                new FormHelperField()
                {
                    Path="_TPL5",
                    Description = "",
                    ControlType="",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    Fillers=fillers2,
                },
            };

            switch (CurrentReceiverType)
            {
                case ReceiverType.ByRole:
                    PrimaryKey = "ROLE_NAME";
                    break;

                case ReceiverType.ByGroup:
                    PrimaryKey = "GROUP_NAME";
                    break;

                case ReceiverType.All:
                    PrimaryKey = "ALL_USER_FLAG";
                    break;

                case ReceiverType.Selected:
                    PrimaryKey = "HOST_USER_ID";
                    break;

                case ReceiverType.Single:
                default:
                    PrimaryKey = "HOST_USER_ID";
                    break;
            }

            OnGet += (FormDialog fd) =>
            {
                var p = new Dictionary<string, string>();
                p.CheckAdd(PrimaryKey, PrimaryKeyValue);
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
            Commander.Init(this);
            Run(Mode);
        }

        /// <summary>
        /// режим работы
        /// create,edit
        /// </summary>
        public string Mode {  get; set; }
        /// <summary>
        /// область действия
        /// mobile,user,agent
        /// </summary>
        public string Scope { get; set; }

        /// <summary>
        /// Тип получателей команды:
        /// Single -- Конкретный клиент
        /// ByRole  -- Список клиентов по роли
        /// ByGroup -- Список клиентов по группе
        /// All -- Все активные клиенты
        /// Selected -- Выбранные клиенты
        /// </summary>
        public enum ReceiverType
        {
            /// <summary>
            /// Конкретный клиент
            /// </summary>
            Single,

            /// <summary>
            /// Список клиентов по роли
            /// </summary>
            ByRole,

            /// <summary>
            /// Список клиентов по группе
            /// </summary>
            ByGroup,

            /// <summary>
            /// Все активные клиенты
            /// </summary>
            All,

            /// <summary>
            /// Выбранные клиенты
            /// </summary>
            Selected
        }

        public ReceiverType CurrentReceiverType { get; set; }

        /// <summary>
        /// интервал неактивности клиента
        /// </summary>
        public int OnlineTimeout { get; set; }

        private bool SendCommand2Result { get; set; }

        private async void SendCommand2(Dictionary<string, string> p)
        {
            var resultData = new Dictionary<string, string>();
            SendCommand2Result = false;

            var modeOld = false;
            if(p.CheckGet("_VERSION").ToInt() == 0)
            {
                modeOld = true;
            }

            var q = new LPackClientQuery();

            if(modeOld)
            {
                q.Request.SetParam("Module", "Messages");
                q.Request.SetParam("Object", "Notification");
                q.Request.SetParam("Action", "Save");
            }
            else
            {
                q.Request.SetParam("Module", "Service");
                q.Request.SetParam("Object", "Control");

                switch (CurrentReceiverType)
                {
                    case ReceiverType.All:
                        if (OnlineTimeout > 0)
                        {
                            p.CheckAdd("ON_LINE_TIMEOUT", $"{OnlineTimeout}");
                        }

                        q.Request.SetParam("Action", "SaveMessageForAll");

                        break;

                    case ReceiverType.ByRole:
                        q.Request.SetParam("Action", "SaveMessageForRole");
                        break;
                    case ReceiverType.ByGroup:
                        q.Request.SetParam("Action", "SaveMessageForGroup");
                        break;
                    case ReceiverType.Single:
                    case ReceiverType.Selected:
                    default:

                        q.Request.SetParam("Action", "SaveMessage");

                        break;
                }
            }
            q.Request.SetParams(p);

            //await Task.Run(() =>
            //{
            q.DoQuery();
            //});

            if(q.Answer.Status == 0)
            {
                if(modeOld)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(q.Answer.Data);
                    if(result != null)
                    {
                        SendCommand2Result = true;
                    }
                }
                else
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if(result != null)
                    {
                        SendCommand2Result = true;
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }
    }
}
