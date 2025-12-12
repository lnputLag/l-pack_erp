using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Client.Common
{
    /// <summary>
    /// кастомные методы для работы со словарями
    /// </summary>
    /// <author>balchugov_dv</author>
    public static class DictionaryExtension
    {

        public static void SaveToFile(Dictionary<string, string> d, string fileName)
        {
            var sw = new StreamWriter(fileName);

            foreach (var pair in d)
            {
                sw.WriteLine($"{pair.Key}={pair.Value}\n");

            }

            sw.Close();
        }

        public static Dictionary<string, string> CreateFromFile(string fileName)
        {
            try
            {
                if (File.Exists(fileName))
                {
                    return CreateFromText(File.ReadAllText(fileName, Encoding.Default));
                }

                return new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        public static Dictionary<string, string> CreateFromText(string text)
        {
            var result = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(text))
            {
                var lines = text.Split(new[] { "\n", "\r\n" }, System.StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var elements = line.Split('=');
                    if (elements.Length >= 2)
                    {
                        if (elements[0] != null && elements[1] != null)
                        {
                            result.Add(elements[0], elements[1]);
                        }
                    }
                }
            }

            return result;
        }

        public static Dictionary<string, string> CreateFromTextConfig(string text)
        {
            var result = new Dictionary<string, string>();

            try
            {
                if (!string.IsNullOrEmpty(text))
                {
                    string[] lines = text.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in lines)
                    {
                        if (!string.IsNullOrEmpty(line))
                        {
                            bool include = true;

                            var s = line.Substring(0, 1);
                            if (s == ";")
                            {
                                include = false;
                            }

                            if (!(line.IndexOf('=') > -1))
                            {
                                include = false;
                            }

                            if (include)
                            {
                                string[] elements = line.Split('=');
                                if (
                                       elements[0] != null
                                       && elements[1] != null
                                   )
                                {
                                    result.CheckAdd(elements[0], elements[1]);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {

            }

            return result;
        }

        public static Dictionary<string, string> CreateFromLine(string text)
        {
            var result = new Dictionary<string, string>();

            if(!string.IsNullOrEmpty(text))
            {
                var lines = text.Split(new[] { "|" }, System.StringSplitOptions.RemoveEmptyEntries);
                foreach(var line in lines)
                {
                    var elements = line.Split('=');
                    if(elements[0] != null && elements[1] != null)
                    {
                        result.Add(elements[0], elements[1]);
                    }
                }
            }

            return result;
        }


        public static KeyValuePair<TKey, TValue> GetEntry<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            return new KeyValuePair<TKey, TValue>(key, dictionary[key]);
        }

        /// <summary>
        /// проверяет наличие элемента массива с указанным ключом
        /// если элемент не существует, создает, далее устанавливает его значение
        /// </summary>
        /// <param name="d"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void CheckAdd<T>(this Dictionary<string, T> d, string key, T value = default)
        {
            if (d != null)
            {
                if (!string.IsNullOrEmpty(key))
                {
                    if (!d.ContainsKey(key))
                    {
                        d.Add(key, default);
                    }
                    d[key] = value;
                }
            }
        }

        public static string CheckGet(this Dictionary<string, string> d, string key)
        {
            string result = "";
            if (d != null)
            {
                if (!string.IsNullOrEmpty(key))
                {
                    if (d.ContainsKey(key))
                    {
                        if (d[key] != null)
                        {
                            result = d[key];
                        }
                    }
                }
            }
            return result;
        }

        public static string CheckGet(this Dictionary<string,object> d, string key)
        {
            string result="";
            if(d!=null)
            {
                if(!string.IsNullOrEmpty(key))
                {
                    if(d.ContainsKey(key))
                    {
                        if(d[key]!=null)
                        {
                            result=d[key].ToString();
                        }
                    }
                }
            }
            return result;
        }

        public static TValue CheckGet<TKey, TValue>(this Dictionary<TKey, TValue> d, TKey key)
        {
            TValue result = default;
            if (d != null)
            {
                if (d.ContainsKey(key))
                {
                    if (d[key] != null)
                    {
                        result = d[key];
                    }
                }
            }
            return result;
        }

        /*
            создание словаря из текста
            каждая строка:
                KEY=VALUE
            например:
                cm1_r1=0
                cm1_r2=0
                cm1_r3=0
            строка не рассматривается, если:
                строка начинается с комментария: ";"
                если в строке нет символа "="
         */
        public static Dictionary<string,string> CreateFromText(string text, string format="ini")
        {
            var result= new Dictionary<string,string>();

            if(!string.IsNullOrEmpty(text))
            {
                string[] lines=text.Split(new string[] {"\n", "\r\n"}, StringSplitOptions.RemoveEmptyEntries);
                foreach(string line in lines)
                {
                    if(!string.IsNullOrEmpty(line))
                    {
                        bool include=true;

                        var s=line.Substring(0,1);
                        if(s == ";")
                        {
                            include=false;
                        }

                        if( !(line.IndexOf("=") > -1) )
                        {
                            include=false;
                        }

                        if(include)
                        {
                            string[] elements=line.Split('=');
                            if(
                                elements[0]!=null
                                && elements[1]!=null
                            )
                            {
                                result.Add(elements[0],elements[1]);
                            }
                        }
                        
                    }
                    
                }
            }

            return result;
        }

        public static string GetDumpString(this Dictionary<string,string> d)
        {
            string result="";
            if(d!=null)
            {
                foreach(KeyValuePair<string,string> item in d)
                {
                    result=$"{result}\n    [{item.Key}]=[{item.Value}]";
                }
            }
            return result;
        }

        
    }
}
