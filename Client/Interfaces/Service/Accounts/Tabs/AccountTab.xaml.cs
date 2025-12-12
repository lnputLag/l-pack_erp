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

namespace Client.Interfaces.Accounts
{
    /// <summary>
    /// аккаунты
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-11-08</released>
    /// <changed>2024-04-08</changed>
    public partial class AccountTab : ControlBase
    {
        /*
            | AccountGrid | RoleGrid |

            В левом гриде показаны все записи из таблицы "аккаунты".
            При выборе одного аккаунта в правый грид подгружаются
            все роли с уровнями доступа для выбранного аккаунта.
         */
        public AccountTab()
        {
            InitializeComponent();

            ControlSection = "account";
            RoleName = "[erp]accounts";
            ControlTitle ="Аккаунты";
            DocumentationUrl = "/doc/l-pack-erp/service/agent/agents";

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
                AccountGridInit();
                RoleGridInit();
                SetDefaults();
            };

            OnUnload=()=>
            {
                AccountGrid.Destruct();
                RoleGrid.Destruct();
            };

            OnFocusGot=()=>
            {
                AccountGrid.ItemsAutoUpdate=true;
                AccountGrid.Run();

                
            };

            OnFocusLost=()=>
            {
                AccountGrid.ItemsAutoUpdate=false;
            };

            OnNavigate = () =>
            {
                var login = Parameters.CheckGet("login");
                if(!login.IsNullOrEmpty())
                {
                    AccountGridSearch.Text = login;
                }
            };

            {
                Commander.SetCurrentGroup("main");
                {
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
                            AccountGrid.LoadItems();
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


                Commander.SetCurrentGridName("AccountGrid");
                {
                    Commander.SetCurrentGroup("item");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "account_create",
                            Title = "Создать",
                            MenuUse = true,
                            HotKey = "Insert",
                            ButtonUse = true,
                            ButtonName = "CreateButton",
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var i = new AccountForm();
                                i.Create();
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = AccountGrid.GetPrimaryKey();
                                var row = AccountGrid.SelectedItem;
                                if(row.CheckGet(k).ToInt() != 0)
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "account_edit",
                            Title = "Изменить",
                            MenuUse = true,
                            HotKey = "Return|DoubleCLick",
                            ButtonUse=true,
                            ButtonName = "EditButton",
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var k = AccountGrid.GetPrimaryKey();
                                var accountID = AccountGrid.SelectedItem.CheckGet(k).ToInt();
                                if(accountID != 0)
                                {
                                    var i = new AccountForm();
                                    i.Edit(accountID);
                                }
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = AccountGrid.GetPrimaryKey();
                                var row = AccountGrid.SelectedItem;
                                if(row.CheckGet(k).ToInt() != 0)
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });
                    }

