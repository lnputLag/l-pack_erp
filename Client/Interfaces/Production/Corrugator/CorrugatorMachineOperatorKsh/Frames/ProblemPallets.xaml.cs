using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Client.Interfaces.Production.Corrugator.CorrugatorMachineOperatorKsh.Frames;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.Corrugator.CorrugatorMachineOperatorKsh
{
    /// <summary>
    /// редактирование простоя
    /// </summary>
    /// <author>zelenskiy_sv</author>
    public partial class ProblemPallets : UserControl
    {
        public ProblemPallets()
        {
            FrameName = "ProblemPallets";

            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);
            
            Init();
        }

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// идентификатор записи, с которой работает форма
        /// (primary key записи таблицы)
        /// </summary>
        public int Id { get; set; }


        public Dictionary<string, string> SelectedBlockedItem { get; set; }
        public Dictionary<string, string> SelectedInconsistencyItem { get; set; }
        public Dictionary<string, string> SelectedScannedItem { get; set; }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void Init()
        {
            BlockPalletGridInit();
            InconsistencyGridInit();
            ScannedGridInit();
            
            BlockPalletGrid.LayoutTransform = new ScaleTransform(1.2, 1.2);
            InconsistencyGrid.LayoutTransform = new ScaleTransform(1.2, 1.2);
            ScannedGrid.LayoutTransform = new ScaleTransform(1.2, 1.2);
        }

        /// <summary>
        /// инициализация грида (заблокированные поддоны)
        /// </summary>
        public void BlockPalletGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                     new DataGridHelperColumn
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=27,
                    },
                     new DataGridHelperColumn
                    {
                        Header="Дата создания",
                        Path="BLOCKED_DTTM",
                        ColumnType=ColumnTypeRef.String,
                        Width=120,
                    },
                     new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=250,
                    },
                     new DataGridHelperColumn
                    {
                        Header="Количество",
                        Path="KOL",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=40,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№",
                        Path="NUM",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=30,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ ПЗ",
                        Path="NUM_PZ",
                        Doc="Номер ПЗ",
                        ColumnType=ColumnTypeRef.String,
                        Width=80,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата окончания",
                        Path="DTEND",
                        Doc="Дата окончания",
                        ColumnType=ColumnTypeRef.String,
                        Width=120,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Причина",
                        Path="REASON",
                        Doc="Причина",
                        ColumnType=ColumnTypeRef.String,
                        Width=150,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Начать до",
                        Path="START_BEFORE",
                        Doc="Начать до",
                        ColumnType=ColumnTypeRef.String,
                        Width=120,
                    },
                    new DataGridHelperColumn
                    {
                        Header="_",
                        Path="ID_PODDON",
                        Doc="ИД поддона",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="_",
                        Path="MASTER_FLAG",
                        Doc="",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                };
                BlockPalletGrid.SetColumns(columns);

                // Раскраска строк
                BlockPalletGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()

                    {
                    // Цвета фона строк
                    {
                        DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";
                            var currentStatus = row.CheckGet("MASTER_FLAG").ToInt();

                            if (currentStatus == 1)
                            {
                                color = HColor.Green;
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result = color.ToBrush();
                            }

                            return result;
                        }
                    },
                };

                BlockPalletGrid.PrimaryKey = "_ROWNUMBER";
                BlockPalletGrid.UseRowHeader = false;
                BlockPalletGrid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                BlockPalletGrid.OnSelectItem = selectedItem =>
                {
                    //if (selectedItem.Count > 0)
                    //{
                        SelectedBlockedItem = selectedItem;
                        //IdleReasonDetailGrid.LoadItems();
                    //}
                };

                //данные грида
                BlockPalletGrid.OnLoadItems = BlockPalletGridLoadItems;
                BlockPalletGrid.OnFilterItems = BlockPalletGridFilterItems;

                BlockPalletGrid.Run();
            }
        }

        /// <summary>
        /// инициализация грида (несоответствия)
        /// </summary>
        public void InconsistencyGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header = "*",
                        Path = "_SELECTED",
                        ColumnType = ColumnTypeRef.Boolean,
                        Width = 25,
                        Editable = true,
                        OnClickAction = (el, row) => true
                    },
                     new DataGridHelperColumn
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=25,
                    },
                     new DataGridHelperColumn
                    {
                        Header="ПЗ",
                        Path="ID_PZ",
                        Doc="ПЗ",
                        ColumnType=ColumnTypeRef.String,
                        Width=80,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ ПЗ",
                        Path="NUM",
                        Doc="NUM",
                        ColumnType=ColumnTypeRef.String,
                        Width = 80
                    },
                     new DataGridHelperColumn
                    {
                        Header="Товар",
                        Path="NAME",
                        Doc="Товар",
                        ColumnType=ColumnTypeRef.String,
                        Width=350,
                    },
                     new DataGridHelperColumn
                    {
                        Header="Блок",
                        Path="BLOCKED_QTY",
                        Doc="Блок",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=40,
                    },
                     new DataGridHelperColumn
                    {
                        Header="По ПЗ",
                        Path="KOL",
                        Doc="По ПЗ",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=50,
                    },
                     new DataGridHelperColumn
                    {
                        Header="BHS",
                        Path="QTY_BHS",
                        Doc="BHS",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=50,
                    },
                     new DataGridHelperColumn
                    {
                        Header="Принято",
                        Path="QTY",
                        Doc="Принято",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="_",
                        Path="ID2",
                        Doc="ID2",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                };
                InconsistencyGrid.SetColumns(columns);

                InconsistencyGrid.UseRowHeader = false;
                InconsistencyGrid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                InconsistencyGrid.OnSelectItem = selectedItem =>
                {
                    SelectedInconsistencyItem = selectedItem;
                };

                //данные грида
                InconsistencyGrid.OnLoadItems = InconsistencyGridLoadItems;

                InconsistencyGrid.Run();
            }
        }

        /// <summary>
        /// инициализация грида (отсканированные поддоны)
        /// </summary>
        public void ScannedGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                     new DataGridHelperColumn
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=25,
                    },
                     new DataGridHelperColumn
                    {
                        Header="Поддон",
                        Path="PALLET",
                        ColumnType=ColumnTypeRef.String,
                        Width=60,
                    },
                     new DataGridHelperColumn
                    {
                        Header="Количество",
                        Path="KOL",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=40,
                    },
                     new DataGridHelperColumn
                    {
                        Header="Название",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width=300,
                    },
                     new DataGridHelperColumn
                    {
                        Header="Дата сканирования",
                        Path="DT",
                        ColumnType=ColumnTypeRef.String,
                        Width=120,
                    },
                     new DataGridHelperColumn
                    {
                        Header="Стекер",
                        Path="LOCATION",
                        ColumnType=ColumnTypeRef.String,
                        Width=60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="_",
                        Path="ID_PODDON",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="_",
                        Path="IDP",
                        Doc="IDP",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                };
                ScannedGrid.SetColumns(columns);

                ScannedGrid.UseRowHeader = false;
                ScannedGrid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                ScannedGrid.OnSelectItem = selectedItem =>
                {
                    SelectedScannedItem = selectedItem;
                };

                //данные грида
                ScannedGrid.OnLoadItems = LoadPalletList;

                ScannedGrid.Run();
            }
        }
        
        
        /// <summary>
        /// Загрузка списка поддонов в буф 0
        /// </summary>
        private async void LoadPalletList()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "CorrugatorMachineOperatorKsh");
            q.Request.SetParam("Action", "ListPaddons");

            await Task.Run(() => q.DoQuery());

            if (q.Answer.Status == 0)
            {
                var list = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                if (list != null)
                {
                    var ds = ListDataSet.Create(list, "ITEMS");
                    ScannedGrid.UpdateItems(ds);
                }
            }
        }

        /// <summary>
        /// получение заблокированных поддонов
        /// </summary>
        public async void BlockPalletGridLoadItems()
        {
            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_ST", CorrugatorMachineOperator.SelectedMachineId.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "ProblemPallets");
            q.Request.SetParam("Action", "BlockPalletList");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            BlockPalletGridDisableControls();

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
                        var ds = ListDataSet.Create(result, "ITEMS");
                        BlockPalletGrid.UpdateItems(ds);
                    }
                }
            }

            BlockPalletGridEnableControls();
        }

        /// <summary>
        /// получение списка несоответствий поддонов
        /// </summary>
        public async void InconsistencyGridLoadItems()
        {
            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_ST", CorrugatorMachineOperator.SelectedMachineId.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "ProblemPallets");
            q.Request.SetParam("Action", "InconsistencyList");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            InconsistencyGridDisableControls();

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
                        var ds = ListDataSet.Create(result, "ITEMS");
                        InconsistencyGrid.UpdateItems(ds);
                    }
                }
            }

            InconsistencyGridEnableControls();
        }

        /// <summary>
        /// получение списка отсканированных поддонов
        /// </summary>
        public async void ScannedGridLoadItems()
        {
            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_ST", CorrugatorMachineOperator.SelectedMachineId.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "ProblemPallets");
            q.Request.SetParam("Action", "ScannedList");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            ScannedGridDisableControls();

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
                        var ds = ListDataSet.Create(result, "ITEMS");
                        ScannedGrid.UpdateItems(ds);
                    }
                }
            }

            ScannedGridEnableControls();
        }

        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "CorrugatorMachineOperator",
                ReceiverName = "",
                SenderName = "IdleEdit",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
        }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// редактирвоание записи
        /// </summary>
        public void Edit()
        {
            Show();
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
            Central.WM.FrameMode = 1;

            var frameName = GetFrameName();

            Central.WM.Show(frameName, "Проблемные поддоны", true, "add", this);
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            var frameName = GetFrameName();
            Central.WM.Close(frameName);

            //вся работа по утилизации ресурсов происходит в Destroy
            //он будет вызван при закрытии фрейма
        }

        /// <summary>
        /// формирует уникальный идентификатор фрейма
        /// </summary>
        /// <returns></returns>
        public string GetFrameName()
        {
            string result = "";
            result = $"{FrameName}_{Id}";
            return result;
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
            BlockPalletGrid.IsEnabled = false;
            //IdleReasonText.IsEnabled = false;
            InconsistencyGrid.IsEnabled = false;
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
            BlockPalletGrid.IsEnabled = true;
            //IdleReasonText.IsEnabled = true;
            InconsistencyGrid.IsEnabled = true;
        }

        private void CancelButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        private void UnblockButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var reason = SelectedBlockedItem.CheckGet("REASON").ToString();
            if (reason == "Заблокированный")
            {
                UnblockPallet("UnBlockPallet");
            }
            else
            {
                UnblockPallet("RemovePallet");
            }
        }

        private async void UnblockPallet(string action)
        {
            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_PODDON", SelectedBlockedItem.CheckGet("ID_PODDON").ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "ProblemPallets");
            q.Request.SetParam("Action", action);
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            BlockPalletGrid.IsEnabled = false;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            BlockPalletGrid.IsEnabled = true;

            if (q.Answer.Status == 0)
            {
                BlockPalletGrid.LoadItems();
            }
            else
            {
                q.ProcessError();
            }
        }

        private void CallMasterButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var flag = SelectedBlockedItem.CheckGet("MASTER_FLAG").ToInt();
            CallMaster(1 - flag);
        }

        private async void CallMaster(int flag)
        {
            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_PODDON", SelectedBlockedItem.CheckGet("ID_PODDON").ToInt().ToString());
                p.CheckAdd("FLAG", flag.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "CorrugatorMachineOperator");
            q.Request.SetParam("Action", "CallMaster");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            BlockPalletGridDisableControls();

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            BlockPalletGridEnableControls();

            if (q.Answer.Status == 0)
            {
                BlockPalletGrid.LoadItems();
            }
            else if (q.Answer.Status == 145)
            {
                var dw = new DialogWindow(q.Answer.Error.Message, "Заблокированные поддоны");
                dw.ShowDialog();
            }
            else
            {
                q.ProcessError();
            }
        }
        
        private async Task<int> InconsistencyGridUnblockMulty(Dictionary<string, string> item)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "ProblemPallets");
            q.Request.SetParam("Action", "UnBlockInconsistency");
            q.Request.SetParam("ID_PZ", item.CheckGet("ID_PZ"));
            q.Request.SetParam("ID2", item.CheckGet("ID2"));

            await Task.Run(() => q.DoQuery());

            if (q.Answer.Status == 0)
            {
                return 1;
            }

            return 0;
        }

        private async void InconsistencyGridUnblock()
        {
            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_PZ", SelectedInconsistencyItem.CheckGet("ID_PZ").ToString());
                p.CheckAdd("ID2", SelectedInconsistencyItem.CheckGet("ID2").ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "ProblemPallets");
            q.Request.SetParam("Action", "UnBlockInconsistency");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            InconsistencyGridDisableControls();

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                InconsistencyGrid.LoadItems();
            }
            else
            {
                q.ProcessError();
            }

            InconsistencyGridEnableControls();
        }

        private async void UnblockButton2_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var slct = InconsistencyGrid.GetSelectedItems();

            if (slct.Count > 0)
            {
                var count = 0;

                foreach (var item in slct)
                {
                    count += await InconsistencyGridUnblockMulty(item);
                }

                var dialog = new DialogWindow($"Успешно обновлено записей {count}", "Проблемные поддоны");
                dialog.ShowDialog();

                InconsistencyGrid.LoadItems();
            }
            else
            {
                InconsistencyGridUnblock();
            }
        }

        private async void ScannedGridUnblock()
        {
            var d = new DialogWindow($"Вы действительно хотите разблокировать поддон \"{SelectedScannedItem.CheckGet("PALLET")}\"?", "Удаление поддона", "", DialogWindowButtons.YesNo);
            d.ShowDialog();
            if (d.DialogResult == true)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("IDP", SelectedScannedItem.CheckGet("IDP").ToString());
                    p.CheckAdd("QTY", SelectedScannedItem.CheckGet("KOL").ToString());
                    p.CheckAdd("ID_PODDON", SelectedScannedItem.CheckGet("ID_PODDON").ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "ProblemPallets");
                q.Request.SetParam("Action", "UnBlockScanned");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                InconsistencyGridDisableControls();

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                InconsistencyGridEnableControls();

                if (q.Answer.Status == 0)
                {
                    ScannedGrid.LoadItems();
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        private void BlockPalletGridDisableControls()
        {
            BlockPalletGrid.IsEnabled = false;
            UnblockButton.IsEnabled = false;
            CallMasterButton.IsEnabled = false;
        }

        private void BlockPalletGridEnableControls()
        {
            if (BlockPalletGrid.Items?.Count > 0)
            {
                BlockPalletGrid.IsEnabled = true;
                UnblockButton.IsEnabled = true;
                CallMasterButton.IsEnabled = true;
            }
        }

        private void InconsistencyGridDisableControls()
        {
            InconsistencyGrid.IsEnabled = false;
            UnblockButton2.IsEnabled = false;
        }

        private void InconsistencyGridEnableControls()
        {
            if (InconsistencyGrid.Items?.Count > 0)
            {
                InconsistencyGrid.IsEnabled = true;
                UnblockButton2.IsEnabled = true;
            }
        }

        private void ScannedGridDisableControls()
        {
            ScannedGrid.IsEnabled = false;
            // UnblockButton3.IsEnabled = false;
        }

        private void ScannedGridEnableControls()
        {
            if (ScannedGrid.Items?.Count > 0)
            {
                ScannedGrid.IsEnabled = true;
                // UnblockButton3.IsEnabled = true;
            }
        }

        private void UnblockButton3_Click(object sender, RoutedEventArgs e)
        {
            ScannedGridUnblock();
        }

        private void SelectLocked_Click(object sender, RoutedEventArgs e)
        {
            BlockPalletGridDisableControls();
            BlockPalletGrid.UpdateItems();
            BlockPalletGrid.SetSelectToFirstRow();
            BlockPalletGridEnableControls();
        }

        /// <summary>
        /// фильтрация записей 
        /// </summary>
        public void BlockPalletGridFilterItems()
        {
            if (BlockPalletGrid.GridItems != null)
            {
                if (BlockPalletGrid.GridItems.Count > 0)
                {
                    //фильтрация строк
                    {
                        var selectLocked = SelectLocked.IsChecked;
                        var selectUnreceived = SelectUnreceived.IsChecked;

                        var items = new List<Dictionary<string, string>>();
                        foreach (Dictionary<string, string> row in BlockPalletGrid.GridItems)
                        {
                            //bool includeByActivity = true;

                            if (selectLocked == true)
                            {
                                if (row.CheckGet("REASON").ToString() == "Заблокированный")
                                {
                                    items.Add(row);
                                }
                            }

                            if (selectUnreceived == true)
                            {
                                if (row.CheckGet("REASON").ToString() == "Неоприходованный")
                                {
                                    items.Add(row);
                                }
                            }
                        }

                        BlockPalletGrid.GridItems = items;
                    }
                }
            }
        }

        /// <summary>
        /// Списание поддона
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void WriteOfPallet_OnClick(object sender, RoutedEventArgs e)
        {
            var id = ScannedGrid.SelectedItem.CheckGet("ID_PODDON").ToInt();
            var num = ScannedGrid.SelectedItem.CheckGet("PALLET");
            
            var reasonWindow = new WriteOfPaddons();
            reasonWindow.SelectReason(id, num);
            
        }
    }
}
