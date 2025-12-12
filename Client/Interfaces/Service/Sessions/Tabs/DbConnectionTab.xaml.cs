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
    /// подключения к бд
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2024-11-11</released>
    /// <changed>2024-11-11</changed>
    public partial class DbConnectionTab : ControlBase
    {
        public DbConnectionTab()
        {
            InitializeComponent();

            ControlSection = "db_connection";
            RoleName = "[erp]session";
            ControlTitle ="Подключения к БД";
            DocumentationUrl = "/doc/l-pack-erp/service/sessions/db_connection";

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
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="ID",
                    Path="ID",
                    Doc="Идентификатор подключения",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="SID",
                    Path="SID",
                    Doc="Идентификатор сессии БД",
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
                    Header="Логин DB",
                    Path="DB_LOGIN",
                    Doc="Имя пользователя БД",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Владелец",
                    Path="OWNER",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if(row.CheckGet("OWNER").ToUpper() == "SYSTEM")
                                {
                                    color = HColor.Violet;
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
                    Header="Владелец",
                    Path="OWNER_STRING",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Тип",
                    Path="TYPE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="ID Аккаунта",
                    Path="ACCOUNT_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Открыто",
                    Path="OPEN",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Занято",
                    Path="BUSY",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Занято",
                    Path="BUSY_TIME",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Используется",
                    Path="IN_USE",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=6,
                },
                 new DataGridHelperColumn
                {
                    Header="Используется",
                    Path="IN_USE_TIME",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Неактивно",
                    Path="IDLE_TIME",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                {
                                    var v=row.CheckGet("IDLE_TIME").ToInt();
                                    if(v> 0 && v < 60000)
                                    {
                                        color = HColor.Green;
                                    }

                                    if(v> 60000 && v < 200000)
                                    {
                                        color = HColor.Gray;
                                    }

                                    if(v> 200000 )
                                    {
                                        color = HColor.Red;
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
                    Header="Удаляется",
                    Path="DESTRUCT",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="NOTE2",
                    Path="NOTE2",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="QUERY_COUNT",
                    Path="QUERY_COUNT",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="LAST_STATE",
                    Path="LAST_STATE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=14,
                },
                new DataGridHelperColumn
                {
                    Header="LAST_SQL",
                    Path="LAST_SQL",
                    ColumnType=ColumnTypeRef.String,
                    Width2=24,
                },
                new DataGridHelperColumn
                {
                    Header="LAST_REQUEST",
                    Path="LAST_REQUEST",
                    ColumnType=ColumnTypeRef.String,
                    Width2=24,
                },

            };
            DbConnectionGrid.SetColumns(columns);
            DbConnectionGrid.SetPrimaryKey("UID");
            DbConnectionGrid.SetSorting("UID", ListSortDirection.Ascending);
            DbConnectionGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            DbConnectionGrid.SearchText = DbConnectionGridSearch;
            DbConnectionGrid.Toolbar = DbConnectionGridToolbar;
            DbConnectionGrid.AutoUpdateInterval = 0;
            DbConnectionGrid.UseProgressBar = true;
            DbConnectionGrid.UseProgressSplashAuto = true;
            DbConnectionGrid.QueryLoadItems = new RequestData()
            {
                Module = "Service",
                Object = "LiteBase",
                Action = "List",
                AnswerSectionKey = "server_stat_db_connections",
                BeforeRequest = (RequestData rd) =>
                {
                    rd.Params = new Dictionary<string, string>()
                    {
                        {"TABLE_NAME", "server_stat_db_connections"},
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
