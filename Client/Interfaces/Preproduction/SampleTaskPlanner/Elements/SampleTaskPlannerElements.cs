using Client.Common;
using System.Collections.Generic;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Перечисления и вспомогательные методы планирования и изготовления образцов
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public static class SampleTaskPlannerElements
    {
        /// <summary>
        /// Операции для изготовления образца
        /// </summary>
        public static Dictionary<string, string> SampleOperation = new Dictionary<string, string>()
        {
            { "1", "Резка" },
            { "2", "Проминание" },
            { "3", "Сборка" },
            { "4", "Сшивка" },
            { "5", "Склейка" },
            { "6", "Контроль качества" },
            { "7", "Упаковка" }
        };

        /// <summary>
        /// Определение группы изделий по классу изделий
        /// </summary>
        /// <param name="pClass">Код класса изделий</param>
        /// <returns>Код группы изделий</returns>
        public static int SampleProductGroup(int pClass)
        {
            int gr = 0;
            // 4-клапанная коробка и лист с просечками
            if (pClass.ContainsIn(2, 3, 4, 107, 108, 109, 113, 114, 115, 116, 217))
            {
                gr = 1;
            }
            // ИСВ
            else if (pClass.ContainsIn(10))
            {
                gr = 2;
            }
            // обечайка
            else if (pClass.ContainsIn(120))
            {
                gr = 3;
            }
            // решетки
            else if (pClass.ContainsIn(8, 12))
            {
                gr = 4;
            }
            // лист
            else if (pClass.ContainsIn(1, 122))
            {
                gr = 5;
            }

            return gr;
        }

        /// <summary>
        /// Время этапа изготовления образца
        /// </summary>
        public static Dictionary<string, int> SampleOperationTime = new Dictionary<string, int>()
        {
            // 4-клапанная коробка
            { "11", 120 },
            { "12", 110 },
            { "13", 0 },
            { "14", 0 },
            { "15", 60 },
            { "16", 60 },
            { "17", 70 },
            // ИСВ
            { "21", 390 },
            { "22", 260 },
            { "23", 0 },
            { "24", 0 },
            { "25", 130 },
            { "26", 70 },
            { "27", 240 },
            // обечайка
            { "31", 120 },
            { "32", 100 },
            { "33", 0 },
            { "34", 0 },
            { "35", 70 },
            { "36", 70 },
            { "37", 140 },
            // решетки
            { "41", 90 },
            { "42", 0 },
            { "43", 0 },
            { "44", 0 },
            { "45", 0 },
            { "46", 30 },
            { "47", 50 },
            // лист
            { "51", 100 },
            { "52", 0 },
            { "53", 0 },
            { "54", 0 },
            { "55", 0 },
            { "56", 30 },
            { "57", 120 },
        };

        /// <summary>
        /// Возвращает время операции изготовления образца
        /// </summary>
        /// <param name="group">код группы изделий</param>
        /// <param name="oper">код операции</param>
        /// <returns>Время в секундах</returns>
        public static int GetOperationTime(int group, int oper)
        {
            string k = $"{group}{oper}";
            int r = 0;
            if (SampleOperationTime.ContainsKey(k))
            {
                r = SampleOperationTime[k];
            }
            return r;
        }
    }
}
