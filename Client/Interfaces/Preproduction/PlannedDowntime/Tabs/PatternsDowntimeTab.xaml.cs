using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction.PlannedDowntime.Elements;
using Client.Interfaces.Preproduction.PlannedDowntime.Frames;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Client.Interfaces.Preproduction.PlannedDowntime.Tabs
{
    public partial class PatternsDowntimeTab : ControlBase
    {
        public PatternsDowntimeTab()
        {
            InitializeComponent();

            ControlSection = "patterns_downtime";
            RoleName = "[erp]planned_downtime";
            ControlTitle = "Типовые простои";

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == "PatternsDowntimeGrid")
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnLoad = () =>
            {
                Init();
            };

            OnUnload = () =>
            {
                PatternsDowntimeGrid.Destruct();
            };

            OnFocusGot = () => { };
            OnFocusLost = () => { };

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

                Commander.SetCurrentGridName("PatternsDowntimeGrid");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "pattern_downtime_grid_refresh",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Обновить",
                        ButtonUse = true,
                        MenuUse = true,
                        ButtonControl = Refresh,
                        AccessLevel = Role.AccessMode.ReadOnly,
                        ActionMessage = (ItemMessage m) =>
                        {
                            if (m != null)
                            {
                                var id = m.Message;

                                if (!id.IsNullOrEmpty())
                                {
                                    PatternsDowntimeGrid.SetSelectedRowAfterUpdate(id);
                                }
                            }
                            PatternsDowntimeGrid.LoadItems();
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "pattern_downtime_grid_add",
                        Group = "grid_tools",
                        Enabled = true,
                        Title = "Создать",
                        ButtonUse = true,
                        MenuUse = true,
                        ButtonControl = AddButton,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var c = new PatternCreateFrame();
                            c.Create(FactId);
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "pattern_downtime_grid_edit",
                        Group = "grid_tools",
                        Enabled = true,
                        Title = "Изменить",
                        ButtonUse = true,
                        MenuUse = true,
                        ButtonControl = EditButton,
                        HotKey = "DoubleCLick",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var item = PatternsDowntimeGrid.SelectedItem;
                            var c = new PatternCreateFrame();
                            c.Edit(item.CheckGet("DOSC_ID").ToInt(), FactId, item);
                        },
                        CheckEnabled = () =>
                        {
                            if (PatternsDowntimeGrid.Items.Count > 0 && PatternsDowntimeGrid.SelectedItem.CheckGet("DOSC_ID").ToInt() > 0)
                            {
                                return true;
                            }

                            return false;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "pattern_downtime_grid_delete",
                        Group = "grid_tools",
                        Enabled = true,
                        Title = "Удалить",
                        ButtonUse = true,
                        MenuUse = true,
                        ButtonControl = DeleteButton,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var id = PatternsDowntimeGrid.SelectedItem.CheckGet("DOSC_ID").ToInt();
                            var d = new DialogWindow($"Вы действительно хотите удалить простой №{id}", "Операция", "", DialogWindowButtons.YesNo);

                            if ((bool)d.ShowDialog())
                            {
                                DowntimePlanDelete(id);
                            }
                        },
                        CheckEnabled = () =>
                        {
                            if (PatternsDowntimeGrid.Items.Count > 0 && PatternsDowntimeGrid.SelectedItem.CheckGet("DOSC_ID").ToInt() > 0)
                            {
                                return true;
                            }

                            return false;
                        }
                    });
                }

                Commander.Init(this);
            }
        }

        private int FactId = 1;

        private void Init()
        {
            SetDefaults();
            GridInit();
        }

        private void SetDefaults()
        {
            var factoryTypeItems = new Dictionary<string, string>
            {
                { "1", "Л-ПАК ЛИПЕЦК" },
                { "2", "Л-ПАК КАШИРА" }
            };
            FactoryType.Items = factoryTypeItems;
            FactoryType.SetSelectedItemByKey("1");
        }

        private void GridInit()
        {
            var column = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "Начать с",
                    Path = "DTTM_START",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Format = "dd.MM.yyyy",
                    Width2 = 10
                },
                new DataGridHelperColumn
                {
                    Header = "Станок",
                    Path = "NAME2",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 10
                },
                new DataGridHelperColumn
                {
                    Header = "Причина простоя",
                    Path = "DESCRIPTION",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 34
                },
                new DataGridHelperColumn
                {
                    Header = "День недели",
                    Path = "DAY_OF_WEEK",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    Format = "N0",
                    Width2 = 11
                },
                new DataGridHelperColumn
                {
                    Header = "День месяца",
                    Path = "DAY_OF_MONTH",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    Format = "N0",
                    Width2 = 11
                },
                new DataGridHelperColumn
                {
                    Header = "С часа",
                    Path = "HOUR_START",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    Format = "N0",
                    Width2 = 6
                },
                new DataGridHelperColumn
                {
                    Header = "С минуты",
                    Path = "MINUTE_START",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    Format = "N0",
                    Width2 = 6
                },
                new DataGridHelperColumn
                {
                    Header = "Минут",
                    Path = "DOWNTIME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    Format = "N0",
                    Width2 = 8
                },
                new DataGridHelperColumn
                {
                    Header = "Периодичность",
                    Path = "REPEAT_HOURS",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    Format = "N0",
                    Width2 = 9
                },
                new DataGridHelperColumn
                {
                    Header = "ИД",
                    Path = "DOSC_ID",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2 = 6
                },
                new DataGridHelperColumn
                {
                    Header = "ID_IDLE_DETAILS",
                    Path = "ID_IDLE_DETAILS",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2 = 6,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "ID_ST",
                    Path = "ID_ST",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2 = 6,
                    Visible = false
                },

            };
            PatternsDowntimeGrid.SetColumns(column);
            PatternsDowntimeGrid.EnableSortingGrid = false;
            PatternsDowntimeGrid.EnableFiltering = true;
            PatternsDowntimeGrid.SetPrimaryKey("DOSC_ID");
            PatternsDowntimeGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            PatternsDowntimeGrid.ItemsAutoUpdate = false;
            PatternsDowntimeGrid.AutoUpdateInterval = 0;
            PatternsDowntimeGrid.OnLoadItems = LoadItems;
            PatternsDowntimeGrid.SearchText = SearchText;
            PatternsDowntimeGrid.Commands = Commander;
            PatternsDowntimeGrid.Init();
        }



        private async void LoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PlannedDowntime");
            q.Request.SetParam("Action", "PatternList");
            q.Request.SetParam("FACT_ID", FactId.ToString());

            await Task.Run(() => q.DoQuery());

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                if (result != null)
                {
                    var i = ListDataSet.Create(result, "ITEMS");

                    PatternsDowntimeGrid.UpdateItems(i);

                    if (PatternsDowntimeGrid.Items.Count > 0)
                    {
                        PatternsDowntimeGrid.SelectRowFirst();
                    }
                }
            }

        }

        private async void DowntimePlanDelete(int id)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PlannedDowntime");
            q.Request.SetParam("Action", "PatternDelete");
            q.Request.SetParam("DOSC_ID", id.ToString());

            await Task.Run(() => q.DoQuery());

            if (q.Answer.Status == 0)
            {
                PatternsDowntimeGrid.LoadItems();
            }
        }

        private void FactoryType_SelectedItemChanged(System.Windows.DependencyObject d, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            var s = (SelectBox)d;

            FactId = s.SelectedItem.Key.ToInt();

            PatternsDowntimeGrid.LoadItems();
        }
    }
}