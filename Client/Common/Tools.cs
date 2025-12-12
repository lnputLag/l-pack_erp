using System;
using System.Collections.Generic;
using System.Text;

namespace Client.Common
{
    /// <summary>
    /// транслит
    /// </summary>
    /// <author>zelenskiy_sv</author>
    public static class Tools
    {

        public static string Translit(string s)
        {
            StringBuilder ret = new StringBuilder(s);

            string[] rus = { "А", "Б", "В", "Г", "Д", "Е", "Ё", "Ж", "З", "И", "Й", "К", "Л", "М", "Н", "О", "П", "Р", "С", "Т", "У", "Ф", "Х", "Ц", "Ч", "Ш", "Щ", "Ъ", "Ы", "Ь", "Э", "Ю", "Я" };
            string[] eng = { "A", "B", "V", "G", "D", "E", "YO", "ZH", "Z", "I", "Y", "K", "L", "M", "N", "O", "P", "R", "S", "T", "U", "F", "H", "TS", "CH", "SH", "SHCH", "", "Y", "", "E", "YU", "YA" };

            for (int i = 0; i < rus.Length; i++)
            {
                ret.Replace(rus[i].ToUpper(), eng[i].ToUpper());
                ret.Replace(rus[i].ToLower(), eng[i].ToLower());
            }

            return ret.ToString();
        }

        [ObsoleteAttribute]
        public static string FioToShortName(string f, string i, string o)
        {
            // используйте более грамотную функцию: StringExtension.SurnameInitials()

            // Формирование короткого имени (Фамилия И.О.)

            string name = i.Length > 0 ? i.Substring(0, 1) + "." : "";

            string MiddleName = o.Length > 0 ? o.Substring(0, 1) + "." : "";

            var shortName = new StringBuilder(f)
                .Append(" ")
                .Append(name)
                .Append(MiddleName).ToString();

            return shortName;
        }

        /// <summary>
        /// разделение строки адреса на составляюшщие
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string,string> AddressSplit(string s)
        {
            /*
                на входе:
                    141533, Московская обл., Солнечногорск г., Майдарово п., дом Д. ЗДАНИЕ КОНТОРЫ, корпус ИНВ. №/ЛИТЕР/ОБЪЕКТ 24283/А/31, офис ЭТАЖ 2 ЧАСТЬ ПОМЕЩ. 16
                    141533,Московская обл., Солнечногорск г., Майдарово п., дом Д. ЗДАНИЕ КОНТОРЫ, корпус ИНВ. №/ЛИТЕР/ОБЪЕКТ 24283/А/31, офис ЭТАЖ 2 ЧАСТЬ ПОМЕЩ. 16
                    0123456789
                на выходе:
                    
             */

            var result=new Dictionary<string,string>();
            result.CheckAdd("ADDRESS","");
            result.CheckAdd("ZIP_CODE","");

            if(!s.IsNullOrEmpty())
            {
                var delimiterIndex=s.IndexOf(",");
                if(delimiterIndex >= 6)
                {
                    var a=s.Substring(0, delimiterIndex);
                    if(!a.IsNullOrEmpty())
                    {
                        result.CheckAdd("ZIP_CODE",a);
                    }

                    if(s.Substring(delimiterIndex, 1) == ",")
                    {
                        delimiterIndex=delimiterIndex+1;
                    }

                    if(s.Substring(delimiterIndex, 1) == " ")
                    {
                        delimiterIndex=delimiterIndex+1;
                    }

                    var b=s.Substring(delimiterIndex, (s.Length-delimiterIndex));
                    if(!b.IsNullOrEmpty())
                    {
                        result.CheckAdd("ADDRESS",b);
                    }

                }

            }


            return result;
        }


        /// <summary>
        /// вычисление промежутка между двумя временными метками
        /// (текущее время - указанное время)
        /// результат представляется в целых секундах
        /// формат на входе dd.MM.yyyy HH:mm:ss
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static int TimeOffsetSeconds(string d)
        {
            var result = 0;

            if(!d.IsNullOrEmpty())
            {
                var d2 = DateTime.Now;
                var d1 = d.ToDateTime();
                var dd = (TimeSpan)(d2 - d1);
                var dt = (int)dd.TotalSeconds;
                result = dt;
            }

            return result;
        }

        public static int TimeOffset(string d)
        {
            var result = 0;

            if(!d.IsNullOrEmpty())
            {
                var d2 = DateTime.Now;
                var d1 = d.ToDateTime();
                var dd = (TimeSpan)(d2 - d1);
                var dt = (int)dd.TotalMilliseconds;
                result = dt;
            }

            return result;
        }

        public static long GetTimeStamp(string dateTime)
        {
            long result = 0;

            if(!dateTime.IsNullOrEmpty())
            {
                var dt = dateTime.ToDateTime();
                DateTime currentTime = dt.ToUniversalTime();
                long unixTime = ((DateTimeOffset)currentTime).ToUnixTimeSeconds();
                result = unixTime;
            }

            return result;
        }

        public static string GetToday()
        {
            var today = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            return today;
        }

        public static string GetTodayMs()
        {
            var today = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss ffffff");
            return today;
        }

        public static DateTime NormalizeDateTime(DateTime datetime)
        {
            return new DateTime(datetime.Year, datetime.Month, datetime.Day, datetime.Hour, datetime.Minute, 0, datetime.Kind);
        }

        public static string DumpListSimple(List<Dictionary<string, string>> list)
        {
            var s = "";
            foreach(var row in list)
            {
                s = s.Append($"{row.GetDumpString()}",true);
            }
            return s;
        }

        public static int GetVersionInteger(string s)
        {
            var result=1;

            if(!s.IsNullOrEmpty())
            {
                var e = s.Split('.');
                if(e[0]!=null)
                {
                    var v=e[0].ToString().ToInt();
                    if(v>0)
                    {
                        result=result+(v*1000000000);
                    }                    
                }
                if(e[1]!=null)
                {
                    var v=e[1].ToString().ToInt();
                    if(v>0)
                    {
                        result=result+(v*1000000);
                    }                    
                }
                if(e[2]!=null)
                {
                    var v=e[2].ToString().ToInt();
                    if(v>0)
                    {
                        result=result+(v*1000);
                    }                    
                }
                if(e[3]!=null)
                {
                    var v=e[3].ToString().ToInt();
                    if(v>3)
                    {
                        result=result+(v*1);
                    }                    
                }
            }
                   

            return result;
        }
    }
}
