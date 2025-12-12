using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Вкладка с настройкой готовности штанцформ для производства в Кашире
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class CuttingStampReadyKshTab : ControlBase
    {
        public CuttingStampReadyKshTab()
        {
            InitializeComponent();

            ControlTitle = "Штанцформы на линии";
            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/cutting_stamp_keeping";
            RoleName = "[erp]rig_cutting_stamp_keep_ksh";
            FactoryId = 2;

            OnLoad = () =>
            {
                GridInit();
            };

            OnUnload = () =>
            {
                Grid.Destruct();
            };

            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverName == ControlName)
                {
                    ProcessMessage(msg);
                }
            };

            OnFocusGot = () =>
            {
                Grid.ItemsAutoUpdate = true;
                Grid.Run();
            };

            OnFocusLost = () =>
            {
                Grid.ItemsAutoUpdate = false;
            };

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
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        Grid.LoadItems();
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
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        Central.ShowHelp(DocumentationUrl);
                    },
                });
            }
            
            Commander.Init(this);
        }

        /// <summary>
        /// Идентификатор производственной площадки: 1 - Липецк, 2 - Кашира
        /// </summary>
        public int FactoryId { get; set; }

        /// <summary>
        /// Обработка и выполнение команд
        /// </summary>
        /// <param name="command"></param>
        public void ProcessMessage(ItemMessage m)
        {
            string command = m.Action;
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "refresh":
                        Grid.LoadItems();
                        break;
                }
            }
        }

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        private void GridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn()
                {
                    Header="ИД",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=12,
                },
                new DataGridHelperColumn()
                {
                    Header="Дата",
                    Path="CREATED_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=12,
                },
                new DataGridHelperColumn()
                {
                    Header="Станок",
                    Path="LINE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn()
                {
                    Header="Номер задания",
                    Path="TASK_NUM",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn()
                {
                    Header="Артикул",
                    Path="SKU_CODE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn()
                {
                    Header="Штанцформа",
                    Path="STAMP_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=25,
                },
                new DataGridHelperColumn()
                {
                    Header="Место хранения",
                    Path="CELL_NUM",
                    ColumnType=ColumnTypeRef.String,
                    Width2=25,
                },
                new DataGridHelperColumn()
                {
                    Header="Готовность",
                    Path="SHTANZ_COMPLETE_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=10,
                    Editable=true,
                    OnClickAction = (row, el) =>
                    {
                        bool result = false;
                        if (el != null)
                        {
                            UpdateCompleteFlag(row);
                        }

                        return result;
                    },
                    Stylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("SHTANZ_COMPLETE_FLAG").ToInt() == 1)
                                {
                                    color = HColor.Green;
                                }

                                if (!color.IsNullOrEmpty())
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        },
                    },
                },
            };

            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("ID");
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.EnableSortingGrid = false;
            Grid.Toolbar = StampToolbar;
            Grid.Commands = Commander;
            Grid.OnLoadItems = LoadItems;

            Grid.Init();
        }

        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        private async void LoadItems()
        {
            Grid.Toolbar.IsEnabled = false;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "CuttingStamp");
            q.Request.SetParam("Action", "ListReady");
            q.Request.SetParam("FACTORY_ID", FactoryId.ToString());

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
                    var ds = ListDataSet.Create(result, "TASK_LIST");
                    Grid.UpdateItems(ds);
                }
            }

            Grid.Toolbar.IsEnabled = true;
        }


        /// <summary>
        /// Отметка готовности штанцформы
        /// </summary>
        /// <param name="data"></param>
        private async void UpdateCompleteFlag(Dictionary<string, string> data)
        {
            var readyFlag = data.CheckGet("SHTANZ_COMPLETE_FLAG").ToInt();

            if (readyFlag == 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Rig");
                q.Request.SetParam("Object", "CuttingStamp");
                q.Request.SetParam("Action", "SetReady");
                q.Request.SetParam("ID", Grid.SelectedItem.CheckGet("ID"));
                q.Request.SetParam("LINE_ID", Grid.SelectedItem.CheckGet("LINE_ID"));

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var answ = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (answ != null)
                    {
                        if (answ.ContainsKey("ITEM"))
                        {
                            Grid.LoadItems();
                        }
                    }
                }
            }
        }
    }
}
