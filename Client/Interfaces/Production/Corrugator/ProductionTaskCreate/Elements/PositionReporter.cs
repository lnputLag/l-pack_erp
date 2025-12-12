using Client.Common;
using Client.Interfaces.Main;
using CodeReason.Reports;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Xps.Packaging;

namespace Client.Interfaces.Production.CreatingTasks
{
    /// <summary>
    /// генератор печатных форм
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>        
    public class PositionReporter 
    {
        public PositionReporter()
        {
            CurrentAssembly = Assembly.GetExecutingAssembly();
            FromDate = "";
            ToDate = "";
        }

        /// <summary>
        /// интерфейс текущей сборки (для получения доступа к ресуурсам: шаблонам отчета)
        /// </summary>
        public Assembly CurrentAssembly { get; set; }

        /// <summary>
        /// заголовок отчета
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// список элементов
        /// </summary>
        public List<Dictionary<string,string>> Items { get; set; }
        /// <summary>
        /// дата начала интервала данных (dd.mm.yyyy)
        /// </summary>
        public string FromDate { get; set; }
        /// <summary>
        /// дата окончания интервала данных (dd.mm.yyyy)
        /// </summary>
        public string ToDate { get; set; }


        /// <summary>
        /// Отчет "Автораскрой"
        /// </summary>
        public void MakeReportCuttingList()
        {
            bool resume = true;

            if (resume)
            {
                if (Items.Count == 0)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                {
                    var culture = new System.Globalization.CultureInfo("");
                    ReportDocument reportDocument = new ReportDocument();

                    Stream stream = CurrentAssembly.GetManifestResourceStream("Client.Reports.Production.СorrugatedСardboardСutting.СorrugatedСardboardСuttingReport.xaml");

                    if (stream != null)
                    {
                        StreamReader reader = new StreamReader(stream);

                        reportDocument.XamlData = reader.ReadToEnd();
                        reportDocument.XamlImagePath = Path.Combine(Environment.CurrentDirectory, @"Templates\");
                        reader.Close();

                        string tpl = reportDocument.XamlData;

                        ReportData data = new ReportData();

                        //общие данные
                        var systemName = $"{Central.Parameters.SystemName} {Central.Parameters.BaseLabel}";
                        data.ReportDocumentValues.Add("SystemName", systemName);
                        data.ReportDocumentValues.Add("Today", DateTime.Now);

                        data.ReportDocumentValues.Add("FromDate", FromDate);
                        data.ReportDocumentValues.Add("ToDate", ToDate);
                        data.ReportDocumentValues.Add("Title", Title);

                        //список отгрузок
                        if (Items != null)
                        {
                            if (Items.Count > 0)
                            {
                                DataTable table = new DataTable("Items");

                                table.Columns.Add("description", typeof(string));
                                table.Columns.Add("dttmship", typeof(string));
                                table.Columns.Add("code", typeof(string));
                                table.Columns.Add("name", typeof(string));
                                table.Columns.Add("blank_name", typeof(string));
                                table.Columns.Add("qty_limit", typeof(string));
                                table.Columns.Add("odqty", typeof(string));
                                table.Columns.Add("curqty", typeof(string));
                                table.Columns.Add("rqty", typeof(string));
                                table.Columns.Add("tlsqty", typeof(string));
                                table.Columns.Add("pztotalqty", typeof(string));
                                table.Columns.Add("pzqty", typeof(string));
                                table.Columns.Add("pzzagqty", typeof(string));
                                table.Columns.Add("qty", typeof(string));
                                table.Columns.Add("taskqty", typeof(string));
                                table.Columns.Add("deviation", typeof(string));
                                table.Columns.Add("task_deviation", typeof(string));

                                var provider = CultureInfo.InvariantCulture;

                                foreach (Dictionary<string,string> item in Items)
                                {
                                    var show = true;
                                    if (show)
                                    {
                                        var d=item.CheckGet("DTTMSHIP").ToDateTime().ToString("dd.MM HH:mm");
                                        
                                        table.Rows.Add(new object[]
                                        {
                                            item.CheckGet("DESCRIPTION"),
                                            //item.CheckGet("DTTMSHIP"),
                                            d,
                                            item.CheckGet("BLANKCODE"),
                                            item.CheckGet("BLANKNAME"),
                                            item.CheckGet("BLANKNAME"),
                                            item.CheckGet("QTY_LIMIT"),
                                            item.CheckGet("ODQTY"),
                                            item.CheckGet("CURQTY"),
                                            item.CheckGet("RQTY"),
                                            item.CheckGet("TLSQTY"),
                                            item.CheckGet("PZTOTALQTY"),
                                            item.CheckGet("PZQTY"),
                                            item.CheckGet("PZZAGQTY"),
                                            item.CheckGet("QTY"),
                                            item.CheckGet("TASKQTY"),
                                            item.CheckGet("DEVIATION"),
                                            item.CheckGet("TASK_DEVIATION"),
                                        });
                                    }
                                }
                                data.DataTables.Add(table);
                            }
                        }

                        reportDocument.XamlData = tpl;
                        data.ShowUnknownValues = false;

                        XpsDocument xps = reportDocument.CreateXpsDocumentKey(data, "MakeReportCuttingList");
                        var pp = new PrintPreview(true);
                        pp.documentViewer.Document = xps.GetFixedDocumentSequence();
                        pp.Show();

                    }
                }
            }
        }
    }
}
