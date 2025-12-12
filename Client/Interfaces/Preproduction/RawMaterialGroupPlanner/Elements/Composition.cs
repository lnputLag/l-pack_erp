using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Класс Композиция для хранения данных
    /// </summary>
    /// <author>sviridov_ae</author>
    public class Composition
    {
        /// <summary>
        /// Конструктор для класса Композиция
        /// </summary>
        public Composition()
        {
            PaperList = new Dictionary<string, string>(); 
        }

        /// <summary>
        /// Профиль картона (например B)
        /// </summary>
        public string Profile { get; set; }

        /// <summary>
        /// Марка картона (например 22)
        /// </summary>
        public string Brand { get; set; }

        /// <summary>
        /// Цвет картона (например Бурый)
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// Ид картона
        /// </summary>
        public int IDC { get; set; }

        /// <summary>
        /// Словарь с данными по ID_RAW_GROUP и FACTOR
        /// </summary>
        public Dictionary<string, string> PaperList { get; set; }

        /// <summary>
        /// Дата
        /// </summary>
        public string Dt { get; set; }
    }
}
