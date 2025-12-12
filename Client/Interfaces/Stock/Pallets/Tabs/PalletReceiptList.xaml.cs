using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Common.LPackClientRequest;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Вкладка приход поддонов на склад
    /// </summary>
    public partial class PalletReceiptList : UserControl
    {
        public PalletReceiptList()
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
            InitGridPallet();
            InitGridItems();

            ProcessPermissions();
        }

        /// <summary>
        /// Выбранная строка в гриде прихода поддонов
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

            if (GridItems != null && GridItems.Menu != null && GridItems.Menu.Count > 0)
            {
                foreach (var manuItem in GridItems.Menu)
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
            DateFrom.Text = DateTime.Now.AddDays(-7).ToString("dd.MM.yyyy");
            DateTo.Text = DateTime.Now.ToString("dd.MM.yyyy");
            SourceTypes.Items = PalletSourceTypes.ExtendItems();
            SourceTypes.SelectedItem = PalletSourceTypes.ExtendItems().First();
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
            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
            //останавливаем таймеры гридов
            GridItems.Destruct();
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
                if (obj.ReceiverName.IndexOf("PalletReceiptList") > -1)
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
        /// инициализация таблицы накладных прихода поддонов
        /// </summary>
        private void InitGridPallet()
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
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header="Дата создания",
                    Path="CREATED_DTTM",
                    Format="dd.MM.yyyy",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                    MinWidth=62,
                    MaxWidth=62,
                },
                new DataGridHelperColumn
                {
                    Header="Источник",
                    Path="SOURCE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=120,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание",
                    Path="NOTE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=70,
                    MaxWidth=150,
                },
                new DataGridHelperColumn
                {
                    Header="Накладная",
                    Path="RECEIPT",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=80,
                    MaxWidth=900,
                },
                new DataGridHelperColumn
                {
                    Header="Проверка ОЭБ",
                    Path="SECURITY_FLAG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    Editable=PalletPermissions.HasPermission("check_security"),
                    OnClickAction = (row, el) =>
                    {
                        var c = (CheckBox)el;

                        CheckSecurity((bool)c.IsChecked);

                        return null;
                    },


                },
                new DataGridHelperColumn
                {
                    Header="Статус",
                    Path="STATUS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=100,
                    Stylers=new Dictionary<DataGridHelperColumn.StylerTypeRef,DataGridHelperColumn.StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.ForegroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                switch(row.CheckGet("STATUS_ID").ToInt())
                                {
                                    //На приемке
                                    case 0:
                                        color = HColor.BlueFG;
                                        break;

                                    //Приняты
                                    case 1:
                                        color = HColor.GreenFG;
                                        break;

                                    //Не приняты
                                    case 2:
                                        color = HColor.RedFG;
                                        break;
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
                    Header="Документы получены",
                    Path="RECEIVE_DOC_FLAG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    Width=30,
                },
                new DataGridHelperColumn
                {
                    Header="Сотрудник",
                    Path="EMPLOYEE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=80,
                    MaxWidth=100,
                },
                new DataGridHelperColumn
                {
                    Header="Приложены файлы",
                    Path="FILE_IS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth=30,
                    MaxWidth=40,
                },
                new DataGridHelperColumn
                {
                    Header="Код статуса",
                    Path="STATUS_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Код источника",
                    Path="SOURCE_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };

            Grid.SetColumns(columns);
            Grid.SearchText = SearchText;
            Grid.Init();

            // контекстное меню
            Grid.Menu = new Dictionary<string, DataGridContextMenuItem>()
            {
                {
                    "AddFile",
                    new DataGridContextMenuItem()
                    {
                        Header="Прикрепить файлы к приходной накладной",
                        Tag = "access_mode_full_access",
                        Action=() =>
                        {
                            AddFile();
                        }
                    }
                },
                {
                    "SetAcceptedState",
                    new DataGridContextMenuItem()
                    {
                        Header="Принять поддоны",
                        Tag = "access_mode_full_access",
                        Action=() =>
                        {
                            SetStatus(PalletReceiptStatus.Accepted);
                        }
                    }
                },
                {
                    "SetRejectedState",
                    new DataGridContextMenuItem()
                    {
                        Header="Отклонить поддоны",
                        Tag = "access_mode_full_access",
                        Action=() =>
                        {
                            SetStatus(PalletReceiptStatus.Rejected);
                        }
                    }
                },
                {
                    "SetIncomingState",
                    new DataGridContextMenuItem()
                    {
                        Header="Вернуть на приемку",
                        Tag = "access_mode_full_access",
                        Action=() =>
                        {
                            SetStatus(PalletReceiptStatus.Incoming);
                        }
                    }
                },
            };

            //данные грида
            Grid.OnLoadItems = LoadItemsPallet;
            // фильтрация грида
            Grid.OnFilterItems = PalletFilterItems;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdateActions(selectedItem);
                }
            };

            Grid.OnDblClick = selectedItem =>
            {
                Edit();
            };

            Grid.Run();

            //фокус ввода           
            Grid.Focus();
        }

        /// <summary>
        /// Инициализация таблицы записей в накладной прихода поддонов
        /// </summary>
        private void InitGridItems()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Поддон",
                    Path="NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=65,
                    MaxWidth=900,
                },
                new DataGridHelperColumn
                {
                    Header="Количество, шт",
                    Path="QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=85,
                },
                new DataGridHelperColumn
                {
                    Header="Цена, р",
                    Path="PRICE",
                    Format="N2",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Double,
                    MinWidth=50,
                    MaxWidth=60,
                },
                new DataGridHelperColumn
                {
                    Header="Сумма, р",
                    Path="TOTAL",
                    Format="N2",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Double,
                    MinWidth=50,
                    MaxWidth=60,
                },
                new DataGridHelperColumn
                {
                    Header="С дефектом",
                    Path="QTY_DEFECT",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=30,
                    MaxWidth=85,
                },
                new DataGridHelperColumn
                {
                    Header="Проведено",
                    Path="RECORD_FLAG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth=30,
                    MaxWidth=80,
                },
                new DataGridHelperColumn
                {
                    Header="Ид поддона",
                    Path="ID_PAL",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            GridItems.SetColumns(columns);
            GridItems.Init();

            //при выборе строки в гриде, обновляются актуальные действия для записи
            GridItems.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdateItemActions(selectedItem);
                }
            };

            // контекстное меню
            GridItems.Menu = new Dictionary<string, DataGridContextMenuItem>()
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
        }

        /// <summary>
        /// Обновление операций с таблицей поддонов
        /// </summary>
        /// <param name="selectedItem"></param>
        private void UpdateItemActions(Dictionary<string, string> selectedItem)
        {
            SelectedItemItem = selectedItem;

            if (SelectedItem.CheckGet("STATUS") == "Приняты")
            {
                if (SelectedItemItem.CheckGet("RECORD_FLAG").ToInt() == 1)
                {
                    GridItems.Menu["SetRecordFlag"].Visible = false;

                    GridItems.Menu["RemoveRecordFlag"].Visible = true;
                }
                else
                {
                    GridItems.Menu["SetRecordFlag"].Visible = true;

                    GridItems.Menu["RemoveRecordFlag"].Visible = false;
                }
            }
            else
            {
                GridItems.Menu["SetRecordFlag"].Visible = false;

                GridItems.Menu["RemoveRecordFlag"].Visible = false;
            }

            ProcessPermissions();
        }

        /// <summary>
        /// Обновление признака проведения поддона
        /// </summary>
        /// <param name="val">Значение признака</param>
        private async void UpdateRecordFlag(int val)
        {
            int plriId = SelectedItemItem["PLRI_ID"].ToInt();
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Pallet");
            q.Request.SetParam("Action", "UpdateReceiptItemRecordFlag");
            q.Request.SetParam("PLRI_ID", plriId.ToString());
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
                    Grid.LoadItems();
                    GridItems.LoadItems();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        private async void LoadItemsPallet()
        {
            bool resume = true;
            GridToolbar.IsEnabled = false;
            Grid.ShowSplash();

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
                q.Request.SetParam("Action", "ListReceipt");
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
                        var PalletsDS = ListDataSet.Create(result, "ReceiptList");
                        GridItems.UpdateItems(new ListDataSet());
                        Grid.UpdateItems(PalletsDS);
                    }
                }
            }

            Grid.HideSplash();
            GridToolbar.IsEnabled = true;
        }

        /// <summary>
        /// Фильтрация элементов списка накладных прихода поддонов
        /// </summary>
        private void PalletFilterItems()
        {
            if (Grid.GridItems != null)
            {
                if (Grid.GridItems.Count > 0)
                {
                    var items = new List<Dictionary<string, string>>();
                    int sourceType = SourceTypes.SelectedItem.Key.ToInt();
                    foreach (var row in Grid.GridItems)
                    {
                        bool includeBySource = false;
                        if (sourceType == -1)
                        {
                            includeBySource = true;
                        }
                        else if (sourceType == row["SOURCE_ID"].ToInt())
                        {
                            includeBySource = true;
                        }

                        if (includeBySource)
                        {
                            items.Add(row);
                        }
                    }
                    Grid.GridItems = items;
                }
            }
        }

        /// <summary>
        /// Загрузка списка поддонов в накладной прихода поддонов
        /// </summary>
        private async void LoadItemsItems()
        {
            GridItems.ShowSplash();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Pallet");
            q.Request.SetParam("Action", "ListReceiptItems");

            q.Request.SetParam("PlreId", SelectedItem["ID"]);

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
                    var PalletsItemsDS = ListDataSet.Create(result, "ReceiptItemsList");
                    GridItems.UpdateItems(PalletsItemsDS);
                }
            }

            GridItems.HideSplash();
            GridItems.SetSelectToFirstRow();
        }

        /// <summary>
        /// Вызов справки
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/warehouse/pallets#block2");
        }

        /// <summary>
        /// Обновление действий с таблицей приходных накладных
        /// </summary>
        /// <param name="selectedItem"></param>
        private void UpdateActions(Dictionary<string, string> selectedItem)
        {
            SelectedItem = selectedItem;

            int status = SelectedItem["STATUS_ID"].ToInt();

            DeleteButton.IsEnabled = (SelectedItem["STATUS_ID"].ToInt() != PalletReceiptStatus.Accepted);

            Grid.Menu["SetAcceptedState"].Enabled = (status != PalletReceiptStatus.Accepted);
            Grid.Menu["SetRejectedState"].Enabled = (status != PalletReceiptStatus.Rejected);
            Grid.Menu["SetIncomingState"].Enabled = (status != PalletReceiptStatus.Incoming);

            LoadItemsItems();

            ProcessPermissions();
        }

        private void Add()
        {
            var receiptForm = new PalletReceipt();
            receiptForm.ReturnTabName = "PalletReceipt";
            receiptForm.Edit(0);
        }

        private void Edit()
        {
            if (Central.Navigator.GetRoleLevel("[erp]pallet") >= Role.AccessMode.FullAccess)
            {
                int plreId = SelectedItem.CheckGet("ID").ToInt();
                if (plreId > 0)
                {
                    var receiptForm = new PalletReceipt();
                    receiptForm.ReturnTabName = "PalletReceipt";
                    receiptForm.Edit(plreId);
                }
            }
        }

        private async void Delete()
        {
            Grid.ShowSplash();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Pallet");
            q.Request.SetParam("Action", "DeleteReceipt");
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
                    Grid.LoadItems();
                }
            }
            else
            {
                q.ProcessError();
            }

            Grid.HideSplash();
        }

        /// <summary>
        /// Добавление файла к приходной накладной
        /// </summary>
        private async void AddFile()
        {
            var fd = new OpenFileDialog();
            var fdResult = (bool)fd.ShowDialog();
            if (fdResult)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Stock");
                q.Request.SetParam("Object", "Pallet");
                q.Request.SetParam("Action", "SaveReceiptFile");
                q.Request.SetParam("ID", SelectedItem["ID"]);
                q.Request.Type = RequestTypeRef.MultipartForm;
                q.Request.UploadFilePath = fd.FileName;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    Grid.LoadItems();
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// Сохраняет состояние чекбокса проверки ОЭБ накладной прихода поддонов
        /// </summary>
        /// <param name="isChecked">Состояние чекбокса Проверка ОЭБ</param>
        private async void CheckSecurity(bool isChecked)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Pallet");
            q.Request.SetParam("Action", "UpdateReceiptSecurityFlag");
            q.Request.SetParam("ID", SelectedItem["ID"]);
            q.Request.SetParam("SECURITY_FLAG", isChecked ? "1" : "0");

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status != 0)
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Изменяет статус накладной прихода поддонов
        /// </summary>
        /// <param name="newValue">Код нового статуса</param>
        private async void SetStatus(int newValue)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Pallet");
            q.Request.SetParam("Action", "UpdateReceiptStatus");
            q.Request.SetParam("ID", SelectedItem["ID"]);
            q.Request.SetParam("STATUS", newValue.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    Grid.LoadItems();
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
            Grid.LoadItems();
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

        private void SourceTypes_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            Add();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            Edit();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem.CheckGet("ID").ToInt() > 0)
            {
                var dw = new DialogWindow("Удалить накладную прихода поддонов?", "Удаление накладной", "", DialogWindowButtons.NoYes);
                if (dw.ShowDialog() == true)
                {
                    Delete();
                }
            }
            else
            {
                var dw = new DialogWindow("Не выбрана накладная", "Удаление накладной");
                dw.ShowDialog();
            }
        }
    }
}
