using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
using Client.Interfaces.Service.Printing;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Printing;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
using static DevExpress.XtraPrinting.Native.ExportOptionsPropertiesNames;
using Microsoft.Office.Interop.Excel;
using Microsoft.Win32;
using System.Threading;

namespace Client.Interfaces.Service.Printing
{
    /// <summary>
    /// вспомогательный класс, централизирует в себе работу по печати документов.
    /// 
    /// Локальные настройки печати имеют приоритет перед настройками профиля печати
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-09-20</released>
    /// <changed>2023-09-22</changed>
    public class PrintHelper : IDisposable
    {
        public PrintHelper()
        {
            Initialized = false;
            PrintingProfile = PrintingSettings.Default.ProfileName;
            PrintingDocumentFile = "";
            DocumentPaginator = null;
            PrintingSettings = new PrintingSettings();
            PrintingCopies = 1;
            PrintingDuplex = Duplex.Default;
            PrintingLandscape = null;
            ErrorLog = "";
            PrintingPreview = null;
            PrintPreview = null;
            PrintDocument = null;
            PdfDocument = null;
            PrintDialog = null;
            PrintCopiesThreadSleepTime = 0;
        }

        /// <summary>
        /// Флаг инициализации класса
        /// </summary>
        private bool Initialized {get;set;}

        /// <summary>
        /// Наименование профиля печати
        /// </summary>
        public string PrintingProfile { get; set; }

        /// <summary>
        /// Количество копий документа на печать.
        /// Будет использовано наибольшее значение среди этого параметра и настройки из профиля печати.
        /// </summary>
        public int PrintingCopies { get; set; }

        /// <summary>
        /// Режим многостраничной печати.
        /// Если задано значение, то оно будет использоваться при печати.
        /// Если значение не задано или Default, то настройка возьмётся из профиля печати.
        /// </summary>
        public Duplex PrintingDuplex { get; set; }

        public bool? PrintingLandscape { get; set; }

        /// <summary>
        /// Задержка между вызовами системной печати документа при печати нескольких копий документа, миллисекунды
        /// </summary>
        public int PrintCopiesThreadSleepTime { get; set; }

        private int SuccesfullPrintedCopiesCount { get; set; }

        /// <summary>
        /// Настройки из профиля печати
        /// </summary>
        private PrintingSettings PrintingSettings { get; set; }

        /// <summary>
        /// Внешнее сообщение об ошибке.
        /// В случае любых проблем здесь будет какая-то информация.
        /// </summary>
        public string ErrorLog { get; set; }

        /// <summary>
        /// Форма предпросмотра xps документов
        /// </summary>
        private PrintPreview PrintPreview { get; set; }

        /// <summary>
        /// Форма предпросмотра pdf документов
        /// </summary>
        private PrintingPreview PrintingPreview { get; set; }

        private DocumentPaginator DocumentPaginator { get; set; }

        /// <summary>
        /// Путь к печатаемому pdf документу
        /// </summary>
        private string PrintingDocumentFile { get; set; }

        private PrintDocument PrintDocument { get; set; }

        private PdfiumViewer.PdfDocument PdfDocument { get; set; }

        private PrintDialog PrintDialog { get; set; }

        #region Default

