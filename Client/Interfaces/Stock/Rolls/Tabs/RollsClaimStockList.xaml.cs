using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Вкладка претензии по рулонам от склада рулонов
    /// </summary>
    /// <author>Грешных Н.И.</author>
    /// <version>1</version>
    /// <changed>2025-03-28</changed>
    public partial class RollsClaimStockList : UserControl
    {
        /// <summary>
        /// Инициализация
        /// </summary>
        public RollsClaimStockList()
        {
            InitializeComponent();

            //регистрация обработчика клавиатуры
            PreviewKeyDown += ProcessKeyboard;

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            SetDefaults();
            InitGridRolls();
            ProcessPermissions();
        }

        /// <summary>
        /// Данные для таблицы списка рулонов
        /// </summary>
        public ListDataSet RollsDS { get; set; }

        /// <summary>
        /// Выбранная строка в таблице рулонов
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
            var mode = Central.Navigator.GetRoleLevel("[erp]claim_stock_rolls");
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
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            var firstDay = new DateTime(DateTime.Now.Year, 1, 1);
            DateOrderFrom.Text = firstDay.ToString("dd.MM.yyyy");
            DateOrderTo.Text = DateTime.Now.AddDays(1).ToString("dd.MM.yyyy");
            SearchText.Text = "";
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
                ReceiverGroup = "Stock",
                ReceiverName = "",
                SenderName = "RollsList",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры гридов
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
                if (obj.ReceiverName.IndexOf("RollsList") > -1)
                {
                    switch (obj.Action)
                    {
                        case "Refresh":
                            Grid.LoadItems();
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Инициализация таблицы списка рулонов
        /// </summary>
        private void InitGridRolls()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=50,
                },
                new DataGridHelperColumn
                {
                    Header = "Дата ПЗ",
                    Path = "DATA_FROM",
                    Description = "",
                    ColumnType = ColumnTypeRef.String,
                    MinWidth=80,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header = "Дата прихода",
                    Path = "DATA_PLACED",
                    Description = "",
                    ColumnType = ColumnTypeRef.String,
//                    Width2 = 15,
                    MinWidth=120,
                    MaxWidth=120,
                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=200,
                    MaxWidth=240,
                },
                new DataGridHelperColumn
                {
                    Header="Внеш. № рулона",
                    Path="NAME_ROLL",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=120,
                    MaxWidth=130,
                },
                new DataGridHelperColumn
                {
                    Header="Внут. № рулона",
                    Path="NUM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=110,
                    MaxWidth=120,
                },
                new DataGridHelperColumn
                {
                    Header="Вес",
                    Path="KOL",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=60,
                    MaxWidth=60,
                },
                new DataGridHelperColumn
                {
                    Header = "Дата претензии",
                    Path = "CLAIM_DT",
                    Description = "",
                    ColumnType = ColumnTypeRef.String,
                    MinWidth=80,
                    MaxWidth=80,
//                    Width2 = 15,
                    /*
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
                     
                     */

                },
                new DataGridHelperColumn
                {
                    Header = "Дата корректировки",
                    Path = "ADJUSTMENT_DT",
                    Description = "",
                    ColumnType = ColumnTypeRef.String,
                    MinWidth=80,
                    MaxWidth=80,
  //                  Width2 = 15,
                },
                new DataGridHelperColumn
                {
                    Header="Накладная прихода",
                    Path="NNAKL",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=100,
                    MaxWidth=110,
                },
                new DataGridHelperColumn
                {
                    Header="ИД прихода",
                    Path="IDP",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=100,
                    MaxWidth=110,
                },
                new DataGridHelperColumn
                {
                    Header="Накладная расхода",
                    Path="NSTHET",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=100,
                    MaxWidth=110,
                },
                new DataGridHelperColumn
                {
                    Header="ИД расхода",
                    Path="IDR",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=100,
                    MaxWidth=100,
                },
            };

            Grid.SetColumns(columns);
            Grid.PrimaryKey = "IDR";
            Grid.UseSorting = true;
            Grid.AutoUpdateInterval = 60;
            Grid.UseRowHeader = true;
            Grid.SelectItemMode = 0;

            Grid.SearchText = SearchText;
            Grid.Init();

            // контекстное меню
            Grid.Menu = new Dictionary<string, DataGridContextMenuItem>()
            {
                {
                    "ChangeLocation",
                    new DataGridContextMenuItem()
                    {
                        Header="Изменить",
                        Tag = "access_mode_full_access",
                        Action=() =>
                        {
                            ChangeClaim();
                        }
                    }
                },

            };

            //данные грида
            Grid.OnLoadItems = LoadItemsRolls;
            Grid.Run();

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdateActions(selectedItem);
                    CancelButton.IsEnabled = false;
                    
                    var claim_dt = selectedItem.CheckGet("CLAIM_DT").ToString();
                    var adjustment_dt = selectedItem.CheckGet("ADJUSTMENT_DT").ToString();
                    
                    if (!adjustment_dt.IsNullOrEmpty())
                    {
                        CancelButton.IsEnabled = false;
                    }
                    else if (!claim_dt.IsNullOrEmpty())
                    {
                        CancelButton.IsEnabled = true;
                    }
                }
            };

            //двойной клик на строке откроет форму редактирования
            Grid.OnDblClick = (Dictionary<string, string> selectedItem) =>
            {
                ChangeClaim();
            };

            //фокус ввода           
            Grid.Focus();
        }

        /// <summary>
        /// Загрузка данных в таблицу списка рулонов
        /// </summary>
        private async void LoadItemsRolls()
        {
            bool resume = true;
            GridToolbar.IsEnabled = false;
            Grid.ShowSplash();

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
                q.Request.SetParam("Object", "Rolls");
                q.Request.SetParam("Action", "RollList");

                q.Request.SetParam("DATE_FROM", DateOrderFrom.Text);
                q.Request.SetParam("DATE_TO", DateOrderTo.Text);

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
                        RollsDS = ListDataSet.Create(result, "ITEMS");
                        Grid.UpdateItems(RollsDS);

                        var adjustment_dt = RollsDS.Items[0].CheckGet("ADJUSTMENT_DT").ToString();
                        if (!adjustment_dt.IsNullOrEmpty())
                        {
                            CancelButton.IsEnabled = false;
                        }
                        else
                        {
                            CancelButton.IsEnabled = true;
                        }

                    }
                }
            }

            Grid.HideSplash();
            GridToolbar.IsEnabled = true;
        }


        /// <summary>
        /// Обновление действий для выбранной строки в таблице списка рулонов
        /// </summary>
        /// <param name="selectedItem"></param>
        private void UpdateActions(Dictionary<string, string> selectedItem)
        {
            SelectedItem = selectedItem;
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
        /// экспорт записей грида в Excel
        /// </summary>
        private async void ExportToExcel()
        {
            if (Grid != null)
            {
                if (Grid.Items.Count > 0)
                {
                    var eg = new ExcelGrid();
                    var cols = Grid.Columns;
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
        private void ExportButton_Click(object sender, System.Windows.RoutedEventArgs e)
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

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeClaim();
        }

        /// <summary>
        /// Вызов фрейма изменения даты претензии и даты корректироваки 
        /// </summary>
        private void ChangeClaim()
        {
            if (Central.Navigator.GetRoleLevel("[erp]claim_stock_rolls") >= Role.AccessMode.FullAccess)
            {
                if (SelectedItem != null)
                {
                    if (SelectedItem.ContainsKey("IDR"))
                    {
                        int idr = SelectedItem["IDR"].ToInt();
                        if (idr > 0)
                        {
                            var rollsForm = new RollsEdit();
                            rollsForm.ReturnTabName = "RollList";
                            rollsForm.Edit(idr);
                        }
                    }
                }
            }

        }

        /// <summary>
        /// нажали отмена претензии
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var dw = new DialogWindow($"Вы действительно хотите отменить претензию [{SelectedItem.CheckGet("NAME_ROLL")}]?", "Отменина претензии", "Подтверждение отмениы претензии", DialogWindowButtons.NoYes);
            if (dw.ShowDialog() == true)
            {
                CancelRolls();
            }
        }

        /// <summary>
        /// отмена претензии
        /// </summary>
        private void CancelRolls()
        {
            if (Central.Navigator.GetRoleLevel("[erp]claim_stock_rolls") >= Role.AccessMode.FullAccess)
            {
                if (SelectedItem != null)
                {
                    {
                        int idr = SelectedItem["IDR"].ToInt();
                        if (idr > 0)
                        {
                            bool resume = true;
                            GridToolbar.IsEnabled = false;
                            Grid.ShowSplash();

                            if (resume)
                            {
                                var q = new LPackClientQuery();
                                q.Request.SetParam("Module", "Stock");
                                q.Request.SetParam("Object", "Rolls");
                                q.Request.SetParam("Action", "SetNnakl");

                                q.Request.SetParam("IDR", SelectedItem["IDR"].ToInt().ToString());
                                q.Request.SetParam("NNAKL", SelectedItem["NNAKL"].ToInt().ToString());
                                q.Request.SetParam("NSTHET", SelectedItem["NSTHET"].ToInt().ToString());

                                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                                q.DoQuery();

                                if (q.Answer.Status == 0)
                                {
                                    Grid.LoadItems();
                                }
                            }

                            Grid.HideSplash();
                            GridToolbar.IsEnabled = true;
                        }
                    }
                }
            }

        }



    }
}
