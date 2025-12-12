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
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Фрейм редактирования припусков заданного изделия
    /// </summary>
    public partial class ProductLosses : ControlBase
    {
        public ProductLosses()
        {
            InitializeComponent();

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
                if (msg.ReceiverName == ControlName)
                {
                    ProcessCommand(msg.Action, msg);
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

            QtyInApplication = 0;
            ProductId = 0;
            ProductSku = "";
        }

        /// <summary>
        /// Идентификатор изделия
        /// </summary>
        public int ProductId;
        /// <summary>
        /// Имя вкладки, откуда вызвана форма и куда передается ответ
        /// </summary>
        public string ReceiverName;
        /// <summary>
        /// Часть артикула для имени вкладки
        /// </summary>
        public string ProductSku;
        /// <summary>
        /// Количество по заявке
        /// </summary>
        public int QtyInApplication;
        /// <summary>
        /// Количество изделий на поддоне
        /// </summary>
        public int ProductsOnPalletQty;
        /// <summary>
        /// Количество изделий из заготовки
        /// </summary>
        public double ProductsFromBlankQty;

        /// <summary>
        /// Обработка и выполнение команд
        /// </summary>
        /// <param name="command"></param>
        public void ProcessCommand(string command, ItemMessage m = null)
        {
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "close":
                        Close();
                        break;
                    case "lossesedit":
                        if (m != null)
                        {
                            if (m.ContextObject != null)
                            {
                                var v = (Dictionary<string, string>)m.ContextObject;
                                if (v.Count > 0)
                                {
                                    SaveLosses(v);
                                }
                            }
                        }
                        break;
                    case "clear":
                        ClearCustomLosses();
                        break;
                }
            }
        }

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="RANGE_NUM",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="Количество от",
                    Path="QTY_MIN",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Количество до",
                    Path="QTY_MAX",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Припуск, %",
                    Path="PCT_LOSSES",
                    ColumnType=ColumnTypeRef.Double,
                    Width2=8,
                    Format="N3",
                    OnClickAction=(row,el) =>
                    {
                        EditLosses(row, 1);
                        return null;
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Припуск, шт",
                    Path="QTY_LOSSES",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                    OnClickAction=(row,el) =>
                    {
                        EditLosses(row, 2);
                        return null;
                    },
                },
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Изменено",
                    Path="EDITED",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("RANGE_NUM");
            Grid.SetSorting("RANGE_NUM", ListSortDirection.Ascending);
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            Grid.AutoUpdateInterval = 0;

            // Раскраска строк
            Grid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                // Цвета фона строк
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        var valueEdited = row.CheckGet("EDITED").ToBool();
                        if (valueEdited)
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

            Grid.OnLoadItems = LoadItems;
            Grid.Init();
        }

        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        public async void LoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Products");
            q.Request.SetParam("Object", "Assortment");
            q.Request.SetParam("Action", "ListLosses");
            q.Request.SetParam("ID", ProductId.ToString());

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                FormStatus.Text = "";
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "LOSSES");
                    Grid.UpdateItems(ds);
                    if (QtyInApplication > 0)
                    {
                        foreach (var item in Grid.Items)
                        {
                            int qtyMin = item.CheckGet("QTY_MIN").ToInt();
                            int qtyMax = item.CheckGet("QTY_MAX").ToInt();

                            if ((QtyInApplication >= qtyMin) && (QtyInApplication <= qtyMax))
                            {
                                var key = Grid.GetPrimaryKey();
                                Grid.SelectRowByKey(item[key]);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Отображение вкладки
        /// </summary>
        public void ShowTab()
        {
            if (ProductId == 0)
            {
                FormStatus.Text = "Не удалось определить изделие. Показаны базовые припуски";
            }
            ControlName = $"ProductLosses{ProductId}";
            string productCode = ProductSku;
            if (productCode.IsNullOrEmpty())
            {
                productCode = ProductId.ToString();
            }
            ControlTitle = $"Припуски {productCode}";

            ProductsOnPallet.Text = ProductsOnPalletQty.ToString();

            var intFromBlank = (double)ProductsFromBlankQty.ToInt();
            if (ProductsFromBlankQty == intFromBlank)
            {
                ProductsFromBlank.Text = intFromBlank.ToString();
            }
            else
            {
                ProductsFromBlank.Text = ProductsFromBlankQty.ToString();
            }

            Central.WM.Show(ControlName, ControlTitle, true, "add", this);
        }

        /// <summary>
        /// Закрытие вкладки с формой
        /// </summary>
        public void Close()
        {
            Central.WM.Close(ControlName);
            Central.WM.SetActive(ReceiverName);
        }

        private void EditLosses(Dictionary<string, string> p, int mode)
        {
            if (ProductId > 0)
            {
                var lossesEdit = new ProductLossesEdit();
                lossesEdit.ReceiverName = ControlName;
                lossesEdit.Mode = mode;
                lossesEdit.Edit(p);
            }
        }

        /// <summary>
        /// Очистка всех сохраненных припусков и возврат к стандартным значениям
        /// </summary>
        private async void ClearCustomLosses()
        {
            bool resume = false; 
            var dw = new DialogWindow("Вы действительно хотите очистить все сохраненные припуски и вернуться к стандартным?", "Очистка припусков", "", DialogWindowButtons.NoYes);
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
                q.Request.SetParam("Module", "Products");
                q.Request.SetParam("Object", "Assortment");
                q.Request.SetParam("Action", "ClearLosses");
                q.Request.SetParam("ID", ProductId.ToString());

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    FormStatus.Text = "";
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "LOSSES");
                        Grid.UpdateItems(ds);
                    }
                }
                else if (q.Answer.Error.Code == 145)
                {
                    FormStatus.Text = q.Answer.Error.Message;
                }
            }
        }

        /// <summary>
        /// Сохранение измененных припусков
        /// </summary>
        /// <param name="p"></param>
        private async void SaveLosses(Dictionary<string, string> p)
        {
            var applyAll = p.CheckGet("APPLY_ALL").ToBool();
            var lossesDict = new Dictionary<string, string>();
            int mode = p.CheckGet("MODE").ToInt();

            if (applyAll)
            {
                //Необходимо во все диапазоны проставить одинаковое значение
                foreach (var row in Grid.Items)
                {
                    if (mode == 1)
                    {
                        lossesDict.Add(row.CheckGet("RANGE_NUM"), p.CheckGet("PCT_LOSSES"));
                    }
                    else
                    {
                        var pctLosses = Math.Round(p.CheckGet("QTY_LOSSES").ToDouble() / row.CheckGet("QTY_MAX").ToDouble() * 100, 4);
                        lossesDict.Add(row.CheckGet("RANGE_NUM"), pctLosses.ToString());
                    }
                }
            }
            else
            {
                lossesDict.Add(p.CheckGet("RANGE_NUM"), p.CheckGet("PCT_LOSSES"));
            }

            string lossesString = JsonConvert.SerializeObject(lossesDict);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Products");
            q.Request.SetParam("Object", "Assortment");
            q.Request.SetParam("Action", "SaveLosses");
            q.Request.SetParam("ID", ProductId.ToString());
            q.Request.SetParam("LOSSES", lossesString);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                FormStatus.Text = "";
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "LOSSES");
                    Grid.UpdateItems(ds);
                }
            }
            else if (q.Answer.Error.Code == 145)
            {
                FormStatus.Text = q.Answer.Error.Message;
            }
        }

        /// <summary>
        /// Обработчик нажатия на кнопку
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
    }
}
