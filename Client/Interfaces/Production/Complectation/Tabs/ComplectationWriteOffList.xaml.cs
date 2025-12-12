using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Stock;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Printing;
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

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Список списаний на комплектации
    /// Включает в себя полные и частичные списания поддонов (когда не создаются новые поддоны).
    /// Операции:
    /// Отмена списания (для частичного списания при комплектации на ГА);
    /// Печать ярлыка.
    /// </summary>
    /// <author>sviridov_ae</author>
    public partial class ComplectationWriteOffList : UserControl
    {
        public ComplectationWriteOffList()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            FormInit();
            SetDefaults();

            WriteOffGridInit();

            ProcessPermissions();
        }

        public int FactoryId = 1;

        /// <summary>
        /// Выбранная запись в гриде списаний
        /// </summary>
        public Dictionary<string, string> WriteOffSelectedItem { get; set; }

        public ListDataSet WriteOffDataSet { get; set; }

        /// <summary>
        /// Выбранная запись поддона
        /// </summary>
        public Dictionary<string, string> PalletWriteOffSelectedItem { get; set; }

        public ListDataSet PalletWriteOffDataSet { get; set; }

        /// <summary>
        /// Флаг особых прав (для отмены списания)
        /// </summary>
        public bool MasterFlag { get; set; }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        public void FormInit()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path = "FROM_DATE",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = FromDate,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
                new FormHelperField()
                {
                    Path = "TO_DATE",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = ToDate,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
            };

            Form.SetFields(fields);
            Form.ToolbarControl = GridToolbar;
        }

        private void WriteOffGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "Номер ПЗ",
                    Path = "NUM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=9,
                },
                new DataGridHelperColumn
                {
                    Header = "ИД ПЗ",
                    Path = "ID_PZ",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header = "Дата",
                    Path = "DT",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Format = "dd.MM.yyyy HH:mm:ss",
                    Width2=14,
                },
                new DataGridHelperColumn
                {
                    Header = "Наименование",
                    Path = "CONSUMPTION_NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=44,
                },
                new DataGridHelperColumn
                {
                    Header = "Артикул",
                    Path = "CONSUMPTION_ARTIKUL",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn
                {
                    Header = "Номер поддона",
                    Path = "PODDON_NUM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header = "На поддоне по умолчанию, шт.",
                    Path = "DEFAULT_QUANTITY",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header = "Расход, шт.",
                    Path = "CONSUMPTION_QTY",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header = "Остаток, шт. (текущий)",
                    Path = "BALANCE_QUANTITY",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header = "ИД заявки",
                    Path = "NSTHET",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header = "Пользователь",
                    Path = "STAFF_NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header = "Склад (текущий)",
                    Path = "CURRENT_WAREHOUSE",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header = "Место (текущее)",
                    Path = "CURRENT_PLACE",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header = "Причина",
                    Path = "REASON_DESCRIPTION",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=21,
                },
                new DataGridHelperColumn
                {
                    Header = "Отгружен",
                    Path = "SOLD_OUT",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Boolean,
                    Width2=9,
                },

                new DataGridHelperColumn
                {
                    Header = "Ид причины",
                    Path = "REASON_ID",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 50,
                    MaxWidth = 50,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header = "Ид станка",
                    Path = "ID_ST",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 50,
                    MaxWidth = 50,
                    Hidden=true,
                },
                
            };

            Grid.SetColumns(columns);
            Grid.SetSorting("DT", ListSortDirection.Descending);
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.SearchText = SearchText;
            Grid.Menu = new Dictionary<string, DataGridContextMenuItem>();
            Grid.OnLoadItems = WriteOffLoadItems;
            Grid.OnSelectItem = item =>
            {
                WriteOffSelectedItem = item;
                UpdateButtons();
            };

            Grid.OnFilterItems = () =>
            {
                if (Grid.GridItems != null)
                {
                    if (Grid.GridItems.Count > 0)
                    {
                        if (ComplectationTypes.SelectedItem.Key != null)
                        {
                            var idSt = ComplectationTypes.SelectedItem.Key;

                            var items = new List<Dictionary<string, string>>();

                            switch (idSt)
                            {
                                // СГП
                                case ComplectationPlace.Stock:
                                    items.AddRange(Grid.GridItems.Where(row => row.CheckGet("ID_ST") == ComplectationPlace.Stock));
                                    break;

                                //ГА
                                case ComplectationPlace.CorrugatingMachines:
                                    items.AddRange(Grid.GridItems.Where(row => row.CheckGet("ID_ST") == ComplectationPlace.CorrugatingMachines));
                                    break;

                                //ПР
                                case ComplectationPlace.ProcessingMachines:
                                    items.AddRange(Grid.GridItems.Where(row => row.CheckGet("ID_ST") == ComplectationPlace.ProcessingMachines));
                                    break;

                                default:
                                    items = Grid.GridItems;
                                    break;
                            }

                            Grid.GridItems = items;
                        }
                    }
                }
            };

            Grid.AutoUpdateInterval = 0;
            Grid.Init();
            Grid.Run();
            Grid.Focus();
        }

        public void UpdateButtons()
        {
            RemoveWriteOffButton.IsEnabled = false;
            LabelPrintButton.IsEnabled = false;
            PalletHistoryButton.IsEnabled = false;

            if (WriteOffSelectedItem != null)
            {
                if (WriteOffSelectedItem.Count > 0)
                {
                    PalletHistoryButton.IsEnabled = true;

                    var idSt = WriteOffSelectedItem.CheckGet("ID_ST").ToInt();

                    // Если станок -- комплектация ГА
                    if (idSt == 719)
                    {
                        if (WriteOffSelectedItem.CheckGet("BALANCE_QUANTITY").ToInt() > 0)
                        {
                            LabelPrintButton.IsEnabled = true;
                        }

                        if (MasterFlag)
                        {
                            RemoveWriteOffButton.IsEnabled = true;
                        }
                    }
                }
            }
        }

        public void SetDefaults()
        {
            Form.SetValueByPath("FROM_DATE", DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy"));
            Form.SetValueByPath("TO_DATE", DateTime.Now.ToString("dd.MM.yyyy"));

            PalletWriteOffDataSet = new ListDataSet();
            WriteOffDataSet = new ListDataSet();

            PalletWriteOffSelectedItem = new Dictionary<string, string>();
            WriteOffSelectedItem = new Dictionary<string, string>();

            {
                var list = new Dictionary<string, string>();
                list.Add("-1", "Все участки");
                list.Add(ComplectationPlace.CorrugatingMachines, "ГА");
                list.Add(ComplectationPlace.Stock, "СГП");
                list.Add(ComplectationPlace.ProcessingMachines, "Переработка");
                ComplectationTypes.Items = list;
                ComplectationTypes.SelectedItem = list.FirstOrDefault((x) => x.Key == "-1");
            }
        }

        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            // Если есть полные права, даём возможность отменять комплектацию и повторно печатать ярлык
            var mode = Central.Navigator.GetRoleLevel("[erp]complectation_list");
            switch (mode)
            {
                case Role.AccessMode.Special:
                    MasterFlag = true;
                    EditReasonButton.IsEnabled = true;
                    break;

                case Role.AccessMode.FullAccess:
                    MasterFlag = true;
                    EditReasonButton.IsEnabled = true;
                    break;

                default:
                    MasterFlag = false;
                    EditReasonButton.IsEnabled = false;
                    break;
            }
        }

        public async void WriteOffLoadItems()
        {
            Grid.ShowSplash();
            GridToolbar.IsEnabled = false;

            var resume = true;

            var f = Form.GetValueByPath("FROM_DATE").ToDateTime();
            var t = Form.GetValueByPath("TO_DATE").ToDateTime();

            if (DateTime.Compare(f, t) > 0)
            {
                const string msg = "Дата начала должна быть меньше даты окончания.";
                var d = new DialogWindow($"{msg}", "Проверка данных");
                d.ShowDialog();
                resume = false;
            }

            if (resume)
            {
                Grid?.ClearItems();

                var p = new Dictionary<string, string>
                {
                    ["FromDate"] = Form.GetValueByPath("FROM_DATE"),
                    ["ToDate"] = Form.GetValueByPath("TO_DATE"),
                    ["FACTORY_ID"] = $"{FactoryId}"
                };

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Complectation");
                q.Request.SetParam("Object", "Operation");
                q.Request.SetParam("Action", "ListWriteOff");

                q.Request.SetParams(p);

                q.Request.Timeout = 30000;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() => {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                    if (result != null)
                    {
                        WriteOffDataSet = ListDataSet.Create(result, "ITEMS");

                        if (WriteOffDataSet.Items.Count > 0)
                        {
                            foreach (var item in WriteOffDataSet.Items)
                            {
                                if (item.CheckGet("SOLD_NSTHET").ToInt() > 0)
                                {
                                    item.CheckAdd("SOLD_OUT", "1");
                                }
                                else
                                {
                                    item.CheckAdd("SOLD_OUT", "0");
                                }
                            }
                        }

                        Grid.UpdateItems(WriteOffDataSet);
                    }
                }
            }

            Grid.HideSplash();
            GridToolbar.IsEnabled = true;
        }

        /// <summary>
        /// Отмена списания
        /// </summary>
        public async void WriteOffRemove()
        {
            DisableControls();
            
            if (WriteOffSelectedItem != null)
            {
                if (WriteOffSelectedItem.Count > 0)
                {
                    var p = new Dictionary<string, string>();
                    p.Add("IDP", WriteOffSelectedItem.CheckGet("IDP"));
                    p.Add("IDR", WriteOffSelectedItem.CheckGet("IDR"));
                    p.Add("ID_ST", WriteOffSelectedItem.CheckGet("ID_ST"));
                    p.Add("ID2", WriteOffSelectedItem.CheckGet("ID2"));
                    p.Add("IDK1", WriteOffSelectedItem.CheckGet("IDK1"));
                    p.Add("PODDON_NUM", WriteOffSelectedItem.CheckGet("PODDON_NUM"));

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Complectation");
                    q.Request.SetParam("Object", "Pallet");
                    q.Request.SetParam("Action", "RollbackWriteOffCM");

                    q.Request.SetParams(p);

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

                            int idMoving = ds.Items.First().CheckGet("ID_MOVING").ToInt();

                            if (idMoving > 0)
                            {
                                var msg = "Успешная отмена списания";
                                var d = new DialogWindow($"{msg}", "Отмена списания", "", DialogWindowButtons.OK);
                                d.ShowDialog();

                                WriteOffLoadItems();

                                {
                                    // отправить сообщение списку доступных комплектаций на ГА
                                    Messenger.Default.Send(new ItemMessage
                                    {
                                        ReceiverGroup = "Complectation",
                                        ReceiverName = "CM",
                                        SenderName = "ComplectationList",
                                        Action = "Refresh",
                                    });
                                }
                            }
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
            }

            EnableControls();
        }

        /// <summary>
        /// Делает неактивным тулбар вкладки
        /// </summary>
        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
        }

        /// <summary>
        /// Делает активным тулбар вкладки
        /// Вызывает метод установки активности кнопок
        /// </summary>
        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
            UpdateButtons();
        }

        /// <summary>
        /// Установка настроек для принтера
        /// </summary>
        public void SetPrintSettings()
        {
            LabelReport2.SetPrintingProfile();
        }

        /// <summary>
        /// Деструктор. Остановка вспомогательных процессов при закрытии вкладки
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии фрейма
            Messenger.Default.Send(new ItemMessage
            {
                ReceiverGroup = "ProductionComplectationList",
                ReceiverName = "",
                SenderName = "ComplectationWriteOffList",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры гридов
            Grid.Destruct();
        }

        private void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp-new/production_new/complectation/list_complectation/write-off");
            //Central.ShowHelp("/doc/l-pack-erp/production/complectation/complectation_list/write_off");
        }

        public void LabelPrint()
        {
            LabelReport2 report = new LabelReport2(true);
            report.PrintLabel(WriteOffSelectedItem.CheckGet("ID_PZ"), WriteOffSelectedItem.CheckGet("PODDON_NUM"), WriteOffSelectedItem.CheckGet("IDK1"), WriteOffSelectedItem.CheckGet("IDP").ToInt());
        }

        /// <summary>
        /// Открываем всплывающее окно для подтверждения отмены списания
        /// и, в случае подтверждения, вызываем функцию отмены списания
        /// </summary>
        public void WriteOffRemoveClick()
        {
            string msg = "Отменить выбранное списание?";

            var d = new DialogWindow($"{msg}", "Список списаний", "", DialogWindowButtons.NoYes);
            if (d.ShowDialog() == true)
            {
                WriteOffRemove();
            }
        }

        /// <summary>
        /// экспорт записей грида в Excel
        /// </summary>
        private async void ExportToExcel()
        {
            if (Grid.Items != null)
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
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.Contains("Complectation"))
            {
                if (m.ReceiverName.Contains("WriteOff"))
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            WriteOffLoadItems();
                            break;
                    }

                }
            }
        }

        private void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
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

        public async void PalletGetHistory()
        {
            var p = new Dictionary<string, string>();
            p.Add("idp", WriteOffSelectedItem.CheckGet("IDP"));
            p.Add("num", WriteOffSelectedItem.CheckGet("PODDON_NUM"));

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Pallet");
            q.Request.SetParam("Action", "ListHistory");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() => {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                if (result != null)
                {
                    DialogWindow.ShowDialog(result["List"].Rows.Select(row => row[0]).Aggregate(string.Empty, (row, record) => row + record + "\n"), "История перемещения поддона");
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        public void EditReason()
        {
            if (WriteOffSelectedItem != null && WriteOffSelectedItem.Count > 0)
            {
                var view = new ComplectationWriteOffReasonsEdit();
                view.OldReasonId = WriteOffSelectedItem.CheckGet("REASON_ID").ToInt();

                switch (WriteOffSelectedItem.CheckGet("ID_ST").ToInt())
                {
                    case 719:
                        view.CorrugatorFlag = 1;
                        break;

                    case 720:
                        view.StockFlag = 1;
                        break;

                    case 721:
                        view.ConvertingFlag = 1;
                        break;

                    default:
                        break;
                }

                view.Show();
                if (view.OkFlag)
                {
                    string reasonId = view.SelectedReason.Key;

                    var p = new Dictionary<string, string>();
                    p.Add("NSTHET", WriteOffSelectedItem.CheckGet("NSTHET"));
                    p.Add("REASON_ID", reasonId);

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Complectation");
                    q.Request.SetParam("Object", "Operation");
                    q.Request.SetParam("Action", "UpdateReasonWriteOff");
                    q.Request.SetParams(p);

                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {
                        int nsthet = 0;

                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            var dataSet = ListDataSet.Create(result, "ITEMS");
                            if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                            {
                                nsthet = dataSet.Items.First().CheckGet("NSTHET").ToInt();
                            }
                        }

                        if (nsthet > 0)
                        {
                            WriteOffLoadItems();
                        }
                        else
                        {
                            DialogWindow.ShowDialog("Ошибка изменения причины списания");
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
            }
            else
            {
                DialogWindow.ShowDialog("Не выбрано списание для изменения причины");
            }
        }

        private void RemoveWriteOffButton_Click(object sender, RoutedEventArgs e)
        {
            WriteOffRemoveClick();
        }

        private void LabelPrintButton_Click(object sender, RoutedEventArgs e)
        {
            LabelPrint();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            SetPrintSettings();
        }

        private void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            WriteOffLoadItems();
        }

        private void ExcelButton_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcel();
        }

        private void PalletHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            PalletGetHistory();
        }

        private void EditReasonButton_Click(object sender, RoutedEventArgs e)
        {
            EditReason();
        }

        private void ComplectationTypes_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }
    }
}
