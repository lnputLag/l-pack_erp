using Client.Common;
using Client.Interfaces.Main;
using DevExpress.Mvvm.Xpf;
using DevExpress.Utils.Controls;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Office.Interop.Excel;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Header;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// диалог выбора ячейки для тмц
    /// </summary>
    /// <author>eletskikh_ya</author>
    /// <author>Greshnyh_ni</author>
    public partial class WarehouseItemCell : ControlBase
    {
        public WarehouseItemCell()
        {
            ControlTitle = "Выбор ячейки";
            DocumentationUrl = "/doc/l-pack-erp/warehouse/";
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

            SetDefaults();
            FormInit();

            //конструктор, будет вызван, когда объект создается
            //здесь создаются все внутренние структуры
            //впервые этот коллбэк будет вызван, когда данный таб станет активным
            //впервые (до этих пор, никакая работа внутри не происходит, что экономит ресурсы)
            OnLoad = () =>
            {
                ProcessPermissions();
                StorageGridInit();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                StorageGrid.Destruct();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                StorageGrid.ItemsAutoUpdate = true;
                StorageGrid.Run();
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                StorageGrid.ItemsAutoUpdate = false;
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
                    ButtonControl = RefreshStorageButton,
                    ButtonName = "RefreshStorageButton",
                    Action = () =>
                    {
                        StorageGrid.LoadItems();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "cancel",
                    Group = "main",
                    Enabled = true,
                    Title = "Отмена",
                    ButtonUse = true,
                    ButtonControl = CancelButton,
                    ButtonName = "CancelButton",
                    Action = () =>
                    {
                        Close();
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
                    ButtonControl = HelpButton,
                    ButtonName = "HelpButton",
                    HotKey = "F1",
                    Action = () =>
                    {
                        Central.ShowHelp(DocumentationUrl);
                    },
                });
            }

            Commander.SetCurrentGridName("StorageGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "save",
                    Title = "Оприходовать",
                    Group = "storage_grid_operation",
                    Enabled = true,
                    ButtonUse = true,
                    ButtonControl = SaveButton,
                    ButtonName = "SaveButton",
                    Action = () =>
                    {
                        DoSaveAction();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (CurrentItemAction != null)
                        {
                            if (StorageGrid != null && StorageGrid.Items != null && StorageGrid.Items.Count > 0)
                            {
                                if (StorageGrid.SelectedItem != null && StorageGrid.SelectedItem.Count > 0)
                                {
                                    // 2 - Свободна
                                    // 4 - Частично занята
                                    if (StorageGrid.SelectedItem.CheckGet("WMSS_ID").ToInt() == 2 || StorageGrid.SelectedItem.CheckGet("WMSS_ID").ToInt() == 4)
                                    {
                                        result = true;
                                    }
                                }
                            }
                        }

                        return result;
                    },
                });
            }

            Commander.Init(this);
        }

        public void DoSaveAction()
        {
            switch (CurrentItemAction)
            {
                case ItemAction.Register:
                    ArrivalItem();
                    break;
                case ItemAction.Move:
                    MoveItem();
                    break;
                case ItemAction.MoveAll:
                    MoveAllItem();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        private ListDataSet StorageDataSet { get; set; }

        /// <summary>
        /// Тип данного диплога определяющий действие над тмц и задающий какие контролы могут быть редактируемые
        /// </summary>
        public ItemAction CurrentItemAction;

        /// <summary>
        /// Перечисленны действия в зависимости от которых диалог будет предоставлять различные операции над тмц
        /// Register оприходовать
        /// Move перемещать
        /// </summary>
        public enum ItemAction
        { 
            Register, 
            Move,
            MoveAll
        };

        /// <summary>
        /// Выбранные записи в гриде кип
        /// </summary>
        public List<Dictionary<string, string>> ItemAllList { get; set; } 

        /// <summary>
        /// идентификатор складской единицы
        /// (primary key записи таблицы)
        /// </summary>
        public int ItemId { get; set; }

        /// <summary>
        /// количество складской единицы
        /// </summary>
        public double ItemQuantity { get; set; }

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// Ид хранилища, в котором находится складская единица
        /// </summary>
        private int SourceCellId { get; set; }

        public void SetDefaults()
        {
            Form = new FormHelper();
            StorageDataSet = new ListDataSet();

            WarehouseSelectBox.Items.Add("0", "Все склады");
            FormHelper.ComboBoxInitHelper(WarehouseSelectBox, "Warehouse", "Warehouse", "List", "WMWA_ID", "WAREHOUSE", null, true);
            WarehouseSelectBox.SelectedItem = new KeyValuePair<string, string>("0", "Все склады");
        }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void FormInit()
        {
            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TextName,
                    ControlType="TextBox",
                },
                new FormHelperField()
                {
                    Path="NUM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CurrentCellName,
                    ControlType="TextBox",
                },
            };

            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
            Form.StatusControl = Status;
        }

        private void StorageGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="WMST_ID",
                        Doc="ID хранилища",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ячейка",
                        Path="NUM",
                        Doc="Ячейка",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 100,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Состояние",
                        Path="STATUS",
                        Doc="Состояние",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 100,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Склад",
                        Path="WAREHOUSE",
                        Doc="Наименование склада",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth = 140,
                    },

                    new DataGridHelperColumn
                    {
                        Header="ИД зоны",
                        Path="WMZO_ID",
                        Doc="ID зоны",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД склада",
                        Path="WMWA_ID",
                        Doc="ID склада",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 5,
                        Hidden=true,
                    },
                    
                    new DataGridHelperColumn
                    {
                        Header=" ",
                        Path="_",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=5,
                        MaxWidth=2000,
                    },
                };
                StorageGrid.SetColumns(columns);
                StorageGrid.SetPrimaryKey("WMST_ID");
                StorageGrid.SearchText = FilterTextBox;
                StorageGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                StorageGrid.Toolbar = StorageGridToolbar;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                StorageGrid.OnSelectItem = selectedItem =>
                {
                    if (StorageGrid.Items != null && StorageGrid.Items.Count > 0)
                    {
                        if (selectedItem != null)
                        {
                            if (StorageGrid.Items.FirstOrDefault(x => x.CheckGet("WMST_ID").ToInt() == selectedItem.CheckGet("WMST_ID").ToInt()) == null)
                            {
                                StorageGrid.SelectRowFirst();
                            }
                        }
                        else
                        {
                            StorageGrid.SelectRowFirst();
                        }
                    }
                    else
                    {
                        StorageGrid.SelectedItem.Clear();
                    }
                };

                StorageGrid.OnFilterItems = () =>
                {
                    if (StorageGrid.Items != null && StorageGrid.Items.Count > 0)
                    {
                        if (CheckShowAll != null)
                        {
                            var items = new List<Dictionary<string, string>>();
                            if ((bool)CheckShowAll.IsChecked == true)
                            {
                                items = StorageGrid.Items;
                            }
                            else
                            {
                                // 2 - Свободна
                                // 4 - Частично занята
                                items.AddRange(StorageGrid.Items.Where(x => x.CheckGet("WMSS_ID").ToInt() == 2 || x.CheckGet("WMSS_ID").ToInt() == 4));
                            }

                            StorageGrid.Items = items;
                        }

                        if (WarehouseSelectBox != null && WarehouseSelectBox.SelectedItem.Key != null)
                        {
                            var key = WarehouseSelectBox.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            switch (key)
                            {
                                // Все склады
                                case 0:
                                    items = StorageGrid.Items;
                                    break;

                                default:
                                    items.AddRange(StorageGrid.Items.Where(x => x.CheckGet("WMWA_ID").ToInt() == key));
                                    break;
                            }

                            StorageGrid.Items = items;
                        }

                        if (ZoneSelectBox != null && ZoneSelectBox.SelectedItem.Key != null)
                        {
                            var key = ZoneSelectBox.SelectedItem.Key.ToInt();
                            var items = new List<Dictionary<string, string>>();

                            switch (key)
                            {
                                // Все зоны
                                case 0:
                                    items = StorageGrid.Items;
                                    break;

                                default:
                                    items.AddRange(StorageGrid.Items.Where(x => x.CheckGet("WMZO_ID").ToInt() == key));
                                    break;
                            }

                            StorageGrid.Items = items;
                        }
                    }

                    if (StorageGrid != null && StorageGrid.SelectedItem != null && StorageGrid.SelectedItem.Count > 0)
                    {
                        StorageGrid.Commands.Message = new ItemMessage() { Action = "refresh", Message = $"{StorageGrid.SelectedItem.CheckGet("WMST_ID").ToInt()}" };
                    }
                };

                StorageGrid.OnLoadItems = StorageGridLoadItems;

                StorageGrid.Commands = Commander;

                StorageGrid.Init();
            }
        }

        /// <summary>
        /// Функция загрузки таблицу данными
        /// </summary>
        private async void StorageGridLoadItems()
        {
            DisableControls();

            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "Storage");
            q.Request.SetParam("Action", "List");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            StorageDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    StorageDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            StorageGrid.UpdateItems(StorageDataSet);

            EnableControls();
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
        }

        private bool ConfirmUserAction()
        {
            bool res = false;

            string question = "Хотите установить ТМЦ " + Form.GetValueByPath("NAME") + " в ячейку: " + StorageGrid.SelectedItem.CheckGet("NUM");
            var dlg = new DialogWindow(question, "Сообщение", "", DialogWindowButtons.YesNo);
            if (dlg.ShowDialog() == true)
            {
                res = true;
            }
            else res = false;

            return res;
        }

        /// <summary>
        /// Перемещение тмц из одной ячейки в другую
        /// </summary>
        public void MoveItem()
        {
            if (ConfirmUserAction())
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("WMIT_ID", ItemId.ToString());
                    p.Add("FROM_WMST_ID", SourceCellId.ToString());
                    p.Add("TO_WMST_ID", StorageGrid.SelectedItem["WMST_ID"]);
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Warehouse");
                q.Request.SetParam("Object", "Item");
                q.Request.SetParam("Action", "Move");
                q.Request.SetParams(p);

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");

                        if (ds.Items.Count > 0)
                        {
                            int resultID = ds.Items[0].CheckGet("ID").ToInt();

                            if (resultID != 0)
                            {
                                // не должно появляться, что то произошло в функции на сервере, такое, что я не обработал в клиенте
                                DialogWindow.ShowDialog("Не удалось переместить данную позицию!");
                            }
                            else
                            {
                                //отправляем сообщение гриду о необходимости обновить данные
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup = "WMS",
                                    ReceiverName = "WMS_list",
                                    SenderName = "WMSItemCell",
                                    Action = "Refresh",
                                    Message = $"{ItemId}",
                                });

                                Central.Msg.SendMessage(new ItemMessage()
                                {
                                    ReceiverGroup = "WMS",
                                    ReceiverName = "WarehouseItemAccounting",
                                    SenderName = "WMSItemCell",
                                    Action = "refresh",
                                });

                                Central.Msg.SendMessage(new ItemMessage()
                                {
                                    ReceiverName = "MoldedContainerWarehouseScrapPaper",
                                    SenderName = "WMSItemCell",
                                    Action = "refresh",
                                });

                                Central.Msg.SendMessage(new ItemMessage()
                                {
                                    ReceiverName = "MoldedContainerWarehouseGoods",
                                    SenderName = "WMSItemCell",
                                    Action = "refresh",
                                });

                                Close();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Оприходование ячейки
        /// </summary>
        public void ArrivalItem()
        {
            if (ConfirmUserAction())
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("WMIT_ID", ItemId.ToString());
                    p.Add("WMST_ID", StorageGrid.SelectedItem["WMST_ID"]);
                    p.Add("QTY", ItemQuantity.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Warehouse");
                q.Request.SetParam("Object", "Item");
                q.Request.SetParam("Action", "Register");
                q.Request.SetParams(p);

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");

                        if (ds.Items.Count > 0)
                        {
                            int resultID = ds.Items[0].CheckGet("ID").ToInt();

                            if (resultID != 0)
                            {
                                // не должно появляться, что то произошло в функции на сервере, такое, что я не обработал в клиенте
                                DialogWindow.ShowDialog("Не удалось оприходовать данную позицию!");
                            }
                            else
                            {
                                //отправляем сообщение гриду о необходимости обновить данные
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup = "WMS",
                                    ReceiverName = "WMS_list",
                                    SenderName = "WMSItemCell",
                                    Action = "Refresh",
                                    Message = $"{ItemId}",
                                });

                                Central.Msg.SendMessage(new ItemMessage()
                                {
                                    ReceiverGroup = "WMS",
                                    ReceiverName = "WarehouseItemAccounting",
                                    SenderName = "WMSItemCell",
                                    Action = "refresh",
                                });

                                Central.Msg.SendMessage(new ItemMessage()
                                {
                                    ReceiverGroup = "WMS",
                                    ReceiverName = "WarehouseListArrival",
                                    SenderName = "WMSItemCell",
                                    Action = "refresh",
                                });

                                Central.Msg.SendMessage(new ItemMessage()
                                {
                                    ReceiverName = "MoldedContainerWarehouseScrapPaper",
                                    SenderName = "WMSItemCell",
                                    Action = "refresh",
                                });

                                Central.Msg.SendMessage(new ItemMessage()
                                {
                                    ReceiverName = "MoldedContainerWarehouseGoods",
                                    SenderName = "WMSItemCell",
                                    Action = "refresh",
                                });

                                Close();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// получение данных с сервера
        /// </summary>
        public async void GetData()
        {
            DisableControls();
            var p = new Dictionary<string, string>();
            switch (CurrentItemAction)
            {
                case ItemAction.Register:
                    {
                        p.CheckAdd("WMIT_ID", ItemId.ToString());
                    }
                    break;
                case ItemAction.Move:
                    {
                        p.CheckAdd("WMIT_ID", ItemId.ToString());
                    }
                    break;
                case ItemAction.MoveAll:
                    var first = ItemAllList.First();
                    var wmit_id = first.CheckGet("WMIT_ID");
                    {
                        p.CheckAdd("WMIT_ID", wmit_id);
                    }
                    break;
                default:
                    break;
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "Item");
            q.Request.SetParam("Action", "Get");
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
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        Form.SetValues(ds);
                        SourceCellId = ds.Items[0].CheckGet("WMST_ID").ToInt();
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            EnableControls();
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
            Central.WM.FrameMode = 1;

            GetData();

            string frameTitle = "";
            if (CurrentItemAction == ItemAction.Register)
            {
                frameTitle = "Оприходование";
                SaveButton.Content = "Оприходовать";
            }
            else if (CurrentItemAction == ItemAction.Move)
            {
                frameTitle = "Перемещение";
                SaveButton.Content = "Переместить";
            }
            
            TextName.IsReadOnly = true;

            this.FrameName = $"{FrameName}_{ItemId}";
            Central.WM.Show(FrameName, $"{frameTitle} {TextName.Text}", true, "add", this);
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            Central.WM.Close(this.FrameName);
        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel("[erp]warehouse_control");
            switch (mode)
            {
                // Если уровень доступа -- "Спецправа",
                case Role.AccessMode.Special:
                    break;

                case Role.AccessMode.FullAccess:
                    break;

                // Если уровень доступа -- "Только чтение",
                case Role.AccessMode.ReadOnly:
                    break;

                default:
                    break;
            }
        }

        public void GetZoneList()
        {
            ClearSelectBox(ZoneSelectBox);

            if (WarehouseSelectBox != null && WarehouseSelectBox.SelectedItem.Key != null)
            {
                ZoneSelectBox.Items.Add("0", "Все зоны");
                FormHelper.ComboBoxInitHelper(ZoneSelectBox, "Warehouse", "Zone", "ListByWarehouse", "WMZO_ID", "ZONE", new Dictionary<string, string>() { { "WMWA_ID", WarehouseSelectBox.SelectedItem.Key } }, true, false);
                ZoneSelectBox.SelectedItem = new KeyValuePair<string, string>("0", "Все зоны");
            }
            else
            {
                ZoneSelectBox.Items.Add("0", "Все зоны");
                ZoneSelectBox.SelectedItem = new KeyValuePair<string, string>("0", "Все зоны");
            }
        }

        /// <summary>
        /// Очищаем наполнение селектбокса
        /// </summary>
        /// <param name="selectBox"></param>
        private void ClearSelectBox(SelectBox selectBox)
        {
            selectBox.DropDownListBox.Items.Clear();
            selectBox.DropDownListBox.SelectedItem = null;
            selectBox.ValueTextBox.Text = "";
            selectBox.Items = new Dictionary<string, string>();
            selectBox.SelectedItem = new KeyValuePair<string, string>();
        }

        private void CheckShowAll_Checked(object sender, RoutedEventArgs e)
        {
            StorageGrid.UpdateItems();
        }

        private void CheckShowAll_Unchecked(object sender, RoutedEventArgs e)
        {
            StorageGrid.UpdateItems();
        }

        private void WarehouseSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GetZoneList();
            StorageGrid.UpdateItems();
        }

        private void ZoneSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            StorageGrid.UpdateItems();
        }

        /// <summary>
        /// Перемещение всех тмц из одной ячейки в другую
        /// </summary>
        public void MoveAllItem()
        {
            if (ConfirmUserAction())
            {
                var jsonString = JsonConvert.SerializeObject(ItemAllList);
                var p = new Dictionary<string, string>();
                {
                    p.Add("ITEMS", jsonString);
                    p.Add("FROM_WMST_ID", SourceCellId.ToString());
                    p.Add("TO_WMST_ID", StorageGrid.SelectedItem["WMST_ID"]);
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Warehouse");
                q.Request.SetParam("Object", "Item");
                q.Request.SetParam("Action", "MoveAll");
                q.Request.SetParams(p);

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");

                        if (ds.Items.Count > 0)
                        {
                            int resultID = ds.Items[0].CheckGet("ID").ToInt();

                            if (resultID != 0)
                            {
                                // не должно появляться, что то произошло в функции на сервере, такое, что я не обработал в клиенте
                                DialogWindow.ShowDialog("Не удалось переместить  позицию!");
                            }
                            else
                            {
                                //отправляем сообщение гриду о необходимости обновить данные
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup = "WMS",
                                    ReceiverName = "WMS_list",
                                    SenderName = "WMSItemCell",
                                    Action = "Refresh",
                                    Message = $"{ItemId}",
                                });

                                Central.Msg.SendMessage(new ItemMessage()
                                {
                                    ReceiverGroup = "WMS",
                                    ReceiverName = "WarehouseItemAccounting",
                                    SenderName = "WMSItemCell",
                                    Action = "refresh",
                                });

                                Central.Msg.SendMessage(new ItemMessage()
                                {
                                    ReceiverName = "MoldedContainerWarehouseScrapPaper",
                                    SenderName = "WMSItemCell",
                                    Action = "refresh",
                                });

                                Central.Msg.SendMessage(new ItemMessage()
                                {
                                    ReceiverName = "MoldedContainerWarehouseGoods",
                                    SenderName = "WMSItemCell",
                                    Action = "refresh",
                                });

                                Close();
                            }
                        }
                    }
                }
            }
        }




    }
}
