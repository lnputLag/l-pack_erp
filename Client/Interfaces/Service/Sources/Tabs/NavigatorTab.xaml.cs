using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static Client.Interfaces.Main.FormDialog;

namespace Client.Interfaces.Service.Sources
{
    /// <summary>
    /// структура меню навигации
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-11-07</released>
    /// <changed>2024-04-18</changed>
    public partial class NavigatorTab : ControlBase
    {
        public NavigatorTab()
        {
            InitializeComponent();

            ControlSection = "navigation";
            RoleName = "[erp]server";
            ControlTitle ="Навигация";
            DocumentationUrl = "/doc/l-pack-erp-new/service_new/sources";

            OnMessage = (ItemMessage m) =>
            {
                if(m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnLoad =()=>
            {
                MenuGridInit();
                RoleGridInit();

                FormInit();
                SetDefaults();
            };

            OnKeyPressed = (KeyEventArgs e) =>
            {
                if(!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };

            OnUnload =()=>
            {
                MenuGrid.Destruct();
                RoleGrid.Destruct();
            };

            OnFocusGot=()=>
            {
                MenuGrid.ItemsAutoUpdate=true;
                MenuGrid.Run();
            };

            OnFocusLost=()=>
            {
                MenuGrid.ItemsAutoUpdate=false;
            };

            OnNavigate = () =>
            {
                
            };

            {
                Commander.SetCurrentGroup("main");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "refresh",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить данные",
                        ButtonUse = true,
                        ButtonName = "MenuRefreshButton",
                        MenuUse = true,
                        Action = () =>
                        {
                            MenuGrid.LoadItems();
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
                }

                
                Commander.SetCurrentGridName("MenuGrid");
                {
                    Commander.SetCurrentGroup("item");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "goto_interface",
                            Title = "Открыть",
                            MenuUse = true,       
                            ButtonUse=true,
                            ButtonName= "MenuNavigateButton",
                            HotKey= "Return|DoubleCLick",
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var url=MenuGrid.SelectedItem.CheckGet("ADDRESS");
                                if(!url.IsNullOrEmpty())
                                {
                                    url = $"l-pack://l-pack_erp{url}";
                                    Central.Navigator.ProcessURL(url);
                                }
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = MenuGrid.GetPrimaryKey();
                                var row = MenuGrid.SelectedItem;
                                if(!row.CheckGet("CODE").IsNullOrEmpty())
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });

                        Commander.Add(new CommandItem()
                        {
                            Name = "export",
                            Title = "Экспорт в Excel",
                            MenuUse = true,                           
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                MenuGrid.ExportItemsExcel();
                            },
                            CheckEnabled = () =>
                            {
                                return true;
                            }
                        });

                    }
                }
                
                
                Commander.SetCurrentGridName("RoleGrid");
                {
                    Commander.SetCurrentGroup("item");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "goto_role",
                            Title = "Перейти к роли",
                            MenuUse = true,
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var role = RoleGrid.SelectedItem.CheckGet("ROLE");
                                var url = $"l-pack://l-pack_erp/service/accounts/role?role_code={role}";
                                Central.Navigator.ProcessURL(url);
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = RoleGrid.GetPrimaryKey();
                                var row = RoleGrid.SelectedItem;
                                if(row.CheckGet(k).ToInt() != 0)
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });
                    }
                }
                
                Commander.Init(this);
            }

        }

        public FormHelper Form { get; set; }

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

        public void MenuGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="ID",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="CODE",
                    Path="CODE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn
                {
                    Header="Title",
                    Path="TITLE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=32,
                    FormatterRaw= (v) =>
                    {
                        var result = "";

                        var t=v.CheckGet("TITLE").ToString();
                        var level=v.CheckGet("LEVEL").ToInt();
                        var o="";
                        o=o.AddSymbols((level-1),"    |");
                        result=$"|{o}-⯈{t}";

                        return result;
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Name",
                    Path="NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=24,
                },
                new DataGridHelperColumn
                {
                    Header="Address",
                    Path="ADDRESS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=32,
                },                  
            };            
            MenuGrid.SetColumns(columns);
            MenuGrid.SetPrimaryKey("ID");
            MenuGrid.SetSorting("ID", ListSortDirection.Ascending);
            MenuGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            MenuGrid.SearchText = MenuGridSearch;
            MenuGrid.Toolbar = MenuGridToolbar;
            MenuGrid.OnSelectItem = selectedItem =>
            {
                RoleGrid.LoadItems();
            };
            MenuGrid.OnLoadItems = () => {
                var list = new List<Dictionary<string, string>>();
                if(Central.Navigator.Items != null)
                {
                    list = Central.Navigator.ExportMainMenu();
                }

                foreach(var row in list)
                {
                    var k = row.CheckGet("NAME");
                    k = k.ToUpper();
                    row.CheckAdd("CODE", k);
                }

                var ds = ListDataSet.Create(list);
                MenuGrid.UpdateItems(ds);
            };           
            MenuGrid.Commands = Commander;
            MenuGrid.Init();            
        }

        public void RoleGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Роль",
                    Path="ROLE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=24,
                },
            };
            RoleGrid.SetColumns(columns);
            RoleGrid.SetPrimaryKey("ID");
            RoleGrid.SetSorting("ID", ListSortDirection.Ascending);
            RoleGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            RoleGrid.SearchText = RoleGridSearch;
            RoleGrid.Toolbar=RoleGridToolbar;
            RoleGrid.ItemsAutoUpdate=false;
            RoleGrid.OnLoadItems = () => {
                var v = MenuGrid.SelectedItem;
                if(v.Count > 0)
                {
                    var list = new List<Dictionary<string, string>>();
                    var roles = v.CheckGet("ALLOWED_ROLES");
                    if(!roles.IsNullOrEmpty())
                    {
                        var rolesList = roles.Split(',').ToList();
                        int j = 0;
                        foreach(string role in rolesList)
                        {
                            j++;
                            var row = new Dictionary<string, string>();
                            row.CheckAdd("ID", j.ToString());
                            row.CheckAdd("ROLE", role.ToString());
                            list.Add(row);
                        }
                    }

                    var ds = ListDataSet.Create(list);
                    RoleGrid.UpdateItems(ds);
                }
            };
            RoleGrid.Commands = Commander;
            RoleGrid.Init();
        }
    }
}
