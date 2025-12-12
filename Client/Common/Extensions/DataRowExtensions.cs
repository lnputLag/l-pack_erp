using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Common
{
    /// <summary>
    /// Работа с DataRow
    /// 
    /// </summary>
    /// <author>eletskikh_ya</author>
    public static class DataRowExtensions
    {
        public static string CheckGet(this DataRow d, string key)
        {
            string result = "";
            if (d != null)
            {
                if (!string.IsNullOrEmpty(key))
                {
                    if (d.Table.Columns.Contains(key))
                    {
                        if (d[key] != null)
                        {
                            result = d[key].ToString();
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// проверяет наличие элемента массива с указанным ключом
        /// если элемент не существует, создает, далее устанавливает его значение
        /// </summary>
        /// <param name="d"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void CheckAdd<T>(this DataRow d, string key, T value = default)
        {
            if (d != null)
            {
                if (!string.IsNullOrEmpty(key))
                {
                    if (d.Table.Columns.Contains(key))
                    {
                        d[key] = value;
                    }
                }
            }
        }

        public static Dictionary<string, string> ToDictionary(this DataRow d)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            if (d != null)
            {
                foreach(DataColumn c in d.Table.Columns)
                {
                    if (d[c.ColumnName]!=null)
                    {
                        result[c.ColumnName] = d[c.ColumnName].ToString();
                    }
                    else
                    {
                        d[c.ColumnName] = string.Empty;
                    }
                }
            }

            return result;
        }
    }
}
