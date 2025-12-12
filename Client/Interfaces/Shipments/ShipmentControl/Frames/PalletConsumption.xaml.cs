using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Расход паллет
    /// <author>михеев</author>
    /// </summary>
    public partial class PalletConsumption
    {
        private Window Window { get; set; }

        private string _idts;
        public string IdTs
        {
            get => _idts;
            set
            {
                _idts = value;
                OperationsGrid.Run();

                var consumptionAlreadyExists = СonsumptionAlreadyExists();

                AmountTextBox.IsEnabled = !consumptionAlreadyExists;
                //AmountTextBox.ToolTip = AmountTextBox.IsEnabled ? string.Empty : "Документ уже проведен. Редактирование запрещено.";

                PrintButton.Content = consumptionAlreadyExists ? "Напечатать накладную" : "Провести и напечатать накладную";
            }
        }

        public PalletConsumption()
        {
            InitializeComponent();

            if (!Central.InDesignMode())
            {
                InitPositionsGrid();
                InitOperationGrid();
                PreviewKeyDown += ProcessKeyboard;
            }
        }


        private void InitOperationGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "№ накладной",
                    Path = "NUM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 100,
                    MaxWidth = 550,
                },
                new DataGridHelperColumn
                {
                    Header = " ",
                    Path = "_",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 0,
                    MaxWidth = 900,
                },
            };

            OperationsGrid.SetColumns(columns);
            OperationsGrid.SetSorting("NUM");
            OperationsGrid.Menu = new Dictionary<string, DataGridContextMenuItem>();


            OperationsGrid.OnLoadItems = LoadItems;
            OperationsGrid.OnSelectItem = selectedItem =>
            {
                SelectedItem = selectedItem;
                PositionsGrid.LoadItems();
            };

            OperationsGrid.Init();
            OperationsGrid.Run();
            OperationsGrid.Focus();
        }

        private void InitPositionsGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "Наименование",
                    Path = "NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 350,
                    MaxWidth = 550,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество, шт",
                    Path = "QTY",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 80,

                    MaxWidth = 200,
                    //Editable = true
                },
            };

            PositionsGrid.SetColumns(columns);
            PositionsGrid.SetSorting("NAME");
            PositionsGrid.Menu = new Dictionary<string, DataGridContextMenuItem>();

            PositionsGrid.OnLoadItems = PositionsLoadItems;
            PositionsGrid.OnSelectItem = selectedItem =>
            {
                if (selectedItem != null && selectedItem.ContainsKey("QTY"))
                {
                    AmountTextBox.Text = selectedItem["QTY"].ToInt().ToString();
                }
            };

            PositionsGrid.Init();
            PositionsGrid.Run();
            PositionsGrid.Focus();
        }

        private Dictionary<string, string> SelectedItem;

        private ListDataSet DataSet { get; set; }


        private async void LoadItems()
        {
            OperationsGrid.ClearItems();


            if (_idts != null)
            {

                OperationsGrid.ShowSplash();


                var p = new Dictionary<string, string>
                {
                    ["IdTs"] = _idts
                };


                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "Pallet");
                q.Request.SetParam("Action", "ListConsumption");

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
                        {
                            DataSet = ListDataSet.Create(result, "List");
                            OperationsGrid.UpdateItems(DataSet);
                        }
                    }
                }

                OperationsGrid.HideSplash();

            }
        }


        private async void PositionsLoadItems()
        {
            PositionsGrid.ClearItems();

            if (SelectedItem == null) return;

            PositionsGrid.ShowSplash();


            var p = new Dictionary<string, string>
            {
                ["plex_id"] = SelectedItem["PLEX_ID"]
            };

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Pallet");
            q.Request.SetParam("Action", "ListExpenditureItems");

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
                        PositionsGrid.UpdateItems(DataSet);
                    }
                }
            }

            PositionsGrid.HideSplash();
        }



        private void ProcessKeyboard(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F5:
                    OperationsGrid.LoadItems();
                    e.Handled = true;
                    break;

                case Key.F1:
                    //ShowHelp();
                    e.Handled = true;
                    break;

                case Key.Home:
                    OperationsGrid.SetSelectToFirstRow();
                    e.Handled = true;
                    break;

                case Key.End:
                    OperationsGrid.SetSelectToLastRow();
                    e.Handled = true;
                    break;
            }
        }


        public void Edit()
        {
            const string title = "Расход поддонов";

            var w = 550;
            var h = 450;

            Window = new Window
            {
                Title = title,
                Width = w + 17,
                Height = h + 40,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow,
                Content = new Frame
                {
                    Content = this,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                },
                //Topmost = true,
            };

            Window.ShowDialog();
        }


        private void PrintDocuments(List<Dictionary<string, string>> invoicePositionDataSet)
        {
            if (invoicePositionDataSet != null)
            {

                // печать счетов
                foreach (var position in invoicePositionDataSet)
                {
                    var positionListParams = new Dictionary<string, string>
                    {
                        ["nsthet"] = position["NSTHET"]
                    };

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Shipments");
                    q.Request.SetParam("Object", "Pallet");
                    q.Request.SetParam("Action", "ListConsumptionReport");
                    q.Request.SetParams(positionListParams);

                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {
                        var positionResult =
                            JsonConvert.DeserializeObject<Dictionary<string, List<Dictionary<string, string>>>>(
                                q.Answer.Data);

                        if (positionResult != null && positionResult.ContainsKey("List"))
                        {
                            var reporter = new PalletConsumptionInvoiceReporter
                            {
                                NamePok = position["NAME_POK"],
                                NameProd = position["NAME_PROD"],
                                Num = position["NUM"],
                                DT = position["DT"],
                                NDS = position["NDS"].ToInt().ToString(),
                                Barcode = position["BARCODE"],
                                Positions = positionResult["List"]
                            };

                            reporter.MakeReport();
                        }

                    }
                    else
                    {
                        q.ProcessError();
                    }

                    Messenger.Default.Send(new ItemMessage
                    {
                        ReceiverGroup = "ShipmentControl",
                        ReceiverName = "ShipmentList",
                        SenderName = "PalletConsumption",
                        Action = "Refresh",
                    });

                    break;
                }
            }

        }

        private List<Dictionary<string, string>> SaveDocumentsChanges()
        {
            List<Dictionary<string, string>> resultDocumentList = null;

            if (string.IsNullOrEmpty(SelectedItem["NUM"]) || SelectedItem["NUM"] == "0")
            {
                DialogWindow.ShowDialog(
                    @"Отсутствует номер у накладной, проведение и печать невозможны! Присвойте номер накладной, например, напечатав ее на готовую продукцию.",
                    "Ошибка проведения и печати");
            }
            else
            {
                // сохранить измененные строки
                // печать отчета

                var p = new Dictionary<string, string>
                {
                    {"idts", IdTs},
                    {"Positions", JsonConvert.SerializeObject(PositionsGrid.DataSet.Items)}
                };

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "Pallet");
                q.Request.SetParam("Action", "DoConsumption");
                q.Request.SetParams(p);

                q.DoQuery(); 

                // смогли сохранить изменения строк
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                    if (result != null)
                    {
                        resultDocumentList = ListDataSet.Create(result, "List").Items;

                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            return resultDocumentList;
        }

        private void PrintButton_OnClick(object sender, RoutedEventArgs e)
        {
            PrintDocuments(SaveDocumentsChanges());
        }

        /// <summary>
        /// Проверяем что расход для данного транспортного средства уже существует и проведен
        /// </summary>
        /// <returns></returns>
        private bool СonsumptionAlreadyExists()
        {
            var r = false;

            var p = new Dictionary<string, string>
            {
                ["idts"] = IdTs
            };

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Pallet");
            q.Request.SetParam("Action", "CheckConsumptionExist");

            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                if (result != null && result.ContainsKey("List"))
                {
                    var list = result["List"];
                    list.Init();

                    if (list.Items.Count > 0)
                    {
                        r = list.Items[0]["RETURNABLE_PDN_CHECK"] == "0";
                    }

                }
            }
            else
            {
                q.ProcessError();
            }

            return r;

        }



        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!СonsumptionAlreadyExists())
            {
                if (DialogWindow.ShowDialog(
                        "Для завершения проверки расхода паллетов распечатайте накладную. Вы хотите закрыть окно без завершения проверки?",
                        "Расход паллетов", "", DialogWindowButtons.YesNo) == true)
                {
                    Close();
                }
            }
            else
            {
                Close();
            }
        }


        private void Close()
        {
            Window?.Close();
        }

        private void AmountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var value = AmountTextBox.Text;


            //e.Handled = new Regex("^[0-9]+").IsMatch(value);

            if (PositionsGrid.SelectedItem != null)
            {
                if (int.TryParse(value, out var result) || value == string.Empty)
                {
                    if (result != 0)
                    {
                        PositionsGrid.SelectedItem["QTY"] = value;
                    }
                }

                PositionsGrid.UpdateGrid();
            }



        }


        private void AmountTextBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var value = e.Text;
            e.Handled = !new Regex("^[0-9]+").IsMatch(value);
        }
    }
}
