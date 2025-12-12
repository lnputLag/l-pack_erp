using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using static Client.Interfaces.Main.DataGridHelperColumn;
using Client.Assets.HighLighters;


namespace Client.Interfaces.Production.ProductTestTrialsKsh
{
    /// <summary>
    /// Логика взаимодействия для Tests.xaml
    /// </summary>
    /// <author>volkov_as</author>
    public partial class TestsTab : ControlBase
    {
        public TestsTab()
        {
            RoleName = "[erp]prod_testing_trial_ksh";

            InitializeComponent();

            ControlTitle = "Испытания";

            OnLoad = () =>
            {
                MainGridInit();
                FormInit();
                LoadCarton();
            };

            OnUnload = () =>
            {
                MainGrid.Destruct();
            };

            OnFocusGot = () =>
            {
                MainGrid.ItemsAutoUpdate = true;
            };

            OnFocusLost = () =>
            {
                MainGrid.ItemsAutoUpdate = false;
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
                        AccessLevel = Role.AccessMode.ReadOnly,
                        HotKey = "F1",
                        Action = () =>
                        {
                            Central.ShowHelp(DocumentationUrl);
                        },
                    });
                }

                Commander.SetCurrentGridName("MainGrid");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "tests_main_grid_refresh",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить данные",
                        ButtonUse = true,
                        ButtonName = "Refresh",
                        AccessLevel = Role.AccessMode.ReadOnly,
                        MenuUse = true,
                        Action = () =>
                        {
                            MainGrid.LoadItems();
                        },
                    });

                    Commander.Add(new CommandItem()
                    {
                        Name = "tests_main_grid_to_excel",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "В Excel",
                        Description = "Экспорт данных в excel",
                        ButtonUse = true,
                        ButtonName = "ExportToExcel",
                        AccessLevel = Role.AccessMode.ReadOnly,
                        MenuUse = true,
                        Action = () =>
                        {
                            MainGrid.ItemsExportExcel();
                        },
                    });

                    Commander.Add(new CommandItem()
                    {
                        Name = "tests_main_grid_show_tech_card",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Показать ТК",
                        Description = "Показать тех.карту",
                        ButtonUse = true,
                        ButtonName = "ShowTechCard",
                        AccessLevel = Role.AccessMode.ReadOnly,
                        MenuUse = true,
                        Action = () =>
                        {
                            TechnologicalMapShow();
                        },
                    });
                }

                Commander.Init(this);
            }


        }

        public int Marka { get; set; } = -1;
        public int Profile { get; set; } = -1;
        public FormHelper Form { get; set; }
        public ListDataSet TestsList;
        public ListDataSet CartonList;

        public void FormInit()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="DATE_FROM",
                    FieldType=FormHelperField.FieldTypeRef.DateTime,
                    Default = DateTime.Now.AddMonths(-1).ToString("dd.MM.yyyy"),
                    Control= FromDate,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {
                        Refresh.Style = (Style)Refresh.TryFindResource("FButtonPrimary");
                    }
                },
                new FormHelperField()
                {
                    Path="DATE_TO",
                    FieldType=FormHelperField.FieldTypeRef.DateTime,
                    Default = DateTime.Now.ToString("dd.MM.yyyy"),
                    Control= ToDate,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {
                        Refresh.Style = (Style)Refresh.TryFindResource("FButtonPrimary");
                    }
                },
                new FormHelperField()
                {
                    Path = "PROFILE",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Default = "-1",
                    Control = SelectProfile,
                    ControlType = "SelectBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{},
                    OnChange = (FormHelperField f, string v) =>
                    {
                        LoadCarton();
                    },
                    QueryLoadItems = new RequestData()
                    {
                        Module = "ProductTestTrialKsh",
                        Object = "Profiles",
                        Action = "List",
                        AnswerSectionKey = "PROFILE",
                        OnComplete = (FormHelperField f, ListDataSet ds) =>
                        {
                            var row = new Dictionary<string, string>()
                            {
                                { "ID_PROF", "-1" },
                                { "PROFIL_NAME", "Все" }
                            };

                            ds.ItemsPrepend(row);
                            var list = ds.GetItemsList("ID_PROF", "PROFIL_NAME");
                            var c = (SelectBox)f.Control;
                            if (c != null)
                            {
                                c.Items = list;
                            }
                        }
                    }
                },
                new FormHelperField()
                {
                    Path = "MARKA",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = SelectMark,
                    Default = "-1",
                    ControlType = "SelectBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{},
                    OnChange = (FormHelperField f, string v) =>
                    {
                        LoadCarton();
                    },
                    QueryLoadItems = new RequestData()
                    {
                        Module = "ProductTestTrialKsh",
                        Object = "Marka",
                        Action = "List",
                        AnswerSectionKey = "MARKA",
                        OnComplete = (FormHelperField f, ListDataSet ds) =>
                        {
                            var row = new Dictionary<string, string>()
                            {
                                {"ID_MARKA", "-1"},
                                {"NAME_MARKA", "Все"}
                            };

                            ds.ItemsPrepend(row);
                            var list = ds.GetItemsList("ID_MARKA", "NAME_MARKA");
                            var c = (SelectBox)f.Control;
                            if (c != null)
                            {
                                c.Items = list;
                            }
                        }
                    },
                },
                new FormHelperField()
                {
                    Path = "CARTON",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = SelectCarton,
                    ControlType = "SelectBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{},
                    OnChange = (FormHelperField f, string v) =>
                    {
                        MainGrid.LoadItems();
                    }
                }
            };

            Form.SetFields(fields);
            Form.SetDefaults();
        }

        public void MainGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "Дата испытания",
                    Path = "TEST_DATE",
                    ColumnType = ColumnTypeRef.DateTime,
                    Format = "dd.MM.yyyy HH:mm:ss",
                    Width2 = 14
                },
                new DataGridHelperColumn
                {
                    Header = "Артикул",
                    Path = "ART",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 16
                },
                new DataGridHelperColumn
                {
                    Header = "Размеры",
                    Path = "SIZE_PRODUCT",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 12
                },
                new DataGridHelperColumn
                {
                    Header = "Профиль",
                    Path = "PROFIL_NAME",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 8
                },
                new DataGridHelperColumn
                {
                    Header = "Марка",
                    Path = "NAME_MARKA",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 24
                },
                new DataGridHelperColumn
                {
                    Header = "Клише",
                    Path = "CLICHE_FLAG",
                    ColumnType = ColumnTypeRef.Boolean,
                    Width2 = 7
                },
                new DataGridHelperColumn
                {
                    Header = "Штанцформа",
                    Path = "SHTANZ_FLAG",
                    ColumnType = ColumnTypeRef.Boolean,
                    Width2 = 11
                },
                new DataGridHelperColumn
                {
                    Header = "Линия",
                    Path = "ST_NAME",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 21
                },
                new DataGridHelperColumn
                {
                    Header = "ВСТ, Н",
                    Path = "BCT",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 8
                },
                new DataGridHelperColumn
                {
                    Header = "ВСТ 24, Н",
                    Path = "BCT_24",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 9
                },
                new DataGridHelperColumn
                {
                    Header = "Целевое ЕСТ, кН",
                    Path = "ECT_PLAN",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 13
                },
                new DataGridHelperColumn
                {
                    Header = "ЕСТ, кН",
                    Path = "ECT",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 7,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";

                                if ((row.CheckGet("ECT_DIF").ToDouble() >= 10) &&
                                    (row.CheckGet("ECT").ToDouble() != 0) && 
                                    (row.CheckGet("ECT_PLAN").ToDouble() != 0))
                                {
                                    color = HColor.Yellow;
                                }
                                else if ((row.CheckGet("ECT_DIF").ToDouble() <= -10) &&
                                         (row.CheckGet("ECT").ToDouble() != 0) &&
                                         (row.CheckGet("ECT_PLAN").ToDouble() != 0))
                                {
                                    color = HColor.Red;
                                }
                                else if ((row.CheckGet("ECT_DIF").ToDouble() > -10) &&
                                         (row.CheckGet("ECT_DIF").ToDouble() < -3) &&
                                         (row.CheckGet("ECT").ToDouble() != 0) &&
                                         (row.CheckGet("ECT_PLAN").ToDouble() != 0))
                                {
                                    color = HColor.Orange;
                                }


                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        },
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "ЕСТ 24, кН",
                    Path = "ECT_24",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 9
                },
                new DataGridHelperColumn
                {
                    Header = "ID_PROF",
                    Path = "ID_PROF",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 9,
                    Hidden = true
                },
                new DataGridHelperColumn
                {
                    Header = "ID_MARKA",
                    Path = "ID_MARKA",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 9,
                    Hidden = true
                },
                new DataGridHelperColumn
                {
                    Header = "IDC",
                    Path = "IDC",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 9,
                    Hidden = true
                },
            };
            MainGrid.SetColumns(columns);
            MainGrid.SetPrimaryKey("TEST_DATE");
            MainGrid.SearchText = SearchText;
            MainGrid.Toolbar = MainGridToolbar;
            MainGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            MainGrid.Toolbar = MainGridToolbar;

            MainGrid.OnLoadItems = LoadItems;

            MainGrid.OnFilterItems = () =>
            {
                if (MainGrid.Items.Count > 0)
                {
                    {
                        var v = Form.GetValues();
                        bool showAll = v.CheckGet("PROFILE") == "-1" && v.CheckGet("MARKA") == "-1" && v.CheckGet("CARTON") == "-1";

                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in MainGrid.Items)
                        {
                            var checkProfile = false;
                            var checkMark = false;
                            var checkCarton = false;

                            if (showAll)
                            {
                                items.Add(row);
                            }
                            else
                            {
                                if (row.CheckGet("ID_PROF") == v.CheckGet("PROFILE") || v.CheckGet("PROFILE") == "-1")
                                {
                                    checkProfile = true;
                                }

                                if (row.CheckGet("ID_MARKA") == v.CheckGet("MARKA") || v.CheckGet("MARKA") == "-1")
                                {
                                    checkMark = true;
                                }

                                if (row.CheckGet("IDC") == v.CheckGet("CARTON") || v.CheckGet("CARTON") == "-1")
                                {
                                    checkCarton = true;
                                }

                                if (checkCarton && checkMark && checkProfile)
                                {
                                    items.Add(row);
                                }
                            }
                        }

                        MainGrid.Items = items;
                    }
                }
            };

            MainGrid.Commands = Commander;

            MainGrid.Init();
        }

        /// <summary>
        /// Загрузка данных MainGrid
        /// </summary>
        public async void LoadItems()
        {
            bool resume = true;

            {
                var f = FromDate.Text.ToDateTime();
                var t = ToDate.Text.ToDateTime();
                if (DateTime.Compare(f, t) > 0)
                {
                    var msg = "Дата начала периода не может быть больше даты окончания периода";
                    var d = new DialogWindow(msg, "Проверка данных");
                    d.ShowDialog();
                    resume = false;
                }
            }

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "ProductTestTrialKsh");
                q.Request.SetParam("Object", "Tests");
                q.Request.SetParam("Action", "List");
                q.Request.SetParam("FROM_DATE", FromDate.Text);
                q.Request.SetParam("TO_DATE", ToDate.Text);

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
                        TestsList = ListDataSet.Create(result, "TESTS");
                        MainGrid.UpdateItems(TestsList);

                        Refresh.Style = (Style)Refresh.TryFindResource("Button");
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
            }
        }

        /// <summary>
        /// Обработчик для кнопки "Показать ТК"
        /// </summary>
        private void TechnologicalMapShow()
        {
            if (MainGrid.Items.Count > 0)
            {
                if (MainGrid.SelectedItem != null)
                {
                    var path = MainGrid.SelectedItem.CheckGet("PATHTK");
                    if (!string.IsNullOrEmpty(path))
                    {
                        if (File.Exists(path))
                        {
                            Central.OpenFile(path);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Запрос для получени списка картона
        /// </summary>
        private async void LoadCarton()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductTestTrialKsh");
            q.Request.SetParam("Object", "Carton");
            q.Request.SetParam("Action", "List");
            q.Request.SetParam("ID_MARKA", Marka.ToString());
            q.Request.SetParam("ID_PROF", Profile.ToString());

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
                    CartonList = ListDataSet.Create(result, "CARTON");

                    var initialItem = new Dictionary<string, string>()
                    {
                        {"-1", "Все"},
                    };

                    SelectCarton.SetItems(initialItem);

                    SelectCarton.AddItems(CartonList, "IDC", "DESCRIPTION");

                    SelectCarton.SetSelectedItemByKey("-1");
                }

                MainGrid.LoadItems();
            }
        }
    }
}
