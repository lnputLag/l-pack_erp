/************************************************************************
 * Copyright: 
 *
 * License:  
 *
 * Author:   DM
 *
 ************************************************************************/

using System.Collections.Generic;

namespace CodeReason.Reports
{
    /// <summary>
    /// </summary>
    public class ReportHelper
    {
        
        /// <summary>
        /// Хелпер помогает расставить стили, опираясь на логические значения переменных.
        /// Нужен для раскраски ячеек красным\зеленым, для значений истина\ложь
        /// </summary>
        /// <param name="values"></param>
        /// <param name="valueKey"></param>
        /// <param name="conditionKey"></param>
        /// <param name="styles"></param>
        /// <param name="styleKey"></param>
        /// <returns></returns>
        public static Dictionary<string, string> ProcRedGreenCondition( Dictionary<string, string> values, string valueKey, string conditionKey, Dictionary<string, string> styles, string styleKey )
        {

            if( !styles.ContainsKey( styleKey ) )
            {
                styles.Add(styleKey,"");
            }
            
            string style="";

            /*
                Logic:

                if( !string.IsNullOrEmpty( v["ProductionTaskDate"].ToString() ) )
                { 
                    if( v["ProductionTaskDateExpired"] == "1" )
                    {
                        cellStyles["ProductionTaskDate"]="TableCellRed";
                    }
                    else
                    {
                        cellStyles["ProductionTaskDate"]="TableCellGreen";
                    }
                }
             */

            if( values.Count > 0 )
            {
                if( values.ContainsKey(valueKey) && values.ContainsKey(conditionKey) )
                {
                    if(  !string.IsNullOrEmpty( values[valueKey] ) )
                    {
                        if( (string)values[conditionKey] == "1" )
                        {
                            style="TableCellRed";
                        }
                        else
                        {
                            style="TableCellGreen";
                        }
                    }
                }
            }

            styles[styleKey]=style;
            return styles;            

        }

    }
}
