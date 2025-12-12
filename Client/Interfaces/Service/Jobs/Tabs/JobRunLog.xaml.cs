using Client.Assets.Converters;
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

namespace Client.Interfaces.Service.Jobs
{
    /// <summary>
    /// сессии пользователей
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2024-11-11</released>
    /// <changed>2022-11-27</changed>
    public partial class JobRunLog : ControlBase
    {
        public JobRunLog()
        {
            InitializeComponent();

            ControlSection = "job_status";
            RoleName = "[erp]job";
            ControlTitle ="Журнал запуска";
            DocumentationUrl = "/doc/l-pack-erp/service/job/run_log";

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
                JobGridInit();
                JobStatusGridInit();
                JobLogGridInit();
            };

            OnUnload=()=>
            {
                JobGrid.Destruct();
                JobStatusGrid.Destruct();
                JobLogGrid.Destruct();
            };

            OnFocusGot=()=>
            {
                JobGrid.Run();
                JobStatusGrid.Run();
                JobLogGrid.Run();
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
                }

                Commander.SetCurrentGridName("JobGrid");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "job_refresh",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить данные",
                        ButtonUse = true,
                        ButtonName = "JobGridRefreshButton",
                        MenuUse = true,
                        Action = () =>
                        {
                            JobGrid.LoadItems();
                        },
                    });

                    Commander.SetCurrentGroup("item");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "jobgrid_item_dump",
                            Enabled = true,
                            MenuUse = true,
                            Title = "Информация",
                            Action = () =>
                            {
                                var logViewer=new ReportViewer();
                                logViewer.Content=JobGrid.SelectedItem.GetDumpString();
                                logViewer.Init();
                                logViewer.Show();
                            },
                            CheckEnabled = () =>
                            {
                                var result = true;
                                return result;
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "jobgrid_item_config",
                            Group = "item",
                            Enabled = true,
                            Title = "Конфигурация",
                            Description = "Изменить конфигурационный файл",
                            ButtonUse = true,
                            ButtonName = "JobGridConfigButton",
                            MenuUse = true,
                            Action = () =>
                            {
                                var name = JobGrid.SelectedItem.CheckGet("NAME");
                                if(!name.IsNullOrEmpty())
                                {
                                    var d = "\\\\192.168.3.243\\external_services$\\ErpServer\\erp\\server\\config\\jobs\\";
                                    var f = $"{name}.cfg";
                                    var p = $"{d}{f}";
                                    Central.OpenFile(p);
                                }                                
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var row = JobGrid.SelectedItem;
                                if(
                                    !row.CheckGet("NAME").IsNullOrEmpty()
                                )
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "jobgrid_item_doc",
                            Group = "item",
                            Enabled = true,
                            Title = "Документация",
                            MenuUse = true,
                            Action = () =>
                            {
                                var url = JobGrid.SelectedItem.CheckGet("DOC_URL");
                                if(!url.IsNullOrEmpty())
                                {
                                    Central.ShowHelp(url,false,true);
                                }                                
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var row = JobGrid.SelectedItem;
                                if(!row.CheckGet("DOC_URL").IsNullOrEmpty())
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });
                    }
                }

                Commander.SetCurrentGridName("JobStatusGrid");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "job_status_refresh",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить данные",
                        ButtonUse = true,
                        ButtonName = "JobStatusGridRefreshButton",
                        MenuUse = true,
                        Action = () =>
                        {
                            JobStatusGrid.LoadItems();
                        },
                    });

                    Commander.SetCurrentGroup("item");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "jobstatusgrid_item_dump",
                            Enabled = true,
                            MenuUse = true,
                            Title = "Информация",
                            ButtonUse = true,
                            ButtonName = "LogButton",
                            Action = () =>
                            {
                                var logViewer=new ReportViewer();
                                logViewer.Content=JobStatusGrid.SelectedItem.GetDumpString();
                                logViewer.Init();
                                logViewer.Show();
                            },
                            CheckEnabled = () =>
                            {
                                var result = true;
                                return result;
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "jobstatusgrid_item_monitor",
                            Title = "Монитор",
                            ButtonUse = true,
                            MenuUse = true,
                            Action = () =>
                            {
                                var url = JobStatusGrid.SelectedItem.CheckGet("MONITOR_FILE_URL");
                                var ip = JobStatusGrid.SelectedItem.CheckGet("HOST_IP");

                                if(!url.IsNullOrEmpty())
                                {
                                    url=url.Replace("[IP]",ip);
                                    Central.ShowHelp(url,false,true);
                                }
                            },
                            CheckEnabled = () =>
                            {
                                var result = true;
                                return result;
                            },
                            AccessLevel = Role.AccessMode.FullAccess,
                        });
                    }
                }

                Commander.SetCurrentGridName("JobLogGrid");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "job_log_refresh",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить данные",
                        ButtonUse = true,
                        ButtonName = "JobLogGridRefreshButton",
                        MenuUse = true,
                        Action = () =>
                        {
                            JobLogGridRefreshButton.Style = (Style)JobLogGridRefreshButton.TryFindResource("Button");
                            JobLogGrid.LoadItems();
                        },
                    });

                    Commander.Add(new CommandItem()
                    {
                        Name = "job_log_filter1",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Час",
                        Description = "Установить фильтр на последний час",
                        ButtonUse = true,
                        ButtonName = "JobLogGridFilter1Button",
                        MenuUse = true,
                        Action = () =>
                        {
                            SetCurrentHour();
                            JobLogGrid.LoadItems();
                        },
                    });

                    Commander.SetCurrentGroup("item");
                    {
                    }
                }

                Commander.Init(this);
            }
        }

        public int ActualInterval { get; set; } = 60;
        public int RequestTimeout { get; set; } = 60000;
        public FormHelper Form { get; set; }

        public void FormInit()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="DATE_START",
                    FieldType=FormHelperField.FieldTypeRef.DateTime,
                    Control=DateStart,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    Default=DateTime.Now.AddHours(-1).ToString("dd.MM.yyyy HH:mm:ss")
                },
                new FormHelperField()
                {
                    Path="DATE_FINISH",
                    FieldType=FormHelperField.FieldTypeRef.DateTime,
                    Control=DateFinish,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    Default=DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")
                },
            };
            Form.SetFields(fields);
        }

        public void SetDefaults()
        {
            SetCurrentHour();
            Form.SetDefaults();
        }

        public void SetCurrentHour()
        {
            var v = new Dictionary<string, string>()
            {
                { "DATE_START", DateTime.Now.AddHours(-1).ToString("dd.MM.yyyy HH:mm:00") },
                { "DATE_FINISH", DateTime.Now.AddMinutes(1).ToString("dd.MM.yyyy HH:mm:00") },
            };
            Form.SetValues(v);
        }

        public void JobGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Имя",
                    Path="NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=26,
                },
                new DataGridHelperColumn
                {
                    Header="Сервер",
                    Path="SERVER",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="Интервал",
                    Path="INTERVAL",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },                
                new DataGridHelperColumn
                {
                    Header="Включен",
                    Path="ENABLED",
                    ColumnType=ColumnTypeRef.Boolean,
                },
                new DataGridHelperColumn
                {
                    Header="Отладка",
                    Path="DEBUG",
                    ColumnType=ColumnTypeRef.Boolean,
                },
                new DataGridHelperColumn
                {
                    Header="Тип",
                    Path="KIND",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                    FormatterRaw= (row) =>
                    {
                        var result = "";
                        var v=row.CheckGet("KIND").ToInt();
                        switch(v)
                        {
                            default:
                                result="";
                                break;

                            case 0:
                                result="системный";
                                break;

                            case 1:
                                result="прикладной";
                                break;

                            case 2:
                                result="вспомогательный";
                                break;
                        }                        
                        return result;
                    },
                },

                new DataGridHelperColumn
                {
                    Header="Описание",
                    Path="DESCRIPTION",
                    ColumnType=ColumnTypeRef.String,
                    Width2=32,
                },
                new DataGridHelperColumn
                {
                    Header="WD",
                    Path="WATCHDOG_ENABLED",
                    Description="Watchdog, механизм анализа работы",
                    ColumnType=ColumnTypeRef.Boolean,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if(row.CheckGet("WATCHDOG_ENABLED").ToBool())
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
                    Header="Конфигурация Watchdog",
                    Path="_WATCHDOG",
                    ColumnType=ColumnTypeRef.String,
                    Width2=4,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if(row.CheckGet("WATCHDOG_ENABLED").ToBool())
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
                    FormatterRaw= (row) =>
                    {
                        var result = "";
                        if(row.CheckGet("WATCHDOG_ENABLED").ToBool())
                        {
                            var s="";
                            s=s.Append($"интервал=[{row.CheckGet("WATCHDOG_INTERVAL")}]");

                            s=s.Append($" проверки:");

                            if(row.CheckGet("WATCHDOG_FAULT_ENABLED").ToBool())
                            {
                                s=s.Append($" ошибки=[{row.CheckGet("WATCHDOG_FAULT_LIMIT")}]");
                            }

                            if(row.CheckGet("WATCHDOG_SKIP_ENABLED").ToBool())
                            {
                                s=s.Append($" пропуски=[{row.CheckGet("WATCHDOG_SKIP_LIMIT")}]");
                            }

                            if(row.CheckGet("WATCHDOG_WORKTIME_ENABLED").ToBool())
                            {
                                s=s.Append($" время=[{row.CheckGet("WATCHDOG_WORKTIME_LIMIT")}]");
                            }
                            result=s;
                        }
                        return result;
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Aктуальность",
                    Path="_ACTUAL",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                    FormatterRaw= (row) =>
                    {
                        var result = "";
                        var dt=(int)Tools.TimeOffsetSeconds(row.CheckGet("ON_DATE"));
                        result=dt.ToString();
                        return result;
                    },
                },
            };
            JobGrid.SetColumns(columns);
            /*
            JobGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
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
            */
            JobGrid.SetPrimaryKey("NAME");
            JobGrid.SetSorting("NAME", ListSortDirection.Ascending);
            JobGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            JobGrid.SearchText = JobGridSearch;
            JobGrid.Toolbar = JobGridToolbar;
            JobGrid.AutoUpdateInterval = 0;            
            JobGrid.ItemsAutoUpdate=false;
            JobGrid.UseProgressBar = true;
            JobGrid.UseProgressSplashAuto = true;
            JobGrid.QueryLoadItems = new RequestData()
            {
                Module = "Service",
                Object = "LiteBase",
                Action = "List",
                AnswerSectionKey = "server_job",
                BeforeRequest = (RequestData rd) =>
                {
                    rd.Params = new Dictionary<string, string>()
                    {
                        {"TABLE_NAME", "server_job"},
                        {"TABLE_DIRECTORY", ""},
                        // 1=global,2=local,3=net
                        {"STORAGE_TYPE", "3"},                                     
                    };
                }                
            };
            JobGrid.OnSelectItem = (item) => {
                //JobStatusGrid.ClearItems();
                JobLogGridClear();
                JobStatusGrid.LoadItems();
            };
            JobGrid.Commands = Commander;
            JobGrid.Init();

            JobGrid.LoadItems();
        }

        public void JobStatusGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="UID",
                    Path="UID",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="Имя",
                    Path="NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="Инстанс",
                    Path="SERVER",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn
                {
                    Header="Запуск",
                    Path="RUNS",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Успешно",
                    Path="RUNS_COMPLETE",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Ошибки",
                    Path="RUNS_FAILED",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                {
                                    if(row.CheckGet("RUNS_FAILED").ToInt() > 0)
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
                    Header="Результат",
                    Path="RESULT",
                    ColumnType=ColumnTypeRef.Boolean,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                {
                                    var r=row.CheckGet("RESULT").ToBool();
                                    if(r)
                                    {
                                        color = HColor.Green;
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
                    Header="Репорт",
                    Path="REPORT",
                    ColumnType=ColumnTypeRef.String,
                    Width2=32,
                },                
                new DataGridHelperColumn
                {
                    Header="Aктуальность",
                    Path="_ACTUAL",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                    FormatterRaw= (row) =>
                    {
                        var result = "";
                        var dt=(int)Tools.TimeOffsetSeconds(row.CheckGet("ON_DATE"));
                        result=dt.ToString();
                        return result;
                    },
                },
            };
            JobStatusGrid.SetColumns(columns);
            JobStatusGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
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
            JobStatusGrid.SetPrimaryKey("UID");
            JobStatusGrid.SetSorting("SERVER", ListSortDirection.Ascending);
            JobStatusGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            JobStatusGrid.SearchText = JobStatusGridSearch;
            JobStatusGrid.Toolbar = JobStatusGridToolbar;
            JobStatusGrid.AutoUpdateInterval = 0;
            JobStatusGrid.ItemsAutoUpdate=false;
            JobStatusGrid.UseProgressBar = true;
            JobStatusGrid.UseProgressSplashAuto = true;
            JobStatusGrid.ProgressBarInterval = 1000;
            JobStatusGrid.QueryLoadItems = new RequestData()
            {
                Module = "Service",
                Object = "LiteBase",
                Action = "List",
                AnswerSectionKey = "server_job_status",
                BeforeRequest = (RequestData rd) =>
                {
                    rd.Params = new Dictionary<string, string>()
                    {
                        {"TABLE_NAME", "server_job_status,instance_state"},
                        {"TABLE_DIRECTORY", ""},
                        // 1=global,2=local,3=net
                        {"STORAGE_TYPE", "3"},                                      
                    };
                },
                AfterRequest = (RequestData rd, ListDataSet ds) =>
                {
                    var instanceDs=ListDataSet.Create(rd.AnswerData, "instance_state");

                    var jobName = JobGrid.SelectedItem.CheckGet("NAME");
                    if(!jobName.IsNullOrEmpty())
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach(var row in ds.Items)
                        {
                            var include = false;
                            if(row.CheckGet("NAME") == jobName)
                            {
                                include = true;
                            }
                            if(include)
                            {
                                var instanceName=row.CheckGet("SERVER");
                                var hostIp="";
                                foreach(var row2 in instanceDs.Items)
                                {
                                    if(row2.CheckGet("INSTANCE_NAME") == instanceName)
                                    {
                                        hostIp=row2.CheckGet("HOST_IP");
                                    }
                                }

                                row.CheckAdd("HOST_IP",hostIp);
                                items.Add(row);
                            }
                        }
                        ds.Items= items;
                    }
                    return ds;
                }
            };
            JobStatusGrid.OnSelectItem = (item) => {
                //JobLogGrid.ClearItems();
                //JobLogGrid.LoadItems();
                //JobLogGridRefreshButton.Style = (Style)JobLogGridRefreshButton.TryFindResource("FButtonPrimary");
                //SetCurrentHour();
                JobLogGrid.LoadItems();
            };
            JobStatusGrid.Commands = Commander;
            JobStatusGrid.Init();
        }

        public void JobLogGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="JID",
                    Path="JID",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="Имя",
                    Path="NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                    Visible=false,
                },
                new DataGridHelperColumn
                {
                    Header="Сервер",
                    Path="SERVER",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                    Visible=false,
                },
                 new DataGridHelperColumn
                {
                    Header="Дата",
                    Path="ON_DATE",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=14,
                },
                new DataGridHelperColumn
                {
                    Header="Счетчик",
                    Path="RUNS",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                
                new DataGridHelperColumn
                {
                    Header="Результат",
                    Path="RESULT",
                    ColumnType=ColumnTypeRef.Boolean,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                {
                                    var r=row.CheckGet("RESULT").ToBool();
                                    if(r)
                                    {
                                        color = HColor.Green;
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
                    Header="Время",
                    Path="WORK_TIME",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Репорт",
                    Path="REPORT",
                    ColumnType=ColumnTypeRef.String,
                    Width2=70,
                },
            };
            JobLogGrid.SetColumns(columns);
            JobLogGrid.SetPrimaryKey("JID");
            JobLogGrid.SetSorting("ON_DATE", ListSortDirection.Descending);
            JobLogGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            JobLogGrid.SearchText = JobStatusGridSearch;
            JobLogGrid.Toolbar = JobStatusGridToolbar;
            JobLogGrid.AutoUpdateInterval = 0;
            JobLogGrid.ItemsAutoUpdate=false;
            JobLogGrid.UseProgressBar = true;
            JobLogGrid.UseProgressSplashAuto = true;
            JobLogGrid.ProgressBarInterval = 1000;
            JobLogGrid.QueryLoadItems = new RequestData()
            {
                Module = "Service",
                Object = "LiteBase",
                Action = "List",
                Timeout=RequestTimeout,
                AnswerSectionKey = "server_job_run",
                BeforeRequest = (RequestData rd) =>
                {
                    JobLogGrid.ProcessToolbar2(0);
                    var d = "";

                    {
                        var serverName = JobStatusGrid.SelectedItem.CheckGet("SERVER");
                        if(!serverName.IsNullOrEmpty())
                        {
                            d = d.AddEtc("/");
                            d = d.Append($"{serverName}");
                        }
                    }

                    {
                        var jobName = JobGrid.SelectedItem.CheckGet("NAME");
                        if(!jobName.IsNullOrEmpty())
                        {
                            d = d.AddEtc("/");
                            d = d.Append($"{jobName}");
                        }
                    }

                    var v = Form.GetValues();

                    rd.Params = new Dictionary<string, string>()
                    {
                        { "TABLE_NAME", "server_job_run" },
                        { "TABLE_DIRECTORY", d },
                        // 1=global,2=local,3=net
                        { "STORAGE_TYPE", "3" },
                        { "DATE_START", v.CheckGet("DATE_START") },
                        { "DATE_FINISH", v.CheckGet("DATE_FINISH") },
                        { "NET_TIMEOUT", RequestTimeout.ToString() },
                        { "TEST_FLAG", "1" },                      
                    };
                },
                AfterRequest = (RequestData rd, ListDataSet ds) =>
                {
                    JobLogGrid.ProcessToolbar2(1);
                    JobLogDS = ds;
                    JobLogGridUpdateMap();
                    return ds;
                }
            };
            JobLogGrid.Commands = Commander;
            JobLogGrid.Init();
        }
        private ListDataSet JobLogDS { get; set; }

        private void JobLogGridClear()
        {
            JobLogGrid.ClearItems();
            JobLogGridClearMap();
        }

        private void JobLogGridClearMap()
        {
            RunMapContainer.ColumnDefinitions.Clear();
            RunMapContainer.RowDefinitions.Clear();
            RunMapContainer.Children.Clear();

            RowIndex = 0;
            ColIndex = 0;
        }

        private void JobLogGridUpdateMap()
        {
            var v = Form.GetValues();
            var dateStart = v.CheckGet("DATE_START").ToDateTime();
            var dateFinish = v.CheckGet("DATE_FINISH").ToDateTime();

            RowDate = dateStart.ToString("dd.MM.yyyy");
            RowIndexHour = dateStart.ToString("HH").ToInt();
            RowIndexMinute = dateStart.ToString("mm").ToInt();
            RowIndexSecond = 0;
            var n = (int)( ((TimeSpan)(dateFinish - dateStart)).TotalMinutes);
            MapRender(n);
        }

        private void MapRender(int n)
        {
            JobLogGridClearMap();

            {
                var cd = new ColumnDefinition();
                cd.Width = new GridLength(40, GridUnitType.Pixel);
                cd.MinWidth = 20;
                RunMapContainer.ColumnDefinitions.Add(cd);
            }

            {
                var cd = new ColumnDefinition();
                cd.Width = new GridLength(1, GridUnitType.Star);
                cd.MinWidth = 20;
                RunMapContainer.ColumnDefinitions.Add(cd);
            }

            for (int i = 0; i < n; i++)
            {
                MapAddRow();                
            }

            RunMapScroller.ScrollToEnd();
        }

        private int RowIndex { get; set; } = 0;
        private int RowIndexHour { get; set; } = 0;
        private int RowIndexMinute { get; set; } = 0;
        private int RowIndexSecond { get; set; } = 0;
        private string RowDate { get; set; }
        private int ColIndex { get; set; } = 0;
        private void MapAddRow()
        {
            var time = GetTimeLabel(RowIndexHour,RowIndexMinute);

            {
                var rd = new RowDefinition();
                rd.Height = new GridLength(0, GridUnitType.Auto);
                RunMapContainer.RowDefinitions.Add(rd);
            }

            {
                var b = RowHeaderCreate(time);
                RunMapContainer.Children.Add(b);
                Grid.SetRow(b, RowIndex);
                Grid.SetColumn(b, 0);
            }

            {
                var c = new StackPanel();
                c.Orientation=Orientation.Horizontal;

                RowIndexSecond = 0;
                for(var i = 0; i < 60; i++)
                {
                    var time2 = GetTimeLabel(RowIndexHour,RowIndexMinute,RowIndexSecond);
                    RowIndexSecond++;
                    {
                        var b = RowCellCreate(time2);                        
                        c.Children.Add(b);
                    }
                }

                RunMapContainer.Children.Add(c);
                Grid.SetRow(c, RowIndex);
                Grid.SetColumn(c, 1);
            }

            RowIndex++;
            RowIndexMinute++;
            if(RowIndexMinute > 59)
            {
                RowIndexMinute = 0;
                RowIndexHour++;
            }

            if(RowIndexHour > 23)
            {
                RowIndexHour = 0;
            }

        }

        private string GetTimeLabel(int h, int m, int s)
        {
            var result = "";
            var hs = GetMasteredInt(h);
            var ms = GetMasteredInt(m);
            var ss = GetMasteredInt(s);
            result = $"{hs}:{ms}:{ss}";
            return result;
        }

        private string GetTimeLabel(int h, int m)
        {
            var result = "";
            var hs = GetMasteredInt(h);
            var ms = GetMasteredInt(m);
            result = $"{hs}:{ms}";
            return result;
        }

        private string GetMasteredInt(int x)
        {
            var result = "";
            if(x<10)
            {
                result = $"0{x}";
            }
            else
            {
                result = $"{x}";
            }
            return result;
        }

        private Border RowCellCreate(string time)
        {
            var k = "";
            var tag = "";
            var text = "";
            var tooltip = "";

            var b = new Border();
            b.Name = $"cell_header_{k}";
            b.Tag = tag;

            tooltip = $"{time}";

            var type = 0;
            {
                if(JobLogDS!=null)
                {
                    if(JobLogDS.Items.Count>0)
                    {
                        var ondate = $"{RowDate} {time}";
                        foreach(var row in JobLogDS.Items)
                        {
                            var start = row.CheckGet("ON_DATE");
                            if(ondate == start)
                            {
                                text = "";

                                if(row.CheckGet("RESULT").ToBool())
                                {
                                    type = 1;
                                }
                                else
                                {
                                    type = 3;
                                }
                                
                                tooltip = tooltip.Append($"запуск: {row.CheckGet("START")}",true);
                                tooltip = tooltip.Append($"результат: {row.CheckGet("ON_DATE")}", true);
                                tooltip = tooltip.Append($"время: {row.CheckGet("WORK_TIME")}", true);
                                tooltip = tooltip.Append($"репорт: {row.CheckGet("REPORT")}", true);

                                break;
                            }
                        }
                    }
                }
            }

            b.ToolTip = tooltip;

            switch(type)
            {
                default:
                case 0:
                    {
                        b.Style = (Style)RunMapContainer.TryFindResource("JobRunMapCell");
                    }
                    break;

                case 1:
                    {
                        b.Style = (Style)RunMapContainer.TryFindResource("JobRunMapCellComplete");
                    }
                    break;

                case 2:
                    {
                        b.Style = (Style)RunMapContainer.TryFindResource("JobRunMapCellWarning");
                    }
                    break;

                case 3:
                    {
                        b.Style = (Style)RunMapContainer.TryFindResource("JobRunMapCellFailed");
                    }
                    break;
            }

            var t = new TextBlock();
            t.Text = text;
            b.Child = t;

            return b;
        }

        private Border RowHeaderCreate(string time)
        {
            var k = "";
            var tag = "";

            var b = new Border();
            b.Name = $"cell_header_{k}";
            b.Tag = tag;

            var t=new TextBlock();
            t.Text = time;
            b.Child = t;

            return b;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            JobLogGridUpdateMap();
        }
    }
}
