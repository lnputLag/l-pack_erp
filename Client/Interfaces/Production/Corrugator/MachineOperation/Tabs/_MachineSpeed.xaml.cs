using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
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
using System.Windows.Media;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production
{

    /// <summary>
    /// тестовый интерфейс для отладки блока "график"
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2022-10-31</released>
    /// <changed>2022-10-31</changed>
    public partial class MachineSpeed : UserControl
    {
        public MachineSpeed()
        {
            InitializeComponent();

            DebugMode = Central.DebugMode;

            InitCallCounter = 0;

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            Loaded += OnLoad;
            //SizeChanged += OnResize;


            XValues = new Dictionary<double, string>();

            PrimaryButtonAccent = false;

            CheckPrimaryButtonAccent();

            InitForm();
            SetDefaults();

            ProcessPermissions();
        }

        public string RoleName = "[erp]corrugator_work_log";

        /// <summary>
        /// данные из выбранной в гриде строки
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }
        Dictionary<string, string> Colors { get; set; }
        Dictionary<double, string> XValues { get; set; }
        private bool PrimaryButtonAccent { get; set; }

        /// <summary>
        /// ID выбранной группы ролей
        /// </summary>
        int DepartmentID { get; set; } = -1;

        /// <summary>
        /// форма с полями для фильтрации данных
        /// </summary>
        public FormHelper Form { get; set; }
        public bool DebugMode { get; set; }
        public ListDataSet Ds { get; set; }

        /// <summary>
        /// цена деления
        /// </summary>
        public int ValueOfDivision { get; set; }

        /// <summary>
        /// множества для фильтрации
        /// </summary>
        public List<int> Machines { get; set; } = new List<int>() { 2, 21, 22, 23 };
        public List<string> Profils { get; set; } = new List<string>() { "В", "С", "Е", "ВС", "ЕВ", "ЕС", "ВВ", "ВЕ", "СЕ", "СВ", "ЕЕ" };
        public List<bool> Tandem { get; set; } = new List<bool>() { true, false };
        public List<bool> ZCardboard { get; set; } = new List<bool>() { true, false };
        public List<bool> Print { get; set; } = new List<bool>() { true, false };
        public int InitCallCounter { get; set; }

        /// <summary>
        /// цены деления по OX
        /// </summary>
        public int[] VALUES_OF_DIVISION = new int[] { 1, 5, 10, 25, 50, 75, 100, 125, 150, 200, 250, 300, 350, 400, 450, 500, 600, 700, 800, 900 };

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

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            Init();
        }

        //private void OnResize(object sender, SizeChangedEventArgs e)
        //{
        //    Init();
        //}

        public void Init()
        {
            InitCallCounter++;
            if (InitCallCounter > 1)
            {
                InitForm();
                //SetDefaults();
                LoadItems();
            }
        }

        public void InitGraph(Dictionary<double, string> xValues)
        {
            RefreshButton.Style = (Style)RefreshButton.TryFindResource("Button");

            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Длина",
                    Path="_SECONDS",
                    ColumnType=ColumnTypeRef.Integer,
                    Params=new Dictionary<string, string>(),
                },
                new DataGridHelperColumn
                {
                    Header="Скорость",
                    Path="SPEED_FACT",
                    ColumnType=ColumnTypeRef.Integer,
                    Params=new Dictionary<string, string>()
                    {
                        //диаметр точки, пикс
                        {"PointDiameter","5"},
                        //толщина линии, пикс
                        //не отрисовываем линии, только точки
                        {"LineThickness","0"},
                    },
                    Stylers=new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                    {
                        {
                            DataGridHelperColumn.StylerTypeRef.ForegroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var points = new List<(Brush, double)>();
                                double y = row.CheckGet("VALUE").ToDouble();

                                //green
                                var color = "#88289744";
                                points.Add((color.ToBrush(), y));

                                if (points.Count > 0)
                                {
                                    result = points;
                                }

                                return result;
                            }
                        },
                    }
                },
            };

            XValues = xValues;

            Graph.YAxis.Min = 0;
            Graph.YAxis.Max = 400;
            // ширина поля для надписей слева OY
            Graph.XOffset = 25;
            Graph.XAxis.Step = 200;
            Graph.YAxis.Step = 20;
            Graph.XAxisLabelStep = 1;

            //колонки грида
            Graph.DebugMode = DebugMode;
            Graph.SetColumns(columns);
            Graph.PrimaryKey = "_SECONDS";
            Graph.PrimaryLabel = "LENGTH_TASK";
            Graph.Init();

            Graph.AutoRender = false;
            Graph.AutoUpdateInterval = 0;

            ////данные грида
            //Graph1.OnLoadItems = Graph1LoadItems;

            Graph.OnCalculateXValues = (GraphBox ctl) =>
            {
                ctl.XValues = XValues;
            };


            //запуск грида в работу
            Graph.Run();

            //фокус ввода           
            Graph.Focus();
        }

        /// <summary>
        /// генерация прямой OX
        /// </summary>
        /// <param name="lowerLimit"></param>
        /// <param name="upperLimit"></param>
        /// <returns></returns>
        public int GenerateXValues(int lowerLimit, int upperLimit)
        {
            var result = new Dictionary<double, string>();

            var center = (upperLimit - lowerLimit) / 2 + lowerLimit;

            double residual = (upperLimit - lowerLimit) / 24;

            for (var i = VALUES_OF_DIVISION.Length - 1; i > 0; i--)
            {
                if (residual > VALUES_OF_DIVISION[i - 1])
                {
                    ValueOfDivision = VALUES_OF_DIVISION[i];
                    break;
                }
            }


            center = center - (center % ValueOfDivision);

            var startPoint = center - ValueOfDivision * 13;
            if (startPoint < 0)
            {
                startPoint = 0;
            }

            ValueOfDivision = ValueOfDivision;

            if (Graph.TypeOX == 1)
            {
                for (int j = 0, point = 0; j < 27; j++, point += ValueOfDivision)
                {
                    var y = (startPoint + point).ToString();
                    result.Add(j * 484, y);
                }
            }
            else if (Graph.TypeOX == 2)
            {
                for (int j = 0, point = 1; j < 15; j++, point *= 2)
                {
                    var y = startPoint + point;
                    result.Add(y, y.ToString());
                }
            }

            InitGraph(result);

            return startPoint;
        }

        public void InitForm()
        {
            //инициализация формы, элементы тулбара
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
                };

                Form.SetFields(fields);

                //после установки значений
                Form.AfterSet = (Dictionary<string, string> v) =>
                {
                    //фокус на кнопку обновления
                    RefreshButton.Focus();
                };
            }

        }

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Accounts",
                ReceiverName = "",
                SenderName = "UserList",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            Graph.Destruct();
        }


        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            {
                var list = new Dictionary<string, string>();
                list.Add("LENGTH_TASK", "Длина ПЗ");
                list.Add("LENGTH_BLANK", "Длина заготовки");
                list.Add("WIDTH_BLANK", "Ширина заготовки");
                Parameter.Items = list;
                Parameter.SelectedItem = list.FirstOrDefault((x) => x.Key == "LENGTH_TASK");
            }
            Form.SetDefaults();
        }

        //public void SetDdefault()
        //{
        //    var list = new Dictionary<string, string>();
        //    list.Add("LENGTH_TASK", "Длина ПЗ");
        //    list.Add("LENGTH_BLANK", "Длина заготовки");
        //    list.Add("WIDTH_BLANK", "Ширина заготовки");
        //    Parameter.Items = list;
        //    Parameter.SelectedItem = list.FirstOrDefault((x) => x.Key == "LENGTH_TASK");
        //}

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            //Group ProductionTask
            if (m.ReceiverGroup.IndexOf("Production") > -1)
            {
                switch (m.Action)
                {
                    case "Refresh":

                        break;
                }
            }

            if (m.ReceiverGroup.IndexOf("All") > -1)
            {
                switch (m.Action)
                {
                    case "Resized":
                        Init();
                        break;
                }
            }
        }

        /// <summary>
        /// получение записей
        /// </summary>
        public async void GraphLoadItems()
        {
            DisableControls();
            //LoadItems();
            EnableControls();
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
            Graph.ShowSplash();
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
            Graph.HideSplash();
        }

        public async void LoadItems()
        {
            Dictionary<string, ListDataSet> result = null;

            var p = new Dictionary<string, string>();
            {
                p.Add("FROM_DATE", FromDate.Text);
                p.Add("TO_DATE", ToDate.Text);
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "Machine");
            q.Request.SetParam("Action", "ListSpeedForGraph");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;


            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                if (result != null)
                {
                    Ds = ListDataSet.Create(result, "ITEMS");
                    RefreshGraph();
                }
            }

            RefreshButton.Style = (Style)RefreshButton.TryFindResource("Button");
        }
        /// <summary>
        /// обновление графика
        /// </summary>
        public void RefreshGraph()
        {
            var msg = "";
            var resume = true;
            if (FromLength.Text.ToInt() > ToLength.Text.ToInt())
            {
                msg += "\r\n Начальная длина ПЗ должна быть меньше конечной";
                resume = false;
            }
            if (FromBlankLength.Text.ToInt() > ToBlankLength.Text.ToInt())
            {
                msg += "\r\n Начальная длина заготовки должна быть меньше конечной";
                resume = false;
            }
            if (FromBlankWidth.Text.ToInt() > ToBlankWidth.Text.ToInt())
            {
                msg += "\r\n Начальная ширина заготовки должна быть меньше конечной";
                resume = false;
            }
            if (resume == false)
            {
                var d = new DialogWindow($"{msg}", "Проверка данных");
                d.ShowDialog();
            }
            else if (Ds != null)
            {
                Graph.ClearData();
                {
                    var FilteredItems = new ListDataSet();
                    FilteredItems.Cols = Ds.Cols;
                    FilteredItems.AdditionalCols = Ds.AdditionalCols;
                    FilteredItems.Rows = Ds.Rows;
                    if (Ds.Items.Count > 0)
                    {
                        var lowerLimit = 0;
                        var upperLimit = 0;

                        if (Parameter.SelectedItem.Key == "LENGTH_TASK")
                        {
                            lowerLimit = FromLength.Text.ToInt();
                            upperLimit = ToLength.Text.ToInt();
                        }

                        else if (Parameter.SelectedItem.Key == "LENGTH_BLANK")
                        {
                            lowerLimit = FromBlankLength.Text.ToInt();
                            upperLimit = ToBlankLength.Text.ToInt();
                        }

                        else
                        {
                            lowerLimit = FromBlankWidth.Text.ToInt();
                            upperLimit = ToBlankWidth.Text.ToInt();
                        }

                        lowerLimit = GenerateXValues(lowerLimit, upperLimit);

                        //формируем список значений, соответствующих фильтрам
                        foreach (Dictionary<string, string> row in Ds.Items)
                        {
                            if (row.CheckGet("LENGTH_TASK").ToInt() >= FromLength.Text.ToInt()
                                && row.CheckGet("LENGTH_TASK").ToInt() <= ToLength.Text.ToInt()
                                && row.CheckGet("LENGTH_BLANK").ToInt() >= FromBlankLength.Text.ToInt()
                                && row.CheckGet("LENGTH_BLANK").ToInt() <= ToBlankLength.Text.ToInt()
                                && row.CheckGet("WIDTH_BLANK").ToInt() >= FromBlankWidth.Text.ToInt()
                                && row.CheckGet("WIDTH_BLANK").ToInt() <= ToBlankWidth.Text.ToInt()
                                && Machines.Contains(row.CheckGet("ID_ST").ToInt())
                                && Profils.Contains(row.CheckGet("FLUTE"))
                                && Tandem.Contains(Convert.ToBoolean(row.CheckGet("TANDEM_FLAG").ToInt()))
                                && ZCardboard.Contains(Convert.ToBoolean(row.CheckGet("FANFOLD_FLAG").ToInt()))
                                && Print.Contains(Convert.ToBoolean(row.CheckGet("PRINTING_FLAG").ToInt())))
                            {
                                var keyRow = new Dictionary<string, string>();

                                keyRow.Add("SPEED_FACT", row.CheckGet("SPEED_FACT"));

                                var l = row.CheckGet(Parameter.SelectedItem.Key).Length;
                                var rowKeyItem = row.CheckGet(Parameter.SelectedItem.Key).Substring(0, l);

                                keyRow.CheckAdd(Parameter.SelectedItem.Key, $"{(rowKeyItem.ToInt() - lowerLimit) * 484 / ValueOfDivision}");

                                FilteredItems.Items.Add(keyRow);
                            }
                        }

                        foreach (Dictionary<string, string> row in FilteredItems.Items)
                        {
                            row.CheckAdd("_SECONDS", row[Parameter.SelectedItem.Key]);
                        }
                    }
                    Graph.UpdateItems(FilteredItems);
                }

                Graph.Render();

                ShowButton.Style = (Style)ShowButton.TryFindResource("Button");
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
                    Graph.LoadItems();
                    e.Handled = true;
                    break;

                case Key.F1:
                    ShowHelp();
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
            Central.ShowHelp("/doc/l-pack-erp/production/machine_operation/machine_speed");
        }

        /// <summary>
        /// акцентирует внимание на кнопке по умолчанию
        /// </summary>
        public void CheckPrimaryButtonAccent(bool accent = false)
        {
            PrimaryButtonAccent = accent;

            if (PrimaryButtonAccent)
            {
                RefreshButton.Style = (Style)RefreshButton.TryFindResource("FButtonPrimary");
            }
            else
            {
                RefreshButton.Style = (Style)RefreshButton.TryFindResource("Button");
            }
        }

        /// <summary>
        /// обработчик нажатия на кнопку документации
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        /// <summary>
        /// обработчик нажатия на кнопку обновить
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            CheckPrimaryButtonAccent();
            LoadItems();
        }

        private void ExportDs1Button_Click(object sender, RoutedEventArgs e)
        {
            ListDataSet.ExportDS(Ds);
        }

        /// <summary>
        /// Обработчик изменения начальной даты отчета
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void DateTextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshButton.Style = (Style)RefreshButton.TryFindResource("FButtonPrimary");
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefault()
        {
            Form.SetDefaults();
        }

        /// <summary>
        /// обработчик изменения состояния CheckBox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GACheckBox_Click(object sender, RoutedEventArgs e)
        {
            var checkBox = (CheckBox)sender;
            if ((bool)checkBox.IsChecked)
            {
                Machines.Add(checkBox.Tag.ToInt());
            }
            else
            {
                Machines.Remove(checkBox.Tag.ToInt());
            }

            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
        }

        private void TandemYesCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)TandemYesCheckBox.IsChecked)
            {
                Tandem.Add(true);
            }
            else
            {
                Tandem.Remove(true);
            }

            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
        }

        private void TandemNoCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)TandemNoCheckBox.IsChecked)
            {
                Tandem.Add(false);
            }
            else
            {
                Tandem.Remove(false);
            }

            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
        }

        private void ZYesCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)ZYesCheckBox.IsChecked)
            {
                ZCardboard.Add(true);
            }
            else
            {
                ZCardboard.Remove(true);
            }

            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
        }

        private void ZNoCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)ZNoCheckBox.IsChecked)
            {
                ZCardboard.Add(false);
            }
            else
            {
                ZCardboard.Remove(false);
            }

            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
        }

        private void PrintYesCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)PrintYesCheckBox.IsChecked)
            {
                Print.Add(true);
            }
            else
            {
                Print.Remove(true);
            }

            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
        }

        private void PrintNoCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)PrintNoCheckBox.IsChecked)
            {
                Print.Add(false);
            }
            else
            {
                Print.Remove(false);
            }

            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
        }

        private void ProfilCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var checkBox = (CheckBox)sender;
            if ((bool)checkBox.IsChecked)
            {
                Profils.Add(checkBox.Content.ToString());
            }
            else
            {
                Profils.Remove(checkBox.Content.ToString());
            }

            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
        }

        private void LogOXCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (LogOXCheckBox.IsChecked ?? false)
            {
                Graph.TypeOX = 2;
            }
            else
            {
                Graph.TypeOX = 1;
            }
            ValueOfDivision = 5;
            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
            LoadItems();
        }

        /// <summary>
        /// обработчик нажатия на кнопку Показать
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            DisableControls();
            RefreshGraph();
            EnableControls();
        }

        /// <summary>
        /// Обработчик изменения параметра фильтрафции
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
        }

        /// <summary>
        /// обработчик изменения состояния TextBox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextChanged(object sender, TextChangedEventArgs e)
        {
            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
        }
    }
}
