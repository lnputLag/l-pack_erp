using Client.Common;
using Client.Interfaces.Main;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;
using Client.Assets;
using System.Reflection;
using Newtonsoft.Json;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using NPOI.XWPF.UserModel;
using Microsoft.Office.Interop.Excel;
using DevExpress.XtraExport.Implementation;
using System.Runtime.InteropServices;
using DevExpress.ClipboardSource.SpreadsheetML;
using DevExpress.Export.Xl;
using System.Windows.Controls;
using SixLabors.ImageSharp.PixelFormats;
using DevExpress.Drawing.Internal.Fonts.Interop;
using System.Windows.Ink;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Класс для работы с Excel файлами техкарт
    /// Содержит все основные поля и методы, необходимые для создания и редактирования Excel файлов техкарт
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public class TechnologicalMapExcel
    {

        public TechnologicalMapExcel()
        {
        }
        public TechnologicalMapExcel(Dictionary<string, string> p)
        {
            Parametrs = p;

            FlagReameFile = true;
            FlagRecreateExcelImg = true;
            FlagRecreateExcelImg2 = true;

            FilePathNew = Parametrs["PATHTK_NEW"];
            FilePathWork = Parametrs["PATHTK_CONFIRM"];
            FilePathArchive = Parametrs["PATHTK_ARCHIVE"];

            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();

                // Получаем путь к папке Templates (два уровня выше + Assets/Templates)
                TemplatesPath = Path.Combine(
                    Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).FullName).FullName,
                    "Assets",
                    "Templates");

                // Создаем директорию, если она не существует
                if (!Directory.Exists(TemplatesPath))
                {
                    Directory.CreateDirectory(TemplatesPath);
                }

                SampleFile = Path.Combine(TemplatesPath, "techcard.xlt");

                var ReportTemplate = "Client.Assets.Templates.techcard.xlt";

                using (var stream = assembly.GetManifestResourceStream(ReportTemplate))
                {
                    if (stream == null)
                    {
                        throw new FileNotFoundException($"Ресурс '{ReportTemplate}' не найден в сборке.");
                    }

                    byte[] buffer = new byte[stream.Length];
                    stream.Read(buffer, 0, buffer.Length);

                    using (FileStream fileStream = new FileStream(SampleFile, FileMode.Create))
                    {
                        fileStream.Write(buffer, 0, buffer.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                var d = new DialogWindow($"Ошибка при создании файла шаблона по пути {SampleFile}: {ex.Message}", "Создание Excel", "", DialogWindowButtons.OK);
                d.ShowDialog();
                Console.WriteLine();
            }

            FileNameNew = Parametrs["PATHTK"];
            FileNameOld = Parametrs["PATHTK"];

            Msg = "";

        }

        #region "Переменные"
        public Dictionary<string, string> Parametrs { get; set; }
        public string FileNameNew { get; set; }
        public string FileNameOld { get; set; }

        /// <summary>
        /// Путь к папке с  вспомогательными файлами, идущими в комплекте с сервером
        /// </summary>
        public string TemplatesPath { get; set; }
        /// <summary>
        /// Путь к шаблону эксель файла
        /// </summary>
        public string SampleFile { get; set; }
        /// <summary>
        /// Путь к новым файлам
        /// </summary>
        public string FilePathNew { get; set; }
        /// <summary>
        /// Путь к архивным файлам
        /// </summary>
        public string FilePathArchive { get; set; }
        /// <summary>
        /// Путь к рабочей папке
        /// </summary>
        public string FilePathWork { get; set; }
        /// <summary>
        /// Полный путь к новому файлу эксель тех карты
        /// </summary>
        public string FileFullNameNew { get; set; }


        /// <summary>
        /// Наименование шаблона для эксель файла тех карты
        /// </summary>
        public string TemplateFileNameForExcel { get; set; }
        /// <summary>
        /// Наименование шаблона для PDF файла тех карты
        /// </summary>
        public string TemplateFileNameForPdf { get; set; }


        /// <summary>
        /// Флаг того, что новый эксель файл тех карты успешно создан
        /// </summary>
        public bool FlagSuccessfullWriteNewExcelFile { get; set; }


        /// <summary>
        /// Список словарей с данными по просечкам первой решётки;
        /// Dictionary :
        /// "NUMBER" -- Номер просечки,
        /// "CONTENT" -- Размер просечки.
        /// </summary>
        public List<Dictionary<string, string>> ListDicNotchesParametrsFirst { get; set; }
        /// <summary>
        /// Список словарей с данными по просечкам второй решётки;
        /// Dictionary :
        /// "NUMBER" -- Номер просечки,
        /// "CONTENT" -- Размер просечки.
        /// </summary>
        public List<Dictionary<string, string>> ListDicNotchesParametrsSecond { get; set; }

        public bool FlagSuccessfullUpdatePathTk { get; set; }
        public bool FlagReameFile { get; set; }
        public bool FlagRecreateExcelImg { get; set; }
        public bool FlagRecreateExcelImg2 { get; set; }
        public string Msg { get; set; }
        #endregion

        #region "Вспомогательные функции"
        /// <summary>
        /// Парсим пришедшие в параметры данные по просечкам
        /// </summary>
        public void ParsNotchesParametrs()
        {
            // Парсим пришедший в запросе параметр с данными по просечкам первой решётки
            if (Parametrs["QUANTITY_NOTCHES_FIRST"].ToInt() > 0)
            {
                var stringNotchesFirst = Parametrs["LIST_NOTCHES_FIRST"].ToString();
                string[] arrayStringNotchesFirst = stringNotchesFirst.Split(';');

                ListDicNotchesParametrsFirst = new List<Dictionary<string, string>>();
                ListDicNotchesParametrsSecond = new List<Dictionary<string, string>>();

                foreach (var item in arrayStringNotchesFirst)
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        var dicStringNotchesFirstParametrs = new Dictionary<string, string>();

                        string[] arrayParametrsOfStringNotches = item.Split(':');

                        dicStringNotchesFirstParametrs.Add("NUMBER", arrayParametrsOfStringNotches[0]);
                        dicStringNotchesFirstParametrs.Add("CONTENT", arrayParametrsOfStringNotches[1]);

                        ListDicNotchesParametrsFirst.Add(dicStringNotchesFirstParametrs);
                    }
                }
            }

            // Парсим пришедший в запросе параметр с данными по просечкам второй решётки
            if (Parametrs["TYPE_PRODUCT"].ToInt().ContainsIn(12, 100, 225, 229))
            {
                var stringNotchesSecond = Parametrs["LIST_NOTCHES_SECOND"].ToString();
                string[] arrayStringNotchesSecond = stringNotchesSecond.Split(';');

                foreach (var item in arrayStringNotchesSecond)
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        var dicStringNotchesSecondParametrs = new Dictionary<string, string>();

                        string[] arrayParametrsOfStringNotches = item.Split(':');

                        dicStringNotchesSecondParametrs.Add("NUMBER", arrayParametrsOfStringNotches[0]);
                        dicStringNotchesSecondParametrs.Add("CONTENT", arrayParametrsOfStringNotches[1]);

                        ListDicNotchesParametrsSecond.Add(dicStringNotchesSecondParametrs);
                    }
                }
            }
        }

        public void RenameExcelFile(string nameOld = "", string nameNew = "")
        {
            if (nameOld == "")
            {
                nameOld = FileNameOld;
            }
            if (nameNew == "")
            {
                nameNew = FileNameNew;
            }
            var pathNew = Path.Combine(FilePathNew, nameOld);
            var pathWork = Path.Combine(FilePathWork, nameOld);
            var pathArchive = Path.Combine(FilePathArchive, nameOld);
            var pathTk = "";
            var pathTkNew = "";
            int typeExist = 0;
            if (System.IO.File.Exists(pathNew))
            {
                pathTk = pathNew;
                pathTkNew = Path.Combine(FilePathNew, nameNew);
            }
            if (System.IO.File.Exists(pathWork))
            {
                pathTk = pathWork;
                pathTkNew = Path.Combine(FilePathWork, nameNew);
            }
            if (System.IO.File.Exists(pathArchive))
            {
                pathTk = pathArchive;
                pathTkNew = Path.Combine(FilePathArchive, nameNew);
            }

            try
            {
                File.Move(pathTk, pathTkNew);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
            finally
            {

            }
        }
        public async void UpdatePathTk()
        {
            FlagSuccessfullUpdatePathTk = false;

            var p = new Dictionary<string, string>();
            p.Add("TK_SET", Parametrs["TK_SET"]);
            p.Add("TK_ID_FIRST", Parametrs["TK_ID_FIRST"]);
            p.Add("TK_ID_SECOND", Parametrs["TK_ID_SECOND"]);
            p.Add("FILE_NAME", FileNameNew);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "TechnologicalMapExcel");
            q.Request.SetParam("Action", "UpdatePathTK");

            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEM");
                    if (ds.Items[0].CheckGet("SUCCESS").ToInt() == 1)
                    {
                        FlagSuccessfullUpdatePathTk = true;
                    }
                }

            }
        }

        public void CreateExcelName()
        {
            this.FileNameNew = "";
            // добавить высоту
            FileNameNew += $"{Parametrs["LENGTH_FIRST"].ToString()}x{Parametrs["HEIGHT_FIRST"].ToString()}";

            if (Parametrs["TYPE_PRODUCT"].ToInt().ContainsIn(12, 100, 225, 229))
            {
                FileNameNew += $"(x{Parametrs["QUANTITY_FIRST"].ToString()}) {Parametrs["LENGTH_SECOND"].ToString()}x{Parametrs["HEIGHT_SECOND"].ToString()}(x{Parametrs["QUANTITY_SECOND"].ToString()})";
            }
            else if (Parametrs["TYPE_PRODUCT"].ToInt().ContainsIn(14, 15))
            {
                if (Parametrs["QUANTITY_NOTCHES_FIRST"].ToInt() > 0)
                {
                    FileNameNew += $"({Parametrs["QUANTITY_NOTCHES_FIRST"]}рил.)";
                }
            }
            else if (Parametrs["TYPE_PRODUCT"].ToInt().ContainsIn(8))
            {
                FileNameNew += $"({Parametrs["QUANTITY_NOTCHES_FIRST"]}прос.)";
            }

            if (Parametrs["TYPE_PRODUCT"].ToInt().ContainsIn(12, 225))
            {
                FileNameNew += $" в сборе";
                if (Parametrs["TYPE_PRODUCT"].ToInt()==225)
                {
                    FileNameNew += " К";
                }
            }
            else if (Parametrs["TYPE_PRODUCT"].ToInt().ContainsIn(100, 229))
            {
                FileNameNew += $" не в сборе";
                if (Parametrs["TYPE_PRODUCT"].ToInt()==229)
                {
                    FileNameNew += " К";
                }
            }
            else
            {
                FileNameNew += $" {Parametrs["NAME_FIRST"]}";
            }
            //добавить если есть печать
            FileNameNew += " б_п";

            if (Parametrs["TYPE_PACKAGE"] == "1")
            {
                FileNameNew += " с_уп";
            }
            else if (Parametrs["TYPE_PACKAGE"] == "0")
            {
                FileNameNew += " б_уп";
            }

            FileNameNew += ".xls";

            FileNameNew = FileNameNew.Replace("/", "_");
            FileNameNew = FileNameNew.Replace("*", "х");
            FileNameNew = FileNameNew.Replace("\\", "_");
            FileNameNew = FileNameNew.Replace(":", "_");
            FileNameNew = FileNameNew.Replace("?", "_");
            FileNameNew = FileNameNew.Replace("\"", "_");
            FileNameNew = FileNameNew.Replace(">", "_");
            FileNameNew = FileNameNew.Replace("<", "_");
            FileNameNew = FileNameNew.Replace("|", "_");
        }

        private void ReleaseExcelObjects(params object[] comObjects)
        {
            foreach (var obj in comObjects)
            {
                try
                {
                    if (obj != null)
                    {
                        if (obj is Excel.Workbook workbook)
                        {
                            workbook.Close(false);
                        }
                        else if (obj is Excel.Application excelApp)
                        {
                            excelApp.Quit();
                        }
                        Marshal.ReleaseComObject(obj);
                    }
                }
                catch {}
            }
        }

        #endregion




        #region "Создание и заполнение файлов техкарт"

        #region "Создание Excel"

        /// <summary>
        /// Создание эксель файла
        /// </summary>
        public new Dictionary<string, string> CreateExcelFile()
        {
            if (Parametrs.Count > 0)
            {
                CreateExcelName();
                if (!string.IsNullOrEmpty(FileNameNew))
                {
                    CreateExcel();
                    if (FlagSuccessfullWriteNewExcelFile && Msg == "")
                    {
                        if (Parametrs["TYPE_PRODUCT"].ToInt().ContainsIn(1, 8, 14, 15, 12, 100, 225, 229))
                        {
                            UpdatePathTk();
                            if (FlagSuccessfullUpdatePathTk == false)
                            {
                                Msg = "Ошибка обновления пути к файлу техкарты";
                            }
                        }
                    }
                }
                else
                {
                    Msg = $"Не удалось сгенерировать наименование нового эксель файла";
                }
            }
            else
            {
                Msg = $"Переданы пустые параметры";
            }
            var result = new Dictionary<string, string>()
            {
                {"MSG", Msg},
                {"FILE_NAME", FileNameNew}
            };
            return result;
        }
        public void CreateExcel()
        {
            if (Parametrs["TYPE_PRODUCT"].ToInt().ContainsIn(1, 8, 12, 14, 15, 100, 225, 229))
            {
                CreateExcelSheet();
            }
        }

        #endregion

        #region "Пересоздание Excel"

        /// <summary>
        /// Пересоздание эксель файла
        /// </summary>
        public new Dictionary<string, string> RecreateExcelFile()
        {
            if (Parametrs.Count > 0)
            {
                if (FlagReameFile)
                {
                    CreateExcelName();
                }
                if (!string.IsNullOrEmpty(FileNameNew))
                {
                    RecreateExcel();
                    if (FlagSuccessfullWriteNewExcelFile && Msg == "")
                    {
                        if (FlagReameFile && Parametrs["TYPE_PRODUCT"].ToInt().ContainsIn(8, 14, 15, 12, 100, 225, 229))
                        {
                            UpdatePathTk();
                            if (FlagSuccessfullUpdatePathTk == false)
                            {
                                Msg = "Ошибка обновления пути к файлу техкарты";
                            }
                        }
                    }
                }
                else
                {
                    Msg = $"Не удалось сгенерировать нименование нового эксель файла";
                }
            }
            else
            {
                Msg = $"Переданы пустые параметры";
            }
            var result = new Dictionary<string, string>()
            {
                {"MSG", Msg},
                {"FILE_NAME", FileNameNew}
            };
            return result;
        }
        public void RecreateExcel()
        {
            if (Parametrs["TYPE_PRODUCT"].ToInt().ContainsIn(1, 8, 14, 15, 12, 100, 225, 229))
            {
                RecreateExcelSheet();
            }
        }
        #endregion

        #region "Функции создания"

        /// <summary>
        /// Создание эксель файла техкарты
        /// </summary>
        public void CreateExcelSheet()
        {
            string FileNew = $"{FilePathNew}{FileNameNew}";
            FileFullNameNew = FileNew;

            // Получение названия листа в файле
            string sheetName = "";
            var newSheetName = "";
            if (Parametrs.CheckGet("TYPE_PRODUCT").ToInt().ContainsIn(12, 225))
            {
                sheetName = "Комплект_решеток";
                newSheetName = "Комплект_решеток";
            }
            else if (Parametrs.CheckGet("TYPE_PRODUCT").ToInt().ContainsIn(100, 229))
            {
                sheetName = "Комплект_решеток_не_в_сборе";
                newSheetName = "Комплект_решеток";
            }
            else if (Parametrs.CheckGet("TYPE_PRODUCT").ToInt().ContainsIn(8))
            {
                sheetName = "Решетка";
                newSheetName = "Решетка";
            }
            else if (Parametrs.CheckGet("TYPE_PRODUCT").ToInt().ContainsIn(14,15))
            {
                sheetName = "Прокладка";
                newSheetName = "Прокладка";
            }
            else if (Parametrs.CheckGet("TYPE_PRODUCT").ToInt().ContainsIn(1))
            {
                sheetName = "Прокладка";
                newSheetName = "Лист";
            }

            if (System.IO.File.Exists(SampleFile))
            {

                Excel.Application excelApp = null;
                try
                {
                    excelApp = new Excel.Application();
                    excelApp.DisplayAlerts = false;
                    excelApp.ScreenUpdating = false;
                    excelApp.EnableEvents = false;
                    excelApp.Visible = false;

                    Excel.Workbook sourceWorkbook = null;
                    Excel.Workbook newWorkbook = null;
                    // Открываем файл шаблона
                    sourceWorkbook = excelApp.Workbooks.Open(SampleFile);

                    Excel.Worksheet sheet = sourceWorkbook.Sheets[sheetName] as Excel.Worksheet;
                    Excel.Worksheet imgsheet = sourceWorkbook.Sheets["Картинки2"] as Excel.Worksheet;
                    Excel.Worksheet newMainSheet = null;
                    Excel.Worksheet newImagesSheet = null;
                    if (sheet == null)
                    {
                        if (Msg.IsNullOrEmpty())
                        {
                            Msg = $"Лист '{sheetName}' не найден в файле шаблона";
                        }
                    }
                    else
                    {
                        try
                        {
                            newWorkbook = excelApp.Workbooks.Add();

                            sheet.Copy(Before: newWorkbook.Sheets[1]);
                            newMainSheet = newWorkbook.Sheets[1] as Excel.Worksheet;
                            newMainSheet.Name = newSheetName;

                            // Копируем лист с картинками
                            imgsheet.Copy(After: newWorkbook.Sheets[1]);
                            newImagesSheet = newWorkbook.Sheets[2] as Excel.Worksheet;
                            newImagesSheet.Name = "Картинки2";

                            switch (Parametrs.CheckGet("TYPE_PRODUCT").ToInt())
                            {
                                case 12:
                                case 225:
                                case 100:
                                case 229:
                                    newWorkbook = FillPartitionSheet(newWorkbook);
                                    break;
                                case 8:
                                    newWorkbook = FillPartitionSingleSheet(newWorkbook);
                                    break;
                                case 1:
                                    newWorkbook = FillGasketSheet(newWorkbook, true, false);
                                    break;
                                case 14:
                                case 15:
                                    newWorkbook = FillGasketSheet(newWorkbook);
                                    break;
                                default:
                                    break;

                            }

                            for (int i = newWorkbook.Sheets.Count; i >= 1; i--)
                            {
                                Excel.Worksheet currentSheet = newWorkbook.Sheets[i] as Excel.Worksheet;
                                if (currentSheet.Name != newSheetName)
                                {
                                    currentSheet.Delete();
                                }
                            }

                            string newFilePath = Path.Combine(FilePathNew, FileNameNew);
                            Excel.XlFileFormat format = Excel.XlFileFormat.xlWorkbookNormal;

                            if (System.IO.File.Exists(newFilePath))
                            {
                                var msg2 = $"Файл {FileNameNew} уже существует.{Environment.NewLine}Перезаписать?";
                                var d2 = new DialogWindow(msg2, "Создание техкарты", "", DialogWindowButtons.NoYes);
                                if (d2.ShowDialog() == true)
                                {
                                    newWorkbook.SaveAs(newFilePath, format);
                                    FlagSuccessfullWriteNewExcelFile = true;
                                }
                            }
                            else
                            {
                                newWorkbook.SaveAs(newFilePath, format);
                                FlagSuccessfullWriteNewExcelFile = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (Msg.IsNullOrEmpty())
                            {
                                Msg = $"Ошибка создания эксель файла: {ex.Message}!";
                            }
                            FlagSuccessfullWriteNewExcelFile = false;
                        }
                        finally
                        {
                        }
                    }
                    ReleaseExcelObjects(newImagesSheet, newMainSheet, sheet, imgsheet, newWorkbook, sourceWorkbook);
                }
                catch (Exception e)
                {
                    FlagSuccessfullWriteNewExcelFile = false;
                    if (Msg.IsNullOrEmpty())
                    {
                        Msg = $"Ошибка создания Excel файла: {e.Message}";
                    }
                }
                finally
                {

                    ReleaseExcelObjects(excelApp);
                }

            }
            else
            {
                FlagSuccessfullWriteNewExcelFile = false;
                Msg = $"Файл шаблона {SampleFile} не найден";
            }
        }
        #endregion

        #region "Функции пересоздания"

        public void RecreateExcelSheet()
        {
            string FileNew = $"{FilePathNew}{FileNameNew}";
            FileFullNameNew = FileNew;

            string sheetName = "";
            // Получение названия листа в файле
            switch (Parametrs["TYPE_PRODUCT"].ToInt())
            {
                case 12:
                case 100:
                case 225:
                case 229:
                    sheetName = "Комплект_решеток";
                    break;
                case 14:
                case 15:
                    sheetName = "Прокладка";
                    break;
                case 8:
                    sheetName = "Решетка";
                    break;
                case 1:
                    sheetName = "Лист";
                    break;

            }


            Excel.Application excelApp = new Excel.Application();
            excelApp.DisplayAlerts = false;
            excelApp.ScreenUpdating = false;
            excelApp.EnableEvents = false;
            excelApp.Visible = false;

            Excel.Workbook workbook = null;
            Excel.Workbook sourceWorkbook = null;
            Excel.Worksheet imgsheet = null;
            Excel.Worksheet newImagesSheet = null;

            var pathNew = Path.Combine(FilePathNew, FileNameOld);
            var pathWork = Path.Combine(FilePathWork, FileNameOld);

            int typeExist = 0;
            if (System.IO.File.Exists(pathNew))
            {
                workbook = excelApp.Workbooks.Open(pathNew);
                typeExist = 1;
            }
            if (System.IO.File.Exists(pathWork))
            {
                workbook = excelApp.Workbooks.Open(pathWork);
                typeExist = 2;
            }

            try
            {
                Excel.Worksheet sheet = null;
                if (workbook.Sheets.Cast<Excel.Worksheet>().Any(sheet => sheet.Name == sheetName))
                {
                    sheet = workbook.Sheets[sheetName] as Excel.Worksheet;
                }
                else if (workbook.Sheets.Count == 1 && workbook.Sheets.Cast<Excel.Worksheet>().Any(sheet => sheet.Name == "Ящик"))
                {
                    sheet = workbook.Sheets["Ящик"] as Excel.Worksheet;
                    sheet.Name = sheetName;
                }

                if (sheet == null)
                {
                    FlagSuccessfullWriteNewExcelFile = false;
                    if (Msg.IsNullOrEmpty())
                    {
                        Msg = "Ошибка при получении листа из файла";
                    }
                }
                else
                {

                    sourceWorkbook = excelApp.Workbooks.Open(SampleFile);
                    imgsheet = sourceWorkbook.Sheets["Картинки2"] as Excel.Worksheet;

                    imgsheet.Copy(After: workbook.Sheets[1]);
                    newImagesSheet = workbook.Sheets[2] as Excel.Worksheet;
                    newImagesSheet.Name = "Картинки2";

                    switch (Parametrs["TYPE_PRODUCT"].ToInt())
                    {
                        case 12:
                        case 100:
                        case 225:
                        case 229:
                            workbook = FillPartitionSheet(workbook, false);
                            break;
                        case 8:
                            workbook = FillPartitionSingleSheet(workbook, false);
                            break;
                        case 14:
                        case 15:
                            workbook = FillGasketSheet(workbook, false, true);
                            break;
                        case 1:
                            workbook = FillGasketSheet(workbook, false, false);
                            break;

                    }

                    for (int i = workbook.Sheets.Count; i >= 1; i--)
                    {
                        Excel.Worksheet currentSheet = workbook.Sheets[i] as Excel.Worksheet;
                        if (currentSheet.Name == "Картинки2")
                        {
                            currentSheet.Delete();
                        }
                    }

                    if (Msg == "")
                    {
                        Excel.XlFileFormat format = Excel.XlFileFormat.xlWorkbookNormal;
                        if (typeExist == 1)
                        {
                            string newFilePath = Path.Combine(FilePathNew, FileNameNew);
                            
                            workbook.SaveAs(newFilePath, format);
                            workbook.Close(false);
                            if (File.Exists(newFilePath))
                            {
                                string oldFilePath = Path.Combine(FilePathNew, FileNameOld);
                                if (FlagReameFile)
                                {
                                    File.Delete(oldFilePath);
                                }
                                FlagSuccessfullWriteNewExcelFile = true;
                            }
                            else
                            {
                                FlagSuccessfullWriteNewExcelFile = false;
                            }
                        }
                        else if (typeExist == 2)
                        {
                            string newFilePath = Path.Combine(FilePathWork, FileNameNew);
                            workbook.SaveAs(newFilePath, format);
                            workbook.Close(false);
                            if (File.Exists(newFilePath))
                            {
                                string oldFilePath = Path.Combine(FilePathWork, FileNameOld);
                                if (FlagReameFile)
                                {
                                    File.Delete(oldFilePath);
                                }
                                FlagSuccessfullWriteNewExcelFile = true;
                            }
                            else
                            {
                                FlagSuccessfullWriteNewExcelFile = false;
                            }
                            FlagSuccessfullWriteNewExcelFile = true;
                        }
                        else
                        {
                            FlagSuccessfullWriteNewExcelFile = false;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                FlagSuccessfullWriteNewExcelFile = false;
                if (Msg.IsNullOrEmpty())
                {
                    Msg = $"Ошибка создания Excel файла: {e.Message}";
                }
            }
            finally
            {
                ReleaseExcelObjects(newImagesSheet, imgsheet, sourceWorkbook, workbook, excelApp);
            }
        }
        #endregion

        #region "Функции заполнения листов"

        /// <summary>
        /// Заполнение листа комплекта решеток
        /// </summary>
        public Excel.Workbook FillPartitionSheet(Excel.Workbook workBook, bool create = true)
        {
            var sheet = workBook.Worksheets["Комплект_решеток"];
            var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                if (Parametrs.CheckGet("TYPE_PRODUCT").ToInt().ContainsIn(12, 225))
                {
                    // Очищаем ячейки, которые заполнены в шаблоне, но не используются для этого типа продукции

                    if (create)
                    {
                        sheet.Range["A15:T22"].ClearContents();
                    }
                    else
                    {

                        // Удаляем картинки решеток
                        if (FlagRecreateExcelImg)
                        {
                            sheet.Range["A15:J22"].ClearContents();

                            Excel.Range cell1 = sheet.Range["A15"];
                            Excel.Range cell2 = sheet.Range["J22"];
                            foreach (Excel.Shape s in sheet.Shapes)
                            {
                                if (s.Left < cell2.Left && s.Left >= cell1.Left && s.Top < cell2.Top && s.Top >= cell1.Top)
                                {
                                    s.Delete();
                                }
                            }
                        }
                        if (FlagRecreateExcelImg2)
                        {
                            sheet.Range["L15:S22"].ClearContents();
                            Excel.Range cell1 = sheet.Range["K15"];
                            Excel.Range cell2 = sheet.Range["T22"];
                            foreach (Excel.Shape s in sheet.Shapes)
                            {
                                if (s.Left < cell2.Left && s.Left >= cell1.Left && s.Top < cell2.Top && s.Top >= cell1.Top)
                                {
                                    s.Delete();
                                }
                            }
                        }


                        // Удаляем картинки укладок
                        {
                            Excel.Range cell1 = sheet.Range["T23"];
                            Excel.Range cell2 = sheet.Range["Y29"];
                            foreach (Excel.Shape s in sheet.Shapes)
                            {
                                if (s.Left < cell2.Left && s.Left >= cell1.Left && s.Top < cell2.Top && s.Top >= cell1.Top)
                                {
                                    s.Delete();
                                }
                            }
                        }
                    }

                    // Шапка
                    {
                        sheet.Range["E7"].Value2 = Parametrs["CLIENT_NAME"];
                        sheet.Range["R4"].Value2 = Parametrs["COLLOR_NAME"];
                        sheet.Range["R5"].Value2 = Parametrs["BRAND_NAME"];
                        sheet.Range["R6"].Value2 = Parametrs["PROFILE_NAME"];

                        if (Parametrs.CheckGet("TYPE_PRODUCT").ToInt() == 225)
                        {
                            sheet.Range["E9"].Value2 = "Исполнение " + Parametrs["PARTITION_TYPE"];
                        }
                    }

                    // решётки
                    {

                        // V10 (Артикул)
                        {
                            if (!string.IsNullOrEmpty(Parametrs.CheckGet("NUMBER_FIRST")))
                            {
                                sheet.Range["V10"].Value2 = Parametrs["NUMBER_FIRST"];
                            }
                        }
                        // F12, P12 (длина)
                        {
                            sheet.Range["F12"].Value2 = Parametrs["LENGTH_FIRST"];
                            sheet.Range["P12"].Value2 = Parametrs["LENGTH_SECOND"];
                        }
                        // F13, P13 (ширина)
                        {
                            sheet.Range["F13"].Value2 = Parametrs["HEIGHT_FIRST"];
                            sheet.Range["P13"].Value2 = Parametrs["HEIGHT_SECOND"];
                        }
                        // F14, P14 (кол-во)
                        {
                            sheet.Range["F14"].Value2 = Parametrs["QUANTITY_FIRST"];
                            sheet.Range["P14"].Value2 = Parametrs["QUANTITY_SECOND"];
                        }



                    }

                    // Флаг того, нужно ли писать в эксель файл информацию по упаковке
                    bool createPackageData = true;
                    if (Parametrs.CheckGet("TYPE_PACKAGE").ToInt() == 0)
                    {
                        createPackageData = false;
                    }


                    // Пачка с решётками в сборе
                    {
                        if (!createPackageData)
                        {
                            sheet.Range["Y24:AB27"].ClearContents();
                            sheet.Range["Y30:AB34"].ClearContents();
                            sheet.Range["V31:X34"].ClearContents();
                            var cell = sheet.Range["W25"];
                            cell.Value2 = $"В пачке {Parametrs["QUANTITY"].ToInt()} штук";
                            cell.Font.Size = 12; // Размер шрифта
                            cell.Font.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Red);
                            cell.Font.Bold = true;
                        }
                        else
                        {
                            // AB23 (Укладка на поддон)
                            {
                                sheet.Range["V23:AB23"].ClearContents();
                                try
                                {
                                    sheet.Range["AB23"].Value2 = "Укладка на поддон " + Parametrs["PALLET_NAME"];
                                }
                                catch
                                {
                                    sheet.Range["X23"].Value2 = "Укладка на поддон " + Parametrs["PALLET_NAME"];
                                }
                            }

                            // AB24 (Кол-во в пачке, шт :)
                            {
                                sheet.Range["Y24"].Value2 = "Кол-во в пачке, шт :";
                                sheet.Range["AB24"].Value2 = Parametrs["QUANTITY"];
                            }
                            // AB25 (Кол-во пачек в ряду :)
                            {
                                sheet.Range["Y25"].Value2 = "Кол-во пачек в ряду :";
                                sheet.Range["AB25"].Value2 = Parametrs["QUANTITY_PACK"];
                            }
                            // AB26 (Кол-во рядов :)
                            {
                                sheet.Range["Y26"].Value2 = "Кол-во рядов :";
                                sheet.Range["AB26"].Value2 = Parametrs["QUANTITY_ROWS"];
                            }
                            // AB27 (Кол-во на поддон :)
                            {
                                sheet.Range["Y27"].Value2 = "Кол-во на поддон :";
                                sheet.Range["AB27"].Value2 = Parametrs["QUANTITY_BOX"];
                            }
                            // Y30 (Упаковка поддона)
                            {
                                sheet.Range["Y30"].Value2 = "Упаковка поддона";
                            }
                            // AB31 (Подпрессовка :)
                            {
                                sheet.Range["Y31"].Value2 = "Подпрессовка :";
                                if (Parametrs["PREPRESSING"].ToInt() == 1)
                                {
                                    sheet.Range["AB31"].Value2 = "есть";
                                }
                                else
                                {
                                    sheet.Range["AB31"].Value2 = "нет";
                                }
                            }
                            // AB32 (Обвязка лентами :)
                            {
                                sheet.Range["Y32"].Value2 = "Обвязка лентами :";
                                if (Parametrs["STRAPPING"].ToInt() > 0)
                                {
                                    sheet.Range["AB32"].Value2 = "есть, " + Parametrs["STRAPPING"];
                                }
                                else
                                {
                                    sheet.Range["AB32"].Value2 = "нет";
                                }
                            }
                            // AB33 (Уголки из гофрокарт :)
                            {
                                sheet.Range["Y33"].Value2 = "Уголки из гофрокарт :";
                                if (Parametrs["CORNERS"].ToInt() == 1)
                                {
                                    sheet.Range["AB33"].Value2 = "есть";
                                }
                                else
                                {
                                    sheet.Range["AB33"].Value2 = "нет";
                                }
                            }
                            // AB34 (Уп-ка в стрейчплёнку :)
                            {
                                sheet.Range["Y34"].Value2 = "Уп-ка в стрейчплёнку :";
                                if (Parametrs["PACKAGING"].ToInt() > 0)
                                {
                                    sheet.Range["AB34"].Value2 = "есть, F" + Parametrs["PACKAGING"];
                                }
                                else
                                {
                                    sheet.Range["AB34"].Value2 = "нет";
                                }
                            }
                            // V31 (Габариты ТП)
                            {
                                sheet.Range["V31"].Value2 = "Габариты ТП";
                            }
                            // W32 (Длина :)
                            {
                                sheet.Range["V32"].Value2 = "Длина :";
                                sheet.Range["W32"].Value2 = Parametrs["PACKAGE_LENGTH"];
                            }
                            // W33 (Ширина :)
                            {
                                sheet.Range["V33"].Value2 = "Ширина :";
                                sheet.Range["W33"].Value2 = Parametrs["PACKAGE_WIDTH"];
                            }
                            // W34 (Высота :)
                            {
                                sheet.Range["V34"].Value2 = "Высота :";
                                sheet.Range["W34"].Value2 = Parametrs["PACKAGE_HEIGTH"];
                            }
                        }
                    }

                    // Картинки
                    {

                        // Картинки решеток
                        {

                            ParsNotchesParametrs();

                            double koef = 1;
                            // Расчет коэф. уменьшения размеров
                            {
                                Excel.Range start = sheet.Range["C17"];
                                Excel.Range end = sheet.Range["J21"];

                                double l1 = Parametrs.CheckGet("LENGTH_FIRST").ToDouble();
                                double l2 = Parametrs.CheckGet("LENGTH_SECOND").ToDouble();
                                double h = Parametrs.CheckGet("HEIGHT_FIRST").ToDouble();

                                // Уменьшение по длине
                                if (l1 > l2)
                                {
                                    koef = (float)((end.Left - start.Left) / l1);
                                }
                                else
                                {
                                    koef = (float)((end.Left - start.Left) / l2);
                                }

                                // Если высота не влезает - уменьшаем по высоте
                                if (h * koef > (float)(end.Top - start.Top))
                                {
                                    koef = (float)((end.Top - start.Top) / h);
                                }
                            }

                            if (FlagRecreateExcelImg)
                            {
                                workBook = CreateImgPartition(workBook, "Комплект_решеток", Parametrs.CheckGet("LENGTH_FIRST").ToDouble(), Parametrs.CheckGet("HEIGHT_FIRST").ToDouble(), ListDicNotchesParametrsFirst, true, "", koef);
                                
                                Excel.Shape pastedShape = sheet.Shapes.Item("Картинка_решетки");
                                pastedShape.Name = "Картинка_решетки_1";
                                Excel.Range cell1 = sheet.Range["B15"];
                                Excel.Range cell2 = sheet.Range["J23"];

                                pastedShape.Left = (float)(cell1.Left + ((cell2.Left - cell1.Left) - pastedShape.Width) / 2);
                                pastedShape.Top = (float)(cell1.Top + ((cell2.Top - cell1.Top) - pastedShape.Height) / 2);
                                
                            }
                            if (FlagRecreateExcelImg2)
                            {
                                workBook = CreateImgPartition(workBook, "Комплект_решеток", Parametrs.CheckGet("LENGTH_SECOND").ToDouble(), Parametrs.CheckGet("HEIGHT_FIRST").ToDouble(), ListDicNotchesParametrsSecond, true, "2", koef);
                                
                                Excel.Shape pastedShape = sheet.Shapes.Item("Картинка_решетки");
                                pastedShape.Name = "Картинка_решетки_2";
                                Excel.Range cell1 = sheet.Range["L15"];
                                Excel.Range cell2 = sheet.Range["T23"];

                                pastedShape.Left = (float)(cell1.Left + ((cell2.Left - cell1.Left) - pastedShape.Width) / 2);
                                pastedShape.Top = (float)(cell1.Top + ((cell2.Top - cell1.Top) - pastedShape.Height) / 2);
                            }

                        }

                        // Картинка укладки
                        if (createPackageData)
                        {
                            var p = new Dictionary<string, string>();
                            p.Add("LAYING_SCHEME", Parametrs["LAYING_SCHEME"]);

                            var q = new LPackClientQuery();
                            q.Request.SetParam("Module", "Preproduction");
                            q.Request.SetParam("Object", "GasketTechnologicalMap");
                            q.Request.SetParam("Action", "GetLayingSchemeImage");

                            q.Request.SetParams(p);

                            q.DoQuery();

                            if (q.Answer.Status == 0)
                            {
                                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                                if (result != null)
                                {
                                    var ds = ListDataSet.Create(result, "ITEMS");
                                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                                    {
                                        byte[] bytes = Convert.FromBase64String(ds.Items.First().CheckGet("JPG"));
                                        var mem = new MemoryStream(bytes) { Position = 0 };
                                        var image = new BitmapImage();
                                        image.BeginInit();
                                        image.StreamSource = mem;
                                        image.CacheOption = BitmapCacheOption.OnLoad;
                                        image.EndInit();

                                        if (bytes != null && bytes.Length > 0)
                                        {
                                            string tempFilePath = Path.GetTempFileName() + ".jpg";
                                            File.WriteAllBytes(tempFilePath, bytes);

                                            Excel.Range cell1 = sheet.Range["V24"];
                                            Excel.Range cell2 = sheet.Range["X28"];

                                            try
                                            {
                                                // Вставляем изображение
                                                Excel.Shape picture = sheet.Shapes.AddPicture(
                                                    tempFilePath,
                                                    Microsoft.Office.Core.MsoTriState.msoFalse,
                                                    Microsoft.Office.Core.MsoTriState.msoTrue,
                                                    10, 10, 100, 100); // Левый верхний угол (10,10) и размер (100x100)

                                                picture.Left = (float)cell1.Left;
                                                picture.Top = (float)cell1.Top;
                                                picture.Width = (float)(cell2.Left + cell2.Width) - (float)cell1.Left;
                                                picture.Height = (float)(cell2.Top + cell2.Height) - (float)cell1.Top;
                                                picture.Name = "Укладка1";

                                            }
                                            finally
                                            {
                                            }

                                            // Удаляем временный файл
                                            File.Delete(tempFilePath);
                                        }

                                    }
                                }
                            }
                        }

                    }

                    // Подвал
                    {
                        sheet.Range["F38"].Value2 = Central.User.Name;
                        sheet.Range["A38"].Value2 = "Инженер ОПП";
                        sheet.Range["N37"].Value2 = DateTime.Now.ToString("dd.MM.yyyy");
                        sheet.Range["N38"].Value2 = DateTime.Now.ToString("dd.MM.yyyy");
                        sheet.Range["N39"].Value2 = DateTime.Now.ToString("dd.MM.yyyy");
                        sheet.Range["N40"].Value2 = DateTime.Now.ToString("dd.MM.yyyy");
                    }
                }
                else if (Parametrs.CheckGet("TYPE_PRODUCT").ToInt().ContainsIn(100, 229))
                {
                    // Очищаем ячейки, которые заполнены в шаблоне, но не используются для этого типа продукции

                    if (create)
                    {
                        sheet.Range["A15:AB22"].ClearContents();
                    }
                    else
                    {
                        // Удаляем картинки решеток
                        if (FlagRecreateExcelImg)
                        {
                            sheet.Range["A15:I22"].ClearContents();
                            Excel.Range cell = sheet.Range["K23"];
                            foreach (Excel.Shape s in sheet.Shapes)
                            {
                                if (s.Left < cell.Left && s.Top < cell.Top)
                                {
                                    s.Delete();
                                }
                            }
                        }
                        if (FlagRecreateExcelImg2)
                        {
                            sheet.Range["L15:S22"].ClearContents();
                            Excel.Range cell1 = sheet.Range["K15"];
                            Excel.Range cell2 = sheet.Range["T23"];
                            foreach (Excel.Shape s in sheet.Shapes)
                            {
                                if (s.Left < cell2.Left && s.Left >= cell1.Left && s.Top < cell2.Top && s.Top >= cell1.Top)
                                {
                                    s.Delete();
                                }
                            }
                        }

                        // Удаляем картинки укладок
                        {
                            Excel.Range cell1 = sheet.Range["A23"];
                            Excel.Range cell2 = sheet.Range["E29"];
                            foreach (Excel.Shape s in sheet.Shapes)
                            {
                                if (s.Left < cell2.Left && s.Left >= cell1.Left && s.Top < cell2.Top && s.Top >= cell1.Top)
                                {
                                    s.Delete();
                                }
                            }
                            cell1 = sheet.Range["K23"];
                            cell2 = sheet.Range["O29"];
                            foreach (Excel.Shape s in sheet.Shapes)
                            {
                                if (s.Left < cell2.Left && s.Left >= cell1.Left && s.Top < cell2.Top && s.Top >= cell1.Top)
                                {
                                    s.Delete();
                                }
                            }
                        }
                    }

                    // Шапка
                    {
                        sheet.Range["E7"].Value2 = Parametrs["CLIENT_NAME"];
                        sheet.Range["R4"].Value2 = Parametrs["COLLOR_NAME"];
                        sheet.Range["R5"].Value2 = Parametrs["BRAND_NAME"];
                        sheet.Range["R6"].Value2 = Parametrs["PROFILE_NAME"];

                        if (Parametrs.CheckGet("TYPE_PRODUCT").ToInt() == 229)
                        {
                            sheet.Range["E9"].Value2 = "Исполнение " + Parametrs["PARTITION_TYPE"];
                        }
                    }

                    // решётки
                    {

                        // B10, L10 (Артикулы)
                        {
                            if (!string.IsNullOrEmpty(Parametrs.CheckGet("NUMBER_FIRST")))
                            {
                                sheet.Range["B10"].Value2 = Parametrs["NUMBER_FIRST"];
                            }
                            if (!string.IsNullOrEmpty(Parametrs.CheckGet("NUMBER_SECOND")))
                            {
                                sheet.Range["L10"].Value2 = Parametrs["NUMBER_SECOND"];
                            }
                        }
                        // F12, P12 (длина)
                        {
                            sheet.Range["F12"].Value2 = Parametrs["LENGTH_FIRST"];
                            sheet.Range["P12"].Value2 = Parametrs["LENGTH_SECOND"];
                        }
                        // F13, P13 (ширина)
                        {
                            sheet.Range["F13"].Value2 = Parametrs["HEIGHT_FIRST"];
                            sheet.Range["P13"].Value2 = Parametrs["HEIGHT_SECOND"];
                        }
                        // F14, P14 (кол-во)
                        {
                            sheet.Range["F14"].Value2 = Parametrs["QUANTITY_FIRST"];
                            sheet.Range["P14"].Value2 = Parametrs["QUANTITY_SECOND"];
                        }

                    }

                    // Флаг того, нужно ли писать в эксель файл информацию по упаковке
                    bool createPackageData = true;
                    if (Parametrs.CheckGet("TYPE_PACKAGE").ToInt() == 0)
                    {
                        createPackageData = false;
                    }
                    bool createPackageData2 = true;
                    if (Parametrs.CheckGet("TYPE_PACKAGE2").ToInt() == 0)
                    {
                        createPackageData2 = false;
                    }

                    // Укладка решеток
                    {
                        if (!createPackageData)
                        {
                            sheet.Range["F30:I34"].ClearContents();
                            sheet.Range["F23:I27"].ClearContents();
                            sheet.Range["B31:E34"].ClearContents();
                            var cell = sheet.Range["F29"];
                            cell.Value2 = $"В пачке {Parametrs["QUANTITY"].ToInt()} штук";
                            cell.Font.Size = 12;
                            cell.Font.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Red);
                            cell.Font.Bold = true;

                        }
                        else
                        {
                            sheet.Range["B23:I23"].ClearContents();
                            try
                            {
                                sheet.Range["E23"].Value2 = "Укладка на поддон " + Parametrs["PALLET_NAME"];
                            }
                            catch
                            {
                                sheet.Range["I23"].Value2 = "Укладка на поддон " + Parametrs["PALLET_NAME"];
                            }

                            // I24 (Кол-во в пачке, шт :)
                            {
                                sheet.Range["F24"].Value2 = "Кол-во в пачке, шт :";
                                sheet.Range["I24"].Value2 = Parametrs["QUANTITY"];
                            }

                            // I25 (Кол-во пачек в ряду :)
                            {
                                sheet.Range["F25"].Value2 = "Кол-во пачек в ряду :";
                                sheet.Range["I25"].Value2 = Parametrs["QUANTITY_PACK"];
                            }
                            // I26 (Кол-во рядов :)
                            {
                                sheet.Range["F26"].Value2 = "Кол-во рядов :";
                                sheet.Range["I26"].Value2 = Parametrs["QUANTITY_ROWS"];
                            }
                            // I27 (Кол-во на поддон :)
                            {
                                sheet.Range["F27"].Value2 = "Кол-во на поддон :";
                                sheet.Range["I27"].Value2 = Parametrs["QUANTITY_BOX"];
                            }
                            // F28 (Укладка на ребро)
                            {
                                if (Parametrs["ON_EDGE"].ToInt() > 0)
                                {
                                    sheet.Range["F28"].Value2 = "Пачки ставить \"на ребро\"";
                                }
                            }
                            // F30 (Упаковка поддона)
                            {
                                sheet.Range["F30"].Value2 = "Упаковка поддона";
                            }
                            // I31 (Подпрессовка :)
                            {
                                sheet.Range["I31"].Value2 = "Подпрессовка :";
                                if (Parametrs["PREPRESSING"].ToInt() == 1)
                                {
                                    sheet.Range["I31"].Value2 = "есть";
                                }
                                else
                                {
                                    sheet.Range["I31"].Value2 = "нет";
                                }
                            }
                            // I32 (Обвязка лентами :)
                            {
                                sheet.Range["I32"].Value2 = "Обвязка лентами :";
                                if (Parametrs["STRAPPING"].ToInt() > 0)
                                {
                                    sheet.Range["I32"].Value2 = "есть, " + Parametrs["STRAPPING"];
                                }
                                else
                                {
                                    sheet.Range["I32"].Value2 = "нет";
                                }
                            }
                            // I33 (Уголки из гофрокарт :)
                            {
                                sheet.Range["I33"].Value2 = "Уголки из гофрокарт :";
                                if (Parametrs["CORNERS"].ToInt() == 1)
                                {
                                    sheet.Range["I33"].Value2 = "есть";
                                }
                                else
                                {
                                    sheet.Range["I33"].Value2 = "нет";
                                }
                            }
                            // I34 (Уп-ка в стрейчплёнку :)
                            {
                                sheet.Range["I34"].Value2 = "Уп-ка в стрейчплёнку :";
                                if (Parametrs["PACKAGING"].ToInt() > 0)
                                {
                                    sheet.Range["I34"].Value2 = "есть, F" + Parametrs["PACKAGING"];
                                }
                                else
                                {
                                    sheet.Range["I34"].Value2 = "нет";
                                }
                            }
                            // B31 (Габариты ТП)
                            {
                                sheet.Range["B31"].Value2 = "Габариты ТП";
                            }
                            // D32 (Длина :)
                            {
                                sheet.Range["B32"].Value2 = "Длина :";
                                sheet.Range["D32"].Value2 = Parametrs["PACKAGE_LENGTH"];
                            }
                            // D33 (Ширина :)
                            {
                                sheet.Range["B33"].Value2 = "Ширина :";
                                sheet.Range["D33"].Value2 = Parametrs["PACKAGE_WIDTH"];
                            }
                            // D34 (Высота :)
                            {
                                sheet.Range["B34"].Value2 = "Высота :";
                                sheet.Range["D34"].Value2 = Parametrs["PACKAGE_HEIGTH"];
                            }
                        }




                        if (!createPackageData2)
                        {
                            sheet.Range["L23:P27"].ClearContents();
                            sheet.Range["L30:P34"].ClearContents();
                            sheet.Range["L31:S34"].ClearContents();
                            var cell = sheet.Range["P29"];
                            cell.Value2 = $"В пачке {Parametrs["QUANTITY2"].ToInt()} штук";
                            cell.Font.Size = 12;
                            cell.Font.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Red);
                            cell.Font.Bold = true;
                        }
                        else
                        {
                            sheet.Range["K23:S23"].ClearContents();
                            try
                            {
                                sheet.Range["O23"].Value2 = "Укладка на поддон " + Parametrs["PALLET_NAME2"];
                            }
                            catch
                            {
                                sheet.Range["S23"].Value2 = "Укладка на поддон " + Parametrs["PALLET_NAME2"];
                            }

                            // S24 (Кол-во в пачке, шт :)
                            {
                                sheet.Range["P24"].Value2 = "Кол-во в пачке, шт :";
                                sheet.Range["S24"].Value2 = Parametrs["QUANTITY2"];
                            }

                            // S25 (Кол-во пачек в ряду :)
                            {
                                sheet.Range["P25"].Value2 = "Кол-во пачек в ряду :";
                                sheet.Range["S25"].Value2 = Parametrs["QUANTITY_PACK2"];
                            }
                            // S26 (Кол-во рядов :)
                            {
                                sheet.Range["P26"].Value2 = "Кол-во рядов :";
                                sheet.Range["S26"].Value2 = Parametrs["QUANTITY_ROWS2"];
                            }
                            // S27 (Кол-во на поддон :)
                            {
                                sheet.Range["P27"].Value2 = "Кол-во на поддон :";
                                sheet.Range["S27"].Value2 = Parametrs["QUANTITY_BOX2"];
                            }
                            // P30 (Упаковка поддона)
                            {
                                sheet.Range["P30"].Value2 = "Упаковка поддона";
                            }
                            // P28 (Укладка на ребро)
                            {
                                if (Parametrs["ON_EDGE2"].ToInt() > 0)
                                {
                                    sheet.Range["P28"].Value2 = "Пачки ставить \"на ребро\"";
                                }
                            }
                            // S31 (Подпрессовка :)
                            {
                                sheet.Range["P31"].Value2 = "Подпрессовка :";
                                if (Parametrs["PREPRESSING2"].ToInt() == 1)
                                {
                                    sheet.Range["S31"].Value2 = "есть";
                                }
                                else
                                {
                                    sheet.Range["S31"].Value2 = "нет";
                                }
                            }
                            // S32 (Обвязка лентами :)
                            {
                                sheet.Range["P32"].Value2 = "Обвязка лентами :";
                                if (Parametrs["STRAPPING2"].ToInt() > 0)
                                {
                                    sheet.Range["S32"].Value2 = "есть, " + Parametrs["STRAPPING2"];
                                }
                                else
                                {
                                    sheet.Range["S32"].Value2 = "нет";
                                }
                            }
                            // S33 (Уголки из гофрокарт :)
                            {
                                sheet.Range["P33"].Value2 = "Уголки из гофрокарт :";
                                if (Parametrs["CORNERS2"].ToInt() == 1)
                                {
                                    sheet.Range["S33"].Value2 = "есть";
                                }
                                else
                                {
                                    sheet.Range["S33"].Value2 = "нет";
                                }
                            }
                            // S34 (Уп-ка в стрейчплёнку :)
                            {
                                sheet.Range["P34"].Value2 = "Уп-ка в стрейчплёнку :";
                                if (Parametrs["PACKAGING2"].ToInt() > 0)
                                {
                                    sheet.Range["S34"].Value2 = "есть, F" + Parametrs["PACKAGING2"];
                                }
                                else
                                {
                                    sheet.Range["S34"].Value2 = "нет";
                                }
                            }
                            // L31 (Габариты ТП)
                            {
                                sheet.Range["L31"].Value2 = "Габариты ТП";
                            }
                            // N32 (Длина :)
                            {
                                sheet.Range["L32"].Value2 = "Длина :";
                                sheet.Range["N32"].Value2 = Parametrs["PACKAGE_LENGTH2"];
                            }
                            // N33 (Ширина :)
                            {
                                sheet.Range["L33"].Value2 = "Ширина :";
                                sheet.Range["N33"].Value2 = Parametrs["PACKAGE_WIDTH2"];
                            }
                            // N34 (Высота :)
                            {
                                sheet.Range["L34"].Value2 = "Высота :";
                                sheet.Range["N34"].Value2 = Parametrs["PACKAGE_HEIGTH2"];
                            }
                        }


                    }

                    // Картинки
                    {
                        // Картинки решеток
                        {
                            ParsNotchesParametrs();

                            double koef = 1;
                            // Расчет коэф. уменьшения размеров
                            {
                                Excel.Range start = sheet.Range["C17"];
                                Excel.Range end = sheet.Range["J21"];

                                double l1 = Parametrs.CheckGet("LENGTH_FIRST").ToDouble();
                                double l2 = Parametrs.CheckGet("LENGTH_SECOND").ToDouble();
                                double h = Parametrs.CheckGet("HEIGHT_FIRST").ToDouble();

                                // Уменьшение по длине
                                if (l1 > l2)
                                {
                                    koef = (float)((end.Left - start.Left) / l1);
                                }
                                else
                                {
                                    koef = (float)((end.Left - start.Left) / l2);
                                }

                                // Если высота не влезает - уменьшаем по высоте
                                if (h * koef > (float)(end.Top - start.Top))
                                {
                                    koef = (float)((end.Top - start.Top) / h);
                                }
                            }

                            if (FlagRecreateExcelImg)
                            {
                                workBook = CreateImgPartition(workBook, "Комплект_решеток", Parametrs.CheckGet("LENGTH_FIRST").ToDouble(), Parametrs.CheckGet("HEIGHT_FIRST").ToDouble(), ListDicNotchesParametrsFirst, true, "", koef);

                                Excel.Shape pastedShape = sheet.Shapes.Item("Картинка_решетки");
                                pastedShape.Name = "Картинка_решетки_1";
                                Excel.Range cell1 = sheet.Range["B15"];
                                Excel.Range cell2 = sheet.Range["J23"];

                                pastedShape.Left = (float)(cell1.Left + ((cell2.Left - cell1.Left) - pastedShape.Width) / 2);
                                pastedShape.Top = (float)(cell1.Top + ((cell2.Top - cell1.Top) - pastedShape.Height) / 2);
                            }
                            if (FlagRecreateExcelImg2)
                            {
                                workBook = CreateImgPartition(workBook, "Комплект_решеток", Parametrs.CheckGet("LENGTH_SECOND").ToDouble(), Parametrs.CheckGet("HEIGHT_FIRST").ToDouble(), ListDicNotchesParametrsSecond, true, "2", koef);

                                Excel.Shape pastedShape = sheet.Shapes.Item("Картинка_решетки");
                                pastedShape.Name = "Картинка_решетки_2";
                                Excel.Range cell1 = sheet.Range["L15"];
                                Excel.Range cell2 = sheet.Range["T23"];

                                pastedShape.Left = (float)(cell1.Left + ((cell2.Left - cell1.Left) - pastedShape.Width) / 2);
                                pastedShape.Top = (float)(cell1.Top + ((cell2.Top - cell1.Top) - pastedShape.Height) / 2);
                            }
                        }

                        // Картинки укладок
                        {
                            if (createPackageData)
                            {
                                var p = new Dictionary<string, string>();
                                p.Add("LAYING_SCHEME", Parametrs["LAYING_SCHEME"]);

                                var q = new LPackClientQuery();
                                q.Request.SetParam("Module", "Preproduction");
                                q.Request.SetParam("Object", "GasketTechnologicalMap");
                                q.Request.SetParam("Action", "GetLayingSchemeImage");

                                q.Request.SetParams(p);

                                q.DoQuery();

                                if (q.Answer.Status == 0)
                                {
                                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                                    if (result != null)
                                    {
                                        var ds = ListDataSet.Create(result, "ITEMS");
                                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                                        {
                                            byte[] bytes = Convert.FromBase64String(ds.Items.First().CheckGet("JPG"));
                                            var mem = new MemoryStream(bytes) { Position = 0 };
                                            var image = new BitmapImage();
                                            image.BeginInit();
                                            image.StreamSource = mem;
                                            image.CacheOption = BitmapCacheOption.OnLoad;
                                            image.EndInit();

                                            if (bytes != null && bytes.Length > 0)
                                            {
                                                string tempFilePath = Path.GetTempFileName() + ".jpg";
                                                File.WriteAllBytes(tempFilePath, bytes);

                                                Excel.Range cell1 = sheet.Range["B24"];
                                                Excel.Range cell2 = sheet.Range["D28"];

                                                try
                                                {
                                                    // Вставляем изображение
                                                    Excel.Shape picture = sheet.Shapes.AddPicture(
                                                        tempFilePath,
                                                        Microsoft.Office.Core.MsoTriState.msoFalse,
                                                        Microsoft.Office.Core.MsoTriState.msoTrue,
                                                        10, 10, 100, 100); // Левый верхний угол (10,10) и размер (100x100)

                                                    picture.Left = (float)cell1.Left;
                                                    picture.Top = (float)cell1.Top;
                                                    picture.Width = (float)(cell2.Left + cell2.Width) - (float)cell1.Left;
                                                    picture.Height = (float)(cell2.Top + cell2.Height) - (float)cell1.Top;
                                                    picture.Name = "Укладка1";


                                                }
                                                finally
                                                {
                                                }

                                                // Удаляем временный файл
                                                File.Delete(tempFilePath);
                                            }

                                        }
                                    }
                                }
                            }

                            if (createPackageData2)
                            {
                                var p = new Dictionary<string, string>();
                                p.Add("LAYING_SCHEME", Parametrs["LAYING_SCHEME2"]);

                                var q = new LPackClientQuery();
                                q.Request.SetParam("Module", "Preproduction");
                                q.Request.SetParam("Object", "GasketTechnologicalMap");
                                q.Request.SetParam("Action", "GetLayingSchemeImage");

                                q.Request.SetParams(p);

                                q.DoQuery();

                                if (q.Answer.Status == 0)
                                {
                                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                                    if (result != null)
                                    {
                                        var ds = ListDataSet.Create(result, "ITEMS");
                                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                                        {
                                            byte[] bytes = Convert.FromBase64String(ds.Items.First().CheckGet("JPG"));
                                            var mem = new MemoryStream(bytes) { Position = 0 };
                                            var image = new BitmapImage();
                                            image.BeginInit();
                                            image.StreamSource = mem;
                                            image.CacheOption = BitmapCacheOption.OnLoad;
                                            image.EndInit();

                                            if (bytes != null && bytes.Length > 0)
                                            {
                                                string tempFilePath = Path.GetTempFileName() + ".jpg";
                                                File.WriteAllBytes(tempFilePath, bytes);

                                                Excel.Range cell1 = sheet.Range["L24"];
                                                Excel.Range cell2 = sheet.Range["N28"];

                                                try
                                                {
                                                    // Вставляем изображение
                                                    Excel.Shape picture = sheet.Shapes.AddPicture(
                                                        tempFilePath,
                                                        Microsoft.Office.Core.MsoTriState.msoFalse,
                                                        Microsoft.Office.Core.MsoTriState.msoTrue,
                                                        10, 10, 100, 100); // Левый верхний угол (10,10) и размер (100x100)

                                                    picture.Left = (float)cell1.Left;
                                                    picture.Top = (float)cell1.Top;
                                                    picture.Width = (float)(cell2.Left + cell2.Width) - (float)cell1.Left;
                                                    picture.Height = (float)(cell2.Top + cell2.Height) - (float)cell1.Top;
                                                    picture.Name = "Укладка2";


                                                }
                                                finally
                                                {
                                                }

                                                // Удаляем временный файл
                                                File.Delete(tempFilePath);
                                            }

                                        }
                                    }
                                }
                            }

                        }

                    }

                    // Подвал
                    {
                        sheet.Range["F38"].Value2 = Central.User.Name;
                        sheet.Range["A38"].Value2 = "Инженер ОПП";
                        sheet.Range["N37"].Value2 = DateTime.Now.ToString("dd.MM.yyyy");
                        sheet.Range["N38"].Value2 = DateTime.Now.ToString("dd.MM.yyyy");
                        sheet.Range["N39"].Value2 = DateTime.Now.ToString("dd.MM.yyyy");
                        sheet.Range["N40"].Value2 = DateTime.Now.ToString("dd.MM.yyyy");
                    }
                }
            }
            catch (Exception e)
            {
                if (Msg.IsNullOrEmpty())
                {
                    Msg = $"Ошибка заполнения листа: {e.Message}";
                }

                var error = new Common.Error();
                error.Code = 146;
                error.Message = "Ошибка заполнения листа";
                error.Description = e.ToString();
                Central.ProcError(error, "", true);
            }
            finally
            {
                ReleaseExcelObjects(sheet);
            }
            return workBook;

        }

        /// <summary>
        /// Заполнение листа прокладок
        /// </summary>
        public Excel.Workbook FillGasketSheet(Excel.Workbook workBook, bool create = true, bool isGasket = true)
        {
            Excel.Worksheet sheet = null;
            if (isGasket)
            {
                sheet = workBook.Worksheets["Прокладка"];
            }
            else
            {
                sheet = workBook.Worksheets["Лист"];
            }

            try
            {
                //Очищаем ячейки
                if (create)
                {
                    sheet.Range["A12:O34"].ClearContents();
                }
                else
                {
                    // Удаляем картинку прокладки
                    if (FlagRecreateExcelImg)
                    {
                        foreach (Excel.Shape s in sheet.Shapes)
                        {
                            s.Delete();
                        }
                    }

                    // Удаляем картинки укладок
                    foreach (Excel.Shape s in sheet.Shapes)
                    {
                        if (s.Name.Contains("Укладка"))
                        {
                            s.Delete();
                        }
                    }
                }

                //Шапка
                {

                    // C2 (Технологическая карта №)
                    if (!string.IsNullOrEmpty(Parametrs.CheckGet("NUMBER_FIRST")))
                        {
                            sheet.Range["C2"].Value2 = Parametrs["NUMBER_FIRST"];
                        }
                    // B5 (Наименов.)
                    sheet.Range["B5"].Value2 = Parametrs["NAME_FIRST"];
                    // B7 (Покупатель:)
                    sheet.Range["B7"].Value2 = Parametrs["CLIENT_NAME"];
                    // Тип ящика (FEFCO)
                    if (!isGasket)
                    {
                        sheet.Range["D9"].Value2 = "0110";
                    }


                    // I3 (длина)
                    sheet.Range["I3"].Value2 = Parametrs["LENGTH_FIRST"];
                    // I4 (ширина)
                    sheet.Range["I4"].Value2 = Parametrs["HEIGHT_FIRST"];
                    // I6 (S, кв м)
                    sheet.Range["I6"].Value2 = Parametrs["BILLET_SPRODUCT"].ToString().Replace(',', '.');
                    // G7 (Количество на заготовке)
                    if (isGasket)
                    {
                        sheet.Range["G7"].Value2 = "Количество на заготовке " + Parametrs["BILLET_QUANTITY_FIRST1"];
                    }
                    else
                    {
                        sheet.Range["G7"].Value2 = "* Допустимое отклонение размеров - по ТУ 17.21.13-002";
                    }
                    // I10 (длина заготовки)
                    // I11 (ширина заготовки)
                    if (isGasket)
                    {
                        sheet.Range["I10"].Value2 = Parametrs["BILLET_LENGTH_FIRST"];
                        sheet.Range["I11"].Value2 = Parametrs["BILLET_WIDTH_FIRST"];

                    }


                    // N4 (Цвет картона)
                    {
                        sheet.Range["N4"].Value2 = Parametrs["COLLOR_NAME"];
                    }
                    // N5 (Марка картона)
                    {
                        string brand = Parametrs["BRAND_NAME"].ToString();
                        if (Parametrs["BRAND_NAME"].ToString().Substring(0, 1) == "2")
                        {
                            brand = $"Т{brand}";
                        }
                        else if (Parametrs["BRAND_NAME"].ToString().Substring(0, 1) == "3")
                        {
                            brand = $"П{brand}";
                        }

                        sheet.Range["N5"].Value2 = brand;
                    }
                    // N6 (Профиль)
                    {
                        sheet.Range["N6"].Value2 = Parametrs["PROFILE_NAME"];
                    }
                }

                // Укдадка/Упаковка
                {
                    // P9 (Станок)
                    if(isGasket)
                    {
                        sheet.Range["P9"].Value2 = Parametrs["PRODUCTION_SCHEME_NAME"];
                    }
                    // R12 (Количество в пачке/стопе, шт.)
                    sheet.Range["R12"].Value2 = Parametrs["QUANTITY"].ToInt().ToString();
                    // R13 (Упаковка на поддоны)
                    {
                        if (Parametrs["TYPE_PACKAGE"] == "1")
                        {
                            sheet.Range["R13"].Value2 = "с упак.";
                        }
                        else
                        {
                            sheet.Range["R13"].Value2 = "россып.";
                        }
                    }

                    // P14 (Укладка на поддон)
                    sheet.Range["P14"].Value2 = $"Укладка на поддон {Parametrs["PALLET_NAME"]}";
                    // R15 (Кол-во пачек в ряду)
                    sheet.Range["R15"].Value2 = Parametrs["QUANTITY_PACK"];
                    // R16 (Кол-во рядов)
                    sheet.Range["R16"].Value2 = Parametrs["QUANTITY_ROWS"];
                    // R17 (Кол-во изд. на поддон)
                    sheet.Range["R17"].Value2 = Parametrs["QUANTITY_BOX"];

                    if (Parametrs["TYPE_PRODUCT"].ToInt().ContainsIn(1, 14))
                    {
                        sheet.Range["P12"].Value2 = "Кол-во в стопе, шт.";
                        sheet.Range["P15"].Value2 = "Кол-во стоп в ряду";
                    }
                    else
                    {
                        sheet.Range["P12"].Value2 = "Кол-во в пачке, шт.";
                        sheet.Range["P15"].Value2 = "Кол-во пачек в ряду";
                    }



                    // R19 (Подпрессовка паллеты)
                    {
                        if (Parametrs["PREPRESSING"].ToInt() == 1)
                        {
                            sheet.Range["R19"].Value2 = "есть";
                        }
                        else
                        {
                            sheet.Range["R19"].Value2 = "нет";
                        }
                    }
                    // R20 (Обвязка паллеты лентами)
                    {
                        if (Parametrs["STRAPPING"].ToInt() == 0)
                        {
                            sheet.Range["R20"].Value2 = "нет";
                        }
                        else
                        {
                            sheet.Range["R20"].Value2 = $"есть, {Parametrs["STRAPPING"]}";
                        }
                    }
                    // R21 (Уголки из гофрокартона)
                    {
                        if (Parametrs["CORNERS"].ToInt() == 0)
                        {
                            sheet.Range["R21"].Value2 = "нет";
                        }
                        else
                        {
                            sheet.Range["R21"].Value2 = "есть";
                        }
                    }
                    // R22 (Упаковка паллеты в стрейчплёнку)
                    {
                        if (Parametrs["PACKAGING"].ToInt() == 0)
                        {
                            sheet.Range["R22"].Value2 = "нет";
                        }
                        else
                        {
                            sheet.Range["R22"].Value2 = $"есть, F{Parametrs["PACKAGING"]}";
                        }
                    }



                    // R36 (длина (± 15))
                    {
                        sheet.Range["R36"].Value2 = Parametrs["PACKAGE_LENGTH"];
                    }
                    // R37 (ширина (± 15))
                    {
                        sheet.Range["R37"].Value2 = Parametrs["PACKAGE_WIDTH"];
                    }
                    // R38 (высота (± 50))
                    {
                        sheet.Range["R38"].Value2 = Parametrs["PACKAGE_HEIGTH"];
                    }
                }

                // Картинки
                {
                    // Картинка прокладки
                    {
                        ParsNotchesParametrs();

                        if (FlagRecreateExcelImg)
                        {
                            if (isGasket)
                            {
                                workBook = CreateImgGasket(workBook, "Прокладка", Parametrs.CheckGet("LENGTH_FIRST").ToDouble(), Parametrs.CheckGet("HEIGHT_FIRST").ToDouble(), ListDicNotchesParametrsFirst);
                            }
                            else
                            {
                                workBook = CreateImgGasket(workBook, "Лист", Parametrs.CheckGet("LENGTH_FIRST").ToDouble(), Parametrs.CheckGet("HEIGHT_FIRST").ToDouble(), ListDicNotchesParametrsFirst);
                            }

                            Excel.Shape pastedShape = sheet.Shapes.Item("Картинка_прокладки");
                            pastedShape.Name = "Картинка_прокладки";
                            Excel.Range cell1 = sheet.Range["B13"];
                            Excel.Range cell2 = sheet.Range["O36"];

                            if (Parametrs.CheckGet("LENGTH_FIRST").ToDouble() > Parametrs.CheckGet("HEIGHT_FIRST").ToDouble())
                            {
                                pastedShape.Left = (float)cell1.Left;
                                pastedShape.Top = (float)(cell1.Top + ((cell2.Top - cell1.Top) - pastedShape.Height) / 2);
                            }
                            else
                            {
                                pastedShape.Top = (float)cell1.Top;
                                pastedShape.Left = (float)(cell1.Left + ((cell2.Left - cell1.Left) - pastedShape.Width) / 2);
                            }

                        }
                    }

                    // Картинка укладки
                    {
                        var p = new Dictionary<string, string>();
                        p.Add("LAYING_SCHEME", Parametrs["LAYING_SCHEME"]);

                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Preproduction");
                        q.Request.SetParam("Object", "GasketTechnologicalMap");
                        q.Request.SetParam("Action", "GetLayingSchemeImage");

                        q.Request.SetParams(p);

                        q.DoQuery();

                        if (q.Answer.Status == 0)
                        {
                            var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                            if (result != null)
                            {
                                var ds = ListDataSet.Create(result, "ITEMS");
                                if (ds != null && ds.Items != null && ds.Items.Count > 0)
                                {
                                    byte[] bytes = Convert.FromBase64String(ds.Items.First().CheckGet("JPG"));
                                    var mem = new MemoryStream(bytes) { Position = 0 };
                                    var image = new BitmapImage();
                                    image.BeginInit();
                                    image.StreamSource = mem;
                                    image.CacheOption = BitmapCacheOption.OnLoad;
                                    image.EndInit();

                                    if (bytes != null && bytes.Length > 0)
                                    {
                                        string tempFilePath = Path.GetTempFileName() + ".jpg";
                                        File.WriteAllBytes(tempFilePath, bytes);

                                        Excel.Range cell1 = sheet.Range["P24"];
                                        Excel.Range cell2 = sheet.Range["Q31"];

                                        try
                                        {
                                            // Вставляем изображение
                                            Excel.Shape picture = sheet.Shapes.AddPicture(
                                                tempFilePath,
                                                Microsoft.Office.Core.MsoTriState.msoFalse,
                                                Microsoft.Office.Core.MsoTriState.msoTrue,
                                                10, 10, 100, 100); // Левый верхний угол (10,10) и размер (100x100)

                                            picture.Left = (float)cell1.Left;
                                            picture.Top = (float)cell1.Top;
                                            picture.Width = (float)(cell2.Left + cell2.Width) - (float)cell1.Left;
                                            picture.Height = (float)(cell2.Top + cell2.Height) - (float)cell1.Top;
                                            picture.Name = "Укладка1";

                                        }
                                        finally
                                        {
                                        }

                                        // Удаляем временный файл
                                        File.Delete(tempFilePath);
                                    }

                                }
                            }
                        }
                    }

                }

                // Подвал
                {
                    // C37 (Примечание)
                    {
                        sheet.Range["C37"].Value2 = Parametrs["NOTE_1_FOR_EXCEL"];
                    }

                    // C38 (Примечание)
                    {
                        sheet.Range["C38"].Value2 = Parametrs["NOTE_2_FOR_EXCEL"];
                    }

                    // C39 (Примечание)
                    {
                        sheet.Range["C39"].Value2 = Parametrs["NOTE_3_FOR_EXCEL"];
                    }

                    // C43 (Инженер ОПП)
                    {
                        sheet.Range["C43"].Value2 = Central.User.Name;
                        sheet.Range["A43"].Value2 = "Инженер ОПП";
                    }

                    // H42 (Дата)
                    {
                        sheet.Range["H42"].Value2 = DateTime.Now.ToString("dd.MM.yyyy");
                    }

                    // H43 (Дата)
                    {
                        sheet.Range["H43"].Value2 = DateTime.Now.ToString("dd.MM.yyyy");
                    }

                    // H44 (Дата)
                    {
                        sheet.Range["H44"].Value2 = DateTime.Now.ToString("dd.MM.yyyy");
                    }

                    // H45 (Дата)
                    {
                        sheet.Range["H45"].Value2 = DateTime.Now.ToString("dd.MM.yyyy");
                    }

                    // Q42 (Дата)
                    {
                        sheet.Range["Q42"].Value2 = DateTime.Now.ToString("dd.MM.yyyy");
                    }
                }

                // Если без упаковки - стираем лишнее
                if (!isGasket && Parametrs["TYPE_PACKAGE"] == "0")
                {
                    sheet.Range["P14:R38"].ClearContents();

                    foreach (Excel.Shape s in sheet.Shapes)
                    {
                        if (s.Name.Contains("Укладка"))
                        {
                            s.Delete();
                        }
                    }

                    sheet.Range["P14:R38"].Borders.LineStyle = Excel.XlLineStyle.xlLineStyleNone;
                    sheet.Range["P13:R13"].Borders[Excel.XlBordersIndex.xlEdgeBottom].LineStyle = Excel.XlLineStyle.xlContinuous;
                }
            }
            catch (Exception e)
            {
                Msg = $"Ошибка заполнения листа: {e.Message}";

                var error = new Common.Error();
                error.Code = 146;
                error.Message = "Ошибка заполнения листа";
                error.Description = e.ToString();
                Central.ProcError(error, "", true);
            }
            finally
            {
                ReleaseExcelObjects(sheet);
            }
            return workBook;
        }


        /// <summary>
        /// Заполнение листа решетки
        /// </summary>
        public Excel.Workbook FillPartitionSingleSheet(Excel.Workbook workBook, bool create = true)
        {
           
            try
            {
                var sheet = workBook.Worksheets["Решетка"];
                //Очищаем ячейки
                if (create)
                {
                    sheet.Range["A12:N34"].ClearContents();
                }
                else
                {
                    // Удаляем картинку решетки
                    if (FlagRecreateExcelImg)
                    {
                        foreach (Excel.Shape s in sheet.Shapes)
                        {
                            s.Delete();
                        }
                    }

                    // Удаляем картинку укладки
                    Excel.Range cell1 = sheet.Range["O23"];
                    Excel.Range cell2 = sheet.Range["R32"];
                    foreach (Excel.Shape s in sheet.Shapes)
                    {
                        if (s.Left < cell2.Left && s.Left >= cell1.Left && s.Top < cell2.Top && s.Top >= cell1.Top)
                        {
                            s.Delete();
                        }
                    }

                }

                //Шапка
                {

                    // C2 (Технологическая карта №)
                    {
                        if (!string.IsNullOrEmpty(Parametrs.CheckGet("NUMBER_FIRST")))
                        {
                            sheet.Range["C2"].Value2 = Parametrs["NUMBER_FIRST"];
                        }
                    }

                    // B5 (Наименов.)
                    {
                        sheet.Range["B5"].Value2 = Parametrs["NAME_FIRST"];
                    }

                    // B7 (Покупатель:)
                    {
                        sheet.Range["B7"].Value2 = Parametrs["CLIENT_NAME"];
                    }

                    // I3 (длина)
                    {
                        sheet.Range["I3"].Value2 = Parametrs["LENGTH_FIRST"];
                    }

                    // I4 (ширина)
                    {
                        sheet.Range["I4"].Value2 = Parametrs["HEIGHT_FIRST"];
                    }

                    // I6 (S, кв м)
                    {
                        sheet.Range["I6"].Value2 = Parametrs["BILLET_SPRODUCT"].ToString().Replace(',', '.');
                    }

                    // G7 (Количество на заготовке)
                    {
                        sheet.Range["G7"].Value2 = "Изделий из заготовки " + Parametrs["BILLET_QUANTITY_FIRST1"];
                    }

                    // I10 (длина)
                    {
                        sheet.Range["I10"].Value2 = Parametrs["BILLET_LENGTH_FIRST"];
                    }

                    // I11 (ширина)
                    {
                        sheet.Range["I11"].Value2 = Parametrs["BILLET_WIDTH_FIRST"];
                    }

                    // N4 (Цвет картона)
                    {
                        sheet.Range["N4"].Value2 = Parametrs["COLLOR_NAME"];
                    }

                    // N5 (Марка картона)
                    {
                        string brand = Parametrs["BRAND_NAME"].ToString();
                        if (Parametrs["BRAND_NAME"].ToString().Substring(0, 1) == "2")
                        {
                            brand = $"Т{brand}";
                        }
                        else if (Parametrs["BRAND_NAME"].ToString().Substring(0, 1) == "3")
                        {
                            brand = $"П{brand}";
                        }

                        sheet.Range["N5"].Value2 = brand;
                    }

                    // N6 (Профиль)
                    {
                        sheet.Range["N6"].Value2 = Parametrs["PROFILE_NAME"];
                    }
                }

                // Укдадка/Упаковка
                {
                    // P7 (Станок)
                    {
                        sheet.Range["P7"].Value2 = Parametrs["PRODUCTION_SCHEME_NAME"];
                    }

                    // R10 (Количество в пачке, шт.)
                    {
                        sheet.Range["R10"].Value2 = Parametrs["QUANTITY"];
                    }

                    // R11 (Упаковка на поддоны)
                    {
                        if (Parametrs["TYPE_PACKAGE"] == "1")
                        {
                            sheet.Range["R11"].Value2 = "с упак.";
                        }
                        else
                        {
                            sheet.Range["R11"].Value2 = "россып.";
                        }
                    }

                    // P13 (Укладка на поддон)
                    {
                        sheet.Range["AB23"].Value2 = $"Укладка на поддон {Parametrs["PALLET_NAME"]}";
                    }

                    // R14 (Кол-во пачек в ряду)
                    {
                        sheet.Range["R14"].Value2 = Parametrs["QUANTITY_PACK"];
                    }

                    // R15 (Кол-во рядов)
                    {
                        sheet.Range["R15"].Value2 = Parametrs["QUANTITY_ROWS"];
                    }

                    // R16 (Кол-во ящик. на поддон)
                    {
                        sheet.Range["R16"].Value2 = Parametrs["QUANTITY_BOX"];
                    }

                    // R19 (Подпрессовка паллеты)
                    {
                        if (Parametrs["PREPRESSING"].ToInt() == 1)
                        {
                            sheet.Range["R19"].Value2 = "есть";
                        }
                        else
                        {
                            sheet.Range["R19"].Value2 = "нет";
                        }
                    }

                    // R20 (Обвязка паллеты лентами)
                    {
                        if (Parametrs["STRAPPING"].ToInt() == 0)
                        {
                            sheet.Range["R20"].Value2 = "нет";
                        }
                        else
                        {
                            sheet.Range["R20"].Value2 = $"есть, {Parametrs["STRAPPING"]}";
                        }
                    }

                    // R21 (Уголки из гофрокартона)
                    {
                        if (Parametrs["CORNERS"].ToInt() == 0)
                        {
                            sheet.Range["R21"].Value2 = "нет";
                        }
                        else
                        {
                            sheet.Range["R21"].Value2 = "есть";
                        }
                    }

                    // R22 (Упаковка паллеты в стрейчплёнку)
                    {
                        if (Parametrs["PACKAGING"].ToInt() == 0)
                        {
                            sheet.Range["R22"].Value2 = "нет";
                        }
                        else
                        {
                            sheet.Range["R22"].Value2 = $"есть, F{Parametrs["PACKAGING"]}";
                        }
                    }

                    // R36 (длина (± 15))
                    {
                        sheet.Range["R36"].Value2 = Parametrs["PACKAGE_LENGTH"];
                    }

                    // R37 (ширина (± 15))
                    {
                        sheet.Range["R37"].Value2 = Parametrs["PACKAGE_WIDTH"];
                    }

                    // R38 (высота (± 50))
                    {
                        sheet.Range["R38"].Value2 = Parametrs["PACKAGE_HEIGTH"];
                    }
                }

                // Картинки
                {
                    // Картинка решетки
                    {
                        ParsNotchesParametrs();

                        double koef = 1;
                        // Расчет коэф. уменьшения размеров
                        {
                            Excel.Range start = sheet.Range["C13"];
                            Excel.Range end = sheet.Range["N32"];

                            double l1 = Parametrs.CheckGet("LENGTH_FIRST").ToDouble();
                            double h = Parametrs.CheckGet("HEIGHT_FIRST").ToDouble();

                            // Уменьшение по длине
                            koef = (float)((end.Left - start.Left) / l1);
                            
                            // Если высота не влезает - уменьшаем по высоте
                            if (h * koef > (float)(end.Top - start.Top))
                            {
                                koef = (float)((end.Top - start.Top) / h);
                            }
                        }

                        if (FlagRecreateExcelImg)
                        {
                            workBook = CreateImgPartition(workBook, "Решетка", Parametrs.CheckGet("LENGTH_FIRST").ToDouble(), Parametrs.CheckGet("HEIGHT_FIRST").ToDouble(), ListDicNotchesParametrsFirst, false, "" , koef);

                            Excel.Shape pastedShape = sheet.Shapes.Item("Картинка_решетки");

                            Excel.Range cell1 = sheet.Range["B13"];
                            Excel.Range cell2 = sheet.Range["N35"];

                            pastedShape.Left = (float)(cell1.Left + ((cell2.Left - cell1.Left) - pastedShape.Width) / 2);
                            pastedShape.Top = (float)(cell1.Top + ((cell2.Top - cell1.Top) - pastedShape.Height) / 2);
                        }
                    }

                    // Картинка укладки
                    {
                        var p = new Dictionary<string, string>();
                        p.Add("LAYING_SCHEME", Parametrs["LAYING_SCHEME"]);

                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Preproduction");
                        q.Request.SetParam("Object", "GasketTechnologicalMap");
                        q.Request.SetParam("Action", "GetLayingSchemeImage");

                        q.Request.SetParams(p);

                        q.DoQuery();

                        if (q.Answer.Status == 0)
                        {
                            var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                            if (result != null)
                            {
                                var ds = ListDataSet.Create(result, "ITEMS");
                                if (ds != null && ds.Items != null && ds.Items.Count > 0)
                                {
                                    byte[] bytes = Convert.FromBase64String(ds.Items.First().CheckGet("JPG"));
                                    var mem = new MemoryStream(bytes) { Position = 0 };
                                    var image = new BitmapImage();
                                    image.BeginInit();
                                    image.StreamSource = mem;
                                    image.CacheOption = BitmapCacheOption.OnLoad;
                                    image.EndInit();

                                    if (bytes != null && bytes.Length > 0)
                                    {
                                        string tempFilePath = Path.GetTempFileName() + ".jpg";
                                        File.WriteAllBytes(tempFilePath, bytes);

                                        Excel.Range cell1 = sheet.Range["P24"];
                                        Excel.Range cell2 = sheet.Range["Q31"];

                                        try
                                        {
                                            // Вставляем изображение
                                            Excel.Shape picture = sheet.Shapes.AddPicture(
                                                tempFilePath,
                                                Microsoft.Office.Core.MsoTriState.msoFalse,
                                                Microsoft.Office.Core.MsoTriState.msoTrue,
                                                10, 10, 100, 100); // Левый верхний угол (10,10) и размер (100x100)

                                            picture.Left = (float)cell1.Left;
                                            picture.Top = (float)cell1.Top;
                                            picture.Width = (float)(cell2.Left + cell2.Width) - (float)cell1.Left;
                                            picture.Height = (float)(cell2.Top + cell2.Height) - (float)cell1.Top;
                                            picture.Name = "Укладка1";

                                        }
                                        finally
                                        {
                                        }

                                        // Удаляем временный файл
                                        File.Delete(tempFilePath);
                                    }

                                }
                            }
                        }
                    }

                }

                // Подвал
                {
                    // C37 (Примечание)
                    {
                        sheet.Range["C37"].Value2 = Parametrs["NOTE_1_FOR_EXCEL"];
                    }

                    // C38 (Примечание)
                    {
                        sheet.Range["C38"].Value2 = Parametrs["NOTE_2_FOR_EXCEL"];
                    }

                    // C39 (Примечание)
                    {
                        sheet.Range["C39"].Value2 = Parametrs["NOTE_3_FOR_EXCEL"];
                    }

                    // C43 (Инженер ОПП)
                    {
                        sheet.Range["C43"].Value2 = Central.User.Name;
                        sheet.Range["A43"].Value2 = "Инженер ОПП";
                    }

                    // H42 (Дата)
                    {
                        sheet.Range["H42"].Value2 = DateTime.Now.ToString("dd.MM.yyyy");
                    }

                    // H43 (Дата)
                    {
                        sheet.Range["H43"].Value2 = DateTime.Now.ToString("dd.MM.yyyy");
                    }

                    // H44 (Дата)
                    {
                        sheet.Range["H44"].Value2 = DateTime.Now.ToString("dd.MM.yyyy");
                    }

                    // H45 (Дата)
                    {
                        sheet.Range["H45"].Value2 = DateTime.Now.ToString("dd.MM.yyyy");
                    }

                    // Q42 (Дата)
                    {
                        sheet.Range["Q42"].Value2 = DateTime.Now.ToString("dd.MM.yyyy");
                    }
                }

            }
            catch (Exception e)
            {
                Msg = $"Ошибка заполнения листа: {e.Message}";
            }
            finally
            {
            }
            return workBook;
        }
        #endregion

        #region "Создание картинки"

        private Excel.Workbook CreateImgPartition(Excel.Workbook book, string nameSheet, double l, double w, List<Dictionary<string, string>> list, bool isSmall = false, string pref = "", double koef = 1.0)
        {
            var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            Excel.Worksheet tmpSheet = book.Sheets[nameSheet];
            Excel.Worksheet sourceSheet = book.Sheets["Картинки2"];
            Dictionary<string, Excel.Shape> templates = new Dictionary<string, Excel.Shape>();

            try
            {
                #region "Берем группы из шаблона"
                var shapeNames = new (string key, string name)[]
                {
                    ("zub", isSmall ? "Зубец_м" : "Зубец"),
                    ("pros_lin", isSmall ? "Просечка_линия_м" : "Просечка_линия"),
                    ("dno", isSmall ? "Реш_дно_м" : "Реш_дно"),
                    ("gofra", "Гофра3"),
                    ("gr_pros", isSmall ? "Длинна_просечка_реш_м" : "Длинна_просечка_реш"),
                    ("vr_pros", isSmall ? "Вр_просечка_м" : "Вр_просечка"),
                    ("z_ver", isSmall ? "Засечка_вер_м" : "Засечка_вер"),
                    ("z_ver_b", isSmall ? "Засечка_вер_б_м" : "Засечка_вер_б"),
                    ("z_gor_b", isSmall ? "Засечка_гор_б_м" : "Засечка_гор_б")
                };
                foreach (var shapeInfo in shapeNames)
                {
                    Excel.Shape sourceShape = sourceSheet.Shapes.Item(shapeInfo.name);
                    sourceShape.Copy();
                    System.Threading.Thread.Sleep(250);
                    tmpSheet.Paste();

                    if (shapeInfo.name == "Зубец_м" || shapeInfo.name == "Зубец" || shapeInfo.name == "Реш_дно_м" || shapeInfo.name == "Реш_дно")
                    {
                        Excel.Shape pastedShape = tmpSheet.Shapes.Item(tmpSheet.Shapes.Count);
                        foreach (Shape item in pastedShape.GroupItems)
                        {
                            if (item.Name == "Прямоугольник1" || item.Name == "Прямоугольник2" || item.Name == "Прямоугольник3" || item.Name == "Прямоугольник4")
                            {
                                item.Fill.ForeColor.RGB = GetExcelColor(213, 204, 158);
                                item.Line.ForeColor.RGB = GetExcelColor(213, 204, 158);
                            }
                        }
                    }
                    
                    templates[shapeInfo.key] = tmpSheet.Shapes.Item(tmpSheet.Shapes.Count);
                }
                #endregion


                #region "Зубцы и дно"
                double x_start = 700;
                double y_start = 700;
                float tmp_w = 0;
                float tmp_l = 0;

                Excel.Range cell1 = null;
                Excel.Range cell2 = null;

                tmp_w = (float)(w*koef);
                tmp_l = (float)(l*koef);

                double sum = l;
                double y1_tmp = y_start + (tmp_w / 2.0); 
                double x1_tmp =  x_start;
                int i = 1;
                double l_pros = 3 * koef;

                List<string> shapesToGroup = new List<string>();
                foreach (var notch in list)
                {
                    Excel.Shape pastedShape = templates["zub"].Duplicate();
                    pastedShape.Width = (float)(notch.CheckGet("CONTENT").ToDouble()*koef - l_pros);
                    pastedShape.Height = (float)(tmp_w / 2.0);
                    pastedShape.Top = (float)y_start;
                    pastedShape.Left = (float)x1_tmp;
                    x1_tmp = x1_tmp + pastedShape.Width;
                    if (i != 1)
                    {
                        pastedShape.Width = (float)(pastedShape.Width - l_pros);
                        x1_tmp = x1_tmp - l_pros;
                    }
                    pastedShape.Name = "Зубец_" + i.ToString() + pref;

                    Excel.Shape pastedShape2 = templates["pros_lin"].Duplicate();
                    pastedShape2.Width = (float)(2* l_pros);
                    pastedShape2.Top = (float)y1_tmp;
                    pastedShape2.Left = (float)x1_tmp;
                    pastedShape2.Name = "Просечка_линия" + i.ToString() + pref;

                    shapesToGroup.Add(pastedShape.Name);
                    shapesToGroup.Add(pastedShape2.Name);


                    x1_tmp = x1_tmp + pastedShape2.Width;

                    sum = sum - notch.CheckGet("CONTENT").ToDouble();

                    i++;
                }

                // Последний зубец
                {
                    Excel.Shape pastedShape3 = templates["zub"].Duplicate();
                    pastedShape3.Width = (float)(sum*koef - l_pros);
                    pastedShape3.Height = (float)(tmp_w / 2.0);
                    pastedShape3.Top = (float)y_start;
                    pastedShape3.Left = (float)x1_tmp;
                    pastedShape3.Name = "Зубец_" + (list.Count + 1).ToString() + pref;

                    shapesToGroup.Add(pastedShape3.Name);
                }

                // Дно 
                {
                    Excel.Shape pastedShape4 = templates["dno"].Duplicate();
                    pastedShape4.Width = (float)(tmp_l);
                    pastedShape4.Height = (float)(tmp_w / 2.0);
                    pastedShape4.Top = (float)(y_start+ tmp_w/2.0);
                    pastedShape4.Left = (float)x_start;
                    pastedShape4.Name = "Дно" + pref;

                    shapesToGroup.Add(pastedShape4.Name);
                }

                #endregion


                #region "Длина и ширина"

                // Вся длина
                {
                    float h = 0;
                    if (nameSheet != "Решетка")
                    {
                        cell1 = tmpSheet.Cells[22, 1];
                        cell2 = tmpSheet.Cells[24, 1];
                        h = (float)((cell2.Top - cell1.Top) * 0.75);
                    }
                    else
                    {
                        cell1 = tmpSheet.Cells[34, 1];
                        cell2 = tmpSheet.Cells[36, 1];
                        h = (float)(cell2.Top - cell1.Top);
                    }

                    Excel.Shape pastedShape5 = templates["z_ver_b"].Duplicate();
                    pastedShape5.Height = (float)h;
                    pastedShape5.Top = (float)(y_start + tmp_w);
                    pastedShape5.Left = (float)x_start;
                    pastedShape5.Name = "Засечка_вер_б_1" + pref;

                    shapesToGroup.Add(pastedShape5.Name);

                    Excel.Shape pastedShape6 = templates["z_ver_b"].Duplicate();
                    pastedShape6.Height = (float)h;
                    pastedShape6.Top = (float)(y_start + tmp_w);
                    pastedShape6.Left = (float)(x_start + tmp_l);
                    pastedShape6.Name = "Засечка_вер_б_2" + pref;
                    var t1 = pastedShape6.Height;
                    shapesToGroup.Add(pastedShape6.Name);

                    Excel.Shape pastedShape7 = templates["gr_pros"].Duplicate();
                    pastedShape7.Height = (float)(h/2);
                    pastedShape7.Width = (float)(tmp_l);
                    pastedShape7.Top = (float)(y_start + tmp_w+ h/2 - 5);
                    pastedShape7.Left = (float)(x_start);
                    pastedShape7.Name = "Длина" + pref;

                    foreach (Shape item in pastedShape7.GroupItems)
                    {
                        if (item.Name == "Просечка_текст")
                        {
                            item.TextFrame.Characters().Text = l.ToString();
                        }
                    }


                    shapesToGroup.Add(pastedShape7.Name);

                }

                // Вся высота
                {
                    float w_tmp = 0;
                    if (nameSheet != "Решетка")
                    {
                        cell1 = tmpSheet.Cells[15, 2];
                        cell2 = tmpSheet.Cells[15, 3];
                        w_tmp = (float)(cell2.Left - cell1.Left);
                    }
                    else
                    {
                        cell1 = tmpSheet.Cells[13, 2];
                        cell2 = tmpSheet.Cells[13, 3];
                        w_tmp = (float)(cell2.Left - cell1.Left);
                    }
                    Excel.Shape pastedShape8 = templates["z_gor_b"].Duplicate();
                    pastedShape8.Width = (float)(w_tmp);
                    pastedShape8.Top = (float)(y_start);
                    pastedShape8.Left = (float)(x_start - pastedShape8.Width);
                    pastedShape8.Name = "Засечка_гор_б_1" + pref;

                    shapesToGroup.Add(pastedShape8.Name);

                    Excel.Shape pastedShape9 = templates["z_gor_b"].Duplicate();
                    pastedShape9.Width = (float)(w_tmp);
                    pastedShape9.Top = (float)(y_start + tmp_w);
                    pastedShape9.Left = (float)(x_start - pastedShape9.Width);
                    pastedShape9.Name = "Засечка_гор_б_2" + pref;

                    shapesToGroup.Add(pastedShape9.Name);

                    Excel.Shape pastedShape10 = templates["vr_pros"].Duplicate();
                    pastedShape10.Width = (float)(w_tmp/2.0);
                    pastedShape10.Height = (float)tmp_w;
                    pastedShape10.Top = (float)(pastedShape8.Top);
                    pastedShape10.Left = (float)(pastedShape8.Left);
                    pastedShape10.Name = "Высота" + pref;

                    foreach (Shape item in pastedShape10.GroupItems)
                    {
                        if (item.Name == "Просечка_текст")
                        {
                            item.TextFrame.Characters().Text = w.ToString();
                        }
                    }

                    shapesToGroup.Add(pastedShape10.Name);

                }
                #endregion


                #region "Просечки"
                if (nameSheet != "Решетка")
                {
                    cell1 = tmpSheet.Cells[15, 1];
                    cell2 = tmpSheet.Cells[16, 1];
                }
                else
                {
                    cell1 = tmpSheet.Cells[13, 1];
                    cell2 = tmpSheet.Cells[15, 1];
                }
                y1_tmp = y_start;
                x1_tmp = x_start;
                var height = (float)(cell2.Top - cell1.Top);
                i = 1;

                Excel.Shape pastedShape14 = templates["z_ver"].Duplicate();
                pastedShape14.Height = height;
                pastedShape14.Top = (float)(y_start - pastedShape14.Height);
                pastedShape14.Left = (float)x1_tmp;
                pastedShape14.Name = "Просечка_лин0" + pref;
                shapesToGroup.Add(pastedShape14.Name);

                foreach (var notch in list)
                {
                    Excel.Shape pastedShape11 = templates["gr_pros"].Duplicate();

                    pastedShape11.Height = height/2;
                    pastedShape11.Width = (float)(notch.CheckGet("CONTENT").ToDouble()*koef);
                    pastedShape11.Top = (float)(y1_tmp - height);
                    pastedShape11.Left = (float)x1_tmp;
                    foreach (Shape item in pastedShape11.GroupItems)
                    {
                        if (item.Name == "Просечка_текст")
                        {
                            item.TextFrame.Characters().Text = notch.CheckGet("CONTENT");
                            if (i == 1 && notch.CheckGet("CONTENT").ToDouble() != sum)
                            {
                                item.TextFrame.Characters().Font.Color = (int)XlRgbColor.rgbRed;
                            }
                        }
                    }
                    pastedShape11.Name = "Просечка_" + i.ToString() + pref;
                    x1_tmp += pastedShape11.Width;
                    shapesToGroup.Add(pastedShape11.Name);

                    Excel.Shape pastedShape12 = templates["z_ver"].Duplicate();
                    pastedShape12.Height = height;
                    pastedShape12.Top = (float)(y1_tmp - height);
                    pastedShape12.Left = (float)x1_tmp;
                    pastedShape12.Name = "Просечка_лин" + i.ToString() + pref;
                    shapesToGroup.Add(pastedShape12.Name);

                    i++;
                }

                Excel.Shape pastedShape13 = templates["gr_pros"].Duplicate();

                pastedShape13.Width = (float)(sum*koef);
                pastedShape13.Height = height/2;
                pastedShape13.Top = (float)(y1_tmp - height);
                pastedShape13.Left = (float)x1_tmp;
                foreach (Shape item in pastedShape13.GroupItems)
                {
                    if (item.Name == "Просечка_текст")
                    {
                        item.TextFrame.Characters().Text = sum.ToString();
                        if (sum != list[0].CheckGet("CONTENT").ToDouble())
                        {
                            item.TextFrame.Characters().Font.Color = (int)XlRgbColor.rgbRed;
                        }
                    }
                }
                pastedShape13.Name = "Просечка_" + i.ToString() + pref;
                x1_tmp += pastedShape13.Width;
                shapesToGroup.Add(pastedShape13.Name);


                Excel.Shape pastedShape15 = templates["z_ver"].Duplicate();

                pastedShape15.Top = (float)(y1_tmp - height);
                pastedShape15.Left = (float)x1_tmp;
                pastedShape15.Name = "Просечка_лин" + (i + 1).ToString() + pref;
                shapesToGroup.Add(pastedShape15.Name);
                #endregion

                #region "Гофра"
                Excel.Shape pastedShape16 = templates["gofra"].Duplicate();
                var new_w = (float)(tmp_l * 0.3);
                var new_h = (float)(tmp_w * 0.4);
                pastedShape16.Width = (float)(new_w);
                pastedShape16.Height = (float)(new_h);
                pastedShape16.Left = (float)(x_start +10*koef);
                pastedShape16.Top = (float)(y_start + tmp_w - pastedShape16.Height-10*koef);
                pastedShape16.Name = "Гофра_для_реш" + pref;
                shapesToGroup.Add(pastedShape16.Name);
                #endregion

                List<string> existingShapes = new List<string>();

                // Проверяем какие фигуры действительно существуют
                for (int j = 1; j <= tmpSheet.Shapes.Count; j++)
                {
                    Excel.Shape shape = tmpSheet.Shapes.Item(j);
                    if (shapesToGroup.Contains(shape.Name))
                    {
                        existingShapes.Add(shape.Name);
                        Console.WriteLine($"{j}) Найдена для группировки: {shape.Name}");
                    }
                }

                Excel.Shape group = tmpSheet.Shapes.Range[existingShapes.ToArray()].Group();
                group.Name = "Картинка_решетки";

            }
            catch (Exception e)
            {
                Msg = $"Ошибка заполнения листа: {e.Message}";
                totalStopwatch.Stop();
                var t = totalStopwatch.ElapsedMilliseconds;
                var error = new Common.Error();
                error.Code = 146;
                error.Message = "Ошибка заполнения листа";
                error.Description = e.ToString();
                Central.ProcError(error, "", true);
            }
            finally 
            {
                foreach (var template in templates.Values)
                {
                    try
                    {
                        if (template != null)
                        {
                            template.Delete();
                            System.Runtime.InteropServices.Marshal.ReleaseComObject(template);
                        }
                    }
                    catch (Exception e)
                    {
                        var tt = e.Message;
                    }
                }
                templates.Clear();
                totalStopwatch.Stop();
                var t = totalStopwatch.ElapsedMilliseconds;
                ReleaseExcelObjects(tmpSheet, sourceSheet);
            }

            return book;

        }

        private Excel.Workbook CreateImgGasket(Excel.Workbook book, string nameSheet, double l, double w, List<Dictionary<string, string>> list)
        {
            Excel.Worksheet tmpSheet = book.Sheets[nameSheet];
            Excel.Worksheet sourceSheet = book.Sheets["Картинки2"];
            Dictionary<string, Excel.Shape> templates = new Dictionary<string, Excel.Shape>();

            try
            {
                #region "Берем группы из шаблона"
                var shapeNames = new (string key, string name)[]
                {
                    ("rectangle", "Прямоугольник"),
                    ("length", "П_Гр_длинна"),
                    ("width", "П_Гр_ширина"),
                    ("gofra1", "Гофра1"),
                    ("gofra2", "Гофра2"),
                    ("dotted_line", "Пунктир"),
                    ("notch", "Засечка"),
                    ("crease_arrow", "Вр_просечка"),
                };
                foreach (var shapeInfo in shapeNames)
                {
                    Excel.Shape sourceShape = sourceSheet.Shapes.Item(shapeInfo.name);
                    sourceShape.Copy();
                    System.Threading.Thread.Sleep(250);
                    tmpSheet.Paste();
                    templates[shapeInfo.key] = tmpSheet.Shapes.Item(tmpSheet.Shapes.Count);
                }
                #endregion

                List<string> shapesToGroup = new List<string>();
                Excel.GroupShapes groupItems = null;
                Excel.Shape pastedShape = null;

                double x_start = 700;
                double y_start = 700;
                float tmp_w = 0;
                float tmp_l = 0;
                float k = 1;



                #region "Прямоугольник"
                {
                    Excel.Range cell1 = tmpSheet.Cells[13, 3];
                    Excel.Range cell2 = tmpSheet.Cells[34, 15];
                    pastedShape = templates["rectangle"].Duplicate();
                    
                    if(w> cell2.Top - cell1.Top)
                    {
                        tmp_w = (float)(cell2.Top - cell1.Top);
                        k = (float)(w / (cell2.Top - cell1.Top));
                        tmp_l = (float)(l / k);
                        if(tmp_l > cell2.Left - cell1.Left)
                        {
                            tmp_l = (float)(cell2.Left - cell1.Left);
                            k = (float)(l / (cell2.Left - cell1.Left));
                            tmp_w = (float)(w / k);
                        }
                    }
                    else if (l > cell2.Left - cell1.Left)          // Будет ли уменьшение длины
                    {
                        tmp_l = (float)(cell2.Left - cell1.Left);
                        k = (float)(l / (cell2.Left - cell1.Left));
                        tmp_w = (float)(w / k);
                    }
                    else
                    {
                        tmp_w = (float)w;
                        tmp_l = (float)l;
                    }
                    pastedShape.Width = (float)(tmp_l);
                    pastedShape.Height = (float)(tmp_w);
                    pastedShape.Top = (float)y_start;
                    pastedShape.Left = (float)x_start;
                    pastedShape.Name = "Прямоугольник1";

                    shapesToGroup.Add(pastedShape.Name);
                }

                #endregion



                #region "Длина и ширина"
                // Длина
                {
                    Excel.Range cell1 = tmpSheet.Cells[34, 1];
                    Excel.Range cell2 = tmpSheet.Cells[36, 1];
                    pastedShape = templates["length"].Duplicate();
                    pastedShape.Width = (float)(tmp_l);
                    pastedShape.Height = (float)(cell2.Top - cell1.Top);
                    pastedShape.Top = (float)(y_start + tmp_w);
                    pastedShape.Left = (float)x_start;
                    pastedShape.Name = "Длина";
                    groupItems = pastedShape.GroupItems;
                    foreach (Excel.Shape item in groupItems)
                    {
                        if (item.Name == "П_длина")
                        {
                            item.TextFrame.Characters().Text = Parametrs["LENGTH_FIRST"];
                            break;
                        }

                    }
                    shapesToGroup.Add(pastedShape.Name);
                }

                // Ширина
                {
                    Excel.Range cell1 = tmpSheet.Cells[14, 2];
                    Excel.Range cell2 = tmpSheet.Cells[14, 3];
                    pastedShape = null;
                    pastedShape = templates["width"].Duplicate();
                    pastedShape.Height = (float)(tmp_w);
                    pastedShape.Width = (float)(cell2.Left - cell1.Left);
                    pastedShape.Top = (float)y_start;
                    pastedShape.Left = (float)(x_start - pastedShape.Width);
                    pastedShape.Name = "Ширина";

                    groupItems = pastedShape.GroupItems;
                    foreach (Excel.Shape item in groupItems)
                    {
                        if (item.Name == "П_ширина")
                        {
                            item.TextFrame.Characters().Text = Parametrs["HEIGHT_FIRST"];
                            break;
                        }

                    }
                    shapesToGroup.Add(pastedShape.Name);
                }



                #endregion

                #region "Рилёвки"
                {
                    var y1_tmp = y_start;
                    var i = 1;
                    double sum = 0;
                    double tmp_width = 0;
                    double tmp_width_arrow = 0;
                    Excel.Range cell1 = tmpSheet.Cells[14, 2];
                    Excel.Range cell2 = tmpSheet.Cells[14, 3];
                    tmp_width = (float)((cell2.Left - cell1.Left) / 2);
                    tmp_width_arrow = tmp_width / 2;
                    if (list!=null && list.Count > 0) 
                    {
                        foreach (var notch in list)
                        {
                            pastedShape = null;
                            pastedShape = templates["dotted_line"].Duplicate();
                            pastedShape.Width = (float)tmp_l;
                            pastedShape.Top = (float)(y1_tmp + notch.CheckGet("CONTENT").ToDouble() / k);
                            pastedShape.Left = (float)x_start;
                            pastedShape.Name = "Пунктир_" + i.ToString();
                            shapesToGroup.Add(pastedShape.Name);

                            pastedShape = null;
                            pastedShape = templates["notch"].Duplicate();
                            pastedShape.Width = (float)tmp_width;
                            pastedShape.Top = (float)(y1_tmp + notch.CheckGet("CONTENT").ToDouble() / k);
                            pastedShape.Left = (float)(x_start - pastedShape.Width);
                            pastedShape.Name = "Засечка_" + i.ToString();
                            shapesToGroup.Add(pastedShape.Name);

                            pastedShape = null;
                            pastedShape = templates["crease_arrow"].Duplicate();
                            pastedShape.Height = (float)(notch.CheckGet("CONTENT").ToDouble() / k);
                            pastedShape.Top = (float)(y1_tmp);
                            pastedShape.Left = (float)(x_start - tmp_width_arrow - pastedShape.Width);
                            foreach (Shape item in pastedShape.GroupItems)
                            {
                                if (item.Name == "Просечка_текст")
                                {
                                    item.TextFrame.Characters().Text = notch.CheckGet("CONTENT");
                                }
                            }
                            pastedShape.Name = "Размер_просечки_" + i.ToString();
                            shapesToGroup.Add(pastedShape.Name);

                            y1_tmp += notch.CheckGet("CONTENT").ToDouble() / k;
                            sum += notch.CheckGet("CONTENT").ToDouble();
                            i++;
                        }
                        pastedShape = null;
                        pastedShape = templates["crease_arrow"].Duplicate();
                        pastedShape.Height = (float)((w - sum) / k);
                        pastedShape.Top = (float)(y1_tmp);
                        pastedShape.Left = (float)(x_start - tmp_width_arrow - pastedShape.Width);
                        foreach (Shape item in pastedShape.GroupItems)
                        {
                            if (item.Name == "Просечка_текст")
                            {
                                item.TextFrame.Characters().Text = (w - sum).ToString();
                            }
                        }
                        pastedShape.Name = "Размер_просечки_" + i.ToString();
                        shapesToGroup.Add(pastedShape.Name);
                    }

                    
                }


                #endregion

                #region "Гофра"

                pastedShape = null;
                pastedShape = templates["gofra2"].Duplicate();
                pastedShape.Left = (float)(x_start - 10 + tmp_l - pastedShape.Width);
                pastedShape.Top = (float)(y_start + 10);
                var left = pastedShape.Left;
                pastedShape.Name = "Гофра_1";
                shapesToGroup.Add(pastedShape.Name);

                if(nameSheet == "Прокладка")
                {
                    pastedShape = null;
                    pastedShape = templates["gofra1"].Duplicate();
                    pastedShape.Left = (float)(left - 10 - pastedShape.Width);
                    pastedShape.Top = (float)(y_start + 10);
                    pastedShape.Name = "Гофра_2";
                    shapesToGroup.Add(pastedShape.Name);
                }
                #endregion

                List<string> existingShapes = new List<string>();

                // Проверяем какие фигуры действительно существуют
                for (int j = 1; j <= tmpSheet.Shapes.Count; j++)
                {
                    Excel.Shape shape = tmpSheet.Shapes.Item(j);
                    if (shapesToGroup.Contains(shape.Name))
                    {
                        existingShapes.Add(shape.Name);
                        Console.WriteLine($"{j}) Найдена для группировки: {shape.Name}");
                    }
                }

                Excel.Shape group = tmpSheet.Shapes.Range[existingShapes.ToArray()].Group();
                group.Name = "Картинка_прокладки";
            }
            catch (Exception e)
            {
                Msg = $"Ошибка заполнения листа: {e.Message}";
                var error = new Common.Error();
                error.Code = 146;
                error.Message = "Ошибка заполнения листа";
                error.Description = e.ToString();
                Central.ProcError(error, "", true);
            }
            finally
            {
                foreach (var template in templates.Values)
                {
                    try
                    {
                        if (template != null)
                        {
                            template.Delete();
                            System.Runtime.InteropServices.Marshal.ReleaseComObject(template);
                        }
                    }
                    catch (Exception e)
                    {
                        var tt = e.Message;
                    }
                }
                templates.Clear();
                ReleaseExcelObjects(tmpSheet, sourceSheet);

            }

            return book;

        }

        private int GetExcelColor(int red, int green, int blue)
        {
            return red + (green * 256) + (blue * 65536);
        }

        #endregion

        #endregion




        #region "Перемещение файла"
        public string MoveToWork()
        {
            var msg = "";

            var pathNew = Path.Combine(FilePathNew, FileNameOld);
            var pathWork = Path.Combine(FilePathWork, FileNameOld);
            var pathArchive = Path.Combine(FilePathArchive, FileNameOld);
            var pathTk = "";
            int typeExist = 0;
            if (System.IO.File.Exists(pathNew))
            {
                typeExist = 1;
                pathTk = pathNew;
            }
            if (System.IO.File.Exists(pathArchive))
            {
                typeExist = 3;
                pathTk = pathArchive;
            }

            try
            {
                if (File.Exists(pathTk))
                {
                    if (File.Exists(pathWork))
                    {
                        File.Delete(pathWork);
                    }

                    File.Move(pathTk, pathWork); // Перемещаем
                }
                else
                {
                    msg = "Исходный файл не найден!";
                }
            }
            catch (Exception ex)
            {
                msg = $"Ошибка перемещения файла: {ex.Message}";
            }

            return msg;
        }
        public string MoveToArchive()
        {
            var msg = "";

            var pathNew = Path.Combine(FilePathNew, FileNameOld);
            var pathWork = Path.Combine(FilePathWork, FileNameOld);
            var pathArchive = Path.Combine(FilePathArchive, FileNameOld);
            var pathTk = "";
            int typeExist = 0;
            if (System.IO.File.Exists(pathNew))
            {
                typeExist = 1;
                pathTk = pathNew;
            }
            if (System.IO.File.Exists(pathWork))
            {
                typeExist = 2;
                pathTk = pathWork;
            }

            try
            {
                if (File.Exists(pathTk))
                {
                    if (File.Exists(pathArchive))
                    {
                        File.Delete(pathArchive);
                    }

                    File.Move(pathTk, pathArchive); // Перемещаем
                }
                else
                {
                    msg = "Исходный файл не найден!";
                }
            }
            catch (Exception ex)
            {
                msg = $"Ошибка перемещения файла: {ex.Message}";
            }

            return msg;
        }
        #endregion




        #region "Комплект техкарт"
        /// <summary>
        /// Создание пустого файла комплекта техкарт
        /// </summary>
        public void CreateTkSetExcelFile()
        {
            FileNameNew = GetExcelName(Parametrs["ID_TK"].ToInt());
            var pathTk = Path.Combine(FilePathNew, FileNameNew);

            Excel.Application excelApp = new Excel.Application();
            Excel.Workbook workbook = excelApp.Workbooks.Add();

            try
            {
                workbook.SaveAs(pathTk);
            }
            catch (Exception ex)
            {
                Msg = $"Ошибка создания файла: {ex.Message}";
            }
            finally
            {
                if (workbook != null)
                {
                    workbook.Close();
                }
                excelApp.Quit();
            }
        }


        public void CopyExcelSheet(string sheetName, string nameNew)
        {
            var pathTkMain = "";
            var pathTk = "";

            var pathNew = Path.Combine(FilePathNew, nameNew);
            if (File.Exists(pathNew))
            {
                pathTkMain = pathNew;
            }

            var pathWork = Path.Combine(FilePathWork, nameNew);
            if (File.Exists(pathWork))
            {
                pathTkMain = pathWork;
            }

            if (pathTkMain == "")
            {
                Msg = "Не найден файл комплекта техкарт.";
            }
            else
            {
                pathNew = Path.Combine(FilePathNew, FileNameNew);
                if (File.Exists(pathNew))
                {
                    pathTk = pathNew;
                }

                pathWork = Path.Combine(FilePathWork, FileNameNew);
                if (File.Exists(pathWork))
                {
                    pathTk = pathWork;
                }

                if (pathTk == "")
                {
                    Msg = $"Не найден файл техкарты комплектующего ({sheetName}).";
                }
                else
                {
                    Excel.Application excelApp = null;
                    Excel.Workbook workbookMain = null;
                    Excel.Workbook workbook = null;
                    try
                    {
                        excelApp = new Excel.Application(); 
                        excelApp.DisplayAlerts = false;
                        excelApp.AlertBeforeOverwriting = false;
                        excelApp.ScreenUpdating = false;


                        workbookMain = excelApp.Workbooks.Open(pathTkMain);
                        workbook = excelApp.Workbooks.Open(pathTk);

                        Excel.XlFileFormat format = Excel.XlFileFormat.xlWorkbookNormal;

                        Excel.Worksheet sourceSheet = null; 
                        bool sheetFound = false;
                        // Поиск исходного листа
                        foreach (Excel.Worksheet sheet in workbook.Sheets)
                        {
                            if (sheet.Name == sheetName)
                            {
                                sourceSheet = sheet;
                                sheetFound = true;
                                break;
                            }
                        }

                        // Если лист не найден, проверяем лист "Ящик"
                        if (!sheetFound)
                        {
                            foreach (Excel.Worksheet sheet in workbook.Sheets)
                            {
                                if (sheet.Name == "Ящик")
                                {
                                    sourceSheet = sheet;
                                    sheetFound = true;
                                    break;
                                }
                            }
                        }

                        if (!sheetFound)
                        {
                            Msg = $"Лист '{sheetName}' не найден в файле комплектующего.";
                            return;
                        }

                        if (sourceSheet.Name == "Ящик")
                        {
                            sourceSheet.Name = sheetName;
                        }

                        // Копируем лист в основную книгу
                        sourceSheet.Copy(After: workbookMain.Sheets[workbookMain.Sheets.Count]);

                        // Сохраняем основную книгу
                        workbookMain.Save();

                        bool willBeEmptyAfterDelete = (workbook.Sheets.Count == 1);

                        if (willBeEmptyAfterDelete)
                        {
                            // Если после удаления книга станет пустой - закрываем и удаляем файл
                            workbook.Close(SaveChanges: false);
                            File.Delete(pathTk);
                        }
                        else
                        {
                            // Если останутся другие листы - удаляем и сохраняем
                            sourceSheet.Delete();
                            workbook.Save();
                        }

                    }
                    catch (Exception ex)
                    {
                        Msg = $"Ошибка копирования листа: {ex.Message}";
                    }
                    finally
                    {
                        try
                        {
                            if (workbookMain != null)
                            {
                                workbookMain.Close(SaveChanges: false);
                                System.Runtime.InteropServices.Marshal.ReleaseComObject(workbookMain);
                            }
                        }
                        catch { }

                        try
                        {
                            if (workbook != null)
                            {
                                workbook.Close(SaveChanges: false);
                                System.Runtime.InteropServices.Marshal.ReleaseComObject(workbook);
                            }
                        }
                        catch { }

                        try
                        {
                            if (excelApp != null)
                            {
                                excelApp.Quit();
                                System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);
                            }
                        }
                        catch { }
                    }
                }
            }
        }


        public void SeveranceExcelSheet(string sheetName, string nameNew)
        {
            var pathTkMain = "";
            var pathTk = "";

            var pathNew = Path.Combine(FilePathNew, nameNew);
            if (File.Exists(pathNew))
            {
                pathTkMain = pathNew;
            }

            var pathWork = Path.Combine(FilePathWork, nameNew);
            if (File.Exists(pathWork))
            {
                pathTkMain = pathWork;
            }

            if (string.IsNullOrEmpty(pathTkMain))
            {
                FileNameNew = "";
                return;
            }

            FileNameNew = GetExcelName(Parametrs["ID_TK"].ToInt());
            pathTk = Path.Combine(FilePathNew, FileNameNew);

            Excel.Application excelApp = null;
            Excel.Workbook workbookMain = null;
            Excel.Workbook workbookNew = null;

            try
            {
                excelApp = new Excel.Application();
                excelApp.DisplayAlerts = false;
                excelApp.AlertBeforeOverwriting = false;
                excelApp.ScreenUpdating = false;

                workbookMain = excelApp.Workbooks.Open(pathTkMain);

                Excel.XlFileFormat format = Excel.XlFileFormat.xlWorkbookNormal;
                Excel.Worksheet sourceSheet = null;
                bool sheetFound = false;

                // Ищем нужный лист
                foreach (Excel.Worksheet sheet in workbookMain.Sheets)
                {
                    if (sheet.Name == sheetName)
                    {
                        sourceSheet = sheet;
                        sheetFound = true;
                        break;
                    }
                }
                if (!sheetFound)
                {
                    FileNameNew = "";
                    return;
                }

                // Создаем новую книгу
                workbookNew = excelApp.Workbooks.Add();

                // Копируем лист в новую книгу
                sourceSheet.Copy(Before: workbookNew.Sheets[1]);

                // Удаляем лишние листы из новой книги (оставляем только скопированный)
                while (workbookNew.Sheets.Count > 1)
                {
                    Excel.Worksheet extraSheet = workbookNew.Sheets[2] as Excel.Worksheet;
                    extraSheet.Delete();
                }
                workbookNew.SaveAs(pathTk, format);

                // Проверяем, сколько листов останется в основной книге после удаления
                bool willBeEmptyAfterDelete = (workbookMain.Sheets.Count == 1);

                if (willBeEmptyAfterDelete)
                {
                    // Если это последний лист - удаляем всю книгу
                    workbookMain.Close(SaveChanges: false);
                    File.Delete(pathTkMain);
                }
                else
                {
                    // Удаляем лист из основной книги
                    sourceSheet.Delete();
                    workbookMain.SaveAs(pathTkMain, format);
                }
            }
            catch (Exception ex)
            {
                Msg = $"Ошибка переноса листа из файла комплекта техкарт: {ex.Message}";
            }
            finally
            {
                try
                {
                    if (workbookMain != null)
                    {
                        workbookMain.Close(SaveChanges: false);
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(workbookMain);
                    }
                }
                catch { }

                try
                {
                    if (workbookNew != null)
                    {
                        workbookNew.Close(SaveChanges: false);
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(workbookNew);
                    }
                }
                catch { }

                try
                {
                    if (excelApp != null)
                    {
                        excelApp.Quit();
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);
                    }
                }
                catch { }
            }
        }

        public string GetExcelName(int id_tk)
        {
            var name = "";
            var p = new Dictionary<string, string>(){
                {"ID_TK", id_tk.ToString()},
            };

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "TechnologicalMapExcel");
            q.Request.SetParam("Action", "CreateName");
            q.Request.SetParams(p);


            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");

                        if (ds != null && ds.Items.Count != 0)
                        {
                            name = ds.Items[0].CheckGet("NAME");
                        }
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
            return name;
        }
        #endregion

    }
}
