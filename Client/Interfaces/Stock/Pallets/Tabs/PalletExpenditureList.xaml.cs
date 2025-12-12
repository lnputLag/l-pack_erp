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
    /// Вкладка расход поддонов на складе
    /// </summary>
    public partial class PalletExpenditureList : UserControl
    {
        public PalletExpenditureList()
        {
            InitializeComponent();

            FactIdSelectBox.SelectedItemChanged += (DependencyObject d, DependencyPropertyChangedEventArgs e) => {
                GridExp.LoadItems();
            };

            PalletsExpDS = new ListDataSet();

            //регистрация обработчика клавиатуры
            PreviewKeyDown += ProcessKeyboard;

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            SetDefaults();
            InitGridExp();
            InitGridExpItems();
            InitGridProducts();

            ProcessPermissions();
        }

        /// <summary>
        /// Данные грида накладных расхода поддонов
        /// </summary>
        ListDataSet PalletsExpDS { get; set; }

        /// <summary>
        /// Выбранная строка в гриде накладных расхода поддонов
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// Выбранная строка в гриде cписка поддонов
        /// </summary>
        Dictionary<string, string> SelectedItemItem { get; set; }

        /// <summary>
        /// обработчик системы навигации по URL
        /// </summary>
        public void ProcessNavigation()
        {

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
                    GridExp.LoadItems();
                    e.Handled = true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

                case Key.Home:
                    GridExp.SetSelectToFirstRow();
                    e.Handled = true;
                    break;

                case Key.End:
                    GridExp.SetSelectToLastRow();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// Деструктор. Остановка вспомогательных процессов при закрытии вкладки
        /// </summary>
        public void Destroy()
        {
            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры гридов
            GridExp.Destruct();
            GridExpItems.Destruct();
            GridProducts.Destruct();
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
            DateFrom.Text = DateTime.Now.AddDays(-7).ToString("dd.MM.yyyy");
            DateTo.Text = DateTime.Now.ToString("dd.MM.yyyy");

            SourceTypes.Items = new Dictionary<string, string>() {
                { "0", "Все типы" },
                { "1", "Возвратные" },
                { "2", "Корректировки" },
            };
            SourceTypes.SelectedItem = SourceTypes.Items.First();
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="obj">сообщение</param>
        private void ProcessMessages(ItemMessage obj)
        {
            if (obj.ReceiverGroup.IndexOf("Stock") > -1)
            {
                if (obj.ReceiverName.IndexOf("PalletExpenditureList") > -1)
                {
                    switch (obj.Action)
                    {
                        case "Refresh":
                            GridExp.LoadItems();
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Инициализация таблицы расходных накладных
        /// </summary>
        private void InitGridExp()
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
                    Header="ИД накладной",
                    Path="ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header="Дата создания",
                    Path="CREATED_DTTM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    MinWidth=110,
                    MaxWidth=110,
                },
                new DataGridHelperColumn
                {
                    Header="Тип",
                    Path="PLEX_TYPE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=30,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание",
                    Path="NOTE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=30,
                    MaxWidth=150,
                },
                new DataGridHelperColumn
                {
                    Header="Накладная",
                    Path="INVOICE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=120,
                    MaxWidth=400,
                },
                new DataGridHelperColumn
                {
                    Header="Отгрузка",
                    Path="SHIPMENT",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=120,
                    MaxWidth=800,
                },
                new DataGridHelperColumn
                {
                    Header="Сотрудник",
                    Path="EMPLOYEE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="Ид накладной отгрузки",
                    Path="NSTHET",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Не проведено",
                    Path="NON_RECORD_FLAG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            GridExp.SetColumns(columns);
            // раскраска строк
            GridExp.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                // определение цветов фона строк
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        // есть не проведённые поддоны
                        if (row["NON_RECORD_FLAG"].ToInt() == 1)
                        {
                            color = HColor.Blue;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }
                        return result;
                    }
                },
            };
            GridExp.SearchText = SearchText;

            GridExp.Init();

            //данные грида
            GridExp.OnLoadItems = LoadItemsExp;
            GridExp.OnFilterItems = FilterItemsExp;
            GridExp.Run();

            //при выборе строки в гриде, обновляются актуальные действия для записи
            GridExp.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdateExpActions(selectedItem);
                }
            };

            //фокус ввода           
            GridExp.Focus();
        }

        /// <summary>
        /// Инициализация таблицы поддонов
        /// </summary>
        private void InitGridExpItems()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Поддон",
                    Path="NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=400,
                },
                new DataGridHelperColumn
                {
                    Header="Количество, шт",
                    Path="QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=150,
                    Totals=(List<Dictionary<string,string>> rows) =>
                    {
                        var result=0;
                        if(rows != null)
                        {
                            foreach(Dictionary<string,string> row in rows)
                            {
                                result += row.CheckGet("QTY").ToInt();
                            }
                        }
                        return result;
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Цена, р",
                    Path="PRICE",
                    Format="N2",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Double,
                    MinWidth=40,
                    MaxWidth=150,
                },
                new DataGridHelperColumn
                {
                    Header="Сумма, р",
                    Path="TOTAL",
                    Format="N2",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Double,
                    MinWidth=40,
                    MaxWidth=150,
                    Totals=(List<Dictionary<string,string>> rows) =>
                    {
                        double result=0;
                        if(rows != null)
                        {
                            foreach(Dictionary<string,string> row in rows)
                            {
                                result += row.CheckGet("TOTAL").ToDouble();
                            }
                        }
                        return result;
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Проведено",
                    Path="RECORD_FLAG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth=60,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="Ид поддона",
                    Path="ID_PAL",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Ид поддона в приходе",
                    Path="PLEI_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            GridExpItems.SetColumns(columns);
            // раскраска строк
            GridExpItems.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
            {
                // определение цветов фона строк
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        // есть не проведённые поддоны
                        if (row["RECORD_FLAG"].ToInt() == 0)
                        {
                            color = HColor.Blue;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }
                        return result;
                    }
                },
            };
            GridExpItems.AutoUpdateInterval = 0;
            GridExpItems.Init();

            // контекстное меню
            GridExpItems.Menu = new Dictionary<string, DataGridContextMenuItem>()
            {
                {
                    "SetRecordFlag",
                    new DataGridContextMenuItem()
                    {
                        Header="Провести",
                        Tag = "access_mode_full_access",
                        Action=() =>
                        {
                            UpdateRecordFlag(1);
                        }
                    }
                },
                {
                    "RemoveRecordFlag",
                    new DataGridContextMenuItem()
                    {
                        Header="Отменить проведение",
                        Tag = "access_mode_full_access",
                        Action=() =>
                        {
                            UpdateRecordFlag(0);
                        }
                    }
                },
            };

            //при выборе строки в гриде, обновляются актуальные действия для записи
            GridExpItems.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdateItemActions(selectedItem);
                }
            };
        }

        /// <summary>
        /// Инициализация таблицы поддонов с продукцией
        /// </summary>
        private void InitGridProducts()
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
                    Header="Дата время отгрузки",
                    Path="TM",
                    Format="dd.MM.yyyy HH:mm",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    MinWidth=100,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="Поддон",
                    Path="NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=70,
                    MaxWidth=120,
                },
                new DataGridHelperColumn
                {
                    Header="Количество, шт",
                    Path="QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=120,
                    Totals=(List<Dictionary<string,string>> rows) =>
                    {
                        var result=0;
                        if(rows != null)
                        {
                            foreach(Dictionary<string,string> row in rows)
                            {
                                result += row.CheckGet("QTY").ToInt();
                            }
                        }
                        return result;
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="ARTIKUL",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=200,
                },
            };
            GridProducts.SetColumns(columns);
            GridProducts.AutoUpdateInterval = 0;
            GridProducts.Init();
        }

        /// <summary>
        /// Загрузка данных в таблице расходных накладных
        /// </summary>
        private async void LoadItemsExp()
        {
            bool resume = true;
            GridToolbar.IsEnabled = false;
            GridExp.ShowSplash();

            if (resume)
            {
                var df = DateFrom.Text.ToDateTime();
                var dt = DateTo.Text.ToDateTime();
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
                q.Request.SetParam("Action", "ListExpenditure");
                q.Request.SetParam("FACT_ID", FactIdSelectBox.SelectedItem.Key);
                q.Request.SetParam("DateFrom", DateFrom.Text);
                q.Request.SetParam("DateTo", DateTo.Text);

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
                        PalletsExpDS = ListDataSet.Create(result, "PalletsExp");
                        GridExpItems.UpdateItems(new ListDataSet());
                        GridProducts.UpdateItems(new ListDataSet());
                        GridExp.UpdateItems(PalletsExpDS);
                    }
                }
            }

            GridExp.HideSplash();
            GridToolbar.IsEnabled = true;
        }

        /// <summary>
        /// Фильтрация содержимого таблицы расходных накладных
        /// </summary>
        private void FilterItemsExp()
        {
            if (GridExp.GridItems != null)
            {
                if (GridExp.GridItems.Count > 0)
                {
                    var items = new List<Dictionary<string, string>>();
                    bool isSelected = false;
                    int selectedId = 0;
                    if (SelectedItem != null)
                    {
                        if (SelectedItem.ContainsKey("ID"))
                        {
                            selectedId = SelectedItem["ID"].ToInt();
                        }
                    }
                    foreach (var row in GridExp.GridItems)
                    {
                        switch (SourceTypes.SelectedItem.Key)
                        {
                            case "1":
                                if (row["RETURNABLE_FLAG"].ToInt() == 1)
                                {
                                    items.Add(row);
                                }
                                break;

                            case "2":
                                if (row["NSTHET"] == null)
                                {
                                    items.Add(row);
                                }
                                break;

                            default:
                                items.Add(row);
                                break;
                        }

                    }
                    GridExp.GridItems = items;

                    // проверим, осталась ли выбранная строка в отфильтрованном содержимом
                    if (items.Count > 0)
                    {
                        foreach(var item in items)
                        {
                            if (item.ContainsKey("ID"))
                            {
                                if (item["ID"].ToInt() == selectedId)
                                {
                                    isSelected = true;
                                    break;
                                }
                            }
                        }
                    }

                    // если выделенной строки нет, очистим подчинённые таблицы
                    if (!isSelected)
                    {
                        GridExpItems.ClearItems();
                        GridProducts.ClearItems();
                    }
                }
            }
        }

        /// <summary>
        /// Загрузка данных в таблицу поддонов
        /// </summary>
        private async void LoadItemsExpItems()
        {
            GridExpItems.ShowSplash();

            int selId = 0;
            if (SelectedItemItem != null)
            {
                selId = SelectedItemItem["PLEI_ID"].ToInt();
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Pallet");
            q.Request.SetParam("Action", "ListExpenditureItems");

            q.Request.SetParam("plex_id", SelectedItem["ID"]);

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
                    var PalletsExpItemsDS = ListDataSet.Create(result, "List");
                    GridExpItems.UpdateItems(PalletsExpItemsDS);
                }
            }

            GridExpItems.HideSplash();
            GridExpItems.SelectRowByKey(selId, "PLEI_ID");
        }

        /// <summary>
        /// Загрузка данных в таблицу поддонов с продукцией
        /// </summary>
        private async void LoadItemsProducts()
        {
            GridProducts.ShowSplash();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Pallet");
            q.Request.SetParam("Action", "ListShipped");

            q.Request.SetParam("NSTHET", SelectedItem["NSTHET"].ToInt().ToString());
            q.Request.SetParam("IdPal", SelectedItemItem["ID_PAL"].ToInt().ToString());

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
                    var PalletsProductsDS = ListDataSet.Create(result, "ListShipped");
                    GridProducts.UpdateItems(PalletsProductsDS);
                }
            }

            GridProducts.HideSplash();
            GridProducts.SetSelectToFirstRow();
        }

        /// <summary>
        /// Обновление действий с таблицей расходных накладных
        /// </summary>
        /// <param name="selectedItem"></param>
        private void UpdateExpActions(Dictionary<string, string> selectedItem)
        {
            SelectedItem = selectedItem;

            DeleteButton.IsEnabled = (SelectedItem["NSTHET"] == null);
            EditButton.IsEnabled = (SelectedItem["NSTHET"] == null) || (SelectedItem["NON_RECORD_FLAG"].ToInt() == 1);

            LoadItemsExpItems();

            ProcessPermissions();
        }

        /// <summary>
        /// Обновление операций с табицей поддонов
        /// </summary>
        /// <param name="selectedItem"></param>
        private void UpdateItemActions(Dictionary<string, string> selectedItem)
        {
            SelectedItemItem = selectedItem;

            if (SelectedItemItem.CheckGet("RECORD_FLAG").ToInt() == 1)
            {
                GridExpItems.Menu["SetRecordFlag"].Visible = false;

                GridExpItems.Menu["RemoveRecordFlag"].Visible = true;
            }
            else
            {
                GridExpItems.Menu["SetRecordFlag"].Visible = true;

                GridExpItems.Menu["RemoveRecordFlag"].Visible = false;
            }

            LoadItemsProducts();

            ProcessPermissions();
        }

        /// <summary>
        /// Удаление накладной расхода поддонов
        /// </summary>
        private async void DeletePalletExpenditure()
        {
            GridExp.ShowSplash();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Pallet");
            q.Request.SetParam("Action", "DeleteExpenditure");
            q.Request.SetParam("ID", SelectedItem["ID"]);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    GridExp.LoadItems();
                }
            }
            else
            {
                q.ProcessError();
            }

            GridExp.HideSplash();

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

            if (GridExp != null && GridExp.Menu != null && GridExp.Menu.Count > 0)
            {
                foreach (var manuItem in GridExp.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }

            if (GridExpItems != null && GridExpItems.Menu != null && GridExpItems.Menu.Count > 0)
            {
                foreach (var manuItem in GridExpItems.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }

            if (GridProducts != null && GridProducts.Menu != null && GridProducts.Menu.Count > 0)
            {
                foreach (var manuItem in GridProducts.Menu)
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
        /// Вызов справки
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/warehouse/pallets#block4");
        }

        /// <summary>
        /// Обновление признака проведения поддона
        /// </summary>
        /// <param name="val">Значение признака</param>
        private async void UpdateRecordFlag(int val)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Pallet");
            q.Request.SetParam("Action", "UpdateExpenditureItemRecordFlag");
            q.Request.SetParam("PLEI_ID", SelectedItemItem["PLEI_ID"]);
            q.Request.SetParam("RECORD_FLAG", val.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    GridExp.LoadItems();
                    GridExpItems.LoadItems();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Обработчик нажатия на кнопку Показать
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            GridExp.LoadItems();
        }

        /// <summary>
        /// Обработчик установки/снятия флажка отображения только возвратных поддонов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReturnableCheckBox_Click(object sender, RoutedEventArgs e)
        {
            GridExp.UpdateItems();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку добавления накладной расхода поддонов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            bool shipped = SelectedItem["NSTHET"] != null;
            var expenditureForm = new PalletExpenditure();
            expenditureForm.ReturnTabName = "Pallets_Expedinture";
            expenditureForm.Edit(0, shipped);
        }

        /// <summary>
        /// Обработчик нажатия на кнопку редактирования накладной
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem.CheckGet("ID").ToInt() > 0)
            {
                bool shipped = SelectedItem["NSTHET"] != null;
                var expenditureForm = new PalletExpenditure();
                expenditureForm.ReturnTabName = "Pallets_Expedinture";
                expenditureForm.Edit(SelectedItem["ID"].ToInt(), shipped);
            }
        }

        /// <summary>
        /// Обработчик нажатия на кнопку удаления
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem.CheckGet("ID").ToInt() > 0)
            {
                var dw = new DialogWindow("Удалить накладную расхода поддонов?", "Удаление накладной", "", DialogWindowButtons.NoYes);
                if (dw.ShowDialog() == true)
                {
                    DeletePalletExpenditure();
                }
            }
            else
            {
                var dw = new DialogWindow("Не выбрана накладная", "Удаление накладной");
                dw.ShowDialog();
            }
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

        private void G2InitButton_Click(object sender,RoutedEventArgs e)
        {
            GridExpItems.Init();
        }

        private void G1InitButton_Click(object sender,RoutedEventArgs e)
        {
            GridExp.Init();
        }

        private void SourceTypes_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GridExp.UpdateItems();
        }
    }
}
