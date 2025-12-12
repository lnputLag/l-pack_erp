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

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Таблица с данными по наличию на складе рулонов выбранной сырьевой группы
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class RawGroupStockResidue : ControlBase
    {
        public RawGroupStockResidue()
        {
            InitializeComponent();
            ControlTitle = "Наличие на складе рулонов";

            OnLoad = () =>
            {
                InitGrid();
            };
        }

        /// <summary>
        /// Идентификатор сырьевой группы
        /// </summary>
        public int RawGroupId { get; set; }
        /// <summary>
        /// Идентификатор производственной площадки
        /// </summary>
        public int FactoryId { get; set; }
        /// <summary>
        /// Формат раскроя
        /// </summary>
        public int ReelWidth { get; set; }
        /// <summary>
        /// Структура окна
        /// </summary>
        private Window Window { get; set; }

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Доступный вес",
                    Path="WEIGHT",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Доступно с",
                    Path="AVAILABLE_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Format="dd.MM.yy HH:mm",
                    Width2=14,
                },
            };
            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("ID");
            Grid.SetSorting("ID", ListSortDirection.Ascending);
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.OnLoadItems = LoadItems;

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

                        var availableDt = row.CheckGet("AVAILABLE_DTTM").ToDateTime("dd.MM.yyyy HH:mm:ss");
                        if (DateTime.Compare(availableDt, DateTime.Now) > 0)
                        {
                            color = HColor.Yellow;
                        }
                        else
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
            };

            Grid.Init();
        }

        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        private async void LoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "Roll");
            q.Request.SetParam("Action", "StockResidueByRawGroup");
            q.Request.SetParam("RAW_GROUP_ID", RawGroupId.ToString());
            q.Request.SetParam("FACTORY_ID", FactoryId.ToString());
            q.Request.SetParam("FORMAT", ReelWidth.ToString());

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
                    var ds = ListDataSet.Create(result, "RAW_STOCK");
                    Grid.UpdateItems(ds);

                    var bdmDS = ListDataSet.Create(result, "RAW_BDM");
                    if (bdmDS.Items != null)
                    {
                        if (bdmDS.Items.Count > 0)
                        {
                            var item = bdmDS.Items[0];
                            NearestDate.Text = item.CheckGet("COMPLETE_DTTM");
                            Weight.Text = item.CheckGet("QTY");
                        }
                    }
                }
            }
        }

        public void ShowWin()
        {
            if (FactoryId > 0 && RawGroupId > 0)
            {
                Show();
                //Grid.LoadItems();
            }
            else
            {
                var dw = new DialogWindow("Заданы не все параметры!", "Остатки рулонов на складе");
                dw.SetIcon("alert");
                dw.ShowDialog();
            }
        }

        /// <summary>
        /// Показ окна
        /// </summary>
        public void Show()
        {
            int w = (int)Width;
            int h = (int)Height;
            string title = $"Наличие рулонов на склде";

            Window = new Window
            {
                Title = title,
                Width = w + 17,
                Height = h + 40,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow,
            };
            Window.Content = new Frame
            {
                Content = this,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            if (Window != null)
            {
                Window.Topmost = true;
                Window.ShowDialog();
            }
        }

        /// <summary>
        /// Закрытие окна
        /// </summary>
        public void Close()
        {
            var window = this.Window;
            if (window != null)
            {
                window.Close();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
