using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Printing;
using Client.Interfaces.Stock;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Форма списка комплектаций Кашира
    /// </summary>
    /// <author>михеев</author>
    public partial class ComplectationListKsh : UserControl
    {
        public ComplectationListKsh()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            FormInit();
            SetDefaults();

            GridFormInit();
            ToGridInit();
            ComplectationGridInit();

            ProcessPermissions();
        }

        public int FactoryId = 2;

        public string FrameName = "ComplectationListKsh";

        private ListDataSet FromDataSet { get; set; }
        private ListDataSet ToDataSet { get; set; }
        private Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// Выбранная запись в правом нижнем гриде (полученные после комплектации поддоны) 
        /// </summary>
        private Dictionary<string, string> ToGridSelectedItem { get; set; }

        /// <summary>
        /// Выбранная запись в левом нижнем гриде (поддоны из которых комплектовались новые)
        /// </summary>
        public Dictionary<string, string> FromGridSelectedItem { get; set; }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        private ListDataSet ComplectationDataSet { get; set; }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void  ProcessPermissions(string roleCode="")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel("[erp]complectation_list_ksh");
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
        }
        
        #region default functions

        public void SetDefaults()
        {
            FromDataSet = new ListDataSet();
            ToDataSet = new ListDataSet();

            Form.SetValueByPath("FROM_DATE", DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy"));
            Form.SetValueByPath("TO_DATE", DateTime.Now.ToString("dd.MM.yyyy"));

            {
                var list = new Dictionary<string, string>();
                list.Add("-1", "Все участки");
                list.Add(ComplectationPlace.CorrugatingMachinesKsh, "ГА КШ");
                list.Add(ComplectationPlace.ProcessingMachinesKsh, "Переработка КШ");
                list.Add(ComplectationPlace.StockKsh, "СГП КШ");
                ComplectationTypes.Items = list;
                ComplectationTypes.SelectedItem = list.FirstOrDefault((x) => x.Key == "-1");
            }

            {
                var list = new Dictionary<string, string>();
                list.Add("-1", "Все смены");
                list.Add("1", "Смена 1");
                list.Add("2", "Смена 2");
                list.Add("3", "Смена 3");
                list.Add("4", "Смена 4");
                WorkTeamSelectBox.Items = list;
                WorkTeamSelectBox.SelectedItem = list.FirstOrDefault((x) => x.Key == "-1");
            }
        }

        /// <summary>
        /// Деструктор. Остановка вспомогательных процессов при закрытии вкладки
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии фрейма
            Messenger.Default.Send(new ItemMessage
            {
                ReceiverGroup = "ComplectationKsh",
                ReceiverName = "",
                SenderName = this.FrameName,
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
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.Contains("ComplectationKsh"))
            {
                if (m.ReceiverName.Contains(this.FrameName))
                {
                    switch (m.Action)
                    {
                        case "Refresh":

                            var isOk = true;

                            if (m.SenderName == "ComplectationStockEdit")
                            {
                                if (Form != null)
                                {
                                    var f = Form.GetValueByPath("FROM_DATE").ToDateTime();
                                    var t = Form.GetValueByPath("TO_DATE").ToDateTime();

                                    isOk = f <= DateTime.Now && t.AddDays(1) >= DateTime.Now;
                                }
                            }

                            if (isOk)
                            {
                                ComplectationLoadItems();
                            }

                            break;
                    }
                }
            }
        }

        /// <summary>
        /// обработчик системы навигации по URL
        /// </summary>
        public void ProcessNavigation()
        {
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

        private void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp-new/production_new/complectation/list_complectation/complectations");
            //Central.ShowHelp("/doc/l-pack-erp/warehouse/operations#block3");
        }


        #endregion

        #region Init Grids

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

        private void ToGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "№ поддона",
                    Path = "NUM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 80,
                    MaxWidth = 80,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество, шт.",
                    Path = "QTY",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 80,
                    MaxWidth = 80,
                },

                new DataGridHelperColumn
                {
                    Header = "IDP",
                    Path = "IDP",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width = 80,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header = "PALLET_ID",
                    Path = "PALLET_ID",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width = 80,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header = "PALLET_NUMBER",
                    Path = "PALLET_NUMBER",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width = 80,
                    Hidden=true,
                },

                new DataGridHelperColumn
                {
                    Header = " ",
                    Path = " ",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 5,
                    MaxWidth = 2000,
                },
            };

            GridTo.SetColumns(columns);
            GridTo.SetSorting("NUM");

            GridTo.Menu = new Dictionary<string, DataGridContextMenuItem>()
            {
                {
                    "MovingHistory",
                    new DataGridContextMenuItem()
                    {
                        Header="История перемещения",
                        Action=()=>
                        {
                            MovingHistory(GridTo.SelectedItem);
                        },
                    }
                },
            };

            GridTo.OnSelectItem = selectedItem =>
            {
                ToGridSelectedItem = selectedItem;
                LabelPrintButton.IsEnabled = ToGridSelectedItem != null;
            };

            GridTo.Init();
            GridTo.Run();

            GridTo.UpdateItems(ToDataSet);
        }

        private void GridFormInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "ПЗ",
                    Path = "FULL_NUM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 80,
                    MaxWidth = 80,
                },
                new DataGridHelperColumn
                {
                    Header = "№ поддона",
                    Path = "NUM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 80,
                    MaxWidth = 80,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество, шт.",
                    Path = "QTY",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 80,
                    MaxWidth = 80,
                },
                new DataGridHelperColumn
                {
                    Header = "Создан",
                    Path = "CREATED_MACHINE_NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 80,
                    MaxWidth = 80,
                },
                new DataGridHelperColumn
                {
                    Header = "IDR",
                    Path = "IDR",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width = 80,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header = "IDP",
                    Path = "IDP",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width = 80,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header = "PALLET_ID",
                    Path = "PALLET_ID",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width = 80,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header = "PALLET_NUMBER",
                    Path = "PALLET_NUMBER",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width = 80,
                    Hidden=true,
                },

                new DataGridHelperColumn
                {
                    Header = " ",
                    Path = " ",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 5,
                    MaxWidth = 2000,
                },

            };

            GridFrom.SetColumns(columns);
            GridFrom.SetSorting("NUM");

            GridFrom.Menu = new Dictionary<string, DataGridContextMenuItem>()
            {
                {
                    "MovingHistory",
                    new DataGridContextMenuItem()
                    {
                        Header="История перемещения",
                        Action=()=>
                        {
                            MovingHistory(GridFrom.SelectedItem);
                        },
                    }
                },
            };

            GridFrom.OnSelectItem = selectedItem =>
            {
                FromGridSelectedItem = selectedItem;
            };

            GridFrom.Init();
            GridFrom.Run();

            GridFrom.UpdateItems(FromDataSet);
        }

        private void ComplectationGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "Номер ПЗ",
                    Path = "NUM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 65,
                    MaxWidth = 75,
                },

                new DataGridHelperColumn
                {
                    Header = "ИД ПЗ",
                    Path = "ID_PZ",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 40,
                    MaxWidth = 55,
                },

                new DataGridHelperColumn
                {
                    Header = "Дата",
                    Path = "DT",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Format = "dd.MM.yyyy HH:mm:ss",
                    MinWidth = 70,
                    MaxWidth = 110,
                },
                new DataGridHelperColumn
                {
                    Header = "Смена",
                    Path = "SMNAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 50,
                    MaxWidth = 50,
                },
                new DataGridHelperColumn
                {
                    Header = "Комплектация",
                    Path = "COMPLECTATION",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 40,
                    MaxWidth = 80,
                },
                new DataGridHelperColumn
                {
                    Header = "Наименование",
                    Path = "CONSUMPTION_NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 180,
                    MaxWidth = 1600,
                },
                new DataGridHelperColumn
                {
                    Header = "Артикул",
                    Path = "CONSUMPTION_ARTIKUL",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 120,
                    MaxWidth = 120,
                },
                new DataGridHelperColumn
                {
                    Header = "Расход, шт.",
                    Path = "CONSUMPTION_QTY",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 30,
                    MaxWidth = 80,
                },
                new DataGridHelperColumn
                {
                    Header = "Приход, шт.",
                    Path = "INCOMING_QTY",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 35,
                    MaxWidth = 80,
                },
                new DataGridHelperColumn
                {
                    Header = "Брак, шт.",
                    Path = "DEFECTIVE_QUANTITY",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 35,
                    MaxWidth = 80,
                },
                new DataGridHelperColumn
                {
                    Header = "Заявка",
                    Path = "ORDER_NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 200,
                    MaxWidth = 300,
                },
                new DataGridHelperColumn
                {
                    Header = "ИД заявки",
                    Path = "NSTHET",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 50,
                    MaxWidth = 80,
                },
                new DataGridHelperColumn
                {
                    Header = "Причина",
                    Path = "CMPL_GROUP_NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 100,
                    MaxWidth = 200,
                },
                new DataGridHelperColumn
                {
                    Header = "Описание причины",
                    Path = "CMPL_REASON",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 50,
                    MaxWidth = 200,
                },
                new DataGridHelperColumn
                {
                    Header = "Мастер",
                    Path = "MASTER",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Boolean
                },
                new DataGridHelperColumn
                {
                    Header = "Старые ПЗ",
                    Path = "OLD_PZ_NUM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 50,
                    MaxWidth = 200,
                    Hidden = true,
                },

                new DataGridHelperColumn
                {
                    Header = "Старая продукция",
                    Path = "CONSUMPTION_PRODUCT_ID",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header = "Новая продукция",
                    Path = "INCOMING_PRODUCT_ID",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden = true,
                },
            };

            Grid.SetColumns(columns);
            Grid.SetSorting("DT", ListSortDirection.Descending);
            Grid.SearchText = SearchText;
            Grid.OnLoadItems = ComplectationLoadItems;
            Grid.OnSelectItem = item =>
            {
                SelectedItem = item;
                ComplectationDetailLoadItems();
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

                            switch (idSt.ToInt())
                            {
                                // Все участки
                                case -1:
                                    items = Grid.GridItems;
                                    break;

                                default:
                                    items.AddRange(Grid.GridItems.Where(row => row.CheckGet("ID_ST").ToInt() == idSt.ToInt()));
                                    break;
                            }

                            Grid.GridItems = items;
                        }

                        if (WorkTeamSelectBox.SelectedItem.Key != null)
                        {
                            var workTeamNumber = WorkTeamSelectBox.SelectedItem.Key;

                            var items = new List<Dictionary<string, string>>();

                            switch (workTeamNumber.ToInt())
                            {
                                // Все смены
                                case -1:
                                    items = Grid.GridItems;
                                    break;

                                default:
                                    items.AddRange(Grid.GridItems.Where(row => row.CheckGet("SMNAME").ToInt() == workTeamNumber.ToInt()));
                                    break;
                            }

                            Grid.GridItems = items;
                        }
                    }
                }
            };

            Grid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>
            {
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result = DependencyProperty.UnsetValue;
                        var color = "";

                        if (row.CheckGet("CONSUMPTION_PRODUCT_ID").ToInt() != row.CheckGet("INCOMING_PRODUCT_ID").ToInt())
                        {
                             color = HColor.Yellow;
                        }

                        if ((row.ContainsKey("INCOMING_QTY") && row["INCOMING_QTY"].ToInt() == 0) ||
                            (row.ContainsKey("CONSUMPTION_QTY") && row["CONSUMPTION_QTY"].ToInt() == 0)
                            )
                        {
                            color = HColor.Red;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result = color.ToBrush();

                        }
                        return result;
                    }
                },
            };           

            Grid.AutoUpdateInterval=0;
            Grid.Init();
            Grid.Run();
            Grid.Focus();
        }

        #endregion

        #region Load data

        private async void ComplectationDetailLoadItems()
        {
            GridFrom.ClearItems();
            GridTo.ClearItems();

            if (SelectedItem != null && SelectedItem.ContainsKey("ID_PZ"))
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Complectation");
                q.Request.SetParam("Object", "Operation");
                q.Request.SetParam("Action", "Get");
                q.Request.SetParam("id_pz", SelectedItem["ID_PZ"]);

                await Task.Run(() => { q.DoQuery(); });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                    if (result != null)
                    {
                        {
                            FromDataSet = ListDataSet.Create(result, "ListFrom");
                            ToDataSet = ListDataSet.Create(result, "ListTo");

                            GridFrom.UpdateItems(FromDataSet);
                            GridTo.UpdateItems(ToDataSet);
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        private async void ComplectationLoadItems()
        {
            Grid.ShowSplash();
            GridToolbar.IsEnabled = false;

            GridFromToolbar.IsEnabled = false;
            GridToToolbar.IsEnabled = false;

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

                GridFrom?.ClearItems();
                GridTo?.ClearItems();

                SelectedItem = null;
                ToGridSelectedItem = null;
                LabelPrintButton.IsEnabled = false;

                var p = new Dictionary<string, string>
                {
                    ["FromDate"] = Form.GetValueByPath("FROM_DATE"),
                    ["ToDate"] = Form.GetValueByPath("TO_DATE"),
                    ["FACTORY_ID"] = $"{FactoryId}"
                };

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Complectation");
                q.Request.SetParam("Object", "Operation");
                q.Request.SetParam("Action", "List");

                q.Request.SetParams(p);

                q.Request.Timeout = 30000;
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
                            ComplectationDataSet = ListDataSet.Create(result, "List");

                            if (ComplectationDataSet != null && ComplectationDataSet.Items.Count > 0)
                            {
                                foreach (var item in ComplectationDataSet.Items)
                                {
                                    int consumptionQuantity = item.CheckGet("CONSUMPTION_QTY").ToInt();
                                    int incomingQuantity = item.CheckGet("INCOMING_QTY").ToInt();
                                    int defectiveQuantity = consumptionQuantity - incomingQuantity;

                                    if (defectiveQuantity > 0)
                                    {
                                        item.CheckAdd("DEFECTIVE_QUANTITY", defectiveQuantity.ToString());
                                    }
                                    else
                                    {
                                        item.CheckAdd("DEFECTIVE_QUANTITY", "0");
                                    }
                                }
                            }

                            Grid.UpdateItems(ComplectationDataSet);
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            Grid.HideSplash();
            GridToolbar.IsEnabled = true;

            GridFromToolbar.IsEnabled = true;
            GridToToolbar.IsEnabled = true;
        }

        #endregion

        /// <summary>
        /// Получить историю перемещения по выбранному поддону
        /// </summary>
        public void MovingHistory(Dictionary<string, string> selectedItem)
        {
            if (selectedItem != null && selectedItem.Count > 0)
            {
                string incomingId = selectedItem.CheckGet("IDP");
                string palletNumber = selectedItem.CheckGet("PALLET_NUMBER");

                if (!string.IsNullOrEmpty(palletNumber) && !string.IsNullOrEmpty(incomingId))
                {
                    var p = new Dictionary<string, string>();
                    p.Add("idp", incomingId);
                    p.Add("num", palletNumber);

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Stock");
                    q.Request.SetParam("Object", "Pallet");
                    q.Request.SetParam("Action", "ListHistory");
                    q.Request.SetParams(p);
                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                        if (result != null)
                        {
                            DialogWindow.ShowDialog(result["List"].Rows.Select(row => row[0]).Aggregate(string.Empty, (row, record) => row + record + "\n"), $"История перемещения поддона {selectedItem.CheckGet("NUM")}");
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
            }
        }

        /// <summary>
        /// Открываем всплывающее окно для подтверждения отмены комплектации
        /// и, в случае подтверждения, вызываем функцию отмены комплектации
        /// </summary>
        public void ComplectationRemoveClick()
        {
            string msg = "Отменить выбранную комплектацию?";

            var d = new DialogWindow($"{msg}", "Список комплектаций", "", DialogWindowButtons.NoYes);
            if (d.ShowDialog() == true)
            {
                RemoveComplectation();
            }
        }

        /// <summary>
        /// Отмена выбранной комплектации
        /// </summary>
        public async void RemoveComplectation()
        {
            var p = new Dictionary<string, string>();

            // Тип комплектации:
            // 1 -- обычная комплектация
            // 2 -- списание (нет прихода новых поддонов)
            // 3 -- создание из воздуха (нет списания старых поддонов)
            int complectationType = 1;

            // Список Ид расхода (idr) поддонов, которые были списаны при комплектации
            string idrList = "";
            if (GridFrom.Items != null)
            {
                if (GridFrom.Items.Count > 0)
                {
                    foreach (var item in GridFrom.Items)
                    {
                        if (!string.IsNullOrEmpty(item.CheckGet("IDR")))
                        {
                            var str = $"{item.CheckGet("IDR")};";
                            idrList += str;
                        }
                    }

                    p.Add("IDR", idrList);
                }
                // если нет расхода поддонов, то это создание из воздуха
                else
                {
                    complectationType = 3;
                }
            }
            // если нет расхода поддонов, то это создание из воздуха
            else
            {
                complectationType = 3;
            }

            // Список Ид прихода (idp) поддонов, которые были созданы при комплектации
            string idpList = "";
            if (GridTo.Items != null)
            {
                if (GridTo.Items.Count > 0)
                {
                    foreach (var item in GridTo.Items)
                    {
                        if (!string.IsNullOrEmpty(item.CheckGet("IDP")))
                        {
                            var str = $"{item.CheckGet("IDP")};";
                            idpList += str;
                        }
                    }

                    p.Add("IDP", idpList);
                }
                // если нет прихода новых поддонов, то это списание
                else
                {
                    complectationType = 2;
                }
            }
            // если нет прихода новых поддонов, то это списание
            else
            {
                complectationType = 2;
            }

            if (SelectedItem != null)
            {
                p.Add("ID_ST", SelectedItem.CheckGet("ID_ST"));
            }

            p.Add("COMPLECTATION_TYPE", complectationType.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Complectation");
            q.Request.SetParam("Object", "Pallet");
            q.Request.SetParam("Action", "RollbackComplectationCM");

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

                    var rollbakResultPrihod = ds.Items.First().CheckGet("RESULTT_PRIHOD").ToInt();
                    var rollbakResultRashod = ds.Items.First().CheckGet("RESULTT_RASHOD").ToInt();

                    if (rollbakResultPrihod == 0 && rollbakResultRashod == 3)
                    {
                        var msg = "Успешная отмена комплектации";
                        var d = new DialogWindow($"{msg}", "Отмена комплектации", "", DialogWindowButtons.OK);
                        d.ShowDialog();

                        ComplectationLoadItems();
                    }
                    else if (rollbakResultPrihod == 0 && rollbakResultRashod == -1)
                    {
                        var msg = "Успешная отмена комплектации из воздуха";
                        var d = new DialogWindow($"{msg}", "Отмена комплектации", "", DialogWindowButtons.OK);
                        d.ShowDialog();

                        ComplectationLoadItems();
                    }
                    else if (rollbakResultPrihod == -1 && rollbakResultRashod == 3)
                    {
                        var msg = "Успешная отмена списания поддонов";
                        var d = new DialogWindow($"{msg}", "Отмена комплектации", "", DialogWindowButtons.OK);
                        d.ShowDialog();

                        ComplectationLoadItems();
                    }
                    else
                    {
                        var msg = "Возможна ошибка отмены комплектации. Пожалуйста сообщите о проблеме.";
                        var d = new DialogWindow($"{msg}", "Отмена комплектации", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }

                    if (ds.Items.First().CheckGet("ID_ST").ToInt() == 881)
                    {
                        // отправить сообщение списку доступных комплектаций на ГА КШ
                        Messenger.Default.Send(new ItemMessage
                        {
                            ReceiverGroup = "ComplectationKsh",
                            ReceiverName = "ComplectationCorrugatorKsh",
                            SenderName = this.FrameName,
                            Action = "Refresh",
                        });
                    }
                }
                else
                {
                    var msg = "Ошибка отмены комплектации";
                    var d = new DialogWindow($"{msg}", "Отмена комплектации", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Установка настроек для принтера
        /// </summary>
        public void SetPrintSettings()
        {
            LabelReport2.SetPrintingProfile();
        }

        private async void ExportToExcel()
        {
            var f = Form.GetValueByPath("FROM_DATE").ToDateTime();
            var t = Form.GetValueByPath("TO_DATE").ToDateTime();

            var eg = new ExcelGrid
            {
                Columns = new List<ExcelGridColumn>
                {
                    new ExcelGridColumn("DT", "Дата", 80, ExcelGridColumn.ColumnTypeRef.DateTime),
                    new ExcelGridColumn("Complectation".ToUpper(),"Комплектация", 150),
                    new ExcelGridColumn("consumption_name".ToUpper(),"Расход | наименование", 270),
                    new ExcelGridColumn("consumption_artikul".ToUpper(),"Расход | артикул", 150),
                    new ExcelGridColumn("consumption_qty".ToUpper(), "Расход | количество", 90, ExcelGridColumn.ColumnTypeRef.Integer),
                    new ExcelGridColumn("incoming_qty".ToUpper(), "Приход | количество", 90, ExcelGridColumn.ColumnTypeRef.Integer),

                    new ExcelGridColumn("CMPL_GROUP_NAME", "Причина", 270, ExcelGridColumn.ColumnTypeRef.String),
                    new ExcelGridColumn("CMPL_REASON", "Описание причины", 270, ExcelGridColumn.ColumnTypeRef.String),

                    new ExcelGridColumn("SMNAME", "Смена", 90, ExcelGridColumn.ColumnTypeRef.Integer),
                },
                Items = Grid.GridItems,
                GridTitle = "Отчет комплектации за период с " + f.ToString("dd.MM.yyyy") + " по " + t.ToString("dd.MM.yyyy")
            };


            await Task.Run(() =>
            {
                eg.Make();
            });

        }

        public void PrintLabel()
        {
            LabelReport2 report = new LabelReport2(true);
            report.PrintLabel(ToGridSelectedItem["PALLET_ID"]);
        }

        /// <summary>
        /// Изменить причину комплектации
        /// </summary>
        public void EditReason()
        {
            if (SelectedItem != null && SelectedItem.Count > 0)
            {
                var view = new ComplectationReasonsEdit();
                view.OldReasonId = SelectedItem.CheckGet("CMRE_ID").ToInt();
                switch (SelectedItem.CheckGet("ID_ST").ToInt())
                {
                    case 881:
                        view.CorrugatorFlag = 1;
                        break;

                    default:
                        break;
                }
                view.Show();

                if (view.OkFlag)
                {
                    string reasonId = view.SelectedReason.Key;
                    string reasonMessage = view.ReasonMessage;

                    var p = new Dictionary<string, string>();
                    p.Add("PRODUCTION_TASK_ID", SelectedItem.CheckGet("ID_PZ"));
                    p.Add("REASON_ID", reasonId);
                    p.Add("REASON_MESSAGE", reasonMessage);

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Complectation");
                    q.Request.SetParam("Object", "Operation");
                    q.Request.SetParam("Action", "UpdateReason");
                    q.Request.SetParams(p);

                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {
                        int productionTaskId = 0;

                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            var dataSet = ListDataSet.Create(result, "ITEMS");

                            if (dataSet != null && dataSet.Items.Count > 0)
                            {
                                productionTaskId = dataSet.Items.First().CheckGet("PRODUCTION_TASK_ID").ToInt();
                            }
                        }

                        if (productionTaskId > 0)
                        {
                            ComplectationLoadItems();

                            DialogWindow.ShowDialog("Успешное изменение причины комплектации");
                        }
                        else
                        {
                            DialogWindow.ShowDialog("Ошибка изменения причины комплектации");
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
                DialogWindow.ShowDialog("Не выбрана комплектация для изменения причины");
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void ExcelButton_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcel();
        }

        private void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            ComplectationLoadItems();
        }

        private void LabelPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintLabel();
        }

        private void RemoveComplectationButton_Click(object sender, RoutedEventArgs e)
        {
            ComplectationRemoveClick();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            SetPrintSettings(); 
        }

        private void ComplectationTypes_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void WorkTeamSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void EditReasonButton_Click(object sender, RoutedEventArgs e)
        {
            EditReason();
        }
    }
}
