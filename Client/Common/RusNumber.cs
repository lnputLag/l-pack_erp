using System;
using System.Text;

namespace Client.Common.Lib
{
    public class RusNumber
    {
        //Наименования сотен
        private static string[] hunds = {
            "", "сто ", "двести ", "триста ", "четыреста ",
            "пятьсот ", "шестьсот ", "семьсот ", "восемьсот ", "девятьсот "
        };
        //Наименования десятков
        private static string[] tens = {
            "", "десять ", "двадцать ", "тридцать ", "сорок ", "пятьдесят ",
            "шестьдесят ", "семьдесят ", "восемьдесят ", "девяносто "
        };
        /// <summary>
        /// Перевод в строку числа с учётом падежного окончания относящегося к числу существительного
        /// </summary>
        /// <param name="val">Число</param>
        /// <param name="male">Род существительного, которое относится к числу</param>
        /// <param name="one">Форма существительного в единственном числе</param>
        /// <param name="two">Форма существительного от двух до четырёх</param>
        /// <param name="five">Форма существительного от пяти и больше</param>
        /// <returns></returns>
        public static string Str(int val, bool male, string one, string two, string five)
        {
            string[] frac20 = {
                "", "один ", "два ", "три ", "четыре ", "пять ", "шесть ",
                "семь ", "восемь ", "девять ", "десять ", "одиннадцать ",
                "двенадцать ", "тринадцать ", "четырнадцать ", "пятнадцать ",
                "шестнадцать ", "семнадцать ", "восемнадцать ", "девятнадцать "
            };

            int num = val % 1000;
            if (0 == num) return "";
            if (num < 0) throw new ArgumentOutOfRangeException("val", "Параметр не может быть отрицательным");
            if (!male)
            {
                frac20[1] = "одна ";
                frac20[2] = "две ";
            }

            var r = new StringBuilder(hunds[num / 100]);

            if (num % 100 < 20)
            {
                r.Append(frac20[num % 100]);
            }
            else
            {
                r.Append(tens[num % 100 / 10]);
                r.Append(frac20[num % 10]);
            }

            r.Append(Case(num, one, two, five));

            if (r.Length != 0) r.Append(" ");
            return r.ToString();
        }

        /// <summary>
        /// Выбор правильного падежного окончания сущесвительного
        /// </summary>
        /// <param name="val">Число</param>
        /// <param name="one">Форма существительного в единственном числе</param>
        /// <param name="two">Форма существительного от двух до четырёх</param>
        /// <param name="five">Форма существительного от пяти и больше</param>
        /// <returns>Возвращает существительное с падежным окончанием, которое соответсвует числу</returns>
        public static string Case(int val, string one, string two, string five)
        {
            int t = (val % 100 > 20) ? val % 10 : val % 20;

            switch (t)
            {
                case 1: return one;
                case 2: case 3: case 4: return two;
                default: return five;
            }
        }
        /// <summary>
        /// Перевод целого числа в строку
        /// </summary>
        /// <param name="val">Число</param>
        /// <returns>Возвращает строковую запись числа</returns>
        public static string Str(int val)
        {
            var minus = false;
            if (val < 0) { val = -val; minus = true; }

            var n = val;

            var r = new StringBuilder();

            if (0 == n) r.Append("0 ");
            if (n % 1000 != 0)
                r.Append(RusNumber.Str(n, true, "", "", ""));

            n /= 1000;

            r.Insert(0, RusNumber.Str(n, false, "тысяча", "тысячи", "тысяч"));
            n /= 1000;

            r.Insert(0, RusNumber.Str(n, true, "миллион", "миллиона", "миллионов"));
            n /= 1000;

            r.Insert(0, RusNumber.Str(n, true, "миллиард", "миллиарда", "миллиардов"));
            n /= 1000;

            r.Insert(0, RusNumber.Str(n, true, "триллион", "триллиона", "триллионов"));
            n /= 1000;

            r.Insert(0, RusNumber.Str(n, true, "триллиард", "триллиарда", "триллиардов"));
            if (minus) r.Insert(0, "минус ");

            //Делаем первую букву заглавной
            r[0] = char.ToUpper(r[0]);

            return r.ToString();
        }
    }

}
