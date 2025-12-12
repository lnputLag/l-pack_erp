using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Service
{
    /// <summary>
    /// список мобильных версий
    /// </summary>
    /// <author>eletskikh_ya</author>
    /// <version>1</version>
    public partial class MobileList : ControlBase
    {
        public MobileList()
        {
            InitializeComponent();


            RoleName = "[erp]client";
            ControlTitle = "Планшеты";
            DocumentationUrl = "/doc/l-pack-erp/service/agent/mobiles";

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

            object item = this.FindName("dfgsdf");

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
                    MenuUse = true,
                    Action = () =>
                    {
                        var id = Grid.SelectedItem.CheckGet("HOST_USER_ID");
                        var h = new SendCommandForm();
                        h.Scope = "mobile";
                        h.PrimaryKeyValue = id;
                        h.TitleCustom=id;
                        h.Init();
                    },
                    CheckEnabled = () =>
                    {
                        //var result = false;
                        //var row = Grid.SelectedItem;
                        //if(
                        //    !row.CheckGet("HOST_USER_ID").IsNullOrEmpty()
                        //    && row.CheckGet("ON_LINE").ToBool()
                        //)
                        //{
                        //    result = true;
                        //}
                        //return result;
                        return true;
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

            OnlineTimeout = 50;
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
                    Doc="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
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
                    Header="Версия",
                    Path="VERSION",
                    Doc="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=9,
                },
                new DataGridHelperColumn
                {
                    Header="ID",
                    Path="HOST_USER_ID",
                    Doc="",
                    ColumnType=ColumnTypeRef.String,
                    Width2=32,
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
                    Header="ACCO",
                    Path="ACCOUNT_ID",
                    Description="ACCOUNT_ID",
                    Doc="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Складская зона",
                    Path="CLIENT_ZONE_ID",
                    Doc="",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
            };
            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("HOST_USER_ID");
            Grid.SetSorting("CLIENT_LOGIN", ListSortDirection.Ascending);
            Grid.SearchText = Search;
            Grid.Toolbar = GridToolbar;
            Grid.AutoUpdateInterval = 60;
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
                            //    color = HColor.Red;
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
                q.Request.SetParam("Action", "ListMobile");
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
                                    var h = "";
                                    {
                                        var s = row.CheckGet("UPTIME").ToDouble();
                                        if(s > 0)
                                        {
                                            TimeSpan t = TimeSpan.FromSeconds(s);
                                            //h=time.ToString("dd hh:mm:ss");
                                            h = string.Format(
                                                "{0:D2} {0:D2}:{1:D2}:{2:D2}",
                                                t.Days,
                                                t.Hours,
                                                t.Minutes,
                                                t.Seconds
                                            );
                                        }
                                    }
                                    row.CheckAdd("UPTIME_HUMAN", h);

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