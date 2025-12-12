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
    /// печатная форма "ярлык рулона",
    /// используется в интерфейсе "управление рулонами",
    /// получает данные по указанному рулону и формирует печатный документ
    /// </summary>
    /// <author></author>
    class RollReceipt
    {
        public RollReceipt(int rollId)
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
            ReportTemplate = "Client.Reports.Production.RollControl.RollReceipt3.xaml";
            ReportTemplate2 = "Client.Reports.Production.RollControl.RollReceipt3f.xaml";
            CompileTemplate=true;

            //(2)
            //ReportTemplate = "Reports/Production/RollControl/RollReceipt3.xaml";
            //CompileTemplate=false;

            Roll = new Dictionary<string, string>();
            CurrentAssembly = Assembly.GetExecutingAssembly();
            RollId=rollId;
            RollFaultReason="";
            Name="RollReceipt";
            Complete=false;
            Report="";

            if(RollId!=0)
            {
                GetData();
            }
        }

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
        private string Name { get;set;}
        /// <summary>
        /// флаг готовности документа,
        /// поднимается, когда документ сформирован
        /// </summary>
        public bool Complete { get;set;}
        public string Report { get;set;}
        /// <summary>
        /// Шаблон печатной формы
        /// </summary>
        private readonly string ReportTemplate;
        private readonly string ReportTemplate2;
        /// <summary>
        /// если установлен, содержимое шаблона будет читаться из
        /// скопилированного бандла, иначе из папки bin,
        /// по умолчанию true.
        /// Для отладки полезно изменить шаблон и перегенерировать
        /// документ без перекомпиляции программы целиком.
        /// </summary>
        private bool CompileTemplate;

        /// <summary>
        /// Данные о рулоне
        /// </summary>
        public Dictionary<string, string> Roll { get; set; }
        /// <summary>
        /// ID рульна
        /// </summary>
        public int RollId { get;set; }
        public string RollFaultReason { get;set; }

        /// <summary>
        /// получение данных о рулоне
        /// </summary>
        public void GetData()
        {
            bool resume=true;

            if(resume)
            {
                if(RollId==0)
                {
                    resume=false;
                }
            }

            if(resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "Roll");
                q.Request.SetParam("Action", "GetDataReceipt");
                q.Request.SetParam("ID", RollId.ToString());

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        Roll = ds.GetFirstItem();
                        Roll.CheckAdd("ID_ROLL", RollId.ToString());
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        public string ReceiptBarcode {get;set;}
        public System.Drawing.Bitmap ReceiptBarcodeBitmap { get; set; }
        /// <summary>
        /// Генерация печатной формы
        /// </summary>
        public bool Make()
        {
            Complete=false;
            bool resume = true;
            Report="";

            Report=$"{Report}\n загрузка данных";
            GetData();

            if(resume)
            {
                if(Roll.Count==0)
                {
                    Report=$"{Report}\n [!] нет данных";
                    resume=false;
                }
            }

            var reportDocument = new ReportDocument();
            Stream stream=null;

            //если нужно показать причину забраковки, переключим шаблоны 
            ///RollFaultReason="неправильный формат в рулоне";
            var tplFile=ReportTemplate;
            if(!string.IsNullOrEmpty(RollFaultReason))
            {
                tplFile=ReportTemplate2;
            }


            Report=$"{Report}\n загрузка шаблона=[{tplFile}]";

            if(CompileTemplate)
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
                    Report=$"{Report}\n [!] не удалось загрузить шаблон";
                    resume = false;
                }
            }

            if (resume)
            {
                Report=$"{Report}\n создание документа";

                var reader = new StreamReader(stream);
                reportDocument.XamlData = reader.ReadToEnd();
                reportDocument.XamlImagePath = Path.Combine(Environment.CurrentDirectory,@"Templates\");
                reportDocument.ImageProcessing += ReportDocumentImageProcessing;
                reportDocument.ImageError += ReportDocumentImageError;
                reader.Close();

                string tpl = reportDocument.XamlData;
                ReportData data = new ReportData();

                var systemName = $"{Central.Parameters.SystemName} ({Name}) {Central.Parameters.BaseLabel}";
                data.ReportDocumentValues.Add("SystemName", systemName);

                //barcode

                //old
                {
                    //data.ReportDocumentValues.Add("ReceiptBarcode", Roll.CheckGet("BARCODE2"));    
                }

                //new
                {
                    ReceiptBarcode=Roll.CheckGet("BARCODE2");
                    
                    ReceiptBarcodeBitmap=(Bitmap)Code128Rendering.MakeBarcodeImage(ReceiptBarcode,1,true);
                    ReceiptBarcodeBitmap=RotateImage(ReceiptBarcodeBitmap);

                    //var h = new Ean13(ReceiptBarcode);
                    //ReceiptBarcodeBitmap = h.CreateBitmap();
                    //ReceiptBarcodeBitmap=RotateImage(ReceiptBarcodeBitmap,90);
                }


                //FIXME: 2022-09-07_F1
                //сделать переключение блоков, основанное не манипуляции текстом шаблона или свойствами блока

                //если причина забраковки задана
                //скроем секцию с баркодом и покажем причину забраковки
                //RollFaultReason="неправильный формат в рулоне";
                //if(!string.IsNullOrEmpty(RollFaultReason))
                //{
                //    tpl = tpl.Replace("RawSection", "_RawSection");
                //    tpl = tpl.Replace("_FaultRollSection", "FaultRollSection");
                //    tpl = tpl.Replace("_FaultReasonSection","FaultReasonSection");
                //}
                //data.ReportDocumentValues.Add("FaultReason", RollFaultReason);

                if(!string.IsNullOrEmpty(RollFaultReason))
                {
                    data.ReportDocumentValues.Add("FaultReason", RollFaultReason);
                }

                var rn = Roll.CheckGet("RAW_NAME");
                string rawName = "";
                if (!string.IsNullOrEmpty(rn))
                {
                    rawName=rn;

                    /*
                    int sl = rn.IndexOf("/");
                    if (sl > 3)
                    {
                        rawName = rn.Insert(sl - 3, "\n");
                    }
                    */
                }
                data.ReportDocumentValues.Add("RawName", rawName);
                data.ReportDocumentValues.Add("Num", Roll.CheckGet("NUM"));
                data.ReportDocumentValues.Add("Lngth", $"{Roll.CheckGet("LNGTH").ToInt()} м");
                data.ReportDocumentValues.Add("Qty", $"{Roll.CheckGet("QTY").ToInt()} кг");
                data.ReportDocumentValues.Add("Reel", $"{Roll.CheckGet("REEL_SIDE")}");
                data.ReportDocumentValues.Add("Shift", $"{Roll.CheckGet("SMENA")}");
                data.ReportDocumentValues.Add("IdRoll", Roll.CheckGet("ID_ROLL"));
                data.ReportDocumentValues.Add("DateNow", DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                data.ReportDocumentValues.Add("WetList", Roll.CheckGet("WET_LIST"));


                reportDocument.XamlData=tpl;
                XpsDocument xps = reportDocument.CreateXpsDocumentKey(data,"RollReceipt");
                Document=xps;

                Complete=true;
                Report=$"{Report}\n готово";
            }
            return Complete;
        }

        private Bitmap RotateImage(Bitmap bmp) {
             int angle=90;
             Bitmap rotatedImage = new Bitmap(bmp.Height, bmp.Width);             
             rotatedImage.SetResolution(bmp.HorizontalResolution, bmp.VerticalResolution);

             using (Graphics g = Graphics.FromImage(rotatedImage)) {
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
        private void ReportDocumentImageProcessing(object sender,ImageEventArgs e)
        {
            if(e.Image.Name == "ReceiptBarcode2")
            {
                if(!string.IsNullOrEmpty(ReceiptBarcode))
                {
                    if(ReceiptBarcodeBitmap != null)
                    {
                        MemoryStream mem = new MemoryStream();
                        ReceiptBarcodeBitmap.Save(mem,System.Drawing.Imaging.ImageFormat.Bmp);
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

        private void ReportDocumentImageError(object sender,ImageErrorEventArgs e)
        {
            e.Handled = true;
        }

    }
}
