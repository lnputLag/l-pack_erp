using Client.Common;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Client.Interfaces.Main
{
    /// <summary>
    /// сплеш-скрин с прогресс-баром
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2022-12-29</released>
    /// <changed>2022-12-29</changed>
    public partial class ProgressControl : UserControl
    {
        public ProgressControl()
        {
            Caption="Загрузка";
            Delay=2000;
            ProgressTickTimerInterval=300;
            Started=false;

            InitializeComponent();

        }
        /// <summary>
        /// сообщение рядом с прогресс-баром
        /// </summary>
        public string Caption {get;set;}
        /// <summary>
        /// задержка перед показом сплеш-скрина, мс
        /// </summary>
        public int Delay {get;set;}
        public bool Started {get;set;}

        public void Start(int delay=2000)
        {
            Started=true;
            if(delay > 0)
            {
                ProgressDelayTimerInterval=delay;
                ProgressDelayTimerRun();
            }
            else
            {
                ShowProgress();
            }
        }

        public void Stop()
        {
             Started=false;
             ProgressTickTimerStop();
             ProgressDelayTimerStop();
             HideProgress();
        }

        private void ShowProgress()
        {
            ProgressCaption.Text=Caption;
            ProgressTickTimerRun();
            Progress.Visibility=Visibility.Visible;
        }

        private void HideProgress()
        {
            ProgressTickTimerStop();
            Progress.Visibility=Visibility.Collapsed;
        }

        /// <summary>
        /// </summary>
        private DispatcherTimer ProgressTickTimer { get; set; }
        /// <summary>
        /// интервал обновления прогресс-бара, мс
        /// </summary>
        private int ProgressTickTimerInterval { get; set; }
        private int ProgressValue {get;set;}

        /// <summary>
        /// </summary>
        private void ProgressTickTimerRun()
        {
            ProgressValue=0;

            if(ProgressTickTimerInterval != 0)
            {
                if(ProgressTickTimer == null)
                {
                    ProgressTickTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0,0,0,0,ProgressTickTimerInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", ProgressTickTimerInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("ProgressControl_ProgressTickTimerRun", row);
                    }

                    ProgressTickTimer.Tick += (s,e) =>
                    {
                        ProgressTickCheck();
                    };
                }

                if(ProgressTickTimer.IsEnabled)
                {
                    ProgressTickTimer.Stop();
                }
                ProgressTickTimer.Start();
            }
        }

        private void ProgressTickTimerStop()
        {
            if(ProgressTickTimer != null)
            {
                if(ProgressTickTimer.IsEnabled)
                {
                    ProgressTickTimer.Stop();
                }
            }
        }

        private void ProgressTickCheck()
        {
            ProgressValue=ProgressValue+1;
            if(ProgressValue>100)
            {
                ProgressValue=100;
                ProgressBar.Value=ProgressValue;
                ProgressTickTimerStop();
            }
        }


        /// <summary>
        /// </summary>
        private DispatcherTimer ProgressDelayTimer { get; set; }
        /// <summary>
        /// интервал обновления прогресс-бара, мс
        /// </summary>
        private int ProgressDelayTimerInterval { get; set; }        

        /// <summary>
        /// </summary>
        private void ProgressDelayTimerRun()
        {
            ProgressValue=0;

            if(ProgressDelayTimerInterval != 0)
            {
                if(ProgressDelayTimer == null)
                {
                    ProgressDelayTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0,0,0,0,ProgressDelayTimerInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", ProgressDelayTimerInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("ProgressControl_ProgressDelayTimerRun", row);
                    }

                    ProgressDelayTimer.Tick += (s,e) =>
                    {
                        ProgressDelayCheck();
                    };
                }

                if(ProgressDelayTimer.IsEnabled)
                {
                    ProgressDelayTimer.Stop();
                }
                ProgressDelayTimer.Start();
            }
        }

        private void ProgressDelayTimerStop()
        {
            if(ProgressDelayTimer != null)
            {
                if(ProgressDelayTimer.IsEnabled)
                {
                    ProgressDelayTimer.Stop();
                }
            }
        }

        private void ProgressDelayCheck()
        {
            ProgressDelayTimerStop();
            if(Started)
            {
                ShowProgress();
            }
            else
            {
                HideProgress();
            }
        }
    }
}
