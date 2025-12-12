using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Office.Interop;

namespace Client.Interfaces.Production.ProcessingMachines
{
    /// <summary>
    /// Интерфейс для автоматического открытия Excel файла тех карты в зависимости от продукции, производимой в данный момент на этом станке
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class TechnologicalMapExcel : UserControl
    {
        public TechnologicalMapExcel()
        {
            FrameName = "TechnologicalMapExcel";
            ExcelApplication = new Microsoft.Office.Interop.Excel.Application();

            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            ProcessPermissions();
            Init();
            SetDefaults();

            RunAutoUpdateTimer();
        }

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Список станков
        /// ID -- Ид станка
        /// NAME -- Наименование станка
        /// </summary>
        public ListDataSet MachinesDataSet { get; set; }

        /// <summary>
        /// Данные по продукции, которая сейчас производится на выбранном станке
        /// </summary>
        public Dictionary<string,string> DictionaryByCurrentProduct { get; set; }

        /// <summary>
        /// ИД продукции, которую в данный момент делают на выбранном станке
        /// </summary>
        public int CurrentId2 { get; set; }

        /// <summary>
        /// Текущий путь к эксель файлу тех карты
        /// </summary>
        public string CurrentPathTk { get; set; }

        /// <summary>
        /// Текущий активный лист в эксель файле тех карты
        /// </summary>
        public int CurrentPageTk { get; set; }

        /// <summary>
        /// интервал автообновления грида, сек
        /// 0- автообновление отключено
        /// </summary>
        public int AutoUpdateInterval { get; set; }
        public DispatcherTimer AutoUpdateTimer { get; set; }

        public Microsoft.Office.Interop.Excel.Application ExcelApplication { get; set; }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel("[erp]technological_map_excel");
            var userAccessMode = mode;
            switch (mode)
            {
                case Role.AccessMode.Special:
                    break;

                case Role.AccessMode.FullAccess:
                    break;

                case Role.AccessMode.ReadOnly:
                default:
                    break;
            }

            List<Button> buttons = UIUtil.GetVisualChilds<Button>(this.Content as DependencyObject);
            if (buttons != null && buttons.Count > 0)
            {
                foreach (var button in buttons)
                {
                    var buttonTagList = UIUtil.GetTagList(button);
                    var accessMode = Acl.FindTagAccessMode(buttonTagList);
                    if (accessMode > userAccessMode)
                    {
                        button.IsEnabled = false;
                    }
                }
            }
        }

        public void Init()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="CURRENT_MACHINE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CurrentMachine,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CURRENT_PRODUCT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CurrentProduct,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            AutoUpdateInterval = 5;

            MachinesDataSet = new ListDataSet();
            DictionaryByCurrentProduct = new Dictionary<string, string>();
            CurrentMachine.Items.Clear();
            CurrentProduct.Clear();
            CurrentId2 = 0;

            LoadMachinesList();

            if (MachinesDataSet.Items.Count > 0)
            {
                GetCurrentMachine();
            }

