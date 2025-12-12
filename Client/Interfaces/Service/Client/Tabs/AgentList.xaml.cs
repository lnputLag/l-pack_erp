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
    /// <version>2</version>
    /// <released>2023-02-16</released>
    /// <changed>2024-05-29</changed>
    public partial class
    AgentList : ControlBase
    {
        public AgentList()
        {
            InitializeComponent();

            OnlineTimeout = 50;

            RoleName = "[erp]client";
            ControlTitle = "Агенты";
            DocumentationUrl = "/doc/l-pack-erp/service/agent/agents";
            HideComplete = false;

            OnMessage = (ItemMessage m) =>
             {
                 if (m.ReceiverName == ControlName)
                 {
                     Commander.ProcessCommand(m.Action, m);
                 }
            };

            OnKeyPressed = (KeyEventArgs e) =>
              {
                  if (!e.Handled)
                  {
                      Commander.ProcessKeyboard(e);
                  }

                  if (!e.Handled)
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

            //object item = this.FindName("dfgsdf");

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
                    Name = "show_info",
                    Title = "Информация",
                    Description = "Показать информацию",
                    MenuUse = true,
                    Action = () =>
                    {
                        var id = Grid.SelectedItem.CheckGet("HOST_USER_ID");
                        var h = new ShowInfoForm();
                        h.PrimaryKeyValue = id;
                        h.Init();
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (
                            !row.CheckGet("HOST_USER_ID").IsNullOrEmpty()
                        )
                        {
                            result = true;
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "send_command",
                    Title = "Команда",
                    Description = "Отправить команду",
                    ButtonUse = true,
                    ButtonName = "CommandButton",
                    HotKey = "Return|DoubleCLick",
                    MenuUse = true,
                    Action = () =>
                    {
                        var id = Grid.SelectedItem.CheckGet("HOST_USER_ID");
                        var h = new SendCommandForm();
                        h.Scope = "agent";
                        h.PrimaryKeyValue = id;
                        h.TitleCustom=id;
                        h.Init();
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (
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
                    Name = "open_radmin",
                    Title = "Radmin",
                    Description = "Запустить Radmin",
                    MenuUse = true,
                    Action = () =>
                    {
                        var ip = Grid.SelectedItem.CheckGet("SYSTEM_NETWORK_IP");
                        if (!ip.IsNullOrEmpty())
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
                            catch (Exception)
                            {

                            }
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (
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
                    Name = "config_edit_host_user_id",
                    Title = "Изменить конфиг (ID хоста)",
                    MenuUse = true,
                    Action = () =>
                    {
                        if (Grid.SelectedItem != null)
                        {
                            var hostUserId = Grid.SelectedItem.CheckGet("HOST_USER_ID").ToString();
                            if (!hostUserId.IsNullOrEmpty())
                            {
                                var i = new Config();
                                i.HostUserId= hostUserId;
                                i.Mode = 2;
                                i.Init();
                                i.Edit();
                            }
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (
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
                    Name = "config_edit_installation_place",
                    Title = "Изменить конфиг (место установки)",
                    MenuUse = true,
                    Action = () =>
                    {
                        if(Grid.SelectedItem != null)
                        {
                            var installationPlace = Grid.SelectedItem.CheckGet("CLIENT_INSTALLATION_PLACE").ToString();
                            if(!installationPlace.IsNullOrEmpty())
                            {
                                var i = new Config();
                                i.InstallationPlace = installationPlace;
                                i.Mode = 1;
                                i.Init();
                                i.Edit();
                            }
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if(
                            !row.CheckGet("CLIENT_INSTALLATION_PLACE").IsNullOrEmpty()
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
                    Name = "show_screenshots",
                    Title = "Скриншоты",
                    Description = "Показать скриншоты экрана",
                    MenuUse = true,
                    Action = () =>
                    {
                        var h = new PhotoScreen();
                        h.HostUserId = Grid.SelectedItem.CheckGet("HOST_USER_ID");
                        h.Edit();
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (
                            row.CheckGet("Printing").ToBool()
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
                    Name = "show_fire_alarm_log",
                    Title = "Протокол работы агента",
                    Description = "Показать логи работы анента",
                    MenuUse = true,
                    Action = () =>
                    {
                        var h = new FireAlarmLog();
                        h.HostUserId = Grid.SelectedItem.CheckGet("HOST_USER_ID").ToLower();
                        h.Edit();
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (
                            row.CheckGet("FireAlarm").ToBool()
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
                Commander.Add(new CommandItem()
                {
                    Name = "export_config",
                    Enabled = true,
                    Title = "Экспорт конфигов",
                    MenuUse = true,
                    ButtonUse=true,
                    ButtonControl= ExportConfigButton,
                    Action = () =>
                    {
                        ExportConfig();
                    },
                });

                Commander.Init(this);
            }

            
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
        /// интервал неактивности клиента, сек
        /// (если клиент не "пинговался" дольше этого интервала, 
        /// считаем, что он оффлайн)
        /// </summary>
        public int OnlineTimeout { get; set; }

        /// <summary>
        /// Признак скрыть всех агентов, кроме пожарки
        /// </summary>
        public bool HideComplete { get; set; }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void GridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="ID",
                    Path="HOST_USER_ID",
                    Doc="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                 new DataGridHelperColumn
                {
                    Header="Placement",
                    Path="CLIENT_PLACEMENT",
                    Doc="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Имя",
                    Path="CLIENT_TITLE",
                    Doc="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn
                {
                    Header="Площадка",
                    Path="CLIENT_FACTORY_ID",
                    Doc="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=4,
                    
                },
                new DataGridHelperColumn
                {
                    Header="Онлайн",
                    Path="ON_LINE",
                    Doc="",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Конфиг сервера",
                    Description="используется удаленный конфиг сервера",
                    Path="CLIENT_USE_EXTERNAL_CONFIG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Место установки",
                    Path="CLIENT_INSTALLATION_PLACE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Версия",
                    Path="VERSION",
                    Doc="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="IP",
                    Path="SYSTEM_NETWORK_IP",
                    Doc="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Активность",
                    Path="ON_DATE",
                    Doc="",
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
                    Header="RAM",
                    Path="SYSTEM_MEMORY_USED",
                    Doc="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="PhoneBook",
                    Path="PhoneBook",
                    Doc="",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="PhotoScreen",
                    Path="PhotoScreen",
                    Doc="",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="ReadOpc",
                    Path="ReadOpc",
                    Doc="",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="ReadModbus",
                    Path="ReadModbus",
                    Doc="",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Printing",
                    Path="Printing",
                    Doc="",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Время печати ярлыка",
                    Path="PRINTING_LAST_DATE",
                    Doc="",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=8,
                    Format="dd.MM HH:mm",
                },
                new DataGridHelperColumn
                {
                    Header="FireAlarm",
                    Path="FireAlarm",
                    Doc="",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Group",
                    Path="CLIENT_GROUP",
                    Doc="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="SubGroup",
                    Path="CLIENT_SUBGROUP",
                    Doc="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Description",
                    Path="CLIENT_DESCRIPTION",
                    Doc="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },               
                new DataGridHelperColumn
                {
                    Header="SessionToken",
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

                            // 0=human, 1=machine, 
                            var mode=0;

                            //if(row.CheckGet("CLIENT_USE_EXTERNAL_CONFIG").ToBool())
                            if (!row.CheckGet("CLIENT_TITLE").ToString().IsNullOrEmpty())
                            {
                                mode=1;
                            }

                            if (row.CheckGet("CLIENT_TITLE").ToString().IsNullOrEmpty())
                            {
                                color = HColor.Gray;
                                
                            }

                            if(mode == 1)
                            {
                                // сервис печати включен, давно не печатал: оранжевый
                                if (
                                    row.CheckGet("Printing").ToBool()
                                    && row.CheckGet("PRINTING_LAST_DATE").IsNullOrEmpty()
                                )
                                {
                                    color = HColor.Orange;
                                }
                            }

                            if(mode == 1)
                            {
                                if (!row.CheckGet("ON_LINE").ToBool())
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
                    Path="VERSION",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Version,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
                new FormHelperField()
                {
                    Path="GROUP",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=GroupType,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
                new FormHelperField()
                {
                    Path="SUBGROUP",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=SubGroupType,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
                new FormHelperField()
                {
                    Path="SERVICE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ServiceType,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },                    
            };

            {
                var list = new Dictionary<string, string>();
                list.CheckAdd("_","");
                list.CheckAdd("PhoneBook", "PhoneBook");
                list.CheckAdd("PhotoScreen", "PhotoScreen");
                list.CheckAdd("ReadOpc", "ReadOpc");
                list.CheckAdd("ReadModbus", "ReadModbus");
                list.CheckAdd("Printing", "Printing");
                list.CheckAdd("FireAlarm", "FireAlarm");
                ServiceType.SetItems(list);
            }
            

            Form.SetFields(fields);


        }

        public void SetDefaults()
        {
            Form.SetDefaults();
        }

        public async void LoadItems()
        {
            var today = DateTime.Now;
            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Service");
                q.Request.SetParam("Object", "Control");
                q.Request.SetParam("Action", "ListAgent");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {

                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            if (ds.Items.Count > 0)
                            {
                                var groupNameList = new Dictionary<string, string>();
                                groupNameList.Add("_", "");

                                var subGroupNameList = new Dictionary<string, string>();
                                subGroupNameList.Add("_", "");

                                foreach (Dictionary<string, string> row in ds.Items)
                                {
                                    row.CheckAdd("PhoneBook", "0");
                                    row.CheckAdd("PhotoScreen", "0");
                                    row.CheckAdd("ReadOpc", "0");
                                    row.CheckAdd("ReadModbus", "0");
                                    row.CheckAdd("Printing", "0");
                                    row.CheckAdd("FireAlarm", "0");
                                    {
                                        var s = row.CheckGet("SERVICES");
                                        if (!s.IsNullOrEmpty())
                                        {
                                            var items = JsonConvert.DeserializeObject<Dictionary<string, string>>(s);
                                            if (items != null)
                                            {
                                                foreach (KeyValuePair<string, string> item in items)
                                                {
                                                    row.CheckAdd(item.Key, item.Value);
                                                }
                                            }
                                        }
                                    }

                                    row.CheckAdd("ON_LINE", "0");
                                    {
                                        var s = row.CheckGet("ON_DATE");
                                        if (!s.IsNullOrEmpty())
                                        {
                                            var onDate = s.ToDateTime();
                                            var timeDiff = (TimeSpan)(today - onDate);
                                            var sec = timeDiff.TotalSeconds;
                                            var onLine = "0";
                                            if (sec <= OnlineTimeout)
                                            {
                                                onLine = "1";
                                            }
                                            row.CheckAdd("ON_LINE", onLine);
                                        }
                                    }

                                    row.CheckAdd("UPTIME_HUMAN", "");
                                    {
                                        var s = row.CheckGet("UPTIME").ToDouble();
                                        if (s > 0)
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

                                    {
                                        string groupName = row.CheckGet("CLIENT_GROUP");
                                        var k = groupName.Trim().ToLower();
                                        var v = groupName.Trim().ToLower();
                                        groupNameList.CheckAdd(k, v);
                                    }

                                    {
                                        string groupName = row.CheckGet("CLIENT_SUBGROUP");
                                        var k = groupName.Trim().ToLower();
                                        var v = groupName.Trim().ToLower();
                                        subGroupNameList.CheckAdd(k, v);
                                    }
                                }

                                groupNameList = groupNameList.OrderBy(obj => obj.Value).ToDictionary(obj => obj.Key, obj => obj.Value);
                                GroupType.Items = groupNameList;

                                subGroupNameList = subGroupNameList.OrderBy(obj => obj.Value).ToDictionary(obj => obj.Key, obj => obj.Value);
                                SubGroupType.Items = subGroupNameList;
                            }

                            Grid.UpdateItems(ds);

                        }

                    }
                }
            }
        }

        public void FilterItems()
        {
            if (Grid.Items != null)
            {
                if (Grid.Items.Count > 0)
                {
                    var v=Form.GetValues();

                    var filterByGroup = false;
                    if(!v.CheckGet("GROUP").IsNullOrEmpty())
                    {
                        filterByGroup = true;
                    }

                    var filterBySubGroup = false;
                    if(!v.CheckGet("SUBGROUP").IsNullOrEmpty())
                    {
                        filterBySubGroup = true;
                    }

                    var filterByService = false;
                    if(!v.CheckGet("SERVICE").IsNullOrEmpty())
                    {
                        filterByService = true;
                    }

                    var filterByVersion = false;
                    var xv=0;
                    if(!v.CheckGet("VERSION").IsNullOrEmpty())
                    {
                        filterByVersion = true;
                        xv=Tools.GetVersionInteger(v.CheckGet("VERSION"));
                    }

                    var items = new List<Dictionary<string, string>>();
                    foreach (var row in Grid.Items)
                    {
                        bool includeByGroup = true;
                        bool includeBySubGroup = true;
                        bool includeByService = true;
                        bool includeByVersion=true;

                        if(filterByGroup)
                        {
                            includeByGroup = false;
                            if(row.CheckGet("CLIENT_GROUP").Trim().ToLower() == v.CheckGet("GROUP").Trim().ToLower())
                            {
                                includeByGroup = true;
                            }
                        }

                        if(filterBySubGroup)
                        {
                            includeBySubGroup = false;
                            if(row.CheckGet("CLIENT_SUBGROUP").Trim().ToLower() == v.CheckGet("SUBGROUP").Trim().ToLower())
                            {
                                includeBySubGroup = true;
                            }
                        }

                        if(filterByService)
                        {
                            includeByService = false;
                            var k = v.CheckGet("SERVICE");
                            if(row.CheckGet(k).ToBool())
                            {
                                includeByService = true;
                            }
                        }

                        if(filterByVersion)
                        {
                            includeByVersion = false;
                            var k = row.CheckGet("VERSION");
                            var x=Tools.GetVersionInteger(k);
                            if(x<xv)
                            {
                                includeByVersion = true;
                            }
                        }

                        if(
                            includeByGroup
                            && includeBySubGroup
                            && includeByService
                            && includeByVersion
                        )
                        {
                            items.Add(row);
                        }
                    }
                    Grid.Items = items;
                }
            }
        }

        public void ExportConfig()
        {
            var items = Grid.GetItems();
            if(items.Count > 0)
            {
                foreach(Dictionary<string,string> row in items)
                {
                    var hostUserId = row.CheckGet("HOST_USER_ID");
                    var content=GetRemoteConfig(hostUserId);
                }
            }
        }


        public string GetRemoteConfig(string hostUserId)
        {
            var content = "";

            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("HOST_USER_ID", hostUserId.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Service");
                q.Request.SetParam("Object", "Control");
                q.Request.SetParam("Action", "GetConfig");
                q.Request.SetParams(p);

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
                                content = ds.GetFirstItemValueByKey("CONTENT");
                            }
                        }
                    }
                }
            }

            return content;
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
