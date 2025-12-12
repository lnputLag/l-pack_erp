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
    public partial class GroupTab : ControlBase
    {
        /*
            | GroupGrid | EmployeeGrid |

            слева группы ролей
            справа сотрудники, состоящие в группе
         */
        public GroupTab()
        {
            InitializeComponent();

            ControlSection = "group";
            RoleName = "[erp]accounts";
            ControlTitle ="Группы";
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
                GroupGridInit();
                RoleGridInit();
                EmployeeGridInit();

                FormInit();
                SetDefaults();
            };

            OnUnload=()=>
            {
                GroupGrid.Destruct();
                RoleGrid.Destruct();
                EmployeeGrid.Destruct();
            };

            OnFocusGot=()=>
            {
                GroupGrid.ItemsAutoUpdate=true;
                GroupGrid.Run();               
            };

            OnFocusLost=()=>
            {
                GroupGrid.ItemsAutoUpdate=false;
            };

            OnNavigate = () =>
            {
                var groupCode = Parameters.CheckGet("group_code");
                if(!groupCode.IsNullOrEmpty())
                {
                    GroupGridSearch.Text = groupCode;
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
                            GroupGrid.LoadItems();
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

                
                Commander.SetCurrentGridName("GroupGrid");
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
                                var i = new GroupForm();
                                i.Init("create");
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = GroupGrid.GetPrimaryKey();
                                var row = GroupGrid.SelectedItem;
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
                                var k = GroupGrid.GetPrimaryKey();
                                var id = GroupGrid.SelectedItem.CheckGet(k).ToInt();
                                if(id != 0)
                                {
                                    var h = new GroupForm();
                                    h.Init("edit", id.ToString());
                                }
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = GroupGrid.GetPrimaryKey();
                                var row = GroupGrid.SelectedItem;
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
                            Name = "role_set_mode_deny",
                            Title = "Запрещен",
                            MenuUse = true,
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                RoleGridGroupRevoke();
                            },
                            CheckEnabled = () =>
                            {
                                return RoleGrid.SelectedItem.CheckGet("DATA_ACCESS_MODE").ToInt() != 0;
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "role_set_mode_ro",
                            Title = "Только чтение",
                            MenuUse = true,
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                RoleGridGroupGrant(1);
                            },
                            CheckEnabled = () =>
                            {
                                return RoleGrid.SelectedItem.CheckGet("DATA_ACCESS_MODE").ToInt() != 1;
                            },
                        });

                        Commander.Add(new CommandItem()
                        {
                            Name = "role_set_mode_full",
                            Title = "Полный доступ",
                            MenuUse = true,
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                RoleGridGroupGrant(2);
                            },
                            CheckEnabled = () =>
                            {
                                return RoleGrid.SelectedItem.CheckGet("DATA_ACCESS_MODE").ToInt() != 2;
                            },
                        });

                        Commander.Add(new CommandItem()
                        {
                            Name = "role_set_mode_special",
                            Title = "Спецправа",
                            MenuUse = true,
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                RoleGridGroupGrant(3);
                            },
                            CheckEnabled = () =>
                            {
                                return RoleGrid.SelectedItem.CheckGet("DATA_ACCESS_MODE").ToInt() != 3;
                            },
                        });

                    }
                }

                Commander.SetCurrentGridName("EmployeeGrid");
                {
                    Commander.SetCurrentGroup("item");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "employee_group_add",
                            Title = "Добавить в группу",
                            MenuUse = true,
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                EmployeeGroupSet(1);
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = EmployeeGrid.GetPrimaryKey();
                                var row = EmployeeGrid.SelectedItem;
                                if(!row.CheckGet("INGROUP").ToBool())
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "employee_group_remove",
                            Title = "Исключить из группы",
                            MenuUse = true,
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                EmployeeGroupSet(0);
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = EmployeeGrid.GetPrimaryKey();
                                var row = EmployeeGrid.SelectedItem;
                                if(row.CheckGet("INGROUP").ToBool())
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

        public void GroupGridInit()
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
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="Название",
                    Path="NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="Описание",
                    Path="DESCRIPTION",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
            };            
            GroupGrid.SetColumns(columns);
            GroupGrid.SetPrimaryKey("ID");
            GroupGrid.SetSorting("CODE", ListSortDirection.Ascending);
            GroupGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            GroupGrid.SearchText = GroupGridSearch;
            GroupGrid.Toolbar = GroupGridToolbar;
            GroupGrid.OnSelectItem = selectedItem =>
            {
                RoleGrid.LoadItems();
                EmployeeGrid.LoadItems();
            };
            GroupGrid.QueryLoadItems = new RequestData()
            {
                Module = "Accounts",
                Object = "Group",
                Action = "List",
                AnswerSectionKey = "ITEMS",               
            };
            GroupGrid.Commands = Commander;
            GroupGrid.Init();
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
            RoleGrid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
            RoleGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            RoleGrid.SearchText = RoleGridSearch;
            RoleGrid.Toolbar = RoleGridToolbar;
            RoleGrid.ItemsAutoUpdate = false;
            RoleGrid.Commands = Commander;
            RoleGrid.QueryLoadItems = new RequestData()
            {
                Module = "Accounts",
                Object = "Role",
                Action = "ListForGroup",
                AnswerSectionKey = "ITEMS",
                BeforeRequest = (RequestData rd) =>
                {
                    rd.Params = new Dictionary<string, string>()
                    {
                        { "WOGR_ID", GroupGrid.SelectedItem.CheckGet("ID") },
                    };
                },
                AfterRequest = (RequestData rd, ListDataSet ds) =>
                {
                    var modeList = Acl.GetAccessModeList();
                    ds.Items = ListDataSet.AddColumnToList(ds.Items, "DATA_ACCESS_MODE", "_MODE_NAME", modeList);
                    return ds;
                },
            };
            RoleGrid.Init();
        }

        public void EmployeeGridInit()
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
                    Header="Состоит в группе",
                    Path="INGROUP",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Сотрудник",
                    Path="FIO",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },               
            };
            EmployeeGrid.SetColumns(columns);
            EmployeeGrid.SetPrimaryKey("ID");
            EmployeeGrid.SetSorting("_ROWNUMBER",ListSortDirection.Ascending);
            EmployeeGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            EmployeeGrid.SearchText = EmployeeGridSearch;
            EmployeeGrid.Toolbar=EmployeeGridToolbar;
            EmployeeGrid.ItemsAutoUpdate=false;
            EmployeeGrid.Commands = Commander;
            EmployeeGrid.QueryLoadItems = new RequestData()
            {
                Module = "Accounts",
                Object = "User",
                Action = "ListNew",
                AnswerSectionKey = "ITEMS",
                BeforeRequest = (RequestData rd) =>
                {
                    rd.Params = new Dictionary<string, string>()
                    {
                        { "WOGR_ID", GroupGrid.SelectedItem.CheckGet("ID") },
                    };
                }
            };
            EmployeeGrid.Init();
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
                            GroupGrid.UpdateItems(ds);
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
                int accountId = GroupGrid.SelectedItem.CheckGet("ID").ToInt();
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

                            EmployeeGrid.UpdateItems(ds);

                        }
                    }
                }
            }
        }

        public async void RoleGridGroupGrant(int mode)
        {
            bool resume = true;
            bool complete = false;
            var p = new Dictionary<string, string>();

            if (resume)
            {
                var roleID = RoleGrid.SelectedItem.CheckGet("ID").ToInt();
                var wogrId = GroupGrid.SelectedItem.CheckGet("ID").ToInt();

                if (wogrId != 0 && roleID != 0)
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

            if (resume)
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


                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        var operationResult = ds.GetFirstItemValueByKey("RESULT").ToInt();
                        if (operationResult == 1)
                        {
                            complete = true;
                        }
                    }
                }

                if (complete)
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

        public async void RoleGridGroupRevoke(int mode = 0)
        {
            bool resume = true;
            bool complete = false;
            var p = new Dictionary<string, string>();

            if (resume)
            {
                var roleID = RoleGrid.SelectedItem.CheckGet("ID").ToInt();
                var wogrId = GroupGrid.SelectedItem.CheckGet("ID").ToInt();

                if (wogrId != 0 && roleID != 0)
                {
                    p.CheckAdd("ROLE_ID", roleID.ToString());
                    p.CheckAdd("WOGR_ID", wogrId.ToString());
                }
                else
                {
                    resume = false;
                }
            }

            if (resume)
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

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        var operationResult = ds.GetFirstItemValueByKey("RESULT").ToInt();
                        if (operationResult == 1)
                        {
                            complete = true;
                        }
                    }
                }

                if (complete)
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

        public void EmployeeGroupSet(int n)
        {
            if(n == 0)
            {
                RemoveUserFromGroup();
            }
            else if(n == 1)
            {
                AddUserToGroup();
            }
        }

        public async void RemoveUserFromGroup()
        {
            {
                var emplId = EmployeeGrid.SelectedItem.CheckGet("ID");
                var wogrId = GroupGrid.SelectedItem.CheckGet("ID");

                if(emplId.ToInt() > 0 && wogrId.ToInt() > 0)
                {
                    var p = new Dictionary<string, string>();
                    {
                        p.CheckAdd("EMPL_ID", emplId.ToString());
                        p.CheckAdd("WOGR_ID", wogrId.ToString());
                    }

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Accounts");
                    q.Request.SetParam("Object", "User");
                    q.Request.SetParam("Action", "RemoveGroup");
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
                            if(result.ContainsKey("ITEMS"))
                            {
                                EmployeeGrid.LoadItems();
                            }
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
            }
        }

        public async void AddUserToGroup()
        {
            {
                var emplId = EmployeeGrid.SelectedItem.CheckGet("ID");
                var wogrId = GroupGrid.SelectedItem.CheckGet("ID");

                if(emplId.ToInt() > 0 && wogrId.ToInt() > 0)
                {
                    var p = new Dictionary<string, string>();
                    {
                        p.CheckAdd("EMPL_ID", emplId.ToString());
                        p.CheckAdd("WOGR_ID", wogrId.ToString());
                    }

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Accounts");
                    q.Request.SetParam("Object", "User");
                    q.Request.SetParam("Action", "AddGroup");
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
                            if(result.ContainsKey("ITEMS"))
                            {
                                EmployeeGrid.LoadItems();
                            }
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
            }
        }

    }
}
