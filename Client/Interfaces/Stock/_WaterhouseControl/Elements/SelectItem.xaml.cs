using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Stock._WaterhouseControl
{
    /// <summary>
    /// Interaction logic for SelectItem.xaml
    /// </summary>
    public partial class SelectItem : UserControl
    {
        public SelectItem()
        {
            FrameName = "SelectItem";
            InitializeComponent();

            ItemsGridInit();
        }

        public string FrameName { get; private set; }
        public Dictionary<string, string> ItemGridSelectedItem { get; private set; }

        public delegate void SelectItemDelegate(Dictionary<string, string> item);
        public event SelectItemDelegate OnSelectItem;

        /// <summary>
        /// настройка отображения грида
        /// </summary>
        private void ItemsGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="WMIT_ID",
                        Doc="ID ТМЦ",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth = 40,
                        MaxWidth = 40,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="NAME",
                        Doc="Наименование тмц",
                        ColumnType=ColumnTypeRef.String,
                        Width = 160,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Группа ТМЦ",
                        Path="GROUP_NAME",
                        Doc="Группа ТМЦ",
                        ColumnType=ColumnTypeRef.String,
                        Width = 160,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Path="QTY",
                        Doc="Количество",
                        ColumnType=ColumnTypeRef.String,
                        Width = 160,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Единица измерения",
                        Path="SHORT_NAME",
                        Doc="Единица измерения",
                        ColumnType=ColumnTypeRef.String,
                        Width = 130,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус",
                        Path="STATE",
                        Doc="Статус",
                        ColumnType=ColumnTypeRef.String,
                        Width = 100,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ячейка",
                        Path="NUM",
                        Doc="Ячейка",
                        ColumnType=ColumnTypeRef.String,
                        Width = 80,
                    },
                    //new DataGridHelperColumn
                    //{
                    //    Header="Длина, мм",
                    //    Path="LENGTH",
                    //    Doc="Длина, мм",
                    //    ColumnType=ColumnTypeRef.Integer,
                    //    Width = 60,
                    //},
                    //new DataGridHelperColumn
                    //{
                    //    Header="Ширина, мм",
                    //    Path="WIDTH",
                    //    Doc="Ширина, мм",
                    //    ColumnType=ColumnTypeRef.Integer,
                    //    Width = 70,
                    //},
                    //new DataGridHelperColumn
                    //{
                    //    Header="Высота, мм",
                    //    Path="HEIGHT",
                    //    Doc="Высота, мм",
                    //    ColumnType=ColumnTypeRef.Integer,
                    //    Width = 60,
                    //},
                    //new DataGridHelperColumn
                    //{
                    //    Header="Вес, кг",
                    //    Path="WEIGHT",
                    //    Doc="Вес, кг",
                    //    ColumnType=ColumnTypeRef.Double,
                    //    Format = "N3",
                    //    Width = 60,
                    //},
                    //new DataGridHelperColumn
                    //{
                    //    Header="Примечание",
                    //    Path="NOTE",
                    //    Doc="Примечание",
                    //    ColumnType=ColumnTypeRef.String,
                    //    Width = 100,
                    //},
                    //new DataGridHelperColumn
                    //{
                    //    Header="Внешний ИД",
                    //    Path="OUTER_ID",
                    //    Doc="Внешний ИД",
                    //    ColumnType=ColumnTypeRef.String,
                    //    Width = 100,
                    //},

                    //new DataGridHelperColumn
                    //{
                    //    Header="Внешний номер",
                    //    Path="OUTER_NUM",
                    //    Doc="Внешний номер",
                    //    ColumnType=ColumnTypeRef.String,
                    //    Width = 100,
                    //},
                    //new DataGridHelperColumn
                    //{
                    //    Header="Проверочный код",
                    //    Path="VERIFICATION_CODE",
                    //    Doc="Проверочный код",
                    //    ColumnType=ColumnTypeRef.String,
                    //    Width = 100,
                    //},
                    new DataGridHelperColumn
                    {
                        Header=" ",
                        Path="_",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=5,
                        MaxWidth=2000,
                    },
                };

                // цветовая маркировка строк
                ItemsGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                {
                    // Цвета фона строк
                    {
                        DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = string.Empty;

                            int currentStatus = row.CheckGet("WMIS_ID").ToInt();

                            switch(currentStatus)
                            {
                                case 1: // На поступление
                                    color = HColor.Blue;
                                    break;
                                case 2: // На складе
                                    break;
                                case 3: // Списана
                                    color = HColor.Yellow;
                                    break;
                                case 4: // Удалена
                                    color = HColor.Red;
                                    break;

                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },
                };

                ItemsGrid.PrimaryKey = "WMIT_ID";
                ItemsGrid.SetColumns(columns);

                ItemsGrid.SearchText = ItemsSearchBox;
                ItemsGrid.OnFilterItems = ItemsGridFilterItems;
                ItemsGrid.Label = "ItemsGrid";
                ItemsGrid.SetSorting("STATE");
                ItemsGrid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                ItemsGrid.OnSelectItem = selectedItem =>
                {
                    ItemsGridUpdateActions(selectedItem);
                };

                ItemsGrid.OnDblClick = selectedItem =>
                {
                    ItemsGridDblClickActions(selectedItem);
                };

                //данные грида
                ItemsGrid.OnLoadItems = ItemsGridLoadItems;

                ItemsGrid.Run();
            }
        }

        /// <summary>
        /// Двойной клик по позиции в гриде
        /// </summary>
        /// <param name="selectedItem"></param>
        private void ItemsGridDblClickActions(Dictionary<string, string> selectedItem)
        {
            if(selectedItem!= null) 
            {
                if(OnSelectItem!=null)
                {
                    OnSelectItem.Invoke(selectedItem);
                    Close();
                }
            }
        }

        /// <summary>
        /// Загрузка данными грида
        /// </summary>
        private async void ItemsGridLoadItems()
        {
            DisableControl();

            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "Item");
            q.Request.SetParam("Action", "List");
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
                    var ds = ListDataSet.Create(result, "ITEMS");
                    ItemsGrid.UpdateItems(ds);
                }
            }

            EnableControl();
        }

        private void DisableControl()
        {
            FormToolbar.IsEnabled = false;
            ItemsGridToolbar.IsEnabled = false;
            ItemsGrid.ShowSplash();
        }

        private void EnableControl()
        {
            FormToolbar.IsEnabled = true;
            ItemsGridToolbar.IsEnabled = true;
            ItemsGrid.HideSplash();
        }

        /// <summary>
        /// отображение фрейма
        /// </summary>
        public void Show()
        {
            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 2;

            Central.WM.Show(FrameName, "Выбор ТМЦ", true, "add", this);
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            Central.WM.Close(FrameName);

            //вся работа по утилизации ресурсов происходит в Destroy
            //он будет вызван при закрытии фрейма
        }

        /// <summary>
        /// Выбрана позиция в гриде
        /// </summary>
        /// <param name="selectedItem"></param>
        private void ItemsGridUpdateActions(Dictionary<string, string> selectedItem)
        {
            ItemGridSelectedItem = selectedItem;

            if (ItemGridSelectedItem != null)
            {
                OkButton.IsEnabled = true;
            }
            else
            {
                OkButton.IsEnabled = false;
            }
        }

        /// <summary>
        /// функция фильтрации грида, по умолчанию показываются с кодами
        /// 1 и 2 (на поступлении и на складе)
        /// </summary>
        private void ItemsGridFilterItems()
        {
            //if (CheckShowAll.IsChecked != null)
            {
                //if (CheckShowAll.IsChecked == false)
                {
                    if (ItemsGrid.GridItems != null)
                    {
                        if (ItemsGrid.GridItems.Count > 0)
                        {
                            var list = new List<Dictionary<string, string>>();
                            foreach (var item in ItemsGrid.GridItems)
                            {
                                int itemStatusId = item.CheckGet("WMIS_ID").ToInt();
                                // 1 - на поступление
                                // 2 - на складе
                                if (itemStatusId == 1 || itemStatusId == 2)
                                {
                                    list.Add(item);
                                }
                            }

                            ItemsGrid.GridItems = list;
                        }
                    }
                }
            }
        }

        
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (ItemGridSelectedItem != null)
            {
                ItemsGridDblClickActions(ItemGridSelectedItem);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
