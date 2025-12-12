using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Xps.Packaging;
using Client.Common;
using Client.Common.Extensions;
using Client.Interfaces.Main;
using Client.Interfaces.Service.Printing;
using CodeReason.Reports;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;

namespace Client.Interfaces.Stock
{
    class BlankLabelReport : LabelReport
    {
        private Assembly CurrentAssembly { get; set; }

        private ListDataSet LabelDS;

        public string PrintingProfile { get; set; }

        public string IdPz { get; set; }
        public string Num { get; set; }
        
        /// <summary>
        /// Ид прихода (idp)
        /// </summary>
        public int ComingId { get; set; }

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

        public ReportDocument ReportDocumentData { get; set; }

        public Dictionary<string, string> LabelData { get; set; }

        public ReportData Data { get; set; }

        /// <summary>
        /// Сгенерированный документ, который можно отпарвить на печать или предпросмотр
        /// </summary>
        public override System.Windows.Xps.Packaging.XpsDocument XPS { get; set; }

        public BlankLabelReport()
        {
            CurrentAssembly = Assembly.GetExecutingAssembly();

            ReportTemplate = "Client.Reports.Stock.BlankLabel.xaml";

            CompileTemplate = true;

            PrintingProfile = PrintingSettings.LabelPrinter.ProfileName;
        }


        private void LoadData()
        {
            var lastCursor = Mouse.OverrideCursor;

            Mouse.OverrideCursor = Cursors.Wait;

            var p = new Dictionary<string, string>();
            {
                p.Add("idpz", IdPz);
                p.Add("num", Num);
            }

            if (ComingId > 0)
            {
                p.Add("idp", ComingId.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Label");
            q.Request.SetParam("Action", "GetBlank");
            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    LabelDS = ListDataSet.Create(result, "Label");

                    if (LabelDS != null && LabelDS.Items != null && LabelDS.Items.Count > 0)
                    {

                    }
                    else
                    {
                        SendReport("Пустой датасет", null);
                    }
                }
                else
                {
                    SendReport("Пустой результат дессириализации ответа сервера", null);
                }
            }
            else
            {
                q.ProcessError();
            }

            Mouse.OverrideCursor = lastCursor;

        }

        /// <summary>
        /// скрывает секцию в отчете
        /// по сути ставит в начале подчеркивание, а сам генератор игнорирует такие секции
        /// может быть в section.Name & TableRowForDataTable.tableName
        /// </summary>
        /// <param name="tpl"></param>
        /// <param name="sectionName"></param>
        private void HideSection(ref string tpl, string sectionName)
        {
            if (sectionName != string.Empty)
            {
                tpl = tpl.Replace(sectionName, $"_{sectionName}");
            }
        }

