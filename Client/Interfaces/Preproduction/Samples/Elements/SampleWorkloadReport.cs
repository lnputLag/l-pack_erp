using Client.Common;
using Client.Interfaces.Main;
using CodeReason.Reports;
using Newtonsoft.Json;
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
    public class SampleWorkloadReport
    {
        public SampleWorkloadReport()
        {
            ReportTemplate = "Client.Reports.Preproduction.Samples.SampleWorkloadReport.xaml";
            CurrentAssembly = Assembly.GetExecutingAssembly();
        }
        public Assembly CurrentAssembly { get; set; }

        /// <summary>
        /// Шаблон печатной формы
        /// </summary>
        private readonly string ReportTemplate;

        private List<Dictionary<string, string>> GetData()
        {
            var result = new List<Dictionary<string, string>>();
            var totalDS = new ListDataSet();
            var userDS = new ListDataSet();
            
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "WorkloadReport");
            q.Request.SetParam("EMPLOYEE_ID", Central.User.EmployeeId.ToString());

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var answer = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (answer != null)
                {
                    totalDS = ListDataSet.Create(answer, "TOTAL_TIME");
                    userDS = ListDataSet.Create(answer, "USER_TIME");

                    foreach (var row in totalDS.Items)
                    {
                        var d = new Dictionary<string, string>();
                        var dt = row.CheckGet("DT");
                        d.Add("Dt", dt);
                        d.Add("TotalOrders", row.CheckGet("ORDER_QTY").ToInt().ToString());
                        var totalMinutes = row.CheckGet("TOTAL_TIME").ToInt();
                        var hours = (int)(totalMinutes / 60);
                        var minutes = (int)(totalMinutes % 60);

                        d.Add("TotalTime", $"{hours} ч {minutes} мин");

                        string uOrders = "";
                        string uTime = "";

                        foreach (var u in userDS.Items)
                        {
                            if (u.CheckGet("DT") == dt)
                            {
                                uOrders = u.CheckGet("ORDER_QTY").ToInt().ToString();
                                var uTotal = u.CheckGet("TOTAL_TIME").ToInt();
                                var uh = (int)(totalMinutes / 60);
                                var um = (int)(totalMinutes % 60);
                                uTime = $"{uh} ч {um} мин";
                            }
                        }
                        d.Add("UserOrders", uOrders);
                        d.Add("UserTime", uTime);

                        result.Add(d);
                    }
                }
            }

            return result;
        }

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
                tableInfo.Columns.Add("Dt", typeof(string));
                tableInfo.Columns.Add("TotalOrders", typeof(string));
                tableInfo.Columns.Add("TotalTime", typeof(string));
                tableInfo.Columns.Add("UserOrders", typeof(string));
                tableInfo.Columns.Add("UserTime", typeof(string));

                var tableData = GetData();
                foreach (var row in tableData)
                {
                    tableInfo.Rows.Add(new object[] {
                        row.CheckGet("Dt"),
                        row.CheckGet("TotalOrders"),
                        row.CheckGet("TotalTime"),
                        row.CheckGet("UserOrders"),
                        row.CheckGet("UserTime")
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