        /// <summary>
        /// Инициализаия класса. Проводит получение настроек по профилю печати. Если настройки получены успешно, то инициализация пройдена успешно
        /// </summary>
        public void Init()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Initialized = CheckGetPrintingSettings();
            });
        }

        /// <summary>
        /// Сохраняем репорт о клиентской ошибке
        /// </summary>
        public void SendErrorReport(string msg)
        {
            var q = new LPackClientQuery();
            // Отключаем стандартное всплывающее сообщение с ошибкой и отправляем репорт
            q.Answer.Error.Message = msg;
            q.SilentErrorProcess = true;
            q.Answer.Status = 145;
            q.ProcessError();
        }

        /// <summary>
        /// Освобождение ресурсов
        /// </summary>
        public void Dispose()
        {
            try
            {
                PrintDocument?.Dispose();
                PdfDocument?.Dispose();
            }
            catch (Exception ex)
            {
                SendErrorReport(ex.Message);
                ErrorLog = ErrorLog.Append($"Dispose error [{ex.Message}]", true);
            }
        }

        public Dictionary<string, string> DebugGetSettings(string printingDocumentFile)
        {
            var result = new Dictionary<string, string>();
            result.CheckAdd("PRINTING_PROFILE", PrintingProfile);
            result.CheckAdd("PRINTING_DOCUMENT_FILE", printingDocumentFile);
            {
                var printingSettings = GetDictionaryFromPrintingSettings(GetPrintingSettingsFromRegistry(PrintingProfile));
                foreach (KeyValuePair<string, string> item in printingSettings)
                {
                    result.CheckAdd(item.Key, item.Value);
                }
            }
            return result;
        }

        #endregion

        #region GetPrintingSettings

        public Dictionary<string, string> GetDictionaryFromPrintingSettings(PrintingSettings printingSettings)
        {
            var p = new Dictionary<string, string>();

            if (printingSettings != null && printingSettings.Initialized)
            {
                p.CheckAdd("NAME", printingSettings.ProfileName);
                p.CheckAdd("DESCRIPTION", printingSettings.Description);
                p.CheckAdd("COPIES", printingSettings.Copies.ToString());
                p.CheckAdd("PRINTER_NAME", printingSettings.PrinterFullName);
                p.CheckAdd("WIDTH", printingSettings.Width.ToString());
                p.CheckAdd("HEIGHT", printingSettings.Height.ToString());
                p.CheckAdd("MARGIN_LEFT", printingSettings.MarginLeft.ToString());
                p.CheckAdd("MARGIN_TOP", printingSettings.MarginTop.ToString());
                p.CheckAdd("MARGIN_RIGHT", printingSettings.MarginRight.ToString());
                p.CheckAdd("MARGIN_BOTTOM", printingSettings.MarginBottom.ToString());
                p.CheckAdd("LANDSCAPE", printingSettings.Landscape.ToInt().ToString());

                if (printingSettings.Duplex == Duplex.Simplex)
                {
                    p.CheckAdd("DUPLEX", "1");
                }
                else if (printingSettings.Duplex == Duplex.Horizontal || printingSettings.Duplex == Duplex.Vertical)
                {
                    p.CheckAdd("DUPLEX", "2");
                }
                else if (printingSettings.Duplex == Duplex.Default)
                {
                    p.CheckAdd("DUPLEX", "3");
                }
            }

            return p;
        }

        public PrintingSettings GetPrintingSettingsFromDictionary(Dictionary<string, string> p)
        {
            PrintingSettings printingSettings = new PrintingSettings();

            printingSettings.ProfileName = p.CheckGet("NAME");
            printingSettings.Description = p.CheckGet("DESCRIPTION");
            printingSettings.Copies = p.CheckGet("COPIES").ToInt();
            printingSettings.PrinterFullName = p.CheckGet("PRINTER_NAME");
            printingSettings.Width = p.CheckGet("WIDTH").ToInt();
            printingSettings.Height = p.CheckGet("HEIGHT").ToInt();
            printingSettings.MarginLeft = p.CheckGet("MARGIN_LEFT").ToInt();
            printingSettings.MarginTop = p.CheckGet("MARGIN_TOP").ToInt();
            printingSettings.MarginRight= p.CheckGet("MARGIN_RIGHT").ToInt();
            printingSettings.MarginBottom  = p.CheckGet("MARGIN_BOTTOM").ToInt();

            if (p.CheckGet("DUPLEX").ToInt() == 1)
            {
                printingSettings.Duplex = Duplex.Simplex;
            }
            else if (p.CheckGet("DUPLEX").ToInt() == 2)
            {
                if (printingSettings.Width > printingSettings.Height)
                {
                    printingSettings.Duplex = Duplex.Horizontal;
                }
                else
                {
                    printingSettings.Duplex = Duplex.Vertical;
                }
            }
            else if (p.CheckGet("DUPLEX").ToInt() == 3)
            {
                printingSettings.Duplex = Duplex.Default;
            }

            printingSettings.Landscape = p.CheckGet("LANDSCAPE").ToBool();

            printingSettings.ParsePrinterFullName();

            if (p != null && p.Count > 0 && !string.IsNullOrEmpty(p.CheckGet("PRINTER_NAME")))
            {
                printingSettings.Initialized = true;
            }

            return printingSettings;
        }

        /// <summary>
        /// Получение настроек печати по умолчанию. 
        /// Будет выбран системный принтер по умолчанию и настройки для печати А4.
        /// </summary>
        /// <returns></returns>
        private PrintingSettings GetPrintingSettingsFromDefault()
        {
            PrintingSettings printingSettings = PrintingSettings.GetPrintingSettingsByProfileName(PrintingSettings.Default.ProfileName);
            var printerdefaultQueu = LocalPrintServer.GetDefaultPrintQueue();
            if (printerdefaultQueu != null)
            {
                printingSettings.PrinterFullName = printerdefaultQueu.FullName;
            }
            else
            {
                printingSettings.PrinterFullName = this.GetPrintingSettingsFromSystem().PrinterFullName;
            }
            printingSettings.ParsePrinterFullName();

            if (!string.IsNullOrEmpty(printingSettings.PrinterFullName))
            {
                printingSettings.Initialized = true;
            }

            return printingSettings;
        }

        /// <summary>
        /// Получение настроек печати по заданному профилю печати.
        /// </summary>
        /// <param name="printingProfile"></param>
        /// <returns></returns>
        private PrintingSettings GetPrintingSettingsFromRegistry(string printingProfile)
        {
            var p = new Dictionary<string, string>();
            {
                p = Central.AppSettings.SectionFindRow("PRINTING_SETTINGS", "NAME", printingProfile);
            }

            PrintingSettings printingSettings = GetPrintingSettingsFromDictionary(p);

            return printingSettings;
        }

        /// <summary>
        /// Получение настроек печати через диалог выбора принтера.
        /// </summary>
        /// <returns></returns>
        public PrintingSettings GetPrintingSettingsFromSystem()
        {
            /*
                показать диалог настройки принтера
                считать из него параметры 
             */
            var p = new Dictionary<string, string>();
            {
                var printDialog = new PrintDialog();
                var printDialogResult = (bool)printDialog.ShowDialog();
                if (printDialogResult)
                {
                    if (printDialog.PrintQueue != null)
                    {
                        p.CheckAdd("PRINTER_NAME", printDialog.PrintQueue.FullName.ToString());
                    }

                    if (printDialog.PrintTicket != null)
                    {
                        p.CheckAdd("COPIES", printDialog.PrintTicket.CopyCount.ToString());

                        var w = (int)printDialog.PrintTicket.PageMediaSize.Width;
                        var h = (int)printDialog.PrintTicket.PageMediaSize.Height;

                        w = (int)((double)w / (double)10 * (double)2.54);
                        h = (int)((double)h / (double)10 * (double)2.54);

                        p.CheckAdd("WIDTH", w.ToString());
                        p.CheckAdd("HEIGHT", h.ToString());
                    }

                    p.CheckAdd("MARGIN_LEFT", "0");
                    p.CheckAdd("MARGIN_TOP", "0");
                    p.CheckAdd("MARGIN_RIGHT", "0");
                    p.CheckAdd("MARGIN_BOTTOM", "0");
                }
            }

            PrintingSettings printingSettings = GetPrintingSettingsFromDictionary(p);

            return printingSettings;
        }

        /// <summary>
        /// Заполнение PrintingSettings параметрами печати по указанному профилю печати PrintingProfile.
        /// Если параметры профиля не заданы, то попробует создать их.
        /// </summary>
        /// <returns></returns>
        public bool CheckGetPrintingSettings()
        {
            bool result = true;

            try
            {
                if (PrintingProfile == PrintingSettings.Default.ProfileName)
                {
                    // необходимо взять принтер по умолчанию
                    PrintingSettings = GetPrintingSettingsFromDefault();
                    if (!PrintingSettings.Initialized)
                    {
                        result = false;

                        string msg = $"Не найдены настройки для профиля печати по умолчанию. PrintingProfile = [{PrintingProfile}]";
                        ErrorLog = ErrorLog.Append($"{msg}", true);
                    }
                }
                else
                {
                    // Пытаемся получить принтер из пользовательских настроек
                    PrintingSettings = GetPrintingSettingsFromRegistry(PrintingProfile);
                    if (!PrintingSettings.Initialized)
                    {
                        string msg = $"Не найдены настройки для указанного профиля печати. PrintingProfile = [{PrintingProfile}]";
                        ErrorLog = ErrorLog.Append($"{msg}", true);

                        // Если не найдены пользовательские настройки для указанного принтера, то проверяем, что это профиль по умолчанию
                        PrintingSettings defaultPrintingSettings = PrintingSettings.GetPrintingSettingsByProfileName(PrintingProfile);
                        if (defaultPrintingSettings != null)
                        {
                            // Если это профиль по умолчанию, то создаём автоматический пользовательский профиль печати для профиля по умолчанию
                            try
                            {
                                defaultPrintingSettings.PrinterFullName = this.GetPrintingSettingsFromSystem().PrinterFullName;
                                if (!string.IsNullOrEmpty(defaultPrintingSettings.PrinterFullName))
                                {
                                    defaultPrintingSettings.Initialized = true;
                                    var p = GetDictionaryFromPrintingSettings(defaultPrintingSettings);
                                    Central.AppSettings.SectionAddRow("PRINTING_SETTINGS", p);
                                    Central.AppSettings.Store();

                                    PrintingSettings = GetPrintingSettingsFromRegistry(PrintingProfile);
                                    if (!PrintingSettings.Initialized)
                                    {
                                        msg = $"Не удалось автоматически создать профиль по умолчанию для указанного профиля печати. PrintingProfile = [{PrintingProfile}]. PrinterName = [{p.CheckGet("PRINTER_NAME")}].";
                                        SendErrorReport(msg);
                                        ErrorLog = ErrorLog.Append($"{msg}", true);

                                        var d = new DialogWindow($"Не задан профиль печати [{PrintingProfile}].{Environment.NewLine}Пожалуйста, создайте и настройте профиль печати через вкладку Сервис/Печать.", "Ошибка печати");
                                        d.ShowDialog();

                                        result = false;
                                    }
                                }
                                else
                                {
                                    var d = new DialogWindow($"Не задан профиль печати [{PrintingProfile}].{Environment.NewLine}Пожалуйста, создайте и настройте профиль печати через вкладку Сервис/Печать.", "Ошибка печати");
                                    d.ShowDialog();

                                    result = false;
                                }
                            }
                            catch (Exception ex)
                            {
                                msg = $"Не удалось автоматически создать профиль по умолчанию для указанного профиля печати. PrintingProfile = [{PrintingProfile}]. [{ex.Message}].";
                                SendErrorReport(msg);
                                ErrorLog = ErrorLog.Append($"{msg}", true);

                                var d = new DialogWindow($"Не задан профиль печати [{PrintingProfile}].{Environment.NewLine}Пожалуйста, создайте и настройте профиль печати через вкладку Сервис/Печать.", "Ошибка печати");
                                d.ShowDialog();

                                result = false;
                            }
                        }
                        else
                        {
                            var d = new DialogWindow($"Не задан профиль печати [{PrintingProfile}].{Environment.NewLine}Пожалуйста, создайте и настройте профиль печати через вкладку Сервис/Печать.", "Ошибка печати");
                            d.ShowDialog();

                            result = false;
                        }
                    }
                }

                if (PrintingSettings.Initialized)
                {
                    if (PrintingCopies > PrintingSettings.Copies)
                    {
                        PrintingSettings.Copies = PrintingCopies;
                    }

                    if (PrintingDuplex != Duplex.Default)
                    {
                        PrintingSettings.Duplex = PrintingDuplex;
                    }

                    if (PrintingLandscape != null)
                    {
                        PrintingSettings.Landscape = (bool)PrintingLandscape;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLog = ErrorLog.Append($"GetPrintingSettings error [{ex.Message}]", true);
                SendErrorReport(ex.Message);
                result = false;
            }

            return result;
        }

        #endregion

        #region ShowPreview

        public bool ShowPreview(XpsDocument xps)
        {
            bool result = false;

            if (!Initialized)
            {
                result = false;
                ErrorLog = ErrorLog.Append($"not Initialized", true);
            }
            else
            {
                if (xps != null)
                {
                    DocumentPaginator = xps.GetFixedDocumentSequence().DocumentPaginator;
                    if (DocumentPaginator != null)
                    {
                        try
                        {
                            PrintPreview = new PrintPreview();
                            PrintPreview.documentViewer.Document = DocumentPaginator.Source;
                            PrintPreview.PrintingSettings = this.PrintingSettings;
                            PrintPreview.Show();

                            result = true;
                        }
                        catch (Exception ex)
                        {
                            SendErrorReport(ex.Message);
                            ErrorLog = ErrorLog.Append($"ShowPreview error {ex.ToString()}", true);
                        }
                    }
                    else
                    {
                        ErrorLog = ErrorLog.Append($"error documentPaginator not exists", true);
                    }
                }
                else
                {
                    ErrorLog = ErrorLog.Append($"error xps file not exists", true);
                }
            }

            return result;
        }

        public bool ShowPreview(DocumentPaginator documentPaginator)
        {
            bool result = false;

            if (!Initialized)
            {
                result = false;
                ErrorLog = ErrorLog.Append($"not Initialized", true);
            }
            else
            {
                DocumentPaginator = documentPaginator;
                try
                {
                    PrintPreview = new PrintPreview();
                    PrintPreview.documentViewer.Document = DocumentPaginator.Source;
                    PrintPreview.PrintingSettings = this.PrintingSettings;
                    PrintPreview.Show();

                    result = true;
                }
                catch (Exception ex)
                {
                    SendErrorReport(ex.Message);
                    ErrorLog = ErrorLog.Append($"ShowPreview error {ex.ToString()}", true);
                }
            }

            return result;
        }

        public bool ShowPreview(string printingDocumentFile)
        {
            var resume = true;
            var result = true;

            if (!Initialized)
            {
                result = false;
                ErrorLog = ErrorLog.Append($"not Initialized", true);
            }
            else
            {
                PrintingDocumentFile = printingDocumentFile;
                try
                {
                    if (System.IO.Path.GetExtension(PrintingDocumentFile).ToUpper() == ".PDF")
                    {
                        PrintingPreview = new PrintingPreview();
                        PrintingPreview.PrintHelper = this;
 
                        if (resume)
                        {
                            PrintingPreview.RenderPreviewPdf(PrintingDocumentFile);
                        }

                        bool printerNotSelected = false;

                        if (resume)
                        {
                            if (!PrintingSettings.Initialized)
                            {
                                printerNotSelected = true;
                                result = false;
                                ErrorLog = ErrorLog.Append($"GetPrinterSettingsFromRegistry error", true);
                            }

                            PrintingPreview.SetPrintingSettings(GetDictionaryFromPrintingSettings(PrintingSettings));
                        }

                        PrintingPreview.PrinterNotSelected = printerNotSelected;
                        PrintingPreview.Open();

                        if (printerNotSelected)
                        {
                            ErrorLog = ErrorLog.Append($"PrinterNotSelected", true);
                        }
                    }
                    else
                    {
                        Central.OpenFile(PrintingDocumentFile);
                    }
                }
                catch (Exception ex)
                {
                    SendErrorReport(ex.Message);
                    ErrorLog = ErrorLog.Append($"ShowPreview error {ex.Message}", true);
                    result = false;
                }
            }

            return result;
        }

        #endregion

        #region StartPrinting

        public bool StartPrinting(string printingDocumentFile)
        {
            bool result = false;

            if (!Initialized)
            {
                result = false;
                ErrorLog = ErrorLog.Append($"not Initialized", true);
            }
            else
            {
                PrintingDocumentFile = printingDocumentFile;
                if (!string.IsNullOrEmpty(PrintingDocumentFile))
                {
                    // Пока умеем печатать только ПДФ, всё остальное отправляем на просмотр
                    if (System.IO.Path.GetExtension(PrintingDocumentFile).ToUpper() == ".PDF")
                    {
                        try
                        {
                            result = StartPrintingPdf();
                        }
                        catch (Exception ex)
                        {
                            SendErrorReport($"Print error {ex.ToString()}");

                            result = true;
                            ShowPreview(PrintingDocumentFile);
                        }
                    }
                    else
                    {
                        result = true;
                        ShowPreview(PrintingDocumentFile);
                    }
                }
                else
                {
                    ErrorLog = ErrorLog.Append($"error file not exists [{PrintingDocumentFile}]", true);
                }
            }

            return result;
        }

        public bool StartPrinting(string printingDocumentFile, int attemptCount)
        {
            bool result = false;

            int _attemptCount = attemptCount;
            if (!(_attemptCount > 0))
            {
                _attemptCount = 1;
            }

            if (!Initialized)
            {
                result = false;
                ErrorLog = ErrorLog.Append($"not Initialized", true);
            }
            else
            {
                PrintingDocumentFile = printingDocumentFile;
                if (!string.IsNullOrEmpty(PrintingDocumentFile))
                {
                    // Пока умеем печатать только ПДФ, всё остальное отправляем на просмотр
                    if (System.IO.Path.GetExtension(PrintingDocumentFile).ToUpper() == ".PDF")
                    {
                        try
                        {
                            for (int i = 0; i < _attemptCount; i++)
                            {
                                result = StartPrintingPdf();
                                if (result)
                                {
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            SendErrorReport($"Print error [StartPrintingPdf()] {ex.Message} {ex.ToString()}");

                            result = true;
                            ShowPreview(PrintingDocumentFile);
                        }
                    }
                    else
                    {
                        result = true;
                        ShowPreview(PrintingDocumentFile);
                    }
                }
                else
                {
                    ErrorLog = ErrorLog.Append($"error file not exists [{PrintingDocumentFile}]", true);
                }
            }

            return result;
        }

        public bool StartPrinting(XpsDocument xps)
        {
            bool result = false;

            if (!Initialized)
            {
                result = false;
                ErrorLog = ErrorLog.Append($"not Initialized", true);
            }
            else
            {
                if (xps != null)
                {
                    DocumentPaginator = xps.GetFixedDocumentSequence().DocumentPaginator;
                    if (DocumentPaginator != null)
                    {
                        try
                        {
                            result = StartPrintingDocumentPaginator();
                        }
                        catch (Exception ex)
                        {
                            SendErrorReport($"Print error {ex.ToString()}");

                            result = true;
                            ShowPreview(DocumentPaginator);
                        }
                    }
                    else
                    {
                        ErrorLog = ErrorLog.Append($"error documentPaginator not exists", true);
                    }
                }
                else
                {
                    ErrorLog = ErrorLog.Append($"error xps file not exists", true);
                }
            }

            return result;
        }

        public bool StartPrinting(DocumentPaginator documentPaginator)
        {
            bool result = false;

            if (!Initialized)
            {
                result = false;
                ErrorLog = ErrorLog.Append($"not Initialized", true);
            }
            else
            {
                DocumentPaginator = documentPaginator;
                if (DocumentPaginator != null)
                {
                    try
                    {
                        result = StartPrintingDocumentPaginator();
                    }
                    catch (Exception ex)
                    {
                        SendErrorReport($"Print error {ex.ToString()}");

                        result = true;
                        ShowPreview(DocumentPaginator);
                    }
                }
                else
                {
                    ErrorLog = ErrorLog.Append($"error documentPaginator not exists", true);
                }
            }

            return result;
        }

        private bool StartPrintingDocumentPaginator()
        {
            var resume = true;
            var result = false;

            if (!Initialized)
            {
                resume = false;
            }

            if (resume)
            {
                var setPrinterSettingsResult = SetPrinterSettingsDocumentPaginator();
                if (!setPrinterSettingsResult)
                {
                    resume = false;
                    ErrorLog = ErrorLog.Append($"SetPrinterSettings error", true);
                }
            }

            if (resume)
            {
                try
                {
                    for (int i = 0; i < PrintingSettings.Copies; i++)
                    {
                        PrintDialog.PrintDocument(DocumentPaginator, "");
                    }

                    result = true;
                }
                catch (Exception ex)
                {
                    ErrorLog = ErrorLog.Append($"Print error {ex.ToString()}", true);
                    SendErrorReport(ex.Message);
                }
            }

            return result;
        }

        private bool SetPrinterSettingsDocumentPaginator()
        {
            var resume = true;
            var result = false;

            if (resume)
            {
                try
                {
                    PrintDialog = new PrintDialog();
                    PrintServer printServer = new PrintServer(!string.IsNullOrEmpty(PrintingSettings.PrinterServerName)? PrintingSettings.PrinterServerName: null);
                    var pq = printServer.GetPrintQueue(PrintingSettings.PrinterQueueName);
                    PrintDialog.PrintQueue = pq; //FIXME передача других параметров печати

                    if (PrintingSettings.Landscape != null)
                    {
                        if ((bool)PrintingSettings.Landscape)
                        {
                            PrintDialog.PrintTicket.PageOrientation = PageOrientation.Landscape;
                        }
                        else
                        {
                            PrintDialog.PrintTicket.PageOrientation = PageOrientation.Portrait;
                        }
                    }

                    switch (PrintingSettings.Duplex)
                    {
                        case Duplex.Default:
                        default:
                            PrintDialog.PrintTicket.Duplexing = Duplexing.Unknown;
                            break;

                        case Duplex.Simplex:
                            PrintDialog.PrintTicket.Duplexing = Duplexing.OneSided;
                            break;

                        case Duplex.Horizontal:
                            PrintDialog.PrintTicket.Duplexing = Duplexing.TwoSidedShortEdge;
                            break;

                        case Duplex.Vertical:
                            PrintDialog.PrintTicket.Duplexing = Duplexing.TwoSidedLongEdge;
                            break;
                    }

                    result = true;
                }
                catch (Exception ex)
                {
                    SendErrorReport(ex.Message);
                    ErrorLog = ErrorLog.Append($"error SetPrinterSettings [{ex.Message}]", true);
                }
            }

            return result;
        }

        private bool CreatePrintDocumentPdf()
        {
            var resume = true;
            var result = false;

            if (resume)
            {
                if (!System.IO.File.Exists(PrintingDocumentFile))
                {
                    resume = false;
                    ErrorLog = ErrorLog.Append($"error file not exists [{PrintingDocumentFile}]", true);
                }
            }

            if (resume)
            {
                try
                {
                    PdfDocument = PdfiumViewer.PdfDocument.Load(PrintingDocumentFile);
                    PrintDocument = PdfDocument.CreatePrintDocument();
                    result = true;
                }
                catch (Exception ex)
                {
                    ErrorLog = ErrorLog.Append($"pdf rendering error [{PrintingDocumentFile}]", true);
                    SendErrorReport(ex.Message);
                }
            }

            return result;
        }

        /// <summary>
        /// извлекает массив с параметрами принтера и
        /// устанавливает их внутри объекта printDocument
        /// </summary>
        /// <param name="printDocument"></param>
        /// <returns></returns>
        private bool SetPrinterSettingsPdf()
        {
            var resume = true;
            var result = false;

            if (resume)
            {
                if (PrintDocument == null)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                try
                {
                    var printerSettings = new PrinterSettings
                    {
                        PrinterName = PrintingSettings.PrinterFullName,
                        Copies = (short)1,
                    };

                    var pageSettings = new PageSettings(printerSettings)
                    {
                        Margins = new Margins(
                            PrintingSettings.MarginLeft,
                            PrintingSettings.MarginRight,
                            PrintingSettings.MarginTop,
                            PrintingSettings.MarginBottom
                        ),
                    };

                    var w = PrintingSettings.Width;
                    var h = PrintingSettings.Height;

                    w = (int)((double)w / (double)2.54 * (double)10);
                    h = (int)((double)h / (double)2.54 * (double)10);

                    pageSettings.PaperSize = new System.Drawing.Printing.PaperSize(
                        "custom",
                        w,
                        h
                    );

                    if (PrintingSettings.Duplex == Duplex.Horizontal || PrintingSettings.Duplex == Duplex.Vertical)
                    {
                        if (PdfDocument != null && PdfDocument.PageSizes != null)
                        {
                            float pdfWidth = PdfDocument.PageSizes.First().Width;
                            float pdfHeight = PdfDocument.PageSizes.First().Height;

                            if (pdfWidth > pdfHeight)
                            {
                                PrintingSettings.Duplex = Duplex.Horizontal;
                            }
                            else
                            {
                                PrintingSettings.Duplex = Duplex.Vertical;
                            }
                        }
                    }

                    printerSettings.Duplex = PrintingSettings.Duplex;

                    PrintDocument.PrintController = new System.Drawing.Printing.StandardPrintController();
                    PrintDocument.PrinterSettings = printerSettings;
                    PrintDocument.DefaultPageSettings = pageSettings;

                    // Если заданна ориентация листа, зададим ее в настройках 
                    if (PrintingSettings.Landscape != null)
                    {
                        if (PrintDocument.DefaultPageSettings != null)
                        {
                            PrintDocument.DefaultPageSettings.Landscape = (bool)PrintingSettings.Landscape;
                        }
                    }

                    result = true;
                }
                catch (Exception ex)
                {
                    SendErrorReport(ex.Message);
                    ErrorLog = ErrorLog.Append($"error SetPrinterSettings [{ex.Message}]", true);
                }
            }

            return result;
        }

        private bool StartPrintingPdf()
        {
            var resume = true;
            var result = false;

            if (!Initialized)
            {
                resume = false;
            }

            if (resume)
            {
                var createPrintDocumentResult = CreatePrintDocumentPdf();
                if (!createPrintDocumentResult)
                {
                    resume = false;
                    ErrorLog = ErrorLog.Append($"CreatePrintDocument error", true);
                }
            }

            if (resume)
            {
                var setPrinterSettingsResult = SetPrinterSettingsPdf();
                if (!setPrinterSettingsResult)
                {
                    resume = false;
                    ErrorLog = ErrorLog.Append($"SetPrinterSettings error", true);
                }
            }

            if (resume)
            {
                try
                {
                    int printCopiesThreadSleepTime = PrintCopiesThreadSleepTime;
                    if (printCopiesThreadSleepTime < 0)
                    {
                        printCopiesThreadSleepTime = 0;
                    }

                    if (SuccesfullPrintedCopiesCount >= PrintingSettings.Copies)
                    {
                        SuccesfullPrintedCopiesCount = 0;
                    }
                    int succesfullPrintedCopiesCount = SuccesfullPrintedCopiesCount;
                    if (succesfullPrintedCopiesCount < 0)
                    {
                        succesfullPrintedCopiesCount = 0;
                    }

                    if (PrintingSettings.Copies == 1)
                    {
                        printCopiesThreadSleepTime = 0;
                        succesfullPrintedCopiesCount = 0;
                    }

                    for (int i = 0; i < PrintingSettings.Copies - succesfullPrintedCopiesCount; i++)
                    {
                        PrintDocument.Print();

                        {
                            SuccesfullPrintedCopiesCount++;
                            if (SuccesfullPrintedCopiesCount >= PrintingSettings.Copies)
                            {
                                SuccesfullPrintedCopiesCount = 0;
                            }
                        }

                        if (printCopiesThreadSleepTime > 0)
                        {
                            Thread.Sleep(printCopiesThreadSleepTime);
                        }
                    }

                    result = true;
                }
                catch (Exception ex)
                {
                    ErrorLog = ErrorLog.Append($"Print error [PrintDocument.Print()] {ex.Message} {ex.ToString()}", true);
                    SendErrorReport(ex.Message);
                }
            }

            return result;
        }

        #endregion

        #region SaveDocument

        public bool SaveDocument(string printingDocumentFile)
        {
            bool result = false;

            PrintingDocumentFile = printingDocumentFile;
            if (!System.IO.File.Exists(PrintingDocumentFile))
            {
                ErrorLog = ErrorLog.Append($"error file not exists [{PrintingDocumentFile}]", true);
            }
            else
            {
                try
                {
                    Central.SaveFile((int)Central.MainWindow.Width / 2, (int)Central.MainWindow.Height / 2, PrintingDocumentFile, true);
                    result = true;                   
                }
                catch (Exception ex)
                {
                    ErrorLog = ErrorLog.Append($"error SaveFiles [{ex.Message}]", true);
                    SendErrorReport($"SaveFiles error {ex.ToString()}");
                }
            }

            return result;
        }

        public bool SaveDocument(XpsDocument xps)
        {
            bool result = false;

            if (xps != null)
            {
                DocumentPaginator = xps.GetFixedDocumentSequence().DocumentPaginator;
                if (DocumentPaginator != null)
                {
                    result = SaveDocument(DocumentPaginator);
                }
                else
                {
                    ErrorLog = ErrorLog.Append($"error documentPaginator not exists", true);
                }
            }
            else
            {
                ErrorLog = ErrorLog.Append($"error xps file not exists", true);
            }

            return result;
        }

        public bool SaveDocument(DocumentPaginator documentPaginator)
        {
            bool result = false;
            DocumentPaginator = documentPaginator;

            try
            {
                string fileName = "DocumentPaginator";
                //string host = XpsDocument.Uri.Host;
                //if (!string.IsNullOrEmpty(host) && host.ToLower().Contains(".xps"))
                //{
                //    fileName = host.Substring(0, host.IndexOf(".xps"));
                //}
                fileName = $"{fileName}_{DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss")}";

                var fd = new SaveFileDialog();
                fd.FileName = $"{fileName}";
                var fdResult = fd.ShowDialog();

                if (fdResult == true && !string.IsNullOrEmpty(fd.FileName))
                {
                    string file_prefix = fd.FileName;

                    // Get a fixed paginator for the document.
                    IDocumentPaginatorSource page_source =
                        DocumentPaginator.Source;
                    DocumentPaginator paginator =
                        page_source.DocumentPaginator;

                    // Process the document's pages.
                    int num_pages = paginator.PageCount;
                    for (int i = 0; i < num_pages; i++)
                    {
                        using (DocumentPage page = paginator.GetPage(i))
                        {
                            // Render the page into the memory stream.
                            int width = (int)page.Size.Width;
                            int height = (int)page.Size.Height;
                            RenderTargetBitmap bitmap =
                                new RenderTargetBitmap(
                                    width, height, 96, 96,
                                    PixelFormats.Default);
                            bitmap.Render(page.Visual);

                            // Save the PNG file.
                            PngBitmapEncoder encoder = new PngBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(bitmap));

                            using (MemoryStream stream = new MemoryStream())
                            {
                                encoder.Save(stream);

                                using (FileStream file = new FileStream(
                                    file_prefix + (i + 1).ToString() + ".png",
                                    FileMode.Create))
                                {
                                    file.Write(stream.GetBuffer(), 0,
                                        (int)stream.Length);
                                    file.Close();
                                }
                            }
                        }
                    }
                }

                result = true;
            }
            catch (Exception ex)
            {
                ErrorLog = ErrorLog.Append($"error SaveFiles [{ex.Message}]", true);
                SendErrorReport($"SaveFiles error {ex.ToString()}");
            }

            return result;
        }

        #endregion


    }
}
