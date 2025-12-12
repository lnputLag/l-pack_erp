using Client.Common;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Client.Interfaces.Main
{
    public partial class LogWindow:Window
    {
        public LogWindow(string message, string title = "")
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();

            Title = title;
            Log.Text = message;
            AutoUpdateInterval=0;
        }

        public delegate string OnUpdateDelegate();
        public OnUpdateDelegate OnUpdate;
        public virtual void OnUpdateAction(){}
        public int AutoUpdateInterval {get;set;}
        public DispatcherTimer AutoCloseTimer { get; set; }

        public void SetOnUpdate(OnUpdateDelegate f)
        {
            OnUpdate=f;
            RefreshButton.Visibility=Visibility.Visible;
            OnUpdate.Invoke();

            if(AutoUpdateInterval > 0)
            {
                var autoUpdateTimer=new Timeout(
                    AutoUpdateInterval,
                    () =>
                    {
                        Update();
                    },
                    true,
                    true
                );
                autoUpdateTimer.SetIntervalMs(AutoUpdateInterval);
                autoUpdateTimer.Run();
            }
        }

        /// <summary>
        /// автоматически скрывает окно через заданное время
        /// </summary>
        /// <param name="seconds"></param>
        public void AutoClose(int seconds = 1)
        {
            if (AutoCloseTimer == null)
            {
                AutoCloseTimer = new DispatcherTimer
                {
                    Interval = new TimeSpan(0, 0, seconds)
                };

                {
                    var row = new Dictionary<string, string>();
                    row.CheckAdd("TIMEOUT", seconds.ToString());
                    row.CheckAdd("DESCRIPTION", "");
                    Central.Stat.TimerAdd("LoginWindow_RunTemplateTimeoutTimer", row);
                }

                AutoCloseTimer.Tick += (s, e) =>
                {
                    Close();
                    AutoCloseTimer.Stop();
                };
            }
            else
            {
                AutoCloseTimer.Stop();
            }
            AutoCloseTimer.Start();
        }

        public void SetSize(int w, int h)
        {
            Width=w;
            Height = h;
        }



        private void Update()
        {
            if(OnUpdate != null)
            {
                var result=OnUpdate.Invoke();
                Log.Text=result;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var window = GetWindow(this);
        }
        
        private void RefreshButton_Click(object sender,RoutedEventArgs e)
        {
        }

        private void ClearButton_Click(object sender,RoutedEventArgs e)
        {
        }
        
        private void HelpButton_Click(object sender,RoutedEventArgs e)
        {
        }

        private void RefreshButton_Click_1(object sender, RoutedEventArgs e)
        {
            Update();
        }
    }
}
