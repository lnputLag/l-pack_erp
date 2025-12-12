using Client.Common;
using Client.Interfaces.Main;
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
using Client.Interfaces.Service.Printing;
using GalaSoft.MvvmLight.Messaging;
using System.Threading;
using DevExpress.DocumentServices.ServiceModel.DataContracts;
using static Client.Interfaces.Sales.DocumentPrintManager;
using DevExpress.Export.Xl;
using Microsoft.Win32;
using System.IO;
using static Client.Common.LPackClientRequest;

namespace Client.Interfaces.Sales
{
    /// <summary>
    /// Менеджер печати документов
    /// </summary>
    public partial class DocumentPrintManager:ControlBase
    {
        /// <summary>
        /// Менеджер печати документов. Конструктор.
        /// </summary>
        public DocumentPrintManager()
        {
            ControlTitle = "Менеджер печати документов";
            InitializeComponent();

            ThreadSleepTime = 0;
            AsyncPrintFlag = true;
            HiddenInterface = false;

            if (Central.DebugMode)
            {
                DebugTools.Visibility = Visibility.Visible;
            }
            else
            {
                DebugTools.Visibility = Visibility.Collapsed;
            }

            OnMessage = (ItemMessage message) => {
                DebugLog($"message=[{message.Message}]");
            };

            //конструктор, будет вызван, когда объект создается
            //здесь создаются все внутренние структуры
            //впервые этот коллбэк будет вызван, когда данный таб станет активным
            //впервые (до этих пор, никакая работа внутри не происходит, что экономит ресурсы)
            OnLoad = () =>
            {
                ProcessPermissions();
                FormInit();
                SetDefaults();
                LoadInvoiceData();
                LoadDocumentList();
                //CheckInvoiceNumber(false);
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
            };
        }

        /// <summary>
        /// Таймаут для функции получения сгенерированного документа с сервера
        /// </summary>
        public static int GetDocumentTimeout = 45000;

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Идентификатор площадки
        /// </summary>
        public int FactoryId { get; set; }

        /// <summary>
        /// Идентификатор накладной расхода
        /// </summary>
        public int InvoiceId { get; set; }

        /// <summary>
        /// Номер накладной расхода
        /// </summary>
        public string InvoiceNumber { get; set; }

        /// <summary>
        /// Дата накладной расхода
        /// </summary>
        public string InvoiceDate { get; set; }

        /// <summary>
        /// Наименование покупателя
        /// </summary>
        public string BuyerName { get; set; }

        /// <summary>
        /// Идентификатор корректировки по накладной
        /// </summary>
        public int AdjustmentId { get; set; }

        /// <summary>
        /// Идентификатор сотрудника - подписанта
        /// </summary>
        public int SignotoryEmployeeId { get; set; }

        /// <summary>
        /// Полное имя сотрудника - подписанта
        /// </summary>
        public string SignotoryEmployeeName { get; set; }

        /// <summary>
        /// Флаг скрытого интерфейса. true -- если печатам документы не показывая пользователю интерфейс.
        /// </summary>
        public bool HiddenInterface { get; set; }

        /// <summary>
        /// Тип документа
        /// </summary>
        public enum BaseDocumentFormat
        {
            PDF,
            HTML,
            DOCX
        }

        /// <summary>
        /// Делегат для определения действий с полученными сгенерированными файлами документов
        /// </summary>
        /// <param name="document"></param>
        public delegate void DocumentPrintVoidDelegate(DocumentItem documentItem);
        /// <summary>
        /// Делегат для определения действий с полученными сгенерированными файлами документов
        /// </summary>
        public DocumentPrintVoidDelegate DocumentPrintVoid;

        /// <summary>
        /// Флаг получения докуменов в асинхронном режиме
        /// </summary>
        public bool AsyncPrintFlag { get; set; }

        /// <summary>
        /// Задержка между вызовами функции получения сгенерированных документов с сервера
        /// </summary>
        public int ThreadSleepTime { get; set; }

        /// <summary>
        /// Последовательность печати документов по умолчанию
        /// </summary>
        public Dictionary<string, int> DocumentPrintIndexQueueDefault { get; set; }

        public const string UniversalTransferDocumentFieldPath = "UNIVERSAL_TRANSFER_DOC";
        public const string WaybillDocumentFieldPath = "WAYBILL";
        public const string ConsignmentNoteDocumentFieldPath = "CONSIGNMENT_NOTE";
        public const string CmrDocumentFieldPath = "CMR";
        public const string ReceiptDocumentFieldPath = "RECEIPT";
        public const string QualityCertificateDocumentFieldPath = "QUALITY_CERTIFICATE";
        public const string QualityPassportDocumentFieldPath = "QUALITY_PASSPORT";
        public const string CertificateGostDocumentFieldPath = "CERTIFICATE_GOST";
        public const string UniversalAdjustmentDocumentFieldPath = "UNIVERSAL_ADJUSTMENT_DOC";
        public const string ClientWaybillDocumentFieldPath = "CLIENT_WAYBILL";
        public const string ClientConsignmentNoteDocumentFieldPath = "CLIENT_CONSIGNMENT_NOTE";
        public const string PackingListDocumentFieldPath = "PACKING_LIST";
        public const string InvoiceDocumentFieldPath = "INVOICE";
        public const string QualityCertificateOnPaperDocumentFieldPath = "QUALITY_CERTIFICATE_ON_PAPER";
        public const string SpecificationOnPaperDocumentFieldPath = "SPECIFICATION_ON_PAPER";
        public const string CertificateTuDocumentFieldPath = "CERTIFICATE_TU";
        public const string InnerUniversalTransferDocumentFieldPath = "INNER_UNIVERSAL_TRANSFER_DOC";
        public const string InnerWaybillDocumentFieldPath = "INNER_WAYBILL";
        public const string InnerConsignmentNoteDocumentFieldPath = "INNER_CONSIGNMENT_NOTE";
        public const string InnerCmrDocumentFieldPath = "INNER_CMR";
        public const string ActWeighingDocumentFieldPath = "ACT_WEIGHING";
        public const string AcceptanceAndTransferCertificateFieldPath = "ACCEPTANCE_AND_TRANSFER_DOC";

        private string LiteBaseReportTableName = "document_print_manager";

        private string LiteBaseReportPrimaryKey = "DOCUMENT_NAME~DTTM";

        #region Default

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel(this.RoleName);
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

        public void FormInit()
        {
            //инициализация формы
            {
                Form = new FormHelper();

                //колонки формы
                var fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path = "PAYMENT_DOCUMENT_NUMBER",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = DocumentNumberTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "PAYMENT_DOCUMENT_DATE",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Format = "dd.MM.yyyy",
                        Control = DocumentDateTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "PAYMENT_DOCUMENT_NUMBER2",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = DocumentNumber2TextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "PAYMENT_DOCUMENT_DATE2",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Format = "dd.MM.yyyy",
                        Control = DocumentDate2TextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },

                    new FormHelperField()
                    {
                        Path = "ZERO_PRICE",
                        FieldType = FormHelperField.FieldTypeRef.Boolean,
                        Control = ZeroPriceCheckBox,
                        ControlType = "CheckBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    


                    new FormHelperField()
                    {
                        Path = PackingListDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = PackingListTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = InvoiceDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = InvoiceTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = ReceiptDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = ReceiptTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = UniversalTransferDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = UniversalTransferDocTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = InnerUniversalTransferDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = InnerUniversalTransferDocTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = UniversalAdjustmentDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = UniversalAdjustmentDocTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },



                    new FormHelperField()
                    {
                        Path = WaybillDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = WaybillTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = InnerWaybillDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = InnerWaybillTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = ClientWaybillDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = ClientWaybillTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = ConsignmentNoteDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = ConsignmentNoteTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = InnerConsignmentNoteDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = InnerConsignmentNoteTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = ClientConsignmentNoteDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = ClientConsignmentNoteTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },



                    new FormHelperField()
                    {
                        Path = QualityCertificateDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = QualityCertificateTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = QualityCertificateOnPaperDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = QualityCertificateOnPaperTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = QualityPassportDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = QualityPassportTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = SpecificationOnPaperDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = SpecificationOnPaperTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = CertificateGostDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = CertificateGostTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = CertificateTuDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = CertificateTuTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = CmrDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = CmrTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = InnerCmrDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = InnerCmrTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },

                    new FormHelperField()
                    {
                        Path = ActWeighingDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = ActWeighingTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = AcceptanceAndTransferCertificateFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = AcceptanceAndTransferCertificateTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                };

                Form.SetFields(fields);
            }
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();

            if (SignotoryEmployeeId == 0 || string.IsNullOrEmpty(SignotoryEmployeeName))
            {
                SetSignotory();
            }

            if (AdjustmentId > 0)
            {
                UniversalAdjustmentDocTextBox.IsReadOnly = false;
            }
            else
            {
                UniversalAdjustmentDocTextBox.IsReadOnly = true;
            }

            DocumentPrintIndexQueueDefault = new Dictionary<string, int>();
            DocumentPrintIndexQueueDefault.Add(InnerUniversalTransferDocumentFieldPath, 1);
            DocumentPrintIndexQueueDefault.Add(InnerWaybillDocumentFieldPath, 2);
            DocumentPrintIndexQueueDefault.Add(InnerConsignmentNoteDocumentFieldPath, 3);
            DocumentPrintIndexQueueDefault.Add(InnerCmrDocumentFieldPath, 4);
            DocumentPrintIndexQueueDefault.Add(UniversalTransferDocumentFieldPath, 5);
            DocumentPrintIndexQueueDefault.Add(ReceiptDocumentFieldPath, 6);
            DocumentPrintIndexQueueDefault.Add(WaybillDocumentFieldPath, 7);
            DocumentPrintIndexQueueDefault.Add(ConsignmentNoteDocumentFieldPath, 8);
            DocumentPrintIndexQueueDefault.Add(ClientConsignmentNoteDocumentFieldPath, 9);
            DocumentPrintIndexQueueDefault.Add(ClientWaybillDocumentFieldPath, 10);
            DocumentPrintIndexQueueDefault.Add(QualityCertificateDocumentFieldPath, 11);
            DocumentPrintIndexQueueDefault.Add(CmrDocumentFieldPath, 12);
            DocumentPrintIndexQueueDefault.Add(CertificateGostDocumentFieldPath, 13);
            DocumentPrintIndexQueueDefault.Add(CertificateTuDocumentFieldPath, 14);
            DocumentPrintIndexQueueDefault.Add(QualityCertificateOnPaperDocumentFieldPath, 15);
            DocumentPrintIndexQueueDefault.Add(SpecificationOnPaperDocumentFieldPath, 16);
            DocumentPrintIndexQueueDefault.Add(QualityPassportDocumentFieldPath, 17);
            DocumentPrintIndexQueueDefault.Add(PackingListDocumentFieldPath, 18);
            DocumentPrintIndexQueueDefault.Add(InvoiceDocumentFieldPath, 19);
            DocumentPrintIndexQueueDefault.Add(UniversalAdjustmentDocumentFieldPath, 20);
            DocumentPrintIndexQueueDefault.Add(ActWeighingDocumentFieldPath, 21);
            DocumentPrintIndexQueueDefault.Add(AcceptanceAndTransferCertificateFieldPath, 22);
            DocumentPrintIndexQueueDefault = DocumentPrintIndexQueueDefault.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
        }

