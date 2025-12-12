using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Client.Common
{
    /// <summary>
    /// реестр статистики
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2025-02-11</released>
    /// <changed>2025-02-22</changed>
    public class Stat
    {
        public Stat()
        {
            RequestStat = new Dictionary<string, Dictionary<string, string>>();
            TimerStat = new Dictionary<string, Dictionary<string, string>>();
        }

        public Dictionary<string, Dictionary<string, string>> RequestStat { get; set; }
        public Dictionary<string, Dictionary<string, string>> TimerStat { get; set; }

        public void RequestAdd(string k, Dictionary<string, string> row2)
        {
            var row = new Dictionary<string, string>();
            if(RequestStat.ContainsKey(k))
            {
                row = RequestStat[k];
            }
            else
            {
                row.CheckAdd("KEY", k);
                row.CheckAdd("ROUTE", k);
                row.CheckAdd("RESULT_COMPLETE_COUNT", "0");
                row.CheckAdd("RESULT_FAILED_COUNT", "0");
                row.CheckAdd("COUNT", "0");
                row.CheckAdd("TIME_TOTAL", "0");
                row.CheckAdd("RESULT_LAST", "0");
                row.CheckAdd("ON_CREATE", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
            }

            foreach(var item in row2)
            {
                row.CheckAdd(item.Key, item.Value);
            }

            RequestStat[k] = row;
        }

        public void TimerAdd(string k, Dictionary<string, string> row2)
        {
            var row = new Dictionary<string, string>();
            if(TimerStat.ContainsKey(k))
            {
                row = TimerStat[k];
            }
            else
            {
                row.CheckAdd("KEY", k);
                row.CheckAdd("NAME", k);
                row.CheckAdd("ON_CREATE", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
            }

            foreach(var item in row2)
            {
                row.CheckAdd(item.Key, item.Value);
            }

            TimerStat[k] = row;
        }

    }
}
