using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Shipments;
using GalaSoft.MvvmLight.Messaging;
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
    /// Interaction logic for SelectCell.xaml
    /// </summary>
    public partial class SelectCell : UserControl
    {
        public Dictionary<string, string> StorageGridSelectedItem { get; private set; }
        public string FrameName { get; private set; }

        public delegate void OnSelectedCellDelegate(Dictionary<string, string> storageGridSelectedItem);
        public event OnSelectedCellDelegate OnSelectedCell;

        public SelectCell()
        {
            FrameName = "SelectCell";

            InitializeComponent();

            SetDefaults();
            StorageGridInit();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);
        }

        private void ProcessMessages(ItemMessage obj)
        {
            
        }

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "wms",
                ReceiverName = "",
                SenderName = FrameName,
                Action = "Closed",
            });


            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }


        /// <summary>
        /// Настройки по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            WarehouseSelectBox.Items.Add("-1", "Все склады");
            FormHelper.ComboBoxInitHelper(WarehouseSelectBox, "Warehouse", "Warehouse", "List", "WMWA_ID", "WAREHOUSE", null, true);
        }


        /// <summary>
        /// Инициализация грида
        /// </summary>
        private void StorageGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="WMST_ID",
                        Doc="ID хранилища",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ячейка",
                        Path="NUM",
                        Doc="Ячейка",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 100,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Состояние",
                        Path="STATUS",
                        Doc="Состояние",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 100,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="WAREHOUSE",
                        Doc="Наименование склада",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 140,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Область хранения",
                        Path="AREA",
                        Doc="Область хранения",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 140,
                    },
                    
                    new DataGridHelperColumn
                    {
                        Header=" ",
                        Path="_",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=5,
                        MaxWidth=2000,
                    },
                };

                StorageGrid.PrimaryKey = "WMST_ID";
                StorageGrid.SetColumns(columns);

                StorageGrid.SearchText = FilterTextBox;
                //RoleGrid.UseRowHeader = false;
                StorageGrid.Label = "StorageGrid";
                StorageGrid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                StorageGrid.OnSelectItem = selectedItem =>
                {
                    StorageGridUpdateActions(selectedItem);
                };

                StorageGrid.OnDblClick = selectedItem =>
                {
                    StorageGridDblClickActions(selectedItem);
                };


                //данные грида
                StorageGrid.OnLoadItems = StorageGridLoadItems;

                StorageGrid.OnFilterItems = () =>
                {
                    if (StorageGrid.GridItems != null && StorageGrid.GridItems.Count > 0)
                    {
                        if (WarehouseSelectBox != null && WarehouseSelectBox.SelectedItem.Key != null)
                        {
                            var key = WarehouseSelectBox.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            switch (key)
                            {
                                // Все склады
                                case -1:
                                    // 1 Заблокирована
                                    // 6 Недоступна
                                    items.AddRange(StorageGrid.GridItems.Where(x => x.CheckGet("WMSS_ID").ToInt() != 1 && x.CheckGet("WMSS_ID").ToInt() != 6));
                                    break;

                                default:
                                    items.AddRange(StorageGrid.GridItems.Where(x => x.CheckGet("WMWA_ID").ToInt() == key && x.CheckGet("WMSS_ID").ToInt() != 1 && x.CheckGet("WMSS_ID").ToInt() != 6));
                                    break;
                            }

                            StorageGrid.GridItems = items;
                        }

                        if (StorageAreaSelectBox != null && StorageAreaSelectBox.SelectedItem.Key != null)
                        {
                            var key = StorageAreaSelectBox.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            switch (key)
                            {
                                // Все области хранения
                                case -1:
                                    items = StorageGrid.GridItems;
                                    break;

                                default:
                                    items.AddRange(StorageGrid.GridItems.Where(x => x.CheckGet("WMSA_ID").ToInt() == key ));
                                    break;
                            }

                            StorageGrid.GridItems = items;
                        }
                    }
                };

                StorageGrid.Run();

                //фокус ввода           
                StorageGrid.Focus();
            }
        }

        /// <summary>
        /// Двойной клик по выбранной позиции в гриде
        /// </summary>
        /// <param name="selectedItem"></param>
        private void StorageGridDblClickActions(Dictionary<string, string> selectedItem)
        {
            if (StorageGridSelectedItem != null)
            {
                if (OnSelectedCell != null)
                {
                    Close();
                    OnSelectedCell(selectedItem);
                }
            }
        }

        /// <summary>
        /// Функция загрузки таблицу данными
        /// </summary>
        private async void StorageGridLoadItems()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "Storage");
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
                    StorageGrid.UpdateItems(ds);
                }
            }
        }

        /// <summary>
        /// выбрана строка в таблице, проверим можем ли мы с ней совершать действия
        /// </summary>
        /// <param name="selectedItem"></param>
        private void StorageGridUpdateActions(Dictionary<string, string> selectedItem)
        {
            StorageGridSelectedItem = selectedItem;

            if(StorageGridSelectedItem!=null)
            {
                OkButton.IsEnabled = true;
            }
            else
            {
                OkButton.IsEnabled = false;
            }
        }

        public void SelectWarehouse()
        {
            ClearSelectBox(StorageAreaSelectBox);
            StorageAreaSelectBox.Items.Add("-1", "Все области хранения");
            FormHelper.ComboBoxInitHelper(StorageAreaSelectBox, "Warehouse", "StorageArea", "ListByWarehouse", "WMSA_ID", "AREA", new Dictionary<string, string>() { { "WMWA_ID", WarehouseSelectBox.SelectedItem.Key } }, true, false);
            StorageAreaSelectBox.SetSelectedItemByKey("-1");
        }

        /// <summary>
        /// Очищаем наполнение селектбокса
        /// </summary>
        /// <param name="selectBox"></param>
        private void ClearSelectBox(SelectBox selectBox)
        {
            selectBox.DropDownListBox.Items.Clear();
            selectBox.DropDownListBox.SelectedItem = null;
            selectBox.ValueTextBox.Text = "";
            selectBox.Items = new Dictionary<string, string>();
            selectBox.SelectedItem = new KeyValuePair<string, string>();
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

            Central.WM.Show(FrameName, "Выбор ячейки", true, "add", this);
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
        /// при смене склада необходимо обновить грид отфильтровав по складу
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void Warehouse_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SelectWarehouse();
            StorageGrid.UpdateItems();
        }

        /// <summary>
        /// кнопка выбора то же самое что и двойной клик на позиции в гриде
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            StorageGridDblClickActions(StorageGridSelectedItem);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if(WarehouseSelectBox.SelectedItem.Value==null)
            {
                WarehouseSelectBox.IsEnabled = true;
            }

            if (StorageAreaSelectBox.SelectedItem.Value == null)
            {
                StorageAreaSelectBox.IsEnabled = true;
            }
        }

        private void StorageAreaSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            StorageGrid.UpdateItems();
        }
    }
}
