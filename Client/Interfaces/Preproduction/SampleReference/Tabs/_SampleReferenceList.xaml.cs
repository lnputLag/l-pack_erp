using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
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
    /// Список эталонных образцов
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleReferenceList : ControlBase
    {
        /// <summary>
        /// Инициализация элемента интерфейса эталонных образцов
        /// </summary>
        public SampleReferenceList()
        {
            ControlTitle = "Эталонные образцы";
            DocumentationUrl = "/doc/l-pack-erp-new/preproduction_new/etalon_samples";
            RoleName = "[erp]sample_reference";

            InitializeComponent();
            InitGrid();

            OnLoad = () =>
            {
                GetPermission();
            };

            OnUnload = () =>
            {
                Grid.Destruct();
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

            OnKeyPressed = (KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    switch (e.Key)
                    {
                        case Key.F1:
                            Commander.ProcessCommand("help");
                            e.Handled = true;
                            break;
                    }
                }
                /*
                if (!e.Handled)
                {
                    switch (e.Key)
                    {
                        case Key.F5:
                            LoadItems();
                            e.Handled = true;
                            break;
                        case Key.Home:
                            Grid.SetSelectToFirstRow();
                            e.Handled = true;
                            break;
                        case Key.End:
                            Grid.SetSelectToLastRow();
                            e.Handled = true;
                            break;
                    }
                }
                */
            };

            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverGroup == "Preproduction")
                {
                    if (msg.ReceiverName == ControlName)
                    {
                        ProcessMessage(msg);
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
                    Title = "Показать",
                    Description = "Загрузить данные",
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
            Commander.SetCurrentGridName("Grid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "edit",
                    Title = "Изменить",
                    Group = "item",
                    MenuUse = true,
                    HotKey = "Return|DoubleCLick",
                    ButtonUse = true,
                    ButtonName = "EditButton",
                    Description = "Изменение места хранения образца",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            // Передадим уникальную часть артикула, если артикул определен
                            string code = Grid.SelectedItem.CheckGet("ARTICLE");
                            if (!string.IsNullOrEmpty(code))
                            {
                                code = code.Substring(0, 7);
                            }

                            var refSampleEdit = new SampleReference();
                            refSampleEdit.TechcardId = id;
                            refSampleEdit.RefSampleId = Grid.SelectedItem.CheckGet("SAMPLE_ID").ToInt();
                            refSampleEdit.ReceiverName = ControlName;
                            refSampleEdit.RefSampleCode = code;
                            refSampleEdit.Show();
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var k = Grid.GetPrimaryKey();
                        var row = Grid.SelectedItem;
                        if (row.CheckGet(k).ToInt() != 0)
                        {
                            if (!row.CheckGet("ARCHIVED").ToBool())
                            {
                                result = true;
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "delete",
                    Title = "Удалить",
                    Group = "item",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonName = "DeleteButton",
                    Description = "Удалить образец",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var id = Grid.SelectedItem.CheckGet(k).ToInt();
                        if (id != 0)
                        {
                            DeleteRefSample();
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
                    Name = "techmap",
                    Title = "Показать ТК",
                    Group = "operation",
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonName = "TechMapButton",
                    Description = "Показать техкарту",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        var k = Grid.GetPrimaryKey();
                        var path = Grid.SelectedItem.CheckGet("PATHTK");
                        if (!string.IsNullOrEmpty(path))
                        {
                            if (File.Exists(path))
                            {
                                Central.OpenFile(path);
                            }
                        }
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = Grid.SelectedItem;
                        if (row != null)
                        {
                            result = !row.CheckGet("PATHTK").IsNullOrEmpty();
                        }
                        return result;
                    },
                });
            }
            Commander.Init(this);
        }

        /// <summary>
        /// Режим доступа к элементам интерфейса для пользователя
        /// </summary>
        Role.AccessMode Permission;

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// Получение уровня доступа пользователя к интерфейсу
        /// </summary>
        private void GetPermission()
        {
            Permission = Central.Navigator.GetRoleLevel("[erp]sample_reference");
        }

        /// <summary>
        /// Обработка и выполнение команд
        /// </summary>
        /// <param name="command"></param>
        public void ProcessMessage(ItemMessage msg)
        {
            string action = msg.Action.ClearCommand();
            if (!action.IsNullOrEmpty())
            {
                switch (action)
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
        public void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn()
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn()
                {
                    Header="Ид Техкарты",
                    Path="ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=7,
                },
                new DataGridHelperColumn()
                {
                    Header="Ид образца",
                    Path="SAMPLE_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=7,
                },
                new DataGridHelperColumn()
                {
                    Header="Дата",
                    Path="REFERENCE_SAMPLE_DT",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Width2=10,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn()
                {
                    Header="Артикул",
                    Path="ARTICLE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn()
                {
                    Header="Клиент",
                    Path="CUSTOMER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=30,
                },
                new DataGridHelperColumn()
                {
                    Header="Наименование",
                    Path="NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=50,
                },
                new DataGridHelperColumn()
                {
                    Header="Вид изделия",
                    Path="PRODUCT_CLASS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=30,
                },
                new DataGridHelperColumn()
                {
                    Header="Место",
                    Path="RACK_NUM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Double,
                    Width2=7,
                    Format="N0",
                },
                new DataGridHelperColumn()
                {
                    Header="Номер",
                    Path="CELL_NUM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Double,
                    Width2=7,
                    Format="N0",
                },
                new DataGridHelperColumn()
                {
                    Header="Файл ТК",
                    Path="PATHTK",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Hidden=true
                },
                new DataGridHelperColumn()
                {
                    Header="В архиве",
                    Path="ARCHIVED",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true
                },
            };

            Grid.SetColumns(columns);

            // Раскраска строк
            Grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                {
                    StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";
                        // эталонные образцы без места
                        if (row.CheckGet("CELL_NUM").IsNullOrEmpty())
                        {
                            color=HColor.Blue;
                        }

                        if (row.CheckGet("PRODUCT_PLACE_FLAG").ToInt() == 1)
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

                        // эталонные образцы на архивные ТК
                        if (row.CheckGet("ARCHIVED").ToInt() == 1)
                        {
                            color=HColor.OliveFG;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                }
            };

            // используем ту сортировку, которая определена в запросе.
            // добавили колонку с номером строки результата запроса, по ней выполним сортировку
            Grid.SetPrimaryKey("ID");
            Grid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.Commands = Commander;
            Grid.AutoUpdateInterval = 600;
            Grid.SearchText = SearchText;

            //данные грида
            Grid.OnLoadItems = LoadItems;

            //при выборе строки в гриде, обновляются актуальные действия для записи
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
        /// Загрузка данных из БД
        /// </summary>
        public async void LoadItems()
        {
            Grid.ShowSplash();
            GridToolbar.IsEnabled = false;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "ReferenceSample");
            q.Request.SetParam("Action", "List");

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;
            
            await Task.Run(() =>
            {
                q.DoQuery();
            }
            );

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "REFERENCE_SAMPLES");
                    // Добавим колонку для чекбоквов
                    foreach (var item in ds.Items)
                    {
                        item.CheckAdd("_CHECKING", "0");
                    }
                    Grid.UpdateItems(ds);
                }
            }
            else
            {
                //q.ProcessError();
            }

            Grid.HideSplash();
            GridToolbar.IsEnabled = true;
        }

        /// <summary>
        /// Удаление эталонного образца
        /// </summary>
        private async void DeleteRefSample()
        {
            var dialog = new DialogWindow(
                "Вы действительно хотите удалить эталонный образец?",
                "Удаление эталонного образца",
                "",
                DialogWindowButtons.NoYes
            );
            if ((bool)dialog.ShowDialog())
            {
                if (dialog.ResultButton == DialogResultButton.Yes)
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Preproduction");
                    q.Request.SetParam("Object", "ReferenceSample");
                    q.Request.SetParam("Action", "Delete");
                    q.Request.SetParam("TECHCARD_ID", Grid.SelectedItem.CheckGet("ID"));

                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    if (q.Answer.Status == 0)
                    {
                        var resultData = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (resultData != null)
                        {
                            if (resultData.ContainsKey("ITEM"))
                            {
                                // если ответ не пустой, обновляем таблицу
                                Grid.LoadItems();
                            }
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
            }
        }

        /// <summary>
        /// Обработчик нажатия на кнопку показа ТК
        /// </summary>
        private void ShowTechMap()
        {
            if (Grid.Items.Count > 0)
            {
                if (Grid.SelectedItem != null)
                {
                    var path = SelectedItem.CheckGet("PATHTK");
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
    }
}
