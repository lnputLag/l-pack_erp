using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
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

namespace Client.Interfaces.Test
{
    public partial class TestGrid3:ControlBase
    {
        public TestGrid3()
        {
            ControlTitle="TestGrid3";

            OnMessage=(ItemMessage message)=>{
                DebugLog($"message=[{message.Message}]");
            };            

            OnLoad=()=>
            {
                InitializeComponent();

                UserGridInit();
                GroupsGridInit();
                SetDefaults();

                Messenger.Default.Register<ItemMessage>(this, _ProcessMessages);
                Central.Msg.Register(ProcessMessages);
            };

            OnUnload=()=>
            {
                Messenger.Default.Unregister<ItemMessage>(this);
                Central.Msg.UnRegister(ProcessMessages);

                UserGrid.Destruct();
                GroupsGrid.Destruct();
            };

            OnFocusGot=()=>
            {
                UserGrid.ItemsAutoUpdate=true;
                UserGrid.LoadItems();
            };

            OnFocusLost=()=>
            {
                UserGrid.ItemsAutoUpdate=false;
            };
        }

        /// <summary>
        /// данные из выбранной в гриде пользователей строки
        /// </summary>
        Dictionary<string, string> UserSelectedItem { get; set; }

        /// <summary>
        /// Основной датасет с данными для грида
        /// </summary>
        public ListDataSet GroupDataSet{ get; set; }

        /// <summary>
        /// данные из выбранной в гриде групп строки
        /// </summary>
        Dictionary<string, string> GroupSelectedItem { get; set; }

        /// <summary>
        /// ID выбранной группы ролей
        /// </summary>
        int DepartmentID { get; set; } = -1;

