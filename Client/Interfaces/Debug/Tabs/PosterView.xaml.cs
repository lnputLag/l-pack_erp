using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Debug
{
    public partial class PosterView:UserControl
    {
        public PosterView()
        {
            InitializeComponent();

            SelectedItemId = 0;
            Module="";
            Object="";
            Action="";
            Params="";
            Result=new Dictionary<string, ListDataSet>();

            GridBlock.Visibility=Visibility.Collapsed;

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            //регистрация обработчика клавиатуры
            PreviewKeyDown += OnKeyDown;

            InitGrid();
            InitForm();
            SetDefaults();
        }

        public FormHelper Form { get;set;}

        public string Module { get; set; }
        public string Object { get; set; }
        public string Action { get; set; }
        public string Params { get; set; }

        public Dictionary<string, ListDataSet> Result { get; set; }

        public void InitGrid()
        {
            //инициализация грида
            {
                //список колонок грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="#",
                        Path="ID",
                        ColumnType=ColumnTypeRef.Integer,                 
                    },
                 
                     
                };
                Grid.SetColumns(columns);

                Grid.SetSorting("ROWNNMBER", ListSortDirection.Ascending);
                Grid.SearchText = SearchText;
                Grid.Init();

                //данные грида
                Grid.OnLoadItems = LoadItems;
                Grid.OnFilterItems = FilterItems;
                Grid.Run();

                //фокус ввода           
                Grid.Focus();
            }
            
        }

        public void InitForm()
        {
            Form=new FormHelper();
            //список колонок формы
            var fields=new List<FormHelperField>()
            {
                
                new FormHelperField()
                { 
                    Path="MODULE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ModuleField,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                        { FormHelperField.FieldFilterRef.Required, null },                    
                    },
                },
                new FormHelperField()
                { 
                    Path="OBJECT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ObjectField,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                        { FormHelperField.FieldFilterRef.Required, null },                    
                    },
                },
                new FormHelperField()
                { 
                    Path="ACTION",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ActionField,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                        { FormHelperField.FieldFilterRef.Required, null },                    
                    },
                },
                new FormHelperField()
                { 
                    Path="PARAMS",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ParamsField,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                    },
                },
               
            };

            Form.SetFields(fields);
            Form.OnValidate=(bool valid, string message) =>
            {
                if(valid)
                {
                    FormStatus.Text="";
                }
                else
                {
                    if(!string.IsNullOrEmpty(message))
                    {
                        FormStatus.Text=message;
                    }
                    else
                    {
                        FormStatus.Text="Не все поля заполнены верно";
                    }                    
                }
            };

            SetDefaults();
        }
        
        public void SetDefaults()
        {
            SearchText.Text = "";
            ModuleField.Text="";
            ObjectField.Text="";
            ActionField.Text="";
            ParamsField.Text="";
            Repeat.Text="1";
            Log.Text="";
            RequestTimeout.Text="10000";
            RequestAttempts.Text="1";
            RequestInterval.Text="0";
        }

        /// <summary>
        /// датасет, содержащий данные
        /// </summary>
        public ListDataSet ItemsDS { get; set; }

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        public int SelectedItemId { get; set; }
        Dictionary<string, string> SelectedItem { get; set; }


        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            //Group ProductionTask
            if (m.ReceiverGroup.IndexOf("User") > -1)
            {
                switch (m.Action)
                {
                    case "Refresh":
                        Grid.LoadItems();
                        break;
                }
            }
        }

        /// <summary>
        /// получение записей
        /// </summary>
        public async void LoadItems()
        {
            return;

            GridToolbar.IsEnabled = false;
            Grid.ShowSplash();
            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();
                
                await Task.Run(() =>
                {
                    ItemsDS = _LPackClientDataProvider.DoQueryDeserialize<ListDataSet>("Accounts", "User", "List", "Items", p);
                });


                ItemsDS?.Init();
                Grid.UpdateItems(ItemsDS);
            }

            GridToolbar.IsEnabled = true;
            Grid.HideSplash();
        }

        /// <summary>
        /// фильтрация записей
        /// </summary>
        public async void FilterItems()
        {
            if (Grid.GridItems != null)
            {
                if (Grid.GridItems.Count > 0)
                {
                    //обработка строк

                    //фильтрация 

                }
            }
        }

        /// <summary>
        /// обновление методов работы с выбранной записью
        /// </summary>
        /// <param name="selectedItem"></param>
        public void UpdateActions(Dictionary<string, string> selectedItem)
        {
            SelectedItem = selectedItem;
            if(SelectedItem.ContainsKey("ID"))
            {
                SelectedItemId=SelectedItem["ID"].ToInt();
            }
        }

        public void Send()
        {
            if(Form.Validate())
            {
                var p=Form.GetValues();
                //p.Add("ID",Id.ToString());
                DoSend0(p);
            }
        }

        public void LogMsg(string text)
        {
            var t=DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss_ffffff");
            var s="";
            s=$"{t} {text}";
            Log.Text=Log.Text.Append(s,true);
            Log.ScrollToEnd();
        }

        public int RepeatCounter {get;set;}
        public int StepCounter {get;set;}
        public Timeout RequestTimer {get;set;}
        public void DoSend0(Dictionary<string,string> p)
        {
            Progress1.Visibility=Visibility.Visible;

            var repeat=Repeat.Text.ToInt();
            RepeatCounter=repeat;
            StepCounter=0;
            var r=repeat;
            var concurrents=1;
            var step=0;
            var profiler=new Profiler();
            profiler.AddPoint();
            Log.Text="";

            if(repeat < 1)
            {
                repeat=1;
            }

            {
                var q=$"{ModuleField.Text}>{ObjectField.Text}>{ActionField.Text}";
                var s=$"{q} repeat=[{repeat}]";
                LogMsg(s);
            }

            var interval=RequestInterval.Text.ToInt();
            if(interval > 0)
            {
                RequestTimer=new Common.Timeout(
                    1,
                    ()=>{
                        if(RepeatCounter > 0)
                        {
                            StepCounter++;
                            DoSend(p,true,StepCounter);
                            RepeatCounter--;
                        }
                        else
                        {
                            RequestTimer.Finish();
                        }
                    },
                    true
                );
                RequestTimer.SetIntervalMs(interval);
                RequestTimer.Run();
            }
            else
            {
                while(repeat > 0)
                {
                    for(int i=0; i<concurrents; i++)
                    {
                        step++;
                        DoSend(p,true,step);
                        repeat--;
                    }
                }
            }

            SaveButton.IsEnabled=true;
            Progress1.Visibility=Visibility.Hidden;
        }

        public async void DoSend(Dictionary<string,string> p, bool async=true, int step=0)
        {
            SaveButton.IsEnabled=false;
            ResultTextField.Text="Sending request...";

            SectionsToolbar.Children.Clear();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", p["MODULE"]);
            q.Request.SetParam("Object", p["OBJECT"]);
            q.Request.SetParam("Action", p["ACTION"]);

            q.Request.Timeout=RequestTimeout.Text.ToInt();
            q.Request.Attempts=RequestAttempts.Text.ToInt();

            var parameters=ParamsField.Text;
            if(!string.IsNullOrEmpty(parameters))
            {
                var args = parameters.Split('\n');

                if(args.Length>0)
                {
                    for(int index = 0; index < args.Length; index = index+1)
                    {
                        //arguments.Add(args[index],args[index+1]);
                        var bits=args[index].Split('=');
                        if(bits.Length>0)
                        {
                            var k=bits[0];
                            var v=bits[1];
                                                       

                            if(!string.IsNullOrEmpty(k))
                            {
                                //ResultTextField.Text=$"{ResultTextField.Text}\n{k}={v}";
                                k=k.Trim();

                                v=v.Replace("\n","");
                                v=v.Replace("\r","");
                                v=v.Trim();
                                q.Request.SetParam(k,v);
                            }
                            

                        }
                    }
                }
            }

            /*
                в результате получим:
                -- кастомный результат
                -- коллекцию датасетов

                Если кастомный результат, покаем его исходный текст.
                Если коллекция датасетов, дадим пользователю выбрать датасет.
                (?) определение коллекции
                Когда пользователь выбрал датасет, отрендерим его результат:
                 -- покажем его исходный текст
                 -- распарсим структуру и содадим под него грид
                   (?) определение стуктуры


                А теперь...
                Если структура распарсилась и грид отрисовался.
                Дадим пользователю определить (входящие пераметры парсера):
                 -- тип колонки
                 -- ширина колонки
                 -- порядок колонки
                                    => это покажите в отдельном окне
                Дадим возможность запустить генератор, который на выходе выдаст 
                определение набора колонок для использования в исходном тексте

                Эти функции ускорят построение интерфейса с гридом в разы)

                RAW, Grid, Generator

             */

             if(async)
             {
                await Task.Run(()=>{ 
                    q.DoQuery();                               
                });
             }
             else
             {
                q.DoQuery();                               
             }
                 
             ProcessAnswer(q,step);
             SaveButton.IsEnabled=true;

        }

        public void ProcessAnswer(LPackClientQuery q,int step=0)
        {
            RequestStatus.Text=q.Answer.Status.ToString();
            RequestTime.Text=q.Answer.Time.ToString();

            var connection = Central.LPackClient.CurrentConnection;
            LogMsg($"    ({step}) [{connection.Host}:{connection.Port}] status=[{q.Answer.Status}] time=[{q.Answer.Time}]");

            if (q.Answer.Status == 0)
            {
                ResultTextField.Text="";

                switch (q.Answer.Type)
                {
                    case LPackClientAnswer.AnswerTypeRef.File:
                    {
                        {
                                var s = "";
                                s = s.Append($"\nFileName=[{q.Answer.DownloadFileName}]");
                                s = s.Append($"\nFilePath=[{q.Answer.DownloadFilePath}]");
                                //var x = JsonHelper.FormatJson(q.Answer.Data);
                                ResultTextField.Text=$"{ResultTextField.Text}\n{s}";
                        }

                        Central.OpenFile(q.Answer.DownloadFilePath);
                    }
                    break;
                    
                    default:
                    {
                        { 
                            var x = JsonHelper.FormatJson(q.Answer.Data);
                            ResultTextField.Text=$"{ResultTextField.Text}\n{x}";
                        }
        
                        try
                        {
                            Result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        }
                        catch(Exception e)
                        {
                            //ResultTextField.Text="Deserialize result exception";
                            Result=null;
                        }
                        
                        if (
                            Result!=null
                            //&& p["ACTION"]!="GetUsers"
                        )
                        {
                            foreach( KeyValuePair<string, ListDataSet> item in Result)
                            {
                                var k=item.Key;
                                var ds=item.Value;
        
                                { 
                                    var b=new Button();
                                    b.Content=k;
                                    b.Name=k;
                                    b.Style=(Style)GridToolbar.FindResource("Button");
                                    b.Click+=B_Click;
        
                                    SectionsToolbar.Children.Add(b);
                                }
                            }    
                        }
                    }
                    break;
                    
                }
                
                

            }
            else
            {
                //q.ProcessError();
                q.SilentErrorProcess = true;
                q.ProcessError();
            }
        }

        public void Set(LPackClientQuery q)
        {
            ModuleField.Text=q.Request.Params.CheckGet("Module");
            ObjectField.Text=q.Request.Params.CheckGet("Object");
            ActionField.Text=q.Request.Params.CheckGet("Action");
            
            
            string paramsString="";
            if(q.Request.Params.Count>0)
            {
                foreach(KeyValuePair<string,string> p in q.Request.Params)
                {
                    var k=p.Key;
                    var v=p.Value;
                    if(
                        k!="Module"
                        || k!="Object"
                        || k!="Action"
                        || k!="Token"
                    )
                    {
                        if(paramsString.Length>0)
                        {
                            paramsString=$"{paramsString}\n";
                        }
                        paramsString=$"{paramsString}{k}={v}";
                    }
                }
            }
            ParamsField.Text=paramsString;
            
            ProcessAnswer(q);

            Show();
        }

        public void Show()
        {
            Central.WM.Show($"Poster","Постер",true,"add",this);
        }

        private void LoadGridData(string name)
        {
            var resume=true;
            if(resume)
            {
                if(string.IsNullOrEmpty(name))
                {
                    resume=false;
                }
            }

            if(resume)
            {
                if(Result!=null)
                {
                    if(Result.ContainsKey(name))
                    {
                        var ds=Result[name];
                        if(ds!=null)
                        {
                            if(!ds.Initialized)
                            {
                                ds.Init();
                            }

                            var columns = new List<DataGridHelperColumn>();
                            foreach(string c in ds.Cols )
                            {
                                var column=new DataGridHelperColumn
                                {
                                    Header=$"{c}",
                                    Path=$"{c}",                                
                                    ColumnType=ColumnTypeRef.String,
                                    MinWidth=80,
                                    MaxWidth=120,
                                };

                                columns.Add(column);
                            }

                            Grid.SetColumns(columns);
                            Grid.SearchText = SearchText;
                            Grid.AutoUpdateInterval=0;
                            Grid.Init();
                            Grid.UpdateItems(ds);      
                        }
                    }
                }
            }

            if(resume)
            {
                GridBlock.Visibility=Visibility.Visible;
            }
            
        }


     
        private void B_Click(object sender,RoutedEventArgs e)
        {
            var b=(Button)sender;
            if(b!=null)
            {
                if(!string.IsNullOrEmpty(b.Name))
                {
                    LoadGridData(b.Name);
                }               
            }
        }

        private string format_json(string json)
        {
            dynamic parsedJson = JsonConvert.DeserializeObject(json);
            return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
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

        public void ShowHelp()
        {
            //Central.ShowHelp("/doc/l-pack-erp/shipments/control/report");
        }

        

        private void ExcelButton_Click(object sender, RoutedEventArgs e)
        {
            Grid.ExportItemsExcel();
        }

        private void RefreshButton_Click(object sender,RoutedEventArgs e)
        {
            Grid.LoadItems();
        }

        private void SendButton_Click(object sender,RoutedEventArgs e)
        {
            Send();
        }

        private void HelpButton_Click(object sender,RoutedEventArgs e)
        {

        }

        private void GetVersionButton_Click(object sender,RoutedEventArgs e)
        {
            ModuleField.Text="Session";
            ObjectField.Text="Info";
            ActionField.Text="GetVersion";
            ParamsField.Text="";
        }

        private void ShipmentsListButton_Click(object sender,RoutedEventArgs e)
        {
            ModuleField.Text="Shipments";
            ObjectField.Text="Listing";
            ActionField.Text="List";
            ParamsField.Text="";
        }

        private void ShipmentsPlanButton_Click(object sender,RoutedEventArgs e)
        {
            ModuleField.Text="Shipments";
            ObjectField.Text="Plan";
            ActionField.Text="List";
            ParamsField.Text="";

            //ModuleField.Text="Shipments";
            //ObjectField.Text="BindDriver";
            //ActionField.Text="Get";
            //ParamsField.Text="TRANSPORTID=506681\nDRIVERLOGID=194833";
        }

        private void UsersListButton_Click(object sender,RoutedEventArgs e)
        {
            ModuleField.Text="Accounts";
            ObjectField.Text="User";
            ActionField.Text="List"; 
            ParamsField.Text="";
        }

        private void GetMetricsButton_Click(object sender,RoutedEventArgs e)
        {
            ModuleField.Text="Session";
            ObjectField.Text="Info";
            ActionField.Text="GetMetrics";      
            ParamsField.Text="";
        }

        private void GetUsersButton_Click(object sender,RoutedEventArgs e)
        {
            ModuleField.Text="Session";
            ObjectField.Text="Info";
            ActionField.Text="GetUsers";  
            ParamsField.Text="";
        }

        private void SalesReport1Button_Click(object sender,RoutedEventArgs e)
        {
            ModuleField.Text="Sales";
            ObjectField.Text="Report";
            ActionField.Text="Products";  
            ParamsField.Text="";
        }

        private void GetTaskDebugButton_Click(object sender,RoutedEventArgs e)
        {
            ModuleField.Text="Production";
            ObjectField.Text="ProductionTask";
            ActionField.Text="GetDebug";  
            ParamsField.Text="ID=";
        }
        
        private void GetTaskLogButton_Click(object sender,RoutedEventArgs e)
        {
            ModuleField.Text="Production";
            ObjectField.Text="ProductionTask";
            ActionField.Text="GetLog";  
            ParamsField.Text="ID=";
        }

        private void ProductionTaskTest1Button_Click(object sender,RoutedEventArgs e)
        {
            ModuleField.Text="Production";
            ObjectField.Text="ProductionTask";
            ActionField.Text="ProductionTaskTest1";  
            ParamsField.Text="ID_ORDERDATES=1176081\nID2=304881\nID2_PRODUCT=304881";
        }

        private void GetTaskDebug2Button_Click(object sender,RoutedEventArgs e)
        {
            ModuleField.Text="Production";
            ObjectField.Text="ProductionTask";
            ActionField.Text="Get";  
            ParamsField.Text="ID=";
        }

        private void GetLayersButton_Click(object sender,RoutedEventArgs e)
        {
            ModuleField.Text="Production";
            ObjectField.Text="Cutter";
            ActionField.Text="GetLayers";  
            ParamsField.Text="ID=";
        }

        private void RollListButton_Click(object sender,RoutedEventArgs e)
        {
            var today=DateTime.Now.ToString("dd.MM.yyyy");

            ModuleField.Text="Production";
            ObjectField.Text="Roll";
            ActionField.Text="List";  
            ParamsField.Text=$"TODAY={today}";
        }

        private void ProductionTaskListDiagramButton_OnClick(object sender, RoutedEventArgs e)
        {
            var today=DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy");

            ModuleField.Text="Production";
            ObjectField.Text="ProductionTask";
            ActionField.Text="ListDiagram";  
            ParamsField.Text=$"TODAY={today}";
        }
        
        private void LoadingGetMapButton_Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text="Shipments";
            ObjectField.Text="Loading";
            ActionField.Text="GetMap";  
            ParamsField.Text=$"ID=526694";
        }

        private void Sales_Click(object sender,RoutedEventArgs e)
        {
            ModuleField.Text="Sales";
            ObjectField.Text="Reports";
            ActionField.Text="MakeSales";  
            ParamsField.Text=$"FROM_DATE=01.01.2022\nTO_DATE=01.02.2022\nFLUSHCACHE=1";
        }

        private void GetCurrentButton_Click(object sender,RoutedEventArgs e)
        {
            ModuleField.Text="Production";
            ObjectField.Text="ProductionTask";
            ActionField.Text="GetCurrent";  
            ParamsField.Text=$"MACHINE_ID=2";
        }

        private void GetDataButton_Click(object sender,RoutedEventArgs e)
        {
            ModuleField.Text="Production";
            ObjectField.Text="Roll";
            ActionField.Text="GetCurrent";  
            ParamsField.Text=$"MACHINE_ID=2\nROLL_ID=2";
        }

        private void GetMapButton_Click(object sender,RoutedEventArgs e)
        {
            ModuleField.Text="Production";
            ObjectField.Text="ProductionTask";
            ActionField.Text="TaskGetMap";  
            ParamsField.Text=$"ID=2488390";
        }

        private void GetLabelButton_Click(object sender,RoutedEventArgs e)
        {
            ModuleField.Text="Production";
            ObjectField.Text="Roll";
            ActionField.Text="GetReceipt";  
            ParamsField.Text=$"ID=7076772";
        }

        private void ListUpdateProductionDateStatus_Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text = "Production";
            ObjectField.Text = "Roll";
            ActionField.Text = "ListUpdateProductionDateStatus";
            ParamsField.Text = $"";
            RequestTimeout.Text = "300000";
        }

        private void Control_GetMetrics_Click(object sender,RoutedEventArgs e)
        {
            ModuleField.Text="Control";
            ObjectField.Text="Info";
            ActionField.Text="GetMetrics";      
            ParamsField.Text="";
        }

        private void Control_GetSessions_Click(object sender,RoutedEventArgs e)
        {
            ModuleField.Text="Control";
            ObjectField.Text="Info";
            ActionField.Text="GetSessions";      
            ParamsField.Text="";
        }

        private void Control_GetDBConnections_Click(object sender,RoutedEventArgs e)
        {
            ModuleField.Text="Control";
            ObjectField.Text="Info";
            ActionField.Text="GetDBConnections";      
            ParamsField.Text="";
        }

        private void Control_GetThreads_Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text = "Control";
            ObjectField.Text = "Info";
            ActionField.Text = "GetThreads";
            ParamsField.Text = "";
        }

        

        private void GetServerParams_Click(object sender,RoutedEventArgs e)
        {
            ModuleField.Text="Session";
            ObjectField.Text="Info";
            ActionField.Text="GetServerParams";  
            ParamsField.Text="";
        }

        private void GetReelDataButton_Click(object sender,RoutedEventArgs e)
        {
            ModuleField.Text="Production";
            ObjectField.Text="Reel";
            ActionField.Text="GetData";  
            ParamsField.Text="";
        }

        private void MenuItem_Click(object sender,RoutedEventArgs e)
        {

        }

        private void ShimpentsGetLoadingMap_Click(object sender,RoutedEventArgs e)
        {
            ModuleField.Text="Shipments";
            ObjectField.Text="Loading";
            ActionField.Text="GetMap";  
            ParamsField.Text= "ID=547978\nDEMO=0";
        }

        private void SalesSecondary_Click(object sender,RoutedEventArgs e)
        {
            ModuleField.Text="Sales";
            ObjectField.Text="Reports";
            ActionField.Text="MakeSecondarySales";  
            ParamsField.Text=$"FROM_DATE=01.01.2022\nTO_DATE=01.05.2022";
        }

        private void ImportWaste_Click(object sender,RoutedEventArgs e)
        {
            ModuleField.Text="Production";
            ObjectField.Text="Cutter";
            ActionField.Text="ImportWaste";  
            ParamsField.Text=$"";
        }

        private void ListTaskButton_Click(object sender,RoutedEventArgs e)
        {
            ModuleField.Text="Production";
            ObjectField.Text="Roll";
            ActionField.Text="ListReelTasks";  
            ParamsField.Text=$"MACHINE_ID=2\nREEL_NUMBER=1";
        }

        private void EmailGetMetrics_Click(object sender,RoutedEventArgs e)
        {
            var today=DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy");
            ModuleField.Text="Messages";
            ObjectField.Text="Email";
            ActionField.Text="GetMetrics";  
            ParamsField.Text=$"TODAY={today}";
        }

        private void CreateEmail_Click(object sender, RoutedEventArgs e)
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            ModuleField.Text = "Messages";
            ObjectField.Text = "Email";
            ActionField.Text = "Save";

            var s = "";
            s = s.Append($"ID=0");
            s = s.Append($"\nSUBJECT=test_{today}");
            s = s.Append($"\nMESSAGE=test");
            s = s.Append($"\nRECIPIENT=balchugov_dv@l-pak.ru");
            s = s.Append($"\nATTACH_NAME=fsc_forest.jpg");
            s = s.Append($"\nATTACH_PATH=/mnt/storage/shared/temp/fsc_forest.jpg");
            ParamsField.Text = s;
        }

        

        private void ListUncuttedButton_Click(object sender, RoutedEventArgs e)
        {
            var today=DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy");

            ModuleField.Text="Production";
            ObjectField.Text="Position";
            ActionField.Text="ListUncutted";  
            ParamsField.Text=$"POSITION_ID=1304757,1305062";
        }

        private void DebugErrorValidator_Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text="Debug";
            ObjectField.Text="Test";
            ActionField.Text="ErrorValidator";  
            ParamsField.Text=$"";
        }

        private void DebugErrorSql_Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text="Debug";
            ObjectField.Text="Test";
            ActionField.Text="ErrorSql";  
            ParamsField.Text=$"";
        }

        private void DebugErrorAlert_Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text="Debug";
            ObjectField.Text="Test";
            ActionField.Text="ErrorAlert";  
            ParamsField.Text=$"MESSAGE=test";
        }

        private void GetDataDebug_Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text="Production";
            ObjectField.Text="Reel";
            ActionField.Text="GetDataDebug";  
            ParamsField.Text=$"REEL_ID=1";
        }

        private void Login2Button_Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text="Session";
            ObjectField.Text="Auth";
            ActionField.Text="Login2";  
            ParamsField.Text="LOGIN=node\nPASSWORD=";
        }

        private void Login3Button_Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text="Session";
            ObjectField.Text="Auth";
            ActionField.Text="Login3";  
            ParamsField.Text="LOGIN=node\nPASSWORD=";
        }

        private void Login3_balchugov_dvButton_Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text="Session";
            ObjectField.Text="Auth";
            ActionField.Text="Login3";  
            ParamsField.Text="LOGIN=balchugov_dv\nPASSWORD=1234";
        }

        private void Login3_mukienko_vvButton_Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text="Session";
            ObjectField.Text="Auth";
            ActionField.Text="Login3";  
            ParamsField.Text="LOGIN=mukienko_vv\nPASSWORD=0908";
        }

        private void Login3_nodeButton_Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text="Session";
            ObjectField.Text="Auth";
            ActionField.Text="Login3";  
            ParamsField.Text="LOGIN=node\nPASSWORD=";
        }

        private void Login3_cm1Button_Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text="Session";
            ObjectField.Text="Auth";
            ActionField.Text="Login3";  
            ParamsField.Text="LOGIN=cm1\nPASSWORD=";
        }

        private void Login3_monitoringButton_Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text="Session";
            ObjectField.Text="Auth";
            ActionField.Text="Login3";  
            ParamsField.Text="LOGIN=monitoring\nPASSWORD=";
        }

        private void Login2_balchugov_dvButton_Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text="Session";
            ObjectField.Text="Auth";
            ActionField.Text="Login2";  
            ParamsField.Text="Login=balchugov_dv\nPassword=1234";
        }

        private void Login2_mukienko_vvButton_Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text="Session";
            ObjectField.Text="Auth";
            ActionField.Text="Login2";  
            ParamsField.Text="Login=mukienko_vv\nPassword=0908";
        }

        private void Login2_nodeButton_Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text="Session";
            ObjectField.Text="Auth";
            ActionField.Text="Login2";  
            ParamsField.Text="Login=node\nPassword=";
        }

        private void Login2_cm1Button_Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text="Session";
            ObjectField.Text="Auth";
            ActionField.Text="Login2";  
            ParamsField.Text="Login=cm1\nPassword=";
        }

        private void Login2_monitoringButton_Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text="Session";
            ObjectField.Text="Auth";
            ActionField.Text="Login2";  
            ParamsField.Text="Login=monitoring\nPassword=";
        }



        private void LiteBase_SaveData_OnClick(object sender, RoutedEventArgs e)
        {
            ModuleField.Text="Service";
            ObjectField.Text="LiteBase";
            ActionField.Text="SaveData";
            
            var data=new Dictionary<string,string>();
            data.CheckAdd("Name","Bob");
            data.CheckAdd("Age","43");
            var s = JsonConvert.SerializeObject(data);

            var p = "";
            p = p.Append($"TABLE_DIRECTORY=\n");
            p = p.Append($"TABLE_NAME=_test\n");            
            p = p.Append($"PRIMARY_KEY=ID\n");
            p = p.Append($"PRIMARY_KEY_VALUE=12\n");
            // Тип добавления информации в файл. 1 -- перезапись; 2 -- добавление в конец.
            p = p.Append($"APPEND_TYPE=1\n");
            // 1=global,2=local,3=net
            p = p.Append($"STORAGE_TYPE=1\n");            
            p = p.Append($"ITEMS={s}");

            ParamsField.Text=p;
        }

        private void LiteBase_SaveList_OnClick(object sender, RoutedEventArgs e)
        {
            //ModuleField.Text = "Service";
            //ObjectField.Text = "LiteBase";
            //ActionField.Text = "SaveList";

            //var data = new Dictionary<string, string>();
            //data.CheckAdd("Name", "Bob");
            //data.CheckAdd("Age", "43");
            //var s = JsonConvert.SerializeObject(data);

            //ParamsField.Text = $"ITEMS={s}\nTABLE_NAME=_test\nPRIMARY_KEY=ID\nPRIMARY_KEY_VALUE=12";
        }

        private void LiteBase_List_OnClick(object sender, RoutedEventArgs e)
        {
            ModuleField.Text = "Service";
            ObjectField.Text = "LiteBase";
            ActionField.Text = "List";

            var p = "";
            p = p.Append($"STORAGE_TYPE=1\n");
            p = p.Append($"TABLE_DIRECTORY=\n");
            p = p.Append($"TABLE_NAME=_test");
            ParamsField.Text = p;
        }

        private void LiteBase_Get_OnClick(object sender, RoutedEventArgs e)
        {
            ModuleField.Text = "Service";
            ObjectField.Text = "LiteBase";
            ActionField.Text = "Get";

            var p = "";
            p = p.Append($"STORAGE_TYPE=1\n");
            p = p.Append($"TABLE_DIRECTORY=\n");
            p = p.Append($"TABLE_NAME=_test\n");            
            p = p.Append($"PRIMARY_KEY=ID\n");
            p = p.Append($"PRIMARY_KEY_VALUE=12");
            ParamsField.Text = p;
        }

        private void LiteBase_Delete_OnClick(object sender, RoutedEventArgs e)
        {
            ModuleField.Text = "Service";
            ObjectField.Text = "LiteBase";
            ActionField.Text = "DeleteData";

            var p = "";
            p = p.Append($"STORAGE_TYPE=1\n");
            p = p.Append($"TABLE_DIRECTORY=\n");
            p = p.Append($"TABLE_NAME=_test\n");
            p = p.Append($"PRIMARY_KEY=ID\n");
            p = p.Append($"PRIMARY_KEY_VALUE=12");
            ParamsField.Text = p;
        }


        private void Labels_Test1_Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text="Stock";
            ObjectField.Text="Label";
            ActionField.Text="MakeTest";
            
            ParamsField.Text=$"idpz=1\nnum=1\nformat=html";
        }

        public void Labels_Make1_Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text="Stock";
            ObjectField.Text="Label";
            ActionField.Text="Make";
            
            ParamsField.Text=$"PALLET_ID=12100919\nFORMAT=html\nMODE=1";
        }

        public void Labels_Make2_Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text="Stock";
            ObjectField.Text="Label";
            ActionField.Text="Make";
            
            ParamsField.Text=$"PALLET_ID=12100919\nFORMAT=html\nMODE=2";
        }

        public void Labels_Make3_Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text="Stock";
            ObjectField.Text="Label";
            ActionField.Text="Make";
            
            ParamsField.Text=$"PALLET_ID=12100919\nFORMAT=html\nMODE=3";
        }

        public void Labels_Docx_Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text = "Sales";
            ObjectField.Text = "Sale";
            ActionField.Text = "GetUniversalTransferDocument";

            ParamsField.Text = $"INVOICE_ID=1652368\nDOCUMENT_FORMAT_NAME=docx\nSIGNOTORY_EMPLOYEE_ID=193";
        }

        public void Stacker_GetProductionTask_Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text="Production";
            ObjectField.Text="Stacker";
            ActionField.Text="GetProductionTask";
            
            ParamsField.Text=$"MACHINE_ID=2\nSTACKER_NUM=1";

            RequestAttempts.Text="1";
            RequestTimeout.Text="1000";
            RequestInterval.Text="1000";
            Repeat.Text="100";
        }

        public void GetTaskData_Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text="Production";
            ObjectField.Text="CorrugatorMachineOperator";
            ActionField.Text="GetTaskData";
            
            ParamsField.Text=$"ID_ST=2";

            RequestAttempts.Text="1";
            RequestTimeout.Text="1000";
            RequestInterval.Text="1000";
            Repeat.Text="100";
        }


        

        public void DBC2TestBlock_Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text="Debug";
            ObjectField.Text="DBC2Test";
            ActionField.Text="Block";
            
            ParamsField.Text=$"";

            RequestAttempts.Text="1";
            RequestTimeout.Text="1000";
            RequestInterval.Text="0";
            Repeat.Text="0";
        }

        public void SFDate_Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text = "Debug";
            ObjectField.Text = "DBC2Test";
            ActionField.Text = "SFDate";

            var s = "";
            s = s.Append($"ORDER_ID=1712290");
            s = s.Append($"\nOLD_DATE=13.09.2024");
            s = s.Append($"\nDATA=13.09.2024");
            ParamsField.Text = s;

            RequestAttempts.Text = "1";
            RequestTimeout.Text = "1000";
            RequestInterval.Text = "0";
            Repeat.Text = "0";
        }

        public void MonitorCorrugatorCounterOnClick(object sender, RoutedEventArgs e)
        {
            ModuleField.Text = "Service";
            ObjectField.Text = "Monitor";
            ActionField.Text = "CorrugatorCounter";

            var s = "";
            s = s.Append($"DATE_START=04.09.2024 08:00:00");
            s = s.Append($"\nDATE_FINISH=05.09.2024 07:59:59");
            s = s.Append($"\nMACHINE_ID=21");
            s = s.Append($"\nMODE=0");
            ParamsField.Text = s;

            RequestAttempts.Text = "1";
            RequestTimeout.Text = "10000";
            RequestInterval.Text = "0";
            Repeat.Text = "0";
        }

        public void OpcListDataOnClick(object sender, RoutedEventArgs e)
        {
            ModuleField.Text = "Service";
            ObjectField.Text = "ReadOpc";
            ActionField.Text = "ListData";

            var s = "";
            s = s.Append($"OPC_SERVER_NAME=server_opc-3");
            ParamsField.Text = s;

            RequestAttempts.Text = "1";
            RequestTimeout.Text = "10000";
            RequestInterval.Text = "0";
            Repeat.Text = "0";
        }

        

        public void MlTlgMessageCreateOnClick(object sender, RoutedEventArgs e)
        {
            ModuleField.Text = "MailingListTelegram";
            ObjectField.Text = "Message";
            ActionField.Text = "Save";

            var s = "";
            s = s.Append($"BOT_NAME=tender");
            s = s.Append($"\nTEXT=test");
            s = s.Append($"\nRECIPIENT_ID=");
            ParamsField.Text = s;

            RequestAttempts.Text = "1";
            RequestTimeout.Text = "10000";
            RequestInterval.Text = "0";
            Repeat.Text = "0";
        }

        public void MlTlgSendAllOnClick(object sender, RoutedEventArgs e)
        {
            ModuleField.Text = "Bot";
            ObjectField.Text = "BotControl";
            ActionField.Text = "DoAction";

            var s = "";
            s = s.Append($"ACTION=SEND_MESSAGES");
            ParamsField.Text = s;

            RequestAttempts.Text = "1";
            RequestTimeout.Text = "10000";
            RequestInterval.Text = "0";
            Repeat.Text = "0";
        }

        



        private void ShipmentReport_SaveData_Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text = "Shipments";
            ObjectField.Text = "Position";
            ActionField.Text = "GetShipmentReport";

            ParamsField.Text = $"SHIPMENT_ID=641160\nFORMAT=html";
        }

        private void ParameterSet_Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text = "Control";
            ObjectField.Text = "Parameter";
            ActionField.Text = "Set";

            ParamsField.Text = $"NAME=\nVALUE=";

            RequestAttempts.Text = "1";
            RequestTimeout.Text = "1000";
            RequestInterval.Text = "0";
            Repeat.Text = "0";
        }

        private void ParameterList_Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text = "Control";
            ObjectField.Text = "Parameter";
            ActionField.Text = "List";

            ParamsField.Text = $"";

            RequestAttempts.Text = "1";
            RequestTimeout.Text = "1000";
            RequestInterval.Text = "0";
            Repeat.Text = "0";
        }

        private void ParameterUpdate_Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text = "Control";
            ObjectField.Text = "Parameter";
            ActionField.Text = "Update";

            ParamsField.Text = $"";

            RequestAttempts.Text = "1";
            RequestTimeout.Text = "1000";
            RequestInterval.Text = "0";
            Repeat.Text = "0";
        }

        private void DocTestOnclick(object sender, RoutedEventArgs e)
        {
            ModuleField.Text = "Sales";
            ObjectField.Text = "Sale";
            ActionField.Text = "GetTestDocument";

            var s = "";
            s = s.Append($"INVOICE_ID=1668571");
            s = s.Append($"\nDOCUMENT_FORMAT_NAME=docx");
            s = s.Append($"\nSIGNOTORY_EMPLOYEE_ID=193");
            s = s.Append($"\nDOCUMENT_TEMPLATE=TestDocument.html");
            ParamsField.Text = s;

            RequestAttempts.Text = "1";
            RequestTimeout.Text = "10000";
            RequestInterval.Text = "0";
            Repeat.Text = "0";
        }

        private void ReceiptDocument2Onclick(object sender, RoutedEventArgs e)
        {
            ModuleField.Text = "Sales";
            ObjectField.Text = "Sale";
            ActionField.Text = "GetTestDocument";

            var s = "";
            s = s.Append($"INVOICE_ID=1668571");
            s = s.Append($"\nDOCUMENT_FORMAT_NAME=docx");
            s = s.Append($"\nSIGNOTORY_EMPLOYEE_ID=193");
            s = s.Append($"\nDOCUMENT_TEMPLATE=ReceiptDocument2.html");
            ParamsField.Text = s;

            RequestAttempts.Text = "1";
            RequestTimeout.Text = "10000";
            RequestInterval.Text = "0";
            Repeat.Text = "0";
        }

        //Doc7Click

        private void Doc7Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text = "Sales";
            ObjectField.Text = "Sale";
            ActionField.Text = "GetPaperSpecificationDocument";

            var s = "";
            s = s.Append($"INVOICE_ID=1681920");
            s = s.Append($"\nDOCUMENT_FORMAT_NAME=pdf");
            s = s.Append($"\nSIGNOTORY_EMPLOYEE_ID=193");
            s = s.Append($"\nVERSION=1");
            ParamsField.Text = s;

            RequestAttempts.Text = "1";
            RequestTimeout.Text = "10000";
            RequestInterval.Text = "0";
            Repeat.Text = "0";
        }

        private void Doc2Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text = "Sales";
            ObjectField.Text = "Sale";
            ActionField.Text = "GetQualityCertificateDocument";

            var s = "";
            s = s.Append($"INVOICE_ID=1693378");
            s = s.Append($"\nDOCUMENT_FORMAT_NAME=pdf");
            s = s.Append($"\nSIGNOTORY_EMPLOYEE_ID=193");
            s = s.Append($"\nVERSION=1");
            ParamsField.Text = s;

            RequestAttempts.Text = "1";
            RequestTimeout.Text = "10000";
            RequestInterval.Text = "0";
            Repeat.Text = "0";
        }

        private void Doc4Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text = "Sales";
            ObjectField.Text = "Sale";
            ActionField.Text = "GetConsignmentNoteDocument";

            var s = "";
            s = s.Append($"INVOICE_ID=1693378");
            s = s.Append($"\nDOCUMENT_FORMAT_NAME=pdf");
            s = s.Append($"\nSIGNOTORY_EMPLOYEE_ID=193");
            s = s.Append($"\nVERSION=1");
            ParamsField.Text = s;

            RequestAttempts.Text = "1";
            RequestTimeout.Text = "10000";
            RequestInterval.Text = "0";
            Repeat.Text = "0";
        }

        private void Doc5Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text = "Sales";
            ObjectField.Text = "Sale";
            ActionField.Text = "GetCmrDocument";

            var s = "";
            s = s.Append($"INVOICE_ID=1686958");
            s = s.Append($"\nDOCUMENT_FORMAT_NAME=pdf");
            s = s.Append($"\nSIGNOTORY_EMPLOYEE_ID=193");
            s = s.Append($"\nVERSION=1");
            ParamsField.Text = s;

            RequestAttempts.Text = "1";
            RequestTimeout.Text = "10000";
            RequestInterval.Text = "0";
            Repeat.Text = "0";
        }

        private void Doc3Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text = "Sales";
            ObjectField.Text = "Sale";
            ActionField.Text = "GetReceiptDocument";

            var s = "";
            s = s.Append($"INVOICE_ID=1686958");
            s = s.Append($"\nDOCUMENT_FORMAT_NAME=pdf");
            s = s.Append($"\nSIGNOTORY_EMPLOYEE_ID=193");
            s = s.Append($"\nVERSION=1");
            ParamsField.Text = s;

            RequestAttempts.Text = "1";
            RequestTimeout.Text = "10000";
            RequestInterval.Text = "0";
            Repeat.Text = "0";
        }

        private void Doc6Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text = "Sales";
            ObjectField.Text = "Sale";
            ActionField.Text = "GetWaybillDocument";

            var s = "";
            s = s.Append($"INVOICE_ID=1688926");
            s = s.Append($"\nINNER_DOCUMENT_FLAG=0");
            s = s.Append($"\nDOCUMENT_FORMAT_NAME=pdf");
            s = s.Append($"\nSIGNOTORY_EMPLOYEE_ID=193");
            s = s.Append($"\nVERSION=1");
            ParamsField.Text = s;

            RequestAttempts.Text = "1";
            RequestTimeout.Text = "10000";
            RequestInterval.Text = "0";
            Repeat.Text = "0";
        }

        private void Doc1Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text = "Sales";
            ObjectField.Text = "Sale";
            ActionField.Text = "GetUniversalTransferDocument";

            var s = "";
            s = s.Append($"INVOICE_ID=1688926");
            s = s.Append($"\nINNER_DOCUMENT_FLAG=0");
            s = s.Append($"\nPAYMENT_DOCUMENT_NUMBER=");
            s = s.Append($"\nPAYMENT_DOCUMENT_DATE=");
            s = s.Append($"\nPAYMENT_DOCUMENT_NUMBER2=");
            s = s.Append($"\nPAYMENT_DOCUMENT_DATE2=");
            s = s.Append($"\nDOCUMENT_FORMAT_NAME=pdf");
            s = s.Append($"\nSIGNOTORY_EMPLOYEE_ID=193");
            s = s.Append($"\nVERSION=1");
            ParamsField.Text = s;

            RequestAttempts.Text = "1";
            RequestTimeout.Text = "10000";
            RequestInterval.Text = "0";
            Repeat.Text = "0";
        }

        

        private void ParameterListRaw_Click(object sender, RoutedEventArgs e)
        {
            ModuleField.Text = "Control";
            ObjectField.Text = "Parameter";
            ActionField.Text = "ListRaw";

            ParamsField.Text = $"";

            RequestAttempts.Text = "1";
            RequestTimeout.Text = "1000";
            RequestInterval.Text = "0";
            Repeat.Text = "0";
        }
    }


    class JsonHelper
{
    private const string INDENT_STRING = "    ";
    public static string FormatJson(string str)
    {
        var indent = 0;
        var quoted = false;
        var sb = new StringBuilder();
        for (var i = 0; i < str.Length; i++)
        {
            var ch = str[i];
            switch (ch)
            {
                case '{':
                case '[':
                    sb.Append(ch);
                    if (!quoted)
                    {
                        sb.AppendLine();
                        Enumerable.Range(0, ++indent).ForEach(item => sb.Append(INDENT_STRING));
                    }
                    break;
                case '}':
                case ']':
                    if (!quoted)
                    {
                        sb.AppendLine();
                        Enumerable.Range(0, --indent).ForEach(item => sb.Append(INDENT_STRING));
                    }
                    sb.Append(ch);
                    break;
                case '"':
                    sb.Append(ch);
                    bool escaped = false;
                    var index = i;
                    while (index > 0 && str[--index] == '\\')
                        escaped = !escaped;
                    if (!escaped)
                        quoted = !quoted;
                    break;
                case ',':
                    sb.Append(ch);
                    if (!quoted)
                    {
                        sb.AppendLine();
                        Enumerable.Range(0, indent).ForEach(item => sb.Append(INDENT_STRING));
                    }
                    break;
                case ':':
                    sb.Append(ch);
                    if (!quoted)
                        sb.Append(" ");
                    break;
                default:
                    sb.Append(ch);
                    break;
            }
        }
        return sb.ToString();
    }
}

}
