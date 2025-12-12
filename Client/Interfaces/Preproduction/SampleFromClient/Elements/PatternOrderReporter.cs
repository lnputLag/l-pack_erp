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
    /// Генератор печатной формы документа передачи образца
    /// </summary>
    public class PatternOrderReporter
    {
        public PatternOrderReporter()
        {
            reportTemplate = "Client.Reports.Preproduction.Samples.PatternOrderTransferDoc.xaml";
            PatternOrderItem = new Dictionary<string, string>();
            PatternOrderPurposes = new List<Dictionary<string, string>>();
            CurrentAssembly = Assembly.GetExecutingAssembly();
        }

        /// <summary>
        /// Данные об образце от клиента
        /// </summary>
        public Dictionary<string, string> PatternOrderItem { get; set; }

        /// <summary>
        /// Список целей выбранного образца от клиента
        /// </summary>
        public List<Dictionary<string, string>> PatternOrderPurposes { get; set; }
        
        public Assembly CurrentAssembly { get; set; }

        /// <summary>
        /// Шаблон печатной формы
        /// </summary>
        private readonly string reportTemplate;

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

                DataTable tableInfo = new DataTable("Info");
                tableInfo.Columns.Add("RowName", typeof(string));
                tableInfo.Columns.Add("Description", typeof(string));

                tableInfo.Rows.Add(new object[] { "Номер образца", PatternOrderItem["ID"] });
                tableInfo.Rows.Add(new object[] { "Название организации", PatternOrderItem["CUSTOMER_NAME"] });
                tableInfo.Rows.Add(new object[] { "Контактное лицо", PatternOrderItem["CONTACT_PERSON"] });
                tableInfo.Rows.Add(new object[] { "Менеджер Л-Пак", PatternOrderItem["MANAGER_NAME"] + " (" + PatternOrderItem["MANAGER_EMAIL"] + ")" });
                tableInfo.Rows.Add(new object[] { "Наименование", PatternOrderItem["NAME"] });
                data.DataTables.Add(tableInfo);

                DataTable tablePurpose = new DataTable("Purposes");
                tablePurpose.Columns.Add("Represence", typeof(string));
                tablePurpose.Columns.Add("Purpose", typeof(string));

                foreach (var item in PatternOrderPurposes)
                {
                    tablePurpose.Rows.Add(new object[] { item["RESPONSIBLE_PERSON"], item["NAME"] });
                }
                data.DataTables.Add(tablePurpose);

                reportDocument.XamlData = tpl;
                XpsDocument xps = reportDocument.CreateXpsDocumentKey(data, "MakeSampleReport");
                var pp = new PrintPreview();
                pp.documentViewer.Document = xps.GetFixedDocumentSequence();
                pp.Show();
            }
        }
    }
}
