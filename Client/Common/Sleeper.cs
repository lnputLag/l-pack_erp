using System;
using System.Text;
using System.Threading.Tasks;

namespace Client.Common
{
    /// <summary>
    /// вызов функций с таймаутом
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2024-03-28</released>
    /// <changed>2024-03-28</changed>
    public class Sleeper
    {
        public Sleeper()
        {
            Timeout = 5000;
            WorkTime = 0;
            Result = false;
            Complete = false;
            Status = TaskStatus.Created;
            Profiler = new Profiler();
            Worker = null;
            InnerLog = "";
        }

        public int Timeout { get; set; }
        public int WorkTime { get; set; }
        public bool Result { get; set; }
        public bool Complete { get; set; }
        public TaskStatus Status { get; set; }
        public delegate void WorkerDelegate(Sleeper s);
        public WorkerDelegate Worker { get; set; }
        public string InnerLog { get; set; }
        private Profiler Profiler { get; set; }

        public void Run()
        {
            InnerLog = InnerLog.Append($"Stage 10",true);
            Result = false;
            Complete = false;

            // https://stackoverflow.com/questions/6682040/how-to-cancel-a-task-that-is-waiting-with-a-timeout-without-exceptions-being-thr            
            //
            //https://metanit.com/sharp/tutorial/3.13.php

            try
            {
                InnerLog = InnerLog.Append($"Stage 11", true);
                Task sleeper = Task.Factory.StartNew(() =>
                {
                    InnerLog = InnerLog.Append($"Stage 20", true);
                    if(Worker != null)
                    {
                        InnerLog = InnerLog.Append($"Stage 21 >>>", true);
                        Worker.Invoke(this);
                    }
                    InnerLog = InnerLog.Append($"Stage 22 <<<", true);
                    Complete = true;
                });
                InnerLog = InnerLog.Append($"Stage 12", true);

                int index = Task.WaitAny(new[] { sleeper }, TimeSpan.FromMilliseconds(Timeout));

                /*
                var cts = new System.Threading.CancellationTokenSource();
                Task cancellable = sleeper.ContinueWith(ignored => { }, cts.Token);
                InnerLog = InnerLog.Append($"Stage 13 status=[{cancellable.Status}]", true);
                cts.Cancel();
                InnerLog = InnerLog.Append($"Stage 14 status=[{cancellable.Status}]", true);
                //index = Task.WaitAny(new[] { cancellable }, TimeSpan.FromMilliseconds(Timeout));
                */

                //Status = cancellable.Status;
                //InnerLog = InnerLog.Append($"Stage 15 status=[{cancellable.Status}]", true);
                
                WorkTime = (int)Profiler.GetDelta();
                
            }
            catch(Exception e)
            {
                InnerLog = InnerLog.Append($"Stage 90", true);
            }
            InnerLog = InnerLog.Append($"Stage 99", true);

        }
    }
}
