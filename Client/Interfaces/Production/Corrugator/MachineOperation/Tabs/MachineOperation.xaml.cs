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
    public partial class MachineOperation : UserControl
    {
        public MachineOperation()
        {
            MomentStart = GetBaseDate();
            MomentEnd = GetBaseDateEnd();
            DebugMode = Central.DebugMode;

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);


            InitCallCounter = 0;
            XValues = new Dictionary<double, string>();

            //цвета графика 
            Colors = new Dictionary<string, string>();

            //производственное задание
            //серый
            Colors.CheckAdd("task_background", "#dddddddd");
            //серый
            Colors.CheckAdd("task_border", "#99999999");

            //простой
            //красный
            Colors.CheckAdd("idle_background", "#33EC5F67");
            //красный
            Colors.CheckAdd("idle_border", "#55EC5F67");

            //группа сырья
            //синий   
            // 7557A2FF FFA9D4FF
            Colors.CheckAdd("block_background", "#75A9D4FF");

            //синий
            Colors.CheckAdd("block_border", "#aaA9D4FF");

            PrimaryButtonAccent = false;

            InitializeComponent();
            CheckPrimaryButtonAccent();

            ProcessPermissions();

            //Init();
            Loaded += OnLoad;
            SizeChanged += OnResize;
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
        public DateTime MomentStart { get; set; }
        public DateTime MomentEnd { get; set; }
        public bool DebugMode { get; set; }
        public ListDataSet Ds { get; set; }
        public int InitCallCounter { get; set; }

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

        private void OnResize(object sender, SizeChangedEventArgs e)
        {
            Init();
        }

        public void Init()
        {
            InitCallCounter++;
            if (InitCallCounter > 1)
            {
                InitForm();
                SetDefaults();
                InitGrid();
            }
        }

        public void InitGrid()
        {
            MomentStart = GetBaseDate();
            MomentEnd = GetBaseDateEnd();

            RefreshButton.Style = (Style)RefreshButton.TryFindResource("Button");

            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Время",
                    Path="TIME",
                    ColumnType=ColumnTypeRef.String,
                },
                new DataGridHelperColumn
                {
                    Header="Время",
                    Path="_SECONDS",
                    ColumnType=ColumnTypeRef.Integer,
                    Params=new Dictionary<string, string>(),
                },
                new DataGridHelperColumn
                {
                    Header="Скорость",
                    Path="SPEED_AVG",
                    ColumnType=ColumnTypeRef.Integer,
                    Params=new Dictionary<string, string>()
                    {
                        //позиция подписи у точки: 1=сверху(50),2=снизу(18)
                        {"LabelYPosition","1"},
                        //диаметр точки, пикс
                        {"PointDiameter","5"},
                        //толщина линии, пикс
                        {"LineThickness","1"},
                        //смещение вправо, чтобы в простой попадали только нулевые точки
                        {"PointXOffset","0"},
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
                                var color = "#cc289744";
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

            XValues = GenerateXValues();

            Graph1Init(columns);
            Graph2Init(columns);
            Graph3Init(columns);
            Graph4Init(columns);
        }

        /// <summary>
        // инициализация компонентов
        /// </summary>
        public void Graph1Init(List<DataGridHelperColumn> columns)
        {
            //инициализация грида
            {
                Graph1.YAxis.Min = 0;
                Graph1.YAxis.Max = 400;
                Graph1.XAxis.Step = 60;
                Graph1.XOffset = 25;

                //колонки грида
                Graph1.DebugMode = DebugMode;
                Graph1.SetColumns(columns);
                Graph1.PrimaryKey = "_SECONDS";
                Graph1.PrimaryLabel = "TIME";
                Graph1.Init();

                Graph1.AutoRender = false;
                Graph1.AutoUpdateInterval = 0;

                ////данные грида
                Graph1.OnLoadItems = Graph1LoadItems;

                Graph1.OnCalculateXValues = (GraphBox ctl) =>
                  {
                      ctl.XValues = XValues;
                  };
            }

            //запуск грида в работу
            Graph1.Run();

            //фокус ввода           
            Graph1.Focus();
        }

        /// <summary>
        // инициализация компонентов
        /// </summary>
        public void Graph2Init(List<DataGridHelperColumn> columns)
        {
            //инициализация грида
            {
                Graph2.YAxis.Min = 0;
                Graph2.YAxis.Max = 400;
                Graph2.XAxis.Step = 60;
                Graph2.XOffset = 25;

                //колонки грида
                Graph2.DebugMode = DebugMode;
                Graph2.SetColumns(columns);
                Graph2.PrimaryKey = "_SECONDS";
                Graph2.PrimaryLabel = "TIME";
                Graph2.Init();

                Graph2.AutoRender = false;
                Graph2.AutoUpdateInterval = 0;

                ////данные грида
                Graph2.OnLoadItems = Graph2LoadItems;

                Graph2.OnCalculateXValues = (GraphBox ctl) =>
                  {
                      ctl.XValues = XValues;
                  };
            }

            //запуск грида в работу
            Graph2.Run();

            //фокус ввода           
            Graph2.Focus();
        }

        /// <summary>
        // инициализация компонентов
        /// </summary>
        public void Graph3Init(List<DataGridHelperColumn> columns)
        {
            //инициализация грида
            {
                Graph3.YAxis.Min = 0;
                Graph3.YAxis.Max = 400;
                Graph3.XAxis.Step = 60;
                Graph3.XOffset = 25;

                //колонки грида
                Graph3.DebugMode = DebugMode;
                Graph3.SetColumns(columns);
                Graph3.PrimaryKey = "_SECONDS";
                Graph3.PrimaryLabel = "TIME";
                Graph3.Init();

                Graph3.AutoRender = false;
                Graph3.AutoUpdateInterval = 0;

                ////данные грида
                Graph3.OnLoadItems = Graph3LoadItems;

                Graph3.OnCalculateXValues = (GraphBox ctl) =>
                  {
                      ctl.XValues = XValues;
                  };
            }

            //запуск грида в работу
            Graph3.Run();

            //фокус ввода           
            Graph3.Focus();
        }

        /// <summary>
        // инициализация компонентов
        /// </summary>
        public void Graph4Init(List<DataGridHelperColumn> columns)
        {
            //инициализация грида
            {
                Graph4.YAxis.Min = 0;
                Graph4.YAxis.Max = 400;
                Graph4.XAxis.Step = 60;
                Graph4.XOffset = 25;

                //колонки грида
                Graph4.DebugMode = DebugMode;
                Graph4.SetColumns(columns);
                Graph4.PrimaryKey = "_SECONDS";
                Graph4.PrimaryLabel = "TIME";
                Graph4.Init();

                Graph4.AutoRender = false;
                Graph4.AutoUpdateInterval = 0;

                ////данные грида
                Graph4.OnLoadItems = Graph4LoadItems;

                Graph4.OnCalculateXValues = (GraphBox ctl) =>
                {
                    ctl.XValues = XValues;
                };
            }

            //запуск грида в работу
            Graph4.Run();

            //фокус ввода           
            Graph4.Focus();
        }


        public async void LoadItemsAll()
        {
            DisableControls();
            CheckPrimaryButtonAccent();
            Graph1.LoadItems();
            Graph2.LoadItems();
            Graph3.LoadItems();
            Graph4.LoadItems();
        }



        public Dictionary<double, string> GenerateXValues()
        {

            /*
            //извлекаем ряд из датасета
            if(ctl.Data.Count>0)
            {
                foreach(Dictionary<string,string> row in ctl.Data)
                {
                    var x=row.CheckGet(ctl.PrimaryKey).ToInt();
                    var y=row.CheckGet(ctl.PrimaryLabel).ToString();
                    if(!ctl.XValues.ContainsKey(x))
                    {
                        ctl.XValues.Add(x,"");
                    }
                    ctl.XValues[x]=y;
                }
            }
            */


            var result = new Dictionary<double, string>();
            var today = GetBaseDate();
            var date = today;

            var step = 60;
            var limit = 60 * 24;

            for (int i = 0; i <= limit; i++)
            {
                var x = Math.Abs((today - date).TotalSeconds);
                var y = date.ToString("HH:mm");
                result.Add(x, y);
                date = date.AddSeconds(step);
            }
            return result;
        }

        public void InitForm()
        {
            //инициализация формы, элементы тулбара
            {
                Form = new FormHelper();

                //колонки формы
                var fields = new List<FormHelperField>()
                {
                    //new FormHelperField()
                    //{
                    //    Path="SEARCH",
                    //    FieldType=FormHelperField.FieldTypeRef.String,
                    //    //Control=Search,
                    //    ControlType="TextBox",
                    //    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    //    }
                    //},
                    new FormHelperField()
                    {
                        Path="TODAY",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=Today,
                        ControlType="TextBox",
                        Default=MomentStart.ToString("dd.MM.yyyy"),
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
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
            Graph1.Destruct();
            Graph2.Destruct();
            Graph3.Destruct();
            Graph4.Destruct();
        }


        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();
        }

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
        public async void Graph1LoadItems()
        {
            DisableControls();
            LoadItems(2, Graph1);
            EnableControls();
        }

        /// <summary>
        /// получение записей
        /// </summary>
        public async void Graph2LoadItems()
        {
            DisableControls();
            LoadItems(21, Graph2);
            EnableControls();
        }

        /// <summary>
        /// получение записей
        /// </summary>
        public async void Graph3LoadItems()
        {
            DisableControls();
            LoadItems(22, Graph3);
            EnableControls();
        }

        /// <summary>
        /// получение записей
        /// </summary>
        public async void Graph4LoadItems()
        {
            DisableControls();
            LoadItems(23, Graph4);
            EnableControls();
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
            Graph1.ShowSplash();

            Progress.Start(1000);
            ProgressContainer.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
            Graph1.HideSplash();

            Progress.Stop();
            ProgressContainer.Visibility = Visibility.Collapsed;
        }

        public async void LoadItems(int machineId, GraphBox graphControl)
        {
            Dictionary<string, ListDataSet> result = null;

            MomentStart = GetBaseDate();
            MomentEnd = GetBaseDateEnd();

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("MACHINE_ID", machineId.ToString());
                p.CheckAdd("DATE", MomentStart.ToString("dd.MM.yyyy"));
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "Machine");
            q.Request.SetParam("Action", "ListSpeed");
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
                    graphControl.ClearData();

                    {
                        var ds = ListDataSet.Create(result, "SPEED");
                        if (ds.Items.Count > 0)
                        {
                            foreach (Dictionary<string, string> row in ds.Items)
                            {
                                var time = row.CheckGet("DTTM");
                                if (!time.IsNullOrEmpty())
                                {
                                    var date = time.ToDateTime();
                                    var s = Math.Abs((MomentStart - date).TotalSeconds) + 30;
                                    row.CheckAdd("_SECONDS", s.ToString());
                                }
                            }
                        }
                        graphControl.UpdateItems(ds);
                    }


                    {
                        var ds = ListDataSet.Create(result, "BLOCK");
                        ds = ProcessTaskItems(ds, graphControl);
                        RenderBlock(ds, graphControl);
                    }

                    {
                        var ds = ListDataSet.Create(result, "TASK");
                        ds = ProcessTaskItems(ds, graphControl);
                        RenderTask(ds, graphControl);
                    }

                    {
                        var ds = ListDataSet.Create(result, "TASK2");
                        ds = ProcessTaskItems(ds, graphControl);
                        RenderTask2(ds, graphControl);
                    }

                    {
                        var ds = ListDataSet.Create(result, "IDLE");
                        ds = ProcessTaskItems(ds, graphControl);
                        RenderIdle(ds, graphControl);
                    }

                    graphControl.Render();
                }

            }
        }


        public ListDataSet ProcessTaskItems(ListDataSet ds, GraphBox grid)
        {

            /*
                Пердаврительная обработка данных.
                В связи с тем, что в WPF сложно добиться пиксельной точности
                в позиционировании блоков, применяем такой алгоритм.
                Здесь границы блоков искуственно подтягиваются друг к другу.
                -- если следующий блок стартует в ту же секунду -- без зазора
                -- если следующий блок стартует в следующую секунду -- зазор 1 пикс
                -- ...
              
             */

            if (ds.Items.Count > 0)
            {
                Dictionary<string, string> row0 = null;

                //опорные координаты
                //текущий блок
                double xRef1 = 0;
                double xRef2 = 0;
                //предыдущий блок
                double xRef01 = 0;
                double xRef02 = 0;

                //экранные координаты
                //текущий блок
                double xScr1 = 0;
                double xScr2 = 0;
                //предыдущий блок
                double xScr01 = 0;
                double xScr02 = 0;

                double w = 0;
                double w0 = 0;


                var items1 = new List<Dictionary<string, string>>();
                foreach (Dictionary<string, string> row in ds.Items)
                {
                    var row1 = new Dictionary<string, string>();
                    foreach (KeyValuePair<string, string> item in row)
                    {
                        var k = item.Key;
                        k = k.ToUpper();
                        row1.Add(k, item.Value);
                    }
                    items1.Add(row1);
                }

                if (items1.Count > 0)
                {
                    ds.Items = items1;
                }

                foreach (Dictionary<string, string> row in ds.Items)
                {
                    xRef01 = xRef1;
                    xRef02 = xRef2;

                    xScr01 = xScr1;
                    xScr02 = xScr2;

                    //начальная координата блока
                    {
                        var time = row.CheckGet("START_DTTM");
                        if (!time.IsNullOrEmpty())
                        {
                            var date = time.ToDateTime();
                            if (date < MomentStart)
                            {
                                date = MomentStart;
                            }
                            xRef1 = (int)Math.Abs((MomentStart - date).TotalSeconds);
                            row.CheckAdd("_START0", xRef1.ToString());
                            xScr1 = grid.TrimX(xRef1);
                            row.CheckAdd("_START", xScr1.ToString());
                        }
                    }

                    //конечная координата блока                    
                    {
                        var time = row.CheckGet("END_DTTM");
                        if (!time.IsNullOrEmpty())
                        {
                            var date = time.ToDateTime();
                            if (date > MomentEnd)
                            {
                                date = MomentEnd;
                            }
                            xRef2 = (int)Math.Abs((MomentStart - date).TotalSeconds);
                            row.CheckAdd("_END0", xRef2.ToString());
                            xScr2 = grid.TrimX(xRef2);
                            row.CheckAdd("_END", xScr2.ToString());
                        }
                    }

                    //ширина блока
                    {
                        w = Math.Abs(xScr2 - xScr1);
                        row.CheckAdd("_WIDTH", w.ToString());
                    }

                    //подстройка ширины блоков
                    {
                        //корректируем предыдущий блок
                        w0 = 0;
                        if (row0 != null)
                        {
                            var l = "";
                            var d = xRef1 - xRef02;

                            if (d == 1)
                            {
                                w0 = xScr1 - xScr01 - 2;
                                l = $"{l}<";
                            }
                            else if (d > 1 && d <= 8)
                            {
                                w0 = xScr1 - xScr01 - 4;
                            }
                            else if (d == 0)
                            {
                                w0 = xScr1 - xScr01;
                            }

                            l = $"{l}({d})";
                            row.CheckAdd("_LABEL", l);
                        }

                        if (w0 != 0)
                        {
                            row0.CheckAdd("_WIDTH", w0.ToString());
                        }
                    }

                    row0 = row;
                }
            }

            return ds;
        }

        public void RenderTask(ListDataSet ds, GraphBox grid)
        {
            if (ds.Items.Count > 0)
            {
                foreach (Dictionary<string, string> row in ds.Items)
                {
                    bool show = true;

                    if (show)
                    {
                        var bc = new BrushConverter();
                        var backgroundBrush = (Brush)bc.ConvertFrom(Colors.CheckGet("task_background"));
                        var borderBrush = (Brush)bc.ConvertFrom(Colors.CheckGet("task_border"));

                        double x = row.CheckGet("_START").ToDouble();
                        double y = 1;
                        double w = row.CheckGet("_WIDTH").ToDouble();
                        double h = row.CheckGet("SPEED_REF").ToDouble();

                        //перевод в экранные координаты
                        y = grid.TrimY(y + h);
                        h *= grid.YAxis.Factor;

                        var b = new Border();
                        {
                            b.SnapsToDevicePixels = true;

                            //FIXME:
                            if (h < 0)
                            {
                                h = 0;
                            }

                            b.Height = h;
                            b.Width = w;
                            b.Margin = new Thickness(x, y, 0, 0);
                            b.HorizontalAlignment = HorizontalAlignment.Left;
                            b.VerticalAlignment = VerticalAlignment.Top;
                            b.BorderThickness = new Thickness(1, 1, 1, 1);
                            b.BorderBrush = borderBrush;
                            b.Background = backgroundBrush;

                            var borderGrid = new Grid();

                            var g = new StackPanel();
                            g.Orientation = Orientation.Vertical;
                            g.VerticalAlignment = VerticalAlignment.Bottom;

                            var number = row.CheckGet("TASK_NUMBER");
                            var number1 = number.CropBefore2("/");
                            var number2 = number.CropAfter2("/");

                            if (DebugMode)
                            {
                                var label = new TextBlock();
                                label.Style = (Style)grid.FindResource("MachineOperationTaskBlockTitle");
                                //label.Text=$"{row.CheckGet("_LABEL")}";
                                label.Text = $"{row.CheckGet("TASK_ID").ToString().ToInt().ToString()}";
                                g.Children.Add(label);
                            }

                            {
                                var label = new TextBlock();
                                label.Style = (Style)grid.FindResource("MachineOperationTaskBlockTitle");
                                label.Text = $"{number1}";
                                g.Children.Add(label);
                            }

                            {
                                var label = new TextBlock();
                                label.Style = (Style)grid.FindResource("MachineOperationTaskBlockTitle");
                                label.Text = $"/{number2}";
                                g.Children.Add(label);
                            }

                            borderGrid.Children.Add(g);

                            //context menu
                            {
                                var menu = new ContextMenu();

                                {
                                    var menuItem = new MenuItem()
                                    {
                                        Header = "Производственное задание",
                                        IsEnabled = true
                                    };
                                    menuItem.Click += (object sender, RoutedEventArgs e) =>
                                    {
                                        Messenger.Default.Send(new ItemMessage()
                                        {
                                            ReceiverGroup = "ProductionTask",
                                            ReceiverName = "TaskList",
                                            SenderName = "MachineOperation",
                                            Action = "Search",
                                            Message = row.CheckGet("TASK_ID").ToString().ToInt().ToString(),
                                        });
                                    };
                                    menu.Items.Add(menuItem);
                                }

                                {
                                    var menuItem = new MenuItem()
                                    {
                                        Header = "Карта ПЗГА",
                                        IsEnabled = true
                                    };
                                    menuItem.Click += (object sender, RoutedEventArgs e) =>
                                    {
                                        ShowProductionTaskMap(row.CheckGet("TASK_ID").ToString().ToInt());
                                    };
                                    menu.Items.Add(menuItem);
                                }



                                borderGrid.ContextMenu = menu;
                            }

                            b.Child = borderGrid;
                        }

                        var tooltip = new InformationTable();
                        tooltip.AddRow("Производственное задание");
                        tooltip.AddRow("ИД", row.CheckGet("ID"));
                        tooltip.AddRow("ИД ПЗ", row.CheckGet("TASK_ID").ToString().ToInt().ToString());
                        tooltip.AddRow("Номер", row.CheckGet("TASK_NUMBER"));
                        tooltip.AddRow("Наименование", row.CheckGet("PROFIL_NAME"));
                        tooltip.AddRow("Начало", row.CheckGet("START_DTTM"));
                        tooltip.AddRow("Окончание", row.CheckGet("END_DTTM"));
                        tooltip.AddRow("Длина, м", row.CheckGet("LENGTH").ToInt().ToString());
                        tooltip.AddRow("Ширина, мм", row.CheckGet("WEB_WIDTH").ToInt().ToString());
                        tooltip.AddRow("Расчетная скорость", row.CheckGet("SPEED_REF").ToInt().ToString());
                        tooltip.AddRow("Код", row.CheckGet("CODE").ToString() + " ");
                        tooltip.AddRow("Тандем", row.CheckGet("TANDEM").ToString());


                        if (DebugMode)
                        {
                            tooltip.AddRow("pos", $"({x};{y}) -> ({x + w}; )");
                            tooltip.AddRow("size", $"{w}x{h}");
                            tooltip.AddRow("s_y", $"y=[{y}] h={h}");
                        }
                        b.ToolTip = tooltip.GetObject();







                        grid.Canvas.Children.Add(b);
                    }
                }
            }
        }

        public void RenderTask2(ListDataSet ds, GraphBox grid)
        {
            if (ds.Items.Count > 0)
            {
                var today = Form.GetValueByPath("TODAY");
                today = $"{today} 08:00:00";
                var todayDt = today.ToDateTime();

                foreach (Dictionary<string, string> row in ds.Items)
                {
                    bool show = false;

                    if (row.CheckGet("START_DTTM").ToDateTime() > todayDt)
                    {
                        show = true;
                    }

                    if (show)
                    {
                        var bc = new BrushConverter();
                        var backgroundBrush = (Brush)bc.ConvertFrom(Colors.CheckGet("task_background"));
                        var borderBrush = (Brush)bc.ConvertFrom(Colors.CheckGet("task_border"));

                        double x = row.CheckGet("_START").ToDouble();
                        double y = 1;
                        double w = row.CheckGet("_WIDTH").ToDouble();
                        double h = row.CheckGet("SPEED_REF").ToDouble();

                        //перевод в экранные координаты
                        y = grid.TrimY(y + h);
                        h *= grid.YAxis.Factor;

                        var b = new Border();
                        {
                            b.SnapsToDevicePixels = true;

                            //FIXME:
                            if (h < 0)
                            {
                                h = 0;
                            }

                            b.Height = 20;
                            b.Width = w;

                            var yo = (int)(grid.Canvas.ActualHeight - (b.Height * 2));
                            y = y - yo;

                            b.Margin = new Thickness(x, y, 0, 0);
                            b.HorizontalAlignment = HorizontalAlignment.Left;
                            b.VerticalAlignment = VerticalAlignment.Top;
                            b.BorderThickness = new Thickness(1, 1, 1, 1);
                            b.BorderBrush = borderBrush;
                            b.Background = backgroundBrush;

                            var borderGrid = new Grid();

                            var g = new StackPanel();
                            g.Orientation = Orientation.Vertical;
                            g.VerticalAlignment = VerticalAlignment.Bottom;

                            var number = row.CheckGet("TASK_NUMBER2");
                            var number1 = number.CropBefore2("/");
                            var number2 = number.CropAfter2("/");

                            if (DebugMode)
                            {
                                var label = new TextBlock();
                                label.Style = (Style)grid.FindResource("MachineOperationTaskBlockTitle");
                                //label.Text=$"{row.CheckGet("_LABEL")}";
                                label.Text = $"{row.CheckGet("ID_PZ")}";
                                g.Children.Add(label);
                            }

                            /*
                            {
                                var label = new TextBlock();
                                label.Style = (Style)grid.FindResource("MachineOperationTaskBlockTitle");
                                label.Text=$"{number1}";
                                g.Children.Add(label);
                            }

                            {
                                var label = new TextBlock();
                                label.Style = (Style)grid.FindResource("MachineOperationTaskBlockTitle");
                                label.Text=$"/{number2}";
                                g.Children.Add(label);
                            }
                            */

                            borderGrid.Children.Add(g);

                            //context menu
                            {
                                var menu = new ContextMenu();

                                {
                                    var menuItem = new MenuItem()
                                    {
                                        Header = "Производственное задание",
                                        IsEnabled = true
                                    };
                                    menuItem.Click += (object sender, RoutedEventArgs e) =>
                                    {
                                        Messenger.Default.Send(new ItemMessage()
                                        {
                                            ReceiverGroup = "ProductionTask",
                                            ReceiverName = "TaskList",
                                            SenderName = "MachineOperation",
                                            Action = "Search",
                                            Message = row.CheckGet("TASK_ID").ToString().ToInt().ToString(),
                                        });
                                    };
                                    menu.Items.Add(menuItem);
                                }

                                {
                                    var menuItem = new MenuItem()
                                    {
                                        Header = "Карта ПЗГА",
                                        IsEnabled = true
                                    };
                                    menuItem.Click += (object sender, RoutedEventArgs e) =>
                                    {
                                        ShowProductionTaskMap(row.CheckGet("TASK_ID").ToString().ToInt());
                                    };
                                    menu.Items.Add(menuItem);
                                }



                                borderGrid.ContextMenu = menu;
                            }

                            b.Child = borderGrid;
                        }

                        var tooltip = new InformationTable();
                        tooltip.AddRow($"Производственное задание {row.CheckGet("ID_ST").ToString().ToInt().ToString()}");
                        tooltip.AddRow("ИД", row.CheckGet("ID"));
                        tooltip.AddRow("ИД ПЗ", row.CheckGet("ID_PZ").ToString().ToInt().ToString());
                        tooltip.AddRow("Номер", row.CheckGet("TASK_NUMBER"));
                        tooltip.AddRow("Наименование", row.CheckGet("PROFIL_NAME"));
                        tooltip.AddRow("Начало", row.CheckGet("START_DTTM"));
                        tooltip.AddRow("Окончание", row.CheckGet("END_DTTM"));
                        tooltip.AddRow("Длина, м", row.CheckGet("LENGTH").ToInt().ToString());
                        tooltip.AddRow("Ширина, мм", row.CheckGet("WEB_WIDTH").ToInt().ToString());
                        tooltip.AddRow("Расчетная скорость", row.CheckGet("SPEED_REF").ToInt().ToString());
                        tooltip.AddRow("Код", row.CheckGet("CODE").ToString() + " ");
                        tooltip.AddRow("Тандем", row.CheckGet("TANDEM").ToString());


                        if (DebugMode)
                        {
                            tooltip.AddRow("pos", $"({x};{y}) -> ({x + w}; )");
                            tooltip.AddRow("size", $"{w}x{h}");
                            tooltip.AddRow("s_y", $"y=[{y}] h={h}");
                        }
                        b.ToolTip = tooltip.GetObject();







                        grid.Canvas.Children.Add(b);
                    }
                }
            }
        }


        public async void ShowProductionTaskMap(int productionTask)
        {
            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();

                {
                    p.Add("ID", productionTask.ToString());
                    p.Add("TEMP_FILE", "1");
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "ProductionTask");
                q.Request.SetParam("Action", "TaskGetMap");

                q.Request.SetParams(p);

                q.Request.Timeout = 10000;
                q.Request.Attempts = 1;

                q.Request.Timeout = Central.Parameters.RequestTimeoutMin;
                //q.Request.Attempts=;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    Central.OpenFile(q.Answer.DownloadFilePath);
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        public void RenderIdle(ListDataSet ds, GraphBox grid)
        {
            if (ds.Items.Count > 0)
            {
                foreach (Dictionary<string, string> row in ds.Items)
                {
                    bool show = true;

                    if (show)
                    {
                        var bc = new BrushConverter();
                        var backgroundBrush = (Brush)bc.ConvertFrom(Colors.CheckGet("idle_background"));
                        var borderBrush = (Brush)bc.ConvertFrom(Colors.CheckGet("idle_border"));

                        double x = row.CheckGet("_START").ToDouble();
                        double y = 1;
                        double w = row.CheckGet("_WIDTH").ToDouble();
                        double h = Graph4.YAxis.Max;

                        //перевод в экранные координаты
                        y = grid.TrimY(y + h);
                        h *= grid.YAxis.Factor;

                        var b = new Border();
                        {
                            b.SnapsToDevicePixels = true;

                            //FIXME:
                            if (h < 0)
                            {
                                h = 0;
                            }

                            b.Height = h;
                            b.Width = w;
                            b.Margin = new Thickness(x, y, 0, 0);
                            b.HorizontalAlignment = HorizontalAlignment.Left;
                            b.VerticalAlignment = VerticalAlignment.Top;
                            b.BorderThickness = new Thickness(1, 1, 1, 1);
                            b.BorderBrush = borderBrush;
                            b.Background = backgroundBrush;
                        }

                        var tooltip = new InformationTable();
                        tooltip.AddRow("Простой");
                        tooltip.AddRow("ИД", row.CheckGet("ID"));
                        tooltip.AddRow("Начало", row.CheckGet("START_DTTM"));
                        tooltip.AddRow("Окончание", row.CheckGet("END_DTTM"));
                        if (DebugMode)
                        {
                            tooltip.AddRow("x", x.ToString());
                            tooltip.AddRow("w", w.ToString());
                        }
                        b.ToolTip = tooltip.GetObject();

                        grid.Canvas.Children.Add(b);
                    }
                }
            }
        }


        public void RenderBlock(ListDataSet ds, GraphBox grid)
        {
            if (ds.Items.Count > 0)
            {
                foreach (Dictionary<string, string> row in ds.Items)
                {
                    bool show = true;

                    if (show)
                    {
                        var bc = new BrushConverter();
                        var backgroundBrush = (Brush)bc.ConvertFrom(Colors.CheckGet("block_background"));
                        var borderBrush = (Brush)bc.ConvertFrom(Colors.CheckGet("block_border"));

                        double x = row.CheckGet("_START").ToDouble();
                        double y = 1;
                        double w = row.CheckGet("_WIDTH").ToDouble();
                        double h = Math.Round(Graph4.YAxis.Max * 0.9, 0).ToInt();

                        //перевод в экранные координаты
                        y = grid.TrimY(y + h);
                        h *= grid.YAxis.Factor;

                        var b = new Border();
                        {
                            b.SnapsToDevicePixels = true;
                            b.Height = h;
                            b.Width = w;
                            b.Margin = new Thickness(x, y, 0, 0);
                            b.HorizontalAlignment = HorizontalAlignment.Left;
                            b.VerticalAlignment = VerticalAlignment.Top;
                            b.BorderThickness = new Thickness(1, 1, 1, 1);
                            b.BorderBrush = borderBrush;
                            b.Background = backgroundBrush;
                        }

                        var tooltip = new InformationTable();
                        tooltip.AddRow("Блок");
                        tooltip.AddRow("ИД", row.CheckGet("PCBW_ID"));
                        tooltip.AddRow("Начало", row.CheckGet("START_DTTM"));
                        tooltip.AddRow("Окончание", row.CheckGet("END_DTTM"));
                        tooltip.AddRow("Ширина, мм", row.CheckGet("WIDTH").ToInt().ToString());
                        tooltip.AddRow("Код", row.CheckGet("CODE"));
                        tooltip.AddRow("Описание", row.CheckGet("DESCRIPTION"));
                        if (DebugMode)
                        {
                            tooltip.AddRow("x", x.ToString());
                            tooltip.AddRow("w", w.ToString());
                        }
                        b.ToolTip = tooltip.GetObject();

                        grid.Canvas.Children.Add(b);
                    }
                }
            }
        }

        /// <summary>
        /// базовая дата, начало интервала
        /// TODAY
        /// dd.MM.yyyy 08:00:00
        /// </summary>
        /// <returns></returns>
        public DateTime GetBaseDate()
        {
            var v = new Dictionary<string, string>();
            if (Form != null)
            {
                v = Form.GetValues();
            }
            var todayDateString = v.CheckGet("TODAY");
            if (todayDateString.IsNullOrEmpty())
            {
                todayDateString = DateTime.Now.ToString("dd.MM.yyyy");
            }
            var todayDateTimeString = $"{todayDateString} 08:00:00";
            var today = todayDateTimeString.ToDateTime();

            return today;
        }

        /// <summary>
        /// базовая дата, конец интервала
        /// BASE+1day
        /// dd.MM.yyyy 08:00:00
        /// </summary>
        /// <returns></returns>
        public DateTime GetBaseDateEnd()
        {
            var today = GetBaseDate();
            today = today.ToString("dd.MM.yyyy").ToDateTime().AddDays(1);
            var today2DateString = today.ToString("dd.MM.yyyy");
            var today2DateTimeString = $"{today2DateString} 08:00:00";
            today = today2DateTimeString.ToDateTime();
            return today;
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
                    Graph1.LoadItems();
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
            Central.ShowHelp("/doc/l-pack-erp/service/accounts/users");
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


        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }



        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadItemsAll();
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

        private void Grid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var d1 = Graph1.GraphHorizontalScroll.HorizontalOffset - e.Delta;
            var d2 = Graph2.GraphHorizontalScroll.HorizontalOffset - e.Delta;
            var d3 = Graph3.GraphHorizontalScroll.HorizontalOffset - e.Delta;
            var d4 = Graph4.GraphHorizontalScroll.HorizontalOffset - e.Delta;
            Graph1.GraphHorizontalScroll.ScrollToHorizontalOffset(d1);
            Graph2.GraphHorizontalScroll.ScrollToHorizontalOffset(d2);
            Graph3.GraphHorizontalScroll.ScrollToHorizontalOffset(d3);
            Graph4.GraphHorizontalScroll.ScrollToHorizontalOffset(d4);
        }
    }
}
