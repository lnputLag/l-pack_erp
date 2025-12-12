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
    /// Формирование печатной формы технического задания на изготовление образца
    /// </summary>
    /// <author>ryasnoy_pv</author>
    class SampleTaskReporter
    {
        public SampleTaskReporter()
        {
            reportTemplate = "Client.Reports.Preproduction.Samples.SampleTaskReport.xaml";
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
                // Особые указания по склейке
                var gluingItems = new Dictionary<string, string>() {
                    { "0", " " },
                    { "1", "склеить" },
                    { "2", "не клеить" },
                    { "3", "сшить" },
                    { "4", "склеить и сшить" },
                };

                var reader = new StreamReader(stream);
                reportDocument.XamlData = reader.ReadToEnd();
                reader.Close();

                string tpl = reportDocument.XamlData;
                ReportData data = new ReportData();

                DataTable tableInfo = new DataTable("Info");
                tableInfo.Columns.Add("RowName", typeof(string));
                tableInfo.Columns.Add("Description", typeof(string));

                string deliveryType = "";
                if (DeliveryTypes.Items.ContainsKey(SampleItem.CheckGet("DELIVERY_ID")))
                {
                    deliveryType = DeliveryTypes.Items[SampleItem.CheckGet("DELIVERY_ID")];
                }

                // По заполненности поля Артикул определяем образцы с линии
                string article = SampleItem.CheckGet("ARTICLE");

                tableInfo.Rows.Add(new object[] { "Идентификатор", SampleItem.CheckGet("ID") });
                tableInfo.Rows.Add(new object[] { "Дата создания", SampleItem.CheckGet("DT_CREATED") });
                tableInfo.Rows.Add(new object[] { "Покупатель", SampleItem.CheckGet("CUSTOMER_NAME") });
                tableInfo.Rows.Add(new object[] { "Номер", SampleItem.CheckGet("NUM") });
                tableInfo.Rows.Add(new object[] { "Вид изделия", SampleItem.CheckGet("SAMPLE_CLASS") });
                tableInfo.Rows.Add(new object[] { "Размер", SampleItem.CheckGet("SAMPLE_SIZE") });
                //tableInfo.Rows.Add(new object[] { "Картон в заявке", SampleItem.CheckGet("ORDER_CARDBOARD") });
                // Для образцов с линии пишем артикул и задание, для образцов с плоттера - картон для образцов
                if (string.IsNullOrEmpty(article))
                {
                    tableInfo.Rows.Add(new object[] { "Картон для образца", $"{SampleItem.CheckGet("CARDBOARD_NAME")} №{SampleItem.CheckGet("CARDBOARD_NUM").ToInt()} [{SampleItem.CheckGet("RACK_PLACE")}]" });
                }
                else
                {
                    tableInfo.Rows.Add(new object[] { "Артикул", article });
                    tableInfo.Rows.Add(new object[] { "Задание", SampleItem.CheckGet("LINE_NAME") });
                }

                tableInfo.Rows.Add(new object[] { "Количество", SampleItem.CheckGet("QTY").ToInt().ToString() });
                int gluing = SampleItem.CheckGet("GLUING").ToInt();
                tableInfo.Rows.Add(new object[] { "Указания по склейке", gluingItems[gluing.ToString()] });
                tableInfo.Rows.Add(new object[] { "Доставка", deliveryType });

                data.DataTables.Add(tableInfo);

                var note = SampleItem.CheckGet("NOTE");
                if (SampleItem.CheckGet("HIDE_NOTE").ToInt() == 1)
                {
                    note = "";
                }
                data.ReportDocumentValues.Add("Note", note);

                reportDocument.XamlData = tpl;
                XpsDocument xps = reportDocument.CreateXpsDocumentKey(data, "MakeSampleReport");
                var pp = new PrintPreview();
                pp.documentViewer.Document = xps.GetFixedDocumentSequence();
                pp.Show();
            }
        }
    }
}
