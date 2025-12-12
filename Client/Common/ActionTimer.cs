using System;
using System.Collections.Generic;
using System.Windows.Threading;

namespace Client.Common
{
    /// <summary>
    /// автоматизация таймерных служб
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-02-14</released>
    /// <changed>2023-02-14</changed>
    public class ActionTimer
    {
        public ActionTimer()
        {
            Interval=0;
            Enabled=false;
        }

        /// <summary>
        /// таймер анимации транспаранта Поставь рулон
        /// </summary>
        private DispatcherTimer Timer { get; set; }

        /// <summary>
        /// интервал вызова обработчика, миллисекунды
        /// </summary>
        public int Interval { get; set; }

        /// <summary>
        /// флаг активности обработчика, обработчик будет вызван,
        /// когда фалаг поднят
        /// </summary>
        public bool Enabled { get; set; }

        public delegate void OnTimerTickDelegate();
        public OnTimerTickDelegate OnTimerTick;
        public virtual void OnTimerTickAction()
        {

        }

        /// <summary>
        /// запусr таймера получения данных
        /// </summary>
        public void Start()
        {
            if(Interval != 0)
            {
                if(Timer == null)
                {
                    Timer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0,0,0,0,Interval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", Interval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("ActionTimer_RunComplectationWarningTimer", row);
                    }

                    Timer.Tick += (s,e) =>
                    {
                        if(Enabled)
                        {
                            if(OnTimerTick!=null)
                            {
                                OnTimerTick.Invoke();
                            }                            
                        }
                    };
                }

                if(Timer.IsEnabled)
                {
                    Timer.Stop();
                }
                Timer.Start();
                Enabled=true;
            }
        }

        //останов таймера 
        public void Stop()
        {
            if(Timer != null)
            {
                if(Timer.IsEnabled)
                {
                    Timer.Stop();
                    Enabled=false;
                }
            }
        }
    }
}
