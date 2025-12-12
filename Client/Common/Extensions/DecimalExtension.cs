using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Common.Extensions
{
    public static class DecimalExtension
    {
        public static decimal ToDecimal(this string s)
        {
            try
            {
                return decimal.Parse(
                    s.Replace(",", CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator),
                    CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0;
            }
        }
    }
}
