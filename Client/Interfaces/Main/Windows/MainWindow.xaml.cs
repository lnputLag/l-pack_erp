using Client.Common;
using Client.Interfaces.Debug;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Orientation = System.Windows.Controls.Orientation;

namespace Client.Interfaces.Main
{
    public partial class MainWindow:Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Central.WM.MainTabsContainer = MainTabsContainer;
            Central.WM.AddTabsContainer  = AddTabsContainer;
            Central.WM.AddTab("GoHome","glyph:menu",false,"add");
            Central.WM.SetLayer("main");

            Closing +=OnClose;
            Loaded+=OnLoad;
            ContentRendered += delegate { OnContentRendered(); }; ;   
            
            PreviewKeyDown+=ProcessKeyboard;

            HideNotifications();

            NotificationItems=new Dictionary<string,NotificationItemView>();
            NotificationsRows=0;

            AnimationStateActive=false;


            //запросы к серверу
            //параметры по умолчанию ставятся в функции InitTimers()
            StatusBarInterval = 0;
            PollInterval = 0;

            //различные анимации
            AnimationInterval = 1;
            NotificationsWarningInterval =0;
            ServerLabelRefreshInterval = 0;
            
            
            OldWindowStyle=WindowStyle;      
            FullscreenMode=false;
            SdiMode=false;
            AutoRestartMode=false;
            StatusBarItems=new List<string>();

