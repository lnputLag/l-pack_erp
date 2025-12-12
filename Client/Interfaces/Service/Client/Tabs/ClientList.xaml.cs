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
    public partial class ClientList : ControlBase
    {
        public ClientList()
        {
            InitializeComponent();


            RoleName = "[erp]client";
            ControlTitle = "Клиенты";
            DocumentationUrl = "/doc/l-pack-erp/service/agent/clients";

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
                GridInit();
                FormInit();
                SetDefaults();
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
                            && row.CheckGet("ON_LINE").ToBool()
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
                    Name = "send_command_group",
                    Title = "Команда по группе",
                    Description = "Отправить команду всем активным пользователям (В группе)",
                    ButtonUse = true,
                    ButtonName = "CommandButtonGroup",
                    MenuUse = false,
                    Enabled = true,
                    Action = () =>
                    {
                        var h = new SendCommandForm();
                        h.CurrentReceiverType = SendCommandForm.ReceiverType.ByGroup;
                        h.Scope = "user";
                        h.OnlineTimeout = this.OnlineTimeout;
                        h.Init();
                    },
                    AccessLevel = Role.AccessMode.FullAccess,
                });
                Commander.Add(new CommandItem()
                {
                    Name = "send_command_role",
                    Title = "Команда по роли",
                    Description = "Отправить команду всем активным пользователям (По роли)",
                    ButtonUse = true,
                    ButtonName = "CommandButtonRole",
                    MenuUse = false,
                    Enabled = true,
                    Action = () =>
                    {
                        var h = new SendCommandForm();
                        h.CurrentReceiverType = SendCommandForm.ReceiverType.ByRole;
                        h.Scope = "user";
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
                    Action = () =>
                    {
                        var ip = Grid.SelectedItem.CheckGet("SYSTEM_NETWORK_IP");
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
                            !row.CheckGet("SYSTEM_NETWORK_IP").IsNullOrEmpty()
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

            OnlineTimeout = 120;
        }

        /// <summary>
        /// данные из выбранной в гриде строки
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// форма с полями для фильтрации данных
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// интервал неактивности клиента
        /// </summary>
        public int OnlineTimeout { get; set; }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void GridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Логин",
                    Path="CLIENT_LOGIN",
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
                                    //|| row.CheckGet("CLIENT_MODE_AUTOLOGIN").ToBool()
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
                    Path="VERSION",
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
                    Header="ID",
                    Path="HOST_USER_ID",
                    ColumnType=ColumnTypeRef.String,
                    Width2=32,
                },
                new DataGridHelperColumn
                {
                    Header="IP",
                    Path="SYSTEM_NETWORK_IP",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Активность",
                    Path="ON_DATE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=14,
                },
                new DataGridHelperColumn
                {
                    Header="Сервер",
                    Path="SERVER",
                    Doc="",
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
                    Header="Серверы",
                    Path="SERVER_STRING",
                    Doc="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Группа",
                    Path="_SERVER_GROUP",
                    Doc="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                    FormatterRaw= (row) =>
                    {
                        var result = "";

                        if(
                            row.CheckGet("SERVER")=="hercules6"
                            || row.CheckGet("SERVER")=="hercules8"
                        )
                        {
                            result="OFFICE";
                        }

                        if(
                            row.CheckGet("SERVER")=="hercules7"
                            || row.CheckGet("SERVER")=="hercules9"
                        )
                        {
                            result="PRODUCTION";
                        }

                        return result;
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Аптайм, с.",
                    Path="UPTIME",
                    Doc="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="EMPL",
                    Path="EMPLOYEE_ID",
                    Description="EMPLOYEE_ID",
                    Doc="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="RAM",
                    Path="SYSTEM_MEMORY_USED",
                    Description="Использовано RAM, Mb",
                    Doc="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Memory",
                    Path="USED_RAM",
                    Description="Available MBytes",
                    Doc="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Processor",
                    Path="USED_CPU",
                    Description="% Processor Time",
                    Doc="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="ACCO",
                    Path="ACCOUNT_ID",
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
                    Header="SID",
                    Path="SID",
                    Doc="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=32,
                },
                new DataGridHelperColumn
                {
                    Header="AUTH_METHOD",
                    Path="AUTH_METHOD",
                    Doc="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="DBC_MODE",
                    Path="DBC_MODE",
                    Doc="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },                
                new DataGridHelperColumn
                {
                    Header="TOKEN",
                    Path="SESSION_USER_TOKEN",
                    Doc="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=32,
                },
            };

            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("HOST_USER_ID");
            Grid.SetSorting("HOST_USER_ID", ListSortDirection.Ascending);
            Grid.SearchText = Search;
            Grid.Toolbar = GridToolbar;
            Grid.AutoUpdateInterval = 60;
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

                            if (!row.CheckGet("ON_LINE").ToBool())
                            {
                                color = HColor.Gray;
                            }

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
                };

            Form.SetFields(fields);
            Form.AfterSet = (Dictionary<string, string> v) =>
            {
                RefreshButton.Focus();
            };
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();
        }

        /// <summary>
        /// получение записей
        /// </summary>
        public async void LoadItems()
        {
            var today = DateTime.Now;
            bool resume = true;

            if(resume)
            {
                var p = new Dictionary<string, string>();
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Service");
                q.Request.SetParam("Object", "Control");
                q.Request.SetParam("Action", "ListClient");
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
                                    {
                                        var s = row.CheckGet("ON_DATE");
                                        if(!s.IsNullOrEmpty())
                                        {
                                            var onDate = s.ToDateTime();
                                            var timeDiff = (TimeSpan)(today - onDate);
                                            var sec = timeDiff.TotalSeconds;
                                            var onLine = "0";
                                            if(sec <= OnlineTimeout)
                                            {
                                                onLine = "1";
                                            }
                                            row.CheckAdd("ON_LINE", onLine);
                                        }
                                    }

                                    row.CheckAdd("UPTIME_HUMAN", "");
                                    {
                                        var s = row.CheckGet("UPTIME").ToDouble();
                                        if(s > 0)
                                        {
                                            TimeSpan t = TimeSpan.FromSeconds(s);
                                            var h = string.Format(
                                                "{0:D2} {0:D2}:{1:D2}:{2:D2}",
                                                t.Days,
                                                t.Hours,
                                                t.Minutes,
                                                t.Seconds
                                            );
                                            row.CheckAdd("UPTIME_HUMAN", h);
                                        }
                                    }
                                }
                            }
                            Grid.UpdateItems(ds);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// фильтрация записей (аккаунты)
        /// </summary>
        public void FilterItems()
        {
        }

    }
}
