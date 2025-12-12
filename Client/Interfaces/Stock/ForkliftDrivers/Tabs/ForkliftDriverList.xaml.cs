using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Client.Interfaces.Stock.ForkliftDrivers.Windows;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// форма списка водителей погрузчиков
    /// </summary>
    /// <author>Михеев И.С.</author>
    public partial class ForkliftDriverList : UserControl
    {
        public ForkliftDriverList()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);
            PreviewKeyDown += ProcessKeyboard;

            InitForkliftDriverGrid();

            ProcessPermissions();
        }

        public int FactoryId = 1;

        public string RoleName = "[erp]forklift_drivers";

        private string FrameName = "ForkliftDrivers_List";

        private ListDataSet ForkliftDriverDataSet { get; set; }

        private Dictionary<string, string> ForkliftDriverSelectedItem { get; set; }

        /// <summary>
        /// обработчик системы навигации по URL
        /// </summary>
        public void ProcessNavigation()
        {

        }

        private void InitForkliftDriverGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "ИД водителя",
                    Path = "ID_FD",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 30,
                    MaxWidth = 70,
                },
                new DataGridHelperColumn
                {
                    Header = "ФИО",
                    Path = "NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 90,
                    MaxWidth = 120,
                },
                new DataGridHelperColumn
                {
                    Header = "Бригадир",
                    Path = "FOREMAN",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 90,
                    MaxWidth = 120,
                },
                new DataGridHelperColumn
                {
                    Header = "СГП",
                    Doc="Склад готовой продукции",
                    Path = "STOCK_PRODUCT_FLAG",
                    Group = "Место работы",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth = 40,
                    MaxWidth = 70,
                },
                new DataGridHelperColumn
                {
                    Header = "Рулоны",
                    Doc="Склад рулонов",
                    Path = "STOCK_ROLL_FLAG",
                    Group = "Место работы",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth = 40,
                    MaxWidth = 70,
                },
                new DataGridHelperColumn
                {
                    Header = "Макулатура",
                    Doc="Склад макулатуры",
                    Path = "STOCK_WASTEPAPER_FLAG",
                    Group = "Место работы",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth = 40,
                    MaxWidth = 70,
                },
                new DataGridHelperColumn
                {
                    Header = "Телефон",
                    Path = "PHONE",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 80,
                    MaxWidth = 90,
                },
                new DataGridHelperColumn
                {
                    Header = "Активен",
                    Path = "ACTIVE_FLAG",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth = 40,
                    MaxWidth = 70,
                },
                new DataGridHelperColumn
                {
                    Header = "Архивный",
                    Path = "ARCHIVE_FLAG",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth = 40,
                    MaxWidth = 70,
                },
                new DataGridHelperColumn
                {
                    Header = " ",
                    Path = "_",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 5,
                    MaxWidth = 1500,
                },

            };

            ForkliftDriverGrid.SetColumns(columns);

            ForkliftDriverGrid.Grid.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;

            ForkliftDriverGrid.SetSorting("NAME");

            ForkliftDriverGrid.SearchText = SearchText;
            ForkliftDriverGrid.Init();

            ForkliftDriverGrid.OnLoadItems = LoadItemsForkliftDriverGrid;

            ForkliftDriverGrid.Run();

            ForkliftDriverGrid.OnSelectItem = selectedItem => 
            { 
                ForkliftDriverSelectedItem = selectedItem;
                ProcessPermissions();
            };

            //двойной клик на строке откроет форму просмотра
            ForkliftDriverGrid.OnDblClick = selectedItem =>
            {
                EditDriver();
            };

            ForkliftDriverGrid.Focus();
        }

        private async void LoadItemsForkliftDriverGrid()
        {
            ForkliftDriverGrid.ShowSplash();
            GridToolbar.IsEnabled = false;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "ForkliftDriver");
            q.Request.SetParam("Action", "List");
            q.Request.SetParam("FACTORY_ID", $"{FactoryId}");

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                if (result != null)
                {
                    {
                        ForkliftDriverDataSet = ListDataSet.Create(result, "List");
                        ForkliftDriverGrid.UpdateItems(ForkliftDriverDataSet);
                    }
                }
            }

            ForkliftDriverGrid.HideSplash();
            GridToolbar.IsEnabled = true;
        }

        private void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/warehouse/rabota-voditelej-pogruzchikov");
        }

        private void ProcessKeyboard(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F5:
                    ForkliftDriverGrid.LoadItems();
                    e.Handled = true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

                case Key.Home:
                    ForkliftDriverGrid.SetSelectToFirstRow();
                    e.Handled = true;
                    break;

                case Key.End:
                    ForkliftDriverGrid.SetSelectToLastRow();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// вызов интерфейса добавления водителя
        /// </summary>
        private void AddDriver()
        {
            var driver = new ForkliftDriver();
            driver.ReturnTabName = this.FrameName;
            driver.DefaultFactoryId = this.FactoryId;
            driver.Edit(0);
        }

        /// <summary>
        /// Открытие вкладки редактирования данных водителя
        /// </summary>
        private void EditDriver()
        {
            if (Central.Navigator.GetRoleLevel(this.RoleName) >= Role.AccessMode.FullAccess)
            {
                if (ForkliftDriverSelectedItem != null)
                {
                    if (ForkliftDriverSelectedItem.ContainsKey("ID_FD"))
                    {
                        var driverId = ForkliftDriverSelectedItem["ID_FD"].ToInt();

                        var driver = new ForkliftDriver();
                        driver.ReturnTabName = this.FrameName;
                        driver.Edit(driverId);
                        EditButton.IsEnabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverName.Contains(this.FrameName))
            {
                switch (m.Action)
                {
                    case "Refresh":
                        ForkliftDriverGrid.LoadItems();
                        break;

                    case "Closed":
                        if (m.SenderName == "ForkliftDriver")
                        {
                            EditButton.IsEnabled = true;
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel(this.RoleName);
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

            if (ForkliftDriverGrid != null && ForkliftDriverGrid.Menu != null && ForkliftDriverGrid.Menu.Count > 0)
            {
                foreach (var manuItem in ForkliftDriverGrid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }
        }

        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage
            {
                ReceiverGroup = "ForkliftDriversControl",
                ReceiverName = "",
                SenderName = this.FrameName,
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            ForkliftDriverGrid.Destruct();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            LoadItemsForkliftDriverGrid();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddDriver();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            EditDriver();
        }
    }
}
