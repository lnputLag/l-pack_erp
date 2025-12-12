using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction.Rig;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static Client.Interfaces.Preproduction.Rig.CuttingStampTab;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Логика взаимодействия для SampleCardboardKshTab.xaml
    /// </summary>
    public partial class SampleCardboardKshTab : ControlBase
    {
        public SampleCardboardKshTab()
        {
            InitializeComponent();
            ControlTitle = "Заготовки для образцов";
            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/samples_new/carton_samples";
            RoleName = "[erp]sample_cardboard_ksh";

            FactoryId = 2;

            OnLoad = () =>
            {
                CardboardGridInit();
                PreformGridInit();
            };

            OnUnload = () =>
            {
                CardboardGrid.Destruct();
                PreformGrid.Destruct();
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
                CardboardGrid.ItemsAutoUpdate = true;
                CardboardGrid.Run();
            };

            OnFocusLost = () =>
            {
                CardboardGrid.ItemsAutoUpdate = false;
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
                        CardboardGrid.LoadItems();
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
            Commander.SetCurrentGridName("CardboardGrid");
            {

            }
            Commander.SetCurrentGridName("PreformGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "editperform",
                    Enabled = true,
                    Title = "Изменить",
                    Group = "operations",
                    Description = "Изменить количество или место хранения заготовок",
                    HotKey = "Return|DoubleCLick",
                    ButtonUse = true,
                    ButtonName = "EditButton",
                    MenuUse = true,
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = PreformGrid.GetPrimaryKey();
                        var id = PreformGrid.SelectedItem.CheckGet(k).ToInt();

                        if (id != 0)
                        {
                            var editForm = new SampleCardboardEditQty();
                            editForm.ReceiverName = ControlName;
                            editForm.Edit(id);
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = PreformGrid.SelectedItem;
                        if (row != null)
                        {
                            result = true;
                        }
                        return result;
                    }
                });
            }
            Commander.Init(this);
        }

        public int FactoryId;

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
                        CardboardGrid.LoadItems();
                        break;
                }
            }
        }

        private void CardboardGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Номер",
                    Path="NUM",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Картон",
                    Path="NAME_CARTON",
                    ColumnType=ColumnTypeRef.String,
                    Width2=30,
                },
                new DataGridHelperColumn
                {
                    Header="Тип",
                    Path="COMPOSITION",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("COMPOSITION_TYPE").ToInt() > 2)
                                {
                                    color = HColor.Gray;
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
                    Header="Количество",
                    Path="QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("QTY").ToInt() < 6)
                                {
                                    color = HColor.Blue;
                                }

                                // Если картон лежит на приемке
                                if (row.CheckGet("IN_CELL").ToInt() == 1)
                                {
                                    color = HColor.Green;
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
                    Header="Масса",
                    Path="WEIGHT",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="ПЗ на картон",
                    Path="PZ_IS",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Номер ПЗ",
                    Path="NUM_PZ",
                    ColumnType=ColumnTypeRef.String,
                    Width2=14,
                },
                new DataGridHelperColumn
                {
                    Header="Последнее ПЗ",
                    Path="MAX_DATA",
                    ColumnType=ColumnTypeRef.String,
                    Width2=14,
                },
                new DataGridHelperColumn
                {
                    Header="Профиль",
                    Path="PROFILE_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Цвет",
                    Path="COMPOSITION_TYPE",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="В ячейке на приемку",
                    Path="IN_CELL",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            CardboardGrid.SetColumns(columns);
            CardboardGrid.SetPrimaryKey("ID");
            CardboardGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            CardboardGrid.SearchText = GridSearch;
            CardboardGrid.Commands = Commander;
            CardboardGrid.OnLoadItems = LoadCardboardItems;
            CardboardGrid.OnFilterItems = FilterCardboardItems;
            CardboardGrid.OnSelectItem = selectedItem =>
            {
                PerformLoadItems();
            };

            // Раскраска строк
            // Дата годности - хранение не больше 30 дней
            var expiredDate = DateTime.Now.Date.AddDays(-30);
            CardboardGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";
                        if (row.CheckGet("QTY").ToInt() > 0)
                        {
                            // Дата постеднего ПЗ раньше даты годности
                            if (!string.IsNullOrEmpty(row.CheckGet("MAX_DATA")))
                            {
                                var lastTaskDate = row.CheckGet("MAX_DATA").ToDateTime();
                                if (DateTime.Compare(lastTaskDate, expiredDate) < 0)
                                {
                                    color = HColor.Yellow;
                                }
                            }
                        }

                        if (!color.IsNullOrEmpty())
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                }
            };

            CardboardGrid.Init();
        }

        private void PreformGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Длина",
                    Path="LENGTH",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Ширина",
                    Path="WIDTH",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Количество",
                    Path="QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="В резерве",
                    Path="RESERVE_QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Хранение",
                    Path="RACK_PLACE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание",
                    Path="NOTE_CARTON",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="Дата производства",
                    Path="END_DTTM",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn
                {
                    Header="Номер задания",
                    Path="PZ_NUM",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
            };
            PreformGrid.SetColumns(columns);
            PreformGrid.SetPrimaryKey("ID");
            PreformGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            PreformGrid.AutoUpdateInterval = 0;
            PreformGrid.Commands = Commander;

            PreformGrid.Init();
        }

        /// <summary>
        /// Загрузка данных в таблицу картона
        /// </summary>
        public async void LoadCardboardItems()
        {
            CardboardGridToolbar.IsEnabled = false;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "SampleCardboards");
            q.Request.SetParam("Action", "ListRef");
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
                if (result.Count > 0)
                {
                    var ds = ListDataSet.Create(result, "SampleCardboards");
                    CardboardGrid.UpdateItems(ds);

                    var ProfileDS = ListDataSet.Create(result, "Profiles");

                    var list = new Dictionary<string, string>();
                    list.Add("0", "");
                    list.AddRange<string, string>(ProfileDS.GetItemsList("ID", "NAME"));
                    Profile.Items = list;
                    Profile.SetSelectedItemByKey("0");
                }
            }

            CardboardGridToolbar.IsEnabled = true;
        }

        /// <summary>
        /// Загрузка данных в таблицу заготовок
        /// </summary>
        public async void PerformLoadItems()
        {
            PreformGridToolbar.IsEnabled = false;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "SampleCardboards");
            q.Request.SetParam("Action", "List");
            q.Request.SetParam("IDC", CardboardGrid.SelectedItem.CheckGet("ID"));
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
                if (result.Count > 0)
                {
                    EditButton.IsEnabled = true;
                    var PreformsDS = ListDataSet.Create(result, "SamplePreforms");
                    PreformGrid.UpdateItems(PreformsDS);
                    if (PreformsDS.Items.Count == 0)
                    {
                        EditButton.IsEnabled = false;
                    }
                }
            }

            PreformGridToolbar.IsEnabled = true;
        }

        /// <summary>
        /// Фильтрация списка картона
        /// </summary>
        public void FilterCardboardItems()
        {
            if (CardboardGrid.Items != null)
            {
                if (CardboardGrid.Items.Count > 0)
                {
                    bool allRecords = (bool)AllCardboardCheckBox.IsChecked;

                    int profileId = Profile.SelectedItem.Key.ToInt();

                    var list = new List<Dictionary<string, string>>();
                    foreach (var item in CardboardGrid.Items)
                    {
                        bool include = true;
                        if (!allRecords && (item.CheckGet("QTY").ToInt() == 0))
                        {
                            include = false;
                        }

                        if ((profileId > 0) && (profileId != item.CheckGet("PROFILE_ID").ToInt()))
                        {
                            include = false;
                        }

                        if (include)
                        {
                            list.Add(item);
                        }
                    }

                    CardboardGrid.Items = list;
                }
            }
        }

        private void RevisionList_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AllSamplesCheckBox_Click(object sender, RoutedEventArgs e)
        {
            CardboardGrid.LoadItems();
        }

        private void Profile_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CardboardGrid.UpdateItems();
        }
    }
}
