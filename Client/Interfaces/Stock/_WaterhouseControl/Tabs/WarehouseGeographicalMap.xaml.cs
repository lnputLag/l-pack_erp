using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
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
using System.Xml;
using Microsoft.Win32;
using static Client.Interfaces.Main.DataGridHelperColumn;
using System.IO;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Interaction logic for WarehouseGeographicalMap.xaml
    /// </summary>
    public partial class WarehouseGeographicalMap : UserControl
    {
        public Dictionary<string, string> StorageSelectedItem { get; private set; }

        private string MapData { get; set; }

        private MapZone Map { get; set; }

        private bool needNewMap = true;

        public string RoleName { get; set; }

        public WarehouseGeographicalMap()
        {
            InitializeComponent();
            RoleName = "[erp]warehouse_control";

            SetDefaults();
            StorageGridInit();
            RowGridInit();

            ProcessPermissions();

            //FIXME:
            // предотвращение 2йного срабатывания события selectItem
            StorageGrid.SelectItemMode = 2;

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);
        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel(RoleName);
            var userAccessMode = mode;
            switch (mode)
            {
                case Role.AccessMode.Special:
                    break;

                case Role.AccessMode.FullAccess:
                    break;

                case Role.AccessMode.ReadOnly:
                default:
                    break;
            }

            List<Button> buttons = UIUtil.GetVisualChilds<Button>(this.Content as DependencyObject);
            if (buttons != null && buttons.Count > 0)
            {
                foreach (var button in buttons)
                {
                    var buttonTagList = UIUtil.GetTagList(button);
                    var accessMode = Acl.FindTagAccessMode(buttonTagList);
                    if (accessMode > userAccessMode)
                    {
                        button.IsEnabled = false;
                    }
                }
            }
        }

        private void ProcessMessages(ItemMessage obj)
        {
            //throw new NotImplementedException();
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
                        Width = 80,
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

                StorageGrid.SearchText = StorageSearchBox;
                StorageGrid.Label = "StorageGrid";

                StorageGrid.SetSorting("NUM");
                StorageGrid.Init();

                StorageGrid.AutoUpdateInterval = 0;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                StorageGrid.OnSelectItem = selectedItem =>
                {
                    StorageGridUpdateActions(selectedItem);
                };

             
                //данные грида
                StorageGrid.OnLoadItems = ()=> StorageGridLoadItems(null);

                StorageGrid.Run();

                //фокус ввода           
                StorageGrid.Focus();

                StorageGrid.UseRowDragDrop = true;
              
            }

        }

        private void RowGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    //new DataGridHelperColumn
                    //{
                    //    Header="ИД",
                    //    Path="WMRO_ID",
                    //    Doc="ID ряда",
                    //    ColumnType=ColumnTypeRef.Integer,

                    //    Width = 40,
                    //},
                    new DataGridHelperColumn
                    {
                        Header="Ряд",
                        Path="ROW_NUM",
                        Doc="Ряд",
                        ColumnType=ColumnTypeRef.String,
                        Width = 60,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Свободно",
                        Path="FREE_CNT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Всего",
                        Path="ALL_CNT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 60,
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


                RowGrid.PrimaryKey = "WMRO_ID";
                RowGrid.SetColumns(columns);

                RowGrid.SearchText = StorageSearchBox;
                RowGrid.Label = "RowGrid";

                RowGrid.SetSorting("ROW_NUM");
                RowGrid.Init();

                RowGrid.AutoUpdateInterval = 0;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                //RowGrid.OnSelectItem = selectedItem =>
                //{
                //    RowGridUpdateActions(selectedItem);
                //};


                //данные грида
                RowGrid.OnLoadItems = RowGridLoadItems;
                //StorageGrid.OnFilterItems = StorageGridFilterItems;

                RowGrid.Run();

                //фокус ввода           
                RowGrid.Focus();

                RowGrid.UseRowDragDrop = true;
            }
        }

        private async void RowGridLoadItems()
        {
            if (Zone.SelectedItem.Key != string.Empty)
            {
                var p = new Dictionary<string, string>() { { "WMZO_ID", Zone.SelectedItem.Key } };

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Warehouse");
                q.Request.SetParam("Object", "Row");
                q.Request.SetParam("Action", "ListByZoneRack");
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

                        RowGrid.UpdateItems(ds);
                    }
                }
            }
        }

        private string NameConfigFrom(string MapId)
        {
            return "WAREHOUSE_GMAP_" + MapId;
        }

        private string GetConfigForMap(string MapId)
        {
            string resultConfig = string.Empty;
            string ConfigName = NameConfigFrom(MapId);

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("HOST_USER_ID", ConfigName);
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Service");
            q.Request.SetParam("Object", "Control");
            q.Request.SetParam("Action", "GetConfig");
            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds.Items.Count > 0)
                        {
                            foreach (Dictionary<string, string> row in ds.Items)
                            {
                                if (row.CheckGet("HOST_USER_ID") == ConfigName)
                                {
                                    //Form.SetValues(row);
                                    resultConfig = row.CheckGet("CONTENT");
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return resultConfig;
        }

        private void SetConfigForMap(string MapId, string data)
        {
            string ConfigName = NameConfigFrom(MapId);

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("HOST_USER_ID", ConfigName);
                p.CheckAdd("CONTENT", data);
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Service");
            q.Request.SetParam("Object", "Control");
            q.Request.SetParam("Action", "SaveConfig");

            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {

                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {

                    //var ds = ListDataSet.Create(result, "ITEMS");
                    //var hostname = ds.GetFirstItemValueByKey("HOST_USER_ID").ToString();

                }

            }
        }

        private void StorageGridUpdateActions(Dictionary<string, string> selectedItem)
        {
            StorageSelectedItem = selectedItem;
        }

        private DateTime lastDate;

        private int lastKey = -1;

        private void StorageGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender != null && e.LeftButton == MouseButtonState.Pressed)
            {
                {
                    try
                    {
                        // Package the data.
                        if (StorageSelectedItem != null)
                        {
                            lock (StorageSelectedItem)
                            {
                                int wmss_id = StorageSelectedItem.CheckGet("WMST_ID").ToInt();

                                Console.WriteLine("Тест " + wmss_id + " " + lastKey);
                                if (lastKey != wmss_id)
                                {
                                    lastKey = wmss_id;
                                    DataObject data = new DataObject();
                                    data.SetData("CellObject", JsonConvert.SerializeObject(StorageSelectedItem));
                                    //data.SetData("Object", StorageSelectedItem);

                                    // Initiate the drag-and-drop operation.
                                    DragDrop.DoDragDrop(StorageGrid, data, DragDropEffects.Copy | DragDropEffects.Move);
                                }
                                
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.ToString());

                    }
                   
                }
            }
        }

        /// <summary>
        /// Загрузка данных
        /// </summary>
        private async void StorageGridLoadItems(string mapData = null)
        {
            if (Zone.SelectedItem.Key != string.Empty)
            {
                var p = new Dictionary<string, string>() { { "WMZO_ID", Zone.SelectedItem.Key } };

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Warehouse");
                q.Request.SetParam("Object", "Storage");
                q.Request.SetParam("Action", "ListByZone");
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

                        SaveButton.IsEnabled = false;

                        if (needNewMap)
                        {
                            Map = new MapZone();
                            Map.GetCellRow = GetCellsForRow;
                            Map.Id = Zone.SelectedItem.Key;

                            Map.Width = Scroll.ActualWidth;
                            Map.Height = Scroll.ActualHeight;

                            MapData = mapData==null ? GetConfigForMap(Map.Id) : mapData;
                            Map.SetData(MapData, Zone.SelectedItem.Key.ToInt());

                            Map.HorizontalAlignment = HorizontalAlignment.Left;
                            Map.VerticalAlignment = VerticalAlignment.Top;


                            Map.DropItem += Map_DropItem;
                            Scroll.Content = Map;
                        }
                        else
                        {
                            MapData = mapData==null ? Map.GetData() : mapData;
                        }


                        Dictionary<string, string> filtersCell = new Dictionary<string, string>();

                        if (!string.IsNullOrEmpty(mapData))
                        {
                            // подготовка списка ячеек, без ячеек что уже отображаются на карте
                            XmlDocument document = new XmlDocument();
                            document.LoadXml(MapData);

                            XmlNodeList cells = document.SelectNodes("/Cells/Item");

                            foreach (XmlNode node in cells)
                            {
                                if (!filtersCell.ContainsKey(node.Attributes["ID"].Value))
                                {
                                    filtersCell.Add(node.Attributes["ID"].Value, node.Attributes["ID"].Value);
                                }
                            }
                        }

                        List<Dictionary<string, string>> list = new List<Dictionary<string, string>>();

                        // пробегаем по полученным ячейкам и добавляем лишь те что не сожержат уже добавленных на карту
                        foreach (var item in ds.Items)
                        {
                            if (!filtersCell.ContainsKey(item.CheckGet("WMST_ID").ToInt().ToString()))
                            {
                                list.Add(item);
                            }
                        }

                        StorageGrid.UpdateItems(ListDataSet.Create(list));
                       

                        if (needNewMap)
                        {
                            Map.OnChangeMap += Map_OnChangeMap;
                            Map.OnDeleteItems += Map_OnDeleteItems;
                        }
                    }
                }
            }
        }

        private List<Dictionary<string, string>> GetCellsForRow(int row)
        {
            List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();
            // возможно что список уже пуст
            if (StorageGrid.Items != null)
            {
                foreach (var item in StorageGrid.Items)
                {
                    if (item.CheckGet("WMRO_ID").ToInt() == row)
                    {
                        result.Add(item);
                    }
                }
            }

            return result;
        }

        private void Map_OnChangeMap(object sender, RoutedEventArgs e)
        {
            SaveButton.IsEnabled = true;
        }

        private void DeleteItemFromGrid(int id)
        {
            int i, n = StorageGrid.Items.Count;
            for (i = 0; i < n; i++)
            {
                if (StorageGrid.Items[i].CheckGet("WMST_ID").ToInt() == id)
                {
                    StorageGrid.Items.Remove(StorageGrid.Items[i]);
                    break;
                }
            }
        }

        private void Map_OnDeleteItems(List<Cell> items)
        {
            needNewMap = false;
            StorageGridLoadItems();

        }

        private void Map_DropItem(object sender, Dictionary<string, string> item)
        {
            if (item.ContainsKey("WMST_ID"))
            {
                DeleteItemFromGrid(item.CheckGet("WMST_ID").ToInt());

                StorageGrid.UpdateItems();
            }
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            lastDate = DateTime.Now;
            FormHelper.ComboBoxInitHelper(Zone, "Warehouse", "Zone", "List", "WMZO_ID", "ZONE_FULL_NAME", null, true);


            //Scroll.ContextMenu
           
        }

        private void StorageFilter_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            needNewMap = true;

            StorageGridLoadItems();
            RowGridLoadItems();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            needNewMap = true;
            StorageGridLoadItems();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (Map != null)
            {
                SetConfigForMap(Map.Id, Map.GetData());

            }

            SaveButton.IsEnabled = false;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Map.DeleteSelected();
        }

        private void LineButton_Click(object sender, RoutedEventArgs e)
        {
            Map.LineSelected();
        }

        private void SaveToFile(object sender, RoutedEventArgs e)
        {
            if (Map != null)
            {
                var fileName = Zone.SelectedItem.Value as string;
                var data = Map.GetData();

                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.FileName = fileName + ".xml";
                saveFileDialog.DefaultExt = ".xml";

                if (saveFileDialog.ShowDialog() == true)
                {
                    File.WriteAllText(saveFileDialog.FileName, data);
                }
            }

        }

        private void LoadFromFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            if(openFileDialog.ShowDialog() == true)
            {
                string data = File.ReadAllText(openFileDialog.FileName);

                StorageGridLoadItems(data);
                RowGridLoadItems();
            }

        }

        private void MenuItem_CDeleteBackround(object sender, RoutedEventArgs e)
        {
            if(Map!=null)
            {
                Map.DeleteBackground();
            }

        }
    }
}