            Central.WindowWidth=(int)Width;
            Central.WindowHeight=(int)Height;
        }

        private Common.Timeout PollTimer { get; set; }
        private Common.Timeout StatusBarTimer { get; set; }

        private Dictionary<string, NotificationItemView> NotificationItems { get; set; }
        private int NotificationsRows { get; set; }

        public string WindowTitle { get; set; }
        public string TitleCustom { get; set; }

        public string ServerLabel { get; set; }
        public string ServerInfo { get; set; }

        private WindowStyle OldWindowStyle { get; set; }
        private bool FullscreenMode { get; set; }
        private bool SdiMode { get; set; }
        private bool EmbdedMode { get; set; }
        private bool AutoRestartMode { get; set; }
        public static IntPtr WindowHandle { get; set; }

        public DispatcherTimer ResizeTimer;
        public int ServerLabelRefreshInterval { get; set; }
        public DispatcherTimer ServerLabelRefreshTimer;

        /// <summary>
        /// интервал анимации
        /// </summary>
        private int AnimationInterval { get; set; }
        /// <summary>
        /// таймер анимации
        /// </summary>
        private DispatcherTimer AnimationTimer { get; set; }
        private bool AnimationStateActive { get; set; }

        /// <summary>
        /// интервал отправки телеметрии, сек.
        /// Service>Control>PollUser
        /// </summary>
        private int PollInterval { get; set; }
        /// <summary>
        /// интервал обновления статусбара, сек.
        /// Messages>Notification>GetStatus
        /// </summary>
        private int StatusBarInterval { get; set; }
        /// <summary>
        /// таймер анимации
        /// </summary>
        private DispatcherTimer StatusBarUpdateTimer { get; set; }

        /// <summary>
        /// интервал активации блока уведомлений
        /// </summary>
        private int NotificationsWarningInterval { get; set; }
        /// <summary>
        /// таймер обновления уведомлений
        /// </summary>
        private DispatcherTimer NotificationsWarningTimer { get; set; }

        /// <summary>
        /// флаг статуса технических работ
        /// </summary>
        private bool TechnicalWorkInProgress { get; set; }

        private Border TechnicalWorkIndicator { get; set; }
        
        /// <summary>
        /// таймер для мигания индикатора технических работ
        /// </summary>
        private DispatcherTimer TechnicalWorkBlinkTimer { get; set; }
        
        private bool TechnicalWorkBlinkState { get; set; }

        
        public void Init()
        {
            //Central.LPackClient.InitSystemRequest();

            //установка обработчика системных сообщений
            WindowHandle = new WindowInteropHelper(this).Handle;
            if(WindowHandle!=null)
            {
                HwndSource.FromHwnd(WindowHandle)?.AddHook(new HwndSourceHook(HandleMessages));
            }

            //обработчик сообщений шины
            Messenger.Default.Register<ItemMessage>(this,ProcessMessages);

            //генерация главного меню
            //Central.Navigator.RenderMainMenu(Menu, MenuRight);
            Central.Navigator.RenderMainMenu2(Menu, MenuRight);

            UpdateTimersInit();
            UpdateTimersReinit();            

            
            if(Central.Parameters.GlobalDebugMemoryDiag)
            {
                ServerLabelRefreshInterval=60;
                ServerLabelRunRefreshTimer();
                UpdateServerLabel();
                UpdateTitle();
            }

            //акцент внимания
            //проверяются важные уведомления, если они есть, показывается окно уведомлений
            if(Central.Parameters.NotificationsWarningInterval!=0)
            {
                NotificationsWarningInterval=Central.Parameters.NotificationsWarningInterval;
                RunNotificationsWarningTimer();                
            }

            // если в конфиге не запрещено, распахнем окно на весь экран
            if( !Central.Config.NoFullScreen )
            {
                WindowState = WindowState.Maximized;
                //RaisePropertyChanged(nameof(WindowState));
            }
            else
            {
                if( 
                    Central.Config.WindowWidth!=0
                    && Central.Config.WindowHeight!=0

                )
                {
                    Width=Central.Config.WindowWidth;
                    Height=Central.Config.WindowHeight;
                }
            }

            bool autoloadFlag=false;
            if(Central.Config.AutoloadInterfaces!=null)
            {
                if(Central.Config.AutoloadInterfaces.Count>0)
                {
                    foreach(string url in Central.Config.AutoloadInterfaces)
                    {
                        Central.Navigator.ProcessURL(url);
                        autoloadFlag=true;
                    }
                }
            }

            if(Central.Config.SingleInterfaceMode)
            {
                SetDisplayMode(1);
            }

            if(Central.Config.FullScreenMode)
            {
                SetScreenMode(1);
                SetFullscreenMode(1);
            }

            if(
                Central.Config.AutoLogin
                && autoloadFlag
            )
            {
                AutoRestartMode=true;
            }
             
            Central.AutoRestartMode=AutoRestartMode;
        }

        private void UpdateTimersInit()
        {
            //Service>Control>PollUser
            //Messages>Notification>GetStatus
            PollTimer = new Common.Timeout(
               PollInterval,
               () => {
                   ServerPoll();
                   GetNotifications();
                   DoService();
               },
               true,
               false
            );
            ServerPoll();
            GetNotifications();
            DoService();

            //Messages>Notification>GetStatus
            StatusBarTimer = new Common.Timeout(
               StatusBarInterval,
               () => {
                   UpdateStatusBar();
               },
               true,
               false
            );
            UpdateStatusBar();

        }

        private void UpdateTimersReinit()
        {
            //отправка телеметрии
            if(Central.Parameters.PollInterval != 0)
            {
                if(PollInterval != Central.Parameters.PollInterval)
                {
                    PollInterval = Central.Parameters.PollInterval;

                    if(PollInterval > 0)
                    {
                        PollTimer.SetInterval(PollInterval);
                        PollTimer.Restart();
                    }
                }
            }

            //обновление данных статус-бара
            if(Central.Parameters.StatusBarUpdateInterval != 0)
            {
                if(StatusBarInterval != Central.Parameters.StatusBarUpdateInterval)
                {
                    StatusBarInterval = Central.Parameters.StatusBarUpdateInterval;

                    if(StatusBarInterval > 0)
                    {
                        StatusBarTimer.SetInterval(StatusBarInterval);
                        StatusBarTimer.Restart();
                    }
                }
            }

        }

        public void Destroy()
        {
            Central.Dbg($"CB MainWindow: Destroy");
            
            StopTechnicalWorkBlink();
            
            Messenger.Default.Unregister<ItemMessage>(this);
            Central.Terminate();
        }

        public async void ProcessMessages(ItemMessage m)
        {
            Central.Dbg($"MSG: G:[{m.ReceiverGroup}] S:[{m.SenderName}] R:[{m.ReceiverName}] A:[{m.Action}] M:[{m.Message}]");

            {
                switch(m.Action)
                {
                    case "UpdateMenu":
                        // получение ролей пользователя
                        Central.User = _LPackClientDataProvider.DoQueryDeserialize<User>("Session", "Auth", "GetUserInfo","",null,Central.Parameters.RequestTimeoutSystem);
                        // рендер меню
                        Central.Navigator.RenderMainMenu2(Menu, MenuRight);
                        break;

                    case "PollUser":
                        ServerPoll();
                        break;

                    case "UpdateStatus":
                        UpdateStatusBar();                        
                        break;
                    
                    case "UpdateServerLabel":
                        UpdateServerLabel();
                    break;

                    case "UpdateTitle":
                        UpdateTitle();
                        break;

                    case "Resize":

                        if(!string.IsNullOrEmpty(m.Message))
                        {

                            string[] tokens = m.Message.Split('x');
                            int w = 0;
                            int h = 0;
                            if(!string.IsNullOrEmpty(tokens[0]))
                            {
                                w=tokens[0].ToInt();
                            }
                            if(!string.IsNullOrEmpty(tokens[1]))
                            {
                                h=tokens[1].ToInt();
                            }
                            if(w!=0 && h!=0)
                            {
                                Width=w;
                                Height=h;

                                Central.WindowWidth=(int)Width;
                                Central.WindowHeight=(int)Height;

                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup="All",
                                    ReceiverName = "",
                                    SenderName = "MainWindow",
                                    Action = "Resized",
                                });                               
                            }

                        }

                        break;

                     case "SetMode":
                        if(!string.IsNullOrEmpty(m.Message))
                        {                            
                            var message=m.Message;
                            message=message.ToLower();
                            switch(message)
                            {
                                case "debug":
                                    Central.SetMode(true);
                                    break;
                                
                                case "release":
                                default:
                                    Central.SetMode(false);
                                    break;
                            }

                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup="All",
                                ReceiverName = "",
                                SenderName = "MainWindow",
                                Action = "Initialized",
                            });
                        }                        
                        break;

                     case "SetDisplayMode":
                        if(!string.IsNullOrEmpty(m.Message))
                        {                            
                            var message=m.Message;
                            message=message.ToLower();
                            switch(message)
                            {
                                case "single":
                                    SetDisplayMode(1);
                                    break;
                                
                                case "multi":
                                default:
                                    SetDisplayMode(0);
                                    break;
                            }
                        }                        
                        break;

                    case "SetScreenMode":
                        if(!string.IsNullOrEmpty(m.Message))
                        {                            
                            var message=m.Message;
                            message=message.ToLower();
                            switch(message)
                            {
                                case "fullscreen":
                                    SetFullscreenMode(1);
                                    break;

                                case "nofullscreen":
                                    SetFullscreenMode(0);
                                    break;

                                case "fullscreentoggle":
                                    ToggleF11();
                                    break;

                                case "maximized":
                                    SetScreenMode(1);
                                    break;
                                
                                case "normal":
                                default:
                                    SetScreenMode(0);
                                    break;
                            }
                        }      
                        break;

                    case "Exit":
                        Exit();
                        break;

                    case "Restart":
                        Restart();
                        break;

                    case "ChangeServer":
                    case "DoHop":
                        DoHop();
                        break;

                    case "TechnicalWorkStatusOn":
                        if (!string.IsNullOrEmpty(m.Message))
                        {
                            var statusFlagTw = JsonConvert.DeserializeObject<Dictionary<string, string>>(m.Message ?? string.Empty);

                            if (statusFlagTw.Count > 0 && !statusFlagTw.CheckGet("TEXT").IsNullOrEmpty())
                            {
                                UpdateTechnicalWorkStatus(true, statusFlagTw.CheckGet("TEXT"), statusFlagTw.CheckGet("TOOLTIP"));
                            }
                            else
                            {
                                UpdateTechnicalWorkStatus(true, "ТЕХНИЧЕСКИЕ РАБОТЫ", "На сервере ведутся технические работы");
                            }
                        }
                        break;
                    case "TechnicalWorkStatusOff":
                            UpdateTechnicalWorkStatus(false, "", "");
                        break;
                        
                }
            }

            {
                if(m.ReceiverName=="Notifications")
                {
                    switch(m.Action)
                    {
                        case "Add":
                            {
                                List<Dictionary<string, string>> items = new List<Dictionary<string, string>> 
                                { 
                                    new Dictionary<string, string> 
                                    {
                                        { "CODE",m.ContextObject.ToString() },
                                        { "TYPE", "2" },
                                        { "CONTENT", m.Message },
                                    }
                                }; 
                                UpdateNotifications(items);

                            }
                            break;


                        case "Close":
                            if(!string.IsNullOrEmpty(m.Message))
                            {
                                var code="";
                                if(m.ContextObject!=null)
                                {
                                    var n=(Dictionary<string,string>)m.ContextObject;
                                    code=n["code"];
                                }
                                if(!string.IsNullOrEmpty(code))
                                {
                                    RemoveItem(code);
                                }
                                
                            }                           
                            break;

                         case "GotoLink":
                            if(!string.IsNullOrEmpty(m.Message))
                            {
                                var code="";
                                var link="";
                                if(m.ContextObject!=null)
                                {
                                    var n=(Dictionary<string,string>)m.ContextObject;
                                    code=n["code"];
                                    link=n["link"];
                                }
                                if(!string.IsNullOrEmpty(link))
                                {
                                    Central.Navigator.ProcessURL(link);
                                }
                                if(!string.IsNullOrEmpty(code))
                                {
                                    RemoveItem(code);
                                }
                            }                           
                            break;

                        case "Show":
                            ShowNotifications();
                            break;
                    }
                }
            }
        }

        private void ToggleF11()
        {
            if(!SdiMode)
            {
                SdiMode=true;                                
                SetDisplayMode(1);
            }
            else
            {
                SdiMode=false;
                SetDisplayMode(0);
            }
            

            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="All",
                ReceiverName = "",
                SenderName = "MainWindow",
                Action = "Resized",
            });
        }

        private void ProcessKeyboard(object sender,System.Windows.Input.KeyEventArgs e)
        {
            bool processed=false;

            if(!processed)
            {
                if(Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    switch(e.Key)
                    {
                        case Key.F12:
                            var i = new DebugInterface();
                            processed=true;
                            break;

                        case Key.F11:
                            ToggleF11();
                            processed=true;
                            break;
                    }
                }
            }
            

            if(!processed)
            {
                {
                    switch(e.Key)
                    {
                        case Key.F10:
                            Exit();
                            processed=true;
                            break;

                        case Key.F11:
                            if(!FullscreenMode)
                            {
                                FullscreenMode=true;
                                SetScreenMode(1);
                                SetFullscreenMode(1);
                            }
                            else
                            {
                                FullscreenMode=false;
                                SetScreenMode(0);
                                SetFullscreenMode(0);
                            }
                            processed=true;

                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup="All",
                                ReceiverName = "",
                                SenderName = "MainWindow",
                                Action = "Resized",
                            });

                            

                            break;
                    }
                }
            }

            //if(!e.Handled)
            {
                Central.WM.ProcessKeyboard(e);
            }
        }

        /// <summary>
        /// mode:0=mdi,1=sdi
        /// </summary>
        /// <param name="mode"></param>
        public void SetDisplayMode(int mode=0)
        {
            switch(mode)
            {
                //single
                case 1:
                    TopMenuBlock.Visibility=Visibility.Collapsed;

                    AddTabsBlock.Margin=new Thickness(0,0,0,0);
                    AddTabsContainer.Margin=new Thickness(0,0,0,-45);
                    AddTabsContainer.BorderThickness=new Thickness(0,0,0,0);                    

                    MainTabsBlock.Margin=new Thickness(0,0,0,0);
                    MainTabsContainer.Margin=Margin=new Thickness(0,-22,0,0);

                    EmbdedMode=true;
                    Central.EmbdedMode=EmbdedMode;
                    break;

                //multi
                case 0:
                default:
                    TopMenuBlock.Visibility=Visibility.Visible;

                    AddTabsBlock.Margin=new Thickness(0,30,0,0);
                    AddTabsContainer.Margin=new Thickness(0,0,0,0);
                    AddTabsContainer.BorderThickness=new Thickness(0,1,0,0);

                    MainTabsBlock.Margin=new Thickness(0,30,0,28);
                    MainTabsContainer.Margin=Margin=new Thickness(0,0,0,0);

                    EmbdedMode=false;
                    Central.EmbdedMode=EmbdedMode;
                    break;
            }
           
        }

        /// <summary>
        /// экранный режим
        /// </summary>
        /// <param name="mode"></param>
        public void SetScreenMode(int mode=0)
        {
            if(mode==1)
            {
                WindowState=WindowState.Maximized;
            }
            else
            {
                WindowState=WindowState.Normal;
            } 
        }

        /// <summary>
        /// полноэкранный режим
        /// </summary>
        /// <param name="mode"></param>
        public void SetFullscreenMode(int mode=0)
        {
            if(mode==1)
            {
                OldWindowStyle=WindowStyle;
                WindowStyle=WindowStyle.None;
            }
            else
            {
                WindowStyle=OldWindowStyle;
            }
        }

        /// <summary>
        /// обновление заголовка окна
        /// </summary>
        public void UpdateTitle(string txt="")
        {
            //в заголовке окна выведем имя программы 
            TitleCustom = $"{Central.ProgramTitle}";

            Version curVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            TitleCustom = $"{TitleCustom} {curVersion.ToString()}";

            var devModeLabel = "";
            if(Central.DeveloperMode)
            {
                devModeLabel = $"{Central.ServerInfo.CheckGet("Name")}";
            }

            if( Central.DebugMode )
            {
                TitleCustom = $"{TitleCustom} [Отладочный режим] {devModeLabel}";                
            }

            if( Central.ServerInfo.ContainsKey("DBTag") )
            {
                if( Central.ServerInfo["DBTag"].IndexOf("testing") > -1 )
                {
                    TitleCustom = $"{TitleCustom} [Тестовая база]";
                    Central.Parameters.BaseLabel="Тестовая база";
                }
            }

            if(!txt.IsNullOrEmpty())
            {
                TitleCustom = $"{TitleCustom} {txt}";
            }


            Application.Current.Dispatcher.Invoke(() =>
            {
                Title = TitleCustom;
            });
            //Title =TitleCustom;
        }

        public void WindowSizeChanged(int w, int h)
        {
            Central.WindowSize=$"{w}x{h}";
            
            if( Central.DebugMode )
            {
                TitleCustom = $"{w}x{h}";
                //RaisePropertyChanged(nameof(TitleCustom));

                if (ResizeTimer == null)
                {
                    ResizeTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0, 0, 5)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", "5");
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("MainWindow_WindowSizeChanged", row);
                    }

                    ResizeTimer.Tick += (s, e) =>
                    {
                        //UpdateTitle();
                        ResizeTimer.Stop();
                    };
                    
                }
                ResizeTimer.Start();
            }

            Central.Msg.SendMessage(new ItemMessage()
            {
                ReceiverGroup="",
                ReceiverName = "",
                SenderName = "MainWindow",
                Action = "Resized",
                Message=Central.WindowSize
            });
        }

        private void OnLoad(object sender,RoutedEventArgs e)
        {
            Init();
        }

        private void OnClose(object sender,System.ComponentModel.CancelEventArgs e)
        {
            var r=Exit();
            e.Cancel=r;
        }

        private void OnContentRendered()
        {
            Background = "#ffffffff".ToBrush();
        }

        public bool Exit()
        {
            bool result=false;

            //в отладочном режиме не задаем глупых вопросов
            if(Central.DebugMode)
            {
                //e.Cancel=false;
                result=false;
                Central.SaveUserParameters();
                Destroy();
            }
            else
            {
                string message = "";
                message += $"Вы действительно хотите закрыть программу?";
                message += $"\n";
                var dialog = new DialogWindow(message,Central.ProgramTitle,"",DialogWindowButtons.NoYes);
                var confirmResult = dialog.ShowDialog();

                if(confirmResult != true)
                {
                    //отмена закрытия окна
                    //e.Cancel=true;
                    result=true;
                }
                else
                {
                    result=false;
                    Central.SaveUserParameters();
                    Destroy();
                }
            }

            return result;
        }

        public void Restart()
        {
            var u = new Updater2(" ");
            u.Restart();
        }

        public void DoHop()
        {
            Central.LPackClient.ChangeServer();
        }

        public void ServerLabelRunRefreshTimer()
        {
            if(ServerLabelRefreshInterval != 0)
            {
                if(ServerLabelRefreshTimer == null)
                {
                    ServerLabelRefreshTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0,0,ServerLabelRefreshInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", ServerLabelRefreshInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("MainWindow_ServerLabelRunRefreshTimer", row);
                    }

                    ServerLabelRefreshTimer.Tick += (s,e) =>
                    {
                        UpdateServerLabel();
                        UpdateTitle();
                    };

                }
                ServerLabelRefreshTimer.Start();
            }
        }
        
        /// <summary>
        /// таймер анимации
        /// </summary>
        private void RunAnimationTimer()
        {
            if(AnimationInterval!=0)
            {
                if(AnimationTimer == null)
                {
                    AnimationTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0,0,AnimationInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", AnimationInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("MainWindow_RunAnimationTimer", row);
                    }

                    AnimationTimer.Tick += (s,e) =>
                    {
                        if(!AnimationStateActive)
                        {
                            AnimationStateActive=true;
                            Central.Navigator.UpdateNoteItemActivity(true);
                        }
                        else
                        {
                            AnimationStateActive=false;
                            Central.Navigator.UpdateNoteItemActivity(false);
                        }
                    };
                }

                if(!AnimationTimer.IsEnabled)
                {
                    AnimationTimer.Start();
                }                
            }           
        }

        public void StopAnimationTimer()
        {
            if(AnimationTimer != null)
            {
                if(AnimationTimer.IsEnabled)
                {
                    AnimationTimer.Stop();
                }
            }
        }
          
        /// <summary>
        /// таймер анимации
        /// </summary>
        private void StatusBarTimerStart()
        {
            if(StatusBarInterval != 0)
            {
                if(StatusBarUpdateTimer == null)
                {
                    StatusBarUpdateTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0,0,StatusBarInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", StatusBarInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("MainWindow_StatusBarTimerStart", row);
                    }

                    StatusBarUpdateTimer.Tick += (s,e) =>
                    {
                        UpdateStatusBar();
                        ServerPoll();
                    };
                }

                if(!StatusBarUpdateTimer.IsEnabled)
                {
                    StatusBarUpdateTimer.Start();
                }                
            }           
        }

        public void StatusBarTimerStop()
        {
            if(StatusBarUpdateTimer != null)
            {
                if(StatusBarUpdateTimer.IsEnabled)
                {
                    StatusBarUpdateTimer.Stop();
                }
            }
        }

        /// <summary>
        /// запуск таймера активации блока уведомлений
        /// проверяются важные уведомления, если они есть, показывается окно уведомлений
        /// </summary>
        private void RunNotificationsWarningTimer()
        {
            if(NotificationsWarningInterval!=0)
            {
                if(NotificationsWarningTimer == null)
                {
                    NotificationsWarningTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0,0,NotificationsWarningInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", NotificationsWarningInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("MainWindow_RunNotificationsWarningTimer", row);
                    }

                    NotificationsWarningTimer.Tick += (s,e) =>
                    {
                        CheckNotificationsWarning();
                    };
                }

                if(NotificationsWarningTimer.IsEnabled)
                {
                    NotificationsWarningTimer.Stop();
                }
                NotificationsWarningTimer.Start();
            }
           
        }

        public void StopNotificationsWarningTimer()
        {
            if(NotificationsWarningTimer != null)
            {
                if(NotificationsWarningTimer.IsEnabled)
                {
                    NotificationsWarningTimer.Stop();
                }
            }
        }

        /// <summary>
        /// Обработчик системных сообщений
        /// </summary>
        /// <returns></returns>
        private static IntPtr HandleMessages(IntPtr handle,int message,IntPtr wParameter,IntPtr lParameter,ref Boolean handled)
        {
            if(handle != MainWindow.WindowHandle)
            {
                return IntPtr.Zero;
            }

            string data = "";
            data = UnsafeNative.GetMessage(message,lParameter);

            if(data != null)
            {
                HandleParameters(data);
                handled = true;
            }

            return IntPtr.Zero;
        }

        private static void HandleParameters(string a = "")
        {
            var arguments = new Dictionary<string,string>();

            //декодируем
            //DialogWindow.ShowDialog(a);
            var args = a.Split('|');

            if(args.Length > 0)
            {
                //разбираем аргументы командной строки    
                for(int index = 1;index < args.Length;index += 2)
                {
                    arguments.Add(args[index],args[index+1]);
                }
            }

            Central.ProcessCmdArgs(arguments);
        }

        public string GetServerLabel()
        {
            var result = Central.GetServerLabel();
            return result;
        }

        /// <summary>
        /// обновление информации о пользователе
        /// </summary>
        public void UpdateServerLabel()
        {
            Central.Navigator.UpdateUserItem();
        }

        /// <summary>
        /// обновление информации для статусбара
        /// Messages>Notification>GetStatus
        /// </summary>
        private async void UpdateStatusBar()
        {
            Central.Dbg("UpdateStatusBar");
            var resume=true;

            if( string.IsNullOrEmpty(Central.LPackClient.Session.Token) )
            {
                resume = false;
            }

            if(resume)
            {
                Central.LoadServerInfo();

                var q = new LPackClientQuery();
                q.Request.SetParam("Module","Messages");
                q.Request.SetParam("Object","Notification");
                q.Request.SetParam("Action","GetStatus");

                q.Request.Timeout = Central.Parameters.RequestTimeoutSystem;
                q.Request.Attempts=1;

                await Task.Run(() =>
                {
                   q.DoQuery();
                });

                if(q.Answer.Status == 0)                
                {

                    var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                    if(result!=null)
                    {
                        StatusBarClear();
                        
                        /*
                            обновите страницу документации при изменениях
                            http://192.168.3.237/developer/erp2/server/struct/messages/status-bar                                                     
                         */

                        //если у пользователя есть одна из требуемых ролей, показываем соотв блоки
                        if (Central.User.Roles.Count > 0)
                        {
                            List<string> userRoleList = Central.User.Roles.Select(x => x.Value.Code.Trim().ToLower()).ToList();

                            if (userRoleList.Contains("[erp]lipetsk"))
                            {
                                // Производство БДМ1, БДМ2
                                // Реализация
                                if (userRoleList.Contains("[erp]sales_status_bar"))
                                {
                                    // Производство БДМ1
                                    if (result.ContainsKey("BDM_PRODUCTION") && result.ContainsKey("BDM_PRODUCTION_PREVIOUS_MONTH"))
                                    {
                                        string currentMounth = "0";
                                        string previousMounth = "0";

                                        {
                                            var ds = (ListDataSet)result["BDM_PRODUCTION"];
                                            ds?.Init();

                                            currentMounth = ds.GetFirstItemValueByKey("PARAM_VALUE");
                                        }

                                        {
                                            var ds = (ListDataSet)result["BDM_PRODUCTION_PREVIOUS_MONTH"];
                                            ds?.Init();

                                            previousMounth = ds.GetFirstItemValueByKey("PARAM_VALUE");
                                        }

                                        var v = $"{previousMounth} / {currentMounth}";

                                        {
                                            StatusBarAddItem(
                                                "BDM_PRODUCTION",
                                                "БДМ1:",
                                                "Производство БДМ1, кг.: предыдущий месяц, текущий месяц",
                                                v
                                            );
                                        }
                                    }

                                    // Производство БДМ2
                                    if (result.ContainsKey("BDM2_PRODUCTION") && result.ContainsKey("BDM2_PRODUCTION_PREVIOUS_MONTH"))
                                    {
                                        string currentMounth = "0";
                                        string previousMounth = "0";

                                        {
                                            var ds = (ListDataSet)result["BDM2_PRODUCTION"];
                                            ds?.Init();

                                            currentMounth = ds.GetFirstItemValueByKey("PARAM_VALUE");
                                        }

                                        {
                                            var ds = (ListDataSet)result["BDM2_PRODUCTION_PREVIOUS_MONTH"];
                                            ds?.Init();

                                            previousMounth = ds.GetFirstItemValueByKey("PARAM_VALUE");
                                        }

                                        var v = $"{previousMounth} / {currentMounth}";

                                        {
                                            StatusBarAddItem(
                                                "BDM2_PRODUCTION",
                                                "БДМ2:",
                                                "Производство БДМ2, кг.: предыдущий месяц, текущий месяц",
                                                v
                                            );
                                        }
                                    }

                                    // Реализация
                                    if (result.ContainsKey("SALES_CURRENT_MONTH") && result.ContainsKey("SALES_PREVIOUS_MONTH"))
                                    {
                                        string currentMounth = "0";
                                        string previousMounth = "0";

                                        {
                                            var ds = (ListDataSet)result["SALES_CURRENT_MONTH"];
                                            ds?.Init();

                                            currentMounth = ds.GetFirstItemValueByKey("PARAM_VALUE").Trim();
                                        }

                                        {
                                            var ds = (ListDataSet)result["SALES_PREVIOUS_MONTH"];
                                            ds?.Init();

                                            previousMounth = ds.GetFirstItemValueByKey("PARAM_VALUE").Trim();
                                        }

                                        var v = $"{previousMounth} / {currentMounth}";

                                        {
                                            StatusBarAddItem(
                                                "SALES",
                                                "Реализ.:",
                                                "Реализация: предыдущий месяц, текущий месяц",
                                                v
                                            );
                                        }
                                    }
                                }

                                // Стеллажный склад. ГП
                                // Ожидающие водители
                                // Алгоритм авторасстановки поддонов отключен
                                // Загруженность первого буфера: %, поддонов. Откондиционировано: %, поддонов
                                // Загруженность склада: %, поддонов
                                // Данные по загруженности склада рулонов
                                // Дежурный менеджер
                                if (userRoleList.Contains("[erp]shipment_control"))
                                {
                                    // Стеллажный склад. ГП
                                    if (result.ContainsKey("WMS_STORAGE_FULLNESS_EXTEND"))
                                    {
                                        var ds = (ListDataSet)result["WMS_STORAGE_FULLNESS_EXTEND"];
                                        ds?.Init();

                                        //
                                        // нам нужно посчитать места отдельно на алеях:
                                        // Под готовую продукцию мы используем 1,2,3 аллею:
                                        // С01,С02,С03,С04,С05,С06
                                        // Под ТМЦ (кроме поддонов) используем 4 аллею
                                        // С07, С08
                                        // Под поддоны мы используем 5 аллею 
                                        // С09, С10

                                        List<string> completedProdRows = new List<string>()
                                        {
                                            "С01","С02","С03","С04","С05","С06"
                                        };

                                        List<string> itemsRows = new List<string>()
                                        {
                                            "С07","С08"
                                        };

                                        List<string> palletsRows = new List<string>()
                                        {
                                            "С09","С10"
                                        };

                                        // выберем только необходимые проходы

                                        if (ds != null)
                                        {
                                            if (ds.Items.Count > 0)
                                            {
                                                var completedProdItem = ds.Items.Where(x => completedProdRows.Contains(x.CheckGet("ROW_NUM")));
                                                var itemsItem = ds.Items.Where(x => itemsRows.Contains(x.CheckGet("ROW_NUM")));
                                                var palletsItem = ds.Items.Where(x => palletsRows.Contains(x.CheckGet("ROW_NUM")));

                                                int free = completedProdItem.Sum(x => x.CheckGet("PROD_FREE_CNT").ToInt());
                                                int busy = (completedProdItem.Sum(x => x.CheckGet("PROD_ALL_CNT").ToInt()) - completedProdItem.Count()) - free;

                                                // сформируем строку просумировав нужные поля, отняв по одной буферной ячейке от каждого ряда
                                                string message = "ГП: " + busy + " / " + (free + busy);


                                                free = itemsItem.Sum(x => x.CheckGet("PROD_FREE_CNT").ToInt());
                                                busy = (itemsItem.Sum(x => x.CheckGet("PROD_ALL_CNT").ToInt()) - itemsItem.Count()) - free;

                                                message += " ";
                                                message += "ТМЦ: " + busy + " / " + (free + busy);


                                                {
                                                    StatusBarAddItem(
                                                        "WMS_STORAGE_FULLNESS",
                                                        "Стел. склад",
                                                        "Заполненность стеллажного склада (занято/всего)",
                                                        message
                                                    );
                                                }
                                            }
                                        }

                                    }

                                    // Ожидающие водители
                                    if (result.ContainsKey("AWAITING_DRIVERS"))
                                    {
                                        var ds = (ListDataSet)result["AWAITING_DRIVERS"];
                                        ds?.Init();

                                        var v = ds.GetFirstItemValueByKey("COUNT").ToInt().ToString();
                                        if (!string.IsNullOrEmpty(v))
                                        {
                                            StatusBarAddItem(
                                                "AWAITING_DRIVERS",
                                                "Ожид. вод.:",
                                                "Ожидающие водители",
                                                v
                                            );
                                        }
                                    }

                                    // Алгоритм авторасстановки поддонов отключен
                                    if (result.ContainsKey("ALLOW_PLACING_PALLET_FLAG"))
                                    {
                                        var ds = (ListDataSet)result["ALLOW_PLACING_PALLET_FLAG"];
                                        ds?.Init();

                                        var v = ds.GetFirstItemValueByKey("VALUE").ToInt();
                                        if (v == 1)
                                        {
                                            StatusBarAddItem(
                                                "ALLOW_PLACING_PALLET_FLAG",
                                                "Авторасст. откл.",
                                                "Алгоритм авторасстановки поддонов отключен",
                                                ""
                                            );
                                        }
                                    }

                                    // Загруженность первого буфера: %, поддонов. Откондиционировано: %, поддонов
                                    if (result.ContainsKey("STOCK_BUFFER_1_STATE_PCT")
                                        && result.ContainsKey("STOCK_BUFFER_1_STATE_CNT")
                                        && result.ContainsKey("STOCK_BUFFER_1_DRY_SHEET_BUFFER_PCT")
                                        && result.ContainsKey("STOCK_BUFFER_1_DRY_SHEET_BUFFER_CNT")
                                        )
                                    {
                                        int procentage = 0;
                                        int quantity = 0;
                                        int procentageDry = 0;
                                        int quantityDry = 0;
                                        {
                                            var ds = (ListDataSet)result["STOCK_BUFFER_1_STATE_PCT"];
                                            ds?.Init();
                                            procentage = ds.GetFirstItemValueByKey("PARAM_VALUE").ToInt();
                                        }
                                        {
                                            var ds = (ListDataSet)result["STOCK_BUFFER_1_STATE_CNT"];
                                            ds?.Init();
                                            quantity = ds.GetFirstItemValueByKey("PARAM_VALUE").ToInt();
                                        }
                                        {
                                            var ds = (ListDataSet)result["STOCK_BUFFER_1_DRY_SHEET_BUFFER_PCT"];
                                            ds?.Init();
                                            procentageDry = ds.GetFirstItemValueByKey("PARAM_VALUE").ToInt();
                                        }
                                        {
                                            var ds = (ListDataSet)result["STOCK_BUFFER_1_DRY_SHEET_BUFFER_CNT"];
                                            ds?.Init();
                                            quantityDry = ds.GetFirstItemValueByKey("PARAM_VALUE").ToInt();
                                        }
                                        var v = $"{procentage}% / {quantity} ({procentageDry}% / {quantityDry})";

                                        {
                                            StatusBarAddItem(
                                                "STOCK_BUFFER_1_STATE",
                                                "Буф. 1:",
                                                "Загруженность первого буфера: %, поддонов. Откондиционировано: %, поддонов",
                                                v
                                            );
                                        }
                                    }

                                    // Загруженность склада: %, поддонов
                                    if (result.ContainsKey("STOCK_STATE_PCT") && result.ContainsKey("STOCK_STATE_CNT"))
                                    {
                                        int procentage = 0;
                                        int quantity = 0;
                                        {
                                            var ds = (ListDataSet)result["STOCK_STATE_PCT"];
                                            ds?.Init();

                                            procentage = ds.GetFirstItemValueByKey("PARAM_VALUE").ToInt();
                                        }
                                        {
                                            var ds = (ListDataSet)result["STOCK_STATE_CNT"];
                                            ds?.Init();

                                            quantity = ds.GetFirstItemValueByKey("PARAM_VALUE").ToInt();
                                        }
                                        var v = $"{procentage}% / {quantity}";

                                        {
                                            StatusBarAddItem(
                                                "STOCK_STATE",
                                                "СГП:",
                                                "Загруженность склада: %, поддонов",
                                                v
                                            );
                                        }
                                    }

                                    // Данные по загруженности склада рулонов
                                    if (result.ContainsKey("ROLL_STOCK_QTY"))
                                    {
                                        // Максимальная вместимость склада рулонов
                                        int procentage = 0;
                                        var ds = ListDataSet.Create(result, "ROLL_STOCK_QTY");
                                        int quantity = ds.GetFirstItemValueByKey("ROLL_QTY").ToInt();
                                        int rollStockCapasity = ds.GetFirstItemValueByKey("ROLL_STOCK_SIZE").ToInt();
                                        double rollWeightNet = ds.GetFirstItemValueByKey("WEIGHT_NET").ToDouble();

                                        if (quantity > 0 && rollStockCapasity > 0)
                                        {
                                            procentage = (int)(quantity * 100 / rollStockCapasity);
                                        }
                                        var v = $"{procentage}% / {quantity} / {rollWeightNet}т.";
                                        StatusBarAddItem(
                                            "ROLL_STOCK_STATE",
                                            "Рул.:",
                                            "Загруженность склада рулонов: %, кол-во рулонов, суммарный вес Нетто",
                                            v
                                        );
                                    }

                                    // Дежурный менеджер
                                    if (result.ContainsKey("MANAGER_ON_DUTY"))
                                    {
                                        var ds = (ListDataSet)result["MANAGER_ON_DUTY"];
                                        ds?.Init();

                                        var v = ds.GetFirstItemValueByKey("NAME").ToString();
                                        if (!string.IsNullOrEmpty(v))
                                        {
                                            if (DateTime.Now.Hour >= 8 && DateTime.Now.Hour < 20)
                                            {
                                                var p = ds.GetFirstItemValueByKey("PHONE").ToString();
                                                if (!string.IsNullOrEmpty(p))
                                                {
                                                    v = $"{v} {p}";
                                                }

                                                StatusBarAddItem(
                                                    "MANAGER_ON_DUTY",
                                                    "Деж. менеджер:",
                                                    "",
                                                    v
                                                );
                                            }
                                        }
                                    }
                                }

                                // Дежурный сисадмин
                                if (result.ContainsKey("SA_ON_DUTY"))
                                {
                                    var ds = (ListDataSet)result["SA_ON_DUTY"];
                                    ds?.Init();

                                    var v = ds.GetFirstItemValueByKey("NAME").ToString();
                                    if (!string.IsNullOrEmpty(v))
                                    {
                                        StatusBarAddItem(
                                            "SA_ON_DUTY",
                                            "Деж. СА.:",
                                            "Дежурный системный администратор",
                                            v
                                        );
                                    }
                                }
                            }

                            if (userRoleList.Contains("[erp]kashira"))
                            {
                                // Ожидающие водители Кашира
                                // Загруженность буфера Кашира: %, поддонов. Откондиционировано: %, поддонов
                                // Загруженность склада Кашира: %, поддонов/вместимость в паллетоместах
                                // Данные по загруженности склада рулонов Кашира
                                if (userRoleList.Contains("[erp]shipment_control_ksh"))
                                {
                                    // Ожидающие водители Кашира
                                    if (result.ContainsKey("AWAITING_DRIVERS_KSH"))
                                    {
                                        var ds = (ListDataSet)result["AWAITING_DRIVERS_KSH"];
                                        ds?.Init();

                                        var v = ds.GetFirstItemValueByKey("COUNT").ToInt().ToString();
                                        if (!string.IsNullOrEmpty(v))
                                        {
                                            StatusBarAddItem(
                                                "AWAITING_DRIVERS_KSH",
                                                "Ожид. вод. КШ:",
                                                "Ожидающие водители Кашира",
                                                v
                                            );
                                        }
                                    }

                                    // Загруженность буфера Кашира: %, поддонов. Откондиционировано: %, поддонов
                                    if (result.ContainsKey("STOCK_BUFFER_STATE_KSH_PCT")
                                        && result.ContainsKey("STOCK_BUFFER_STATE_KSH_CNT")
                                        && result.ContainsKey("STOCK_BUFFER_KSH_DRY_SHEET_BUFFER_PCT")
                                        && result.ContainsKey("STOCK_BUFFER_1_DRY_SHEET_BUFFER_CNT")
                                        )
                                    {
                                        int procentage = 0;
                                        int quantity = 0;
                                        int procentageDry = 0;
                                        int quantityDry = 0;
                                        {
                                            var ds = (ListDataSet)result["STOCK_BUFFER_STATE_KSH_PCT"];
                                            ds?.Init();
                                            procentage = ds.GetFirstItemValueByKey("PARAM_VALUE").ToInt();
                                        }
                                        {
                                            var ds = (ListDataSet)result["STOCK_BUFFER_STATE_KSH_CNT"];
                                            ds?.Init();
                                            quantity = ds.GetFirstItemValueByKey("PARAM_VALUE").ToInt();
                                        }
                                        {
                                            var ds = (ListDataSet)result["STOCK_BUFFER_KSH_DRY_SHEET_BUFFER_PCT"];
                                            ds?.Init();
                                            procentageDry = ds.GetFirstItemValueByKey("PARAM_VALUE").ToInt();
                                        }
                                        {
                                            var ds = (ListDataSet)result["STOCK_BUFFER_KSH_DRY_SHEET_BUFFER_CNT"];
                                            ds?.Init();
                                            quantityDry = ds.GetFirstItemValueByKey("PARAM_VALUE").ToInt();
                                        }
                                        var v = $"{procentage}% / {quantity} ({procentageDry}% / {quantityDry})";

                                        {
                                            StatusBarAddItem(
                                                "STOCK_BUFFER_STATE_KSH",
                                                "Буф. КШ:",
                                                "Загруженность буфера Кашира: %, поддонов. Откондиционировано: %, поддонов",
                                                v
                                            );
                                        }
                                    }

                                    // Загруженность склада Кашира: %, поддонов/вместимость в паллетоместах
                                    if (result.ContainsKey("STOCK_STATE_KSH_PCT") 
                                        && result.ContainsKey("STOCK_STATE_KSH_CNT")
                                        && result.ContainsKey("STOCK_STATE_KSH_SIZE_CNT"))
                                    {
                                        int procentage = 0;
                                        int quantity = 0;
                                        int sizeQuantity = 0;
                                        {
                                            var ds = (ListDataSet)result["STOCK_STATE_KSH_PCT"];
                                            ds?.Init();

                                            procentage = ds.GetFirstItemValueByKey("PARAM_VALUE").ToInt();
                                        }
                                        {
                                            var ds = (ListDataSet)result["STOCK_STATE_KSH_CNT"];
                                            ds?.Init();

                                            quantity = ds.GetFirstItemValueByKey("PARAM_VALUE").ToInt();
                                        }
                                        {
                                            var ds = (ListDataSet)result["STOCK_STATE_KSH_SIZE_CNT"];
                                            ds?.Init();

                                            sizeQuantity = ds.GetFirstItemValueByKey("PARAM_VALUE").ToInt();
                                        }
                                        var v = $"{procentage}% {quantity}/{sizeQuantity}";

                                        {
                                            StatusBarAddItem(
                                                "STOCK_STATE_KSH",
                                                "СГП КШ:",
                                                "Загруженность склада Кашира: %, поддонов/вместимость в паллетоместах",
                                                v
                                            );
                                        }
                                    }

                                    // Данные по загруженности склада рулонов Кашира
                                    if (result.ContainsKey("ROLL_STOCK_QTY_KSH"))
                                    {
                                        // Максимальная вместимость склада рулонов
                                        int procentage = 0;
                                        var ds = ListDataSet.Create(result, "ROLL_STOCK_QTY_KSH");
                                        int quantity = ds.GetFirstItemValueByKey("ROLL_QTY").ToInt();
                                        int rollStockCapasity = ds.GetFirstItemValueByKey("ROLL_STOCK_SIZE").ToInt();
                                        double rollWeightNet = ds.GetFirstItemValueByKey("WEIGHT_NET").ToDouble();

                                        if (quantity > 0 && rollStockCapasity > 0)
                                        {
                                            procentage = (int)(quantity * 100 / rollStockCapasity);
                                        }
                                        var v = $"{procentage}% / {quantity} / {rollWeightNet}т.";
                                        StatusBarAddItem(
                                            "ROLL_STOCK_STATE_KSH",
                                            "Рул. КШ:",
                                            "Загруженность склада рулонов Кашира: %, кол-во рулонов, суммарный вес Нетто",
                                            v
                                        );
                                    }
                                }

                                // Дежурный сисадмин Кашира
                                if (result.ContainsKey("SA_ON_DUTY_KSH"))
                                {
                                    var ds = (ListDataSet)result["SA_ON_DUTY_KSH"];
                                    ds?.Init();

                                    var v = ds.GetFirstItemValueByKey("NAME").ToString();
                                    if (!string.IsNullOrEmpty(v))
                                    {
                                        StatusBarAddItem(
                                            "SA_ON_DUTY_KSH",
                                            "Деж. СА. КШ.:",
                                            "Дежурный системный администратор Кашира",
                                            v
                                        );
                                    }
                                }
                            }
                        }

                        // Общие данные статусбара. Показываем всем.
                        // Тестовая база
                        // Авторестарт
                        // Учетная запись
                        {
                            // Тестовая база
                            {
                                if (Central.ServerInfo.ContainsKey("DBTag"))
                                {
                                    if (Central.ServerInfo["DBTag"].IndexOf("testing") > -1)
                                    {
                                        StatusBarAddItem(
                                            "DB_TESTING",
                                            "Тестовая база",
                                            "Тестовая база",
                                            "",
                                            80,
                                            1
                                        );
                                    }
                                }
                            }

                            // Авторестарт
                            if (AutoRestartMode)
                            {
                                StatusBarAddItem(
                                    "AR",
                                    "AR",
                                    "Авторестарт",
                                    "",
                                    80,
                                    0
                                );
                            }

                            // Учетная запись
                            if (EmbdedMode)
                            {
                                {
                                    var login = GetServerLabel();
                                    StatusBarAddItem(
                                        $"{login}",
                                        $"{login}",
                                        "Учетная запись",
                                        "",
                                        80,
                                        0
                                    );
                                }

                                {
                                    var time = DateTime.Now.ToString("HH:mm");
                                    StatusBarAddItem(
                                        $"{time}",
                                        $"{time}",
                                        "",
                                        "",
                                        80,
                                        0
                                    );
                                }

                            }
                        }
                    }
                }
            }
        }

        private int ServerPollCounter {get;set;}=0;

        /// <summary>
        /// отправка телеметрии на сервер
        /// получение уведомлений (новый механизм)
        /// Service>Control>PollUser
        /// </summary>
        private async void ServerPoll()
        {
            ServerPollCounter++;
            Central.Dbg($"ServerPoll ({ServerPollCounter})");
            var resume=true;

            if(resume)
            {
                var p=new Dictionary<string,string>();
                {
                    p.CheckAdd("TODAY", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
                    p.CheckAdd("UPTIME", Central.GetUptime().ToString());
                    p.CheckAdd("VERSION", Central.Version.ToString());
                    p.CheckAdd("COUNTER", ServerPollCounter.ToString());

                    {
                        var systemInfo=Central.GetSystemInfo();
                        p.AddRange(systemInfo);

                        if(Central.User != null)
                        {
                            Central.User.HostUserId = systemInfo.CheckGet("HOST_USER_ID");
                        }

                        var memoryUsed=Central.GetUsedMemory();
                        p.CheckAdd("SYSTEM_MEMORY_USED",memoryUsed.ToString());

                        var used = Central.GetResourcesUsed();
                        p.AddRange(used);
                    }

                    {
                        if(Central.User != null)
                        {
                            p.CheckAdd("CLIENT_ACCOUNT_ID", Central.User.AccountId.ToString());
                            p.CheckAdd("CLIENT_EMPLOYEE_ID", Central.User.EmployeeId.ToString());
                            p.CheckAdd("CLIENT_LOGIN", Central.User.Login.ToString());
                        }
                    }

                    if(Central.LPackClient != null)
                    {
                        var s = "";
                        foreach(LPackClientConnection c in Central.LPackClient.Connections)
                        {
                            s = s.AddComma();
                            s = s.Append(c.Host);
                        }
                        p.CheckAdd("CLIENT_SERVER_ADDRESS", s.ToString());
                    }

                    {
                        p.CheckAdd("CLIENT_MODE_EMBDED", Central.EmbdedMode.ToString().ToInt().ToString());
                        p.CheckAdd("CLIENT_MODE_AUTORESTART", Central.AutoRestartMode.ToString().ToInt().ToString());
                        p.CheckAdd("CLIENT_MODE_AUTOLOGIN", Central.Config.AutoLogin.ToString().ToInt().ToString());
                    }

                    {
                        var a = Central.WM.GetTabItemsList();
                        p.CheckAdd("TABS", JsonConvert.SerializeObject(a));

                        var b = Central.Stat.RequestStat;
                        p.CheckAdd("REQUESTS", JsonConvert.SerializeObject(b));

                        var c = Central.Stat.TimerStat;
                        p.CheckAdd("TIMERS", JsonConvert.SerializeObject(c));
                    }
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module","Service");
                q.Request.SetParam("Object","Client");
                q.Request.SetParam("Action","PollClient");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestTimeoutDefault;
                q.Request.Attempts = 1;

                await Task.Run(() =>
                {
                   q.DoQuery();
                });

                if(q.Answer.Status == 0)                
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                    if(result!=null)
                    {   
                        {
                            var ds=ListDataSet.Create(result,"ITEMS");
                            if(ds.Items.Count>0)
                            {
                                var first=ds.GetFirstItem();
                            }
                        }

                        {
                            var ds=ListDataSet.Create(result,"MESSAGE");
                            if(ds.Items.Count>0)
                            {
                                ProcessMessages2(ds.Items);
                            }
                        }

                        {
                            var ds = ListDataSet.Create(result, "TIMINGS");
                            if(ds.Items.Count > 0)
                            {
                                ProcessParameters(ds.GetFirstItem());
                            }
                        }
                    }
                }
            }
        }

        public void ProcessMessages2(List<Dictionary<string,string>> messages)
        {
            if(messages.Count>0)
            {
                var notifications=new List<Dictionary<string,string>>();

                foreach(Dictionary<string,string> m in messages)
                {
                    /*
                        HOSTNAME=BALCHUGOV-DV
                        COMMAND=test
                        MESSAGE=asdf
                        PARAMETERS=asdf
                     */

                    var command=m.CheckGet("COMMAND");
                    if(!command.IsNullOrEmpty())
                    {
                        command=command.Trim();
                        command=command.ToUpper();
                        switch(command)
                        {
                            case "_TEST_STRING":
                                {
                                    var testString=m.CheckGet("MESSAGE");
                                    UpdateTitle(testString);
                                }
                                break;

                            case "SYSTEM_RESTART":
                                {
                                    Restart();
                                }
                                break;

                            case "SYSTEM_SHUTDOWN":
                                {
                                    System.Windows.Application.Current.Shutdown();
                                }
                                break;

                            case "DO_HOP":
                                {
                                    DoHop();
                                }
                                break;

                            case "GC":
                                {
                                    GC.Collect();
                                }
                                break;

                            case "UPDATE_TITLE":
                                {
                                    UpdateTitle();
                                }
                                break;

                            case "SYSTEM_UPDATE_CONFIG_SERVER":
                                var serverList = m.CheckGet("MESSAGE");
                                Central.ConfigUpdate(serverList);
                                break;

                            case "SHOW_NOTIFICATION":
                                {
                                    var notification=new Dictionary<string,string>();
                                    notification.CheckAdd("CODE",m.CheckGet("CODE"));
                                    notification.CheckAdd("CLASS",m.CheckGet("CLASS"));
                                    notification.CheckAdd("TYPE",m.CheckGet("TYPE"));
                                    notification.CheckAdd("TITLE",m.CheckGet("TITLE"));
                                    notification.CheckAdd("CONTENT",m.CheckGet("CONTENT"));
                                    notification.CheckAdd("LINK",m.CheckGet("LINK"));
                                    notifications.Add(notification);
                                }
                                break;
                        }
                    }
                }

                if(notifications.Count > 0)
                {
                    UpdateNotifications(notifications);
                }
            }
        }

        public void ProcessParameters(Dictionary<string, string> row)
        {
            var reinit = false;

            if(row.Count > 0)
            {
                var s = "";
                s = s.Append($"server parameters", true);
                foreach(KeyValuePair<string, string> item in row)
                {
                    var k = item.Key;
                    var v = item.Value;
                    s = s.Append($"{k}=[{v}]",true);

                    k = k.ToUpper();
                    k = k.Trim();
                    switch(k)
                    {
                        case "POLL_INTERVAL":
                            {
                                var i = v.ToInt();
                                if(i > 0)
                                {
                                    Central.Parameters.PollInterval = i;
                                    reinit = true;
                                }
                            }
                            break;

                        case "STATUS_BAR_UPDATE_INTERVAL":
                            {
                                var i = v.ToInt();
                                if(i > 0)
                                {
                                    Central.Parameters.StatusBarUpdateInterval = i;
                                    reinit = true;
                                }
                            }
                            break;


                        case "REQUEST_MINIMUM_TIMEOUT":
                            {
                                var i = v.ToInt();
                                if(i > 0)
                                {
                                    Central.Parameters.RequestTimeoutMin = i;
                                    reinit = true;
                                }
                            }
                            break;

                        case "REQUEST_GRID_TIMEOUT":
                            {
                                var i = v.ToInt();
                                if(i > 0)
                                {
                                    Central.Parameters.RequestGridTimeout = i;
                                    reinit = true;
                                }
                            }
                            break;

                        case "REQUEST_GRID_CUTTINGTIMEOUT":
                            {
                                var i = v.ToInt();
                                if(i > 0)
                                {
                                    Central.Parameters.RequestTimeoutMax = i;
                                    reinit = true;
                                }
                            }
                            break;

                        case "REQUEST_GRID_ATTEMPTS":
                            {
                                var i = v.ToInt();
                                if(i > 0)
                                {
                                    Central.Parameters.RequestAttemptsDefault = i;
                                    reinit = true;
                                }
                            }
                            break;


                        case "REQUEST_DEAULT_TIMEOUT":
                            {
                                var i = v.ToInt();
                                if(i > 0)
                                {
                                    Central.Parameters.RequestTimeoutDefault = i;
                                    reinit = true;
                                }
                            }
                            break;

                        case "REQUEST_SYSTEM_TIMEOUT":
                            {
                                var i = v.ToInt();
                                if(i > 0)
                                {
                                    Central.Parameters.RequestTimeoutSystem = i;
                                    reinit = true;
                                }
                            }
                            break;

                        case "CLIENT_HOP_MODE":
                            {
                                var i = v.ToInt();
                                //if(i > 0)
                                {
                                    Central.Parameters.HopMode = i;
                                    reinit = true;
                                }
                            }
                            break;

                        case "CLIENT_HOP_CONTROL_INTERVAL_SLOW":
                            {
                                var i = v.ToInt();
                                if(i > 0)
                                {
                                    Central.Parameters.HopControlIntervalSlow = i;
                                    reinit = true;
                                }
                            }
                            break;

                        case "CLIENT_HOP_CONTROL_INTERVAL_FAST":
                            {
                                var i = v.ToInt();
                                if(i > 0)
                                {
                                    Central.Parameters.HopControlIntervalFast = i;
                                    reinit = true;
                                }
                            }
                            break;

                        case "CLIENT_HOP_WAIT_TIMEOUT":
                            {
                                var i = v.ToInt();
                                if(i > 0)
                                {
                                    Central.Parameters._HopWaitTimeout = i;
                                    reinit = true;
                                }
                            }
                            break;

                        case "CLIENT_REQUEST_ATTEMPTS_FIX_MODE":
                            {
                                var i = v.ToInt();
                                //if(i > 0)
                                {
                                    Central.Parameters.RequestAttemptsFixMode = i;
                                    reinit = true;
                                }
                            }
                            break;

                        case "CLIENT_DO_SYSTEM_REQUEST_FAULT_LIMIT":
                            {
                                var i = v.ToInt();
                                if(i > 0)
                                {
                                    Central.Parameters.DoSystemRequestFaultLimit = i;
                                    reinit = true;
                                }
                            }
                            break;

                        case "CLIENT_QUERY_REPEAT_LIMIT_TIME":
                            {
                                var i = v.ToInt();
                                if(i > 0)
                                {
                                    Central.Parameters.QueryRepeatLimitTime = i;
                                    reinit = true;
                                }
                            }
                            break;

                        case "CLIENT_QUERY_REPEAT_DELAY":
                            {
                                var i = v.ToInt();
                                if(i > 0)
                                {
                                    Central.Parameters.QueryRepeatDelay = i;
                                    reinit = true;
                                }
                            }
                            break;

                        case "CLIENT_WAIT_REPEAT_LIMIT_TIME":
                            {
                                var i = v.ToInt();
                                if(i > 0)
                                {
                                    Central.Parameters.WaitRepeatLimitTime = i;
                                    reinit = true;
                                }
                            }
                            break;

                        case "CLIENT_WAIT_REPEAT_DELAY":
                            {
                                var i = v.ToInt();
                                if(i > 0)
                                {
                                    Central.Parameters.WaitRepeatDelay = i;
                                    reinit = true;
                                }
                            }
                            break;
                    }
                }

                //Central.LPackClient.InitSystemRequest();
                Central.Logger.Trace(s);
            }

            if(reinit)
            {
                UpdateTimersReinit();
            }

        }

        private void StatusBarClear()
        {
            StatusBarContainer.Children.Clear();
            StatusBarItems.Clear();
        }

        private List<string> StatusBarItems {get;set;}
        private void StatusBarAddItem(string key, string caption, string tooltip="", string value="", int width=80, int style=0)
        {
            if(!StatusBarItems.Contains(key))
            {
                StatusBarItems.Add(key);

                StatusBarContainer.Style=(Style)StatusBarContainer.TryFindResource("StatusBarContainer");

                var g=new Grid();
                { 
                    var rd=new RowDefinition();
                    rd.Height=GridLength.Auto;
                    g.RowDefinitions.Add(rd);

                    {
                        var cd=new ColumnDefinition();
                        cd.Width=GridLength.Auto;
                        g.ColumnDefinitions.Add(cd);
                    }

                    {
                        var cd=new ColumnDefinition();
                        cd.Width=GridLength.Auto;
                        g.ColumnDefinitions.Add(cd);
                    }
               
                    g.Style=(Style)StatusBarContainer.TryFindResource("StatusBarBlockGrid");               
                }

                {
                    var t=new TextBlock();
                    t.Text=caption;

                    t.Style=(Style)StatusBarContainer.TryFindResource("StatusBarTextBlock");
                    if(style==1)
                    {
                        t.Style=(Style)StatusBarContainer.TryFindResource("StatusBarTextBlockAlert");
                    }
                
                    var b=new Border();
                    b.Style=(Style)StatusBarContainer.TryFindResource("StatusBarBlockCaption");
                    b.Child=t;
                    g.Children.Add(b);
                    Grid.SetRow(b,0);
                    Grid.SetColumn(b,0);
                }
            
                {
                    var t=new TextBlock();
                    t.Text=value;     
                
                    var b=new Border();
                    b.Child=t;
                    b.Style=(Style)StatusBarContainer.TryFindResource("StatusBarBlockValue");
                    g.Children.Add(b);
                    Grid.SetRow(b,0);
                    Grid.SetColumn(b,1);
                }

                {
                    var b=new Border();
                    b.Style=(Style)StatusBarContainer.TryFindResource("StatusBarBlock");

                    if(style==1)
                    {
                        b.Style=(Style)StatusBarContainer.TryFindResource("StatusBarBlockAlert");
                    }

                    b.Child=g;

                    if(!string.IsNullOrEmpty(tooltip))
                    {
                        b.ToolTip=tooltip;
                    }

                    StatusBarContainer.Children.Add(b);
                }

            }
            
        }

        private void UpdateTechnicalWorkStatus(bool isActive, string text,
            string tooltip)
        {
            Dispatcher.Invoke(() =>
            {
                TechnicalWorkInProgress = isActive;

                if (isActive)
                {
                    if (TechnicalWorkIndicator == null)
                    {
                        var g = new Grid();
                        {
                            var rd = new RowDefinition();
                            rd.Height = GridLength.Auto;
                            g.RowDefinitions.Add(rd);

                            var cd = new ColumnDefinition();
                            cd.Width = GridLength.Auto;
                            g.ColumnDefinitions.Add(cd);

                            g.Style = (Style)StatusBarContainer.TryFindResource("StatusBarBlockGrid");
                        }

                        {
                            var t = new TextBlock();
                            t.Text = $"⚠ {text}";
                            t.FontWeight = FontWeights.Bold;
                            t.Style = (Style)StatusBarContainer.TryFindResource("StatusBarTextBlockAlert");

                            var b = new Border();
                            b.Style = (Style)StatusBarContainer.TryFindResource("StatusBarBlockCaption");
                            b.Child = t;
                            g.Children.Add(b);
                            Grid.SetRow(b, 0);
                            Grid.SetColumn(b, 0);
                        }

                        TechnicalWorkIndicator = new Border();
                        TechnicalWorkIndicator.Style = (Style)StatusBarContainer.TryFindResource("StatusBarBlockAlert");
                        TechnicalWorkIndicator.Child = g;
                        TechnicalWorkIndicator.ToolTip = tooltip;
                    }

                    if (!StatusBarContainer.Children.Contains(TechnicalWorkIndicator))
                    {
                        StatusBarContainer.Children.Insert(StatusBarContainer.Children.Count, TechnicalWorkIndicator);
                    }

                    StartTechnicalWorkBlink();
                }
                else
                {
                    StopTechnicalWorkBlink();

                    if (TechnicalWorkIndicator != null && StatusBarContainer.Children.Contains(TechnicalWorkIndicator))
                    {
                        StatusBarContainer.Children.Remove(TechnicalWorkIndicator);
                        TechnicalWorkIndicator = null;
                    }
                }
            });
        }

        private void StartTechnicalWorkBlink()
        {
            if (TechnicalWorkBlinkTimer == null)
            {
                TechnicalWorkBlinkTimer = new DispatcherTimer();
                TechnicalWorkBlinkTimer.Interval = TimeSpan.FromMilliseconds(500);
                TechnicalWorkBlinkTimer.Tick += TechnicalWorkBlinkTimer_Tick;
            }

            if (!TechnicalWorkBlinkTimer.IsEnabled)
            {
                TechnicalWorkBlinkState = true;
                TechnicalWorkBlinkTimer.Start();
            }
        }

        private void StopTechnicalWorkBlink()
        {
            if (TechnicalWorkBlinkTimer != null && TechnicalWorkBlinkTimer.IsEnabled)
            {
                TechnicalWorkBlinkTimer.Stop();
            }

            if (TechnicalWorkIndicator != null)
            {
                TechnicalWorkIndicator.Opacity = 1.0;
            }
        }

        private void TechnicalWorkBlinkTimer_Tick(object sender, EventArgs e)
        {
            if (TechnicalWorkIndicator != null)
            {
                TechnicalWorkBlinkState = !TechnicalWorkBlinkState;
                TechnicalWorkIndicator.Opacity = TechnicalWorkBlinkState ? 1.0 : 0.3;
            }
        }

        [Obsolete]
        public async void GetNotifications()
        {
            bool resume = true;


            if( string.IsNullOrEmpty(Central.LPackClient.Session.Token) )
            {
                resume = false;
            }

            if(resume)
            {
                var s=Central.LPackClient.Session.Token;

                var q = new LPackClientQuery();
                q.Request.SetParam("Module","Messages");
                q.Request.SetParam("Object","Notification");
                q.Request.SetParam("Action","List");

                q.Request.Timeout = Central.Parameters.RequestTimeoutMin;
                q.Request.Attempts=1;

                await Task.Run(() =>
                {
                   q.DoQuery();
                });

                if(q.Answer.Status == 0)                
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                    if(result!=null)
                    {
                        if(result.ContainsKey("Items"))
                        {
                            var messagesDS=(ListDataSet)result["Items"];
                            messagesDS?.Init();  

                            if(messagesDS.Items.Count>0)
                            {
                                //NotificationItems=new Dictionary<string,NotificationItemView>();
                                

                                var items=new List<Dictionary<string,string>>();

                                //пересортируем: сначала важные
                                foreach(Dictionary<string,string> m in messagesDS.Items)
                                {
                                    if(m.ContainsKey("TYPE"))
                                    {
                                        if(m["TYPE"].ToInt()==1)
                                        {
                                            items.Add(m);
                                        }
                                    }
                                }
                                foreach(Dictionary<string,string> m in messagesDS.Items)
                                {
                                    if(m.ContainsKey("TYPE"))
                                    {
                                        if(m["TYPE"].ToInt()!=1)
                                        {
                                            items.Add(m);
                                        }
                                    }
                                }

                                UpdateNotifications(items);
                            } 

                            UpdateNotificationsLabel();
                            CheckNotificationsWarning();
                            UpdateServerLabel();

                        }
                    }
                }

            }

        }

        public void DoService()
        {
            Central.DoService();
        }

        public void UpdateNotifications(List<Dictionary<string,string>> items)
        {
             /*
                Сообщения могут быть 2 типов:
                TYPE:
                    1 -- (важное) призыв к действию, нельзя скрыть
                    2 -- (обычное) информационное сообщение, можно скрыть 
                        
                Получение сообщений производится таймером 
                    NotificationsUpdateInterval (20 сек)
                    Новые сообщения добавляются в блок сообщений.
                    Если есть сообщения, начинает мигать кнопка.
                            
                Проверка активности блока производится таймером:
                    NotificationsWarningInterval (60 сек)
                    Если в блоке есть важные, блок появится.


                С сервера приходят сообщения.
                Сообщения идентифицируются уникальным кодом (CODE).
                Нове сообщения добавляются в блок.
                Если сообщение уже есть в блоке, оно не добавляется.

                UPD:2021-07-15
                Если сообщение уже есть в блоке с таким же классом,
                оно будет заменено.

                Пользователь может скрыть сообщения.

                */

            int rowIndex=NotificationsRows;

            foreach(Dictionary<string,string> m in items)
            {
                //Central.Dbg($"NOTE: {m["CREATED"]} {m["TITLE"]} ");

                var code="";
                if(m.ContainsKey("CODE"))
                {
                    code=m["CODE"];
                }

                var noteClass="";
                if(m.ContainsKey("CLASS"))
                {
                    noteClass=m["CLASS"];
                }

                if(!string.IsNullOrEmpty(code))
                {
                    var n = new NotificationItemView();
                    n.ToolTip="";

                    if(m.ContainsKey("TITLE"))
                    {
                        n.Title.Text=$"{m["TITLE"]}";
                        n.TitleButton.Content=$"{m["TITLE"]}";
                    }

                    if(m.ContainsKey("CONTENT"))
                    {
                        n.Content.Text=m["CONTENT"];
                    }                       

                    if(m.ContainsKey("LINK"))
                    {
                        if(!string.IsNullOrEmpty(m["LINK"]))
                        {
                            n.Link=m["LINK"];
                            n.TitleButton.Visibility=Visibility.Visible;
                            n.Title.Visibility=Visibility.Collapsed;
                        }
                        else
                        {
                            n.Title.Visibility=Visibility.Visible;
                            n.TitleButton.Visibility=Visibility.Collapsed;
                        }
                    }

                    if(m.ContainsKey("CREATED"))
                    {
                        n.Created.Text=m["CREATED"].ToDateTime().ToString("dd.MM.yyyy HH:mm");
                    }

                    var type=2;
                    if(m.ContainsKey("TYPE"))
                    {
                        type=m["TYPE"].ToInt();
                    }

                    switch(type)
                    {
                        //"(1) Побудительное сообщение");   развернутое
                        case 1:
                            n.NotificationItemContainer.Style=(Style)n.TryFindResource("NotificationItemContainer");
                            break;

                        //"(2) Информационное сообщение");  свернутое              
                        default:
                        case 2:
                            n.NotificationItemContainer.Style=(Style)n.TryFindResource("NotificationItemContainer");
                            break;

                        //"(9) Системное");                 системное, невидимое
                        case 9:
                            break;
                    }

                    n.Code=code;
                    n.Type=type;
                    n.Class=noteClass;

                                        
                    if(Central.DebugMode)
                    {
                        var s1="";
                        s1=$"{s1}code=[{code}]\n";
                        s1=$"{s1}class=[{noteClass}]\n";
                        n.Content.Text=$"{s1}{n.Content.Text}";
                    }

                    bool system=false;
                    bool addMessage=false;
                    bool updateMessage=false;
                    int rowNumber=rowIndex;
                    string noteCode="";
                    bool incRowIndex=true;

                    //если нет в коллекции, добавим
                    if(!NotificationItems.ContainsKey(code))
                    {
                        addMessage=true;
                    }


                    //если есть с таким же классом, обновим
                    {
                        foreach( KeyValuePair<string,NotificationItemView> item in NotificationItems)
                        {
                            var ni=item.Value;
                            if(
                                !string.IsNullOrEmpty(ni.Class)
                                && !string.IsNullOrEmpty(n.Class)
                                && ni.Class==n.Class
                            )
                            {
                                updateMessage=true;
                                addMessage=true;
                                rowNumber=ni.RowNumber;
                                noteCode=ni.Code;
                                incRowIndex=false;
                            }
                        }
                    }

                    if(type==9)
                    {
                        system=true;
                        updateMessage=false;
                        addMessage=false;
                    }


                    if(updateMessage)
                    {
                        var n0=NotificationItems[noteCode];
                        NotificationsContainer.Children.Remove(n0);
                        NotificationItems.Remove(noteCode);
                    }

                    if(addMessage)
                    {
                        NotificationsContainer.Children.Add(n);

                        var rd=new RowDefinition();
                        rd.Height=GridLength.Auto;
                        NotificationsContainer.RowDefinitions.Add(rd);
                        Grid.SetRow(n, rowNumber);
                        n.RowNumber=rowNumber;
                        Grid.SetColumn(n, 0);

                        if(!NotificationItems.ContainsKey(code)){
                            NotificationItems.Add(code,n);
                        }

                        if(incRowIndex)
                        {
                            rowIndex++;
                        }                                
                    }


                    if(system)
                    {
                        if(AutoRestartMode)
                        {
                            var cmd=m.CheckGet("TITLE");
                            cmd=cmd.ToLower();
                            switch(cmd)
                            {
                                case "restart":
                                    Restart();
                                    break;
                            }
                        }                                            
                    }

                    if(system)
                    {
                        {
                            var cmd=m.CheckGet("TITLE");
                            cmd=cmd.ToLower();
                            switch(cmd)
                            {
                                case "do_hop":
                                    DoHop();
                                    break;
                            }
                        }                                            
                    }


                }
            }
            NotificationsRows=rowIndex;
        }

        public void CheckNotificationsWarning()
        {
            if(NotificationItems!=null)
            {
                if(NotificationItems.Count>0)
                {
                    var containsImportant=false;
                    foreach(KeyValuePair<string,NotificationItemView> i in NotificationItems)
                    {
                        var n=i.Value;
                        if(n.Type==1)
                        {
                            containsImportant=true;
                        }
                    }
                    if(containsImportant)
                    {
                        ShowNotifications();
                    }
                }
            }
        }

        private void UpdateNotificationsLabel()
        {
            if(NotificationItems!=null)
            {
                if(NotificationItems.Count>0)
                {
                    {
                        Central.Navigator.UpdateNoteItem($"{NotificationItems.Count}", "Новые уведомления");
                        Central.Navigator.UpdateNoteItemActivity(true);
                    }
                    
                    RunAnimationTimer();
                }
                else
                {
                    {
                        Central.Navigator.UpdateNoteItem("", "");
                        Central.Navigator.UpdateNoteItemActivity();
                    }
                    
                    StopAnimationTimer();
                    HideNotifications();
                }
            }
        }

        private void CloseButton_Click(object sender,RoutedEventArgs e)
        {
            HideNotifications();
        }

        public void HideNotifications()
        {
            Notifications.Visibility=Visibility.Collapsed;
            if(NotificationItems!=null)
            {
                if(NotificationItems.Count>0)
                {
                    RunAnimationTimer();
                }            
            }            
        }

        public void ShowNotifications()
        {
            if(NotificationItems!=null)
            {
                if(NotificationItems.Count>0)
                {
                    Notifications.Visibility=Visibility.Visible;
                    StopAnimationTimer();
                }
            }            
        }

        public void RemoveItem(string code)
        {
            if(!string.IsNullOrEmpty(code))
            {
                if(NotificationItems!=null)
                {
                    if(NotificationItems.ContainsKey(code))
                    {
                        var n=NotificationItems[code];
                        NotificationsContainer.Children.Remove(n);
                        NotificationItems.Remove(code);
                        //rowIndex--;
                    }
                }
            }
            UpdateNotificationsLabel();
        }

        public void Window1_SizeChanged(object sender,SizeChangedEventArgs e)
        {
            int w = 0;
            int h = 0;
            if(e!=null)
            {
                w=(int)e.NewSize.Width;
                h=(int)e.NewSize.Height;
            }
            WindowSizeChanged(w,h);
        }       
    }
}
