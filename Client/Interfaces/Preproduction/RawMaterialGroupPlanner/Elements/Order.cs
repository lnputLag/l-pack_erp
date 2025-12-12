using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Класс Заявка для хранения данных
    /// </summary>
    /// <author>sviridov_ae</author>
    public class Order
    {
        /// <summary>
        /// Конструктор для класса Заявка
        /// </summary>
        public Order()
        {
            DicOfTrimWidth = new Dictionary<string, string>();
            DicOfTrimWidth.Add("1900", "");
            DicOfTrimWidth.Add("2000", "");
            DicOfTrimWidth.Add("2100", "");
            DicOfTrimWidth.Add("2200", "");
            DicOfTrimWidth.Add("2300", "");
            DicOfTrimWidth.Add("2400", "");
            DicOfTrimWidth.Add("2500", "");
            DicOfTrimWidth.Add("2700", "");
            DicOfTrimWidth.Add("2800", "");
        }

        /// <summary>
        /// Дата
        /// </summary>
        public string DT { get; set; }

        /// <summary>
        /// ИД картона
        /// </summary>
        public int IDC { get; set; }

        /// <summary>
        /// Количество картона в заявке (шт?)
        /// </summary>
        public int QtyOrder { get; set; }

        /// <summary>
        /// Ширина заготовки
        /// </summary>
        public int BlankWidth { get; set; }

        /// <summary>
        /// Длина заготовки
        /// </summary>
        public int BlankLength { get; set; }

        /// <summary>
        /// Ширина обрези
        /// </summary>
        public int TrimWidth { get; set; }

        /// <summary>
        /// Длина обрези
        /// </summary>
        public int TrimLength { get; set; }

        /// <summary>
        /// Коэффициент по ширине обрези
        /// </summary>
        public double FactorTrimWidth { get; set; }

        /// <summary>
        /// Количество квадратных метров
        /// </summary>
        public double QtySqrMetr { get; set; }

        /// <summary>
        /// Словарь с форматами обрези (шириной) и коэффициентами
        /// </summary>
        public Dictionary<string, string> DicOfTrimWidth { get; set; }

        /// <summary>
        /// Идентификатор заявки (idorderdates)
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Внешний номер заявки
        /// </summary>
        public string OrderNumber { get; set; }

        /// <summary>
        /// Покупатель
        /// </summary>
        public string BuyerName { get; set; }

        /// <summary>
        /// Наименование продукции
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        /// Артикул продукции
        /// </summary>
        public string ProductCode { get; set; }

        /// <summary>
        /// Наименование картона
        /// </summary>
        public string CardboardName { get; set; }
    }
}
