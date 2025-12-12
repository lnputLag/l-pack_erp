using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Common;

namespace Client.Interfaces.Stock
{
    public abstract class LabelReport
    {
        /// <summary>
        /// todo stanok вычисляется по idpz
        /// в теории можно на сервер передать idpz получить данные для ярлыка и тип отчета для печати
        /// </summary>
        /// <param name="idPz"></param>
        /// <param name="palletNum"></param>
        /// <param name="stanok"></param>
        /// <returns></returns>
        public static LabelReport Create(string idPz, string palletNum, string stanok, string idk1, int comingId = 0)
        {
            LabelReport report = null;


            if (idk1 == "4")
            {
                report = new BlankLabelReport
                {
                    IdPz = idPz,
                    Num = palletNum,
                    ComingId = comingId
                };
            }
            else
            {
                report = new StockLabelReport
                {
                    IdPz = idPz,
                    Num = palletNum,
                };
            }

            /*
            switch (stanok.ToInt())
            {
                case 719:
                    report = new BlankLabelReport
                    {
                        IdPz = idPz,
                        Num = palletNum
                    };
                    break;

                case 720:
                case 721:

                    report = new StockLabelReport
                    {
                        IdPz = idPz,
                        Num = palletNum
                    };
                    break;
                    
            }
            */

            return report;
        }

        public static LabelReport Create(string idPz, string palletNum, string idk1, int comingId = 0)
        {
            LabelReport report = null;

            if (idk1 == "4")
            {
                report = new BlankLabelReport
                {
                    IdPz = idPz,
                    Num = palletNum,
                    ComingId = comingId
                };
            }
            else
            {
                report = new StockLabelReport
                {
                    IdPz = idPz,
                    Num = palletNum,
                };
            }

            return report;
        }

        /// <summary>
        /// Сгенерированный документ, который можно отпарвить на печать или предпросмотр
        /// </summary>
        public abstract System.Windows.Xps.Packaging.XpsDocument XPS { get; set; }

        /// <summary>
        /// Печать ярлыка
        /// </summary>
        public abstract void Print();

        /// <summary>
        /// Формируем документ
        /// </summary>
        public abstract void CreateDocument();

        /// <summary>
        /// Вызов стандартной формы предпросмотра документа
        /// </summary>
        public abstract void Show();
    }
}
