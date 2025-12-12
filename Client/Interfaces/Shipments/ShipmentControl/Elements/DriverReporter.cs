using Client.Common;
using Client.Interfaces.Main;
using CodeReason.Reports;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Windows.Xps.Packaging;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Управление отгрузками, генератор печатных форм для списка приехавших водителей
    /// </summary>
    /// <author>balchugov_dv</author>   
    class DriverReporter
    {
        public DriverReporter()
        {
            CurrentAssembly = Assembly.GetExecutingAssembly();
            Drivers = new List<Dictionary<string, string>>();
        }

        public List<Dictionary<string, string>> Drivers { get; set; }
        public Assembly CurrentAssembly { get; set; }

        /// <summary>
        /// отчет по отгрузке
        /// </summary>
        public void MakeDriverReport()
        {
            var resume = true;

            if (resume)
            {
                if (Drivers.Count == 0)
                {
                    Central.Dbg($"No items");
                    resume = false;
                }
            }

            var reportDocument = new ReportDocument();
            var reportTemplate = "Client.Reports.Shipments.DriversListReport.xaml";
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
                reportDocument.XamlImagePath = Path.Combine(Environment.CurrentDirectory, @"Templates\");
                reader.Close();

                string tpl = reportDocument.XamlData;

                ReportData data = new ReportData();

                //общие данные
                var systemName = $"{Central.Parameters.SystemName} {Central.Parameters.BaseLabel}";
                data.ReportDocumentValues.Add("SystemName", systemName);
                data.ReportDocumentValues.Add("Today", DateTime.Now);


                //список водителей
                DataTable table = new DataTable("Drivers");

                table.Columns.Add("DriverName", typeof(string));
                table.Columns.Add("Car", typeof(string));
                table.Columns.Add("DriverPhone", typeof(string));

                foreach (var item in Drivers)
                {
                    var show = true;

                    //водитель въехал
                    if (item.ContainsKey("ENTRYDATE"))
                    {
                        if (!string.IsNullOrEmpty(item["ENTRYDATE"]))
                        {
                            show = false;
                        }
                    }

                    //отгрузка не привязана
                    if (item.ContainsKey("TRANSPORTID"))
                    {
                        if (string.IsNullOrEmpty(item["TRANSPORTID"]))
                        {
                            show = false;
                        }
                    }

                    if (show)
                    {
                        var phone = "";
                        if (!string.IsNullOrEmpty(item["DRIVERPHONE"].ToString()))
                        {
                            phone = item["DRIVERPHONE"].ToString();
                            phone = phone.CellPhone();
                        }

                        table.Rows.Add(new object[]
                        {
                            item["DRIVERNAME"],
                            item["CAR"],
                            phone,
                        });
                    }


                }

                data.DataTables.Add(table);



                reportDocument.XamlData = tpl;
                data.ShowUnknownValues = false;

                XpsDocument xps = reportDocument.CreateXpsDocumentKey(data, "MakeDriversListReport");
                var pp = new PrintPreview();
                pp.documentViewer.Document = xps.GetFixedDocumentSequence();
                pp.Show();


            }
        }

    }
}
