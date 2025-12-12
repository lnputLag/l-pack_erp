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

namespace Client.Interfaces.Service.Jobs
{
    /// <summary>
    /// реестр джобов
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2025-11-27</released>
    /// <changed>2025-11-27</changed>
    public partial class JobList : ControlBase
    {
        public JobList()
        {
            InitializeComponent();


            RoleName = "[erp]client";
            ControlTitle = "Джобы";
            DocumentationUrl = "/doc/l-pack-erp/service/job/job_list";

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

                Commander.SetCurrentGridName("Grid");
                {
                    Commander.SetCurrentGroup("grid_base");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "grid_base_refresh",
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
                    }

                    Commander.SetCurrentGroup("grid_item");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "grid_item_dump",
                            Enabled = true,
                            MenuUse = true,
                            Title = "Информация",
                            Action = () =>
                            {
                                var logViewer=new ReportViewer();
                                logViewer.Content=Grid.SelectedItem.GetDumpString();
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
                            Name = "grid_item_config",
                            Enabled = true,
                            Title = "Конфигурация",
                            Description = "Изменить конфигурационный файл",
                            ButtonUse = true,
                            ButtonName = "ConfigButton",
                            MenuUse = true,
                            Action = () =>
                            {
                                var name = Grid.SelectedItem.CheckGet("NAME");
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
                                var row = Grid.SelectedItem;
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
                            Name = "grid_item_doc",
                            Enabled = true,
                            Title = "Документация",
                            MenuUse = true,
                            Action = () =>
                            {
                                var url = Grid.SelectedItem.CheckGet("DOC_URL");
                                if(!url.IsNullOrEmpty())
                                {
                                    Central.ShowHelp(url,false,true);
                                }                                
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var row = Grid.SelectedItem;
                                if(!row.CheckGet("DOC_URL").IsNullOrEmpty())
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "grid_item_mon",
                            Enabled = true,
                            Title = "Монитор",
                            MenuUse = true,
                            Action = () =>
                            {
                                var url = Grid.SelectedItem.CheckGet("MONITOR_FILE_URL");
                                if(!url.IsNullOrEmpty())
                                {
                                    Central.ShowHelp(url,false,true);
                                }                                
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var row = Grid.SelectedItem;
                                if(!row.CheckGet("MONITOR_FILE_URL").IsNullOrEmpty())
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "grid_item_var",
                            Title = "Переменные",
                            Description = "Установить переменные",
                            MenuUse = true,
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
                                    !row.CheckGet("NAME").IsNullOrEmpty()
                                )
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
           
        }

        private string Log{get;set;}="";
        public FormHelper Form { get; set; }

        public void GridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Имя",
                    Path="NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=32,
                },
                new DataGridHelperColumn
                {
                    Header="Сервер",
                    Path="SERVER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn
                {
                    Header="Описание",
                    Path="DESCRIPTION",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn
                {
                    Header="Тип",
                    Path="KIND",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Активность",
                    Path="ENABLED",
                    ColumnType=ColumnTypeRef.Boolean,
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
                    Header="Площадка",
                    Path="VAR_FACTORY_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Сегмент",
                    Path="VAR_SEGMENT",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание",
                    Path="VAR_DESCRIPTION",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },

                new DataGridHelperColumn
                {
                    Header="Отладка",
                    Path="DEBUG",
                    ColumnType=ColumnTypeRef.Boolean,
                },
                
                new DataGridHelperColumn
                {
                    Header="WD",
                    Path="WATCHDOG_ENABLED",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="INT",
                    Path="WATCHDOG_INTERVAL",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="WDFL",
                    Path="WATCHDOG_FAULT_ENABLED",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="LIMIT",
                    Path="WATCHDOG_FAULT_LIMIT",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="WDSK",
                    Path="WATCHDOG_SKIP_ENABLED",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="LIMIT",
                    Path="WATCHDOG_SKIP_LIMIT",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
              
               
                new DataGridHelperColumn
                {
                    Header="ON_DATE",
                    Path="ON_DATE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
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
                new DataGridHelperColumn
                {
                    Header="DOC_URL",
                    Path="DOC_URL",
                    ColumnType=ColumnTypeRef.String,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="MONITOR_FILE_URL",
                    Path="MONITOR_FILE_URL",
                    ColumnType=ColumnTypeRef.String,
                    Width2=4,
                },
               
            };

            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("NAME");
            Grid.SetSorting("NAME", ListSortDirection.Ascending);
            Grid.SearchText = Search;
            Grid.Toolbar = GridToolbar;
            Grid.AutoUpdateInterval = 0;
            Grid.UseProgressSplashAuto = true;
            Grid.UseProgressBar = true;
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.OnLoadItems = LoadItems;
            Grid.OnFilterItems = FilterItems;
            Grid.Commands = Commander;
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
                q.Request.SetParam("Module", "Service/Job");
                q.Request.SetParam("Object", "Item");
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
        }
    }
}
