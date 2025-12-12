using System;
using System.Collections.Generic;
using System.Windows.Threading;

namespace Client.Common
{
    /// <summary>
    /// хелпер для работы с таймаутами и интервалами
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2022-08-29</released>
    /// <changed>2022-08-29</changed>
    public class Timeout
    {
        /// <summary>
        /// таймер 
        /// </summary>
        private DispatcherTimer ReturnTimer { get; set; }
        /// <summary>
        /// интервал запуска, сек
        /// </summary>
        private int TimerInterval { get; set; }
        private int IntervalType { get; set; }
        /// <summary>
        /// бесконечный запуск
        /// </summary>
        public bool RunForever {get;set;}
        /// <summary>
        /// запускать при инициализации
        /// </summary>
        public bool RunAtStart {get;set;}

        public Timeout(int delay, TimeoutActionDelegate callback, bool runForever=false, bool runAtStart=false)
        {
            IntervalType=1;
            TimerInterval=delay;
            TimeoutAction = callback;
            ReturnTimer=null; 
            RunForever=runForever;
            RunAtStart=runAtStart;
        }
              
        public delegate void TimeoutActionDelegate();
        public TimeoutActionDelegate TimeoutAction;
        public virtual void TimeoutActionTemplate()
        {
        }

        public void SetIntervalMs(int delay)
        {
            IntervalType=2;
            TimerInterval=delay;
        }
        
        public void SetInterval(int delay)
        {
            IntervalType=1;
            TimerInterval=delay;
        }

        public void Run(){
            TimerRun();
        }

        public void Finish(){
            TimerStop();
        }

        public void Restart()
        {
            TimerStop();
            Run();
        }

        private void TimerRun()
        {
            if(TimerInterval != 0)
            {
                //if(ReturnTimer == null)
                {
                    var interval=new TimeSpan(0,0,TimerInterval);
                    if(IntervalType==2)
                    {
                        interval=new TimeSpan(0,0,0,0,TimerInterval);
                    }

                    ReturnTimer = new DispatcherTimer
                    {                        
                        Interval = interval
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", interval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("Timeout_TimerRun", row);
                    }

                    ReturnTimer.Tick += (s,e) =>
                    {
                        TimeoutAction?.Invoke();
                        if(!RunForever)
                        {
                            if(ReturnTimer!=null)
                            {
                                ReturnTimer.Stop();
                            }                            
                            ReturnTimer=null;
                        }
                    };

                    ReturnTimer.Start();
                }
               
            }

            if(RunAtStart)
            {
                TimeoutAction?.Invoke();
            }
        }

        private void TimerStop()
        {
            if(ReturnTimer != null)
            {
                {
                    ReturnTimer.Stop();
                    ReturnTimer=null;
                }
            }
        }

    }
   
}
