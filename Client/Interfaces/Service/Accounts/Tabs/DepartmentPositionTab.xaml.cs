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
    public partial class DepartmentPositionTab : ControlBase
    {
        /*
            | DepartmentGrid | PositionGrid |

            2 независимых грида
         */
        public DepartmentPositionTab()
        {
            InitializeComponent();

            ControlSection = "department";
            RoleName = "[erp]accounts";
            ControlTitle ="Отделы и должности";
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
                DepartmentGridInit();
                PositionGridInit();
            };

            OnUnload=()=>
            {
                DepartmentGrid.Destruct();
                PositionGrid.Destruct();
            };

            OnFocusGot=()=>
            {
                DepartmentGrid.ItemsAutoUpdate=true;
                DepartmentGrid.Run();
                PositionGrid.ItemsAutoUpdate = true;
                PositionGrid.Run();
            };

            OnFocusLost=()=>
            {
                DepartmentGrid.ItemsAutoUpdate=false;
                PositionGrid.ItemsAutoUpdate = false;
            };

            OnNavigate = () =>
            {
                var positionId = Parameters.CheckGet("position_id");
                if(!positionId.IsNullOrEmpty())
                {
                    PositionGridSearch.Text = positionId;
                    PositionGrid.UpdateItems();
                }

                var departmentId = Parameters.CheckGet("department_id");
                if(!departmentId.IsNullOrEmpty())
                {
                    DepartmentGridSearch.Text = departmentId;
                    DepartmentGrid.UpdateItems();
                }
            };

            {
                Commander.SetCurrentGroup("main");
                {                   
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

                Commander.SetCurrentGridName("DepartmentGrid");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "department_refresh",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить данные",
                        ButtonUse = true,
                        ButtonName = "DepartmentRefreshButton",
                        MenuUse = true,
                        Action = () =>
                        {
                            DepartmentGrid.LoadItems();
                        },
                    });

                    Commander.SetCurrentGroup("item");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "department_create",
                            Title = "Создать",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "DepartmentCreateButton",
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var h = new DepartmentForm();
                                h.Init("create", "0");
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = DepartmentGrid.GetPrimaryKey();
                                var row = DepartmentGrid.SelectedItem;
                                if(row.CheckGet(k).ToInt() != 0)
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "department_edit",
                            Title = "Изменить",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "DepartmentEditButton",
                            HotKey= "Return|DoubleCLick",
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var id = DepartmentGrid.SelectedItem.CheckGet("ID").ToInt();
                                if(id != 0)
                                {
                                    var h = new DepartmentForm();
                                    h.Init("edit", id.ToString());
                                }
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = DepartmentGrid.GetPrimaryKey();
                                var row = DepartmentGrid.SelectedItem;
                                if(row.CheckGet(k).ToInt() != 0)
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });
                    }
                }

                Commander.SetCurrentGridName("PositionGrid");                
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "position_refresh",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить данные",
                        ButtonUse = true,
                        ButtonName = "PositionRefreshButton",
                        MenuUse = true,
                        Action = () =>
                        {
                            PositionGrid.LoadItems();
                            //if(Commander.Message!=null)
                            //{
                            //    var id = Commander.Message.Message.ToString();
                            //    if(!id.IsNullOrEmpty())
                            //    {
                            //        PositionGrid.SelectRowByKey(id);
                            //    }
                            //}
                        },
                    });

                    Commander.SetCurrentGroup("item");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "position_create",
                            Title = "Создать",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "PositionCreateButton",
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var h = new PositionForm();
                                h.Init("create", "0");
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = PositionGrid.GetPrimaryKey();
                                var row = PositionGrid.SelectedItem;
                                if(row.CheckGet(k).ToInt() != 0)
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "position_edit",
                            Title = "Изменить",
                            MenuUse = true,
                            ButtonUse = true,
                            ButtonName = "PositionEditButton",
                            HotKey = "Return|DoubleCLick",
                            AccessLevel = Common.Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var id = PositionGrid.SelectedItem.CheckGet("ID").ToInt();
                                if(id != 0)
                                {
                                    var h = new PositionForm();
                                    h.Init("edit", id.ToString());
                                }
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = PositionGrid.GetPrimaryKey();
                                var row = PositionGrid.SelectedItem;
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

        public FormHelper DepartmentForm { get; set; }

        public void DepartmentGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="ID",
                    Doc="Идентификатор отдела",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="NAME",
                    Doc="Наименование отдела",
                    ColumnType=ColumnTypeRef.String,
                    Width2=32,
                },
                new DataGridHelperColumn
                {
                    Header="Сокращённое наименование",
                    Path="SHORT_NAME",
                    Doc="Сокращённое наименование отдела",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
            };
            DepartmentGrid.SetColumns(columns);
            DepartmentGrid.SetPrimaryKey("ID");
            DepartmentGrid.SetSorting("NAME", ListSortDirection.Ascending);
            DepartmentGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            DepartmentGrid.SearchText = DepartmentGridSearch;
            DepartmentGrid.Toolbar = DepartmentGridToolbar;
            DepartmentGrid.QueryLoadItems = new RequestData()
            {
                Module = "Accounts",
                Object = "Department",
                Action = "List",
                AnswerSectionKey = "ITEMS",               
            };
            DepartmentGrid.Commands = Commander;
            DepartmentGrid.Init();            
        }

        public void PositionGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="ID",
                    Doc="Идентификатор должности",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="NAME",
                    Doc="Наименование должности",
                    ColumnType=ColumnTypeRef.String,
                    Width2=32,
                },
            };

            PositionGrid.SetColumns(columns);
            PositionGrid.SetPrimaryKey("ID");
            PositionGrid.SetSorting("NAME", ListSortDirection.Ascending);
            PositionGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            PositionGrid.SearchText = PositionGridSearch;
            PositionGrid.Toolbar = PositionGridToolbar;
            //PositionGrid.AutoUpdateInterval = 15;
            PositionGrid.QueryLoadItems = new RequestData()
            {
                Module = "Accounts",
                Object = "Position",
                Action = "List",
                AnswerSectionKey = "ITEMS",
            };
            PositionGrid.Commands = Commander;
            PositionGrid.Init();
        }

       
       

      

       

       

       
               
      
    }
}
