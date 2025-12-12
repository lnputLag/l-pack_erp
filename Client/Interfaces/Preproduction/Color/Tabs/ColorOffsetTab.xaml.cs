using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Список оффсетных красок для литой тары
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class ColorOffsetTab : ControlBase
    {
        public ColorOffsetTab()
        {
            InitializeComponent();
            ControlTitle = "Оффсетные краски";
            DocumentationUrl = "/doc/l-pack-erp/preproduction/sticker_list";
            RoleName = "[erp]color";

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
                        Description = "Создание новой краски",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var colorForm = new ColorOffset();
                            colorForm.ReceiverName = ControlName;
                            colorForm.Edit();
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
                        Description = "Внесение изменений в краску",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            var k = Grid.GetPrimaryKey();
                            var id = Grid.SelectedItem.CheckGet(k).ToInt();
                            if (id != 0)
                            {
                                var colorForm = new ColorOffset();
                                colorForm.ReceiverName = ControlName;
                                colorForm.Edit(id);
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
                        Name = "exportexcel",
                        Title = "В Excel",
                        MenuUse = true,
                        ButtonUse = true,
                        ButtonName = "ExportExcelButton",
                        Description = "Экспорт содержимого таблицы в Excel",
                        AccessLevel = Role.AccessMode.ReadOnly,
                        Action = () =>
                        {
                            Grid.ItemsExportExcel();
                        },
                        CheckEnabled = () =>
                        {
                            var result = false;
                            if (Grid.Items.Count > 0)
                            {
                                result = true;
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
        /// Функция перевода строки содержащей hex код цвета краски в цвет Brush
        /// <param name="hex_code">строка с hex числом</param>
        /// <return>Brush.цвет</return>
        /// </summary>
        private Brush HexToBrush(string hex_code)
        {
            SolidColorBrush result = null;
            var hexString = (hex_code as string).Replace("#", "");

            if (hexString.Length == 6)
            {
                var r = hexString.Substring(0, 2);
                var g = hexString.Substring(2, 2);
                var b = hexString.Substring(4, 2);

                result = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0xff,
                   byte.Parse(r, System.Globalization.NumberStyles.HexNumber),
                   byte.Parse(g, System.Globalization.NumberStyles.HexNumber),
                   byte.Parse(b, System.Globalization.NumberStyles.HexNumber)));
            }

            return result;
        }

        /// <summary>
        /// Иницифлизация таблицы
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
                    Path="PANTONE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Цвет",
                    Path="COLOR",
                    ColumnType=ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn
                {
                    Header="Код цвета",
                    Path="HEX",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = row.CheckGet("HEX");

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result = HexToBrush(color);
                                }

                                return result;
                            }
                        },
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Архив",
                    Path="ARCHIVED_FLAG",
                    Doc="",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=5,
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
                    Width2=20,
                },
            };
            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("ID");
            Grid.SetSorting("COLOR", ListSortDirection.Ascending);
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.SearchText = GridSearch;
            Grid.Toolbar = GridToolbar;
            Grid.Commands = Commander;

            Grid.OnLoadItems = LoadItems;

            Grid.Init();
        }

        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        private async void LoadItems()
        {
            bool showArchived = (bool)ShowArchivedCheckBox.IsChecked;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PrintInk");
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
                    var ds = ListDataSet.Create(result, "PRINT_INK");
                    Grid.UpdateItems(ds);
                }
            }
        }

        private void ShowArchivedCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }
    }
}
