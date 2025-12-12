using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Sales
{
    /// <summary>
    /// Продажи
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2021-06-07</released>     
    public partial class SalesReport : UserControl
    {        
        public SalesReport()
        {
            InitializeComponent();

            SelectedItemId = 0;
            Columns=new List<Dictionary<string,string>>();
            ProgressUpdateInterval=1;
            ControlUpdateInterval=5;
            ProgressValue=0;
            GeneratingReport=false;
            AwaitingResponce=false;
            ManagerList= new Dictionary<string,string>();
            PartnerList= new Dictionary<string,string>();
            BuyerList= new Dictionary<string,string>();
            ChannelList=new Dictionary<string, string>();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            //регистрация обработчика клавиатуры
            PreviewKeyDown += ProcessKeyboard;
            //Loaded += OnLoad;
            
            InitGrid();
            SetDefaults();
            BaseGrid.Run();

            ProcessPermissions();
        }

        public string RoleName = "[erp]sales_report";

        /// <summary>
        /// датасет, содержащий данные
        /// </summary>
        public ListDataSet ItemsDS { get; set; }
        /// <summary>
        /// набор динамических колонок
        /// используется для формирования грида и документа excel
        /// </summary>
        public List<Dictionary<string,string>> Columns { get; set; }

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        public int SelectedItemId { get; set; }
        Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// интервал обновления прогресс-бара занятости
        /// </summary>
        public int ProgressUpdateInterval { get; set; }
        public DispatcherTimer ProgressUpdateTimer { get; set; }
        public int ProgressValue { get;set;}

        /// <summary>
        /// интервал работы контроля длиетльности
        /// </summary>
        public int ControlUpdateInterval { get; set; }
        public DispatcherTimer ControlUpdateTimer { get; set; }

        /// <summary>
        /// Флажок, показывающий, что идет затяжная генерация данных со сбросом кэша
        /// Это происходит только по кнопке "сформировать"
        /// После получения данных сбрасывается.
        /// </summary>
        public bool GeneratingReport { get;set;}

        /// <summary>
        /// Флаг ожидания данных.
        /// После получения данных сбрасывается.
        /// </summary>
        public bool AwaitingResponce { get;set;}

        /// <summary>
        /// Часть таблицы со статическими колонками
        /// </summary>
        public ScrollViewer BaseGridViewer { get;set;}
        /// <summary>
        /// Часть таблицы с динамическими колонками
        /// </summary>
        public ScrollViewer DynamicGridViewer { get;set;}

        /// <summary>
        /// Содержимое выпадающего списка менеджеров
        /// </summary>
        public Dictionary<string, string> ManagerList { get; set; }
        
        /// <summary>
        /// Содержимое выпадающего списка партнеров
        /// </summary>
        public Dictionary<string, string> PartnerList { get; set; }

        /// <summary>
        /// Содержимое выпадающего списка покупателей
        /// </summary>
        public Dictionary<string, string> BuyerList { get; set; }

        public Dictionary<string, string> ChannelList { get; set; }

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
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            SearchText.Text = "";
            Created.Content="";

            var y=DateTime.Now.ToString("yyyy");
            var d=$"01.01.{y}";
            FromDate.Text = d.ToDateTime().ToString("dd.MM.yyyy");
            ToDate.Text = DateTime.Now.ToString("dd.MM.yyyy");

            UpdateFilters();
        }

        

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            //Group ProductionTask
            if (m.ReceiverGroup.IndexOf("Sales") > -1)
            {
                switch (m.Action)
                {
                    case "Refresh":
                        BaseGrid.LoadItems();
                        break;
                }
            }
        }

        /// <summary>
        /// обновление списков выбора
        /// </summary>
        public void UpdateFilters()
        {
            {
                var list = new Dictionary<string, string>();
                list.Add("-1", "Все");
                if(ChannelList.Count>0)
                {
                    list.AddRange(ChannelList);
                }

                Channel.Items = list;
                Channel.SelectedItem = list.FirstOrDefault((x) => x.Key == "-1");
            }

            {
                var list = new Dictionary<string, string>();
                list.Add("-1", "Все");
                list.Add("ЛОП", "Липецк");
                list.Add("МОП", "Москва");
                list.Add("ОРК", "ОРК");
                list.Add("РБ", "РБ");
                Department.Items = list;
                Department.SelectedItem = list.FirstOrDefault((x) => x.Key == "-1");
            }

            {
                var list = new Dictionary<string, string>();
                list.Add("-1", "Все");
                if(ManagerList.Count>0)
                {
                    list.AddRange(ManagerList);
                }
                Manager.Items = list;
                Manager.SelectedItem = list.FirstOrDefault((x) => x.Key == "-1");
            }

            {
                var list = new Dictionary<string, string>();
                list.Add("-1", "Все");    
                if(PartnerList.Count>0)
                {
                    list.AddRange(PartnerList);
                }
                Partner.Items = list;
                Partner.SelectedItem = list.FirstOrDefault((x) => x.Key == "-1");
            }

            {
                var list = new Dictionary<string, string>();
                list.Add("-1", "Все");
                if(BuyerList.Count>0)
                {
                    list.AddRange(BuyerList);
                }
                Buyer.Items = list;
                Buyer.SelectedItem = list.FirstOrDefault((x) => x.Key == "-1");
            }
        }
        
        /// <summary>
        /// инициализация грида
        /// </summary>
        public void InitGrid()
        {
            var rowNumberHidden=true;
            
            //в отладочном режиме покажем колонку с номером строки
            if(Central.DebugMode)
            {
                rowNumberHidden=false;
            }

            //base grid
            {
                //список колонок грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        Group=$" ",
                        ColumnType=ColumnTypeRef.Integer,        
                        Hidden=rowNumberHidden,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ОП",
                        Path="DEPARTMENTSHORTNAME",
                        Group=$" ",
                        ColumnType=ColumnTypeRef.String,                        
                        Width=40,                        
                    },
                    new DataGridHelperColumn
                    {
                        Header="Менеджер",
                        Path="MANAGERNAME",
                        Group=$" ",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=90,
                        MaxWidth=120,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Канал продаж",
                        Path="SALESCHANNEL",
                        Group=$" ",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=90,
                        MaxWidth=100,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Партнер",
                        Path="PARTNERNAME",
                        Group=$" ",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=80,
                        MaxWidth=120,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№",
                        Path="BUYERID",
                        Group=$"Покупатель",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=37,
                        MaxWidth=37,
                    },
                    new DataGridHelperColumn
                    {           
                        Header="Имя",
                        Path="BUYERNAME",
                        Group=$"Покупатель",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=80,
                        MaxWidth=200,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Отрасль",
                        Path="INDUSTRY",
                        Group=$" ",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=80,
                        MaxWidth=200,
                    }, 
                };               
                BaseGrid.SetColumns(columns);
                BaseGrid.SearchText = SearchText;
                BaseGrid.AutoUpdateInterval=0;
                BaseGrid.Init();
                              
                //данные грида
                BaseGrid.OnLoadItems = LoadItems;
                BaseGrid.OnFilterItems = FilterItems;
            }
            
            //dynamic grid
            {
                var columns = new List<DataGridHelperColumn>();
                if(Columns.Count>0)
                {
                    {
                        var column=new DataGridHelperColumn
                        {
                            Header="#",
                            Path="_ROWNUMBER",
                            ColumnType=ColumnTypeRef.Integer,                                                
                            Hidden=rowNumberHidden,
                        };
                        columns.Add(column);
                    }

                    var y1=0;
                    foreach(Dictionary<string,string> c in Columns)
                    {
                        y1=c.CheckGet("YEAR").ToInt();

                        {
                            var column=new DataGridHelperColumn
                            {
                                Header=$"{c.CheckGet("MONTH")}",
                                Path=$"{c.CheckGet("PATH")}",
                                Group=$"{c.CheckGet("YEAR")}",
                                ColumnType=ColumnTypeRef.Double,
                                Format="N0",
                                MinWidth=80,
                                MaxWidth=100,
                            };

                            if(c.CheckGet("MONTH")!="TOTAL")
                            {
                                columns.Add(column);
                            }
                        }

                        if(c.CheckGet("MONTH").ToInt()==12)
                        {
                            var m=$"{c.CheckGet("YEAR")}-TOTAL";
                            
                            var column=new DataGridHelperColumn
                            {
                                Header=$"Итог",
                                Path=m,
                                Group=$"{c.CheckGet("YEAR")}",
                                ColumnType=ColumnTypeRef.Double,
                                Format="N0",
                                MinWidth=80,
                                MaxWidth=100,
                            };
                            columns.Add(column);
                        }
                    }

                    { 
                        var m=$"{y1}-TOTAL";
                            
                        var column=new DataGridHelperColumn
                        {
                            Header=$"Итог",
                            Path=m,
                            Group=$"{y1}",
                            ColumnType=ColumnTypeRef.Double,
                            Format="N0",
                            MinWidth=80,
                            MaxWidth=100,
                            
                           
                        };
                        columns.Add(column);
                    }

                    
                    { 
                        var m=$"TOTAL";
                            
                        var column=new DataGridHelperColumn
                        {
                            Header=$"Всего",
                            Path=m,
                            Group=$"{y1}",
                            ColumnType=ColumnTypeRef.Double,
                            Format="N0",
                            MinWidth=80,
                            MaxWidth=100,
                           
                        };
                        columns.Add(column);
                    }
                    


                }
                DynamicGrid.SetColumns(columns);
                DynamicGrid.SearchText = SearchText;
                DynamicGrid.AutoUpdateInterval=0;
                DynamicGrid.Init();
            }

            BaseGrid.Grid.VerticalScrollBarVisibility=ScrollBarVisibility.Hidden;

            BaseGrid.Grid.HorizontalScrollBarVisibility=ScrollBarVisibility.Visible;
            DynamicGrid.Grid.HorizontalScrollBarVisibility=ScrollBarVisibility.Visible;
            DynamicGrid.DisableRowHeader();
            
            BaseGrid.SelectItemMode=2;
            DynamicGrid.SelectItemMode=2;

            BaseGrid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    if(selectedItem.ContainsKey("_ROWNUMBER"))
                    {
                        DynamicGrid.SelectRowByKey(selectedItem["_ROWNUMBER"].ToInt(),"_ROWNUMBER",false);
                        SyncScrolls("base");
                    }
                }
            };

            DynamicGrid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    if(selectedItem.ContainsKey("_ROWNUMBER"))
                    {
                        BaseGrid.SelectRowByKey(selectedItem["_ROWNUMBER"].ToInt(),"_ROWNUMBER",false);
                        SyncScrolls("dynamic");
                    }
                }
            };

            BaseGrid.OnScroll=(sender,e) =>
            {
                SyncScrolls("base");
            };

            DynamicGrid.OnScroll=(sender,e) =>
            {
                SyncScrolls("dynamic");
            };
        }


        /// <summary>
        /// получение записей
        /// </summary>
        public async void LoadItems()
        {
            GridToolbar.IsEnabled = false;
            //Grid.ShowSplash();

            
            
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
            }

            if (resume)
            {
                if(GeneratingReport)
                {
                    ShowProgressSplash();
                }  

                /*
                    В этом запросе получаем данные для построения отчета
                    Горизонтальная часть таблицы содержит статические поля и динамические.
                    Набор динамических колонок меняется в зависимости от текущей даты.

                    Начиная с 13 пошла динамика.
                    Динамические колонки определены в дополнительной секции: DataCols
                 */

                var p=new Dictionary<string,string>();
                {
                    p.Add("FROM_DATE",FromDate.Text);
                    p.Add("TO_DATE",ToDate.Text);
                }
                
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "Reports");
                q.Request.SetParam("Action", "MakeSales");
                q.Request.SetParams(p);

                q.Request.Timeout=60000;

                if(GeneratingReport)
                {
                    q.Request.SetParam("FLUSHCACHE", "1");
                }
                else
                {
                    q.Request.SetParam("FLUSHCACHE", "0");
                }

                AwaitingResponce=true;
                RunControlTimer();
                await Task.Run(() =>
                {
                    q.DoQuery();
                });
                AwaitingResponce=false;
                StopControlTimer();

                Columns=new List<Dictionary<string,string>>();
                if (q.Answer.Status == 0)
                {
                    var t = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (t != null)
                    {
                        if (t.ContainsKey("DataCols"))
                        {
                            // получаем колонки для динамической части таблицы, затем заново отрисовываем таблицу
                            var colsDS = t["DataCols"];
                            colsDS?.Init();

                            if(colsDS.Items.Count>0)
                            {
                                foreach(Dictionary<string,string> row in colsDS.Items)
                                {
                                    Columns.Add(row);                                  
                                }
                            }
                            InitGrid();
                        }

                        // заполняем таблицы данными
                        if (t.ContainsKey("DataItems"))
                        {
                            ItemsDS = t["DataItems"];
                            ItemsDS?.Init();
                            BaseGrid.UpdateItems(ItemsDS);             
                            DynamicGrid.UpdateItems(ItemsDS);   
                            
                            //BaseGrid.UpdateGrid();
                            //DynamicGrid.UpdateGrid();
                        }

                        // обновляем содержимое выпадающих списков
                        if (ItemsDS.Items.Count>0)
                        {
                            
                            ManagerList = new Dictionary<string,string>();
                            PartnerList = new Dictionary<string,string>();
                            BuyerList = new Dictionary<string,string>();
                            foreach(Dictionary<string,string> row in ItemsDS.Items)
                            {
                                var k="";
                                {
                                    k="MANAGERNAME";
                                    if(row.ContainsKey(k))
                                    {
                                        var k2=row[k];                                        
                                        var v2=row[k];
                                        if(!v2.IsNullOrEmpty())
                                        {
                                            if(!ManagerList.ContainsKey(k2))
                                            {
                                                ManagerList.Add(k2,v2);
                                            }
                                        }
                                    }
                                }

                                {
                                    k="PARTNERNAME";
                                    if(row.ContainsKey(k))
                                    {
                                        var k2=row[k];
                                        var v2=row[k];
                                        if(!PartnerList.ContainsKey(k2))
                                        {
                                            PartnerList.Add(k2,v2);
                                        }
                                    }
                                }

                                {
                                    k="BUYERNAME";
                                    if(row.ContainsKey(k))
                                    {
                                        var k2=row[k];
                                        var v2=row[k];
                                        if(!BuyerList.ContainsKey(k2))
                                        {
                                            BuyerList.Add(k2,v2);
                                        }
                                    }
                                }

                                {
                                    k="SALESCHANNEL";
                                    if(row.ContainsKey(k))
                                    {
                                        var k2=row[k];
                                        var v2=row[k];
                                        if(!ChannelList.ContainsKey(k2))
                                        {
                                            ChannelList.Add(k2,v2);
                                        }
                                    }
                                }
                                
                            }
                            UpdateFilters();
                        }


                        if (t.ContainsKey("Statistics"))
                        {
                            var statisticsDS = t["Statistics"];
                            statisticsDS?.Init();
                            if(statisticsDS.Items.Count>0)
                            {
                                if(statisticsDS.Items[0]!=null)
                                {
                                    if(statisticsDS.Items[0].ContainsKey("CREATIONDATE"))
                                    {
                                        Created.Content=statisticsDS.Items[0]["CREATIONDATE"].ToString();
                                    }

                                    if(statisticsDS.Items[0].ContainsKey("STARTDATE"))
                                    {
                                        FromDate.Text=statisticsDS.Items[0]["STARTDATE"].ToString();
                                    }

                                    if(statisticsDS.Items[0].ContainsKey("FINISHDATE"))
                                    {
                                        ToDate.Text=statisticsDS.Items[0]["FINISHDATE"].ToString();
                                    }
                                }
                            }
                        }

                    }
                }

            }

            GridToolbar.IsEnabled = true;

            if(GeneratingReport)
            {
                HideProgressSplash();   
            }            

            GeneratingReport=false;

            BaseGridViewer=UIUtil.GetScrollViewer(BaseGrid.Grid);
            DynamicGridViewer=UIUtil.GetScrollViewer(DynamicGrid.Grid);     
        }

        /// <summary>
        /// фильтрация записей
        /// </summary>
        public void FilterItems()
        {
            if (BaseGrid.GridItems != null)
            {
                if (BaseGrid.GridItems.Count > 0)
                {

                    bool doFilteringByDepartment = false;
                    string department = "-1";
                    if (Department.SelectedItem.Key != null && Department.SelectedItem.Key!="-1")
                    {
                        doFilteringByDepartment = true;
                        department = Department.SelectedItem.Key;
                    }

                    bool doFilteringByChannel = false;
                    string channel = "-1";
                    if (Channel.SelectedItem.Key != null && Channel.SelectedItem.Key!="-1")
                    {
                        doFilteringByChannel = true;
                        channel = Channel.SelectedItem.Key;
                    }

                    bool doFilteringByManager = false;
                    string manager = "-1";
                    if (Manager.SelectedItem.Key != null && Manager.SelectedItem.Key!="-1")
                    {
                        doFilteringByManager = true;
                        manager = Manager.SelectedItem.Key;
                    }

                    bool doFilteringByPartner = false;
                    string partner = "-1";
                    if (Partner.SelectedItem.Key != null && Partner.SelectedItem.Key!="-1")
                    {
                        doFilteringByPartner = true;
                        partner = Partner.SelectedItem.Key;
                    }

                    bool doFilteringByBuyer = false;
                    string buyer = "-1";
                    if (Buyer.SelectedItem.Key != null && Buyer.SelectedItem.Key!="-1")
                    {
                        doFilteringByBuyer = true;
                        buyer = Buyer.SelectedItem.Key;
                    }

                    var items = new List<Dictionary<string, string>>(ItemsDS.Items);

                    if (
                        doFilteringByDepartment
                        || doFilteringByChannel
                        || doFilteringByManager
                        || doFilteringByPartner
                        || doFilteringByBuyer
                    )
                    {
                        items = new List<Dictionary<string, string>>();
                        foreach (var row in ItemsDS.Items)
                        {
                            bool includeByDepartment = false;
                            bool includeByChannel = false;
                            bool includeByManager = false;
                            bool includeByPartner = false;
                            bool includeByBuyer = false;

                            if (doFilteringByDepartment)
                            {
                                if (row.ContainsKey("DEPARTMENTSHORTNAME"))
                                {
                                    if (row["DEPARTMENTSHORTNAME"] == department)
                                    {
                                        includeByDepartment = true;
                                    }
                                }
                            }
                            else
                            {
                                includeByDepartment=true;
                            }

                            if (doFilteringByChannel)
                            {
                                if (row.ContainsKey("SALESCHANNEL"))
                                {
                                    if (row["SALESCHANNEL"] == channel)
                                    {
                                        includeByChannel = true;
                                    }
                                }
                            }
                            else
                            {
                                includeByChannel=true;
                            }

                            if (doFilteringByManager)
                            {
                                if (row.ContainsKey("MANAGERNAME"))
                                {
                                    if (row["MANAGERNAME"] == manager)
                                    {
                                        includeByManager = true;
                                    }
                                }
                            }
                            else
                            {
                                includeByManager=true;
                            }

                            if (doFilteringByPartner)
                            {
                                if (row.ContainsKey("PARTNERNAME"))
                                {
                                    if (row["PARTNERNAME"] == partner)
                                    {
                                        includeByPartner = true;
                                    }
                                }
                            }
                            else
                            {
                                includeByPartner=true;
                            }

                            if (doFilteringByBuyer)
                            {
                                if (row.ContainsKey("BUYERNAME"))
                                {
                                    if (row["BUYERNAME"] == buyer)
                                    {
                                        includeByBuyer = true;
                                    }
                                }
                            }
                            else
                            {
                                includeByBuyer=true;
                            }

                            if (
                                includeByDepartment
                                && includeByChannel
                                && includeByManager
                                && includeByPartner
                                && includeByBuyer
                            )
                            {
                                items.Add(row);
                            }
                        }
                    }
                    BaseGrid.GridItems = items;
                    DynamicGrid.Items = items;
                }
            }
        }

        /// <summary>
        /// Синхронизирует вертикальную прокрутку в статической и динамической таблицах
        /// base, dynamic
        /// source -- t.m. "from"   
        /// </summary>
        /// <param name="source"></param>
        public void SyncScrolls(string source="base")
        {
            if(BaseGridViewer!=null && DynamicGridViewer!=null)
            {
                if(source=="base")
                {
                    var vo=BaseGridViewer.VerticalOffset;
                    DynamicGridViewer.ScrollToVerticalOffset(vo);
                }
                else
                {
                    var vo=DynamicGridViewer.VerticalOffset;
                    BaseGridViewer.ScrollToVerticalOffset(vo);
                }
            }
        }

        /// <summary>
        /// Отображение прогресс-бара
        /// </summary>
        public void ShowProgressSplash()
        {
            Splash.Visibility=Visibility.Visible;
            RunProgressTimer();
        }

        /// <summary>
        /// Скрытие прогресс-бара
        /// </summary>
        public void HideProgressSplash()
        {
            Splash.Visibility=Visibility.Collapsed;
            StopProgressTimer();
        }

        
        /// <summary>
        /// таймер обновления прогресс-бара
        /// </summary>
        public void RunProgressTimer()
        {
            ProgressValue=0;
            if(ProgressUpdateInterval!=0)
            {
                if(ProgressUpdateTimer == null)
                {
                    ProgressUpdateTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0,0,ProgressUpdateInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", ProgressUpdateInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("SalesReport_RunProgressTimer", row);
                    }

                    ProgressUpdateTimer.Tick += (s,e) =>
                    {
                        ProgressValue=ProgressValue+ProgressUpdateInterval;
                        Progress.Value=ProgressValue;
                    };
                }

                if(ProgressUpdateTimer.IsEnabled)
                {
                    ProgressUpdateTimer.Stop();
                }
                ProgressUpdateTimer.Start();
            }
           
        }

        /// <summary>
        /// Остановка таймера обновления прогресс-бара
        /// </summary>
        public void StopProgressTimer()
        {
            if(ProgressUpdateTimer != null)
            {
                if(ProgressUpdateTimer.IsEnabled)
                {
                    ProgressUpdateTimer.Stop();
                }
            }
        }

        /// <summary>
        /// watchdog следящий за получением данных
        /// Eсли получение данных идет дольше 5 сек (может быть такая ситуация,
        /// что кэш сброшен, но клиент об этом не знает), тогда отображается
        /// сплэш с прогресс-баром, развлекающим пользователя.
        /// </summary>
        public void RunControlTimer()
        {
            if(ControlUpdateInterval!=0)
            {
                if(ControlUpdateTimer == null)
                {
                    ControlUpdateTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0,0,ControlUpdateInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", ControlUpdateInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("SalesReport_RunControlTimer", row);
                    }

                    ControlUpdateTimer.Tick += (s,e) =>
                    {
                        GeneratingReport=true;
                        ShowProgressSplash();
                        StopControlTimer();
                    };
                }

                if(ControlUpdateTimer.IsEnabled)
                {
                    ControlUpdateTimer.Stop();
                }
                ControlUpdateTimer.Start();
            }
           
        }

        /// <summary>
        /// Останавливает работу таймера
        /// </summary>
        public void StopControlTimer()
        {
            if(ControlUpdateTimer != null)
            {
                if(ControlUpdateTimer.IsEnabled)
                {
                    ControlUpdateTimer.Stop();
                }
            }
        }

        /// <summary>
        /// Обработчик нажатия на клавиши
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessKeyboard(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

                case Key.Home:
                    BaseGrid.SetSelectToFirstRow();
                    e.Handled = true;
                    break;

                case Key.End:
                    BaseGrid.SetSelectToLastRow();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// Вызов справки
        /// </summary>
        public void ShowHelp()
        {
            //Central.ShowHelp("/doc/l-pack-erp/shipments/control/report");
        }

        /// <summary>
        /// Обработчик нажатия на кнопку справки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HelpButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ShowHelp();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку формирования отчета
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GenerateButton_Click(object sender,RoutedEventArgs e)
        {
            GeneratingReport=true;
            BaseGrid.LoadItems();
        }

        /// <summary>
        /// Экспорт данных из таблицы в Excel
        /// </summary>
        public async void ExportToExcel()
        {
            var eg = new ExcelGrid();

            var cols=BaseGrid.Columns;
            cols.AddRange(DynamicGrid.Columns);
            eg.SetColumnsFromGrid(cols);

            eg.Items = ItemsDS.Items;
            await Task.Run(() =>
            {
                eg.Make();
            });
        }

        private void ExportExcelButton_Click(object sender,RoutedEventArgs e)
        {
            ExportToExcel();
        }

        private void Bayer_SelectedItemChanged(DependencyObject d,DependencyPropertyChangedEventArgs e)
        {
            BaseGrid.UpdateItems();
        }

        private void Types_SelectedItemChanged(DependencyObject d,DependencyPropertyChangedEventArgs e)
        {
            BaseGrid.UpdateItems();
        }

        private void Manager_SelectedItemChanged(DependencyObject d,DependencyPropertyChangedEventArgs e)
        {
            BaseGrid.UpdateItems();
        }

        private void Department_SelectedItemChanged(DependencyObject d,DependencyPropertyChangedEventArgs e)
        {
            BaseGrid.UpdateItems();
        }

        private void Channel_SelectedItemChanged(DependencyObject d,DependencyPropertyChangedEventArgs e)
        {
            BaseGrid.UpdateItems();
        }
        
    }
}
