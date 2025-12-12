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

namespace Client.Interfaces.Service.Sessions
{
    /// <summary>
    /// сессии пользователей
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2024-11-11</released>
    /// <changed>2024-11-13</changed>
    public partial class UserSessionTab : ControlBase
    {
        public UserSessionTab()
        {
            InitializeComponent();

            ControlSection = "user_session";
            RoleName = "[erp]session";
            ControlTitle ="Сесиии";
            DocumentationUrl = "/doc/l-pack-erp/service/sessions/user_session";

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
                DbConnectionGridInit();
            };

            OnUnload=()=>
            {
                DbConnectionGrid.Destruct();
            };

            OnFocusGot=()=>
            {
                DbConnectionGrid.ItemsAutoUpdate=true;
                DbConnectionGrid.Run();
            };

            OnFocusLost=()=>
            {
                DbConnectionGrid.ItemsAutoUpdate=false;
            };

            OnNavigate = () =>
            {
                //var positionId = Parameters.CheckGet("position_id");
                //if(!positionId.IsNullOrEmpty())
                //{
                //    PositionGridSearch.Text = positionId;
                //    PositionGrid.UpdateItems();
                //}
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

                Commander.SetCurrentGridName("DbConnectionGrid");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "dbconnection_refresh",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить данные",
                        ButtonUse = true,
                        ButtonName = "DbConnectionRefreshButton",
                        MenuUse = true,
                        Action = () =>
                        {
                            DbConnectionGrid.LoadItems();
                        },
                    });

                    Commander.SetCurrentGroup("item");
                    {
                    }
                }

                Commander.Init(this);
            }
        }

        public void DbConnectionGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="UID",
                    Path="UID",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Дата",
                    Path="ON_DATE",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=16,
                },
                new DataGridHelperColumn
                {
                    Header="Сервер",
                    Path="SERVER",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="ID",
                    Path="ID",
                    Doc="Идентификатор сессии",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Логин ERP",
                    Path="LOGIN",
                    Doc="Имя пользователя ERP",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                
                new DataGridHelperColumn
                {
                    Header="Создана",
                    Path="CREATED",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=16,
                },
                new DataGridHelperColumn
                {
                    Header="Обновление",
                    Path="UPDATED",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=16,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                {
                                    var s=row.CheckGet("UPDATED");
                                    if(!s.IsNullOrEmpty())
                                    {
                                        var dt=Tools.TimeOffsetSeconds(s);
                                        if(dt < 60)
                                        {
                                            color = HColor.Green;
                                        }
                                    }
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
                    Header="Aктуальность",
                    Path="_ACTUAL",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                    FormatterRaw= (row) =>
                    {
                        var result = "";

                        var s=row.CheckGet("UPDATED");
                        if(!s.IsNullOrEmpty())
                        {
                            var dt=Tools.TimeOffsetSeconds(s);
                            result=dt.ToString();
                        }

                        return result;
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Тип",
                    Path="TYPE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="METHOD",
                    Path="METHOD",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn
                {
                    Header="MODE",
                    Path="DB_CONNECTION_MODE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
               
            };
            DbConnectionGrid.SetColumns(columns);
            DbConnectionGrid.SetPrimaryKey("UID");
            DbConnectionGrid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
            DbConnectionGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            DbConnectionGrid.SearchText = DbConnectionGridSearch;
            DbConnectionGrid.Toolbar = DbConnectionGridToolbar;
            DbConnectionGrid.QueryLoadItems = new RequestData()
            {
                Module = "Service",
                Object = "LiteBase",
                Action = "List",
                AnswerSectionKey = "server_stat_sessions",
                BeforeRequest = (RequestData rd) =>
                {
                    rd.Params = new Dictionary<string, string>()
                    {
                        {"TABLE_NAME", "server_stat_sessions"},
                        {"TABLE_DIRECTORY", ""},
                        // 1=global,2=local,3=net
                        {"STORAGE_TYPE", "3"},
                    };
                }                
            };
            DbConnectionGrid.Commands = Commander;
            DbConnectionGrid.Init();            
        }
    }
}
