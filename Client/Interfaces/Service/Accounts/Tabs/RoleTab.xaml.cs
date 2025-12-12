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

namespace Client.Interfaces.Accounts
{
    /// <summary>
    /// группы ролей
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-11-08</released>
    /// <changed>2024-04-08</changed>
    public partial class RoleTab : ControlBase
    {
        /*
            | RoleGrid | GroupGrid   |
            |          | AccountGrid |

            слева роли
            справа группы и аккаунты
         */
        public RoleTab()
        {
            InitializeComponent();

            ControlSection = "role";
            RoleName = "[erp]accounts";
            ControlTitle ="Роли";
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
                RoleGridInit();
                GroupGridInit();
                AccountGridInit();

                FormInit();
                SetDefaults();
            };

            OnUnload=()=>
            {
                RoleGrid.Destruct();
                GroupGrid.Destruct();
            };

            OnFocusGot=()=>
            {
                RoleGrid.ItemsAutoUpdate=true;
                RoleGrid.Run();               
            };

            OnFocusLost=()=>
            {
                RoleGrid.ItemsAutoUpdate=false;
            };

            OnNavigate = () =>
            {
                var roleCode = Parameters.CheckGet("role_code");
                if(!roleCode.IsNullOrEmpty())
                {
                    RoleGridSearch.Text = roleCode;
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
                            RoleGrid.LoadItems();
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

                
                Commander.SetCurrentGridName("RoleGrid");
                {
                    Commander.SetCurrentGroup("item");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "role_create",
                            Title = "Создать",
                            MenuUse = true,
                            HotKey = "Insert",
                            ButtonUse = true,
                            ButtonName = "RoleCreateButton",
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var i = new RoleForm();
                                i.Create();
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
                            Name = "role_edit",
                            Title = "Изменить",
                            MenuUse = true,
                            HotKey = "Return|DoubleCLick",
                            ButtonUse=true,
                            ButtonName = "RoleEditButton",
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var k = RoleGrid.GetPrimaryKey();
                                var id = RoleGrid.SelectedItem.CheckGet(k).ToInt();
                                if(id != 0)
                                {
                                    var h = new RoleForm();
                                    h.Edit(id);
                                }
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
                
                
                Commander.SetCurrentGridName("GroupGrid");
                {
                    Commander.SetCurrentGroup("item");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "group_set_mode_deny",
                            Title = "Запрещен",
                            MenuUse = true,
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                GroupGridRoleRevoke();
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = GroupGrid.GetPrimaryKey();
                                var row = GroupGrid.SelectedItem;
                                if(row.CheckGet("DATA_ACCESS_MODE").ToInt() != 0)
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });

                        Commander.Add(new CommandItem()
                        {
                            Name = "group_set_mode_ro",
                            Title = "Только чтение",
                            MenuUse = true,
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                GroupGridRoleGrant(1);
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = GroupGrid.GetPrimaryKey();
                                var row = GroupGrid.SelectedItem;
                                if(row.CheckGet("DATA_ACCESS_MODE").ToInt() != 1)
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });

                        Commander.Add(new CommandItem()
                        {
                            Name = "group_set_mode_full",
                            Title = "Полный доступ",
                            MenuUse = true,
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                GroupGridRoleGrant(2);
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = GroupGrid.GetPrimaryKey();
                                var row = GroupGrid.SelectedItem;
                                if(row.CheckGet("DATA_ACCESS_MODE").ToInt() != 2)
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });

                        Commander.Add(new CommandItem()
                        {
                            Name = "group_set_mode_special",
                            Title = "Спецправа",
                            MenuUse = true,
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                GroupGridRoleGrant(3);
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = GroupGrid.GetPrimaryKey();
                                var row = GroupGrid.SelectedItem;
                                if(row.CheckGet("DATA_ACCESS_MODE").ToInt() != 3)
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });
                    }
                }


