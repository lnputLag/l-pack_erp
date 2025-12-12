using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Accounts;
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

namespace Client.Interfaces.Test
{
    /// <summary>
    /// аккаунты
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-11-08</released>
    /// <changed>2023-11-08</changed>
    public partial class TestGrid4 : ControlBase
    {
        /*
         
            | AccountGrid | RoleGrid |

            В левом гриде показаны все записи из таблицы "аккаунты".
            При выборе одного аккаунта в правый грид подгружаются
            все роли с уровнями доступа для выбранного аккаунта.
         */
        public TestGrid4()
        {
            InitializeComponent();
            ControlTitle= "TestGrid4";

            OnMessage=(ItemMessage m)=>
            {
                if(m.ReceiverName == ControlName)
                {
                    ProcessCommand(m.Action,m);
                }
            };              

            OnLoad=()=>
            {
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

        }

        public FormHelper Form { get; set; }

        public void ProcessCommand(string command, ItemMessage m=null)
        {
            command=command.ClearCommand();
            if(!command.IsNullOrEmpty())
            {
                switch(command)
                {
                    case "account_create":
                    {
                        AccountCreate();
                    }
                        break;

                    case "account_edit":
                    {
                        AccountEdit();
                    }
                        break;

                    case "account_refresh":
                        {
                            AccountGrid.LoadItems();
                            if (m != null)
                            {
                                var id = m.Message.ToString();
                                if (!id.IsNullOrEmpty())
                                {
                                    AccountGrid.SelectRowByKey(id);
                                }
                            }
                        }
                        break;

                    case "account_export":
                    {
                        AccountGrid.ItemsExportExcel();
                    }
                        break;

                    case "account_selected_list":
                        {
                            var r = AccountGrid.GetItemsSelected();
                        }
                        break;

                    case "account_lock":
                        {
                            AccountLock(1);
                        }
                        break;

                    case "account_unlock":
                        {
                            AccountLock(0);
                        }
                        break;

                    case "role_list":
                        {
                            RoleList();
                        }
                        break;

                    case "role_refresh":
                        {
                        }
                        break;

                    case "role_deny":
                        {
                            RoleRevoke();
                        }
                        break;

                    case "role_ro":
                        {
                            RoleGrant(1);
                        }
                        break;

                    case "role_full":
                        {
                            RoleGrant(2);
                        }
                        break;

                    case "role_spec":
                        {
                            RoleGrant(3);
                        }
                        break;

                    case "help":
                    {
                        Central.ShowHelp("/doc/l-pack-erp/service/accounts/accounts");
                    }
                        break;
                }
            }
        }

        public void SetDefaults()
        {         
            Form.SetDefaults();
        }

        public void AccountGridInit()
        {
            {
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="selected",
                        Path="_SELECTED",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=10,
                        Editable=true,
                        Exportable=false,
                        OnClickAction = (row, el) =>
                        {
                            var result=false;
                            if (row.CheckGet("ID").ToInt() < 500 )
                            {
                                result=true;
                            }
                            return result;
                        },
                    },
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
                    /*
                    new DataGridHelperColumn
                    {
                        Header="Сотрудник",
                        Path="IS_EMPLOYEE",
                        Doc="Аккаунт принадлежит сотруднику",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=10,
                    },
                    */
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
                    var row = AccountGrid.SelectedItem;
                    AccountGridUpdateActions(selectedItem);
                };
                AccountGrid.OnDblClick = selectedItem =>
                {
                    ProcessCommand("account_edit");
                };
                AccountGrid.OnLoadItems = AccountGridLoadItems;
                AccountGrid.OnFilterItems = AccountGridFilterItems;
                AccountGrid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    {
                        "account_refresh",
                        new DataGridContextMenuItem()
                        {
                            Header="Обновить",
                            Action=()=>
                            {
                                ProcessCommand("account_refresh");
                            },
                        }
                    },
                    { "s1", new DataGridContextMenuItem() {
                        Header="-",
                    }},
                    {
                        "account_create",
                        new DataGridContextMenuItem()
                        {
                            Header="Создать",
                            Action=()=>
                            {
                                ProcessCommand("account_create");
                            },
                        }
                    },
                    {
                        "account_edit",
                        new DataGridContextMenuItem()
                        {
                            Header="Изменить",
                            Action=()=>
                            {
                                ProcessCommand("account_edit");
                            },
                        }
                    },
                    { "s2", new DataGridContextMenuItem() {
                        Header="-",
                    }},
                    {
                        "account_lock",
                        new DataGridContextMenuItem()
                        {
                            Header="Блокировать",
                            Action=()=>
                            {
                                ProcessCommand("account_lock");
                            },
                        }
                    },
                    {
                        "account_unlock",
                        new DataGridContextMenuItem()
                        {
                            Header="Разблокировать",
                            Action=()=>
                            {
                                ProcessCommand("account_unlock");
                            },
                        }
                    },
                    {
                        "role_list",
                        new DataGridContextMenuItem()
                        {
                            Header="Список ролей",
                            Action=()=>
                            {
                                ProcessCommand("role_list");
                            },
                        }
                    },
                    {
                        "account_export",
                        new DataGridContextMenuItem()
                        {
                            Header="Экспорт в Excel",
                            Action=()=>
                            {
                                ProcessCommand("account_export");
                            },
                        }
                    },
                    {
                        "account_selected_list",
                        new DataGridContextMenuItem()
                        {
                            Header="Список выбранных",
                            Action=()=>
                            {
                                ProcessCommand("account_selected_list");
                            },
                        }
                    },
                };
                AccountGrid.Init();
            }

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
                                c.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");
                            }
                        },
                    },
                };
                Form.SetFields(fields);
            }
        }

        public void AccountGridUpdateActions(Dictionary<string, string> selectedItem)
        {
            EditButton.IsEnabled = false;
            AccountGrid.Menu["account_edit"].Enabled = false;
            AccountGrid.Menu["account_lock"].Enabled = false;
            AccountGrid.Menu["account_unlock"].Enabled = false;

            selectedItem = AccountGrid.SelectedItem;
            if (selectedItem.Count > 0)
            {
                EditButton.IsEnabled = true;
                AccountGrid.Menu["account_edit"].Enabled = true;

                var lockedFlag = selectedItem.CheckGet("LOCKED_FLAG").ToInt();
                if (lockedFlag == 1)
                {
                    AccountGrid.Menu["account_lock"].Enabled = false;
                    AccountGrid.Menu["account_unlock"].Enabled = true;
                }
                else
                {
                    AccountGrid.Menu["account_lock"].Enabled = true;
                    AccountGrid.Menu["account_unlock"].Enabled = false;
                }

                RoleGrid.LoadItems();
            }
        }

        public void RoleGridInit()
        {
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
                     new DataGridHelperColumn
                    {
                        Header="ROGR_ID",
                        Path="ROGR_ID",
                        Doc="",
                        ColumnType=ColumnTypeRef.String,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Path="ACRO_ID",
                        Doc="",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden = true,
                    },
                   
                };
                RoleGrid.SetColumns(columns);
                RoleGrid.SetPrimaryKey("ID");
                RoleGrid.SetSorting("_ROWNUMBER",ListSortDirection.Ascending);
                RoleGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
                RoleGrid.SearchText = RoleGridSearch;
                RoleGrid.Toolbar=RoleGridToolbar;
                RoleGrid.ItemsAutoUpdate=false;
                RoleGrid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    {
                        "role_refresh",
                        new DataGridContextMenuItem()
                        {
                            Header="Обновить",
                            Action=() =>
                            {
                                ProcessCommand("role_refresh");
                            }
                        }
                    },
                    { "s1", new DataGridContextMenuItem() {
                        Header="-",
                    }},
                    {
                        "role_deny",
                        new DataGridContextMenuItem()
                        {
                            Header="Запрещен",
                            Action=() =>
                            {
                                ProcessCommand("role_deny");
                            }
                        }
                    },
                    {
                        "role_ro",
                        new DataGridContextMenuItem()
                        {
                            Header="Только чтение",
                            Action=() =>
                            {
                                ProcessCommand("role_ro");
                            }
                        }
                    },
                    {
                        "role_full",
                        new DataGridContextMenuItem()
                        {
                            Header="Полный доступ",
                            Action=() =>
                            {
                                ProcessCommand("role_full");
                            }
                        }
                    },
                    {
                        "role_spec",
                        new DataGridContextMenuItem()
                        {
                            Header="Спецправа",
                            Action=() =>
                            {
                                ProcessCommand("role_spec");
                            }
                        }
                    },
                };
                RoleGrid.OnLoadItems = RoleGridLoadItems;
                RoleGrid.OnFilterItems = RoleGridFilterItems;
                RoleGrid.Init();
            }
        }

        public async void AccountGridLoadItems()
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

        public void AccountGridFilterItems()
        {
            AccountGridUpdateActions(null);

            if (AccountGrid.Items != null)
            {
                if (AccountGrid.Items.Count > 0)
                {
                    //фильтрация строк
                    {
                        var v = Form.GetValues();
                        var accountType = v.CheckGet("ACCOUNT_TYPE").ToInt();

                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in AccountGrid.Items)
                        {
                            bool include = false;

                            switch (accountType)
                            {
                                //Общие аккаунты
                                case 1:
                                    {
                                        if (row.CheckGet("IS_EMPLOYEE").ToInt() == 0)
                                        {
                                            include = true;
                                        }
                                    }
                                    break;

                                //Аккаунты пользователей
                                case 2:
                                    {
                                        if (row.CheckGet("IS_EMPLOYEE").ToInt() == 1)
                                        {
                                            include = true;
                                        }
                                    }
                                    break;

                                //Уволенные пользователи
                                case 3:
                                    {
                                        if (row.CheckGet("LOCKED_FLAG").ToInt() == 1)
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

                            if (include)
                            {
                                items.Add(row);
                            }

                        }
                        AccountGrid.Items = items;
                    }

                }
            }

        }

        public async void RoleGridLoadItems()
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

        public void RoleGridFilterItems()
        {
            var v = Form.GetValues();
            var roleGroupID = v.CheckGet("ROLE_GROUPS").ToInt();

            if (RoleGrid.Items != null)
            {
                if (roleGroupID != 0)
                {
                    var items = new List<Dictionary<string, string>>();
                    foreach (Dictionary<string, string> row in RoleGrid.Items)
                    {
                        if (row.CheckGet("ROGR_ID").ToInt() == roleGroupID)
                        {
                            items.Add(row);
                        }
                    }
                    RoleGrid.Items = items;
                }
            }
        }

        private async void RoleGrant(int mode=1)
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
                                var m="";
                                m=$"{m}\n LOGIN=[{login}]";
                                    
                                var s="";
                                foreach(Dictionary<string, string> row in ds.Items)
                                {
                                    s=$"{s}\n {row.CheckGet("CODE").SPadLeft(32)} mode=[{row.CheckGet("MODE").SPadLeft(2)}] source=[{row.CheckGet("SOURCE").SPadLeft(1)}]";
                                }

                                var e = new DialogWindow(m, "Роли пользователя", s);
                                e.ShowDialog();
                            }
                        }

                    }
                }
            }
        }

        private async void RoleRevoke(int mode = 0)
        {
            var resume = true;
            bool complete = false;
            var p = new Dictionary<string, string>();

            if (resume)
            {
                var roleID = RoleGrid.SelectedItem.CheckGet("ID").ToInt();
                var accountId = AccountGrid.SelectedItem.CheckGet("ID").ToInt();

                if (accountId != 0 && roleID != 0)
                {
                    p.CheckAdd("ROLE_ID", roleID.ToString());
                    p.CheckAdd("ACCO_ID", accountId.ToString());
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
                q.Request.SetParam("Object", "Account");
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
                    RoleGrid.LoadItems();
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        public async void AccountLock(int lockedFlag)
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

        public void AccountCreate()
        {
            var i = new AccountForm();
            i.Create();
        }

        public void AccountEdit()
        {
            var accountID = AccountGrid.SelectedItem.CheckGet("ID").ToInt();
            if (accountID != 0)
            {
                var i = new AccountForm();
                i.Edit(accountID);
            }
        }

        private void ButtonOnClick(object sender, RoutedEventArgs e)
        {
            var b = (Button)sender;
            if (b != null)
            {
                var t = b.Tag.ToString();
                ProcessCommand(t);
            }
        }
    }
}
