using Client.Common;
using Client.Interfaces.Main;
using DevExpress.Mvvm.Native;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Интерфейс для создания расхода находится во вкладке "Расход оснастки".
    /// </summary>
    public partial class ConsumptionCreateFrame : ControlBase, INotifyPropertyChanged
    {
        public ConsumptionCreateFrame()
        {
            InitializeComponent();

            RoleName = "[erp]rig_movement";

            FrameMode = 0;

            OnGetFrameTitle = () =>
            {
                var result = "";
                var selected = StoreId;

                if (selected.Length != 0)
                {
                    result = "Добавление расхода";
                }

                return result;
            };
            
            OnLoad = () =>
            {
                FormInit();
                BuyerLoadItems();
                GridMainInit();
                GridTabInit();
                SetDefault();
            };

            OnUnload = () =>
            {
                GridBoxTab.Destruct();
                GridBoxMain.Destruct();
            };

            OnFocusGot = () =>
            {
                GridBoxTab.ItemsAutoUpdate = true;
                GridBoxMain.ItemsAutoUpdate = true;
            };

            OnFocusLost = () =>
            {
                GridBoxTab.ItemsAutoUpdate = false;
                GridBoxMain.ItemsAutoUpdate = false;
            };

            {
                Commander.SetCurrentGroup("main");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "save",
                        Enabled = true,
                        Title = "Сохранить",
                        ButtonUse = true,
                        ButtonName = "SaveButton",
                        AccessLevel = Role.AccessMode.FullAccess,
                        Action = () =>
                        {
                            Save();
                        }
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "cancel",
                        Enabled = true,
                        Title = "отмена",
                        ButtonUse = true,
                        ButtonName = "CancelButton",
                        Action = () =>
                        {
                            Close();
                        }
                    });
                }

                Commander.SetCurrentGridName("GridBoxTab");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "grid_tab_refresh_items",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить данные",
                        ButtonUse = true,
                        ButtonName = "UpdatePackingListGrid",
                        MenuUse = true,
                        Action = () =>
                        {
                            GridBoxTab.LoadItems();
                        },
                    });
                }

                Commander.SetCurrentGridName("GridBoxMain");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "grid_main_refresh_items",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Обновить",
                        Description = "Обновить данные",
                        ButtonUse = true,
                        ButtonName = "UpdateRigListGrid",
                        MenuUse = true,
                        Action = () =>
                        {
                            GridBoxMain.LoadItems();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "grid_main_accept_new_price",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Применить",
                        ButtonUse = true,
                        ButtonName = "AcceptNewPrice",
                        Action = () =>
                        {
                            if (!string.IsNullOrEmpty(PriceInput.Text))
                            {
                                var itemSelect = GridBoxMain.SelectedItem.CheckGet("IDP");
                                
                                foreach (var item in GridMainList.Items)
                                {
                                    if (item.CheckGet("IDP") == itemSelect)
                                    {
                                        item.CheckAdd("CENAPRODRR", PriceInput.Text);
                                    }
                                }
                                
                                GridBoxMain.UpdateItems(GridMainList);
                                
                                EditPricePanel.Visibility = Visibility.Hidden;
                                EditPrice.Style = (Style)EditPrice.TryFindResource("Button");
                            }
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "grid_main_cancel_new_price",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Отменить",
                        ButtonUse = true,
                        ButtonName = "CancelNewPrice",
                        Action = () =>
                        {
                            EditPricePanel.Visibility = Visibility.Hidden;
                            EditPrice.Style = (Style)EditPrice.TryFindResource("Button");
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "grid_main_edit_price",
                        Group = "grid_base",
                        Enabled = true,
                        Title = "Изменить цену",
                        ButtonUse = true,
                        MenuUse = true,
                        ButtonName = "EditPrice",
                        Action = () =>
                        {
                            if (EditPricePanel.Visibility == Visibility.Hidden)
                            {
                                EditPricePanel.Visibility = Visibility.Visible;
                                EditPrice.Style = (Style)EditPrice.TryFindResource("FButtonPrimary");
                            }
                            else
                            {
                                EditPricePanel.Visibility = Visibility.Hidden;
                                EditPrice.Style = (Style)EditPrice.TryFindResource("Button");
                            }
                        },
                    });
                }

                Commander.Init(this);
            }
        }

        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get { return _selectedTabIndex; }
            set
            {
                if (_selectedTabIndex != value)
                {
                    _selectedTabIndex = value;
                    OnPropertyChanged(nameof(SelectedTabIndex));
                    OnTabChanged();
                }
            }
        }


        /// <summary>
        /// Отслеживание изменения вкладки
        /// </summary>
        private void OnTabChanged()
        {
            if (SelectedTabIndex == 1)
            {
                if (SelectBuyer.SelectedItem.Key != null && SelectedTabIndex == 1)
                {
                    BuyerId = SelectBuyer.SelectedItem.Key;
                    GridBoxTab.LoadItems();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        private string BuyerId { get; set; }
        private string StoreId { get; set; }
        private bool BanFlag { get; set; } = false;
        private string ConsignmentNoteId { get; set; }
        private int Count { get; set; } = 0;
        private FormHelper Form { get; set; }
        private ListDataSet GridMainList { get; set; }
        private ListDataSet GridTabList { get; set; }
        private ListDataSet SelectBuyerDs { get; set; }

        private ListDataSet ContractDataSet { get; set; }

        public int FactoryId = 1;


        /// <summary>
        /// Создание формы
        /// </summary>
        private void FormInit()
        {
            Form = new FormHelper();

            var field = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path = "BUYER_NAME",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = SelectBuyer,
                    ControlType = "SelectBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{},
                },
                new FormHelperField()
                {
                    Path = "VENDOR_LIST",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = SelectVendor,
                    Default = "1",
                    ControlType = "SelectBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{},
                    OnChange = (FormHelperField f, string v) =>
                    {
                        ListContract();
                    },
                    QueryLoadItems = new RequestData()
                    {
                        Module = "Rig",
                        Object = "Vendor",
                        Action = "List",
                        AnswerSectionKey = "ITEMS",
                        OnComplete = (FormHelperField f, ListDataSet ds) =>
                        {
                            var list = ds.GetItemsList("ID_PROD", "NAME");
                            var c = (SelectBox)f.Control;
                            if (c != null)
                            {
                                c.Items = list;
                            }

                            SelectVendor.SetSelectedItemByKey("0");
                        }
                    }
                },
                new FormHelperField()
                {
                    Path = "CONTRACT",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = ContractSelectBox,
                    ControlType = "SelectBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{},
                },
                new FormHelperField()
                {
                    Path = "DATE",
                    FieldType = FormHelperField.FieldTypeRef.DateTime,
                    Control = FromDate,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{},
                },
                new FormHelperField
                {
                    Path = "PRICE_INPUT",
                    FieldType = FormHelperField.FieldTypeRef.Double,
                    Control = PriceInput,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{},
                },
                new FormHelperField()
                {
                    Path = "NOTE_BOX",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = NoteBox,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{},
                },
                new FormHelperField()
                {
                    Path = "STATUS",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = FormStatus,
                    ControlType = "TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{},
                }
            };

            Form.SetFields(field);
            Form.SetDefaults();
            Form.StatusControl = FormStatus;
        }

        

        /// <summary>
        /// Получаение списка покупателей
        /// </summary>
        public async void BuyerLoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "Buyer");
            q.Request.SetParam("Action", "List");

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                if (result != null)
                {
                    SelectBuyerDs = ListDataSet.Create(result, "ITEMS");
                    SelectBuyer.Items = SelectBuyerDs.GetItemsList("ID", "NAME");
                }
            }


        }

        private void SetDefault()
        {
            DateTime t = DateTime.Now;
            FromDate.Text = t.ToString("dd.MM.yyyy");

            var factoryTypeItems = new Dictionary<string, string>
            {
                { "1", "Л-ПАК ЛИПЕЦК" },
                { "2", "Л-ПАК КАШИРА" }
            };
            FactoryList.Items = factoryTypeItems;
            FactoryList.SetSelectedItemByKey($"{FactoryId}");
        }

        /// <summary>
        /// Получение списка накладных
        /// </summary>
        public async void PackingListLoadList()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "PackingList");
            q.Request.SetParam("Action", "List");
            q.Request.SetParam("ID_POK", BuyerId);
            q.Request.SetParam("FACTORY_ID", $"{FactoryId}");

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
                    GridTabList = ListDataSet.Create(result, "ITEMS");
                    GridBoxTab.UpdateItems(GridTabList);
                }
            }
        }


        /// <summary>
        /// Загрузка списка оснасток в грид "выбраных оснасток"
        /// </summary>
        public async void MainGridLoad()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "Consumption");
            q.Request.SetParam("Action", "ListSelected");
            q.Request.SetParam("LIST_ID", StoreId);

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
                    GridMainList = ListDataSet.Create(result, "SELECT_ITEMS");
                    GridBoxMain.UpdateItems(GridMainList);
                }
            }


        }

        /// <summary>
        /// Инициализация грида для отображения выбранных оснасток
        /// </summary>
        private void GridMainInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="NAME",
                    ColumnType= ColumnTypeRef.String,
                    Width2= 80,
                },
                new DataGridHelperColumn
                {
                    Header = "NNAKL",
                    Path = "NNAKL",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2= 60,
                    Visible = false,
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во",
                    Path= "KOL",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 10,
                },
                new DataGridHelperColumn
                {
                    Header = "Ед. изм",
                    Path = "NAME_IZM",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 10,
                },
                new DataGridHelperColumn
                {
                    Header = "Цена покупки, руб",
                    Path = "CENAPOKR",
                    ColumnType = ColumnTypeRef.Double,
                    TotalsType = TotalsTypeRef.Summ,
                    Width2 = 30,
                },
                new DataGridHelperColumn
                {
                    Header = "Цена продажи, руб",
                    Path = "CENAPRODRR",
                    ColumnType = ColumnTypeRef.Double,
                    TotalsType = TotalsTypeRef.Summ,
                    Width2 = 30,
                },
                new DataGridHelperColumn
                {
                    Header = "Поставщик",
                    Path = "NAME_POST",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 50,
                },
                new DataGridHelperColumn
                {
                    Header="IDK1",
                    Path="IDK1",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=0,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header="ID1",
                    Path="ID1",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=0,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header="ID",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=0,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "IDP",
                    Path = "IDP",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=0,
                    Visible = false
                }
            };

            GridBoxMain.SetColumns(columns);
            GridBoxMain.SetPrimaryKey("IDP");
            GridBoxMain.OnLoadItems = MainGridLoad;
            GridBoxMain.AutoUpdateInterval = 0;
            GridBoxMain.SearchText = SearchTextForRig;
            GridBoxMain.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;

            GridBoxMain.OnSelectItem = item =>
            {
                var price = Math.Round(item.CheckGet("CENAPRODRR").ToDouble()* 100) / 100;
                PriceInput.Text = $"{price}";
            };

            GridBoxMain.Commands = Commander;

            GridBoxMain.Init();
        }


        /// <summary>
        /// Инициализация грида для отображения накладных покупателя
        /// </summary>
        private void GridTabInit()
        {
            var column = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "Дата расхода",
                    Path = "DATA",
                    ColumnType = ColumnTypeRef.DateTime,
                    Width2 = 50,
                    Format = "dd.MM.yyyy"
                },
                new DataGridHelperColumn
                {
                    Header = "NSTHET",
                    Path = "NSTHET",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 10,
                    Visible = false,
                },
                new DataGridHelperColumn
                {
                    Header = "ТТН",
                    Path = "NAME_STH",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 20,
                },
                new DataGridHelperColumn
                {
                    Header = "СФ",
                    Path = "NAME_SF",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 20,
                },
                new DataGridHelperColumn
                {
                    Header = "ТН",
                    Path = "NAME_PRIH",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 20,
                },
                new DataGridHelperColumn
                {
                    Header = "Счет",
                    Path = "NAME_TOVCHEK",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 30,
                },
                new DataGridHelperColumn
                {
                    Header = "Продавец",
                    Path = "NAME_PROD",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 50,
                }
            };

            GridBoxTab.SetColumns(column);
            GridBoxTab.SearchText = SearchText;
            GridBoxTab.SetPrimaryKey("NSTHET");
            GridBoxTab.OnLoadItems = PackingListLoadList;
            GridBoxTab.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            GridBoxTab.OnSelectItem = (Dictionary<string, string> selectedItem) =>
            {
                ConsignmentNoteId = selectedItem.CheckGet("NSTHET");
            };

            GridBoxTab.Commands = Commander;
            GridBoxTab.Init();

        }


        /// <summary>
        /// Выполняется и в 0 и в 1 табе. Добавление расходов в накладную
        /// </summary>
        /// <param name="consignmentNoteId">Id расходной накладной</param>
        /// <param name="productCategoryId">Индификатор категории товара</param>
        /// <param name="departmentId">Идентификатор отдела из которого расходуется товар</param>
        /// <param name="productId">Идентификатор товара</param>
        /// <param name="amount">Кол-во</param>
        /// <param name="sellingPrice">цена продажаи</param>
        /// <param name="incomeId">Идентфикатор прихода - используется как идентификатор поддона</param>
        private async void ConsumptionInPakingListCreate(string consignmentNoteId, string productCategoryId, string departmentId, string productId, string amount, string sellingPrice, string incomeId)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "Consumption");
            q.Request.SetParam("Action", "Create");
            q.Request.SetParam("NSTHET", consignmentNoteId);
            q.Request.SetParam("IDK1", productCategoryId);
            q.Request.SetParam("ID1", departmentId);
            q.Request.SetParam("ID2", productId);
            q.Request.SetParam("KOL", amount);
            q.Request.SetParam("CENAPRODRR", sellingPrice);
            q.Request.SetParam("IDP", incomeId);

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
                    var result2 = ListDataSet.Create(result, "ITEMS");
                    var idr = result2.Items[0].CheckGet("IDR");

                    PackingListWriteOff(idr);
                }
            }
            else
            {
                FormStatusSave.Text = "";
                FormStatus.Text = "Произошла ошибка при сохранении.";

                if (q.Answer.Error.Code == 145)
                {
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// Выполнение процедуры проведение накладной
        /// </summary>
        /// <param name="recordId">Идентификатор записи в настоящей таблице</param>
        private async void PackingListWriteOff(string recordId)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "PackingList");
            q.Request.SetParam("Action", "MakeWriteOff");
            q.Request.SetParam("IDR", recordId);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Count--;
                if (Count == 0)
                {
                    FormStatus.Text = "";
                    FormStatusSave.Text = "Cохранено.";

                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverName = "PreproductionRig",
                        Action = "consumption_refresh",
                        Message = $""
                    });

                    Close();
                }
            }
        }


        /// <summary>
        /// Для таба "Новая" SelectedTabIndex = 0. Создание новой накладной
        /// </summary>
        private async void PackingListCreate()
        {
            var f = Form.GetValues();

            if (ContractDataSet.Items.Count == 0)
            {
                f.Remove("CONTRACT");
            }

            if (!string.IsNullOrEmpty(f.CheckGet("CONTRACT")))
            {
                if (CheckActiveContract())
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Rig");
                    q.Request.SetParam("Object", "PackingList");
                    q.Request.SetParam("Action", "Create");
                    q.Request.SetParam("DATA", f.CheckGet("DATE"));
                    q.Request.SetParam("ID_DOG", f.CheckGet("CONTRACT"));
                    q.Request.SetParam("ID_POK", f.CheckGet("BUYER_NAME"));
                    q.Request.SetParam("ID_PROD", f.CheckGet("VENDOR_LIST"));
                    q.Request.SetParam("COMMENTS", f.CheckGet("NOTE_BOX"));
                    q.Request.SetParam("FACTORY_ID", FactoryId.ToString());

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
                            var newNakl = ListDataSet.Create(result, "ITEM");
                            var naklNsthet = newNakl.Items[0].CheckGet("NSTHET");
                            FormStatusSave.Text = "Сохранение";
                            // Загружаем в новую накладную записи с расходом оснасток
                            foreach (var item in GridMainList.Items)
                            {
                                ConsumptionInPakingListCreate(
                                    naklNsthet,
                                    item.CheckGet("IDK1"),
                                    item.CheckGet("ID1"),
                                    item.CheckGet("ID2"),
                                    item.CheckGet("KOL"),
                                    item.CheckGet("CENAPRODRR"),
                                    item.CheckGet("IDP"));
                            }
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
                string msg = $"Не выбран договор.";
                var d = new DialogWindow($"{msg}", "Расход оснастки", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        private bool CheckActiveContract()
        {
            bool result = false;

            if (ContractSelectBox.SelectedItem.Key != null && ContractDataSet.Items.Count > 0)
            {
                var contractItem = ContractDataSet.Items.FirstOrDefault(x => x.CheckGet("CONTRACT_ID").ToInt() == ContractSelectBox.SelectedItem.Key.ToInt());
                if (contractItem != null)
                {
                    if (contractItem.CheckGet("ACTIVE_FLAG").ToInt() == 0)
                    {
                        string msg = $"Выбран неактивный договор. Вы хотите продолжить?";
                        var d = new DialogWindow($"{msg}", "Расход оснастки", "", DialogWindowButtons.YesNo);
                        if (d.ShowDialog() == true)
                        {
                            result = true;
                        }
                    }
                    else
                    {
                        result = true;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Получаем договора для создания новой накладной
        /// </summary>
        private void ListContract()
        {
            var v = Form.GetValues();

            if (!string.IsNullOrEmpty(v.CheckGet("BUYER_NAME"))
                && !string.IsNullOrEmpty(v.CheckGet("VENDOR_LIST")))
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Rig");
                q.Request.SetParam("Object", "PackingList");
                q.Request.SetParam("Action", "ListContract");
                q.Request.SetParam("ID_POK", v.CheckGet("BUYER_NAME"));
                q.Request.SetParam("ID_PROD", v.CheckGet("VENDOR_LIST"));
                q.Request.SetParam("FACTORY_ID", $"{FactoryId}");

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                ContractSelectBox.DropDownListBox.Items.Clear();
                ContractSelectBox.Items.Clear();
                ContractSelectBox.DropDownListBox.SelectedItem = null;
                ContractSelectBox.ValueTextBox.Text = "";
                ContractSelectBox.IsEnabled = false;
                Form.SetValueByPath("CONTRACT", "-1");

                ContractDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        ContractDataSet = ListDataSet.Create(result, "ITEMS");
                        if (ContractDataSet != null && ContractDataSet.Items != null && ContractDataSet.Items.Count > 0)
                        {
                            ContractSelectBox.IsEnabled = true;
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
                ContractSelectBox.SetItems(ContractDataSet, "CONTRACT_ID", "CONTRACT_FULL_NAME");
                ContractSelectBox.SetSelectedItemFirst();
            }
            else
            {
                ContractSelectBox.DropDownListBox.Items.Clear();
                ContractSelectBox.Items.Clear();
                ContractSelectBox.DropDownListBox.SelectedItem = null;
                ContractSelectBox.ValueTextBox.Text = "";
                ContractSelectBox.IsEnabled = false;
                Form.SetValueByPath("CONTRACT_ID", "-1");
            }
        }

        /// <summary>
        /// Логика кнопки "Сохранить"
        /// </summary>
        private void Save()
        {
            Count = GridBoxMain.Items.Count;
            var f = Form.GetValues();

            bool canProceed = true;
            string statusMessage = "";

            if (BanFlag)
            {
                statusMessage = "Данному покупателю нельзя перевыставлять счет.";
                canProceed = false;
            }

            if (canProceed && string.IsNullOrEmpty(f.CheckGet("BUYER_NAME")))
            {
                statusMessage = "Выберите покупателя.";
                canProceed = false;
            }

            if (canProceed)
            {
                switch (_selectedTabIndex)
                {
                    case 1:
                        if (ConsignmentNoteId == null)
                        {
                            statusMessage = "Выберите накладную.";
                            canProceed = false;
                        }
                        else
                        {
                            FormStatusSave.Text = "Сохранение";
                            foreach (var item in GridMainList.Items)
                            {
                                ConsumptionInPakingListCreate(
                                    ConsignmentNoteId,
                                    item.CheckGet("IDK1"),
                                    item.CheckGet("ID1"),
                                    item.CheckGet("ID2"),
                                    item.CheckGet("KOL"),
                                    item.CheckGet("CENAPRODRR"),
                                    item.CheckGet("IDP"));
                            }
                        }

                        break;

                    case 0:
                        if (string.IsNullOrEmpty(f.CheckGet("DATE"))
                            || string.IsNullOrEmpty(f.CheckGet("CONTRACT"))
                            || string.IsNullOrEmpty(f.CheckGet("VENDOR_LIST")))
                        {
                            statusMessage = "Заполните все обязательные поля.";
                            canProceed = false;
                        }
                        else
                        {
                            // Создаем накладную
                            PackingListCreate();
                        }

                        break;
                }
            }

            if (!string.IsNullOrEmpty(statusMessage))
            {
                FormStatus.Text = statusMessage;
            }
        }


        /// <summary>
        /// Выполняется при создании фрейма
        /// </summary>
        /// <param name="selectItems">список id выбранных записей в гриде</param>
        public void Create(List<Dictionary<string, string>> selectItems)
        {
            StoreId = "";

            for (int index = 0; index < selectItems.Count; index++)
            {

                var i = selectItems[index];

                StoreId += i.CheckGet("RIG_ID");

                if (index < selectItems.Count - 1)
                {
                    StoreId += ", ";
                }
            }

            Show();
        }

        /// <summary>
        /// Логика для отслеживания бана у покупателя.
        /// </summary>
        private void SelectBuyer_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (SelectBuyer.SelectedItem.Key != null && _selectedTabIndex != 0)
            {
                BuyerId = SelectBuyer.SelectedItem.Key;
                GridBoxTab.LoadItems();
            }

            Dictionary<string, string> item = SelectBuyerDs.GetItemByKeyValue("ID", SelectBuyer.SelectedItem.Key);

            if (item != null && item.Count > 0)
            {
                string otherField = item.CheckGet("REARRANGE_FLAG");

                if (otherField == "1")
                {
                    FormStatus.Text = "Данному покупателю нельзя перевыставлять счет.";
                    BanFlag = true;
                }
                else
                {
                    FormStatus.Text = "";
                    BanFlag = false;
                }
            }

            ListContract();
        }

        private void FactoryList_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var s = (SelectBox)d;
            FactoryId = s.SelectedItem.Key.ToInt();
            ListContract();
        }
    }
}
