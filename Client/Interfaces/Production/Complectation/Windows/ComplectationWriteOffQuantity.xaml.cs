using Client.Common;
using GalaSoft.MvvmLight.Messaging;
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

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Интерфейс ввода количества продукции, которое необходимо списать с выбранного поддона при комплектации
    /// </summary>
    public partial class ComplectationWriteOffQuantity : UserControl
    {
        public ComplectationWriteOffQuantity(int defaultQuantityOnPallet, int currentQuantityOnPallet, string palletNumber)
        {
            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            InitializeComponent();
            //регистрация обработчика клавиатуры
            PreviewKeyDown += ProcessKeyboard;

            FrameName = "ComplectationWriteOffQuantity";
            DefaultQuantityOnPallet = defaultQuantityOnPallet;
            CurrentQuantityOnPallet = currentQuantityOnPallet;
            PalletNumber = palletNumber;

            InitForm();
            SetDefaults();
        }

        /// <summary>
        /// Техническое имя фрейма
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// Процессор форм
        /// </summary>
        public FormHelper FormHelper { get; set; }

        /// <summary>
        /// Количество продукции на поддоне по умолчанию
        /// </summary>
        public int DefaultQuantityOnPallet { get; set; }

        /// <summary>
        /// Количество продукции на выбранном поддоне
        /// </summary>
        public int CurrentQuantityOnPallet { get; set; }

        /// <summary>
        /// Количество продукции на поддоне, которое останется после списания
        /// </summary>
        public int RemainQuantityOnPallet { get; set; }

        /// <summary>
        /// Номер поддона
        /// </summary>
        public string PalletNumber { get; set; }

        /// <summary>
        /// Флаг того, что работа с интерфейсом успешно завершена. Результат работы можно забрать из RemainQuantityOnPallet
        /// </summary>
        public bool OkFlag { get; set; }

        public void InitForm()
        {
            FormHelper = new FormHelper();

            var fields = new List<FormHelperField>
                {
                    new FormHelperField
                    {
                        Path = "DEFAULT_QUANTITY_ON_PALLET",
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = DefaultQuantityOnPalletTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField
                    {
                        Path = "CURRENT_QUANTITY_ON_PALLET",
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = CurrentQuantityOnPalletTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField
                    {
                        Path = "WRITE_OFF_QUANTITY",
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = WriteOffQuantityTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField
                    {
                        Path = "REMAIN_QUANTITY_ON_PALLET",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = RemainQuantityOnPalletTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.MinValue, 0},
                        },
                    },
                    new FormHelperField
                    {
                        Path = "PALLET_NUMBER",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = PalletNumberTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                };

            FormHelper.SetFields(fields);
        }

        public void SetDefaults()
        {
            FormHelper.SetValueByPath("DEFAULT_QUANTITY_ON_PALLET", $"{DefaultQuantityOnPallet}");
            FormHelper.SetValueByPath("CURRENT_QUANTITY_ON_PALLET", $"{CurrentQuantityOnPallet}");
            FormHelper.SetValueByPath("PALLET_NUMBER", $"{PalletNumber}");
            FormHelper.SetValueByPath("WRITE_OFF_QUANTITY", "0");

            WriteOffQuantityTextBox.Focus();
        }

        /// <summary>
        /// Расчёт количества на поддоне, которое останется после списания с заданными параметрами (количеством на выбранном поддоне и количеством списываемой продукции)
        /// </summary>
        public void CalculateRemainQuantityOnPallet()
        {
            if (CurrentQuantityOnPalletTextBox != null && WriteOffQuantityTextBox != null && RemainQuantityOnPalletTextBox != null)
            {
                int current = CurrentQuantityOnPalletTextBox.Text.ToInt();
                int writeOff = WriteOffQuantityTextBox.Text.ToInt();
                int remain = current - writeOff;

                RemainQuantityOnPalletTextBox.Text = $"{remain}";
            }
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

            Dictionary<string, string> windowParametrs = new Dictionary<string, string>();
            windowParametrs.Add("no_resize", "1");
            windowParametrs.Add("center_screen", "1");
            Central.WM.Show(FrameName, "Списание с поддона", true, "add", this, "top", windowParametrs);
        }

        public void Save()
        {
            if (FormHelper.Validate())
            {
                if ((!string.IsNullOrEmpty(RemainQuantityOnPalletTextBox.Text)) && RemainQuantityOnPalletTextBox.Text.ToInt() >= 0)
                {
                    OkFlag = true;
                    RemainQuantityOnPallet = RemainQuantityOnPalletTextBox.Text.ToInt();
                    Close();
                }
            }
        }


        /// <summary>
        /// обработчик нажатий клавиатуры
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessKeyboard(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    Save();
                    e.Handled = true;
                    break;

                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;
            }
        }

        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Complectation",
                ReceiverName = "",
                SenderName = "ComplectationWriteOffQuantity",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
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

        /// <summary>
        /// Вызов справки
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/production/");
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m">Сообщение</param>
        private void ProcessMessages(ItemMessage m)
        {

        }

        private void CurrentQuantityOnPalletTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateRemainQuantityOnPallet();
        }

        private void WriteOffQuantityTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateRemainQuantityOnPallet();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }
    }
}