        /// <summary>
        /// форма с полями для фильтрации данных
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void UserGridInit()
        {
            //инициализация грида
            {
                //колонки грида
 
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=37,
                        MaxWidth=50,   
                        Width2 = 4,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Уволен",
                        Path="LOCKED",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=37,
                        MaxWidth=50,
                        Width2 = 4,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Аккаунт",
                        Path="IS_ACCOUNT",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=37,
                        MaxWidth=50,
                        Width2 = 4,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Фамилия",
                        Path="SURNAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=100,
                        Width2 = 12,
                        //OnClickAction = (row,el) =>
                        //{
                        //    //var c=(CheckBox)el;
                        //    //SetTime(row["NUM"], (bool)c.IsChecked);

                        //    {
                        //        var s=row.GetDumpString();            
                        //        //DebugLog($"CLICK {s}");
                        //        System.Diagnostics.Trace.WriteLine($"{s}");
                        //    }

                        //    return null;
                        //},
                        OnRender=(row,el)=>
                        {
                            var list=new List<Border>();
                            var s=new StackPanel();
                            s.Orientation=Orientation.Horizontal;

                            {
                                var b=new Border();
                                b.Width=15;
                                b.Height=15;
                                b.Background="#ff1A73E8".ToBrush();
                                b.HorizontalAlignment=HorizontalAlignment.Left;
                                b.VerticalAlignment=VerticalAlignment.Top;
                                b.Margin=new Thickness(0,0,5,0);
                                b.CornerRadius=new CornerRadius(7);


                                var t=new TextBlock();
                                t.Text="M";
                                t.HorizontalAlignment=HorizontalAlignment.Center;
                                t.VerticalAlignment=VerticalAlignment.Center;
                                t.Foreground="#ffffffff".ToBrush();
                                t.FontSize=10;
                            
                                b.Child=t;
                                s.Children.Add(b);
                            }

                            {
                                var b=new Border();
                                b.Width=15;
                                b.Height=15;
                                b.Background="#ffFBBC04".ToBrush();
                                b.HorizontalAlignment=HorizontalAlignment.Left;
                                b.VerticalAlignment=VerticalAlignment.Top;
                                b.Margin=new Thickness(0,0,5,0);

                                var t=new TextBlock();
                                t.Text="5";
                                t.HorizontalAlignment=HorizontalAlignment.Center;
                                t.VerticalAlignment=VerticalAlignment.Center;
                                t.Foreground="#ff000000".ToBrush();
                                t.FontSize=10;
                            
                                b.Child=t;
                                s.Children.Add(b);
                            }
                            
                            {
                                var b=new Border();
                                b.Child=s;
                                list.Add(b);
                            }
                            
                            return list;
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Имя",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=100,
                        Width2 = 12,

                        //Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        //{
                        //    {
                        //        StylerTypeRef.BackgroundColor,
                        //        (Dictionary<string, string> row) =>
                        //        {
                        //            var result=DependencyProperty.UnsetValue;
                        //            var color = "";

                        //            color = HColor.Green;

                        //            if (!string.IsNullOrEmpty(color))
                        //            {
                        //                result=color.ToBrush();
                        //            }

                        //            return result;
                        //        }
                        //    },
                        //},
                    },
                    new DataGridHelperColumn
                    {
                        Header="Отчество",
                        Path="MIDDLE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=100,
                        Width2 = 12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Логин",
                        Path="LOGIN",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=150,
                        Width2 = 10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Внутренний телефон",
                        Path="INNER_PHONE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=50,
                        Width2 = 4,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Мобильный телефон",
                        Path="MOBILE_PHONE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=120,
                        Width2 = 11,
                    },
                    new DataGridHelperColumn
                    {
                        Header="E-Mail",
                        Path="EMAIL",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=150,
                        Width2 = 16,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Отдел",
                        Path="DEPARTMENT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 120,
                        Width2 = 30,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Должность",
                        Path="POSITION_NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 120,
                        Width2 = 30,
                    },
                    
                    new DataGridHelperColumn
                    {
                        Path="ACCO_ID",
                        Doc="Идентификатор аккаунта",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Path="DEPARTMENT_ID",
                        Doc="Идентификатор отдела",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden = true,
                    },
                };
                UserGrid.SetColumns(columns);

                UserGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
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
                
                UserGrid.SetPrimaryKey("ID");
                UserGrid.SetSorting("SURNAME", ListSortDirection.Ascending);
                UserGrid.SearchText = Search;
                UserGrid.Name="user_list2";
                UserGrid.ColumnWidthMode= GridBox.ColumnWidthModeRef.Compact;
                
                UserGrid.OnLoadItems = UserGridLoadItems;
                UserGrid.OnFilterItems = UserGridFilterItems;
                UserGrid.OnSelectItem = (row) =>
                {
                    UserGridUpdateActions(row);                    
                };
                UserGrid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    {
                        "Item1",
                        new DataGridContextMenuItem()
                        {
                            Header="Отметить увольнение",
                            Action=()=>
                            {
                                LockUser();
                            },
                        }
                    },
                    {
                        "Item2",
                        new DataGridContextMenuItem()
                        {
                            Header="Привязать аккаунт",
                            
                        }
                    },
                };
                UserGrid.ItemsAutoUpdate=true;
                UserGrid.Init();
                /*
                UserGrid.OnViewItem = (row,cols) =>
                {
                    var result="";
                    
                    foreach(DataGridHelperColumn c in cols)
                    {
                        var k=$"{c.Header}".SPadRight(10);
                        var v=$"{row.CheckGet(c.Path)}".SPadLeft(20);
                        result=result.Append($"{k}: {v}",true);
                    }
                    return result;
                };
                */

                
            }

