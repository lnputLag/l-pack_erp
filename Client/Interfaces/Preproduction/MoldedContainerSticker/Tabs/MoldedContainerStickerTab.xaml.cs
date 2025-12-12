using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Список этикеток для литой тары
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class MoldedContainerStickerTab : ControlBase
    {
        public MoldedContainerStickerTab()
        {
            InitializeComponent();
            ControlTitle = "Этикетки ЛТ";
            DocumentationUrl = "/doc/l-pack-erp/preproduction/sticker_list";
            RoleName = "[erp]molded_contnr_sticker";

            OnLoad = () =>
            {
                InitGrid();
            };


            OnUnload = () =>
            {
                Grid.Destruct();
            };

            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverGroup == "PreproductionContainer")
                {
                    if (msg.ReceiverName == ControlName)
                    {
                        ProcessCommand(msg.Action, msg);
                    }
                }
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

                Commander.SetCurrentGridName("Grid");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "create",
                        Title = "Создать",
                        MenuUse = true,
                        HotKey = "Insert",
                        ButtonUse = true,
                        ButtonName = "CreateButton",
                        Description = "Создание новой этикетки",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var stickerFrame = new MoldedContainerSticker();
                            stickerFrame.ReceiverName = ControlName;
                            stickerFrame.Edit();
                        },
                        CheckEnabled = () =>
                        {
                            var result = true;
                            return result;
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "edit",
                        Title = "Изменить",
                        MenuUse = true,
                        HotKey = "Return|DoubleCLick",
                        ButtonUse = true,
                        ButtonName = "EditButton",
                        Description = "Внесение изменений в этикетку",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var k = Grid.GetPrimaryKey();
                            var id = Grid.SelectedItem.CheckGet(k).ToInt();
                            if (id != 0)
                            {
                                var stickerFrame = new MoldedContainerSticker();
                                stickerFrame.ReceiverName = ControlName;
                                stickerFrame.Edit(id);
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
                    Commander.Add(new CommandItem()
                    {
                        Name = "show_image",
                        Enabled = true,
                        Title = "Дизайн",
                        MenuUse = true,
                        ButtonName = "ShowDesignButton",
                        AccessLevel = Role.AccessMode.ReadOnly,
                        ButtonUse = true,
                        Action = () =>
                        {
                            var k = Grid.GetPrimaryKey();
                            var id = Grid.SelectedItem.CheckGet(k).ToInt();
                            if (id != 0)
                            {
                                bool imageExists = Grid.SelectedItem.CheckGet("IMAGE_IS").ToBool();
                                if (imageExists)
                                {
                                    ShowImageFile(id);
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
                                bool imageExists = row.CheckGet("IMAGE_IS").ToBool();
                                if (imageExists)
                                {
                                    result = true;
                                }
                            }
                            return result;
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "bindtechcard",
                        Enabled = true,
                        Title = "Привязать техкарту",
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
                                var techcardSelectFrame = new MoldedContainerStickerTechcardSelect();
                                techcardSelectFrame.ReceiverName = ControlName;
                                techcardSelectFrame.StickerId = id;
                                techcardSelectFrame.Show();
                            }
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            var k = Grid.GetPrimaryKey();
                            var row = Grid.SelectedItem;
                            if (row.CheckGet(k).ToInt() != 0)
                            {
                                if (row.CheckGet("TECHCARD_CNT").ToInt() == 0)
                                {
                                    result = true;
                                }
                            }
                            return result;
                        },
                    });
                }
            }
            Commander.Init(this);
        }

        /// <summary>
        /// Обработка общих сообщений
        /// </summary>
        /// <param name="action"></param>
        /// <param name="obj"></param>
        private void ProcessCommand(string command, ItemMessage obj = null)
        {
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
        /// Инициализация грида
        /// </summary>
        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Название",
                    Path="NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=30,
                },
                new DataGridHelperColumn
                {
                    Header="Изображение",
                    Path="IMAGE_IS",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="Дизайн",
                    Path="DRAWING_IS",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="Потребитель",
                    Path="CUSTOMER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=30,
                },
                new DataGridHelperColumn
                {
                    Header="Техкарта",
                    Path="ACTIVE_TECHCARD_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=40,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.ForegroundColor,
                            row=>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("TECHCARD_CNT").ToInt() > 1)
                                {
                                    color = HColor.RedFG;
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        },
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Дата следующей отгрузки",
                    Path="NEXT_SHIPMENT",
                    ColumnType=ColumnTypeRef.DateTime,
                    Format="dd.MM.yy HH:mm",
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Архивная",
                    Path="ARCHIVED_FLAG",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="На остатке",
                    Path="QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="В заявке",
                    Path="ORDER_QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Заказано",
                    Path="ORDER_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="Дата поступления",
                    Path="ORDER_RECEIPT_DT",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=15,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header="GUID",
                    Path="GUID",
                    ColumnType=ColumnTypeRef.String,
                    Width2=30,
                },

            };
            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("ID");
            Grid.SetSorting("ID", ListSortDirection.Descending);
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.SearchText = GridSearch;
            Grid.Toolbar = GridToolbar;
            Grid.Commands = Commander;

            Grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                // Цвета шрифта строк
                {
                    StylerTypeRef.ForegroundColor,
                    row =>
                    {
                        var result=System.Windows.DependencyProperty.UnsetValue;
                        var color = "";
                        bool archived = row.CheckGet("ARCHIVED_FLAG").ToBool();

                        // в архиве
                        if (archived)
                        {
                            color = HColor.OliveFG;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
            };

            Grid.OnLoadItems = LoadItems;
            Grid.OnFilterItems = FilterItems;

            Grid.Init();
        }

        /// <summary>
        /// Загрузка данных
        /// </summary>
        private async void LoadItems()
        {
            bool showArchived = (bool)ShowArchivedCheckBox.IsChecked;
            
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Sticker");
            q.Request.SetParam("Action", "List");
            q.Request.SetParam("SHOW_ARCHIVED", showArchived ? "1" : "0");

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Grid.ClearItems();
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "STICKERS");
                    Grid.UpdateItems(ds);
                }

            }
        }

        /// <summary>
        /// Фильтрация строк
        /// </summary>
        public void FilterItems()
        {
            if (Grid.Items != null)
            {
                if (Grid.Items.Count > 0)
                {
                    bool withShipment = (bool)WithShipmentCheckBox.IsChecked;

                    if (withShipment)
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in Grid.Items)
                        {
                            bool includeShipment = false;

                            if (!row.CheckGet("NEXT_SHIPMENT").IsNullOrEmpty())
                            {
                                includeShipment = true;
                            }

                            if (includeShipment)
                            {
                                items.Add(row);
                            }
                        }

                        Grid.Items = items;
                    }
                }
            }
        }

        public async void ShowImageFile(int id)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Sticker");
            q.Request.SetParam("Action", "GetDrawingFile");
            q.Request.SetParam("ID", id.ToString());
            q.Request.SetParam("FILE_TYPE", "2");
            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Central.OpenFile(q.Answer.DownloadFilePath);
            }
            else if (q.Answer.Error.Code == 145)
            {
                q.ProcessError();
            }
        }

        private void ShowArchivedCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        private void WithShipmentCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Grid.UpdateItems();
        }
    }
}
