using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Production.CreatingTasks;
using CodeReason.Reports;
using DevExpress.ClipboardSource.SpreadsheetML;
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
    public class CuttingStampOrderReport
    {
        public CuttingStampOrderReport()
        {
            ReportTemplate = "Client.Reports.Preproduction.CuttingStamp.CuttingStampOrderList.xaml";
            OrderList = new List<Dictionary<string, string>>();
            CurrentAssembly = Assembly.GetExecutingAssembly();
        }

        public Assembly CurrentAssembly { get; set; }

        /// <summary>
        /// Шаблон печатной формы
        /// </summary>
        private readonly string ReportTemplate;

        public List<Dictionary<string, string>> OrderList { get; set; }

        public List<DataGridHelperColumn> PrintColumns { get; set; }

        /// <summary>
        /// Генерация печатной формы
        /// </summary>
        public void Make()
        {
            bool resume = true;
            var reportDocument = new ReportDocument();
            var stream = CurrentAssembly.GetManifestResourceStream(ReportTemplate);

            if (stream == null)
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

                DataTable tableInfo = new DataTable("Info");
                tableInfo.Columns.Add("Payer", typeof(string));
                tableInfo.Columns.Add("OrderNum", typeof(string));
                tableInfo.Columns.Add("TechCard", typeof(string));

                foreach (var item in OrderList)
                {
                    string orderNum = item.CheckGet("OUTER_NUM");
                    if (orderNum.IsNullOrEmpty())
                    {
                        orderNum = item.CheckGet("ORDER_NUM");
                    }

                    tableInfo.Rows.Add(new object[] {
                        item.CheckGet("BUYER_NAME"),
                        orderNum,
                        item.CheckGet("TK_NAME"),
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

        public async void MakeExcel()
        {
            var eg = new ExcelGrid();
            eg.SetColumnsFromGrid(PrintColumns);
            eg.Items = OrderList;
            await Task.Run(() =>
            {
                eg.Make();
            });
        }
    }
}
