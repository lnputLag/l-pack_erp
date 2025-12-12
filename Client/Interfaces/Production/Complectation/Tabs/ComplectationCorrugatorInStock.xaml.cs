using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Printing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
using Client.Interfaces.Stock;
using DevExpress.Printing.Native.PrintEditor;
using DevExpress.Xpf.Grid;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Комплектация ГА на складе
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class ComplectationCorrugatorInStock : UserControl
    {
        public ComplectationCorrugatorInStock()
        {
            InitializeComponent();
            RoleName = "[erp]complectation_cm";
            FrameName = "ComplectationCorrugatorInStock";

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            InitForm();
            SetDefaults();

            InitProductGrid();
            InitPalletGrid();
            InitNewPalletGrid();
            InitCompletedGrid();

            LoadProductItems();

            ProcessPermissions();
        }

        public string RoleName { get; set; }

        /// <summary>
        /// Техническое имя таба
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// Выбранная запись в гриде товаров, которые находятся в К0 для комплектации
        /// </summary>
        private Dictionary<string, string> SelectedProductItem { get; set; }

        /// <summary>
        /// Датасет товаров, которые находятся в К0 для комплектации
        /// </summary>
        private ListDataSet ProductDataSet { get; set; }

        /// <summary>
        /// Выбранная запись в гриде поддонов, из которых будут комплектоваться новые
        /// </summary>
        public Dictionary<string, string> SelectedPalletItem { get; set; }

        /// <summary>
        /// Датасет поддонов, из которых будут комплектоваться новые
        /// </summary>
        private ListDataSet PalletDataSet { get; set; }

        private ListDataSet NewPalletDataSet { get; set; }

        /// <summary>
        /// Выбранная запис в гриде скомплектованных позиций
        /// </summary>
        public Dictionary<string, string> SelectedCompletedItem { get; set; }

        /// <summary>
        /// Датасет с данными по скомплектованным позициям
        /// </summary>
        public ListDataSet CompletedDataSet { get; set; }

        #region default functions

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m">Сообщение</param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.Contains("Complectation"))
            {
                if (m.ReceiverName == this.FrameName)
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            SetDefaults();
                            LoadProductItems();
                            CompletedGrid.LoadItems();
                            break;
                    }
                }
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
        }

        /// <summary>
        /// Инициализация формы
        /// </summary>
        public void InitForm()
        {

        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            ProductDataSet = new ListDataSet();
            PalletDataSet = new ListDataSet();
            CompletedDataSet = new ListDataSet();
            NewPalletDataSet = new ListDataSet();

            SelectedCompletedItem = new Dictionary<string, string>();

            PalletGrid.UpdateItems(PalletDataSet);
            ProductGrid.UpdateItems(ProductDataSet);
            CompletedGrid.UpdateItems(CompletedDataSet);
            NewPalletGrid.UpdateItems(NewPalletDataSet);
        }

        /// <summary>
        /// Вызов справки
        /// </summary>
        private void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/");
        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
        private void Close()
        {
            Central.WM.RemoveTab(FrameName);
            Destroy();
        }

        /// <summary>
        /// Деструктор компонентов. Завершает вспомогательные процессы
        /// </summary>
        private void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage
            {
                ReceiverGroup = "Complectation",
                ReceiverName = "",
                SenderName = FrameName,
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// Обработчик нажатий клавиатуры
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessKeyboard(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// Отображение вкладки с формой редактирования данных
        /// </summary>
        private void Show()
        {
            var title = "Комплектация ГА на СГП";
            Central.WM.AddTab(FrameName, title, true, "add", this);
        }

        #endregion


        #region init grids

        private void InitProductGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "Наименование",
                    Path = "NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn
                {
                    Header = $"Всего на поддонах, шт.{Environment.NewLine}Суммарное количество продукции на поддонах в К0 по этой заявке",
                    Path = "KOL_SUM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header = $"На полном, шт.{Environment.NewLine}Количество продукции на полном поддоне по умолчанию",
                    Path = "KOL_PAK",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header = $"По заявке, шт.{Environment.NewLine}Количество продукции необходимое для этой заявки",
                    Path = "KOL_ORDER",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header = $"Произведено, шт.{Environment.NewLine}Количество продукции произведённой для этой заявки",
                    Path = "CREATE_BY_ORDER",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header = $"Заданий на ГА, шт.{Environment.NewLine}Количество не законченных проиводственных заданий по этой заявке на гофроагрегатах",
                    Path = "C",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header = $"Поддонов, шт.{Environment.NewLine}Количество поддонов в К0 по этой заявке",
                    Path = "PALLETS",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=5,
                },

                new DataGridHelperColumn
                {
                    Header = "Закончить до",
                    Path = "FINISH_BEFORE_DTTM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Width2=15,
                },
                new DataGridHelperColumn
                {
                    Header = "Дата/Время следующего этапа",
                    Path = "DTTM_PZ",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Width2=15,
                },

                new DataGridHelperColumn
                {
                    Header = "Ид заявки",
                    Path = "IDORDERDATES",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=5,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header = "К/КХ",
                    Path = "SKLAD",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2=5,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header = "Ячейка",
                    Path = "NUM_PLACE",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2=5,
                    Hidden=true,
                },
                
                new DataGridHelperColumn
                {
                    Header = " ",
                    Path = "_",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 5,
                    MaxWidth = 800,
                },
            };

            ProductGrid.AutoUpdateInterval = 60 * 5;
            ProductGrid.OnLoadItems = LoadProductItems;
            ProductGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            ProductGrid.SetColumns(columns);
            ProductGrid.SetSorting("FINISH_BEFORE_DTTM");

            ProductGrid.Menu = new Dictionary<string, DataGridContextMenuItem>();

            ProductGrid.OnSelectItem = selectedItem =>
            {
                SelectedProductItem = selectedItem;
                if (selectedItem != null)
                {
                    NewPalletDataSet.Items.Clear();
                    NewPalletGrid.UpdateItems(NewPalletDataSet);

                    LoadPalletItems();

                    UpdateNewPalletButtons();
                }
            };

            ProductGrid.RowStylers =
            new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>
            {
                {
                    DataGridHelperColumn.StylerTypeRef.BackgroundColor, row =>
                    {
                        var color = "";

                        // если паллет больше одного подсветить. попоросили оставить. сами не знают для чего.
                        if (row.CheckGet("PALLETS").ToInt() > 1)
                        {
                            color = HColor.Blue;
                        }

                        if ((row.CheckGet("FINISH_BEFORE_DTTM").ToDateTime("dd.MM.yyyy HH:mm:ss") - DateTime.Now).TotalHours <= 2)
                        {
                             color = HColor.Yellow;

                            if ((row.CheckGet("FINISH_BEFORE_DTTM").ToDateTime("dd.MM.yyyy HH:mm:ss") - DateTime.Now).TotalHours <= 1)
                            {
                                color = HColor.Orange;

                                if ((row.CheckGet("FINISH_BEFORE_DTTM").ToDateTime("dd.MM.yyyy HH:mm:ss") - DateTime.Now).TotalHours <= 0)
                                {
                                    color = HColor.Red;
                                }
                            }
                        }

                        var result = !string.IsNullOrEmpty(color) ? color.ToBrush() : DependencyProperty.UnsetValue;
                        return result;
                    }
                }
            };

            ProductGrid.Init();
            ProductGrid.Run();

            ProductGrid.UpdateItems(ProductDataSet);
        }

        /// <summary>
        /// Инициализация грида списываемых поддонов
        /// </summary>
        private void InitPalletGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "*",
                    Path = "SelectedFlag",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth = 45,
                    MaxWidth = 45,
                    Editable = true,
                    OnClickAction = (row, el) =>
                    {
                        if (row.CheckGet("SelectedFlag").ToBool())
                        {
                            NewPalletDataSet.Items.Add(row);
                            NewPalletGrid.UpdateItems(NewPalletDataSet);
                        }
                        else
                        {
                            NewPalletDataSet.Items.Remove(row);
                            NewPalletGrid.UpdateItems(NewPalletDataSet);
                        }

                        //PalletGrid.UpdateItems();
                         UpdateNewPalletButtons();

                        return true;
                    },
                },
                new DataGridHelperColumn
                {
                    Header = "Поддон",
                    Path = "PALLET",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 60,
                    MaxWidth = 60,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество на поддоне, шт.",
                    Path = "KOL",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 50,
                    MaxWidth = 170,
                },
                new DataGridHelperColumn
                {
                    Header = "Причина забраковки",
                    Path = "DESCRIPTION",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 70,
                    MaxWidth = 500,
                },
                new DataGridHelperColumn
                {
                    Header = "Кондиционирование, мин.",
                    Path = "COND_MIN",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 35,
                    MaxWidth = 165,
                },
                new DataGridHelperColumn
                {
                    Header = "Профиль",
                    Path = "PROFILE_NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 30,
                    MaxWidth = 70,
                },
                new DataGridHelperColumn
                {
                    Header = "Дата перемещения",
                    Path = "MOVING_DTTM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 60,
                    MaxWidth = 110,
                },
                new DataGridHelperColumn
                {
                    Header = "Ид",
                    Path = "PALLET_ID",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 60,
                    MaxWidth = 60,
                },
                new DataGridHelperColumn
                {
                    Header = "К/КХ",
                    Path = "SKLAD",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 40,
                    MaxWidth = 45,
                    Hidden = true,
                },
                new DataGridHelperColumn
                {
                    Header = "Начало кондиционирования",
                    Path = "CONDITION_DTTM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Hidden = true,
                },

                new DataGridHelperColumn
                {
                    Header = " ",
                    Path = "_",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 5,
                    MaxWidth = 800,
                },
            };
            PalletGrid.SetColumns(columns);

            PalletGrid.SetSorting("PALLET");

            PalletGrid.Menu = new Dictionary<string, DataGridContextMenuItem>()
            {
                {
                    "MovingHistory",
                    new DataGridContextMenuItem()
                    {
                        Header="История перемещения",
                        Action=()=>
                        {
                            MovingHistory(PalletGrid.SelectedItem);
                        },
                    }
                },
            };

            PalletGrid.OnSelectItem = selectedItem =>
            {
                SelectedPalletItem = selectedItem;
                UpdateNewPalletButtons();
            };

            PalletGrid.Init();
            PalletGrid.Run();
            PalletGrid.Focus();
        }

        private void InitNewPalletGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "Ид",
                    Path = "PALLET_ID",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 60,
                    MaxWidth = 60,
                },
                new DataGridHelperColumn
                {
                    Header = "Поддон",
                    Path = "PALLET",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 60,
                    MaxWidth = 60,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество на поддоне, шт.",
                    Path = "QTY",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 50,
                    MaxWidth = 170,
                },


                new DataGridHelperColumn
                {
                    Header = " ",
                    Path = "_",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 5,
                    MaxWidth = 800,
                },
            };
            NewPalletGrid.SetColumns(columns);
            NewPalletGrid.SetSorting("PALLET");
            
            NewPalletGrid.Init();
            NewPalletGrid.Run();
            NewPalletGrid.Focus();
        }

        /// <summary>
        /// Инициализация грида с скомплектованными позициями
        /// </summary>
        private void InitCompletedGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "Поддон",
                    Path = "PALLET_FULL_NUMBER",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 32,
                    MaxWidth = 50,
                },
                new DataGridHelperColumn
                {
                    Header = "Продукция",
                    Path = "PRODUCT_NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 120,
                    MaxWidth = 1600,
                },
                new DataGridHelperColumn
                {
                    Header = "Артикул",
                    Path = "PRODUCT_CODE",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 60,
                    MaxWidth = 120,
                },
                new DataGridHelperColumn
                {
                    Header = "Дата",
                    Path = "DT",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.DateTime,
                    Format = "HH:mm dd.MM.yyyy",
                    MinWidth = 65,
                    MaxWidth = 110,
                },
                new DataGridHelperColumn
                {
                    Header = "Ид",
                    Path = "PALLET_ID",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 40,
                    MaxWidth = 70,
                },
            };

            CompletedGrid.SetColumns(columns);

            CompletedGrid.AutoUpdateInterval = 0;

            CompletedGrid.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>
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

                        if (!string.IsNullOrEmpty(color))
                        {
                            result = color.ToBrush();

                        }
                        return result;
                    }
                },
            };

            CompletedGrid.OnSelectItem = selectedItem =>
            {
                SelectedCompletedItem = selectedItem;

                UpdateNewPalletButtons();
            };

            CompletedGrid.OnLoadItems = LoadCompletedItems;

            CompletedGrid.Init();
            CompletedGrid.Run();
        }

        #endregion

        #region load data

        private async void LoadPalletItems()
        {
            PalletGrid.ShowSplash();

            PalletGrid.ClearItems();

            if (SelectedProductItem != null)
            {
                var p = new Dictionary<string, string>();
                p.CheckAdd("ID2", SelectedProductItem.CheckGet("ID2"));
                p.CheckAdd("SKLAD", SelectedProductItem.CheckGet("SKLAD"));
                p.CheckAdd("NUM_PLACE", SelectedProductItem.CheckGet("NUM_PLACE"));
                p.CheckAdd("IDORDERDATES", SelectedProductItem.CheckGet("IDORDERDATES"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Complectation");
                q.Request.SetParam("Object", "Pallet");
                q.Request.SetParam("Action", "ListComplectationCorrugatorInStock");

                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() => 
                {
                    q.DoQuery();
                });

                PalletDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                    if (result != null)
                    {
                        PalletDataSet = ListDataSet.Create(result, "List");

                        foreach (var item in PalletDataSet.Items)
                        {
                            item.Add("SelectedFlag", 0.ToString());

                            // Если профиль картона "Е"
                            // то уменьшаем толщину картона на 0,01
                            // UPD 03/07/2023
                            // Округляем толщину Е картона до 1 знака после запятой
                            // UPD 04/07/2023
                            // Возвращаем ночальную толщину Е картона. Увеличиваем толщину Е картона на 0,035 (1.43+3,5)
                            // UPD 05/09/2024 для Е картона толщину брать как 1,5
                            // UPD 15/10/2024 для Е картона толщину брать как 1,45
                            if (item.CheckGet("PROFILE").ToInt() == 4)
                            {
                                //double thiknes = item.CheckGet("THIKNES").ToDouble() + 0.035;
                                double thiknes = 1.47;
                                item.CheckAdd("THIKNES", thiknes.ToString());
                            }
                        }
                    }
                }
                PalletGrid.UpdateItems(PalletDataSet);
            }

            PalletGrid.HideSplash();
        }

        private async void LoadProductItems()
        {
            ProductGrid.ShowSplash();

            ProductGrid.ClearItems();

            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Complectation");
            q.Request.SetParam("Object", "Product");
            q.Request.SetParam("Action", "ListComplectationCorrugatorInStock");

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
                    ProductDataSet = ListDataSet.Create(result, "List");
                    ProductGrid.UpdateItems(ProductDataSet);
                }
            }

            ProductGrid.HideSplash();
        }

        /// <summary>
        /// Получедие данных для грида с скомплектованными позициями
        /// </summary>
        public async void LoadCompletedItems()
        {
            CompletedGridToolbar.IsEnabled = false;

            CompletedGrid.ShowSplash();

            CompletedGrid.ClearItems();

            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Complectation");
            q.Request.SetParam("Object", "Operation");
            q.Request.SetParam("Action", "ListCorrugatorInStock");

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
                    CompletedDataSet = ListDataSet.Create(result, "ITEMS");
                    CompletedGrid.UpdateItems(CompletedDataSet);
                }
            }
            else
            {
                q.ProcessError();
            }

            CompletedGrid.HideSplash();

            CompletedGridToolbar.IsEnabled = true;
        }

        #endregion

        /// <summary>
        /// Получить историю перемещения по выбранному поддону
        /// </summary>
        public void MovingHistory(Dictionary<string, string> selectedItem)
        {
            if (selectedItem != null && selectedItem.Count > 0)
            {
                string movingId = selectedItem.CheckGet("IDPER");
                string incomingId = selectedItem.CheckGet("IDP");
                string palletNumber = selectedItem.CheckGet("NUM");

                if (!string.IsNullOrEmpty(palletNumber) && !string.IsNullOrEmpty(incomingId))
                {
                    var p = new Dictionary<string, string>();
                    p.Add("idp", incomingId);
                    p.Add("num", palletNumber);
                    p.Add("ID_PER", movingId);

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

        public void EditNewPallet()
        {
            if (NewPalletGrid.SelectedItem != null && NewPalletGrid.SelectedItem.Count > 0)
            {
                var numberEditView = new ComplectationNumberEdit { Value = NewPalletGrid.SelectedItem.CheckGet("QTY").ToInt() };
                numberEditView.Show();
                if (numberEditView.OkFlag)
                {
                    if (numberEditView.Value > NewPalletGrid.SelectedItem.CheckGet("QTY").ToInt())
                    {
                        DialogWindow.ShowDialog("Нельзя скомплектовать больше товара чем было до комплектации. Уменьшите количество получаемого товара", "Комплектация ГА на СГП");
                    }
                    else
                    {
                        NewPalletGrid.SelectedItem.CheckAdd("QTY", numberEditView.Value.ToString());
                        NewPalletGrid.UpdateItems(NewPalletDataSet);

                        UpdateNewPalletButtons();
                    }
                }
            }
        }

        /// <summary>
        /// Комплактация на ГА
        /// </summary>
        public async void ComplectationCM()
        {
            var resume = true;
            DisableControls();

            List<Dictionary<string, string>> oldPalletList = PalletGrid.Items.Where(x => x.CheckGet("SelectedFlag").ToInt() == 1).ToList();

            if (oldPalletList != null && oldPalletList.Count > 0)
            {
                if (oldPalletList[0].CheckGet("ID2").ToInt() == ProductGrid.SelectedItem.CheckGet("ID2").ToInt())
                {
                    int summaryQuantityOnOldPallet = oldPalletList.Sum(x => x.CheckGet("KOL").ToInt());
                    int summaryQuantityOnNewPallet = 0;
                    if (NewPalletGrid.Items != null)
                    {
                        summaryQuantityOnNewPallet = NewPalletGrid.Items.Sum(x => x.CheckGet("QTY").ToInt());
                    }

                    if (summaryQuantityOnOldPallet < summaryQuantityOnNewPallet)
                    {
                        DialogWindow.ShowDialog("Нельзя скомплектовать больше товара чем было до комплектации. Уменьшите суммарное количество получаемого товара", "Комплектация ГА на СГП");
                        resume = false;
                    }

                    // Ид причины комплектации/списания
                    var reasonId = "0";
                    // Описание причины комплектации/списания
                    var reasonMessage = "";

                    if (resume)
                    {
                        // Если есть списание
                        if (summaryQuantityOnNewPallet < summaryQuantityOnOldPallet)
                        {
                            var view = new ComplectationWriteOffReasonsEdit();
                            view.CorrugatorFlag = 1;
                            view.Show();

                            if (view.OkFlag)
                            {
                                reasonId = view.SelectedReason.Key;
                            }

                            if (reasonId.ToInt() > 0)
                            {
                                resume = true;
                            }
                            else
                            {
                                resume = false;
                            }
                        }
                    }

                    if (resume)
                    {
                        var message =
                            $"Будет списано{Environment.NewLine}" +
                            $"{summaryQuantityOnOldPallet - summaryQuantityOnNewPallet} шт.{Environment.NewLine}" +
                            $"Продолжить?";

                        if (DialogWindow.ShowDialog(message, "Комплектация ГА на СГП", "", DialogWindowButtons.YesNo) != true)
                        {
                            resume = false;
                        }
                    }

                    // Комплектуем
                    if (resume)
                    {
                        SplashControl.Visible = true;

                        var p = new Dictionary<string, string>
                        {
                            ["Product"] = JsonConvert.SerializeObject(SelectedProductItem),
                            ["OldPalletList"] = JsonConvert.SerializeObject(oldPalletList),
                            ["NewPalletList"] = JsonConvert.SerializeObject(NewPalletGrid.Items),

                            ["idorderdates"] = ProductGrid.SelectedItem.CheckGet("IDORDERDATES"),

                            ["StanokId"] = ComplectationPlace.CorrugatingMachines,
                            ["ReasonId"] = reasonId,
                            ["ReasonMessage"] = reasonMessage
                        };

                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Complectation");
                        q.Request.SetParam("Object", "Pallet");
                        q.Request.SetParam("Action", "CreateCorrugatorInStockPallet");

                        q.Request.SetParams(p);

                        await Task.Run(() => { q.DoQuery(); });

                        if (q.Answer.Status == 0)
                        {
                            var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                            if (result != null)
                            {
                                var ds = ListDataSet.Create(result, "ITEMS");
                                if (ds.Items.Count > 0)
                                {
                                    // Если есть списание
                                    if (summaryQuantityOnNewPallet < summaryQuantityOnOldPallet)
                                    {
                                        foreach (var oldPalletItem in oldPalletList)
                                        {
                                            var newPalletItem = NewPalletGrid.Items.FirstOrDefault(x => x.CheckGet("PALLET_ID").ToInt() == oldPalletItem.CheckGet("PALLET_ID").ToInt());
                                            if (newPalletItem != null)
                                            {
                                                if (newPalletItem.CheckGet("QTY").ToInt() < oldPalletItem.CheckGet("KOL").ToInt())
                                                {
                                                    LabelReport2 report = new LabelReport2(true);
                                                    report.PrintLabel(oldPalletItem.CheckGet("ID_PZ"), oldPalletItem.CheckGet("NUM"), ProductGrid.SelectedItem.CheckGet("IDK1"), oldPalletItem.CheckGet("IDP").ToInt());
                                                }
                                            }
                                        }
                                    }

                                    Refresh();
                                }
                                else
                                {
                                    var msg = "Произошла ошибка. Пожалуйста сообщите о проблеме.";
                                    var d = new DialogWindow($"{msg}", "Комплектация ГА на СГП", "", DialogWindowButtons.OK);
                                    d.ShowDialog();
                                }
                            }
                            else
                            {
                                var msg = "Произошла ошибка. Пожалуйста сообщите о проблеме.";
                                var d = new DialogWindow($"{msg}", "Комплектация ГА на СГП", "", DialogWindowButtons.OK);
                                d.ShowDialog();
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
                    var msg = $"Продукция на поддонах не совпадает с выбранной. Пожалуйста, повторите операцию.";
                    var d = new DialogWindow($"{msg}", "Комплектация ГА на СГП", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                var msg = $"Необходимо выбрать хотя бы один поддон из которого будем комплектовать.";
                var d = new DialogWindow($"{msg}", "Комплектация ГА на СГП", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }

            SplashControl.Visible = false;
            EnableControls();
        }

        /// <summary>
        /// Делает неактивными все тулбары вкладки
        /// </summary>
        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
            CompletedGridToolbar.IsEnabled = false;
        }

        /// <summary>
        /// Делает активными все тулбары вкладки
        /// Вызывает метод установки активности кнопок
        /// </summary>
        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
            CompletedGridToolbar.IsEnabled = true;

            UpdateNewPalletButtons();
        }

        /// <summary>
        /// обновляем доступность кнопок новых поддонов в зависимости от ситуации
        /// </summary>
        private void UpdateNewPalletButtons()
        {
            var palletsSelected = PalletGrid.Items?.Count(row => row.CheckGet("SelectedFlag").ToInt() == 1) > 0;
            ComplectationButton.IsEnabled = palletsSelected;

            if (NewPalletGrid.Items != null && NewPalletGrid.Items.Count > 0)
            {
                NewPalletEditButton.IsEnabled = true;
            }
            else
            {
                NewPalletEditButton.IsEnabled = false;
            }

            if (SelectedCompletedItem != null)
            {
                if (SelectedCompletedItem.Count > 0)
                {
                    LabelPrintButton.IsEnabled = true;
                }
                else
                {
                    LabelPrintButton.IsEnabled = false;
                }
            }
            else
            {
                LabelPrintButton.IsEnabled = false;
            }

            {
                if (palletsSelected)
                {
                    ConsumptionPalletQuantityLabel.Content = PalletGrid.Items?.Count(row => row.CheckGet("SelectedFlag").ToInt() == 1);
                    ConsumptionQuantityLabel.Content = PalletGrid.Items?.Where(row => row.CheckGet("SelectedFlag").ToInt() == 1).Sum(row => row.CheckGet("KOL").ToInt());

                    IncomingPalletQuantityLabel.Content = NewPalletGrid.Items?.Count();
                    IncomingQuantityLabel.Content = NewPalletGrid.Items?.Sum(row => row.CheckGet("QTY").ToInt());
                }
                else
                {
                    ConsumptionPalletQuantityLabel.Content = 0;
                    ConsumptionQuantityLabel.Content = 0;

                    IncomingPalletQuantityLabel.Content = 0;
                    IncomingQuantityLabel.Content = 0;
                }
            }

            ProcessPermissions();
        }

        /// <summary>
        /// Печать ярлыка выбранной комплектации;
        /// Открывает окно выбора поддонов, по которым нужно напечатать ярлык
        /// </summary>
        public void LabelPrint()
        {
            if (SelectedCompletedItem != null)
            {
                LabelReport2 report = new LabelReport2(true);
                report.PrintLabel(SelectedCompletedItem.CheckGet("PRODUCTION_TASK_ID"), SelectedCompletedItem.CheckGet("PALLET_NUMBER"), SelectedCompletedItem.CheckGet("PRODUCT_IDK1"));
            }
            else
            {
                var msg = "Не выбран поддон";
                var d = new DialogWindow($"{msg}", "Комплектация ГА на СГП", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Обновляет данные для всех гридов формы
        /// </summary>
        public void Refresh()
        {
            SetDefaults();

            if (PalletDataSet.Items != null)
            {
                PalletDataSet.Items.Clear();
                PalletGrid.UpdateItems(PalletDataSet);
            }

            LoadProductItems();

            CompletedGrid.LoadItems();

            UpdateNewPalletButtons();
        }

        /// <summary>
        /// Установка настроек для принтера
        /// </summary>
        public void SetPrintSettings()
        {
            LabelReport2.SetPrintingProfile();
        }

        private void ComplectationButton_Click(object sender, RoutedEventArgs e)
        {
            ComplectationCM();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void LabelPrintButton_Click(object sender, RoutedEventArgs e)
        {
            LabelPrint();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            SetPrintSettings();
        }

        private void NewPalletEditButton_Click(object sender, RoutedEventArgs e)
        {
            EditNewPallet();
        }
    }
}
