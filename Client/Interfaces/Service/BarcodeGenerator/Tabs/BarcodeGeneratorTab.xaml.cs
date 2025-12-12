using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Service.Printing;
using DevExpress.Mvvm.Xpf;
using DevExpress.Xpf.Core.Native;
using NPOI.OpenXmlFormats.Vml;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Client.Interfaces.Service
{
    /// <summary>
    /// Интерфейс Генератор штрих-кода
    /// </summary>
    public partial class BarcodeGeneratorTab : ControlBase
    {
        public BarcodeGeneratorTab()
        {
            ControlTitle = "Генератор штрих-кода";
            RoleName = "[erp]barcode_generator";
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

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        public string BarcodeImageFilePath { get; set; }

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
                        Path = "BARCODE_FORMAT",
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = BarcodeFormatSelectBox,
                        ControlType = "SelectBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "WIDTH",
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = WidthTextBox,
                        ControlType = "TextBox",
                        Default = "400",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "HEIGHT",
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = HeightTextBox,
                        ControlType = "TextBox",
                        Default = "400",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "BARCODE_CONTENT",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = BarcodeContentTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },

                    new FormHelperField()
                    {
                        Path = "PRINTING_PROFILE",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = PrintingProfileSelectBox,
                        ControlType = "SelectBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                };

                Form.SetFields(fields);
                Form.StatusControl = StatusTextBox;
                Form.ToolbarControl = FormToolbar;
            }
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();

            BarcodeFormatSelectBox.SetItems(BarcodeGenerator.FormatDictionary);

            var list = Central.AppSettings.SectionGet("PRINTING_SETTINGS");
            var ds = ListDataSet.Create(list);
            if (ds != null && ds.Items != null && ds.Items.Count > 0)
            {
                PrintingProfileSelectBox.SetItems(ds, "NAME", "NAME");
            }

            ImageToolbar.IsEnabled = false;
        }

        public void GenerateBarcode()
        {
            BarcodeImageFilePath = null;

            if (Form.Validate())
            {
                var v = Form.GetValues();

                BarcodeImageFilePath = BarcodeGenerator.GetBarcodeFile(v.CheckGet("BARCODE_CONTENT"), v.CheckGet("BARCODE_FORMAT").ToInt(), v.CheckGet("WIDTH").ToInt(), v.CheckGet("HEIGHT").ToInt());
                RenderBarcodeImage(BarcodeImageFilePath);

                BarcodeImageBorder.Width = v.CheckGet("WIDTH").ToInt();
                BarcodeImageBorder.Height = v.CheckGet("HEIGHT").ToInt();
            }
        }

        public void RenderBarcodeImage(string filePath)
        {
            BarcodeImage.Source = null;

            if (!string.IsNullOrEmpty(filePath))
            {
                try
                {
                    if (System.IO.File.Exists(filePath))
                    {
                        BitmapImage bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.UriSource = new Uri(filePath);
                        bitmapImage.EndInit();

                        BarcodeImage.Source = bitmapImage;

                        ImageToolbar.IsEnabled = true;
                    }
                }
                catch (Exception ex)
                {
                    var msg = $"Ошибка отрисовки изображения. Пожалуйста, сообщите о проблеме. {ex.Message}";
                    var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                var msg = "Нет изображения для отрисовки. Пожалуйста, запустите операцию генерации изображения повторно.";
                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        private void SetFormFilters()
        {
            switch (Form.GetValueByPath("BARCODE_FORMAT").ToInt())
            {
                case 1:
                    Form.RemoveFilter("BARCODE_CONTENT", FormHelperField.FieldFilterRef.MaxLen);
                    Form.RemoveFilter("BARCODE_CONTENT", FormHelperField.FieldFilterRef.DigitOnly);
                    Form.RemoveFilter("BARCODE_CONTENT", FormHelperField.FieldFilterRef.MinLen);

                    Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BARCODE_CONTENT"), FormHelperField.FieldFilterRef.MaxLen, 13);
                    Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BARCODE_CONTENT"), FormHelperField.FieldFilterRef.DigitOnly);
                    Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BARCODE_CONTENT"), FormHelperField.FieldFilterRef.MinLen, 12);
                    break;

                case 2:
                    Form.RemoveFilter("BARCODE_CONTENT", FormHelperField.FieldFilterRef.MaxLen);
                    Form.RemoveFilter("BARCODE_CONTENT", FormHelperField.FieldFilterRef.DigitOnly);
                    Form.RemoveFilter("BARCODE_CONTENT", FormHelperField.FieldFilterRef.MinLen);

                    Form.AddFilter(Form.Fields.FirstOrDefault(x => x.Path == "BARCODE_CONTENT"), FormHelperField.FieldFilterRef.MaxLen, 70);
                    break;

                default:
                    Form.RemoveFilter("BARCODE_CONTENT", FormHelperField.FieldFilterRef.MaxLen);
                    Form.RemoveFilter("BARCODE_CONTENT", FormHelperField.FieldFilterRef.DigitOnly);
                    Form.RemoveFilter("BARCODE_CONTENT", FormHelperField.FieldFilterRef.MinLen);
                    break;
            }
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            var frameName = $"{ControlName}";
            Central.WM.Close(frameName);
        }

        public void SaveDocument()
        {
            if (!string.IsNullOrEmpty(BarcodeImageFilePath))
            {
                Client.Interfaces.Service.Printing.PrintHelper printHelper = new Client.Interfaces.Service.Printing.PrintHelper();
                printHelper.Init();
                if (printHelper != null)
                {
                    printHelper.SaveDocument(this.BarcodeImageFilePath);
                }
            }
            else
            {
                var msg = "Нет изображения для сохранения. Запустите генерацию изображения.";
                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        public void Print()
        {
            if (!string.IsNullOrEmpty(BarcodeImageFilePath))
            {
                var printingProfile = Form.GetValueByPath("PRINTING_PROFILE");
                if (!string.IsNullOrEmpty(printingProfile))
                {
                    Client.Interfaces.Service.Printing.PrintHelper printHelper = new Client.Interfaces.Service.Printing.PrintHelper();
                    printHelper.PrintingProfile = printingProfile;
                    printHelper.PrintingCopies = 1;
                    printHelper.Init();
                    printHelper.StartPrinting(this.BarcodeImageFilePath);
                    printHelper.Dispose();
                }
                else
                {
                    var msg = "Не выбран профиль печати. Выберите профиль из выпадающего списка и повторите операцию.";
                    var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                var msg = "Нет изображения для печати. Запустите генерацию изображения.";
                var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Установка настроек для принтера
        /// </summary>
        public void SetPrintSettings()
        {
            var i = new PrintingInterface();
        }

        public void ShowHelper()
        {
            Central.ShowHelp("/doc/l-pack-erp/");
        }

        public void ClearImage()
        {
            Form.SetDefaults();
            BarcodeImage.Source = null;
            ImageToolbar.IsEnabled = false;
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            Print();
        }

        private void SaveDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            SaveDocument();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            GenerateBarcode();
        }

        private void BarcodeFormatSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SetFormFilters();
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            SetPrintSettings();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelper();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearImage();
        }
    }
}