                Commander.SetCurrentGridName("AccountGrid");
                {
                    Commander.SetCurrentGroup("item");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "account_set_mode_deny",
                            Title = "Запрещен",
                            MenuUse = true,
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                AccountGridRoleRevoke();
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = AccountGrid.GetPrimaryKey();
                                var row = AccountGrid.SelectedItem;
                                if(row.CheckGet("DATA_ACCESS_MODE").ToInt() != 0)
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });

                        Commander.Add(new CommandItem()
                        {
                            Name = "account_set_mode_ro",
                            Title = "Только чтение",
                            MenuUse = true,
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                AccountGridRoleGrant(1);
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = AccountGrid.GetPrimaryKey();
                                var row = AccountGrid.SelectedItem;
                                if(row.CheckGet("DATA_ACCESS_MODE").ToInt() != 1)
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });

                        Commander.Add(new CommandItem()
                        {
                            Name = "account_set_mode_full",
                            Title = "Полный доступ",
                            MenuUse = true,
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                AccountGridRoleGrant(2);
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = AccountGrid.GetPrimaryKey();
                                var row = AccountGrid.SelectedItem;
                                if(row.CheckGet("DATA_ACCESS_MODE").ToInt() != 2)
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });

                        Commander.Add(new CommandItem()
                        {
                            Name = "account_set_mode_special",
                            Title = "Спецправа",
                            MenuUse = true,
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                AccountGridRoleGrant(3);
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = AccountGrid.GetPrimaryKey();
                                var row = AccountGrid.SelectedItem;
                                if(row.CheckGet("DATA_ACCESS_MODE").ToInt() != 3)
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

        public void RoleGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Код",
                    Path="CODE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=24,
                },
                new DataGridHelperColumn
                {
                    Header="Описание",
                    Path="DESCRIPTION",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
            };            
            RoleGrid.SetColumns(columns);
            RoleGrid.SetPrimaryKey("ID");
            RoleGrid.SetSorting("CODE", ListSortDirection.Ascending);
            RoleGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            RoleGrid.SearchText = RoleGridSearch;
            RoleGrid.Toolbar = RoleGridToolbar;
            RoleGrid.OnSelectItem = selectedItem =>
            {
                GroupGrid.LoadItems();
                AccountGrid.LoadItems();
            };
            RoleGrid.QueryLoadItems = new RequestData()
            {
                Module = "Accounts",
                Object = "Role",
                Action = "ListNew",
                AnswerSectionKey = "ITEMS",               
            };
            RoleGrid.Commands = Commander;
            RoleGrid.Init();            
        }

