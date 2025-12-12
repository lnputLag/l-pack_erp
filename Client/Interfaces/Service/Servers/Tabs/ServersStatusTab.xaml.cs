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
    public partial class ServersStatusTab : ControlBase
    {
        public ServersStatusTab()
        {           

            InitializeComponent();

            ControlSection = "server_status";
            RoleName = "[erp]server";
            ControlTitle ="Инстансы";
            DocumentationUrl = "/doc/l-pack-erp-new/service_new/servers";

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
                //if(!Central.DebugMode)
                //{
                //    LoadItemsTimer.Run();
                //    //UpdateGridTimer.Run();
                //}
            };

            OnFocusLost=()=>
            {
                //if(!Central.DebugMode)
                //{
                //    LoadItemsTimer.Finish();
                //    //UpdateGridTimer.Finish();
                //}
            };

            OnNavigate = () =>
            {
                //var positionId = Parameters.CheckGet("position_id");
                //if(!positionId.IsNullOrEmpty())
                //{
                //    PositionGridSearch.Text = positionId;
                //    PositionGrid.UpdateItems();
                //}
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
                                var id = ServerGrid.SelectedItem.CheckGet("INSTANCE_NAME");
                                var h = new SendCommandForm();
                                h.PrimaryKeyValue = id;
                                h.Init();
                            },
                            CheckEnabled = () =>
                            {
                                //var result = false;
                                //var row = ServerGrid.SelectedItem;
                                //if(
                                //    !row.CheckGet("INSTANCE_NAME").IsNullOrEmpty()
                                //    && row.CheckGet("STATUS").IsNullOrEmpty()
                                //)
                                //{
                                //    result = true;
                                //}

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
                                var url = $"http://{ip}:{port-1000}/status";
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

            LoadItemsTimer = new Timeout(
                1,
                () => {
                    LoadItems();
                    c1++;
                },
                true,
                false
            );
            //LoadItemsTimer.SetIntervalMs(DataLoadTimeout);
            LoadItemsTimer.SetIntervalMs(1000);
            //LoadItemsTimer.Run();


            UpdateGridTimer = new Timeout(
                1,
                () => {
                    ServerGrid.LoadItems();
                    c2++;
                },
                true,
                false
            );
            UpdateGridTimer.SetIntervalMs(1000);
            //UpdateGridTimer.Run();

            {
                UpdateLogTimer = new Timeout(
                   1,
                   () => {

                       var s = "";
                       s = s.Append($"журнал", true);
                       s = s.Append($"update: {c2}",true);
                       s = s.Append($"load: {c1}", true);

                       var text = Log.Text;
                       //text = text.Append(text, true);
                       text = s;
                       Log.Text = text;
                   },
                   true,
                   false
                );
                UpdateLogTimer.SetIntervalMs(1000);
                UpdateLogTimer.Run();
            }

            if(Central.DebugMode)
            {
                DebugPanel.Visibility = Visibility.Visible;
            }
            else
            {
                DebugPanel.Visibility = Visibility.Collapsed;               
            }
        }

        private void UpdRun_Click(object sender, RoutedEventArgs e)
        {
            var i = UpdInterval.Text.ToInt();
            UpdateGridTimer.SetIntervalMs(i);
            UpdateGridTimer.Run();
        }

        private void UpdStop_Click(object sender, RoutedEventArgs e)
        {
            UpdateGridTimer.Finish();
        }

        private void LoadRun_Click(object sender, RoutedEventArgs e)
        {
            var i = LoadInterval.Text.ToInt();
            LoadItemsTimer.SetIntervalMs(i);
            LoadItemsTimer.Run();
        }

        private void LoadStop_Click(object sender, RoutedEventArgs e)
        {
            LoadItemsTimer.Finish();
        }

        private int DataLoadTimeout { get; set; }
        private int ActualInterval {  get; set; }
        private int LimitLA { get; set; }
        private int LimitIow { get; set; }
        private int LimitDf { get; set; }
        private double LimitIf { get; set; }

        private int c1 {  get; set; }
        private int c2 { get; set; }
        private Timeout LoadItemsTimer {  get; set; }
        private Timeout UpdateGridTimer { get; set; }
        private Timeout UpdateLogTimer { get; set; }
        private FormHelper Form { get; set; }

        private ListDataSet ItemsDs { get; set; }
        private ListDataSet ServerDs { get; set; }
        private ListDataSet InstanceDs { get; set; }
        private ListDataSet HostDs { get; set; }

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
                    Path="INSTANCE_NAME",
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
                                    && row.CheckGet("REQUEST_COUNTER").ToInt() < 300
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
                    Header="GIT",
                    Path="GIT_WORKING_BRANCH",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="БД",
                    Path="DB_TAG",
                    ColumnType=ColumnTypeRef.String,
                    Width2=6,
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
                new DataGridHelperColumn
                {
                    Header="Aктуальность",
                    Path="_ACTUAL",
                    ColumnType=ColumnTypeRef.String,
                    Width2=5,                  
                    FormatterRaw= (row) =>
                    {
                        var result = "";
                        var dt=(int)Tools.TimeOffsetSeconds(row.CheckGet("ON_DATE"));
                        result=dt.ToString();
                        return result;
                    },
                },
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

                                if(row.CheckGet("PID").ToInt() == 0)
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
                    Header="IOW",
                    Path="IOW",
                    Description="IO Wait",
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

                                if(row.CheckGet("IOW").ToInt() > LimitIow)
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
                /*
                new DataGridHelperColumn
                {
                    Header="IF",
                    Path="IFH",
                    Description="Inode Free",
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

                                if(row.CheckGet("IFH").ToInt() < LimitIf)
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
                */
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

                        {
                            if(!row.CheckGet("STATUS").IsNullOrEmpty())
                            {
                                color = HColor.Yellow;
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
            ServerGrid.SetPrimaryKey("INSTANCE_NAME");
            ServerGrid.SetSorting("ORDER", ListSortDirection.Ascending);
            ServerGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            ServerGrid.SearchText = ServerGridSearch;
            ServerGrid.Toolbar = ServerGridToolbar;
            ServerGrid.AutoUpdateInterval = 2;
            ServerGrid.UseProgressBar = true;
            ServerGrid.UseProgressSplashAuto = true;
            ServerGrid.OnLoadItems = () => {
                LoadItems();
            };
      
            ServerGrid.OnSelectItem = (item) => {
            };
            ServerGrid.Commands = Commander;
            ServerGrid.Init();            
        }

        public void LoadItems()
        {
            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("TABLE_NAME", "server_stat_state,instance_state,host_state");
                p.CheckAdd("TABLE_DIRECTORY", "");
                // 1=global,2=local,3=net
                p.CheckAdd("STORAGE_TYPE", "3");
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Service");
            q.Request.SetParam("Object", "LiteBase");
            q.Request.SetParam("Action", "List");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if(q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if(result != null)
                {
                    ServerDs = ListDataSet.Create(result, "server_stat_state");
                    InstanceDs = ListDataSet.Create(result, "instance_state");                  
                    HostDs = ListDataSet.Create(result, "host_state");
                    MergeItems();
                    ServerGrid.UpdateItems(ItemsDs);
                }
            }
        }
        
        public void MergeItems()
        {
            var items = new List<Dictionary<string, string>>();

            var row0 = new Dictionary<string, string>();
            {
                row0 = new Dictionary<string, string>();
                int j=0;
                foreach(var instance in InstanceDs.Items)
                {
                    j++;

                    if(j==1)
                    {
                        foreach(var item in instance)
                        {
                            row0.CheckAdd(item.Key, "");
                        }

                        foreach(var host in HostDs.Items)
                        {
                            if(host.CheckGet("INSTANCE_NAME") == instance.CheckGet("INSTANCE_NAME"))
                            {
                                foreach(var item in host)
                                {
                                    row0.CheckAdd(item.Key, "");
                                }
                                break;
                            }
                        }

                        foreach(var server in ServerDs.Items)
                        {
                            if(server.CheckGet("SERVER_NAME") == instance.CheckGet("INSTANCE_NAME"))
                            {
                                foreach(var item in server)
                                {
                                    row0.CheckAdd(item.Key, "");
                                }
                                break;
                            }
                        }                        
                    }
                   
                }
            }
           
            foreach(var instance in InstanceDs.Items)
            {
                var row = new Dictionary<string, string>(row0);

                foreach(var item in instance)
                {
                    row.CheckAdd(item.Key, item.Value);
                }

                foreach(var host in HostDs.Items)
                {
                    if(host.CheckGet("INSTANCE_NAME") == instance.CheckGet("INSTANCE_NAME"))
                    {
                        foreach(var item in host)
                        {
                            row.CheckAdd(item.Key, item.Value);
                        }
                        break;
                    }
                }

                foreach(var server in ServerDs.Items)
                {
                    if(server.CheckGet("SERVER_NAME") == instance.CheckGet("INSTANCE_NAME"))
                    {
                        foreach(var item in server)
                        {
                            row.CheckAdd(item.Key, item.Value);
                        }
                        break;
                    }
                }
                items.Add(row);
            }
            ItemsDs = ListDataSet.Create2(items);
        }
    }
}
