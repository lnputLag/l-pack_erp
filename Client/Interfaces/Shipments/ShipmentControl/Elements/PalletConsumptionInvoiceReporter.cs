using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using Client.Common;
using Client.Common.Extensions;
using Client.Common.Lib;
using Client.Interfaces.Main;
using CodeReason.Reports;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// отчет по расходу паллет
    /// </summary>
    public class PalletConsumptionInvoiceReporter
    {
        public string NamePok { get; set; }
        public string NameProd { get; set; }
        public string Num { get; set; }
        public string DT { get; set; }
        public string NDS { get; set; }

        public string Barcode { get; set; }

        public string BarcodeCFormat => (Barcode.Length % 2 == 1 ? "0" : "") + Barcode;

        private Bitmap BarcodeBitmap { get; set; }

        public List<Dictionary<string, string>> Positions { get; set; }

        public PalletConsumptionInvoiceReporter()
        {
            CurrentAssembly = Assembly.GetExecutingAssembly();

        }

        private Assembly CurrentAssembly { get; }

        public void MakeReport()
        {
            var reportDocument = new ReportDocument();

            const string reportTemplate = "Client.Reports.Shipments.InvoiceReport.xaml";

            var stream = CurrentAssembly.GetManifestResourceStream(reportTemplate);

            var reader = new StreamReader(stream);

            reportDocument.XamlData = reader.ReadToEnd();
            reportDocument.XamlImagePath = Path.Combine(Environment.CurrentDirectory, @"Templates\");
            //reportDocument.ImageProcessing += ReportDocumentImageProcessing;
            //reportDocument.ImageError += ReportDocumentImageError;
            reader.Close();

            var tpl = reportDocument.XamlData;

            var data = new ReportData();

            //общие данные
            var systemName = $"{Central.Parameters.SystemName} {Central.Parameters.BaseLabel}";
            data.ReportDocumentValues.Add("SystemName", systemName);
            data.ReportDocumentValues.Add("Today", DateTime.Now);

            data.ReportDocumentValues.Add("NUM", Num);
            try
            {

                data.ReportDocumentValues.Add("DT", DateTime.Parse(DT).ToString("dd.MM.yyyy"));
            }
            catch
            {
                data.ReportDocumentValues.Add("DT", DT);
            }

            data.ReportDocumentValues.Add("NAME_POK", NamePok);
            data.ReportDocumentValues.Add("NAME_PROD", NameProd);
            data.ReportDocumentValues.Add("NDS", NDS);

            var table = new DataTable("Positions");

            table.Columns.Add("Number", typeof(string));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Qty", typeof(string));
            table.Columns.Add("Price", typeof(string));
            table.Columns.Add("Sum", typeof(string));


            var sumS = 0M;

            var count = 1;

            foreach (var item in Positions)
            {
                var s = item["QTY"].ToDecimal() * item["PRICE"].ToDecimal();

                table.Rows.Add(count + " ", " " + item["NAME"], item["QTY"] + " шт ", item["PRICE"].ToDecimal().ToString(), s.ToString());

                sumS += s;

                count++;
            }

            data.DataTables.Add(table);

            var ci = new CultureInfo("ru-ru");

            data.ReportDocumentValues.Add("SumS", sumS.ToString("N", ci));
            data.ReportDocumentValues.Add("SumX", DigitalToStr(sumS, false));


            var sumNDS = sumS / (100 + decimal.Parse(NDS)) * decimal.Parse(NDS);

            data.ReportDocumentValues.Add("TotalVAT", Math.Round(sumNDS, 2).ToString("N", ci));
            data.ReportDocumentValues.Add("AllLines", count - 1);
            data.ReportDocumentValues.Add("DIGITALTOSTRSumS", DigitalToStr(sumS));

            data.ReportDocumentValues.Add("Code128", BarcodeCFormat);

            reportDocument.XamlData = tpl;
            data.ShowUnknownValues = false;

            var xps = reportDocument.CreateXpsDocumentKey(data, "InvoiceReport");
            var pp = new PrintPreview
            {
                documentViewer = { Document = xps.GetFixedDocumentSequence() },
                Topmost = true
            };
            pp.ShowDialog();
        }

        private string DigitalToStr(decimal d, bool inWorlds = true)
        {
            var t1 = (int)d;

            var part1 = (int)Math.Truncate(d);

            var part2 = d == 0 ? 0 : (int)(100 * Math.Round(d % (int)d, 2));

            var resultStr = "";

            var v1 = RusNumber.Str(part1);
            var v2 = RusNumber.Str(t1, true, "рубль", "рубля", "рублей");
            var z1 = v2.Split(' ');

            var z2 = RusNumber.Str(part2, false, "копейка", "копейки", "копеек").Split(' ');

            resultStr += inWorlds ? v1.Trim() : part1.ToString();

            if (z1.Length >= 2)
            {
                resultStr += " " + z1[z1.Length - 2];
            }


            if (z2.Length > 2)
            {
                resultStr += " " + part2 + " " + z2[z2.Length - 2];
            }
            else
            {
                resultStr += " 00 копеек";
            }

            return resultStr;
        }
    }
}
