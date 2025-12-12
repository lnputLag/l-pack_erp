using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Client.Interfaces.Debug
{
    /// <summary>
    /// </summary>
    public partial class LogView:UserControl
    {
        /// <summary>
        /// </summary>
        public LogView()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this,ProcessMessages);

            //регистрация обработчика клавиатуры
            PreviewKeyDown += OnKeyDown;


            AutoUpdateInterval=1;
            RunAutoUpdateTimer();
        }

        public void UpdateLog()
        {
            Log.Text=Central.Logger.Log;
        }

        public void ClearLog()
        {
            Central.Logger.Log="";
            UpdateLog();
        }

       
        public int AutoUpdateInterval { get; set; }
        public DispatcherTimer AutoUpdateTimer { get; set; }
        public void RunAutoUpdateTimer()
        {
            if(AutoUpdateInterval!=0)
            {
                if(AutoUpdateTimer == null)
                {
                    AutoUpdateTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0,0,AutoUpdateInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", AutoUpdateInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("LogView_RunAutoUpdateTimer", row);
                    }

                    AutoUpdateTimer.Tick += (s,e) =>
                    {
                        UpdateLog();
                    };
                }

                if(AutoUpdateTimer.IsEnabled)
                {
                    AutoUpdateTimer.Stop();
                }
                AutoUpdateTimer.Start();
            }
           
        }

        public void StopAutoUpdateTimer()
        {
            if(AutoUpdateTimer != null)
            {
                if(AutoUpdateTimer.IsEnabled)
                {
                    AutoUpdateTimer.Stop();
                }
            }
        }



        /// <summary>
        /// Остановка вспомогательных процессов при закрытии интерфейса.
        /// </summary>
        public void Destroy()
        {
            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            StopAutoUpdateTimer();
        }

        
        /// <summary>
        /// обработчик сообщений.
        /// </summary>
        /// <param name="m">сообщение.</param>
        private void ProcessMessages(ItemMessage m)
        {
            //Group ShipmentControl
            if(m.ReceiverGroup.IndexOf("ShipmentControl") > -1)
            {
                if(m.ReceiverName.IndexOf("ShipmentCliche") > -1)
                {
                    switch(m.Action)
                    {
                        case "Refresh":
                            
                            break;
                    }

                }
            }
        }

        /// <summary>
        /// The OnKeyDown.
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="KeyEventArgs"/>.</param>
        private void OnKeyDown(object sender,KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.F5:
                    //id.LoadItems();
                    e.Handled = true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

               
            }
        }

        /// <summary>
        /// The ShowHelp.
        /// </summary>
        public void ShowHelp()
        {
            //Central.ShowHelp("/doc/l-pack-erp/shipments/control/equipments/shipmentcliche");
        }

        /// <summary>
        /// обработчик системы навигации по URL.
        /// </summary>
        public void ProcessNavigation()
        {
        }

        /// <summary>
        /// The OnLoad.
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="RoutedEventArgs"/>.</param>
        private void OnLoad(object sender,RoutedEventArgs e)
        {
            //Grid.UpdateGrid();
        }

        /// <summary>
        /// The HelpButton_Click.
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="RoutedEventArgs"/>.</param>
        private void HelpButton_Click(object sender,RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void RefreshButton_Click(object sender,RoutedEventArgs e)
        {
            UpdateLog();
        }

        private void ClearButton_Click(object sender,RoutedEventArgs e)
        {
            ClearLog();
        }
    }
}
