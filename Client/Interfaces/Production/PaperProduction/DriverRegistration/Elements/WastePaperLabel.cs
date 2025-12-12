using Client.Common;
using CodeReason.Reports;
using CodeReason.Reports.Barcode;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.Windows.Xps.Packaging;
using static CodeReason.Reports.Barcode.BarcodeC128;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// печатная форма "ярлык машины",
    /// используется в интерфейсе "регистрация машин на БДМ1 м БДМ2",
    /// получает данные по указанной машине  и формирует печатный документ
    /// </summary>
    /// <author></author>
    class WastePaperLabel
    {
        public WastePaperLabel()
        {
            /*
                
                Для отладки можно менять шаблон на лету и пересоздавать документ
                для шаблона установите свойства (F4)
                --------------------------------------------|---------------
                                                (1)         |     (2)
                --------------------------------------------|---------------
                            Build Action = Embdede resource | None
                Copy to Output Directory = Do not Copy      | Copy Always
                --------------------------------------------|---------------
             */

            //(1)
            ReportTemplate = "Client.Reports.Production.PaperProduction.WastePaperLabel.xaml";
            CompileTemplate = true;

            //(2)
            //ReportTemplate = "Reports/Production/PaperProduction/WastePaperLabel.xaml";
            //CompileTemplate = false;

            CurrentAssembly = Assembly.GetExecutingAssembly();
            ScrapFaultReason = "";
            Name = "ScrapReceipt";
            Complete = false;
            Report = "";
        }


        /// <summary>
        /// ID машины
        /// </summary>
        public int IdScrap { get; set; }

        /// <summary>
        /// Данные о машине
        /// </summary>
        private ListDataSet ScrapDs;

        /// <summary>
        /// структура данных сборки
        /// </summary>
        public Assembly CurrentAssembly { get; set; }
        /// <summary>
        /// документ
        /// </summary>
        public XpsDocument Document { get; set; }
        /// <summary>
        /// технологическое имя документа,
        /// будет напечатано в заголовке документа для его идентификации
        /// </summary>
        private string Name { get; set; }
        /// <summary>
        /// флаг готовности документа,
        /// поднимается, когда документ сформирован
        /// </summary>
        public bool Complete { get; set; }
        public string Report { get; set; }
        /// <summary>
        /// Шаблон печатной формы
        /// </summary>
        private readonly string ReportTemplate;

        /// <summary>
        /// если установлен, содержимое шаблона будет читаться из
        /// скопилированного бандла, иначе из папки bin,
        /// по умолчанию true.
        /// Для отладки полезно изменить шаблон и перегенерировать
        /// документ без перекомпиляции программы целиком.
        /// </summary>
        private bool CompileTemplate;

        public string ScrapFaultReason { get; set; }

        /// <summary>
        /// получаем данные для печати ярлыка текущей машины
        /// </summary>
        private void LoadData()
        {
            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID_SCRAP", IdScrap.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "PaperProduction");
                q.Request.SetParam("Object", "TransportDriver");
                q.Request.SetParam("Action", "ListDataForLabel");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        ScrapDs = ListDataSet.Create(result, "Label");

                        if (ScrapDs == null)
                        {
                            q.ProcessError();
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        public string ReceiptBarcode { get; set; }
        public System.Drawing.Bitmap ReceiptBarcodeBitmap { get; set; }
        /// <summary>
        /// Генерация печатной формы
        /// </summary>
        public bool Make()
        {
            Complete = false;
            bool resume = true;
            Report = "";

            Report = $"{Report}\n загрузка данных";
            LoadData();
            SaveUnloadingTimeCar();
            
            var label = ScrapDs?.GetFirstItem();

            var name = label.CheckGet("NAME").ToString();
            var post = "\n" + label.CheckGet("POST").ToString();
            var phoneNumber = "\n" + label.CheckGet("PHONE_NUMBER").ToString();
            var barcode = label.CheckGet("BARCODE").ToString() + "0";
            var created_dttm = "\n" + label.CheckGet("CREATED_DTTM").ToString();
            var rnum = "\n" + label.CheckGet("RN").ToString();
            var date_unloading = "\n" + label.CheckGet("DATE_UNLOADING").ToString();

            if (label == null)
            {
                Report = $"{Report}\n [!] нет данных";
                resume = false;
            }

            var reportDocument = new ReportDocument();
            Stream stream = null;

            var tplFile = ReportTemplate;

            Report = $"{Report}\n загрузка шаблона=[{tplFile}]";

            if (CompileTemplate)
            {
                //скомпилированный ресурс
                stream = CurrentAssembly.GetManifestResourceStream(tplFile);
            }
            else
            {
                //папка с исполнимым файлом
                stream = File.OpenRead(tplFile);
            }

            if (resume)
            {
                if (stream == null)
                {
                    Report = $"{Report}\n [!] не удалось загрузить шаблон";
                    resume = false;
                }
            }

            if (resume)
            {
                Report = $"{Report}\n создание документа";

                var reader = new StreamReader(stream);
                reportDocument.XamlData = reader.ReadToEnd();
                reportDocument.XamlImagePath = Path.Combine(Environment.CurrentDirectory, @"Templates\");
                reportDocument.ImageProcessing += ReportDocumentImageProcessing;
                reportDocument.ImageError += ReportDocumentImageError;
                reader.Close();

                string tpl = reportDocument.XamlData;
                ReportData data = new ReportData();

                data.ReportDocumentValues.Add("SystemName", "Добро пожаловать в ООО Л-ПАК");
                data.ReportDocumentValues.Add("Name", name);
                data.ReportDocumentValues.Add("Post", post);
                data.ReportDocumentValues.Add("Phone", phoneNumber);
                data.ReportDocumentValues.Add("Barcode", barcode);
                data.ReportDocumentValues.Add("DateNow", created_dttm);
                data.ReportDocumentValues.Add("Rnum", rnum);
                data.ReportDocumentValues.Add("DateUnloading", date_unloading);
                data.ReportDocumentValues.Add("Info", "Сохраняйте талон до\nокончания разгрузки");

                reportDocument.XamlData = tpl;
                XpsDocument xps = reportDocument.CreateXpsDocumentKey(data, "ScpapReceipt");
                Document = xps;

                Complete = true;
                Report = $"{Report}\n готово";
            }
            return Complete;
        }

        private Bitmap RotateImage(Bitmap bmp)
        {
            int angle = 90;
            Bitmap rotatedImage = new Bitmap(bmp.Height, bmp.Width);
            rotatedImage.SetResolution(bmp.HorizontalResolution, bmp.VerticalResolution);

            using (Graphics g = Graphics.FromImage(rotatedImage))
            {
                g.FillRectangle(System.Drawing.Brushes.White, 0, 0, bmp.Width, bmp.Width);
                g.RotateTransform(angle);
                g.TranslateTransform(0, -bmp.Height);
                g.DrawImage(bmp, new Point(0, 0));
            }
            return rotatedImage;
        }

        /// <summary>
        /// генератор штрих-кода
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReportDocumentImageProcessing(object sender, ImageEventArgs e)
        {
            if (e.Image.Name == "ReceiptBarcode2")
            {
                if (!string.IsNullOrEmpty(ReceiptBarcode))
                {
                    if (ReceiptBarcodeBitmap != null)
                    {
                        MemoryStream mem = new MemoryStream();
                        ReceiptBarcodeBitmap.Save(mem, System.Drawing.Imaging.ImageFormat.Bmp);
                        mem.Position = 0;

                        BitmapImage image = new BitmapImage();
                        image.BeginInit();
                        image.StreamSource = mem;
                        image.EndInit();
                        e.Image.Source = image;
                    }
                }
            }
        }

        private void ReportDocumentImageError(object sender, ImageErrorEventArgs e)
        {
            e.Handled = true;
        }

        /// <summary>
        /// сохраняем предполагаемое время разгрузки машины
        /// </summary>
        private void SaveUnloadingTimeCar()
        {
            bool resume = true;

            var label = ScrapDs?.GetFirstItem();
            var date_unloading = label.CheckGet("DATE_UNLOADING").ToString();

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID_SCRAP", IdScrap.ToString());
                    p.CheckAdd("DTTM", date_unloading);
                }
            
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "PaperProduction");
                q.Request.SetParam("Object", "TransportDriver");
                q.Request.SetParam("Action", "SaveUnloadingTimeCar");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                if (q.Answer.Status != 0)
                {
                    q.ProcessError();
                }
            }
        }

    }
}