            if (CurrentMachine.Items.Count > 0)
            {
                if (CurrentMachine.SelectedItem.Key.ToInt() > 0)
                {
                    LoadCurrentProduct();
                }
            }
        }

        /// <summary>
        /// Получаем список станков
        /// </summary>
        public void LoadMachinesList()
        {
            DisableControls();

            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "TechnologicalMapExcel");
            q.Request.SetParam("Action", "ListMachine");

            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");

                    MachinesDataSet = ds;

                    Dictionary<string, string> emptyDic = new Dictionary<string, string>();
                    emptyDic.Add("ID", "-1");
                    emptyDic.Add("NAME", " ");

                    MachinesDataSet.Items.Add(emptyDic);
                    MachinesDataSet.Items = MachinesDataSet.Items.OrderBy(x => x.CheckGet("ID").ToInt()).ToList();

                    CurrentMachine.SetItems(MachinesDataSet, "ID", "NAME");
                }
            }

            EnableControls();
        }

        /// <summary>
        /// Получаем текущий станок из конфига
        /// Устанавливаем его в качестве выбранного в селектбоксе списка станков
        /// </summary>
        public void GetCurrentMachine()
        {
            if (Central.Config.CurrentMachineId > 0)
            {
                // Если в конфиге указан станок, выбираем его

                int idSt = Central.Config.CurrentMachineId;

                if (CurrentMachine.Items.ContainsKey(idSt.ToString()))
                {
                    CurrentMachine.SelectedItem = CurrentMachine.Items.FirstOrDefault(x => x.Key.ToInt() == idSt);
                }
                else
                {
                    // Иначе выбираем пустой элемент

                    CurrentMachine.SetSelectedItemByKey("-1");
                }
            }
            else
            {
                // Иначе выбираем пустой элемент

                CurrentMachine.SetSelectedItemByKey("-1");
            }
        }

        /// <summary>
        /// Получаем текущую продукцию, которая производится на заданном станке
        /// </summary>
        public void LoadCurrentProduct()
        {
            DisableControls();

            if (CurrentMachine.SelectedItem.Key.ToInt() > 0)
            {
                string idSt = CurrentMachine.SelectedItem.Key;

                var p = new Dictionary<string, string>();
                p.Add("ID_ST", idSt);

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "TechnologicalMapExcel");
                q.Request.SetParam("Action", "ListProductCurrent");

                q.Request.SetParams(p);

                q.Request.Timeout = 5000;
                q.Request.Attempts= 1;

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");

                        if (ds != null)
                        {
                            if (ds.Items != null)
                            {
                                if (ds.Items.Count > 0)
                                {
                                    DictionaryByCurrentProduct = ds.Items.First();

                                    CurrentId2 = DictionaryByCurrentProduct.CheckGet("ID2").ToInt();
                                    CurrentProduct.Text = DictionaryByCurrentProduct.CheckGet("NAME");

                                    ExcelManager();
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                CurrentProduct.Clear();
                CurrentId2 = 0;
                DictionaryByCurrentProduct.Clear();
                ExcelManager();

                var msg = "Не выбран станок";
                ShowInformationWindow(msg, 2, false);
            }

            EnableControls();
        }

        /// <summary>
        /// Управление открытием и закрытием эксель файлов
        /// </summary>
        public void ExcelManager()
        {
            string pathTk = "";
            int pageTk = 0;
            bool resume = false;

            if (DictionaryByCurrentProduct != null)
            {
                if (DictionaryByCurrentProduct.Count > 0)
                {
                    pathTk = DictionaryByCurrentProduct.CheckGet("PATHTK");
                    pageTk = DictionaryByCurrentProduct.CheckGet("PAGETK").ToInt();

                    if (!string.IsNullOrEmpty(pathTk))
                    {
                        if (!(pageTk > 0))
                        {
                            pageTk = 1;
                        }

                        resume = true;
                    }
                }
            }

            if (resume)
            {
                // Если сейчас не открыт ни один эксель файл
                // т.е. первичное открытие, то открываем эксель файл
                if (string.IsNullOrEmpty(CurrentPathTk))
                {
                    OpenExcel();
                }
                // Если уже открыт эксель файл и он отличается от того, который нужно открыть
                // то закрывает открытый файл и открываем новый
                else if (pathTk != CurrentPathTk || pageTk != CurrentPageTk)
                {
                    CloseExcel();

                    OpenExcel();
                }
                // Если нужно открыть тот же файл, что сейчас открыт (ExcelApplication.ActiveWorkbook),
                // то проверяем, что файл открыт: если нет, то повторно открываем его
                // нужно для случая, когда эксель файл закрыли вручную
                else
                {
                    CheckOpenedExcel();
                }
            }
            else
            {
                CloseExcel();
            }
        }

        /// <summary>
        /// Открывает эксель файл в режиме Только чтение
        /// </summary>
        public void OpenExcel()
        {
            if (DictionaryByCurrentProduct != null)
            {
                if (DictionaryByCurrentProduct.Count > 0)
                {
                    // Перед открытием нового файла проверяем, что закрыт старый
                    // если нет, то закрываем его
                    if (ExcelApplication.ActiveWorkbook != null)
                    {
                        ExcelApplication.ActiveWorkbook.Close();
                    }

                    string pathTk = DictionaryByCurrentProduct.CheckGet("PATHTK");
                    int pageTk = DictionaryByCurrentProduct.CheckGet("PAGETK").ToInt();

                    if (!string.IsNullOrEmpty(pathTk))
                    {
                        if (System.IO.File.Exists(pathTk))
                        {
                            if (!(pageTk > 0))
                            {
                                pageTk = 1;
                            }

                            try
                            {
                                Microsoft.Office.Interop.Excel.Workbook excelWorkbook;
                                Microsoft.Office.Interop.Excel.Worksheet excelWorksheet;
                                ExcelApplication.Visible = true;

                                excelWorkbook = ExcelApplication.Workbooks.Open(pathTk, null, true);

                                // Проверяем, что такой лист существует
                                // если нет, то открываем первый лист
                                if (excelWorkbook.Sheets.Count >= pageTk)
                                {
                                    excelWorksheet = excelWorkbook.Sheets[pageTk];
                                }
                                else
                                {
                                    excelWorksheet = excelWorkbook.Sheets[1];
                                }
                                
                                excelWorksheet.Visible = Microsoft.Office.Interop.Excel.XlSheetVisibility.xlSheetVisible;
                                excelWorksheet.Activate();

                                CurrentPathTk = pathTk;
                                CurrentPageTk = pageTk;
                            }
                            catch (Exception ex)
                            {
                                ExeptionReporter(ex);

                                var msg = $"Ошибка открытия Excel файла тех карты. {Environment.NewLine}Пожалуйста сообщите об ошибке.";
                                ShowInformationWindow(msg);
                            }
                        }
                        else
                        {
                            var msg = "Excel файл тех карты не найден";
                            ShowInformationWindow(msg);
                        }
                    }
                    else
                    {
                        var msg = "Excel файл тех карты не найден";
                        ShowInformationWindow(msg);
                    }
                }
            }
        }

        /// <summary>
        /// Закрывает текущий открытый эксель файл (по ExcelApplication.ActiveWorkbook, предварительно проверив, что он открыт);
        /// Очищает CurrentPathTk -- текущий путь к открытому эксель файлу
        /// и CurrentPageTk -- текущий активный лист в эксель файле.
        /// </summary>
        public void CloseExcel()
        {
            {
                try
                {
                    if(ExcelApplication.ActiveWorkbook != null)
                    {
                        ExcelApplication.ActiveWorkbook.Close();
                        ExcelApplication.Visible = false;
                    }                    
                }
                catch (Exception ex)
                {
                    ExeptionReporter(ex);

                    var msg = $"Ошибка закрытия Excel файла тех карты. {Environment.NewLine}Пожалуйста сообщите об ошибке.";
                    ShowInformationWindow(msg);
                }
            }

            CurrentPathTk = "";
            CurrentPageTk = 0;
        }

        /// <summary>
        /// Проверяем текущий эксель файл.
        /// Если он закрыт (например вручную), но по текущему заданию он нужен,
        /// то открываем его.
        /// </summary>
        public void CheckOpenedExcel()
        {
            if (ExcelApplication.ActiveWorkbook == null)
            {
                try
                {
                    OpenExcel();
                }
                catch (Exception ex)
                {
                    ExeptionReporter(ex);

                    var msg = $"Ошибка открытия Excel файла тех карты. {Environment.NewLine}Пожалуйста сообщите об ошибке.";
                    ShowInformationWindow(msg);
                }
            }
        }

        /// <summary>
        /// Отображение всплывающего информационного окна, 
        /// которое автоматически закроется через указанное количество секунд
        /// </summary>
        /// <param name="msg">Текст, отображаемый в информационном окне</param>
        /// <param name="seconds">Количество секунд, через которое закроется окно</param>
        /// <param name="sendReport">Флаг того, что информация запишется в лог</param>
        public void ShowInformationWindow(string msg, int seconds = 2, bool sendReport = true)
        {
            // Показываем всплывающие окна только если эта вкладка открыта и отображается сейчас и программа ЛПак_ерп не свёрнута и не перекрыта другими программами.
            // Если открыта и отображается другая вкладка (this.IsVisible = false), то перестаём отправлять всплывающие сообщения до тех пор, пока снова не переключимся на эту
            // Если ЛПак_ерп свёрнуто или перекрыто поверх другим приложением (Central.MainWindow.IsActive = false), то перестаём отправлять всплывающие сообщения.
            if (this.IsVisible && Central.MainWindow.IsActive)
            {
                var d = new TechnologicalMapExcelInformationWindow($"{msg}");
                d.ShowAndAutoClose(seconds, sendReport);
            }
        }

        /// <summary>
        /// Отправляет отчёт об ошибках
        /// Предназначен для получения информации об ошибках в try_catch
        /// </summary>
        public void ExeptionReporter(Exception ex)
        {
            var q = new LPackClientQuery();
            q.SilentErrorProcess = true;

            var error = new Error();
            error.Code = 146;
            error.Message = ex.Message;

            try
            {
                var exDictionary = (Dictionary<object, object>)ex.Data;

                foreach (var item in exDictionary)
                {
                    string key = item.Key.ToString();
                    string value = item.Value.ToString();

                    error.Description += $"{Environment.NewLine}KEY={key}, VALUE={value};";
                }
            }
            catch (Exception exeption)
            {

            }

            Central.ProcError(error, "", true, q);
        }

        /// <summary>
        /// Запуск автообновления
        /// </summary>
        public void RunAutoUpdateTimer()
        {
            if (AutoUpdateInterval != 0)
            {
                if (AutoUpdateTimer == null)
                {
                    AutoUpdateTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0, 0, AutoUpdateInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", AutoUpdateInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("TechnologicalMapExcel_RunComplectationWarningTimer", row);
                    }

                    AutoUpdateTimer.Tick += (s, e) =>
                    {
                        LoadCurrentProduct();
                    };
                }

                if (AutoUpdateTimer.IsEnabled)
                {
                    AutoUpdateTimer.Stop();
                }
                AutoUpdateTimer.Start();
            }
        }

        /// <summary>
        /// Остановка таймера автообновления
        /// </summary>
        public void StopAutoUpdateTimer()
        {
            if (AutoUpdateTimer != null)
            {
                if (AutoUpdateTimer.IsEnabled)
                {
                    AutoUpdateTimer.Stop();
                }
            }
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            Central.WM.Close(FrameName);

            //вся работа по утилизации ресурсов происходит в Destroy
            //он будет вызван при закрытии фрейма
        }

        public void Destroy()
        {
            StopAutoUpdateTimer();

            CloseExcel();

            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "All",
                ReceiverName = "",
                SenderName = "TechnologicalMapExcel",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// Выключение контролов
        /// </summary>
        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
            MainFormFields.IsEnabled = false;
        }

        /// <summary>
        /// Включение контролов
        /// </summary>
        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
            MainFormFields.IsEnabled = true;
        }

        /// <summary>
        /// Открытие документации
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp-new/recycling/tk_pz");
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {

        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void ResreshButton_Click(object sender, RoutedEventArgs e)
        {
            SetDefaults();
        }

        private void RefreshProductButton_Click(object sender, RoutedEventArgs e)
        {
            LoadCurrentProduct();
        }

        private void OpenTechnologicalMapExcel_Click(object sender, RoutedEventArgs e)
        {
            ExcelManager();
        }
    }
}
