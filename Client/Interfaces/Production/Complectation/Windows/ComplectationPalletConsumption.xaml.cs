using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
using Client.Interfaces.Production.Pillory;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static Client.Interfaces.Production.ComplectationMainComplectationTab;

namespace Client.Interfaces.Production.Complectation
{
    /// <summary>
    /// Списание поддонов из другой ячейки в брак комплектации
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class ComplectationPalletConsumption : ControlBase
    {
        public ComplectationPalletConsumption()
        {
            ControlTitle = "Списание из другой ячейки";
            DocumentationUrl = "/doc/l-pack-erp/";
            InitializeComponent();

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnKeyPressed = (KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };

            //конструктор, будет вызван, когда объект создается
            //здесь создаются все внутренние структуры
            //впервые этот коллбэк будет вызван, когда данный таб станет активным
            //впервые (до этих пор, никакая работа внутри не происходит, что экономит ресурсы)
            OnLoad = () =>
            {
                Commander.Init(this);

                SetDefaults();
                PalletGridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                PalletGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
            };

            {
                Commander.Add(new CommandItem()
                {
                    Name = "refresh",
                    Group = "main",
                    Enabled = true,
                    Title = "Обновить",
                    Description = "Обновить данные",
                    ButtonUse = true,
                    ButtonControl = RefreshButton,
                    ButtonName = "RefreshButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        Refresh();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "help",
                    Group = "main",
                    Enabled = true,
                    Title = "Справка",
                    Description = "Показать справочную информацию",
                    ButtonUse = true,
                    ButtonName = "HelpButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    HotKey = "F1",
                    Action = () =>
                    {
                        Central.ShowHelp(DocumentationUrl);
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "cancel",
                    Group = "main",
                    Enabled = true,
                    Title = "",
                    Description = "Отмена",
                    ButtonUse = true,
                    ButtonControl = CancelButton,
                    ButtonName = "CancelButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        Close();
                    },
                });
            }

            Commander.SetCurrentGridName("PalletGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "consumption_pallet",
                    Title = "Списать",
                    Group = "pallet_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = ConsumptionPalletButton,
                    ButtonName = "ConsumptionPalletButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        ConsumptionPallet();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (PalletGrid != null && PalletGrid.Items != null && PalletGrid.Items.Count > 0)
                        {
                            if (PalletGrid.SelectedItem != null && PalletGrid.SelectedItem.Count > 0)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
            }
        }

        /// <summary>
        /// Выбранный тип комплектации
        /// </summary>
        public ComplectationTypeRef ComplectationType { get; set; }

        /// <summary>
        /// Наименование таба, который вызвал этот таб
        /// </summary>
        public string ParentFrame { get; set; }

        /// <summary>
        /// Датасет с данными грида
        /// </summary>
        private ListDataSet PalletGridDataSet { get; set; }

        /// <summary>
        /// Список ячеек, поддоны в которых анализируются
        /// </summary>
        private List<Cell> CellList { get; set; }

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

            FrameName = $"{FrameName}";
            Dictionary<string, string> windowParametrs = new Dictionary<string, string>();
            windowParametrs.Add("no_resize", "1");
            windowParametrs.Add("center_screen", "1");
            this.MinHeight = 650;
            this.MinWidth = 1050;
            Central.WM.Show(FrameName, this.ControlTitle, true, "main", this, "top", windowParametrs);
        }

        private void SetDefaults()
        {
            PalletGridDataSet = new ListDataSet();

            CellList = new List<Cell>();
            var storageSelectBoxItems = new Dictionary<string, string>();
            switch (ComplectationType)
            {
                case ComplectationTypeRef.CorrugatingMachine:
                    break;
                case ComplectationTypeRef.ProcessingMachine:
                    break;
                case ComplectationTypeRef.Stock:
                    break;
                case ComplectationTypeRef.MoldedContainer:
                    break;
                case ComplectationTypeRef.CorrugatingMachineKsh:
                    {
                        CreateCellItem("БУФ", 0);
                    }
                    break;
                case ComplectationTypeRef.ProcessingMachineKsh:
                    {
                        CreateCellItem("АПС", 1);
                        CreateCellItem("АПС", 2);
                        CreateCellItem("ТПР", 1);
                        CreateCellItem("ТПР", 2);
                        CreateCellItem("ТПР", 3);
                        CreateCellItem("СРШ", 1);
                        CreateCellItem("ПЛВ", 1);
                    }
                    break;
                default:
                    break;
            }

            if (CellList.Count > 0)
            {
                foreach (var cell in CellList)
                {
                    storageSelectBoxItems.Add(cell.Name, cell.Name);
                }
            }
            StorageSelectBox.SetItems(storageSelectBoxItems);
            StorageSelectBox.SetSelectedItemFirst();
        }

        private void PalletGridInit()
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
                        Width2=3,
                        Editable = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид поддона",
                        Description = "Идентификатор поддона",
                        Path="PALLET_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Поддон",
                        Description = "Полный номер поддона",
                        Path="PALLET_FULL_NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество, шт.",
                        Description = "Количество продукции на поддоне",
                        Path="QUANTITY_ON_PALLET",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=6,
                        Format="N0",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Продукция",
                        Description = "Наименование продукции",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=40,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул",
                        Description = "Артикул продукции",
                        Path="PRODUCT_CODE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=15,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ячейка",
                        Description = "Местонахождение поддона",
                        Path="PLACE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=4,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Номер поддона",
                        Description = "Порядковый номер поддона",
                        Path="PALLET_NUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=4,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Задание",
                        Description = "Номер производственного задания",
                        Path="PRODUCTION_TASK_NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид прихода",
                        Description = "Идентификатор прихода",
                        Path="INCOMING_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=4,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид продукции",
                        Description = "Идентификатор продукции",
                        Path="PRODUCT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=4,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид категории продукции",
                        Description = "Идентификатор категории продукции",
                        Path="PRODUCT_CATEGORY_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=4,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид отдела",
                        Description = "Идентификатор отдела",
                        Path="DEPARTMENT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=4,
                        Hidden=true,
                    },
                };
                PalletGrid.SetColumns(columns);
                PalletGrid.SetPrimaryKey("PALLET_ID");
                PalletGrid.SetSorting("PRODUCT_NAME", System.ComponentModel.ListSortDirection.Ascending);
                PalletGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                PalletGrid.SearchText = SearchText;
                PalletGrid.AutoUpdateInterval = 0;
                PalletGrid.OnLoadItems = PalletGridLoadItems;
                PalletGrid.UseProgressSplashAuto = false;
                PalletGrid.Commands = this.Commander;
                PalletGrid.Init();
            }
        }

        private async void PalletGridLoadItems()
        {
            EnableSplash();

            if (StorageSelectBox.SelectedItem.Key != null)
            {
                var cell = CellList.FirstOrDefault(x => x.Name == StorageSelectBox.SelectedItem.Key);
                if (cell != null)
                {
                    var p = new Dictionary<string, string>();
                    p.Add("SKLAD", cell.CellName);
                    p.Add("NUM_PLACE", $"{cell.CellNumber}");

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Production/Pillory");
                    q.Request.SetParam("Object", "Pallet");
                    q.Request.SetParam("Action", "ListByCell");
                    q.Request.SetParams(p);
                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    PalletGridDataSet = new ListDataSet();
                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            PalletGridDataSet = ListDataSet.Create(result, "ITEMS");
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                    PalletGrid.UpdateItems(PalletGridDataSet);
                }
            }

            DisableSplash();
        }

        private void Refresh()
        {
            PalletGrid.LoadItems();
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        private void Close()
        {
            if (!string.IsNullOrEmpty(ParentFrame))
            {
                Central.WM.SetActive(ParentFrame, true);
            }

            Central.WM.Close(FrameName);

            //вся работа по утилизации ресурсов происходит в Destroy
            //он будет вызван при закрытии фрейма
        }

        private void EnableSplash()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                SplashControl.Visible = true;
            });
        }

        private void DisableSplash()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                SplashControl.Visible = false;
            });
        }

        private void CreateCellItem(string cellName, int cellNumber)
        {
            Cell cell = new Cell(cellName, cellNumber);
            CellList.Add(cell);
        }

        private async void ConsumptionPallet()
        {
            if (PalletGrid.SelectedItem != null && PalletGrid.SelectedItem.Count > 0)
            {
                var resume = true;

                // Список списываемых поддонов
                List<Dictionary<string, string>> consumptionItemList = new List<Dictionary<string, string>>();
                // Ид причины комплектации/списания
                var reasonId = "0";
                // Описание причины комплектации/списания
                var reasonMessage = "";

                string machineId = "";
                int reasonsCorrugatorFlag = 0;
                int reasonsConvertingFlag = 0;
                string queryAction = "";
                Dictionary<string, string> product = new Dictionary<string, string>();
                // Настраиваем в зависимости от участка комплектации
                {
                    switch (ComplectationType)
                    {
                        case ComplectationTypeRef.CorrugatingMachine:
                            break;
                        case ComplectationTypeRef.ProcessingMachine:
                            break;
                        case ComplectationTypeRef.Stock:
                            break;
                        case ComplectationTypeRef.MoldedContainer:
                            break;
                        case ComplectationTypeRef.CorrugatingMachineKsh:
                            machineId = ComplectationPlace.CorrugatingMachinesKsh;
                            reasonsCorrugatorFlag = 1;
                            queryAction = "CreateCorrugatingMachineKsh";
                            product.Add("IDK1", "4");
                            break;
                        case ComplectationTypeRef.ProcessingMachineKsh:
                            machineId = ComplectationPlace.ProcessingMachinesKsh;
                            reasonsConvertingFlag = 1;
                            queryAction = "CreateProcessingMachineKsh";
                            break;
                        default:
                            break;
                    }
                }

                List<Dictionary<string, string>> _consumptionItemList = new List<Dictionary<string, string>>();
                List<Dictionary<string, string>> selectedItemList = PalletGrid.GetItemsSelected();
                // Поулчаем список поддонов на списание
                if (selectedItemList != null && selectedItemList.Count > 0)
                {
                    _consumptionItemList = selectedItemList;
                }
                else
                {
                    _consumptionItemList.Add(PalletGrid.SelectedItem);
                }

                // Преобразуем данные к нужному формату
                if (_consumptionItemList != null && _consumptionItemList.Count > 0)
                {
                    foreach (var item in _consumptionItemList)
                    {
                        consumptionItemList.Add(new Dictionary<string, string>() {
                            {"ID2", item.CheckGet("PRODUCT_ID")},
                            {"IDK1", item.CheckGet("PRODUCT_CATEGORY_ID")},
                            {"KOL", item.CheckGet("QUANTITY_ON_PALLET").ToInt().ToString()},
                            {"IDP", item.CheckGet("INCOMING_ID")},
                            {"NUM", item.CheckGet("PALLET_NUMBER")},
                            {"IDPER", ""},
                            {"ID1", item.CheckGet("DEPARTMENT_ID")},
                        });
                    }
                }
                else
                {
                    resume = false;
                }

                // Запрашиваем подтверждение операции
                if (resume)
                {
                    var message =
                                $"Будет списано" +
                                $"{Environment.NewLine}{consumptionItemList.Sum(x => x.CheckGet("KOL").ToInt())} товара на {consumptionItemList.Count} поддонах" +
                                $"{Environment.NewLine}в брак." +
                                $"{Environment.NewLine}Продолжить?";

                    if (DialogWindow.ShowDialog(message, this.ControlTitle, "", DialogWindowButtons.YesNo) != true)
                    {
                        resume = false;
                    }
                }

                // Запрашиваем причину списания
                if (resume)
                {
                    var view = new ComplectationWriteOffReasonsEdit();
                    view.CorrugatorFlag = reasonsCorrugatorFlag;
                    view.ConvertingFlag = reasonsConvertingFlag;
                    view.Show();

                    if (view.OkFlag)
                    {
                        reasonId = view.SelectedReason.Key;
                    }

                    if (!(reasonId.ToInt() > 0))
                    {
                        resume = false;
                    }
                }

                // Списываем
                if (resume)
                {
                    EnableSplash();

                    var p = new Dictionary<string, string>();
                    p.Add("OldPalletList", JsonConvert.SerializeObject(consumptionItemList));
                    p.Add("idorderdates", "");
                    p.Add("ReasonId", reasonId);
                    p.Add("ReasonMessage", reasonMessage);
                    p.Add("StanokId", machineId);
                    p.Add("NewPalletList", JsonConvert.SerializeObject(new List<Dictionary<string, string>>()));
                    p.Add("Product", JsonConvert.SerializeObject(product));

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Complectation");
                    q.Request.SetParam("Object", "Pallet");
                    q.Request.SetParam("Action", queryAction);

                    q.Request.SetParams(p);

                    await Task.Run(() => { q.DoQuery(); });

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            if (ds != null && ds.Items != null && ds.Items.Count > 0)
                            {
                                DialogWindow.ShowDialog("Списание выполнено");

                                Close();
                            }
                            else
                            {
                                var msg = "На сервере произошла ошибка. Пожалуйста сообщите о проблеме.";
                                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                                d.ShowDialog();
                            }
                        }
                        else
                        {
                            var msg = "На сервере произошла ошибка. Пожалуйста сообщите о проблеме.";
                            var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
            }

            DisableSplash();
        }

        private void StorageSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Refresh();
        }
    }
}
