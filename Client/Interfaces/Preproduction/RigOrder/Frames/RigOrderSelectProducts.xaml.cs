using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Выбор изделий для создания заявки на оснастку
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class RigOrderSelectProducts : ControlBase
    {
        public RigOrderSelectProducts()
        {
            InitializeComponent();

            OnUnload = () =>
            {
                Grid.Destruct();
            };

            InitGrid();
        }

        /// <summary>
        /// Имя вкладки, которая вызвала открытие фрейма, и в которую возвращается фокус после закрытия фрейма
        /// </summary>
        public string ReceiverName { get; set; }

        /// <summary>
        /// Обработка команд
        /// </summary>
        /// <param name="command"></param>
        private void ProcessCommand(string command)
        {
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "save":
                        Save();
                        break;
                    case "close":
                        Close();
                        break;
                }
            }
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
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="Покупатель",
                    Path="CUSTOMER",
                    ColumnType=ColumnTypeRef.String,
                    Width2=26,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="SKU",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Название",
                    Path="TECHCARD_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=26,
                },
                new DataGridHelperColumn
                {
                    Header="Отгрузка",
                    Path="SHIPMENT_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=12,
                    Format="dd.MM HH:mm"
                },
                new DataGridHelperColumn
                {
                    Header="Разрешение на заказ",
                    Path="RIG_ORDER_ALLOWED_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=6,
                    Stylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("RIG_ORDER_ALLOWED_FLAG").ToInt() == 1)
                                {
                                    color = HColor.Green;
                                }
                                else
                                {
                                    color = HColor.YellowOrange;
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
                new DataGridHelperColumn
                {
                    Header="Дизайнер",
                    Path="DESIGNER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="ИД покупателя",
                    Path="PAYER_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Количество элементов печати",
                    Path="FORM_QTY",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Файл дизайна",
                    Path="DESIGN_FILE",
                    ColumnType=ColumnTypeRef.String,
                    Hidden=true,
                },
            };
            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("_ROWNUMBER");
            Grid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.SearchText = GridSearch;

            Grid.OnLoadItems = LoadItems;
            Grid.OnFilterItems = FilterItems;
            Grid.OnSelectItem = selectedItem =>
            {
                FormStatus.Text = "";
                SaveButton.IsEnabled = selectedItem.CheckGet("RIG_ORDER_ALLOWED_FLAG").ToBool();
            };

            // Раскраска строк
            Grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                // Цвета фона строк
                {
                    StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result = DependencyProperty.UnsetValue;
                        var color = "";

                        int formQty = row.CheckGet("FORM_QTY").ToInt();
                        int clicheQty = row.CheckGet("CLICHE_QTY").ToInt();

                        if (formQty == clicheQty)
                        {
                            color = HColor.Green;
                        }
                        else if (clicheQty > 0)
                        {
                            color = HColor.Blue;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
            };

            Grid.Init();
        }

        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        private async void LoadItems()
        {
            bool ordered = (bool)OrderedCheckBox.IsChecked;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "RigOrder");
            q.Request.SetParam("Action", "ListContainerNotOrdered");
            q.Request.SetParam("ORDERED", ordered ? "1" : "0");

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
                    //Дизайнеры
                    {
                        var dsnDS = ListDataSet.Create(result, "DESIGNER");
                        var list = new Dictionary<string, string> { { "0", "Все" } };
                        foreach (var d in dsnDS.Items)
                        {
                            list.Add(d.CheckGet("ID"), d.CheckGet("FIO"));
                        }
                        DesignerName.Items = list;

                        // Если активный пользователь есть в списке, установим его в выбранном значении
                        string emplId = Central.User.EmployeeId.ToString();
                        if (list.ContainsKey(emplId))
                        {
                            DesignerName.SetSelectedItemByKey(emplId);
                        }
                        else
                        {
                            DesignerName.SetSelectedItemByKey("0");
                        }
                    }

                    var ds = ListDataSet.Create(result, "TECHCARDS");
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
                    bool doFilterByDesigner = false;
                    int designerId = DesignerName.SelectedItem.Key.ToInt();
                    if (designerId > 0)
                    {
                        doFilterByDesigner = true;
                    }

                    if (doFilterByDesigner)
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in Grid.Items)
                        {
                            bool includeByDesigner = true;
                            if (doFilterByDesigner)
                            {
                                includeByDesigner = false;
                                if(row.CheckGet("DESIGNER_ID").ToInt() == designerId)
                                {
                                    includeByDesigner = true;
                                }
                            }

                            if (includeByDesigner)
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
        /// Отображение вкладки со списком техкарт
        /// </summary>
        public void Show()
        {
            string title = $"Выбор техкарт для заявки";

            Central.WM.Show(ControlName, title, true, "add", this);
        }

        /// <summary>
        /// Передача выбранных строк в форму заказа оснастки
        /// </summary>
        public void Save()
        {
            bool resume = true;
            //Список всех выбранных строк для передачи в следующую форму
            var item = Grid.SelectedItem;
            var d = new Dictionary<string, string>();

            if (resume)
            {
                d.Add("_ROWNUMBER", "1");
                d.Add("TECHCARD_ID", item.CheckGet("ID"));
                d.Add("CUSTOMER", item.CheckGet("CUSTOMER"));
                d.Add("SKU", item.CheckGet("SKU"));
                d.Add("TECHCARD_NAME", item.CheckGet("TECHCARD_NAME"));
                d.Add("PAYER_ID", item.CheckGet("PAYER_ID"));
                d.Add("FORM_QTY", item.CheckGet("FORM_QTY"));

                //Файл дизайна
                string designFile = item.CheckGet("DESIGN_FILE");
                string designFileName = "";
                if (!designFile.IsNullOrEmpty())
                {
                    if (File.Exists(designFile))
                    {
                        designFileName = Path.GetFileName(designFile);
                    }
                    else
                    {
                        designFile = "";
                    }
                }

                d.Add("DRAWING_FILE_BIND", designFile.IsNullOrEmpty() ? "0" : "1");
                d.Add("DRAWING_FILE", designFile);
                d.Add("DRAWING_FILE_NAME", designFileName);
            }

            if (resume)
            {
                var rigOrderFrame = new RigOrderContainer();
                rigOrderFrame.ReceiverName = ReceiverName;
                rigOrderFrame.TechCard = d;
                rigOrderFrame.Edit(0);

                Central.WM.Close(ControlName);
            }
        }

        /// <summary>
        /// Закрытие формы
        /// </summary>
        public void Close()
        {
            Central.WM.Close(ControlName);
            if (!ReceiverName.IsNullOrEmpty())
            {
                //Central.WM.SetActive(ReceiverName);
                ReceiverName = "";
            }
        }

        /// <summary>
        /// Обработка нажатия на кнопку
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonOnClick(object sender, RoutedEventArgs e)
        {
            var b = (Button)sender;
            if (b != null)
            {
                var t = b.Tag.ToString();
                ProcessCommand(t);
            }
        }

        private void GridLoadItems(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        private void GridUpdateItems(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }
    }
}
