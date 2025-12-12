using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;

namespace Client.Interfaces.Service
{
    /// <summary>
    /// Интерфейс выбора типа штрихкода
    /// </summary>
    public partial class BarcodeType : ControlBase
    {
        /// <summary>
        /// Конструктор интерфейса выбора типа штрихкода
        /// </summary>
        public BarcodeType()
        {
            ControlTitle = "Выбор типа штрих-кода";
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

        public FormHelper Form { get; set; }

        public delegate void SaveDelegate(KeyValuePair<string, string> selectedType);
        public SaveDelegate OnSave { get; set; }

        public KeyValuePair<string, string> SelectedType { get; set; }

        public string ParentFrame { get; set; }

        /// <summary>
        /// Инициализация компонентов
        /// </summary>
        private void FormInit()
        {
            Form = new FormHelper();

            //колонки формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path = "BARCODE_FORMAT",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = BarcodeFormatSelectBox,
                    ControlType = "SelectBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
            };

            Form.SetFields(fields);
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            Form.SetDefaults();

            Dictionary<string, string> selectBoxItems = new Dictionary<string, string>();
            selectBoxItems.Add("0", "Не использовать");
            selectBoxItems.AddRange(BarcodeGenerator.FormatDictionary);
            BarcodeFormatSelectBox.SetItems(selectBoxItems);

            BarcodeFormatSelectBox.SetSelectedItemFirst();
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

            var frameName = $"{ControlName}";
            Central.WM.Close(frameName);
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

            FrameName = $"{ControlName}";
            Dictionary<string, string> windowParametrs = new Dictionary<string, string>();
            windowParametrs.Add("no_resize", "1");
            windowParametrs.Add("center_screen", "1");
            this.MinHeight = 70;
            this.MinWidth = 280;
            Central.WM.Show(FrameName, ControlTitle, true, "main", this, "top", windowParametrs);
        }

        /// <summary>
        /// Выбираем и закрываем окно
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (Form.Validate())
            {
                SelectedType = BarcodeFormatSelectBox.SelectedItem;
                OnSave?.Invoke(SelectedType);
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
