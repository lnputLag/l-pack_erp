using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Service.Printing;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Список техкарт для литой тары
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class TechnologicalMapTabManagers : ControlBase
    {
        public TechnologicalMapTabManagers()
        {
            InitializeComponent();

            RoleName = "[erp]molded_contnr_techcard";
            ControlTitle = "Техкарты ЛТ";
            DocumentationUrl = "/doc/l-pack-erp/preproduction/tk_grid/molded_container";
            TabName = "TechnologicalMapTabManagers";
            OnLoad = () =>
            {
                LoadRef();
                LoadStatuses();
                GridInit();
            };

            OnUnload = () =>
            {
                Grid.Destruct();
            };

            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverGroup == "MoldedContainerTechCard")
                {
                    if (msg.ReceiverName == ControlName)
                    {
                        switch (msg.Action)
                        {
                            case "Refresh":
                                Grid.LoadItems();
                                break;
                            case "Rework":
                                if (msg.Message == "4")
                                {
                                    TechMapChangeDesignStatus(1);
                                }
                                Grid.LoadItems();
                                break;
                        }
                    }
                }
                if (msg.ReceiverGroup == "PreproductionSample")
                {
                    if (msg.ReceiverName == ControlName)
                    {
                        switch (msg.Action)
                        {
                            case "Refresh":
                                Grid.LoadItems();
                                break;
                        }
                    }
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

            {
                Commander.SetCurrentGroup("main");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "Show",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Показать",
                        Description = "Показать",
                        ButtonUse = true,
                        ButtonName = "ShowButton",
                        MenuUse = false,
                        AccessLevel = Role.AccessMode.ReadOnly,
                        Action = () =>
                        {
                            Grid.LoadItems();
                            ShowButton.Style = (Style)ShowButton.TryFindResource("Button");
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "Print",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Печать",
                        Description = "Распечатать информацию о техкарте",
                        ButtonUse = true,
                        ButtonName = "PrintButton",
                        MenuUse = true,
                        AccessLevel = Role.AccessMode.ReadOnly,
                        Action = () =>
                        {
                            TechcardPrint();
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

                Commander.SetCurrentGridName("Grid");
                {
                    Commander.SetCurrentGroup("standart");
                    {
                        Commander.Add(new CommandItem()
                        {
                            Name = "open",
                            Title = "Открыть",
                            MenuUse = true,
                            HotKey = "Return|DoubleCLick",
                            ButtonUse = false,
                            ButtonName = "",
                            Description = "Открыть техкрту",
                            AccessLevel = Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var k = Grid.GetPrimaryKey();
                                var id = Grid.SelectedItem.CheckGet(k).ToInt();
                                if (id != 0)
                                {
                                    if (Grid.SelectedItem.CheckGet("STATUS_ID").ToInt() != 1)
                                    {
                                        var techCardFrame = new MoldedContainerTechCard();
                                        techCardFrame.ReceiverName = ControlName;
                                        techCardFrame.Edit(id);
                                    }
                                }
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;
                                var k = Grid.GetPrimaryKey();
                                var row = Grid.SelectedItem;
                                if (row.CheckGet(k).ToInt() != 0)
                                {
                                    result = true;
                                }
                                return result;
                            },
                        });
                        

                    }
                    Commander.SetCurrentGroup("special");
                    {
                        // Чат
                        Commander.Add(new CommandItem()
                        {
                            Name = "open_chat_with_client",
                            Enabled = true,
                            Title = "Открыть чат с клиентом",
                            Group = "chatoperation",
                            MenuUse = true,
                            ButtonName = "",
                            ButtonUse = false,
                            AccessLevel = Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                OpenChat(0);
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "open_inner_chat",
                            Enabled = true,
                            Title = "Открыть внутренний чат",
                            Group = "chatoperation",
                            MenuUse = true,
                            ButtonName = "",
                            ButtonUse = false,
                            AccessLevel = Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                OpenChat(1);
                            },
                        });
                        // Файл
                        Commander.Add(new CommandItem()
                        {
                            Name = "open_attachments",
                            Enabled = true,
                            Title = "Прикрепленные файлы",
                            Group = "fileoperation",
                            MenuUse = true,
                            ButtonName = "",
                            ButtonUse = false,
                            AccessLevel = Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                OpenAttachments();
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "show_tech_card",
                            Enabled = true,
                            Title = "Показать файл ТК",
                            Group = "fileoperation",
                            MenuUse = true,
                            ButtonName = "ShowFileTkButton",
                            ButtonUse = true,
                            AccessLevel = Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var filePath = Grid.SelectedItem.CheckGet("TK_FILE_PATH");
                                Central.OpenFile(filePath);
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;

                                var filePath = Grid.SelectedItem.CheckGet("TK_FILE_PATH");
                                if (!filePath.IsNullOrEmpty())
                                {
                                    if (File.Exists(filePath))
                                    {
                                        result = true;
                                    }
                                }
                                return result;
                            },
                        });
                        Commander.Add(new CommandItem()
                        {
                            Name = "show_design",
                            Enabled = false,
                            Title = "Открыть файл дизайна",
                            MenuTitle = "Открыть файл дизайна",
                            Group = "fileoperation",
                            MenuUse = true,
                            ButtonName = "",
                            ButtonUse = false,
                            AccessLevel = Role.AccessMode.FullAccess,
                            Action = () =>
                            {
                                var k = Grid.GetPrimaryKey();
                                var id = Grid.SelectedItem.CheckGet(k).ToInt();
                                if (id != 0)
                                {
                                    string designFilePath = Grid.SelectedItem.CheckGet("DESIGN_FILE_PATH");
                                    if (!designFilePath.IsNullOrEmpty())
                                    {
                                        Central.OpenFile(designFilePath);
                                    }
                                }
                            },
                            CheckEnabled = () =>
                            {
                                var result = false;

                                var filePath = Grid.SelectedItem.CheckGet("DESIGN_FILE_PATH");
                                if (!filePath.IsNullOrEmpty())
                                {
                                    if (File.Exists(filePath))
                                    {
                                        result = true;
                                    }
                                }
                                return result;
                            },
                        });
                    }
                    Commander.Add(new CommandItem()
                    {
                        Name = "show_history",
                        Enabled = true,
                        Title = "История изменений",
                        Group = "history",
                        MenuUse = true,
                        ButtonUse = false,
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var historyFrame = new MoldedContainerTechCardHistory();
                            historyFrame.ReceiverName = TabName;
                            historyFrame.IdTk = SelectedItem.CheckGet("ID").ToInt();
                            historyFrame.Show();
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var row = Grid.SelectedItem;
                            if (row.CheckGet("ID").ToInt() > 0)
                            {
                                result = true;
                            }
                            return result;
                        },
                    });
                }

                Commander.Init(this);
            }
        }

        /// <summary>
        /// Название вкладки
        /// </summary>
        public string TabName;

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        private void GridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="SKU_CODE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=9,
                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=34,
                },

                new DataGridHelperColumn
                {
                    Header="Дата создания",
                    Path="CREATED_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=8,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header="Тип контейнера",
                    Path="CONTAINER_TYPE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=11,
                },
                new DataGridHelperColumn
                {
                    Header="Статус",
                    Path="STATUS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Дата внесения в ассортимент",
                    Path="ACCEPTED_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=8,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header="Схема производства",
                    Path="PRODUCTION_SCHEME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Дизайн",
                    Path="DESIGN_STATUS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=13,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("DESIGN").ToInt() == 1  && row.CheckGet("STATUS_ID").ToInt() !=7)
                                {
                                    color = HColor.Yellow;
                                }
                                if( row.CheckGet("DESIGN").ToInt() == 2  && row.CheckGet("STATUS_ID").ToInt() !=7)
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
                    Header="Файл дизайна",
                    Path="HAS_DESIGN_FILE",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Файлы клиента",
                    Path="HAS_CLIENT_FILE",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header="Покупатель",
                    Path="BUYER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=25,
                },
                new DataGridHelperColumn
                {
                    Header="Дата подтверждения клиентом",
                    Path="APPROVED_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=8,
                    Format="dd.MM.yyyy",
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                // тех карта согласована с клиентом
                                if( row.CheckGet("APPROVED_DTTM") != "" )
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
                        {
                            StylerTypeRef.ForegroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                // тех карта согласована с клиентом
                                if( row.CheckGet("APPROVED_DTTM") != "" )
                                {
                                    color = HColor.BlackFG;
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
                    Header="Создатель",
                    Path="CREATOR",
                    ColumnType=ColumnTypeRef.String,
                    Width2=14,
                },
                new DataGridHelperColumn
                {
                    Header="Дизайнер",
                    Path="DESIGNER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=14,
                },
                new DataGridHelperColumn
                {
                    Header="Чат с клиентом",
                    Path="CNT_UNREAD",
                    Width2=10,
                    ColumnType=ColumnTypeRef.Double,
                    Format = "N0",
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if( row.CheckGet("COUNT_MSG").ToInt() > 0 )
                                {
                                    color = HColor.Yellow;
                                }
                                if( row.CheckGet("CNT_UNREAD").ToInt() > 0 )
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
                        {
                            StylerTypeRef.ForegroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = HColor.BlackFG;


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
                    Header="Чат с коллегами",
                    Path="EMPL_UNREAD",
                    Width2=10,
                    ColumnType=ColumnTypeRef.Double,
                    Format = "N0",
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";
                                if( row.CheckGet("EMPL_COUNT_MSG").ToInt() > 0 )
                                {
                                    color = HColor.Yellow;
                                }
                                if( row.CheckGet("EMPL_UNREAD").ToInt() > 0 )
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
                        {
                            StylerTypeRef.ForegroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = HColor.BlackFG;


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
                    Header="Примечание клиента",
                    Path="CUSTOMER_NOTE",
                    Width2=11,
                    ColumnType=ColumnTypeRef.String,
                },
                new DataGridHelperColumn
                {
                    Header="Кол-во доработок",
                    Path="REWORK_CNT",
                    ColumnType=ColumnTypeRef.Double,
                    Format = "N0",
                    Width2=11,
                },
                new DataGridHelperColumn
                {
                    Header="Причина доработок",
                    Path="ALL_REASONS",
                    Width2=15,
                    ColumnType=ColumnTypeRef.String,
                },
                new DataGridHelperColumn
                {
                    Header="Статус",
                    Path="STATUS_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ИД чата",
                    Path="CHAT_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Все сообщения с клиентом",
                    Path="COUNT_MSG",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Все сообщения с колегами",
                    Path="EMPL_COUNT_MSG",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="design",
                    Path="DESIGN",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ИД клиента",
                    Path="BUYER_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Путь к файлу дизайна",
                    Path="DESIGN_FILE_PATH",
                    Width2=12,
                    ColumnType=ColumnTypeRef.String,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Путь к файлу ТК",
                    Path="TK_FILE_PATH",
                    Width2=12,
                    ColumnType=ColumnTypeRef.String,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ИД дизайнера",
                    Path="DESIGNER_EMPL_ID",
                    Width2=12,
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ИД дизайнера",
                    Path="CONTAINER_TYPE_ID",
                    Width2=12,
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            Grid.SetColumns(columns);

            Grid.SetPrimaryKey("ID");
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.SearchText = GridSearch;
            Grid.Toolbar = GridToolbar;
            Grid.Commands = Commander;
            // Раскраска строк
            Grid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                // Цвета фона строк
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=System.Windows.DependencyProperty.UnsetValue;
                        var color = "";
                        int currentStatus = 0;
                        currentStatus = row.CheckGet("STATUS_ID").ToInt();
                        switch (currentStatus)
                        {
                            // Новая
                            case 1:
                                color = HColor.Blue;
                                break;
                            // Отклонена
                            case 2:
                                color = HColor.Red;
                                break;
                            // Готова
                            case 6:
                                color = HColor.Green;
                                break;
                            // Архив
                            case 7:
                                color = HColor.Olive;
                                break;
                            default:
                                color = HColor.White; 
                                break;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
                {
                    DataGridHelperColumn.StylerTypeRef.ForegroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var foreColor = "";
                        int currentStatus = row.CheckGet("STATUS_ID").ToInt();
                        if (currentStatus == 4 || currentStatus == 5)
                        {
                            foreColor = HColor.BlueDark;
                        }

                        if (!string.IsNullOrEmpty(foreColor))
                        {
                            result=foreColor.ToBrush();
                        }

                        return result;
                    }
                }
            };

            Grid.OnLoadItems = LoadItems;
            Grid.OnFilterItems = FilterItems;
            Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    SelectedItem = selectedItem;
                }
                
            };
            Grid.Init();
        }

        /// <summary>
        /// Загрузка справочников
        /// </summary>
        public async void LoadRef()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "GetRef");
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
                    var ds = ListDataSet.Create(result, "BUYERS");
                    var buyerList = new Dictionary<string, string>()
                    {
                        { "-1", "Все" },
                    };
                    foreach (var item in ds.Items) {
                        buyerList.CheckAdd(item["ID"].ToInt().ToString(), item.CheckGet("NAME"));
                    }
                    BuyerName.Items = buyerList;
                    BuyerName.SetSelectedItemByKey("-1");
                }
            }
        }



        /// <summary>
        /// Загрузка справочников
        /// </summary>
        public async void LoadStatuses()
        {
            var status = new Dictionary<string, string>()
            {
                { "-1", "Все" },
                { "1", "Новые" },
                { "2", "Отклоненные" },
                { "3", "В работе" },
                { "4", "На согласовании" },
                { "5", "Согласованные" },
                { "6", "Готовые" }
            };
            TkStatus.Items = status;

            TkStatus.SetSelectedItemByKey("-1");
        }


        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        public async void LoadItems()
        {
            Grid.Toolbar.IsEnabled = false;
            Grid.ShowSplash();

            string showArchived = (bool)ShowArchivedCheckBox.IsChecked ? "1" : "0";

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "ListTechCard");
            q.Request.SetParam("SHOW_ARCHIVED", showArchived);
            q.Request.SetParam("STATUS", TkStatus.SelectedItem.Key.ToString());
            
            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "TECHCARD");
                    Grid.UpdateItems(ds);
                }
            }
            Grid.HideSplash();
            Grid.Toolbar.IsEnabled = true;
        }

        /// <summary>
        /// Фильтрация строк табщицы
        /// </summary>
        public void FilterItems()
        {
            if (Grid.Items != null)
            {
                if (Grid.Items.Count > 0)
                {
                    bool doFilteringByBuyer = false;
                    int buyerId = BuyerName.SelectedItem.Key.ToInt();
                    if (buyerId > 0)
                    {
                        doFilteringByBuyer = true;
                    }

                    // Фильтрация строк
                    if (doFilteringByBuyer)
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in Grid.Items)
                        {
                            bool includeByBuyer = true;
                            if (doFilteringByBuyer)
                            {
                                includeByBuyer = false;
                                if (row.CheckGet("BUYER_ID").ToInt() == buyerId)
                                {
                                    includeByBuyer = true;
                                }
                            }

                            if (includeByBuyer)
                            {
                                items.Add(row);
                            }
                        }

                        Grid.Items = items;
                    }
                }
            }
        }

        /// <summary>
        /// Удаление техкарты
        /// </summary>
        /// <param name="id"></param>
        private async void DeleteTechCard(int id)
        {
            var dw = new DialogWindow("Вы действительно хотите удалить техкарту?", "Удаление", "", DialogWindowButtons.NoYes);
            if ((bool)dw.ShowDialog())
            {
                if (dw.ResultButton == DialogResultButton.Yes)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Preproduction");
                    q.Request.SetParam("Object", "MoldedContainer");
                    q.Request.SetParam("Action", "DeleteTechCard");
                    q.Request.SetParam("ID", id.ToString());

                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                        if (result.ContainsKey("ITEMS"))
                        {
                            Grid.LoadItems();
                        }
                    }
                    else if (q.Answer.Error.Code == 145)
                    {
                        q.ProcessError();
                    }
                }
            }
        }

        /// <summary>
        /// Отправка ТК на подтверждение клиенту
        /// </summary>
        /// <param name="p"></param>
        public async void TkSendToConfirm()
        {
            Dictionary<string, string> p = new Dictionary<string, string>();

            p.CheckAdd("ID", Grid.SelectedItem.CheckGet("ID"));

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "SetConfirmStatus");
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
                    LoadItems();
                }
            }
            else if (q.Answer.Error.Code == 145)
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Изменить статус техкарты
        /// </summary>
        private async void TechMapChangeStatus(int status)
        {
            
            int id = Grid.SelectedItem.CheckGet("ID").ToInt();
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "SetStatus");
            q.Request.SetParam("ID", id.ToString());
            q.Request.SetParam("STATUS_ID", status.ToString());
            q.Request.SetParam("CONFIRMED_DTTM_FLAG", "1");

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Grid.LoadItems();
            }
            else if (q.Answer.Error.Code == 145)
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Изменение статуса дизайна
        /// </summary>
        private async void TechMapChangeDesignStatus(int status)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "SetDesignStatus");
            q.Request.SetParam("ID", Grid.SelectedItem.CheckGet("ID").ToString());
            q.Request.SetParam("STATUS_ID", status.ToString());

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Grid.LoadItems();
            }
            else if (q.Answer.Error.Code == 145)
            {
                q.ProcessError();
            }
        }


        /// <summary>
        /// Изменение ИД дизайнера
        /// type: 1 - установить себя, 0 - удалить себя
        /// </summary>
        private async void TechMapChangeDesignerId(int type)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "SetDesignerId");
            q.Request.SetParam("ID", Grid.SelectedItem.CheckGet("ID").ToString());
            q.Request.SetParam("TYPE", type.ToString());

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Grid.LoadItems();
            }
            else if (q.Answer.Error.Code == 145)
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Привязывает выбранный файл дизайна к техкарте
        /// </summary>
        /// <param name="id"></param>
        private async void BindDesign(int id)
        {
            var fd = new OpenFileDialog();
            fd.Filter = "CorelDraw (*.cdr)|*.cdr|Все файлы (*.*)|*.*";
            fd.FilterIndex = 0;
            fd.InitialDirectory = "\\\\file-server-4\\Техкарты\\_Дизайн\\Рисунки\\_Литая тара (яичные лотки)";

            var fdResult = (bool)fd.ShowDialog();

            if (fdResult)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "MoldedContainer");
                q.Request.SetParam("Action", "BindDesignFile");
                q.Request.SetParam("ID", id.ToString());
                q.Request.SetParam("DESIGN_FILE_PATH", fd.FileName);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                    if (result.ContainsKey("ITEM"))
                    {
                        Grid.LoadItems();
                    }
                }
                else if (q.Answer.Error.Code == 145)
                {
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// Создание копии техкарты
        /// </summary>
        /// <param name="id"></param>
        private async void CreateCopy(int id)
        {
            bool resume = false;
            var dw = new DialogWindow("Вы действительно хотите создать копию выбранной техкарты?", "Создать копию", "", DialogWindowButtons.YesNo);
            if ((bool)dw.ShowDialog())
            {
                if (dw.ResultButton == DialogResultButton.Yes)
                {
                    resume = true;
                }
            }

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "MoldedContainer");
                q.Request.SetParam("Action", "CreateCopy");
                q.Request.SetParam("ID", id.ToString());

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                    if (result.ContainsKey("ITEM"))
                    {
                        Grid.LoadItems();
                    }
                }
                else if (q.Answer.Error.Code == 145)
                {
                    q.ProcessError();
                }
            }
        }

        private void ShowArchivedCheckBox_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        private void UpdateGridItems(System.Windows.DependencyObject d, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");

        }

        /// <summary>
        /// Открытие вкладки с приложенными файлами
        /// </summary>
        private void OpenAttachments()
        {
            if (SelectedItem != null)
            {
                var fraimeFiles = new MoldedContainerFiles();
                fraimeFiles.TkId = SelectedItem.CheckGet("ID").ToInt();
                if (SelectedItem.CheckGet("SKU_CODE").ToString() != "")
                {
                    fraimeFiles.TechCardName = SelectedItem.CheckGet("SKU_CODE").ToString() + " " + SelectedItem.CheckGet("NAME").ToString();
                }
                else
                {
                    fraimeFiles.TechCardName = SelectedItem.CheckGet("NAME").ToString();
                }
                fraimeFiles.ReturnTabName = TabName;
                fraimeFiles.Show();
            }
        }

        /// <summary>
        /// Открытие вкладки с чатом по образцу
        /// </summary>
        /// <param name="chatType">Тип чата: 0 - чат с клиентом, 1 - чат с коллегами</param>
        private void OpenChat(int chatType = 0)
        {
            if (SelectedItem != null)
            {
                var chatFrame = new MoldedContainerChat();
                chatFrame.ObjectId = SelectedItem.CheckGet("ID").ToInt();
                chatFrame.ReceiverName = TabName;
                chatFrame.ChatObject = "TechMap";
                chatFrame.ChatType = chatType;
                chatFrame.ChatId = SelectedItem.CheckGet("CHAT_ID").ToInt();
                chatFrame.Edit();
            }
        }

        /// <summary>
        /// Печать информации по техкарте
        /// </summary>
        private async void TechcardPrint(int chatType = 0)
        {
            if (SelectedItem != null)
            {
                int id = SelectedItem.CheckGet("ID").ToInt();

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "MoldedContainer");
                q.Request.SetParam("Action", "GetTechcardInformationDocument");
                q.Request.SetParam("ID", id.ToString());

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    if (q.Answer.Type == LPackClientAnswer.AnswerTypeRef.File)
                    {
                        Central.OpenFile(q.Answer.DownloadFilePath);

                    }
                    else
                    {
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// обработчик системы навигации по URL
        /// </summary>
        public void ProcessNavigation()
        {

        }
    }
}
