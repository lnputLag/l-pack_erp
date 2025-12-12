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
    /// Окно ввода/подтверждения числовых данных при расчёте количества картона на поддоне для комплектации на ГА.
    /// Используется для корректировки данных по количеству стоп на поддоне и толщине картона.
    /// </summary>
    public partial class ComplectationCMQuantity : UserControl
    {
        /// <summary>
        /// Форма для подтверждения данных.
        /// Тип входных/выходных данных -- double.
        /// Результирующее значение заносит в QtyDouble.
        /// </summary>
        /// <param name="qty"></param>
        public ComplectationCMQuantity(double qty = 0, bool qtyIsNotZeroFlag = true)
        {
            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            ToIntFlag = false;

            InitializeComponent();
            PreviewKeyDown += ProcessKeyboard2;
            InitForm();

            if (!qtyIsNotZeroFlag)
            {
                FormHelper.RemoveFilter("QTY", FormHelperField.FieldFilterRef.IsNotZero);
            }

            FrameName = "ComplectationCMQuantityStackInPallet";

            SetVisibility();

            QtyDouble = qty;

            QtyTextBox.Text = QtyDouble.ToString();
            QtyTextBox.CaretIndex = QtyTextBox.Text.Length;

            OkFlag = false;
        }

        /// <summary>
        /// Форма для подтверждения данных.
        /// Тип входных/выходных данных -- int.
        /// Результирующее значение заносит в QtyInt.
        /// </summary>
        /// <param name="qty"></param>
        public ComplectationCMQuantity(int qty = 0, bool qtyIsNotZeroFlag = true)
        {
            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            ToIntFlag = true;

            InitializeComponent();
            PreviewKeyDown += ProcessKeyboard2;
            InitForm();

            if (!qtyIsNotZeroFlag)
            {
                FormHelper.RemoveFilter("QTY", FormHelperField.FieldFilterRef.IsNotZero);
            }

            FrameName = "ComplectationCMQuantityStackInPallet";

            SetVisibility();

            QtyInt = qty;

            QtyTextBox.Text = QtyInt.ToString();
            QtyTextBox.CaretIndex = QtyTextBox.Text.Length;

            OkFlag = false;
        }

        /// <summary>
        /// Форма для подтверждения данных.
        /// Тип входных/выходных данных -- string.
        /// Результирующее значение заносит в QtyString.
        /// </summary>
        /// <param name="text"></param>
        public ComplectationCMQuantity(string text = "", bool bigFormFlag = false, bool qtyStringIsNotNullFlag = true)
        {
            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            ToStringFlag = true;

            InitializeComponent();

            if (bigFormFlag)
            {
                var cd = MainGrid.ColumnDefinitions;
                cd.Clear();

                {
                    ColumnDefinition columnDefinition = new ColumnDefinition();
                    var gridLength = new GridLength(1, GridUnitType.Star);
                    columnDefinition.Width = gridLength;
                    cd.Add(columnDefinition); 
                }

                {
                    ColumnDefinition columnDefinition = new ColumnDefinition();
                    var gridLength = new GridLength(2, GridUnitType.Star);
                    columnDefinition.Width = gridLength;
                    cd.Add(columnDefinition);
                }

                {
                    ColumnDefinition columnDefinition = new ColumnDefinition();
                    var gridLength = new GridLength(5, GridUnitType.Pixel);
                    columnDefinition.Width = gridLength;
                    cd.Add(columnDefinition);
                }

                this.Width = 600;
                this.Height = 95;
                QtyTextBox.Width = 395;
                QtyTextBox.Height = 55;
                QtyTextBox.TextWrapping = TextWrapping.Wrap;
            }
            else
            {
                PreviewKeyDown += ProcessKeyboard2;
            }

            InitForm();

            if (!qtyStringIsNotNullFlag)
            {
                FormHelper.RemoveFilter("QTY", FormHelperField.FieldFilterRef.Required);
            }

            FrameName = "ComplectationCMQuantityStackInPallet";

            SetVisibility();

            QtyString = text;

            QtyTextBox.Text = QtyString;
            QtyTextBox.CaretIndex = QtyTextBox.Text.Length;

            OkFlag = false;
        }

        /// <summary>
        /// Форма для подтверждения данных.
        /// Тип входных/выходных данных -- DateTime.ToString.
        /// Результирующее значение заносит в DateTimeValue.
        /// </summary>
        /// <param name="datetime"></param>
        public ComplectationCMQuantity(DateTime datetime)
        {
            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            ToDateTimeFlag = true;

            InitializeComponent();
            PreviewKeyDown += ProcessKeyboard2;
            InitForm();

            FrameName = "ComplectationCMQuantityStackInPallet";

            SetVisibility();

            if (datetime != null)
            {
                DateTimeValue = datetime.ToString("dd.MM.yyyy");
            }
            else
            {
                DateTimeValue = DateTime.Now.ToString("dd.MM.yyyy");
            }

            FormHelper.SetValueByPath("DATETIME", DateTimeValue);

            OkFlag = false;
        }

        /// <summary>
        /// Абстрактная сущность, которая означает числовое значение (double) чего-либо в зависимости от контекста.
        /// Заполняется в конструкторе класса и передаёт значение в textbox формы.
        /// Сейчас используется для подтверждения количества стоп на поддоне и толщины картона.
        /// </summary>
        public double QtyDouble { get; set; }

        /// <summary>
        /// Абстрактная сущность, которая означает числовое значение (int) чего-либо в зависимости от контекста.
        /// Заполняется в конструкторе класса и передаёт значение в textbox формы.
        /// Сейчас используется для подтверждения количества стоп на поддоне и толщины картона.
        /// </summary>
        public int QtyInt { get; set; }

        /// <summary>
        /// Абстрактная сущность, которая означает значение (string) чего-либо в зависимости от контекста.
        /// Заполняется в конструкторе класса и передаёт значение в textbox формы.
        /// </summary>
        public string QtyString { get; set; }

        /// <summary>
        /// Абстрактная сущность, которая означает значение (DateTime.ToString) чего-либо в зависимости от контекста.
        /// Заполняется в конструкторе класса и передаёт значение в textbox формы.
        /// </summary>
        public string DateTimeValue { get; set; }

        /// <summary>
        /// Имя фрейма
        /// </summary>
        public string FrameName { get; set; }

        public FormHelper FormHelper { get; set; }

        public bool OkFlag { get; set; }

        /// <summary>
        /// Флаг того, что данные нужно приводить к типу int
        /// </summary>
        public bool ToIntFlag { get; set; }

        /// <summary>
        /// Флаг того, что данные нужно приводить к типу string
        /// </summary>
        public bool ToStringFlag { get; set; }

        /// <summary>
        /// Флаг того, что данные нужно риводить к типу DateTime
        /// </summary>
        public bool ToDateTimeFlag { get; set; }

        private void InitForm()
        {
            FormHelper = new FormHelper();
            //список колонок формы

            var fields = new List<FormHelperField>();

            if (ToIntFlag)
            {
                fields = new List<FormHelperField>
                {
                    new FormHelperField
                    {
                        Path = "QTY",
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = QtyTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.DigitOnly, null },
                            { FormHelperField.FieldFilterRef.IsNotZero, null }
                        },
                    },
                };
            }
            else if (ToStringFlag)
            {
                fields = new List<FormHelperField>
                {
                    new FormHelperField
                    {
                        Path = "QTY",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = QtyTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                };
            }
            else if (ToDateTimeFlag)
            {
                fields = new List<FormHelperField>
                {
                    new FormHelperField()
                    {
                        Path="DATETIME",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = DateTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                };
            }
            else
            {
                fields = new List<FormHelperField>
                {
                    new FormHelperField
                    {
                        Path = "QTY",
                        FieldType = FormHelperField.FieldTypeRef.Double,
                        Control = QtyTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.DigitCommaOnly, null },
                            { FormHelperField.FieldFilterRef.IsNotZero, null }
                        },
                    },
                };
            }

            FormHelper.SetFields(fields);
        }

        /// <summary>
        /// Установка видимости контролов
        /// </summary>
        public void SetVisibility()
        {
            if (ToDateTimeFlag)
            {
                QtyTextBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                DateTimeGrid.Visibility = Visibility.Collapsed;

                if (ToIntFlag)
                {
                    NumericUpDownPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    NumericUpDownPanel.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// отображение фрейма
        /// </summary>
        /// <param name="labelText">Текст, отображаемый на форме возле поля ввода</param>
        /// <param name="dataType">Тип данных в поле ввода:
        /// 1 -- int;
        /// 2 -- float.
        /// </param>
        public void Show(string labelText)
        {
            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 2;
            Label.Content = labelText;

            Dictionary<string, string> windowParametrs = new Dictionary<string, string>();
            windowParametrs.Add("no_resize", "1");
            windowParametrs.Add("center_screen", "1");
            Central.WM.Show(FrameName, labelText, true, "add", this, "top", windowParametrs);
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m">Сообщение</param>
        private void ProcessMessages(ItemMessage m)
        {

        }

        public void Save()
        {
            if (FormHelper != null)
            {
                if (FormHelper.Validate())
                {
                    if (ToIntFlag)
                    {
                        QtyInt = QtyTextBox.Text.ToInt();
                    }
                    else if (ToStringFlag)
                    {
                        QtyString = QtyTextBox.Text;
                    }
                    else if (ToDateTimeFlag)
                    {
                        DateTimeValue = DateTextBox.Text;
                    }
                    else
                    {
                        QtyDouble = QtyTextBox.Text.ToDouble();
                    }

                    OkFlag = true;

                    Close();
                }
            } 
        }

        /// <summary>
        /// Обработчик нажатий клавиатуры
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ProcessKeyboard2(object sender, KeyEventArgs e)
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
            }
        }

        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Complectation",
                ReceiverName = "",
                SenderName = "ComplectationCMQuantityStackInPallet",
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

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void IncrementButton_Click(object sender, RoutedEventArgs e)
        {
            UIUtil.ChangeIntValue(QtyTextBox, 1, 0, int.MaxValue);
        }

        private void DecrementButton_Click(object sender, RoutedEventArgs e)
        {
            UIUtil.ChangeIntValue(QtyTextBox, 2, 0, int.MaxValue);
        }
    }
}
