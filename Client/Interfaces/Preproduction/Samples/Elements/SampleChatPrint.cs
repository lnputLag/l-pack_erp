using Client.Common;
using Client.Interfaces.Main;
using CodeReason.Reports;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Windows.Xps.Packaging;


namespace Client.Interfaces.Preproduction
{
    public class SampleChatPrint
    {
        public SampleChatPrint()
        {
            ReportTemplate = "Client.Reports.Preproduction.Samples.SampleChatReport.xaml";
            CurrentAssembly = Assembly.GetExecutingAssembly();

            ChatType = 2;
        }

        public Assembly CurrentAssembly { get; set; }

        /// <summary>
        /// Шаблон печатной формы
        /// </summary>
        private readonly string ReportTemplate;

        public List<Dictionary<string, string>> MessageList { get; set; }

        public string SampleName;

        /// <summary>
        /// Тип чата 0 - с клиентом, 1 - внутренний, 2 - чат по образцу
        /// </summary>
        public int ChatType;

        private string WriteMessages()
        {
            string result = "";

            if (MessageList.Count > 0)
            {
                foreach(var mes in MessageList)
                {
                    // "SENDER_TYPE" - тип отправителя: 0 - Л-ПАК, 1 - клиент
                    int senderType = mes.CheckGet("SENDER_TYPE").ToInt();
                    string header = $"{mes.CheckGet("DTTM")} {mes.CheckGet("SENDER")}";
                    var emplName = mes.CheckGet("FULL_NAME");
                    if ((senderType == 0) && (!string.IsNullOrEmpty(emplName)))
                    {
                        header = $"{header} ({emplName})";
                    }
                    var msgBody = mes.CheckGet("TXT");

                    if (senderType == 0)
                    {
                        result = $"{result}\n<Paragraph Style=\"{{StaticResource HeaderEmployeeStyle}}\">{header}</Paragraph>";
                        result = $"{result}\n<Paragraph Style=\"{{StaticResource MessageEmployeeStyle}}\">{msgBody}</Paragraph>";
                    }
                    else
                    {
                        result = $"{result}\n<Paragraph Style=\"{{StaticResource HeaderCustomerStyle}}\">{header}</Paragraph>";
                        result = $"{result}\n<Paragraph Style=\"{{StaticResource MessageCustomerStyle}}\">{msgBody}</Paragraph>";
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
            var reportDocument = new ReportDocument();
            var stream = CurrentAssembly.GetManifestResourceStream(ReportTemplate);
            var reader = new StreamReader(stream);

            string tp = "<!--INSERT-->";

            string line = reader.ReadLine();
            string chatData = line;

            while (line != null)
            {
                line = reader.ReadLine();

                if (line != null)
                {
                    if (line.Trim(' ') == tp)
                    {
                        var msg = WriteMessages();
                        chatData = $"{chatData}\n{msg}";
                    }
                    chatData = $"{chatData}\n{line}";
                }
            }
            reader.Close();

            string chatHeader = "";
            if (ChatType == 0)
            {
                chatHeader = "Чат с клиентом ";
            }
            else if (ChatType == 1)
            {
                chatHeader = "Чат с коллегами ";
            }
            else
            {
                chatHeader = "Чат по образцу ";
            }

            ReportData data = new ReportData();
            data.ReportDocumentValues.Add("SampleName", $"{chatHeader}{SampleName}");
            reportDocument.XamlData = chatData;
            XpsDocument xps = reportDocument.CreateXpsDocumentKey(data, "SampleChatReport");
            var pp = new PrintPreview();
            pp.documentViewer.Document = xps.GetFixedDocumentSequence();
            pp.Show();
        }
    }
}
