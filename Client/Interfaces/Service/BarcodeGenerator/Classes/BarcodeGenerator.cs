using Client.Common;
using Client.Interfaces.Main;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Service
{
    public static class BarcodeGenerator
    {
        public enum Format
        {
            EAN13 = 1,
            CODE128 = 2,
            GS1_DM = 3,
            QR_CODE = 4,
            GS1_128 = 5
        }

        private const int DefaultWidth = 400;

        private const int DefaultHeight = 400;

        public static Dictionary<string, string> FormatDictionary = new Dictionary<string, string>()
        {
            { ((int)Format.EAN13).ToString(), "EAN13" },
            { ((int)Format.CODE128).ToString(), "CODE128" },
            { ((int)Format.GS1_DM).ToString(), "GS1_DM" },
            { ((int)Format.QR_CODE).ToString(), "QR_CODE" },
            { ((int)Format.GS1_128).ToString(), "GS1_128" },
        };

        public static string ValidateBarcodeContent(string barcodeContent, int format)
        {
            string result = barcodeContent;

            switch (format)
            {
                case (int)Format.EAN13:
                    if (barcodeContent.Length > 12)
                    {
                        result = barcodeContent.Substring(0, 12);
                    }
                    break;

                case (int)Format.CODE128:
                    if (barcodeContent.Length > 70)
                    {
                        result = barcodeContent.Substring(0, 70);
                    }
                    break;

                default:
                    break;
            }

            return result;
        }

        public static string GetBarcodeFile(string barcodeContent, int format, int width = DefaultWidth, int height = DefaultHeight)
        {
            string result = null;

            if (width == 0)
            {
                width = DefaultWidth;
            }

            if (height == 0)
            {
                height = DefaultHeight;
            }

            barcodeContent = ValidateBarcodeContent(barcodeContent, format);

            var p = new Dictionary<string, string>();

            p.Add("CODE", barcodeContent);
            //1=EAN13,2=CODE128,3=GS1_DM,4=QR_CODE,5=GS1_128
            p.Add("FORMAT", $"{format}");
            p.Add("WIDTH", $"{width}");
            p.Add("HEIGHT", $"{height}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Label");
            q.Request.SetParam("Action", "MakeBarcode");
            q.Request.SetParams(p);

            q.Request.Timeout = 5000;
            q.Request.Attempts = 1;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                if (q.Answer.Type == LPackClientAnswer.AnswerTypeRef.File)
                {
                    result = q.Answer.DownloadFilePath;
                }
                if (q.Answer.Type == LPackClientAnswer.AnswerTypeRef.Data)
                {
                    var msg = "";
                    msg = msg.Append(q.Answer.Data.ToString());
                    var d = new LogWindow($"{msg}", "Создание ярлыка");
                    d.ShowDialog();
                }
            }

            return result;
        }
    }
}
