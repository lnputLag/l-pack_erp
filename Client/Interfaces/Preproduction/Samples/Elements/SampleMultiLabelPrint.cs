using Client.Common;
using Client.Interfaces.Main;
using CodeReason.Reports;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Xps.Packaging;

namespace Client.Interfaces.Preproduction
{
    public class SampleMultiLabelPrint
    {
        public SampleMultiLabelPrint()
        {
            reportTemplate = "Client.Reports.Preproduction.Samples.SampleTaskMultiLabel.xaml";
            SampleItems = new List<Dictionary<string, string>>();
            CurrentAssembly = Assembly.GetExecutingAssembly();
            Note = "";
        }

        public Assembly CurrentAssembly { get; set; }

        /// <summary>
        /// Шаблон печатной формы
        /// </summary>
        private readonly string reportTemplate;
        /// <summary>
        /// Данные об образце
        /// </summary>
        public List<Dictionary<string, string>> SampleItems { get; set; }

        public string Note { get; set; }

        public XpsDocument Make()
        {
            bool resume = true;
            var reportDocument = new ReportDocument();
            var stream = CurrentAssembly.GetManifestResourceStream(reportTemplate);

            ReportData data = new ReportData();

            if (resume)
            {
                if (stream == null)
                {
                    Central.Dbg($"Cant load report template [{reportTemplate}]");
                    resume = false;
                }
            }

            if (resume)
            {
                var reader = new StreamReader(stream);

                string tp = "<!--TABLE ROWS-->";

                string line = reader.ReadLine();
                string labelData = line;

                while (line != null)
                {
                    line = reader.ReadLine();
                    if (line != null)
                    {
                        if (line.Trim(' ') == tp)
                        {
                            foreach (var item in SampleItems)
                            {
                                string sampleSize = item.CheckGet("SAMPLE_SIZE");
                                string sampleClass = item.CheckGet("SAMPLE_CLASS");
                                string cardboard = "";
                                string note = "";
                                if (item.CheckGet("HIDE_MARK").ToBool())
                                {
                                    cardboard = item.CheckGet("PROFILE_NAME");
                                }
                                else
                                {
                                    cardboard = item.CheckGet("CARDBOARD_NAME");
                                }

                                // Проверяем настройку ярлыка
                                string labelNote = "";
                                string labelCustomizing = item.CheckGet("LABEL_TEXT");
                                if (!labelCustomizing.IsNullOrEmpty())
                                {
                                    var v = labelCustomizing.Split(';');
                                    if (v[2].ToInt() == 1)
                                    {
                                        sampleSize = v[3];
                                    }

                                    if (v[4].ToInt() == 1)
                                    {
                                        sampleClass = v[5];
                                    }

                                    if (v[6].ToInt() == 1)
                                    {
                                        cardboard = v[7];
                                    }

                                    if (v.Length > 8)
                                    {
                                        labelNote = v[8];
                                    }


                                }

                                string row = $"<TableRow><TableCell Style = \"{{StaticResource CellStyle}}\">";
                                row = $"{row}\n<Paragraph Style = \"{{StaticResource MainRowStyle}}\" Margin=\"0,1cm,0,0\">{item.CheckGet("DT_COMPLITED")} / {item.CheckGet("ID")}</Paragraph>";
                                row = $"{row}\n<Paragraph>{sampleSize}</Paragraph>";
                                row = $"{row}\n<Paragraph>{sampleClass}</Paragraph>";
                                row = $"{row}\n<Paragraph>{cardboard}</Paragraph>";
                                row = $"{row}\n</TableCell><TableCell Style = \"{{StaticResource CellStyle}}\">";
                                row = $"{row}\n<Paragraph Margin=\"0,1cm,0,0\">{item.CheckGet("QTY").ToInt()} шт</Paragraph>";
                                row = $"{row}\n</TableCell></TableRow>";
                                row = $"{row}\n<TableRow><TableCell ColumnSpan=\"2\" Style = \"{{StaticResource CellStyle}}\">";
                                row = $"{row}\n<Paragraph TextAlignment=\"Center\">{labelNote}</Paragraph>";
                                row = $"{row}\n</TableCell></TableRow>";

                                labelData = $"{labelData}\n{row}";
                            }
                        }
                        labelData = $"{labelData}\n{line}";
                    }
                }
                reader.Close();



                reportDocument.XamlData = labelData;

                string customerName = SampleItems[0].CheckGet("CUSTOMER_NAME");
                string customerCustomizing = SampleItems[0].CheckGet("LABEL_TEXT");
                if (!customerCustomizing.IsNullOrEmpty())
                {
                    var v = customerCustomizing.Split(';');
                    if (v[0].ToInt() == 1)
                    {
                        customerName = v[1];
                    }
                }

                data.ReportDocumentValues.Add("BuyerName", customerName);
                data.ReportDocumentValues.Add("Note", Note);
                reportDocument.XamlData = labelData;
                //var pp = new PrintPreview();
                //pp.documentViewer.Document = xps.GetFixedDocumentSequence();
                //pp.Show();
            }
            XpsDocument xps = reportDocument.CreateXpsDocumentKey(data, "SampleMultiLabel");

            return xps;
        }
    }
}
