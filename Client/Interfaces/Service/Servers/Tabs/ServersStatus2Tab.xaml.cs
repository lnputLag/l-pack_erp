using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Service.Servers
{
    /// <summary>
    /// серверы
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2024-11-11</released>
    /// <changed>2024-11-13</changed>
    public partial class ServersStatus2Tab : ControlBase
    {
        public ServersStatus2Tab()
        {           

            InitializeComponent();

            ControlSection = "server_status";
            RoleName = "[erp]server";
            ControlTitle ="Инстансы";
            DocumentationUrl = "/doc/l-pack-erp-new/administration/servers/instance";

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
            };

            OnLoad =()=>
            {
                FormInit();
                SetDefaults();
                ServerGridInit();
            };

            OnUnload=()=>
            {
                ServerGrid.Destruct();
            };

            OnFocusGot=()=>
            {
            };

            OnFocusLost=()=>
            {
            };

            OnNavigate = () =>
            {
            };

            {
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
                }

                Commander.SetCurrentGridName("ServerGrid");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "server_refresh",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить данные",
                        ButtonUse = true,
                        ButtonName = "ServerGridRefreshButton",
                        MenuUse = true,
                        Action = () =>
                        {
                            ServerGrid.LoadItems();
                        },
                    });
                    
                    Commander.Add(new CommandItem()
                    {
                        Name = "server_technical_work",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Тех. работы",
                        ButtonUse = true,
                        ButtonControl = ServerTechnicalWorkButton,
                        Action = () =>
                        {
                            var h = new SendTechnicalWorkStatusForm();
                            h.Init();
                        }
                    });

                    Commander.SetCurrentGroup("item");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "server_send_command",
                            Title = "Команда",
                            Description = "Отправить команду",
                            ButtonUse = true,
                            ButtonName = "ServerCommandButton",
                            HotKey = "Return|DoubleCLick",
                            MenuUse = true,
                            Action = () =>
                            {
                                var id = ServerGrid.SelectedItem.CheckGet("INSTANCE");
                                var h = new SendCommandForm();
                                h.PrimaryKeyValue = id;
                                h.TitleCustom=id;
                                h.Init();
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
                            Name = "server_status",
                            Title = "Статус",
                            Description = "Открыть страницу статуса",
                            ButtonUse = true,
                            ButtonName = "ServerStatusButton",
                            MenuUse = true,
                            Action = () =>
                            {
                                var ip = ServerGrid.SelectedItem.CheckGet("HOST_IP").ToString();
                                var port = ServerGrid.SelectedItem.CheckGet("SERVICE_PORT").ToInt();
                                var url = $"http://{ip}:{port-1000}/";
                                Central.ShowHelp(url,false,true);
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var ip = ServerGrid.SelectedItem.CheckGet("HOST_IP").ToString();
                                var port = ServerGrid.SelectedItem.CheckGet("SERVICE_PORT").ToInt();
                                if(!ip.IsNullOrEmpty() && port!=0)
                                {
                                    result = true;
                                }
                                return result;
                            },
                            AccessLevel = Role.AccessMode.FullAccess,
                        });
                    }
                }

                Commander.Init(this);
            }

            DataLoadTimeout = 1000;
            ActualInterval = 30;
            LimitLA = 15;
            LimitIow = 20;
            LimitDf = 2;
            LimitIf = 0.2;


            if(Central.DebugMode)
            {
                DebugPanel.Visibility = Visibility.Visible;
            }
            else
            {
                DebugPanel.Visibility = Visibility.Collapsed;               
            }
        }

        private string Log{get;set;}="";

        private int DataLoadTimeout { get; set; }
        private int ActualInterval {  get; set; }
        private int LimitLA { get; set; }
        private int LimitIow { get; set; }
        private int LimitDf { get; set; }
        private double LimitIf { get; set; }

        private FormHelper Form { get; set; }

        public void FormInit()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
            };
            Form.SetFields(fields);
        }

        public void SetDefaults()
        {
            Form.SetDefaults();
        }

        public void ServerGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="ORDER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Инстанс",
                    Path="INSTANCE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Хост",
                    Path="HOST_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="IP",
                    Path="HOST_IP",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Порт",
                    Path="SERVICE_PORT",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Тег",
                    Path="INSTANCE_TAG",
                    ColumnType=ColumnTypeRef.String,
                    Width2=6,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if(row.CheckGet("INSTANCE_TAG")=="master")
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
                    Header="WD",
                    Path="WATCHDOG_ENABLED",
                    Description="Watchdog активирован",
                    ColumnType=ColumnTypeRef.Boolean,
                },
                new DataGridHelperColumn
                {
                    Header="Описание",
                    Path="DESCRIPTION",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Версия",
                    Path="SERVER_VERSION",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if(
                                    row.CheckGet("REQUEST_COUNTER").ToInt()>0
                                    && row.CheckGet("REQUEST_COUNTER").ToInt() < 100
                                )
                                {
                                    color = HColor.Blue;
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
                    Header="Режим работы",
                    Path="SYSTEM_MODE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="GIT",
                    Path="GIT_WORKING_BRANCH",
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

                                if(color.IsNullOrEmpty())
                                {
                                    var s=row.CheckGet("GIT_WORKING_BRANCH").Trim();
                                    if(!s.IsNullOrEmpty() && s!="master")
                                    {
                                        color = HColor.Yellow;
                                    }
                                }

                                if(!color.IsNullOrEmpty())
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
                    Header="БД",
                    Path="DB_TAG",
                    ColumnType=ColumnTypeRef.String,
                    Width2=6,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if(color.IsNullOrEmpty())
                                {
                                    var s=row.CheckGet("DB_TAG").Trim();
                                    if(!s.IsNullOrEmpty() && s!="production")
                                    {
                                        color = HColor.Yellow;
                                    }
                                }

                                if(!color.IsNullOrEmpty())
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
                    Header="Треды",
                    Path="THREADS_ALIVE",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="Занятые треды",
                    Path="THREADS_PROCESSING",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=5,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if(color.IsNullOrEmpty())
                                {
                                    if(row.CheckGet("THREADS_PROCESSING").ToInt()>50)
                                    {
                                        color = HColor.Red;
                                    }
                                }

                                if(!color.IsNullOrEmpty())
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
                    Header="USR",
                    Path="SERVER_USERS_ONLINE",
                    Description="Число активных пользователей",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="REQ",
                    Path="REQUEST_COUNTER",
                    Description="Общее число обработанных запросов",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                   
                },
                new DataGridHelperColumn
                {
                    Header="RPM",
                    Path="REQUEST_PER_MINUTE",
                    Description="Среднее число запросов в минуту",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                //new DataGridHelperColumn
                //{
                //    Header="Aктуальность",
                //    Path="_ACTUAL",
                //    ColumnType=ColumnTypeRef.String,
                //    Width2=5,                  
                //    FormatterRaw= (row) =>
                //    {
                //        var result = "";
                //        var dt=(int)Tools.TimeOffsetSeconds(row.CheckGet("ON_DATE"));
                //        result=dt.ToString();
                //        return result;
                //    },
                //},
                new DataGridHelperColumn
                {
                    Header="STATUS",
                    Path="STATUS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="PID",
                    Path="PID",
                    Description="Process ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=5,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if(color.IsNullOrEmpty())
                                {
                                    if(row.CheckGet("PID").ToInt() == 0)
                                    {
                                        color = HColor.Red;
                                    }
                                }

                                if(!color.IsNullOrEmpty())
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
                    Header="LA1",
                    Path="LA1",
                    Description="Load Average",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=5,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if(row.CheckGet("LA1").ToInt() > LimitLA)
                                {
                                    color = HColor.Red;
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
                    Header="DF",
                    Path="DFH",
                    Description="Disk Free",
                    ColumnType=ColumnTypeRef.Double,
                    Width2=5,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if(row.CheckGet("INSTANCE_TAG").ToString()=="master")
                                {
                                    if(row.CheckGet("DFH").ToInt() < LimitDf)
                                    {
                                        color = HColor.Red;
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
                    Header="HOUSTON",
                    Path="HOUSTON_VERSION_NUMBER",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="CNT",
                    Path="_COUNTER",
                    Description="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="RND",
                    Path="RND",
                    ColumnType=ColumnTypeRef.String,
                    Width2=6,
                },
            };
            ServerGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                {
                    StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=System.Windows.DependencyProperty.UnsetValue;
                        var color = "";

                        {
                            var dt=(int)Tools.TimeOffsetSeconds(row.CheckGet("ON_DATE"));
                            if(dt > ActualInterval)
                            {
                                color = HColor.Gray;
                            }
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
            };
            ServerGrid.SetColumns(columns);
            ServerGrid.SetPrimaryKey("INSTANCE");
            ServerGrid.SetSorting("ORDER", ListSortDirection.Ascending);
            ServerGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            ServerGrid.SearchText = ServerGridSearch;
            ServerGrid.Toolbar = ServerGridToolbar;
            ServerGrid.AutoUpdateInterval = 10;
            ServerGrid.ItemsAutoUpdate=true;
            ServerGrid.QueryLoadItems = new RequestData()
            {
                Module = "Service",
                Object = "Server",
                Action = "List",
                AnswerSectionKey = "ITEMS",    
                Timeout=10000,
                AfterRequest = (RequestData rd, ListDataSet ds) =>
                {
                    var report=ListDataSet.Create(rd.AnswerData, "REPORT");
                    var s=report.GetFirstItemValueByKey("LOG");
                    Log="";
                    Log=Log.Append($"{rd.Module}>{rd.Object}>{rd.Action}",true);
                    Log=Log.Append($"{s}",true);
                    return ds;
                },
            };
      
            ServerGrid.OnSelectItem = (item) => {
            };
            ServerGrid.Commands = Commander;
            ServerGrid.Init();            
        }
    }
}
