using Client.Common;
using Client.Interfaces.Main;
using CodeReason.Reports;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Windows.Xps.Packaging;

namespace Client.Interfaces.Preproduction
{
    class SampleTaskLabel
    {
        public SampleTaskLabel()
        {
            reportTemplate = "Client.Reports.Preproduction.Samples.SampleTaskLabel.xaml";
            SampleItem = new Dictionary<string, string>();
            CurrentAssembly = Assembly.GetExecutingAssembly();
        }

        public Assembly CurrentAssembly { get; set; }

        /// <summary>
        /// Шаблон печатной формы
        /// </summary>
        private readonly string reportTemplate;
        /// <summary>
        /// Данные об образце
        /// </summary>
        public Dictionary<string, string> SampleItem { get; set; }

        /// <summary>
        /// Генерация печатной формы
        /// </summary>
        public void Make()
        {
            bool resume = true;
            var reportDocument = new ReportDocument();
            var stream = CurrentAssembly.GetManifestResourceStream(reportTemplate);

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
                reportDocument.XamlData = reader.ReadToEnd();
                reader.Close();

                string tpl = reportDocument.XamlData;
                ReportData data = new ReportData();

                string labelCustomizing = SampleItem.CheckGet("LABEL_TEXT");

                string customerName = SampleItem.CheckGet("CUSTOMER_NAME");
                string sampleSize = SampleItem.CheckGet("SAMPLE_SIZE");
                string sampleClass = SampleItem.CheckGet("SAMPLE_CLASS");
                string cardboard = "";
                string note = "";

                if (SampleItem.CheckGet("HIDE_MARK").ToBool())
                {
                    cardboard = SampleItem.CheckGet("PROFILE_NAME");
                }
                else
                {
                    cardboard = SampleItem.CheckGet("CARDBOARD_NAME");
                }

                if (!labelCustomizing.IsNullOrEmpty())
                {
                    var v = labelCustomizing.Split(';');
                    if (v[0].ToInt() == 1)
                    {
                        customerName = v[1];
                    }

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
                        note = v[8];
                    }
                }

                cardboard = $"{cardboard} [{SampleItem.CheckGet("CARDBOARD_NUM")}]";

                if (!string.IsNullOrEmpty(SampleItem.CheckGet("SAMPLE_NUM")))
                {
                    cardboard = $"{cardboard} ({SampleItem.CheckGet("SAMPLE_NUM")})";
                }

                var technologNote = SampleItem.CheckGet("TECHNOLOG_NOTE");
                if (!technologNote.IsNullOrEmpty())
                {
                    if (note.IsNullOrEmpty())
                    {
                        note = technologNote;
                    }
                    else
                    {
                        note = $"{note}\n\n{technologNote}";
                    }
                }

                data.ReportDocumentValues.Add("BuyerName", customerName);
                data.ReportDocumentValues.Add("Sample", $"{SampleItem.CheckGet("DT_COMPLITED")} / {SampleItem.CheckGet("ID")}");
                data.ReportDocumentValues.Add("SampleSize", sampleSize);
                data.ReportDocumentValues.Add("SampleClass", sampleClass);
                data.ReportDocumentValues.Add("SampleCardboard", cardboard);
                data.ReportDocumentValues.Add("Qty", $"{SampleItem.CheckGet("QTY").ToInt()} шт");
                data.ReportDocumentValues.Add("Note", note);
                data.ReportDocumentValues.Add("Place", SampleItem.CheckGet("PLACE"));

                reportDocument.XamlData = tpl;
                XpsDocument xps = reportDocument.CreateXpsDocumentKey(data, "MakeSampleReport");
                var pp = new PrintPreview();
                pp.documentViewer.Document = xps.GetFixedDocumentSequence();
                pp.Show();
            }
        }
    }
}