        /// <summary>
        /// Сгенерированный документ, который можно отпарвить на печать или предпросмотр
        /// </summary>
        public override void CreateDocument()
        {
            LoadData();

            LabelData = LabelDS?.GetFirstItem();

            if (LabelData != null)
            {
                if (!(LabelData.CheckGet("KOL").ToInt() > 0))
                {
                    var msg = "Количество картона на поддоне равно 0.";
                    var d = new DialogWindow($"{msg}", "Печать ярлыков", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
                else
                {
                    ReportDocumentData = new ReportDocument();

                    //var stream = CurrentAssembly.GetManifestResourceStream(reportTemplate);
                    var stream = CompileTemplate
                        ? CurrentAssembly.GetManifestResourceStream(ReportTemplate)
                        : File.OpenRead(ReportTemplate);

                    var reader = new StreamReader(stream);

                    ReportDocumentData.XamlData = reader.ReadToEnd();
                    ReportDocumentData.XamlImagePath = Path.Combine(Environment.CurrentDirectory, @"Templates\");
                    ReportDocumentData.ImageProcessing += ReportDocumentImageProcessing;
                    ReportDocumentData.ImageError += ReportDocumentImageError;
                    reader.Close();

                    var tpl = ReportDocumentData.XamlData;
                    Data = new ReportData();

                    // Если нет информации по рилёвкам, ты скрываем секцию с текстом Внимание Рилёвки
                    if (
                        LabelData.CheckGet("H").ToInt() > 0
                        || LabelData.CheckGet("P1").ToInt() > 0
                        || LabelData.CheckGet("P2").ToInt() > 0
                        || LabelData.CheckGet("P3").ToInt() > 0
                        || LabelData.CheckGet("P4").ToInt() > 0
                        || LabelData.CheckGet("P5").ToInt() > 0
                        || LabelData.CheckGet("P6").ToInt() > 0
                        || LabelData.CheckGet("P7").ToInt() > 0
                        || LabelData.CheckGet("P8").ToInt() > 0
                        || LabelData.CheckGet("P9").ToInt() > 0
                        || LabelData.CheckGet("P10").ToInt() > 0
                        || LabelData.CheckGet("P11").ToInt() > 0
                        || LabelData.CheckGet("P12").ToInt() > 0
                        || LabelData.CheckGet("P13").ToInt() > 0
                        || LabelData.CheckGet("P14").ToInt() > 0
                        || LabelData.CheckGet("P15").ToInt() > 0
                        || LabelData.CheckGet("P16").ToInt() > 0
                        || LabelData.CheckGet("P17").ToInt() > 0
                        || LabelData.CheckGet("P18").ToInt() > 0
                        || LabelData.CheckGet("P19").ToInt() > 0
                        || LabelData.CheckGet("P20").ToInt() > 0
                        || LabelData.CheckGet("P21").ToInt() > 0
                        || LabelData.CheckGet("P22").ToInt() > 0
                        || LabelData.CheckGet("P23").ToInt() > 0
                        || LabelData.CheckGet("P24").ToInt() > 0
                        )
                    {
                       
                    }
                    else
                    {
                        HideSection(ref tpl, "ScoringSection");
                    }

                    var idPoddon = LabelData.CheckGet("ID_PODDON").ToInt().ToString();

                    var verificationCode = LabelData.CheckGet("VERIFICATION_CODE").ToString();

                    if (!string.IsNullOrEmpty(verificationCode) && verificationCode.Length >= 3)
                    {
                        Data.ReportDocumentValues.Add("digit3", verificationCode.Substring(0, 1));
                        Data.ReportDocumentValues.Add("digit2", verificationCode.Substring(1, 1));
                        Data.ReportDocumentValues.Add("digit1", verificationCode.Substring(2, 1));
                    }
                    else
                    {
                        Data.ReportDocumentValues.Add("digit3", idPoddon.Length >= 3 ? idPoddon.Substring(idPoddon.Length - 3, 1) : "");
                        Data.ReportDocumentValues.Add("digit2", idPoddon.Length >= 3 ? idPoddon.Substring(idPoddon.Length - 2, 1) : "");
                        Data.ReportDocumentValues.Add("digit1", idPoddon.Length >= 3 ? idPoddon.Substring(idPoddon.Length - 1, 1) : "");
                    }

                    Data.ReportDocumentValues.Add("qid", LabelData.CheckGet("QID"));
                    Data.ReportDocumentValues.Add("ect", LabelData.CheckGet("ECT"));
                    Data.ReportDocumentValues.Add("qid_and_ect", $"{LabelData.CheckGet("QID")} {LabelData.CheckGet("ECT").ToDouble()}");
                    Data.ReportDocumentValues.Add("thiknes", LabelData.CheckGet("THIKNES"));
                    Data.ReportDocumentValues.Add("barcode", ((long)LabelData.CheckGet("BARCODE").ToDecimal()).ToString("D13"));
                    Data.ReportDocumentValues.Add("QActualSizeL", LabelData.CheckGet("QACTUALSIZEL").ToInt());
                    Data.ReportDocumentValues.Add("QActualSizeB", LabelData.CheckGet("QACTUALSIZEB").ToInt());

                    BarcodeKshContent = LabelData.CheckGet("BARCODE_KSH");
                    if (string.IsNullOrEmpty(LabelData.CheckGet("BARCODE_KSH")))
                    {
                        HideSection(ref tpl, "BarcodeKshSection");
                    }

                    var palletNumberBatch = "";
                    // Количесвто поддонов, взятое из БД
                    int defaultPalletQuantity = LabelData.CheckGet("NUM_PALLET").ToInt();
                    // Расчитанное количество поддонов
                    int calculatedPalletQuantity = LabelData.CheckGet("CALCULATED_NUM_PALLET").ToInt();
                    // Отображаемое количество поддонов
                    int resultPalletQuantity = 0;

                    if (defaultPalletQuantity > calculatedPalletQuantity)
                    {
                        resultPalletQuantity = defaultPalletQuantity;
                    }
                    else
                    {
                        resultPalletQuantity = calculatedPalletQuantity;
                    }

                    if (resultPalletQuantity > 0)
                    {
                        palletNumberBatch =
                            $"{LabelData.CheckGet("PALLET_NUMBER_BY_TASK").ToInt()}/{resultPalletQuantity}";
                    }
                    else
                    {
                        palletNumberBatch = $"{LabelData.CheckGet("PALLET_NUMBER_BY_TASK").ToInt()}";
                    }

                    Data.ReportDocumentValues.Add("page1", $"{LabelData.CheckGet("PAGE1").ToInt()}");
                    Data.ReportDocumentValues.Add("pallet_number_batch", palletNumberBatch);
                    Data.ReportDocumentValues.Add("kol2", LabelData.CheckGet("KOL2").ToInt());
                    if (LabelData.CheckGet("TESTING_FLAG").ToInt() > 0)
                    {
                        Data.ReportDocumentValues.Add("testing_flag", "Л");
                    }
                    else
                    {
                        Data.ReportDocumentValues.Add("testing_flag", " ");
                    }

                    // Работа с значками М/О/Л
                    {
                        if (LabelData.CheckGet("MASTER_FLAG").ToInt() == 0)
                        {
                            tpl = tpl.Replace("Assets/Images/Reports/MasterFlag.png", "null");
                        }

                        if (LabelData.CheckGet("SAMPLE_FLAG").ToInt() == 0)
                        {
                            tpl = tpl.Replace("Assets/Images/Reports/SampleFlag.png", "null");
                        }

                        if (LabelData.CheckGet("TESTING_FLAG").ToInt() == 0)
                        {
                            tpl = tpl.Replace("Assets/Images/Reports/TestingFlagBlack.png", "null");
                        }
                    }

                    Data.ReportDocumentValues.Add("prihod_pz_kol", LabelData.CheckGet("PRIHOD_PZ_KOL").ToInt());

                    var num = LabelData.CheckGet("NUM");
                    Data.ReportDocumentValues.Add("num", num);
                    Data.ReportDocumentValues.Add("numSmall", num.Length > 4 ? num.Substring(0, 4) : num);

                    var num2 = LabelData.CheckGet("NUM2");
                    Data.ReportDocumentValues.Add("num2", num2.ToInt());
                    Data.ReportDocumentValues.Add("num2Small", num2.Length > 4 ? num2.Substring(0, 4) : num2);
                    //data.ReportDocumentValues.Add("num2", label.CheckGet("NUM2").ToInt());

                    var stanok = LabelData.CheckGet("STANOK");
                    Data.ReportDocumentValues.Add("stanok", stanok);

                    Data.ReportDocumentValues.Add("kol_pak", LabelData.CheckGet("KOL_PAK").ToInt());

                    Data.ReportDocumentValues.Add("kol", LabelData.CheckGet("KOL").ToInt());

                    Data.ReportDocumentValues.Add("nshtanz", LabelData.CheckGet("nshtanz".ToUpper()));
                    Data.ReportDocumentValues.Add("nklishe", LabelData.CheckGet("nklishe".ToUpper()));
                    Data.ReportDocumentValues.Add("reference_sample", LabelData.CheckGet("REFERENCE_SAMPLE".ToUpper()));
                    Data.ReportDocumentValues.Add("name_otgr", LabelData.CheckGet("name_otgr".ToUpper()));

                    Data.ReportDocumentValues.Add("strdttm", LabelData.CheckGet("strdttm".ToUpper()));
                    Data.ReportDocumentValues.Add("smname", LabelData.CheckGet("smname".ToUpper()));
                    Data.ReportDocumentValues.Add("artikul", LabelData.CheckGet("artikul".ToUpper()));
                    Data.ReportDocumentValues.Add("id_pz", LabelData.CheckGet("id_pz".ToUpper()));
                    Data.ReportDocumentValues.Add("cartonnum", LabelData.CheckGet("cartonnum".ToUpper()));
                    Data.ReportDocumentValues.Add("id2", LabelData.CheckGet("id2".ToUpper()));

                    string name = LabelData.CheckGet("NAME");
                    if (!string.IsNullOrEmpty(name))
                    {
                        name = name.Replace(" для", $"{Environment.NewLine}для");
                    }
                    Data.ReportDocumentValues.Add("name", name);
                    Data.ReportDocumentValues.Add("id_pz_next", LabelData.CheckGet("id_pz_next".ToUpper()));
                    Data.ReportDocumentValues.Add("id2_next", LabelData.CheckGet("id2_next".ToUpper()));

                    // машина на которой печатается ярлык. на комплектации пусто
                    switch (LabelData.CheckGet("CREATOR_MACHINE_ID").ToInt())
                    {
                        case 2:
                            Data.ReportDocumentValues.Add("stname", "ГА-1 (Шейла)");
                            break;

                        case 21:
                            Data.ReportDocumentValues.Add("stname", "ГА-2 (Антон)"); 
                            break;

                        case 22:
                            Data.ReportDocumentValues.Add("stname", "ГА-3 (Фосбер)");
                            break;

                        default:
                            Data.ReportDocumentValues.Add("stname", " ");
                            break;
                    }

                    // Собираем документ
                    try
                    {
                        ReportDocumentData.XamlData = tpl;
                        XPS = ReportDocumentData.CreateXpsDocumentKey(Data, $"StockLabel_{Guid.NewGuid()}");
                    }
                    catch (Exception exception)
                    {
                        var msg = "";
                        msg = $"{msg}Не удалось создать документ.\n";
                        msg = $"{msg}Пожалуйста, запустите создание документа снова.\n";
                        var d = new DialogWindow(msg, "Ошибка создания документа");
                        d.ShowDialog();

                        SendReport(msg, exception);
                    }
                }
            }
        }

        /// <summary>
        /// Печать ярлыка
        /// 
        /// доп. инфа: размер ленты, перевод миллиметров (mm) в пиксели (pix)
        /// https://bbf.ru/converter/93/417/
        /// </summary>
        public override void Print()
        {
            var result=false;
            CreateDocument();

            // сама печать
            try
            {
                var copyCount = 1; //label["LABEL_COPY_QTY"].ToInt();

                var continuePrinting = true;
                if (copyCount > 10 || Central.DebugMode && copyCount > 1)
                {
                    continuePrinting =
                        DialogWindow.ShowDialog(
                            $"Для данного товара необходимо распечатать {copyCount} копий. Продолжить?",
                            "Требуется подтверждение", "", DialogWindowButtons.YesNo) == true;

                }

                if (continuePrinting)
                {
                    for (var i = 0; i < copyCount; i++)
                    {
                        // в режиме отладки просматриваем документ и можем отклонить печать
                        if (Central.DebugMode)
                        {
                            ShowDocument(XPS);
                            break;
                        }
                        else
                        {
                            PrintDocument(XPS);
                        }
                    }
                    result=true;
                }
            }
            catch (Exception exception)
            {
                var msg = "";
                msg = $"{msg}Не удалось напечатать документ.\n";
                msg = $"{msg}Пожалуйста, запустите создание документа снова.\n";
                var d = new DialogWindow(msg, "Ошибка создания документа");
                d.ShowDialog();

                SendReport(msg, exception);
            }

            if(result)
            {
                var palletId=LabelData.CheckGet("ID_PODDON").ToInt();
                UpdateReprintNumber(palletId);
            }
        }

        public void PrintDocument(XpsDocument xps)
        {
            var printHelper = new PrintHelper();
            printHelper.PrintingProfile = PrintingProfile;
            printHelper.PrintingDuplex = System.Drawing.Printing.Duplex.Simplex;
            printHelper.Init();
            var printingResult = printHelper.StartPrinting(xps);
            printHelper.Dispose();
        }

        private void UpdateReprintNumber(int palletId)
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
        /// Вызов стандартной формы предпросмотра документа
        /// </summary>
        public override void Show()
        {
            CreateDocument();

            try
            {
                ShowDocument(XPS);
            }
            catch (Exception exception)
            {
                var msg = "";
                msg = $"{msg}Не удалось отобразить документ.\n";
                msg = $"{msg}Пожалуйста, запустите создание документа снова.\n";
                var d = new DialogWindow(msg, "Ошибка создания документа");
                d.ShowDialog();

                SendReport(msg, exception);
            }
        }

        public void ShowDocument(XpsDocument xps)
        {
            var printHelper = new PrintHelper();
            printHelper.PrintingProfile = PrintingProfile;
            printHelper.PrintingDuplex = System.Drawing.Printing.Duplex.Simplex;
            printHelper.Init();
            var printingResult = printHelper.ShowPreview(xps);
            printHelper.Dispose();
        }

        /// <summary>
        /// Отправка сообщений об ошибке на сервер
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="exception"></param>
        public void SendReport(string msg, Exception exception)
        {
            try
            {
                var error = new Error();
                error.Code = 145;
                error.Message = $"{msg}. {exception?.Message}. XPS == NULL:{XPS == null}.";
                error.Description = "";
                Central.ProcError(error, "", true);
            }
            catch (Exception ex)
            {
            }
        }

        private byte[] uklJpgBlob;

        private string BarcodeKshContent;

        /// <summary>
        /// генератор штрих-кода
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReportDocumentImageProcessing(object sender, ImageEventArgs e)
        {
            if (e.Image.Name == "UKLJPGImage")
            {
                var mem = new MemoryStream(uklJpgBlob) { Position = 0 };
                var image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = mem;
                image.EndInit();
                e.Image.Source = image;
            }

            if (e.Image.Name == "BarcodeKsh")
            {
                var file = GetBarcode(BarcodeKshContent, 4, 100, 100);
                if (!file.IsNullOrEmpty())
                {
                    if (System.IO.File.Exists(file))
                    {
                        var fs = new FileStream(file, FileMode.Open, FileAccess.Read);
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.StreamSource = fs;
                        image.EndInit();
                        e.Image.Source = image;
                    }
                }
            }
        }

        /// <summary>
        /// format: 1=EAN13,2=CODE128,3=GS1_DM,4=QR_CODE,5-GS1_128
        /// </summary>
        /// <param name="code"></param>
        /// <param name="format">1=EAN13,2=CODE128,3=GS1_DM,4=QR_CODE,5-GS1_128</param>
        /// <returns></returns>
        private string GetBarcode(string code, int format, int width = 400, int height = 400)
        {
            var result = Client.Interfaces.Service.BarcodeGenerator.GetBarcodeFile(code, format, width, height);
            return result;
        }

        private void ReportDocumentImageError(object sender, ImageErrorEventArgs e)
        {
            e.Handled = true;
        }
    }
}
