using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Класс для работы с допустимыми процентными отклонениями фактического количества отгруженной продукции к количсетву по заявке
    /// </summary>
    public static class PercentageDeviation
    {
        /// <summary>
        /// Словарь количсетва штук продукции в заказе и допустимым отклонением количества отгруженного в процентах.
        /// Первое значение -- количество определённой позиции в заявке.
        /// Второе значение -- допустимое отклонение по этой позиции.
        /// </summary>
        public static Dictionary<int, int> PercentageDeviationValues = new Dictionary<int, int>()
        {
            { 0, 25 },
            { 500, 20 },
            { 1000, 15 },
            { 3000, 10 },
            { 5000, 8 },
            { 10000, 7 },
        };

        /// <summary>
        /// Проверяем, соответствует ли текущее отклонение допустимому для заданного количества продукции в заявке
        /// </summary>
        /// <param name="quantityInOrder"></param>
        /// <param name="percentageDeviation"></param>
        /// <returns></returns>
        public static bool CheckPercentageDeviation(int quantityInOrder, double percentageDeviation)
        {
            int allowedPercentageDeviation = 0;
            foreach (var item in PercentageDeviationValues)
            {
                if (quantityInOrder > item.Key)
                {
                    allowedPercentageDeviation = item.Value;
                }
            }

            if ((double)allowedPercentageDeviation >= Math.Abs(percentageDeviation))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
