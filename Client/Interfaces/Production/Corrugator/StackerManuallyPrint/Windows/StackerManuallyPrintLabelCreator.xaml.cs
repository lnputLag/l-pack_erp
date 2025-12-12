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
    /// Заполнение информации для управлением ручной печатью ярлыка.
    /// В случае печати одного ярлыка в форме заполняется порядковый номер создаваемого поддона;
    /// В случае печати нескольких ярлыков в форме указывается количество создаваемых поддонов.
    /// </summary>
    /// <author>sviridov_ae</author>
    public partial class StackerManuallyPrintLabelCreator : UserControl
    {
        /// <summary>
        /// Конструктор формы заполнения информации для управлением ручной печатью ярлыка
        /// </summary>
        /// <param name="type">
        /// Тип формы.
        /// 1 -- форма для заполнения порядкового номера поддона;
        /// 2 -- форма для заполнения количества создаваемых поддонов.
        /// </param>
        public StackerManuallyPrintLabelCreator(int type)
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            FrameName = "StackerManuallyPrintLabelCreator";
            Loaded += (object sender, RoutedEventArgs e) =>
            {
                if (Type == 1)
                {
                    PalletNumberTextBox.Focus();
                }
                else if (Type == 2)
                {
                    QuantityPalletTextBox.Focus();
                }

                //регистрация обработчика клавиатуры
                PreviewKeyDown += ProcessKeyboard;
            };

            Type = type;

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
        public FormHelper Form { get; set; }

        /// <summary>
        /// Итоговое значение поля ввода формы
        /// </summary>
        public int ResultValue { get; set; }

        /// <summary>
        /// Флаг того, что вся работа на форме успешно завершена
        /// </summary>
        public bool SuccessFlag { get; set; }

        /// <summary>
        /// Тип формы.
        /// 1 -- форма для заполнения порядкового номера поддона;
        /// 2 -- форма для заполнения количества создаваемых поддонов.
        /// </summary>
        public int Type { get; set; }

        public void InitForm()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>();

            if (Type == 1)
            {
                fields = new List<FormHelperField>
                {
                    new FormHelperField
                    {
                        Path = "PALLET_NUMBER",
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = PalletNumberTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.DigitOnly, null },
                            { FormHelperField.FieldFilterRef.IsNotZero, null }
                        },
                    },
                };

                QuantityPalletGrid.Visibility = Visibility.Collapsed;
            }
            else if (Type == 2)
            {
                fields = new List<FormHelperField>
                {
                    new FormHelperField
                    {
                        Path = "QUANTITY_PALLET",
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = QuantityPalletTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.DigitOnly, null },
                            { FormHelperField.FieldFilterRef.IsNotZero, null }
                        },
                    },
                };

                PalletNumberGrid.Visibility = Visibility.Collapsed;
            }

            Form.SetFields(fields);
        }

        public void SetDefaults()
        {
            Form.SetDefaults();
        }

        public void Save()
        {
            if (Form != null)
            {
                if (Form.Validate())
                {
                    if (Type == 1)
                    {
                        ResultValue = Form.GetValueByPath("PALLET_NUMBER").ToInt();
                    }
                    else if (Type == 2)
                    {
                        ResultValue = Form.GetValueByPath("QUANTITY_PALLET").ToInt();
                    }

                    SuccessFlag = true;

                    Close();
                }
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
            Central.WM.Show(FrameName, "Печать ярлыка", true, "add", this, "top", windowParametrs);
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m">Сообщение</param>
        private void ProcessMessages(ItemMessage m)
        {

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
                    break;

                case Key.Escape:
                    Close();
                    break;
            }
        }

        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Production",
                ReceiverName = "",
                SenderName = "StackerManuallyPrintLabelCreator",
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

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }
    }
}
