using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Service.Printing;
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

namespace Client.Interfaces.Sales
{
    /// <summary>
    /// Логика взаимодействия для DocumentListChoise.xaml
    /// </summary>
    public partial class DocumentListChoise : ControlBase
    {
        public DocumentListChoise()
        {
            ControlTitle = "Выбор пакета документов";
            InitializeComponent();

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

        public delegate void SaveDelegate(Dictionary<string, string> documentItem);

        public SaveDelegate OnSaveVoid;

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Идентификатор сотрудника - подписанта
        /// </summary>
        public int SignotoryEmployeeId { get; set; }

        /// <summary>
        /// Полное имя сотрудника - подписанта
        /// </summary>
        public string SignotoryEmployeeName { get; set; }

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
                        Path = DocumentPrintManager.PackingListDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = PackingListTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = DocumentPrintManager.InvoiceDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = InvoiceTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = DocumentPrintManager.ReceiptDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = ReceiptTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = DocumentPrintManager.UniversalTransferDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = UniversalTransferDocTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = DocumentPrintManager.InnerUniversalTransferDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = InnerUniversalTransferDocTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = DocumentPrintManager.UniversalAdjustmentDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = UniversalAdjustmentDocTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },



                    new FormHelperField()
                    {
                        Path = DocumentPrintManager.WaybillDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = WaybillTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = DocumentPrintManager.InnerWaybillDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = InnerWaybillTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = DocumentPrintManager.ClientWaybillDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = ClientWaybillTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = DocumentPrintManager.ConsignmentNoteDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = ConsignmentNoteTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = DocumentPrintManager.InnerConsignmentNoteDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = InnerConsignmentNoteTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = DocumentPrintManager.ClientConsignmentNoteDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = ClientConsignmentNoteTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },



                    new FormHelperField()
                    {
                        Path = DocumentPrintManager.QualityCertificateDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = QualityCertificateTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = DocumentPrintManager.QualityCertificateOnPaperDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = QualityCertificateOnPaperTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = DocumentPrintManager.QualityPassportDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = QualityPassportTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = DocumentPrintManager.SpecificationOnPaperDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = SpecificationOnPaperTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = DocumentPrintManager.CertificateGostDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = CertificateGostTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = DocumentPrintManager.CertificateTuDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = CertificateTuTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = DocumentPrintManager.CmrDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = CmrTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = DocumentPrintManager.InnerCmrDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = InnerCmrTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },

                    new FormHelperField()
                    {
                        Path = DocumentPrintManager.ActWeighingDocumentFieldPath,
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = ActWeighingTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },

                };

                Form.SetFields(fields);
                Form.ToolbarControl = FormToolbar;
            }
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();

            SetSignotory(SignotoryEmployeeId, SignotoryEmployeeName);

            Form.SetValueByPath(DocumentPrintManager.UniversalTransferDocumentFieldPath, "2");
        }

        /// <summary>
        /// Очистка всех полей с количеством копий документа
        /// </summary>
        public void Reset()
        {
            Form.SetValueByPath(DocumentPrintManager.UniversalTransferDocumentFieldPath, "");
            Form.SetValueByPath(DocumentPrintManager.WaybillDocumentFieldPath, "");
            Form.SetValueByPath(DocumentPrintManager.ConsignmentNoteDocumentFieldPath, "");
            Form.SetValueByPath(DocumentPrintManager.CmrDocumentFieldPath, "");
            Form.SetValueByPath(DocumentPrintManager.ReceiptDocumentFieldPath, "");
            Form.SetValueByPath(DocumentPrintManager.QualityCertificateDocumentFieldPath, "");
            Form.SetValueByPath(DocumentPrintManager.QualityPassportDocumentFieldPath, "");
            Form.SetValueByPath(DocumentPrintManager.CertificateGostDocumentFieldPath, "");
            Form.SetValueByPath(DocumentPrintManager.UniversalAdjustmentDocumentFieldPath, "");
            Form.SetValueByPath(DocumentPrintManager.ClientWaybillDocumentFieldPath, "");
            Form.SetValueByPath(DocumentPrintManager.ClientConsignmentNoteDocumentFieldPath, "");
            Form.SetValueByPath(DocumentPrintManager.PackingListDocumentFieldPath, "");
            Form.SetValueByPath(DocumentPrintManager.InvoiceDocumentFieldPath, "");
            Form.SetValueByPath(DocumentPrintManager.QualityCertificateOnPaperDocumentFieldPath, "");
            Form.SetValueByPath(DocumentPrintManager.SpecificationOnPaperDocumentFieldPath, "");
            Form.SetValueByPath(DocumentPrintManager.CertificateTuDocumentFieldPath, "");
            Form.SetValueByPath(DocumentPrintManager.InnerUniversalTransferDocumentFieldPath, "");
            Form.SetValueByPath(DocumentPrintManager.InnerWaybillDocumentFieldPath, "");
            Form.SetValueByPath(DocumentPrintManager.InnerConsignmentNoteDocumentFieldPath, "");
            Form.SetValueByPath(DocumentPrintManager.InnerCmrDocumentFieldPath, "");
            Form.SetValueByPath(DocumentPrintManager.ActWeighingDocumentFieldPath, "");
        }

        public void Save()
        {
            // Данные формы
            Dictionary<string, string> formValues = Form.GetValues();
            OnSaveVoid?.Invoke(formValues);
            this.Close();
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
            this.MinHeight = 300;
            this.MinWidth = 700;
            Central.WM.Show(frameName, this.ControlTitle, true, "main", this, null, windowParametrs);
        }


        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            var frameName = $"{ControlName}";
            Central.WM.Close(frameName);
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

        /// <summary>
        /// Установка настроек для принтера
        /// </summary>
        public void SetPrintSettings()
        {
            var i = new PrintingInterface();
            Close();
        }

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

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            Reset();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessCommand("help");
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            SetPrintSettings();
        }
    }
}
