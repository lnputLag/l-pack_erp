using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Production;
using Client.Interfaces.Service.Printing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Client.Common.LPackClientAnswer;
using static DevExpress.Mvvm.UI.Native.ViewGenerator.EditorsGeneratorBase;
using static DevExpress.XtraPrinting.Native.ExportOptionsPropertiesNames;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Класс работы с ярлыками сырья
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public class RawMaterialLabelReport
    {
        /// <summary>
        /// Класс работы с ярлыками сырья
        /// </summary>
        public RawMaterialLabelReport() 
        {
            Initialized = false;
            LabelData = new Dictionary<string, string>();
            QueryAttempt = 1;
            QueryTimeout = 5000;
            CurrentLabelType = LabelType.InnerLabel;
            PrintingCopies = 1;
            Init();
        }

        /// <summary>
        /// Флаг того, что интерфейс готов к работе
        /// </summary>
        public bool Initialized { get; set; }

        /// <summary>
        /// Путь к сформированному файлу ярлыка
        /// </summary>
        public string PrintingDocumentFilePath { get; set; }

        /// <summary>
        /// Данные для заполнения ярлыка
        /// </summary>
        public Dictionary<string, string> LabelData { get; set; }

        public int PrintingCopies { get; set; }

        public int QueryAttempt { get; set; }

        public int QueryTimeout { get; set; }

        /// <summary>
        /// 0 - внутренний ярлык
        /// 1 - внешний ярлык
        /// 2 - ярлык с раскатов
        /// </summary>
        public enum LabelType
        {
            /// <summary>
            /// внутренний ярлык
            /// </summary>
            InnerLabel,
            /// <summary>
            /// внешний ярлык
            /// </summary>
            OuterLabel,
            /// <summary>
            /// ярлык с раскатов
            /// </summary>
            ReelLabel
        }

        public LabelType CurrentLabelType { get; set; }

        /// <summary>
        /// Проверяем заполненность данных, необходимых для старта работы интерфейса.
        /// </summary>
        public void Init()
        {
            Initialized = true;
        }

        public string GetPrintingProfile()
        {
            string profile = "";

            switch (CurrentLabelType)
            {
                case LabelType.InnerLabel:
                    profile = PrintingSettings.RawLabelPrinter.ProfileName;
                    break;
                case LabelType.OuterLabel:
                    profile = PrintingSettings.OuterRawLabelPrinter.ProfileName;
                    break;
                case LabelType.ReelLabel:
                    break;
                default:
                    profile = PrintingSettings.RawLabelPrinter.ProfileName;
                    break;
            }

            return profile;
        }

        /// <summary>
        /// Получение файла ярлыка. При успешном выполнении заполнит PrintingDocumentFilePath путём до полученного файла ярлыка
        /// </summary>
        /// <param name="incomingId">Ид прихода</param>
        /// <param name="format">Формат документа: pdf или html</param>
        public void GetLabel(string incomingId, string format = "pdf")
        {
            if (Initialized)
            {
                var p = new Dictionary<string, string>();
                p.Add("INCOMING_ID", incomingId);
                //html
                //pdf
                p.Add("FORMAT", format);
                //0=ответ для браузера (filename не кодирован)
                //1=ответ для клиента EPR (filename закодирован base64)
                //3=ответ JSON (сырые данные для ярлыка)
                p.Add("MODE", "1");

                p.Add("TYPE", $"{(int)CurrentLabelType}");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Stock");
                q.Request.SetParam("Object", "RawMaterialLabel");
                q.Request.SetParam("Action", "Make");
                q.Request.SetParams(p);
                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    if (q.Answer.Type == LPackClientAnswer.AnswerTypeRef.File)
                    {
                        PrintingDocumentFilePath = q.Answer.DownloadFilePath;
                    }

                    if (q.Answer.Type == LPackClientAnswer.AnswerTypeRef.Data)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        var ds = ListDataSet.Create(result, "REPORT");
                        var s = ds.GetFirstItemValueByKey("LOG");

                        var msg = s;
                        var d = new LogWindow($"{msg}", "Ошибка при создании документа");
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        public void GetLabelData(string incomingId)
        {
            if (Initialized)
            {
                var p = new Dictionary<string, string>();
                p.Add("INCOMING_ID", incomingId);
                //0=ответ для браузера (filename не кодирован)
                //1=ответ для клиента EPR (filename закодирован base64)
                //3=ответ JSON (сырые данные для ярлыка)
                p.Add("MODE", "3");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Stock");
                q.Request.SetParam("Object", "RawMaterialLabel");
                q.Request.SetParam("Action", "Make");
                q.Request.SetParams(p);
                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    if (q.Answer.Type == AnswerTypeRef.Data)
                    {
                        var answerResult = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (answerResult != null)
                        {
                            var ds = ListDataSet.Create(answerResult, "ITEMS");
                            if (ds != null && ds.Items != null && ds.Items.Count > 0)
                            {
                                LabelData = ds.Items[0];
                            }
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        public void PrintLabel(string incomingId)
        {
            GetLabel(incomingId, "pdf");
            if (!string.IsNullOrEmpty(PrintingDocumentFilePath))
            {
                var printHelper = new PrintHelper();
                printHelper.PrintingProfile = GetPrintingProfile();
                printHelper.PrintingCopies = this.PrintingCopies;
                printHelper.Init();
                var printingResult = printHelper.StartPrinting(PrintingDocumentFilePath);
                printHelper.Dispose();
            }
        }

        public void ShowLabel(string incomingId)
        {
            GetLabel(incomingId, "pdf");
            if (!string.IsNullOrEmpty(PrintingDocumentFilePath))
            {
                var printHelper = new PrintHelper();
                printHelper.PrintingProfile = GetPrintingProfile();
                printHelper.Init();
                var printingResult = printHelper.ShowPreview(PrintingDocumentFilePath);
                printHelper.Dispose();
            }
        }

        public void DebugLabel(string incomingId)
        {
            GetLabel(incomingId, "html");
            if (!string.IsNullOrEmpty(PrintingDocumentFilePath))
            {
                Central.OpenFile(PrintingDocumentFilePath);
            }
        }

        public void SetPrintingProfile()
        {
            var i = new PrintingInterface();
        }
    }
}
