using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Xceed.Wpf.Toolkit.Calculator;

namespace Client.Interfaces.Production.Strapper
{
    /// <summary>
    /// Монитор упаковщиков
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class StrapperMonitor : ControlBase
    {
        public StrapperMonitor()
        {
            ControlTitle = "Монитор упаковщиков";
            DocumentationUrl = "/doc/l-pack-erp/";
            RoleName = "[erp]strapper_monitor";
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
                InitForm();
                SetDefaults();

                Refresh();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
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
            }

            Commander.Init(this);
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Датасет с данными совершённым операциям на сигноде
        /// </summary>
        private ListDataSet OperationDataSet { get; set; }

        /// <summary>
        /// Датасет с данными по интервалам между операциями на сигноде
        /// </summary>
        private ListDataSet IntervalDataSet { get; set; }

        /// <summary>
        /// Датасет с данными по обвязчикам
        /// </summary>
        private ListDataSet StrapperDataSet { get; set; }

        /// <summary>
        /// Список строк-обвязчиков
        /// </summary>
        private List<StrapperMonitorRow> StrapperMonitorRowList { get; set; }

        /// <summary>
        /// Ид производственной площадки
        /// </summary>
        private int FactoryId = 1;

        /// <summary>
        /// Количество пикселей для отображения одной минуты
        /// </summary>
        private int PixelPerMinute = 24;

        /// <summary>
        /// Инициализация формы
        /// </summary>
        private void InitForm()
        {
            //инициализация формы
            {
                Form = new FormHelper();

                //колонки формы
                var fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="FROM_DATE_TIME",
                        FieldType=FormHelperField.FieldTypeRef.DateTime,
                        Control=FromDateTime,
                        ControlType="dateedit",
                        Format="dd.MM.yyyy HH:mm:ss",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        }
                    },
                    new FormHelperField()
                    {
                        Path="TO_DATE_TIME",
                        FieldType=FormHelperField.FieldTypeRef.DateTime,
                        Control=ToDateTime,
                        ControlType="dateedit",
                        Format="dd.MM.yyyy HH:mm:ss",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        }
                    },
                };

                Form.SetFields(fields);
            }
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            OperationDataSet = new ListDataSet();
            IntervalDataSet = new ListDataSet();
            StrapperDataSet = new ListDataSet();

            StrapperMonitorRowList = new List<StrapperMonitorRow>();

            var date = DateTime.Now;
            if (date.Hour >= 20 || date.Hour < 8)
            {
                if (date.Hour >= 20)
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.AddDays(1).ToString("dd.MM.yyyy")} 08:00:00");
                }
                else
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 08:00:00");
                }
            }
            else
            {
                Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 08:00:00");
                Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 20:00:00");
            }
        }

        /// <summary>
        /// Получене данных по сканированиям
        /// </summary>
        private async Task LoadItems()
        {
            bool resume = true;
            var f = Form.GetValueByPath("FROM_DATE_TIME").ToDateTime();
            var t = Form.GetValueByPath("TO_DATE_TIME").ToDateTime();
            if (DateTime.Compare(f, t) > 0)
            {
                const string msg = "Дата начала должна быть меньше даты окончания.";
                var d = new DialogWindow($"{msg}", "Проверка данных");
                d.ShowDialog();

                resume = false;
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();
                p.Add("FACTORY_ID", $"{this.FactoryId}");
                p.Add("FROM_DTTM", Form.GetValueByPath("FROM_DATE_TIME"));
                p.Add("TO_DTTM", Form.GetValueByPath("TO_DATE_TIME"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production/Strapper");
                q.Request.SetParam("Object", "Operation");
                q.Request.SetParam("Action", "List");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestTimeoutDefault;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                OperationDataSet = new ListDataSet();
                IntervalDataSet = new ListDataSet();
                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null && result.Count > 0)
                    {
                        OperationDataSet = ListDataSet.Create(result, "OPERATION");
                        IntervalDataSet = ListDataSet.Create(result, "INTERVAL");
                        StrapperDataSet = ListDataSet.Create(result, "STRAPPER");
                    }
                }
                else
                {
                    q.SilentErrorProcess = true;
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// Инициализация грида
        /// </summary>
        private void InitGrid()
        {
            if (StrapperDataSet != null && StrapperDataSet.Items != null && StrapperDataSet.Items.Count > 0)
            {
                {
                    // Очищаем данные
                    {
                        MainGrid.ColumnDefinitions.Clear();
                        MainGrid.RowDefinitions.Clear();
                        MainGrid.Children.Clear();

                        LabelGrid.ColumnDefinitions.Clear();
                        LabelGrid.RowDefinitions.Clear();
                        LabelGrid.Children.Clear();

                        StrapperMonitorRowList.Clear();
                    }

                    MainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                    LabelGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

                    var fromDateTime = Form.GetValueByPath("FROM_DATE_TIME").ToDateTime();
                    var toDateTime = Form.GetValueByPath("TO_DATE_TIME").ToDateTime();

                    int rowIndex = 0;

                    // Заголовки
                    {
                        MainGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

                        StrapperMonitorTimeLine strapperMonitorTimeLine = new StrapperMonitorTimeLine(PixelPerMinute, fromDateTime, toDateTime);
                        strapperMonitorTimeLine.CreateVisual();
                        strapperMonitorTimeLine.CreateVisualCoordinate();

                        Grid.SetColumn(strapperMonitorTimeLine.VisualBorder, 0);
                        Grid.SetRow(strapperMonitorTimeLine.VisualBorder, rowIndex);

                        MainGrid.Children.Add(strapperMonitorTimeLine.VisualBorder);



                        LabelGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                        StrapperMonitorLabel strapperMonitorLabel = new StrapperMonitorLabel(strapperMonitorTimeLine.VisualGridCoordinate.Height);
                        strapperMonitorLabel.CreateVisual();

                        Grid.SetColumn(strapperMonitorLabel.VisualBorder, 0);
                        Grid.SetRow(strapperMonitorLabel.VisualBorder, rowIndex);

                        LabelGrid.Children.Add(strapperMonitorLabel.VisualBorder);
                    }

                    // Строки - упаковщики
                    {
                        foreach (var item in StrapperDataSet.Items)
                        {
                            rowIndex++;

                            MainGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

                            StrapperMonitorRow strapperMonitorRow = new StrapperMonitorRow(PixelPerMinute, fromDateTime, toDateTime);
                            strapperMonitorRow.StrapperNumber = item.CheckGet("STRAPPER_NUMBER").ToInt();
                            strapperMonitorRow.CreateVisual();
                            strapperMonitorRow.CreateVisualCoordinate();

                            Grid.SetColumn(strapperMonitorRow.VisualBorder, 0);
                            Grid.SetRow(strapperMonitorRow.VisualBorder, rowIndex);

                            MainGrid.Children.Add(strapperMonitorRow.VisualBorder);

                            StrapperMonitorRowList.Add(strapperMonitorRow);



                            LabelGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

                            StrapperMonitorLabel strapperMonitorLabel = new StrapperMonitorLabel(strapperMonitorRow.VisualGrid.Height);
                            strapperMonitorLabel.StrapperNumber = item.CheckGet("STRAPPER_NUMBER").ToInt();
                            strapperMonitorLabel.StrapperName = $"Pk{item.CheckGet("STRAPPER_NUMBER").ToInt()}";
                            strapperMonitorLabel.CreateVisual();

                            Grid.SetColumn(strapperMonitorLabel.VisualBorder, 0);
                            Grid.SetRow(strapperMonitorLabel.VisualBorder, rowIndex);

                            LabelGrid.Children.Add(strapperMonitorLabel.VisualBorder);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Заполнение данных по сканированиям
        /// </summary>
        private void SetItems()
        {
            if (OperationDataSet != null && OperationDataSet.Items != null && OperationDataSet.Items.Count > 0)
            {
                foreach (var strapperMonitorRow in StrapperMonitorRowList)
                {
                    strapperMonitorRow.OperationList = OperationDataSet.Items.Where(x => x.CheckGet("STRAPPER_NUMBER").ToInt() == strapperMonitorRow.StrapperNumber).ToList();
                    strapperMonitorRow.CreateVisualOperation();
                }
            }

            if (IntervalDataSet != null && IntervalDataSet.Items != null && IntervalDataSet.Items.Count > 0)
            {
                foreach (var strapperMonitorRow in StrapperMonitorRowList)
                {
                    strapperMonitorRow.IntervalList = IntervalDataSet.Items.Where(x => x.CheckGet("STRAPPER_NUMBER").ToInt() == strapperMonitorRow.StrapperNumber).ToList();
                    strapperMonitorRow.CreateVisualInterval();
                }
            }
        }

        /// <summary>
        /// Обновление данны.
        /// 1 -- Получем данные
        /// 2 -- Переинициализируем гриды
        /// 3 -- Заполняем гриды данными
        /// </summary>
        public async void Refresh()
        {
            SplashControl.Visible = true;
            GridToolbar.IsEnabled = false;

            await LoadItems();
            InitGrid();
            SetItems();

            SplashControl.Visible = false;
            GridToolbar.IsEnabled = true;
        }

        private void OnCurrentShift(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now;
            if (date.Hour >= 20 || date.Hour < 8)
            {
                if (date.Hour >= 20)
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.AddDays(1).ToString("dd.MM.yyyy")} 08:00:00");
                }
                else
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 08:00:00");
                }
            }
            else
            {
                Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 08:00:00");
                Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 20:00:00");
            }

            Refresh();
        }

        private void OnPrevShift(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now.AddHours(-12);
            if (date.Hour >= 20 || date.Hour < 8)
            {
                if (date.Hour >= 20)
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{date.ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{date.AddDays(1).ToString("dd.MM.yyyy")} 08:00:00");
                }
                else
                {
                    Form.SetValueByPath("FROM_DATE_TIME", $"{date.AddDays(-1).ToString("dd.MM.yyyy")} 20:00:00");
                    Form.SetValueByPath("TO_DATE_TIME", $"{date.ToString("dd.MM.yyyy")} 08:00:00");
                }
            }
            else
            {
                Form.SetValueByPath("FROM_DATE_TIME", $"{date.ToString("dd.MM.yyyy")} 08:00:00");
                Form.SetValueByPath("TO_DATE_TIME", $"{date.ToString("dd.MM.yyyy")} 20:00:00");
            }

            Refresh();
        }

        private void OnCurrentDay(object sender, RoutedEventArgs e)
        {
            Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.AddDays(1).ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void OnPrevDay(object sender, RoutedEventArgs e)
        {
            Form.SetValueByPath("FROM_DATE_TIME", $"{DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{DateTime.Now.ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void OnCurrentWeek(object sender, RoutedEventArgs e)
        {
            DayOfWeek day = DateTime.Now.DayOfWeek;
            int days = day - DayOfWeek.Monday;
            DateTime date = DateTime.Now.AddDays(-days);

            Form.SetValueByPath("FROM_DATE_TIME", $"{date.Date.ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{date.Date.AddDays(7).ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void OnPrevWeek(object sender, RoutedEventArgs e)
        {
            DayOfWeek day = DateTime.Now.DayOfWeek;
            int days = day - DayOfWeek.Monday;
            DateTime date = DateTime.Now.AddDays(-days).AddDays(-7);

            Form.SetValueByPath("FROM_DATE_TIME", $"{date.Date.ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{date.Date.AddDays(7).ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void OnCurrentMonth(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now;

            Form.SetValueByPath("FROM_DATE_TIME", $"{new DateTime(date.Year, date.Month, 1).ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{new DateTime(date.Year, date.Month, 1).AddMonths(1).ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }

        private void OnPrevMonth(object sender, RoutedEventArgs e)
        {
            var date = DateTime.Now.AddMonths(-1);

            Form.SetValueByPath("FROM_DATE_TIME", $"{new DateTime(date.Year, date.Month, 1).ToString("dd.MM.yyyy")} 00:00:00");
            Form.SetValueByPath("TO_DATE_TIME", $"{new DateTime(date.Year, date.Month, 1).AddMonths(1).ToString("dd.MM.yyyy")} 00:00:00");

            Refresh();
        }
    }
}
