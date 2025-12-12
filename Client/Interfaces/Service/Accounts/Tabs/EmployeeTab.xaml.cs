using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Debug;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
using Client.Interfaces.Production;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using Xceed.Wpf.Toolkit.Primitives;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static Client.Interfaces.Main.FormDialog;

namespace Client.Interfaces.Accounts
{
    /// <summary>
    /// сотрудники
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2022-06-02</released>
    /// <changed>2023-11-09</changed>
    public partial class EmployeeTab:ControlBase
    {
        public EmployeeTab()
        {
            InitializeComponent();

            ControlSection = "employee";
            RoleName = "[erp]accounts";
            ControlTitle = "Сотрудники"; 
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
                EmployeeGridInit();
                FormInit();
                GroupGridInit();
                SetDefaults();
            };

            OnUnload=()=>
            {
                EmployeeGrid.Destruct();
                GroupGrid.Destruct();
            };

            OnFocusGot=()=>
            {
                EmployeeGrid.ItemsAutoUpdate=true;
                EmployeeGrid.Run();
            };

            OnFocusLost=()=>
            {
                EmployeeGrid.ItemsAutoUpdate=false;
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
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить данные",
                        ButtonUse = true,
                        ButtonName = "RefreshButton",
                        MenuUse = true,
                        Action = () =>
                        {
                            EmployeeGrid.LoadItems();
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

                Commander.SetCurrentGridName("EmployeeGrid");
                {
                    Commander.SetCurrentGroup("item");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "employee_create",
                            Title = "Создать",
                            MenuUse = true,
                            HotKey = "Insert",
                            ButtonUse= true,
                            ButtonName = "CreateButton",
                            AccessLevel=Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var i = new EmployeeForm();
                                i.Create();
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = EmployeeGrid.GetPrimaryKey();
                                var row = EmployeeGrid.SelectedItem;
                                if(row.CheckGet(k).ToInt() != 0)
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "employee_edit",
                            Title = "Изменить",
                            MenuUse = true,
                            HotKey = "Return|DoubleCLick",
                            ButtonUse = true,
                            ButtonName = "EditButton",
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var k = EmployeeGrid.GetPrimaryKey();
                                var id = EmployeeGrid.SelectedItem.CheckGet("ID").ToInt();
                                if(id != 0)
                                {
                                    var i = new EmployeeForm();
                                    i.Edit(id);
                                }
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = EmployeeGrid.GetPrimaryKey();
                                var row = EmployeeGrid.SelectedItem;
                                if(row.CheckGet(k).ToInt() != 0)
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "employee_create_account",
                            Title = "Создать аккаунт",
                            MenuUse = true,
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var id = EmployeeGrid.SelectedItem.CheckGet("ID").ToInt();
                                if (id != 0)
                                {
                                    CreateAccount(EmployeeGrid.SelectedItem);
                                }
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = EmployeeGrid.GetPrimaryKey();
                                var row = EmployeeGrid.SelectedItem;
                                if (row.CheckGet(k).ToInt() != 0)
                                {
                                    if (!row.CheckGet("IS_ACCOUNT").ToBool() && !row.CheckGet("LOCKED").ToBool())
                                    {
                                        result = true;
                                    }
                                }
                                return result;
                            },
                        });
                    }

