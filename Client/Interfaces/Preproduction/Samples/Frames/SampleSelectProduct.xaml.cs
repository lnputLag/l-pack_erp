using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Выбор изделия, для которого будет создан образец с линии
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleSelectProduct : UserControl
    {
        public SampleSelectProduct()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            ProcessPermissions();

            InitGrid();
            SetDefaults();
        }

        /// <summary>
        /// ИД заказчика/клиента
        /// </summary>
        public int CustomerId;

        /// <summary>
        /// Имя вкладки, куда происходит возврат при закрытии этой вкладки
        /// </summary>
        public string ReceiverName;

        /// <summary>
        /// Имя этой вкладки
        /// </summary>
        public string TabName;
        /// <summary>
        /// Право на выполнение специальных действий
        /// </summary>
        public bool MasterRights;


        private ListDataSet CardboardDS { get; set; }

        /// <summary>
        /// Обработка сообщений
        /// </summary>
        /// <param name="obj"></param>
        private void ProcessMessages(ItemMessage obj)
        {
            if (obj.ReceiverGroup.IndexOf("PreproductionSample") > -1)
            {
                if (obj.ReceiverName.IndexOf(TabName) > -1)
                {
                }
            }
        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Если пользователь имеет спецправа, включаем режим мастера
            var mode = Central.Navigator.GetRoleLevel("[erp]sample");
            switch (mode)
            {
                case Role.AccessMode.Special:
                    MasterRights = true;
                    break;

                default:
                    MasterRights = false;
                    break;
            }
        }

        /// <summary>
        /// Инициализация таблицы изделий
        /// </summary>
        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>()
            {
                new DataGridHelperColumn()
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    Doc="Номер по порядку в списке",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=50,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="PRODUCT_CODE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=140,
                },
                new DataGridHelperColumn
                {
                    Header="Картон",
                    Path="CARDBOARD_NAME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=160,
                },
                new DataGridHelperColumn
                {
                    Header="Изделие",
                    Path="PRODUCT_NAME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=340,
                },
                new DataGridHelperColumn
                {
                    Header="Дата отгрузки",
                    Path="SHIPMENT_DATE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=80,
                },
                new DataGridHelperColumn()
                {
                    Header="Есть заготовки",
                    Path="CORRURATOR_COMPLETE",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ИД изделия",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Категория изделия",
                    Path="CATEGORY",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Размеры",
                    Path="PRODUCT_SIZE",
                    ColumnType=ColumnTypeRef.String,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Профиль",
                    Path="PROFILE_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="картон",
                    Path="CARDBOARD_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            Grid.SetColumns(columns);
            Grid.AutoUpdateInterval = 0;
            Grid.SearchText = SearchText;
            // Раскраска строк
            Grid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                // Цвета фона строк
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=System.Windows.DependencyProperty.UnsetValue;
                        var color = "";

                        // готова
                        if (row.CheckGet("CORRURATOR_COMPLETE").ToInt() == 1)
                        {
                            color = HColor.YellowOrange;
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

            Grid.OnLoadItems = LoadItems;
            Grid.OnFilterItems = FilterItems;
            Grid.OnDblClick = selectedItem =>
            {
                Save();
            };
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            CustomerId = 0;
            TabName = "";
        }

        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        private async void LoadItems()
        {
            string customer = CustomerId.ToString();
            if (CustomerId == 4202)
            {
                customer = "0";
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "ListOrderdates");
            q.Request.SetParam("CUSTOMER_ID", customer);
            //Если есть спецправа, то показываем заявки, в которых завершено производство заготовок
            q.Request.SetParam("BLANK_COMPLETED", MasterRights ? "1" : "0");

            q.Request.Timeout = Central.Parameters.RequestTimeoutMax;
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
                    CardboardDS = ListDataSet.Create(result, "CARDBOARD");

                    var profileDS = ListDataSet.Create(result, "PROFILE");
                    var dict = new Dictionary<string, string>();
                    dict.Add("0", "Все");
                    foreach (var item in profileDS.Items)
                    {
                        dict.Add(item["ID"], item["NAME"]);
                    }
                    ProfileType.Items = dict;
                    ProfileType.SetSelectedItemByKey("0");

                    var ds = ListDataSet.Create(result, "PRODUCTS");
                    Grid.UpdateItems(ds);
                }
            }
        }

        /// <summary>
        /// Фильтрация записей таблицы
        /// </summary>
        private void FilterItems()
        {
            if (Grid.GridItems != null)
            {
                if (Grid.GridItems.Count > 0)
                {
                    int profileId = ProfileType.SelectedItem.Key.ToInt();
                    int cardboardId = Cardboard.SelectedItem.Key.ToInt();

                    var list = new List<Dictionary<string, string>>();
                    foreach (var row in Grid.GridItems)
                    {
                        bool includeByProfile = true;
                        bool includeByCardboard = true;

                        if (profileId > 0)
                        {
                            includeByProfile = false;
                            if (row.CheckGet("PROFILE_ID").ToInt() == profileId)
                            {
                                includeByProfile = true;
                            }
                        }

                        if (cardboardId > 0)
                        {
                            includeByCardboard = false;
                            if (row.CheckGet("CARDBOARD_ID").ToInt() == cardboardId)
                            {
                                includeByCardboard = true;
                            }
                        }

                        if (
                            includeByProfile
                            && includeByCardboard
                        )
                        {
                            list.Add(row);
                        }
                    }

                    Grid.GridItems = list;
                }
            }
        }

        /// <summary>
        /// Показывает текущую вкладку
        /// </summary>
        public void Show()
        {
            string title = $"Изделия для образца";
            TabName = $"SampleProduct";
            Central.WM.AddTab(TabName, title, true, "add", this);
            Grid.LoadItems();
        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
        public void Close()
        {
            Central.WM.RemoveTab(TabName);

            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "PreproductionSample",
                ReceiverName = "",
                SenderName = TabName,
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            Central.WM.SetActive(ReceiverName, true);
            ReceiverName = "";
        }

        /// <summary>
        /// Обновление списка картона в фильтре
        /// </summary>
        private void UpdateCardboardFilter()
        {
            int profileId = ProfileType.SelectedItem.Key.ToInt();
            var cardboardDict = new Dictionary<string, string>();
            cardboardDict.Add("0", " ");
            bool include = true;
            foreach (var c in CardboardDS.Items)
            {
                include = true;
                if (profileId > 0)
                {
                    if (c["PROFILE_ID"].ToInt() != profileId)
                    {
                        include = false;
                    }
                }

                if (include)
                {
                    cardboardDict.Add(c["ID"], c["NAME"]);
                }
            }
            Cardboard.Items = cardboardDict;
            Cardboard.SetSelectedItemByKey("0");

            Grid.UpdateItems();
        }

        /// <summary>
        /// Выбор изделия. Передача данных в окно редактирования образца
        /// </summary>
        private void Save()
        {
            if (Grid.SelectedItem != null)
            {
                //отправляем сообщение о закрытии окна
                Messenger.Default.Send(new ItemMessage()
                {
                    ReceiverGroup = "PreproductionSample",
                    ReceiverName = ReceiverName,
                    SenderName = TabName,
                    Action = "SelectProduct",
                    ContextObject = Grid.SelectedItem,
                });
                Close();
            }
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AllOrdersCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        private void ProfileType_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UpdateCardboardFilter();
        }

        private void Cardboard_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }
    }
}