        public void GroupGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2 = 6,
                },
                new DataGridHelperColumn
                {
                    Header="Доступ",
                    Path="_MODE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2 = 16,
                },
                new DataGridHelperColumn
                {
                    Header="Название",
                    Path="NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2 = 24,
                },
                new DataGridHelperColumn
                {
                    Header="Код",
                    Path="CODE",
                    ColumnType=ColumnTypeRef.String,
                    Width2 = 16,
                },
            };
            GroupGrid.SetColumns(columns);
            GroupGrid.SetPrimaryKey("ID");
            GroupGrid.SetSorting("_ROWNUMBER",ListSortDirection.Ascending);
            GroupGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            GroupGrid.SearchText = GroupGridSearch;
            GroupGrid.Toolbar= GroupGridToolbar;
            GroupGrid.ItemsAutoUpdate=false;
            GroupGrid.Commands = Commander;
            GroupGrid.QueryLoadItems = new RequestData()
            {
                Module = "Accounts",
                Object = "Group",
                Action = "List",
                AnswerSectionKey = "ITEMS",
                BeforeRequest = (RequestData rd) =>
                {
                    rd.Params = new Dictionary<string, string>()
                    {
                        { "ROLE_ID", RoleGrid.SelectedItem.CheckGet("ID") },
                    };
                },
                AfterRequest = (RequestData rd, ListDataSet ds) =>
                {
                    var modeList = Acl.GetAccessModeList();
                    ds.Items = ListDataSet.AddColumnToList(ds.Items, "DATA_ACCESS_MODE", "_MODE_NAME", modeList);
                    return ds;
                },
            };
            GroupGrid.Init();
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
                    Header="Доступ",
                    Path="_MODE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="NAME",
                    Doc="Наименование аккаунта",
                    ColumnType=ColumnTypeRef.String,
                    Width2=24,
                },
                new DataGridHelperColumn
                {
                    Header="Логин",
                    Path="LOGIN",
                    Doc="Логин",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },                   
            };
            AccountGrid.SetColumns(columns);
            AccountGrid.SetPrimaryKey("ID");
            AccountGrid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
            AccountGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            AccountGrid.SearchText = AccountGridSearch;
            AccountGrid.Toolbar = AccountGridToolbar;
            AccountGrid.ItemsAutoUpdate = false;
            AccountGrid.Commands = Commander;
            AccountGrid.QueryLoadItems = new RequestData()
            {
                Module = "Accounts",
                Object = "Account",
                Action = "ListNew",
                AnswerSectionKey = "ITEMS",
                BeforeRequest = (RequestData rd) =>
                {
                    rd.Params = new Dictionary<string, string>()
                    {
                        { "ROLE_ID", RoleGrid.SelectedItem.CheckGet("ID") },
                    };
                },
                AfterRequest = (RequestData rd, ListDataSet ds) =>
                {
                    var modeList = Acl.GetAccessModeList();
                    ds.Items = ListDataSet.AddColumnToList(ds.Items, "DATA_ACCESS_MODE", "_MODE_NAME", modeList);
                    return ds;
                },
            };
            AccountGrid.Init();
        }


        public async void GroupGridRoleGrant(int mode)
        {
            bool resume = true;
            bool complete = false;
            var p = new Dictionary<string, string>();

            if(resume)
            {
                var roleID = RoleGrid.SelectedItem.CheckGet("ID").ToInt();
                var wogrId = GroupGrid.SelectedItem.CheckGet("ID").ToInt();

                if(wogrId != 0 && roleID != 0)
                {
                    p.CheckAdd("ROLE_ID", roleID.ToString());
                    p.CheckAdd("WOGR_ID", wogrId.ToString());
                }
                else
                {
                    resume = false;
                }
            }

            p.CheckAdd("MODE", mode.ToString());

            if(resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Accounts");
                q.Request.SetParam("Object", "Group");
                q.Request.SetParam("Action", "GrantRole");

                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });


                if(q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if(result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        var operationResult = ds.GetFirstItemValueByKey("RESULT").ToInt();
                        if(operationResult == 1)
                        {
                            complete = true;
                        }
                    }
                }

                if(complete)
                {
                    ////обновление меню
                    //Messenger.Default.Send(new ItemMessage()
                    //{
                    //    ReceiverGroup = "Main",
                    //    SenderName = "RoleList",
                    //    Action = "UpdateMenu",
                    //});

                    //{
                    //    var modeList = Acl.GetAccessModeList();
                    //    var v = modeList.CheckGet(mode.ToString());
                    //    GroupGrid.UpdateRowColumn("_MODE_NAME", v);
                    //}

                    GroupGrid.LoadItems();

                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        public async void GroupGridRoleRevoke(int mode = 0)
        {
            bool resume = true;
            bool complete = false;
            var p = new Dictionary<string, string>();

            if(resume)
            {
                var roleID = RoleGrid.SelectedItem.CheckGet("ID").ToInt();
                var wogrId = GroupGrid.SelectedItem.CheckGet("ID").ToInt();

                if(wogrId != 0 && roleID != 0)
                {
                    p.CheckAdd("ROLE_ID", roleID.ToString());
                    p.CheckAdd("WOGR_ID", wogrId.ToString());
                }
                else
                {
                    resume = false;
                }
            }

            if(resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Accounts");
                q.Request.SetParam("Object", "Group");
                q.Request.SetParam("Action", "RevokeRole");

                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if(q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if(result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        var operationResult = ds.GetFirstItemValueByKey("RESULT").ToInt();
                        if(operationResult == 1)
                        {
                            complete = true;
                        }
                    }
                }

                if(complete)
                {
                    ////обновление меню
                    //Messenger.Default.Send(new ItemMessage()
                    //{
                    //    ReceiverGroup = "Main",
                    //    SenderName = "RoleList",
                    //    Action = "UpdateMenu",
                    //});

                    //{
                    //    var modeList = Acl.GetAccessModeList();
                    //    var v = modeList.CheckGet(mode.ToString());
                    //    GroupGrid.UpdateRowColumn("_MODE_NAME", v);
                    //}

                    GroupGrid.LoadItems();
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        private async void AccountGridRoleGrant(int mode)
        {
            bool resume = true;
            bool complete = false;
            var p = new Dictionary<string, string>();

            if(resume)
            {
                var roleID = RoleGrid.SelectedItem.CheckGet("ID").ToInt();
                var accountID = AccountGrid.SelectedItem.CheckGet("ID").ToInt();

                if(accountID != 0 && roleID != 0)
                {
                    p.CheckAdd("ROLE_ID", roleID.ToString());
                    p.CheckAdd("ACCO_ID", accountID.ToString());
                }
                else
                {
                    resume = false;
                }
            }

            p.CheckAdd("MODE", mode.ToString());

            if(resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Accounts");
                q.Request.SetParam("Object", "Account");
                q.Request.SetParam("Action", "GrantRole");

                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if(q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if(result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        var operationResult = ds.GetFirstItemValueByKey("RESULT").ToInt();
                        if(operationResult == 1)
                        {
                            complete = true;
                        }
                    }
                }

                if(complete)
                {
                    ////обновление меню
                    //Messenger.Default.Send(new ItemMessage()
                    //{
                    //    ReceiverGroup = "Main",
                    //    SenderName = "RoleList",
                    //    Action = "UpdateMenu",
                    //});

                    //{
                    //    var modeList = Acl.GetAccessModeList();
                    //    var v = modeList.CheckGet(mode.ToString());
                    //    AccountGrid.UpdateRowColumn("_MODE_NAME", v);
                    //}
                    AccountGrid.LoadItems();
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        private async void AccountGridRoleRevoke(int mode = 0)
        {
            bool resume = true;
            bool complete = false;
            var p = new Dictionary<string, string>();

            if(resume)
            {
                var roleID = RoleGrid.SelectedItem.CheckGet("ID").ToInt();
                var accountID = AccountGrid.SelectedItem.CheckGet("ID").ToInt();

                if(accountID != 0 && roleID != 0)
                {
                    p.CheckAdd("ROLE_ID", roleID.ToString());
                    p.CheckAdd("ACCO_ID", accountID.ToString());
                }
                else
                {
                    resume = false;
                }
            }

            if(resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Accounts");
                q.Request.SetParam("Object", "Account");
                q.Request.SetParam("Action", "RevokeRole");

                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if(q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if(result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        var operationResult = ds.GetFirstItemValueByKey("RESULT").ToInt();
                        if(operationResult == 1)
                        {
                            complete = true;
                        }
                    }
                }

                if(complete)
                {
                    ////обновление меню
                    //Messenger.Default.Send(new ItemMessage()
                    //{
                    //    ReceiverGroup = "Main",
                    //    SenderName = "RoleList",
                    //    Action = "UpdateMenu",
                    //});

                    //{
                    //    var modeList = Acl.GetAccessModeList();
                    //    var v = modeList.CheckGet(mode.ToString());
                    //    AccountGrid.UpdateRowColumn("_MODE_NAME", v);
                    //}
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
