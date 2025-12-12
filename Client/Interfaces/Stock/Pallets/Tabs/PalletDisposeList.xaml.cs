using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
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

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Утилизация паллета
    /// </summary>
    public partial class PalletDisposeList : ControlBase
    {
        public PalletDisposeList()
        {
            InitializeComponent();
            ControlTitle = "Утилизация паллета";

            ProcessPermissions();

            OnMessage = (ItemMessage message) => {
                DebugLog($"message=[{message.Message}]");
            };

            OnLoad = () =>
            {
                SetDefaults();
                ItemGridInit();

                Messenger.Default.Register<ItemMessage>(this, ProcessMessages);
            };

            OnUnload = () =>
            {
                Messenger.Default.Unregister<ItemMessage>(this);

                ItemsGrid.Destruct();
            };

            OnFocusGot = () =>
            {
                ItemsGrid.ItemsAutoUpdate = true;
            };

            OnFocusLost = () =>
            {
                ItemsGrid.ItemsAutoUpdate = false;
            };
        }

        /// <summary>
        /// выбранная строка
        /// </summary>
        private Dictionary<string, string> SelectedItem { get; set; }

        private void SetDefaults()
        {
            var factorySelectBoxItems = new Dictionary<string, string>();
            factorySelectBoxItems.Add("1", "Л-ПАК ЛИПЕЦК");
            factorySelectBoxItems.Add("2", "Л-ПАК КАШИРА");
            FactorySelectBox.SetItems(factorySelectBoxItems);
            FactorySelectBox.SetSelectedItemByKey("1");
        }

        private void ItemGridInit()
        {
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="NAME",
                        Doc="Наименование",
                        ColumnType=ColumnTypeRef.String,
                        Width = 340,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул",
                        Path="ARTIKUL",
                        Doc="Наименование",
                        ColumnType=ColumnTypeRef.String,
                        Width = 140,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер поддона",
                        Path="NUM",
                        Doc="Номер поддона",
                        ColumnType=ColumnTypeRef.String,
                        Width = 100,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ячейка",
                        Path="PLACE",
                        Doc="Ячейка",
                        ColumnType=ColumnTypeRef.String,
                        Width = 100,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Path="QTY",
                        Doc="Количество",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 100,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Площадь",
                        Path="SQUARE",
                        Doc="Площадь",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 100,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата прихода",
                        Path="PRIHOD_DTTM",
                        Doc="Дата прихода",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width = 120,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дней с даты прихода",
                        Path="PRIHOD_DAY",
                        Doc="Дней с даты прихода",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 100,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус",
                        Path="STATUS",
                        Doc="Статус",
                        ColumnType=ColumnTypeRef.String,
                        Width = 100,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид площадки",
                        Path="FACTORY_ID",
                        Doc="Ид площадки",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 5,
                        Hidden=true,
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

                            int currentStatus = row.CheckGet("DISPOSAL_STATUS").ToInt();

                            switch(currentStatus)
                            {
                                case 3: // Списана
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

                ItemsGrid.PrimaryKey = "ID_PODDON";
                ItemsGrid.SearchText = SearchText;
                ItemsGrid.SetColumns(columns);

                ItemsGrid.Menu = new Dictionary<string, DataGridContextMenuItem>
                {
                    {
                        "2",
                        new DataGridContextMenuItem()
                        {
                            Header = "Не утилизировать",
                            Tag = "access_mode_full_access",
                            Action = () => { SetStatus(2); }
                        }
                    },
                    {
                        "3",
                        new DataGridContextMenuItem()
                        {
                            Header = "Утилизировать",
                            Tag = "access_mode_full_access",
                            Action = () => { SetStatus(3); }
                        }
                    },
                };

                ItemsGrid.OnSelectItem = OnSelectItem;

                //данные грида
                ItemsGrid.OnLoadItems = ItemsGridLoadItems;

                ItemsGrid.Init();
                ItemsGrid.Run();
            }
        }

        private void OnSelectItem(Dictionary<string, string> selectedItem)
        {
            SelectedItem = selectedItem;
            if (SelectedItem != null)
            {
                // DECODE(qpt.disposal_status, 1, 'На рассмотрение', 2, 'Не утилизировать', 3, 'Утилизировать') status -- Статус
                int disposal_status = SelectedItem.CheckGet("DISPOSAL_STATUS").ToInt();
                foreach (string key in ItemsGrid.Menu.Keys)
                {
                    bool visible = false;
                    if (key == "2")
                    {
                        if (disposal_status == 1 || disposal_status == 3)
                        {
                            visible = true;
                        }
                    }
                    else if (key == "3")
                    {
                        if (disposal_status == 1 || disposal_status == 2)
                        {
                            visible = true;
                        }
                    }

                    ItemsGrid.Menu[key].Visible = visible;
                }
            }

            ProcessPermissions();
        }

        /// <summary>
        /// Установка статуса поддона
        /// 1, 'На рассмотрение', 2, 'Не утилизировать', 3, 'Утилизировать'
        /// </summary>
        /// <param name="status"></param>
        private async void SetStatus(int status)
        {
            if(SelectedItem!=null)
            {
                int poddonId = SelectedItem.CheckGet("ID_PODDON").ToInt();

                if(poddonId > 0)
                {
                    var dlg = new DialogWindow(
                        (status == 2 ? "Вы действительно хотите не утилизировать паллет:" : "Вы действительно хотите утилизировать паллет:") +
                        Environment.NewLine +
                        SelectedItem["NAME"] + " " +
                        poddonId.ToString() + " ?"
                        , "Вопрос", "", DialogWindowButtons.YesNo);

                    if (dlg.ShowDialog() == true)
                    {
                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Stock");
                        q.Request.SetParam("Object", "Pallet");
                        q.Request.SetParam("Action", "UpdateDisposalStatus");

                        q.Request.SetParams(new Dictionary<string, string>()
                        {
                            { "ID_PODDON", poddonId.ToString() },
                            { "DISPOSAL_STATUS", status.ToString() },
                        }
                        );

                        q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                        q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                        await Task.Run(() =>
                        {
                            q.DoQuery();
                        });

                        if (q.Answer.Status == 0)
                        {
                            ItemsGridLoadItems();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private async void ItemsGridLoadItems()
        {
            RefreshButton.Style = (Style)RefreshButton.TryFindResource("Button");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Pallet");
            q.Request.SetParam("Action", "ListForDisposing");

            q.Request.SetParam("FACTORY_ID", FactorySelectBox.SelectedItem.Key);

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
        }

        public async void ExportExcel()
        {
            if (ItemsGrid != null)
            {
                if (ItemsGrid.Items.Count > 0)
                {
                    var eg = new ExcelGrid();
                    var cols = ItemsGrid.Columns;
                    eg.SetColumnsFromGrid(cols);
                    eg.Items = ItemsGrid.Items;
                    await Task.Run(() =>
                    {
                        eg.Make();
                    });
                }
            }
        }

        private void ProcessMessages(ItemMessage obj)
        {
        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel("[erp]pallet_dispose");
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

            if (ItemsGrid != null && ItemsGrid.Menu != null && ItemsGrid.Menu.Count > 0)
            {
                foreach (var manuItem in ItemsGrid.Menu)
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

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            ExportExcel();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            ItemsGridLoadItems();
        }

        private void FactorySelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RefreshButton.Style = (Style)RefreshButton.TryFindResource("FButtonPrimary");
        }
    }
}
