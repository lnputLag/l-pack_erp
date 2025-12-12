using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Источники поступления поддонов
    /// </summary>
    public static class PalletSourceTypes
    {
        public static Dictionary<string, string> Items = new Dictionary<string, string>()
        {
            { "1", "Поставка" },
            { "2", "Изготовление" },
            { "3", "Возврат" },
            { "4", "Перемещение с ГА" },
            { "5", "Корректировка остатка" },
            { "6", "Давальческие" },
        };
        /// <summary>
        /// Расширенный список значений, добавлена строка для выбора всех значений
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, string> ExtendItems()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                { "-1", "Все источники"}
            };
            result.AddRange(Items);

            return result;
        }
        /// <summary>
        /// Поставка
        /// </summary>
        public static readonly int Purchase = 1;
        /// <summary>
        /// Изготовление
        /// </summary>
        public static readonly int Manufacture = 2;
        /// <summary>
        /// Возврат
        /// </summary>
        public static readonly int Return = 3;
        /// <summary>
        /// Перемещение с ГА
        /// </summary>
        public static readonly int Transfer = 4;
        /// <summary>
        /// Корректировка
        /// </summary>
        public static readonly int Correction = 5;
        /// <summary>
        /// Давальческие
        /// </summary>
        public static readonly int Giving = 6;
    }

    /// <summary>
    /// Статусы накладной прихода поддонов
    /// </summary>
    public static class PalletReceiptStatus
    {
        public static Dictionary<string, string> Items = new Dictionary<string, string>()
        {
            { "0", "На приемке" },
            { "1", "Приняты" },
            { "2", "Не приняты" },
        };
        public static KeyValuePair<string, string> IncomingItem = new KeyValuePair<string, string>("0", "На приемке");
        public static KeyValuePair<string, string> AcceptedItem = new KeyValuePair<string, string>("1", "Приняты");
        /// <summary>
        /// На приемке
        /// </summary>
        public static readonly int Incoming = 0;
        /// <summary>
        /// Приняты
        /// </summary>
        public static readonly int Accepted = 1;
        /// <summary>
        /// Не приняты
        /// </summary>
        public static readonly int Rejected = 2;
    }
}