                    Commander.SetCurrentGroup("block");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "employee_block",
                            Title = "Блокировать",
                            MenuUse = true,
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                EmployeeLock();
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = EmployeeGrid.GetPrimaryKey();
                                var row = EmployeeGrid.SelectedItem;
                                if(!row.CheckGet("LOCKED").ToBool())
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });
                    }

                    //Commander.SetCurrentGroup("link");
                    //{
                    //    Commander.Add(new CommandItem()
                    //    {
                    //        Name = "goto_position",
                    //        Title = "Перейти к должности",
                    //        MenuUse = true,
                    //        AccessLevel = Common.Role.AccessMode.AllowAll,
                    //        Action = () =>
                    //        {
                    //            var positionId = EmployeeGrid.SelectedItem.CheckGet("POSITION_ID").ToInt();
                    //            var url = $"l-pack://l-pack_erp/service/accounts/department?position_id={positionId}";
                    //            Central.Navigator.ProcessURL(url);
                    //        },
                    //        CheckEnabled = () =>
                    //        {
                    //            var result = false;
                    //            var k = EmployeeGrid.GetPrimaryKey();
                    //            var row = EmployeeGrid.SelectedItem;
                    //            if(row.CheckGet("POSITION_ID").ToInt() != 0)
                    //            {
                    //                result = true;
                    //            }
                    //            return result;
                    //        },
                    //    });
                    //    Commander.Add(new CommandItem()
                    //    {
                    //        Name = "goto_account",
                    //        Title = "Перейти к аккаунту",
                    //        MenuUse = true,
                    //        AccessLevel = Common.Role.AccessMode.AllowAll,
                    //        Action = () =>
                    //        {
                    //            var login = EmployeeGrid.SelectedItem.CheckGet("LOGIN");
                    //            var url = $"l-pack://l-pack_erp/service/accounts/account?login={login}";
                    //            Central.Navigator.ProcessURL(url);
                    //        },
                    //        CheckEnabled = () =>
                    //        {
                    //            var result = false;
                    //            var k = EmployeeGrid.GetPrimaryKey();
                    //            var row = EmployeeGrid.SelectedItem;
                    //            if(!row.CheckGet("LOGIN").IsNullOrEmpty())
                    //            {
                    //                result = true;
                    //            }
                    //            return result;
                    //        },
                    //    });
                    //}
                }

                Commander.SetCurrentGridName("GroupGrid");
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
                                EmployeeAddGroup();
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = GroupGrid.GetPrimaryKey();
                                var row = GroupGrid.SelectedItem;
                                if(!row.CheckGet("IN_GROUP").ToBool())
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
                                EmployeeRemoveGroup();
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = GroupGrid.GetPrimaryKey();
                                var row = GroupGrid.SelectedItem;
                                if(row.CheckGet("IN_GROUP").ToBool())
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
                            Name = "goto_group",
                            Title = "Перейти к группе",
                            MenuUse = true,
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var groupCode = GroupGrid.SelectedItem.CheckGet("CODE");
                                var url = $"l-pack://l-pack_erp/service/accounts/group?group_code={groupCode}";
                                Central.Navigator.ProcessURL(url);
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = GroupGrid.GetPrimaryKey();
                                var row = GroupGrid.SelectedItem;
                                if(!row.CheckGet("CODE").IsNullOrEmpty())
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
                    Header="Уволен",
                    Path="LOCKED",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2 = 8,
                },
                new DataGridHelperColumn
                {
                    Header="Аккаунт",
                    Path="IS_ACCOUNT",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2 = 8,
                },
                new DataGridHelperColumn
                {
                    Header="Фамилия",
                    Path="SURNAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2 = 12,
                },
                new DataGridHelperColumn
                {
                    Header="Имя",
                    Path="NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2 = 12,
                },
                new DataGridHelperColumn
                {
                    Header="Отчество",
                    Path="MIDDLE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2 = 12,
                },
                new DataGridHelperColumn
                {
                    Header="Логин",
                    Path="LOGIN",
                    ColumnType=ColumnTypeRef.String,
                    Width2 = 10,
                },
                new DataGridHelperColumn
                {
                    Header="Внутренний телефон",
                    Path="INNER_PHONE",
                    ColumnType=ColumnTypeRef.String,
                    Width2 = 4,
                },
                new DataGridHelperColumn
                {
                    Header="Мобильный телефон",
                    Path="MOBILE_PHONE",
                    ColumnType=ColumnTypeRef.String,
                    Width2 = 11,
                },
                new DataGridHelperColumn
                {
                    Header="E-Mail",
                    Path="EMAIL",
                    ColumnType=ColumnTypeRef.String,
                    Width2 = 16,
                },
                new DataGridHelperColumn
                {
                    Header="Отдел",
                    Path="DEPARTMENT_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2 = 30,
                },
                new DataGridHelperColumn
                {
                    Header="Должность",
                    Path="POSITION_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2 = 30,
                },
            };
            EmployeeGrid.SetColumns(columns);
            EmployeeGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";
                        var currentStatus = row.CheckGet("LOCKED").ToBool();

                        if (currentStatus == true)
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
            };
            EmployeeGrid.SetPrimaryKey("ID");
            EmployeeGrid.SetSorting("SURNAME", ListSortDirection.Ascending);
            EmployeeGrid.SearchText = UserGridSearch;
            EmployeeGrid.Toolbar=UserGridToolbar;
            EmployeeGrid.Commands = Commander;
            EmployeeGrid.ColumnWidthMode= GridBox.ColumnWidthModeRef.Compact;
            EmployeeGrid.QueryLoadItems = new RequestData() 
            {
                Module = "Accounts",
                Object = "User",
                Action = "List",
                AnswerSectionKey="ITEMS",
            };
            EmployeeGrid.OnFilterItems = () =>
            {
                if(EmployeeGrid.Items.Count > 0)
                {
                    {
                        var showAll = false;
                        var v = Form.GetValues();
                        var departmentId = v.CheckGet("DEPARTMENT").ToInt();

                        if(v.CheckGet("SHOW_ALL").ToBool())
                        {
                            showAll = true;
                        }

                        var items = new List<Dictionary<string, string>>();
                        foreach(Dictionary<string, string> row in EmployeeGrid.Items)
                        {
                            var includeRowByLockedFlag = false;
                            var includeRowByDepartment = false;

                            if(showAll)
                            {
                                includeRowByLockedFlag = true;
                            }
                            else
                            {
                                if(row.CheckGet("LOCKED").ToInt() == 0)
                                {
                                    includeRowByLockedFlag = true;
                                }
                            }

                            if(departmentId != 0)
                            {
                                if(row.CheckGet("DEPARTMENT_ID").ToInt() == departmentId)
                                {
                                    includeRowByDepartment = true;
                                }
                            }
                            else
                            {
                                includeRowByDepartment = true;
                            }

                            if(
                                includeRowByLockedFlag
                                && includeRowByDepartment
                            )
                            {
                                items.Add(row);
                            }
                        }
                        EmployeeGrid.Items = items;
                    }
                }
            };
            EmployeeGrid.Commands = Commander;
            EmployeeGrid.OnSelectItem = (row) =>
            {
                GroupGrid.LoadItems();
            };
            EmployeeGrid.Init();
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
                    Control=UserGridSearch,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
                new FormHelperField()
                {
                    Path="SHOW_ALL",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ShowAll,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
                new FormHelperField()
                {
                    Path="DEPARTMENT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Default="0",
                    Control=Department,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange=(FormHelperField f, string v)=>
                    {
                        EmployeeGrid.UpdateItems();
                    },
                    QueryLoadItems = new RequestData()
                    {
                        Module = "Accounts",
                        Object = "User",
                        Action = "List",
                        AnswerSectionKey="DEPARTMENTS",
                        OnComplete = (FormHelperField f,ListDataSet ds) =>
                        {
                            {
                                var row = new Dictionary<string, string>()
                                {
                                    {"ID", "0" },
                                    {"NAME", "Все" },
                                };
                                ds.ItemsPrepend(row);
                            }

                            {
                                var row = new Dictionary<string, string>()
                                {
                                    {"ID", "999" },
                                    {"NAME", "Никакие" },
                                };
                                ds.ItemsPrepend(row);
                            }

                            var list=ds.GetItemsList("ID","NAME");
                            var c=(SelectBox)f.Control;
                            if(c != null)
                            {
                                c.Items=list;
                            }
                        },
                    },
                },
            };

            Form.SetFields(fields);
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
                },
                new DataGridHelperColumn
                {
                    Header="Состоит в группе",
                    Path="IN_GROUP",
                    ColumnType=ColumnTypeRef.Boolean,
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
                    Header="Код",
                    Path="CODE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,                        
                },
            };
            GroupGrid.SetColumns(columns);
            GroupGrid.SetPrimaryKey("ID");
            GroupGrid.SetSorting("IN_GROUP", ListSortDirection.Descending);
            GroupGrid.ItemsAutoUpdate=false;
            GroupGrid.ColumnWidthMode= GridBox.ColumnWidthModeRef.Compact;
            GroupGrid.SearchText = GroupGridSearch;
            GroupGrid.Toolbar = GroupGridToolbar;
            GroupGrid.Commands = Commander;
            GroupGrid.QueryLoadItems = new RequestData()
            {
                Module = "Accounts",
                Object = "Group",
                Action = "ListByUser",
                BeforeRequest = (RequestData rd) =>
                {
                    rd.Params = new Dictionary<string, string>()
                    {
                        { "ID", EmployeeGrid.SelectedItem.CheckGet("ID") },
                    };
                }
            };
            GroupGrid.Init();
        }

        public async void EmployeeLock()
        {
            bool resume = true;

            var employeeID = EmployeeGrid.SelectedItem.CheckGet("ID").ToInt();
            var lockedFlag = "1";

            if (resume)
            {
                if(employeeID == 0)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("EMPL_ID", employeeID.ToString());
                    p.Add("LOCKED_FLAG", lockedFlag.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Accounts");
                q.Request.SetParam("Object", "User");
                q.Request.SetParam("Action", "Lock");
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
                        EmployeeGrid.UpdateItems(ds);
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        public async void CreateAccount(Dictionary<string, string> selectedItem)
        {
            var shortName = Tools.FioToShortName(selectedItem["SURNAME"], selectedItem["NAME"], selectedItem["MIDDLE_NAME"]);
            var login = Tools.Translit(shortName.Replace(" ", "_").Replace(".", "").ToLower());
            var pwd = "1234";

            var p = new Dictionary<string, string>();
            {
                p.Add("ID", selectedItem.CheckGet("ID"));
                p.Add("LOGIN", login);
                p.Add("LOGIN_NAME", shortName);
                p.Add("PASSWORD", pwd);
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Accounts");
            q.Request.SetParam("Object", "User");
            q.Request.SetParam("Action", "SaveAccount");
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
                    EmployeeGrid.LoadItems();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        public async void EmployeeRemoveGroup()
        {
            var accountId = EmployeeGrid.SelectedItem.CheckGet("ID");
            var groupId = GroupGrid.SelectedItem.CheckGet("ID");

            if (accountId.ToInt() > 0 && groupId.ToInt() > 0)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("EMPL_ID", accountId.ToString());
                    p.CheckAdd("WOGR_ID", groupId.ToString());
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

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                    if (result != null)
                    {
                        if (result.ContainsKey("ITEMS"))
                        {
                            GroupGrid.LoadItems();
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        public async void EmployeeAddGroup()
        {
            {
                var accountId = EmployeeGrid.SelectedItem.CheckGet("ID");
                var groupId = GroupGrid.SelectedItem.CheckGet("ID");

                if (accountId.ToInt() > 0 && groupId.ToInt() > 0)
                {
                    var p = new Dictionary<string, string>();
                    {
                        p.CheckAdd("EMPL_ID", accountId.ToString());
                        p.CheckAdd("WOGR_ID", groupId.ToString());
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

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                        if (result != null)
                        {
                            if (result.ContainsKey("ITEMS"))
                            {
                                GroupGrid.LoadItems();                              
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

        private void EmployeeBindAccount(string key, string name)
        {
            var resume = true;
            var employeeId = EmployeeGrid.SelectedItem.CheckGet("ID").ToInt();

            if (resume)
            {
                if(employeeId == 0)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                var d = new DialogWindow("Вы действительно хотите назначить пользователю " + EmployeeGrid.SelectedItem.CheckGet("FULL_NAME") + " аккаунт " + name, "Назначение аккаунта", "", DialogWindowButtons.NoYes);
                if (d.ShowDialog() != true)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Accounts");
                q.Request.SetParam("Object", "User");
                q.Request.SetParam("Action", "BindAccount");

                var p = new Dictionary<string, string>();
                p.CheckAdd("ID", employeeId.ToString());
                p.CheckAdd("ACCO_ID", key.ToString());

                q.Request.SetParams(p);

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            var id = ds.GetFirstItemValueByKey("ID").ToInt();

                            if (id != 0)
                            {
                                
                            }
                        }
                    }
                }
            }
        }

        private void ShowAll_Click(object sender, RoutedEventArgs e)
        {
            EmployeeGrid.UpdateItems();
        }
    }
}
