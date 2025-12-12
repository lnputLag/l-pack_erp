using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Production;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Client.Interfaces.Sales
{
    /// <summary>
    /// Окно сканирования штрихкода документов
    /// Для отметки возвратных документов или для поиска документов
    /// </summary>
    public partial class ScanDocument:ControlBase
    {
        public ScanDocument()
        {
            ControlTitle = "Сканирование документов";
            FrameName = "ScanDocument";
            InitializeComponent();

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnKeyPressed = (System.Windows.Input.KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }

                switch (e.Key)
                {
                    case Key.F1:
                        ShowHelp();
                        e.Handled = true;
                        break;

                    case Key.Down:
                    case Key.Enter:

                        // Если в данный момент не выполняется запрос, то можем обрабатывать новый введённый штрихкод
                        if (!QueryInProgress)
                        {
                            var code = Central.WM.GetScannerInput();
                            if (!string.IsNullOrEmpty(code) && code.Length >= DocumentBarcodeLenght)
                            {
                                Form.SetValueByPath("BARCODE", code);
                            }

                            if (!string.IsNullOrEmpty(Form.GetValueByPath("BARCODE")) && Form.GetValueByPath("BARCODE").Length >= DocumentBarcodeLenght)
                            {
                                ProcessDocumentBarcode();
                            }
                            else
                            {
                                Form.SetValueByPath("BARCODE", "");
                            }
                        }
                        // Если в данный момент выполняется запрос, то не даём обрабатывать новый введённый штрихкод
                        else
                        {
                            Form.SetValueByPath("BARCODE", "");
                        }

                        break;
                }
            };

            OnLoad = () =>
            {
                Messenger.Default.Register<ItemMessage>(this, _ProcessMessages);

                Init();
                SetDefaults();

                SearchText?.Focus();
            };

            OnUnload = () =>
            {
                Messenger.Default.Unregister<ItemMessage>(this);
            };

            OnFocusGot = () =>
            {
                SearchText?.Focus();
            };

            OnFocusLost = () =>
            {
            };
        }

        //FIXME 32
        /// <summary>
        /// 
        /// </summary>
        public static int DocumentBarcodeLenght = 30;

        private bool QueryInProgress { get; set; }

        /// <summary>
        /// Наименование таба, который вызвал этот таб
        /// </summary>
        public string ParentFrame { get; set; }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// инициализация компонентов формы
        /// </summary>
        public void Init()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="BARCODE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=SearchText,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
            };
            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            ReturnDocumentRadioButton.IsChecked = true;
            SearchDocumentRadioButton.IsChecked = false;

            ClearItems();
        }

        public void ClearItems()
        {
            if (Form != null)
            {
                Form.SetValueByPath("BARCODE", "");
            }
        }

        /// <summary>
        /// Обработчик ввода
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;

            switch (e.Key)
            {
                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

                case Key.Down:
                case Key.Enter:

                    // Если в данный момент не выполняется запрос, то можем обрабатывать новый введённый штрихкод
                    if (!QueryInProgress)
                    {
                        var code = Central.WM.GetScannerInput();
                        if (!string.IsNullOrEmpty(code) && code.Length >= DocumentBarcodeLenght)
                        {
                            Form.SetValueByPath("BARCODE", code);
                        }

                        if (!string.IsNullOrEmpty(Form.GetValueByPath("BARCODE")) && Form.GetValueByPath("BARCODE").Length >= DocumentBarcodeLenght)
                        {
                            ProcessDocumentBarcode();
                        }
                        else
                        {
                            Form.SetValueByPath("BARCODE", "");
                        }
                    }
                    // Если в данный момент выполняется запрос, то не даём обрабатывать новый введённый штрихкод
                    else
                    {
                        Form.SetValueByPath("BARCODE", "");
                    }

                    break;
            }
        }

        public void SetQueryInProgress(bool queryInProgress)
        {
            QueryInProgress = queryInProgress;
            SearchText.IsReadOnly = queryInProgress;
            FormToolbar.IsEnabled = !queryInProgress;
            Toolbar.IsEnabled = !queryInProgress;
        }

        public void ProcessDocumentBarcode()
        {
            SetQueryInProgress(true);

            string barcodeString = Form.GetValueByPath("BARCODE").Trim();
            if (!string.IsNullOrEmpty(barcodeString))
            {
                // Для старых документов УПД СОХ, где не было указания типа документа в штрихкоде
                {
                    if (barcodeString.Length == 30)
                    {
                        barcodeString = $"01{barcodeString}";
                    }
                }

                if (ReturnDocumentRadioButton.IsChecked == true)
                {
                    var p = new Dictionary<string, string>();
                    p.Add("BARCODE", barcodeString);

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sales");
                    q.Request.SetParam("Object", "Sale");
                    q.Request.SetParam("Action", "UpdateReturnDocumentFlagByBarcode");
                    q.Request.SetParams(p);
                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {
                        int barcodeProcessingResult = 0;
                        string barcodeProcessingResultMessage = "";
                        int barcodeProcessingResultType = -1;

                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            var dataSet = ListDataSet.Create(result, "ITEMS");
                            if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                            {
                                barcodeProcessingResult = dataSet.Items[0].CheckGet("RESULT").ToInt();
                                barcodeProcessingResultMessage = dataSet.Items[0].CheckGet("RESULT_MESSAGE");
                                barcodeProcessingResultType = dataSet.Items[0].CheckGet("RESULT_TYPE").ToInt();
                            }
                        }

                        if (barcodeProcessingResult > 0)
                        {
                            if (barcodeProcessingResultType == 0)
                            {
                                string msg = $"{barcodeProcessingResultMessage}";
                                int status = 2;
                                var d = new StackerScanedLableInfo(msg, status);
                                d.WindowMaxSizeFlag = true;
                                d.ShowAndAutoClose(1);
                            }
                            else
                            {
                                string msg = $"Внимание!{Environment.NewLine}{barcodeProcessingResultMessage}";
                                int status = 1;
                                var d = new StackerScanedLableInfo(msg, status);
                                d.WindowMaxSizeFlag = true;
                                d.ShowAndAutoClose(1);
                            }
                        }
                        else
                        {
                            string msg = $"При обработке штрихкода произошла ошибка. Пожалуйста, сообщите о проблеме.";
                            var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }

                    ClearItems();
                }
                else if (SearchDocumentRadioButton.IsChecked == true)
                {
                    var p = new Dictionary<string, string>();
                    p.Add("BARCODE", barcodeString);

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sales");
                    q.Request.SetParam("Object", "Sale");
                    q.Request.SetParam("Action", "SearchByBarcode");
                    q.Request.SetParams(p);
                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {
                        int invoiceId = 0;
                        Dictionary<string, string> contextObject = new Dictionary<string, string>();

                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            var dataSet = ListDataSet.Create(result, "ITEMS");
                            if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                            {
                                invoiceId = dataSet.Items[0].CheckGet("INVOICE_ID").ToInt();
                                contextObject = dataSet.Items[0];
                            }
                        }

                        if (invoiceId > 0)
                        {
                            // Отправляем сообщение вкладке "Список продаж" обновиться
                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup = "Preproduction",
                                ReceiverName = this.ParentFrame,
                                SenderName = this.FrameName,
                                Action = "Find",
                                Message = "",
                                ContextObject = contextObject,
                            }
                            );

                            // Отправляем сообщение
                            Central.Msg.SendMessage(new ItemMessage()
                            {
                                ReceiverGroup = "Sales",
                                ReceiverName = this.ParentFrame,
                                SenderName = this.FrameName,
                                Action = "Find",
                                Message = "",
                                ContextObject = contextObject,
                            }
                            );

                            this.Close();
                        }
                        else
                        {
                            string msg = $"При обработке штрихкода произошла ошибка. Пожалуйста, сообщите о проблеме.";
                            var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }

                    ClearItems();
                }
            }

            SetQueryInProgress(false);
        }

        /// <summary>
        /// отображение фрейма
        /// </summary>
        public void Show()
        {
            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 2;

            FrameName = $"{FrameName}";
            Dictionary<string, string> windowParametrs = new Dictionary<string, string>();
            windowParametrs.Add("no_resize", "1");
            windowParametrs.Add("center_screen", "1");
            this.MinHeight = 124;
            this.MinWidth = 280;
            Central.WM.Show(FrameName, this.ControlTitle, true, "main", this, "top", windowParametrs);
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            if (!string.IsNullOrEmpty(ParentFrame))
            {
                Central.WM.SetActive(ParentFrame, true);
            }

            Central.WM.Close(FrameName);

            //вся работа по утилизации ресурсов происходит в Destroy
            //он будет вызван при закрытии фрейма
        }

        /// <summary>
        /// отображение справочной статьи
        /// (относительный путь)
        /// </summary>
        public void ShowHelp()
        {
            //FIXME Документация 
            Central.ShowHelp("/doc/l-pack-erp/sales/sale_list/");
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void _ProcessMessages(ItemMessage m)
        {

        }


        private void ReturnDocumentRadioButton_Click(object sender, RoutedEventArgs e)
        {
            SearchDocumentRadioButton.IsChecked = false;

            SearchText?.Focus();
        }

        private void SearchDocumentRadioButton_Click(object sender, RoutedEventArgs e)
        {
            ReturnDocumentRadioButton.IsChecked = false;

            SearchText?.Focus();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();

            SearchText?.Focus();
        }

        private void SearchText_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Central.WM.ProcessKeyboard(e);
            this.OnKeyPressed?.Invoke(e);
        }
    }
}
