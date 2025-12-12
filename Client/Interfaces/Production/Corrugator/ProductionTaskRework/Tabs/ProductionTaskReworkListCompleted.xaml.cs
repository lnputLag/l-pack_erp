using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Раскрой по ПЗ. Дублирование выполненного задания
    /// </summary>
    /// <author>Рясной П.В.</author>
    public partial class ProductionTaskReworkListCompleted : UserControl
    {
        /// <summary>
        /// Дублирование выполненного задания
        /// </summary>
        public ProductionTaskReworkListCompleted()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            ShippedTask = new Dictionary<string, string>();
            FactoryId = 1;

            ProcessPermissions();
        }

        public string TabName;

        public string RoleName = "[erp]production_task_cm_rework";

        /// <summary>
        /// данные из выбранной в гриде строки
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// Словарь с отгруженными заданиями
        /// </summary>
        Dictionary<string, string> ShippedTask { get; set; }
        /// <summary>
        /// Идентификатор производственной площадки, на которой выполняется ПЗГА
        /// </summary>
        public int FactoryId;

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
        // инициализация компонентов
        /// </summary>
        public void Init()
        {
            //колонки грида
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn()
                {
                    Header="Время Начала",
                    Path="TASK_START",
                    ColumnType=ColumnTypeRef.DateTime,
                    Format="dd.MM.yyyy HH:mm",
                    MinWidth=100,
                    MaxWidth=100,
                },
                new DataGridHelperColumn()
                {
                    Header="ИД ПЗ",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=60,
                    MaxWidth=60,
                },
                new DataGridHelperColumn()
                {
                    Header="Номер ПЗ",
                    Path="NUM",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=75,
                    MaxWidth=90,
                },
                new DataGridHelperColumn()
                {
                    Header="ГА",
                    Path="MACHINE",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=75,
                    MaxWidth=90,
                },
                new DataGridHelperColumn()
                {
                    Header="Длина, м",
                    Path="LEN",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=75,
                },
                new DataGridHelperColumn()
                {
                    Header="Профиль",
                    Path="PROFIL_NAME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=40,
                    MaxWidth=70,
                },
                new DataGridHelperColumn()
                {
                    Header="Формат",
                    Path="WIDTH",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=65,
                },
                new DataGridHelperColumn()
                {
                    Header=" ",
                    Path="_",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=5,
                    MaxWidth=1500,
                },
                new DataGridHelperColumn()
                {
                    Header="ИД профиля",
                    Path="ID_PROF",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn()
                {
                    Header="Обрезь %",
                    Path="TRIM",
                    ColumnType=ColumnTypeRef.Double,
                    Hidden=true,
                },
                new DataGridHelperColumn()
                {
                    Header="Флаг вес строго",
                    Path="FIXED_WEIGHT_FLAG",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn()
                {
                    Header="Выполнено",
                    Path="POSTING",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn()
                {
                    Header="Отгружено",
                    Path="AT_SHIPMENT",
                    ColumnType=ColumnTypeRef.String,
                    Hidden=true,
                },
                new DataGridHelperColumn()
                {
                    Header="Артикулы",
                    Path="ARTICLE",
                    ColumnType=ColumnTypeRef.String,
                    Hidden=true,
                },
            };
            Grid.SetColumns(columns);

            Grid.SetSorting("_ROWNNMBER", ListSortDirection.Ascending);
            Grid.SearchText = SearchText;
            Grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                // Цвета фона строк
                {
                    StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";
                        if (row["POSTING"].ToInt() == 0)
                        {
                            color = HColor.Blue;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
            };
            Grid.Init();

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdateActions(selectedItem);
                }
            };

            //двойной клик на строке откроет форму редактирования
            Grid.OnDblClick = selectedItem =>
            {
                Edit();
            };

            //данные грида
            Grid.OnLoadItems = LoadItems;
            Grid.OnFilterItems = FilterTaskItems;
            Grid.Run();

            //фокус ввода           
            Grid.Focus();
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            //Group ProductionTask
            if (m.ReceiverGroup.IndexOf("ProductionTask") > -1)
            {
                if (m.ReceiverName.IndexOf("RecuttingDuplicate") > -1)
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            Grid.LoadItems();
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// получение записей
        /// </summary>
        public async void LoadItems()
        {
            GridToolbar.IsEnabled = false;
            Grid.ShowSplash();

            bool resume = true;

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "ProductionTask");
                q.Request.SetParam("Action", "ListCompleteShort");
                // количество дней, за которые нам нужен список выполненных заданий
                q.Request.SetParam("DAYS", "1");
                q.Request.SetParam("FACTORY_ID", FactoryId.ToString());

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

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
                        var processedDs = ProcessItems(ds);
                        Grid.UpdateItems(processedDs);
                    }
                }
            }
            GridToolbar.IsEnabled = true;
            Grid.HideSplash();
        }

        /// <summary>
        /// Обработка данных перед загрузкой в таблицу
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        private ListDataSet ProcessItems(ListDataSet ds)
        {
            ListDataSet _ds = new ListDataSet();
            _ds.Init();

            if (ds.Items != null)
            {
                if (ds.Items.Count > 0)
                {
                    foreach (var row in ds.Items)
                    {
                        // Осталвляем только те записи, где в поле AT_SHIPMENT присутствует A
                        // Если только S, то запоминаем номера заданий для последующего информирования
                        string atShipment = row.CheckGet("AT_SHIPMENT");
                        if (atShipment == "S" || atShipment == "S,S")
                        {
                            string num = row.CheckGet("NUM").Substring(0, 4);
                            ShippedTask.CheckAdd(num, row.CheckGet("NUM"));
                        }
                        else
                        {
                            _ds.Items.Add(row);
                        }
                    }

                }
            }


            return _ds;
        }

        /// <summary>
        /// Фильтрация строк таблицы заданий
        /// </summary>
        private void FilterTaskItems()
        {
            if (Grid.GridItems != null)
            {
                // Номер для поиска берем из строки поиска
                string num = SearchText.Text;
                if (Grid.GridItems.Count > 0)
                {
                    var items = new List<Dictionary<string, string>>();

                    foreach (Dictionary<string, string> row in Grid.GridItems)
                    {
                        bool includeNum = true;
                        bool includeArticle = true;

                        if (num.Length > 1)
                        {
                            // Оставляем только строки с найденным номером задания
                            includeNum = false;
                            if (row.CheckGet("NUM").Contains(num))
                            {
                                includeNum = true;
                            }
                        }

                        if (num.Length > 2)
                        {
                            includeArticle = false;
                            if (row.CheckGet("ARTICLE").Contains(num))
                            {
                                includeArticle = true;
                            }
                        }

                        if (includeNum || includeArticle)
                        {
                            items.Add(row);
                        }
                    }

                    Grid.GridItems = items;
                }
                else
                {
                    if (num.Length == 4)
                    {
                        if (ShippedTask.ContainsKey(num))
                        {
                            var dw = new DialogWindow($"Задание {ShippedTask[num]} полностью отгружено. Перевыгон невозможен", "Перевыгон задания");
                            dw.ShowDialog();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// обновление методов работы с выбранной в гриде строкой
        /// </summary>
        /// <param name="selectedItem"></param>
        public void UpdateActions(Dictionary<string, string> selectedItem)
        {
            if (selectedItem != null)
            {
                SelectedItem = selectedItem;
            }
        }


        /// <summary>
        /// обработка ввода с клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
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

        /// <summary>
        /// отображение справочной статьи
        /// (относительный путь)
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/production/creating_tasks/task_rework");
        }

        /// <summary>
        /// редактирование записи
        /// </summary>
        public void Edit()
        {
            var id = SelectedItem.CheckGet("ID").ToInt();
            if (id > 0)
            {
                var duplicateWindow = new ProductionTaskReworkDuplicate();
                duplicateWindow.TaskId = id;
                duplicateWindow.FactoryId = FactoryId;
                duplicateWindow.PreviousTask = SelectedItem;
                duplicateWindow.ReturnTabName = TabName;
                duplicateWindow.Edit();
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        private void CloneButton_Click(object sender, RoutedEventArgs e)
        {
            Edit();
        }
    }
}