            //инициализация формы
            {
                Form = new FormHelper();

                //колонки формы
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
                    },

                };

                Form.SetFields(fields);

                //после установки значений
                Form.AfterSet = (Dictionary<string, string> v) =>
                {
                    //фокус на кнопку обновления
                    RefreshButton.Focus();
                };

                {
                    Department.OnSelectItem = (Dictionary<string, string> selectedItem) =>
                    {
                        bool result = true;
                        if (selectedItem.Count > 0)
                        {
                            DepartmentID = selectedItem.CheckGet("ID").ToInt();
                            UserGrid.UpdateItems();

                        }
                        return result;
                    };
                }

            }

            //фокус ввода           
            UserGrid.Focus();
        }

        private void AttachAccount()
        {
            throw new NotImplementedException();
        }

        public void GroupsGridInit()
        {
            //Инициализация грида
            {
                //колонки грида

                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=37,
                        MaxWidth=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Состоит в группе",
                        Path="INGROUP",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=37,
                        MaxWidth=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Название",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 150,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Код",
                        Path="CODE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 150,
                    },
                    new DataGridHelperColumn
                    {
                        Header=" ",
                        Path="_",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=2,
                        MaxWidth=2000,
                    },
                };
                GroupsGrid.SetColumns(columns);

                //GroupsGrid.SetSorting("NAME", ListSortDirection.Ascending);
                GroupsGrid.SearchText = SearchGroup;
                GroupsGrid.UseRowHeader = false;

                //контекстное меню
                GroupsGrid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    {
                        "Item1",
                        new DataGridContextMenuItem()
                        {
                            Header="Добавить в группу",
                            Action=()=>
                            {
                                AddUserToGroup();
                            },
                        }
                    },
                    {
                        "Item2",
                        new DataGridContextMenuItem()
                        {
                            Header="Исключить из группы",
                            Action=()=>
                            {
                                RemoveUserFromGroup();
                            },
                        }
                    },
                };

                GroupsGrid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                GroupsGrid.OnSelectItem = selectedItem =>
                {
                    GroupsGridUpdateActions(selectedItem);
                };

               
                //данные грида
                GroupsGrid.OnLoadItems = GroupsGridLoadItems;
               // GroupsGrid.OnFilterItems = FilterItems;
                GroupsGrid.Run();
            }

            //фокус ввода           
            GroupsGrid.Focus();
        }

        public async void RemoveUserFromGroup()
        {
            {
                var accountId = UserSelectedItem.CheckGet("ID");
                var groupId = GroupSelectedItem.CheckGet("ID");

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
                                GroupsGrid.LoadItems();
                                {
                                    //отправляем сообщение гриду о необходимости обновить данные
                                    Messenger.Default.Send(new ItemMessage()
                                    {
                                        ReceiverGroup = "User",
                                        SenderName = "GroupList",
                                        Action = "Refresh",
                                    });
                                }
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
                var accountId = UserSelectedItem.CheckGet("ID");
                var groupId = GroupSelectedItem.CheckGet("ID");

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
                                GroupsGrid.LoadItems();
                                {
                                    //отправляем сообщение гриду о необходимости обновить данные
                                    Messenger.Default.Send(new ItemMessage()
                                    {
                                        ReceiverGroup = "User",
                                        SenderName = "GroupList",
                                        Action = "Refresh",
                                    });
                                }
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

        /// <summary>
        /// получение записей
        /// </summary>
        public async void GroupsGridLoadItems()
        {
            DisableControls();

            if(UserSelectedItem != null)
            {
                if (GroupDataSet != null)
                { 
                    var employeeID = UserSelectedItem.CheckGet("ID").ToString();

                    var p = new Dictionary<string, string>();
                    p.Add("ID", employeeID);

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Accounts");
                    q.Request.SetParam("Object", "Group");
                    //FIXME: rename action
                    q.Request.SetParam("Action", "GroupListByUser");
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
                            var employeeGroups = ListDataSet.Create(result, "ITEMS");
                            
                            // изначальный список групп копируется
                            var resultDataSet = GroupDataSet.Clone();

                            // выставить чекбоксы там где id группы совпадает
                            foreach (var item in employeeGroups.Items)
                            {
                                int groupId = item.CheckGet("WOGR_ID").ToInt();

                                foreach (var groupItem in resultDataSet.Items)
                                {
                                    if(groupItem.CheckGet("ID").ToInt() == groupId) 
                                    {
                                        groupItem["INGROUP"] = "1";
                                        break;
                                    }
                                }
                            }

                            // применить сортировки так что бы сначала шли отмеченные группы,
                            // а затем остальные отсортированные по наименованию. 
                            // стандартный метод не сортирует boolean поля
                            // 
                            resultDataSet.Items.Sort((x, y) =>
                                y["INGROUP"]==x["INGROUP"] ? x["NAME"].CompareTo(y["NAME"]) : y["INGROUP"].CompareTo(x["INGROUP"]) );

                            GroupsGrid.UpdateItems(resultDataSet);
                            GroupsGrid.SetSelectToFirstRow();

                            
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }

                }
            }

            EnableControls();
        }


        /// <summary>
        /// увольнение (блокировка сотрудника)
        /// </summary>
        public async void LockUser()
        {
            DisableControls();

            bool resume = true;

            if (resume)
            {
                var employeeID = UserSelectedItem.CheckGet("ID").ToString();
                var lockedFlag = "1";

                var p = new Dictionary<string, string>();
                p.Add("EMPL_ID", employeeID);
                p.Add("LOCKED_FLAG", lockedFlag);

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
                        UserGrid.UpdateItems(ds);
                    }

                    UserGrid.LoadItems();
                }
                else
                {
                    q.ProcessError();
                }
            }

            EnableControls();
        }

      


        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();

            UpdateGroups();
        }

        private void UpdateGroups()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Accounts");
            q.Request.SetParam("Object", "Group");
            q.Request.SetParam("Action", "List");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                if (result != null)
                {
                    GroupDataSet = ListDataSet.Create(result, "ITEMS");

                    // добавляем поле для чекбоксов отмечающих присутствие пользователей в группах
                    foreach (var item in GroupDataSet.Items)
                    {
                        item.Add("INGROUP", "0");
                    }

                    GroupDataSet.Items.Sort((x, y) => x["NAME"].CompareTo(y["NAME"]));
                }
            }
        }

        /// <summary>
        /// обработка сообщений
        /// </summary>
        /// <param name="message"></param>
        public void ProcessMessages(ItemMessage message)
        {
            if(message!=null)
            {
                if(message.SenderName == "WindowManager")
                {
                    switch (message.Action)
                    {
                        case "FocusGot":
                            //UserGrid.ItemsAu
                            GroupsGrid.ItemsAutoUpdate=true;
                            break;

                        case "FocusLost":
                            GroupsGrid.ItemsAutoUpdate=false;
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void _ProcessMessages(ItemMessage m)
        {
            //Group ProductionTask
            if (m.ReceiverGroup.IndexOf("User") > -1)
            {
                switch (m.Action)
                {
                    case "Refresh":
                        UserGrid.LoadItems();

                        // выделение на новую строку
                        var id = m.Message.ToInt();
                        UserGrid.SetSelectedItemId(id);

                        break;
                }
            }

            if (m.ReceiverGroup.IndexOf("Group") > -1)
            {
                switch (m.Action)
                {
                    case "Refresh":
                        UserGrid.LoadItems();
                        break;
                }
            }
        }

        /// <summary>
        /// получение записей
        /// </summary>
        public async void UserGridLoadItems()
        {
            DisableControls();

            bool resume = true;

            var i = Department.SelectedItem;

            if (resume)
            {
                
                var p=new Dictionary<string,string>();

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Accounts");
                q.Request.SetParam("Object", "User");
                q.Request.SetParam("Action", "List");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;
                
                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {

                        {
                            var ds=ListDataSet.Create(result,"ITEMS");
                            UserGrid.UpdateItems(ds);
                        }

                        {
                            var ds = ListDataSet.Create(result, "DEPARTMENTS");

                            {
                                var list = new Dictionary<string, string>();
                                list.Add("-1", "Все");

                                foreach (var item in ds.Items)
                                {
                                    list.Add(item["ID"], item["NAME"]);
                                }

                                Department.Items = list;

                                if (i.Key == null)
                                    Department.SelectedItem = list.FirstOrDefault((x) => x.Key == "-1");
                                else
                                    Department.SelectedItem = i;

                            }
                        }
                    }
                }                
            }

            EnableControls();
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
            GridToolbarGroup.IsEnabled = false;
            UserGrid.ShowSplash();
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
            GridToolbarGroup.IsEnabled = true;
            UserGrid.HideSplash();
        }

        /// <summary>
        /// фильтрация записей
        /// </summary>
        public void UserGridFilterItems()
        {
            UserGridUpdateActions(null);

            if (UserGrid.GridItems != null)
            {
                if (UserGrid.GridItems.Count > 0)
                {
                    //фильтрация строк
                    {
                         
                        var showAll = false;

                        var v = Form.GetValues();

                        if (v.CheckGet("SHOW_ALL").ToBool())
                        {
                            // покаывать все
                            showAll = true;
                        }

                        var items = new List<Dictionary<string, string>>();
                        
                        foreach (Dictionary<string, string> row in UserGrid.GridItems)
                        {
                            var includeRowByLocked = false;

                            if (showAll)
                            {
                                includeRowByLocked = true;
                            }
                            else
                            {
                                includeRowByLocked = row.CheckGet("LOCKED").ToInt() == 0;
                            }

                            var includeRowByDepartment = false;

                            if (DepartmentID != -1)
                            {
                                includeRowByDepartment = row.CheckGet("DEPARTMENT_ID").ToInt() == DepartmentID;
                            }
                            else
                            {
                                includeRowByDepartment = true;
                            }

                            if ((includeRowByLocked) && (includeRowByDepartment))
                            {
                                items.Add(row);
                            }

                        }
                        UserGrid.GridItems = items;
                    }
                }
            }
        }

        /// <summary>
        /// обновление методов работы с выбранной в гриде строкой
        /// </summary>
        /// <param name="selectedItem"></param>
        public void UserGridUpdateActions(Dictionary<string, string> selectedItem)
        {
            UserSelectedItem = selectedItem;

            // Редактирование доступно, когда есть строка и есть ИД
            EditButton.IsEnabled = !string.IsNullOrEmpty(UserSelectedItem?["ID"]);

            // Настрока меню

            var outVal = new DataGridContextMenuItem();

            if (UserGrid.Menu.TryGetValue("Item1", out outVal))
                outVal.Enabled = !UserSelectedItem.CheckGet("LOCKED").ToBool();

            if (UserGrid.Menu.TryGetValue("Item2", out outVal))
            {
                outVal.Enabled = !UserSelectedItem.CheckGet("IS_ACCOUNT").ToBool();

                if (outVal.Enabled)
                {
                    var q = new LPackClientQuery();

                    q.Request.SetParam("Module", "Accounts");
                    q.Request.SetParam("Object", "Account");
                    q.Request.SetParam("Action", "ListUnbind");

                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            var items = ListDataSet.Create(result, "ITEMS").GetItemsList("ACCO_ID", "NAME");

                            var menuItems = new Dictionary<string, DataGridContextMenuItem>();
                            foreach (var item in items)
                            {
                                menuItems.Add(item.Key.ToString(), new DataGridContextMenuItem()
                                {
                                    Header = item.Value,
                                    Action =()=>BindAccount(item.Key, item.Value)
                                }) ;

                            }

                            outVal.Items = menuItems;
                        }
                    }

                }
            }

            GroupsGrid.LoadItems();
        }

        private void BindAccount(string key, string name)
        {
            if (UserSelectedItem != null)
            {
                var d = new DialogWindow("Вы действительно хотите назначить пользователю " + UserSelectedItem.CheckGet("FULL_NAME") + " аккаунт " + name, "Назначение аккаунта", "", DialogWindowButtons.NoYes);
                if (d.ShowDialog() == true)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Accounts");
                    q.Request.SetParam("Object", "User");
                    q.Request.SetParam("Action", "BindAccount");

                    var p = new Dictionary<string, string>();
                    p.CheckAdd("ID", UserSelectedItem.CheckGet("ID"));
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
                                    //отправляем сообщение гриду Сотрудники о необходимости обновить данные
                                    Messenger.Default.Send(new ItemMessage()
                                    {
                                        ReceiverGroup = "User",
                                        SenderName = "UserView",
                                        Action = "Refresh",
                                        Message = $"{id}",
                                    });

                                    //отправляем сообщение гриду Аккаунты о необходимости обновить данные
                                    Messenger.Default.Send(new ItemMessage()
                                    {
                                        ReceiverGroup = "Account",
                                        SenderName = "AccountView",
                                        Action = "Refresh",
                                    });
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// при выборе группы обновляем действия с этой группой
        /// </summary>
        /// <param name="selectedItem"></param>
        public void GroupsGridUpdateActions(Dictionary<string, string> selectedItem)
        {
            GroupSelectedItem = selectedItem;

            // В зависимости от выбранных груп предлагаем либо удалить либо добавить в группу, если пользователя в ней нет
            if(GroupSelectedItem!=null)
            {
                if(GroupSelectedItem.CheckGet("INGROUP").ToInt()==1)
                {
                    GroupsGrid.Menu["Item1"].Enabled = false;
                    GroupsGrid.Menu["Item2"].Enabled = true;
                }
                else
                {
                    GroupsGrid.Menu["Item1"].Enabled = true;
                    GroupsGrid.Menu["Item2"].Enabled = false;
                }
            }
        }

        /// <summary>
        /// обработка ввода с клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e=Central.WM.KeyboardEventsArgs;
            switch(e.Key)
            {
                case Key.F5:
                    UserGrid.LoadItems();
                    e.Handled=true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled=true;
                    break;

                case Key.Home:
                    UserGrid.SetSelectToFirstRow();
                    e.Handled=true;
                    break;

                case Key.End:
                    UserGrid.SetSelectToLastRow();
                    e.Handled=true;
                    break;
            }

            UserGrid.ProcessKeyboardEvents(e);      

        }
       
        /// <summary>
        /// отображение справочной статьи
        /// (относительный путь)
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/service/accounts/users");
        }

        /// <summary>
        /// создание новой записи
        /// </summary>
        public void Create()
        {
            //var i=new User();
            //i.Create();
        }

        /// <summary>
        /// редактирование записи
        /// </summary>
        public void Edit()
        {
            //var id=UserSelectedItem.CheckGet("ID").ToInt();
            //if(id!=0)
            //{
            //    var i=new User();
            //    i.Edit(id);
            //}            
        }

        /// <summary>
        /// экспорт в Excel
        /// </summary>
        private async void Export()
        {
            if(UserGrid !=null)
            {
                if(UserGrid.Items.Count>0)
                {
                    var eg = new ExcelGrid();
                    var cols=UserGrid.Columns;
                    eg.SetColumnsFromGrid(cols);
                    eg.Items = UserGrid.Items;
                    await Task.Run(() =>
                    {
                        eg.Make();
                    });
                }
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void CreateButton_Click(object sender,RoutedEventArgs e)
        {
            Create();
        }

        private void EditButton_Click(object sender,RoutedEventArgs e)
        {
            Edit();
        }

        private void ExportButton_Click(object sender,RoutedEventArgs e)
        {
            Export();
        }

        private void RefreshButton_Click(object sender,RoutedEventArgs e)
        {
            UserGrid.LoadItems();
        }

        private void ShowAll_Click(object sender,RoutedEventArgs e)
        {
            UserGrid.UpdateItems();
        }

        private void RefreshGroupButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateGroups();
            GroupsGrid.LoadItems();
        }
    }


}
