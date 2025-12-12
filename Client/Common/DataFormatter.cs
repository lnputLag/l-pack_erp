namespace Client.Common
{
    /// <summary>
    /// форматтеры данных
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    public static class DataFormatter
    {
        /// <summary>
        /// приводит строку с номером телефона к стандартному виду
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string CellPhone(string value)
        {
            string result="";

            if(value!=null)
            {
                var s=value.ToString();
                if( !string.IsNullOrEmpty(s) )
                {
                    s=s.Replace(" ","");
                    s=s.Replace("-","");
                    s=s.Replace("+","");

                    if( s.Substring(0,1)=="8" )
                    {
                        s=$"7{s.Substring(1,s.Length-1)}";
                    }

                    s=$"+{s}";
                    result=s;
                }
            }

            return result;
        }

    }
}
