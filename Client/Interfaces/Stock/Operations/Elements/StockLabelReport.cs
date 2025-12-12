using Client.Common;
using Client.Common.Extensions;
using Client.Interfaces.Main;
using Client.Interfaces.Service.Printing;
using CodeReason.Reports;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Xps.Packaging;
using System.Xml;

namespace Client.Interfaces.Stock
{
    class StockLabelReport : LabelReport
    {
        private Assembly CurrentAssembly { get; set; }

        public string IdPz { get; set; }
        public string Num { get; set; }

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

        public string PrintingProfile { get; set; }

        public StockLabelReport()
        {
            CurrentAssembly = Assembly.GetExecutingAssembly();

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
            ReportTemplate = "Client.Reports.Stock.StockLabel.xaml";
            CompileTemplate = true;

            //(2)
            //ReportTemplate = "Reports/Stock/StockLabel.xaml";
            //CompileTemplate=false;

            PrintingProfile = PrintingSettings.LabelPrinter.ProfileName;
        }

        private ListDataSet LabelDS;

        private void LoadData()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            var p = new Dictionary<string, string>();
            {
                p.Add("idpz", IdPz);
                p.Add("num", Num);
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Label");
            q.Request.SetParam("Action", "Get");
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
                        foreach (var item in LabelDS.Items)
                        {
                            item.CheckAdd("PRODUCTION_NOTES", "Гофропродукция должна храниться в закрытых проветриваемых помещениях при температуре -14 до +40 C " +
                                "и относительной влажности 25-70%, не ближе 1.5м от отопительных приборов. " +
                                $"Утилизировать в макулатуру. Срок годности {item.CheckGet("SHELF_LIFE").ToInt()} мес. Страна изготовления: Россия");
                            //// Если клиент это АО ПРОГРЕСС, то заменяем стандартный текст в конце ярлыка
                            //if (item.CheckGet("CUSTOMER_ID").ToInt() == 2885)
                            //{
                            //    item.CheckAdd("PRODUCTION_NOTES", "Условия хранения: В закрытых складских помещениях, защищенных от атмосферных осадков и прямых солнечных лучей " +
                            //        "при температуре (10-25) °С и относительной влажности не более 70% на расстоянии не менее 1 м от отопительных приборов. " +
                            //        $"Утилизировать в макулатуру. Срок годности {item.CheckGet("SHELF_LIFE").ToInt()} мес. Страна изготовления: Россия");
                            //}
                        }
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
        /// 
        /// </summary>
        /// <param name="tpl"></param>
        /// <param name="sectionType"></param>
        /// <param name="sectionName"></param>
        private void RemoveSection(ref string tpl, string sectionType, string sectionName)
        {
            if (sectionName != string.Empty && sectionType != string.Empty)
            {
                var doc = new XmlDocument();
                doc.LoadXml(tpl);

                var node = doc.DocumentElement?.SelectSingleNode($"//*[local-name()='{sectionType}'][@Name='{sectionName}']");
                node?.ParentNode?.RemoveChild(node);

                tpl = doc.OuterXml;
            }
        }

        /// <summary>
        /// скрывает список секций
        /// </summary>
        /// <param name="tpl"></param>
        /// <param name="sectionNameList"></param>
        private void HideSectionList(ref string tpl, params string[] sectionNameList)
        {
            foreach (var sectionName in sectionNameList)
            {
                HideSection(ref tpl, sectionName);
            }
        }


        /// <summary>
        /// делает видимой секцию в отчете
        /// </summary>
        /// <param name="tpl"></param>
        /// <param name="sectionName"></param>
        private void ShowSection(ref string tpl, string sectionName)
        {
            tpl = tpl.Replace($"_{sectionName}", sectionName);
        }


        /// <summary>
        /// функция вырезает символы из строки и ставит пробелы если есть какие-то проблемы по длине
        /// </summary>
        /// <param name="s"></param>
        /// <param name="startIndex"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        private string GetSubStr(string s, int startIndex, int len)
        {
            var r = "";

            if (startIndex + len <= s.Length)
            {
                r = s.Substring(startIndex, len);
            }
            else if (startIndex < s.Length)
            {
                r = s.Substring(startIndex);
                for (var i = 0; i < startIndex + len - s.Length; i++)
                {
                    r += " ";
                }
            }
            else
            {
                for (var i = 0; i < len; i++)
                {
                    r += " ";
                }
            }

            return r;
        }

        /// <summary>
        /// Формируем документ
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
                    var stream = CompileTemplate ? CurrentAssembly.GetManifestResourceStream(ReportTemplate) : File.OpenRead(ReportTemplate);

                    var reader = new StreamReader(stream);

                    ReportDocumentData.XamlData = reader.ReadToEnd();
                    ReportDocumentData.XamlImagePath = Path.Combine(Environment.CurrentDirectory, @"Templates\");
                    ReportDocumentData.ImageProcessing += ReportDocumentImageProcessing;
                    ReportDocumentData.ImageError += ReportDocumentImageError;
                    reader.Close();

                    var tpl = ReportDocumentData.XamlData;
                    Data = new ReportData();

                    //var systemName = $"{Central.Parameters.SystemName} {Central.Parameters.BaseLabel}";

                    // Если нет информации по рилёвкам, ты скрываем секцию с текстом Внимание Рилёвки
                    if (
                        string.IsNullOrEmpty(LabelData.CheckGet("P1"))
                        && string.IsNullOrEmpty(LabelData.CheckGet("P2"))
                        && string.IsNullOrEmpty(LabelData.CheckGet("P3"))
                        && string.IsNullOrEmpty(LabelData.CheckGet("P4"))
                        && string.IsNullOrEmpty(LabelData.CheckGet("P5"))
                        && string.IsNullOrEmpty(LabelData.CheckGet("P6"))
                        && string.IsNullOrEmpty(LabelData.CheckGet("P7"))
                        && string.IsNullOrEmpty(LabelData.CheckGet("P8"))
                        && string.IsNullOrEmpty(LabelData.CheckGet("P9"))
                        && string.IsNullOrEmpty(LabelData.CheckGet("P10"))
                        && string.IsNullOrEmpty(LabelData.CheckGet("P11"))
                        && string.IsNullOrEmpty(LabelData.CheckGet("P12"))
                        && string.IsNullOrEmpty(LabelData.CheckGet("P13"))
                        && string.IsNullOrEmpty(LabelData.CheckGet("P14"))
                        && string.IsNullOrEmpty(LabelData.CheckGet("P15"))
                        && string.IsNullOrEmpty(LabelData.CheckGet("P16"))
                        && string.IsNullOrEmpty(LabelData.CheckGet("P17"))
                        && string.IsNullOrEmpty(LabelData.CheckGet("P18"))
                        && string.IsNullOrEmpty(LabelData.CheckGet("P19"))
                        && string.IsNullOrEmpty(LabelData.CheckGet("P20"))
                        && string.IsNullOrEmpty(LabelData.CheckGet("P21"))
                        && string.IsNullOrEmpty(LabelData.CheckGet("P22"))
                        && string.IsNullOrEmpty(LabelData.CheckGet("P23"))
                        && string.IsNullOrEmpty(LabelData.CheckGet("P24"))
                        )
                    {
                        HideSection(ref tpl, "ScoringSection");
                    }

                    /*
                     * информация по товару
                     */
                    // артикул
                    var artikul = LabelData.CheckGet("ARTIKUL");

                    var realMarka = "";
                    if (!LabelData.CheckGet("PRINTARTIKUL").IsNullOrEmpty())
                    {
                        artikul = LabelData["PRINTARTIKUL"];
                        if (artikul.Length > 21)
                        {
                            realMarka = artikul.Substring(19 - 1, 2);
                        }
                    }

                    Data.ReportDocumentValues.Add("RealMarka", realMarka);

                    //artikulParts.Length
                    Data.ReportDocumentValues.Add("ArtikulKodPotreb", GetSubStr(artikul, 1 - 1, 3));
                    Data.ReportDocumentValues.Add("ArtikulKodProd", GetSubStr(artikul, 5 - 1, 3));
                    Data.ReportDocumentValues.Add("ArtikulKodIzd", GetSubStr(artikul, 9 - 1, 2));
                    Data.ReportDocumentValues.Add("ArtikulVar", GetSubStr(artikul, 12 - 1, 1));
                    Data.ReportDocumentValues.Add("ArtikulProf", GetSubStr(artikul, 14 - 1, 2));
                    Data.ReportDocumentValues.Add("ArtikulColor", GetSubStr(artikul, 17 - 1, 1));
                    Data.ReportDocumentValues.Add("ArtikulMarka", GetSubStr(artikul, 19 - 1, 2));

                    // [IF([KODIZD]='---00', 
                    var artikulKodIzd = GetSubStr(artikul, 9 - 1, 2);
                    var svidetelstvoDate = artikulKodIzd == "00" ? "24.05.2012" : "02.02.2011";
                    var svidetelstvo =
                        $"Свидетельство о государственной регистрации от {svidetelstvoDate}г. Срок действия свидетельства на весь период изготовления продукции или поставок подконтрольных товаров на территорию таможенного союза";

                    Data.ReportDocumentValues.Add("GOST", LabelData.CheckGet("GOST"));
                    Data.ReportDocumentValues.Add("REFERENCE_SAMPLE", LabelData.CheckGet("REFERENCE_SAMPLE"));
                    //data.ReportDocumentValues.Add("NOT_BARCODE", );

                    Data.ReportDocumentValues.Add("TU", LabelData.CheckGet("TU"));
                    Data.ReportDocumentValues.Add("TU_DESCRIPTION", LabelData.CheckGet("TU_DESCRIPTION"));

                    Data.ReportDocumentValues.Add("PRODUCTION_NOTES", LabelData.CheckGet("PRODUCTION_NOTES"));
                    //Data.ReportDocumentValues.Add("ShelfLife", LabelData.CheckGet("SHELF_LIFE").ToInt());

                    // количество товара
                    Data.ReportDocumentValues.Add("ProductAmount", LabelData.CheckGet("KOL").ToInt());

                    // типы строковые в БД поэтому такое сравнение, а не через bool

                    // Наличие подпрессовки паллета 
                    Data.ReportDocumentValues.Add("ProductPress", LabelData.CheckGet("PRESS").ToInt() == 1 ? "ДА" : "НЕТ");

                    // Наличие уголка на паллете
                    Data.ReportDocumentValues.Add("ProductUgolok", LabelData.CheckGet("UGOLOK").ToInt() == 1 ? "ДА" : "НЕТ");

                    // Вариант обмотки паллет плёнкой
                    Data.ReportDocumentValues.Add("ProductOBM",
                        LabelData.CheckGet("SIGNOT").ToInt() == 0 ? "НЕТ" : $"F{LabelData.CheckGet("SIGNOT")}");

                    // обвязка
                    Data.ReportDocumentValues.Add("ProductOBV", LabelData.CheckGet("MOD_SHORT"));

                    // Вариант обвязки паллет лентой
                    Data.ReportDocumentValues.Add("ProductUP", LabelData.CheckGet("OBVAZ"));

                    Data.ReportDocumentValues.Add("ProductPrim", LabelData.CheckGet("DETAILS"));

                    Data.ReportDocumentValues.Add("ProductName", LabelData.CheckGet("NAME2"));

                    // Кол-во на поддоне по умолчанию
                    Data.ReportDocumentValues.Add("ProductKolPak", LabelData.CheckGet("KOL_PAK"));

                    // код укладки
                    Data.ReportDocumentValues.Add("UKLCode", LabelData.CheckGet("KOD"));

                    // Кол-во в пачке(стопе)
                    Data.ReportDocumentValues.Add("ProductHUKL",
                        LabelData.CheckGet("H_UKL").ToInt() != 0 ? $"В пачке: {LabelData.CheckGet("H_UKL")}" : "");

                    // картинка укладки
                    uklJpgBlob = Convert.FromBase64String(LabelData.CheckGet("JPG"));

                    var productHukl2 = string.Empty;

                    if (LabelData.CheckGet("H_UKL").ToInt() != 0 && LabelData.CheckGet("UKL_KOL").ToInt() != 0)
                    {
                        var rowsCount = Math.Round(LabelData.CheckGet("KOL_PAK").ToDecimal() /
                                                   LabelData.CheckGet("H_UKL").ToDecimal() / LabelData.CheckGet("UKL_KOL").ToInt())
                            .ToInt();
                        productHukl2 = $"Рядов: {rowsCount}";
                    }

                    // Рядов 
                    Data.ReportDocumentValues.Add("ProductHUKL2", productHukl2);

                    // время печати ярлык по текущему заданию
                    Data.ReportDocumentValues.Add("SMNAME", LabelData.CheckGet("SMNAME"));

                    // название смены которая распечатала ярлык
                    Data.ReportDocumentValues.Add("STRDTTM", LabelData.CheckGet("PRINT_DTTM"));

                    Data.ReportDocumentValues.Add("LabelCopyQuantity", $"{LabelData.CheckGet("LABEL_COPY_QTY").ToInt()}");

                    // (номер и дата партии) Номер и дата изготовления. тут берется исторический или если он null его замена
                    string outerNum = "";
                    if (!string.IsNullOrEmpty(LabelData.CheckGet("IDORDERDATES_MIN_PZ")))
                    {
                        outerNum = $"{LabelData.CheckGet("IDORDERDATES_MIN_PZ").ToInt()}";
                    }
                    else
                    {
                        outerNum = $"{LabelData.CheckGet("NUM_PZ")} {LabelData.CheckGet("ID_PZ")}";
                    }

                    if (!string.IsNullOrEmpty(LabelData.CheckGet("OUTER_DATE_BY_ATTR")))
                    {
                        outerNum = $"{outerNum} {LabelData.CheckGet("OUTER_DATE_BY_ATTR")}";
                    }
                    else
                    {
                        outerNum = $"{outerNum} {DateTime.Now:dd.MM.yyyy}";
                    }

                    Data.ReportDocumentValues.Add("OuterNum", outerNum);

                    //var outerNum = LabelData.CheckGet("OUTER_NUM_BY_ORDERDATES");
                    //if (string.IsNullOrEmpty(outerNum))
                    //{
                    //    if (!string.IsNullOrEmpty(LabelData.CheckGet("OUTER_DATE_BY_ATTR")))
                    //    {
                    //        outerNum = $"{LabelData.CheckGet("OUTER_NUM_BY_ATTR")} {LabelData.CheckGet("OUTER_DATE_BY_ATTR")}";
                    //    }
                    //    else
                    //    {
                    //        outerNum = LabelData.CheckGet("OUTER_NUM");
                    //    }
                    //}

                    //Data.ReportDocumentValues.Add("OuterNum", outerNum);

                    //if (outerNum == "")
                    //{
                    //    HideSection(ref tpl, "sectionOuterNum");

                    //    var batchNumberAndDate =
                    //        $"{LabelData.CheckGet("NUM_PZ")} {LabelData.CheckGet("ID_PZ")} {DateTime.Now:MM.dd.yyyy}";
                    //    Data.ReportDocumentValues.Add("BatchNumberAndDate", batchNumberAndDate);

                    //    ShowSection(ref tpl, "sectionBatchNumberAndDate");
                    //}

                    /*
                     * информация по текущему паллету
                     *
                     */

                    // вес паллета нетто
                    Data.ReportDocumentValues.Add("PalletWeightNet",
                        Math.Round(LabelData.CheckGet("WEIGHT_NET").ToDouble()).ToInt().ToString());

                    // вес паллета брутто
                    Data.ReportDocumentValues.Add("PalletWeightGross",
                        Math.Round(LabelData.CheckGet("WEIGHT_GROSS").ToDouble()).ToInt().ToString());

                    var idPoddon = LabelData.CheckGet("PALLET_ID").ToInt().ToString();

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

                    /*
                     * todo
                     * begin
                      if [LENGTH([Potreb])] <= 9  then
                              Memo9.Font.Size := 28
                    else
                              Memo9.Font.Size := 14;
                      if [PRINT_CUSTOMER]=1 then
                         Memo9.Visible:=true
                      else
                         Memo9.Visible:=false;
                    end
                     */

                    // потребитель
                    Data.ReportDocumentValues.Add("Potreb", LabelData.CheckGet("CUSTOMER"));

                    /*
                     * остальное
                     *
                     */

                    // вариант поллетирования + 
                    //  В случаях, когда можно использовать пластиковый поддон, к наименованию поддона будет дописываться 2 буквы «ПЛ».
                    Data.ReportDocumentValues.Add("OTGR", LabelData.CheckGet("OTGR") + LabelData.CheckGet("PLASTIC_PALLET_TEXT"));

                    // номер паллета в задании
                    if (LabelData.CheckGet("NUM_PALLET").ToInt() != 0)
                    {
                        Data.ReportDocumentValues.Add("NumberPallet", $"{LabelData.CheckGet("NUM").ToInt()}/{LabelData.CheckGet("NUM_PALLET").ToInt()}");
                    }
                    else if (LabelData.CheckGet("CALCULATED_NUM_PALLET").ToInt() != 0)
                    {
                        Data.ReportDocumentValues.Add("NumberPallet", $"{LabelData.CheckGet("NUM").ToInt()}/{LabelData.CheckGet("CALCULATED_NUM_PALLET").ToInt()}");
                    }
                    else
                    {
                        Data.ReportDocumentValues.Add("NumberPallet", LabelData.CheckGet("NUM").ToInt());
                    }

                    // выясняем есть ли диллер или мы продаем
                    var dealer = !string.IsNullOrEmpty(LabelData.CheckGet("DEALER"));
                    Data.ReportDocumentValues.Add("Intermediary", dealer ? LabelData.CheckGet("DEALER") : "ООО \"Л-ПАК\"");

                    // номер задания
                    Data.ReportDocumentValues.Add("Zadan", LabelData.CheckGet("NUMPZ"));

                    // Идентификатор состава сырья по протоколу BHS
                    Data.ReportDocumentValues.Add("QID", LabelData.CheckGet("QID"));

                    Data.ReportDocumentValues.Add("sname", LabelData.CheckGet("SNAME"));
                    Data.ReportDocumentValues.Add("a1name", $"{LabelData.CheckGet("A1NAME")} {LabelData.CheckGet("FRAME")}");
                    Data.ReportDocumentValues.Add("a2name", LabelData.CheckGet("A2NAME"));

                    // метраж нужно 6 знаков после запятой
                    if (!LabelData.CheckGet("MPALLET").IsNullOrEmpty())
                    {
                        double dMpallet = Math.Truncate(1000000 * LabelData.CheckGet("MPALLET").ToDouble()) / 1000000;
                        string sMpallet = dMpallet.ToString("#.#####0");
                        sMpallet = $"{sMpallet} м2";

                        //data.ReportDocumentValues.Add("PalletM",
                        //    (Math.Truncate(1000000 * label.CheckGet("MPALLET").ToDouble()) / 1000000).ToString());

                        Data.ReportDocumentValues.Add("PalletM", sMpallet);
                    }
                    else
                    {
                        Data.ReportDocumentValues.Add("PalletM", "");
                        HideSection(ref tpl, "SectionPalletM");
                    }

                    // Габариты транспортного пакета
                    string transportPackage = " ";
                    if (LabelData.CheckGet("TRANSPORT_PACKAGE_LENGTH").ToInt() > 0 && LabelData.CheckGet("TRANSPORT_PACKAGE_WIDTH").ToInt() > 0)
                    {
                        if (LabelData.CheckGet("TRANSPORT_PACKAGE_HEIGTH").ToInt() > 0)
                        {
                            transportPackage = $"{LabelData.CheckGet("TRANSPORT_PACKAGE_LENGTH").ToInt()}x{LabelData.CheckGet("TRANSPORT_PACKAGE_WIDTH").ToInt()}x{LabelData.CheckGet("TRANSPORT_PACKAGE_HEIGTH").ToInt()}";
                        }
                        else
                        {
                            transportPackage = $"{LabelData.CheckGet("TRANSPORT_PACKAGE_LENGTH").ToInt()}x{LabelData.CheckGet("TRANSPORT_PACKAGE_WIDTH").ToInt()}";
                        }
                    }
                    Data.ReportDocumentValues.Add("TransportPackage", transportPackage);

                    {
                    
                        var customerBarcodeVisible=false;
                        
                        var customerBarcodeContent=LabelData.CheckGet("CUSTOMER_BARCODE_CONTENT");
                        CustomerBarcodeContent=customerBarcodeContent;
                        //1=CODE128,2=GS1_DATAMATRIX,3=EAN13
                        CustomerBarcodeType = LabelData.CheckGet("CUSTOMER_BARCODE_TYPE").ToInt();
                        //0=hidden,1=CODE128,2=GS1_DATAMATRIX,3=EAN13
                        var barcodeType=0;

                        if(!customerBarcodeContent.IsNullOrEmpty())
                        {
                            barcodeType=1;
                            if(CustomerBarcodeType == 2)
                            {
                                barcodeType=2;
                            }
                            else if (CustomerBarcodeType == 3)
                            {
                                barcodeType = 3;
                            }
                        }

                        switch(barcodeType)
                        {
                            case 1:
                                {
                                    ShowSection(ref tpl, "ClientBigBarCode1");
                                    Data.ReportDocumentValues.Add("ClientBarcode", customerBarcodeContent);
                                }
                                break;

                            case 2:
                                {
                                    ShowSection(ref tpl, "ClientBigBarCode2");
                                }
                                break;

                            case 3:
                                {
                                    ShowSection(ref tpl, "ClientBigBarCode3");
                                }
                                break;
                        }

                    }

                    BarcodeKshContent = LabelData.CheckGet("BARCODE_KSH");
                    if (string.IsNullOrEmpty(LabelData.CheckGet("BARCODE_KSH")))
                    {
                        HideSection(ref tpl, "BarcodeKshSection");
                    }


                    //var barcodeOuter = LabelData.CheckGet("CUSTOMER_BARCODE");

                    //Data.ReportDocumentValues.Add("ClientBarcode", barcodeOuter);

                    //if (string.IsNullOrEmpty(barcodeOuter) || barcodeOuter == "0")
                    //{
                    //    HideSection(ref tpl, "ClientBigBarCode");
                    //}
                    //else
                    //{
                    //    ReceiptBarcode = barcodeOuter;
                    //    ReceiptBarcodeBitmap = (Bitmap)Code128Rendering.MakeBarcodeImage(ReceiptBarcode, 1, true);
                    //    ReceiptBarcodeBitmap = RotateImage(ReceiptBarcodeBitmap);
                    //}

                    // при комплектации возникла ошибка
                    var barCode = "";
                    try
                    {
                        barCode = ((long)LabelData.CheckGet("BARCODE").ToDecimal()).ToString("D13");
                    }
                    catch
                    {

                    }
                    Data.ReportDocumentValues.Add("Barcode", barCode);

                    /*
                     * секция beforeload
                     */

                    if (dealer)
                    {
                        HideSectionList(ref tpl, "LPakSection", "LPakAddressSection");
                    }
                    else
                    {
                        HideSection(ref tpl, "IntermediarySection");
                    }

                    // Если есть хотя бы один флаг, то показываем секцию флагов
                    if (LabelData.CheckGet("MASTER_FLAG").ToInt() > 0 
                        || LabelData.CheckGet("SAMPLE_FLAG").ToInt() > 0 
                        || LabelData.CheckGet("TESTING_FLAG").ToInt() > 0)
                    {
                        HideSection(ref tpl, "FlagClearSection");

                        // Обрабатываем флаг (О)Образец
                        {
                            if (LabelData.CheckGet("SAMPLE_FLAG").ToInt() == 0)
                            {
                                tpl = tpl.Replace("Assets/Images/Reports/SampleFlag.png", "null");
                            }
                        }

                        // Обрабатываем флаг (Л)Лаборатория
                        {
                            if (LabelData.CheckGet("TESTING_FLAG").ToInt() == 0)
                            {
                                tpl = tpl.Replace("Assets/Images/Reports/TestingFlag.png", "null");
                            }
                        }

                        // Обрабатываем флаг (М)Мастер
                        {
                            if (LabelData.CheckGet("MASTER_FLAG").ToInt() == 0)
                            {
                                tpl = tpl.Replace("Assets/Images/Reports/MasterFlag.png", "null");
                            }
                        }
                    }
                    // Если нет ни одного флага, то заменяем секцию флагов на отступ
                    else
                    {
                        HideSection(ref tpl, "FlagSection");
                    }

                    // Обрабатываем флаг (К)Отправить в комплектацию
                    if (LabelData.CheckGet("COMPLECTATION_FLAG").ToInt() > 0)
                    {

                    }
                    else
                    {
                        HideSection(ref tpl, "FlagSection2");
                    }

                    /* логика */
                    var fscType = LabelData.CheckGet("FSC")?.ToInt();

                    switch (fscType)
                    {
                        case 1:
                            ShowSection(ref tpl, "ImPackRec");
                            break;

                        case 2:
                            ShowSection(ref tpl, "ImPackMix");
                            break;

                        case 3:
                            ShowSection(ref tpl, "ImBoardRec");
                            break;

                        case 4:
                            ShowSection(ref tpl, "ImBoardMix");
                            break;
                    }

                    // печать возврата
                    if (LabelData.CheckGet("ID_ST").ToInt() == 1800)
                    {
                        ShowSection(ref tpl, "ReturnSection1");

                        HideSectionList(ref tpl,
                            "sectionUKL",
                            "SectionPalletM",
                            "SectionProductDetails",
                            "sectionOTGR",
                            "sectionProductHUNKL"

                        );

                        ShowSection(ref tpl, "ReturnSection2");
                        HideSectionList(ref tpl,
                            "SectionManSign", "declarationInfo", "SectionManSignInfo",
                            "ImPackRec", "ImPackMix", "ImBoardRec", "ImBoardMix"
                        );
                    }

                    // знак food, picturefood, food_flag
                    if (!LabelData.CheckGet("SIGN_FOOD_FLAG").ToBool())
                    {
                        RemoveSection(ref tpl, "Image", "ManFoodSign");
                    }

                    // вертикальная прямая
                    var numPallet = LabelData.CheckGet("NUM").ToInt();
                    if (numPallet == 1)
                    {
                        ShowSection(ref tpl, "Line1");
                    }

                    // Собираем документ
                    try
                    {
                        ReportDocumentData.XamlData = tpl;
                        XPS = ReportDocumentData.CreateXpsDocumentKey(Data, $"StockLabel_{Guid.NewGuid()}");
                    }
                    catch (Exception e)
                    {
                        var msg = "";
                        msg=msg.Append($"Не удалось создать документ.",true);

                        var error = new Error();
                        error.Code = 146;
                        error.Message = msg;
                        error.Description = e.ToString();
                        Central.ProcError(error, "", true);
                    }
                }
            }
        }

