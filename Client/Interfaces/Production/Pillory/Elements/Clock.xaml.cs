using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Логика взаимодействия для PilloryClock.xaml
    /// </summary>
    public partial class Clock : UserControl
    {
        public Clock()
        {
            InitializeComponent();

            SetClockLabelValue();
            TimerInterval = 60;
            RunTimer();
        }

        /// <summary>
        /// Таймер
        /// </summary>
        private DispatcherTimer Timer { get; set; }

        /// <summary>
        /// интервал обновления таймера (сек)
        /// </summary>
        private int TimerInterval { get; set; }

        public void Start()
        {
            if (Timer != null)
            {
                Timer.Start();
            }
        }

        public void Stop() 
        {
            if (Timer != null)
            {
                Timer.Stop();
            }
        }

        private void RunTimer()
        {
            if (TimerInterval != 0)
            {
                if (Timer == null)
                {
                    Timer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0, 0, TimerInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", TimerInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("PilloryClock_RunTimer", row);
                    }

                    Timer.Tick += (s, e) =>
                    {
                        SetClockLabelValue();
                    };
                }

                if (Timer.IsEnabled)
                {
                    Timer.Stop();
                }

                Timer.Start();
            }
        }

        private void SetClockLabelValue()
        {
            ClockLabel.Content = $"{(DateTime.Now.Hour):00}:{DateTime.Now.Minute:00}";
        }
    }
}
