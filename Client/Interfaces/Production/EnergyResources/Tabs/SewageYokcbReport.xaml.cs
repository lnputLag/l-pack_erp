using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
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
using System.Windows.Media;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.EnergyResources
{
    /// <summary>
    /// Отчет по воде от УОКСВ
    /// </summary>
    /// <author>Грешных Н.И.</author> 
    /// <change>2025-06-04</change>
    public partial class SewageYokcbReport : UserControl
    {
        public SewageYokcbReport()
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
                   // Default=DateTime.Now.AddMonths(-1).ToString("dd.MM.yyyy"),
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TO_DATE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ToDate,
                  //  Default=DateTime.Now.ToString("dd.MM.yyyy"),
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
                    //new DataGridHelperColumn
                    //{
                    //    Header="#",
                    //    Path="_ROWNUMBER",
                    //    ColumnType=ColumnTypeRef.Integer,
                    //    MinWidth=50,
                    //    MaxWidth=50,
                    //},

                    new DataGridHelperColumn
                    {
                        Header="Бригада",
                        Path="BRIGADE",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=30,
                        MaxWidth=120,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Расход с переработки",
                        Path="SEWAGE_YOKCB_1",
                        ColumnType=ColumnTypeRef.Double,
                        MinWidth=130,
                        MaxWidth=150,
                        Totals =(List<Dictionary<string,string>> rows) =>
                        {
                            double result=0;
                            if(rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    result += row.CheckGet("SEWAGE_YOKCB_1").ToDouble();
                                }
                            }
                            return string.Format("{0:0.00}", result);
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Расход с BHS",
                        Path="SEWAGE_YOKCB_2",
                        ColumnType=ColumnTypeRef.Double,
                        MinWidth=180,
                        MaxWidth=180,
                        Totals =(List<Dictionary<string,string>> rows) =>
                        {
                            double result=0;
                            if(rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    result += row.CheckGet("SEWAGE_YOKCB_2").ToDouble();
                                }
                            }
                            return string.Format("{0:0.00}", result);
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Расход в канализацию",
                        Path="SEWAGE_YOKCB_3",
                        ColumnType=ColumnTypeRef.Double,
                        MinWidth=130,
                        MaxWidth=150,
                        Totals =(List<Dictionary<string,string>> rows) =>
                        {
                            double result=0;
                            if(rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    result += row.CheckGet("SEWAGE_YOKCB_3").ToDouble();
                                }
                            }
                            return string.Format("{0:0.00}", result);
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Расход в канализацию с прессов",
                        Path="SEWAGE_YOKCB_4",
                        ColumnType=ColumnTypeRef.Double,
                        MinWidth=130,
                        MaxWidth=200,
                        Totals =(List<Dictionary<string,string>> rows) =>
                        {
                            double result=0;
                            if(rows != null)
                            {
                                foreach(Dictionary<string,string> row in rows)
                                {
                                    result += row.CheckGet("SEWAGE_YOKCB_4").ToDouble();
                                }
                            }
                            return string.Format("{0:0.00}", result);
                        },
                    },
                    new DataGridHelperColumn
                    {
                       Header = " ",
                       Path = "_",
                       ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                       MinWidth = 5,
                       MaxWidth = 2500,
                    },
                };
                Grid.SetColumns(columns);

                Grid.UseRowHeader = true;
                Grid.SearchText = Search;
                Grid.Init();

              //  Grid.PrimaryKey = "_ROWNUMBER";
              //  Grid.SetSorting("FROMDT");

                //данные грида
                Grid.OnLoadItems = GridLoadItems;
                Grid.Run();
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
        /// получение данных по канализации УОКСВ
        /// </summary>
        public async void GridLoadItems()
        {
            GridDisableControls();

            bool resume = true;

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

                var p = new Dictionary<string, string>();
                {
                    p.Add("FROM_DATE", FromDate.Text);
                    p.Add("TO_DATE", ToDate.Text);
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "EnergyResource");
                q.Request.SetParam("Action", "ListSewageYokcb");

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
            var start = DateTime.Now;

            int hour = start.Hour;

            if (hour > 20)
            {
                start = new DateTime(start.Year, start.Month, start.Day, 20, 0, 0);
            }
            else
            {
                start = new DateTime(start.Year, start.Month, start.Day, 8, 0, 0);
            }

            FromDate.EditValue = start;
            ToDate.EditValue = start.AddHours(12);

            //   Form.SetDefaults();
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
            ExportToExcel(Grid.Columns, Grid.GridItems);
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
        /// Закрытие окна
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "EnergyResources",
                ReceiverName = "",
                SenderName = "SewageYokcbReport",
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
        /// нажали кнопку "Предыдущая смена"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LeftShiftButton_Click(object sender, RoutedEventArgs e)
        {
            if (FromDate.EditValue is DateTime start)
            {
                start = start.AddHours(-12);
                int hour = start.Hour;

                if (hour > 12)
                {
                    start = new DateTime(start.Year, start.Month, start.Day, 20, 0, 0);
                }
                else
                {
                    start = new DateTime(start.Year, start.Month, start.Day, 8, 0, 0);
                }

                FromDate.EditValue = start;
                ToDate.EditValue = start.AddHours(12);

                GridLoadItems();
            }

        }

        /// <summary>
        /// нажали кнопку "Следующая смена"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RightShiftButton_Click(object sender, RoutedEventArgs e)
        {
            if (FromDate.EditValue is DateTime start)
            {
                start = start.AddHours(12);
                int hour = start.Hour;

                if (hour > 12)
                {
                    start = new DateTime(start.Year, start.Month, start.Day, 20, 0, 0);
                }
                else
                {
                    start = new DateTime(start.Year, start.Month, start.Day, 8, 0, 0);
                }

                FromDate.EditValue = start;
                ToDate.EditValue = start.AddHours(12);

                GridLoadItems();
            }

        }
    }
}

