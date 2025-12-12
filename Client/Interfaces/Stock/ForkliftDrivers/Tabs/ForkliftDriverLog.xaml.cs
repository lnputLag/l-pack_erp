using Client.Common;
using Client.Interfaces.Main;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json;
using System.Runtime.InteropServices.ComTypes;
using NPOI.SS.Formula.Functions;
using System.Linq;
using GalaSoft.MvvmLight.Messaging;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Работа водителей погрузчиков
    /// </summary>
    /// <author>Михеев И.С.</author>
    public partial class ForkliftDriverLog : UserControl
    {
        public ForkliftDriverLog()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            FormInit();
            SetDefaults();

            InitForkliftDriverGrid();
            InitWorkForkliftDriverGrid();
            InitAbsentDriverGrid();

            ProcessPermissions();

            PreviewKeyDown += ProcessKeyboard;
        }

        public int FactoryId = 1;

        public string RoleName = "[erp]forklift_drivers";

        private string FrameName = "ForkliftDrivers_Log";

        /// <summary>
        /// Датасет с данными для грида водитлей погрузчика
        /// </summary>
        private ListDataSet ForkliftDriverDataSet { get; set; }

        /// <summary>
        /// Выбранная запись в гриде водителей погрузчика
        /// </summary>
        private Dictionary<string, string> ForkliftDriverSelectedItem { get; set; }

        /// <summary>
        /// Датасет с данными по операциям, выполненым выбранным водителем погрузчика
        /// </summary>
        private ListDataSet WorkForkliftDriverDataSet { get; set; }

        /// <summary>
        /// Датасет с данными по отсутствию водителя
        /// </summary>
        private ListDataSet AbsentDriverDataSet { get; set; }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Инициализация формы
        /// </summary>
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
        }

        /// <summary>
        /// Установки по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            Form.SetValueByPath("FROM_DATE", $"{DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy")} 08:00:00");
            Form.SetValueByPath("TO_DATE", $"{DateTime.Now.ToString("dd.MM.yyyy")} 08:00:00");

            var dictionary = new Dictionary<string, string>();
            dictionary.Add("0", "Все склады");
            dictionary.Add("1", "СГП и Буфер");
            dictionary.Add("10", "СГП / Буфер");
            dictionary.Add("11", "Буфер");
            dictionary.Add("12", "СГП");
            dictionary.Add("2", "Стеллажный склад");
            dictionary.Add("3", "Склад рулонов");
            StockSelectBox.SetItems(dictionary);
            StockSelectBox.SetSelectedItemByKey("0");
        }

        /// <summary>
        /// обработчик системы навигации по URL
        /// </summary>
        public void ProcessNavigation()
        {

        }

        /// <summary>
        /// Инициализация грида водителей погрузчика
        /// </summary>
        private void InitForkliftDriverGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "Водитель",
                    Path = "NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width=125,
                },
                new DataGridHelperColumn
                {
                    Header = "Склад",
                    Path = "STOCK_NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width=115,
                },
                new DataGridHelperColumn
                {
                    Header = "Операций",
                    Path = "CNT",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width=46,
                },


                new DataGridHelperColumn
                {
                    Header = "STOCK",
                    Path = "STOCK",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header = "SUB_STOCK",
                    Path = "SUB_STOCK",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header = "ORDER_NUM",
                    Path = "ORDER_NUM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            ForkliftDriverGrid.SetColumns(columns);

            ForkliftDriverGrid.Grid.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
            ForkliftDriverGrid.SearchText = ForkliftDriverSearchText;
            ForkliftDriverGrid.OnLoadItems = ForkliftDriverGridLoadItems;
            ForkliftDriverGrid.UseSorting = false;
            
            ForkliftDriverGrid.OnSelectItem = selectedItem =>
            {
                ForkliftDriverSelectedItem = selectedItem;
                WorkForkliftDriverGridLoadItems();

                ExcelButton.IsEnabled = selectedItem != null;
                ProcessPermissions();
            };

            ForkliftDriverGrid.OnFilterItems = () =>
            {
                if (ForkliftDriverGrid.GridItems != null)
                {
                    if (ForkliftDriverGrid.GridItems.Count > 0)
                    {
                        if (StockSelectBox.SelectedItem.Key != null)
                        {
                            var key = StockSelectBox.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            switch (key)
                            {
                                // Все склады
                                case 0:
                                    items = ForkliftDriverGrid.GridItems;
                                    break;

                                case 10:
                                    items.AddRange(ForkliftDriverGrid.GridItems.Where(x => x.CheckGet("STOCK").ToInt() == 1 && x.CheckGet("SUB_STOCK").ToInt() == key));
                                    break;

                                case 11:
                                    items.AddRange(ForkliftDriverGrid.GridItems.Where(x => x.CheckGet("STOCK").ToInt() == 1 && x.CheckGet("SUB_STOCK").ToInt() == key));
                                    break;

                                case 12:
                                    items.AddRange(ForkliftDriverGrid.GridItems.Where(x => x.CheckGet("STOCK").ToInt() == 1 && x.CheckGet("SUB_STOCK").ToInt() == key));
                                    break;

                                default:
                                    items.AddRange(ForkliftDriverGrid.GridItems.Where(x => x.CheckGet("STOCK").ToInt() == key));
                                    break;
                            }

                            ForkliftDriverGrid.GridItems = items;
                        }
                    }
                }
            };

            ForkliftDriverGrid.Init();
            ForkliftDriverGrid.Run();
            ForkliftDriverGrid.Focus();
        }

        /// <summary>
        /// Получаем данные для грида подителей погрузчика
        /// </summary>
        private async void ForkliftDriverGridLoadItems()
        {
            DisableControls();

            var p = new Dictionary<string, string>
            {
                ["FromDate"] = Form.GetValueByPath("FROM_DATE"),
                ["ToDate"] = Form.GetValueByPath("TO_DATE"),
                ["FACTORY_ID"] = $"{FactoryId}"
            };

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "ForkliftDriver");
            q.Request.SetParam("Action", "ListTask");
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
                    ForkliftDriverDataSet = ListDataSet.Create(result, "List");
                    ForkliftDriverGrid.UpdateItems(ForkliftDriverDataSet);
                }
            }

            EnableControls();
        }

        /// <summary>
        /// Деактивация контролов
        /// </summary>
        public void DisableControls()
        {
            ForkliftDriverGrid.ShowSplash();
            WorkForkliftDriverGrid.ShowSplash();
            GridToolbar.IsEnabled = false;
        }

        /// <summary>
        /// Активация контролов
        /// </summary>
        public void EnableControls()
        {
            ForkliftDriverGrid.HideSplash();
            WorkForkliftDriverGrid.HideSplash();
            GridToolbar.IsEnabled = true;
        }

        /// <summary>
        /// Инициализация грида операций выбранного водителя погрузчика
        /// </summary>
        private void InitWorkForkliftDriverGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "Дата",
                    Path = "TM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Format="dd.MM.yyyy HH:mm",
                    MinWidth = 100,
                    MaxWidth = 100,
                },
                new DataGridHelperColumn
                {
                    Header = "Наименование",
                    Path = "NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width = 300,
                },
                new DataGridHelperColumn
                {
                    Header = "Объект",
                    Path = "PALLET",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width = 70,
                },
                new DataGridHelperColumn
                {
                    Header = "Операция",
                    Path = "REASON",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    //Width = 154,
                    Width=183,
                },
                new DataGridHelperColumn
                {
                    Header = "Место",
                    Path = "PLACE",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width = 80,
                },
                new DataGridHelperColumn
                {
                    Header = "Водитель",
                    Path = "FD_NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width = 150,
                },
                new DataGridHelperColumn
                {
                    Header = "Предыдущая операция, мин",
                    Path = "PREV_OPERATION",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    //Width = 150,
                    Width = 173,
                },

                new DataGridHelperColumn
                {
                    Header = "STOCK",
                    Path = "STOCK",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },

                new DataGridHelperColumn
                {
                    Header = " ",
                    Path = "_",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 5,
                    MaxWidth = 1500,
                }
            };
            WorkForkliftDriverGrid.SetColumns(columns);
            WorkForkliftDriverGrid.SetSorting("TM", ListSortDirection.Descending);
            WorkForkliftDriverGrid.SearchText = WorkForkliftDriverSearchText;

            WorkForkliftDriverGrid.Init();
            WorkForkliftDriverGrid.Run();
            WorkForkliftDriverGrid.Focus();
        }

        /// <summary>
        /// Получаем данные для грида операций выбранного водителя погрузчика
        /// </summary>
        private async void WorkForkliftDriverGridLoadItems()
        {
            if (ForkliftDriverSelectedItem != null && ForkliftDriverSelectedItem.Count > 0)
            {
                DisableControls();

                var p = new Dictionary<string, string>
                {
                    ["STOCK"] = ForkliftDriverSelectedItem.CheckGet("STOCK"),
                    ["SUB_STOCK"] = ForkliftDriverSelectedItem.CheckGet("SUB_STOCK"),
                    ["FromDate"] = Form.GetValueByPath("FROM_DATE"),
                    ["ToDate"] = Form.GetValueByPath("TO_DATE"),
                    ["FACTORY_ID"] = $"{FactoryId}"
                };

                if (ForkliftDriverSelectedItem.CheckGet("NAME") != "Все")
                {
                    p.Add("NAME_FD", ForkliftDriverSelectedItem.CheckGet("NAME"));
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Stock");
                q.Request.SetParam("Object", "ForkliftDriver");
                q.Request.SetParam("Action", "ListTaskByDriver");
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
                        WorkForkliftDriverDataSet = ListDataSet.Create(result, "ITEMS");
                        WorkForkliftDriverGrid.UpdateItems(WorkForkliftDriverDataSet);
                    }
                }
                else
                {
                    q.ProcessError();
                }

                EnableControls();
            }
        }

        private void InitAbsentDriverGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "Водитель",
                    Path = "NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 120,
                    MaxWidth = 150,
                },
                new DataGridHelperColumn
                {
                    Header = "Причина отсутствия",
                    Path = "ABSENT",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 120,
                    MaxWidth = 150,
                },
                new DataGridHelperColumn
                {
                    Header = "Начало отсутствия",
                    Path = "START_DTTM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width = 120,
                },
                new DataGridHelperColumn
                {
                    Header = "Конец отсутствия",
                    Path = "END_DTTM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width = 120,
                },
                new DataGridHelperColumn
                {
                    Header = "Время отсутствия, мин",
                    Path = "LUNCH",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 70,
                    MaxWidth = 140,
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

            AbsentDriverGrid.SetColumns(columns);

            AbsentDriverGrid.SetSorting("NAME");

            AbsentDriverGrid.Init();

            AbsentDriverGrid.OnLoadItems = AbsentDriverGridLoadItems;

            AbsentDriverGrid.Run();

            AbsentDriverGrid.Focus();
        }

        private async void AbsentDriverGridLoadItems()
        {
            AbsentDriverGrid.ShowSplash();

            var p = new Dictionary<string, string>
            {
                ["FromDate"] = Form.GetValueByPath("FROM_DATE"),
                ["ToDate"] = Form.GetValueByPath("TO_DATE"),
                ["FACTORY_ID"] = $"{FactoryId}"
            };

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "ForkliftDriver");
            q.Request.SetParam("Action", "ListAbsent");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() => { 
                q.DoQuery(); 
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    {
                        AbsentDriverDataSet = ListDataSet.Create(result, "List");
                        AbsentDriverGrid.UpdateItems(AbsentDriverDataSet);
                    }
                }
            }

            AbsentDriverGrid.HideSplash();
        }

        private void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/warehouse/rabota-voditelej-pogruzchikov#block2");
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {

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
            AbsentDriverGrid.Destruct();
            WorkForkliftDriverGrid.Destruct();
        }

        private void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            var f = FromDate.Text.ToDateTime();
            var t = ToDate.Text.ToDateTime();
            if (DateTime.Compare(f, t) > 0)
            {
                const string msg = "Дата начала должна быть меньше даты окончания.";
                var d = new DialogWindow($"{msg}", "Проверка данных");
                d.ShowDialog();
                return;
            }

            ForkliftDriverGridLoadItems();
            AbsentDriverGridLoadItems();
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

            if (WorkForkliftDriverGrid != null && WorkForkliftDriverGrid.Menu != null && WorkForkliftDriverGrid.Menu.Count > 0)
            {
                foreach (var manuItem in WorkForkliftDriverGrid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }

            if (AbsentDriverGrid != null && AbsentDriverGrid.Menu != null && AbsentDriverGrid.Menu.Count > 0)
            {
                foreach (var manuItem in AbsentDriverGrid.Menu)
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
        /// отчет 
        /// </summary>
        private async void ExportToExcel()
        {
            if (ForkliftDriverSelectedItem != null)
            {
                var eg = new ExcelGrid
                {
                    Columns = new List<ExcelGridColumn>
                    {
                        new ExcelGridColumn("TM", "Дата", 100),
                        new ExcelGridColumn("NAME", "Наименование", 350),
                        new ExcelGridColumn("PALLET", "Объект", 70),
                        new ExcelGridColumn("REASON", "Операция", 120),
                        new ExcelGridColumn("PLACE", "Место", 55),
                        new ExcelGridColumn("FD_NAME", "Водитель", 55),
                        new ExcelGridColumn("PREV_OPERATION", "Предыдущая операция, мин", 55, ExcelGridColumn.ColumnTypeRef.Integer)
                    },

                    Items = WorkForkliftDriverGrid.GridItems,
                    GridTitle = $"Отчет о работе погрузчика на {DateTime.Now:dd.MM.yyyy hh:mm:ss}. Водитель {ForkliftDriverSelectedItem.CheckGet("NAME")}. Склад {ForkliftDriverSelectedItem.CheckGet("STOCK_NAME")}."
                };

                await Task.Run(() =>
                {
                    eg.Make();
                });
            }
        }

        public async void DriverexportToExcel()
        {
            var eg = new ExcelGrid
            {
                Columns = new List<ExcelGridColumn>
                {
                    new ExcelGridColumn("NAME", "Водитель", 100),
                    new ExcelGridColumn("STOCK_NAME", "Склад", 100),
                    new ExcelGridColumn("CNT", "Количество операций", 110, ExcelGridColumn.ColumnTypeRef.Integer),
                },

                Items = ForkliftDriverGrid.GridItems,
                GridTitle = $"Отчет по водителям погрузчика от {DateTime.Now:dd.MM.yyyy hh:mm:ss}."
            };

            await Task.Run(() =>
            {
                eg.Make();
            });
        }

        private void StockSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ForkliftDriverGrid.UpdateItems();
        }

        private void DriverExcelButton_Click(object sender, RoutedEventArgs e)
        {
            DriverexportToExcel();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void ExportToExcelButton_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcel();
        }
    }
}
