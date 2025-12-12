using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Preproduction
{
    public class Carton
    {
        public Carton()
        {
            DicOFFactorByRawGroup = new Dictionary<int, double>();
            ListOfOrderData = new List<Dictionary<string, string>>();
        }

        /// <summary>
        /// Дата (дата заявок)
        /// </summary>
        public string Dt { get; set; }

        /// <summary>
        /// Список с данными по сырьевым группам и коэффициентам 
        /// для преобразования метража этого картона в вес сырьевых групп, которые используются для его производства
        /// </summary>
        public Dictionary<int, double> DicOFFactorByRawGroup { get; set; }

        /// <summary>
        /// Колличество квадратных метров картона (по сумме заявок)
        /// </summary>
        public double QtySqrMetr { get; set; }

        /// <summary>
        /// Ид картона
        /// </summary>
        public int Idc { get; set; }

        /// <summary>
        /// Ширина обрези (ширина)
        /// </summary>
        public int TrimWidth { get; set; }

        /// <summary>
        /// Лист с данными по заявкам на этот картон
        /// </summary>
        public List<Dictionary<string, string>> ListOfOrderData { get; set; }
    }
}
