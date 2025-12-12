using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Service
{
    /// <summary>
    /// список агентов
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-02-16</released>
    /// <changed>2023-10-04</changed>
    public partial class SessionList : ControlBase
    {
        public SessionList()
        {
            InitializeComponent();


            RoleName = "[erp]client";
            ControlTitle = "Сессии";
            DocumentationUrl = "/doc/l-pack-erp/service/agent/sessions";

            OnMessage = (ItemMessage m) =>
            {
                if(m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnKeyPressed = (KeyEventArgs e) =>
            {
                if(!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }

                if(!e.Handled)
                {
                    Grid.ProcessKeyboard(e);
                }
            };

            OnLoad = () =>
            {
                FormInit();
                SetDefaults();
                GridInit();
            };

            OnUnload = () =>
            {
                Grid.Destruct();
            };

            OnFocusGot = () =>
            {
                Grid.ItemsAutoUpdate = true;
                Grid.Run();
            };

            OnFocusLost = () =>
            {
                Grid.ItemsAutoUpdate = false;
            };


            {
                Commander.SetCurrentGridName("Grid");
                Commander.Add(new CommandItem()
                {
                    Name = "refresh",
                    Group = "grid_base",
                    Enabled = true,
                    Title = "Обновить",
                    Description = "Обновить данные",
                    ButtonUse = true,
                    ButtonName = "RefreshButton",
                    MenuUse = true,
                    Action = () =>
                    {
                        Grid.LoadItems();
                    },
                });
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
                Commander.Add(new CommandItem()
                {
                    Name = "log",
                    Enabled = true,
                    Title = "Журнал",
                    Description = "Показать журнал",
                    ButtonUse = true,
                    ButtonName = "LogButton",
                    Action = () =>
                    {
                        var logViewer=new ReportViewer();
                        logViewer.Content=Log;
                        logViewer.Init();
                        logViewer.Show();
                    },
                });
                /*
                Commander.Add(new CommandItem()
                {
                    Name = "snippets",
                    Enabled = true,
                    ButtonUse = true,
                    ButtonName = "SearchSnippetsButton",
                    Action = () =>
                    {
                        var logViewer=new ReportViewer();
                        logViewer.Content=Log;
                        logViewer.Init();
                        logViewer.Show();
                    },
                });
                */               
                Commander.Add(new CommandItem()
                {
                    Name = "get_info",
                    Title = "Информация",
                    ButtonUse = true,
                    MenuUse = true,
                    Action = () =>
                    {
                        var s=Grid.SelectedItem.GetDumpString();
                        var reportViewer = new ReportViewer();
                        reportViewer.Content = s;
                        reportViewer.Init();
                        reportViewer.Show();
                    },
                    CheckEnabled = () =>
                    {
                        var result = true;
                        return result;
                    },
                    AccessLevel = Role.AccessMode.FullAccess,
                });
                
                Commander.Add(new CommandItem()
                {
                    Name = "session_delete",
                    Title = "Удалить сессию",
                    ButtonUse = true,
                    MenuUse = true,
                    Action = () =>
                    {
                        SessionDelete();
                    },
                    CheckEnabled = () =>
                    {
                        var result = true;
                        return result;
                    },
                    AccessLevel = Role.AccessMode.FullAccess,
                });

                Commander.Add(new CommandItem()
                {
                    Name = "send_command",
                    Title = "Команда",
                    Description = "Отправить команду",
                    ButtonUse = true,
                    ButtonName = "CommandButton",
                    HotKey= "Return|DoubleCLick",
                    MenuUse = true,
                    Action = () =>
                    {
                        var id = Grid.SelectedItem.CheckGet("HOST_USER_ID");
                        var h = new SendCommandForm();
                        h.CurrentReceiverType = SendCommandForm.ReceiverType.Single;
                        h.Scope = "user";
                        h.PrimaryKeyValue = id;
                        h.TitleCustom=id;
                        h.Init();
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if(
                            !row.CheckGet("HOST_USER_ID").IsNullOrEmpty()
                        )
                        {
                            result = true;
                        }
                        return result;
                    },
                    AccessLevel = Role.AccessMode.FullAccess,
                });

                Commander.Add(new CommandItem()
                {
                    Name = "send_variables",
                    Title = "Переменные",
                    Description = "Установить переменные",
                    MenuUse = true,
                    HotKey= "V",
                    Action = () =>
                    {
                        var h = new SetVariableForm();
                        h.Row=Grid.SelectedItem;
                        h.Init();
                    },
                    CheckEnabled = () =>
                    {
                        var result = true;
                        var row = Grid.SelectedItem;
                        if(
                            !row.CheckGet("APPLICATION_NAME").IsNullOrEmpty()
                            && !row.CheckGet("LOGIN").IsNullOrEmpty()
                        )
                        {
                            result = true;
                        }
                        return result;
                    },
                    AccessLevel = Role.AccessMode.FullAccess,
                });

                Commander.Add(new CommandItem()
                {
                    Name = "send_command_all",
                    Title = "Команда всем",
                    Description = "Отправить команду всем активным пользователям",
                    ButtonUse = true,
                    ButtonName = "CommandButtonAll",
                    MenuUse = false,
                    Enabled = true,
                    Action = () =>
                    {
                        var h = new SendCommandForm();
                        h.CurrentReceiverType = SendCommandForm.ReceiverType.All;
                        h.Scope = "user";
                        h.PrimaryKeyValue = "1";
                        h.OnlineTimeout = this.OnlineTimeout;
                        h.Init();
                    },
                    AccessLevel = Role.AccessMode.FullAccess,
                });
                Commander.Add(new CommandItem()
                {
                    Name = "open_radmin",
                    Title = "Radmin",
                    Description = "Запустить Radmin",
                    MenuUse = true,
                    HotKey= "R",
                    Action = () =>
                    {
                        var ip = Grid.SelectedItem.CheckGet("SESSION_IP");
                        if(!ip.IsNullOrEmpty())
                        {
                            var cmd = $"C:/Program Files (x86)/Radmin Viewer 3/Radmin.exe";
                            try
                            {
                                var startInfo = new ProcessStartInfo
                                {
                                    Arguments = $"/connect:{ip}",
                                    FileName = cmd
                                };

                                Process.Start(startInfo);
                            }
                            catch(Exception)
                            {

                            }
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if(
                            !row.CheckGet("SESSION_IP").IsNullOrEmpty()
                        )
                        {
                            result = true;
                        }
                        return result;
                    },
                    AccessLevel = Role.AccessMode.FullAccess,
                });
                
                Commander.Add(new CommandItem()
                {
                    Name = "export_excel",
                    Enabled = true,
                    Title = "Экспорт в Excel",
                    MenuUse = true,
                    Action = () =>
                    {
                        Grid.ItemsExportExcel();
                    },
                });
                Commander.Init(this);
            }

            OnlineTimeout = 60;
        }

        private string Log{get;set;}="";

        Dictionary<string, string> SelectedItem { get; set; }

        public FormHelper Form { get; set; }

        public int OnlineTimeout { get; set; }

        public void GridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Логин",
                    Path="USER_LOGIN",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if(
                                    row.CheckGet("CLIENT_MODE_AUTORESTART").ToBool()
                                )
                                {
                                    color = HColor.Violet;
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        },
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Онлайн",
                    Path="ON_LINE",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Версия",
                    Path="CLIENT_VERSION",
                    ColumnType=ColumnTypeRef.String,
                    Width2=9,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                var v=row.CheckGet("VERSION");
                                if(!v.IsNullOrEmpty())
                                {
                                    var vs=v.Split('.').ToList();
                                    if(vs[3] != null)
                                    {
                                        var v4=vs[3].ToInt();
                                        if(v4 >= 625 && v4 <=632)
                                        {
                                            color = HColor.Red;
                                        }

                                        if(v4 < 660)
                                        {
                                            color = HColor.Red;
                                        }

                                        if(v4 < 741)
                                        {
                                            color = HColor.Red;
                                        }
                                    }
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        },
                    },
                },
                 new DataGridHelperColumn
                {
                    Header="Приложение",
                    Path="APPLICATION_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="HOST_USER_ID",
                    Path="HOST_USER_ID",
                    ColumnType=ColumnTypeRef.String,
                    Width2=24,
                },
                new DataGridHelperColumn
                {
                    Header="VAR_FACTORY_ID",
                    Path="VAR_FACTORY_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="VAR_SEGMENT",
                    Path="VAR_SEGMENT",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn
                {
                    Header="VAR_DESCRIPTION",
                    Path="VAR_DESCRIPTION",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn
                {
                    Header="IP",
                    Path="SESSION_IP",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Создана",
                    Path="ON_CREATE",
                    ColumnType=ColumnTypeRef.DateTime,
                    Format="dd.MM.yyyy HH:mm:ss",
                    Width2=14,
                },
                new DataGridHelperColumn
                {
                    Header="Сек. назад создана",
                    Path="ACT_CREATE",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },

                new DataGridHelperColumn
                {
                    Header="Обновлена",
                    Path="ON_UPDATE",
                    ColumnType=ColumnTypeRef.DateTime,
                    Format="dd.MM.yyyy HH:mm:ss",
                    Width2=14,
                },
                new DataGridHelperColumn
                {
                    Header="Сек. назад обновлена",
                    Path="ACT_UPDATE",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Инстанс",
                    Path="SERVER",
                    Doc="инстанс, обработавший последний запрос",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if(
                                    row.CheckGet("SERVER")=="hercules6"
                                    || row.CheckGet("SERVER")=="hercules8"
                                )
                                {
                                    color = HColor.Green;
                                }

                                if(
                                    row.CheckGet("SERVER")=="hercules7"
                                    || row.CheckGet("SERVER")=="hercules9"
                                )
                                {
                                    color = HColor.Violet;
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        },
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Сервер",
                    Path="CLIENT_SERVER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Кластер",
                    Path="CLIENT_SERVER_CLUSTER",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                /*
                new DataGridHelperColumn
                {
                    Header="Авторизация",
                    Path="SERVER_LOGIN",
                    Doc="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                */
                new DataGridHelperColumn
                {
                    Header="EMPL",
                    Path="USER_EMPLOYEE_ID",
                    Description="EMPLOYEE_ID",
                    Doc="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="ACCO",
                    Path="USER_ACCOUNT_ID",
                    Description="ACCOUNT_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="SDI",
                    Path="CLIENT_MODE_EMBDED",
                    Description="Single Document Interface",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="AR",
                    Path="CLIENT_MODE_AUTORESTART",
                    Description="Авторестарт",
                    Doc="",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="AL",
                    Path="CLIENT_MODE_AUTOLOGIN",
                    Description="Автологин",
                    Doc="",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="AUTH_METHOD",
                    Path="METHOD",
                    Doc="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="DBC_MODE",
                    Path="USER_DB_CONNECTION_MODE",
                    Doc="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=4,
                },                
                new DataGridHelperColumn
                {
                    Header="TOKEN",
                    Path="TOKEN",
                    Doc="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=32,
                },
                /*
                new DataGridHelperColumn
                {
                    Header="SID",
                    Path="SID",
                    Doc="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=32,
                },
                */
            };

            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("TOKEN");
            Grid.SetSorting("USER_LOGIN", ListSortDirection.Ascending);
            Grid.SearchText = Search;
            Grid.Toolbar = GridToolbar;
            Grid.AutoUpdateInterval = 0;
            Grid.UseProgressSplashAuto = true;
            Grid.UseProgressBar = true;
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.OnLoadItems = LoadItems;
            Grid.OnFilterItems = FilterItems;
            Grid.Commands = Commander;
            Grid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                {
                    {
                        DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            //if (!row.CheckGet("ON_LINE").ToBool())
                            //{
                            //    color = HColor.Gray;
                            //}

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },
                };
            Grid.Init();
        }

        public void FormInit()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="SEARCH",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Search,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
                new FormHelperField()
                {
                    Path="SHOW_ONLINE",
                    Default="1",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ShowOnline,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
                new FormHelperField()
                {
                    Path="VERSION",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Version,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
                new FormHelperField()
                {
                    Path="APPLICATION_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ApplicationName,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },   
                new FormHelperField()
                {
                    Path="FACTORY_ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=FactoryId,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },  
            };

            {
                var list = new Dictionary<string, string>();
                list.CheckAdd("*","Все");
                list.CheckAdd("l-pack_erp","Клиент");
                list.CheckAdd("l-pack_erp_agent","Агент");
                list.CheckAdd("l-pack_erp_mobile","Планшет погрузчика");
                ApplicationName.SetItems(list);
            }

            {
                var list = new Dictionary<string, string>();
                list.CheckAdd("0","Все");
                list.CheckAdd("1","Липецк");
                list.CheckAdd("2","Кашира");
                FactoryId.SetItems(list);
            }

            Form.SetFields(fields);
            Form.AfterSet = (Dictionary<string, string> v) =>
            {
                RefreshButton.Focus();
            };
        }

        public void SetDefaults()
        {
            Form.SetDefaults();
        }

        public async void LoadItems()
        {
            var today = DateTime.Now;
            bool resume = true;

            if(resume)
            {
                var p = new Dictionary<string, string>();
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Service");
                q.Request.SetParam("Object", "Client");
                q.Request.SetParam("Action", "List");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if(q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if(result != null)
                    {
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            if(ds.Items.Count > 0)
                            {
                                foreach(Dictionary<string, string> row in ds.Items)
                                {
                                    row.CheckAdd("ON_LINE", "0");
                                    row.CheckAdd("ACT_UPDATE", "0");
                                    {
                                        var s = row.CheckGet("ON_UPDATE");
                                        if(!s.IsNullOrEmpty())
                                        {
                                            var dt=Tools.TimeOffsetSeconds(s);
                                            var onLine = "0";
                                            if(dt <= OnlineTimeout)
                                            {
                                                onLine = "1";
                                            }
                                            row.CheckAdd("ON_LINE", onLine);
                                            row.CheckAdd("ACT_UPDATE", dt.ToString());
                                        }
                                    }

                                    row.CheckAdd("ACT_CREATE", "0");
                                    {
                                        var s = row.CheckGet("ON_CREATE");
                                        if(!s.IsNullOrEmpty())
                                        {
                                            var dt=Tools.TimeOffsetSeconds(s);
                                            row.CheckAdd("ACT_CREATE", dt.ToString());
                                        }
                                    }
                                }
                            }
                            Grid.UpdateItems(ds);
                        }

                        var rd=q;
                        { 
                            var report=ListDataSet.Create(result, "REPORT");
                            var s=report.GetFirstItemValueByKey("LOG");
                            Log="";
                            Log=Log.Append($"{rd.Module}>{rd.Object}>{rd.Action}",true);
                            Log=Log.Append($"{s}",true);
                        }
                    }
                }
            }
        }

        public void FilterItems()
        {
            if(Grid.Items.Count > 0)
            {
                {
                    var v = Form.GetValues();
                    var items = new List<Dictionary<string, string>>();
                    foreach(Dictionary<string, string> row in Grid.Items)
                    {
                        var includeRowByShowOnline = false;
                        var includeRowByApplicationName = false;
                        var includeRowByFactoryId = false;
                        var includeRowByVersion = false;

                        if(v.CheckGet("SHOW_ONLINE").ToBool())
                        {
                            if(row.CheckGet("ON_LINE").ToInt() == 1)
                            {
                                includeRowByShowOnline = true;
                            }
                        }
                        else
                        {
                            includeRowByShowOnline = true;
                        }

                        if(
                            !v.CheckGet("APPLICATION_NAME").IsNullOrEmpty()
                            && v.CheckGet("APPLICATION_NAME")!="*"
                        )
                        {
                            if(row.CheckGet("APPLICATION_NAME")==v.CheckGet("APPLICATION_NAME"))
                            {
                                includeRowByApplicationName=true;
                            }
                        }
                        else
                        {
                            includeRowByApplicationName=true;
                        }

                        if(v.CheckGet("FACTORY_ID").ToInt()>0)
                        {
                            if(!row.CheckGet("VAR_FACTORY_ID").IsNullOrEmpty())
                            {
                                if(row.CheckGet("VAR_FACTORY_ID")==v.CheckGet("FACTORY_ID"))
                                {
                                    includeRowByFactoryId=true;
                                }
                            }
                            else
                            {
                                //includeRowByFactoryId=true;
                            }                            
                        }
                        else
                        {
                            includeRowByFactoryId=true;
                        }

                        if(!v.CheckGet("VERSION").IsNullOrEmpty())
                        {
                            if(!row.CheckGet("CLIENT_VERSION").IsNullOrEmpty())
                            {
                                var xv=Tools.GetVersionInteger(v.CheckGet("VERSION"));
                                var x=Tools.GetVersionInteger(row.CheckGet("CLIENT_VERSION"));
                                if(x<xv)
                                {
                                    includeRowByVersion = true;
                                }
                            }
                            else
                            {
                                includeRowByVersion=true;
                            }
                        }
                        else
                        {
                            includeRowByVersion=true;
                        }

                        if(
                            includeRowByShowOnline
                            && includeRowByApplicationName
                            && includeRowByFactoryId
                            && includeRowByVersion
                        )
                        {
                            items.Add(row);
                        }
                    }
                    Grid.Items = items;
                }
            }
        }

        public void ClientInfoShow(string hostUserId)
        {
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("HOST_USER_ID", hostUserId);
                }
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Service");
                q.Request.SetParam("Object", "Client");
                q.Request.SetParam("Action", "GetInfo");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                if(q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if(result != null)
                    {
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            if(ds.Items.Count > 0)
                            {
                                var row=ds.Items.First();     
                                var s=row.GetDumpString();  

                                var logViewer=new ReportViewer();
                                logViewer.Content=s;
                                logViewer.Init();
                                logViewer.Show();

                            }
                        }

                        var rd=q;
                        { 
                            var report=ListDataSet.Create(result, "REPORT");
                            var s=report.GetFirstItemValueByKey("LOG");
                            Log="";
                            Log=Log.Append($"{rd.Module}>{rd.Object}>{rd.Action}",true);
                            Log=Log.Append($"{s}",true);
                        }
                    }
                }
            }
        }

        
        public void SessionDelete()
        {
            var token=Grid.SelectedItem.CheckGet("TOKEN");
            if(!token.IsNullOrEmpty())
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("TOKEN", token);
                }
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Service");
                q.Request.SetParam("Object", "Client");
                q.Request.SetParam("Action", "Delete");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                if(q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if(result != null)
                    {
                        Grid.LoadItems();

                        var rd=q;
                        { 
                            var report=ListDataSet.Create(result, "REPORT");
                            var s=report.GetFirstItemValueByKey("LOG");
                            Log="";
                            Log=Log.Append($"{rd.Module}>{rd.Object}>{rd.Action}",true);
                            Log=Log.Append($"{s}",true);
                        }
                    }
                }
            }
        }

        private void ShowAll_Click(object sender, RoutedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void OnChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void Version_TextChanged(object sender, TextChangedEventArgs e)
        {
            Grid.UpdateItems();
        }
    }
}
