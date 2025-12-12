using Client.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using static Client.Interfaces.Main.DataGridHelperColumn;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using UserControl = System.Windows.Controls.UserControl;
using System.Windows.Controls;

namespace Client.Interfaces.Service.Mail
{
    /// <summary>
    /// ярлык с адресом для почтовых конвертов, список ярлыков
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-06-22</released>
    /// <changed>2023-06-22</changed>
    public partial class LabelList : UserControl
    {
        public LabelList()
        {
            InitializeComponent();
            
            if(Central.InDesignMode()){
                return;
            }
            
            InitForm();
            InitGrid();
            SetDefaults();
            Central.Msg.Register(ProcessMessage);

            ProcessPermissions();
        }

        public string RoleName = "[erp]mail";

        public FormHelper Form { get; set; }

        public void InitForm()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {           
                new FormHelperField()
                {
                    Path="_SEARCH",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=SearchText,                    
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },          
                    First=true,
                },
            };

            Form.SetFields(fields);
        }

        public void InitGrid()
        {
             //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header = "*",
                        Path="_SELECTED",
                        ColumnType=ColumnTypeRef.Boolean,
                        Editable = true,
                        MinWidth=35,
                        MaxWidth=35,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=35,
                        MaxWidth=35,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Тип",
                        Path="TYPE_TITLE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=50,
                        MaxWidth=80,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Получатель",
                        Path="RECIPIENT",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=250,
                        MaxWidth=350,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Индекс",
                        Path="ZIP_CODE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=70,
                        MaxWidth=70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Адрес",
                        Path="ADDRESS",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=250,
                        MaxWidth=1500,                        
                    },
                  
                    new DataGridHelperColumn
                    {
                        Header="Дата отправки",
                        Path="DISPATCH_DT",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format = "dd.MM.yyyy",
                        MinWidth=80,
                        MaxWidth=105,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата получения",
                        Path="RECEIVED_DT",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format = "dd.MM.yyyy",
                        MinWidth=80,
                        MaxWidth=105,
                    },
                       new DataGridHelperColumn
                    {
                        Header="Примечание",
                        Path="NOTE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=100,
                        MaxWidth=250,                        
                    },
                    
                    new DataGridHelperColumn
                    {
                        Header="DAYS",
                        Path="DAYS",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=80,
                        MaxWidth=105,
                        Visible = false
                    },                 
                };
                Grid.SetColumns(columns);
            };

            Grid.SetSorting("ID", ListSortDirection.Descending);
            Grid.PrimaryKey = "ID";
            Grid.SearchText = SearchText;            

            Grid.DisableControls=()=>
            {
                GridToolbar.IsEnabled = false;
                Grid.ShowSplash();
            };
                
            Grid.EnableControls=()=>
            {
                GridToolbar.IsEnabled = true;
                Grid.HideSplash();
            };

            Grid.OnLoadItems = async ()=>
            {
                Grid.DisableControls();

                var today=DateTime.Now;
                bool resume = true;

                if (resume)
                {
                    var p = new Dictionary<string, string>();
                    {
                    }

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Mail");
                    q.Request.SetParam("Object", "Label");
                    q.Request.SetParam("Action", "List");
                    q.Request.SetParams(p);

                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    if(q.Answer.Status == 0)                
                    {

                        
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            {
                                var ds = ListDataSet.Create(result, "ITEMS");

                                if (ds.Items.Count > 0)
                                {
                                    foreach (Dictionary<string,string> row in ds.Items)
                                    {
                                        
                                        {
                                            var todayDate = DateTime.Now.ToString("dd.MM.yyyy").ToDateTime();
                                            var difDays = 0;
                                            var created = row.CheckGet("DISPATCH_DT");
                                            if (!created.IsNullOrEmpty())
                                            {
                                                var createdDate = created.ToDateTime().ToString("dd.MM.yyyy").ToDateTime();
                                                difDays = (int)((TimeSpan)(todayDate - createdDate)).TotalDays;
                                            }
                                            row.CheckAdd("DAYS", difDays.ToString());
                                        }
                                        
                                    }
                                }
                                
                                Grid.UpdateItems(ds);
                            }
                        }
                        
                        //Grid.UpdateItemsAnswer(q.Answer,"ITEMS");
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }

                Grid.EnableControls();
            };

            //Grid.OnFilterItems = async () =>
            //{
            //    Grid.DisableControls();
                
            //    /*
            //        list.Add("0", "Все");
            //        list.Add("1", "Созданные сегодня");
            //        list.Add("2", "Созданные вчера");
            //     */
            //    bool doFilteringByCreated = false;
            //    int createdType = 0;

            //    var flag = (bool)SelectAllCheckBox.IsChecked;
            //    if(flag)
            //    {
            //        doFilteringByCreated = true;
            //        createdType=1;
            //    }
                
            //    /*
            //    if (FilterSelect.SelectedItem.Key != null)
            //    {
            //        if (FilterSelect.SelectedItem.Key.ToInt() > 0)
            //        {
            //            doFilteringByCreated = true;
            //            createdType = FilterSelect.SelectedItem.Key.ToInt();    
            //        }
            //    }
            //    */

            //    if (
            //        doFilteringByCreated
            //    )
            //    {
            //        var items = new List<Dictionary<string, string>>();
            //        foreach (Dictionary<string, string> row in Grid.GridItems)
            //        {
            //            bool includeByCreated = true;

            //            if (doFilteringByCreated)
            //            {
            //                includeByCreated = false;
                            
            //                var difDays = row.CheckGet("DAYS").ToInt();
                            
            //                switch (createdType)
            //                {
            //                    //Созданные сегодня
            //                    case 1:
            //                    {
            //                        if (difDays == 1)
            //                        {
            //                            includeByCreated = true;
            //                        }
            //                    }
            //                        break;
                                
            //                    //Созданные вчера
            //                    case 2:
            //                    {
            //                        if (difDays == 2)
            //                        {
            //                            includeByCreated = true;
            //                        }
            //                    }
            //                        break;
            //                }
            //            }
                        
            //            if (
            //                includeByCreated
            //            )
            //            {
            //                items.Add(row);
            //            }
            //        }
            //        Grid.GridItems = items;
            //    }

            //    Grid.EnableControls();
            //};
                

            Grid.OnSelectItem = (row) =>
            {
                ProcessCommand("actions_update");
            };
            
            Grid.OnDblClick= (row) =>
            {
                if (Central.Navigator.GetRoleLevel(this.RoleName) >= Role.AccessMode.FullAccess)
                {
                    ProcessCommand("edit");
                }
            };
            
            Grid.Menu = new Dictionary<string, DataGridContextMenuItem>()
            {
                {
                    "Edit",
                    new DataGridContextMenuItem()
                    {
                        Header="Изменить",
                        Tag = "access_mode_full_access",
                        Action=() =>
                        {
                            ProcessCommand("edit");
                        }
                    }
                },
                {
                    "Удалить",
                    new DataGridContextMenuItem()
                    {
                        Header="Удалить",
                        Tag = "access_mode_full_access",
                        Action=() =>
                        {
                            ProcessCommand("delete");
                        }
                    }
                },
                {
                    "SetStateReceived",
                    new DataGridContextMenuItem()
                    {
                        Header="Отметить получение",
                        Tag = "access_mode_full_access",
                        Action=() =>
                        {
                            ProcessCommand("SetStateReceived");
                        }
                    }
                },
                {
                    "Print",
                    new DataGridContextMenuItem()
                    {
                        Header="Печать",
                        Action=() =>
                        {
                            ProcessCommand("Print");
                        }
                    }
                },
                { "s1", new DataGridContextMenuItem(){
                    Header="-",
                }},
                {
                    "SelectAll",
                    new DataGridContextMenuItem()
                    {
                        Header="Выделить все",
                        Action=() =>
                        {
                            ProcessCommand("SelectAll");
                        }
                    }
                },
                {
                    "DeselectAll",
                    new DataGridContextMenuItem()
                    {
                        Header="Снять выделение",
                        Action=() =>
                        {
                            ProcessCommand("DeselectAll");
                        }
                    }
                },
            };

            Grid.Init();
            Grid.Run();
        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel(this.RoleName);
            var userAccessMode = mode;
            switch (mode)
            {
                case Role.AccessMode.Special:
                    break;

                case Role.AccessMode.FullAccess:
                    break;

                case Role.AccessMode.ReadOnly:
                default:
                    break;
            }

            List<Button> buttons = UIUtil.GetVisualChilds<Button>(this.Content as DependencyObject);
            if (buttons != null && buttons.Count > 0)
            {
                foreach (var button in buttons)
                {
                    var buttonTagList = UIUtil.GetTagList(button);
                    var accessMode = Acl.FindTagAccessMode(buttonTagList);
                    if (accessMode > userAccessMode)
                    {
                        button.IsEnabled = false;
                    }
                }
            }

            if (Grid != null && Grid.Menu != null && Grid.Menu.Count > 0)
            {
                foreach (var manuItem in Grid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }
        }

        private void SetDefaults()
        {
            Form.SetDefaults();
            
            //{
            //    var list = new Dictionary<string, string>();
            //    list.Add("0", "Все");
            //    list.Add("1", "Созданные сегодня");
            //    list.Add("2", "Созданные вчера");

            //    FilterSelect.Items = list;
            //    FilterSelect.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");
            //}
            
        }
        
        private void SelectAllItems(bool flag)
        {
            if (Grid.Items.Count > 0)
            {
                foreach (var item in Grid.Items)
                {
                    if (flag)
                    {
                        item.CheckAdd("_SELECTED", "1");    
                    }
                    else
                    {
                        item.CheckAdd("_SELECTED", "0");
                    }
                    
                }
                Grid.UpdateItems();
            }
        }

        private void SelectTodayItems(bool flag)
        {
            if (Grid.Items.Count > 0)
            {
                foreach (var item in Grid.Items)
                {
                    if (flag && item.CheckGet("DAYS").ToInt()==0)
                    {
                        item.CheckAdd("_SELECTED", "1");    
                    }
                    else
                    {
                        item.CheckAdd("_SELECTED", "0");
                    }
                    
                }
                Grid.UpdateItems();
            }
        }
        
        private void ProcessCommand(string command)
        {
            command = command.ClearCommand();
            switch (command)
            {
                case "showed":
                    Grid.Run();
                    break;
                
                case "refresh":
                    Grid.LoadItems();
                    break;
                
                case "filter":
                    Grid.UpdateItems();
                    break;
                
                case "selectall":
                    SelectAllItems(true);
                    break;

                case "deselectall":
                    SelectAllItems(false);
                    break;

                case "create":
                {
                    var h = new Label();
                    h.Create();                    
                }
                    break;

                case "edit":
                {
                    var id = Grid.GetSelectedRowId();
                    if (id!=0)
                    {
                        var h = new Label();
                        h.Edit(id);
                    }                    
                }
                    break;
                
                case "print":
                {
                    var list = Grid.GetListItems();

                    if (list.Count > 0)
                    {
                        var h=new LabelPrintSheet();
                        h.Items=list;
                        h.Init();
                    }

                }
                    break;


                case "setstatereceived":
                {
                    var list = Grid.GetListItems();
                    bool resume = true;
                   
                    if (resume)
                    {
                        if (list.Count == 0)
                        {
                            resume = false;
                        }
                    }

                    if (resume)
                    {
                        string msg = "";
                        msg += $"Отметить получение для выбранных писем?";
                        
                        var d = new DialogWindow($"{msg}", "Получение", "", DialogWindowButtons.NoYes);
                        if (d.ShowDialog() != true)
                        {
                            resume = false;
                        }
                    }

                    if (resume)
                    {
                        var ids="";
                        foreach(Dictionary<string, string> row in list)
                        {
                            var id=row.CheckGet("ID");
                            if(!ids.IsNullOrEmpty())
                            {
                                ids=$"{ids},";
                            }                            
                            ids=$"{ids}{id}";
                        }
                        SetReceived(ids);
                    }

                }
                    break;
                
                case "delete":
                {
                    var list = Grid.GetListItems();
                    bool resume = true;
                   
                    if (resume)
                    {
                        if (list.Count == 0)
                        {
                            resume = false;
                        }
                    }

                    if (resume)
                    {
                        string msg = "";
                        msg += $"Удалить выбранные письма?";
                        
                        var d = new DialogWindow($"{msg}", "Удаление", "", DialogWindowButtons.NoYes);
                        if (d.ShowDialog() != true)
                        {
                            resume = false;
                        }
                    }

                    if (resume)
                    {
                        var ids="";
                        foreach(Dictionary<string, string> row in list)
                        {
                            var id=row.CheckGet("ID");
                            if(!ids.IsNullOrEmpty())
                            {
                                ids=$"{ids},";
                            }                            
                            ids=$"{ids}{id}";
                        }
                        Delete(ids);
                    }

                }
                    break;

                    

                case "actions_update":
                {
                    var list=Grid.GetSelectedItems();
                    var row = Grid.SelectedItem;
                    
                    PrintButton.IsEnabled=false;
                    SetStateReceivedButton.IsEnabled = false;
                    Grid.Menu["Print"].Enabled = false;
                    Grid.Menu["SetStateReceived"].Enabled = false;
                    
                    if(list.Count > 0 )
                    {
                        PrintButton.IsEnabled=true;
                        Grid.Menu["Print"].Enabled = true;
                    }

                    if (row.CheckGet("ID").ToInt() != 0)
                    {
                        PrintButton.IsEnabled=true;
                        Grid.Menu["Print"].Enabled = true;
                    }

                    /*
                        1='простое'
                        2='заказное'
                        3='заказное с уведомлением'                      
                     */
                    if (row.CheckGet("TYPE_ID").ToInt() == 3)
                    {
                        SetStateReceivedButton.IsEnabled = true;
                        Grid.Menu["SetStateReceived"].Enabled = true;
                    }

                        ProcessPermissions();
                }
                    break;
            }
        }

        public async void SetReceived(string ids)
        {
            var resume=true; 
            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("IDS", ids.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Mail");
                q.Request.SetParam("Object", "Label");
                q.Request.SetParam("Action", "SetReceived");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if(q.Answer.Status == 0)                
                {
                    ProcessCommand("refresh");    
                }
                else
                {
                    q.ProcessError();
                }
            }
        }
        
        public async void Delete(string ids)
        {
            var resume=true; 
            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("IDS", ids.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Mail");
                q.Request.SetParam("Object", "Label");
                q.Request.SetParam("Action", "Delete");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if(q.Answer.Status == 0)                
                {
                    ProcessCommand("refresh");    
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        public void ProcessMessage(ItemMessage message)
        {
            if(message!=null)
            {
                //if(message.ReceiverName==ControlName)
                {
                    ProcessCommand(message.Action);
                }
            }
        }

        public void UpdateActions()
        {
            
        }

        private void GridToolbarButtonOnClick(object sender, RoutedEventArgs e)
        {
            if (sender!=null)
            {
                var c = (System.Windows.Controls.Button) sender;
                var buttonTagList = UIUtil.GetTagList(c);
                foreach (var tag in buttonTagList) 
                {
                    ProcessCommand(tag);
                }
            }
        }
        
        private void SelectAllCheckBoxOnClick(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            {
                var c = (System.Windows.Controls.CheckBox) sender;
                var flag = (bool)c.IsChecked;
                //SelectAllItems(flag);
                 SelectTodayItems(flag);
            }
            
        }
        
        private void FilterSelectOnChange(System.Windows.DependencyObject d, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            ProcessCommand("filter");    
        }
    }
}
