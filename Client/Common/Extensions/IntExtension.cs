using System;
using System.Globalization;
using System.Linq;
using System.Windows.Media;

namespace Client.Common
{
    public static class IntExtension
    {
        /// <summary>
        /// проверка, есть ли данное значение в указанном списке
        /// если есть, возвращает true
        /// </summary>        
        public static bool ContainsIn(this int i, params int[] values)
        {
            /*
                var a=1;
                a.ContainsIn(1,3,5); => true
                a.ContainsIn(2,4,6); => false
             */
            return values.Any(el => el == i);
        }

        public static string ZeroEmpty(this int i)
        {
            string result = "";
            if (i!=0)
            {
                result = i.ToString();
            }
            return result;
        }

        /// Взято отсюда https://stackoverflow.com/questions/2683442/where-can-i-find-the-clamp-function-in-net
        /// <summary>
        /// Возвращает значение в рамках заданных границ
        /// </summary>
        /// <param name="value">Входное значение</param>
        /// <param name="min">Минимальное возвращаемое значение</param>
        /// <param name="max">Максимальное возвращаемое значение</param>
        /// <returns></returns>
        public static T Clamp<T>(this T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0)
            {
                return min;
            }
            else if (value.CompareTo(max) > 0)
            {
                return max;
            }
            else
            {
                return value;
            }
        }
    }
}
