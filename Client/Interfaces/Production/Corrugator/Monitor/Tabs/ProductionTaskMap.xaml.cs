using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Client.Interfaces.Production.Monitor
{
    /// <summary>
    /// карта производственного задания
    /// отображает текущее производственное задание на ГА
    /// </summary>
    /// <author>balchugov_dv</author>   
    public partial class ProductionTaskMap:UserControl
    {
        public ProductionTaskMap()
        {
            MachineId=0;
            LoadItemsTimerInterval=5;
            ReturnTabName="";
            
            InitializeComponent();

            //после успешной загрузки интерфейса
            Loaded+=OnLoad;           

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            //значения по умолчанию
            SetDefaults();
        }

        
        public int MachineId { get;set;}

        /// <summary>
        /// предыдущий интерфейс
        /// </summary>
        public string ReturnTabName { get; set; }

        /// <summary>
        /// Деструктор. Завершает вспомогательные процессы
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="Monitor",
                ReceiverName = "",
                SenderName = "ProductionTaskMap",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останов таймера
            LoadItemsStop();

            //возврат к предыдущему интерфейсу
            GoBack();
        }

        /// <summary>
        /// Возврат на фрейм, откуда был вызван данный фрейм
        /// </summary>
        public void GoBack()
        {
            if (!string.IsNullOrEmpty(ReturnTabName))
            {
                Central.WM.SetActive(ReturnTabName, true);
                ReturnTabName = "";
            }
        }

        /// <summary>
        /// после окончания загрузки интерфейса
        /// </summary>
        private void OnLoad(object sender,RoutedEventArgs e)
        {
            //отправляем сообщение о загрузке интерфейса
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="Monitor",
                ReceiverName = "",
                SenderName = "ProductionTaskMap",
                Action = "Loaded",
            });
        }
       
        /// <summary>
        /// обработчик системы навигации по URL
        /// </summary>
        public void ProcessNavigation()
        {
            MachineId=2;

            //параметры запуска
            var p=Central.Navigator.Address.Params;

            var machineId=p.CheckGet("machine_id").ToInt();
            if(machineId!=0)
            {
                if(machineId.ContainsIn(2,21,22))
                {
                    MachineId=machineId;
                }
            }

            Init();
        }

        /// <summary>
        /// Вызов справки
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/production/monitor/current_task");
        }

        /// <summary>
        /// инициализация
        /// </summary>
        public void Init(int machineId=0)
        {
            if(machineId!=0)
            {
                MachineId=machineId;
            }
            LoadItemsRun();   
            LoadItems();

            if(LoadItemsTimerInterval!=0)
            {
                RefreshButton.ToolTip=$"Автоматическое обновление каждые {LoadItemsTimerInterval} сек.";
            }            
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            ScreenMap.Visibility=Visibility.Collapsed;
            ScreenEmpty.Visibility=Visibility.Visible;
            ScreenEmptyText.Text="Загрузка данных";
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        private void ProcessMessages(ItemMessage m)
        {
        }


        /// <summary>
        /// таймер запуска функции обновления данных занных
        /// </summary>
        private DispatcherTimer LoadItemsTimer { get; set; }
        /// <summary>
        /// интервал запуска функции обновления данных занных
        /// </summary>
        private int LoadItemsTimerInterval { get; set; }
        /// <summary>
        /// запуск таймера функции обновления данных занных
        /// </summary>
        private void LoadItemsRun()
        {
            if(LoadItemsTimerInterval != 0)
            {
                if(LoadItemsTimer == null)
                {
                    LoadItemsTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0,0,LoadItemsTimerInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", LoadItemsTimerInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("PaperMakingMachineOperator_LoadItemsRun", row);
                    }

                    LoadItemsTimer.Tick += (s,e) =>
                    {
                        LoadItems();
                    };
                }

                if(LoadItemsTimer.IsEnabled)
                {
                    LoadItemsTimer.Stop();
                }
                LoadItemsTimer.Start();
            }
        }
        
        /// <summary>
        /// останов таймера функции обновления данных занных
        /// </summary>
        private void LoadItemsStop()
        {
            if(LoadItemsTimer != null)
            {
                if(LoadItemsTimer.IsEnabled)
                {
                    LoadItemsTimer.Stop();
                }
            }
        }
    
        /// <summary>
        /// получение данных
        /// </summary>
        public async void LoadItems()
        {
            Central.Dbg("LoadItems");

            var p=new Dictionary<string,string>();
            { 
                p.CheckAdd("MACHINE_ID",MachineId.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module","Production");
            q.Request.SetParam("Object","ProductionTask");
            q.Request.SetParam("Action","GetCurrent");
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;
            
            q.Request.SetParams(p);
            
            q.Request.Timeout = 10000;
            q.Request.Attempts= 1;

            await Task.Run(() =>
            {
               q.DoQuery();
            });

            //var poster=new PosterView();
            //poster.Set(q);

            if(q.Answer.Status == 0)                
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                if(result!=null)
                {
                    var ds=ListDataSet.Create(result, "ITEMS");
                    var productionTask=ds.GetFirstItemValueByKey("ID").ToInt();
                    UpdateItems(productionTask);                    
                }
            }
            
            
        }


        /// <summary>
        /// получение карты ПЗ
        /// </summary>
        /// <param name="productionTask"></param>
        private async void UpdateItems(int productionTask)
        {
            Central.Dbg("UpdateItems");
            bool complete=false;
            bool resume=true;
            string error="";

            if(resume)
            {
                if(productionTask==0)
                {
                    resume=false;
                    error="Нет заданий на ГА";
                }
            }

            if(resume)
            {
                var p=new Dictionary<string,string>();
                { 
                    p.CheckAdd("ID",productionTask.ToString());
                    p.CheckAdd("TEMP_FILE","1");
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module","Production");
                q.Request.SetParam("Object","ProductionTask");
                q.Request.SetParam("Action","TaskGetMap");
            
                q.Request.SetParams(p);

                q.Request.Timeout = 10000;
                q.Request.Attempts= 1;

                q.Request.RequiredAnswerType=LPackClientAnswer.AnswerTypeRef.Stream;
            
                await Task.Run(() =>
                {
                   q.DoQuery();
                });

                if(q.Answer.Status == 0)                
                {
                    if(q.Answer.DataStream!=null)
                    {                        
                        try
                        {
                            BitmapImage image = new BitmapImage();
                            image.BeginInit();
                            image.StreamSource = q.Answer.DataStream;
                            image.EndInit();
                            MapImage.Source=image;

                            complete=true;
                        }
                        catch(Exception e)
                        {
                            resume=false;
                            error="Ошибка при отображении карты ПЗ";
                        }
                        
                    }
                }
                else
                {
                    //q.ProcessError();
                    resume=false;
                    error="Ошибка при получении карты ПЗ";
                }
            }
            

            if(complete)
            {
                ScreenMap.Visibility=Visibility.Visible;
                ScreenEmpty.Visibility=Visibility.Collapsed;
            }
            else
            {
                ScreenMap.Visibility=Visibility.Collapsed;
                ScreenEmpty.Visibility=Visibility.Visible;

                ScreenEmptyText.Text=error;
            }
        }

        /// <summary>
        /// отображение экрана "Нет заданий на ГА"
        /// </summary>
        private void ShowEmpty()
        {

        }

        /// <summary>
        /// блокировка контролов управления
        /// (на время загрузки данных)
        /// </summary>
        private void DisableControls()
        {
            Toolbar.IsEnabled=false;
        }

        /// <summary>
        /// разблокировка контролов управления
        /// </summary>
        private void EnableControls()
        {
            Toolbar.IsEnabled=true;
        }



        private void HelpButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void RefreshButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            LoadItems();
        }
    }


}
