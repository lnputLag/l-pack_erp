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

namespace Client.Interfaces.Preproduction.Rig
{
    public class StampTransportLabel
    {
        public StampTransportLabel()
        {
            ReportTemplate = "Client.Reports.Preproduction.CuttingStamp.CuttingStampTransportLabel.xaml";
            CurrentAssembly = Assembly.GetExecutingAssembly();
        }

        public Assembly CurrentAssembly { get; set; }

        /// <summary>
        /// Шаблон печатной формы
        /// </summary>
        private readonly string ReportTemplate;
        /// <summary>
        /// Данные об образце
        /// </summary>
        public ListDataSet RigTranportItem { get; set; }

        /// <summary>
        /// Генерация печатной формы
        /// </summary>
        public void Make()
        {
            bool resume = true;
            var reportDocument = new ReportDocument();
            var stream = CurrentAssembly.GetManifestResourceStream(ReportTemplate);

            if (resume && stream == null)
            {
                Central.Dbg($"Cant load report template [{ReportTemplate}]");
                resume = false;
            }

            if (resume)
            {
                var reader = new StreamReader(stream);
                reportDocument.XamlData = reader.ReadToEnd();
                reader.Close();

                string tpl = reportDocument.XamlData;
                ReportData data = new ReportData();

                var row = RigTranportItem.Items[0];

                string packageNum = row.CheckGet("ID").ToInt().ToString();

                data.ReportDocumentValues.Add("PackageNum", packageNum);
                data.ReportDocumentValues.Add("OwnerName", row.CheckGet("OWNER_NAME"));

                DataTable tableInfo = new DataTable("Info");
                tableInfo.Columns.Add("StampNum", typeof(string));

                foreach (var item in RigTranportItem.Items)
                {
                    var els = item.CheckGet("STAMP_ITEM_NAME").Split(' ');
                    var orderNum = els[0];
                    tableInfo.Rows.Add(new object[] {
                        orderNum
                    });
                }

                data.DataTables.Add(tableInfo);
                reportDocument.XamlData = tpl;
                XpsDocument xps = reportDocument.CreateXpsDocumentKey(data, "MakeSampleReport");
                var pp = new PrintPreview();
                pp.documentViewer.Document = xps.GetFixedDocumentSequence();
                pp.Show();
            }
        }
    }
}
