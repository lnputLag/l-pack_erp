using System;
using System.Collections.Generic;

namespace Client.Common
{
    /// <summary>
    /// вычисляет временные интервалы
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2019-07-12</released>
    /// <changed>2023-11-29</changed>
    public class Profiler
    {
        public Profiler(string name = "", bool noAddPoint = false)
        {
            Delta = 0;
            Total = 0;

            Points = new Dictionary<string, double>();
            LastTimeStamp = DateTime.Now;

            if(!noAddPoint)
            {
                AddPoint(name);
            }
        }

        public Dictionary<string, double> Points { get; set; }
        public DateTime LastTimeStamp { get; set; }
        public int Count { get; set; }
        public double Delta { get; set; }
        public double Total { get; set; }

        public void AddPoint(string name = "")
        {
            GetDelta(name);
        }

        public double GetDelta(string name = "")
        {
            Count++;
            DateTime current = DateTime.Now;
            double delta = (current - LastTimeStamp).TotalMilliseconds;
            delta = Math.Round(delta, 0);
            var rnd = Cryptor.MakeRandom();
            Delta = delta;
            LastTimeStamp = current;
            name = $"{Count}_{name}_{rnd}";
            if(!Points.ContainsKey(name))
            {
                Points.Add(name, delta);
            }
            Total = Total + delta;

            return delta;
        }

        public string GetTimeLabel()
        {
            string result = "";
            GetDelta();
            result = $"[{Delta.ToString().SPadLeft(5)}][{Total.ToString().SPadLeft(5)}]";
            return result;
        }
    }
}
