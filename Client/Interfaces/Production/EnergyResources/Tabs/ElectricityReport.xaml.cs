using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.EnergyResources
{
    /// <summary>
    /// Отчет по электроэнергии
    /// </summary>
    /// <author>ledovskikh_dv</author>
    public partial class ElectricityReport : UserControl
    {
        public ElectricityReport()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            FormInit();
            SetDefault();
            GridInit();
            ProcessPermissions();
        }

        /// <summary>
        /// форма с полями для фильтрации данных
        /// </summary>
        public FormHelper Form { get; set; }

        public string RoleName = "[erp]energy_resources";

        /// <summary>
        // инициализация компонентов формы
        /// </summary>
        public void FormInit()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="FROM_DATE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=FromDate,
                    Default=DateTime.Now.AddMonths(-1).ToString("dd.MM.yyyy"),
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TO_DATE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ToDate,
                    Default=DateTime.Now.ToString("dd.MM.yyyy"),
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SEARCH",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Search,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            Form.SetFields(fields);
            Form.SetDefaults();
        }

        /// <summary>
        // инициализация компонентов таблицы
        /// </summary>
        public void GridInit()
        {
            //инициализация грида
            {
                //колонки грида

                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Дата",
                        Path="CREATED_DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ввод 1, 6 кВ",
                        Path="EL_AREA1_INPUT1_6KV",
                        Group="Площадка 1, приход",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=10,
                        TotalsType=TotalsTypeRef.Summ,
                       
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ввод 2, 6 кВ",
                        Path="EL_AREA1_INPUT2_6KV",
                        Group="Площадка 1, приход",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=10,
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Всего",
                        Path="AREA1",
                        Group="Площадка 1, приход",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=10,
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Гофропроизводство",
                        Path="EL_BHS",
                        Group="Площадка 1, расход",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=10,
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Переработка",
                        Path="EL_CONVERSATION",
                        Group="Площадка 1, расход",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=10,
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="БДМ 1",
                        Path="BDM1",
                        Group="Площадка 1, расход",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=10,
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Всего",
                        Path="CONSUMPTION1",
                        Group="Площадка 1, расход",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=10,
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ввод1, 110 кВ",
                        Path="EL_AREA2_INPUT1_110KV",
                        Group="Площадка 2, приход",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=10,
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ввод2, 110 кВ",
                        Path="EL_AREA2_INPUT2_110KV",
                        Group="Площадка 2, приход",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=10,
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Всего",
                        Path="AREA2",
                        Group="Площадка 2, приход",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=10,
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Котельная",
                        Path="AREA2_KOTELN",
                        Group="Площадка 2, расход",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=10,
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="БДМ 2",
                        Path="BDM2",
                        Group="Площадка 2, расход",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=10,
                        TotalsType=TotalsTypeRef.Summ,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Всего",
                        Path="CONSUMPTION2",
                        Group="Площадка 2, расход",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=10,
                        TotalsType=TotalsTypeRef.Summ,
                    },
                };
                Grid.SetColumns(columns);
                Grid.SetPrimaryKey("_ROWNUMBER");
                Grid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
                Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
                Grid.SearchText = Search;
                Grid.Toolbar = GridToolbar;
                Grid.OnLoadItems = GridLoadItems;
                Grid.Init();
            }

            //инициализация формы
            {
                Form = new FormHelper();

                //колонки формы
                var fields = new List<FormHelperField>();

                Form.SetFields(fields);
            }
        }

        /// <summary>
        /// получение данных по потреблению электричества
        /// </summary>
        public async void GridLoadItems()
        {
            GridDisableControls();

            bool resume = true;

            if (resume)
            {
                var f = FromDate.Text.ToDateTime();
                var t = ToDate.Text.ToDateTime();

                if (resume)
                {
                    if (DateTime.Compare(f, t) > 0)
                    {
                        var msg = "Дата начала должна быть меньше даты окончания.";
                        var d = new DialogWindow($"{msg}", "Проверка данных");
                        d.ShowDialog();
                        resume = false;
                    }
                }

                var p = new Dictionary<string, string>();
                {
                    p.Add("FROM_DATE", FromDate.Text);
                    p.Add("TO_DATE", ToDate.Text);
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "EnergyResource");
                q.Request.SetParam("Action", "ListElectricity");
                switch (Types.SelectedItem.Key)
                {
                    case "-1":
                        p.Add("PERIOD", "hour");
                        break;
                    case "0":
                        p.Add("PERIOD", "day");
                        break;
                    case "1":
                        p.Add("PERIOD", "month");
                        break;
                }                
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
                            var ds = ListDataSet.Create(result, "ITEMS");
                            Grid.UpdateItems(ds);
                        }                        
                    }
                }
            }
            ShowButton.Style = (Style)ShowButton.TryFindResource("Button");
            GridEnableControls();
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void GridDisableControls()
        {
            GridToolbar.IsEnabled = false;
            Grid.ShowSplash();
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void GridEnableControls()
        {
            GridToolbar.IsEnabled = true;
            Grid.HideSplash();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку обновления формы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefault()
        {
            {
                var list = new Dictionary<string, string>();
                list.Add("-1", "По часам");
                list.Add("0", "По суткам");
                list.Add("1", "По месяцам");
                Types.Items = list;
                Types.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");
            }
            Form.SetDefaults();
        }

        /// <summary>
        /// Обработчик изменения параметра фильтрафции
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void Types_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
            Grid.UpdateItems();
        }

        /// <summary>
        /// Обработчик изменения начальной даты отчета
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void ToDateTextChanged(object sender, TextChangedEventArgs args)
        {
            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
            Grid.UpdateItems();
        }

        /// <summary>
        /// Обработчик изменения конечной даты отчета
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void FromDateTextChanged(object sender, TextChangedEventArgs args)
        {
            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
            Grid.UpdateItems();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку экспорта в Excel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExcelButton_Click(object sender, RoutedEventArgs e)
        {
            Grid.ItemsExportExcel();
        }

        /// <summary>
        /// Экспорт данных из таблицы в Excel
        /// </summary>
        public async void ExportToExcel(List<DataGridHelperColumn> columns, List<Dictionary<string, string>> items)
        {
            var eg = new ExcelGrid();
            eg.SetColumnsFromGrid(columns);
            eg.Items = items;
            await Task.Run(() =>
            {
                eg.Make();
            });

        }

        /// <summary>
        /// Обработчик нажатия на кнопку справки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/production/energy_resources");
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

            if (Grid != null && Grid.Menu != null && Grid.Menu.Count > 0)
            {
                foreach (var manuItem in Grid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// Закрытие окна
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "EnergyResources",
                ReceiverName = "",
                SenderName = "ElectricityReport",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            Grid.Destruct();
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
        }

        /// <summary>
        /// обработка ввода с клавиатуры (роли)
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

               
            }
        }
    }
}
