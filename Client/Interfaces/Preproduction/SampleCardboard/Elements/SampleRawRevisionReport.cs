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
    /// <summary>
    /// Отчет для ревизии сырья для образцов
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public class SampleRawRevisionReport
    {
        /// <summary>
        /// Отчет для ревизии сырья для образцов
        /// </summary>
        public SampleRawRevisionReport()
        {
            reportTemplate = "Client.Reports.Preproduction.Samples.SampleRawRevision.xaml";
            SampleRawData = new ListDataSet();
            SampleRawData.Init();
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
        public ListDataSet SampleRawData { get; set; }

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

                DataTable tableRawList = new DataTable("RawList");
                tableRawList.Columns.Add("Num", typeof(string));
                tableRawList.Columns.Add("Name", typeof(string));
                tableRawList.Columns.Add("RawSize", typeof(string));
                tableRawList.Columns.Add("Total", typeof(string));
                tableRawList.Columns.Add("Free", typeof(string));
                tableRawList.Columns.Add("Reserve", typeof(string));
                tableRawList.Columns.Add("RackPlace", typeof(string));

                foreach (var item in SampleRawData.Items)
                {

                    tableRawList.Rows.Add(new object[] {
                        item.CheckGet("CARDBOARD_NUM"),
                        item.CheckGet("CARDBOARD_NAME"),
                        item.CheckGet("FORMAT"),
                        item.CheckGet("TOTAL").ToInt().ToString(),
                        item.CheckGet("QTY").ToInt().ToString(),
                        item.CheckGet("RESERVE").ToInt().ToString(),
                        item.CheckGet("RACK_PLACE"),
                    });
                }
                data.DataTables.Add(tableRawList);

                reportDocument.XamlData = tpl;
                XpsDocument xps = reportDocument.CreateXpsDocumentKey(data, "MakeSampleReport");
                var pp = new PrintPreview();
                pp.documentViewer.Document = xps.GetFixedDocumentSequence();
                pp.Show();
            }
        }
    }
}
