using System;
using System.Globalization;
using System.Windows.Media;

namespace Client.Common
{
    /// <summary>
    /// дополнительные методы для работы со строками
    /// Версия: 3, Дата обновления: 2021-01-22
    /// </summary>
    public static class StringExtension
    {
        /// <summary>
        /// преобразует строку в число int
        /// (если строка пустая или Null, вернет 0)
        /// на входе:  (string) "4.5" "4,5" "425" "True" "False"
        /// на выходе: (int)    4     4     425   1      0 
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static int ToInt(this string t)
        {
            int result = 0;

            if(t!=null)
            { 
                string s = t.ToString();
                s=s.ToLower();

                if(!string.IsNullOrEmpty(s))
                {
                    if(
                        s.IndexOf("true") > -1
                        || s.IndexOf("false") > -1
                    )
                    {
                        switch( s )
                        {
                            case "true":
                                result=1;
                                break;

                            case "false":
                                result=0;
                                break;
                        }
                    }
                    else
                    {
                        double d = s.ToDouble();                    
                        result=(int)d;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// преобразует строку в число int
        /// (если строка пустая или Null, вернет 0)
        /// на входе объект, преобразуемый в cтроку
        /// на входе:  (string) "4.5" "4,5" "425" "True" "False"
        /// на выходе: (int)    4     4     425   1      0 
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static int ToInt(this object o)
        {
            int result = 0;

            if(o!=null)
            { 
                string s = o.ToString();
                s=s.ToLower();

                if(!string.IsNullOrEmpty(s))
                {
                    if(
                        s.IndexOf("true") > -1
                        || s.IndexOf("false") > -1
                    )
                    {
                        switch( s )
                        {
                            case "true":
                                result=1;
                                break;

                            case "false":
                                result=0;
                                break;
                        }
                    }
                    else
                    {
                        double d = s.ToDouble();                    
                        result=(int)d;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// преобразует строку в булев тип
        /// (если строка пустая или Null, вернет false)
        /// на входе:  (string) "4.5" "4,5" "425" "True" "False"
        /// на выходе: (int)    4     4     425   1      0 
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool ToBool(this string t)
        {
            bool result = false;

            if(!string.IsNullOrEmpty(t))
            {
                string s = t.ToString();
                s = s.ToLower();

                if (
                    s.IndexOf("true") > -1
                    || s.IndexOf("false") > -1
                )
                {
                    switch( s )
                    {
                        case "true":
                            result=true;
                            break;

                        case "false":
                            result=false;
                            break;
                    }
                }
                else
                {
                    double d = s.ToDouble();                    
                    int di=(int)d;
                    if(di == 1)
                    {
                        result=true;
                    }
                }
            }

            return result;
        }


        /// <summary>
        /// преобразует строку в число double
        /// (если строка пустая или Null, вернет 0)
        /// на входе:  (string) "4.5" "4,5"
        /// на выходе: (double) 4.5 (в системном представлении)
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static double ToDouble(this string t)
        {
            double result = 0;
            IFormatProvider formatter = new NumberFormatInfo { NumberDecimalSeparator = "," };

            string s = t.ToString();

            if(!string.IsNullOrEmpty(s))
            {
                s = s.Replace(" ", "");
                if( s.IndexOf(".") > -1 )
                {
                    s=s.Replace(".",",");
                }

                double r=0;
                var parseResult = double.TryParse(s, NumberStyles.Number, formatter, out r );
                if( parseResult )
                {
                    if(r != 0)
                    {
                        result=r;
                    }
                }
            }

            return result;
        }


        /// <summary>
        /// преобразует строку в представление даты и времени
        /// (если строка пустая или Null, вернет DateTime.MinValue)
        /// на входе:  (string) "10.12.2020" (в указанном формате)
        /// на выходе: (DateTime) (в системном представлении)
        /// для преобразования из заданного формата нужно передать f
        /// dd.MM.yyyy HH:mm:ss
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static DateTime ToDateTime(this string t, string f="")
        {
            DateTime result = DateTime.MinValue;
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            var style = System.Globalization.DateTimeStyles.None;

             /*
                типовые форматы
                                C#                          Oracle DB
                --------------  --------------------------  -------------------
                2--date         dd.MM.yyyy                  dd.mm.yy
                3--datetime     dd.MM.yyyy HH:mm:ss         dd.mm.yy hh24:mi:ss
                4--datetimehm   dd.MM.yyyy HH:mm            dd.mm.yy hh24:mi
                5--dateshorthm  dd.MM HH:mm                 dd.mm hh24:mi
                6--dateshort    dd.MM                       dd.mm 
             */
            string[] fs = 
            { 
                "dd.MM.yyyy", 
                "dd.MM.yyyy HH:mm:ss",
                "dd.MM.yyyy HH:mm",
                "dd.MM HH:mm",
                "dd.MM",
            };

            int mode=1;
            if( string.IsNullOrEmpty(f) )
            {
                mode=2;
            }

            string s = t.ToString();

            if(!string.IsNullOrEmpty(s))
            {
                DateTime r=DateTime.MinValue;

                var parseResult=false;
                if( mode==1 )
                {
                    parseResult = DateTime.TryParseExact(s, f, culture, style, out r );

                }else if( mode==2 )
                {
                    parseResult = DateTime.TryParseExact(s, fs, culture, style, out r );

                }
                
                
                if( parseResult )
                {
                    result=r;
                }
            }

            return result;
        }

        /// <summary>
        /// преобразует строку в представление даты и времени
        /// (если строка пустая или Null, вернет DateTime.MinValue)
        /// на входе:  (string) "10.12.2020" (в одном из типовых форматов)
        /// на выходе: (DateTime) (в системном представлении)
        /// пытается подобрать формат из списка типовых
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static DateTime ToDateTime(this string t )
        {
            DateTime result = DateTime.MinValue;

            if (t != null)
            {

                var culture = System.Globalization.CultureInfo.InvariantCulture;
                var style = System.Globalization.DateTimeStyles.None;

                /*
                    типовые форматы
                                    C#                          Oracle DB
                    --------------  --------------------------  -------------------
                    2--date         dd.MM.yyyy                   dd.mm.yy
                    3--datetime     dd.MM.yyyy HH:mm:ss         dd.mm.yy hh24:mi:ss
                    4--datetimehm   dd.MM.yyyy HH:mm            dd.mm.yy hh24:mi
                    5--dateshorthm  dd.MM HH:mm                 dd.mm hh24:mi
                    6--dateshort    dd.MM                       dd.mm 
                 */
                string[] fs =
                {
                    "dd.MM.yyyy",
                    "dd.MM",
                    "MM.yyyy",
                    "HH:mm:ss",
                    "HH:mm",
                    "dd.MM.yyyy HH:mm:ss",
                    "dd.MM.yyyy HH:mm",
                    "dd.MM HH:mm:ss",
                    "dd.MM HH:mm",
                    "MM.yyyy HH:mm:ss",
                    "MM.yyyy HH:mm",
                    "HH:mm:ss dd.MM.yyyy",
                    "HH:mm dd.MM.yyyy",
                    "HH:mm:ss dd.MM",
                    "HH:mm dd.MM",
                    "HH:mm:ss MM.yyyy",
                    "HH:mm MM.yyyy",
                };

                string s = t.ToString();

                if (!string.IsNullOrEmpty(s))
                {
                    DateTime r = DateTime.MinValue;
                    var parseResult = DateTime.TryParseExact(s, fs, culture, style, out r);
                    if (parseResult)
                    {
                        result = r;
                    }
                }
            }

            return result;
        }

        
        /// <summary>
        /// Преобразование строки с кодом цвета в системный тип кисть
        /// используется для работы дизайном строк и колонок в гридах.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Brush ToBrush(this string t)
        {
            Brush result = default(Brush);

            if(t!=null)
            { 
                string s = t.ToString();
                s=s.ToLower();

                if(!string.IsNullOrEmpty(s))
                {
                   var bc = new BrushConverter();
                   result = (Brush)bc.ConvertFrom(s);
                }
            }

            return result;
        }


        /// <summary>
        /// Преобразует первый символ строки в верхний регистр
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string FirstLetterToUpper(this string s)
        {
            string result = "";
            if(!string.IsNullOrEmpty(s))
            {
                result=char.ToUpper(s[0]) + s.Substring(1);
            }
            return result;
        }

        /// <summary>
        /// Преобразует первый символ строки в верхний регистр
        /// (это алиас функции FirstLetterToUpper)
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string ToUpperFirstLetter(this string s)
        {
            return FirstLetterToUpper(s);
        }

        /// <summary>
        /// Приводит имя сотрудника к формату: фамилия + инициалы
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string SurnameInitials(this string s)
        {
            /*
                На входе
                КОНОНОВ НИКОЛАЙ НИКОЛАЕВИЧ
                Кононов Николай Николаевич
                Кононов Ник Ник
                Кононов Ник. Ник.
                Кононов Н.Н. 
                На выходе
                Кононов Н. Н. 
             */
            string result="";
             if( !string.IsNullOrEmpty(s) )
            {
                s=s.Trim();
                s=s.Replace("."," ");
                s=s.Replace(","," ");
                s=s.Replace("   "," ");
                s=s.Replace("  "," ");
                
                string[] e=s.Split(' ');
                if( e.Length > 0 )
                {
                    if( !string.IsNullOrEmpty( e[0] ) )
                    {
                        var w=e[0];
                        w=w.FirstLetterToUpper();
                        result=$"{w}";
                    }
                }
                

                if( e.Length > 1 )
                {
                    if( !string.IsNullOrEmpty( e[1] ) )
                    {
                        var w=e[1];
                        w=w.Substring(0,1);
                        w=w.FirstLetterToUpper();
                        result=$"{result} {w}.";
                    }
                }
               

                if( e.Length > 2 )
                {
                    if( !string.IsNullOrEmpty( e[2] ) )
                    {
                        var w=e[2];
                        w=w.Substring(0,1);
                        w=w.FirstLetterToUpper();
                        result=$"{result} {w}.";
                    }
                }
                
                

            }
            return result;
        }

         /// <summary>
        /// Приводит строку, содержащую номер телефона к установленному формату
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string CellPhone(this string s)
        {
              /*
                На входе
                8 920 736 35
                8 920 736-35-35
                8 (920( 736-35-35
                8 (920( 736-35-35 , 8 (920( 736-35-35
                8920 73635
                892073635
                На выходе
                +79207363535
             */

            string result=s;

            if( !string.IsNullOrEmpty(s) )
            {
                /*
                s=s.Trim();
                s=s.Replace(".","");
                s=s.Replace(",","");
                s=s.Replace("(","");
                s=s.Replace(")","");
                s=s.Replace("+","");
                s=s.Replace("-","");
                s=s.Replace(" ","");
                */

                var phone2=s;
                phone2=phone2.Trim();
                phone2=phone2.Replace(" ","");
                phone2=phone2.Replace("(","");
                phone2=phone2.Replace(")","");
                phone2=phone2.Replace("-","");
                phone2=phone2.Replace("+","");

                if( phone2.Length>1)
                {
                    if(phone2.Substring(0,1) == "/")
                    {
                        phone2=phone2.Replace("/","");
                    }
                }

                                    

                phone2=phone2.Replace(".",",");
                phone2=phone2.Replace("/",",");
                phone2=phone2.Replace(";",",");
                phone2=phone2.Replace(":",",");

                if(phone2.IndexOf(",") > -1)
                {
                    phone2=phone2.CropBefore(",");
                }

                phone2=phone2.Replace(".","");
                phone2=phone2.Replace("/","");
                phone2=phone2.Replace(";","");
                phone2=phone2.Replace(":","");

                if(phone2.Length > 11)
                {
                    var i = phone2.IndexOf("89",11);
                    if(i > -1)
                    {
                        phone2=phone2.Substring(0,i);
                    }
                }

                if( phone2.Length>1 )
                {
                    phone2=phone2.Substring(1,(phone2.Length-1));
                    phone2=$"+7{phone2}";
                }
                
                result=phone2;
            }
            return result;
        }


        /// <summary>
        /// Возвращает подстроку после первого найденного элемента needle
        /// </summary>
        /// <param name="source"></param>
        /// <param name="needle"></param>
        /// <returns></returns>
        public static string CropAfter(this string source, string needle)
        {
            string result="";
            int startIndex = source.IndexOf(needle, StringComparison.Ordinal);
            if(startIndex>-1)
            {
                result=source.Substring(startIndex + 1, source.Length - startIndex - 1);
            }
            else
            {
                result=source;
            }
            return result;
        }

        /// <summary>
        /// Возвращает подстроку после первого найденного элемента needle
        /// Элемент needle может быть многосимвольной строкой
        /// </summary>
        /// <param name="source"></param>
        /// <param name="needle"></param>
        /// <returns></returns>
        public static string CropAfter2(this string source, string needle)
        {
            string result="";
            int startIndex = source.IndexOf(needle, StringComparison.Ordinal);
            int a=needle.Length;
            if(startIndex>-1)
            {
                result=source.Substring(startIndex + a, source.Length - (startIndex + a));
            }
            else
            {
                result=source;
            }
            return result;
        }


        /// <summary>
        /// Возвращает подстроку перед первым найденным элементом needle
        /// </summary>
        /// <param name="source"></param>
        /// <param name="needle"></param>
        /// <returns></returns>
        public static string CropBefore(this string source, string needle)
        {
            string result="";
            int startIndex = source.IndexOf(needle, StringComparison.Ordinal);
            if(startIndex>-1)
            {
                result=source.Substring(0, source.Length - startIndex-2);
            }
            else
            {
                result=source;
            }
            return result;
        }

        /// <summary>
        /// Возвращает подстроку перед первым найденным элементом needle
        /// Элемент needle может быть многосимвольной строкой
        /// </summary>
        /// <param name="source"></param>
        /// <param name="needle"></param>
        /// <returns></returns>
        public static string CropBefore2(this string source, string needle)
        {
            string result="";
            int startIndex = source.IndexOf(needle, StringComparison.Ordinal);
            if(startIndex>-1)
            {
                result=source.Substring(0, startIndex);
            }
            else
            {
                result=source;
            }
            return result;
        }

        /// <summary>
        /// подрезает строку до указанной длины
        /// отрезает в конце строки
        /// </summary>
        /// <param name="value"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public static string Truncate(this string value, int maxLength)
        {
            string result = "";

            if(!value.IsNullOrEmpty())
            {
                if(value.Length <= maxLength)
                {
                    result = value;
                }
                else
                {
                    result = value.Substring(0, maxLength);
                }
            }

            return result;
        }

        /// <summary>
        /// подрезает строку до указанной длины
        /// отрезает в начале строки
        /// </summary>
        /// <param name="value"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public static string TruncateHead(this string value, int maxLength)
        {
            string result = "";

            if(!value.IsNullOrEmpty())
            {
                if(value.Length <= maxLength)
                {
                    result = value;
                }
                else
                {
                    var len = value.Length;
                    var start = len - maxLength;
                    result = value.Substring(start, maxLength);
                }
            }

            return result;
        }


        /// <summary>
        /// специальная функция, возвращает упроценный репорт об ошибке OracleDB
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string GetSimpleOracleException(this string s)
        {
            /*
             Oracle.ManagedDataAccess.Client.OracleException(0x80004005): ORA - 02292: нарушено ограничение целостности(GOFRA5.PMTS_PMTA_FK) - обнаружена порожденная запись
             at OracleInternal.ServiceObjects.OracleConnectionImpl.VerifyExecution(Int32 & cursorId, Boolean bThrowArrayBindRelatedErrors, SqlStatementType sqlStatementType, Int32 arrayBindCount, OracleExcepti
             Этот вывод не очень информативен, порежем его
             */
            s = s.Replace("\r", "");
            s = s.Replace("\n", "");
            s = s.CropAfter("):");
            s = s.CropBefore("at");

            return s;
        }

         /// <summary>
        /// Возвращает текстовый блок укзазанной длины, помещает входную строку в начало блока.
        /// Работает аналогично SPadLeft, упрощает работу за счет дополнительных проверок
        /// </summary>
        /// <param name="s"></param>
        /// <param name="l"></param>
        /// <returns></returns>
        public static string SPadLeft(this string s, int l)
        {
            var result="";
            result=$"{s}";
            if(result.Length<l)
            {
                result=$"{result}                    ";
            }
            if(result.Length>l)
            {
                result=result.Substring(0,l);
            }
            result=result.PadLeft(l);
            return result;
        }

        /// <summary>
        /// Возвращает текстовый блок укзазанной длины, помещает входную строку в конец блока.
        /// Работает аналогично PadRight, упрощает работу за счет дополнительных проверок.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="l"></param>
        /// <returns></returns>
        public static string SPadRight(this string s, int l)
        {
            var result="";
            result=$"{s}";
            if(result.Length<l)
            {
                result=$"{result}                    ";
            }
            if(result.Length>l)
            {
                result=result.Substring(0,l);
            }
            result=result.PadRight(l);
            return result;
        }

        /// <summary>
        /// проверка строки на пустое значение
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsNullOrEmpty(this string s)
        {
            return string.IsNullOrEmpty(s);
        }

         public static string AddComma(this string s)
        {
            string result=s;
            if (!result.IsNullOrEmpty())
            {
                result=$"{result},";
            }
            return result;
        }

        public static string Append(this string s, string text, bool addCr=false, int offset=0)
        {
            string result=s;
            if(!text.IsNullOrEmpty())
            {
                if(addCr)
                {
                    result=$"{result}\n";
                }
                var o="";
                if(offset>0)
                {
                    for(int i=0; i<=offset; i++)
                    {
                        o=$"{o}    ";
                    }
                }
                result=$"{result}{o}{text}";
            }
            return result;
        }

        public static string Append(this string s, string text, bool addCr=false)
        {
            string result=s;
            if(!text.IsNullOrEmpty())
            {
                if(addCr)
                {
                    result=$"{result}\n";
                }
                result=$"{result}{text}";
            }
            return result;
        }

        public static string Append(this string s, string text)
        {
            string result=s;
            if(!text.IsNullOrEmpty())
            {
                result=$"{result}{text}";
            }
            return result;
        }

        /// <summary>
        /// добавит к строке перевод строки
        /// (если строка не пустая, вставит перевод)
        /// для человекочитаемых логов
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string AddCR(this string s)
        {
            string result=s;
            if (!result.IsNullOrEmpty())
            {
                result=$"{result}\n";
            }
            return result;
        }

        public static string AddEtc(this string s, string separator=",")
        {
            string result = s;
            if(!result.IsNullOrEmpty())
            {
                result = $"{result}{separator}";
            }
            return result;
        }

        /// <summary>
        /// добавит к строке символ RS        
        /// для машиночитаемых логов
        /// (по этому символу машины будут разбивать строки при анализе, 
        /// занчение может быть многострочным)
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string AddRS(this string s)
        {
            string result=s;
            {
                //30 RS RecordSeparator
                var c=Convert.ToChar(30);
                result=$"{result}{c}";
            }
            return result;
        }

        /// <summary>
        /// добавляет к строке указанное количество символов
        /// </summary>
        /// <param name="s"></param>
        /// <param name="quantity"></param>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public static string AddSymbols(this string s, int quantity=4, string symbol=" ")
        {
            string result=s;
            var o="";
            for(int i=0; i<quantity; i++)
            {
                o=$"{o}{symbol}";
            }
            if(!o.IsNullOrEmpty())
            {
                result=$"{result}{o}";
            }
            return result;
        }


        /// <summary>
        /// перевод слова в нижний регистр и очистка от лишних символов
        /// snake case: task_create
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string ClearCommand(this string s)
        {
            string result="";
            if(!s.IsNullOrEmpty())
            {
                s=s.ToLower();
                s=s.Trim();
            }
            result=s;
            return result;
        }

        /// <summary>
        /// безопасное имя
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string MakeSafeName(this string s)
        {
            string result="";
            if(!s.IsNullOrEmpty())
            {
                s=s.ToLower();
                s=s.Trim();
                s=s.Replace("--","_");
                s=s.Replace("-","_");
                s=s.Replace(" ","_");
                s=s.Replace("~","_");
                s=s.Replace(" ", "_");
                s=s.Replace(",", "");
                s=s.Replace(".", "");
                s=s.Replace(";", "");
            }
            result=s;
            return result;
        }
        
        /// <summary>
        /// если строка длиннее лимита, подрезка с конца
        /// </summary>
        /// <param name="text"></param>
        /// <param name="symbols"></param>
        /// <returns></returns>
        public static string Crop(this string text, int symbols=1000)
        {
            try
            {
                var l1=text.Length;
                var l=symbols;
                if(l1 > l )
                {
                    var l2=Math.Abs(l1-l);
                    text=text.Substring(l2,l);
                }
            }
            catch(Exception e)
            {
            }

            return text;
        }

        /// <summary>
        /// Удаляет строку в конце строки, аналогично стандартной Rtrim(char ch)
        /// Взято от сюда https://stackoverflow.com/questions/7170909/trim-string-from-the-end-of-a-string-in-net-why-is-this-missing
        /// </summary>
        /// <param name="input"></param>
        /// <param name="suffixToRemove"></param>
        /// <param name="comparisonType"></param>
        /// <returns></returns>
        public static string TrimEnd(this string input, string suffixToRemove, StringComparison comparisonType = StringComparison.CurrentCulture)
        {
            if (suffixToRemove != null && input.EndsWith(suffixToRemove, comparisonType))
            {
                return input.Substring(0, input.Length - suffixToRemove.Length);
            }

            return input;
        }


    }
}
