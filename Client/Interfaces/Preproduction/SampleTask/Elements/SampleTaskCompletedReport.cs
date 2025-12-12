using Client.Common;
using Client.Interfaces.Main;
using CodeReason.Reports;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Windows.Xps.Packaging;

namespace Client.Interfaces.Preproduction
{
    class SampleTaskCompletedReport
    {
        public SampleTaskCompletedReport()
        {
            reportTemplate = "Client.Reports.Preproduction.Samples.SampleTaskCompletedByDelivery.xaml";
            SampleList = new List<Dictionary<string, string>>();
            CurrentAssembly = Assembly.GetExecutingAssembly();

            FieldIdName = "ID";
            ShowStatus = false;
        }

        public Assembly CurrentAssembly { get; set; }

        /// <summary>
        /// Шаблон печатной формы
        /// </summary>
        private readonly string reportTemplate;

        public string DeliveryType;
        /// <summary>
        /// Имя поля с идентификатором образца
        /// </summary>
        public string FieldIdName;
        /// <summary>
        /// Признак печати статуса образца для менеджеров
        /// </summary>
        public bool ShowStatus;
        /// <summary>
        /// Данные списка образцов для печати
        /// </summary>
        public List<Dictionary<string, string>> SampleList { get; set; }

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

                data.ReportDocumentValues.Add("DeliveryType", DeliveryType);

                DataTable tableInfo = new DataTable("Info");
                tableInfo.Columns.Add("SampleId", typeof(string));
                tableInfo.Columns.Add("Created", typeof(string));
                tableInfo.Columns.Add("Buyer", typeof(string));
                tableInfo.Columns.Add("RowName", typeof(string));
                tableInfo.Columns.Add("Cardboard", typeof(string));
                tableInfo.Columns.Add("Qty", typeof(string));
                tableInfo.Columns.Add("Delivery", typeof(string));

                foreach (var item in SampleList)
                {
                    // ИД и статус
                    int sampleId = item.CheckGet(FieldIdName).ToInt();
                    string sampleIdState = $"{sampleId}";
                    if (ShowStatus)
                    {
                        sampleIdState = $"{sampleId}\n{item.CheckGet("STATUS_NAME")}";
                    }
                    // Дата изготовления
                    var created = item.CheckGet("END_DTTM");
                    if (string.IsNullOrEmpty(created))
                    {
                        // Пробуем другое поле для списка образцов
                        created = item.CheckGet("DT_COMPLITED");
                        if (string.IsNullOrEmpty(created))
                        {
                            created = DateTime.Now.ToString("dd.MM.yy");
                        }
                    }
                    else
                    {
                        created = $"{created.Substring(0, 5)}.{DateTime.Now.Year}";
                    }
                    // Название образца
                    string sampleName = "";
                    if (item.ContainsKey("SAMPLE_NAME"))
                    {
                        sampleName = item["SAMPLE_NAME"];
                    }
                    else
                    {
                        sampleName = $"{item.CheckGet("SAMPLE_SIZE")} {item.CheckGet("SAMPLE_CLASS")}";
                    }

                    string delivery = item.CheckGet("DELIVERY");
                    int cellNum = item.CheckGet("CELL_NUM").ToInt();
                    if (cellNum > 0)
                    {
                        delivery = $"{delivery}({cellNum})";
                    }

                    tableInfo.Rows.Add(new object[] {
                        sampleIdState,
                        created,
                        item.CheckGet("CUSTOMER_NAME"),
                        sampleName,
                        item.CheckGet("CARDBOARD_NAME"),
                        item.CheckGet("QTY").ToInt().ToString(),
                        delivery
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
