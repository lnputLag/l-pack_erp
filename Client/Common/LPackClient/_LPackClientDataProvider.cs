using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Client.Common
{
    /// <summary>
    /// Хелпер для выполнения типовых запросов
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>3</version>    
    [Obsolete]
    public class _LPackClientDataProvider
    {
        /// <summary>
        /// Выполнение запроса, разбор данных в структуру указанного типа.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="m">module</param>
        /// <param name="o">object</param>
        /// <param name="a">action</param>
        /// <param name="key">answer section key</param>
        /// <param name="p">parameters</param>
        /// <param name="timeout">timeout, ms</param>
        /// <returns></returns>
        [Obsolete]
        public static T DoQueryDeserialize<T>(string m, string o, string a, string key = "", Dictionary<string, string> p = null, int timeout=0)
        {
            T result = default;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", m);
            q.Request.SetParam("Object", o);
            q.Request.SetParam("Action", a);
            
            if(timeout!=0)
            {
                //default=30000
                q.Request.Timeout = timeout;
            }

            if (p != null)
            {
                if (p.Count > 0)
                {
                    foreach (var i in p)
                    {
                        if (i.Value != null)
                        {
                            q.Request.SetParam(i.Key, i.Value);
                        }
                    }
                }
            }

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                if (!string.IsNullOrEmpty(q.Answer.Data))
                {
                    if (string.IsNullOrEmpty(key))
                    {
                        result = JsonConvert.DeserializeObject<T>(q.Answer.Data);
                    }
                    else
                    {
                        var t = JsonConvert.DeserializeObject<Dictionary<string, T>>(q.Answer.Data);
                        if (t != null)
                        {
                            if (t.ContainsKey(key))
                            {
                                result = t[key];
                            }
                        }
                    }
                }
            }

            return (T)Convert.ChangeType(result, typeof(T));
        }

        /// <summary>
        /// Выполнение запроса, возврат результата в сыром виде (текст)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="m">module</param>
        /// <param name="o">object</param>
        /// <param name="a">action</param>
        /// <param name="key">answer section key</param>
        /// <param name="p">parameters</param>
        /// <returns></returns>
        [Obsolete("DoQueryRawResult устарел, используйте вместо него DoQueryGetResult")]
        public static string DoQueryRawResult(string m, string o, string a, string key = "", Dictionary<string, string> p = null)
        {
            string result = "";

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", m);
            q.Request.SetParam("Object", o);
            q.Request.SetParam("Action", a);

            if (p != null)
            {
                if (p.Count > 0)
                {
                    foreach (var i in p)
                    {
                        if (i.Value != null)
                        {
                            q.Request.SetParam(i.Key, i.Value);
                        }
                    }
                }
            }

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                if (!string.IsNullOrEmpty(q.Answer.Data))
                {
                    var t = q.Answer.Data;
                    if (t != null)
                    {
                        result = t;
                    }
                }
            }

            /*
            if(!complete)
            {
                //q.ProcessError();
            }
            */

            return result;
        }

        [Obsolete]
        public static LPackClientQuery DoQueryGetResult(string m, string o, string a, string key = "", Dictionary<string, string> p = null)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", m);
            q.Request.SetParam("Object", o);
            q.Request.SetParam("Action", a);

            if (p != null)
            {
                if (p.Count > 0)
                {
                    foreach (var i in p)
                    {
                        if (i.Value != null)
                        {
                            q.Request.SetParam(i.Key, i.Value);
                        }
                    }
                }
            }

            q.DoQuery();

            /*
            if (q.Answer.Status == 0)
            {
                if (!string.IsNullOrEmpty(q.Answer.Data))
                {
                    var t = q.Answer.Data;
                    if (t != null)
                    {
                        result = t;
                    }
                }
            }
            */

            /*
            if(!complete)
            {
                //q.ProcessError();
            }
            */

            return q;
        }
    }
}