        /// <summary>
        /// Очистка всех полей с количеством копий документа
        /// </summary>
        public void Reset()
        {
            Form.SetValueByPath(UniversalTransferDocumentFieldPath, "");
            Form.SetValueByPath(WaybillDocumentFieldPath, "");
            Form.SetValueByPath(ConsignmentNoteDocumentFieldPath, "");
            Form.SetValueByPath(CmrDocumentFieldPath, "");
            Form.SetValueByPath(ReceiptDocumentFieldPath, "");
            Form.SetValueByPath(QualityCertificateDocumentFieldPath, "");
            Form.SetValueByPath(QualityPassportDocumentFieldPath, "");
            Form.SetValueByPath(CertificateGostDocumentFieldPath, "");
            Form.SetValueByPath(UniversalAdjustmentDocumentFieldPath, "");
            Form.SetValueByPath(ClientWaybillDocumentFieldPath, "");
            Form.SetValueByPath(ClientConsignmentNoteDocumentFieldPath, "");
            Form.SetValueByPath(PackingListDocumentFieldPath, "");
            Form.SetValueByPath(InvoiceDocumentFieldPath, "");
            Form.SetValueByPath(QualityCertificateOnPaperDocumentFieldPath, "");
            Form.SetValueByPath(SpecificationOnPaperDocumentFieldPath, "");
            Form.SetValueByPath(CertificateTuDocumentFieldPath, "");
            Form.SetValueByPath(InnerUniversalTransferDocumentFieldPath, "");
            Form.SetValueByPath(InnerWaybillDocumentFieldPath, "");
            Form.SetValueByPath(InnerConsignmentNoteDocumentFieldPath, "");
            Form.SetValueByPath(InnerCmrDocumentFieldPath, "");
            Form.SetValueByPath(ActWeighingDocumentFieldPath, "");
            Form.SetValueByPath(AcceptanceAndTransferCertificateFieldPath, "");
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
            var frameName = $"{ControlName}";
            Dictionary<string, string> windowParametrs = new Dictionary<string, string>();
            windowParametrs.Add("no_resize", "1");
            windowParametrs.Add("center_screen", "1");
            this.MinHeight = 450;
            this.MinWidth = 750;
            Central.WM.Show(frameName, $"Менеджер печати документов", true, "main", this, null, windowParametrs);
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            var frameName = $"{ControlName}";
            Central.WM.Close(frameName);
        }

        public void DisableControls()
        {
            DocumentCountGrid.IsEnabled = false;
            DocumentSettingsGrid.IsEnabled = false;
            FormToolbar.IsEnabled = false;
        }

        public void EnableControls()
        {
            DocumentCountGrid.IsEnabled = true;
            DocumentSettingsGrid.IsEnabled = true;
            FormToolbar.IsEnabled = true;
        }

