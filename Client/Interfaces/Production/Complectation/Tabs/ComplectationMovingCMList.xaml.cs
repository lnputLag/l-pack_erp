using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Stock;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Printing;
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
    /// Список поддонов перемещённых и находящихся в К -1.
    /// Основные операции:
    /// Отмена перемещения (возвращение поддона в К0);
    /// Повторная печать ярлыка.
    /// </summary>
    public partial class ComplectationMovingCMList : UserControl
    {
        public ComplectationMovingCMList()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            SetDefaults();

            MovingCMGridInit();

            ProcessPermissions();
        }

        /// <summary>
        /// Флаг особых прав (для отмены перемещения и печати ярлыка)
        /// </summary>
        public bool MasterFlag { get; set; }

        /// <summary>
        /// Выбранная запись в гриде перемещений
        /// </summary>
        public Dictionary<string, string> MovingCMSelectedItem { get; set; }

        /// <summary>
        /// Датасет с данными по перемещению (и находящимися там) поддонов в к -1
        /// </summary>
        public ListDataSet MovingCMDataSet { get; set; }

        public void SetDefaults()
        {
        }

        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            // Если есть полные права, даём возможность отменять комплектацию и повторно печатать ярлык
            var mode = Central.Navigator.GetRoleLevel("[erp]complectation_list");
            switch (mode)
            {
                case Role.AccessMode.FullAccess:
                    MasterFlag = true;
                    break;

                default:
                    MasterFlag = false;
                    break;
            }
        }

        private void MovingCMGridInit()
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
                    Header = "Наименование",
                    Path = "TOVAR_NAME",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 220,
                    MaxWidth = 360,
                },
                new DataGridHelperColumn
                {
                    Header = "Артикул",
                    Path = "TOVAR_ARTIKUL",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 120,
                    MaxWidth = 120,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество, шт.",
                    Path = "QTY",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 30,
                    MaxWidth = 80,
                },
                new DataGridHelperColumn
                {
                    Header = "Номер поддона",
                    Path = "PODDON_NUM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 30,
                    MaxWidth = 110,
                },
                new DataGridHelperColumn
                {
                    Header = "Склад",
                    Path = "SKLAD",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 60,
                    MaxWidth = 60,
                },
                new DataGridHelperColumn
                {
                    Header = "Место",
                    Path = "NUM_PLACE",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 60,
                    MaxWidth = 60,
                },

                new DataGridHelperColumn
                {
                    Header = " ",
                    Path = " ",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 5,
                    MaxWidth = 1200,
                },
            };


            Grid.SetColumns(columns);
            Grid.SetSorting("DT", ListSortDirection.Descending);
            Grid.SearchText = SearchText;
            Grid.Menu = new Dictionary<string, DataGridContextMenuItem>();
            Grid.OnLoadItems = MovingCMLoadItems;
            Grid.OnSelectItem = item =>
            {
                MovingCMSelectedItem = item;

                UpdateButtons();
            };

            Grid.AutoUpdateInterval = 0;
            Grid.Init();
            Grid.Run();
            Grid.Focus();
        }

        public async void MovingCMLoadItems()
        {
            Grid.ShowSplash();
            GridToolbar.IsEnabled = false;

            Grid?.ClearItems();

            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Complectation");
            q.Request.SetParam("Object", "Operation");
            q.Request.SetParam("Action", "ListMoving");

            q.Request.SetParams(p);

            q.Request.Timeout = 30000;
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
                    MovingCMDataSet = ListDataSet.Create(result, "ITEMS");
                    Grid.UpdateItems(MovingCMDataSet);
                }
            }

            Grid.HideSplash();
            GridToolbar.IsEnabled = true;
        }

        public void UpdateButtons()
        {
            LabelPrintButton.IsEnabled = false;

            if (MovingCMSelectedItem != null)
            {
                if (MovingCMSelectedItem.Count > 0)
                {
                    LabelPrintButton.IsEnabled = true;
                }
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
        /// Деструктор. Остановка вспомогательных процессов при закрытии вкладки
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии фрейма
            Messenger.Default.Send(new ItemMessage
            {
                ReceiverGroup = "ProductionComplectationList",
                ReceiverName = "",
                SenderName = "ComplectationMovingCMList",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры гридов
            Grid.Destruct();
        }

        private void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp-new/production_new/complectation/list_complectation/pallets_k-1");
            //Central.ShowHelp("/doc/l-pack-erp/production/complectation/complectation_list/moving_cm");
        }

        public void LabelPrint()
        {
            LabelReport2 report = new LabelReport2(true);
            report.PrintLabel(MovingCMSelectedItem.CheckGet("ID_PZ"), MovingCMSelectedItem.CheckGet("PODDON_NUM"), MovingCMSelectedItem.CheckGet("IDK1"), MovingCMSelectedItem.CheckGet("IDP").ToInt());
        }

        /// <summary>
        /// экспорт записей грида в Excel
        /// </summary>
        private async void ExportToExcel()
        {
            if (Grid.Items != null)
            {
                if (Grid.Items.Count > 0)
                {
                    var eg = new ExcelGrid();
                    var cols = Grid.Columns;
                    eg.SetColumnsFromGrid(cols);
                    eg.Items = Grid.Items;
                    await Task.Run(() =>
                    {
                        eg.Make();
                    });
                }
            }
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.Contains("Complectation"))
            {
                if (m.ReceiverName.Contains("MovingCMList"))
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            MovingCMLoadItems();
                            break;
                    }

                }
            }
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

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            SetPrintSettings();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void ExcelButton_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcel();
        }

        private void LabelPrintButton_Click(object sender, RoutedEventArgs e)
        {
            LabelPrint();
        }

        private void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            MovingCMLoadItems();
        }
    }
}
