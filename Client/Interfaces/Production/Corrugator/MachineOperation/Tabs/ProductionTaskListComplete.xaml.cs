using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Производственные задания, выполенные задания
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>2</version>
    /// <released>2021-11-16</released>
    public partial class ProductionTaskListComplete:UserControl
    {
        public ProductionTaskListComplete()
        {
            InitializeComponent();

            SelectedItemId=0;

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this,ProcessMessages);

            //регистрация обработчика клавиатуры
            PreviewKeyDown+=ProcessKeyboard;

            SetDefaults();
            InitGrid();

            ProcessPermissions();
        }

        public string RoleName = "[erp]corrugator_work_log";

        /// <summary>
        /// датасет, содержащий данные
        /// </summary>
        public ListDataSet ProductionTasksDS { get; set; }
        public ListDataSet PositionsDS { get; set; }

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        public int SelectedItemId { get; set; }

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
        /// обработчик сообщений
        /// </summary>
        private void ProcessMessages(ItemMessage m)
        {
            //Group ProductionTask
            if(m.ReceiverGroup.IndexOf("ProductionTask") > -1)
            {
                switch(m.Action)
                {
                    case "Refresh":
                        Grid.LoadItems();
                        break;
                }
            }
        }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        private void ProcessKeyboard(object sender,System.Windows.Input.KeyEventArgs e)
        {
            switch(e.Key)
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
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/production/production_tasks/complete");
        }

        /// <summary>
        /// деструктор интерфейса
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="ProductionTask",
                ReceiverName = "",
                SenderName = "ProductionTaskListCompleteView",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            Grid.Destruct();
            Grid2.Destruct();
        }

        /// <summary>
        /// инициализация грида
        /// </summary>
        public void InitGrid()
        {
            //инициализация грида
            {
                //список колонок грида
                var columns = new List<DataGridHelperColumn>()
                {
                    new DataGridHelperColumn
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        MinWidth=30,
                        MaxWidth=30,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="ИД ПЗ",
                        Path="ID",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        MinWidth=50,
                        MaxWidth=60,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Создание",
                        Path="DATA",
                        Group="Дата",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm",
                        MinWidth=110,
                        MaxWidth=130,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Начала выполнения",
                        Path="TASKSTART",
                        Group="Дата",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm",
                        MinWidth=110,
                        MaxWidth=130,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Окончания выполнения",
                        Path="TASKFINISH",
                        Group="Дата",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm",
                        MinWidth=130,
                        MaxWidth=150,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Номер ПЗ",
                        Path="NUM",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=70,
                        MaxWidth=90,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Название станка",
                        Path="CORRUGATORNAME",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=60,
                        MaxWidth=90,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Длина, м",
                        Path="LENGTH",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=70,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Длина ПЗ факт, м",
                        Path="LENGTH_FACT",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=70,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Ширина, мм",
                        Path="WIDTH",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        MinWidth=70,
                        MaxWidth=85,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Профиль",
                        Path="PROFILENAME",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=55,
                        MaxWidth=75,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Формат",
                        Path="WIDTH",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width=60,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Качество",
                        Path="QUALITYID",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=50,
                        MaxWidth=70,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Обрезь, %",
                        Path="TRIMPERCENTAGE",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Double,
                        MinWidth=60,
                        MaxWidth=75,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Количество вырубленных листов",
                        Path="QTY_CUTDOWN",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        MinWidth=190,
                        MaxWidth=210,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Склеенный слой",
                        Path="GLUED",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        MinWidth=90,
                        MaxWidth=110,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Тандем",
                        Path="TANDEM",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        MinWidth=50,
                        MaxWidth=60,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Смена",
                        Path="SHIFT",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        MinWidth=20,
                        MaxWidth=60,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Теоретическая",
                        Path="THEORETICALSPEED",
                        Group="Cкорость, м/мин",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        MinWidth=60,
                        MaxWidth=90,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Фактическая",
                        Path="ACTUALSPEED",
                        Group="Cкорость, м/мин",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        MinWidth=60,
                        MaxWidth=90,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Статистическая",
                        Path="STATISTIC_SPEED",
                        Group="Cкорость, м/мин",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        MinWidth=60,
                        MaxWidth=90,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Теоретическое",
                        Path="THEORETICALTIME",
                        Group="Время, мин",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Double,
                        MinWidth=80,
                        MaxWidth=100,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Фактическое",
                        Path="ACTUALTIME",
                        Group="Время, мин",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Double,
                        MinWidth=60,
                        MaxWidth=90,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Статистическое",
                        Path="STATISTIC_TIME",
                        Group="Время, мин",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Double,
                        MinWidth=60,
                        MaxWidth=90,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Композиция",
                        Path="DESCRIPTION",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=120,
                        MaxWidth=180,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="1",
                        Path="LAYER1MARK",
                        Group="Слои",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=60,
                        MaxWidth=90,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="2",
                        Path="LAYER2MARK",
                        Group="Слои",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=60,
                        MaxWidth=90,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="3",
                        Path="LAYER3MARK",
                        Group="Слои",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=60,
                        MaxWidth=90,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="4",
                        Path="LAYER4MARK",
                        Group="Слои",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=60,
                        MaxWidth=90,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="5",
                        Path="LAYER5MARK",
                        Group="Слои",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=60,
                        MaxWidth=90,
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
                Grid.SetColumns(columns);
                Grid.SetSorting("_ROWNUMBER",ListSortDirection.Ascending);
                Grid.SearchText=SearchText;
                Grid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                Grid.OnSelectItem=(Dictionary<string,string> selectedItem) =>
                {
                    if(selectedItem.Count > 0)
                    {
                        UpdateActions(selectedItem);
                    }
                };

                //данные грида
                Grid.OnLoadItems=LoadItems;
                //Grid.OnFilterItems=FilterItems;
                Grid.Run();

                //фокус ввода           
                Grid.Focus();
            }


             //позиции задания
            {
                //список колонок грида
                var columns = new List<DataGridHelperColumn>()
                {
                    new DataGridHelperColumn()
                    {
                        Header="Стекер",
                        Path="CUTOFFALLOCATION",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width=55,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Категория",
                        Path="IDK1",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=40,
                        Width=250,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Наименование",
                        Path="PRODUCTNAME",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=80,
                        Width=250,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Артикул",
                        Path="VENDORCODE",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width=120,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Станок",
                        Path="PLACENAME",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width=60,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Потоков",
                        Path="NUMBEROFOUTS",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width=70,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Дата окончания производства",
                        Path="DATETIMEEND",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm",
                        Width=100,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Количество",
                        Path="QUANTITY",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width=85,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Счетчик",
                        Path="COUNTER",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width=70,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Приход",
                        Path="COMMING",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width=60,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Длина",
                        Path="L",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width=60,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Ширина",
                        Path="B",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width=60,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Z-картон",
                        Path="FANFOLD",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        Width=60,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Печать",
                        Path="PRINTING_FLAG",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        Width=60,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="В стопе",
                        Path="QTY_IN_REAM",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width=60,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Кол-во стоп",
                        Path="QTY_REAM",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width=60,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Паллет",
                        Path="NAME_PLT",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width=60,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="ИД отгрузки",
                        Path="ID_TS",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width=60,
                    },
                    new DataGridHelperColumn
                    {
                       Header = " ",
                       Path = "_",
                       ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                       MinWidth = 5,
                       MaxWidth = 1500,
                    },
                };
                Grid2.SetColumns(columns);
                

                Grid2.SetSorting("STACKER",ListSortDirection.Ascending);
                Grid2.SearchText=SearchText;
                Grid2.Init();

                //данные грида
                Grid2.OnLoadItems=LoadItems2;                
                Grid2.Run();
            }
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            //значения полей по умолчанию
            {
                FromDate.Text=DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy");
                ToDate.Text=DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy");

                var list = new Dictionary<string, string>();
                list.Add("0", "Все");
                list.Add("2", "ГА-1");
                list.Add("21", "ГА-2");
                list.Add("22", "ГА-3");
                list.Add("23", "ГА-1 КШ");
                Machines.Items = list;
                Machines.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");
            }
            RefreshButton.Style = (Style)RefreshButton.TryFindResource("Button");
        }

        /// <summary>
        /// обновление записей
        /// </summary>
        public async void LoadItems()
        {
            GridDisableControls();

            bool resume=true;
            
            if (resume)
            {
                var f = FromDate.Text.ToDateTime();
                var t = ToDate.Text.ToDateTime();
                if (DateTime.Compare(f, t) > 0)
                {
                    var msg = "Дата начала должна быть меньше даты окончания.";
                    var d = new DialogWindow($"{msg}", "Проверка данных");
                    d.ShowDialog();
                    resume = false;
                }
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("FROM_DATE", FromDate.Text);
                    p.CheckAdd("TO_DATE", ToDate.Text);
                    p.CheckAdd("ID_ST", Machines.SelectedItem.Key.ToInt().ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "ProductionTask");
                q.Request.SetParam("Action", "ListComplete");
                q.Request.Timeout = 30000;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if(q.Answer.Status == 0)                
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                    if(result!=null)
                    {
                        if(result.ContainsKey("ITEMS"))
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            Grid.UpdateItems(ds);
                        }
                    }
                }
            }

            RefreshButton.Style = (Style)RefreshButton.TryFindResource("Button");

            GridEnableControls();
        }

        /// <summary>
        /// получение записей
        /// </summary>
        public async void LoadItems2()
        {
            GridDisableControls();

            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();

                { 
                    p.Add("ID",SelectedItemId.ToString());
                    p.Add("ID_ST", Machines.SelectedItem.Key.ToInt().ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module","Production");
                q.Request.SetParam("Object","Position");
                q.Request.SetParam("Action","ListComplete");
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if(q.Answer.Status == 0)                
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                    if(result!=null)
                    {
                        if(result.ContainsKey("ITEMS"))
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            Grid2.UpdateItems(ds);
                        }                     
                    }
                }
               
            }

            RefreshButton.Style = (Style)RefreshButton.TryFindResource("Button");

            GridEnableControls();
        }

        /// <summary>
        /// экспорт записей грида в Excel
        /// </summary>
        private async void ExportToExcel()
        {
            if(Grid!=null)
            {
                if(Grid.Items.Count>0)
                {
                    var eg = new ExcelGrid();
                    var cols=Grid.Columns;
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
        /// обновление методов работы с выбранной записью
        /// </summary>
        /// <param name="selectedItem"></param>
        public void UpdateActions(Dictionary<string,string> selectedItem)
        {

            if(selectedItem.Count > 0)
            {
                int id = 0;
                if(selectedItem.ContainsKey("ID"))
                {
                    id=selectedItem["ID"].ToInt();
                }

                if(id!=0)
                {
                    SelectedItemId=id;
                }
            }

            
            if(SelectedItemId!=0)
            {
                Grid2.LoadItems();
            }
        }

        private void ShowButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        private void ExportButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            ExportToExcel();
        }

        private void HelpButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            ShowHelp();
        }

        /// <summary>
        /// Обработчик изменения даты отчета
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void DateTextChanged(object sender, TextChangedEventArgs args)
        {
            RefreshButton.Style = (Style)RefreshButton.TryFindResource("FButtonPrimary");
        }

        /// <summary>
        /// Обработчик выбора станка
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void Types_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RefreshButton.Style = (Style)RefreshButton.TryFindResource("FButtonPrimary");
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void GridDisableControls()
        {
            Toolbar.IsEnabled = false;
            Grid.ShowSplash();
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void GridEnableControls()
        {
            Toolbar.IsEnabled = true;
            Grid.HideSplash();
        }
    }


}
