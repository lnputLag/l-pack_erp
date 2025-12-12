using Client.Common;
using Client.Interfaces.Main;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Окно суммарных длин заданий, полученных автораскроем, по каждому картону и формату
    /// </summary>
    public partial class ProductionTaskTotalFormatLength : UserControl
    {
        public ProductionTaskTotalFormatLength()
        {
            InitializeComponent();
        }

        public Window Window { get; set; }

        /// <summary>
        /// Ширина таблицы
        /// </summary>
        public int TableWidth;

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessKeyboard2(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// Создание и заполнени таблицы по полученным данным
        /// </summary>
        /// <param name="ds">Данные для таблицы</param>
        /// <param name="headers">Данные для формирования колонок</param>
        public void InitGrid(ListDataSet ds, Dictionary<string, string> headers)
        {
            // Если данные для колонок отсутствуют, названия колонок берём из ключей данных таблицы
            if (headers == null)
            {
                headers = new Dictionary<string, string>();
            }
            if (headers.Count == 0)
            {
                foreach (var item in ds.Items[0])
                {
                    headers.Add(item.Key, item.Key);
                }
            }

            var columns = new List<DataGridHelperColumn>();
            foreach (var h in headers)
            {
                int maxWidth = 150;
                var columnType = DataGridHelperColumn.ColumnTypeRef.String;
                if ((h.Key == "_ROWNUMBER") || (h.Key == "FORMAT"))
                {
                    maxWidth = 70;
                    columnType = DataGridHelperColumn.ColumnTypeRef.Integer;
                }
                columns.Add(new DataGridHelperColumn
                {
                    Header = h.Value,
                    Path = h.Key,
                    ColumnType= columnType,
                    MinWidth=50,
                    MaxWidth=maxWidth,
                });
            }
            columns.Add(new DataGridHelperColumn
            {
                Header = " ",
                Path = "_",
                ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                MinWidth = 5,
                MaxWidth = 1500,
            });
            Grid.SetColumns(columns);
            Grid.SetSorting("FORMAT", ListSortDirection.Ascending);
            Grid.Init();
            Grid.UpdateItems(ds);

            TableWidth = (columns.Count - 3) * 150 + 250;
            Show();
        }

        public void Show()
        {
            string title = $"Итоги автораскроя";

            Width = Math.Min(TableWidth, 1500);
            int w = (int)Width;
            int h = (int)Height;

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

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