                    Commander.SetCurrentGroup("block");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "account_block",
                            Title = "Блокировать",
                            MenuUse = true,
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                AccountSetLock(1);
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = AccountGrid.GetPrimaryKey();
                                var row = AccountGrid.SelectedItem;
                                if(!row.CheckGet("LOCKED_FLAG").ToBool())
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "account_unblock",
                            Title = "Разблокировать",
                            MenuUse = true,
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                AccountSetLock(0);
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = AccountGrid.GetPrimaryKey();
                                var row = AccountGrid.SelectedItem;
                                if(row.CheckGet("LOCKED_FLAG").ToBool())
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });
                    }

                    Commander.SetCurrentGroup("service");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "account_role_list",
                            Title = "Роли",
                            MenuUse = true,
                            Action = () =>
                            {
                                RoleList();
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = AccountGrid.GetPrimaryKey();
                                var row = AccountGrid.SelectedItem;
                                if(row.CheckGet(k).ToInt() != 0)
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });
                    }
                }

                Commander.SetCurrentGridName("RoleGrid");
                {
                    Commander.SetCurrentGroup("item");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "role_set_level_deny",
                            Title = "Запрещен",
                            MenuUse = true,
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                RoleSet(0);
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
                        Commander.Add(new CommandItem()
                        {
                            Name = "role_set_level_readonly",
                            Title = "Только чтение",
                            MenuUse = true,
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                RoleSet(1);
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
                        Commander.Add(new CommandItem()
                        {
                            Name = "role_set_level_access_all",
                            Title = "Полный доступ",
                            MenuUse = true,
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                RoleSet(2);
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
                        Commander.Add(new CommandItem()
                        {
                            Name = "role_set_level_special",
                            Title = "Спецправа",
                            MenuUse = true,
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                RoleSet(3);
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

                    Commander.SetCurrentGroup("link");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "goto_role",
                            Title = "Перейти к роли",
                            MenuUse = true,
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var role_code = RoleGrid.SelectedItem.CheckGet("CODE");
                                var url = $"l-pack://l-pack_erp/service/accounts/role?role_code={role_code}";
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

        public void SetDefaults()
        {         
            Form.SetDefaults();
        }

        public void AccountGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="ID",
                    Doc="Идентификатор аккаунта",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Логин",
                    Path="LOGIN",
                    Doc="Логин",
                    ColumnType=ColumnTypeRef.String,
                    Width2=17,

                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="NAME",
                    Doc="Наименование аккаунта",
                    ColumnType=ColumnTypeRef.String,
                    Width2=23,
                },
                new DataGridHelperColumn
                {
                    Header="Заблокирован",
                    Path="LOCKED_FLAG",
                    Doc="Аккаунт заблокирован",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Сотрудник",
                    Path="IS_EMPLOYEE",
                    Name="IsEmployee",
                    Doc="Аккаунт принадлежит сотруднику",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=10,
                },
            };

            AccountGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>() 
            {
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        var currentStatus = row.CheckGet("LOCKED_FLAG").ToBool();
                        if (currentStatus == true)
                        {
                            color = HColor.Red;
                        }

                        var isEmployee = row.CheckGet("IS_EMPLOYEE").ToBool();
                        if (isEmployee == false)
                        {
                            //это общий аккаунт
                            color = HColor.Blue;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
            };
            AccountGrid.SetColumns(columns);
            AccountGrid.SetPrimaryKey("ID");
            AccountGrid.SetSorting("LOGIN", ListSortDirection.Ascending);
            AccountGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            AccountGrid.SearchText = AccountGridSearch;
            AccountGrid.Toolbar = AccountGridToolbar;
            AccountGrid.OnSelectItem = selectedItem =>
            {
                RoleGrid.LoadItems();
            };
            //AccountGrid.OnLoadItems = AccountGridLoadItems;
            AccountGrid.QueryLoadItems = new RequestData()
            {
                Module = "Accounts",
                Object = "Account",
                Action = "List",
                AnswerSectionKey = "ITEMS",               
            };
            AccountGrid.OnFilterItems = ()=> 
            {
                if(AccountGrid.Items.Count > 0)
                {
                    var v = Form.GetValues();
                    var accountType = v.CheckGet("ACCOUNT_TYPE").ToInt();

                    var items = new List<Dictionary<string, string>>();
                    foreach(Dictionary<string, string> row in AccountGrid.Items)
                    {
                        bool include = false;

                        switch(accountType)
                        {
                            //Общие аккаунты
                            case 1:
                                {
                                    if(row.CheckGet("IS_EMPLOYEE").ToInt() == 0)
                                    {
                                        include = true;
                                    }
                                }
                                break;

                            //Аккаунты пользователей
                            case 2:
                                {
                                    if(row.CheckGet("IS_EMPLOYEE").ToInt() == 1)
                                    {
                                        include = true;
                                    }
                                }
                                break;

                            //Уволенные пользователи
                            case 3:
                                {
                                    if(row.CheckGet("LOCKED_FLAG").ToInt() == 1)
                                    {
                                        include = true;
                                    }
                                }
                                break;

                            //Все
                            case 0:
                            default:
                                {
                                    include = true;
                                }
                                break;
                        }

                        if(include)
                        {
                            items.Add(row);
                        }

                    }
                    AccountGrid.Items = items;
                }
            };
            AccountGrid.Commands = Commander;
            AccountGrid.Init();            
        }

        public void FormInit()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="ROLE_GROUPS",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Default="0",
                    Control=RoleGroup,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange=(FormHelperField f, string v)=>
                    {
                        RoleGrid.UpdateItems();
                    },
                    QueryLoadItems = new RequestData()
                    {
                        Module = "Accounts",
                        Object = "Role",
                        Action = "List",
                        AnswerSectionKey="ROLE_GROUPS",
                        OnComplete = (FormHelperField f,ListDataSet ds) =>
                        {
                            var row = new Dictionary<string, string>()
                            {
                                {"ID", "0" },
                                {"NAME", "Все" },
                            };
                            ds.ItemsPrepend(row);
                            var list=ds.GetItemsList("ID","NAME");
                            var c=(SelectBox)f.Control;
                            if(c != null)
                            {
                                c.Items=list;
                            }
                        },
                    },
                },
                new FormHelperField()
                {
                    Path="SEARCH",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=AccountGridSearch,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
                new FormHelperField()
                {
                    Path="ACCOUNT_TYPE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Default="0",
                    Control=AccountType,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange=(FormHelperField f, string v)=>
                    {
                        AccountGrid.UpdateItems();
                    },
                    OnCreate = (FormHelperField f) =>
                    {
                        var list = new Dictionary<string, string>();
                        list.Add("0", "Все");
                        list.Add("1", "Общие аккаунты");
                        list.Add("2", "Аккаунты сотрудников");
                        list.Add("3", "Заблокированные аккаунты");

                        var c=(SelectBox)f.Control;
                        if(c != null)
                        {
                            c.Items=list;
                            //c.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");
                        }
                    },
                },
            };
            Form.SetFields(fields);
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
                },  
                new DataGridHelperColumn
                {
                    Header="Доступ",
                    Path="_MODE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn
                {
                    Header="Группа",
                    Path="ROGR_NAME",
                    Doc="Группа ролей",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="NAME",
                    Doc="Наименование роли",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn
                {
                    Header="Код",
                    Path="CODE",
                    Doc="Код роли",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
            };
            RoleGrid.SetColumns(columns);
            RoleGrid.SetPrimaryKey("ID");
            RoleGrid.SetSorting("_ROWNUMBER",ListSortDirection.Ascending);
            RoleGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            RoleGrid.SearchText = RoleGridSearch;
            RoleGrid.Toolbar=RoleGridToolbar;
            RoleGrid.ItemsAutoUpdate=false;
            RoleGrid.Commands = Commander;
            RoleGrid.OnLoadItems = _RoleGridLoadItems;
            /*
            RoleGrid.QueryLoadItems = new FormDialog.RequestData()
            {
                Module = "Accounts",
                Object = "Role",
                Action = "List",
                AnswerSectionKey = "ITEMS",
                BeforeRequest = (qli) =>
                {
                    int accountId = AccountGrid.SelectedItem.CheckGet("ID").ToInt();
                    qli.Params.Add("ACCOUNT_ID", accountId.ToString());
                },
            };
            */

            RoleGrid.OnFilterItems = ()=>
            {
                var v = Form.GetValues();
                var roleGroupID = v.CheckGet("ROLE_GROUPS").ToInt();

                if(RoleGrid.Items.Count > 0)
                {
                    if(roleGroupID != 0)
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach(Dictionary<string, string> row in RoleGrid.Items)
                        {
                            if(row.CheckGet("ROGR_ID").ToInt() == roleGroupID)
                            {
                                items.Add(row);
                            }
                            RoleGrid.Items = items;
                        }
                    }
                }
            };
            RoleGrid.Init();
        }

        public async void _AccountGridLoadItems()
        {
            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Accounts");
                q.Request.SetParam("Object", "Account");
                q.Request.SetParam("Action", "List");
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
                            AccountGrid.UpdateItems(ds);
                        }
                    }
                }
            }
        }

        public async void _RoleGridLoadItems()
        {
            bool resume = true;

            if (resume)
            {
                int accountId = AccountGrid.SelectedItem.CheckGet("ID").ToInt();
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ACCOUNT_ID", accountId.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Accounts");
                q.Request.SetParam("Object", "Role");
                q.Request.SetParam("Action", "List");
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

                            var modeList = Acl.GetAccessModeList();
                            ds.Items = ListDataSet.AddColumnToList(ds.Items, "DATA_ACCESS_MODE", "_MODE_NAME", modeList);

                            RoleGrid.UpdateItems(ds);

                        }
                    }
                }
            }
        }

        private async void RoleSet(int mode=1)
        {
            var resume=true;
            bool complete=false;
            var p = new Dictionary<string, string>();

            if(resume)
            {
                var roleID = RoleGrid.SelectedItem.CheckGet("ID").ToInt();    
                var accountId = AccountGrid.SelectedItem.CheckGet("ID").ToInt();

                if(accountId != 0 && roleID != 0)
                {
                    p.CheckAdd("ROLE_ID", roleID.ToString());    
                    p.CheckAdd("ACCO_ID", accountId.ToString());                    
                }
                else
                {
                    resume=false;
                }
            }

            p.CheckAdd("MODE", mode.ToString());

            var action = "GrantRole";
            if(mode==0)
            {
                action = "RevokeRole";
            }
          
            if(resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Accounts");
                q.Request.SetParam("Object", "Account");
                q.Request.SetParam("Action", action); 

                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        var operationResult=ds.GetFirstItemValueByKey("RESULT").ToInt();
                        if(operationResult == 1)
                        {
                            complete=true;
                        }
                    }
                }

                if(complete)
                {
                   RoleGrid.LoadItems();
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        public async void RoleList()
        {
            bool resume = true;
            
            var login=AccountGrid.SelectedItem.CheckGet("LOGIN");

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("LOGIN", login);
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Accounts");
                q.Request.SetParam("Object", "Role");
                q.Request.SetParam("Action", "ListAll");
                q.Request.SetParams(p);

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
                            var ds = ListDataSet.Create(result, "ROLES");
                            if(ds.Items.Count > 0)
                            {
                                var s="";
                                foreach(Dictionary<string, string> row in ds.Items)
                                {
                                    var line = $"{row.CheckGet("CODE").Truncate(32).SPadLeft(32)} mode=[{row.CheckGet("MODE").SPadLeft(2)}] source=[{row.CheckGet("SOURCE").SPadLeft(1)}]";
                                    line = line.Trim();
                                    s = s.Append(line, true);
                                }

                                var e = new LogWindow(s, "Роли пользователя");
                                e.ShowDialog();
                            }
                        }
                    }
                }
            }
        }
               
        public async void AccountSetLock(int lockedFlag)
        {
            bool resume = true;

            if (resume)
            {
                var accountID = AccountGrid.SelectedItem.CheckGet("ID").ToInt();

                var p = new Dictionary<string, string>();
                {
                    p.Add("ACCO_ID", accountID.ToString());
                    p.Add("LOCKED_FLAG", lockedFlag.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Accounts");
                q.Request.SetParam("Object", "Account");
                q.Request.SetParam("Action", "Lock");
                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    AccountGrid.LoadItems();
                }
                else
                {
                    q.ProcessError();
                }
            }
        }
    }
}