        public void ProcessCommand(string command)
        {
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "help":
                    {
                        // FIXME сделать документацию
                        Central.ShowHelp("/doc/l-pack-erp/");
                    }
                    break;
                }
            }
        }

        public async void UploadFile()
        {
            var p = new Dictionary<string, string>();

            var fd = new OpenFileDialog();
            var fdResult = (bool)fd.ShowDialog();
            if (fdResult)
            {
                var fileName = Path.GetFileName(fd.FileName);
                var filePath = fd.FileName;

                p.CheckAdd("FILE_NAME", fileName);
                p.CheckAdd("FILE_PATH", filePath.ToString());

                p.CheckAdd("PRIMARY_KEY", "DOCUMENT_NAME");
                p.CheckAdd("PRIMARY_KEY_VALUE", fileName);
                p.CheckAdd("TABLE_NAME", "documents");
                p.CheckAdd("TABLE_DIRECTORY", "certificate");
                p.CheckAdd("STORAGE_TYPE", "3");
            }

            if (!string.IsNullOrEmpty(p.CheckGet("FILE_NAME")))
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Service");
                q.Request.SetParam("Object", "LiteBase");
                q.Request.SetParam("Action", "SaveFile");
                q.Request.SetParams(p);

                q.Request.Type = RequestTypeRef.MultipartForm;
                q.Request.UploadFilePath = p.CheckGet("FILE_PATH");

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    bool succesfullFlag = false;
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(ds.Items[0].CheckGet("ID")))
                            {
                                succesfullFlag = true;
                            }
                        }
                    }

                    if (succesfullFlag)
                    {
                        var msg = $"Успешное сохранение документа";
                        var d = new DialogWindow($"{msg}", "Менеджер печати документов", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                    else
                    {
                        var msg = $"Ошибка сохранения документа";
                        var d = new DialogWindow($"{msg}", "Менеджер печати документов", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// Установка настроек для принтера
        /// </summary>
        public void SetPrintSettings()
        {
            var i = new PrintingInterface();
            Close();
        }

        public void EnableSplash()
        {
            DisableControls();
            SplashControl.Message = $"Пожалуйста, подождите.{Environment.NewLine}Идёт печать документов.";
            SplashControl.Visible = true;
        }

        public void DisableSplash()
        {
            EnableControls();
            SplashControl.Message = "";
            SplashControl.Visible = false;
        }

        public string GetDocumentUserName(string fieldName)
        {
            string fileName = "";

            switch (fieldName)
            {
                case UniversalTransferDocumentFieldPath: 
                    fileName = "УПД";
                    break;
                case WaybillDocumentFieldPath: 
                    fileName = "ТН";
                    break;
                case ConsignmentNoteDocumentFieldPath: 
                    fileName = "ТТН";
                    break;
                case CmrDocumentFieldPath: 
                    fileName = "CMR";
                    break;
                case ReceiptDocumentFieldPath: 
                    fileName = "Счёт";
                    break;
                case QualityCertificateDocumentFieldPath: 
                    fileName = "УК";
                    break;
                case QualityPassportDocumentFieldPath: 
                    fileName = "Паспорт качества";
                    break;
                case CertificateGostDocumentFieldPath: 
                    fileName = "Сертификат ГОСТ";
                    break;
                case UniversalAdjustmentDocumentFieldPath: 
                    fileName = "УКД";
                    break;
                case ClientWaybillDocumentFieldPath: 
                    fileName = "ТН клиента";
                    break;
                case ClientConsignmentNoteDocumentFieldPath: 
                    fileName = "ТТН клиента";
                    break;
                case PackingListDocumentFieldPath: 
                    fileName = "ТОРГ12";
                    break;
                case InvoiceDocumentFieldPath: 
                    fileName = "Накладная";
                    break;
                case QualityCertificateOnPaperDocumentFieldPath: 
                    fileName = "УК на бумагу";
                    break;
                case SpecificationOnPaperDocumentFieldPath: 
                    fileName = "Спецификация на бумагу";
                    break;
                case CertificateTuDocumentFieldPath: 
                    fileName = "Сертификат ТУ";
                    break;
                case InnerUniversalTransferDocumentFieldPath: 
                    fileName = "УПД внутрунний";
                    break;
                case InnerWaybillDocumentFieldPath: 
                    fileName = "ТН внутренний";
                    break;
                case InnerConsignmentNoteDocumentFieldPath: 
                    fileName = "ТТН внутренний";
                    break;
                case InnerCmrDocumentFieldPath: 
                    fileName = "CMR внутренний";
                    break;
                case ActWeighingDocumentFieldPath:
                    fileName = "Акт взвешивания";
                    break;
                case AcceptanceAndTransferCertificateFieldPath:
                    fileName = "Акт приёма-передачи";
                    break;

                default:
                    break;
            }

            return fileName;
        }

        public void LoadInvoiceData()
        {
            var p = new Dictionary<string, string>();
            p.Add("INVOICE_ID", InvoiceId.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "Sale");
            q.Request.SetParam("Action", "Get");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            bool succesfullFlag = false;

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        InvoiceNumber = ds.Items[0].CheckGet("INVOICE");
                        InvoiceDate = ds.Items[0].CheckGet("INVOICE_DATE");
                        BuyerName = ds.Items[0].CheckGet("BUYER_NAME_SIMPLE");
                        FactoryId = ds.Items[0].CheckGet("FACTORY_ID").ToInt();

                        succesfullFlag = true;
                    }
                }
            }
            else
            {
                q.SilentErrorProcess = true;
                q.ProcessError();
            }


            if (!succesfullFlag)
            {
                if (q.Answer.Error.Code == 7)
                {
                    var msg = $"Произошла ошибка при получении данных по накладной.{Environment.NewLine}Пожалуйста, закройте и повторно откройте окно печати документов.";
                    var d = new DialogWindow($"{msg}", "Менеджер печати документов", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
                else
                {
                    var msg = "Произошла ошибка при получении данных по накладной. Пожалуйста, сообщите о проблеме.";
                    var d = new DialogWindow($"{msg}", "Менеджер печати документов", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
        }

        #endregion

        #region Printing

        /// <summary>
        /// Установка подписанта документов
        /// </summary>
        /// <param name="signotoryEmployeeId"></param>
        /// <param name="signotoryEmployeeName"></param>
        public void SetSignotory(int signotoryEmployeeId = 0, string signotoryEmployeeName = "")
        {
            if (signotoryEmployeeId == 0 || string.IsNullOrEmpty(signotoryEmployeeName))
            {
                SignotoryEmployeeId = Central.User.EmployeeId;
                SignotoryEmployeeName = Central.User.Name;
                SignotoryLabel.Content = $"Подписант: {SignotoryEmployeeName}";
            }
            else
            {
                SignotoryEmployeeId = signotoryEmployeeId;
                SignotoryEmployeeName = signotoryEmployeeName;
                SignotoryLabel.Content = $"Подписант: {SignotoryEmployeeName}";
            }
        }

        /// <summary>
        /// Получаем список документов, которые нужно печатать по этой накладной
        /// </summary>
        public void LoadDocumentList()
        {
            var p = new Dictionary<string, string>();
            p.Add("INVOICE_ID", InvoiceId.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "Sale");
            q.Request.SetParam("Action", "ListDocument");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    Form.SetValues(ds);
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Получение наименования формата документа
        /// </summary>
        /// <param name="documentFormat"></param>
        /// <returns></returns>
        public static string GetDocumentFormatName(BaseDocumentFormat documentFormat)
        {
            string documentFormatName = "";

            switch (documentFormat)
            {
                case BaseDocumentFormat.PDF:
                    documentFormatName = "pdf";
                    break;

                case BaseDocumentFormat.DOCX:
                    documentFormatName = "docx";
                    break;

                case BaseDocumentFormat.HTML:
                    documentFormatName = "html";
                    break;

                default:
                    documentFormatName = "html";
                    break;
            }

            return documentFormatName;
        }

        /// <summary>
        /// Проверяем, заполнен ли номер счёт-фактуры. 
        /// Если нет, то  заполняем его.
        /// </summary>
        public bool CheckInvoiceNumber(bool createIfNotExist = true)
        {
            bool succesfullFlag = false;

            DisableControls();

            if (!string.IsNullOrEmpty(InvoiceNumber))
            {
                succesfullFlag = true;
            }
            else
            {
                var p = new Dictionary<string, string>();
                p.Add("INVOICE_ID", InvoiceId.ToString());
                p.Add("CREATE_FLAG", createIfNotExist.ToInt().ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "Sale");
                q.Request.SetParam("Action", "CheckInvoiceNumber");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(ds.Items.First().CheckGet("INVOICE_NUMBER")))
                            {
                                succesfullFlag = true;
                                InvoiceNumber = ds.Items[0].CheckGet("INVOICE_NUMBER");
                                InvoiceDate = ds.Items[0].CheckGet("INVOICE_DATE");

                                if (createIfNotExist)
                                {
                                    // Отправляем сообщение вкладке "Список продаж" обновиться
                                    Messenger.Default.Send(new ItemMessage()
                                    {
                                        ReceiverGroup = "Preproduction",
                                        ReceiverName = "SaleList",
                                        SenderName = "DocumentPrintManager",
                                        Action = "Refresh",
                                        Message = "",
                                    }
                                    );

                                    // Отправляем сообщение вкладке "Позиции накладной расхода" обновиться
                                    Messenger.Default.Send(new ItemMessage()
                                    {
                                        ReceiverGroup = "Preproduction",
                                        ReceiverName = "ConsumptionList",
                                        SenderName = "DocumentPrintManager",
                                        Action = "RefreshAll",
                                        Message = "",
                                    }
                                    );
                                }
                            }
                            else if (!createIfNotExist)
                            {
                                succesfullFlag = true;
                            }
                        }
                    }
                }
                else
                {
                    q.SilentErrorProcess = true;
                    q.ProcessError();
                }

                if (!succesfullFlag)
                {
                    if (q.Answer.Error.Code == 7)
                    {
                        var msg = $"Произошла ошибка при получении номера счёт-фактуры.{Environment.NewLine}Пожалуйста, выполните повторную печать документов.";
                        var d = new DialogWindow($"{msg}", "Менеджер печати документов", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                    else
                    {
                        var msg = "Произошла ошибка при получении номера счёт-фактуры. Пожалуйста, сообщите о проблеме.";
                        var d = new DialogWindow($"{msg}", "Менеджер печати документов", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
            }

            EnableControls();

            return succesfullFlag;
        }

        /// <summary>
        /// Получаем формат указанного документа для выгрузки в веб
        /// </summary>
        /// <param name="documentType"></param>
        /// <returns></returns>
        public int GetDocumentWebFormat(string documentType)
        {
            int documentWebFormat = 0;

            DisableControls();

            var p = new Dictionary<string, string>();
            p.Add("INVOICE_ID", InvoiceId.ToString());
            p.Add("DOCUMENT_TYPE", documentType);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "Sale");
            q.Request.SetParam("Action", "GetDocumentWebFormat");
            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                bool succesfullFlag = false;

                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        if (!string.IsNullOrEmpty(ds.Items[0].CheckGet("DOCUMENT_WEB_FORMAT")))
                        {
                            succesfullFlag = true;
                            documentWebFormat = ds.Items[0].CheckGet("DOCUMENT_WEB_FORMAT").ToInt();
                        }
                    }
                }

                if (!succesfullFlag)
                {
                    var msg = "Произошла ошибка проверки формата документа для выгрузки в веб. Пожалуйста, сообщите о проблеме.";
                    var d = new DialogWindow($"{msg}", "Менеджер печати документов", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                q.ProcessError();
            }

            EnableControls();

            return documentWebFormat;
        }

        /// <summary>
        /// Проверяем, заполнен ли номер счёта. 
        /// Если нет, то  заполняем его.
        /// </summary>
        public void CheckReceiptNumber()
        {
            DisableControls();

            int documentWebFormat = GetDocumentWebFormat("RECEIPT");

            if (Form.GetValueByPath("RECEIPT").ToInt() > 0 || documentWebFormat > 0)
            {
                var p = new Dictionary<string, string>();
                p.Add("INVOICE_ID", InvoiceId.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "Sale");
                q.Request.SetParam("Action", "CheckReceiptNumber");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    bool succesfullFlag = false;

                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(ds.Items.First().CheckGet("RECEIPT_NUMBER")))
                            {
                                succesfullFlag = true;

                                // Отправляем сообщение вкладке "Список продаж" обновиться
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup = "Preproduction",
                                    ReceiverName = "SaleList",
                                    SenderName = "DocumentPrintManager",
                                    Action = "Refresh",
                                    Message = "",
                                }
                                );
                            }
                        }
                    }

                    if (!succesfullFlag)
                    {
                        var msg = "Произошла ошибка при получении номера счёта. Пожалуйста, сообщите о проблеме.";
                        var d = new DialogWindow($"{msg}", "Менеджер печати документов", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            EnableControls();
        }

        /// <summary>
        /// Обновляем данные по оплате покупателем
        /// </summary>
        public void UpdatePayDebtBuyer()
        {
            DisableControls();

            if (InvoiceId > 0)
            {
                var p = new Dictionary<string, string>();
                p.Add("INVOICE_ID", InvoiceId.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "Sale");
                q.Request.SetParam("Action", "UpdatePayDebtPokupatel");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    bool succesFullFlag = false;

                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                        {
                            if (ds.Items.First().CheckGet("INVOICE_ID").ToInt() > 0)
                            {
                                succesFullFlag = true;
                            }
                        }
                    }

                    if (!succesFullFlag)
                    {
                        var msg = "Ошибка корректировки данных по оплате покупателем. Пожалуйста, сообщите о проблеме.";
                        var d = new DialogWindow($"{msg}", "Редактирование документа", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            EnableControls();
        }

        /// <summary>
        /// Обновляем дату накладной на текущую календарную дату, если документ ещё не печатался
        /// </summary>
        public void UpdateInvoiceDate()
        {
            DisableControls();

            if (InvoiceId > 0)
            {
                var p = new Dictionary<string, string>();
                p.Add("INVOICE_ID", InvoiceId.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "Sale");
                q.Request.SetParam("Action", "UpdateInvoiceDate");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    bool succesFullFlag = false;

                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                        {
                            if (ds.Items.First().CheckGet("INVOICE_ID").ToInt() > 0)
                            {
                                succesFullFlag = true;

                                // Отправляем сообщение вкладке "Позиции накладной расхода" обновиться
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup = "Preproduction",
                                    ReceiverName = "ConsumptionList",
                                    SenderName = "DocumentPrintManager",
                                    Action = "RefreshAll",
                                    Message = "",
                                }
                                );
                            }
                        }
                    }

                    if (!succesFullFlag)
                    {
                        var msg = "Ошибка обновления даты накладной. Пожалуйста, сообщите о проблеме.";
                        var d = new DialogWindow($"{msg}", "Редактирование документа", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            EnableControls();
        }

        /// <summary>
        /// Обновление даты печати документов по накладной
        /// </summary>
        public async void UpdatePrintingDateTime()
        {
            var p = new Dictionary<string, string>();
            p.Add("INVOICE_ID", InvoiceId.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "Sale");
            q.Request.SetParam("Action", "UpdatePrintingDateTime");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                bool succesfullFlag = false;

                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        if (!string.IsNullOrEmpty(ds.Items.First().CheckGet("INVOICE_ID")))
                        {
                            succesfullFlag = true;

                            // Отправляем сообщение вкладке "Список продаж" обновиться
                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup = "Preproduction",
                                ReceiverName = "SaleList",
                                SenderName = "DocumentPrintManager",
                                Action = "Refresh",
                                Message = "",
                            }
                            );

                            // Отправляем сообщение вкладке "Список продаж" обновиться
                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup = "ShipmentControl",
                                ReceiverName = "ShipmentsList",
                                SenderName = "DocumentPrintManager",
                                Action = "Refresh",
                                Message = "",
                            }
                            );
                        }
                    }
                }

                if (!succesfullFlag)
                {
                    var msg = "Произошла ошибка при обновлении даты печати документов. Пожалуйста, сообщите о проблеме.";
                    var d = new DialogWindow($"{msg}", "Менеджер печати документов", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                if (q.Answer.Error.Code == 7)
                {
                    q.SilentErrorProcess = true;
                }
                q.ProcessError();
            }
        }

        /// <summary>
        /// Проверка необходимости использовать печать клиента. 
        /// Если да, то выведет соответствующее сообщение пользователю.
        /// </summary>
        public void CheckClientStamp()
        {
            // Если печатаем ТТН клиента
            if (Form.GetValueByPath(ClientConsignmentNoteDocumentFieldPath).ToInt() > 0)
            {
                var p = new Dictionary<string, string>();
                p.Add("INVOICE_ID", InvoiceId.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "Sale");
                q.Request.SetParam("Action", "CheckClientStamp");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    bool succesfullFlag = false;

                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(ds.Items.First().CheckGet("CLIENT_STAMP_FLAG")))
                            {
                                succesfullFlag = true;

                                if (ds.Items.First().CheckGet("CLIENT_STAMP_FLAG").ToInt() > 0)
                                {
                                    var msg = "Внимание! Нужна печать клиента.";
                                    var d = new DialogWindow($"{msg}", "Менеджер печати документов", "", DialogWindowButtons.OK);
                                    d.ShowDialog();
                                }
                            }
                        }
                    }

                    if (!succesfullFlag)
                    {
                        var msg = "Произошла ошибка при проверке необходимости проставления печати клиента. Пожалуйста, сообщите о проблеме.";
                        var d = new DialogWindow($"{msg}", "Менеджер печати документов", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// Асинхронная функция запускает получение всех выбранных документов и блокирует интерфейс на время работы.
        /// После получение документа вызывает DocumentPrintVoid.
        /// Получение документов происходит согласно очереди печати по умолчанию.
        /// </summary>
        /// <param name="documentFormatName"></param>
        public async void GetAsyncDocumentList(string documentFormatName = "html")
        {
            if (!HiddenInterface)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    EnableSplash();
                });
            }

            // Данные формы
            Dictionary<string, string> formValues = Form.GetValues();
            // Список документов на печать
            Dictionary<string, string> formDocumentValues = formValues.Where(x => x.Value.ToInt() > 0).ToDictionary(x => x.Key, x => x.Value);
            formDocumentValues.Remove("PAYMENT_DOCUMENT_NUMBER");
            formDocumentValues.Remove("PAYMENT_DOCUMENT_DATE");
            formDocumentValues.Remove("PAYMENT_DOCUMENT_NUMBER2");
            formDocumentValues.Remove("PAYMENT_DOCUMENT_DATE2");
            formDocumentValues.Remove("ZERO_PRICE");

            // Синхронно, чтобы не отпускать поток, пока не закончится работа с документам
            if (HiddenInterface)
            {
                // Сортируем список необходимых документов в соответствии с очередью
                formDocumentValues = GetDocumentQueueCurrent(formDocumentValues);
                List<Task<DocumentItem>> taskQueue = new List<Task<DocumentItem>>();

                // Асинхронное получение документов
                if (AsyncPrintFlag)
                {
                    foreach (var item in formDocumentValues)
                    {
                        taskQueue.Add(GetDocumentTask(item, formValues, documentFormatName));
                        Thread.Sleep(ThreadSleepTime);
                    }

                    foreach (var task in taskQueue)
                    {
                        DocumentItem documentItem = await task;
                        DocumentPrintVoid?.Invoke(documentItem);
                    }
                }
                // Синхронное получение документов
                else
                {
                    foreach (var item in formDocumentValues)
                    {
                        var task = GetDocumentTask(item, formValues, documentFormatName);
                        DocumentItem documentItem = await task;
                        DocumentPrintVoid?.Invoke(documentItem);
                    }
                }
            }
            // Асинхронно, чтобы работал сплеш на экране
            else
            {
                // Асинхронно, чтобы работал сплеш на экране
                await Task.Run(async () =>
                {
                    // Сортируем список необходимых документов в соответствии с очередью
                    formDocumentValues = GetDocumentQueueCurrent(formDocumentValues);
                    List<Task<DocumentItem>> taskQueue = new List<Task<DocumentItem>>();

                    // Асинхронное получение документов
                    if (AsyncPrintFlag)
                    {
                        foreach (var item in formDocumentValues)
                        {
                            taskQueue.Add(GetDocumentTask(item, formValues, documentFormatName));
                            Thread.Sleep(ThreadSleepTime);
                        }

                        foreach (var task in taskQueue)
                        {
                            DocumentItem documentItem = await task;
                            DocumentPrintVoid?.Invoke(documentItem);
                        }
                    }
                    // Синхронное получение документов
                    else
                    {
                        foreach (var item in formDocumentValues)
                        {
                            var task = GetDocumentTask(item, formValues, documentFormatName);
                            DocumentItem documentItem = await task;
                            DocumentPrintVoid?.Invoke(documentItem);
                        }
                    }
                });
            }

            if (!HiddenInterface)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DisableSplash();
                });
            }
        }

        /// <summary>
        /// Сортирует текущий список документов в соответствии с очередью печати документов по умолчанию
        /// </summary>
        /// <param name="formDocumentValues"></param>
        /// <returns></returns>
        public Dictionary<string, string> GetDocumentQueueCurrent(Dictionary<string, string> formDocumentValues)
        {
            Dictionary<string, string> documentPrintQueueCurrent = new Dictionary<string, string>();

            foreach (var item in DocumentPrintIndexQueueDefault)
            {
                if (formDocumentValues.ContainsKey(item.Key))
                {
                    documentPrintQueueCurrent.Add(item.Key, formDocumentValues[item.Key]);
                }
            }

            return documentPrintQueueCurrent;
        }

        /// <summary>
        /// Запускает отправку запроса на сервер на получение конкретного документа и позвращает таск, которые по выполнении вернёт DocumentItem
        /// </summary>
        /// <param name="formField"></param>
        /// <param name="formValues"></param>
        /// <param name="documentFormatName"></param>
        /// <returns></returns>
        public Task<DocumentItem> GetDocumentTask(KeyValuePair<string, string> formField, Dictionary<string, string> formValues, string documentFormatName = "html")
        {
            return Task.Run(() =>
            {
                int requestAttempts = 2;
                int requestTimeout = GetDocumentTimeout;

                int documentCopyCount = 0;
                var p = new Dictionary<string, string>();
                p.Add("FACTORY_ID", $"{FactoryId}");
                var q = new LPackClientQuery();

                switch (formField.Key)
                {
                    case InvoiceDocumentFieldPath:

                        if (formField.Value.ToInt() > 0)
                        {
                            p.Add("INVOICE_ID", InvoiceId.ToString());
                            p.Add("DOCUMENT_FORMAT_NAME", documentFormatName);
                            p.Add("SIGNOTORY_EMPLOYEE_ID", $"{SignotoryEmployeeId}");

                            p.Add("PAYMENT_DOCUMENT_NUMBER", formValues.CheckGet("PAYMENT_DOCUMENT_NUMBER"));
                            p.Add("PAYMENT_DOCUMENT_DATE", formValues.CheckGet("PAYMENT_DOCUMENT_DATE"));
                            p.Add("PAYMENT_DOCUMENT_NUMBER2", formValues.CheckGet("PAYMENT_DOCUMENT_NUMBER2"));
                            p.Add("PAYMENT_DOCUMENT_DATE2", formValues.CheckGet("PAYMENT_DOCUMENT_DATE2"));


                            q.Request.SetParam("Module", "Sales");
                            q.Request.SetParam("Object", "Sale");
                            q.Request.SetParam("Action", "GetInvoiceDocument");

                            documentCopyCount = formField.Value.ToInt();
                        }

                        break;

                    case ReceiptDocumentFieldPath:

                        if (formField.Value.ToInt() > 0)
                        {
                            p.Add("INVOICE_ID", InvoiceId.ToString());
                            p.Add("DOCUMENT_FORMAT_NAME", documentFormatName);
                            p.Add("SIGNOTORY_EMPLOYEE_ID", $"{SignotoryEmployeeId}");

                            q.Request.SetParam("Module", "Sales");
                            q.Request.SetParam("Object", "Sale");
                            q.Request.SetParam("Action", "GetReceiptDocument");

                            documentCopyCount = formField.Value.ToInt();
                        }

                        break;

                    case PackingListDocumentFieldPath:

                        if (formField.Value.ToInt() > 0)
                        {
                            p.Add("INVOICE_ID", InvoiceId.ToString());
                            p.Add("DOCUMENT_FORMAT_NAME", documentFormatName);
                            p.Add("SIGNOTORY_EMPLOYEE_ID", $"{SignotoryEmployeeId}");


                            q.Request.SetParam("Module", "Sales");
                            q.Request.SetParam("Object", "Sale");
                            q.Request.SetParam("Action", "GetPackingListDocument");

                            documentCopyCount = formField.Value.ToInt();
                        }

                        break;

                    case UniversalTransferDocumentFieldPath:

                        if (formField.Value.ToInt() > 0)
                        {
                            p.Add("INVOICE_ID", InvoiceId.ToString());
                            p.Add("DOCUMENT_FORMAT_NAME", documentFormatName);
                            p.Add("SIGNOTORY_EMPLOYEE_ID", $"{SignotoryEmployeeId}");

                            p.Add("PAYMENT_DOCUMENT_NUMBER", formValues.CheckGet("PAYMENT_DOCUMENT_NUMBER"));
                            p.Add("PAYMENT_DOCUMENT_DATE", formValues.CheckGet("PAYMENT_DOCUMENT_DATE"));
                            p.Add("PAYMENT_DOCUMENT_NUMBER2", formValues.CheckGet("PAYMENT_DOCUMENT_NUMBER2"));
                            p.Add("PAYMENT_DOCUMENT_DATE2", formValues.CheckGet("PAYMENT_DOCUMENT_DATE2"));

                            p.Add("INNER_DOCUMENT_FLAG", "0");

                            q.Request.SetParam("Module", "Sales");
                            q.Request.SetParam("Object", "Sale");
                            q.Request.SetParam("Action", "GetUniversalTransferDocument");

                            documentCopyCount = formField.Value.ToInt();
                        }

                        break;

                    case InnerUniversalTransferDocumentFieldPath:

                        if (formField.Value.ToInt() > 0)
                        {
                            p.Add("INVOICE_ID", InvoiceId.ToString());
                            p.Add("DOCUMENT_FORMAT_NAME", documentFormatName);
                            p.Add("SIGNOTORY_EMPLOYEE_ID", $"{SignotoryEmployeeId}");

                            p.Add("PAYMENT_DOCUMENT_NUMBER", formValues.CheckGet("PAYMENT_DOCUMENT_NUMBER"));
                            p.Add("PAYMENT_DOCUMENT_DATE", formValues.CheckGet("PAYMENT_DOCUMENT_DATE"));
                            p.Add("PAYMENT_DOCUMENT_NUMBER2", formValues.CheckGet("PAYMENT_DOCUMENT_NUMBER2"));
                            p.Add("PAYMENT_DOCUMENT_DATE2", formValues.CheckGet("PAYMENT_DOCUMENT_DATE2"));

                            p.Add("INNER_DOCUMENT_FLAG", "1");

                            q.Request.SetParam("Module", "Sales");
                            q.Request.SetParam("Object", "Sale");
                            q.Request.SetParam("Action", "GetUniversalTransferDocument");

                            documentCopyCount = formField.Value.ToInt();
                        }

                        break;

                    case UniversalAdjustmentDocumentFieldPath:

                        if (formField.Value.ToInt() > 0)
                        {
                            p.Add("INVOICE_ID", InvoiceId.ToString());
                            p.Add("DOCUMENT_FORMAT_NAME", documentFormatName);
                            p.Add("SIGNOTORY_EMPLOYEE_ID", $"{SignotoryEmployeeId}");

                            p.Add("ADJUSTMENT_ID", AdjustmentId.ToString());


                            q.Request.SetParam("Module", "Sales");
                            q.Request.SetParam("Object", "Sale");
                            q.Request.SetParam("Action", "GetUniversalAdjustmentDocument");

                            documentCopyCount = formField.Value.ToInt();
                        }

                        break;

                    case QualityCertificateDocumentFieldPath:

                        if (formField.Value.ToInt() > 0)
                        {
                            p.Add("INVOICE_ID", InvoiceId.ToString());
                            p.Add("DOCUMENT_FORMAT_NAME", documentFormatName);
                            p.Add("SIGNOTORY_EMPLOYEE_ID", $"{SignotoryEmployeeId}");

                            q.Request.SetParam("Module", "Sales");
                            q.Request.SetParam("Object", "Sale");
                            q.Request.SetParam("Action", "GetQualityCertificateDocument");

                            documentCopyCount = formField.Value.ToInt();
                        }

                        break;

                    case QualityPassportDocumentFieldPath:

                        if (formField.Value.ToInt() > 0)
                        {
                            p.Add("INVOICE_ID", InvoiceId.ToString());
                            p.Add("DOCUMENT_FORMAT_NAME", documentFormatName);
                            p.Add("SIGNOTORY_EMPLOYEE_ID", $"{SignotoryEmployeeId}");


                            q.Request.SetParam("Module", "Sales");
                            q.Request.SetParam("Object", "Sale");
                            q.Request.SetParam("Action", "GetQualityPassportDocument");

                            documentCopyCount = formField.Value.ToInt();
                        }

                        break;

                    case QualityCertificateOnPaperDocumentFieldPath:

                        if (formField.Value.ToInt() > 0)
                        {
                            p.Add("INVOICE_ID", InvoiceId.ToString());
                            p.Add("DOCUMENT_FORMAT_NAME", documentFormatName);
                            p.Add("SIGNOTORY_EMPLOYEE_ID", $"{SignotoryEmployeeId}");


                            q.Request.SetParam("Module", "Sales");
                            q.Request.SetParam("Object", "Sale");
                            q.Request.SetParam("Action", "GetPaperQualityCertificateDocument");

                            documentCopyCount = formField.Value.ToInt();
                        }

                        break;

                    case SpecificationOnPaperDocumentFieldPath:

                        if (formField.Value.ToInt() > 0)
                        {
                            p.Add("INVOICE_ID", InvoiceId.ToString());
                            p.Add("DOCUMENT_FORMAT_NAME", documentFormatName);
                            p.Add("SIGNOTORY_EMPLOYEE_ID", $"{SignotoryEmployeeId}");

                            q.Request.SetParam("Module", "Sales");
                            q.Request.SetParam("Object", "Sale");
                            q.Request.SetParam("Action", "GetPaperSpecificationDocument");

                            documentCopyCount = formField.Value.ToInt();
                        }

                        break;

                    case CertificateGostDocumentFieldPath:

                        if (formField.Value.ToInt() > 0)
                        {

                            q.Request.SetParam("Module", "Sales");
                            q.Request.SetParam("Object", "Sale");
                            q.Request.SetParam("Action", "GetCertificateGostDocument");

                            documentCopyCount = formField.Value.ToInt();
                        }

                        break;

                    case CertificateTuDocumentFieldPath:

                        if (formField.Value.ToInt() > 0)
                        {
                            p.Add("INVOICE_ID", InvoiceId.ToString());

                            q.Request.SetParam("Module", "Sales");
                            q.Request.SetParam("Object", "Sale");
                            q.Request.SetParam("Action", "GetCertificateTuDocument");

                            documentCopyCount = formField.Value.ToInt();
                        }

                        break;

                    case CmrDocumentFieldPath:

                        if (formField.Value.ToInt() > 0)
                        {
                            p.Add("INVOICE_ID", InvoiceId.ToString());
                            p.Add("DOCUMENT_FORMAT_NAME", documentFormatName);
                            p.Add("SIGNOTORY_EMPLOYEE_ID", $"{SignotoryEmployeeId}");
                            p.Add("INNER_DOCUMENT_FLAG", "0");

                            q.Request.SetParam("Module", "Sales");
                            q.Request.SetParam("Object", "Sale");
                            q.Request.SetParam("Action", "GetCmrDocument");

                            documentCopyCount = formField.Value.ToInt();
                        }

                        break;

                    case InnerCmrDocumentFieldPath:

                        if (formField.Value.ToInt() > 0)
                        {
                            p.Add("INVOICE_ID", InvoiceId.ToString());
                            p.Add("DOCUMENT_FORMAT_NAME", documentFormatName);
                            p.Add("SIGNOTORY_EMPLOYEE_ID", $"{SignotoryEmployeeId}");
                            p.Add("INNER_DOCUMENT_FLAG", "1");


                            q.Request.SetParam("Module", "Sales");
                            q.Request.SetParam("Object", "Sale");
                            q.Request.SetParam("Action", "GetCmrDocument");

                            documentCopyCount = formField.Value.ToInt();
                        }

                        break;

                    case ConsignmentNoteDocumentFieldPath:

                        if (formField.Value.ToInt() > 0)
                        {
                            p.Add("INVOICE_ID", InvoiceId.ToString());
                            p.Add("DOCUMENT_FORMAT_NAME", documentFormatName);
                            p.Add("SIGNOTORY_EMPLOYEE_ID", $"{SignotoryEmployeeId}");
                            p.Add("ZERO_PRICE_FLAG", formValues.CheckGet("ZERO_PRICE").ToBool().ToString());
                            p.Add("INNER_DOCUMENT_FLAG", "0");

                            q.Request.SetParam("Module", "Sales");
                            q.Request.SetParam("Object", "Sale");
                            q.Request.SetParam("Action", "GetConsignmentNoteDocument");

                            documentCopyCount = formField.Value.ToInt();
                        }

                        break;

                    case InnerConsignmentNoteDocumentFieldPath:

                        if (formField.Value.ToInt() > 0)
                        {
                            p.Add("INVOICE_ID", InvoiceId.ToString());
                            p.Add("DOCUMENT_FORMAT_NAME", documentFormatName);
                            p.Add("SIGNOTORY_EMPLOYEE_ID", $"{SignotoryEmployeeId}");
                            p.Add("ZERO_PRICE_FLAG", formValues.CheckGet("ZERO_PRICE").ToBool().ToString());
                            p.Add("INNER_DOCUMENT_FLAG", "1");


                            q.Request.SetParam("Module", "Sales");
                            q.Request.SetParam("Object", "Sale");
                            q.Request.SetParam("Action", "GetConsignmentNoteDocument");

                            documentCopyCount = formField.Value.ToInt();
                        }

                        break;

                    case ClientConsignmentNoteDocumentFieldPath:

                        if (formField.Value.ToInt() > 0)
                        {
                            p.Add("INVOICE_ID", InvoiceId.ToString());
                            p.Add("DOCUMENT_FORMAT_NAME", documentFormatName);
                            p.Add("SIGNOTORY_EMPLOYEE_ID", $"{SignotoryEmployeeId}");
                            p.Add("ZERO_PRICE_FLAG", formValues.CheckGet("ZERO_PRICE").ToBool().ToString());


                            q.Request.SetParam("Module", "Sales");
                            q.Request.SetParam("Object", "Sale");
                            q.Request.SetParam("Action", "GetClientConsignmentNoteDocument");

                            documentCopyCount = formField.Value.ToInt();
                        }

                        break;

                    case WaybillDocumentFieldPath:

                        if (formField.Value.ToInt() > 0)
                        {
                            p.Add("INVOICE_ID", InvoiceId.ToString());
                            p.Add("DOCUMENT_FORMAT_NAME", documentFormatName);
                            p.Add("SIGNOTORY_EMPLOYEE_ID", $"{SignotoryEmployeeId}");
                            p.Add("INNER_DOCUMENT_FLAG", "0");

                            q.Request.SetParam("Module", "Sales");
                            q.Request.SetParam("Object", "Sale");
                            q.Request.SetParam("Action", "GetWaybillDocument");

                            documentCopyCount = formField.Value.ToInt();
                        }

                        break;

                    case InnerWaybillDocumentFieldPath:

                        if (formField.Value.ToInt() > 0)
                        {
                            p.Add("INVOICE_ID", InvoiceId.ToString());
                            p.Add("DOCUMENT_FORMAT_NAME", documentFormatName);
                            p.Add("SIGNOTORY_EMPLOYEE_ID", $"{SignotoryEmployeeId}");
                            p.Add("INNER_DOCUMENT_FLAG", "1");

                            q.Request.SetParam("Module", "Sales");
                            q.Request.SetParam("Object", "Sale");
                            q.Request.SetParam("Action", "GetWaybillDocument");

                            documentCopyCount = formField.Value.ToInt();
                        }

                        break;

                    case ClientWaybillDocumentFieldPath:

                        if (formField.Value.ToInt() > 0)
                        {
                            p.Add("INVOICE_ID", InvoiceId.ToString());
                            p.Add("DOCUMENT_FORMAT_NAME", documentFormatName);
                            p.Add("SIGNOTORY_EMPLOYEE_ID", $"{SignotoryEmployeeId}");


                            q.Request.SetParam("Module", "Sales");
                            q.Request.SetParam("Object", "Sale");
                            q.Request.SetParam("Action", "GetClientWaybillDocument");

                            documentCopyCount = formField.Value.ToInt();
                        }

                        break;

                    case ActWeighingDocumentFieldPath:

                        if (formField.Value.ToInt() > 0)
                        {
                            p.Add("INVOICE_ID", InvoiceId.ToString());
                            p.Add("DOCUMENT_FORMAT_NAME", documentFormatName);
                            p.Add("SIGNOTORY_EMPLOYEE_ID", $"{SignotoryEmployeeId}");


                            q.Request.SetParam("Module", "Sales");
                            q.Request.SetParam("Object", "Sale");
                            q.Request.SetParam("Action", "GetActWeighingDocument");

                            documentCopyCount = formField.Value.ToInt();
                        }

                        break;

                    case AcceptanceAndTransferCertificateFieldPath:

                        if (formField.Value.ToInt() > 0)
                        {
                            p.Add("INVOICE_ID", InvoiceId.ToString());
                            p.Add("DOCUMENT_FORMAT_NAME", documentFormatName);
                            p.Add("SIGNOTORY_EMPLOYEE_ID", $"{SignotoryEmployeeId}");


                            q.Request.SetParam("Module", "Sales");
                            q.Request.SetParam("Object", "Sale");
                            q.Request.SetParam("Action", "GetAcceptanceAndTransferCertificateDocument");

                            documentCopyCount = formField.Value.ToInt();
                        }

                        break;
                }

                DocumentItem documentItem = new DocumentItem();
                documentItem.FieldPath = formField.Key;
                if (documentCopyCount > 0)
                {
                    SendLiteBaseReport($"{this.InvoiceId}", $"{formField.Key}~{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_ffffff")}", 
                        $"Отправляем запрос на формирование документа [{GetDocumentUserName(formField.Key)}]." +
                        $"{Environment.NewLine}Количество запрошенных копий документа=[{documentCopyCount}]");

                    q.Request.SetParams(p);
                    q.Request.Timeout = requestTimeout;
                    q.Request.Attempts = requestAttempts;

                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {
                        if (q.Answer.Type == LPackClientAnswer.AnswerTypeRef.File)
                        {
                            documentItem.Initialized = true;
                            documentItem.FilePath = q.Answer.DownloadFilePath;
                            documentItem.CopyCount = documentCopyCount;

                            SendLiteBaseReport($"{this.InvoiceId}", $"{formField.Key}~{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_ffffff")}", 
                                $"Успешное формирование документа [{GetDocumentUserName(documentItem.FieldPath)}]." +
                                $"{Environment.NewLine}Локальный путь к сформированному файлу=[{documentItem.FilePath}]" +
                                $"{Environment.NewLine}Количество копий при печати=[{documentItem.CopyCount}]" +
                                $"{Environment.NewLine}Тип документа=[{documentItem.FieldPath}].");
                        }
                        else
                        {
                            q.SilentErrorProcess = true;
                            q.Answer.Status = 145;
                            q.Answer.Error.Message = "Неверный тип ответа";
                            q.Answer.Error.Description = $"Получен тип ответа [{q.Answer.Type.ToString()}], а ожидался LPackClientAnswer.AnswerTypeRef.File [{LPackClientAnswer.AnswerTypeRef.File.ToString()}]";
                            q.ProcessError();

                            SendLiteBaseReport($"{this.InvoiceId}", $"{formField.Key}~{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_ffffff")}", 
                                $"Ошибка формирования документа [{GetDocumentUserName(documentItem.FieldPath)}]." +
                                $"{Environment.NewLine}Код ошибки=[{q.Answer.Status}]" +
                                $"{Environment.NewLine}Ошибка=[{q.Answer.Error.Message}]" +
                                $"{Environment.NewLine}Описание ошибки=[{q.Answer.Error.Description}].");
                        }
                    }
                    else
                    {
                        SendLiteBaseReport($"{this.InvoiceId}", $"{formField.Key}~{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_ffffff")}", 
                            $"Ошибка формирования документа [{GetDocumentUserName(documentItem.FieldPath)}]." +
                            $"{Environment.NewLine}Код ошибки=[{q.Answer.Status}]" +
                            $"{Environment.NewLine}Ошибка=[{q.Answer.Error.Message}]" +
                            $"{Environment.NewLine}Описание ошибки=[{q.Answer.Error.Description}].");

                        q.SilentErrorProcess = true;
                        q.ProcessError();
                    }
                }

                return documentItem;
            });
        }

        /// <summary>
        /// Печать выбранных документов
        /// </summary>
        public void PrintDocument()
        {
            if (CheckInvoiceNumber())
            {
                CheckReceiptNumber();
                UpdatePayDebtBuyer();
                UpdateInvoiceDate();

                DocumentPrintVoid = PrintDocumentDelegate;

                GetAsyncDocumentList(GetDocumentFormatName(BaseDocumentFormat.PDF));

                UpdatePrintingDateTime();
                UploadWebDocumentList();
                UploadMailDocument();

                if (!HiddenInterface)
                {
                    CheckClientStamp();
                }
            }
        }

        /// <summary>
        /// Отправка документа на печать.
        /// document.Key -- Путь к файлу для печати
        /// document.Value -- Количество копий
        /// </summary>
        /// <param name="document"></param>
        public void PrintDocumentDelegate(DocumentItem documentItem)
        {
            if (documentItem.Initialized)
            {
                StartPrintingDocument(documentItem.FilePath, documentItem.CopyCount, documentItem.FieldPath);
            }
        }

        /// <summary>
        /// Отправка документа на принтер 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="copyCount"></param>
        /// <param name="fieldPath"></param>
        public void StartPrintingDocument(string filePath, int copyCount, string fieldPath = "")
        {
            SendLiteBaseReport($"{this.InvoiceId}", $"{fieldPath}~{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_ffffff")}", 
                $"Отправляем документ [{GetDocumentUserName(fieldPath)}] на печать." +
                $"{Environment.NewLine}Локальный путь к сформированному файлу=[{filePath}]" +
                $"{Environment.NewLine}Количество копий при печати=[{copyCount}]" +
                $"{Environment.NewLine}Тип документа=[{fieldPath}].");

            var printHelper = new PrintHelper();
            printHelper.PrintingProfile = PrintingSettings.DocumentPrinter.ProfileName;
            if (!string.IsNullOrEmpty(fieldPath) 
                && (fieldPath == CertificateGostDocumentFieldPath 
                || fieldPath == CertificateTuDocumentFieldPath
                || fieldPath == CmrDocumentFieldPath
                || fieldPath == QualityCertificateDocumentFieldPath
                || fieldPath == ClientConsignmentNoteDocumentFieldPath
                || fieldPath == QualityPassportDocumentFieldPath
                || fieldPath == QualityCertificateOnPaperDocumentFieldPath))
            {
                printHelper.PrintingDuplex = System.Drawing.Printing.Duplex.Simplex;
            }

            printHelper.PrintingCopies = copyCount;
            printHelper.Init();
            if (copyCount > 1)
            {
                printHelper.PrintCopiesThreadSleepTime = 500;
            }
            var printingResult = printHelper.StartPrinting(filePath, 2);
            string errorMsg = printHelper.ErrorLog;
            printHelper.Dispose();

            SendLiteBaseReport($"{this.InvoiceId}", $"{fieldPath}~{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_ffffff")}", 
                $"Документ [{GetDocumentUserName(fieldPath)}] отправлен на печать на принтер." +
                $"{Environment.NewLine}Результат отправки на печать=[{printingResult}]" +
                $"{Environment.NewLine}Количество копий=[{copyCount}]" +
                $"{Environment.NewLine}Сообщение об ошибке=[{errorMsg}]");
        }

        /// <summary>
        /// Просмотр выбранных документов
        /// </summary>
        public void ViewDocument()
        {
            DocumentPrintVoid = ViewDocumentDelegate;
            GetAsyncDocumentList(GetDocumentFormatName(BaseDocumentFormat.PDF));
        }

        /// <summary>
        /// Просмотр документа.
        /// document.Key -- Путь к файлу документа
        /// </summary>
        /// <param name="document"></param>
        public void ViewDocumentDelegate(DocumentItem documentItem)
        {
            if (documentItem.Initialized)
            {
                try
                {
                    if (!string.IsNullOrEmpty(InvoiceNumber))
                    {
                        string filePath = System.IO.Path.GetDirectoryName(documentItem.FilePath);
                        string fileName = GetDocumentUserName(documentItem.FieldPath);
                        string fileExtension = System.IO.Path.GetExtension(documentItem.FilePath);
                        //string fileFullName = $"{fileName}{fileExtension}";
                    
                        string fileNumber = $"{InvoiceNumber} от {InvoiceDate}_{DateTime.Now.ToString("ddMMyyyyHHmmss")}";
                        string fileFullName = $"{fileNumber}{fileExtension}";

                        if (!string.IsNullOrEmpty(fileName))
                        {
                            fileFullName = $"{fileName} {fileFullName}";
                        }

                        if (!string.IsNullOrEmpty(BuyerName))
                        {
                            BuyerName = BuyerName.Replace("\"", "");
                            fileFullName = $"{BuyerName} {fileFullName}";
                        }
                    
                        string fileFullPath = System.IO.Path.Combine(filePath, fileFullName);

                        if (System.IO.File.Exists(fileFullPath))
                        {
                            System.IO.File.Delete(fileFullPath);
                        }

                        System.IO.File.Move(documentItem.FilePath, fileFullPath);

                        documentItem.FilePath = fileFullPath;
                    }
                }
                catch (Exception ex)
                {
                }
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var printHelper = new PrintHelper();
                    printHelper.PrintingProfile = PrintingSettings.DocumentPrinter.ProfileName;
                    printHelper.Init();
                    var printingResult = printHelper.ShowPreview(documentItem.FilePath);
                    printHelper.Dispose();
                });
            }
        }

        /// <summary>
        /// Просмотр выбранных документов в Html.
        /// </summary>
        public void HtmlDocument()
        {
            DocumentPrintVoid = HtmlDocumentDelegate;
            GetAsyncDocumentList(GetDocumentFormatName(BaseDocumentFormat.HTML));
        }

        /// <summary>
        /// Просмотр документа в Html.
        /// document.Key -- Путь к файлу документа
        /// </summary>
        public void HtmlDocumentDelegate(DocumentItem documentItem)
        {
            if (documentItem.Initialized)
            {
                Central.OpenFile(documentItem.FilePath);
            }
        }

        /// <summary>
        /// Просмотр выбранных документов в docx.
        /// </summary>
        public void DocxDocument()
        {
            DocumentPrintVoid = DocxDocumentDelegate;
            GetAsyncDocumentList(GetDocumentFormatName(BaseDocumentFormat.DOCX));
        }

        /// <summary>
        /// Просмотр документа в docx.
        /// document.Key -- Путь к файлу документа
        /// </summary>
        public void DocxDocumentDelegate(DocumentItem documentItem)
        {
            if (documentItem.Initialized)
            {
                Central.OpenFile(documentItem.FilePath);
            }
        }

        /// <summary>
        /// Выгрузка всех необходимых документов в веб для выбранной накладной
        /// </summary>
        public async void UploadWebDocumentList()
        {
            UploadWebDocumentButton.IsEnabled = false;

            SendLiteBaseReport($"{this.InvoiceId}", $"WEB~{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_ffffff")}", $"Отправляем запрос на выгрузку [Web документов].");

            var p = new Dictionary<string, string>();

            p.Add("INVOICE_ID", InvoiceId.ToString());
            p.Add("SIGNOTORY_EMPLOYEE_ID", $"{SignotoryEmployeeId}");
            p.Add("PAYMENT_DOCUMENT_NUMBER", Form.GetValueByPath("PAYMENT_DOCUMENT_NUMBER"));
            p.Add("PAYMENT_DOCUMENT_DATE", Form.GetValueByPath("PAYMENT_DOCUMENT_DATE"));
            p.Add("PAYMENT_DOCUMENT_NUMBER2", Form.GetValueByPath("PAYMENT_DOCUMENT_NUMBER2"));
            p.Add("PAYMENT_DOCUMENT_DATE2", Form.GetValueByPath("PAYMENT_DOCUMENT_DATE2"));
            p.Add("ZERO_PRICE_FLAG", Form.GetValueByPath("ZERO_PRICE").ToBool().ToString());
            p.Add("FACTORY_ID", $"{FactoryId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "Sale");
            q.Request.SetParam("Action", "UploadWebDocumentList");
            q.Request.SetParams(p);

            q.Request.Timeout = GetDocumentTimeout;
            q.Request.Attempts = 1;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                bool succesfullFlag = false;

                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        if (!string.IsNullOrEmpty(ds.Items.First().CheckGet("INVOICE_ID")))
                        {
                            succesfullFlag = true;

                            SendLiteBaseReport($"{this.InvoiceId}", $"WEB~{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_ffffff")}", $"Успешная выгрузка [Web документов].");
                        }
                    }
                }

                if (!succesfullFlag)
                {
                    var msg = "Произошла ошибка при выгрузке Web документов. Пожалуйста, сообщите о проблеме.";
                    var d = new DialogWindow($"{msg}", "Менеджер печати документов", "", DialogWindowButtons.OK);
                    d.ShowDialog();

                    SendLiteBaseReport($"{this.InvoiceId}", $"WEB~{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_ffffff")}", $"Произошла необработанная ошибка при выгрузке [Web документов].");
                }
            }
            else
            {
                SendLiteBaseReport($"{this.InvoiceId}", $"WEB~{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_ffffff")}", $"Ошибка выгрузки [Web документов]." +
                            $"{Environment.NewLine}Код ошибки=[{q.Answer.Status}]" +
                            $"{Environment.NewLine}Ошибка=[{q.Answer.Error.Message}]" +
                            $"{Environment.NewLine}Описание ошибки=[{q.Answer.Error.Description}].");

                q.SilentErrorProcess = true;
                q.ProcessError();
            }

            UploadWebDocumentButton.IsEnabled = true;
        }

        /// <summary>
        /// Выгрузка документа по почте
        /// </summary>
        public async void UploadMailDocument()
        {
            var p = new Dictionary<string, string>();

            p.Add("INVOICE_ID", InvoiceId.ToString());
            p.Add("SIGNOTORY_EMPLOYEE_ID", $"{SignotoryEmployeeId}");
            p.Add("PAYMENT_DOCUMENT_NUMBER", Form.GetValueByPath("PAYMENT_DOCUMENT_NUMBER"));
            p.Add("PAYMENT_DOCUMENT_DATE", Form.GetValueByPath("PAYMENT_DOCUMENT_DATE"));
            p.Add("PAYMENT_DOCUMENT_NUMBER2", Form.GetValueByPath("PAYMENT_DOCUMENT_NUMBER2"));
            p.Add("PAYMENT_DOCUMENT_DATE2", Form.GetValueByPath("PAYMENT_DOCUMENT_DATE2"));
            p.Add("ZERO_PRICE_FLAG", Form.GetValueByPath("ZERO_PRICE").ToBool().ToString());
            p.Add("FACTORY_ID", $"{FactoryId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "Sale");
            q.Request.SetParam("Action", "UploadMailDocument");
            q.Request.SetParams(p);

            q.Request.Timeout = GetDocumentTimeout;
            q.Request.Attempts = 1;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                bool succesfullFlag = false;

                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        if (!string.IsNullOrEmpty(ds.Items.First().CheckGet("INVOICE_ID")))
                        {
                            succesfullFlag = true;
                        }
                    }
                }

                if (!succesfullFlag)
                {
                    SendErrorReport($"Произошла ошибка при выгрузке документа по почте. InvoiceId=[{InvoiceId}]");
                }
            }
            else
            {
                q.SilentErrorProcess = true;
                q.ProcessError();
            }
        }

        #endregion

        /// <summary>
        /// Сохраняем репорт о клиентской ошибке
        /// </summary>
        private void SendErrorReport(string msg)
        {
            var q = new LPackClientQuery();
            // Отключаем стандартное всплывающее сообщение с ошибкой и отправляем репорт
            q.Answer.Error.Message = msg;
            q.SilentErrorProcess = true;
            q.Answer.Status = 145;
            q.ProcessError();
        }

        private async void SendLiteBaseReport(string tableDirectory, string primaryKeyValue, string msg)
        {
            string jsonString = "";

            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("MESSAGE", msg);
            data.Add("DTTM", DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_ffffff"));

            try
            {
                jsonString = JsonConvert.SerializeObject(data);
            }
            catch (Exception ex)
            {
                SendErrorReport(ex.Message);
            }

            var p = new Dictionary<string, string>();

            p.Add("ITEMS", jsonString);
            // 1=global,2=local,3=net
            p.Add("STORAGE_TYPE", "3");
            p.Add("TABLE_NAME", LiteBaseReportTableName);
            p.Add("TABLE_DIRECTORY", tableDirectory);
            p.Add("PRIMARY_KEY", LiteBaseReportPrimaryKey);
            p.Add("PRIMARY_KEY_VALUE", primaryKeyValue);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Service");
            q.Request.SetParam("Object", "LiteBase");
            q.Request.SetParam("Action", "SaveData");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestTimeoutDefault;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
            }
            else
            {
                q.SilentErrorProcess = true;
                q.ProcessError();
            }
        }

        public async void ShowLiteBaseReport()
        {
            var p = new Dictionary<string, string>();
            // 1=global,2=local,3=net
            p.Add("STORAGE_TYPE", "3");
            p.Add("TABLE_NAME", LiteBaseReportTableName);
            p.Add("TABLE_DIRECTORY", $"{this.InvoiceId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Service");
            q.Request.SetParam("Object", "LiteBase");
            q.Request.SetParam("Action", "List");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestTimeoutDefault;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                string logMsg = "";

                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, LiteBaseReportTableName);
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        List<Dictionary<string, string>> logList = new List<Dictionary<string, string>>();
                        logList = ds.Items.OrderBy(x => x.CheckGet("DTTM")).ThenBy(x => x.CheckGet(LiteBaseReportPrimaryKey)).ToList();

                        foreach (var logItem in logList)
                        {
                            logMsg = $"{logMsg}" +
                                $"{Environment.NewLine}-----{logItem.CheckGet("ON_DATE")}-----" +
                                $"{Environment.NewLine}{logItem.CheckGet("MESSAGE")}";
                        }

                        var d = new DialogWindow($"{logMsg}", this.ControlTitle, "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }

                if (string.IsNullOrEmpty(logMsg))
                {
                    var d = new DialogWindow($"Не найдена история печати по выбранной накладной.", this.ControlTitle, "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                q.SilentErrorProcess = true;
                q.ProcessError();
            }
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            PrintDocument();
        }

        private void ViewButton_Click(object sender, RoutedEventArgs e)
        {
            ViewDocument();
        }

        private void HtmlButton_Click(object sender, RoutedEventArgs e)
        {
            HtmlDocument();
        }

        private void DocxButton_Click(object sender, RoutedEventArgs e)
        {
            DocxDocument();
        }

        private void UploadWebDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            UploadWebDocumentList();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessCommand("help");
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            SetPrintSettings();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            Reset();
        }

        private void AsyncPrintCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)AsyncPrintCheckBox.IsChecked)
            {
                AsyncPrintFlag = true;
            }
            else
            {
                AsyncPrintFlag = false; 
            }
        }

        private void ThreadSleepTimeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                ThreadSleepTime = ((TextBox)sender).Text.ToInt();
            }
            catch 
            { 
            }
        }

        private void UploadFileButton_Click(object sender, RoutedEventArgs e)
        {
            UploadFile();
        }

        private void ShowPrintingHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            ShowLiteBaseReport();
        }
    }

    public struct DocumentItem
    {
        public bool Initialized { get; set; }

        /// <summary>
        /// Путь к сгенерированному файлу документа
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Поле формы, соответсвтующее этому типу документа
        /// </summary>
        public string FieldPath { get; set; }

        /// <summary>
        /// Количество копий документа, которое нужно распечатать
        /// </summary>
        public int CopyCount { get; set; }
    }
}
