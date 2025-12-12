using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Stock;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static Client.Interfaces.Production.ComplectationMainComplectationTab;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Интерфейс Перекомплектация
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class Recomplectation : ControlBase
    {
        public Recomplectation()
        {
            ControlTitle = "Перекомплектация";
            RoleName = "[erp]recomplectation";
            DocumentationUrl = "/doc/l-pack-erp-new/production_new/complectation/recomplectation";
            InitializeComponent();

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnKeyPressed = (System.Windows.Input.KeyEventArgs e) =>
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
                SetDefaults();
                NewProductGridInit();
                OldProductGridInit();
                OrderGridInit();
                NewPalletGridInit();
                OldPalletGridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                NewProductGrid.Destruct();
                OldProductGrid.Destruct();
                OrderGrid.Destruct();
                NewPalletGrid.Destruct();
                OldPalletGrid.Destruct();
            };

            OnFocusGot = () =>
            {
            };

            OnFocusLost = () =>
            {
            };

            {
                Commander.Add(new CommandItem()
                {
                    Name = "old_product_refresh",
                    Group = "main",
                    Enabled = true,
                    Title = "Показать",
                    Description = "Показать данные на основе введённых данных",
                    ButtonUse = true,
                    ButtonControl = OldProductRefreshButton,
                    ButtonName = "OldProductRefreshButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        OldProductGrid.LoadItems();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "new_product_refresh",
                    Group = "main",
                    Enabled = true,
                    Title = "Показать",
                    Description = "Показать данные на основе введённых данных",
                    ButtonUse = true,
                    ButtonControl = NewProductRefreshButton,
                    ButtonName = "NewProductRefreshButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        NewProductGrid.LoadItems();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (OldProductGrid != null && OldProductGrid.Items != null && OldProductGrid.Items.Count > 0
                            && OldProductGrid.SelectedItem != null && OldProductGrid.SelectedItem.Count > 0)
                        {
                            result = true;
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "recomplectation",
                    Group = "main",
                    Enabled = false,
                    Title = "Перекомплектовать",
                    Description = "Выполнить перекомплектацию",
                    ButtonUse = true,
                    ButtonControl = RecomplectationButton,
                    ButtonName = "RecomplectationButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        MakeRecomplectation();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (OldProductGrid != null && OldProductGrid.Items != null && OldProductGrid.Items.Count > 0
                            && OldProductGrid.SelectedItem != null && OldProductGrid.SelectedItem.Count > 0)
                        {
                            if (NewProductGrid != null && NewProductGrid.Items != null && NewProductGrid.Items.Count > 0
                                && NewProductGrid.SelectedItem != null && NewProductGrid.SelectedItem.Count > 0)
                            {
                                if (OrderGrid != null && OrderGrid.Items != null && OrderGrid.Items.Count > 0
                                    && OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0)
                                {
                                    if (NewPalletGrid != null && NewPalletGrid.Items != null && NewPalletGrid.Items.Count > 0)
                                    {
                                        if (OldPalletGrid != null && OldPalletGrid.Items != null && OldPalletGrid.Items.Count > 0
                                            && OldPalletGrid.GetItemsSelected().Count > 0)
                                        {
                                            if (OldProductGrid.SelectedItem.CheckGet("PRODUCT_IDK1").ToInt() == NewProductGrid.SelectedItem.CheckGet("PRODUCT_IDK1").ToInt())
                                            {
                                                if (SelectedPlace != null && SelectedPlace.Count > 0)
                                                {
                                                    result = true;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        return result;
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
            }

            Commander.SetCurrentGridName("NewPalletGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "new_pallet_add",
                    Title = "Добавить",
                    Group = "new_pallet_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = NewPalletAddButton,
                    ButtonName = "NewPalletAddButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        NewPalletAdd();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (NewProductGrid != null && NewProductGrid.Items != null && NewProductGrid.Items.Count > 0)
                        {
                            if (NewProductGrid.SelectedItem != null && NewProductGrid.SelectedItem.Count > 0)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "new_pallet_edit",
                    Title = "Изменить",
                    Group = "new_pallet_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = NewPalletEditButton,
                    ButtonName = "NewPalletEditButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        NewPalletEdit();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (NewPalletGrid != null && NewPalletGrid.Items != null && NewPalletGrid.Items.Count > 0)
                        {
                            if (NewPalletGrid != null && NewPalletGrid.SelectedItem != null && NewPalletGrid.SelectedItem.Count > 0)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "new_pallet_delete",
                    Title = "Удалить",
                    Group = "new_pallet_grid_default",
                    Enabled = false,
                    MenuUse = true,
                    ButtonUse = true,
                    ButtonControl = NewPalletDeleteButton,
                    ButtonName = "NewPalletDeleteButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        NewPalletDelete();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (NewPalletGrid != null && NewPalletGrid.Items != null && NewPalletGrid.Items.Count > 0)
                        {
                            if (NewPalletGrid != null && NewPalletGrid.SelectedItem != null && NewPalletGrid.SelectedItem.Count > 0)
                            {
                                result = true;
                            }
                        }

                        return result;
                    },
                });
            }

            Commander.Init(this);
        }

        private int _oldProductIdk1 { get; set; }

        private ListDataSet NewProductGridDataSet { get; set; }
        private ListDataSet OldProductGridDataSet { get; set; }
        private ListDataSet OrderGridDataSet { get; set; }
        private ListDataSet NewPalletGridDataSet { get; set; }
        private ListDataSet OldPalletGridDataSet { get; set; }

        /// <summary>
        /// Станок, на котором выполняется Перекомплектация
        /// </summary>
        public int MachineId = 715;

        /// <summary>
        /// Идентификатор площадки
        /// </summary>
        public int FactoryId = 1;

        /// <summary>
        /// Системное имя пользователя, выполняющего перекомплектацию
        /// </summary>
        public string StaffName = "Перекомпл.";

        public List<Dictionary<string, string>> PlaceData { get; set; }

        public Dictionary<string, string> SelectedPlace { get; set; }

        public void SetDefaults()
        {
            NewProductGridDataSet = new ListDataSet();
            OldProductGridDataSet = new ListDataSet();
            OrderGridDataSet = new ListDataSet();
            NewPalletGridDataSet = new ListDataSet();
            OldPalletGridDataSet = new ListDataSet();

            SelectedPlace = new Dictionary<string, string>();
            PlaceData = new List<Dictionary<string, string>>();

            {
                var corrugatorPlace = new Dictionary<string, string>();
                corrugatorPlace.Add("ID", "1");
                corrugatorPlace.Add("NAME", "К0");
                corrugatorPlace.Add("PLACE_NAME", "К");
                corrugatorPlace.Add("PLACE_NUMBER", "0");
                corrugatorPlace.Add("NEW_NAME", "К-1");
                corrugatorPlace.Add("NEW_PLACE_NAME", "К");
                corrugatorPlace.Add("NEW_PLACE_NUMBER", "-1");
                corrugatorPlace.Add("IDK1_4", "1");
                corrugatorPlace.Add("IDK1_5", "1");
                corrugatorPlace.Add("IDK1_6", "0");
                corrugatorPlace.Add("MACHINE_ID", ComplectationPlace.CorrugatingMachines);
                PlaceData.Add(corrugatorPlace);
            }

            {
                var procesingPlace = new Dictionary<string, string>();
                procesingPlace.Add("ID", "2");
                procesingPlace.Add("NAME", "Ц0");
                procesingPlace.Add("PLACE_NAME", "Ц");
                procesingPlace.Add("PLACE_NUMBER", "0");
                procesingPlace.Add("NEW_NAME", "Ц0");
                procesingPlace.Add("NEW_PLACE_NAME", "Ц");
                procesingPlace.Add("NEW_PLACE_NUMBER", "0");
                procesingPlace.Add("IDK1_4", "0");
                procesingPlace.Add("IDK1_5", "1");
                procesingPlace.Add("IDK1_6", "1");
                procesingPlace.Add("MACHINE_ID", ComplectationPlace.ProcessingMachines);
                PlaceData.Add(procesingPlace);
            }

            {
                var stockPlace = new Dictionary<string, string>();
                stockPlace.Add("ID", "3");
                stockPlace.Add("NAME", "С-1");
                stockPlace.Add("PLACE_NAME", "С");
                stockPlace.Add("PLACE_NUMBER", "-1");
                stockPlace.Add("NEW_NAME", "С-1");
                stockPlace.Add("NEW_PLACE_NAME", "С");
                stockPlace.Add("NEW_PLACE_NUMBER", "-1");
                stockPlace.Add("IDK1_4", "0");
                stockPlace.Add("IDK1_5", "1");
                stockPlace.Add("IDK1_6", "1");
                stockPlace.Add("MACHINE_ID", ComplectationPlace.Stock);
                PlaceData.Add(stockPlace);
            }

            var typeProduct = new Dictionary<string, string>();
            typeProduct.Add("-1", "Все типы");
            typeProduct.Add("4", "Заготовка");
            typeProduct.Add("5", "Лист");
            typeProduct.Add("6", "Коробка");
            TypeProductSelectBox.SetItems(typeProduct);
            TypeProductSelectBox.SetSelectedItemByKey("-1");
        }

        public void SelectPlace()
        {
            if (PlaceSelectBox != null && !PlaceSelectBox.SelectedItem.Key.IsNullOrEmpty())
            {
                SelectedPlace = PlaceData.FirstOrDefault(x => x.CheckGet("ID") == PlaceSelectBox.SelectedItem.Key);
            }
        }

        public void FillPlaceSelectBox()
        {
            PlaceSelectBox.DropDownListBox.Items.Clear();
            PlaceSelectBox.DropDownListBox.SelectedItem = null;
            PlaceSelectBox.ValueTextBox.Text = "";
            PlaceSelectBox.Items = new Dictionary<string, string>();
            PlaceSelectBox.SelectedItem = new KeyValuePair<string, string>();

            var placeList = PlaceData.Where(x => x.CheckGet($"IDK1_{_oldProductIdk1}").ToInt() == 1)?.ToList();
            if (placeList != null && placeList.Count > 0)
            {
                Dictionary<string, string> placeDictionary = new Dictionary<string, string>();
                foreach (var place in placeList) 
                {
                    placeDictionary.Add(place.CheckGet("ID"), place.CheckGet("NAME"));
                }
                PlaceSelectBox.SetItems(placeDictionary);
                PlaceSelectBox.SetSelectedItemFirst();
            }
        }

        public void NewProductGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Description = "Идентификатор продукции",
                        Path="PRODUCT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Description = "Наименование продукции",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=45,
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
                        Header="Код",
                        Description = "Код продукции",
                        Path="PRODUCT_KOD",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид категории продукции",
                        Description = "Ид категории продукции",
                        Path="PRODUCT_IDK1",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=1,
                        Hidden=true,
                    },
                };
                NewProductGrid.SetColumns(columns);
                NewProductGrid.SearchText = NewProductSearchBox;
                NewProductGrid.SetPrimaryKey("PRODUCT_ID");
                NewProductGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
                NewProductGrid.AutoUpdateInterval = 0;
                NewProductGrid.OnLoadItems = NewProductGridLoadItems;

                NewProductGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem != null && selectedItem.Count > 0 && NewProductGrid.Items.FirstOrDefault(x => x.CheckGet("PRODUCT_ID").ToInt() == selectedItem.CheckGet("PRODUCT_ID").ToInt()) != null)
                    {
                        OrderGrid.LoadItems();
                    }
                    else
                    {
                        OrderGrid.ClearItems();
                        NewProductGrid.SelectRowFirst();
                    }
                };

                NewProductGrid.Commands = Commander;
                NewProductGrid.UseProgressSplashAuto = false;
                NewProductGrid.Init();
            }
        }

        public async void NewProductGridLoadItems()
        {
            if (NewProductSearchBox != null && !string.IsNullOrEmpty(NewProductSearchBox.Text))
            {
                if (OldProductGrid != null && OldProductGrid.SelectedItem != null && OldProductGrid.SelectedItem.Count > 0)
                {
                    NewProductGridToolbar.IsEnabled = false;
                    SetSplashVisible(true, $"Пожалуйста, подождите.{Environment.NewLine}Идёт загрузка данных");

                    var p = new Dictionary<string, string>();
                    p.Add("SEARCH_TEXT", NewProductSearchBox.Text);
                    p.Add("PRODUCT_KATEGORY_ID", OldProductGrid.SelectedItem.CheckGet("PRODUCT_IDK1"));
                    p.Add("MODE", "2");

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Complectation");
                    q.Request.SetParam("Object", "Recomplectation");
                    q.Request.SetParam("Action", "ListProductByKategorySearchText");
                    q.Request.SetParams(p);
                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    NewProductGridDataSet = new ListDataSet();
                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            NewProductGridDataSet = ListDataSet.Create(result, "ITEMS");
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                    NewProductGrid.UpdateItems(NewProductGridDataSet);

                    NewProductGridToolbar.IsEnabled = true;
                    SetSplashVisible(false);
                }
            }
        }

        public void OldProductGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Description = "Идентификатор продукции",
                        Path="PRODUCT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Description = "Наименование продукции",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=45,
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
                        Header="Код",
                        Description = "Код продукции",
                        Path="PRODUCT_KOD",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид категории продукции",
                        Description = "Ид категории продукции",
                        Path="PRODUCT_IDK1",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=1,
                        Hidden=true,
                        Visible=false,
                    },
                };
                OldProductGrid.SetColumns(columns);
                OldProductGrid.SearchText = OldProductSearchBox;
                OldProductGrid.SetPrimaryKey("PRODUCT_ID");
                OldProductGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
                OldProductGrid.AutoUpdateInterval = 0;
                OldProductGrid.OnLoadItems = OldProductGridLoadItems;

                OldProductGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem != null && selectedItem.Count > 0 && OldProductGrid.Items.FirstOrDefault(x => x.CheckGet("PRODUCT_ID").ToInt() == selectedItem.CheckGet("PRODUCT_ID").ToInt()) != null)
                    {
                        if (_oldProductIdk1 != selectedItem.CheckGet("PRODUCT_IDK1").ToInt())
                        {
                            _oldProductIdk1 = selectedItem.CheckGet("PRODUCT_IDK1").ToInt();
                            NewProductGrid.ClearItems();
                            OrderGrid.ClearItems();
                        }

                        OldPalletGrid.LoadItems();

                        FillPlaceSelectBox();
                    }
                    else
                    {
                        OldPalletGrid.ClearItems();
                        OldProductGrid.SelectRowFirst();
                    }
                };

                OldProductGrid.OnFilterItems = () =>
                {
                    if (OldProductGrid.Items != null && OldProductGrid.Items.Count > 0)
                    {
                        // Фильтрация по типу продукции
                        // -1 -- Все типы продукции
                        if (TypeProductSelectBox.SelectedItem.Key != null)
                        {
                            var key = TypeProductSelectBox.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            switch (key)
                            {
                                // Все типы продукции
                                case -1:
                                    items = OldProductGrid.Items;
                                    break;

                                default:
                                    items.AddRange(OldProductGrid.Items.Where(x => x.CheckGet("PRODUCT_IDK1").ToInt() == key));
                                    break;
                            }

                            OldProductGrid.Items = items;
                        }
                    }
                };

                OldProductGrid.Commands = Commander;
                OldProductGrid.UseProgressSplashAuto = false;
                OldProductGrid.Init();
            }
        }

        public async void OldProductGridLoadItems()
        {
            if (OldProductSearchBox != null && !string.IsNullOrEmpty(OldProductSearchBox.Text))
            {
                OldProductGridToolbar.IsEnabled = false;
                SetSplashVisible(true, $"Пожалуйста, подождите.{Environment.NewLine}Идёт загрузка данных");

                var p = new Dictionary<string, string>();
                p.Add("SEARCH_TEXT", OldProductSearchBox.Text);
                p.Add("MODE", "1");
                p.Add("FACTORY_ID", $"{FactoryId}");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Complectation");
                q.Request.SetParam("Object", "Recomplectation");
                q.Request.SetParam("Action", "ListProductByKategorySearchText");
                q.Request.SetParams(p);
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                OldProductGridDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        OldProductGridDataSet = ListDataSet.Create(result, "ITEMS");
                    }
                }
                else
                {
                    q.ProcessError();
                }
                OldProductGrid.UpdateItems(OldProductGridDataSet);

                OldProductGridToolbar.IsEnabled = true;
                SetSplashVisible(false);
            }
            else
            {
                OldProductGridDataSet = new ListDataSet();
                OldProductGrid.UpdateItems(OldProductGridDataSet);
            }
        }

        public void OrderGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Path="ORDER_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Заявка",
                        Path="ORDER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=40,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата отгрузки",
                        Path="ORDER_DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm",
                        Width2=12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество, шт.",
                        Description="Количество готовой продукции по заявке, штук",
                        Path="QUANTITY_GOODS",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ПЗ",
                        Description="Создано производственное задание по заявке для этого вида продукции",
                        Path="PRODUCTION_TASK_EXIST_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=4,
                    },
                    
                    new DataGridHelperColumn
                    {
                        Header="Ид продукции",
                        Path="PRODUCT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид категории продукции",
                        Path="PRODUCT_KATEGORY_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование продукции",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=7,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="На поддоне, шт.",
                        Description="Количество готовой продукции на поддоне, штук",
                        Path="QUANTITY_GOODS_ON_PALLET",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                        Hidden=true,
                    },
                };
                OrderGrid.SetColumns(columns);
                OrderGrid.SetPrimaryKey("ORDER_ID");
                OrderGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
                OrderGrid.AutoUpdateInterval = 0;
                OrderGrid.SearchText = OrderSearchBox;
                OrderGrid.Toolbar = OrderGridToolbar;
                OrderGrid.OnLoadItems = OrderGridLoadItems;

                OrderGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem != null && selectedItem.Count > 0 && OrderGrid.Items.FirstOrDefault(x => x.CheckGet("ORDER_ID").ToInt() == selectedItem.CheckGet("ORDER_ID").ToInt()) != null)
                    {
                    }
                    else
                    {
                        OrderGrid.SelectRowFirst();
                    }
                };

                OrderGrid.Commands = Commander;
                OrderGrid.UseProgressSplashAuto = false;
                OrderGrid.Init();
            }
        }

        public void OrderGridLoadItems()
        {
            if (NewProductGrid != null && NewProductGrid.SelectedItem != null && NewProductGrid.SelectedItem.Count > 0)
            {
                var p = new Dictionary<string, string>();
                p.Add("PRODUCT_ID", NewProductGrid.SelectedItem.CheckGet("PRODUCT_ID"));
                p.Add("FACTORY_ID", $"{FactoryId}");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Complectation");
                q.Request.SetParam("Object", "Recomplectation");
                q.Request.SetParam("Action", "ListOrder");
                q.Request.SetParams(p);
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                OrderGridDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        OrderGridDataSet = ListDataSet.Create(result, "ITEMS");
                    }
                }
                else
                {
                    q.ProcessError();
                }
                OrderGrid.UpdateItems(OrderGridDataSet);
            }
            else
            {
                OrderGridDataSet = new ListDataSet();
                OrderGrid.UpdateItems(OrderGridDataSet);
            }
        }

        public void NewPalletGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="№ поддона",
                        Path="PALLET_NUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=10,
                        TotalsType = TotalsTypeRef.Count,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество, шт.",
                        Path="QUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=12,
                        TotalsType = TotalsTypeRef.Summ,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Ид поддона",
                        Description="Ид поддона, из которого делают этот поддон",
                        Path="OLD_PALLET_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=10,
                        Hidden=true,
                    },
                };
                NewPalletGrid.SetColumns(columns);
                NewPalletGrid.SetPrimaryKey("PALLET_NUMBER");
                NewPalletGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                NewPalletGrid.AutoUpdateInterval = 0;
                NewPalletGrid.Toolbar = NewPalletGridToolbar;
                NewPalletGrid.OnLoadItems = NewPalletGridLoadItems;

                NewPalletGrid.Commands = Commander;
                NewPalletGrid.UseProgressSplashAuto = false;
                NewPalletGrid.Init();
            }
        }

        public void NewPalletGridLoadItems()
        {
            NewPalletGrid.UpdateItems(NewPalletGridDataSet);
        }

        public void OldPalletGridInit()
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
                        OnAfterClickAction = (row, el) =>
                        {
                            {
                                var selectedItemList = OldPalletGrid.GetItemsSelected();
                                if (selectedItemList != null && selectedItemList.Count > 0)
                                {
                                    ConsumptionQuantityLabel.Content = selectedItemList.Sum(x => x.CheckGet("QUANTITY").ToInt());
                                    ConsumptionPalletQuantityLabel.Content = selectedItemList.Count;
                                }
                                else
                                {
                                    ConsumptionQuantityLabel.Content = 0;
                                    ConsumptionPalletQuantityLabel.Content = 0;
                                }

                                if (selectedItemList.Count(x => x.CheckGet("PALLET_ID").ToInt() == row.CheckGet("PALLET_ID").ToInt()) > 0)
                                {
                                    NewPalletAddByOldPallet(row.CheckGet("PALLET_ID").ToInt(), row.CheckGet("QUANTITY").ToInt());
                                }
                                else
                                {
                                    NewPalletDeleteByOldPallet(row.CheckGet("PALLET_ID").ToInt());
                                }
                            }

                            Commander.UpdateActions();
                            Commander.RenderButtons();

                            return true;
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид поддона",
                        Path="PALLET_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=10,
                        TotalsType = TotalsTypeRef.Count,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Поддон",
                        Path="PALLET",
                        ColumnType=ColumnTypeRef.String,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество, шт.",
                        Path="QUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=12,
                        TotalsType = TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ячейка",
                        Path="PLACE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Дата перемещения",
                        Path = "MOVING_DTTM",
                        ColumnType = DataGridHelperColumn.ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2=16,
                    },

                    new DataGridHelperColumn
                    {
                        Header="№ поддона",
                        Path="PALLET_NUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=1,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид прихода",
                        Path="INCOMING_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=1,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Начало кондиционирования",
                        Path = "CONDITION_DTTM",
                        ColumnType = DataGridHelperColumn.ColumnTypeRef.DateTime,
                        Width2=1,
                        Hidden = true,
                    },

                };
                OldPalletGrid.SetColumns(columns);
                OldPalletGrid.SetPrimaryKey("PALLET_ID");
                OldPalletGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
                OldPalletGrid.AutoUpdateInterval = 0;
                OldPalletGrid.SearchText = OldPalletSearchBox;
                OldPalletGrid.Toolbar = OldPalletGridToolbar;
                OldPalletGrid.OnLoadItems = OldPalletGridLoadItems;

                OldPalletGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem != null && selectedItem.Count > 0 && OldPalletGrid.Items.FirstOrDefault(x => x.CheckGet("PALLET_ID").ToInt() == selectedItem.CheckGet("PALLET_ID").ToInt()) != null)
                    {
                    }
                    else
                    {
                        OldPalletGrid.SelectRowFirst();
                    }
                };

                OldPalletGrid.Commands = Commander;
                OldPalletGrid.UseProgressSplashAuto = false;
                OldPalletGrid.Init();
                OldPalletGrid.Run();
            }
        }

        public void OldPalletGridLoadItems()
        {
            if (OldProductGrid != null && OldProductGrid.SelectedItem != null && OldProductGrid.SelectedItem.Count > 0)
            {
                var p = new Dictionary<string, string>();
                p.Add("PRODUCT_ID", OldProductGrid.SelectedItem.CheckGet("PRODUCT_ID"));
                p.Add("FACTORY_ID", $"{FactoryId}");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Stock");
                q.Request.SetParam("Object", "Pallet");
                q.Request.SetParam("Action", "ListByProduct");
                q.Request.SetParams(p);
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                OldPalletGridDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        OldPalletGridDataSet = ListDataSet.Create(result, "List");
                        if (OldPalletGridDataSet != null && OldPalletGridDataSet.Items != null && OldPalletGridDataSet.Items.Count > 0)
                        {
                            var selectedItems = OldPalletGrid.GetItemsSelected();
                            if (selectedItems != null && selectedItems.Count > 0)
                            {
                                foreach (var dataSetItem in OldPalletGridDataSet.Items) 
                                {
                                    var selectedItem = selectedItems.FirstOrDefault(x => x.CheckGet("PALLET_ID").ToInt() == dataSetItem.CheckGet("PALLET_ID").ToInt());
                                    if (selectedItem != null)
                                    {
                                        dataSetItem.CheckAdd("_SELECTED", selectedItem.CheckGet("_SELECTED"));
                                    }
                                    else
                                    {
                                        dataSetItem.CheckAdd("_SELECTED", "0");
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
                OldPalletGrid.UpdateItems(OldPalletGridDataSet);
            }
            else
            {
                OldPalletGridDataSet = new ListDataSet();
                OldPalletGrid.UpdateItems(OldPalletGridDataSet);
            }
        }

        /// <summary>
        /// Добавляем поддон в грид новых поддонов
        /// по данным от старого поддона
        /// </summary>
        private void NewPalletAddByOldPallet(int oldPalletId, int quantityOnPallet)
        {
            if (quantityOnPallet > 0 && oldPalletId > 0)
            {
                int palletNumber = 1;
                if (NewPalletGridDataSet.Items != null)
                {
                    palletNumber = NewPalletGridDataSet.Items.Count + 1;
                }

                var item = new Dictionary<string, string>
                {
                    ["PALLET_NUMBER"] = palletNumber.ToString(),
                    ["QUANTITY"] = quantityOnPallet.ToString(),
                    ["OLD_PALLET_ID"] = oldPalletId.ToString()
                };

                NewPalletGridDataSet.Items.Add(item);
                NewPalletGrid.LoadItems();
            }
        }

        /// <summary>
        /// Добавляем поддон в грид новых поддонов
        /// </summary>
        public void NewPalletAdd()
        {
            int quantityOnPallet = 0;
            var i = new ComplectationCMQuantity(quantityOnPallet);
            i.Show("Количество на поддоне");
            if (i.OkFlag)
            {
                quantityOnPallet = i.QtyInt;
            }

            if (quantityOnPallet > 0)
            {
                int palletNumber = 1;
                if (NewPalletGridDataSet.Items != null)
                {
                    palletNumber = NewPalletGridDataSet.Items.Count + 1;
                }

                var item = new Dictionary<string, string>
                {
                    ["PALLET_NUMBER"] = palletNumber.ToString(),
                    ["QUANTITY"] = quantityOnPallet.ToString(),
                    ["OLD_PALLET_ID"] = ""
                };

                NewPalletGridDataSet.Items.Add(item);
                NewPalletGrid.LoadItems();
            }
        }

        /// <summary>
        /// Редактируем поддон в гриде новых поддонов
        /// </summary>
        public void NewPalletEdit()
        {
            if (NewPalletGrid != null && NewPalletGrid.SelectedItem != null && NewPalletGrid.SelectedItem.Count > 0)
            {
                int quantityOnPallet = NewPalletGrid.SelectedItem.CheckGet("QUANTITY").ToInt();
                var i = new ComplectationCMQuantity(quantityOnPallet);
                i.Show("Количество на поддоне");
                if (i.OkFlag)
                {
                    quantityOnPallet = i.QtyInt;
                }

                if (quantityOnPallet > 0)
                {
                    Dictionary<string, string> selectedItem = NewPalletGridDataSet.Items.FirstOrDefault(x => x.CheckGet("PALLET_NUMBER").ToInt() == NewPalletGrid.SelectedItem.CheckGet("PALLET_NUMBER").ToInt());
                    if (selectedItem != null)
                    {
                        selectedItem.CheckAdd("QUANTITY", quantityOnPallet.ToString());
                    }

                    NewPalletGrid.LoadItems();
                }
            }
        }

        /// <summary>
        /// Удаляем поддон в гриде новых поддонов
        /// </summary>
        public void NewPalletDelete()
        {
            if (NewPalletGrid != null && NewPalletGrid.SelectedItem != null && NewPalletGrid.SelectedItem.Count > 0)
            {
                NewPalletGridDataSet.Items.Remove(NewPalletGrid.SelectedItem);

                var palletNumber = 1;
                foreach (var item in NewPalletGridDataSet.Items)
                {
                    item["PALLET_NUMBER"] = palletNumber.ToString();
                    palletNumber++;
                }

                NewPalletGrid.LoadItems();
            }
        }

        /// <summary>
        /// Удаляем поддон в гриде новых поддонов
        /// по данным от старого поддона
        /// </summary>
        private void NewPalletDeleteByOldPallet(int oldPalletId)
        {
            Dictionary<string, string> deletingRow = NewPalletGridDataSet.Items.FirstOrDefault(x => x.CheckGet("OLD_PALLET_ID").ToInt() == oldPalletId);
            if (deletingRow != null && deletingRow.Count > 0)
            {
                NewPalletGridDataSet.Items.Remove(deletingRow);

                var palletNumber = 1;
                foreach (var item in NewPalletGridDataSet.Items)
                {
                    item["PALLET_NUMBER"] = palletNumber.ToString();
                    palletNumber++;
                }

                NewPalletGrid.LoadItems();
            }
        }

        public void MakeRecomplectation()
        {
            bool resume = false;

            if (OldProductGrid != null && OldProductGrid.Items != null && OldProductGrid.Items.Count > 0
                && OldProductGrid.SelectedItem != null && OldProductGrid.SelectedItem.Count > 0)
            {
                if (NewProductGrid != null && NewProductGrid.Items != null && NewProductGrid.Items.Count > 0
                    && NewProductGrid.SelectedItem != null && NewProductGrid.SelectedItem.Count > 0)
                {
                    if (OrderGrid != null && OrderGrid.Items != null && OrderGrid.Items.Count > 0
                        && OrderGrid.SelectedItem != null && OrderGrid.SelectedItem.Count > 0)
                    {
                        if (NewPalletGrid != null && NewPalletGrid.Items != null && NewPalletGrid.Items.Count > 0)
                        {
                            if (OldPalletGrid != null && OldPalletGrid.Items != null && OldPalletGrid.Items.Count > 0
                                && OldPalletGrid.GetItemsSelected().Count > 0)
                            {
                                if (OldProductGrid.SelectedItem.CheckGet("PRODUCT_IDK1").ToInt() == NewProductGrid.SelectedItem.CheckGet("PRODUCT_IDK1").ToInt())
                                {
                                    if (SelectedPlace != null && SelectedPlace.Count > 0)
                                    {
                                        resume = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (resume)
            {
                {
                    var msg = 
                        $"Будет списано:{Environment.NewLine}" +
                        $"  {OldProductGrid.SelectedItem.CheckGet("PRODUCT_NAME")} ({OldPalletGrid.GetItemsSelected().Sum(x => x.CheckGet("QUANTITY").ToInt())} шт.){Environment.NewLine}" +
                        $"Будет создано:{Environment.NewLine}" +
                        $"  {NewProductGrid.SelectedItem.CheckGet("PRODUCT_NAME")} ({NewPalletGrid.Items.Sum(x => x.CheckGet("QUANTITY").ToInt())} шт.){Environment.NewLine}" +
                        $"По заявке:{Environment.NewLine}" +
                        $"  {OrderGrid.SelectedItem.CheckGet("ORDER_NAME")}{Environment.NewLine}" +
                        $"В ячейку:{Environment.NewLine}" +
                        $"  {SelectedPlace.CheckGet("NEW_NAME")}{Environment.NewLine}" +
                        $"{Environment.NewLine}Продолжить ?";
                    var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.NoYes);
                    resume = (bool)d.ShowDialog();
                }

                if (resume)
                {
                    SetSplashVisible(true, $"Пожалуйста, подождите.{Environment.NewLine}Выполняется перекомплектация");

                    var p = new Dictionary<string, string>();
                    //p.Add("MACHINE_ID", $"{MachineId}");
                    p.Add("MACHINE_ID", SelectedPlace.CheckGet("MACHINE_ID"));
                    p.Add("PLACE_NAME", SelectedPlace.CheckGet("NEW_PLACE_NAME"));
                    p.Add("PLACE_NUMBER", SelectedPlace.CheckGet("NEW_PLACE_NUMBER"));
                    p.Add("STAFF_NAME", StaffName);

                    p.Add("OLD_PALLET_LIST", JsonConvert.SerializeObject(OldPalletGrid.GetItemsSelected()));
                    p.Add("OLD_PRODUCT_ID", OldProductGrid.SelectedItem.CheckGet("PRODUCT_ID"));
                    p.Add("OLD_PRODUCT_KATEGORY_ID", OldProductGrid.SelectedItem.CheckGet("PRODUCT_IDK1"));
                    p.Add("OLD_PRODUCT_NAME", OldProductGrid.SelectedItem.CheckGet("PRODUCT_NAME"));

                    p.Add("NEW_PALLET_LIST", JsonConvert.SerializeObject(NewPalletGrid.Items));
                    p.Add("NEW_APPLICATION_ID", OrderGrid.SelectedItem.CheckGet("ORDER_ID"));
                    p.Add("NEW_APPLICATION_QUANTITY_GOODS", OrderGrid.SelectedItem.CheckGet("QUANTITY_GOODS"));
                    p.Add("NEW_PRODUCT_ID", NewProductGrid.SelectedItem.CheckGet("PRODUCT_ID"));
                    p.Add("NEW_PRODUCT_KATEGORY_ID", NewProductGrid.SelectedItem.CheckGet("PRODUCT_IDK1"));
                    p.Add("NEW_PRODUCT_NAME", NewProductGrid.SelectedItem.CheckGet("PRODUCT_NAME"));                    
                    p.Add("NEW_NEXT_PRODUCT_ID", OrderGrid.SelectedItem.CheckGet("PRODUCT_ID"));
                    p.Add("NEW_NEXT_PRODUCT_KATEGORY_ID", OrderGrid.SelectedItem.CheckGet("PRODUCT_KATEGORY_ID"));
                    p.Add("NEW_NEXT_PRODUCT_NAME", OrderGrid.SelectedItem.CheckGet("PRODUCT_NAME"));
                    p.Add("NEW_NEXT_PRODUCT_QUANTITY_ON_PALLET", OrderGrid.SelectedItem.CheckGet("QUANTITY_GOODS_ON_PALLET"));
                    
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Complectation");
                    q.Request.SetParam("Object", "Recomplectation");
                    q.Request.SetParam("Action", "Make");

                    q.Request.SetParams(p);
                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {
                        int productionTaskId = 0;

                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            if (ds != null && ds.Items != null && ds.Items.Count > 0)
                            {
                                productionTaskId = ds.Items[0].CheckGet("PRODUCTION_TASK_ID").ToInt();
                            }
                        }

                        if (productionTaskId > 0)
                        {
                            // печать ярлыков
                            foreach (var item in NewPalletGrid.Items)
                            {
                                LabelReport2 report = new LabelReport2(true);
                                report.PrintLabel(productionTaskId.ToString(), item.CheckGet("PALLET_NUMBER"), OldProductGrid.SelectedItem.CheckGet("PRODUCT_IDK1"));
                            }

                            SetSplashVisible(false);

                            var msg = "Успешное выполнение перекомплектации.";
                            var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                            d.ShowDialog();

                            ClearInterface();
                        }
                        else
                        {
                            SetSplashVisible(false);

                            var msg = "Ошибка выполнения перекомплектации. Пожалуйста, сообщите о проблеме";
                            var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                    else
                    {
                        SetSplashVisible(false);

                        q.ProcessError();
                    }
                }
            }
            else
            {
                var msg = "Заполнены не все данные";
                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        private void SetSplashVisible(bool visible, string msg = "")
        {
            SplashControl.Visible = visible;
            SplashControl.Message = msg;
        }

        private void ClearInterface()
        {
            NewPalletGridDataSet.Items.Clear();
            NewPalletGrid.LoadItems();

            NewProductSearchBox.Text = "";
            NewProductGridDataSet = new ListDataSet();
            NewProductGrid.UpdateItems(NewProductGridDataSet);

            OrderSearchBox.Text = "";
            OrderGridDataSet = new ListDataSet();
            OrderGrid.UpdateItems(OrderGridDataSet);

            OldPalletSearchBox.Text = "";
            OldPalletGridDataSet = new ListDataSet();
            OldPalletGrid.UpdateItems(OldPalletGridDataSet);
            ConsumptionQuantityLabel.Content = 0;
            ConsumptionPalletQuantityLabel.Content = 0;

            OldProductSearchBox.Text = "";
            OldProductGridDataSet = new ListDataSet();
            OldProductGrid.UpdateItems(OldProductGridDataSet);
        }

        /// <summary>
        /// Установка настроек для принтера
        /// </summary>
        public void SetPrintSettings()
        {
            LabelReport2.SetPrintingProfile();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            SetPrintSettings();
        }

        private void PlaceSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SelectPlace();
        }

        private void TypeProductSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            OldProductGrid.UpdateItems();
        }
    }
}
