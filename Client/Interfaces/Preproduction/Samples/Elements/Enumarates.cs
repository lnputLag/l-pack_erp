using Client.Common;
using System.Collections.Generic;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Состояние разработки чертежа для образца
    /// </summary>
    /// <author>Рясной П.В.</author>
    public static class SampleDesignTypes
    {
        /// <summary>
        /// Список состояний разработки чертежа
        /// </summary>
        public static Dictionary<string, string> Items = new Dictionary<string, string>()
        {
            { "0", "Не нужен" },
            { "1", "В разработке" },
            { "2", "Выполнен" },

        };
        public static readonly int NotRequired = 0;
        public static readonly int InDesign = 1;
        public static readonly int Performed = 2;
    }

    /// <summary>
    /// Типы доставки образца заказчику
    /// </summary>
    public static class DeliveryTypes
    {
        /// <summary>
        /// Список типов доставки образца
        /// </summary>
        public static Dictionary<string, string> Items = new Dictionary<string, string>()
        {
            { "0", "С отгрузкой" },
            { "1", "Самовывоз из Липецк. офиса" },
            { "2", "Самовывоз из Москов. офиса" },
            { "3", "Доставка трасп. компанией" },
            { "4", "Региональный представитель" },
            { "5", "Со склада Каширы" },
        };
        /// <summary>
        /// С отгрузкой (0)
        /// </summary>
        public static readonly int Shipment = 0;
        /// <summary>
        /// Самовывоз из Липецкого офиса (1)
        /// </summary>
        public static readonly int FromLipetsk = 1;
        /// <summary>
        /// Самовывоз из Московского офиса (2)
        /// </summary>
        public static readonly int FromMoscow = 2;
        /// <summary>
        /// Доставка трасп. компанией (3)
        /// </summary>
        public static readonly int Delivery = 3;
        /// <summary>
        /// Региональный представитель (4)
        /// </summary>
        public static readonly int Representative = 4;
        /// <summary>
        /// С отгрузкой из Каширы (5)
        /// </summary>
        public static readonly int ShipmentKashira = 5;

        /// <summary>
        /// Добавление выбора всех типов для списка фильтрации
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, string> ExtendItems()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                { "-1", "Все"}
            };
            result.AddRange(Items);

            return result;
        }
    }

    /// <summary>
    /// Тип изготовления образца
    /// </summary>
    public static class ProductionTypes
    {
        public static readonly int PlotterType = 0;
        public static readonly int ProductType = 1;
    }

    /// <summary>
    /// Статусы образцов
    /// </summary>
    public static class SampleStates
    {
        /// <summary>
        /// Новый (0)
        /// </summary>
        public static readonly int New = 0;
        /// <summary>
        /// В работе (1)
        /// </summary>
        public static readonly int InWork = 1;
        /// <summary>
        /// Отклонен (2)
        /// </summary>
        public static readonly int Rejected = 2;
        /// <summary>
        /// Изготовлен (3)
        /// </summary>
        public static readonly int Produced = 3;
        /// <summary>
        /// Получен (4)
        /// </summary>
        public static readonly int Received = 4;
        /// <summary>
        /// Отгружен (5)
        /// </summary>
        public static readonly int Shipped = 5;
        /// <summary>
        /// Утилизирован (6)
        /// </summary>
        public static readonly int Utilized = 6;
        /// <summary>
        /// Передан (7)
        /// </summary>
        public static readonly int Transferred = 7;
        /// <summary>
        /// Приемка (8)
        /// </summary>
        public static readonly int Acceptance = 8;
    }

    /// <summary>
    /// Типы упаковки образца
    /// </summary>
    public static class PackingTypes
    {
        /// <summary>
        /// Список типов упаковки образца
        /// </summary>
        public static Dictionary<string, string> Items = new Dictionary<string, string>()
        {
            { "0", "Без упаковки" },
            { "1", "В пленке" },
            { "2", "В картоне" },
            { "3", "В картоне и пленке" },

        };
        public static readonly int NoPacking = 0;
        public static readonly int Stretch = 1;
        public static readonly int Cardboard = 2;
    }

    /// <summary>
    /// Виды подтверждений для ограничений по количеству образцов в заказе
    /// </summary>
    public static class SampleConfirmationReasons
    {
        /// <summary>
        /// Список видов подтверждений
        /// </summary>
        public static Dictionary<string, string> Items = new Dictionary<string, string>()
        {
            { "0", "" },
            { "1", "Крупный клиент" },
            { "2", "Согласовано по цене" },
            { "3", "Согласовано с Васильевым А.В." },
            { "4", "Решение менеджера" },
        };
    }
}
