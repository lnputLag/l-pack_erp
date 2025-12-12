using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Common
{
    /// <summary>
    /// вспомогательные функции для валидаии полей
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    class FormFieldsUtil
    {
        
        /// <summary>
        /// фильтр: проверяет, является ли значение целочисленным (только цифры)
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static Boolean IsInputIntegerOnly(String text) 
        { 
            foreach (Char c in text.ToCharArray()) 
            { 
                if (Char.IsDigit(c) || Char.IsControl(c)){
                    continue; 
                }
                else
                {
                    return false; 
                }
            } 
            return true; 
        }

        /// <summary>
        /// фильтр: проверяет, является ли значение числом с плавающей запятой (цифры и запятая)
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static Boolean IsInputDoubleOnly(String text) 
        { 
            bool result=true;

            //проверим, что содержатся только цифры и запятая
            if( result )
            {
                foreach (Char c in text.ToCharArray()) 
                {                
                    if (Char.IsDigit(c) || Char.IsControl(c) || c==',' ){
                        continue; 
                    }
                    else
                    {                    
                        result=false;
                    }
                }     
            }

            //проверим, что только 1 запятая
            if( result)
            {
                var rr=text.IndexOf(",");
                if( text.IndexOf(",") > 1 )
                {
                    result=false;
                }
            }
                    
            return result; 
        }


        /// <summary>
        /// фильтрует строку, на выход пропускает только число с плавающей запятой
        /// точки преобразует в запятые
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static String FilterInputDoubleOnly(String text) 
        { 
            String result="";

            if( !string.IsNullOrEmpty( text ) )
            {
                text=text.Replace(".",",");

                int commaCounter=0;
                foreach (Char c in text.ToCharArray()) 
                {
                    if( Char.IsDigit(c) )
                    {
                        result=$"{result}{c}";
                    }

                    if( c==',' )
                    {
                        commaCounter++;
                        if( commaCounter==1 )
                        {
                            result=$"{result}{c}";
                        }
                    }
                }
            }

            return result; 
        }

        public static String FilterInputIntegerOnly(String text) 
        { 
            String result="";

            if( !string.IsNullOrEmpty( text ) )
            {
                int commaCounter=0;
                foreach (Char c in text.ToCharArray()) 
                {
                    if( Char.IsDigit(c) )
                    {
                        result=$"{result}{c}";
                    }
                }
            }

            return result; 
        }
      
         /// <summary>
        /// фильтрует строку, на выход пропускает только дату в заданном формате:
        /// dd.MM.yyyy
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static String FilterInputDate(String text) 
        { 
            String result="";

            if( !string.IsNullOrEmpty( text ) )
            {
                text=text.Replace(",",".");
                text=text.Replace(" ","");
                text=text.Replace("-","");
                text=text.Replace("/","");

                int commaCounter=0;
                foreach (Char c in text.ToCharArray()) 
                {
                    if( Char.IsDigit(c) )
                    {
                        result=$"{result}{c}";
                    }

                    if( c=='.' )
                    {
                        commaCounter++;
                        if( commaCounter <= 2 )
                        {
                            result=$"{result}{c}";
                        }
                    }
                }
            }

            if( !string.IsNullOrEmpty( result ) )
            {
                
                //if( result.IndexOf(".") > -1 ) 
                if( result.Length >= 2 )
                {
                    result=result.Replace(".","");
                    /*
                        12.06.2020
                        0123456789
                        12 06 2020
                        01 23 4567
                     */
                    var d="";
                    var m="";
                    var l="";
                    var r="";

                    l=result;

                    if( result.Length > 2 )
                    {
                        d=result.Substring(0,2);
                        l=result.Substring(2,(result.Length-2));
                    }

                    if( result.Length > 4 )
                    {
                        m=result.Substring(2,2);
                        l=result.Substring(4,(result.Length-4));
                    }

                    r=$"{l}";

                    if( !string.IsNullOrEmpty( d ) )
                    {
                        r=$"{d}.{l}";
                    }
                    if( !string.IsNullOrEmpty( m ) )
                    {
                        r=$"{d}.{m}.{l}";
                    }
                    
                    //Central.Dbg($" d=[{d}] m=[{m}] y=[{y}] r=[{r}]");                    

                    result=r;
                }
            }

            /*
            if( !string.IsNullOrEmpty( result ) )
            {
                // 99.99.9999 
                var dateRegex = new Regex(@"^(?:(?:31(\.)(?:0?[13578]|1[02]))\1|(?:(?:29|30)(\.)(?:0?[1,3-9]|1[0-2])\2))(?:(?:1[6-9]|[2-9]\d)?\d{2})$|^(?:29(\.)0?2\3(?:(?:(?:1[6-9]|[2-9]\d)?(?:0[48]|[2468][048]|[13579][26])|(?:(?:16|[2468][048]|[3579][26])00))))$|^(?:0?[1-9]|1\d|2[0-8])(\.)(?:(?:0?[1-9])|(?:1[0-2]))\4(?:(?:1[6-9]|[2-9]\d)?\d{4})$");
                var match = dateRegex.Match( result );
                if( match.Success )
                {
                    try {
                        //Central.Dbg($"Match: [{result}]");
                        var dt = DateTime.ParseExact(  result, "dd.MM.yyyy", CultureInfo.InvariantCulture);                    
                        result=dt.ToString("dd.MM.yyyy");
                    }
                    catch( Exception e )
                    {

                    }
                }
            }
            */
            

            return result; 
        }


         /// <summary>
        /// фильтрует строку, на выход пропускает только дату в заданном формате:
        /// +79156317092 +375297876989
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static String FilterInputCellPhone(String text) 
        { 
            /*
                +79156317092 
                +375297876989
                 123456789012
                 11 или 12 символов (без учета знака +)
             */
            
            String result="";

            if( !string.IsNullOrEmpty( text ) )
            {

                foreach (Char c in text.ToCharArray()) 
                {
                    if( Char.IsDigit(c) || c=='+' )
                    {
                        result=$"{result}{c}";
                    }
                }

            }

            return result; 
        }



        /// <summary>
        /// фильтр ввода: пропускает только разрешенные символы для вставки
        /// срабатывает при отпускании клавиши
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void KeyUpHandler(object sender,KeyEventArgs e, string filter="doubleonly")
        {
            if(e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up || e.Key == Key.Down)
            {
                return;
            }

            var o = (TextBox)sender;
            String text = (String)o.Text;
            var ci = o.CaretIndex;

            var text2 = "";

            filter=filter.ToLower();
            switch( filter )
            {
                case "doubleonly":
                    text2 = FormFieldsUtil.FilterInputDoubleOnly(text);
                    break;

                case "date":
                    text2 = FormFieldsUtil.FilterInputDate(text);                    
                    break;

                case "cellphone":
                    text2 = FormFieldsUtil.FilterInputCellPhone(text);                    
                    break;
            }
            

            o.Text=text2;
            if(text2.Length != text.Length)
            {
                var c2=ci-1;
                if(c2<0)
                {
                    c2=0;
                }
                o.CaretIndex=c2;
            }
            else if(text2 != text)
            {
                o.CaretIndex=o.Text.Length;
            }

        }


        /// <summary>
        /// фильтр ввода: пропускает только разрешенные символы для вставки
        /// срабатывает при вставке данных из буфера обмена
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void PastingHandler(object sender,DataObjectPastingEventArgs e,string filter="doubleonly")
        {
            var o = (TextBox)sender;
            string text = (string)o.Text;

            filter=filter.ToLower();
            switch( filter )
            {
                case "doubleonly":
                    text = FormFieldsUtil.FilterInputDoubleOnly(text);
                    break;

                case "date":
                    text = FormFieldsUtil.FilterInputDate(text);                    
                    break;

                case "cellphone":
                    text = FormFieldsUtil.FilterInputCellPhone(text);                    
                    break;
            }

            o.Text=text;
            o.CaretIndex = o.Text.Length;
        }


       
    }
}
