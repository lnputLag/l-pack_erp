using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Вкладка поддоны на складе
    /// </summary>
    public partial class PalletList : UserControl
    {
        /// <summary>
        /// Инициализация
        /// </summary>
        public PalletList()
        {
            InitializeComponent();

            FactIdSelectBox.SelectedItemChanged += (DependencyObject d, DependencyPropertyChangedEventArgs e) => {
                Grid.LoadItems();
            };

            //регистрация обработчика клавиатуры
            PreviewKeyDown += ProcessKeyboard;

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            SetDefaults();

            InitGridPallets();
            InitGridPalletsInApplications();
            InitGridQty();

            ProcessPermissions();
        }

        /// <summary>
        /// Данные для таблицы списка поддонов
        /// </summary>
        public ListDataSet PalletsDS { get; set; }

        /// <summary>
        /// Выбранная строка в таблице поддонов
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// обработчик системы навигации по URL
        /// </summary>
        public void ProcessNavigation()
        {

        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel("[erp]pallet");
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

            if (Grid != null && Grid.Menu != null && Grid.Menu.Count > 0)
            {
                foreach (var manuItem in Grid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }

            if (GridQty != null && GridQty.Menu != null && GridQty.Menu.Count > 0)
            {
                foreach (var manuItem in GridQty.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }

            if (PalletsInApplication != null && PalletsInApplication.Menu != null && PalletsInApplication.Menu.Count > 0)
            {
                foreach (var manuItem in PalletsInApplication.Menu)
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

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            FactIdSelectBox.Items = new Dictionary<string, string>() {
                { "1", "Л-ПАК ЛИПЕЦК" },
                { "2", "Л-ПАК КАШИРА" },
            };
            FactIdSelectBox.SelectedItem = FactIdSelectBox.Items.First();
            DateOrderFrom.Text = DateTime.Now.ToString("dd.MM.yyyy");
            DateOrderTo.Text = DateTime.Now.AddDays(6).ToString("dd.MM.yyyy");
            SearchText.Text="";
        }

        /// <summary>
        /// Обработчики нажатий клавиш
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessKeyboard(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F5:
                    Grid.LoadItems();
                    e.Handled = true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

                case Key.Home:
                    Grid.SetSelectToFirstRow();
                    e.Handled = true;
                    break;

                case Key.End:
                    Grid.SetSelectToLastRow();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// Деструктор компонента
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии фрейма
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="Stock",
                ReceiverName = "",
                SenderName = "PalletList",
                Action = "Closed",
            });
            
            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
            
            //останавливаем таймеры гридов
            GridQty.Destruct();
            Grid.Destruct();
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="obj">сообщение</param>
        private void ProcessMessages(ItemMessage obj)
        {
            if (obj.ReceiverGroup.IndexOf("Stock") > -1)
            {
                if (obj.ReceiverName.IndexOf("PalletList") > -1)
                {
                    switch (obj.Action)
                    {
                        case "Refresh":
                            Grid.LoadItems();
                            break;
                    }
                }
                switch (obj.Action)
                {
                    case "PalletListRefresh":
                        Grid.LoadItems();
                        break;
                }
            }
        }

        /// <summary>
        /// Инициализация таблицы поддонов
        /// </summary>
        private void InitGridPallets()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=30,
                },
                new DataGridHelperColumn
                {
                    Header="ИД поддона",
                    Path="ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=90,
                },
                new DataGridHelperColumn
                {
                    Header="Поддон",
                    Path="FULL_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=150,
                    MaxWidth=220,
                },
                new DataGridHelperColumn
                {
                    Header="Всего",
                    Path="QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=60,
                },
                new DataGridHelperColumn
                {
                    Header="На складе",
                    Path="STOCK_CNT",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="Без продукции",
                    Path="QTY_FREE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="Остаются",
                    Path="C_IN",
                    Group="Под продукцией",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=70,
                },
                new DataGridHelperColumn
                {
                    Header="Отгрузятся",
                    Path="C_OUT",
                    Group="Под продукцией",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="В заявке",
                    Path="QTY_QP",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=70,
                    Stylers=new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                if (row["QTY_QP"].ToInt() > row["QTY_FREE"].ToInt())
                                {
                                    color = HColor.Red;
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result=color.ToBrush();
                                }

                                return result;
                            }
                        },
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Под заготовки",
                    Path="QTY_Z",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header=" ",
                    Path="_",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=1,
                    MaxWidth=600,
                },
                new DataGridHelperColumn
                {
                    Header="Приобретено",
                    Path="PURCHASED_FLAG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    Hidden=true,
                },
            };
            Grid.SetColumns(columns);
            Grid.SearchText=SearchText;
            Grid.Init();

            // контекстное меню
            Grid.Menu = new Dictionary<string, DataGridContextMenuItem>()
            {
                {
                    "SetBalance",
                    new DataGridContextMenuItem()
                    {
                        Header="Выравнивание остатков",
                        Tag = "access_mode_full_access",
                        Action=() =>
                        {
                            SetBalance();
                        }
                    }
                },

            };

            //данные грида
            Grid.OnLoadItems = LoadItemsPallets;
            Grid.Run();

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdateActions(selectedItem);
                }
            };

            //двойной клик на строке откроет форму редактирования
            Grid.OnDblClick=(Dictionary<string,string> selectedItem) =>
            {
                SetBalance();
            };

            //фокус ввода           
            Grid.Focus();
        }


        /// <summary>
        /// Инициализация таблицы с позициями в поддонах
        /// </summary>
        private void InitGridPalletsInApplications()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "Название заказа",
                    Path = "ORDER_NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 33
                },
                new DataGridHelperColumn
                {
                    Header = "Артикул",
                    Path = "ARTIKUL",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 16
                },
                new DataGridHelperColumn
                {
                    Header = "Название",
                    Path = "NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 34
                },
                new DataGridHelperColumn
                {
                    Header = "Количество",
                    Path = "QTY",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2 = 5
                },
            };

            PalletsInApplication.SetColumns(columns);
            PalletsInApplication.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            PalletsInApplication.Init();

            PalletsInApplication.Run();
        }

        /// <summary>
        /// Инициализация таблицы количества по линиям
        /// </summary>
        private void InitGridQty()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Станок",
                    Path="NAME_ST",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=70,
                    MaxWidth=400,
                },
                new DataGridHelperColumn
                {
                    Header="Количество",
                    Path="QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=80,
                },
            };
            GridQty.SetColumns(columns);

            GridQty.Init();
        }

        /// <summary>
        /// Загрузка данных в таблицу поддонов
        /// </summary>
        private async void LoadItemsPallets()
        {
            bool resume = true;
            GridToolbar.IsEnabled = false;
            Grid.ShowSplash();
            GridQty.ShowSplash();

            if (resume)
            {
                var df = DateOrderFrom.Text.ToDateTime();
                var dt = DateOrderTo.Text.ToDateTime();
                if (DateTime.Compare(df, dt) > 0)
                {
                    var msg = "Дата начала должна быть меньше даты окончания.";
                    var d = new DialogWindow($"{msg}", "Проверка данных");
                    d.ShowDialog();
                    resume = false;
                }
            }

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Stock");
                q.Request.SetParam("Object", "Pallet");
                q.Request.SetParam("Action", "List");
                q.Request.SetParam("FACT_ID", FactIdSelectBox.SelectedItem.Key);
                q.Request.SetParam("DATE_ORDER_FROM", DateOrderFrom.Text);
                q.Request.SetParam("DATE_ORDER_TO", DateOrderTo.Text);

                q.Request.Timeout = 60000;
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
                        PalletsDS = ListDataSet.Create(result, "PALLETS");
                        GridQty.UpdateItems(new ListDataSet());
                        PalletsInApplication.UpdateItems(new ListDataSet());
                        Grid.UpdateItems(PalletsDS);
                    }
                }
            }

            Grid.HideSplash();
            GridQty.HideSplash();
            GridToolbar.IsEnabled = true;
        }

        /// <summary>
        /// Загрузка данных в таблицу количества по линиям
        /// </summary>
        private async void LoadItemsQty()
        {
            GridQty.ShowSplash();

            int idPal = 0;
            if (SelectedItem != null)
            {
                if (SelectedItem.ContainsKey("ID"))
                {
                    idPal = SelectedItem["ID"].ToInt();
                }
            }

            if (idPal > 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Stock");
                q.Request.SetParam("Object", "Pallet");
                q.Request.SetParam("Action", "ListOrderdatesQty");
                q.Request.SetParam("FACT_ID", FactIdSelectBox.SelectedItem.Key);
                q.Request.SetParam("DateFrom", DateOrderFrom.Text);
                q.Request.SetParam("DateTo", DateOrderTo.Text);
                q.Request.SetParam("IdPal", idPal.ToString());

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
                        var QtyDS = ListDataSet.Create(result, "OrderdatesQty");
                        GridQty.UpdateItems(QtyDS);
                    }
                }
            }

            GridQty.HideSplash();
        }


        /// <summary>
        /// Загрузка позиций выбранного поддона
        /// </summary>
        /// <param name="idPal">ИД палета</param>
        private async void LoadItemsPalletsInApplication()
        {
            int idPal = 0;
            if (SelectedItem != null)
            {
                if (SelectedItem.ContainsKey("ID"))
                {
                    idPal = SelectedItem["ID"].ToInt();
                }
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Pallet");
            q.Request.SetParam("Action", "ListPalletInApplication");
            q.Request.SetParam("FACT_ID", FactIdSelectBox.SelectedItem.Key);
            q.Request.SetParam("DATE_FROM", DateOrderFrom.Text);
            q.Request.SetParam("DATE_TO", DateOrderTo.Text);
            q.Request.SetParam("ID_PAL", idPal.ToString());

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
                    var palletsInApplicationItems = ListDataSet.Create(result, "LIST_PA");
                    PalletsInApplication.UpdateItems(palletsInApplicationItems);
                }
            }
        }

        /// <summary>
        /// Обновление действий для выбранной строки в таблице поддонов
        /// </summary>
        /// <param name="selectedItem"></param>
        private void UpdateActions(Dictionary<string, string> selectedItem)
        {
            SelectedItem = selectedItem;
            LoadItemsQty();
            LoadItemsPalletsInApplication();

            ProcessPermissions();
        }

        /// <summary>
        /// Вызов справки
        /// </summary>
        private void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/warehouse/pallets#block1");
        }

        /// <summary>
        /// Вызов фрейма изменения расположения поддона, количества в ремонте и на собственные нужды
        /// </summary>
        private void ChangeLocation()
        {
            if (Central.Navigator.GetRoleLevel("[erp]pallet") >= Role.AccessMode.FullAccess)
            {
                if (SelectedItem != null)
                {
                    if (SelectedItem.ContainsKey("ID"))
                    {
                        int idPal = SelectedItem["ID"].ToInt();
                        if (idPal > 0)
                        {
                            var locationForm = new PalletLocation();
                            locationForm.ReturnTabName = "Pallets_List";
                            locationForm.Edit(idPal);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// экспорт записей грида в Excel
        /// </summary>
        private async void ExportToExcel()
        {
            if(Grid!=null)
            {
                if(Grid.Items.Count>0)
                {
                    var eg = new ExcelGrid();
                    var cols=Grid.Columns;
                    eg.SetColumnsFromGrid(cols);
                    eg.Items = Grid.Items;
                    await Task.Run(() =>
                    {
                        eg.Make();
                    });
                }
            }
        }

        /// <summary>
        /// Обработчик нажатия на кнопку Показать
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку экспорта в Excel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExportButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            ExportToExcel();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку справки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void EditButton_Click(object sender,RoutedEventArgs e)
        {
            ChangeLocation();
        }
        private void SetBalance()
        {
            if (Central.Navigator.GetRoleLevel("[erp]pallet") >= Role.AccessMode.FullAccess)
            {
                if (Grid.SelectedItem != null)
                {
                    if (Grid.SelectedItem.ContainsKey("ID"))
                    {
                        int id = Grid.SelectedItem["ID"].ToInt();
                        if (id > 0)
                        {
                            var locationForm = new SetBalance();
                            locationForm.ReturnTabName = "Pallets_List";
                            locationForm.Set(id, Grid.SelectedItem["QTY"].ToInt(), FactIdSelectBox.SelectedItem.Key.ToInt());
                        }
                    }
                }
            }
        }
    }
}