        private string CustomerBarcodeContent{get;set;}

        /// <summary>
        /// //1=CODE128,2=GS1_DATAMATRIX,3=EAN13
        /// </summary>
        private int CustomerBarcodeType { get; set; }

        /// <summary>
        /// Печать ярлыка
        /// 
        /// доп. инфа: размер ленты, перевод миллиметров (mm) в пиксели (pix)
        /// https://bbf.ru/converter/93/417/
        /// </summary>
        public  override void Print()
        {
            var result=false;
            CreateDocument();

            try
            {
                var copyCount = LabelData.CheckGet("LABEL_COPY_QTY").ToInt();
                if (copyCount == 0)
                {
                    copyCount = 1;
                }

                var continuePrinting = true;
                if (copyCount > 10 || (Central.DebugMode && copyCount > 1))
                {
                    continuePrinting=(bool)DialogWindow.ShowDialog(
                        $"Для данной продукции необходимо распечатать {copyCount} копий.\nПродолжить?",
                        "Печать ярлыка", 
                        "",
                        DialogWindowButtons.YesNo
                    );
                }

                if (continuePrinting)
                {
                    for (var i = 0; i < copyCount; i++)
                    {
                        PrintDocument(XPS);
                    }
                    result=true;
                }
            }
            catch (Exception e)
            {
                var msg = "";
                msg=msg.Append($"Не удалось напечатать документ.",true);

                var error = new Error();
                error.Code = 146;
                error.Message = msg;
                error.Description = e.ToString();
                Central.ProcError(error, "", true);
            }

            if(result)
            {
                var palletId=LabelData.CheckGet("PALLET_ID").ToInt();
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
            catch (Exception e)
            {
                var msg = "";
                msg=msg.Append($"Не удалось отобразить документ.",true);

                var error = new Error();
                error.Code = 146;
                error.Message = msg;
                error.Description = e.ToString();
                Central.ProcError(error, "", true);
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

        private byte[] uklJpgBlob;

        private string BarcodeKshContent;

        /// <summary>
        /// Takes a bitmap and converts it to an image that can be handled by WPF ImageBrush
        /// </summary>
        /// <param name="src">A bitmap image</param>
        /// <returns>The image as a BitmapImage for WPF</returns>
        public BitmapImage Convert1(Bitmap src)
        {
            MemoryStream ms = new MemoryStream();
            ((System.Drawing.Bitmap)src).Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }

        public string ReceiptBarcode { get; set; }
        public System.Drawing.Bitmap ReceiptBarcodeBitmap { get; set; }
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
            if (e.Image.Name == "ImageX")
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

            if (e.Image.Name == "UKLJPGImage")
            {
                var mem = new MemoryStream(uklJpgBlob) { Position = 0 };
                var image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = mem;
                image.EndInit();
                e.Image.Source = image;
            }

            if (e.Image.Name == "image1")
            {
                Bitmap bmp = new Bitmap(100, 250);

                var g = Graphics.FromImage(bmp);

                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;


                g.DrawRectangle(new Pen(Brushes.White, 200), 0, 0, 200, 200);

                var rectf1 = new RectangleF(0, 0, 70, 70);
                g.DrawString("1", new Font("Arial", 64, FontStyle.Bold), Brushes.Black, rectf1);

                var rectf2 = new RectangleF(0, 65, 70, 70);
                g.DrawString("2", new Font("Arial", 64, FontStyle.Bold), Brushes.Black, rectf2);

                var rectf3 = new RectangleF(0, 130, 70, 70);
                g.DrawString("3", new Font("Arial", 64, FontStyle.Bold), Brushes.Black, rectf3);

                g.Flush();

                e.Image.Source = Convert1(bmp);
            }

            if (e.Image.Name == "ClientBarcodeGS")
            {
                if(!CustomerBarcodeContent.IsNullOrEmpty() && CustomerBarcodeType == 2)
                {
                    var file=GetBarcode(CustomerBarcodeContent, 3);
                    if(!file.IsNullOrEmpty())
                    {
                        if(System.IO.File.Exists(file))
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

            if (e.Image.Name == "ClientBarcodeEan13")
            {
                if (!CustomerBarcodeContent.IsNullOrEmpty() && CustomerBarcodeType == 3)
                {
                    var file = GetBarcode(CustomerBarcodeContent, 1, 500, 300);
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
    }
}
