using Client.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Client.Assets.HighLighters;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using GalaSoft.MvvmLight.Messaging;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// ЭДО
    /// </summary>
    /// <author>Михеев И.С.</author>
    public partial class EdmSalesList : UserControl
    {
        public EdmSalesList()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this,ProcessMessages);

            //регистрация обработчика клавиатуры
            PreviewKeyDown+=ProcessKeyboard;

            SetDefaults();
            InitGrid();

            ProcessPermissions();
        }

        private ListDataSet DataSet { get; set; }
        private Dictionary<string, string> SelectedItem { get; set; }

        private string RoleName = "[erp]shipment_edm";

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            FromDate.Text = DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy");
            ToDate.Text = DateTime.Now.ToString("dd.MM.yyyy");

            var factorySelectBoxItems = new Dictionary<string, string>();
            factorySelectBoxItems.Add("1", "Л-ПАК ЛИПЕЦК");
            factorySelectBoxItems.Add("2", "Л-ПАК КАШИРА");
            FactorySelectBox.SetItems(factorySelectBoxItems);
            FactorySelectBox.SetSelectedItemByKey("1");

            var statusSelectList = new Dictionary<string, string>
            {
                {"-1", "все документы"},
                {"5", "в очереди" }, // на автовыгрузку
                {"1", "выгружен"},
                {"2", "отправлен"},
                {"3", "отклонен"},
                {"4", "получен"},
                {"9", "не выгруженные"},
                {"10", "не полученные"},
            };
            StatusComboBox.Items = statusSelectList;
            StatusComboBox.SelectedItem = statusSelectList.FirstOrDefault(x => x.Key == "-1");
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

        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "*",
                    Path = "_SELECTED",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth = 45,
                    MaxWidth = 45,
                    Editable = true,
                },
                new DataGridHelperColumn
                {
                    Header = "№ заявки",
                    Path = "NSTHET",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 70,
                    MaxWidth = 70,
                },
                new DataGridHelperColumn
                {
                    Header = "Дата СФ",
                    Path = "SFDATE",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Format = "dd.MM.yyyy",
                    MinWidth = 70,
                    MaxWidth = 70,
                },
                new DataGridHelperColumn
                {
                    Header = "№ СФ",
                    Path = "SFNUMBER",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 60,
                    MaxWidth = 60,
                },
                new DataGridHelperColumn
                {
                    Header = "Продавец",
                    Path = "BUYER",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 250,
                    MaxWidth = 390,
                    Width = 390,
                },
                new DataGridHelperColumn
                {
                    Header = "Покупатель",
                    Path = "SELLER",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 250,
                    MaxWidth = 800,
                    Width = 725,
                },
                new DataGridHelperColumn
                {
                    Header = "Сумма, руб",
                    Path = "TOTAL",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Double,
                    MinWidth = 80,
                    MaxWidth = 80,
                    Format = "N2"
                },
                new DataGridHelperColumn
                {
                    Header = "Статус",
                    Path = "EDMUPDTEXT",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 70,
                    MaxWidth = 70
                },
                new DataGridHelperColumn
                {
                    Header = $"Дата выгрузки{Environment.NewLine}Дата выгрузки в ЭДО",
                    Path = "EDM_UPLOAD_DTTM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.DateTime,
                    MinWidth = 120,
                    MaxWidth = 120,
                    Format = "dd.MM.yyyy HH:mm:ss",
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                // Если заполнена дата выгрузки документа в ЭДО
                                if (!string.IsNullOrEmpty(row.CheckGet("EDM_UPLOAD_DTTM")))
                                {
                                    // Если дата выгрузки в ЭДО раньше, чем дата последнего расхода
                                    if (row.CheckGet("EDM_UPLOAD_DTTM").ToDateTime("dd.MM.yyyy HH:mm:ss") < row.CheckGet("LAST_CONSUMPTION_DTTM").ToDateTime("dd.MM.yyyy HH:mm:ss"))
                                    {
                                        color = HColor.Red;
                                    }
                                    // Если дата выгрузки в ЭДО раньше, чем дата печати документов
                                    else if (row.CheckGet("EDM_UPLOAD_DTTM").ToDateTime("dd.MM.yyyy HH:mm:ss") < row.CheckGet("DOCUMENT_PRINTING_DTTM").ToDateTime("dd.MM.yyyy HH:mm:ss"))
                                    {
                                        color = HColor.Orange;
                                    }
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
                    Header = $"Дата расхода{Environment.NewLine}Дата последнего расхода",
                    Path = "LAST_CONSUMPTION_DTTM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.DateTime,
                    MinWidth = 120,
                    MaxWidth = 120,
                    Format = "dd.MM.yyyy HH:mm:ss",
                },
                new DataGridHelperColumn
                {
                    Header = $"Дата печати{Environment.NewLine}Дата печати документов",
                    Path = "DOCUMENT_PRINTING_DTTM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.DateTime,
                    MinWidth = 120,
                    MaxWidth = 120,
                    Format = "dd.MM.yyyy HH:mm:ss",
                },

                new DataGridHelperColumn
                {
                    Header="Ид площадки",
                    Path="FACTORY_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };

            Grid.SetColumns(columns);
            Grid.SetSorting("NAME");
            Grid.Menu = new Dictionary<string, DataGridContextMenuItem>
            {
                {
                    "CancelDocumentExport",
                    new DataGridContextMenuItem
                    {
                        Header = "Отменить выгрузку",
                        Tag = "access_mode_full_access",
                        Action = CancelDocumentExport,
                    }
                },
                {
                    "ExportDocumentList",
                    new DataGridContextMenuItem
                    {
                        Header = "Выгрузить выбранные документы",
                        Tag = "access_mode_full_access",
                        Action = ExportDocumentList,
                    }
                },
                { "s0", new DataGridContextMenuItem(){
                    Header="-",
                }},
                {
                    "DownloadDocumentList",
                    new DataGridContextMenuItem
                    {
                        Header = "Скачать выбранные документы",
                        Tag = "access_mode_full_access",
                        Action = DownloadDocumentList,
                    }
                }
            };

            Grid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>
            {
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        if(row.CheckGet("EDMUPD").ToInt() != 0)
                        {
                            color = HColor.Yellow;

                            if(row.CheckGet("EDMUPDTEXT").ToLower() == "получен")
                            {
                                color = HColor.Green;
                            }
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                }
            };

            Grid.SearchText = SearchText;

            Grid.OnLoadItems = LoadItems;
            Grid.OnFilterItems = OnFilterItems;

            Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdateActions(selectedItem);
                }
            };

            Grid.Init();
            Grid.Run();
            Grid.Focus();
            Grid.Sort("NSTHET", ListSortDirection.Descending);
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        private void ProcessMessages(ItemMessage m)
        {
        }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        private void ProcessKeyboard(object sender,System.Windows.Input.KeyEventArgs e)
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
        /// отображение статьи в справочной системе
        /// </summary>
        private void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp-new/Sales/edo");
            //Central.ShowHelp("/doc/l-pack-erp/shipments/edm");
        }

        /// <summary>
        /// деструктор интерфейса
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="Edm",
                ReceiverName = "",
                SenderName = "EdmSalesList",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            Grid.Destruct();
        }

        
        private async void LoadItems()
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

            Grid.ShowSplash();
            GridToolbar.IsEnabled = false;

            var p = new Dictionary<string, string>
            {
                ["FromDate"] = FromDate.Text,
                ["ToDate"] = ToDate.Text,
            };

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Shipment");
            //FIXME: rename action: EdmList -> List*
            q.Request.SetParam("Action", "EdmList");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() => { q.DoQuery(); });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                if (result != null)
                {
                    {
                        DataSet = ListDataSet.Create(result, "List");
                        Grid.UpdateItems(DataSet);
                    }
                }
            }

            Grid.HideSplash();
            GridToolbar.IsEnabled = true;
        }

        /// <summary>
        /// обновление методов работы с выбранной записью
        /// </summary>
        private void UpdateActions(Dictionary<string, string> selectedItem)
        {
            SelectedItem = selectedItem;

            Grid.Menu["CancelDocumentExport"].Enabled = selectedItem != null && selectedItem["EDMUPD"].ToInt() != 0;

            {
                if (Grid.Items.Count(x => x.CheckGet("_SELECTED").ToBool() && x["EDMUPD"].ToInt() == 0) > 0
                    || (selectedItem != null && selectedItem["EDMUPD"].ToInt() == 0))
                {
                    Grid.Menu["ExportDocumentList"].Enabled = true;
                    ExportDocumentListButton.IsEnabled = true;
  
                }
                else
                {
                    Grid.Menu["ExportDocumentList"].Enabled = false;
                    ExportDocumentListButton.IsEnabled = false;
                }
            }

            {
                if (Grid.Items.Count(x => x.CheckGet("_SELECTED").ToBool()) > 0
                    || selectedItem != null)
                {
                    Grid.Menu["DownloadDocumentList"].Enabled = true;
                    DownloadDocumentListButton.IsEnabled = true;
                }
                else
                {
                    Grid.Menu["DownloadDocumentList"].Enabled = false;
                    DownloadDocumentListButton.IsEnabled = false;
                }
            }
           
            ProcessPermissions();
        }

        private void SelectAllItems()
        {
            var selected = false;
            if (SelectAllCheckBox.IsChecked != null && (bool)SelectAllCheckBox.IsChecked)
            {
                selected = true;
            }

            if (Grid.Items?.Count > 0)
            {
                foreach (var item in Grid.Items)
                {
                    item.CheckAdd("_SELECTED", "0");

                    if (selected)
                    {
                        item.CheckAdd("_SELECTED", "1");
                    }
                }

                Grid.UpdateItems();
            }
        }

        private void SelectUnuploadedItems()
        {
            var selected = false;
            if (SelectUnuploadedCheckBox.IsChecked != null && (bool)SelectUnuploadedCheckBox.IsChecked)
            {
                selected = true;
            }

            if (Grid.Items?.Count > 0)
            {
                foreach (var item in Grid.Items)
                {
                    item.CheckAdd("_SELECTED", "0");

                    if (item["EDMUPD"].ToInt() == 0 && selected)
                    {
                        item.CheckAdd("_SELECTED", "1");
                    }
                }

                Grid.UpdateItems();
            }
        }

        private void OnFilterItems()
        {
            if (Grid.GridItems != null && Grid.GridItems.Count > 0)
            {
                // Фильтрация по площадке
                {
                    if (FactorySelectBox.SelectedItem.Key != null)
                    {
                        var key = FactorySelectBox.SelectedItem.Key.ToInt();
                        var items = new List<Dictionary<string, string>>();

                        items.AddRange(Grid.GridItems.Where(x => x.CheckGet("FACTORY_ID").ToInt() == key));

                        Grid.GridItems = items;
                    }
                }

                // Фильтрация по статусу
                {
                    var doFilteringByStatus = false;
                    var status = -1;
                    if (StatusComboBox.SelectedItem.Key != null)
                    {
                        status = StatusComboBox.SelectedItem.Key.ToInt();
                        if (status > 0)
                        {
                            doFilteringByStatus = true;
                        }
                    }

                    if (doFilteringByStatus)
                    {
                        var items = new List<Dictionary<string, string>>();
                        foreach (var row in Grid.GridItems)
                        {
                            var statusResult = status == row["EDMUPD"].ToInt() ||
                                               status == 9 && row["EDMUPD"].ToInt() == 0 ||
                                               status == 10 && row["EDMUPD"].ToInt() != 4;

                            if (statusResult)
                            {
                                items.Add(row);
                            }
                        }

                        Grid.GridItems = items;
                    }
                }

                // Фильтрация по признаку На перевыгрузку
                {
                    if (ReUploadCheckBox.IsChecked == true)
                    {
                        var items = new List<Dictionary<string, string>>();

                        items.AddRange(Grid.GridItems.Where(row => !string.IsNullOrEmpty(row.CheckGet("EDM_UPLOAD_DTTM")) && row.CheckGet("EDM_UPLOAD_DTTM").ToDateTime("dd.MM.yyyy HH:mm:ss") < row.CheckGet("LAST_CONSUMPTION_DTTM").ToDateTime("dd.MM.yyyy HH:mm:ss")));

                        Grid.GridItems = items;
                    }
                }
            }
        }

        private async void ExportToExcel()
        {
            var eg = new ExcelGrid
            {
                Columns = new List<ExcelGridColumn>
                {
                    new ExcelGridColumn("NSTHET", "№ заявки", 70),
                    new ExcelGridColumn("SFDATE", "Дата отгрузки", 80, ExcelGridColumn.ColumnTypeRef.DateTime),
                    new ExcelGridColumn("SFNUMBER","№ СФ", 60),
                    new ExcelGridColumn("BUYER", "Продавец", 300),
                    new ExcelGridColumn("SELLER", "Покупатель", 300),
                    new ExcelGridColumn("TOTAL", "Сумма (руб.)", 90, ExcelGridColumn.ColumnTypeRef.Double),
                    new ExcelGridColumn("EDMUPDTEXT", "Статус", 100),
                },
                Items = Grid.GridItems,
                GridTitle = "Отчет ЭДО на " + DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss")
            };

            await Task.Run(() =>
            {
                eg.Make();
            });
        }

        private async void ExportDocumentList()
        {
            var list = new List<Dictionary<string, string>>(Grid.Items.Where(x => x.CheckGet("_SELECTED").ToInt() == 1));
            if (list.Count == 0 && SelectedItem != null)
            {
                list.Add(SelectedItem);
            }

            {
                var d = new DialogWindow($"Выгрузить {list.Count} документов?", "Проверка данных", "", DialogWindowButtons.YesNo);
                if (d.ShowDialog() != true)
                {
                    return;
                }
            }

            foreach (var row in list)
            {
                if (row["EDMUPD"].ToInt() == 0)
                {
                    // выгружаем УПД по накладной
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Shipments");
                    q.Request.SetParam("Object", "Shipment");
                    q.Request.SetParam("Action", "UploadUPD");

                    q.Request.SetParam("NSTHET", row["NSTHET"]);
                    q.Request.SetParam("LOGIN", Central.User.Login);

                    await Task.Run(() => { q.DoQuery(); });

                    if (q.Answer.Status != 0)
                    {
                        q.ProcessError();
                        break;
                    }
                }
            }

            Grid.LoadItems();
        }

        private async void CancelDocumentExport()
        {
            if (DialogWindow.ShowDialog("Отменить выгрузку документа?", "Выгрузка документов", "", DialogWindowButtons.YesNo) == true)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "Shipment");
                q.Request.SetParam("Action", "UpdEdmUpd");

                q.Request.SetParam("operationId", SelectedItem["NSTHET"]);
                q.Request.SetParam("flag", null);

                await Task.Run(() => { q.DoQuery(); });

                if (q.Answer.Status == 0)
                {
                    LoadItems();
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        private async void DownloadDocumentList()
        {
            var list = new List<Dictionary<string, string>>(Grid.Items.Where(x => x.CheckGet("_SELECTED").ToInt() == 1));
            if (list.Count == 0 && SelectedItem != null)
            {
                list.Add(SelectedItem);
            }

            {
                var d = new DialogWindow($"Скачать {list.Count} документов?", "Проверка данных", "", DialogWindowButtons.YesNo);
                if (d.ShowDialog() != true)
                {
                    return;
                }
            }

            foreach (var row in list)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "Edm");
                q.Request.SetParam("Action", "DownloadInvoiceFile");

                q.Request.SetParam("INVOICE_ID", row["NSTHET"]);

                await Task.Run(() => 
                { 
                    q.DoQuery(); 
                });

                if (q.Answer.Status == 0)
                {
                    if (q.Answer.Type == LPackClientAnswer.AnswerTypeRef.File)
                    {
                        row.CheckAdd("_SELECTED", "0");
                        Central.SaveFile(q.Answer.DownloadFilePath, false, q.Answer.DownloadFileOriginalName);
                    }
                    else
                    {
                        q.Answer.Status = 145;
                        q.Answer.Error.Message = "Неверный тип ответа";
                        q.Answer.Error.Description = $"Получен тип ответа [{q.Answer.Type.ToString()}], а ожидался LPackClientAnswer.AnswerTypeRef.File [{LPackClientAnswer.AnswerTypeRef.File.ToString()}]";
                        q.ProcessError();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            Grid.UpdateItems();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            LoadItems();
        }

        private void ExportToExcelButton_OnClick(object sender, RoutedEventArgs e)
        {
            ExportToExcel();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (SelectUnuploadedCheckBox.IsChecked != null)
            {
                SelectUnuploadedCheckBox.IsChecked = false;
            }

            SelectAllItems();
        }

        private void ExportDocumentList_OnClick(object sender, RoutedEventArgs e)
        {
            ExportDocumentList();
        }

        private void DownloadDocumentList_OnClick(object sender, RoutedEventArgs e)
        {
            DownloadDocumentList();
        }

        /// <summary>
        /// вызывается при выборе значения в фильтре
        /// </summary>
        private void StatusComboBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void FactorySelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Grid.UpdateItems();
        }

        private void SelectUnuploadedCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (SelectAllCheckBox.IsChecked != null)
            {
                SelectAllCheckBox.IsChecked = false;
            }

            SelectUnuploadedItems();
        }

        private void ReUploadCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Grid.UpdateItems();
        }
    }
}
