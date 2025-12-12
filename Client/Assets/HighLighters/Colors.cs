namespace Client.Assets.HighLighters
{

    /// <summary>
    /// 
    /// </summary>
    public static class HColor
    {
        /*
            �����������:
            ��������� ����������
            https://cssgradient.io/
         */

        public const string
            Blue                = "#FFA9D4FF",
            BlueDark            = "#0000ff",
            Green               = "#FFC5FBC5",
            Red                 = "#FFFFA0A0",
            Yellow              = "#FFFFFF99",
            YellowDark          = "#f6ff00",
            Orange              = "#FFFFC182",
            YellowOrange        = "#FFffe28e",            
            OrangeOrange        = "#ffffa598",  
            Pink                = "#ffc0cb",
            PinkOrange          = "#ffff88ad",               
            VioletPink          = "#ffff98e2",
            Violet              = "#FFFFB3FF",
            Brown               = "#ffD5A657",
            VioletDark          = "#FFc791c7",
            Olive               = "#FFDCD475",
            Gray                = "#FFdedede",
            GrayDeep            = "#5e594a",
            White               = "#ffffffff",

            BlueFg              = "#ff0000ff",
            ErrorFG             = "#FFEC0012",
            NoteFG              = "#FF000000",
            BlueFG              = "#FF0055ff",
            GreenFG             = "#FF00ab46",
            RedFG               = "#FFfc0d0d",
            BlackFG             = "#FF000000",
            RedAccented         = "#FFFF6665",
            LightSelection      = "#FFC9DEF5",
            GreenAccented       = "#e6bffbbf",
            MagentaFG           = "#FFFF00FF",
            OliveFG             = "#FFA99A00",
            GrayFG              = "#FF999999",
            OrangeFG            = "#FFFF7F0A";

        public const string
            FieldBorderNormal   = "#FFcccccc",
            FieldBorderInvalid  = "#9eff6666";

        
        /// <summary>
        /// ����������� ����, ������� �����-������������: #AARRGGBB -> #RRGGBB
        /// </summary>
        public static string ToHexRGB(string s)
        {
            string result="";

            if( !string.IsNullOrEmpty(s))
            {
                /*
                     #AARRGGBB
                     012345678
                 */
                
                var l=s.Length;
                result=s.Substring( 3, l-3);
                result=$"#{result}";
            }

            return result;
        }
    }
}
