using Client.Common;
using Client.Interfaces.Main;
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

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Форма ввода данных по размерам просечек
    /// </summary>
    public partial class NotchData : UserControl
    {
        /// <summary>
        /// Форма ввода данных по размерам просечек
        /// </summary>
        /// <param name="sizeNotch">Размер просечки</param>
        public NotchData(double sizeNotch = 0)
        {
            FrameName = "NotchData";
            SizeNotch = sizeNotch;

            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            //регистрация обработчика клавиатуры
            PreviewKeyDown += ProcessKeyboard;

            Init();
            SetDefaults();
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
        /// Размер просечки
        /// </summary>
        public double SizeNotch { get; set; }

        /// <summary>
        /// Номер строки грида
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// Условный номер грида (1 -- GridNotchesFirst; 2 -- GridNotchesSecond; 3 -- GridCrease)
        /// </summary>
        public int GridNumber { get; set; }

        /// <summary>
        /// Вид исполнения для коробочных решеток
        /// </summary>
        public int PartitionType { get; set; }

        /// <summary>
        /// Ид типа продукции. Используется для обработки ограничения, когда в коробочной решётке ширина просечки менее 55
        /// </summary>
        public int ProductClassId { get; set; }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void Init()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="CONTENT",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=SizeNotchTextBox,
                    ControlType="TextBox",
                    Format="N1",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitCommaOnly, null },
                    },
                },
            };

            Form.SetFields(fields);
            //Form.ToolbarControl = FormToolbar;

            //фокус на поле ввода размеров просечки
            SizeNotchTextBox.Focus();
        }

        public void SetDefaults()
        {
            Form.SetDefaults();

            if (SizeNotch > 0)
            {
                SizeNotchTextBox.Text = SizeNotch.ToString();

                //Выделение всего текста в текстовом поле
                SizeNotchTextBox.SelectAll();
            }
            else
            {
                SizeNotchTextBox.Clear();
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
            windowParametrs.Add("position_left", "200");
            windowParametrs.Add("position_top", "400");
            if (GridNumber == 1 || GridNumber == 2)
            {
                TitleOfNotchData.Content = "Размер просечки :";
                Central.WM.Show(FrameName, "Размер просечки", true, "add", this, null, windowParametrs);
            }
            else if (GridNumber == 3)
            {
                TitleOfNotchData.Content = "Размер рилёвки :";
                Central.WM.Show(FrameName, "Размер рилёвки", true, "add", this, null, windowParametrs);
            }
            
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {

        }

        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Preproduction",
                ReceiverName = "",
                SenderName = "NotchData",
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

        public void SetData()
        {
            if (Form != null)
            {
                var rowValue = Form.GetValues();
                rowValue.Add("NUMBER", Number.ToString());
                rowValue.Add("GRID_NUMBER", GridNumber.ToString());
                double min = 1;
                double max = 350;
                if (GridNumber == 1 || GridNumber == 2)
                {
                    min = 40;
                }
                if (PartitionType == 1)
                {
                    min = 28.5;
                    max = 260;
                }
                if (PartitionType == 2)
                {
                    min = 24;
                    max = 260;
                }
                if (Number > 1)
                {
                    if (SizeNotch >= min && SizeNotch <= max)
                    {
                        Messenger.Default.Send(new ItemMessage()
                        {
                            ReceiverGroup = "Preproduction",
                            ReceiverName = "TechnologicalMap",
                            SenderName = "NotchData",
                            Action = "Save",
                            Message = "",
                            ContextObject = rowValue,
                        }
                        );
                        Central.Msg.SendMessage(new ItemMessage()
                        {
                            ReceiverGroup = "Preproduction",
                            ReceiverName = "TechnologicalMap",
                            SenderName = "NotchData",
                            Action = "Save",
                            Message = "",
                            ContextObject = rowValue,
                        }
                        );
                        Close();
                    }
                    else
                    {
                        var msg = "Расстояние между не крайними просечками должно быть не менее " + min + " и не более " + max;
                        var d = new DialogWindow($"{msg}", "ТК решётки", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup = "Preproduction",
                        ReceiverName = "TechnologicalMap",
                        SenderName = "NotchData",
                        Action = "Save",
                        Message = "",
                        ContextObject = rowValue,
                    }
                    );

                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverGroup = "Preproduction",
                        ReceiverName = "TechnologicalMap",
                        SenderName = "NotchData",
                        Action = "Save",
                        Message = "",
                        ContextObject = rowValue,
                    }
                    );

                    Close();
                }

            }
        }

        public void OkButtonClick()
        {
            SizeNotch = SizeNotchTextBox.Text.ToDouble();
            SetData();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            OkButtonClick();
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
                    OkButtonClick();
                    break;

                case Key.Escape:
                    Close();
                    break;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
