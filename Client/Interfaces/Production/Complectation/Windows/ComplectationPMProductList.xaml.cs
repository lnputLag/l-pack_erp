using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Stock;
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

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Окно выбора продукции для комплектации из воздуха на переработке
    /// </summary>
    public partial class ComplectationPMProductList : UserControl
    {
        public ComplectationPMProductList(bool masterRigth = false)
        {
            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            InitializeComponent();

            FrameName = "ComplectationPMProductList";

            MasterRigth = masterRigth;

            InitGrid();

            InitNewPalletGrid();

            SetDefaults();
        }

        public int FactoryId { get; set; }

        /// <summary>
        /// Выделеная запись в гриде товаров
        /// </summary>
        public Dictionary<string, string> SelectedItemProductPM { get; set; }

        /// <summary>
        /// Датасет с данными по товарам
        /// </summary>
        public ListDataSet ProductPMDataSet { get; set; }

        /// <summary>
        /// Выделеная запись в гриде создаваемых поддонов
        /// </summary>
        public Dictionary<string, string> SelectedItemNewPalletPM { get; set; }

        /// <summary>
        /// Датасет с данными по создаваемым поддонам
        /// </summary>
        public ListDataSet NewPalletPMDataSet { get; set; }

        public string FrameName { get; set; }

        /// <summary>
        /// Флаг того, что есть особые права
        /// </summary>
        public bool MasterRigth { get; set; }

        public void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "ИД",
                    Path = "ID2",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width = 55,
                },
                new DataGridHelperColumn
                {
                    Header = "Наименование",
                    Path = "NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width = 330,
                },
                new DataGridHelperColumn
                {
                    Header = "Артикул",
                    Path = "ARTIKUL",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width = 130,
                },


                new DataGridHelperColumn
                {
                    Header = "Количество, шт.",
                    Path = "KOL_PAK",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 50,
                    MaxWidth = 120,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header = "Поддонов, шт.",
                    Path = "PALLET_COUNT",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 50,
                    MaxWidth = 120,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                Header = " ",
                Path = "_",
                ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                MinWidth = 5,
                MaxWidth = 2000,
                },
            };

            ProductPMGrid.SetColumns(columns);
            ProductPMGrid.SetSorting("NAME");

            ProductPMGrid.OnLoadItems = LoadProductItems;

            ProductPMGrid.OnSelectItem = selectedItem =>
            {
                if (selectedItem != null)
                {
                    ProductPMGrid.SelectedItem = selectedItem;
                    SelectedItemProductPM = selectedItem;
                }

                UpdateButtons();
            };

            ProductPMGrid.AutoUpdateInterval = 0;
            ProductPMGrid.Init();
            ProductPMGrid.Run();
        }

        public void InitNewPalletGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "№ поддона",
                    Path = "PODDON_NUMBER",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
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
                    //Editable = true,

                },
                new DataGridHelperColumn
                {
                    Header = " ",
                    Path = "_",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 5,
                    MaxWidth = 2000,
                },
            };

            NewPalletPMGrid.SetColumns(columns);
            NewPalletPMGrid.SetSorting("PODDON_NUMBER");

            NewPalletPMGrid.Menu = new Dictionary<string, DataGridContextMenuItem>();

            NewPalletPMGrid.OnSelectItem = selectedItem => { SelectedItemNewPalletPM = selectedItem; };

            NewPalletPMGrid.Init();
            NewPalletPMGrid.Run();

            NewPalletPMGrid.UpdateItems(NewPalletPMDataSet);
        }

        public async void LoadProductItems()
        {
            ProductPMGridToolbar.IsEnabled = false;
            ProductPMGrid.ShowSplash();

            ProductPMGrid.ClearItems();

            var searchStr = ProductPMSearchText.Text.Trim();

            if (searchStr.Length >= 3)
            {
                var p = new Dictionary<string, string>
                {
                    ["searchString"] = searchStr,

                    // Если false (MasterRigth=true), то в ответ вернётся весь список продукций (для программистрв и мастеров СГП, ГА и Переработки)
                    // ВРЕМЕННО ДОБАВЛЕНЫ ПРАВА МАСТЕРА ДЛЯ ОБЩЕЙ УЧЁТНОЙ ЗАПИСИ КОМПЛЕКТАЦИИ НА ПЕРЕРАБОТКЕ (до тех пор, пока не заработает переборка)
                    ["onlyExistInStock"] = (!MasterRigth).ToString(),

                    ["FACTORY_ID"] = $"{FactoryId}",
                };

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Complectation");
                q.Request.SetParam("Object", "Product");
                q.Request.SetParam("Action", "ListStock");

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
                        ProductPMDataSet = ListDataSet.Create(result, "List");
                        ProductPMGrid.UpdateItems(ProductPMDataSet);
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            ProductPMGrid.HideSplash();
            ProductPMGridToolbar.IsEnabled = true;
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
            Central.WM.FrameMode = 2;

            Central.WM.Show(FrameName, "Комплектация из воздуха", true, "add", this);
        }

        public void SetDefaults()
        {
            NewPalletPMDataSet = new ListDataSet();
            ProductPMDataSet = new ListDataSet();

            ProductPMSearchText.Clear();

            if (ProductPMGrid.Items != null)
            {
                ProductPMGrid.Items.Clear();

                ProductPMGrid.UpdateItems(ProductPMDataSet);
            }

            if (NewPalletPMGrid.Items != null)
            {
                NewPalletPMGrid.Items.Clear();

                NewPalletPMGrid.UpdateItems(NewPalletPMDataSet);
            }
            
            UpdateButtons();
        }

        /// <summary>
        /// Обновление активности кнопок
        /// </summary>
        public void UpdateButtons()
        {
            if (ProductPMDataSet.Items != null)
            {
                if (ProductPMDataSet.Items.Count > 0)
                {
                    if (SelectedItemProductPM != null)
                    {
                        AddButton.IsEnabled = true;

                        if (NewPalletPMGrid.Items != null)
                        {
                            if (NewPalletPMGrid.Items.Count > 0)
                            {
                                ComplectationButton.IsEnabled = true;

                                if (SelectedItemNewPalletPM != null)
                                {
                                    EditButton.IsEnabled = true;
                                    DeleteButton.IsEnabled = true;
                                }
                                else
                                {
                                    EditButton.IsEnabled = false;
                                    DeleteButton.IsEnabled = false;
                                }
                            }
                            else
                            {
                                ComplectationButton.IsEnabled = false;
                                EditButton.IsEnabled = false;
                                DeleteButton.IsEnabled = false;
                            }
                        }
                        else
                        {
                            ComplectationButton.IsEnabled = false;
                            EditButton.IsEnabled = false;
                            DeleteButton.IsEnabled = false;
                        }
                    }
                    else
                    {
                        AddButton.IsEnabled = false;
                        ComplectationButton.IsEnabled = false;
                        EditButton.IsEnabled = false;
                        DeleteButton.IsEnabled = false;
                    }
                }
                else
                {
                    AddButton.IsEnabled = false;
                    ComplectationButton.IsEnabled = false;
                    EditButton.IsEnabled = false;
                    DeleteButton.IsEnabled = false;
                }
            }

        }

        public void AddPallet()
        {
            var numberEditView = new ComplectationNumberEdit();
            numberEditView.Show();

            if (numberEditView.OkFlag)
            {
                var n = NewPalletPMDataSet.Items.Count + 1;

                var item = new Dictionary<string, string>
                {
                    ["PODDON_NUMBER"] = n.ToString(),
                    ["QTY"] = numberEditView.Value.ToString()
                };

                NewPalletPMDataSet.Items.Add(item);
                NewPalletPMGrid.UpdateItems(NewPalletPMDataSet);

                UpdateButtons();
            }
        }

        public void DeletePallet()
        {
            NewPalletPMDataSet.Items.Remove(SelectedItemNewPalletPM);
            SelectedItemNewPalletPM = null;

            var n = 1;

            foreach (var item in NewPalletPMDataSet.Items)
            {
                item["PODDON_NUMBER"] = n.ToString();
                n++;
            }

            NewPalletPMGrid.UpdateItems(NewPalletPMDataSet);

            UpdateButtons();
        }

        public void EditPallet()
        {
            var currentPoddonNumber = SelectedItemNewPalletPM["PODDON_NUMBER"];
            var currentQty = SelectedItemNewPalletPM["QTY"];

            var numberEditView = new ComplectationNumberEdit { Value = currentQty.ToInt() };

            numberEditView.Show();

            if (numberEditView.OkFlag)
            {
                SelectedItemNewPalletPM["QTY"] = numberEditView.Value.ToString();
                NewPalletPMGrid.UpdateItems(NewPalletPMDataSet);
                NewPalletPMGrid.SelectRowByKey(currentPoddonNumber.ToInt(), "PODDON_NUMBER");

                UpdateButtons();
            }
        }

        public async void ComplectationPMNewPallet()
        {
            var newPalletCount = NewPalletPMDataSet.Items.Count;

            var newPalletSum = NewPalletPMDataSet.Items.Sum(x => x.CheckGet("QTY").ToInt());

            var palletList = new List<Dictionary<string, string>>();

            var isOk = true;

            isOk = DialogWindow.ShowDialog(
                       "Вы мастер Переработки. Внимание вы комплектуете товара больше чем было до комплектации. Продолжить?",
                       "Предупреждение", "", DialogWindowButtons.NoYes) == true;

            if (isOk)
            {
                var message = $"Будет списано 0 товара на 0 поддонах и скомплектовано {newPalletSum} товара на {newPalletCount} поддонах";

                message += ". Продолжить?";

                if (DialogWindow.ShowDialog(message, "Предупреждение", "", DialogWindowButtons.YesNo) != true)
                {
                    isOk = false;
                }
            }

            var reasonId = "0";
            var reasonMessage = "";

            if (isOk)
            {
                var view = new ComplectationReasonsEdit();
                view.ConvertingFlag = 1;
                view.Show();

                if (view.OkFlag)
                {
                    reasonId = view.SelectedReason.Key;
                    reasonMessage = view.ReasonMessage;
                }
                else
                {
                    isOk = false;
                }
            }

            if (isOk)
            {
                ProductPMGrid.ShowSplash();
                NewPalletPMGrid.ShowSplash();

                ProductPMGridToolbar.IsEnabled = false;
                NewPalletPMGridToolbar.IsEnabled = false;
                ComplectationPMGridToolbar.IsEnabled = false;

                var p = new Dictionary<string, string>
                {
                    ["Product"] = JsonConvert.SerializeObject(SelectedItemProductPM),
                    ["OldPalletList"] = JsonConvert.SerializeObject(palletList),// JsonConvert.SerializeObject(palletList),
                    ["NewPalletList"] = JsonConvert.SerializeObject(NewPalletPMDataSet.Items),

                    //["idorderdates"] = SelectedProductItem.CheckGet("IDORDERDATES"),
                    ["idorderdates"] = null,// SelectedItemFrom.CheckGet("IDORDERDATES"),

                    ["ReasonId"] = reasonId,
                    ["ReasonMessage"] = reasonMessage
                };

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Complectation");
                q.Request.SetParam("Object", "Pallet");

                if (FactoryId == 2)
                {
                    p["StanokId"] = ComplectationPlace.ProcessingMachinesKsh;
                    q.Request.SetParam("Action", "CreateProcessingMachineKsh");
                }
                else
                {
                    p["StanokId"] = ComplectationPlace.ProcessingMachines;
                    q.Request.SetParam("Action", "CreateConversion");
                }

                q.Request.SetParams(p);

                await Task.Run(() => { q.DoQuery(); });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        var idpz = ds.Items.First().CheckGet("idpz");

                        if (idpz != "0")
                        {
                            // печать ярлыков
                            for (var i = 1; i <= newPalletCount; i++)
                            {
                                LabelReport2 report = new LabelReport2(true);
                                report.PrintLabel(idpz, i.ToString(), SelectedItemProductPM["IDK1"]);
                            }

                            if (FactoryId == 2)
                            {
                                // отправить сообщение
                                Messenger.Default.Send(new ItemMessage
                                {
                                    ReceiverGroup = "ComplectationKsh",
                                    ReceiverName = "ComplectationListKsh",
                                    SenderName = "ComplectationMainComplectationTab",
                                    Action = "Refresh",
                                });

                                // отправить сообщение
                                Messenger.Default.Send(new ItemMessage
                                {
                                    ReceiverGroup = "ComplectationKsh",
                                    ReceiverName = "ComplectationProcessingKsh",
                                    SenderName = "ComplectationMainComplectationTab",
                                    Action = "Refresh",
                                });
                            }
                            else
                            {
                                //// отправить сообщение списку списаний на сгп обновиться текущей датой
                                Messenger.Default.Send(new ItemMessage
                                {
                                    ReceiverGroup = "Complectation",
                                    ReceiverName = "Stock",
                                    SenderName = "ComplectationPMProductList",
                                    Action = "Refresh",
                                });

                                Messenger.Default.Send(new ItemMessage
                                {
                                    ReceiverGroup = "Complectation",
                                    ReceiverName = "PM",
                                    SenderName = "ComplectationPMProductList",
                                    Action = "Refresh",
                                });
                            }
                        }
                        else
                        {
                            DialogWindow.ShowDialog("Ошибка комплектации");
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }

                SetDefaults();

                ProductPMGrid.HideSplash();
                NewPalletPMGrid.HideSplash();

                ProductPMGridToolbar.IsEnabled = true;
                NewPalletPMGridToolbar.IsEnabled = true;
                ComplectationPMGridToolbar.IsEnabled = true;
            }
        }

        /// <summary>
        /// Установка настроек для принтера
        /// </summary>
        public void SetPrintSettings()
        {
            LabelReport2.SetPrintingProfile();
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m">Сообщение</param>
        private void ProcessMessages(ItemMessage m)
        {

        }

        private void ProductSearchText_TextChanged(object sender, TextChangedEventArgs e)
        {
            ShowButton.IsEnabled = ProductPMSearchText.Text.Trim().Length >= 3;
        }

        private void ProductSearchText_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoadProductItems();
            }
        }

        private void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            LoadProductItems();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddPallet();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            EditPallet();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            DeletePallet();
        }

        private void ComplectationButton_Click(object sender, RoutedEventArgs e)
        {
            ComplectationPMNewPallet();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            SetPrintSettings();
        }
    }
}
