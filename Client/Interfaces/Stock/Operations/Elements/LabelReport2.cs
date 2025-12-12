using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Service.Printing;
using Newtonsoft.Json;
using System.Printing;
using System.Windows.Controls;
using static Client.Common.LPackClientAnswer;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Общий класс для печати ярлыков
    /// </summary>
    public class LabelReport2
    {
        /// <summary>
        /// Общий класс для работы с ярлыками
        /// </summary>
        public LabelReport2()
        {
            Initialized = false;
            PrintingProfileLabel = PrintingSettings.LabelPrinter.ProfileName;
            QueryAttempt = 1;
            QueryTimeout = 5000;
            LabelData = new Dictionary<string, string>();
            Init();
        }

        /// <summary>
        /// Общий класс для работы с ярлыками
        /// </summary>
        public LabelReport2(bool useNewPrinting = true)
        {
            Initialized = false;
            PrintingProfileLabel = PrintingSettings.LabelPrinter.ProfileName;
            QueryAttempt = 1;
            QueryTimeout = 5000;
            LabelData = new Dictionary<string, string>();
            Init(useNewPrinting);
        }

        /// <summary>
        /// Флаг того, что интерфейс готов к работе
        /// </summary>
        public bool Initialized { get; set; }

        /// <summary>
        /// Путь к сформированному файлу ярлыка
        /// </summary>
        public string PrintingDocumentFilePath { get; set; }

        public Dictionary<string, string> LabelData { get; set; }

        /// <summary>
        /// Профиль печати, на который отправися документ
        /// </summary>
        public string PrintingProfileCurrent { get; set; }

        /// <summary>
        /// Профиль печати для ярлыков
        /// </summary>
        public string PrintingProfileLabel { get; set; }

        /// <summary>
        /// Флаг использования нового механима печати
        /// </summary>
        public bool UseNewPrinting { get; set; }

        public int QueryAttempt { get; set; }

        public int QueryTimeout { get; set; }

        /// <summary>
        /// Проверяем заполненность данных, необходимых для старта работы интерфейса.
        /// </summary>
        public void Init()
        {
            if (Central.Config.UseNewPrintingFlag > 0)
            {
                UseNewPrinting =  true;
            }
            else
            {
                UseNewPrinting = false;
            }
            
            Initialized = true;
        }

        public void Init(bool useNewPrinting)
        {
            UseNewPrinting = useNewPrinting;

            Initialized = true;
        }

        /// <summary>
        /// получение сформированного pdf файла ярлыка
        /// </summary>
        public void GetLabelPdf(string productionTaskId, string palletNumber, string palletId, int debugOption = 0)
        {
            if (Initialized)
            {
                var p = new Dictionary<string, string>();
                p.Add("PRODUCTION_TASK_ID", productionTaskId);
                p.Add("PALLET_NUMBER", palletNumber);
                p.Add("PALLET_ID", palletId);
                //html
                //pdf
                p.Add("FORMAT", "pdf");
                //0=ответ для браузера (filename не кодирован)
                //1=ответ для клиента EPR (filename закодирован base64)
                //3=ответ JSON (сырые данные для ярлыка)
                p.Add("MODE", "1");
                // 1=шаблон ярлыка с различными штрихкодами
                p.Add("DEBUG_OPTION", debugOption.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Stock");
                q.Request.SetParam("Object", "Label");
                q.Request.SetParam("Action", "Make");
                q.Request.SetParams(p);
                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    if (q.Answer.Type == LPackClientAnswer.AnswerTypeRef.File)
                    {
                        PrintingDocumentFilePath = q.Answer.DownloadFilePath;
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// Получение сформированного html файла ярлыка
        /// </summary>
        public void GetLabelHtml(string productionTaskId, string palletNumber, string palletId, int debugOption=0)
        {
            if (Initialized)
            {
                var p = new Dictionary<string, string>();
                p.Add("PRODUCTION_TASK_ID", productionTaskId);
                p.Add("PALLET_NUMBER", palletNumber);
                p.Add("PALLET_ID", palletId);
                //html
                //pdf
                p.Add("FORMAT", "html");
                //0=ответ для браузера (filename не кодирован)
                //1=ответ для клиента EPR (filename закодирован base64)
                //3=ответ JSON (сырые данные для ярлыка)
                p.Add("MODE", "1");
                // 1=шаблон ярлыка с различными штрихкодами
                p.Add("DEBUG_OPTION", debugOption.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Stock");
                q.Request.SetParam("Object", "Label");
                q.Request.SetParam("Action", "Make");
                q.Request.SetParams(p);
                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    if (q.Answer.Type == LPackClientAnswer.AnswerTypeRef.File)
                    {
                        PrintingDocumentFilePath = q.Answer.DownloadFilePath;
                    }

                    if(q.Answer.Type == LPackClientAnswer.AnswerTypeRef.Data)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        var ds = ListDataSet.Create(result, "REPORT");
                        var s=ds.GetFirstItemValueByKey("LOG");

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

        public void GetLabelData(string productionTaskId, string palletNumber, string palletId, int debugOption = 0)
        {
            if (Initialized)
            {
                var p = new Dictionary<string, string>();
                p.Add("PRODUCTION_TASK_ID", productionTaskId);
                p.Add("PALLET_NUMBER", palletNumber);
                p.Add("PALLET_ID", palletId);
                //0=ответ для браузера (filename не кодирован)
                //1=ответ для клиента EPR (filename закодирован base64)
                //3=ответ JSON (сырые данные для ярлыка)
                p.Add("MODE", "3");
                // 1=шаблон ярлыка с различными штрихкодами
                p.Add("DEBUG_OPTION", debugOption.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Stock");
                q.Request.SetParam("Object", "Label");
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

        public int GetLabelCopyCount()
        {
            int copyCount = 1;

            if (LabelData != null && LabelData.Count > 0)
            {
                copyCount = LabelData.CheckGet("LABEL_COPY_QTY").ToInt();
            }

            if (copyCount < 1)
            {
                copyCount = 1;
            }

            return copyCount;
        }

        public void UpdateReprintNumber(int palletId)
        {
            var p = new Dictionary<string, string>();
            {
                p.Add("PALLET_ID", palletId.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Label");
            q.Request.SetParam("Action", "UpdateReprintNumber");
            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Вывод сформированного ярлыка на печать на указанный профиль печати
        /// </summary>
        public void PrintLabel(string productionTaskId, string palletNumber, string productIdk1 = null, int comingId = 0)
        {
            if (UseNewPrinting)
            {
                GetLabelPdf(productionTaskId.ToInt().ToString(), palletNumber.ToInt().ToString(), "");
                GetLabelData(productionTaskId.ToInt().ToString(), palletNumber.ToInt().ToString(), "");
                int labelCopyCount = GetLabelCopyCount();
                if (!string.IsNullOrEmpty(PrintingDocumentFilePath))
                {
                    var printHelper = new PrintHelper();
                    printHelper.PrintingProfile = PrintingProfileLabel;
                    printHelper.PrintingCopies = labelCopyCount;
                    printHelper.Init();
                    var printingResult = printHelper.StartPrinting(PrintingDocumentFilePath);
                    printHelper.Dispose();
                }

                if (LabelData != null && LabelData.Count > 0)
                {
                    if (!string.IsNullOrEmpty(LabelData.CheckGet("PALLET_ID")))
                    {
                        UpdateReprintNumber(LabelData.CheckGet("PALLET_ID").ToInt());
                    }
                }
            }
            else
            {
                if (productIdk1 == "4")
                {
                    var report = new BlankLabelReport();
                    report.IdPz = productionTaskId;
                    report.Num = palletNumber;
                    report.ComingId = comingId;
                    report.PrintingProfile = PrintingProfileLabel;
                    report.Print();
                }
                else
                {
                    var report = new StockLabelReport();
                    report.IdPz = productionTaskId;
                    report.Num = palletNumber;
                    report.PrintingProfile = PrintingProfileLabel;
                    report.Print();
                }
            }
        }

        /// <summary>
        /// Вывод сформированного ярлыка на печать на указанный профиль печати
        /// </summary>
        public void PrintLabel(string palletId)
        {
            try
            {
                GetLabelPdf("", "", palletId.ToInt().ToString());
                GetLabelData("", "", palletId.ToInt().ToString());
                int labelCopyCount = GetLabelCopyCount();
                if (!string.IsNullOrEmpty(PrintingDocumentFilePath))
                {
                    var printHelper = new PrintHelper();
                    printHelper.PrintingProfile = PrintingProfileLabel;
                    printHelper.PrintingCopies = labelCopyCount;
                    printHelper.Init();
                    var printingResult = printHelper.StartPrinting(PrintingDocumentFilePath);
                    printHelper.Dispose();
                }
                UpdateReprintNumber(palletId.ToInt());
            } catch { }
        }

        /// <summary>
        /// Открытие окна предпросмотра сформированного ярлыка
        /// </summary>
        public void ShowLabel(string productionTaskId, string palletNumber, string productIdk1 = null, int comingId = 0)
        {
            if (UseNewPrinting)
            {
                GetLabelPdf(productionTaskId.ToInt().ToString(), palletNumber.ToInt().ToString(), "");
                if (!string.IsNullOrEmpty(PrintingDocumentFilePath))
                {
                    var printHelper = new PrintHelper();
                    printHelper.PrintingProfile = PrintingProfileLabel;
                    printHelper.Init();
                    var printingResult = printHelper.ShowPreview(PrintingDocumentFilePath);
                    printHelper.Dispose();
                }
            }
            else
            {
                if (productIdk1 == "4")
                {
                    var report = new BlankLabelReport();
                    report.IdPz = productionTaskId;
                    report.Num = palletNumber;
                    report.ComingId = comingId;
                    report.PrintingProfile = PrintingProfileLabel;
                    report.Show();
                }
                else
                {
                    var report = new StockLabelReport();
                    report.IdPz = productionTaskId;
                    report.Num = palletNumber;
                    report.PrintingProfile = PrintingProfileLabel;
                    report.Show();
                }
            }
        }

        /// <summary>
        /// Открытие окна предпросмотра сформированного ярлыка
        /// </summary>
        public void ShowLabelPdf(string palletId, int debugOption = 0)
        {
            try
            {

                /*
                    debugOption -- отладочные функции
                        0=
                        1=шаблон ярлыка с различными штрихкодами
                 */
                GetLabelPdf("", "", palletId.ToInt().ToString(), debugOption);
                if(!string.IsNullOrEmpty(PrintingDocumentFilePath))
                {
                    var printHelper = new PrintHelper();
                    printHelper.PrintingProfile = PrintingProfileLabel;
                    printHelper.Init();
                    var printingResult = printHelper.ShowPreview(PrintingDocumentFilePath);
                    printHelper.Dispose();
                }
            } catch { }
        }

        /// <summary>
        /// Открытие html файла ярлыка
        /// </summary>
        public void DebugLabelHtml(string productionTaskId, string palletNumber)
        {
            GetLabelHtml(productionTaskId.ToInt().ToString(), palletNumber.ToInt().ToString(), "");
            if (!string.IsNullOrEmpty(PrintingDocumentFilePath))
            {
                Central.OpenFile(PrintingDocumentFilePath);
            }
        }

        /// <summary>
        /// Открытие html файла ярлыка
        /// </summary>
        public void ShowLabelHtml(string palletId, int debugOption=0)
        {
            /*
                debugOption -- отладочные функции
                    0=
                    1=шаблон ярлыка с различными штрихкодами
             */
            GetLabelHtml("", "", palletId.ToInt().ToString(), debugOption);
            if (!string.IsNullOrEmpty(PrintingDocumentFilePath))
            {
                Central.OpenFile(PrintingDocumentFilePath);
            }
        }

        /// <summary>
        /// Открывает интерфейс редактирования профилей печати
        /// </summary>
        public static void SetPrintingProfile()
        {
            var i = new PrintingInterface();
        }
    }
}
