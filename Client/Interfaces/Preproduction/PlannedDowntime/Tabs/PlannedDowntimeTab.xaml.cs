using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction.PlannedDowntime.Elements;
using Client.Interfaces.Preproduction.PlannedDowntime.Frames;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace Client.Interfaces.Preproduction.PlannedDowntime.Tabs
{
    public partial class PlannedDowntimeTab : ControlBase
    {
        public PlannedDowntimeTab()
        {
            InitializeComponent();

            ControlSection = "plan_downtime";
            RoleName = "[erp]planned_downtime";
            ControlTitle = "Запланированные простои";

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == "DowntimeGrid")
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
                DowntimeGrid.Destruct();
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

                Commander.SetCurrentGridName("DowntimeGrid");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "downtime_grid_refresh",
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
                                    DowntimeGrid.SetSelectedRowAfterUpdate(id);
                                }
                            }
                            DowntimeGrid.LoadItems();
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "downtime_grid_add",
                        Group = "grid_tools",
                        Enabled = true,
                        Title = "Создать",
                        ButtonUse = true,
                        MenuUse = true,
                        ButtonControl = AddButton,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var c = new DowntimeCreateFrame();
                            c.Create(FactId);
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "downtime_auto_grid_add",
                        Group = "grid_tools",
                        Enabled = true,
                        Title = "Автозаполнение",
                        ButtonUse = true,
                        MenuUse = false,
                        ButtonControl = AutoPasteDowntime,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = async () =>
                        {
                            await AutoDowntimePlan();
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "downtime_grid_edit",
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
                            var item = DowntimeGrid.SelectedItem;

                            var de = new DowntimeEdit();
                            de.Edit(item.CheckGet("DOPL_ID").ToInt(), item.CheckGet("DTTM_START"), item.CheckGet("DTTM_END"));
                        },
                        CheckEnabled = () =>
                        {
                            if (DowntimeGrid.Items.Count > 0 && DowntimeGrid.SelectedItem.CheckGet("DOPL_ID").ToInt() > 0)
                            {
                                return true;
                            }

                            return false;
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "downtime_grid_delete",
                        Group = "grid_tools",
                        Enabled = true,
                        Title = "Удалить",
                        ButtonUse = true,
                        MenuUse = true,
                        ButtonControl = DeleteButton,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var id = DowntimeGrid.SelectedItem.CheckGet("DOPL_ID").ToInt();
                            var d = new DialogWindow($"Вы действительно хотите удалить простой №{id}", "Операция", "", DialogWindowButtons.YesNo);

                            if ((bool)d.ShowDialog())
                            {
                                DowntimePlanDelete(id);
                            }
                        },
                        CheckEnabled = () =>
                        {
                            if (DowntimeGrid.Items.Count > 0 && DowntimeGrid.SelectedItem.CheckGet("DOPL_ID").ToInt() > 0)
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

            FromDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
            ToDate.Text = DateTime.Now.AddDays(7).ToString("dd.MM.yyyy");
        }

        private void GridInit()
        {
            var column = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "День",
                    Path = "DAY",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 6
                },
                new DataGridHelperColumn
                {
                    Header = "Начало",
                    Path = "DTTM_START",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 15
                },
                new DataGridHelperColumn
                {
                    Header = "Окончание",
                    Path = "DTTM_END",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 15
                },
                new DataGridHelperColumn
                {
                    Header = "Станок",
                    Path = "NAME2",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 8
                },
                new DataGridHelperColumn
                {
                    Header = "Причина простоя",
                    Path = "DESCRIPTION",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 35
                },
                new DataGridHelperColumn
                {
                    Header = "ИД",
                    Path = "DOPL_ID",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2 = 10,
                    //Visible = false,
                },
                new DataGridHelperColumn
                {
                    Header = "ID_ST",
                    Path = "ID_ST",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2 = 10,
                    Visible = false,
                },
                new DataGridHelperColumn
                {
                    Header = "ID_IDLE_DETAILS",
                    Path = "ID_IDLE_DETAILS",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2 = 10,
                    Visible = false,
                },
            };
            DowntimeGrid.SetColumns(column);
            DowntimeGrid.EnableSortingGrid = false;
            DowntimeGrid.EnableFiltering = true;
            DowntimeGrid.SetPrimaryKey("DOPL_ID");
            DowntimeGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            DowntimeGrid.ItemsAutoUpdate = false;
            DowntimeGrid.AutoUpdateInterval = 0;
            DowntimeGrid.OnLoadItems = LoadItems;
            DowntimeGrid.SearchText = SearchText;
            DowntimeGrid.Commands = Commander;
            DowntimeGrid.Init();
        }


        private async Task AutoDowntimePlan()
        {
            bool resume = true;

            {
                var f = FromDate.Text.ToDateTime();
                var t = ToDate.Text.ToDateTime();

                if (DateTime.Compare(f, t) > 0)
                {
                    var d = new DialogWindow("Дата начала периода не может быть больше даты окончания периода", "Проверка данных");
                    d.ShowDialog();
                    resume = false;
                }
            }

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "PlannedDowntime");
                q.Request.SetParam("Action", "AutoDowntimePlan");
                q.Request.SetParam("DT_FROM", FromDate.Text);
                q.Request.SetParam("DT_TO", ToDate.Text);
                q.Request.SetParam("FACT_ID", FactId.ToString());

                await Task.Run(() => q.DoQuery());

                if (q.Answer.Status == 0)
                {
                    DowntimeGrid.LoadItems();
                }
            }
        }

        private async void LoadItems()
        {
            bool resume = true;

            {
                var f = FromDate.Text.ToDateTime();
                var t = ToDate.Text.ToDateTime();

                if (DateTime.Compare(f, t) > 0)
                {
                    var d = new DialogWindow("Дата начала периода не может быть больше даты окончания периода", "Проверка данных");
                    d.ShowDialog();
                    resume = false;
                }
            }

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "PlannedDowntime");
                q.Request.SetParam("Action", "List");
                q.Request.SetParam("DT_FROM", FromDate.Text);
                q.Request.SetParam("DT_TO", ToDate.Text);
                q.Request.SetParam("FACT_ID", FactId.ToString());

                await Task.Run(() => q.DoQuery());

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                    if (result != null)
                    {
                        var i = ListDataSet.Create(result, "ITEMS");

                        DowntimeGrid.UpdateItems(i);

                        if (DowntimeGrid.Items.Count > 0)
                        {
                            DowntimeGrid.SelectRowFirst();
                        }

                        Refresh.Style = (Style)Refresh.TryFindResource("Button");
                    }
                } 
            }
        }

        private async void DowntimePlanDelete(int id)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PlannedDowntime");
            q.Request.SetParam("Action", "Delete");
            q.Request.SetParam("DOPL_ID", id.ToString());

            await Task.Run(() => q.DoQuery());

            if (q.Answer.Status == 0)
            {
                DowntimeGrid.LoadItems();
            }
        }

        private void FactoryType_SelectedItemChanged(System.Windows.DependencyObject d, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            var s = (SelectBox)d;

            FactId = s.SelectedItem.Key.ToInt();

            DowntimeGrid.LoadItems();
        }

        private void DatePick_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            Refresh.Style = (Style)Refresh.TryFindResource("FButtonPrimary");
        }
    }
}